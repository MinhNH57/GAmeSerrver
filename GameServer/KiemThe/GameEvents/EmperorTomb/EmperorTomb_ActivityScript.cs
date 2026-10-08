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
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.KiemThe.GameEvents.EmperorTomb
{
    ///
    /// <summary>
    /// Script hoạt động Tần Lăng
    /// </summary>
    public class EmperorTomb_ActivityScript : IActivityScript
    {
        #region Define
        /// <summary>
        /// Hoạt động tương ứng
        /// </summary>
        public KTActivity Activity { get; set; }

        /// <summary>
        /// Danh sách Script đang thực thi
        /// </summary>
        private readonly ConcurrentDictionary<int, EmperorTomb_Script_Main> scripts = new ConcurrentDictionary<int, EmperorTomb_Script_Main>();

        /// <summary>
        /// Lưu lại thời gian của người chơi ở trong Tần Lăng
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


            /// Nếu là mở Tần Lăng
            if (activityID == 400)
            {
                float durationHourPerDay = EmperorTomb.Config.DurationPerDay / 3600000f;

                //TODO: viết lại phần tính toán startTime và endTime theo config
                var startTime = "7:00";
                var endTime = "23:59";
                if (GameManager.IsKuaFuServer)
                {
                    startTime = "17:00";
                    endTime = "23:59";
                } else if (ServerConfig.Instance.KuaFuServerEnable)
                {
                    startTime = "7:00";
                    endTime = "16:59";
                }
                
                KTGlobal.SendSystemEventNotification(string.Format("Tần Lăng đã khai mở. Trong thời gian từ {0} đến {1}, anh hùng hào kiệt cấp {2} trở lên có thể thông qua Lương Tiếu Tiếu ở Hoàng Thành Liên Server tiến vào. Lưu ý mỗi ngày chỉ được vào tối đa {3} giờ.", startTime, endTime, EmperorTomb.Config.LimitLevel, Utils.Truncate(durationHourPerDay, 1)));
                /// Làm rỗng Script
                this.scripts.Clear();
                /// Làm rỗng thời gian vào
                EmperorTomb_ActivityScript.playerTimes.Clear();

                /// Duyệt danh sách tầng tương ứng
                foreach (KeyValuePair<int, EmperorTomb.EventDetail.Stage> pair in EmperorTomb.Event.Stages)
                {
                    /// Bản đồ tương ứng
                    GameMap map = GameManager.MapMgr.GetGameMap(pair.Key);
                    /// Nếu có bản đồ tương ứng
                    if (map != null)
                    {
                        /// Khởi tạo Script tương ứng
                        EmperorTomb_Script_Main script = new EmperorTomb_Script_Main(map, this.Activity);
                        /// Bắt đầu thực thi
                        script.Begin();
                        /// Thêm Script vào danh sách
                        this.scripts[script.Map.MapCode] = script;
                    }
                }
            }
            /// Nếu là thông báo xuất hiện Tần Thủy Hoàng
            else if (activityID == 401)
            {
                KTGlobal.SendSystemEventNotification("Tần Lăng đã mở tầng 3, thời gian tồn tại 1 tiếng 30 phút!");
                EmperorTomb_ActivityScript.timeBossAppear = KTGlobal.GetCurrentTimeMilis();
            }
            /// Nếu là kết thúc Tần Lăng
            else if (activityID == 402)
            {
                KTGlobal.SendSystemEventNotification("Tần Lăng đã đóng lại, hẹn quý bằng hữu lần tới!");
            }
            /// Nếu là thời gian đăng ký
            else if (activityID == 403)
            {
                KTGlobal.SendSystemEventNotification("Tần Lăng đã mở đăng ký. Bang chủ hãy đến gặp NPC Lương Tiếu Tiếu để đăng ký cho bang mình vào tần lăng!");
                /// làm rỗng danh sách đăng ký
                EmperorTomb_ActivityScript.guildRegister.Clear();
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
                if (!this.scripts.TryGetValue(key, out EmperorTomb_Script_Main script))
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
        /// Trả về thời gian còn lại ở trong Tần Lăng trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTodayTotalSecLeft(KPlayer player)
        {
            /// Nếu không tồn tại trong danh sách
            if (!EmperorTomb_ActivityScript.playerTimes.ContainsKey(player.RoleID))
            {
                /// Trả về thời gian tối đa
                return EmperorTomb.Config.DurationPerDay;
            }
            /// Trả về kết quả
            return EmperorTomb_ActivityScript.playerTimes[player.RoleID];
        }

        /// <summary>
        /// Thiết lập thời gian còn lại ở trong Tần Lăng trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="totalSecLeft"></param>
        /// <returns></returns>
        public static void SetTotayTotalSecLeft(KPlayer player, int totalSecLeft)
        {
            EmperorTomb_ActivityScript.playerTimes[player.RoleID] = totalSecLeft;
        }

        /// <summary>
        /// Kiểm tra điều kiện để người chơi có thể vào Tần Lăng
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string EnterMap_CheckCondition(KPlayer player)
        {
            /// Nếu Tần Lăng chưa mở
            var controlScript = GameMapEventsManager.GetActivityScript(400); 
            if (controlScript == null)
            {
                return "Tần Lăng hiện chưa mở, hãy quay lại sau!";
            }

            if (!controlScript.Activity.IsStarted)
            {
                return "Tần Lăng hiện chưa mở, hãy quay lại sau!";
            }

            if (!GameManager.IsKuaFuServer)
                /// Nếu bang hội chưa đăng ký
                if (!EmperorTomb_ActivityScript.guildRegister.ContainsKey(player.GuildID) && !EmperorTomb_ActivityScript.guildRegister.ContainsKey(player.RoleID))
                {
                    return "Bạn chưa đăng ký!";
                }    

            /// Nếu chưa đủ cấp 80 thì bỏ
            if (player.m_Level < EmperorTomb.Config.LimitLevel)
            {
                return $"Bạn chưa đủ cấp độ. Yêu cầu cấp {EmperorTomb.Config.LimitLevel}!";
            }    

            /// Nếu chưa đủ tài phú thì
            if (player.GetTotalValue() < 1500)
            {
                return "Bạn chưa đủ tài phú. Yêu cầu 1500 tài phú";
            }

            if (Global.GetMantleLevel(player) < ServerConfig.Instance.MantleLevelForEmperorTomb)
            {
                return $"Hoạt động yêu cầu trang bị phi phong cấp {KTLuaLib_System.GetMantleNameFromLevel(ServerConfig.Instance.MantleLevelForEmperorTomb)}";
            }

            /// Nếu đã hết thời gian được ở trong Tần Lăng
            else if (EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player) <= 0)
            {
                return "Ngươi đã hết thời gian ở trong Tần Lăng trong ngày, để đảm bảo an toàn tính mạng, hãy quay lại vào hôm sau!";
            }
            /// Nếu cấp độ không đủ
            else if (player.m_Level < EmperorTomb.Config.LimitLevel)
            {
                return string.Format("Ngươi cần đạt tối thiểu cấp {0} mới có thể tiến vào!", EmperorTomb.Config.LimitLevel);
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

            if (player.GuildID <=0)
            {
                return "Bạn phải vào bang trước";
            }    

            /// Nếu đã đăng ký trong ngày hôm nay
            if (EmperorTomb_ActivityScript.guildRegister.ContainsKey(player.GuildID) || EmperorTomb_ActivityScript.guildRegister.ContainsKey(player.RoleID))
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
        /// Đăng ký bang vào tần lăng
        /// </summary>
        /// <param name="player"></param>
        public static void RegisterEnterEmperorTomp(KPlayer player)
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            if (hour < 15)
            {
                EmperorTomb_ActivityScript.guildRegister[player.GuildID] = player.RoleID;
            }    
            else
            {
                EmperorTomb_ActivityScript.guildRegister[player.RoleID] = player.RoleID;
            }    
            
        }

        /// <summary>
        /// Dịch chuyển người chơi vào Tần Lăng
        /// </summary>
        /// <param name="player"></param>
        public static void MoveToEmperorTomb(KPlayer player)
        {

            if (player.Buffs.HasBuff(1413))
            {
                BuffDataEx buff = player.Buffs.GetBuff(1413);
                //EmperorTomb_ActivityScript.mapIDHasBuff[player.RoleID] = player.MapCode;
                EmperorTomb_ActivityScript.timeDurationDMC[player.RoleID] = buff.Duration;
            }
            else
            {
                //xóa bộ nhớ
                EmperorTomb_ActivityScript.mapIDHasBuff.TryRemove(player.RoleID, out _);
                EmperorTomb_ActivityScript.timeDurationDMC.TryRemove(player.RoleID, out _);
            }
            GlobalEventSource.getInstance().fireEvent(new JoinEmperorTombEventObject(player,GameManager.IsKuaFuServer));
            /// Chuyển về bản đồ tương ứng
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, EmperorTomb.Event.Relive.MapID, EmperorTomb.Event.Relive.PosX, EmperorTomb.Event.Relive.PosY, (int) player.CurrentDir);
        }

        /// <summary>
        /// Trục xuất người chơi khỏi Tần Lăng
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
            int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
            LogManager.WriteLog(LogTypes.EmperorTomb, $"Người chơi: {player.RoleName} bị kich khỏi tần lăng, thời gian còn lại là: {totalSecLeft}");
        }

        /// <summary>
        /// Thực hiện Tick liên tục khi người chơi ở trong Tần Lăng
        /// </summary>
        /// <param name="player"></param>
        public static void ProcessPlayerTick(KPlayer player)
        {
            /// Script điều khiển
            EmperorTomb_ActivityScript activityScript = GameMapEventsManager.GetActivityScript<EmperorTomb_ActivityScript>(400);
            /// Nếu Tần Lăng chưa mở
            if (activityScript == null)
            {
                return;
            }

            /// Thời gian còn lại
            int totalSecLeft = EmperorTomb_ActivityScript.GetTodayTotalSecLeft(player);
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
                    LogManager.WriteLog(LogTypes.EmperorTomb, $"Player: {player.RoleName}, roleID={player.RoleID} bị kích vì bang không phù hợp");
                }

                /// Trục xuất người chơi ra khỏi bản đồ
                EmperorTomb_ActivityScript.KickOut(player);

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
            EmperorTomb_ActivityScript.SetTotayTotalSecLeft(player, totalSecLeft);

            /// Nếu tồn tại Script
            if (activityScript.scripts.TryGetValue(player.CurrentMapCode, out EmperorTomb_Script_Main script))
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
                EmperorTomb_ActivityScript.KickOut(player);
            }
        }
        #endregion
    }
}
