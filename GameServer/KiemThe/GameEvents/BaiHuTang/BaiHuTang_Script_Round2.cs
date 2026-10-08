using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.GameEvents.Interface;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;

namespace GameServer.KiemThe.GameEvents.BaiHuTang
{
    /// <summary>
    /// Script Bạch Hổ Đường tầng 2
    /// </summary>
    public class BaiHuTang_Script_Round2 : GameMapEvent
    {
        #region Constants
        /// <summary>
        /// Thời gian mỗi lần thông báo thông tin sự kiện tới người chơi
        /// </summary>
        private const int NotifyActivityInfoToPlayersEveryTick = 5000;

        /// <summary>
        /// Thời gian mỗi lần cập nhật trạng thái PK của người chơi
        /// </summary>
        private const int ChangePlayersPKModeEveryTick = 5000;
        #endregion

        #region Private fields
        /// <summary>
        /// Thời gian lần trước chuyển trạng thái PK của tất cả người chơi
        /// </summary>
        private long LastChangePKModeTick;

        /// <summary>
        /// Thời điểm thông báo cập nhật thông tin sự kiện tới tất cả người chơi lần trước
        /// </summary>
        private long LastNotifyTick;

        /// <summary>
        /// Bước hiện tại của hoạt động
        /// </summary>
        private int nStep;

        /// <summary>
        /// Boss
        /// </summary>
        private Monster boss;
        #endregion

        #region Properties
        /// <summary>
        /// Cấp độ hoạt động
        /// </summary>
        public int Level { get; set; }
        #endregion

        #region Core GameMapEvent
        /// <summary>
        /// Script Bạch Hổ Đường tầng 2
        /// </summary>
        /// <param name="gameMap"></param>
        /// <param name="activity"></param>
        public BaiHuTang_Script_Round2(GameMap gameMap, KTActivity activity) : base(gameMap, activity)
        {

        }

        /// <summary>
        /// Sự kiện chuẩn bị
        /// </summary>
        protected override void OnStart()
        {
            /// Xóa toàn bộ NPC
            this.RemoveAllNPCs();
            /// Xóa toàn bộ quái
            this.RemoveAllMonsters();
            /// Xóa toàn bộ điểm thu thập
            this.RemoveAllGrowPoints();
            /// Xóa toàn bộ cổng dịch chuyển
            this.RemoveAllDynamicAreas();
            /// Cập nhật thời điểm thông báo gần nhất
            this.LastNotifyTick = 0;
            /// Cập nhật Step của hoạt động
            this.nStep = 0;
            /// Tạo quái
            this.CreateMonsters();
        }

        /// <summary>
        /// Sự kiện Tick
        /// </summary>
        protected override void OnTick()
        {
            /// Nếu đã đến thời gian chuyển PKMode
            if (KTGlobal.GetCurrentTimeMilis() - this.LastChangePKModeTick >= BaiHuTang_Script_Round2.ChangePlayersPKModeEveryTick)
            {
                /// Đánh dấu thời gian chuyển PKMode
                this.LastChangePKModeTick = KTGlobal.GetCurrentTimeMilis();
                /// Chuyển PKMode tương ứng
                this.ChangePlayersPKMode();
            }

            /// Nếu ở Step 0
            if (this.nStep == 0)
            {
                /// Nếu đã đến thời gian ra Boss
                if (this.LifeTimeTicks >= BaiHuTang.Round2.Boss.SpawnAfter)
                {
                    /// Chuyển Step
                    this.nStep = 100;
                    /// Tạo Boss
                    this.CreateBoss();
                    /// Đánh dấu thời gian thông báo thông tin sự kiện
                    this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
                    /// Cập nhật thông tin sự kiện
                    this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", this.DurationTicks - this.LifeTimeTicks, string.Format("Đánh bại <color=yellow>{0}</color>.", "Thủ Lĩnh Thiết Đồ Tặc"));
                }
                else
                {
                    /// Nếu đã đến thời gian thông báo thông tin sự kiện
                    if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= BaiHuTang_Script_Round2.NotifyActivityInfoToPlayersEveryTick)
                    {
                        /// Đánh dấu thời gian thông báo thông tin sự kiện
                        this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
                        /// Cập nhật thông tin sự kiện
                        this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", BaiHuTang.Round2.Boss.SpawnAfter - this.LifeTimeTicks, string.Format("Đợi <color=yellow>{0}</color> xuất hiện.", "Thủ Lĩnh Thiết Đồ Tặc"));
                    }
                }
            }
            /// Nếu ở Step 1
            else if (this.nStep == 1)
            {
                /// Nếu Boss còn sống
                if (this.IsBossAlive())
                {
                    /// Nếu đã đến thời gian thông báo thông tin sự kiện
                    if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= BaiHuTang_Script_Round2.NotifyActivityInfoToPlayersEveryTick)
                    {
                        /// Đánh dấu thời gian thông báo thông tin sự kiện
                        this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
                        /// Cập nhật thông tin sự kiện
                        this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", this.DurationTicks - this.LifeTimeTicks, string.Format("Đánh bại <color=yellow>{0}</color>.", "Thủ Lĩnh Thiết Đồ Tặc"));
                    }
                    return;
                }
                /// Nếu chưa mở tầng tiếp theo
                else if (BaiHuTang.CurrentStage != 4)
                {
                    /// Nếu đã đến thời gian thông báo thông tin sự kiện
                    if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= BaiHuTang_Script_Round2.NotifyActivityInfoToPlayersEveryTick)
                    {
                        /// Đánh dấu thời gian thông báo thông tin sự kiện
                        this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
                        /// Cập nhật thông tin sự kiện
                        this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", this.DurationTicks - this.LifeTimeTicks, string.Format("Đợi khai mở <color=yellow>[{0}]</color>.", "Bạch Hổ Đường - Tầng 3"));
                    }
                    return;
                }

                /// Thông báo mở tầng
                this.NotifyAllPlayers("[Bạch Hổ Đường - Tầng 3] đã mở, hãy nhanh chân!");
                /// Tạo cổng dịch chuyển lên tầng
                this.CreateTeleport();
                /// Chuyển Step
                this.nStep = 100;

                /// Đánh dấu thời gian thông báo thông tin sự kiện
                this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
                /// Cập nhật thông tin sự kiện
                this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", this.DurationTicks - this.LifeTimeTicks, string.Format("Tiến vào <color=yellow>[{0}]</color>.", "Bạch Hổ Đường - Tầng 3"));
            }
        }

        /// <summary>
        /// Sự kiện kết thúc
        /// </summary>
        protected override void OnClose()
        {
            /// Đưa toàn bộ người chơi rời khỏi
            this.KickOutAllPlayers();
        }

        /// <summary>
        /// Sự kiện người chơi giết quái
        /// </summary>
        /// <param name="player"></param>
        /// <param name="obj"></param>
        public override void OnKillObject(KPlayer player, GameObject obj)
        {
            if (obj is Monster boss)
            {
                base.OnKillObject(player, obj);

                if ((int)boss.MonsterType == 3)
                {
                    this.NotifyAllPlayers(string.Format("{0} đã đánh bại {1}!", player.RoleName, obj.RoleName));
                    int oldPrestige = player.Prestige;
                    /// mặc định cộng cho bản thân thằng giết
                    player.Prestige += BaiHuTang.Round2.Repute.GuildKillBossPrestige;
                    LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Bạch Hổ Đường, uy danh tăng từ {2} lên {3}", player.RoleName, BaiHuTang.Round2.Repute.GuildKillBossPrestige, oldPrestige, player.Prestige));

                    /// Thêm danh vọng tương ứng
                    KTGlobal.AddRepute(player, 501, BaiHuTang.Round2.Repute.GuildKillBossRepute);
                    /// Thông báo nhận danh vọng Bạch Hổ Đường

                    PlayerManager.ShowNotification(player, string.Format("Nhận {0} điểm Uy danh và {1} điểm danh vọng Bạch Hổ Đường!", BaiHuTang.Round2.Repute.GuildKillBossPrestige, BaiHuTang.Round2.Repute.GuildKillBossRepute));
                    if (player.TeamID != -1)
                    {
                        List<KPlayer> TotalMember = player.Teammates;
                        List<int> idMapBHD = new List<int>() { 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 346, 347, 348, 349, 350, 351, 352, 354, 355, 356, 357, 358, 359, 360 };
                        foreach (KPlayer member in TotalMember)
                        {
                            // nếu đã chết thì cút
                            if (member.IsDead())
                            {
                                continue;
                            }

                            // nếu ko trong bhd thi cút
                            if (!idMapBHD.Contains(member.CurrentMapCode))
                            {
                                continue;
                            }
                            // nếu là thằng giết thì thôi
                            if (member.RoleID == player.RoleID)
                            {
                                continue;
                            }

                            int prestige = BaiHuTang.Round2.Repute.OtherPrestige;
                            int repute = BaiHuTang.Round2.Repute.OtherRepute;
                            int oldPrestigeMember = member.Prestige;
                            /// Thêm uy danh tương ứng
                            member.Prestige += prestige;
                            LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Bạch Hổ Đường", member.RoleName, prestige, oldPrestigeMember, member.Prestige));

                            /// Thêm danh vọng tương ứng
                            KTGlobal.AddRepute(member, 501, repute);
                            /// Thông báo nhận danh vọng Bạch Hổ Đường
                            PlayerManager.ShowNotification(member, string.Format("Nhận {0} điểm Uy danh và {1} điểm danh vọng Bạch Hổ Đường!", prestige, repute));
                        }
                    }

                    /// Thêm phần thưởng khi giết được Boss
                    string[] itemList = null;

                    if (GameManager.IsKuaFuServer)
                    {
                        if (this.Level == 79) // BHĐ sơ
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsLow.Split('#');
                        }
                        else if (this.Level == 90) // BHĐ trung
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsMid.Split('#');
                        }
                        else if (this.Level == 120) // BHĐ cao
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsHigh.Split('#');
                        }
                        if (itemList == null)
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (this.Level == 50) // BHĐ sơ
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsLow.Split('#');
                        }
                        else if (this.Level == 90) // BHĐ trung
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsMid.Split('#');
                        }
                        else if (this.Level == 120) // BHĐ cao
                        {
                            itemList = BaiHuTang.Round2.Boss.ItemsHigh.Split('#');
                        }
                        if (itemList == null)
                        {
                            return;
                        }
                    }

                    /// Danh sách vật phẩm sẽ rơi
                    List<ItemData> dropItems = new List<ItemData>();

                    foreach (string item in itemList)
                    {
                        string[] paramItem = item.Split('|');

                        int itemID = Int32.Parse(paramItem[0]);
                        int itemNum = Int32.Parse(paramItem[1]);

                        ItemData itemData = ItemManager.GetItemTemplate(itemID);

                        for (int i = 0; i < itemNum; i++)
                        {
                            dropItems.Add(itemData);
                        }
                        // tạo log xem dồ có rơi ko?
                        LogManager.WriteLog(LogTypes.Error, string.Format("BaihutangRound1: DropItem: {0}, số lượng: {1}", itemData.Name, itemNum));
                    }
                    /// Nếu tồn tại danh sách vật phẩm sẽ rơi
                    if (dropItems.Count > 0)
                    {
                        /// Tạo vật phẩm rơi dưới đất tương ứng
                        ItemDropManager.CreateDropToMap(player, dropItems, this.boss);
                        /// Giải phóng bộ nhớ
                        dropItems.Clear();
                    }
                }
            }
            GlobalEventSource.getInstance().fireEvent(new BachHoDuongKillEventObject(player, obj));
        }

        /// <summary>
        /// Sự kiện người chơi vào bản đồ hoạt động
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerEnter(KPlayer player)
        {
            base.OnPlayerEnter(player);

            /// Nếu không phải thời gian hoạt động
            if (BaiHuTang.CurrentStage != 3)
            {
                /// Thông báo
                PlayerManager.ShowNotification(player, "Hiện không phải thời gian Bạch Hổ Đường!");
                /// Đưa người chơi rời khỏi bản đồ hoạt động
                this.KickOutPlayer(player);
                return;
            }

            /// thêm điểm uy danh, danh vọng vì đến được đây
            int prestige = 1;
            int repute = 10;
            int oldPrestige = player.Prestige;
            /// Thêm uy danh tương ứng
            player.Prestige += prestige;
            LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Bạch Hổ Đường, uy danh tăng từ {2} lên {3}", player.RoleName, prestige, oldPrestige, player.Prestige));
            /// Thêm danh vọng tương ứng
            KTGlobal.AddRepute(player, 501, repute);
            /// Thông báo nhận danh vọng Bạch Hổ Đường
            PlayerManager.ShowNotification(player, string.Format("Nhận {0} điểm Uy danh và {1} điểm danh vọng Bạch Hổ Đường!", prestige, repute));

            /// Mở bảng thông báo hoạt động
            this.OpenEventBroadboard(player, BaiHuTang.EventID);
            /// Cập nhật thông tin
            this.UpdateEventDetailsToPlayers("Bạch Hổ Đường - Tầng 2", BaiHuTang.Round2.Boss.SpawnAfter - this.LifeTimeTicks, string.Format("Đợi <color=yellow>{0}</color> xuất hiện.", "Thủ Lĩnh Thiết Đồ Tặc"));
        }

        /// <summary>
        /// Sự kiện người chơi rời bản đồ hoạt động
        /// </summary>
        /// <param name="player"></param>
        /// <param name="toMap"></param>
        public override void OnPlayerLeave(KPlayer player, GameMap toMap)
        {
            base.OnPlayerLeave(player, toMap);

            /// Đóng bảng thông báo hoạt động
            this.CloseEventBroadboard(player, BaiHuTang.EventID);
            /// Chuyển trạng thái PK
            player.PKMode = (int) PKMode.Peace;
            /// Chuyển Camp về -1
            player.Camp = -1;
        }

        /// <summary>
        /// Sự kiện khi người choi ấn nút về thành khi bị trọng thương
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
		public override bool OnPlayerClickReliveButton(KPlayer player)
        {
            base.OnPlayerClickReliveButton(player);

            /// Đưa người chơi về bản đồ báo danh Bạch Hổ Đường
            PlayerManager.Relive(player, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutMapID, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutPosX, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutPosY, 100, 100, 100);
            return true;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Cập nhật toàn bộ trạng thái PK của người chơi
        /// </summary>
        private void ChangePlayersPKMode()
        {
            foreach (KPlayer player in this.GetPlayers())
            {
                player.PKMode = (int) PKMode.Guild;
            }
        }

        /// <summary>
        /// Tạo quái
        /// </summary>
        private void CreateMonsters()
        {
            /// Duyệt danh sách thiết lập quái
            foreach (BaiHuTang.RoundInfo.MonsterInfo monsterInfo in BaiHuTang.Round2.Monsters)
            {
                /// Ngũ hành
                KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;
                /// Hướng quay
                KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
                /// Mức máu
                int hp = monsterInfo.BaseHP + monsterInfo.HPIncreaseEachLevel * this.Level;
                /// Tạo quái
                GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, monsterInfo.IDByActivity[this.Activity.Data.ID], -1, 1, monsterInfo.PosX, monsterInfo.PosY, monsterInfo.Name, monsterInfo.Title, hp, this.Level, dir, series, (MonsterAIType) monsterInfo.AIType, monsterInfo.RespawnTick, -1, monsterInfo.AIScriptID, null, null, 65535, () => {
                    /// Nếu hoạt động đã bị hủy thì thôi
                    return !this.isDisposed;
                });
            }
        }

        /// <summary>
        /// Khởi tạo Boss
        /// </summary>
        private void CreateBoss()
        {
            /// Vị trí xuất hiện
            KeyValuePair<int, int> randomPos = BaiHuTang.Round2.Boss.RandomPos[KTGlobal.GetRandomNumber(0, BaiHuTang.Round2.Boss.RandomPos.Count - 1)];
            /// Ngũ hành
            KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;
            /// Hướng quay
            KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
            /// Mức máu
            int hp = BaiHuTang.Round2.Boss.BaseHP + BaiHuTang.Round2.Boss.HPIncreaseEachLevel * this.Level;

            GameManager.MonsterZoneMgr.AddDynamicMonsters(this.Map.MapCode, BaiHuTang.Round2.Boss.IDByActivity[this.Activity.Data.ID], -1, 1, randomPos.Key, randomPos.Value, BaiHuTang.Round2.Boss.Name, BaiHuTang.Round2.Boss.Title, hp, this.Level, dir, series, (MonsterAIType) BaiHuTang.Round2.Boss.AIType, -1, -1, BaiHuTang.Round2.Boss.AIScriptID, null, (monster) => {
                /// Thông báo Boss đã xuất hiện
                this.NotifyAllPlayers(string.Format("{0} đã xuất hiện!", monster.RoleName));
                /// Ghi lại Boss
                this.boss = monster;
                /// Chuyển qua Step 1
                this.nStep = 1;
            }, 65535, () => {
                /// Nếu hoạt động đã bị hủy thì thôi
                return !this.isDisposed;
            });
        }

        /// <summary>
        /// Kiểm tra Boss có còn sống không
        /// </summary>
        /// <returns></returns>
        private bool IsBossAlive()
        {
            return this.boss != null && this.boss.Alive;
        }

        /// <summary>
        /// Khởi tạo cổng dịch chuyển
        /// </summary>
        private void CreateTeleport()
        {
            KDynamicArea teleport = KTDynamicAreaManager.Add(this.Map.MapCode, -1, BaiHuTang.Round2.Teleport.Name, BaiHuTang.Round2.Teleport.ResID, (int) BaiHuTang.Round2.Teleport.PosX, (int) BaiHuTang.Round2.Teleport.PosY, (this.DurationTicks - this.LifeTimeTicks) / 1000f, 2f, BaiHuTang.Round2.Teleport.Radius, -1, null);
            teleport.OnEnter = (obj) => {
                if (obj is KPlayer player)
                {
                    this.TeleportMovePlayerToNextStage(player);
                }
            };
        }

        /// <summary>
        /// Dịch người chơi đến cửa tiếp theo
        /// </summary>
        /// <param name="player"></param>
        private void TeleportMovePlayerToNextStage(KPlayer player)
        {
            UnityEngine.Vector2 randPos = BaiHuTang.RandomTeleportPositions[KTGlobal.GetRandomNumber(0, BaiHuTang.RandomTeleportPositions.Count - 1)];
            int posX = (int) randPos.x;
            int posY = (int) randPos.y;
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, BaiHuTang.Round2.NextMaps[this.Map.MapCode].NextMapID, posX, posY, (int) player.CurrentDir);
        }

        /// <summary>
        /// Đưa người chơi ra khỏi bản đồ hoạt động
        /// </summary>
        /// <param name="player"></param>
        private void KickOutPlayer(KPlayer player)
        {
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutMapID, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutPosX, BaiHuTang.Round2.OutMaps[this.Map.MapCode].OutPosY, (int) player.CurrentDir);
        }

        /// <summary>
        /// Đưa toàn bộ người chơi ra khỏi bản đồ hoạt động
        /// </summary>
        private void KickOutAllPlayers()
        {
            foreach (KPlayer player in this.GetPlayers())
            {
                this.KickOutPlayer(player);
            }
        }
        #endregion
    }
}
