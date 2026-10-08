using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using static GameServer.KiemThe.CopySceneEvents.YouLongGe.YouLong.AwardInfo;


namespace GameServer.KiemThe.GameEvents.FlowerBet
{
    public static class FlowerBetManager
    {
        private const int BET_AMOUNT_1 = 200;
        private const int BET_AMOUNT_2 = 1000;
        private const int BET_AMOUNT_3 = 5000;

        public enum Stage
        {
            Ended = 0,
            Started = 1,
            Reward = 2,
        }

        private static Dictionary<int, string> dictFlower = new Dictionary<int, string>()
        {
            //{ 0, "Toán" },
            //{ 1, "Lý" },
            //{ 2, "Hóa" },
            //{ 3, "Văn" },
            //{ 4, "Sử" },
            //{ 5, "Địa" },
            //{ 6, "Tiếng Anh" },
            //{ 7, "Giáo dục công dân" },
            { 0, "Socola" },
            { 1, "Kem Dâu" },
        };

        private static Stage eventStage = Stage.Ended;

        // private static List<PlayerBetData> ParticipatePlayers = new List<PlayerBetData>();
        // private static List<int> SelectedFlowers = new List<int>();
        private static int BloomedFlowerID = 0;
        private static DateTime chooseEndTime = DateTime.MinValue;
        private static DateTime rewardEndTime = DateTime.MinValue;
        private static FlowerBetData currentStatus;

        /// <summary>
        /// Thời gian mặc định của vòng dự đoán khi người chơi mở giao diện
        /// trong lúc bộ lập lịch chưa kích hoạt FlowerBet.
        /// </summary>
        private const int FallbackChooseDurationSeconds = 15 * 60;

        private class FlowerBetData
        {
            public int flower_1;
            public int flower_2;
            public int count_1;
            public int count_2;
            public int total_1;
            public int total_2;
            public int winner;
            public int round_id;
        }

        private static FlowerBetData GetKuaFuFlowerBetStatus()
        {
            try
            {
                var data = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.FLOWER_BET, true);
                var fields = data.Split('_');
                if (fields.Length < 7)
                {
                    return null;
                }

                var r = new FlowerBetData
                {
                    flower_1 = int.Parse(fields[0]),
                    flower_2 = int.Parse(fields[1]),
                    count_1 = int.Parse(fields[2]),
                    count_2 = int.Parse(fields[3]),
                    total_1 = int.Parse(fields[4]),
                    total_2 = int.Parse(fields[5]),
                    winner = int.Parse(fields[6]),
                    round_id = int.Parse(fields[7])
                };
                return r;
            }
            catch
            {
                return null;
            }
        }

        public static void StartFlowerBet(long duration)
        {
            ClearData();

            eventStage = Stage.Started;
            chooseEndTime = GetActivityEndTime(duration);

            currentStatus = GetKuaFuFlowerBetStatus();
            if (currentStatus == null)
                return;
            // SelectedFlowers.Add(currentStatus.flower_1);
            // SelectedFlowers.Add(currentStatus.flower_2);
            // StaticRandom.Shuffle(SelectedFlowers);

            // Random random = new Random();
            // while (SelectedFlowers.Count < 2)
            // {
            //     int randomFlower = (int)random.Next(0, dictFlower.Count);
            //     if (!SelectedFlowers.Contains(randomFlower))
            //     {
            //         SelectedFlowers.Add(randomFlower);
            //     }
            // }

            KTGlobal.SendSystemEventNotification(string.Format(
                "Kiếm Vương 1 cắt bánh sinh nhật mời anh hùng hào kiệt khắp chốn giang hồ. " +
                "Hãy đoán xem chúng ta sẽ được thưởng thức bánh {0} hay bánh {1}. Thời gian dự đoán là 15 phút!",
                dictFlower[currentStatus.flower_1],
                dictFlower[currentStatus.flower_2]));

            LogManager.WriteLog(LogTypes.FlowerBet,
                string.Format("[FLOWERBET] Bắt đầu hoạt động đoán hoa. {0} : [{1}] và {2} : [{3}]",
                    dictFlower[currentStatus.flower_1], currentStatus.flower_1, dictFlower[currentStatus.flower_2],
                    currentStatus.flower_2));
        }

        public static void WaitForResult()
        {
        }

        public static void RewardFlowerBet(long duration)
        {
            eventStage = Stage.Reward;
            rewardEndTime = GetActivityEndTime(duration);

            currentStatus = GetKuaFuFlowerBetStatus();
            var totalParticipated = currentStatus.count_1 + currentStatus.count_2;
            if (totalParticipated < 2)
            {
                BloomedFlowerID = 0;
                KTGlobal.SendSystemEventNotification(
                    "Kiếm Vương 1 chưa thể cắt bánh do không đủ người chơi tham gia." +
                    "Mời anh hùng hào kiệt vào giao diện nhận hoàn trả. Thời gian hoàn trả kéo dài 60 phút!");

                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET] Bắt đầu hoạt động nhận thưởng. Không đủ người tham gia. ParticipatePlayers Count = {totalParticipated}");
            }
            else
            {
                var totalFlower1Bet = currentStatus.total_1;
                var totalFlower2Bet = currentStatus.total_2;

                // foreach (var player in ParticipatePlayers)
                // {
                //     if (player.selectedFlower == SelectedFlowers[0])
                //         totalFlower1Bet += player.betAmount;
                //
                //     else if (player.selectedFlower == SelectedFlowers[1])
                //         totalFlower2Bet += player.betAmount;
                // }

                // if (totalFlower1Bet == totalFlower2Bet)
                // {
                //     //Ngau nhien
                //     Random random = new Random();
                //     BloomedFlowerID = (int)SelectedFlowers[random.Next(0, 2)];
                // }
                // else
                // {
                //     //Du doan it hon se thang
                //     BloomedFlowerID = currentStatus.winner;
                // }

                BloomedFlowerID = currentStatus.winner;


                KTGlobal.SendSystemEventNotification(string.Format(
                    "Kiếm Vương 1 cắt Bánh {0} chiêu đãi anh hùng hào kiệt. " +
                    "Người chơi dự đoán đúng hãy mau vào giao diện và nhận thưởng. Thời gian nhận thưởng kéo dài 60 phút!",
                    dictFlower[BloomedFlowerID]));

                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET] Bắt đầu hoạt động nhận thưởng. Picked flower = {dictFlower[BloomedFlowerID]} : {BloomedFlowerID}. TotalFlower1Bet = {totalFlower1Bet}. TotalFlower2Bet = {totalFlower2Bet}. ParticipatePlayers Count = {totalParticipated}");
            }
        }

        public static void EndFlowerBet()
        {
            eventStage = Stage.Ended;

            ClearData();

            KTGlobal.SendSystemEventNotification(
                $"Hoạt động CẮT BÁNH SINH NHẬT đã kết thúc. Cảm ơn các anh hùng hào kiệt đã tham gia.");

            LogManager.WriteLog(LogTypes.FlowerBet, "[FLOWERBET] Kết thúc hoạt động đoán hoa.");
        }

        public static string GiveOutReward(KPlayer player, TCPClientPool tcpClientPool)
        {
            string messageToPlayer = string.Format("{0}:{1}", "Thông báo", $"Đã nhận thưởng");

            var playerBetStatus = GetPlayerBetData(player);

            var totalParticipated = currentStatus.count_1 + currentStatus.count_2;
            if (totalParticipated <= 1)
            {
                int reward = playerBetStatus.betAmount;
                KTGlobal.AddMoney(player, reward, Entities.MoneyType.Dong);
                messageToPlayer = string.Format("{0}:{1}", "Thông báo", $"Hoàn trả {reward} KNB");

                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET]{player.RoleName} [{player.RoleID}] nhận hoàn trả = {reward} KNB.");
            }

            else
            {
                if (playerBetStatus.selectedFlower == BloomedFlowerID)
                {
                    int totalCorrectBet;
                    int totalIncorrectBet;

                    if (currentStatus.winner == currentStatus.flower_1)
                    {
                        totalCorrectBet = currentStatus.total_1;
                        totalIncorrectBet = currentStatus.total_2;
                    }
                    else
                    {
                        totalCorrectBet = currentStatus.total_2;
                        totalIncorrectBet = currentStatus.total_1;
                    }

                    // foreach (var data in ParticipatePlayers)
                    // {
                    //     if (data.selectedFlower == BloomedFlowerID)
                    //         totalCorrectBet += data.betAmount;
                    //     else 
                    //         totalIncorrectBet += data.betAmount;
                    // }

                    int betReward = (int)(((float)playerBetStatus.betAmount / totalCorrectBet) * totalIncorrectBet *
                                          0.7f);
                    int reward = playerBetStatus.betAmount + betReward;

                    KTGlobal.AddMoney(player, reward, Entities.MoneyType.Dong);

                    messageToPlayer = string.Format("{0}:{1}", "Chúc mừng", $"Nhận được {reward} KNB");

                    LogManager.WriteLog(LogTypes.FlowerBet,
                        $"[FLOWERBET]{player.RoleName} [{player.RoleID}] nhận phần thưởng thắng = {reward} KNB. ");
                }
                else
                    messageToPlayer = GiveConsolationReward(player, playerBetStatus.betAmount, tcpClientPool);
            }

            playerBetStatus.receiveReward = 1;
            UpdatePlayerBetData(player, playerBetStatus);

            return messageToPlayer;
        }

        public static string PlaceBet(KPlayer player, int flowerID, int betAmount)
        {
            int isReload = 0;
            string messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo", "Đã có lỗi xảy ra", isReload.ToString());
            if (eventStage != Stage.Started)
            {
                messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo",
                    "Không phải thời gian dự đoán! Vui lòng chờ kết quả được cập nhật hoặc vòng chơi tiếp theo!",
                    isReload.ToString("[PlaceBet] Không phải thời gian dự đoán: "));
                Console.WriteLine($"[PlaceBet] Không phải thời gian dự đoán: currentStatusNull={currentStatus == null} ");
                return messageToPlayer;
            }

            if (DateTime.Now > chooseEndTime)
            {
                messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo",
                    "Không phải thời gian dự đoán! Vui lòng chờ kết quả được cập nhật hoặc vòng chơi tiếp theo!",
                    isReload.ToString());
                Console.WriteLine("[PlaceBet] Không phải thời gian dự đoán: chooseEndTime");

                return messageToPlayer;
            }

            var playerBetStatus = GetPlayerBetData(player);
            if (playerBetStatus == null)
            {
                if (player != null && KTGlobal.GetMoneyhave(player, Entities.MoneyType.Dong) >= betAmount)
                {
                    KTGlobal.AddMoney(player, -betAmount, Entities.MoneyType.Dong);
                    playerBetStatus = new PlayerBetData
                    {
                        roleId = player.RoleID,
                        selectedFlower = flowerID,
                        betAmount = betAmount,
                        receiveReward = 0,
                        roundId = currentStatus.round_id
                    };

                    UpdatePlayerBetData(player, playerBetStatus);

                    isReload = 1;

                    messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo", "Dự đoán thành công",
                        isReload.ToString());

                    LogManager.WriteLog(LogTypes.FlowerBet,
                        $"[FLOWERBET]{player.RoleName} [{player.RoleID}] dự đoán môn học {dictFlower[flowerID]} với mức cược {betAmount} thành công.");

                    // FlowerTowerManager.UpdateTaskProgress(player, 14, 1);
                }

                else
                {
                    isReload = 0;

                    messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo", "Tiền dự đoán không đủ",
                        isReload.ToString());

                    LogManager.WriteLog(LogTypes.FlowerBet,
                        $"[FLOWERBET]{player.RoleName} [{player.RoleID}] dự đoán môn học {dictFlower[flowerID]} với mức cược {betAmount} thất bại. Tiền dự đoán không đủ.");
                }
            }

            else
            {
                isReload = 1;

                messageToPlayer = string.Format("{0}:{1}:{2}", "Thông báo",
                    $"Bạn đã dự đoán môn {dictFlower[playerBetStatus.selectedFlower]} với mức cược {playerBetStatus.betAmount} trước đó. Không thể dự đoán thêm.",
                    isReload.ToString());

                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET]{player.RoleName} [{player.RoleID}] cược hoa {dictFlower[flowerID]} với mức cược {betAmount} thất bại. Đã đặt cược trước đó.");
            }

            return messageToPlayer;
        }

        public static string GetPlayerBetStatus(KPlayer player)
        {
            int remainingSeconds = 0;

            //flower1:flower2:betAmount:selectedFlower:eventstage:isReciveReward:remainingSeconds:BloomedFlowerID
            // Giao diện "Kiểm Tra Bài Cũ" cần dữ liệu thật từ Server để hiển thị.
            // Nếu scheduler chưa mở vòng chơi, thử nạp trạng thái liên server và
            // chủ động mở một vòng dự đoán 15 phút thay vì trả eventStage = 0.
            if (eventStage == Stage.Ended || currentStatus == null)
            {
                currentStatus = GetKuaFuFlowerBetStatus();
                if (currentStatus == null)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        "[FLOWERBET] Không thể mở vòng dự đoán khi Client hỏi trạng thái: dữ liệu FLOWER_BET trong DB không tồn tại hoặc không hợp lệ.");
                    return $"0:0:0:0:{(int)Stage.Started}:1:{remainingSeconds}:{BloomedFlowerID}";
                }

                eventStage = Stage.Started;
                BloomedFlowerID = 0;
                chooseEndTime = DateTime.Now.AddSeconds(FallbackChooseDurationSeconds);
                rewardEndTime = DateTime.MinValue;

                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET] Tự mở vòng dự đoán {FallbackChooseDurationSeconds} giây khi người chơi yêu cầu trạng thái.");
            }

            if (eventStage == Stage.Started)
                remainingSeconds = (int)(chooseEndTime - DateTime.Now).TotalSeconds;
            else if (eventStage == Stage.Reward)
                remainingSeconds = (int)(rewardEndTime - DateTime.Now).TotalSeconds;

            int playerBetAmount = 0;
            int playerSelectedFlowerID = 0;
            int isReceivedReward = 1;

            var playerBetStatus = GetPlayerBetData(player);
            if (playerBetStatus != null)
            {
                playerBetAmount = playerBetStatus.betAmount;
                playerSelectedFlowerID = playerBetStatus.selectedFlower;
                isReceivedReward = playerBetStatus.receiveReward;
            }

            return
                $"{currentStatus.flower_1}:{currentStatus.flower_2}:{playerBetAmount}:{playerSelectedFlowerID}:{(int)eventStage}:{isReceivedReward}:{remainingSeconds}:{BloomedFlowerID}";
        }

        private static string GiveConsolationReward(KPlayer player, int betAmount, TCPClientPool tcpClientPool)
        {
            var TimeUsing = Global.ConstGoodsEndTime;
            var chest1ID = 20729;
            var chest2ID = 20730;
            var chest3ID = 20731;

            string messageToPlayer = "";

            if (betAmount == BET_AMOUNT_1)
            {
                var isReceivedAward = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                    chest1ID, 1, 0, "FLOWER_BET", true, 1, false, TimeUsing);

                messageToPlayer = string.Format("{0}:{1}", "Chúc mừng", $"Nhận được {KTGlobal.GetItemName(chest1ID)}");
                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET]{player.RoleName} [{player.RoleID}] nhận phần thưởng thua = randomboxID [{chest1ID}].");
            }
            else if (betAmount == BET_AMOUNT_2)
            {
                var isReceivedAward = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                    chest2ID, 1, 0, "FLOWER_BET", true, 1, false, TimeUsing);

                messageToPlayer = string.Format("{0}:{1}", "Chúc mừng", $"Nhận được {KTGlobal.GetItemName(chest2ID)}");
                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET]{player.RoleName} [{player.RoleID}] nhận phần thưởng thua = randomboxID [{chest2ID}].");
            }
            else if (betAmount == BET_AMOUNT_3)
            {
                var isReceivedAward = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                    chest3ID, 1, 0, "FLOWER_BET", true, 1, false, TimeUsing);

                messageToPlayer = string.Format("{0}:{1}", "Chúc mừng", $"Nhận được {KTGlobal.GetItemName(chest3ID)}");
                LogManager.WriteLog(LogTypes.FlowerBet,
                    $"[FLOWERBET]{player.RoleName} [{player.RoleID}] nhận phần thưởng thua = randomboxID [{chest3ID}].");
            }

            return messageToPlayer;
        }

        private static void ClearData()
        {
            eventStage = Stage.Ended;
            // ParticipatePlayers = new List<PlayerBetData>();
            // SelectedFlowers = new List<int>();
            BloomedFlowerID = 0;
            chooseEndTime = DateTime.MinValue;
            rewardEndTime = DateTime.MinValue;

            LogManager.WriteLog(LogTypes.FlowerBet, "[FLOWERBET] Làm mới dữ liệu.");
        }

        private static PlayerBetData GetPlayerBetData(KPlayer player)
        {
            var flowerBetPlayerRaw = Global.GetRoleParamByName(player, RoleParamName.FlowerBet);
            if (string.IsNullOrEmpty(flowerBetPlayerRaw))
                return null;

            var flowerBetPlayerStatus = flowerBetPlayerRaw.Split('_').Select(int.Parse).ToList();
            if (flowerBetPlayerStatus == null || flowerBetPlayerStatus.Count != 4)
                return null;

            if (flowerBetPlayerStatus[3] != currentStatus.round_id)
            {
                // ko phải round hiện tại
                return null;
            }

            return new PlayerBetData
            {
                roleId = player.RoleID,
                selectedFlower = flowerBetPlayerStatus[0],
                betAmount = flowerBetPlayerStatus[1],
                receiveReward = flowerBetPlayerStatus[2],
                roundId = flowerBetPlayerStatus[3]
            };
        }

        private static void UpdatePlayerBetData(KPlayer player, PlayerBetData betData)
        {
            Global.SaveRoleParamsStringToDB(player, RoleParamName.FlowerBet,
                $"{betData.selectedFlower}_{betData.betAmount}_{betData.receiveReward}_{betData.roundId}", true);
        }

        private static DateTime GetActivityEndTime(long duration)
        {
            int ticks = 0;

            DateTime currentTime = DateTime.Now;
            DateTime endTime = currentTime.AddSeconds(duration / 1000);

            return endTime;
        }
    }

    public class PlayerBetData
    {
        public int roleId { get; set; }
        public int selectedFlower { get; set; }
        public int betAmount { get; set; }
        public int receiveReward { get; set; }
        public int roundId { get; set; }
    }
}
