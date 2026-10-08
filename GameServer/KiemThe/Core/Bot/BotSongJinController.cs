using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.SongJin;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.Logic.Manager.Battle;
using GameServer.Logic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace GameServer.KiemThe.Core.Bot
{
    // Admission only. Combat, scoring and battlefield revival are deliberately not implemented here.
    public sealed class BotSongJinController
    {
        private readonly KTBot bot;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private SongJinBotManager.Ticket ticket;
        private int returnMap, returnCamp;
        private Point returnPosition;
        private KTBot.BotActivity returnActivity;
        private bool transferred, leaving;
        private long retryAt;
        private string lastStatus;

        public BotSongJinController(KTBot bot) { this.bot = bot; }
        public bool IsParticipating => ticket != null;
        private bool enteredBattle;
        private long nextTeleportAttemptAt;

        private Monster battleMonster;
        private long nextMonsterSearchAt;
        private long nextCombatMoveAt;
        private long nextCombatAttackAt;
        private int battleSkillId = 0;
        private double battleAttackRange = 100;

        private bool IsEnemyMonster(Monster monster)
        {
            if (ticket == null || monster == null || monster.IsDead())
                return false;

            if (monster.CurrentMapCode != ticket.Admission.Map ||
                monster.CurrentCopyMapID != bot.CurrentCopyMapID)
            {
                return false;
            }

            int enemyCamp = ticket.Camp == 10 ? 20 : 10;

            return monster.Camp == enemyCamp &&
                   KTLogic.IsOpposite(bot, monster);
        }

        public void PauseForNoPlayers()
        {
            battleMonster = null;
            nextMonsterSearchAt = 0;
            nextCombatMoveAt = 0;
            nextCombatAttackAt = 0;
        }

        private void FightBattleMonsters()
        {
            if (ticket == null || bot.IsDead() ||
                bot.QuestExecutionDisabled ||
                bot.CurrentMapCode != ticket.Admission.Map)
            {
                return;
            }

            var state = SongJinBotManager.GetState(ticket);

            if (state != BattelStatus.STATUS_START &&
                state != BattelStatus.STATUS_PREPAREEND)
            {
                return;
            }

            if (!TrySelectBattleSkill())
                return;

            long now = clock.ElapsedMilliseconds;

            // Chỉ tìm lại khi mục tiêu cũ không còn hợp lệ.
            if (!IsEnemyMonster(battleMonster))
            {
                if (battleMonster != null)
                    KTOtherObjectStoryBoard.Instance.Remove(bot);

                battleMonster = null;

                // Không quét cả map liên tục mỗi tick.
                if (now < nextMonsterSearchAt)
                    return;

                nextMonsterSearchAt = now + 2000;

                battleMonster = GameManager.MonsterMgr
                    .GetObjectsByMap(
                        bot.CurrentMapCode,
                        bot.CurrentCopyMapID)
                    .Where(m => IsEnemyMonster(m))
                    .OrderBy(m =>
                        (m.CurrentPos - bot.CurrentPos).LengthSquared)
                    .FirstOrDefault();

                if (battleMonster == null)
                    return;

                nextCombatMoveAt = 0;
            }

            double distance =
                (battleMonster.CurrentPos - bot.CurrentPos).Length;

            // Ngoài tầm đánh: chạy tới quái.
            if (distance > battleAttackRange)
            {
                // Đang đi thì không xóa và tạo lại đường mỗi tick.
                if (!KTOtherObjectStoryBoard.Instance.HasStoryBoard(bot) &&
                    now >= nextCombatMoveAt)
                {
                    nextCombatMoveAt = now + 3000;

                    bot.CheckQuestCancellation("SongJinChaseMonster");
                    bot.MoveTo(battleMonster.CurrentPos);
                }

                return;
            }

            // Đã vào tầm: dừng chạy rồi đánh.
            KTOtherObjectStoryBoard.Instance.Remove(bot);

            if (now < nextCombatAttackAt)
                return;

            nextCombatAttackAt = now + 1000;

            bot.CheckQuestCancellation("SongJinAttackMonster");
            bot.UseSkill(battleSkillId, 1, battleMonster);
        }

        public void Tick()
        { 
            if (clock.ElapsedMilliseconds < retryAt) return;
            if (ticket == null)
            {
                if (bot.QuestExecutionDisabled || bot.IsDead() || bot.CurrentCopyMapID != -1 ||
                    bot.IsWaitingForRevive || bot.QuestController.IsPausedForRevive) return;
                ticket = SongJinBotManager.TryReserve(bot);
                if (ticket == null) return;
                enteredBattle = false;
                nextTeleportAttemptAt = 0;
                returnMap = bot.CurrentMapCode;
                returnPosition = bot.CurrentPos;
                returnCamp = bot.Camp;
                returnActivity = bot.CurrentActivity;
                bot.QuestController.PauseForActivity();
                bot.MainQuestActions.PrepareForRevive();
                bot.CurrentActivity = KTBot.BotActivity.SongJin;
                try
                {
                    KTBotManager.MoveSongJinBot(bot, ticket.Admission.Map, ticket.Position, ticket.Camp);
                    transferred = true;
                    if (!SongJinBotManager.Activate(ticket))
                    {
                        leaving = true;
                        Log("JoinCancelled", "Registration closed during transfer");
                    }
                    else Log("Waiting", "Da bao danh; dang cho tran bat dau");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Log("JoinFailed", ex.Message);
                    leaving = true;
                }
            }

            var state = SongJinBotManager.GetState(ticket);
            if (leaving || (state != BattelStatus.STATUS_PREPARE && state != BattelStatus.STATUS_START &&
                state != BattelStatus.STATUS_PREPAREEND))
            {
                Leave();
                return;
            }
            if (bot.IsDead())
            {
                Log("Dead", "BOT chet; cho ket thuc tran de tro ve");
                enteredBattle = false;
                nextTeleportAttemptAt = 0;
                return;
            }
            if (state == BattelStatus.STATUS_START ||
                state == BattelStatus.STATUS_PREPAREEND)
            {
                if (!enteredBattle &&
                    clock.ElapsedMilliseconds >= nextTeleportAttemptAt)
                {
                    nextTeleportAttemptAt = clock.ElapsedMilliseconds + 5000;

                    if (TeleportBotIntoBattle())
                    {
                        enteredBattle = true;

                        Console.WriteLine(
                            $"BOT {bot.RoleName} ({bot.RoleID}) đã vào trận");
                    }
                }

                // Logic tìm đối thủ và đánh nhau sau này đặt ở đây.
                FightBattleMonsters();
            }
        }

        private bool TeleportBotIntoBattle()
        {
            if (ticket == null || bot.IsDead() || bot.QuestExecutionDisabled)
                return false;

            var state = SongJinBotManager.GetState(ticket);

            if (state != BattelStatus.STATUS_START &&
                state != BattelStatus.STATUS_PREPAREEND)
            {
                return false;
            }

            if (bot.CurrentMapCode != ticket.Admission.Map)
                return false;

            try
            {
                var destination =
                    Battel_SonJin_Manager.GetRandomBattlePosition(ticket.Camp);

                KTBotManager.MoveSongJinBot(
                    bot,
                    ticket.Admission.Map,
                    new Point(destination.x, destination.y),
                    ticket.Camp);

                Log(
                    "EnteredBattle",
                    "BOT da ra chien truong tai " + bot.CurrentPos);

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log("TeleportFailed", ex.Message);
                return false;
            }
        }

        private void Leave()
        {
            leaving = true;
            try
            {
                if (transferred)
                    KTBotManager.MoveSongJinBot(bot, returnMap, returnPosition, returnCamp);
                bot.MainQuestActions.PrepareForRevive();
                bot.QuestController.ResumeAfterActivity();
                bot.CurrentActivity = returnActivity;
                Log("Left", "Da roi tran va khoi phuc hoat dong truoc do");
                SongJinBotManager.Release(ticket);
                ticket = null;
                transferred = leaving = false;
                lastStatus = null;
                retryAt = clock.ElapsedMilliseconds + 30000;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log("ReturnFailed", ex.Message);
                retryAt = clock.ElapsedMilliseconds + 10000;
            }
        }

        private const long ReviveDelayMs = 10000;

        private bool waitingBattleRevive;
        private long battleReviveAt;
        private void HandleBattleRevive()
        {
            long now = clock.ElapsedMilliseconds;
            if (!waitingBattleRevive)
            {
                waitingBattleRevive = true;
                battleReviveAt = now + ReviveDelayMs;

                KTOtherObjectStoryBoard.Instance.Remove(bot);
                bot.ResetAI();
                bot.MainQuestActions.PrepareForRevive();

                Log("WaitingRevive", "BOT chet; hoi sinh sau 10 giay");
                return;
            }
            if (now < battleReviveAt)
                return;

            var state = SongJinBotManager.GetState(ticket);

            if (leaving ||
                (state != BattelStatus.STATUS_PREPARE &&
                 state != BattelStatus.STATUS_START &&
                 state != BattelStatus.STATUS_PREPAREEND))
            {
                Leave();
                return;
            }

            try
            {
                bot.Camp = ticket.Camp;
                KTBotManager.ReviveQuestBot(
                    bot,
                    ticket.Admission.Map,
                    ticket.Position);

                if (bot.IsDead())
                {
                    throw new InvalidOperationException(
                        "BOT van o trang thai chet sau khi hoi sinh");
                }

                waitingBattleRevive = false;
                battleReviveAt = 0;

                Log("Revived", "BOT da hoi sinh tai khu cho cua phe");
            }
            catch (OperationCanceledException)
            {
                // Để scheduler xử lý hủy BOT.
                throw;
            }
            catch (Exception ex)
            {
                // Chờ 5 giây rồi thử lại, không lặp liên tục.
                battleReviveAt = clock.ElapsedMilliseconds + 5000;

                Log("ReviveFailed", ex.Message);
            }
        }

        private bool TrySelectBattleSkill()
        {
            // Dùng cùng cách xác định loại vũ khí
            // đang có trong BotMainQuestCombat.FightQuest().
            int weaponCategory = 11;

            ItemData weapon;
            if (ItemManager._TotalGameItem.TryGetValue(
                bot.WeaponID, out weapon))
            {
                weaponCategory = weapon.Category;
            }

            battleSkillId = 0;

            // Source nhiệm vụ hiện tìm đòn cơ bản trong ID 1–20.
            for (int id = 1; id <= 20; id++)
            {
                var skill = KSkill.GetSkillData(id);

                if (skill == null)
                    continue;

                if (!skill.IsBasicWeaponSkill)
                    continue;

                if (skill.WeaponLimit != weaponCategory)
                    continue;

                battleSkillId = id;

                // Lấy tầm tiếp cận theo kỹ năng đã chọn.
                battleAttackRange = Math.Max(
                    40,
                    skill.AttackRadius * 0.8);

                return true;
            }

            Log(
                "CombatConfigMissing",
                "Khong tim thay don danh co ban. WeaponID=" +
                bot.WeaponID + " Category=" + weaponCategory);

            return false;
        }

        public void Cleanup()
        {
            if (ticket == null) return;
            Log("Removed", "BOT scheduler cleanup");
            SongJinBotManager.Release(ticket);
            ticket = null;
        }

        private void Log(string status, string reason)
        {
            if (lastStatus == status) return;
            lastStatus = status;
            BotRuntimeLog.Write("[BotSongJin] Bot=" + bot.RoleID + " Name=" + bot.RoleName +
                " Battle=" + ticket.Admission.Name + " Round=" + ticket.Admission.Round +
                " Camp=" + (ticket.Camp == 10 ? "Tong" : "Kim") + " State=" + status +
                " Map=" + bot.CurrentMapCode + " Pos=" + bot.CurrentPos + " Reason=" + reason);
        }


    }
}
