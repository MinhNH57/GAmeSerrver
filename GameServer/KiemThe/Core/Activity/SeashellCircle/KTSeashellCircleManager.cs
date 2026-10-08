using System;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using GameServer.Core.Executor;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Server;
using System.Reflection.Emit;

namespace GameServer.KiemThe.Core.Activity.SeashellCircle
{
    /// <summary>
    /// Quản lý Vòng quay sò (Bách Bảo Rương)
    /// </summary>
    public static class KTSeashellCircleManager
    {

        // private const int COUPON_ID = 20780;
        // private const int MOONSTONE_ID = 506;
        // private const int EVENT_MATERIAL = 20471;
        /// <summary>
        /// Đánh dấu vị trí dừng lại lần trước ở vòng sò
        /// </summary>
        public const int SeashellCircle_LastSeashellCircleStopPos = 1000041;
        /// <summary>
        /// Đánh dấu tầng dừng lại lần trước ở vòng sò
        /// </summary>
        public const int SeashellCircle_LastSeashellCircleStage = 1000042;
        /// <summary>
        /// Đánh dấu số cược lần trước ở vòng sò
        /// </summary>
        public const int SeashellCircle_LastSeashellCircleBet = 1000043;

        #region Define

        public static Dictionary<int, Tuple<int, int>> CustomTreasureRate;

        /// <summary>
        /// Mở vòng quay sò không
        /// </summary>
        //public static bool EnableSeashellCircle => KTLuaLib_System.IsDateTimeInRange(Data.StartDate, Data.EndDate);

        public static string SeashellEventKey = "SeashellEvent";
        public static string SeashellEventFreeTicketKey = "SeashellEvent_FreeTicket";

        /// <summary>
        /// Vỏ sò vàng
        /// </summary>
        public static int Seashells = 746;

        /// <summary>
        /// Rương xấu xí
        /// </summary>
        private static int Treasure = 745;

        /// <summary>
        /// Tổng số ô
        /// </summary>
        private const int CellsPerRound = 22;

        /// <summary>
        /// Dữ liệu Bách bảo rương
        /// </summary>
        private static KTSeashellCircle Data = null;

        /// <summary>
        /// Tổng số sò đã tích lũy từ lần bảo trì trước tới thời điểm hiện tại
        /// </summary>
        private static int TotalIncomingSells = 0;

        /// <summary>
        /// Tổng số sò quyết định việc sẽ tăng rate
        /// </summary>
        private static int TotalSellsRate = 0;

        /// <summary>
        /// Tổng số sò đã nhả (hoặc quy đổi) từ lần bảo trì trước tới thời điểm hiện tại
        /// </summary>
        private static int TotalOutcomingSells = 0;

        public static string TopSeashellXML = "<Roles></Roles>";

        /// <summary>
        /// Khởi tạo dữ liệu
        /// </summary>
        public static void Init()
        {
            // TreasureRecord = new Dictionary<int, long>();
            CustomTreasureRate = new Dictionary<int, Tuple<int, int>>();
            // Đối tượng XML tương ứng đọc từ File
            XElement xmlNode = Global.GetGameResXml("Config/KT_Activity/KTSeashellCircle-EVENT.xml");
            // Chuyển sang dạng đối tượng
            KTSeashellCircle data = KTSeashellCircle.Parse(xmlNode);
            KTSeashellCircleManager.Data = data;
            Seashells = data.SeashellID;
            Treasure = data.TreasureID;

            var totalSeashells = GetSystemStorageSeashells();
            if (totalSeashells < 3000)
            {
                SetSystemStorageSeashells(3000);
            }
            
            // đọc log event từ file json 10'/lần
            // json được generate từ script python
            ScheduleExecutor2.Instance.scheduleCancle("TopSeashellRanking");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopSeashellRanking", ReadSeashellRanking), 1000,
                60000);
        }

        private static void ReadSeashellRanking()
        {
            string xml = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_SEASHELL, true);
            if (!string.IsNullOrEmpty(xml) && xml != "0" && xml != "-1")
            {
                TopSeashellXML = xml;
            }
        }

        public static void SetRateToTreasure(int rate) {
            Data.RateToTreasure = rate;
        }

        public static void SetRateTo100Treasure(int rate) {
            Data.RateTo100Treasure = rate;
        }

        #endregion Define

        #region Core

        /// <summary>
        /// tự động thêm vật phẩm vé bách bảo rương vào shop kỳ trân các
        /// </summary>
        public static void AddTicketItemToShop()
        {
            // nếu chưa có dữ liệu thì khởi tạo trước
            if (Data == null || Data.SeashellID == 0)
            {
                Init();
            }

            // nếu sau khi khởi tạo vẫn không có giá trị -> lỗi
            // không thao tác nữa
            if (Data == null || Data.SeashellID == 0)
                return;

            // nếu không trong thời gian chạy sự kiện thì thôi
            if (!EventTimeManager.IsEventEnabled(SeashellEventKey)) 
                return;
            
            // tạo shop item mới
            // id 9998 (cố định)
            // giá 25 knb
            // TODO: đẩy giá sang file config
            var newShopITem = new ShopItem
            {
                ID = 9998,
                ItemID = Data.SeashellID,
                GoodsPrice = 25
            };
            
            // thêm shop item vào danh sách
            ShopManager.ForceAddShopItem(newShopITem);

            // lấy shop tab huyền tinh và thêm shop item mới vào
            var tokenSale = ShopManager.TokenShop.Token.FirstOrDefault(x => x.ShopID == 203);
            tokenSale?.ShopItem.Remove(9998);
            tokenSale?.ShopItem.Insert(0, 9998);
            tokenSale?.Items.Insert(0, newShopITem);
        }

        /// <summary>
        /// Trả về tổng số vỏ sò mà hệ thống đã giữ của người chơi
        /// </summary>
        /// <returns></returns>
        public static long GetSystemStorageSeashells()
        {
            var systemSeashells = KTGlobal.GetSystemGlobalParameterInt64((int)SystemGlobalParameterIndexes.STORAGE_SEASHELL);
            if (systemSeashells < 3000)
                systemSeashells = 3000;
            return systemSeashells;
        }

        /// <summary>
        /// Thiết lập tổng số vỏ sò mà hệ thống đã giữ của người chơi
        /// </summary>
        /// <param name="totalSeashells"></param>
        private static void SetSystemStorageSeashells(long totalSeashells)
        {
            KTGlobal.SetSystemGlobalParameter((int)SystemGlobalParameterIndexes.STORAGE_SEASHELL,
                totalSeashells);
        }

        private static void UpdateSystemStorageSeashells(KPlayer client)
        {
            long totalStorage = KTSeashellCircleManager.GetSystemStorageSeashells();
            KTSeashellCircleManager.RecoverPreviousData(client, out int lastStage, out int lastStopPos,
                out int lastBet);
            bool enableGet = KTSeashellCircleManager.CanGet(client);
            bool enableExchange = KTSeashellCircleManager.CanExchange(client);
            bool enableBet = KTSeashellCircleManager.CanBet(client);
            bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
            var _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", 0, 1, totalStorage, lastStage,
                lastStopPos, lastBet, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0,
                enableBet ? 1 : 0);
            client.SendPacket((int)TCPGameServerCmds.CMD_KT_SEASHELL_CIRCLE, _cmdData);
        }

        public static void UpdateSeashellsInBag(KPlayer client)
        {
            return;
            // KT_TCPHandler.SendOpenUI(client, "Messages/Seashells", Array.Empty<int>(),
            //     new List<string>
            //     {
            //         "CoinInBag",
            //         ItemManager.GetItemCountInBag(client, KTSeashellCircleManager.Seashells).ToString() + " vé"
            //     });
        }

        public static string ReceiveFreeTicket(KPlayer player) 
        {
            var ret = -1;
            if (!EventTimeManager.IsEventEnabled(SeashellEventKey)) {
                // không trong thời gian hoạt động
                ret = -1;
            }

            if (!KTGlobal.IsHaveSpace(1, player))
            {
                // túi đồ đầy
                ret = -2;
            }
            
            var evt_freeticket = EventTimeManager.GetEvent(SeashellEventFreeTicketKey);

            int totalXuCharged = 0;

            if (evt_freeticket != null){
                totalXuCharged = (int)KTGlobal.GetPlayerTotalSumRechargeByTime(player, evt_freeticket.StartDate_str, evt_freeticket.EndDate_str);
            }

            var totalTicketReceived = Global.GetRoleParamsInt32FromDB(player, RoleParamName.SeashellFreeTicket, 0);

            var ticketToReceive = totalXuCharged / 100 - totalTicketReceived;
            if (ticketToReceive <= 0)
            {
                // đã nhận đủ
                ret = -3;
            }
            
            var evt = EventTimeManager.GetEvent(SeashellEventKey);

            if (!ItemManager.CreateSimpleItem(player, Data.SeashellID, (int)ticketToReceive, 0, 1, 0, evt.EndDate_str)) {
                // lỗi khi add vật phẩm
                ret = -9;
            } else {
                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.SeashellFreeTicket, totalXuCharged / 100, true);
                totalTicketReceived = totalXuCharged / 100;
            }
            return $"4:{ret}:{totalXuCharged}:{totalTicketReceived}";
        }

        public static void OpenExchangeCouponShop(KPlayer player) 
        {
            var _Shop = ShopManager.GetShopTable(Data.ShopID, player);

            if (_Shop != null)
            {
                KT_TCPHandler.SendShopData(player, _Shop);
            }
        }

        public static void OpenSeashellUI(KPlayer client)
        {
            // chặn BBR trong thời gian diễn ra các hoạt động tết
            // TODO: bỏ chặn sau khi hết sự kiện
            var currentDay = DateTime.Now.Day;
            if (!EventTimeManager.IsEventEnabled(SeashellEventKey))
            {
                PlayerManager.ShowMessageBox(client, "Thông báo",
                    "Hoạt động chưa mở.\nVui lòng quay trở lại sau.");
                return;
            }
            long totalStorage = KTSeashellCircleManager.GetSystemStorageSeashells();
            KTSeashellCircleManager.RecoverPreviousData(client, out int lastStage, out int lastStopPos,
                out int lastBet);
            bool enableGet = KTSeashellCircleManager.CanGet(client);
            bool enableExchange = KTSeashellCircleManager.CanExchange(client);
            bool enableBet = KTSeashellCircleManager.CanBet(client);
            bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);

            var evt = EventTimeManager.GetEvent(SeashellEventFreeTicketKey);

            long totalXuCharged = 0;
            if (evt != null){
                totalXuCharged = KTGlobal.GetPlayerTotalSumRechargeByTime(client, evt.StartDate_str, evt.EndDate_str);
            }

            var totalTicketReceived = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SeashellFreeTicket, 0);

            var currentScore = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SeashellScore, 0);

            var _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}", 0, 1, totalStorage, lastStage,
                lastStopPos, lastBet, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0,
                enableBet ? 1 : 0, totalXuCharged, totalTicketReceived, currentScore);

            client.SendPacket((int)TCPGameServerCmds.CMD_KT_SEASHELL_CIRCLE, _cmdData);
            UpdateSeashellsInBag(client);
        }

        /// <summary>
        /// Trả về thông tin ô tại vị trí tương ứng
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static KeyValuePair<KTSeashellCircle.Cell, int> GetCellAtPos(int pos)
        {
            // Duyệt danh sách ô
            foreach (KTSeashellCircle.Cell cell in KTSeashellCircleManager.Data.Cells)
            {
                // Nếu nằm trong vị trí 1 sao
                if (cell.Position_1.Any(x => x == pos))
                {
                    return new KeyValuePair<KTSeashellCircle.Cell, int>(cell, 1);
                }

                // Nếu nằm trong vị trí 2 sao
                if (cell.Position_2.Any(x => x == pos))
                {
                    return new KeyValuePair<KTSeashellCircle.Cell, int>(cell, 2);
                }

                // Nếu nằm trong vị trí 3 sao
                if (cell.Position_3.Any(x => x == pos))
                {
                    return new KeyValuePair<KTSeashellCircle.Cell, int>(cell, 3);
                }
            }

            // Không tìm thấy
            return new KeyValuePair<KTSeashellCircle.Cell, int>(null, -1);
        }

        /// <summary>
        /// Trả về loại ô dừng lại lần trước
        /// </summary>
        /// <param name="player"></param>
        private static int GetLastCellType(KPlayer player)
        {
            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;
            // Vị trí dừng lại trước đó
            int lastStopPos = player.LastSeashellCircleStopPos;

            // Nếu tầng trước đó không tồn tại
            if (lastStage < 0 || lastStage >= KTSeashellCircleManager.Data.ExchangeSeashells.Count)
            {
                return -1;
            }

            KeyValuePair<KTSeashellCircle.Cell, int> pair = KTSeashellCircleManager.GetCellAtPos(lastStopPos);
            // Nếu không tìm thấy
            if (pair.Key == null)
            {
                return -1;
            }

            return (int)pair.Key.Type;
        }

        /// <summary>
        /// Kiểm tra người chơi tương ứng có rương để nhận không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool HasTresureToGet(KPlayer player)
        {
            // Vị trí dừng lại trước đó
            int lastStopPos = player.LastSeashellCircleStopPos;
            // Trả về kết quả
            return KTSeashellCircleManager.Data.TreasurePosition.Contains(lastStopPos);
        }

        /// <summary>
        /// Kiểm tra người chơi tương ứng có quà để nhận không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool CanGet(KPlayer player)
        {
            // Nếu có rương thì ok
            if (KTSeashellCircleManager.HasTresureToGet(player))
            {
                return true;
            }

            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;

            // Nếu tầng trước đó không tồn tại
            if (lastStage < 0 || lastStage >= KTSeashellCircleManager.Data.ExchangeSeashells.Count)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra người chơi tương ứng có thể đổi sò không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool CanExchange(KPlayer player)
        {
            return KTSeashellCircleManager.CanGet(player) && !KTSeashellCircleManager.HasTresureToGet(player);
        }

        /// <summary>
        /// Kiểm tra người chơi tương ứng có thể đặt cược không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool CanBet(KPlayer player)
        {
            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;
            // Nếu không có gì để nhận và đổi thì có thể đặt cược
            return (lastStage < 0 || lastStage >= KTSeashellCircleManager.Data.ExchangeSeashells.Count) &&
                   (!KTSeashellCircleManager.HasTresureToGet(player) && !KTSeashellCircleManager.CanGet(player) &&
                    !KTSeashellCircleManager.CanExchange(player));
        }

        /// <summary>
        /// Kiểm tra người chơi tương ứng có thể thực hiện quay không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool CanStartTurn(KPlayer player)
        {
            // Nếu có rương thì không được
            return !KTSeashellCircleManager.HasTresureToGet(player);
        }

        #endregion Core

        #region Public methods

        /// <summary>
        /// Khôi phục lại thông tin lần trước
        /// </summary>
        /// <param name="player"></param>
        /// <param name="lastStage"></param>
        /// <param name="lastStopPos"></param>
        /// <param name="lastBet"></param>
        /// <param name="enableGet"></param>
        /// <param name="enableExchange"></param>
        /// <param name="enableBet"></param>
        /// <returns></returns>
        public static void RecoverPreviousData(KPlayer player, out int lastStage, out int lastStopPos, out int lastBet)
        {
            // Tầng lần trước đó
            lastStage = player.LastSeashellCircleStage;
            // Vị trí dừng lại trước đó
            lastStopPos = player.LastSeashellCircleStopPos;
            // Số sò cược lần trước
            lastBet = player.LastSeashellCircleBet;
        }

        /// <summary>
        /// Nhận vỏ sò
        /// </summary>
        /// <param name="player"></param>
        /// <param name="totalSeashells"></param>
        /// <returns></returns>
        public static bool GetSeashell(KPlayer player, out int totalSeashells)
        {
            // Tổng số sò nhận được
            totalSeashells = 0;
            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;
            // Số sò cược lần trước
            int lastBet = player.LastSeashellCircleBet;

            // Nếu số lượng cược không khớp
            if (lastBet != 2 && lastBet != 10 && lastBet != 50)
            {
                PlayerManager.ShowNotification(player, "Xin lỗi, bạn không có quà để nhận!");
                return false;
            }

            // Nếu không có phần quà
            if (KTSeashellCircleManager.CanExchange(player))
            {
                // Nếu tầng trước đó không tồn tại
                if (lastStage < 0 || lastStage >= KTSeashellCircleManager.Data.ExchangeSeashells.Count)
                {
                    PlayerManager.ShowNotification(player, "Bạn không có phần thưởng có thể quy đổi qua vé!");
                    return false;
                }

                // Số sò có thể đổi được
                int shellsCount = KTSeashellCircleManager.Data.ExchangeSeashells[lastStage].Value * lastBet / 100;
                // do event nên tỉ lệ đổi giảm một nửa
                shellsCount /= 2;
                // Số ô trong túi cần
                int totalSpacesNeed = ItemManager.TotalSpaceNeed(KTSeashellCircleManager.Seashells, shellsCount);
                // Nếu túi đồ không đủ chỗ trống
                if (!KTGlobal.IsHaveSpace(totalSpacesNeed, player))
                {
                    PlayerManager.ShowNotification(player,
                        string.Format("Bạn cần sắp xếp {0} ô trống trong túi đồ để nhận xu!", totalSpacesNeed));
                    return false;
                }

                // Reset toàn bộ
                player.LastSeashellCircleStage = -1;
                player.LastSeashellCircleStopPos = -1;
                player.LastSeashellCircleBet = -1;
                // Lưu thông tin vào DB
                player.SaveSeashellCircleParamToDB();

                // Tạo vật phẩm cho người chơi
                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                        KTSeashellCircleManager.Data.CouponID, shellsCount, 0, "SeashellCircle", true, 0, false,
                        Global.ConstGoodsEndTime))
                {
                    PlayerManager.ShowNotification(player,
                        "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.",
                            KTSeashellCircleManager.Seashells, player.RoleID));
                    return false;
                }
                
                UpdateScore(player, lastStage, lastBet);

                // Thông báo đổi thành công
                PlayerManager.ShowNotification(player,
                    string.Format("Đổi thành công, bạn nhận được {0} phiếu đổi thưởng!", shellsCount));

                // Cập nhật tổng số sò nhận được
                totalSeashells = shellsCount;

                // Ghi LOG
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    string.Format("{0} (ID: {1}) đổi được {2} xu.", player.RoleName, player.RoleID, shellsCount));

                if (lastStage >= 2)
                    ShowReward(player.RoleName, lastBet, $"{shellsCount} {KTGlobal.GetItemName(Data.CouponID)}");

                // Tăng tổng số sò đã nhả của Server
                KTSeashellCircleManager.TotalOutcomingSells += shellsCount;
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    string.Format("Server nhả tổng số sò: {0}", KTSeashellCircleManager.TotalOutcomingSells));
            }

            // Trả về nhận thành công
            return true;
        }

        private static void ShowReward(string playerName, int seashellNumber, string reward)
        {
            var sPlayerName = KTGlobal.CreateStringByColor(playerName, ColorType.AppleGreen);
            var seashellTemplate = ItemManager.GetItemTemplate(Seashells);
            var sSeashellNumber =
                KTGlobal.CreateStringByColor($"{seashellNumber} {seashellTemplate.Name}", ColorType.PeachYellow);
            var sReward = KTGlobal.CreateStringByColor(reward, ColorType.VanillaIce);
            KTGlobal.SendSystemEventNotification(
                $"Chúc mừng {sPlayerName} sử dụng {sSeashellNumber} quay Bách Bảo Rương [Sự Kiện] nhận được {sReward}!");
        }

        /// <summary>
        /// Nhận quà
        /// </summary>
        /// <param name="player"></param>
        /// <param name="awardType"></param>
        /// <param name="itemID"></param>
        /// <param name="itemNumber"></param>
        /// <returns></returns>
        public static bool GetAward(KPlayer player, out int awardType, out int itemID, out int itemNumber)
        {
            // Loại quà
            awardType = -1;
            // Vật phẩm nhận được
            itemID = -1;
            // Số lượng nhận được
            itemNumber = 0;

            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;
            // Vị trí dừng lại trước đó
            int lastStopPos = player.LastSeashellCircleStopPos;
            // Số sò cược lần trước
            int lastBet = player.LastSeashellCircleBet;
            // Loại ô lần trước
            int lastCellType = KTSeashellCircleManager.GetLastCellType(player);

            // Nếu số lượng cược không khớp
            if (lastBet != 2 && lastBet != 10 && lastBet != 50)
            {
                LogManager.WriteLog(LogTypes.SeashellCircle, $"{player.RoleName} số lượng cược không khớp. {lastBet}");
                PlayerManager.ShowNotification(player, "Xin lỗi, bạn không có quà để nhận!");
                return false;
            }

            // Tỷ lệ tăng thêm của quà thưởng (lần)
            int awardRate;
            if (lastBet == 2)
            {
                awardRate = KTSeashellCircleManager.Data.BetRates.Bet_2;
            }
            else if (lastBet == 10)
            {
                awardRate = KTSeashellCircleManager.Data.BetRates.Bet_10;
            }
            else
            {
                awardRate = KTSeashellCircleManager.Data.BetRates.Bet_50;
            }

            // Nếu dừng lại ở rương
            if (KTSeashellCircleManager.HasTresureToGet(player))
            {
                // var hasRecord = TreasureRecord.TryGetValue(player.RoleID, out var recorded);
                // if (!hasRecord)
                // {
                //     player.LastSeashellCircleStage = -1;
                //     player.LastSeashellCircleStopPos = -1;
                //     player.LastSeashellCircleBet = -1;
                //     // Lưu thông tin vào DB
                //     player.SaveSeashellCircleParamToDB();
                //     PlayerManager.ShowNotification(player,
                //         "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                //     LogManager.WriteLog(LogTypes.SeashellCircle,
                //         $"Nổ hũ cho {player.RoleName}[{player.RoleID}] lỗi, không tìm thấy record trước đó.");
                //     return false;
                // }
                // var numberSeashell = (int)(recorded);

                var storageSeashells = GetSystemStorageSeashells();
                // var numberSeashell = (int)(storageSeashells * percentage / 100);
                var numberSpaceNeeded = KTGlobal.GetTotalSpacesNeedToTakeItem(Treasure, awardRate);

                // Nếu túi đồ không đủ ô trống
                if (!KTGlobal.IsHaveSpace(numberSpaceNeeded, player))
                {
                    PlayerManager.ShowNotification(player,
                        string.Format("Bạn cần sắp xếp {0} ô trống trong túi đồ để nhận quà!", numberSpaceNeeded));
                    return false;
                }

                // Thông báo nhận được rương tới tất cả người chơi
                // KTGlobal.SendSystemEventNotification(string.Format("Người chơi <color=#38c3ff>[{0}]</color> tại sự kiện <color=green>Bách Bảo Rương</color> đã may mắn quay được <color=orange>{1} cái</color> <color=yellow>[{2}]</color>. Xin chúc mừng!", player.RoleName, awardRate, KTGlobal.GetItemName(KTSeashellCircleManager.Treasure)));

                // Reset toàn bộ
                player.LastSeashellCircleStage = -1;
                player.LastSeashellCircleStopPos = -1;
                player.LastSeashellCircleBet = -1;
                // TreasureRecord.Remove(player.RoleID);
                // Lưu thông tin vào DB
                player.SaveSeashellCircleParamToDB();

                // Tạo vật phẩm cho người chơi
                if (awardRate > 0 && !ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                        KTSeashellCircleManager.Treasure, awardRate, 0, "SeashellCircle", true, 0, false,
                        Global.ConstGoodsEndTime))
                {
                    PlayerManager.ShowNotification(player,
                        "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.",
                            KTSeashellCircleManager.Treasure, player.RoleID));
                    return false;
                }

                // storageSeashells -= numberSeashell;
                // SetSystemStorageSeashells(storageSeashells);
                // UpdateSystemStorageSeashells(player);

                // Thông báo đổi thành công
                PlayerManager.ShowNotification(player,
                    string.Format("Nhận thành công, bạn nhận được {0} cái [{1}]!", awardRate,
                        KTGlobal.GetItemName(KTSeashellCircleManager.Treasure)));

                ShowReward(player.RoleName, lastBet,
                    $"{awardRate} {KTGlobal.GetItemName(KTSeashellCircleManager.Treasure)}");

                // Cập nhật quà nhận được là vật phẩm
                awardType = 0;
                // Cập nhật vật phẩm nhận được
                itemID = KTSeashellCircleManager.Treasure;
                // Cập nhật số lượng nhận được
                itemNumber = awardRate;

                // Ghi LOG
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    string.Format("{0} (ID: {1}) nhận được {2} Rương xấu xí.", player.RoleName, player.RoleID,
                        itemNumber));
                return true;
            }
            // Nếu có quà có thể nhận
            else if (KTSeashellCircleManager.CanGet(player))
            {
                // Nếu tầng trước đó không tồn tại
                if (lastStage < 0 || lastStage >= KTSeashellCircleManager.Data.TurnRateByStages.Count)
                {
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        $"{player.RoleName} tầng không tồn tại. {lastStage}/{Data.TurnRateByStages.Count}");
                    PlayerManager.ShowNotification(player, "Xin lỗi, bạn không có quà để nhận!");
                    return false;
                }
                // Nếu ô không tồn tại
                else if (lastCellType < 0 || lastCellType >= (int)KTSeashellCircle.SeashellCircleCellType.Total)
                {
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        $"{player.RoleName} ô không tồn tại. {lastCellType}/{Data.Cells.Count}");
                    PlayerManager.ShowNotification(player, "Xin lỗi, bạn không có quà để nhận!");
                    return false;
                }

                // Dữ liệu ô ở tầng trước đó
                KTSeashellCircle.Cell cell = KTSeashellCircleManager.Data.Cells[lastCellType];

                // Giá trị quà ở tầng này
                int value = cell.ValuesByStage[lastStage];

                // Nếu là Huyền Tinh thì yêu cầu chỗ trống tương ứng
                if (cell.Type == KTSeashellCircle.SeashellCircleCellType.CrystalStone &&
                    !KTGlobal.IsHaveSpace(awardRate, player))
                {
                    PlayerManager.ShowNotification(player,
                        string.Format("Bạn cần sắp xếp {0} ô trống trong túi đồ để nhận quà!", awardRate));
                    return false;
                }

                if (cell.Type == KTSeashellCircle.SeashellCircleCellType.BirthdayCoupon &&
                    !KTGlobal.IsHaveSpace(1, player))
                {
                    PlayerManager.ShowNotification(player, "Bạn cần sắp xếp 1 ô trống trong túi đồ để nhận quà!");
                    return false;
                }

                // Reset toàn bộ
                player.LastSeashellCircleStage = -1;
                player.LastSeashellCircleStopPos = -1;
                player.LastSeashellCircleBet = -1;
                // Lưu thông tin vào DB
                player.SaveSeashellCircleParamToDB();

                // Xem loại nhận là gì
                switch (lastCellType)
                {
                    // Huyền Tinh
                    case (int)KTSeashellCircle.SeashellCircleCellType.CrystalStone:
                    {
                        // Tạo vật phẩm cho người chơi
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, value, awardRate, 0,
                                "SeashellCircle", true, 0, false, Global.ConstGoodsEndTime))
                        {
                            PlayerManager.ShowNotification(player,
                                "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                            LogManager.WriteLog(LogTypes.SeashellCircle,
                                string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.", value,
                                    player.RoleID));
                            return false;
                        }

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player,
                            string.Format("Nhận thành công, bạn nhận được {0} cái [{1}]!", awardRate,
                                KTGlobal.GetItemName(value)));

                        // Cập nhật quà nhận được là vật phẩm
                        awardType = 0;
                        // Cập nhật vật phẩm nhận được
                        itemID = value;
                        // Cập nhật số lượng nhận được
                        itemNumber = awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} cái {3}.", player.RoleName, player.RoleID,
                                itemNumber, KTGlobal.GetItemName(value)));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} {KTGlobal.GetItemName(value)}");
                        }

                        break;
                    }
                    // Tinh hoạt lực
                    case (int)KTSeashellCircle.SeashellCircleCellType.GatherMakePoint:
                    {
                        // Thêm Tinh Hoạt lực cho người chơi
                        player.ChangeCurMakePoint(value * awardRate);
                        player.ChangeCurGatherPoint(value * awardRate);

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player, string.Format("Nhận thành công!"));

                        // Cập nhật quà nhận được là tinh hoạt lực
                        awardType = 1;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} Tinh hoạt lực.", player.RoleName, player.RoleID,
                                itemNumber));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} tinh hoạt lực");
                        }

                        break;
                    }
                    // Bạc
                    case (int)KTSeashellCircle.SeashellCircleCellType.Money:
                    {
                        GameManager.ClientMgr.AddMoney(player, value * awardRate, "SeashellCircle");

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player, string.Format("Nhận thành công!"));

                        // Cập nhật quà nhận được là bạc
                        awardType = 2;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} Bạc.", player.RoleName, player.RoleID,
                                itemNumber));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber / 10000} vạn bạc");
                        }

                        break;
                    }
                    // Đồng khóa
                    case (int)KTSeashellCircle.SeashellCircleCellType.BoundToken:
                    {
                        GameManager.ClientMgr.AddBoundToken(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, player,
                            value * awardRate, "SeashellCircle");

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player, string.Format("Nhận thành công!"));

                        // Cập nhật quà nhận được là đồng khóa
                        awardType = 3;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} KNB khóa.", player.RoleName, player.RoleID,
                                itemNumber));
                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} KNB");
                        }

                        break;
                    }
                    case (int)KTSeashellCircle.SeashellCircleCellType.BirthdayCoupon:
                    {
                        // Tạo vật phẩm cho người chơi
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, Data.CouponID,
                                value * awardRate, 0, "SeashellCircle", true, 0, false, Global.ConstGoodsEndTime))
                        {
                            PlayerManager.ShowNotification(player,
                                "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                            LogManager.WriteLog(LogTypes.SeashellCircle,
                                string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.", value,
                                    player.RoleID));
                            return false;
                        }

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player,
                            string.Format("Nhận thành công, bạn nhận được {0} cái [{1}]!", awardRate,
                                KTGlobal.GetItemName(Data.CouponID)));

                        // Cập nhật quà nhận được là coupon sinh nhật
                        awardType = 0;
                        // Cập nhật vật phẩm nhận được
                        itemID = Data.CouponID;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} cái {3}.", player.RoleName, player.RoleID,
                                itemNumber, KTGlobal.GetItemName(itemID)));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} {KTGlobal.GetItemName(itemID)}");
                        }

                        break;
                    }
                    case (int)KTSeashellCircle.SeashellCircleCellType.MoonStone:
                    {
                        // Tạo vật phẩm cho người chơi
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, Data.MoonStoneID,
                                value * awardRate, 0, "SeashellCircle", true, 0, false, Global.ConstGoodsEndTime))
                        {
                            PlayerManager.ShowNotification(player,
                                "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                            LogManager.WriteLog(LogTypes.SeashellCircle,
                                string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.", value,
                                    player.RoleID));
                            return false;
                        }

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player,
                            string.Format("Nhận thành công, bạn nhận được {0} cái [{1}]!", awardRate,
                                KTGlobal.GetItemName(Data.MoonStoneID)));

                        // Cập nhật quà nhận được là coupon sinh nhật
                        awardType = 0;
                        // Cập nhật vật phẩm nhận được
                        itemID = Data.MoonStoneID;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} cái {3}.", player.RoleName, player.RoleID,
                                itemNumber, KTGlobal.GetItemName(itemID)));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} {KTGlobal.GetItemName(itemID)}");
                        }

                        break;
                    }
                    case (int)KTSeashellCircle.SeashellCircleCellType.EventMaterial:
                    {
                        // Tạo vật phẩm cho người chơi
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, Data.EventMaterialID,
                                value * awardRate, 0, "SeashellCircle", true, 0, false, Global.ConstGoodsEndTime))
                        {
                            PlayerManager.ShowNotification(player,
                                "Có lỗi khi tạo vật phẩm, hãy liên hệ với hỗ trợ để được trợ giúp!");
                            LogManager.WriteLog(LogTypes.SeashellCircle,
                                string.Format("[ERROR] Tạo vật phẩm ID {0} cho RoleID {1} lỗi SML.", value,
                                    player.RoleID));
                            return false;
                        }

                        // Thông báo đổi thành công
                        PlayerManager.ShowNotification(player,
                            string.Format("Nhận thành công, bạn nhận được {0} cái [{1}]!", awardRate,
                                KTGlobal.GetItemName(Data.EventMaterialID)));

                        // Cập nhật quà nhận được là coupon sinh nhật
                        awardType = 0;
                        // Cập nhật vật phẩm nhận được
                        itemID = Data.EventMaterialID;
                        // Cập nhật số lượng nhận được
                        itemNumber = value * awardRate;

                        // Ghi LOG
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format("{0} (ID: {1}) nhận được {2} cái {3}.", player.RoleName, player.RoleID,
                                itemNumber, KTGlobal.GetItemName(itemID)));

                        if (lastStage >= 2)
                        {
                            ShowReward(player.RoleName, lastBet, $"{itemNumber} {KTGlobal.GetItemName(itemID)}");
                        }

                        break;
                    }
                }

                // Quy ra số sò có thể đổi được để ghi vào LOG thống kê
                int shellsCount = KTSeashellCircleManager.Data.ExchangeSeashells[lastStage].Value * lastBet / 100;
                // Tăng tổng số sò đã nhả của Server
                KTSeashellCircleManager.TotalOutcomingSells += shellsCount;
                LogManager.WriteLog(LogTypes.SeashellCircle,
                     string.Format("Server nhả tổng số sò: {0}", KTSeashellCircleManager.TotalOutcomingSells));
                
                
                UpdateScore(player, lastStage, lastBet);
            }
            else
            {
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    $"{player.RoleName} ô không tồn tại. {lastCellType}/{Data.Cells.Count}");
                PlayerManager.ShowNotification(player, "Xin lỗi, bạn không có quà để nhận!");
                return false;
            }

            // Trả về nhận thành công
            return true;
        }

        // private static Dictionary<int, long> TreasureRecord;

        /// <summary>
        /// Bắt đầu quay
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="bet">Số sò đã cược</param>
        /// <param name="lastStopPos">Vị trí sẽ dừng lại ở lượt quay này (xem thứ tự ở khung ở Client, đánh số từ trái sang phải, trên xuống dưới)</param>
        /// <param name="durationTick">Thời gian quay</param>
        /// <param name="totalCells">Tổng số ô cần qua</param>
        /// <param name="isFirstTurn">Có phải lần đầu quay không</param>
        public static bool StartTurn(KPlayer player, int bet, out int lastStopPos, out int durationTick,
            out int totalCells, out bool isFirstTurn)
        {
            // Thời gian quay
            durationTick = -1;
            // Tổng số ô cần qua
            totalCells = 0;
            // Đánh dấu không phải lần đầu
            isFirstTurn = false;

            // Tầng lần trước đó
            int lastStage = player.LastSeashellCircleStage;
            // Vị trí dừng lại trước đó
            lastStopPos = player.LastSeashellCircleStopPos;
            // Số sò cược lần trước
            int lastBet = player.LastSeashellCircleBet;
            // Loại ô lần trước
            int lastCellType = KTSeashellCircleManager.GetLastCellType(player);

            // Nếu có rương để nhận
            if (KTSeashellCircleManager.HasTresureToGet(player))
            {
                PlayerManager.ShowNotification(player, "Bạn có phần quà cần nhận trước mới có thể quay tiếp!");
                return false;
            }

            // Nếu không thể quay
            if (!KTSeashellCircleManager.CanStartTurn(player))
            {
                PlayerManager.ShowNotification(player,
                    "Bạn hiện không thể thực hiện quay, hãy liên hệ với hỗ trợ để được xử lý!");
                return false;
            }

            // Đánh dấu có phải lần đầu quay không
            isFirstTurn = lastStage < 0;

            // Nếu tầng lần trước tồn tại và số sò cược ở lần này không khớp
            if (lastStage >= 0 && lastStage < KTSeashellCircleManager.Data.TurnRateByStages.Count && lastBet != bet)
            {
                PlayerManager.ShowNotification(player, "Thao tác bị lỗi. Số sò cược giữa 2 lần liên tiếp không khớp!");
                return false;
            }

            // Nếu là lần đầu
            if (isFirstTurn)
            {
                // Nếu số sò có trên người không đủ
                if (ItemManager.GetItemCountInBag(player, KTSeashellCircleManager.Seashells) < bet)
                {
                    PlayerManager.ShowNotification(player, "Số vé mang theo không đủ!");
                    return false;
                }

                LogManager.WriteLog(LogTypes.SeashellCircle, $"{player.RoleName} bắt đầu quay, cược {bet}");
            }

            // Tầng hiện tại
            int currentStage = lastStage;
            // Nếu tầng hiện tại vượt quá ngưỡng
            if (currentStage < 0 || currentStage >= KTSeashellCircleManager.Data.TurnRateByStages.Count)
            {
                // Thiết lập lại
                currentStage = -1;
            }

            // Có quay vào rương được không
            bool willGetTreasure = false;
            // Nếu là lượt đầu tiên
            if (isFirstTurn)
            {
                // Nếu có GM thiết lập
                if (player.GM_SetWillGetTreasureNextTurn && (player.GM_SetWillGetTreasureNextTurnWithBet == -1 ||
                                                             player.GM_SetWillGetTreasureNextTurnWithBet == bet))
                {
                    // Thiết lập có thể quay vào rương
                    willGetTreasure = true;
                    // Hủy đánh dấu GM thiết lập
                    player.GM_SetWillGetTreasureNextTurn = false;
                    player.GM_SetWillGetTreasureNextTurnWithBet = -1;
                }
                // Nếu không có GM thiết lập
                else
                {
                    int BOUNDRATE = 0;
                    int BOUNDFROMLUCKY = 0;
                    
                    
                    // BBR sinh nhật, không tính luck của user
                    // // Nếu mà Rate > 10.000 thì tỉ lệ vào rương sẽ tăng thêm 10
                    // if (KTSeashellCircleManager.TotalSellsRate > 10000)
                    // {
                    //     BOUNDRATE = KTSeashellCircleManager.TotalSellsRate / 1000;
                    // }
                    //
                    // int LUCKEY = player.m_nCurLucky;
                    //
                    // // Nếu người chơi có điểm may mắn và điểm may mắn >20
                    // if (LUCKEY > 20)
                    // {
                    //     BOUNDFROMLUCKY = LUCKEY / 20;
                    //     //Cho đoạn này vào để chắc chắn ko vượt quá 5% may mắn
                    //     if (BOUNDFROMLUCKY > 5)
                    //     {
                    //         BOUNDFROMLUCKY = 5;
                    //     }
                    // }

                    // if (GetSystemStorageSeashells() >= 15000)
                    // {
                    //     willGetTreasure = false;
                    // } else 
                    {
                        // Tỷ lệ ngẫu nhiên
                        int myRate = KTGlobal.GetRandomNumber(1, 5000);

                        var treasureRate = Data.RateToTreasure;
                        if (CustomTreasureRate.TryGetValue(player.RoleID, out var customRate))
                            treasureRate = customRate.Item1;
                        // Nếu may mắn
                        if (myRate <= (treasureRate + BOUNDRATE + BOUNDFROMLUCKY))
                        {
                            // Nếu có thằng quay vào rương thì reset lại rate
                            KTSeashellCircleManager.TotalSellsRate = 0;
                            willGetTreasure = true;
                        }
                    }                    
                }
            }

            // Vị trí sẽ dừng lại
            int stopPos;
            // Thời gian quay (Tick)
            durationTick = KTGlobal.GetRandomNumber(KTSeashellCircleManager.Data.MinTimeTick,
                KTSeashellCircleManager.Data.MaxTimeTick);
            // Số vòng quay
            int totalRounds = KTGlobal.GetRandomNumber(KTSeashellCircleManager.Data.MinRound,
                KTSeashellCircleManager.Data.MaxRound);
            // Đánh dấu quay thành công không
            bool isSuccess;
            // Tầng kế sau khi quay
            int nextStage = currentStage;

            // Nếu quay vào rương
            if (willGetTreasure)
            {
                // Chọn vị trí dừng
                stopPos = KTSeashellCircleManager.Data.TreasurePosition[
                    KTGlobal.GetRandomNumber(0, KTSeashellCircleManager.Data.TreasurePosition.Count - 1)];

                var treasure100Rate = Data.RateTo100Treasure;
                if (CustomTreasureRate.TryGetValue(player.RoleID, out var customRate))
                {
                    treasure100Rate = customRate.Item2;
                }
                // BBR sinh nhật, có 90% vào ô 3, 10% vào ô 14
                stopPos = StaticRandom.PositiveOutcome(treasure100Rate) ? 14 : 3;

                // if (stopPos == 3)
                // {
                //     // 5%
                //     TreasureRecord[player.RoleID] = GetSystemStorageSeashells() / 20;
                // }
                // else
                // {
                //     // 20%
                //     TreasureRecord[player.RoleID] = GetSystemStorageSeashells() / 5;
                // }
                
                // Đánh dấu quay thành công
                isSuccess = true;
            }
            // Nếu không quay vào rương
            else
            {
                // Thông tin tỷ lệ quay tầng hiện tại
                KTSeashellCircle.TurnRateByStage stageInfo =
                    KTSeashellCircleManager.Data.TurnRateByStages[currentStage + 1];

                // Danh sách các vị trí 1 sao
                List<int> position_1 = new List<int>();
                // Danh sách các vị trí 2 sao
                List<int> position_2 = new List<int>();
                // Danh sách các vị trí 3 sao
                List<int> position_3 = new List<int>();

                // Có quay vào ô cùng loại không
                bool willGetSameTypeCell = false;
                // Nếu tầng hiện tại khác 0
                if (currentStage != -1)
                {
                    // Tỷ lệ hiện tại
                    int nRate = KTGlobal.GetRandomNumber(1, 1000);
                    // Nếu có thể quay vào ô cùng loại
                    if (nRate <= stageInfo.RateToSameType)
                    {
                        // Đánh dấu có thể quay vào ô cùng loại
                        willGetSameTypeCell = true;
                    }
                }

                // Nếu tầng hiện tại là -1
                if (currentStage == -1)
                {
                    // Duyệt danh sách các ô
                    foreach (KTSeashellCircle.Cell cell in KTSeashellCircleManager.Data.Cells)
                    {
                        // Thêm các ô 1 sao vào
                        position_1.AddRange(cell.Position_1);
                        // Thêm các ô 2 sao vào
                        position_2.AddRange(cell.Position_2);
                        // Thêm các ô 3 sao vào
                        position_3.AddRange(cell.Position_3);
                        position_1.RemoveAll(x => x == -1);
                        position_2.RemoveAll(x => x == -1);
                        position_3.RemoveAll(x => x == -1);
                    }

                    // Đánh dấu quay thành công
                    isSuccess = true;
                }
                // Nếu không thể quay vào ô cùng loại
                else if (!willGetSameTypeCell)
                {
                    // Duyệt danh sách các ô
                    foreach (KTSeashellCircle.Cell cell in KTSeashellCircleManager.Data.Cells)
                    {
                        // Nếu loại trùng với loại hiện tại thì bỏ qua
                        if ((int)cell.Type == lastCellType)
                        {
                            continue;
                        }

                        // Thêm các ô 1 sao vào
                        position_1.AddRange(cell.Position_1);
                        // Thêm các ô 2 sao vào
                        position_2.AddRange(cell.Position_2);
                        // Thêm các ô 3 sao vào
                        position_3.AddRange(cell.Position_3);
                        position_1.RemoveAll(x => x == -1);
                        position_2.RemoveAll(x => x == -1);
                        position_3.RemoveAll(x => x == -1);
                    }

                    // Đánh dấu quay thất bại
                    isSuccess = false;
                }
                // Nếu tầng hiện tại khác 0
                else
                {
                    // Nếu loại ô không hợp lý
                    if (lastCellType < 0 || lastCellType >= KTSeashellCircleManager.Data.Cells.Count)
                    {
                        PlayerManager.ShowNotification(player,
                            "Dữ liệu bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                        LogManager.WriteLog(LogTypes.SeashellCircle,
                            string.Format(
                                "[ERROR] Lỗi bắt đầu quay sò của RoleID {0} SML. 'lastCellType < 0 || lastCellType >= KTSeashellCircleManager.Data.Cells.Count'",
                                player.RoleID));
                        return false;
                    }

                    // Loại ô tương ứng
                    KTSeashellCircle.Cell cell = KTSeashellCircleManager.Data.Cells[lastCellType];
                    // Thêm các ô 1 sao vào
                    position_1.AddRange(cell.Position_1);
                    // Thêm các ô 2 sao vào
                    position_2.AddRange(cell.Position_2);
                    // Thêm các ô 3 sao vào
                    position_3.AddRange(cell.Position_3);
                    position_1.RemoveAll(x => x == -1);
                    position_2.RemoveAll(x => x == -1);
                    position_3.RemoveAll(x => x == -1);
                    // Đánh dấu quay thành công
                    isSuccess = true;
                }

                // Loại sẽ quay vào
                int type = -1;
                // Tỷ lệ ngẫu nhiên
                int myRate = KTGlobal.GetRandomNumber(1, 1000);
                // Nếu có thể quay vào ô 3 sao
                if (myRate <= stageInfo.RateToPosition_3)
                {
                    // Loại sẽ quay vào ô 3 sao
                    type = 3;
                }
                else
                {
                    myRate -= stageInfo.RateToPosition_3;
                    // Nếu có thể quay vào ô 2 sao
                    if (myRate <= stageInfo.RateToPosition_2)
                    {
                        // Loại sẽ quay vào ô 2 sao
                        type = 2;
                    }
                    else
                    {
                        myRate -= stageInfo.RateToPosition_2;
                        if (myRate <= stageInfo.RateToPosition_1)
                        {
                            // Loại sẽ quay vào ô 1 sao
                            type = 1;
                        }
                    }
                }

                //giảm tỷ lệ vào ô huyền tinh ỏe lượt đầu
                // if (isFirstTurn)
                // {
                //     int rateToHTTurn1 = KTGlobal.GetRandomNumber(1, 100);
                //
                //     List<int> position_1WithoutHT = new List<int> { 2, 5, 19, 8, 13, 16 };
                //     List<int> position_2WithoutHT = new List<int> { 10, 21 };
                //     List<int> position_3WithoutHT = new List<int> { 1, 12 };
                //     List<int> position_1OnlyHT = new List<int> { 7, 11, 20 };
                //     List<int> position_1OnlyTHL = new List<int> { 0, 9, 18 };
                //
                //     if (rateToHTTurn1 < 15)
                //     {
                //         switch (type)
                //         {
                //             // 1 sao
                //             case 1:
                //             {
                //                 nextStage++;
                //                 stopPos = position_1OnlyTHL[KTGlobal.GetRandomNumber(0, position_1OnlyTHL.Count - 1)];
                //                 break;
                //             }
                //             // 2 sao
                //             case 2:
                //             {
                //                 nextStage += 2;
                //                 stopPos = 4;
                //                 break;
                //             }
                //             // 3 sao
                //             case 3:
                //             {
                //                 nextStage += 3;
                //                 stopPos = 17;
                //                 break;
                //             }
                //             // Toác
                //             default:
                //             {
                //                 PlayerManager.ShowNotification(player,
                //                     "Dữ liệu bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                //                 LogManager.WriteLog(LogTypes.SeashellCircle,
                //                     string.Format(
                //                         "[ERROR] Lỗi bắt đầu quay sò của RoleID {0} SML. 'switch (type) => default'",
                //                         player.RoleID));
                //                 return false;
                //             }
                //         }
                //     }
                //     else if (rateToHTTurn1 <= 85)
                //     {
                //         switch (type)
                //         {
                //             // 1 sao
                //             case 1:
                //             {
                //                 nextStage++;
                //                 stopPos = position_1WithoutHT[
                //                     KTGlobal.GetRandomNumber(0, position_1WithoutHT.Count - 1)];
                //                 break;
                //             }
                //             // 2 sao
                //             case 2:
                //             {
                //                 nextStage += 2;
                //                 stopPos = position_2WithoutHT[
                //                     KTGlobal.GetRandomNumber(0, position_2WithoutHT.Count - 1)];
                //                 break;
                //             }
                //             // 3 sao
                //             case 3:
                //             {
                //                 nextStage += 3;
                //                 stopPos = position_3WithoutHT[
                //                     KTGlobal.GetRandomNumber(0, position_3WithoutHT.Count - 1)];
                //                 break;
                //             }
                //             // Toác
                //             default:
                //             {
                //                 PlayerManager.ShowNotification(player,
                //                     "Dữ liệu bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                //                 LogManager.WriteLog(LogTypes.SeashellCircle,
                //                     string.Format(
                //                         "[ERROR] Lỗi bắt đầu quay sò của RoleID {0} SML. 'switch (type) => default'",
                //                         player.RoleID));
                //                 return false;
                //             }
                //         }
                //     }
                //     else
                //     {
                //         switch (type)
                //         {
                //             // 1 sao
                //             case 1:
                //             {
                //                 nextStage++;
                //                 stopPos = position_1OnlyHT[KTGlobal.GetRandomNumber(0, position_1OnlyHT.Count - 1)];
                //                 break;
                //             }
                //             // 2 sao
                //             case 2:
                //             {
                //                 nextStage += 2;
                //                 stopPos = 15;
                //                 break;
                //             }
                //             // 3 sao
                //             case 3:
                //             {
                //                 nextStage += 3;
                //                 stopPos = 6;
                //                 break;
                //             }
                //             // Toác
                //             default:
                //             {
                //                 PlayerManager.ShowNotification(player,
                //                     "Dữ liệu bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                //                 LogManager.WriteLog(LogTypes.SeashellCircle,
                //                     string.Format(
                //                         "[ERROR] Lỗi bắt đầu quay sò của RoleID {0} SML. 'switch (type) => default'",
                //                         player.RoleID));
                //                 return false;
                //             }
                //         }
                //     }
                // }
                // else
                {
                    // Kiểm tra loại quay
                    switch (type)
                    {
                        // 1 sao
                        case 1:
                        {
                            nextStage++;
                            stopPos = position_1[KTGlobal.GetRandomNumber(0, position_1.Count - 1)];
                            break;
                        }
                        // 2 sao
                        case 2:
                        {
                            nextStage += 2;
                            stopPos = position_2[KTGlobal.GetRandomNumber(0, position_2.Count - 1)];
                            break;
                        }
                        // 3 sao
                        case 3:
                        {
                            nextStage += 3;
                            stopPos = position_3[KTGlobal.GetRandomNumber(0, position_3.Count - 1)];
                            break;
                        }
                        // Toác
                        default:
                        {
                            PlayerManager.ShowNotification(player,
                                "Dữ liệu bị lỗi, hãy liên hệ với hỗ trợ để được trợ giúp!");
                            LogManager.WriteLog(LogTypes.SeashellCircle,
                                string.Format(
                                    "[ERROR] Lỗi bắt đầu quay sò của RoleID {0} SML. 'switch (type) => default'",
                                    player.RoleID));
                            return false;
                        }
                    }
                }
            }

            // Nếu không có vị trí trước đó
            if (lastStopPos == -1)
            {
                lastStopPos = 0;
            }

            // Cập nhật tổng số ô cần đi qua
            totalCells = stopPos + totalRounds * KTSeashellCircleManager.CellsPerRound - lastStopPos;

            //PlayerManager.ShowNotification(player, string.Format("LastStopPos = {0}, StopPos = {1}, TotalRound = {2}, Total cells = {3}", lastStopPos, stopPos, totalRounds, totalCells));

            // Nếu là lần đầu
            if (isFirstTurn)
            {
                // Xóa số sò tương ứng đã cược
                if (!ItemManager.RemoveItemFromBag(player, KTSeashellCircleManager.Seashells, bet, -1, "Quay Sò"))
                {
                    PlayerManager.ShowNotification(player, "Không thể xóa số vé đã cược, xin hãy thử lại!");
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        string.Format("[ERROR] Lỗi đéo xóa được {0}x sò của RoleID {1}", bet, player.RoleID));
                    return false;
                }

                // Tăng tổng số sò đã tích lũy của Server
                KTSeashellCircleManager.TotalIncomingSells += bet;
                KTSeashellCircleManager.TotalSellsRate += bet;
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    string.Format("Server tích lũy tổng số sò: {0}", KTSeashellCircleManager.TotalIncomingSells));
            }

            // Số sò hiện đang tích lũy
            long currentStorageCount = KTSeashellCircleManager.GetSystemStorageSeashells();
            // Tăng số sò đã tích lũy
            currentStorageCount += bet;
            // Ghi lại số sò hiện đang tích lũy
            KTSeashellCircleManager.SetSystemStorageSeashells(currentStorageCount);

            // Nếu quay thành công
            if (isSuccess)
            {
                // Nếu quay vào rương
                if (willGetTreasure)
                {
                    // Cập nhật số cược
                    player.LastSeashellCircleBet = bet;
                    // Cập nhật tầng dừng lại
                    player.LastSeashellCircleStage = -1;
                    // Cập nhật vị trí dừng lại
                    player.LastSeashellCircleStopPos = stopPos;
                    // Lưu thông tin vào DB
                    player.SaveSeashellCircleParamToDB();

                    // Ghi LOG
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        string.Format("{0} (ID: {1}) cược {2} sò, quay vào Rương xấu xí.", player.RoleName,
                            player.RoleID, bet));
                }
                // Nếu không quay vào rương
                else
                {
                    // Cập nhật số cược
                    player.LastSeashellCircleBet = bet;
                    // Cập nhật tầng dừng lại
                    player.LastSeashellCircleStage = nextStage;
                    // Cập nhật vị trí dừng lại
                    player.LastSeashellCircleStopPos = stopPos;
                    // Lưu thông tin vào DB
                    player.SaveSeashellCircleParamToDB();

                    // Ghi LOG
                    LogManager.WriteLog(LogTypes.SeashellCircle,
                        string.Format("{0} (ID: {1}) tầng {2}, cược {3} sò, quay vào vị trí {4}, tăng lên tầng {5}.",
                            player.RoleName, player.RoleID, currentStage, bet, stopPos, nextStage));
                }
            }
            // Nếu quay thất bại
            else
            {
                // Reset toàn bộ
                player.LastSeashellCircleStage = -1;
                player.LastSeashellCircleStopPos = -1;
                player.LastSeashellCircleBet = -1;
                // Lưu thông tin vào DB
                player.SaveSeashellCircleParamToDB();
                
                UpdateScore(player, currentStage, bet);   
                // Ghi LOG
                LogManager.WriteLog(LogTypes.SeashellCircle,
                    string.Format("{0} (ID: {1}) tầng {2}, cược {3} sò, quay vào vị trí {4}, toạch.", player.RoleName,
                        player.RoleID, currentStage, bet, stopPos));
            }

            // Trả về kết quả thành công
            return true;
        }

        #endregion Public methods

        private static void UpdateScore(KPlayer player, int stage, int bet)
        {
            var stageConfig = Data.ExchangeSeashells.FirstOrDefault(x => x.Stage == stage);
            if (stageConfig == null)
            {
                return;
            }
            var currentScore = Global.GetRoleParamsInt32FromDB(player, RoleParamName.SeashellScore);
            currentScore += stageConfig.Score * bet / 2;
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.SeashellScore, currentScore, false);
        }
    }
}