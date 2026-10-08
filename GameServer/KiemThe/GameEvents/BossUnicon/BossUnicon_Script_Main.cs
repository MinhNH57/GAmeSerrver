using GameServer.Core.Executor;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.Interface;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents.BossUnicon
{
    /// <summary>
    /// Script chính thực thi sự kiện 
    /// </summary>
    public class BossUnicon_Script_Main : GameMapEvent
    {
        #region Define
        /// <summary>
        /// Thông tin tầng
        /// </summary>
        private readonly BossUnicon.EventDetail.Stage stageInfo;

        /// <summary>
        /// Danh sách Boss đã được tạo ra
        /// </summary>
        private readonly HashSet<string> createdBosses;


        /// <summary>
        /// Thời điểm tiếp theo tạo MiniBoss tương ứng
        /// </summary>
        private readonly ConcurrentDictionary<BossUnicon.EventDetail.Stage.MiniBossInfo, long> nextCreateMiniBosses;

        /// <summary>
        /// Thời điểm Tick lần trước
        /// </summary>
        private long lastTicks = 0;

        private List<Monster> listBosses;
        #endregion

        #region Core GameMapEvent
        /// <summary>
        /// Script chính thực thi sự kiện 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="activity"></param>
        public BossUnicon_Script_Main(GameMap map, KTActivity activity) : base(map, activity)
        {
            this.stageInfo = BossUnicon.Event.Stages[map.MapCode];
            this.createdBosses = new HashSet<string>();
            this.nextCreateMiniBosses = new ConcurrentDictionary<BossUnicon.EventDetail.Stage.MiniBossInfo, long>();
            this.listBosses = new List<Monster>();
        }

        /// <summary>
        /// Sự kiện bắt đầu
        /// </summary>
        protected override void OnStart()
        {
            // Xóa toàn bộ quái
            this.RemoveAllMonsters();
            // Xóa toàn bộ NPC
            this.RemoveAllNPCs();
            // Xóa toàn bộ cổng dịch chuyển
            this.RemoveAllDynamicAreas();

            // Tạo quái
            this.CreateMonsters();
            // Tạo NPC
            this.CreateNPCs();
            // Tạo cổng dịch chuyển
            this.CreateTeleports();
        }

        /// <summary>
        /// Sự kiện Tick
        /// </summary>
        protected override void OnTick()
        {
            // Nếu chưa đến thời gian
            if (KTGlobal.GetCurrentTimeMilis() - this.lastTicks < 5000)
            {
                return;
            }
            // Đánh dấu thời gian
            this.lastTicks = KTGlobal.GetCurrentTimeMilis();
            // Tạo Boss ở thời gian tương ứng
            this.SpawnBossInTime();
            // Tạo MiniBoss
            this.SpawnMiniBossInTime();
            // Xóa Boss nếu đến thời gian mà chưa chết
            if (BossUnicon_ActivityScript.timeBossAppear > 0)
            {
                this.RemoveBoss();
            }
        }

        /// <summary>
        /// Sự kiện kết thúc
        /// </summary>
        protected override void OnClose()
        {
            // Làm rỗng danh sách Boss đã tạo
            this.createdBosses.Clear();

            // Duyệt danh sách người chơi
            foreach (KPlayer player in this.GetPlayers())
            {
                // Đưa ra khỏi 
                BossUnicon_ActivityScript.KickOut(player);
            }
        }

        /// <summary>
        /// Sự kiện khi người chơi vào bản đồ sự kiện
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerEnter(KPlayer player)
        {
            base.OnPlayerEnter(player);

            DateTime now = DateTime.Now;

            int day = TimeUtil.GetWeekDay1To7(now);

            if (day == 2)
            {
                // đổi trạng thái PK của người chơi
                player.PKMode = (int)PKMode.Guild;
            }  
            else
            {
                player.PKMode = (int)PKMode.Peace;
            }    
            

            // Mở bảng thông báo hoạt động
            this.OpenEventBroadboard(player, 900);
            // Thời gian còn lại ở 
            int totalSecLeft = BossUnicon_ActivityScript.GetTodayTotalSecLeft(player);
            // Cập nhật thông tin
            this.UpdateEventDetailsToPlayers("Thời gian còn lại", totalSecLeft, "Hãy tiêu diệt Boss");
        }

        /// <summary>
        /// Sự kiện khi người chơi rời khỏi bản đồ sự kiện
        /// </summary>
        /// <param name="player"></param>
        /// <param name="toMap"></param>
        public override void OnPlayerLeave(KPlayer player, GameMap toMap)
        {
            base.OnPlayerLeave(player, toMap);

            // trả về trạng thái PK cho người chơi
            player.PKMode = (int)PKMode.Peace;
            // Toác
            if (toMap == null)
            {
                return;
            }

            // Nếu bản đồ đích không thuộc 
            if (!BossUnicon.Event.Stages.ContainsKey(toMap.MapCode))
            {
                // Đóng bảng thông báo hoạt động
                this.CloseEventBroadboard(player, 900);
                //xóa bộ nhớ
                BossUnicon_ActivityScript.mapIDHasBuff.TryRemove(player.RoleID, out _);
                BossUnicon_ActivityScript.timeDurationDMC.TryRemove(player.RoleID, out _);
            }

            // Hủy khu an toàn
            player.IsInsideSafeZone = false;

            // ghi lại log trước khi thoát
            int totalSecLeft = BossUnicon_ActivityScript.GetTodayTotalSecLeft(player);
            LogManager.WriteLog(LogTypes.BossUnicon, $"Người chơi: {player.RoleName} đã rời khỏi , thời gian còn lại là: {totalSecLeft}");
        }

        /// <summary>
        /// Sự kiện khi người chơi ấn nút hồi sinh
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool OnPlayerClickReliveButton(KPlayer player)
        {
            // Đưa về khu an toàn tầng 1
            PlayerManager.Relive(player, BossUnicon.Event.Relive.MapID, BossUnicon.Event.Relive.PosX, BossUnicon.Event.Relive.PosY, 100, 100, 100);

            // Trả về True
            return true;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Tạo quái
        /// </summary>
        private void CreateMonsters()
        {
            // Duyệt danh sách thiết lập quái
            foreach (BossUnicon.EventDetail.Stage.MonsterInfo monsterInfo in this.stageInfo.Monsters)
            {
                // Hướng quay
                Entities.Direction dir = Entities.Direction.NONE;
                // Mức máu
                int hp = monsterInfo.BaseHP + monsterInfo.HPIncreaseEachLevel * BossUnicon.Config.MonsterLevel;
                // Ngũ hành
                int rand = KTGlobal.GetRandomNumber(1, 5);
                KE_SERIES_TYPE series = (KE_SERIES_TYPE)rand;

                // Tạo quái
                GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, monsterInfo.ID, -1, 1, monsterInfo.PosX, monsterInfo.PosY, monsterInfo.Name, monsterInfo.Title, hp, BossUnicon.Config.MonsterLevel, dir, series, monsterInfo.AIType, monsterInfo.RespawnTicks, -1, monsterInfo.AIScriptID, null, (monster) => {
                    // Nếu có kỹ năng
                    if (monsterInfo.Skills.Count > 0)
                    {
                        // Duyệt danh sách kỹ năng
                        foreach (SkillLevelRef skill in monsterInfo.Skills)
                        {
                            // Thêm vào danh sách kỹ năng dùng của quái
                            monster.CustomAISkills.Add(skill);
                        }
                    }

                    // Nếu có vòng sáng
                    if (monsterInfo.Auras.Count > 0)
                    {
                        // Duyệt danh sách vòng sáng
                        foreach (SkillLevelRef aura in monsterInfo.Auras)
                        {
                            // Kích hoạt vòng sáng
                            monster.UseSkill(aura.SkillID, aura.Level, monster);
                        }
                    }

                    // add thêm kháng
                    // thêm kháng tất cả cho boss (500)
                    for (int nType = (int)DAMAGE_TYPE.damage_physics; nType <= (int)DAMAGE_TYPE.damage_magic; ++nType)
                    {
                        monster.AddCurResist((DAMAGE_TYPE)nType, 100);
                        //Console.WriteLine("Add {0} to {1}: +{2}", (DAMAGE_TYPE) nType, obj.RoleName, magic.nValue[0] * stackCount);
                    }
                }, 65535, () => {
                    // Nếu hoạt động đã bị hủy thì thôi
                    return !this.isDisposed;
                });
            }
        }

        /// <summary>
        /// Tạo Boss tương ứng
        /// </summary>
        /// <param name="bossInfo"></param>
        /// <param name="onDie"></param>
        /// <param name="onTick"></param>
        /// <param name="onTakeDamage"></param>
        private void CreateBoss(BossUnicon.EventDetail.Stage.BossInfo bossInfo, Action<GameObject> onDie, Action<GameObject> onTick)
        {
            // Hướng quay
            KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
            // Mức máu
            int hp = bossInfo.BaseHP + bossInfo.HPIncreaseEachLevel * BossUnicon.Config.MonsterLevel;
            // Ngũ hành
            int rand = KTGlobal.GetRandomNumber(1, 5);
            KE_SERIES_TYPE series = (KE_SERIES_TYPE)rand;

            // Tạo Boss
            bool resultCreate = GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, bossInfo.ID, -1, 1, bossInfo.PosX, bossInfo.PosY, bossInfo.Name, bossInfo.Title, hp, BossUnicon.Config.MonsterLevel, dir, series, bossInfo.AIType, -1, -1, bossInfo.AIScriptID, null, (boss) => {
                // Thông báo
                //this.NotifyAllPlayers(string.Format("{0} đã xuất hiện, anh hùng hào kiệt hãy nhanh chân tiêu diệt!", boss.RoleName));
                this.listBosses.Add(boss);
                // Cho hiện trên Minimap
                boss.UpdatePositionToLocalMapContinuously = true;
                // Thực thi sự kiện Tick
                boss.OnTick = () => {
                    onTick?.Invoke(boss);
                };

                // Nếu có kỹ năng
                if (bossInfo.Skills.Count > 0)
                {
                    // Duyệt danh sách kỹ năng
                    foreach (SkillLevelRef skill in bossInfo.Skills)
                    {
                        // Thêm vào danh sách kỹ năng dùng của quái
                        boss.CustomAISkills.Add(skill);
                    }
                }

                // Nếu có vòng sáng
                if (bossInfo.Auras.Count > 0)
                {
                    // Duyệt danh sách vòng sáng
                    foreach (SkillLevelRef aura in bossInfo.Auras)
                    {
                        // Kích hoạt vòng sáng
                        boss.UseSkill(aura.SkillID, aura.Level, boss);
                    }
                }

                if (bossInfo.Reduce != 0)
                {
                    boss.ChangeReduceReceivedDamage(bossInfo.Reduce);
                }

                if (bossInfo.IgnoreSerie != 0)
                {
                    // thêm kháng tất cả cho boss (500)
                    for (int nType = (int)DAMAGE_TYPE.damage_physics; nType <= (int)DAMAGE_TYPE.damage_magic; ++nType)
                    {
                        boss.AddCurResist((DAMAGE_TYPE)nType, bossInfo.IgnoreSerie);
                    }

                }

                boss.OnDieCallback = (killer) =>
                {
                    

                    int damagePlayer = this.stageInfo.Bosses.Where(x => x.ID == boss.MonsterInfo.ExtensionID).FirstOrDefault().DamagePlayer;
                    int damageGuild = this.stageInfo.Bosses.Where(x => x.ID == boss.MonsterInfo.ExtensionID).FirstOrDefault().DamageGuild;
                    // nếu là Lam kỳ lân
                    if (boss.MonsterInfo.ExtensionID == 22648)
                    {
                        // thông báo đã giết được boss
                        const string congratNotificationFormat = "Sau 300 hiệp đại chiến, {0} dẫn dắt nhân sĩ giang hồ tiêu diệt thành công Lam Kỳ Lân, nhận được phần quà đặc biệt giá trị. ";

                        var congratNotification = string.Format(
                        congratNotificationFormat,
                        KTGlobal.CreateStringByColor(killer.RoleName, ColorType.Yellow));

                        KTGlobal.SendSystemEventNotification(congratNotification);

                        var TimeUsing = Global.ConstGoodsEndTime;

                        // gửi phần thưởng cho thằng lasthit
                        /// kiểm tra có đủ chỗ trống trong túi đồ không
                        if (KTGlobal.IsHaveSpace(1, (KPlayer)killer))
                        {
                            var td = DateTime.Now.AddMinutes(30 * 24 * 60);

                            TimeUsing = td.ToString("yyyy-MM-dd HH:mm:ss");

                            if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, (KPlayer)killer, 25232, 1, 0, "BossUnicon", false, 0, false, TimeUsing))
                            {
                                //PlayerManager.ShowMessageBox((KPlayer)killer, "Thông báo", "Bạn đã nhận thưởng khi tiêu diệt boss Lam Kỳ Lân, hãy kiểm tra túi đồ!");
                                LogManager.WriteLog(LogTypes.BossUnicon, $"Player {killer.RoleName} roleID = {killer.RoleID} đã tiêu diệt được {bossInfo.Name}, nhận thưởng Rương Hỏa Kỳ Lân thành công");
                            }
                            else
                            {
                                //PlayerManager.ShowMessageBox((KPlayer)killer, "Thông báo", "Nhận thưởng tiêu diệt boss lam Kỳ Lân lỗi, hãy liên hệ nhà phát hành!");
                                LogManager.WriteLog(LogTypes.BossUnicon, $"Player {killer.RoleName} roleID = {killer.RoleID} nhận thưởng lỗi");
                            }
                        }  
                        else
                        {
                            LogManager.WriteLog(LogTypes.BossUnicon, $"Player {killer.RoleName} roleID = {killer.RoleID} nhận thưởng lỗi, do túi đồ đầy");
                        }    
                        
                        // lưu lại damage của bang hôi tham gia hạ gục boss Lam
                        BossUnicon.dameGuilds = boss.DamageGuildIDRecord.Values.Where(x => x.GuildID != -1).ToList().Clone();

                        // duyệt danh sách bang hội damage cao nhất
                        var listGuild = boss.DamageGuildIDRecord.Values.Where(x => x.TotalDamage > damageGuild && x.GuildID > 0).ToList();

                        if (listGuild != null)
                        {
                            foreach( var guild in listGuild)
                            {
                                LogManager.WriteLog(LogTypes.BossUnicon, $"GuildID = {guild.GuildID} có lượng sát thương = {guild.TotalDamage} đủ điều kiện nhận thẻ bang hội");
                                foreach (var playerID in guild.ID)
                                {
                                    LogManager.WriteLog(LogTypes.BossUnicon, $"PlayerID = {playerID} có trong danh sách nhận thẻ bang hội");
                                    // gửi cho mỗi thằng 1 thẻ có thể dùng trong 6 ngày
                                    KPlayer player = GameManager.ClientMgr.FindClient(playerID);
                                    if (player != null)
                                    {
                                        if (KTGlobal.IsHaveSpace(1, player))
                                        {
                                            /// thẻ shop bang hội
                                            int itemID = 25233;

                                            var dt = DateTime.Now.AddMinutes(6 * 24 * 60);
                                            TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                            if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, itemID, 1, 0, "BossUnicon", false, 0, false, TimeUsing))
                                            {
                                                LogManager.WriteLog(LogTypes.BossUnicon, $"Player {player.RoleName} roleID = {player.RoleID} đã nhận được thẻ bang hội trong vòng 6 ngày thành công");
                                            }
                                            else
                                            {
                                                LogManager.WriteLog(LogTypes.BossUnicon, $"Player {player.RoleName} roleID = {player.RoleID} đã nhận được thẻ bang hội trong vòng 6 ngày thất bại");
                                            }
                                        }
                                        else
                                        {
                                            LogManager.WriteLog(LogTypes.BossUnicon, $"Player {player.RoleName} roleID = {player.RoleID} đã nhận được thẻ bang hội trong vòng 6 ngày thất bại, do túi đồ đầy");
                                        }
                                    }    
                                    else
                                    {
                                        LogManager.WriteLog(LogTypes.BossUnicon, $"Không tìm thấy nhân vật có PlayerID =  {playerID} ");
                                    }    
                                }
                            }   
                        }
                    }

                    // duyệt danh sách dame xem bang nào nhiều dame nhất
                    BossUnicon.dameGroups = boss.DamagePlayerIDRecord.Values.Where(x => x.TotalDamage >= damagePlayer).OrderByDescending(x => x.TotalDamage).ToList();
                    if (BossUnicon.dameGroups != null)
                    {
                        foreach (var playerID in BossUnicon.dameGroups)
                        {
                            // ghi lại log ds những thằng nhận được quà qua npc
                            LogManager.WriteLog(LogTypes.BossUnicon, $"PlayerID: {playerID.ID} có lượng damage = {playerID.TotalDamage} đủ điện kiện nhận thưởng sát thương qua npc ");
                        }
                        this.CreateNPCSpecial(20203, bossInfo.PosX, bossInfo.PosY, 235);
                    }                        
                };

            }, 65535, null, onDie);
            if (bossInfo.Name != "")
            {
                KTGlobal.SendSystemEventNotification($"{bossInfo.Name} đã xuất hiện tại , Chỉ nhận sát thương nội.");
            }
            // thêm log sau khi tạo boss
            LogManager.WriteLog(LogTypes.BossUnicon, $"Tạo ra boss {bossInfo.Name} thành công hay không: {resultCreate}");
        }

        /// <summary>
        /// Tạo cổng dịch chuyển tương ứng
        /// </summary>
        private void CreateTeleports()
        {
            // Duyệt danh sách cổng dịch chuyển
            foreach (BossUnicon.EventDetail.Stage.TeleportInfo teleportInfo in this.stageInfo.Teleports)
            {
                // Tạo cổng dịch chuyển
                KDynamicArea teleport = KTDynamicAreaManager.Add(this.Map.MapCode, -1, teleportInfo.Name, 5340, teleportInfo.PosX, teleportInfo.PosY, -1, 2f, 100, -1, null);
            }
        }

        /// <summary>
        /// Tạo NPC tương ứng
        /// </summary>
        private void CreateNPCs()
        {
            // Duyệt danh sách NPC
            foreach (BossUnicon.EventDetail.Stage.NPCInfo npcInfo in this.stageInfo.NPCs)
            {
                // Tạo NPC
                NPCGeneralManager.AddNewNPC(this.Map.MapCode, -1, npcInfo.ID, npcInfo.PosX, npcInfo.PosY, "", "", Entities.Direction.DOWN_RIGHT, npcInfo.ScriptID, "", (npc) => {
                    npc.VisibleOnMinimap = true;
                    npc.MinimapName = npc.Name;
                });
            }
        }

        /// <summary>
        /// tạo npc sau khi boss chết
        /// </summary>
        /// <param name="resID"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="scriptID"></param>
        public void CreateNPCSpecial(int resID, int posX, int posY, int scriptID)
        {
            NPCGeneralManager.AddNewNPC(this.Map.MapCode, -1, resID, posX, posY, "", "", Entities.Direction.DOWN_RIGHT, scriptID, "", (npc) =>
            {
                npc.VisibleOnMinimap = true;
                npc.MinimapName = "Sứ giả Hỏa Kỳ Lân";
            });

        }

        private void RemoveBoss()
        {
            long currentTime = KTGlobal.GetCurrentTimeMilis();
            long timeAliveOfBoss = 3600000 * 3 / 2;
            // nếu hết thời gian tồn tại quái thì ko cho xuất hiện nữa
            if ((currentTime - BossUnicon_ActivityScript.timeBossAppear) > timeAliveOfBoss)
            {
                BossUnicon_ActivityScript.timeBossAppear = 0;
                if (!this.listBosses.Any() || this.listBosses != null)
                {
                    foreach (Monster monster in this.listBosses)
                    {
                        this.RemoveMonster(monster);
                        // xóa khỏi ds bigBoss tồn tại
                        BossUnicon.bigBossIDs.TryRemove(monster.MonsterInfo.ExtensionID, out _);
                        LogManager.WriteLog(LogTypes.BossUnicon, $"{monster.MonsterInfo.Name} đã hết thời gian xuất hiện, đã bị xóa khỏi map sự kiện");
                    }
                }
                // hủy acivity boss 
                KTActivityManager.StopActivity(901);
            }
        }

        /// <summary>
        /// Tạo Boss ở thời gian tương ứng
        /// </summary>
        private void SpawnBossInTime()
        {
            // Thời gian hiện tại
            DateTime now = DateTime.Now;
            // Giờ
            int hour = now.Hour;
            // Phút
            int minute = now.Minute;
            // Ngày
            int day = now.Day;
            // Tháng
            int month = now.Month;
            // Năm
            int year = now.Year;
            // Mã hóa thời gian
            string dateTimeKey = string.Format("{0}_{1}_{2}_{3}_{4}", day, month, year, hour, minute);

            int dayOfWeek = TimeUtil.GetWeekDay1To7(now);

            // Nếu đã tạo Boss ở thời gian này rồi thì thôi
            if (this.createdBosses.Contains(dateTimeKey))
            {
                return;
            }

            // Thông tin Boss ở thời gian này
            BossUnicon.EventDetail.Stage.BossInfo bossInfo = this.stageInfo.Bosses.Where(x =>x.SpawnAt.Day == dayOfWeek && x.SpawnAt.Hour == hour && x.SpawnAt.Minute == minute).FirstOrDefault();
            // Nếu toác
            if (bossInfo == null)
            {
                return;
            }
            // Nếu boss vẫn còn tồn tại thì thôi
            if (BossUnicon.bigBossIDs.TryGetValue(this.stageInfo.MapID, out int bossID))
            {
                if (bossInfo.ID == bossID)
                {
                    return;
                }
            }
            // Đánh dấu đã tạo Boss ở thời gian này
            this.createdBosses.Add(dateTimeKey);
            // đánh dấu là boss đã tồn tại
            BossUnicon.bigBossIDs[this.stageInfo.MapID] = bossInfo.ID;
            // Thời điểm lần trước cập nhật Boss thuộc về đội nào
            long lastTickUpdateBelonging = KTGlobal.GetCurrentTimeMilis();
            // Đã thông báo miễn sát thương hay chưa?
            int hasSendNoti = 0;
            // them log cho chac chan co ra boss hay ko
            LogManager.WriteLog(LogTypes.BossUnicon, $" bắt đầu tạo ra boss {bossInfo.Name} vào {dateTimeKey}");
            // Tạo Boss tương ứng
            this.CreateBoss(bossInfo, (killer) => {
                // Nếu không phải người chơi
                if (!(killer is KPlayer killerPlayer))
                {
                    return;
                }

                // Thực hiện Logic giết Boss 
                this.ProcessPlayerKillBossLogic(killerPlayer, bossInfo);
            }, (boss) => {

                // % máu hiện tại
                int nHPPercent = (int)(boss.m_CurrentLife / (float)boss.m_CurrentLifeMax * 100);
                // Nếu còn trên 50% máu
                if (nHPPercent >= 55)
                {
                    // Miễn dịch sát thương ngoại
                    boss.m_ImmuneToPhysicDamage = true;
                    boss.m_ImmuneToMagicDamage = false;
                    if (hasSendNoti == 0)
                    {
                        hasSendNoti = 1;
                    }
                }
                // Nếu còn dưới 50% máu
                else if (nHPPercent >= 11)
                {
                    // Miễn dịch sát thương nội
                    boss.m_ImmuneToMagicDamage = true;
                    boss.m_ImmuneToPhysicDamage = false;
                    if (hasSendNoti == 1)
                    {
                        KTGlobal.SendSystemEventNotification($"{bossInfo.Name} đã suy yếu chỉ nhận sát thương ngoại.");
                        LogManager.WriteLog(LogTypes.BossUnicon, $"{bossInfo.Name} đã suy yếu chỉ nhận sát thương ngoại. (HP={nHPPercent}%)");
                        hasSendNoti = 2;
                    }
                }
                else
                {
                    // bỏ miễn sát thương
                    boss.m_ImmuneToMagicDamage = false;
                    boss.m_ImmuneToPhysicDamage = false;
                    if (hasSendNoti == 2)
                    {
                        KTGlobal.SendSystemEventNotification($"{bossInfo.Name} đã trọng thương nhận tất cả sát thương nội ngoại.");
                        LogManager.WriteLog(LogTypes.BossUnicon, $"{bossInfo.Name} đã trọng thương nhận tất cả sát thương nội ngoại. (HP={nHPPercent}%)");
                        hasSendNoti = 3;
                    }
                }

                // Nếu chưa đến thời điểm cập nhật
                if (KTGlobal.GetCurrentTimeMilis() - lastTickUpdateBelonging < 5000)
                {
                    return;
                }
                

                // Đối tượng quái tương ứng
                Monster monster = boss as Monster;
                // Toác
                if (monster == null)
                {
                    return;
                }

                // Nếu có sát thương
                if (monster.DamageTakeRecord.Count > 0)
                {
                    // Thời điểm hiện tại
                    long currentTicks = TimeUtil.NOW();

                    // Đánh dấu thời điểm cập nhật
                    lastTickUpdateBelonging = KTGlobal.GetCurrentTimeMilis();

                    // Danh sách sát thương cao nhất
                    Monster.DamgeGroup topDamage = monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).FirstOrDefault();

                    // Nếu có sát thương
                    if (topDamage.TotalDamage > 0)
                    {
                        // Nếu là nhóm
                        if (topDamage.IsTeam)
                        {
                            // Đội trưởng
                            KPlayer teamLeader = KTTeamManager.GetTeamLeader(topDamage.ID);
                            // Nếu tồn tại
                            if (teamLeader != null)
                            {
                                monster.Title = "<b><color=#00ff2a>(Thuộc đội: " + teamLeader.RoleName + ")</color></b>";
                                // ghi log
                                LogManager.WriteLog(LogTypes.BossUnicon, $"Log tính sát thương: Reset title cho boss thành công! thuộc về {teamLeader.RoleName}");
                            }
                        }
                        else
                        {
                            // Người chơi tương ứng
                            KPlayer player = GameManager.ClientMgr.FindClient(topDamage.ID);
                            // Nếu tồn tại
                            if (player != null)
                            {
                                monster.Title = "<b><color=#00ff2a>(Thuộc về: " + player.RoleName + ")</color></b>";
                                // ghi log
                                LogManager.WriteLog(LogTypes.BossUnicon, $"Log tính sát thương: Reset title cho boss thành công! thuộc về {player.RoleName}");
                            }
                        }
                    }
                }
                else
                {
                    monster.Title = "(Không thuộc về ai)";
                    // ghi log
                    LogManager.WriteLog(LogTypes.BossUnicon, $"Log tính sát thương: Reset title cho boss thành công! Không thuộc về ai");
                }
            });
        }

        /// <summary>
        /// Tạo MiniBoss ở thời gian tương ứng
        /// </summary>
        private void SpawnMiniBossInTime()
        {
            // Duyệt danh sách MiniBoss
            foreach (BossUnicon.EventDetail.Stage.MiniBossInfo miniBossInfo in this.stageInfo.MiniBosses)
            {
                // Nếu chưa tồn tại trong danh sách
                if (!this.nextCreateMiniBosses.TryGetValue(miniBossInfo, out long nextCreateMiniBossTicks))
                {
                    // Tạo mới
                    this.nextCreateMiniBosses[miniBossInfo] = KTGlobal.GetCurrentTimeMilis() + KTGlobal.GetRandomNumber(miniBossInfo.MinDuration, miniBossInfo.MaxDuration);
                    // Bỏ qua
                    continue;
                }

                // Nếu chưa đánh bại Boss cũ
                if (nextCreateMiniBossTicks == -1)
                {
                    // Bỏ qua
                    continue;
                }

                // Nếu chưa đến thời gian
                if (KTGlobal.GetCurrentTimeMilis() < nextCreateMiniBossTicks)
                {
                    // Bỏ qua
                    continue;
                }

                // Đánh dấu thời gian tiếp theo tạo MiniBoss
                this.nextCreateMiniBosses[miniBossInfo] = -1;

                // Chọn Boss ngẫu nhiên
                BossUnicon.EventDetail.Stage.BossInfo bossInfo = miniBossInfo.Bosses[KTGlobal.GetRandomNumber(0, miniBossInfo.Bosses.Count - 1)];

                // Toác gì đó
                if (bossInfo == null)
                {
                    // Đánh dấu thời gian tiếp theo tạo MiniBoss
                    this.nextCreateMiniBosses[miniBossInfo] = KTGlobal.GetCurrentTimeMilis() + KTGlobal.GetRandomNumber(miniBossInfo.MinDuration, miniBossInfo.MaxDuration);
                    // Bỏ qua
                    continue;
                }
                // Tạo Boss
                this.CreateBoss(bossInfo, (killer) => {
                    // Đánh dấu thời gian tiếp theo tạo MiniBoss
                    this.nextCreateMiniBosses[miniBossInfo] = KTGlobal.GetCurrentTimeMilis() + KTGlobal.GetRandomNumber(miniBossInfo.MinDuration, miniBossInfo.MaxDuration);

                    // Nếu không phải người chơi
                    if (!(killer is KPlayer killerPlayer))
                    {
                        return;
                    }

                    // Thực hiện Logic giết Boss
                    this.ProcessPlayerKillBossLogic(killerPlayer, bossInfo);
                }, (boss) => {

                    // thêm kháng tất cả cho miniboss (300)
                    for (int nType = (int)DAMAGE_TYPE.damage_physics; nType <= (int)DAMAGE_TYPE.damage_magic; ++nType)
                    {
                        boss.AddCurResist((DAMAGE_TYPE)nType, 300);
                        //Console.WriteLine("Add {0} to {1}: +{2}", (DAMAGE_TYPE) nType, obj.RoleName, magic.nValue[0] * stackCount);
                    }

                    // Đối tượng quái tương ứng
                    Monster monster = boss as Monster;
                    // Toác
                    if (monster == null)
                    {
                        return;
                    }

                    // Nếu có sát thương
                    if (monster.DamageTakeRecord.Count > 0)
                    {
                        // Thời điểm hiện tại
                        long currentTicks = TimeUtil.NOW();

                        // Lấy ra toàn bộ các damage mà 30s chưa cập nhật
                        List<Monster.DamgeGroup> needRessetDamages = monster.DamageTakeRecord.Values.Where(x => currentTicks - x.LastUpdateTime > 30000).ToList();
                        if (needRessetDamages.Count <= 0)
                        {
                            monster.Title = "(Không thuộc về ai)";
                        }
                        else
                        {
                            // Reset Damage
                            monster.ResetDamge(needRessetDamages);

                            // Danh sách sát thương cao nhất
                            Monster.DamgeGroup topDamage = monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).FirstOrDefault();
                            // Nếu có sát thương
                            if (topDamage.TotalDamage > 0)
                            {
                                // Thời gian Reset
                                long resetTime = (30000 - currentTicks + topDamage.LastUpdateTime) / 1000;
                                // Nếu là nhóm
                                if (topDamage.IsTeam)
                                {
                                    // Đội trưởng
                                    KPlayer teamLeader = KTTeamManager.GetTeamLeader(topDamage.ID);
                                    // Nếu tồn tại
                                    if (teamLeader != null)
                                    {
                                        monster.Title = "<b><color=#00ff2a>(Thuộc đội: " + teamLeader.RoleName + ", còn: " + resetTime + "s)</color></b>";
                                    }
                                }
                                else
                                {
                                    // Người chơi tương ứng
                                    KPlayer player = GameManager.ClientMgr.FindClient(topDamage.ID);
                                    // Nếu tồn tại
                                    if (player != null)
                                    {
                                        monster.Title = "<b><color=#00ff2a>(Thuộc về: " + player.RoleName + ", còn: " + resetTime + "s)</color></b>";
                                    }
                                }
                            }
                            else
                            {
                                monster.Title = "(Không thuộc về ai)";
                            }
                        }
                    }
                    else
                    {
                        monster.Title = "(Không thuộc về ai)";
                    }
                });
            }
        }

        /// <summary>
        /// Thực hiện Logic cổng dịch chuyển
        /// </summary>
        /// <param name="teleportInfo"></param>
        /// <param name="player"></param>
        private bool DoTeleportLogic(BossUnicon.EventDetail.Stage.TeleportInfo teleportInfo, KPlayer player)
        {
            // Nếu thông tin bản đồ đến không tồn tại
            if (!BossUnicon.Event.Stages.TryGetValue(teleportInfo.ToMapID, out BossUnicon.EventDetail.Stage nextStageInfo))
            {
                return false;
            }

            // Thông tin bản đồ kế tiếp
            GameMap map = GameManager.MapMgr.GetGameMap(nextStageInfo.MapID);
            // Toác
            if (map == null)
            {
                PlayerManager.ShowNotification(player, "Bản đồ chưa được mở!");
                return false;
            }

            // kiểm tra xem đến giờ mở boss chưa? nếu có mới mở tầng 3
            if (BossUnicon_ActivityScript.timeBossAppear == 0 && nextStageInfo.ID == 3)
            {
                PlayerManager.ShowNotification(player, "Chưa đến thời gian để lên!!!");
                return false;
            }

            // Dịch chuyển đến tầng tương ứng
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, teleportInfo.ToMapID, teleportInfo.ToPosX, teleportInfo.ToPosY, (int)player.CurrentDir);
            return true;
        }

        /// <summary>
        /// Thực thi Logic người chơi giết Boss
        /// </summary>
        /// <param name="killerPlayer"></param>
        /// <param name="bossInfo"></param>
        private void ProcessPlayerKillBossLogic(KPlayer killerPlayer, BossUnicon.EventDetail.Stage.BossInfo bossInfo)
        {
            /// Xóa khỏi ds boss còn tồn tại
            BossUnicon.bigBossIDs.TryRemove(this.stageInfo.MapID, out _);  
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Sự kiện Tick người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerTick(KPlayer player)
        {
            // Nếu chưa chuyển Map xong
            if (player.WaitingForChangeMap)
            {
                return;
            }

            
            // Nếu đã đến thời gian
            if (KTGlobal.GetCurrentTimeMilis() - player.LastBossUniconTicks >= 10000)
            {
                
                // Thời gian còn lại ở 
                int totalSecLeft = BossUnicon_ActivityScript.GetTodayTotalSecLeft(player);
                // Mở bảng thông báo hoạt động
                this.OpenEventBroadboard(player, 900);
                /// cập nhật dame lên bảng thông tin
                var HKL = this.listBosses.Where(x => BossUnicon.IsBoss(x.MonsterInfo.ExtensionID)).FirstOrDefault();
                if (HKL != null)
                {
                    var damage = HKL.DamagePlayerIDRecord.Values.Where(x => x.ID == player.RoleID).FirstOrDefault();
                    if (damage!= null)
                    {
                        this.UpdateEventDetails(player, "Thời gian còn lại", totalSecLeft, $"\n Damage đã gây ra:  \n   {damage.TotalDamage}");
                    }    
                    else
                    {
                        this.UpdateEventDetails(player, "Thời gian còn lại", totalSecLeft, "Hãy tiêu diệt Boss");
                    }    
                }
                else
                {
                    this.UpdateEventDetails(player, "Thời gian còn lại", totalSecLeft, "Hãy tiêu diệt Boss");
                }
                // Đánh dấu thời gian Tick
                player.LastBossUniconTicks = KTGlobal.GetCurrentTimeMilis();
            }
            

            // Vị trí của bản thân
            UnityEngine.Vector2 currentPos = new UnityEngine.Vector2(player.PosX, player.PosY);

            // Có tìm thấy khu an toàn ở vị trí đang đứng không
            bool isFound = false;
            // Duyệt danh sách khu an toàn
            foreach (BossUnicon.EventDetail.Stage.SafeZoneInfo safeZoneInfo in this.stageInfo.SafeZones)
            {
                // Vị trí khu an toàn
                UnityEngine.Vector2 safeZonePos = new UnityEngine.Vector2(safeZoneInfo.PosX, safeZoneInfo.PosY);
                // Khoảng cách
                float distance = UnityEngine.Vector2.Distance(currentPos, safeZonePos);
                // Nếu nằm trong phạm vi
                if (distance < safeZoneInfo.Radius)
                {
                    // Đánh dấu đang ở trong khu an toàn
                    player.IsInsideSafeZone = true;
                    // Đánh dấu có tìm thấy
                    isFound = true;
                    // Thoát
                    break;
                }
            }
            if (!isFound)
            {
                player.IsInsideSafeZone = false;
            }

                
        }

        public static int CheckDamageUnicon(KPlayer player)
        {
            var damage = BossUnicon.dameGroups.Where(x => x.ID == player.RoleID).FirstOrDefault();
            if (damage != null)
                return 1;
            else
            {
                return 0;
            }    
        }

        public static void GetAwardDamageUnicon(KPlayer player)
        {
            if (BossUnicon.dameGroups.Where(x => x.ID == player.RoleID).FirstOrDefault() != null)
            {
                // xem đã nhận quà chưa
                if (player.GetValueOfDailyRecore((int)AcitvityRecore.Emperortomb_AwardUnicon) > 0)
                {
                    // đã nhận thưởng trước đó
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã nhận thưởng trước đó!");
                    LogManager.WriteLog(LogTypes.BossUnicon, $"Player: {player.RoleName} roleID={player.RoleID} đã nhận thưởng damage lên boss trước đó!");
                }
                else
                {
                    player.SetValueOfDailyRecore((int)AcitvityRecore.Emperortomb_AwardUnicon, 1);
                    // gửi thưởng qua thư cho người chơi
                    if (BossUnicon.Config.AwardUnicon != 0)
                    {
                        List<GoodsData> goods = new List<GoodsData>();
                        goods.Add(new GoodsData
                        {
                            GoodsID = BossUnicon.Config.AwardUnicon,
                            Binding = 1,
                            GCount = 1,
                        });
                        
                        // cho người chơi nhận luôn- rương ko khóa hạn sử dụng 15 ngày
                        var TimeUsing = Global.ConstGoodsEndTime;
                        var dt = DateTime.Now.AddMinutes(15 * 24 * 60);
                        TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");

                        ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, BossUnicon.Config.AwardUnicon, 1, 0, "BossUnicon", false, 0, false, TimeUsing);
                        PlayerManager.ShowMessageBox(player, "Thông báo", "Bạn đã nhận thưởng thành công, hãy kiểm tra túi đồ!");
                        LogManager.WriteLog(LogTypes.BossUnicon, $"Player: {player.RoleName} roleID={player.RoleID} đã nhận thưởng damage lên boss thành công!");
                    }

                }
            }
        }

        public static string GetDamageGuildID(KPlayer player)
        {
            var result = "";
            if (player.GuildID > 0 || BossUnicon.dameGuilds != null)
            {
                var damage = BossUnicon.dameGuilds.Where(x => x.GuildID == player.GuildID).FirstOrDefault();
                if (damage != null)
                {
                    result = damage.TotalDamage.ToString();
                }    
                
            }    
            return result;
        }
        #endregion
    }
}
