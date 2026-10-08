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

namespace GameServer.KiemThe.GameEvents.EmperorTomb
{
    /// <summary>
    /// Script chính thực thi sự kiện Tần Lăng
    /// </summary>
    /// ///
    public class EmperorTomb_Script_Main : GameMapEvent
    {
        #region Define
        /// <summary>
        /// Thông tin tầng
        /// </summary>
        private readonly EmperorTomb.EventDetail.Stage stageInfo;

        /// <summary>
        /// Danh sách Boss đã được tạo ra
        /// </summary>
        private readonly HashSet<string> createdBosses;


        /// <summary>
        /// Thời điểm tiếp theo tạo MiniBoss tương ứng
        /// </summary>
        private readonly ConcurrentDictionary<EmperorTomb.EventDetail.Stage.MiniBossInfo, long> nextCreateMiniBosses;

        /// <summary>
        /// Thời điểm Tick lần trước
        /// </summary>
        private long lastTicks = 0;

        private List<Monster> listBosses;
        #endregion

        #region Core GameMapEvent
        /// <summary>
        /// Script chính thực thi sự kiện Tần Lăng
        /// </summary>
        /// <param name="map"></param>
        /// <param name="activity"></param>
        public EmperorTomb_Script_Main(GameMap map, KTActivity activity) : base(map, activity)
        {
            this.stageInfo = EmperorTomb.Event.Stages[map.MapCode];
            this.createdBosses = new HashSet<string>();
            this.nextCreateMiniBosses = new ConcurrentDictionary<EmperorTomb.EventDetail.Stage.MiniBossInfo, long>();
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
            if (EmperorTomb_ActivityScript.timeBossAppear > 0)
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
                // Đưa ra khỏi Tần Lăng
                EmperorTomb_ActivityScript.KickOut(player);
            }
        }

        /// <summary>
        /// Sự kiện khi người chơi vào bản đồ sự kiện
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerEnter(KPlayer player)
        {
            base.OnPlayerEnter(player);

            // đổi trạng thái PK của người chơi
            player.PKMode = (int)PKMode.Guild;

            // check xem tài phú có thay đổi so với lần gần nhất dùng buff ko
            var currentValue = player.GetTotalValue() / 10000;
            var savedValue = player.GetValueOfDailyRecore((int)RecoreDef.SavedTotalValue);
            var currentLevel = Global.GetMantleLevel(currentValue);
            var savedLevel = Global.GetMantleLevel(savedValue);
            if (currentLevel > savedLevel)
            {
                if (player.Buffs.HasBuff(1413))
                {
                    PlayerManager.ShowNotification(player, "Cấp độ tài phú thay đổi. Xóa kỹ năng hỗ trợ của Dạ Minh Châu.");
                    player.Buffs.RemoveBuff(1413);
                }    
            }    

            // Mở bảng thông báo hoạt động
            this.OpenEventBroadboard(player, 400);
            // Thời gian còn lại ở Tần Lăng
            int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
            // Cập nhật thông tin
            this.UpdateEventDetailsToPlayers("Thời gian còn lại", totalSecLeft, "Đánh quái và săn Boss");
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

            // Nếu bản đồ đích không thuộc Tần Lăng
            if (!EmperorTomb.Event.Stages.ContainsKey(toMap.MapCode))
            {
                // Đóng bảng thông báo hoạt động
                this.CloseEventBroadboard(player, 400);
                // xóa buff chướng khí cũng như dạ minh châu
                player.Buffs.RemoveBuff(1412);
                player.Buffs.RemoveBuff(1413);
                //xóa bộ nhớ
                EmperorTomb_ActivityScript.mapIDHasBuff.TryRemove(player.RoleID, out _);
                EmperorTomb_ActivityScript.timeDurationDMC.TryRemove(player.RoleID, out _);
            }

            // Hủy khu an toàn
            player.IsInsideSafeZone = false;

            // ghi lại log trước khi thoát
            int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
            LogManager.WriteLog(LogTypes.EmperorTomb, $"Người chơi: {player.RoleName} đã rời khỏi Tần Lăng, thời gian còn lại là: {totalSecLeft}");
        }

        /// <summary>
        /// Sự kiện khi người chơi ấn nút hồi sinh
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool OnPlayerClickReliveButton(KPlayer player)
        {
            // Đưa về khu an toàn tầng 1
            PlayerManager.Relive(player, EmperorTomb.Event.Relive.MapID, EmperorTomb.Event.Relive.PosX, EmperorTomb.Event.Relive.PosY, 100, 100, 100);
            // nếu mà là lên tầng mà chưa kịp ăn buff mà chết thì add lại buff theo durationtick cũ
            if (EmperorTomb_ActivityScript.timeDurationDMC.TryGetValue(player.RoleID, out var durationTicks)) {
                if (!player.Buffs.HasBuff(1413) && durationTicks > 0)
                {
                    player.Buffs.AddBuff(1413, 10, durationTicks);
                }
            } 
            
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
            foreach (EmperorTomb.EventDetail.Stage.MonsterInfo monsterInfo in this.stageInfo.Monsters)
            {
                // Hướng quay
                Entities.Direction dir = Entities.Direction.NONE;
                // Mức máu
                int hp = monsterInfo.BaseHP + monsterInfo.HPIncreaseEachLevel * EmperorTomb.Config.MonsterLevel;
                // Ngũ hành
                int rand = KTGlobal.GetRandomNumber(1, 5);
                KE_SERIES_TYPE series =(KE_SERIES_TYPE) rand;

                // Tạo quái
                GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, monsterInfo.ID, -1, 1, monsterInfo.PosX, monsterInfo.PosY, monsterInfo.Name, monsterInfo.Title, hp, EmperorTomb.Config.MonsterLevel, dir, series, monsterInfo.AIType, monsterInfo.RespawnTicks, -1, monsterInfo.AIScriptID, null, (monster) => {
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
        private void CreateBoss(EmperorTomb.EventDetail.Stage.BossInfo bossInfo, Action<GameObject> onDie, Action<GameObject> onTick)
        {
            // Hướng quay
            KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
            // Mức máu
            int hp = bossInfo.BaseHP + bossInfo.HPIncreaseEachLevel * EmperorTomb.Config.MonsterLevel;
            if(GameManager.ServerId == 9001 && bossInfo.ID == 7384)
            {
                hp *= 3;
            }    
            // Ngũ hành
            int rand = KTGlobal.GetRandomNumber(1, 5);
            KE_SERIES_TYPE series =(KE_SERIES_TYPE) rand;

            // Tạo Boss
            bool resultCreate = GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, bossInfo.ID, -1, 1, bossInfo.PosX, bossInfo.PosY, bossInfo.Name, bossInfo.Title, hp, EmperorTomb.Config.MonsterLevel, dir, series, bossInfo.AIType, -1, -1, bossInfo.AIScriptID, null, (boss) => {
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
            }, 65535, null, onDie);
            if (bossInfo.Name != "")
            {
                KTGlobal.SendSystemEventNotification($"{bossInfo.Name} đã xuất hiện tại Tần Lăng, Chỉ nhận sát thương nội.");
            }   
            // thêm log sau khi tạo boss
            LogManager.WriteLog(LogTypes.EmperorTomb, $"Tạo ra boss {bossInfo.Name} thành công hay không: {resultCreate}");
        }

        /// <summary>
        /// Tạo cổng dịch chuyển tương ứng
        /// </summary>
        private void CreateTeleports()
        {
            // Duyệt danh sách cổng dịch chuyển
			foreach (EmperorTomb.EventDetail.Stage.TeleportInfo teleportInfo in this.stageInfo.Teleports)
            {
                // Tạo cổng dịch chuyển
                KDynamicArea teleport = KTDynamicAreaManager.Add(this.Map.MapCode, -1, teleportInfo.Name, 5340, teleportInfo.PosX, teleportInfo.PosY, -1, 2f, 100, -1, null);
                teleport.OnEnter = (obj) => {
                    if (obj is KPlayer player)
                    {
                        // dịch chuyển lên tầng
                        if (this.DoTeleportLogic(teleportInfo, player))
                        {
                            int mapIDHasBuff = 0;

                            if (EmperorTomb_ActivityScript.mapIDHasBuff.ContainsKey(player.RoleID))
                            {
                                mapIDHasBuff = EmperorTomb_ActivityScript.mapIDHasBuff[player.RoleID];
                            }    
                            
                            int idBuffDMC = 1413;
                            // nếu là lên tầng và chưa có buff
                            if (mapIDHasBuff < teleportInfo.ToMapID && mapIDHasBuff != 0)
                            {
                                if (player.Buffs.HasBuff(idBuffDMC))
                                {
                                    // lưu lại thời gian còn lại của buff cũ
                                    BuffDataEx buff = player.Buffs.GetBuff(idBuffDMC);
                                    EmperorTomb_ActivityScript.timeDurationDMC[player.RoleID] = buff.TimeLeft;

                                    // xóa buff dạ minh châu khi dịch chuyển qua cổng
                                    player.Buffs.RemoveBuff(idBuffDMC);
                                }
                            }
                            // nếu là xuống tầng đã có buff tầng dưới
                            if (mapIDHasBuff >= teleportInfo.ToMapID && !player.Buffs.HasBuff(idBuffDMC) )
                            {
                                if (EmperorTomb_ActivityScript.timeDurationDMC.ContainsKey(player.RoleID) && EmperorTomb_ActivityScript.timeDurationDMC[player.RoleID] > 0)
                                {
                                    player.Buffs.AddBuff(idBuffDMC, 10, EmperorTomb_ActivityScript.timeDurationDMC[player.RoleID]);
                                }
                            }
                        }    
                        
                    }
                };
            }
        }

        /// <summary>
        /// Tạo NPC tương ứng
        /// </summary>
        private void CreateNPCs()
        {
            // Duyệt danh sách NPC
            foreach (EmperorTomb.EventDetail.Stage.NPCInfo npcInfo in this.stageInfo.NPCs)
            {
                // Tạo NPC
                NPCGeneralManager.AddNewNPC(this.Map.MapCode, -1, npcInfo.ID, npcInfo.PosX, npcInfo.PosY, "", "", Entities.Direction.DOWN_RIGHT, npcInfo.ScriptID, "", (npc) => {
                    npc.VisibleOnMinimap = true;
                    npc.MinimapName = npc.Name;
                });
            }
        }

        private void RemoveBoss()
        {
            long currentTime = KTGlobal.GetCurrentTimeMilis();
            long timeAliveOfBoss = 3600000 * 3 / 2;
            // nếu hết thời gian tồn tại quái thì ko cho xuất hiện nữa
            if ((currentTime - EmperorTomb_ActivityScript.timeBossAppear) > timeAliveOfBoss)
            {
                EmperorTomb_ActivityScript.timeBossAppear = 0;
                if (!this.listBosses.Any() || this.listBosses != null)
                {
                    foreach (Monster monster in this.listBosses)
                    {
                        ///chỉ xóa tần thủy hoàng
                        if (monster.MonsterInfo.ExtensionID == 7384)
                        {
                            this.RemoveMonster(monster);
                            // xóa khỏi ds bigBoss tồn tại
                            EmperorTomb.bigBossIDs.TryRemove(monster.MonsterInfo.ExtensionID, out _);
                            LogManager.WriteLog(LogTypes.EmperorTomb, $"Boss {monster.RoleName} đã bị xóa khỏi tần lăng do hết thời gian tồn tại");
                        }    
                    }
                }
                // hủy acivity boss tần lăng
                KTActivityManager.StopActivity(401);
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
            
            // Nếu đã tạo Boss ở thời gian này rồi thì thôi
            if (this.createdBosses.Contains(dateTimeKey))
            {
                return;
            }
            
            // Thông tin Boss ở thời gian này
            EmperorTomb.EventDetail.Stage.BossInfo bossInfo = this.stageInfo.Bosses.Where(x => x.SpawnAt.Hour == hour && x.SpawnAt.Minute == minute).FirstOrDefault();
            // Nếu toác
            if (bossInfo == null)
            {
                LogManager.WriteLog(LogTypes.EmperorTomb, $"Không có thông tin boss sinh ra tại thời điểm {dateTimeKey}");
                return;
            }
            // Nếu boss vẫn còn tồn tại thì thôi
            if (EmperorTomb.bigBossIDs.TryGetValue(this.stageInfo.MapID, out int bossID))
            {
                if (bossInfo.ID == bossID)
                {
                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Boss {bossInfo.Name} vẫn còn tồn tại ");
                    return;
                }
            }   
            // Đánh dấu đã tạo Boss ở thời gian này
            this.createdBosses.Add(dateTimeKey);
            // đánh dấu là boss đã tồn tại
            EmperorTomb.bigBossIDs[this.stageInfo.MapID] = bossInfo.ID;
            // Thời điểm lần trước cập nhật Boss thuộc về đội nào
            long lastTickUpdateBelonging = KTGlobal.GetCurrentTimeMilis();
            // Đã thông báo miễn sát thương hay chưa?
            int hasSendNoti = 0;
            // them log cho chac chan co ra boss hay ko
            LogManager.WriteLog(LogTypes.EmperorTomb, $"Tần Lăng bắt đầu tạo ra boss {bossInfo.Name} vào {dateTimeKey}");
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
                
                // thêm kháng tất cả cho boss (500)
                for (int nType = (int)DAMAGE_TYPE.damage_physics; nType <= (int)DAMAGE_TYPE.damage_magic; ++nType)
                {
                    boss.AddCurResist((DAMAGE_TYPE)nType, 500);
                    //Console.WriteLine("Add {0} to {1}: +{2}", (DAMAGE_TYPE) nType, obj.RoleName, magic.nValue[0] * stackCount);
                }

                // % máu hiện tại
                int nHPPercent = (int) (boss.m_CurrentLife / (float) boss.m_CurrentLifeMax * 100);
                // Nếu còn trên 50% máu
                if (nHPPercent >= 55)
                {
                    // Miễn dịch sát thương ngoại
                    boss.m_ImmuneToPhysicDamage = true;
                    boss.m_ImmuneToMagicDamage = false;
                    if (hasSendNoti == 0)
                    {
                        //KTGlobal.SendSystemEventNotification($"{bossInfo.Name} đã xuất hiện tại Tần Lăng, Chỉ nhận sát thương nội.");
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
                        KTGlobal.SendSystemEventNotification($"{bossInfo.Name}đã trọng thương nhận tất cả sát thương nội ngoại");
                        hasSendNoti = 3;
                    }
                }

                // Nếu chưa đến thời điểm cập nhật
                if (KTGlobal.GetCurrentTimeMilis() - lastTickUpdateBelonging < 5000)
                {
                    return;
                }
                // Đánh dấu thời điểm cập nhật
                lastTickUpdateBelonging = KTGlobal.GetCurrentTimeMilis();

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
                    // ghi log
                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Bắt đầu!");
                    // Thời điểm hiện tại
                    long currentTicks = TimeUtil.NOW();
                    
                    // Lấy ra toàn bộ các damage mà 30s chưa cập nhật
                    List<Monster.DamgeGroup> needRessetDamages = monster.DamageTakeRecord.Values.Where(x => currentTicks - x.LastUpdateTime > 30000).ToList();
                    // nếu ko có dame cần reset thì sẽ xh exception
                    if (needRessetDamages.Count <= 0)
                    {
                        monster.Title = "(Không thuộc về ai)";
                        // ghi log
                        LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset title cho boss thành công! Không thuộc về ai");
                    }  
                    else
                    {
                        // Reset Damage
                        monster.ResetDamge(needRessetDamages);
                        // ghi log
                        LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset damage thành công!");
                        // Danh sách sát thương cao nhất
                        Monster.DamgeGroup topDamage = monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).FirstOrDefault();
                        // ghi log
                        LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Lấy damage cao nhất!");
                        List<Monster.DamgeGroup> damgeGroups = monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).ToList();
                        // Nếu có sát thương
                        if (topDamage.TotalDamage > 0)
                        {
                            int playerID = 0;

                            // Thời gian Reset
                            long resetTime = (30000 - currentTicks + topDamage.LastUpdateTime) / 1000;
                            // ghi log
                            LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset thời gian tính dame!");
                            // Nếu là nhóm
                            if (topDamage.IsTeam)
                            {
                                // Đội trưởng
                                KPlayer teamLeader = KTTeamManager.GetTeamLeader(topDamage.ID);
                                LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Lấy tên chủ damage thành công!");
                                // Nếu tồn tại
                                if (teamLeader != null)
                                {
                                    monster.Title = "<b><color=#00ff2a>(Thuộc đội: " + teamLeader.RoleName + ", còn: " + resetTime + "s)</color></b>";
                                    // ghi log
                                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset title cho boss thành công! thuộc về {teamLeader.RoleName}");
                                    playerID = teamLeader.RoleID;
                                }
                            }
                            else
                            {
                                // Người chơi tương ứng
                                KPlayer player = GameManager.ClientMgr.FindClient(topDamage.ID);
                                // ghi log
                                LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Lấy tên chủ damage thành công!");
                                // Nếu tồn tại
                                if (player != null)
                                {
                                    monster.Title = "<b><color=#00ff2a>(Thuộc về: " + player.RoleName + ", còn: " + resetTime + "s)</color></b>";
                                    // ghi log
                                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset title cho boss thành công! thuộc về {player.RoleName}");
                                    playerID = player.RoleID;
                                }
                            }
                        }
                        else
                        {
                            monster.Title = "(Không thuộc về ai)";
                            // ghi log
                            LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset title cho boss thành công! Không thuộc về ai");
                        }
                    }    
                }
                else
                {
                    monster.Title = "(Không thuộc về ai)";
                    // ghi log
                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Log tính sát thương: Reset title cho boss thành công! Không thuộc về ai");
                }
            });
        }

        /// <summary>
        /// Tạo MiniBoss ở thời gian tương ứng
        /// </summary>
        private void SpawnMiniBossInTime()
        {
            // Duyệt danh sách MiniBoss
            foreach (EmperorTomb.EventDetail.Stage.MiniBossInfo miniBossInfo in this.stageInfo.MiniBosses)
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
                EmperorTomb.EventDetail.Stage.BossInfo bossInfo = miniBossInfo.Bosses[KTGlobal.GetRandomNumber(0, miniBossInfo.Bosses.Count - 1)];

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
        private bool DoTeleportLogic(EmperorTomb.EventDetail.Stage.TeleportInfo teleportInfo, KPlayer player)
        {
            // Nếu thông tin bản đồ đến không tồn tại
            if (!EmperorTomb.Event.Stages.TryGetValue(teleportInfo.ToMapID, out EmperorTomb.EventDetail.Stage nextStageInfo))
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
            if (EmperorTomb_ActivityScript.timeBossAppear == 0 && nextStageInfo.ID == 3)
            {
                PlayerManager.ShowNotification(player, "Chưa đến thời gian để lên!!!");
                return false;
            }

            // Dịch chuyển đến tầng tương ứng
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, teleportInfo.ToMapID, teleportInfo.ToPosX, teleportInfo.ToPosY, (int) player.CurrentDir);
            return true;
        }

        /// <summary>
        /// Thực thi Logic người chơi giết Boss
        /// </summary>
        /// <param name="killerPlayer"></param>
        /// <param name="bossInfo"></param>
        private void ProcessPlayerKillBossLogic(KPlayer killerPlayer, EmperorTomb.EventDetail.Stage.BossInfo bossInfo)
        {
            /// Xóa khỏi ds boss còn tồn tại
            EmperorTomb.bigBossIDs.TryRemove(this.stageInfo.MapID, out _);
            LogManager.WriteLog(LogTypes.EmperorTomb, $"Boss {bossInfo.Name} đã bị xóa khỏi tần lăng do bị giết bởi {killerPlayer.RoleName}");
            //// Nếu thằng này không có bang và tộc
            //if (killerPlayer.GuildID <= 0 && killerPlayer.FamilyID <= 0)
            //{
            //    // Thông báo
            //    // this.NotifyAllPlayers(string.Format("Người chơi {0} đã đánh bại {1}.", killerPlayer.RoleName, bossInfo.Name));
            //    int oldPrestige = killerPlayer.Prestige;
            //    // Tăng uy danh cho thằng giết
            //    killerPlayer.Prestige += bossInfo.Prestige;
            //    LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tần Lăng, uy danh tăng từ {2} lên {3}", killerPlayer.RoleName, bossInfo.Prestige, oldPrestige, killerPlayer.Prestige));
            //    // Thông báo
            //    KTGlobal.SendDefaultChat(killerPlayer, string.Format("Nhận được <color=yellow>{0} Uy danh</color>.", bossInfo.Prestige));
            //
            //    // Nếu có nhóm
            //    if (killerPlayer.TeamID != -1 && KTTeamManager.IsTeamExist(killerPlayer.TeamID))
            //    {
            //        // Duyệt danh sách thành viên nhóm
            //        foreach (KPlayer teammate in killerPlayer.Teammates)
            //        {
            //            // Nếu là bản thân
            //            if (teammate == killerPlayer)
            //            {
            //                continue;
            //            }
            //            int oldPrestigeTeammate = teammate.Prestige;
            //            // Tăng uy danh
            //            teammate.Prestige += bossInfo.Prestige;
            //            LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tần Lăng, uy danh tăng từ {2} lên {3}", teammate.RoleName, bossInfo.Prestige, oldPrestigeTeammate, teammate.Prestige));
            //            // Thông báo
            //            KTGlobal.SendDefaultChat(teammate, string.Format("Nhận được <color=yellow>{0} Uy danh</color>.", bossInfo.Prestige));
            //        }
            //    }
            //}
            //else
            //{
            //    // Thông báo
            //    // this.NotifyAllPlayers(string.Format("Người chơi {0} thuộc bang hội {1} đã đánh bại {2}.", killerPlayer.RoleName, killerPlayer.GuildName, bossInfo.Name));
            //    int oldPrestige = killerPlayer.Prestige;
            //    // Tăng uy danh cho thằng giết
            //    killerPlayer.Prestige += bossInfo.Prestige;
            //    LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tần Lăng, uy danh tăng từ {2} lên {3}", killerPlayer.RoleName, bossInfo.Prestige, oldPrestige, killerPlayer.Prestige));
            //    // Thông báo
            //    KTGlobal.SendDefaultChat(killerPlayer, string.Format("Nhận được <color=yellow>{0} Uy danh</color>.", bossInfo.Prestige));
            //
            //    // Danh sách người chơi
            //    List<KPlayer> players = this.GetPlayers();
            //    // Duyệt danh sách người chơi
            //    foreach (KPlayer player in players)
            //    {
            //        // Nếu là bản thân
            //        if (player == killerPlayer)
            //        {
            //            continue;
            //        }
            //        // Nếu khác bang và tộc
            //        else if (player.GuildID != killerPlayer.GuildID && player.FamilyID != killerPlayer.FamilyID)
            //        {
            //            continue;
            //        }
            //        // Nếu khác đội
            //        else if (killerPlayer.TeamID == -1 || !KTTeamManager.IsTeamExist(killerPlayer.TeamID) || killerPlayer.TeamID != player.TeamID)
            //        {
            //            continue;
            //        }
            //        int oldPrestigeTeammate = player.Prestige;
            //        // Tăng uy danh
            //        player.Prestige += bossInfo.Prestige;
            //        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Tần Lăng, uy danh tăng từ {2} lên {3}", player.RoleName, bossInfo.Prestige, oldPrestigeTeammate, player.Prestige));
            //        // Thông báo
            //        KTGlobal.SendDefaultChat(player, string.Format("Nhận được <color=yellow>{0} Uy danh</color>.", bossInfo.Prestige));
            //    }
            //} 
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

            if (EmperorTomb_ActivityScript.timeBossAppear == 0 && player.CurrentMapCode == 1538)
            {
                /// Đưa về khu an toàn tầng 1
                /// Ngừng di chuyển
                KTPlayerStoryBoardEx.Instance.Remove(player);

                // Hủy trạng thái khinh công
                player.StopBlink();

                // Lấy dữ liệu bản đồ đích đến
                if (GameManager.MapMgr.DictMaps.TryGetValue(EmperorTomb.Event.Relive.MapID, out GameMap gameMap))
                {
                    // Nếu bản đồ đích khác bản đồ hiện tại
                    if (player.CurrentMapCode != EmperorTomb.Event.Relive.MapID)
                    {
                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener,
                            Global._TCPManager.TcpOutPacketPool, player, EmperorTomb.Event.Relive.MapID, EmperorTomb.Event.Relive.PosX, EmperorTomb.Event.Relive.PosY,
                            (int)player.CurrentDir);
                    }
                    else
                    {
                        GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener,
                            Global._TCPManager.TcpOutPacketPool, player, EmperorTomb.Event.Relive.PosX, EmperorTomb.Event.Relive.PosY,
                            (int)player.CurrentDir);
                    }
                }
                return;
            }    

            // Nếu đã đến thời gian
            if (KTGlobal.GetCurrentTimeMilis() - player.LastEmperorTombTicks >= 10000)
            {
                // Mở bảng thông báo hoạt động
                this.OpenEventBroadboard(player, 400);
                // Thời gian còn lại ở Tần Lăng
                int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
                // Cập nhật thông tin
                this.UpdateEventDetailsToPlayers("Thời gian còn lại", totalSecLeft, "Đánh quái và săn Boss");
            }
            // Đánh dấu thời gian Tick
            player.LastEmperorTombTicks = KTGlobal.GetCurrentTimeMilis();

            // Vị trí của bản thân
            UnityEngine.Vector2 currentPos = new UnityEngine.Vector2(player.PosX, player.PosY);

            // Có tìm thấy khu an toàn ở vị trí đang đứng không
            bool isFound = false;
            // Duyệt danh sách khu an toàn
            foreach (EmperorTomb.EventDetail.Stage.SafeZoneInfo safeZoneInfo in this.stageInfo.SafeZones)
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
                // thêm buff chướng khí
                if (!player.Buffs.HasBuff(1412))
                {
                    int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
                    player.Buffs.AddBuff(1412, 10, totalSecLeft, 1);
                }    
            }    
        }
        #endregion
    }
}
