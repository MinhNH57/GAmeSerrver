using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.CopySceneEvents.MiJingFuBen
{
    /// <summary>
    /// Script phụ bản Bí Cảnh
    /// </summary>
    public static class MiJing_EventScript
    {
        public static int GetRequiredNumberOfMijingMap(KPlayer player, bool isLeader = true)
        {
            // bí cảnh đi lượt 1, 2 mất 1 bản đồ, lượt 3 mất 2 bản đồ
            var entriesToday = CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.MiJing);
            if (isLeader)
            {
                // nếu là leader thì số lượng được tính trước khi cập nhật record nên phải điều chỉnh + 1
                entriesToday += 1;
                // nếu là teammate thì check số lượng sau khi cập nhật record,
            }
            if (entriesToday == 1)
                return 1;
            if (entriesToday <= 3)
                return 2;
            return 3;
        }

        /// <summary>
        /// Kiểm tra điều kiện mở bí cảnh
        /// </summary>
        /// <param name="player"></param>
        public static string CheckCondition(KPlayer player)
        {
            // Nếu không có nhóm
            if (player.TeamID == -1 || !KTTeamManager.IsTeamExist(player.TeamID))
            {
                return "Bạn cần tạo tổ đội tối thiểu 1 thành viên!";
            }

            // Nếu không phải đội trưởng
            if (player.TeamLeader != null && player.TeamLeader != player)
            {
                return "Chỉ đội trưởng mới có thể thao tác!";
            }

            // if (KTGMCommandManager.IsGM(player)) 
            //     return "OK";

            // Nếu cấp độ không đủ
            if (player.m_Level < MiJing.RequireLevel)
            {
                return string.Format("Yêu cầu cấp độ <color=green>{0}</color> trở lên mới có thể mở Bí Cảnh.",
                    MiJing.RequireLevel);
            }

            // Nếu đã tham gia trong ngày rồi thì thôi
            if (CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.MiJing) >=
                MiJing.LimitRoundPerDay)
            {
                return "Bạn đã tham gia đủ số lượt trong ngày, không thể tham gia nữa!";
            }
            else
            {
                var required = GetRequiredNumberOfMijingMap(player);

                // Nếu không có bản đồ bí cảnh
                if (ItemManager.GetItemCountInBag(player, MiJing.MapItem) < required)
                {
                    return $"Không có đủ Bản Đồ Bí Cảnh, không thể tạo bí cảnh! Cần phải có {required} bản đồ.";
                }
            }

            // Nếu số lượng Bí cảnh đã đạt tối đa
            if (CopySceneEventManager.GetTotalCopyScene<MiJing_Script_Main>() >= MiJing.LimitCopyScenes)
            {
                return "Số lượng bí cảnh đang mở đã đạt giới hạn, hãy thử lại lúc khác!";
            }

            // Nếu số lượng phụ bản đã đạt tối đa
            if (CopySceneEventManager.CurrentCopyScenesCount >= CopySceneEventManager.LimitCopyScenes)
            {
                return "Số lượng phụ bản đã đạt giới hạn, hãy thử lại lúc khác!";
            }

            List<KPlayer> teammates = player.Teammates;

            if (teammates.Count < 1)
            {
                return "Nhóm cần tối thiểu 1 thành viên để bắt đầu!";
            }

            // Kiểm tra xem có thành viên không trong khu vực
            foreach (KPlayer teammate in teammates)
            {
                // Nếu khác bản đồ
                if (teammate.MapCode != player.MapCode)
                {
                    return "Hãy tập hợp đủ thành viên tới chỗ ta trước!";
                }
            }

            // Danh sách đội viên không đủ cấp
            List<string> notEnoughLevelPlayers = new List<string>();
            // Kiểm tra nhóm
            foreach (KPlayer teammate in teammates)
            {
                // Nếu đã tham gia rồi
                if (teammate.m_Level < MiJing.RequireLevel)
                {
                    notEnoughLevelPlayers.Add(string.Format("<color=#4dbeff>[{0}]</color>", teammate.RoleName));
                }
            }

            // Nếu tồn tại danh sách đội viên không đủ cấp
            if (notEnoughLevelPlayers.Count > 0)
            {
                return string.Format(
                    "Trong tổ đội có {0} cấp độ không đủ <color=green>{1}</color>, không thể tham gia!",
                    string.Join(", ", notEnoughLevelPlayers), MiJing.RequireLevel);
            }

            // Danh sách đội viên đã tham gia quá số lượt trong ngày
            List<string> alreadyAttempMaxRoundTodayPlayers = new List<string>();
            // Kiểm tra nhóm
            foreach (KPlayer teammate in teammates)
            {
                // Nếu đã tham gia rồi
                if (CopySceneEventManager.GetCopySceneTotalEnterTimesToday(teammate, AcitvityRecore.MiJing) >=
                    MiJing.LimitRoundPerDay)
                {
                    alreadyAttempMaxRoundTodayPlayers.Add(string.Format("<color=#4dbeff>[{0}]</color>",
                        teammate.RoleName));
                }
            }

            // Nếu tồn tại danh sách đội viên đã tham gia quá số lượt trong ngày
            if (alreadyAttempMaxRoundTodayPlayers.Count > 0)
            {
                return string.Format("Trong tổ đội có {0} đã tham gia quá số lượt trong ngày!",
                    string.Join(", ", alreadyAttempMaxRoundTodayPlayers));
            }

            // Trả ra kết quả OK
            return "OK";
        }

        /// <summary>
        /// Bắt đầu mở bí cảnh
        /// </summary>
        /// <param name="player"></param>
        public static void Begin(KPlayer player)
        {
            // Nếu lỗi gì đó
            if (player == null)
            {
                return;
            }

            var required = GetRequiredNumberOfMijingMap(player);
            // Xóa vật phẩm bản đồ bí cảnh
            if (!ItemManager.RemoveItemFromBag(player, MiJing.MapItem, required, -1, "Bí Cảnh"))
            {
                return;
            }

            List<KPlayer> teammates = player.Teammates;

            // Duyệt danh sách thành viên, đánh dấu đã tham gia ngày hôm nay
            foreach (KPlayer teammate in teammates)
            {
                int totalEnterTimes =
                    CopySceneEventManager.GetCopySceneTotalEnterTimesToday(teammate, AcitvityRecore.MiJing);
                totalEnterTimes++;
                CopySceneEventManager.SetCopySceneTotalEnterTimesToday(teammate, AcitvityRecore.MiJing,
                    totalEnterTimes);
                //SideTaskManager.getInstance().ProcessTask(teammate, (int)SideTaskName.BiCanh);

                GlobalEventSource.getInstance().fireEvent(new BiCanhEventObject(teammate));
            }

            // Cấp độ trung bình của các thành viên trong nhóm
            int totalLevel = 0;
            int totalMembers = 0;
            foreach (KPlayer teammate in teammates)
            {
                totalMembers++;
                totalLevel += teammate.m_Level;
            }

            // Cấp độ trung bình
            int nLevel = totalLevel / totalMembers;

            // Bản đồ tương ứng
            int mapID = MiJing.Map.ID;
            GameMap map = GameManager.MapMgr.GetGameMap(mapID);
            // Tạo mới phụ bản
            KTCopyScene copyScene = new KTCopyScene(map, MiJing.Duration)
            {
                AllowReconnect = true,
                EnterPosX = MiJing.Map.EnterPosX,
                EnterPosY = MiJing.Map.EnterPosY,
                Level = nLevel,
                Name = "Bí cảnh",
                OutMapCode = player.CurrentMapCode,
                OutPosX = player.PosX,
                OutPosY = player.PosY,
                ReliveHPPercent = 20,
                ReliveMPPercent = 20,
                ReliveStaminaPercent = 20,
                ReliveMapCode = MiJing.Map.ID,
                RelivePosX = MiJing.Map.EnterPosX,
                RelivePosY = MiJing.Map.EnterPosY,
            };
            MiJing_Script_Main script = new MiJing_Script_Main(copyScene);
            script.MasterRoleId = player.RoleID;
            script.Begin(teammates);
        }
    }
}