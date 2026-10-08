using GameServer.KiemThe.Core.Item;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace GameServer.KiemThe.GameEvents.ThienPhuc
{
    public class ThienPhucManager
    {
        private const string ConfigPath = "Config/KT_Activity/KTThienPhuc.xml";

        public static ThienPhucManager Instance { get; }
            = new ThienPhucManager();

        private ThienPhucConfig config;
        private readonly Random random = new Random();

        private ThienPhucManager()
        {
        }

        public void Init()
        {
            config = LoadConfig();
        }

        public void SendState(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            // Tính trạng thái từ config hiện tại.
            ThienPhucInfo info = GetPlayerInfo(player);

            // Chuyển đối tượng thành dữ liệu gửi qua mạng.
            byte[] data = DataHelper.ObjectToBytes(info);

            // Chủ động gửi cho người chơi, không cần chờ ThienPhuc:Show.
            GameManager.ClientMgr.SendToClient(
                player,
                data,
                (int)TCPGameServerCmds.CMD_KT_G2C_THIENPHUC_STATE
            );
        }

        private ThienPhucConfig LoadConfig()
        {
            string absolutePath = Global.GameResPath(ConfigPath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    $"Không tìm thấy cấu hình Thiên Phúc: {absolutePath}",
                    absolutePath
                );
            }

            ThienPhucFileConfig fileConfig;
            XmlSerializer serializer = new XmlSerializer(typeof(ThienPhucFileConfig));

            using (FileStream stream = File.OpenRead(absolutePath))
            {
                fileConfig = serializer.Deserialize(stream) as ThienPhucFileConfig;
            }

            if (fileConfig?.Config == null)
            {
                throw new InvalidDataException("Cấu hình Thiên Phúc không hợp lệ hoặc thiếu node <Config>.");
            }

            ThienPhucConfig loadedConfig = fileConfig.Config;
            loadedConfig.Rewards = fileConfig.Rewards;

            ValidateConfig(loadedConfig);

            return loadedConfig;
        }

        private static void ValidateConfig(ThienPhucConfig loadedConfig)
        {
            if (loadedConfig.StartTime >= loadedConfig.EndTime)
            {
                throw new InvalidDataException("StartTime phải nhỏ hơn EndTime.");
            }

            if (loadedConfig.MinNumber > loadedConfig.MaxNumber)
            {
                throw new InvalidDataException("MinNumber phải nhỏ hơn hoặc bằng MaxNumber.");
            }

            if (loadedConfig.DailyLimit <= 0)
            {
                throw new InvalidDataException("DailyLimit phải lớn hơn 0.");
            }

            if (loadedConfig.RequiredItemID <= 0 || loadedConfig.RequiredItemCount <= 0)
            {
                throw new InvalidDataException("Vật phẩm yêu cầu và số lượng phải lớn hơn 0.");
            }

            if (loadedConfig.Rewards == null || loadedConfig.Rewards.Count == 0)
            {
                throw new InvalidDataException("Thiên Phúc phải có ít nhất một phần thưởng.");
            }

            foreach (ThienPhucReward reward in loadedConfig.Rewards)
            {
                if (reward.ItemID <= 0 || reward.Quantity <= 0)
                {
                    throw new InvalidDataException("ItemID và Quantity của phần thưởng phải lớn hơn 0.");
                }

                if (reward.Binding != 0 && reward.Binding != 1)
                {
                    throw new InvalidDataException("Binding của phần thưởng chỉ được là 0 hoặc 1.");
                }
            }
        }

        private void EnsureConfigLoaded()
        {
            if (config == null)
            {
                config = LoadConfig();
            }
        }

        public ThienPhucInfo GetPlayerInfo(KPlayer player)
        {
            EnsureConfigLoaded();

            DateTime now = DateTime.Now;
            return new ThienPhucInfo
            {
                IsActive = config.Activate && now >= config.StartTime && now <= config.EndTime,
                EventName = config.Name,
                MinNumber = config.MinNumber,
                MaxNumber = config.MaxNumber,
                PlayedToday = 0,
                DailyLimit = config.DailyLimit,
                RequiredItemID = config.RequiredItemID,
                RequiredItemCount = config.RequiredItemCount,
            };
        }

        public ThienPhucSpinResult Spin(KPlayer player, int guessedNumber)
        {
            EnsureConfigLoaded();

            ThienPhucSpinResult result = new ThienPhucSpinResult
            {
                Success = false,
                GuessedNumber = guessedNumber,
                ResultNumber = -1,
                IsWinner = false,
                RewardItemID = 0,
                RewardQuantity = 0,
                DailyLimit = config.DailyLimit
            };

            if (player == null)
            {
                result.Message = "Không tìm thấy người chơi.";
                return result;
            }

            DateTime now = DateTime.Now;

            if (!config.Activate ||
                now < config.StartTime ||
                now > config.EndTime)
            {
                result.Message = "Sự kiện Thiên Phúc hiện chưa mở.";
                return result;
            }

            if (player.m_Level < config.MinLevel)
            {
                result.Message =
                    $"Cần đạt cấp {config.MinLevel} để tham gia.";

                return result;
            }

            if (guessedNumber < config.MinNumber ||
                guessedNumber > config.MaxNumber)
            {
                result.Message =
                    $"Số dự đoán phải từ {config.MinNumber} đến {config.MaxNumber}.";

                return result;
            }

            //int ticketCount = ItemManager.GetItemCountInBag(
            //    player,
            //    config.RequiredItemID
            //);

            //if (ticketCount < config.RequiredItemCount)
            //{
            //    result.Message =
            //        $"Không đủ vé, cần {config.RequiredItemCount} vật phẩm.";

            //    return result;
            //}

            /*
             * Nếu thắng chắc chắn nhận vật phẩm thì cần kiểm tra túi trước.
             * Cách đơn giản nhất là yêu cầu còn ít nhất một ô trống.
             */
            if (!KTGlobal.IsHaveSpace(1, player))
            {
                result.Message = "Túi đồ không đủ chỗ.";
                return result;
            }

            //if (!ItemManager.RemoveItemFromBag(
            //    player,
            //    config.RequiredItemID,
            //    config.RequiredItemCount))
            //{
            //    result.Message = "Không thể trừ vé, vui lòng thử lại.";
            //    return result;
            //}

            int resultNumber;

            lock (random)
            {
                resultNumber = random.Next(
                    config.MinNumber,
                    config.MaxNumber + 1
                );
            }

            result.ResultNumber = resultNumber;
            result.IsWinner = guessedNumber == resultNumber;

            /*
             * Tăng lượt ngay sau khi đã trừ vé.
             * Hàm này phải lưu xuống RoleParam hoặc DB.
             */

            if (!result.IsWinner)
            {
                result.Success = true;
                result.Message =
                    $"Kết quả là {resultNumber}. Chúc bạn may mắn lần sau!";

                return result;
            }

            /*
             * Hiện tại chọn ngẫu nhiên một phần thưởng.
             * Nếu XML sau này có Rate thì phải random theo trọng số Rate.
             */
            ThienPhucReward reward;

            lock (random)
            {
                reward = config.Rewards[
                    random.Next(0, config.Rewards.Count)
                ];
            }

            bool added = ItemManager.CreateItem(
                Global._TCPManager.TcpOutPacketPool,
                player,
                reward.ItemID,
                reward.Quantity,
                0,
                "THIEN_PHUC",
                false,
                reward.Binding,
                false,
                Global.ConstGoodsEndTime,
                "",
                0,
                "",
                0,
                0,
                true
            );

            if (!added)
            {
                /*
                 * Vé đã bị trừ nhưng trao thưởng thất bại.
                 * Trong bản hoàn thiện nên:
                 * - gửi thưởng qua thư; hoặc
                 * - hoàn lại vé và lượt chơi.
                 */
                result.Message =
                    "Đã quay trúng nhưng trao thưởng thất bại. Hãy liên hệ quản trị viên.";

                LogManager.WriteLog(
                    LogTypes.Error,
                    $"[ThienPhuc] Trao thưởng thất bại. " +
                    $"RoleID={player.RoleID}, ItemID={reward.ItemID}, " +
                    $"Quantity={reward.Quantity}"
                );

                return result;
            }

            result.Success = true;
            result.RewardItemID = reward.ItemID;
            result.RewardQuantity = reward.Quantity;
            result.Message =
                $"Chúc mừng! Số may mắn là {resultNumber}, bạn đã đoán chính xác.";

            LogManager.WriteLog(
                LogTypes.Analysis,
                $"[ThienPhuc] Trao thưởng thành công. " +
                $"RoleID={player.RoleID}, ItemID={reward.ItemID}, " +
                $"Quantity={reward.Quantity}, Binding={reward.Binding}"
            );

            return result;
        }
    }
}
