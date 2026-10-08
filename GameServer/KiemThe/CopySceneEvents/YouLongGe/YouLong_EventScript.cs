using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Generic;

namespace GameServer.KiemThe.CopySceneEvents.YouLongGe
{
    /// <summary>
    /// Script phụ bản Du Long Các
    /// </summary>
    public static class YouLong_EventScript
    {

        public static int GetDailyLimitYouLong(KPlayer player)
        {
            var usedLenhBai = player.GetValueOfDailyRecore((int)RecoreDef.LenhBaiDuLong);
            if (usedLenhBai < 0)
            {
                usedLenhBai = 0;
            }
            return YouLong.Data.LimitRoundPerDay + usedLenhBai * 100;
        }
        /// <summary>
        /// Kiểm tra điều kiện mở phụ bản Du Long Các
        /// </summary>
        /// <param name="player"></param>
        public static string CheckCondition(KPlayer player)
        {
            
            // Nếu cấp độ không đủ
            if (player.m_Level < YouLong.Data.RequireLevel)
            {
                return
                    $"Yêu cầu cấp độ <color=green>{YouLong.Data.RequireLevel}</color> trở lên mới có thể mở Du Long Các.";
            }
            // Nếu không có Chiến Thư
            if (ItemManager.GetItemCountInBag(player, YouLong.Data.RequireItem) <= 0)
            {
                return "Không có Chiến Thư, không thể vào Du Long Các!";
            }
            // Nếu đã tham gia trong ngày rồi thì thôi
            var todayChallengeTimes =
                CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong);
            if (todayChallengeTimes >= GetDailyLimitYouLong(player))
            {
                return $"Ngươi đã tham gia đủ {todayChallengeTimes} lượt trong ngày, không thể tham gia nữa!";
            }
            // Nếu số lượng phụ bản đã đạt tối đa
            else if (CopySceneEventManager.CurrentCopyScenesCount >= CopySceneEventManager.LimitCopyScenes)
            {
                return "Số lượng phụ bản đã đạt giới hạn, hãy thử lại lúc khác!";
            }

            // Trả ra kết quả OK
            return "OK";
        }

        /// <summary>
        /// Bắt đầu phụ bản Du Long Các
        /// </summary>
        /// <param name="player"></param>
        public static void Begin(KPlayer player)
        {
            // Nếu lỗi gì đó
            if (player == null)
            {
                return;
            }

            // Xóa vật phẩm Chiến Thư Du Long
            // if (!ItemManager.RemoveItemFromBag(player, YouLong.Data.RequireItem, 1,-1,"YouLong"))
            // {
            // 	return;
            // }

            // Cấp độ phụ bản
            var level = player.m_Level;

            // Bản đồ tương ứng
            var mapID = YouLong.Data.Map.ID;
            var map = GameManager.MapMgr.GetGameMap(mapID);
            // Tạo mới phụ bản
            var copyScene = new KTCopyScene(map, YouLong.Data.Duration)
            {
                AllowReconnect = false,
                EnterPosX = YouLong.Data.Map.EnterPosX,
                EnterPosY = YouLong.Data.Map.EnterPosY,
                Level = level,
                Name = "Du Long Các",
                OutMapCode = player.CurrentMapCode,
                OutPosX = player.PosX,
                OutPosY = player.PosY,
                ReliveHPPercent = 100,
                ReliveMPPercent = 100,
                ReliveStaminaPercent = 100,
                ReliveMapCode = player.CurrentMapCode,
                RelivePosX = player.PosX,
                RelivePosY = player.PosY,
            };
            // Bắt đầu phụ bản
            var script = new YouLong_Script_Main(copyScene);
            script.Begin(new List<KPlayer>() { player });
            //DayActivityManager.CheckMissionDone((int)ActivityDayName.DU_LONG_CHIEN_THU, player, 1);
        }
    }
}