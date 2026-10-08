using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.Model;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents.BossUnicon
{
    /// <summary>
    /// Script hoạt động 
    /// </summary>
    public class BossUnicon_ActivityScript : IActivityScript
    {
        #region Define
        /// <summary>
        /// Hoạt động tương ứng
        /// </summary>
        public KTActivity Activity { get; set; }

        /// <summary>
        /// Danh sách Script đang thực thi
        /// </summary>
        private readonly ConcurrentDictionary<int, BossUnicon_Script_Main> scripts = new ConcurrentDictionary<int, BossUnicon_Script_Main>();

        /// <summary>
        /// Lưu lại thời gian của người chơi ở trong 
        /// </summary>
        private static readonly ConcurrentDictionary<int, int> playerTimes = new ConcurrentDictionary<int, int>();

        private static readonly ConcurrentDictionary<int, int> guildRegister = new ConcurrentDictionary<int, int>();

        public static int isHadAward = 0;

        public static long timeBossAppear = 0;

        public static readonly ConcurrentDictionary<int, int> mapIDHasBuff = new ConcurrentDictionary<int, int>();

        public static readonly ConcurrentDictionary<int, long> timeDurationDMC = new ConcurrentDictionary<int, long>();
        #endregion

        #region Core ActivityScript
        /// <summary>
        /// Bắt đầu sự kiện
        /// </summary>
        public void Begin()
        {
            /// Nếu không có thông tin hoạt động
            if (this.Activity == null)
            {
                return;
            }

            /// ID sự kiện
            int activityID = this.Activity.Data.ID;


            /// Nếu là mở 
            if (activityID == 900)
            {
                float durationHourPerDay = BossUnicon.Config.DurationPerDay / 3600000f;

                KTGlobal.SendSystemEventNotification("Tích Cung Liên Server đã khai mở, hãy lập tổ đội để tiêu diệt Hỏa Kỳ Lân");
                /// Làm rỗng Script
                this.scripts.Clear();
                /// Làm rỗng thời gian vào
                BossUnicon_ActivityScript.playerTimes.Clear();

                /// Duyệt danh sách tầng tương ứng
                foreach (KeyValuePair<int, BossUnicon.EventDetail.Stage> pair in BossUnicon.Event.Stages)
                {
                    /// Bản đồ tương ứng
                    GameMap map = GameManager.MapMgr.GetGameMap(pair.Key);
                    /// Nếu có bản đồ tương ứng
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        BossUnicon_Script_Main script = new BossUnicon_Script_Main(map, this.Activity);
                        /// Bắt đầu thực thi
                        script.Begin();
                        /// Thêm Script vào danh sách
                        this.scripts[script.Map.MapCode] = script;
                    }
                }
            }
            /// Nếu là thông báo xuất hiện Boss đã xuất hiện
            else if (activityID == 901)
            {
                KTGlobal.SendSystemEventNotification("Đã xuất hiện Boss Hỏa Kỳ Lân, hãy đến tiêu diệt ");
                BossUnicon_ActivityScript.timeBossAppear = KTGlobal.GetCurrentTimeMilis();
            }
            /// Nếu là kết thúc 
            else if (activityID == 902)
            {
                KTGlobal.SendSystemEventNotification("Tích Cung Liên Server đã đóng lại, hẹn quý bằng hữu lần tới!");
            }
            /// Nếu là thời gian đăng ký
            else if (activityID == 903)
            {
                KTGlobal.SendSystemEventNotification("Boss Hỏa Kỳ Lân đã mở đăng ký. Bang chủ hãy đến gặp NPC Hoàng Thường để đăng ký cho bang mình vào !");
                /// làm rỗng danh sách đăng ký
                BossUnicon_ActivityScript.guildRegister.Clear();
            }
        }

        /// <summary>
        /// Kết thúc sự kiện
        /// </summary>
        public void Close()
        {
            List<int> keys = this.scripts.Keys.ToList();
            /// Duyệt danh sách Script và thực hiện đóng lại
            foreach (int key in keys)
            {

                /// Nếu không tồn tại
                if (!this.scripts.TryGetValue(key, out BossUnicon_Script_Main script))
                {
                    continue;
                }

                /// Hủy Script
                script.Dispose();
            }
            /// Giải phóng bộ nhớ
            this.scripts.Clear();
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Trả về hệ số nhân Dạ Minh Châu của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetMultiplyNightPearl(KPlayer player)
        {
            /// Tổng tài phú
            long totalValue = player.GetTotalValue() / 10000L;
            /// Nếu dưới lăng tuyệt
            if (totalValue < 1500L)
            {
                return 0;
            }

            /// Nếu vô song
            if (totalValue >= 300000L)
            {
                return 7;
            }
            /// Nếu chí tôn
            else if (totalValue >= 120000L)
            {
                return 6;
            }
            /// Nếu tiềm long 
            else if (totalValue >= 60000L)
            {
                return 5;
            }
            /// Nếu sồ phượng
            else if (totalValue >= 24000L)
            {
                return 4;
            }
            /// Nếu hỗn thiên 
            else if (totalValue >= 12000)
            {
                return 3;
            }
            /// Nếu ngự không
            else if (totalValue >= 6000)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Trả về thời gian còn lại ở trong  trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTodayTotalSecLeft(KPlayer player)
        {
            /// Nếu không tồn tại trong danh sách
            if (!BossUnicon_ActivityScript.playerTimes.ContainsKey(player.RoleID))
            {
                /// Trả về thời gian tối đa
                return BossUnicon.Config.DurationPerDay;
            }
            /// Trả về kết quả
            return BossUnicon_ActivityScript.playerTimes[player.RoleID];
        }

        /// <summary>
        /// Thiết lập thời gian còn lại ở trong  trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="totalSecLeft"></param>
        /// <returns></returns>
        public static void SetTotayTotalSecLeft(KPlayer player, int totalSecLeft)
        {
            BossUnicon_ActivityScript.playerTimes[player.RoleID] = totalSecLeft;
        }

        /// <summary>
        /// Kiểm tra điều kiện để người chơi có thể vào 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string EnterMap_CheckCondition(KPlayer player)
        {
            /// Nếu  chưa mở
            var controlScript = GameMapEventsManager.GetActivityScript(900);
            if (controlScript == null)
            {
                return " hiện chưa mở, hãy quay lại sau!";
            }

            if (!controlScript.Activity.IsStarted)
            {
                return " hiện chưa mở, hãy quay lại sau!";
            }

            /// Nếu bang hội chưa đăng ký
            if (!BossUnicon_ActivityScript.guildRegister.ContainsKey(player.GuildID) && !BossUnicon_ActivityScript.guildRegister.ContainsKey(player.RoleID))
            {
                return "Bạn chưa đăng ký!";
            }

            /// Nếu chưa đủ cấp 90 thì bỏ
            if (player.m_Level < 90)
            {
                return "Bạn chưa đủ cấp độ. Yêu cầu cấp 90!";
            }

            /// Nếu chưa đủ tài phú thì
            if (player.GetTotalValue() / 10000L < 3000)
            {
                return "Bạn chưa đủ tài phú. Yêu cầu 3000 tài phú";
            }

            /// nếu chưa đeo phi phong sổ phượng tường
            if (player.GoodsDataList != null)
            {
                lock (player.GoodsDataList)
                {
                    var mantle =
                        player.GoodsDataList.FirstOrDefault(x =>
                            x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
                    if (mantle == null)
                    {
                        return "Phi Phong phải đạt cấp Kinh Thế";
                    }

                    var template = ItemManager.GetItemTemplate(mantle.GoodsID);

                    if (template == null)
                    {
                        return "Phi Phong phải đạt cấp Kinh Thế";
                    }

                    if (template.ItemValue < 3000)
                    {
                        return "Phi Phong phải đạt cấp Kinh Thế";
                    }
                }
            }

            /// Nếu đã hết thời gian được ở trong 
            else if (BossUnicon_ActivityScript.GetTodayTotalSecLeft(player) <= 0)
            {
                return "Ngươi đã hết thời gian ở trong  trong ngày, để đảm bảo an toàn tính mạng, hãy quay lại vào hôm sau!";
            }
            /// Nếu cấp độ không đủ
            else if (player.m_Level < BossUnicon.Config.LimitLevel)
            {
                return string.Format("Ngươi cần đạt tối thiểu cấp {0} mới có thể tiến vào!", BossUnicon.Config.LimitLevel);
            }

            /// Trả về kết quả có thể vào
            return "OK";
        }

        /// <summary>
        /// Kiểm tra điều kiện đăng ký
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string Register_CheckCondition(KPlayer player)
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;

            /// Nếu đã đăng ký trong ngày hôm nay
            if (BossUnicon_ActivityScript.guildRegister.ContainsKey(player.GuildID) || BossUnicon_ActivityScript.guildRegister.ContainsKey(player.RoleID))
            {
                return "Bạn đã đăng ký trước đó";
            }
            /// 
            if (hour < 15)
            {
                /// Nếu không phải bang chủ
                if (player.GuildRank != (int)GuildRank.Master)
                {
                    return "Chỉ bang chủ mới có thể đăng ký!";
                }
            }
            return "OK";
        }

        /// <summary>
        /// Đăng ký bang vào 
        /// </summary>
        /// <param name="player"></param>
        public static void RegisterEnterBossUnicon(KPlayer player)
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            if (hour < 15)
            {
                BossUnicon_ActivityScript.guildRegister[player.GuildID] = player.RoleID;
            }
            else
            {
                BossUnicon_ActivityScript.guildRegister[player.RoleID] = player.RoleID;
            }

        }

        /// <summary>
        /// Dịch chuyển người chơi vào 
        /// </summary>
        /// <param name="player"></param>
        public static void MoveToBossUnicon(KPlayer player)
        {
            if (player.TeamID != -1 && KTTeamManager.IsTeamExist(player.TeamID))
            {
                // ko cho vào
            }   
            else
            {
                /// Chuyển về bản đồ tương ứng
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, BossUnicon.Event.Relive.MapID, BossUnicon.Event.Relive.PosX, BossUnicon.Event.Relive.PosY, (int)player.CurrentDir);
                ///DayActivityManager.CheckMissionDone((int)ActivityDayName.TAN_LANG, player);
            }
        }

        /// <summary>
        /// Trục xuất người chơi khỏi 
        /// </summary>
        /// <param name="player"></param>
        public static void KickOut(KPlayer player)
        {
            if (GameManager.IsKuaFuServer)
            {
                //neu la lien sv thi cho ve hoang thanh
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, 1616, 7545, 4454, (int)player.CurrentDir);
            }
            else
            {
                /// Thông tin điểm về thành gần nhất
                KT_TCPHandler.GetPlayerDefaultRelivePos(player, out int mapCode, out int posX, out int posY);
                /// Chuyển về bản đồ tương ứng
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, mapCode, posX, posY, (int)player.CurrentDir);
            }
            /// thêm log khi bị kick
            int totalSecLeft = BossUnicon_ActivityScript.GetTodayTotalSecLeft(player);
            LogManager.WriteLog(LogTypes.BossUnicon, $"Người chơi: {player.RoleName} bị kich khỏi , thời gian còn lại là: {totalSecLeft}");
        }

        /// <summary>
        /// Thực hiện Tick liên tục khi người chơi ở trong 
        /// </summary>
        /// <param name="player"></param>
        public static void ProcessPlayerTick(KPlayer player)
        {
            /// Script điều khiển
            BossUnicon_ActivityScript activityScript = GameMapEventsManager.GetActivityScript<BossUnicon_ActivityScript>(900);
            /// Nếu  chưa mở
            if (activityScript == null)
            {
                return;
            }

            /// Thời gian còn lại
            int totalSecLeft = BossUnicon_ActivityScript.GetTodayTotalSecLeft(player);
            /// Nếu đã hết thời gian
            if (totalSecLeft <= 0 || player.GuildID <= 0)
            {
                /// Nếu đang đợi chuyển Map thì thôi
                if (player.WaitingForChangeMap)
                {
                    return;
                }

                if (player.GuildID <= 0)
                {
                    LogManager.WriteLog(LogTypes.BossUnicon, $"Player: {player.RoleName}, roleID={player.RoleID} bị kích vì bang không phù hợp");
                }

                /// Trục xuất người chơi ra khỏi bản đồ
                BossUnicon_ActivityScript.KickOut(player);

                /// Bỏ qua
                return;
            }

            /// Giảm thời gian còn lại
            totalSecLeft -= 500;
            /// Nếu dưới 0
            if (totalSecLeft < 0)
            {
                totalSecLeft = 0;
            }
            /// Lưu lại kết quả
            BossUnicon_ActivityScript.SetTotayTotalSecLeft(player, totalSecLeft);

            /// Nếu tồn tại Script
            if (activityScript.scripts.TryGetValue(player.CurrentMapCode, out BossUnicon_Script_Main script))
            {
                /// Thực thi sự kiện Tick người chơi tương ứng
                script.OnPlayerTick(player);
            }

            /// Nếu đã hết thời gian
            if (totalSecLeft == 0)
            {
                /// Nếu đang đợi chuyển Map thì thôi
                if (player.WaitingForChangeMap)
                {
                    return;
                }

                /// Trục xuất người chơi ra khỏi bản đồ
                BossUnicon_ActivityScript.KickOut(player);
            }
        }
        #endregion
    }
}
