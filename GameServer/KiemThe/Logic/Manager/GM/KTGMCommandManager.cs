using GameServer.KiemThe.CopySceneEvents;
using GameServer.KiemThe.GameEvents.TeamBattle;
using GameServer.KiemThe.CopySceneEvents.XiaoYaoGu;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Activity.PlayerPray;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FactionBattle;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.Logic.Manager.Battle;
using GameServer.KiemThe.LuaSystem;
using GameServer.Logic;
using GameServer.Server;
using KF.Client;
using Server.Data;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using GameServer.KiemThe.CopySceneEvents.YouLongGe;
using Tmsk.Contract;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.SpecialEvent;
using GameServer.KiemThe.Core.Activity.LuckyCircle;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.GameEvents.LotteryEvent;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.GameEvents.LeagueBattle;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;
using System.Security.Cryptography;
using GameServer.KiemThe.GameEvents.BaoThach;
using GameServer.KiemThe.GameEvents.DailyCheckin;
using GameServer.KiemThe.Utilities;
using GameServer.Tools;
using GameServer.KiemThe.GameEvents.FlowerTower;
using Server.Protocol;
using static UnityEngine.GridBrushBase;
using System.Net.Sockets;
using GameServer.KiemThe.GameEvents.DoThach.Data;
using GameServer.KiemThe.GameEvents.DoThach;
using GameServer.KiemThe.Core.BattlePass;
using GameServer.Core.Executor;
using KF.Client;
using GameServer.KiemThe.GameEvents.VongQuayMayMan;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Lớp thực hiện lệnh GM
    /// </summary>
    public static class KTGMCommandManager
    {
        #region Khởi tạo

        /// <summary>
        /// Thông tin GM
        /// </summary>
        private class GMInfo
        {
            /// <summary>
            /// ID nhân vật
            /// </summary>
            public int RoleID { get; set; }

            /// <summary>
            /// Địa chỉ IP
            /// </summary>
            public string IP { get; set; }
        }

        /// <summary>
        /// Đối tượng trung gian sử dụng khóa LOCK
        /// </summary>
        private static readonly object Mutex = new object();

        /// <summary>
        /// Danh sách GM trong hệ thống
        /// </summary>
        private static readonly Dictionary<int, GMInfo> GMList = new Dictionary<int, GMInfo>();

        /// <summary>
        /// Địa chỉ IP mà tất cả người chơi đều là GM
        /// </summary>
        private static readonly HashSet<string> EverybodyAtSpecificIPAddressIsGM = new HashSet<string>();

        private static void BanRole(string roleName, int minutes)
        {
            var roleID = RoleName2IDs.FindRoleIDByName(roleName);
            if (-1 != roleID)
            {
                var otherClient = GameManager.ClientMgr.FindClient(roleID);
                if (null != otherClient)
                {
                    Global.ForceCloseClient(otherClient, "GMKICK");
                }
                else
                {
                    var gmCmdData = string.Format("-kick {0}", roleName);

                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                        string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", roleID, "", 0, "", 0,
                            gmCmdData, 0, 0, GameManager.ServerLineID),
                        null, GameManager.LocalServerId);
                }
            }

            Global.BanRoleNameToDBServer(roleName, minutes);
        }

        private static void UnBanRole(string roleName)
        {
            Global.BanRoleNameToDBServer(roleName, 0);
        }

        public static void LoadBanList()
        {
            SysConOut.WriteLine("Load BanList.xml");
            try
            {
                lock (KTGMCommandManager.Mutex)
                {
                    var xmlNode = Global.GetGameResXml("Config/KT_Setting/BanList.xml");

                    var unBanList = new List<string>();

                    foreach (var node in xmlNode.Elements("UnBan"))
                    {
                        var roleName = node.Attribute("RoleName").Value;
                        unBanList.Add(roleName);
                        UnBanRole(roleName);
                        SysConOut.WriteLine($"Unban {roleName}");
                    }

                    foreach (var node in xmlNode.Elements("Ban"))
                    {
                        var strRoleName = node.Attribute("RoleName").Value;
                        if (unBanList.Contains(strRoleName))
                            continue;
                        BanRole(strRoleName, 10080);
                        SysConOut.WriteLine($"Ban {strRoleName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"Load GMList.xml error\nException: {ex}");
            }
        }

        /// <summary>
        /// Tải danh sách GM mới nhất trong hệ thống
        /// </summary>
        public static void LoadGMList()
        {
            Console.WriteLine("Load GMList.xml");
            try
            {
                lock (KTGMCommandManager.Mutex)
                {
                    KTGMCommandManager.GMList.Clear();
                    KTGMCommandManager.EverybodyAtSpecificIPAddressIsGM.Clear();
                    var xmlText = File.ReadAllText("GMList.xml");
                    var xmlNode = XElement.Parse(xmlText);
                    foreach (var node in xmlNode.Elements("GM"))
                    {
                        var strRoleID = node.Attribute("RoleID").Value;
                        var strIPAddress = node.Attribute("IP").Value;
                        // Nếu ID nhân vật là bất kỳ *
                        if (strRoleID == "*")
                        {
                            // Nếu danh sách địa chỉ IP chưa chứa
                            if (!KTGMCommandManager.EverybodyAtSpecificIPAddressIsGM.Contains(strIPAddress))
                            {
                                KTGMCommandManager.EverybodyAtSpecificIPAddressIsGM.Add(strIPAddress);
                            }
                        }
                        else
                        {
                            var gmInfo = new GMInfo()
                            {
                                RoleID = int.Parse(strRoleID),
                                IP = strIPAddress,
                            };
                            KTGMCommandManager.GMList[gmInfo.RoleID] = gmInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"Load GMList.xml error\nException: {ex}");
            }
        }

        #endregion Khởi tạo

        #region Kiểm tra

        /// <summary>
        /// Kiểm tra người chơi có ID tương ứng có phải GM không
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static bool IsGM(TMSKSocket socket, int roleID)
        {
            lock (KTGMCommandManager.Mutex)
            {
                try
                {
                    // Địa chỉ IP của người chơi
                    var ipEndPoint = socket.RemoteEndPoint as IPEndPoint;
                    var ipAddress = ipEndPoint?.Address.ToString();

                    // Nếu trong danh sách toàn bộ GM thuộc IP có dấu * hoặc IP tương ứng của người chơi này thì là GM
                    if (KTGMCommandManager.EverybodyAtSpecificIPAddressIsGM.Contains("*") ||
                        KTGMCommandManager.EverybodyAtSpecificIPAddressIsGM.Contains(ipAddress))
                    {
                        return true;
                    }

                    // Nếu không có tên trong danh sách GM
                    if (!KTGMCommandManager.GMList.TryGetValue(roleID, out var gmInfo))
                    {
                        return false;
                    }

                    // Nếu IP GM config là bất kỳ hoặc IP GM config trùng với IP người chơi thì là GM
                    return gmInfo.IP == "*" || gmInfo.IP == ipAddress;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        public static bool IsGMByRoleID(int roleID)
        {
            return KTGMCommandManager.GMList.TryGetValue(roleID, out _);
        }

        /// <summary>
        /// Kiểm tra người chơi có phải GM không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsGM(KPlayer player)
        {
            return KTGMCommandManager.IsGM(player.ClientSocket, player.RoleID);
        }

        #endregion Kiểm tra

        #region Thực thi lệnh GM

        public static string ProcessCommandFromHttpServices(string command)
        {
            var response = "";
            try
            {
                LarkNotificationHelper.PushNormalNotification($"[HTTP][{GameManager.ServerId}] Lệnh GM từ HTTP: {command}");
                // Chuẩn hóa
                command = command.Trim();
                while (command.IndexOf("  ", StringComparison.Ordinal) != -1)
                {
                    command = command.Replace("  ", " ");
                }

                // Phân tích dữ liệu
                var para = command.Split(' ');

                // Tên hàm
                var functionName = para[0];
                // Tổng số tham biến
                var paramsCount = para.Length - 1;

                // Kiểm tra và thực thi
                switch (functionName)
                {
                    #region Vòng quay may mắn

                    case "ReloadLuckyCircle":
                        {
                            if (paramsCount == 0)
                            {
                                KTLuckyCircleManager.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadLuckyCircle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Bách Bảo Rương

                    case "ReloadSeashellCircle":
                        {
                            if (paramsCount == 0)
                            {
                                KTSeashellCircleManager.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadSeashellCircle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Tải lại ServerConfig

                    case "ReloadServerConfig":
                        {
                            if (paramsCount == 0)
                            {
                                ServerConfig.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadServerConfig' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tải lại ServerConfig

                    #region Script-Lua

                    case "LoadLua":
                        {
                            foreach (var element in para)
                                if (element != "LoadLua")
                                {
                                    if (!int.TryParse(element, out var scriptId))
                                    {
                                        if (KTGMCommandManager.ReloadScriptLua(element))
                                        {
                                            response = $"Tải mới Script {element} thành công!";
                                        }
                                        else
                                        {
                                            response = $"Không tìm thấy Script {element}!";
                                        }

                                        continue;
                                    }

                                    KTGMCommandManager.ReloadScriptLua(scriptId);
                                    response = $"Tải mới Script ID {scriptId} thành công!";
                                }

                            break;
                        }

                    #endregion Script-Lua

                    case "SendSystemMail":
                        {
                            LogManager.WriteLog(LogTypes.Mail, $"[GMCmd]-[HTTP]-[{command}]");

                            if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    var result = KTGMCommandManager.SendSystemMail(targetID, title, content, "", 0, 0);
                                    response = result
                                        ? $"Gửi thư tới {targetID} thành công."
                                        : $"Gửi thư tới {targetID} thất bại.";

                                    if (result)
                                    {
                                        LarkNotificationHelper.PushNormalNotification($"[HTTP][{GameManager.ServerId}] Gửi thư thành công tới {targetID}: [{title}] [{content}]");
                                    }
                                }
                                else
                                {
                                    response = "ERROR";
                                }
                            }
                            else if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var items = para[4];
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    var result = KTGMCommandManager.SendSystemMail(targetID, title, content, items, 0, 0);
                                    response = result
                                        ? $"Gửi thư tới {targetID} thành công."
                                        : $"Gửi thư tới {targetID} thất bại.";

                                    if (result)
                                    {
                                        LarkNotificationHelper.PushNormalNotification($"[HTTP][{GameManager.ServerId}] Gửi thư thành công tới {targetID}: [{title}] [{content}], vật phẩm: [{items}]");
                                    }
                                }
                                else
                                {
                                    response = "ERROR";
                                }
                            }
                            else if (paramsCount == 5)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var boundMoney = int.Parse(para[4]);
                                var boundToken = int.Parse(para[5]);
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    var result = KTGMCommandManager.SendSystemMail(targetID, title, content, "", boundMoney, boundToken);
                                    response = result
                                        ? $"Gửi thư tới {targetID} thành công."
                                        : $"Gửi thư tới {targetID} thất bại.";

                                    if (result)
                                    {
                                        LarkNotificationHelper.PushNormalNotification($"[HTTP][{GameManager.ServerId}] Gửi thư thành công tới {targetID}: [{title}] [{content}], bạc khóa: [{boundMoney}], knb khóa: [{boundToken}]");
                                    }
                                }
                                else
                                {
                                    response = "ERROR";
                                }
                            }
                            else if (paramsCount == 6)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var items = para[4];
                                var boundMoney = int.Parse(para[5]);
                                var boundToken = int.Parse(para[6]);
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    var result = KTGMCommandManager.SendSystemMail(targetID, title, content, items,
                                        boundMoney,
                                        boundToken);
                                    response = result
                                        ? $"Gửi thư tới {targetID} thành công."
                                        : $"Gửi thư tới {targetID} thất bại.";

                                    if (result)
                                    {
                                        LarkNotificationHelper.PushNormalNotification($"[HTTP][{GameManager.ServerId}] Gửi thư thành công tới {targetID}: [{title}] [{content}], vật phẩm: [{items}] bạc khóa: [{boundMoney}], knb khóa: [{boundToken}]");
                                    }
                                }
                                else
                                {
                                    response = "ERROR";
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SendSystemMail' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "BanAcc":
                        {
                            var otherRoleName = para[1];
                            var minutes = Global.SafeConvertToInt32(para[2]);
                            var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);
                            if (-1 != roleID)
                            {
                                var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                if (null != otherClient)
                                {
                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                                else
                                {
                                    var gmCmdData = string.Format("-kick {0}", para[1]);

                                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                                        $"{roleID}::{0}::{0}:{gmCmdData}:{0}:{0}:{GameManager.ServerLineID}",
                                        null, GameManager.LocalServerId);
                                }
                            }

                            Global.BanRoleNameToDBServer(otherRoleName, minutes);

                            response = "Ban người chơi thành công";

                            break;
                        }

                    case "BanAccByRoleID":
                        {
                            var minutes = Global.SafeConvertToInt32(para[2]);
                            var roleID = Int32.Parse(para[1]);
                            if (-1 != roleID)
                            {
                                var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                if (null != otherClient)
                                {
                                    Global.BanRoleNameToDBServer(otherClient.RoleName, minutes);

                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                                else
                                {
                                    var gmCmdData = $"-kick {para[1]}";

                                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                                        $"{roleID}::{0}::{0}:{gmCmdData}:{0}:{0}:{GameManager.ServerLineID}",
                                        null, GameManager.LocalServerId);
                                }
                            }

                            response = "Ban người chơi thành công";

                            break;
                        }

                    case "UnBanAcc":
                        {
                            var otherRoleName = para[1];
                            Global.BanRoleNameToDBServer(otherRoleName, 0);
                            response = "Gỡ ban người chơi thành công";

                            break;
                        }

                    case "BanChat":
                        {
                            var otherRoleName = para[1];
                            var Hours = Global.SafeConvertToInt32(para[2]);

                            if (Hours > 0)
                            {
                                Global.BanRoleChatToDBServer(otherRoleName, Hours);

                                BanChatManager.AddBanRoleName(otherRoleName, Hours);
                            }

                            response = "Ban chat người chơi thành công";

                            break;
                        }

                    case "KickOut":
                        {
                            if (paramsCount == 1)
                            {
                                var otherRoleName = para[1];
                                otherRoleName = otherRoleName.Replace("<color=#0486dc>[GM]</color> ", "");
                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);
                                if (-1 != roleID)
                                {
                                    var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                    if (null != otherClient)
                                    {
                                        Global.ForceCloseClient(otherClient, "GMKICK");
                                    }
                                }

                                response = "Kick người chơi thành công";

                                var str_cmd = $"{otherRoleName}";
                                var dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GM_CLEAR_CACHE_USER,
                                    str_cmd, GameManager.ServerLineID);
                                if (null == dbFields)
                                {
                                    response = "Xóa cache người chơi lỗi";
                                    break;
                                }

                                response = $"Xóa cache người chơi: {dbFields[1]}";
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'KickOut' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "KickOutLsv":
                        {
                            KTGlobal.TakePlayersBackVillage();
                            break;
                        }

                    #region System chat

                    case "SysChat":
                        {
                            var message = command.Substring(functionName.Length + 1);
                            KTGMCommandManager.SendSystemChat(message);
                            break;
                        }
                    case "SysNotify":
                        {
                            var message = command.Substring(functionName.Length + 1);
                            KTGMCommandManager.SendSystemEventNotification(message);
                            break;
                        }

                    #endregion System chat

                    #region Hoạt động

                    case "StartActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var activityID = int.Parse(para[1]);
                                KTGMCommandManager.StartActivity(activityID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'StartActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "StopActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var activityID = int.Parse(para[1]);
                                KTGMCommandManager.StopActivity(activityID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'StopActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Hoạt động

                    #region reload config

                    case "ReloadRandomBox":
                        ItemRandomBox.Setup();
                        SysConOut.WriteLine("Reloaded Random Boxes Configuration!");
                        break;

                        #endregion
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "Process GM Command Error...\n" + ex.ToString(), false);
            }

            return response;
        }

        /// <summary>
        /// Thực hiện lệnh GM
        /// </summary>
        /// <param name="player">Đối tượng người chơi</param>
        /// <param name="command">Chuỗi biểu diễn lệnh</param>
        public static async void Process(KPlayer player, string command)
        {
            // Nếu dữ liệu không chính xác
            if (player == null || string.IsNullOrEmpty(command))
            {
                return;
            }
            // Nếu không phải GM
            else if (!KTGMCommandManager.IsGM(player))
            {
                LogManager.WriteLog(LogTypes.Diagnostic, $"{player.RoleName} thực hiện lệnh GM {command}");
                LogManager.WriteLog(LogTypes.Error,
                    string.Format("Process GM Command for {0}({1}) => ACCESS DENIED", player.RoleName, player.RoleID));
                return;
            }


            LogManager.WriteLog(LogTypes.Diagnostic, $"{player.RoleName} thực hiện lệnh GM {command}");
            LarkNotificationHelper.PushNormalNotification($"[Server {GameManager.ServerId}] Lệnh GM từ {player.RoleName}[{player.RoleID}]: {command}");

            try
            {
                // Chuẩn hóa
                command = command.Trim();
                while (command.IndexOf("  ") != -1)
                {
                    command = command.Replace("  ", " ");
                }

                // Phân tích dữ liệu
                var para = command.Split(' ');

                // Tên hàm
                var functionName = para[0];
                // Tổng số tham biến
                var paramsCount = para.Length - 1;

                // Kiểm tra và thực thi
                switch (functionName)
                {
                    #region Vòng quay may mắn

                    case "ReloadLuckyCircle":
                        {
                            if (paramsCount == 0)
                            {
                                KTLuckyCircleManager.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadLuckyCircle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SetLuckyCircleTotalTurn":
                        {
                            if (paramsCount == 1)
                            {
                                var totalTurn = int.Parse(para[1]);
                                KTGMCommandManager.SetLuckyCircleTotalTurn(player, totalTurn);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var totalTurn = int.Parse(para[2]);
                                KTGMCommandManager.SetLuckyCircleTotalTurn(targetID, totalTurn);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetLuckyCircleTotalTurn' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "GetLuckyCircleTotalTurn":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.GetLuckyCircleTotalTurn(player, player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.GetLuckyCircleTotalTurn(player, targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'GetLuckyCircleTotalTurn' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #region Theo dõi packet (PacketMonitoringUsers)

                    // Thêm RoleID vào danh sách theo dõi packet + trace damage (chỉ trong RAM, không ghi file XML).
                    // Restart server hoặc lệnh console reloadPacketMonitor sẽ lấy lại danh sách gốc từ file.
                    case "PacketMonitorAdd":
                        {
                            if (paramsCount == 1 && int.TryParse(para[1], out int roleID) && roleID > 0)
                            {
                                bool added = PacketMonitoringManager.Add(roleID);
                                string msg = added
                                    ? string.Format("PacketMonitor: đã thêm RoleID {0} (tổng {1} đang theo dõi, không ghi file).", roleID, PacketMonitoringManager.Count)
                                    : string.Format("PacketMonitor: RoleID {0} đã có sẵn trong danh sách ({1} đang theo dõi).", roleID, PacketMonitoringManager.Count);
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, msg);
                            }
                            else
                            {
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, "Usage: PacketMonitorAdd <roleID>");
                            }

                            break;
                        }

                    // Xóa RoleID khỏi danh sách theo dõi packet (chỉ trong RAM, không ghi file XML).
                    case "PacketMonitorRemove":
                        {
                            if (paramsCount == 1 && int.TryParse(para[1], out int roleID) && roleID > 0)
                            {
                                bool removed = PacketMonitoringManager.Remove(roleID);
                                string msg = removed
                                    ? string.Format("PacketMonitor: đã xóa RoleID {0} (còn {1} đang theo dõi, không ghi file).", roleID, PacketMonitoringManager.Count)
                                    : string.Format("PacketMonitor: RoleID {0} không có trong danh sách ({1} đang theo dõi).", roleID, PacketMonitoringManager.Count);
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, msg);
                            }
                            else
                            {
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, "Usage: PacketMonitorRemove <roleID>");
                            }

                            break;
                        }

                    // Liệt kê danh sách RoleID đang theo dõi packet.
                    case "PacketMonitorList":
                        {
                            List<int> monitoredIds = PacketMonitoringManager.Snapshot();
                            string msg = monitoredIds.Count == 0
                                ? "PacketMonitor: danh sách rỗng, không theo dõi ai."
                                : string.Format("PacketMonitor: đang theo dõi {0} RoleID: {1}", monitoredIds.Count, string.Join(", ", monitoredIds));
                            GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, msg);

                            break;
                        }

                    #endregion Theo dõi packet (PacketMonitoringUsers)

                    case "TestJackpotTry":
                    case "JackpotTry":
                    case "JackopotTry":
                        {
                            if (paramsCount >= 1)
                            {
                                long bet = long.Parse(para[1]);
                                bool isBig = paramsCount >= 2 && para[2] == "1";
                                bool isSmall = paramsCount >= 3 && para[3] == "1";
                                var result = JackpotClient.getInstance().TryHitKnbJackpot(player.RoleID, player.RoleName, bet, "GMTest", isBig, isSmall, true);
                                if (result != null)
                                {
                                    string msg = string.Format("JackpotTry: Player={0}, Bet={1}, WinBig={2}, WinSmall={3}, Award={4}, JarBefore={5}, JarAfter={6}, Time={7}, Msg={8}", result.PlayerID, bet, result.IsBigWin, result.IsSmallWin, result.AwardKnb, result.JarBefore, result.JarAfter, result.Time, result.Message);
                                    LogManager.WriteLog(LogTypes.Error, msg);
                                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, msg);
                                }
                                else
                                {
                                    LogManager.WriteLog(LogTypes.Error, string.Format("JackpotTry: result null for playerId={0}, bet={1}, isBig={2}, isSmall={3}", player.RoleID, bet, isBig, isSmall));
                                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, "JackpotTry: result null");
                                }
                            }
                            else
                            {
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, "Usage: JackpotTry bet [isBig] [isSmall]");
                            }
                            break;
                        }

                    case "TestJackpotDump":
                    case "JackpotDump":
                    case "JackopotDump":
                        {
                            var list = JackpotClient.getInstance().GetJackpotHistory();
                            if (list == null || list.Count == 0)
                            {
                                LogManager.WriteLog(LogTypes.Error, "JackpotDump: empty");
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, "JackpotDump: empty");
                            }
                            else
                            {
                                foreach (var r in list)
                                {
                                    string msg = string.Format("JackpotDump: Player={0}, WinBig={1}, WinSmall={2}, Award={3}, JarBefore={4}, JarAfter={5}, Time={6}, Msg={7}", r.PlayerID, r.IsBigWin, r.IsSmallWin, r.AwardKnb, r.JarBefore, r.JarAfter, r.Time, r.Message);
                                    LogManager.WriteLog(LogTypes.Error, msg);
                                    GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, msg);
                                }
                            }
                            break;
                        }

                    case "AddJackpot":
                        {
                            if (paramsCount == 1 && long.TryParse(para[1], out var amount))
                            {
                                long jarAfter = JackpotClient.getInstance().AdjustJarAmount(amount);
                                if (jarAfter >= 0)
                                {
                                    string action = amount > 0 ? "thêm" : "trừ";

                                    string logMessage = $"AddJackpot: {player.RoleName} vừa {action} {amount} KNB vào hũ, JarAfter={jarAfter}";
                                    LogManager.WriteLog(LogTypes.GM_CMD, logMessage);
                                    PlayerManager.ShowNotification(player, $"{action} thành công {amount} KNB vào hũ");
                                    LarkNotificationHelper.PushNormalNotification($"{player.RoleName} vừa {action} {amount} KNB vào hũ, JarAfter={jarAfter}");

                                    LuckySpinManager.PushNewNotification(jarAfter, 0, "", "", 0, 0); // Cập nhật hũ về giao diện user
                                }
                                else
                                {
                                    string msg = $"AddJackpot: failed, amount={amount}";
                                    LogManager.WriteLog(LogTypes.GM_CMD, msg);
                                    PlayerManager.ShowNotification(player, msg);
                                }
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Usage: AddJackpot [So_Luong_KNB]");
                            }

                            break;
                        }

                    case "HitJackpot":
                        {
                            if (paramsCount == 2)
                            {
                                int jackpot = int.Parse(para[1]);
                                int targetId = int.Parse(para[2]);

                                if (jackpot != 1 && jackpot != 2)
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh thất bại: Sai tham số loại hũ (1: Hũ Nhỏ, 2: Hũ Lớn).");
                                    break;
                                }

                                KPlayer targetPlayer = GameManager.ClientMgr.FindClient(targetId);
                                if (targetPlayer == null)
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh thất bại: ID nhân vật không tồn tại hoặc không online.");
                                    break;
                                }

                                targetPlayer.NextSpinJackpot = jackpot;

                                long currentJarAmount = JackpotClient.getInstance().AdjustJarAmount(0);
                                string jackpotName = jackpot == 1 ? "hũ nhỏ" : "hũ lớn";

                                string logMessage = $"HitJackpot: {player.RoleName} cấp buff nổ {jackpotName} cho {targetPlayer.RoleName}({targetId}). Quỹ hũ hiện tại: {currentJarAmount} KNB.";
                                LogManager.WriteLog(LogTypes.GM_CMD, logMessage);
                                PlayerManager.ShowNotification(player, $"Đã cấp buff nổ {jackpotName} cho nhân vật {targetPlayer.RoleName}.");
                                LarkNotificationHelper.PushNormalNotification($"{player.RoleName} cấp buff nổ {jackpotName} cho {targetPlayer.RoleName}({targetId}). Quỹ hũ hiện tại: {currentJarAmount} KNB.");
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Cú pháp: HitJackpot [Loại_Hũ(1/2)] [ID_Nhân_Vật]");
                            }
                            break;
                        }

                    #endregion

                    #region Bách Bảo Rương

                    case "ReloadSeashellCircle":
                        {
                            if (paramsCount == 0)
                            {
                                KTSeashellCircleManager.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadSeashellCircle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    // case "SetRateToTreasure":
                    // {
                    //     if (paramsCount == 1) {
                    //         if (int.TryParse(para[1], out var rate)) {
                    //             KTSeashellCircleManager.SetRateToTreasure(rate);
                    //             PlayerManager.ShowNotification(player, "Cập nhật tỉ lệ hũ thành công!");
                    //         }
                    //     }
                    //     break;
                    // }
                    //
                    // case "SetRateTo100Treasure":
                    // {
                    //     if (paramsCount == 1) {
                    //         if (int.TryParse(para[1], out var rate)) {
                    //             KTSeashellCircleManager.SetRateTo100Treasure(rate);
                    //             PlayerManager.ShowNotification(player, "Cập nhật tỉ lệ hũ 100 thành công!");
                    //         }
                    //     }
                    //     break;
                    // }
                    //
                    // case "SetCustomSeashellRate":
                    // {
                    //     if (paramsCount == 3)
                    //     {
                    //         int.TryParse(para[1], out var roleID);
                    //         int.TryParse(para[2], out var rate);
                    //         int.TryParse(para[3], out var rate100);
                    //         if (roleID > 0 && rate > 0)
                    //         {
                    //             KTSeashellCircleManager.CustomTreasureRate[roleID] = new Tuple<int, int>(rate, rate100);
                    //             PlayerManager.ShowNotification(player, "Thiết lập tỉ lệ tùy chỉnh thành công!");
                    //         }
                    //     }
                    //
                    //     break;
                    // }

                    case "SetSeashellTreasureNextTurn":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.SetSeashellTreasureNextTurn(player, -1);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.SetSeashellTreasureNextTurn(targetID, -1);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var bet = int.Parse(para[2]);
                                KTGMCommandManager.SetSeashellTreasureNextTurn(targetID, bet);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetSeashellTreasureNextTurn' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Tải lại ServerConfig

                    case "ReloadServerConfig":
                        {
                            if (paramsCount == 0)
                            {
                                ServerConfig.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadServerConfig' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tải lại ServerConfig

                    #region Script-Lua

                    case "LoadLua":
                        {
                            foreach (var element in para)
                                if (element != "LoadLua")
                                {
                                    if (!int.TryParse(element, out var scriptId))
                                    {
                                        if (KTGMCommandManager.ReloadScriptLua(element))
                                        {
                                            PlayerManager.ShowNotification(player,
                                                $"Tải mới Script {element} thành công!");
                                        }
                                        else
                                        {
                                            PlayerManager.ShowNotification(player,
                                                $"Không tìm thấy Script {element}!");
                                        }

                                        continue;
                                    }

                                    KTGMCommandManager.ReloadScriptLua(scriptId);
                                    PlayerManager.ShowNotification(player,
                                        $"Tải mới Script ID {scriptId} thành công!");
                                }

                            break;
                        }

                    #endregion Script-Lua

                    #region Bất tử GM

                    case "Invisiblity":
                        {
                            if (paramsCount == 1)
                            {
                                var state = int.Parse(para[1]);
                                player.GM_Invisiblity = state == 1;
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'Invisiblity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "Hidden":
                        {
                            if (paramsCount == 1)
                            {
                                var state = int.Parse(para[1]);
                                player.GM_Hidden = state == 1;
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'Invisiblity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "Immortality":
                        {
                            if (paramsCount == 1)
                            {
                                var state = int.Parse(para[1]);
                                player.GM_Immortality = state == 1;
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'Immortality' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "BandAcc":
                        {
                            var otherRoleName = para[1];
                            var minutes = Global.SafeConvertToInt32(para[2]);
                            var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);
                            if (-1 != roleID)
                            {
                                var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                if (null != otherClient)
                                {
                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                                else
                                {
                                    var gmCmdData = string.Format("-kick {0}", para[1]);

                                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                                        string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", roleID, "", 0, "", 0,
                                            gmCmdData, 0, 0, GameManager.ServerLineID),
                                        null, GameManager.LocalServerId);
                                }
                            }

                            Global.BanRoleNameToDBServer(otherRoleName, minutes);

                            PlayerManager.ShowNotification(player, "Ban người chơi thành công");

                            break;
                        }

                    case "BandAccByRoleID":
                        {
                            var minutes = Global.SafeConvertToInt32(para[2]);
                            var roleID = Int32.Parse(para[1]);
                            if (-1 != roleID)
                            {
                                var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                if (null != otherClient)
                                {
                                    Global.BanRoleNameToDBServer(otherClient.RoleName, minutes);

                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                                else
                                {
                                    var gmCmdData = string.Format("-kick {0}", para[1]);

                                    GameManager.DBCmdMgr.AddDBCmd((int)TCPGameServerCmds.CMD_SPR_CHAT,
                                        string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", roleID, "", 0, "", 0,
                                            gmCmdData, 0, 0, GameManager.ServerLineID),
                                        null, GameManager.LocalServerId);
                                }
                            }


                            PlayerManager.ShowNotification(player, "Ban người chơi thành công");

                            break;
                        }

                    case "UnBandAcc":
                        {
                            var otherRoleName = para[1];
                            Global.BanRoleNameToDBServer(otherRoleName, 0);
                            PlayerManager.ShowNotification(player, "Ban người chơi thành công");

                            break;
                        }

                    case "BandChat":
                        {
                            var otherRoleName = para[1];
                            var Hours = Global.SafeConvertToInt32(para[2]);

                            if (Hours > 0)
                            {
                                Global.BanRoleChatToDBServer(otherRoleName, Hours);

                                BanChatManager.AddBanRoleName(otherRoleName, Hours);
                            }

                            PlayerManager.ShowNotification(player, "Ban người chơi thành công");

                            break;
                        }

                    #endregion Bất tử GM

                    #region Tạo Captcha

                    case "GenerateCaptcha":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.GenerateCaptcha(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.GenerateCaptcha(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'GenerateCaptcha' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Trạng thái ngũ hành

                    case "AddSeriesState":
                        {
                            if (paramsCount == 2)
                            {
                                var stateID = para[1];
                                var time = float.Parse(para[2]);
                                KTGMCommandManager.AddSeriesState(player, stateID, time);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var stateID = para[2];
                                var time = float.Parse(para[3]);
                                KTGMCommandManager.AddSeriesState(targetID, stateID, time);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddSeriesState' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "RemoveSeriesState":
                        {
                            if (paramsCount == 1)
                            {
                                var stateID = para[1];
                                KTGMCommandManager.RemoveSeriesState(player, stateID);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var stateID = para[2];
                                KTGMCommandManager.RemoveSeriesState(targetID, stateID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'RemoveSeriesState' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Trạng thái ngũ hành

                    #region Trị liệu

                    case "Heal":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.Heal(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.Heal(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'Heal' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Trị liệu

                    #region Tải lại danh sách GM

                    case "ReloadGMList":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.LoadGMList();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadGMList' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "BotQuestStatus":
                        {
                            if (paramsCount == 1 && int.TryParse(para[1], out int botId))
                            {
                                var bot = KTBotManager.FindBot(botId);
                                string message = bot == null ? "BOT not found: " + botId :
                                    bot.RoleName + " | Quest=" + bot.QuestController.QuestId +
                                    " | State=" + bot.QuestController.State +
                                    " | Map=" + bot.CurrentMapCode + " | Pos=" + bot.CurrentPos +
                                    " | Moving=" + KTOtherObjectStoryBoard.Instance.HasStoryBoard(bot) +
                                    " | Speed=" + bot.GetCurrentRunSpeed() +
                                    " | Progress=" + bot.MainQuestActions.ObjectiveValue +
                                    " | Completed=" + bot.MainQuestActions.CompletedCount +
                                    " | Status=" + bot.MainQuestActions.Status +
                                    " | Error=" + bot.QuestController.LastError;
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener,
                                    Global._TCPManager.TcpOutPacketPool, player, message);
                            }
                            else
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener,
                                    Global._TCPManager.TcpOutPacketPool, player, "Usage: BotQuestStatus <botId>");
                            break;
                        }
                    case "AddBot":
                        {
                            if (paramsCount == 0)
                            {
                                KTBotManager.AddBotByGm(player.MapCode, player.PosX, player.PosY, player.RoleDirection,
                                    player.CurrentGrid, true, 100);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddBot' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "KickOut":
                        {
                            if (paramsCount == 1)
                            {
                                var otherRoleName = para[1];
                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);
                                if (-1 != roleID)
                                {
                                    var otherClient = GameManager.ClientMgr.FindClient(roleID);
                                    if (null != otherClient)
                                    {
                                        Global.ForceCloseClient(otherClient, "GMKICK");
                                    }
                                    else
                                    {
                                    }
                                }

                                PlayerManager.ShowNotification(player, "Kick người chơi thành công");

                                var strcmd = string.Format("{0}", otherRoleName);
                                var dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GM_CLEAR_CACHE_USER,
                                    strcmd, GameManager.ServerLineID);
                                if (null == dbFields)
                                {
                                    PlayerManager.ShowNotification(player, "Xóa cache người chơi lỗi");
                                    break;
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player,
                                        string.Format("Xóa cache người chơi: {0}", dbFields[1]));
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'KickOut' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tải lại danh sách GM

                    #region ID người chơi

                    case "PlayerInfo":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.GetPlayerInfo(player, player.RoleName);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetName = para[1];
                                KTGMCommandManager.GetPlayerInfo(player, targetName);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'PlayerInfo' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion ID người chơi

                    #region Dịch chuyển

                    case "GoTo":
                        {
                            if (paramsCount == 2)
                            {
                                var posX = int.Parse(para[1]);
                                var posY = int.Parse(para[2]);
                                KTGMCommandManager.GoTo(player, player.CurrentMapCode, posX, posY);
                            }
                            else if (paramsCount == 3)
                            {
                                var mapCode = int.Parse(para[1]);
                                var posX = int.Parse(para[2]);
                                var posY = int.Parse(para[3]);
                                KTGMCommandManager.GoTo(player, mapCode, posX, posY);
                            }
                            else if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var mapCode = int.Parse(para[2]);
                                var posX = int.Parse(para[3]);
                                var posY = int.Parse(para[4]);
                                KTGMCommandManager.GoTo(targetID, mapCode, posX, posY);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'GoTo' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "GoToPlayer":
                        {
                            if (paramsCount == 1)
                            {
                                var targetName = para[1];
                                // tìm nhân vật theo tên
                                var target = KTGMCommandManager.FindPlayer(targetName);
                                if (target != null)
                                {
                                    var mapCodeID = target.CurrentMapCode;
                                    var posX = target.PosX;
                                    var posY = target.PosY;
                                    KTGMCommandManager.GoTo(player, mapCodeID, posX, posY);
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, $"Không tìm thấy người chơi: {targetName}");
                                }
                            }
                            else if (paramsCount == 2)
                            {
                                var targetName = para[1];
                                var inviable = int.Parse(para[2]);
                                // tìm nhân vật theo tên
                                var target = KTGMCommandManager.FindPlayer(targetName);
                                if (target != null)
                                {
                                    var mapCodeID = target.CurrentMapCode;
                                    var posX = target.PosX;
                                    var posY = target.PosY;
                                    KTGMCommandManager.GoTo(player, mapCodeID, posX, posY);

                                    // tự động tàng hình và bất tử khi đến nơi
                                    if (player.CurrentMapCode != mapCodeID)
                                    {
                                        await System.Threading.Tasks.Task.Delay(3000);
                                    }

                                    player.GM_Immortality = inviable == 1;
                                    player.GM_Hidden = inviable == 1;
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, $"Không tìm thấy người chơi: {targetName}");
                                }
                            }

                            break;
                        }

                    #endregion Dịch chuyển

                    #region Kỹ năng và Buff

                    case "AddSkill":
                        {
                            if (paramsCount == 2)
                            {
                                var skillID = int.Parse(para[1]);
                                var level = int.Parse(para[2]);
                                KTGMCommandManager.AddSkill(player, skillID, level);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var skillID = int.Parse(para[2]);
                                var level = int.Parse(para[3]);
                                KTGMCommandManager.AddSkill(targetID, skillID, level);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddSkill' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "RemoveSkill":
                        {
                            if (paramsCount == 1)
                            {
                                var skillID = int.Parse(para[1]);
                                KTGMCommandManager.RemoveSkill(player, skillID);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var skillID = int.Parse(para[2]);
                                KTGMCommandManager.RemoveSkill(targetID, skillID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'RemoveSkill' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "AddBuff":
                        {
                            if (paramsCount == 2)
                            {
                                var skillID = int.Parse(para[1]);
                                var level = int.Parse(para[2]);
                                KTGMCommandManager.AddBuff(player, skillID, level);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var skillID = int.Parse(para[2]);
                                var level = int.Parse(para[3]);
                                KTGMCommandManager.AddBuff(targetID, skillID, level);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddBuff' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "RemoveBuff":
                        {
                            if (paramsCount == 1)
                            {
                                var skillID = int.Parse(para[1]);
                                KTGMCommandManager.RemoveBuff(player, skillID);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var skillID = int.Parse(para[2]);
                                KTGMCommandManager.RemoveBuff(targetID, skillID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'RemoveBuff' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetAllSkillCooldown":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetAllSkillCooldown(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetAllSkillCooldown(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllSkillCooldown' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Kỹ năng và Buff

                    case "ResetRecord":
                        {
                            if (paramsCount == 1)
                            {
                                var recordID = int.Parse(para[1]);
                                player.SetValueOfDailyRecore(recordID, -1);
                                player.SetValueOfForeverRecore(recordID, -1);
                                player.SetValueOfMonthRecore(recordID, -1);
                                player.SetValueOfWeekRecore(recordID, -1);
                                PlayerManager.ShowNotification(player, "Reset thành công!");
                            }
                            else if (paramsCount == 2)
                            {
                                var recordID = int.Parse(para[1]);
                                var recordType = para[2];
                                switch (recordType)
                                {
                                    case "Day":
                                        player.SetValueOfDailyRecore(recordID, -1);
                                        break;
                                    case "Forever":
                                        player.SetValueOfForeverRecore(recordID, -1);
                                        break;
                                    case "Month":
                                        player.SetValueOfMonthRecore(recordID, -1);
                                        break;
                                    case "Week":
                                        player.SetValueOfWeekRecore(recordID, -1);
                                        break;
                                    default:
                                        PlayerManager.ShowNotification(player, "Cú pháp lỗi!");
                                        return;
                                }

                                PlayerManager.ShowNotification(player, $"Reset {recordType} thành công!");
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Cú pháp lỗi!");
                            }

                            break;
                        }

                    #region System chat

                    case "SysChat":
                        {
                            var message = command.Substring(functionName.Length + 1);
                            KTGMCommandManager.SendSystemChat(message);
                            break;
                        }
                    case "SysNotify":
                        {
                            var message = command.Substring(functionName.Length + 1);
                            KTGMCommandManager.SendSystemEventNotification(message);
                            break;
                        }

                    #endregion System chat

                    #region Tạo vật phẩm

                    case "CreateItem":
                        {
                            // if (GameManager.IsLiveServer)
                            //     break;
                            if (paramsCount == 2)
                            {
                                var itemID = int.Parse(para[1]);
                                var quantity = int.Parse(para[2]);
                                KTGMCommandManager.CreateItem(player, itemID, quantity, -1);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var itemID = int.Parse(para[2]);
                                var quantity = int.Parse(para[3]);
                                KTGMCommandManager.CreateItem(player, targetID, itemID, quantity, -1);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'CreateItem' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "CreateItemExp":
                        {
                            // if (GameManager.IsLiveServer)
                            //     break;
                            if (paramsCount == 3)
                            {
                                var itemID = int.Parse(para[1]);
                                var quantity = int.Parse(para[2]);
                                var EXP = int.Parse(para[3]);
                                KTGMCommandManager.CreateItem(player, itemID, quantity, EXP);
                            }
                            else if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var itemID = int.Parse(para[2]);
                                var quantity = int.Parse(para[3]);
                                var EXP = int.Parse(para[4]);
                                KTGMCommandManager.CreateItem(player, targetID, itemID, quantity, EXP);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'CreateItem' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tạo vật phẩm

                    #region Tạo quái

                    case "CreateMonster":
                        {
                            if (paramsCount == 2)
                            {
                                var monsterID = int.Parse(para[1]);
                                var monsterType = int.Parse(para[2]);
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(player.MapCode, monsterID, player.CopyMapID,
                                    1, player.PosX, player.PosY, "", "", -1, -1, Entities.Direction.DOWN,
                                    KE_SERIES_TYPE.series_none, (MonsterAIType)monsterType, -1, -1, -1, "", null);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'CreateMonster' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tạo quái

                    case "UnBind":
                        {
                            if (paramsCount == 1)
                            {
                                var slot = int.Parse(para[1]);
                                var itemGD = player.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot)
                                    .FirstOrDefault();
                                if (itemGD == null)
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                                    break;
                                }

                                var TotalUpdate = new Dictionary<UPDATEITEM, object>();

                                TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
                                TotalUpdate.Add(UPDATEITEM.ITEMDBID, itemGD.Id);
                                TotalUpdate.Add(UPDATEITEM.BINDING, 0);

                                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, itemGD, player, "GMUNBIND",
                                        true))
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                                }
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                            }

                            break;
                        }

                    case "BreakItem":
                        {
                            if (paramsCount == 2)
                            {
                                var slot = int.Parse(para[1]);
                                var strong = int.Parse(para[2]);
                                var itemGD = player.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot)
                                    .FirstOrDefault();
                                if (itemGD == null)
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                                    break;
                                }

                                var TotalUpdate = new Dictionary<UPDATEITEM, object>();

                                TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
                                TotalUpdate.Add(UPDATEITEM.ITEMDBID, itemGD.Id);
                                TotalUpdate.Add(UPDATEITEM.STRONG, strong);

                                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, itemGD, player, "GMBREAK",
                                        true))
                                {
                                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                                }
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                            }

                            break;
                        }

                    case "ResetPoints":
                        {
                            if (paramsCount == 1)
                            {
                                var roleID = int.Parse(para[1]);
                                player = KTGMCommandManager.FindPlayer(roleID);
                                if (player != null)
                                {
                                    player.UnAssignPotential();
                                }
                            }
                            else
                            {
                                player.UnAssignPotential();
                            }
                            break;
                        }

                    #region Xóa toàn bộ quái trong bản đồ

                    case "RemoveAllMonsters":
                        {
                            if (paramsCount == 0)
                            {
                                // Danh sách đối tượng trong bản đồ
                                var objs =
                                    GameManager.MonsterMgr.GetObjectsByMap(player.CurrentMapCode, player.CurrentCopyMapID);
                                // Duyệt danh sách
                                foreach (var obj in objs)
                                {
                                    if (!obj.IsDead())
                                    {
                                        obj.MonsterZoneNode?.DestroyMonster(obj);
                                    }
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'RemoveAllMonsters' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Thiết lập cấp cường hóa cho trang bị

                    case "EquipEnhance":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            switch (paramsCount)
                            {
                                case 1:
                                    {
                                        var slot = int.Parse(para[1]);
                                        KTGMCommandManager.EquipEnhance(player, slot);
                                        break;
                                    }
                                case 2:
                                    {
                                        var targetID = int.Parse(para[1]);
                                        var slot = int.Parse(para[2]);
                                        KTGMCommandManager.EquipEnhance(targetID, slot);
                                        break;
                                    }
                                case 3:
                                    {
                                        var slot = int.Parse(para[1]);
                                        var fakeIsSuccess = int.Parse(para[2]);
                                        var fakePercent = int.Parse(para[3]);
                                        FakeEquipEnhance(player, player.RoleID, slot, fakeIsSuccess, fakePercent);
                                        break;
                                    }
                                case 4:
                                    {
                                        var target = int.Parse(para[1]);
                                        var slot = int.Parse(para[2]);
                                        var fakeIsSuccess = int.Parse(para[3]);
                                        var fakePercent = int.Parse(para[4]);
                                        FakeEquipEnhance(player, target, slot, fakeIsSuccess, fakePercent);
                                        break;
                                    }
                            }

                            break;
                        }

                    #endregion Thiết lập cấp cường hóa cho trang bị
                    #region Thiết lập cường hóa thêm kỹ năng cho trang bị BN HK
                    case "EquipEnhanceArtifact":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            switch (paramsCount)
                            {
                                case 1:
                                    {
                                        var slot = int.Parse(para[1]);
                                        KTGMCommandManager.EquipEnhanceArtifact(player, slot);
                                        break;
                                    }
                            }
                            break;
                        }
                    #endregion
                    case "EquipStrong":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            switch (paramsCount)
                            {
                                case 2:
                                    {
                                        var slot = int.Parse(para[1]);
                                        var strong = int.Parse(para[2]);
                                        KTGMCommandManager.EquipStrong(player.RoleID, slot, strong);
                                        break;
                                    }
                                case 3:
                                    {
                                        var targetID = int.Parse(para[1]);
                                        var slot = int.Parse(para[2]);
                                        var strong = int.Parse(para[3]);
                                        KTGMCommandManager.EquipStrong(targetID, slot, strong);
                                        break;
                                    }
                            }

                            break;
                        }


                    #region Thêm tiền

                    case "AddMoney":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            if (paramsCount == 2)
                            {
                                var type = int.Parse(para[1]);
                                var amount = int.Parse(para[2]);
                                KTGMCommandManager.AddMoney(player, type, amount);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var type = int.Parse(para[2]);
                                var amount = int.Parse(para[3]);
                                KTGMCommandManager.AddMoney(targetID, type, amount);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddMoney' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ResetGuildRecoreMoney":
                        {
                            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.TotalGuildMoneyAdd, 0, true);
                            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.TotalGuildMoneyWithDraw, 0, true);

                            break;
                        }

                    case "ResetLuckySpin":
                        {
                            if (paramsCount == 1)
                            {
                                int targetId = int.Parse(para[1]);
                                var target = GameManager.ClientMgr.FindClient(targetId);
                                if (target != null)
                                {
                                    target.ResetLuckySpinData();
                                    PlayerManager.ShowNotification(player, $"Đã xóa dữ liệu vòng quay may mắn của người chơi {target.RoleName}");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, $"Không tìm thấy dữ liệu người chơi");
                                }
                            }
                            else
                                PlayerManager.ShowNotification(player, $"Sai cú pháp");
                            break;
                        }

                    case "CheckDailyCharge":
                        {
                            if (paramsCount == 0)
                            {
                                var record = KTGlobal.GetPlayerTodayChargeRecord(player, out _);
                                var str = string.Join(", ", record);
                                PlayerManager.ShowNotification(player, $"Hôm nay {player.RoleName}  đã nạp {str} xu!");
                            }
                            else if (paramsCount == 1)
                            {
                                var target = KTGMCommandManager.FindPlayer(para[1]);
                                if (target != null)
                                {
                                    var record = KTGlobal.GetPlayerTodayChargeRecord(target, out _);
                                    var str = string.Join(", ", record);
                                    PlayerManager.ShowNotification(player, $"Hôm nay {target.RoleName} đã nạp {str} xu!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Không tìm thấy dữ liệu.");
                                }
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Lệnh GM lỗi.");
                            }

                            break;
                        }
                    case "FakeBill":
                        {
                            // if (GameManager.IsLiveServer)
                            // {
                            //     PlayerManager.ShowNotification(player, "Không hỗ trợ.");
                            //     break;
                            // }
                            if (paramsCount == 1)
                            {
                                if (!int.TryParse(para[1], out var value))
                                {
                                    PlayerManager.ShowNotification(player, "Số tiền không hợp lệ!");
                                    break;
                                }

                                var listValid = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };
                                if (!listValid.Contains(value))
                                {
                                    PlayerManager.ShowNotification(player, "Số tiền không hợp lệ!");
                                    break;
                                }

                                var result = Global.SendToDB((int)TCPGameServerCmds.CMD_DB_FAKE_BILL,
                                    new Tuple<int, int, int, string>(player.RoleID, value, GameManager.ServerId, ""),
                                    GameManager.ServerId);

                                if (result != null && result[0] == "0")
                                {
                                    PlayerManager.ShowNotification(player, "Thêm lệnh chuyển xu thành công!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Thêm lệnh chuyển xu thất bại!");
                                }
                            }
                            else if (paramsCount == 4)
                            {
                                if (!int.TryParse(para[1], out var value))
                                {
                                    PlayerManager.ShowNotification(player, "Số tiền không hợp lệ!");
                                    break;
                                }
                                if (!int.TryParse(para[2], out var rId))
                                {
                                    PlayerManager.ShowNotification(player, "Người nhận tiền không hợp lệ!");
                                    break;
                                }
                                if (rId == -1)
                                    rId = player.RoleID;


                                var listValid = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };
                                if (!listValid.Contains(value))
                                {
                                    PlayerManager.ShowNotification(player, "Số tiền không hợp lệ!");
                                    break;
                                }

                                var result = Global.SendToDB((int)TCPGameServerCmds.CMD_DB_FAKE_BILL,
                                    new Tuple<int, int, int, string>(rId, value, GameManager.ServerId, $"{para[3]} {para[4]}"),
                                    GameManager.ServerId);

                                if (result != null && result[0] == "0")
                                {
                                    PlayerManager.ShowNotification(player, "Thêm lệnh chuyển xu thành công!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Thêm lệnh chuyển xu thất bại!");
                                }
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Tham số không hợp lệ!");
                            }

                            break;
                        }

                    #endregion Thêm tiền

                    #region Thêm vật phẩm rơi ở MAP

                    case "AddDropItem":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            if (paramsCount == 1)
                            {
                                var monsterID = int.Parse(para[1]);
                                KTGMCommandManager.AddDropItem(player, monsterID);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var monsterID = int.Parse(para[2]);
                                KTGMCommandManager.AddDropItem(targetID, monsterID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddDropItem' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Thêm vật phẩm rơi ở MAP

                    #region Hoạt động

                    case "StartActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var activityID = int.Parse(para[1]);
                                KTGMCommandManager.StartActivity(activityID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'StartActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "StopActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var activityID = int.Parse(para[1]);
                                KTGMCommandManager.StopActivity(activityID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'StopActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Hoạt động

                    #region Sát khí

                    case "SetPKValue":
                        {
                            if (paramsCount == 1)
                            {
                                var value = int.Parse(para[1]);
                                KTGMCommandManager.SetPKValue(player, value);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var value = int.Parse(para[2]);
                                KTGMCommandManager.SetPKValue(targetID, value);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetPKValue' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Sát khí

                    #region Kỹ năng sống

                    case "ResetLifeSkillLevelAndExp":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetLifeSkillLevelAndExp(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetLifeSkillLevelAndExp(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetLifeSkillLevelAndExp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SetLifeSkillLevelAndExp":
                        {
                            if (paramsCount == 3)
                            {
                                var lifeSkillID = int.Parse(para[1]);
                                var level = int.Parse(para[2]);
                                var exp = int.Parse(para[3]);
                                KTGMCommandManager.SetLifeSkillLevelAndExp(player, lifeSkillID, level, exp);
                            }
                            else if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var lifeSkillID = int.Parse(para[2]);
                                var level = int.Parse(para[3]);
                                var exp = int.Parse(para[4]);
                                KTGMCommandManager.SetLifeSkillLevelAndExp(targetID, lifeSkillID, level, exp);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetLifeSkillLevelAndExp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "AddGatherMakePoint":
                        {
                            if (paramsCount == 2)
                            {
                                var gatherPoint = int.Parse(para[1]);
                                var makePoint = int.Parse(para[2]);
                                KTGMCommandManager.AddGatherMakePoint(player, gatherPoint, makePoint);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var gatherPoint = int.Parse(para[2]);
                                var makePoint = int.Parse(para[3]);
                                KTGMCommandManager.AddGatherMakePoint(targetID, gatherPoint, makePoint);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddGatherMakePoint' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Kỹ năng sống

                    #region Danh hiệu

                    case "SetTempTitle":
                        {
                            if (paramsCount == 1)
                            {
                                var title = para[1];
                                KTGMCommandManager.SetTempTitle(player, title.Replace("_", " "));
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2];
                                KTGMCommandManager.SetTempTitle(targetID, title.Replace("_", " "));
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetTempTitle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Danh hiệu

                    #region Thư

                    case "SendSystemMail":
                        {
                            if (GameManager.IsLiveServer)
                                break;
                            LogManager.WriteLog(LogTypes.Mail, $"[GMCmd]-[{player.RoleID}][{player.RoleName}]-[{command}]");

                            if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(targetID, title, content, "", 0, 0)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(player.RoleID, title, content, "", 0, 0)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                            }
                            else if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var items = para[4];
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(targetID, title, content, items, 0, 0)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(player.RoleID, title, content, items, 0, 0)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                            }
                            else if (paramsCount == 5)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var boundMoney = int.Parse(para[4]);
                                var boundToken = int.Parse(para[5]);
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(targetID, title, content, "", boundMoney,
                                            boundToken)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(player.RoleID, title, content, "", boundMoney,
                                            boundToken)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                            }
                            else if (paramsCount == 6)
                            {
                                var targetID = int.Parse(para[1]);
                                var title = para[2].Replace('_', ' ');
                                var content = para[3].Replace('_', ' ');
                                var items = para[4];
                                var boundMoney = int.Parse(para[5]);
                                var boundToken = int.Parse(para[6]);
                                // Nếu có mục tiêu
                                if (targetID != -1)
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(targetID, title, content, items, boundMoney,
                                            boundToken)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player,
                                        KTGMCommandManager.SendSystemMail(player.RoleID, title, content, items, boundMoney,
                                            boundToken)
                                            ? $"Gửi thư tới {targetID} thành công."
                                            : $"Gửi thư tới {targetID} thất bại.");
                                }
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SendSystemMail' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Thư

                    #region Kinh nghiệm và cấp độ

                    case "AddExp":
                        {
                            if (paramsCount == 1)
                            {
                                var exp = int.Parse(para[1]);
                                KTGMCommandManager.AddExp(player, exp);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var exp = int.Parse(para[2]);
                                KTGMCommandManager.AddExp(targetID, exp);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddExp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SetLevel":
                        {
                            if (paramsCount == 1)
                            {
                                var level = int.Parse(para[1]);
                                KTGMCommandManager.SetLevel(player, level);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var level = int.Parse(para[2]);
                                KTGMCommandManager.SetLevel(targetID, level);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetLevel' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Kinh nghiệm

                    #region Vào phái

                    case "JoinFaction":
                        {
                            if (paramsCount == 1)
                            {
                                var factionID = int.Parse(para[1]);
                                KTGMCommandManager.JoinFaction(player, factionID);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var factionID = int.Parse(para[2]);
                                KTGMCommandManager.JoinFaction(targetID, factionID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'JoinFaction' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Vào phái

                    #region Battle

                    case "StopServer":
                        {
                            Program.Exit();
                        }
                        break;

                    case "CreateFireCamp":
                        {
                            if (paramsCount == 0)
                            {
                                var Map = GameManager.MapMgr.DictMaps[player.MapCode];
                                MonsterDeadHelper.CreateFireCampStep1(Map, player.PosX, player.PosY, player,
                                    player.CopyMapID, 120);
                            }
                            else if (paramsCount == 2)
                            {
                                var posX = int.Parse(para[1]);
                                var posY = int.Parse(para[2]);
                                var Map = GameManager.MapMgr.DictMaps[player.MapCode];
                                MonsterDeadHelper.CreateFireCampStep1(Map, posX, posY, player, player.CopyMapID, 120);
                            }
                        }
                        break;

                    case "SetMainTask":
                        {
                            if (paramsCount == 1)
                            {
                                var State = int.Parse(para[1]);

                                ProcessTask.GMSetMainTaskID(player, State);
                            }
                            else if (paramsCount == 2)
                            {
                                var otherRoleName = para[1];

                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);

                                var otherClient = GameManager.ClientMgr.FindClient(roleID);

                                if (null != otherClient)
                                {
                                    var State = int.Parse(para[2]);

                                    ProcessTask.GMSetMainTaskID(otherClient, State);
                                }
                            }
                        }
                        break;

                    case "ResetBVD":
                        {
                            if (paramsCount == 1)
                            {
                                var otherRoleName = para[1];

                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);

                                var otherClient = GameManager.ClientMgr.FindClient(roleID);

                                if (otherClient != null)
                                {
                                    otherClient.CurenQuestIDBVD = -1;
                                    otherClient.CanncelQuestBVD = 0;
                                    otherClient.QuestBVDStreakCount = 0;
                                    otherClient.QuestBVDTodayCount = 0;

                                    if (otherClient.TaskDataList != null)
                                    {
                                        // Đoạn này để tránh bug nếu mà nhiệm vụ hiện tại khác nhiệm vụ đã nhận trước đó
                                        foreach (var TaskArmy in otherClient.TaskDataList)
                                        {
                                            var _Task = TaskDailyArmyManager.getInstance()
                                                .GetTaskTemplate(TaskArmy.DoingTaskID);

                                            //Tức là đang có nhiệm vụ BVD đang nhận
                                            if (_Task != null && _Task.TaskClass == (int)TaskClasses.NghiaQuan)
                                            {
                                                TaskDailyArmyManager.getInstance().CancelTask(otherClient, TaskArmy.DbID,
                                                    TaskArmy.DoingTaskID);
                                            }
                                        }
                                    }

                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                            }
                        }
                        break;

                    case "ResetHaiTac":
                        {
                            if (paramsCount == 1)
                            {
                                var otherRoleName = para[1];

                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);

                                var otherClient = GameManager.ClientMgr.FindClient(roleID);

                                if (otherClient != null)
                                {
                                    PirateTaskManager.getInstance().SetQuestIDDay(otherClient, -1);
                                    PirateTaskManager.getInstance().SetNumQuestThisDay(otherClient, 0);

                                    if (otherClient.TaskDataList != null)
                                    {
                                        // Đoạn này để tránh bug nếu mà nhiệm vụ hiện tại khác nhiệm vụ đã nhận trước đó
                                        foreach (var TaskArmy in otherClient.TaskDataList)
                                        {
                                            var _Task = PirateTaskManager.getInstance()
                                                .GetTaskTemplate(TaskArmy.DoingTaskID);

                                            //Tức là đang có nhiệm vụ BVD đang nhận
                                            if (_Task != null && _Task.TaskClass == (int)TaskClasses.HaiTac)
                                            {
                                                PirateTaskManager.getInstance().CancelTask(otherClient, TaskArmy.DbID,
                                                    TaskArmy.DoingTaskID);
                                            }
                                        }
                                    }

                                    Global.ForceCloseClient(otherClient, "GMKICK");
                                }
                            }
                        }
                        break;

                    case "ResetThuongHoi":
                        {
                            if (paramsCount == 1)
                            {
                                var otherRoleName = para[1];

                                var roleID = RoleName2IDs.FindRoleIDByName(otherRoleName);

                                var otherClient = GameManager.ClientMgr.FindClient(roleID);

                                FirmTaskManager.getInstance().SetNumQuestThisWeek(otherClient, 0);
                            }
                        }
                        break;

                    case "BonusNumQuestThuongHoi":
                        {
                            if (paramsCount == 1)
                            {
                                var number = int.Parse(para[1]);
                                FirmTaskManager.getInstance().BonusNumQuestThisWeek(player, number);
                            }
                            else if (paramsCount == 3)
                            {
                                var number = int.Parse(para[1]);
                                var playerID = int.Parse(para[2]);
                                var otherPlayer = KTGMCommandManager.FindPlayer(playerID);
                                FirmTaskManager.getInstance().BonusNumQuestThisWeek(otherPlayer, number);
                            }

                        }
                        break;

                    case "BattleStateTest":
                        {
                            var State = int.Parse(para[1]);

                            var _State = new G2C_EventState();
                            _State.EventID = 50;
                            _State.State = State;

                            player.SendPacket<G2C_EventState>((int)TCPGameServerCmds.CMD_KT_EVENT_STATE, _State);

                            break;
                        }

                    //case "GuildWarStart":
                    //    {
                    //        GuidWarManager.getInstance().ChangeState(player);

                    //        break;
                    //    }

                    case "StartBattle":
                        {
                            var Level = int.Parse(para[1]);

                            if (Level < 1 || Level > 3)
                            {
                                PlayerManager.ShowNotification(player, "Cấp chiến trương không hợp lệ");
                            }
                            else
                            {
                                Battel_SonJin_Manager.ForceStartBattle(Level);
                            }

                            break;
                        }

                    case "StartFactionBattle":
                        {
                            FactionBattleManager.ForceStartBattle();
                        }
                        break;

                    case "EndFactionBattle":
                        {
                            FactionBattleManager.ForceEndBattle();
                        }
                        break;

                    case "StartLeagueBattle":
                        {
                            LeagueBattleManager.ForceStartBattle();
                        }
                        break;

                    case "EndLeagueBattle":
                        {
                            LeagueBattleManager.ForceEndBattle();
                        }
                        break;

                    case "GuildWarStart":
                        {
                            GuidWarManager.getInstance().BattleForceStart();
                        }
                        break;

                    case "GuildWarEnd":
                        {
                            GuidWarManager.getInstance().BattleForceEnd();
                        }
                        break;

                    case "DoneTask":
                        {
                            var TaskID = int.Parse(para[1]);

                            var findtask = player.TaskDataList.Where(x => x.DoingTaskID == TaskID).FirstOrDefault();
                            if (findtask != null)
                            {
                                findtask.DoingTaskVal1 = 100;

                                if (!GameManager.MapMgr.DictMaps.TryGetValue(player.MapCode, out var map))
                                {
                                    PlayerManager.ShowNotification(player, "Không lấy được bản đồ đang đứng");
                                }

                                var FindTask = TaskManager.getInstance().FindTaskById(TaskID);
                                if (FindTask != null)
                                {
                                    var npc = NPCGeneralManager.FindNPCBYRES(player.MapCode, player.CopyMapID,
                                        FindTask.DestNPC);

                                    if (npc != null)
                                    {
                                        if (FindTask.TaskClass == (int)TaskClasses.MainTask)
                                        {
                                            MainTaskManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                        else if (FindTask.TaskClass == (int)TaskClasses.HaiTac)
                                        {
                                            PirateTaskManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                        else if (FindTask.TaskClass == (int)TaskClasses.NghiaQuan)
                                        {
                                            TaskDailyArmyManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                    }
                                    else
                                    {
                                        npc = NPCGeneralManager.FindNPCBYRES(8, player.CopyMapID, 6786);

                                        if (FindTask.TaskClass == (int)TaskClasses.MainTask)
                                        {
                                            MainTaskManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                        else if (FindTask.TaskClass == (int)TaskClasses.HaiTac)
                                        {
                                            PirateTaskManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                        else if (FindTask.TaskClass == (int)TaskClasses.NghiaQuan)
                                        {
                                            TaskDailyArmyManager.getInstance().CompleteTask(map, npc, player, TaskID);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                    case "EndBattle":
                        {
                            var Level = int.Parse(para[1]);

                            if (Level < 1 || Level > 3)
                            {
                                PlayerManager.ShowNotification(player, "Cấp chiến trương không hợp lệ");
                            }
                            else
                            {
                                Battel_SonJin_Manager.ForceEndBattle(Level);
                            }

                            break;
                        }

                    case "KillEffectTest":
                        {
                            var COUNT = int.Parse(para[1]);

                            var _State = new G2C_KillStreak();

                            _State.KillNumber = COUNT;

                            player.SendPacket<G2C_KillStreak>((int)TCPGameServerCmds.CMD_KT_KILLSTREAK, _State);

                            break;
                        }

                    case "NotifyTest":
                        {
                            var _Notify = new G2C_EventNotification();
                            _Notify.EventName = "Tống Kim Công Báo";
                            _Notify.ShortDetail = "TIME|500";
                            _Notify.TotalInfo = new List<string>();

                            _Notify.TotalInfo.Add("Giết Địch : 100");
                            _Notify.TotalInfo.Add("Bị Giết : 100");

                            _Notify.TotalInfo.Add("Tích Lũy : 10550");

                            _Notify.TotalInfo.Add("Hạng Hiện Tại  : 1");

                            player.SendPacket<G2C_EventNotification>((int)TCPGameServerCmds.CMD_KT_EVENT_NOTIFICATION,
                                _Notify);

                            break;
                        }

                    case "CheckSigNet":
                        {
                            var TotalBS = int.Parse(para[1]);

                            var LevelStart = int.Parse(para[2]);

                            var ExpStart = int.Parse(para[3]);

                            ItemEnhance.Caclulation(TotalBS, LevelStart, ExpStart);

                            break;
                        }

                    #endregion Battle

                    #region LIENSV

                    case "GetLine":
                        {
                            var MapCode = int.Parse(para[1]);

                            try
                            {
                                var list =
                                    YongZheZhanChangClient.getInstance().GetKuaFuLineDataList(MapCode) as
                                        List<KuaFuLineData>;

                                Console.WriteLine(list);

                                player.SendPacket((int)(TCPGameServerCmds.CMD_SPR_KUAFU_MAP_INFO), list);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                            break;
                        }

                    case "TryEnter":
                        {
                            var MapCode = int.Parse(para[1]);
                            var Lines = int.Parse(para[2]);

                            try
                            {
                                var cmdParams = new string[6];
                                cmdParams[0] = MapCode + "";
                                cmdParams[1] = Lines + "";
                                cmdParams[3] = para[3];
                                cmdParams[4] = para[4];
                                cmdParams[5] = para[5];
                                KuaFuMapManager.getInstance().ProcessKuaFuMapEnterCmd(player,
                                    (int)(TCPGameServerCmds.CMD_SPR_KUAFU_MAP_ENTER), null, cmdParams);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                            break;
                        }

                    #endregion LIENSV

                    #region Kiểm tra thông tin hệ thống

                    case "CheckFireCamp":
                        {
                            try
                            {
                                PlayerManager.ShowNotification(player,
                                    $"Monster Died: {MonsterDeadHelper.TotalMonsterDie}. " +
                                    $"Elite Spawned: {MonsterDeadHelper.TotalEliteMonsterSpawned}/{MonsterDeadHelper.TotalAttempToSpawnElite}. " +
                                    $"Campfire Dropped: {MonsterDeadHelper.TotalFireCampDropped} ");
                            }
                            catch (Exception ex)
                            {
                                // ignored
                            }

                            break;
                        }

                    case "CheckCcu":
                        {
                            try
                            {
                                PlayerManager.ShowNotification(player,
                                    "CCU ONLINE: " + GameManager.ClientMgr.GetClientCount());
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                            break;
                        }
                    case "CheckCcu2":
                        {
                            try
                            {
                                var dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GETTOTALONLINENUM,
                                    string.Format("{0}", player.RoleID), GameManager.LocalServerId);
                                if (null == dbFields || dbFields.Length < 1)
                                {
                                }
                                else
                                {
                                    var totalOnlineNum = Global.SafeConvertToInt32(dbFields[0]);
                                    PlayerManager.ShowNotification(player, "CCU ONLINE 2: " + totalOnlineNum);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                            break;
                        }

                    #endregion Kiểm tra thông tin hệ thống

                    #region Tần Lăng

                    case "GetEmperorTombData":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.GetEmperorTombData(player, player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.GetEmperorTombData(player, targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'GetEmperorTombData' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ResetEmperorTombTimeLeft":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetEmperorTombTimeLeft(player.RoleID);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetEmperorTombTimeLeft(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetEmperorTombTimeLeft' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "EmperorTomb_CreateBossTest":
                        {
                            if (paramsCount == 0)
                            {
                                // Hướng quay
                                var dir = KiemThe.Entities.Direction.NONE;
                                // Mức máu
                                var hp = 1000000;
                                // Ngũ hành
                                var series = KE_SERIES_TYPE.series_none;

                                // Tạo Boss
                                GameManager.MonsterZoneMgr.AddDynamicMonsters(player.MapCode, 6918, -1, 1, player.PosX,
                                    player.PosY, "", "", hp, 100, dir, series, MonsterAIType.Boss, -1, -1, 100000, null,
                                    (boss) =>
                                    {
                                        // Thực thi sự kiện Tick
                                        boss.OnTick = () =>
                                        {
                                            // % máu hiện tại
                                            var nHPPercent = (int)(boss.m_CurrentLife / (float)boss.m_CurrentLifeMax * 100);
                                            // Nếu còn trên 50% máu
                                            if (nHPPercent >= 50)
                                            {
                                                // Miễn dịch sát thương ngoại
                                                boss.m_ImmuneToPhysicDamage = true;
                                                boss.m_ImmuneToMagicDamage = false;

                                                PlayerManager.ShowNotification(player, "Immune to physic");
                                            }
                                            // Nếu còn dưới 50% máu
                                            else
                                            {
                                                // Miễn dịch sát thương nội
                                                boss.m_ImmuneToMagicDamage = true;
                                                boss.m_ImmuneToPhysicDamage = false;

                                                PlayerManager.ShowNotification(player, "Immune to magic");
                                            }
                                        };
                                    }, 65535, null, null);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'EmperorTomb_CreateBossTest' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Đoán Hoa Đăng

                    case "ResetKnowledgeChallengeQuestions":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetKnowledgeChallengeQuestions(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetKnowledgeChallengeQuestions(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetKnowledgeChallengeQuestions' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Bí cảnh

                    #region Tiêu Dao Cốc

                    case "ResetXoYo":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetCopySceneEnterTimes(player, AcitvityRecore.XoYo);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetCopySceneEnterTimes(targetID, AcitvityRecore.XoYo);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetXoYo' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "AddXoYoMonthStoragePoint":
                        {
                            if (paramsCount == 1)
                            {
                                var nPoint = int.Parse(para[1]);
                                KTGMCommandManager.AddXoYoMonthStoragePoint(player, nPoint);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var nPoint = int.Parse(para[2]);
                                KTGMCommandManager.AddXoYoMonthStoragePoint(targetID, nPoint);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddXoYoMonthStoragePoint' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "GetXoYoMonthStoragePoint":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.GetXoYoMonthStoragePoint(player);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.GetXoYoMonthStoragePoint(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'GetXoYoMonthStoragePoint' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Tiêu Dao Cốc

                    #region Bí cảnh

                    case "ResetMiJing":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetCopySceneEnterTimes(player, AcitvityRecore.MiJing);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetCopySceneEnterTimes(targetID, AcitvityRecore.MiJing);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetMiJing' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Bí cảnh

                    #region Du Long Các

                    case "ResetYouLong":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetCopySceneEnterTimes(player, AcitvityRecore.YouLong);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetCopySceneEnterTimes(targetID, AcitvityRecore.YouLong);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetYouLong' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "SeeYouLong":
                        {
                            if (paramsCount == 0)
                            {
                                int todayPassedStages = player.GetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily);
                                if (todayPassedStages == -1)
                                {
                                    todayPassedStages = 0;
                                }
                                int thisMonthPassedStages = player.GetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount);
                                if (thisMonthPassedStages == -1)
                                {
                                    thisMonthPassedStages = 0;
                                }

                                var nServerPassed = YouLong.UpdateServerTotalPassedByValue(0);
                                PlayerManager.ShowNotification(player, $"Ngày = {todayPassedStages}\nTháng = {thisMonthPassedStages}\nServer = {nServerPassed}");
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetYouLong' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "AddYouLong":
                        {
                            if (paramsCount == 1)
                            {
                                var youlongValue = int.Parse(para[1]);
                                int todayPassedStages = player.GetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily);
                                if (todayPassedStages == -1)
                                {
                                    todayPassedStages = 0;
                                }
                                todayPassedStages += youlongValue;
                                player.SetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily, todayPassedStages);


                                int thisMonthPassedStages = player.GetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount);
                                if (thisMonthPassedStages == -1)
                                {
                                    thisMonthPassedStages = 0;
                                }
                                thisMonthPassedStages += youlongValue;
                                player.SetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount, thisMonthPassedStages);

                                YouLong.UpdateServerTotalPassedByValue(youlongValue);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddYouLong' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    #endregion Du Long Các

                    #region Võ lâm liên đấu

                    case "StartTeamBattle":
                        {
                            if (paramsCount == 0)
                            {
                                TeamBattle_ActivityScript.BeginBattle();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'StartTeamBattle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ArrangeTeamBattleFinalRound":
                        {
                            if (paramsCount == 0)
                            {
                                TeamBattle_ActivityScript.ArrangeAndIncreaseStageToTopTeamToTheFinalRound();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ArrangeTeamBattleFinalRound' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ArrangeTeamBattlePlayersRank":
                        {
                            if (paramsCount == 0)
                            {
                                TeamBattle_ActivityScript.ArrangePlayersRank();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ArrangeTeamBattlePlayersRank' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ArrangeTeamBattlePlayersRankAndUpdateAwardsStateToAllTeams":
                        {
                            if (paramsCount == 1)
                            {
                                var state = int.Parse(para[1]);
                                TeamBattle_ActivityScript.ArrangePlayersRankAndUpdateAllTeamsAwardState(state == 1);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ArrangeTeamBattlePlayersRankAndUpdateAwardsStateToAllTeams' Failed...\n" +
                                    "Invalid param.");
                            }

                            break;
                        }

                    case "UpdateTeamBattleAwardState":
                        {
                            if (paramsCount == 1)
                            {
                                var state = int.Parse(para[1]);
                                KTGMCommandManager.SetTeamBattleAwardState(player, state);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var state = int.Parse(para[2]);
                                KTGMCommandManager.SetTeamBattleAwardState(targetID, state);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'UpdateTeamBattleAwardState' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ResetTeamBattleTeamData":
                        {
                            if (paramsCount == 0)
                            {
                                TeamBattle_ActivityScript.ClearTeamsData();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetTeamBattleTeamData' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "ReloadTeamBattle":
                        {
                            if (paramsCount == 0)
                            {
                                TeamBattle.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadTeamBattle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion

                    #region Uy danh

                    case "AddPrestige":
                        {
                            if (paramsCount == 1)
                            {
                                var value = int.Parse(para[1]);
                                KTGMCommandManager.AddPrestige(player, value);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var value = int.Parse(para[2]);
                                KTGMCommandManager.AddPrestige(targetID, value);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddPrestige' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Uy danh

                    #region Danh vọng

                    case "AddRepute":
                        {
                            if (paramsCount == 2)
                            {
                                var reputeID = int.Parse(para[1]);
                                var value = int.Parse(para[2]);
                                KTGMCommandManager.AddRepute(player, reputeID, value);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var reputeID = int.Parse(para[2]);
                                var value = int.Parse(para[3]);
                                KTGMCommandManager.AddRepute(targetID, reputeID, value);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddRepute' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SetRetupe":
                        {
                            if (paramsCount == 4)
                            {
                                var targetID = int.Parse(para[1]);
                                var reputeID = int.Parse(para[2]);
                                var value = int.Parse(para[3]);
                                var exp = int.Parse(para[4]);


                                var target = KTGMCommandManager.FindPlayer(targetID);

                                KTGlobal.SetRepute(target, reputeID, value, exp);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddRepute' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Danh vọng

                    #region Xóa túi đồ bản thân

                    case "ClearBag":
                        {
                            List<GoodsData> itemGDs;
                            lock (player.GoodsDataList)
                            {
                                itemGDs = player.GoodsDataList.Where(x => x.Using < 0 && !x.IsPet()).ToList();
                            }

                            PlayerManager.ResolveRemoveItems(player, itemGDs, true);
                        }
                        break;

                    #endregion Xóa túi đồ bản thân

                    #region Thay đổi danh hiệu cá nhân

                    case "ModRoleTitle":
                        {
                            if (paramsCount == 2)
                            {
                                var method = int.Parse(para[1]);
                                var titleID = int.Parse(para[2]);
                                KTGMCommandManager.ModRoleTitle(player, method, titleID);
                            }
                            else if (paramsCount == 3)
                            {
                                var targetID = int.Parse(para[1]);
                                var method = int.Parse(para[2]);
                                var titleID = int.Parse(para[3]);
                                KTGMCommandManager.ModRoleTitle(targetID, method, titleID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ModRoleTitle' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Thay đổi danh hiệu cá nhân

                    #region Tu Luyện Châu

                    case "SetXiuLianZhu_TimeLeft":
                        {
                            if (paramsCount == 1)
                            {
                                var hour10 = int.Parse(para[1]);
                                KTGMCommandManager.SetXiuLianZhu_TimeLeft(player, hour10);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var hour10 = int.Parse(para[2]);
                                KTGMCommandManager.SetXiuLianZhu_TimeLeft(targetID, hour10);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetXiuLianZhu_TimeLeft' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "AddXiuLianZhu_Exp":
                        {
                            if (paramsCount == 1)
                            {
                                var exp = int.Parse(para[1]);
                                KTGMCommandManager.AddXiuLianZhu_Exp(player, exp);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var exp = int.Parse(para[2]);
                                KTGMCommandManager.AddXiuLianZhu_Exp(targetID, exp);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddXiuLianZhu_Exp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SetXiuLianZhu_Exp":
                        {
                            if (paramsCount == 1)
                            {
                                var exp = int.Parse(para[1]);
                                KTGMCommandManager.SetXiuLianZhu_Exp(player, exp);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var exp = int.Parse(para[2]);
                                KTGMCommandManager.SetXiuLianZhu_Exp(targetID, exp);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetXiuLianZhu_Exp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Chúc phúc

                    #region Chúc phúc

                    case "ResetPray":
                        {
                            player.LastPrayResult.Clear();
                            player.LastPrayResult = null;
                            player.SavePrayDataToDB();
                            break;
                        }
                    case "SetPrayTimes":
                        {
                            if (paramsCount == 1)
                            {
                                var value = int.Parse(para[1]);
                                KTGMCommandManager.SetPrayTimes(player, value);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var value = int.Parse(para[2]);
                                KTGMCommandManager.SetPrayTimes(targetID, value);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'SetPrayTimes' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion Chúc phúc

                    #region Sự kiện đặc biệt

                    case "ReloadSpecialEvent":
                        {
                            if (paramsCount == 0)
                            {
                                SpecialEvent.Init();
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ReloadSpecialEvent' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    #endregion


                    #region Set lại chức vụ

                    case "SetRankGiaToc":
                        {
                            var targetID = int.Parse(para[1]);
                            var Rank = int.Parse(para[2]);
                            var client = GameManager.ClientMgr.FindClient(targetID);

                            if (client != null)
                            {
                                var CMDBUILD = client.RoleID + ":" + client.FamilyID + ":" + Rank + ":" + -1;

                                var result = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_FAMILY_CHANGE_RANK,
                                    CMDBUILD, client.ServerId);

                                var Status = Int32.Parse(result[0]);

                                if (Status > 0)
                                {
                                    client.FamilyRank = Rank;

                                    KT_TCPHandler.NotifyOthersMyTitleChanged(client);

                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(client);


                                    // PlayerManager.ShowMessageBox(client, "Thông báo", "Chuyển giao chức vụ thành công");

                                    client.SendPacket((int)TCPGameServerCmds.CMD_KT_FAMILY_CHANGE_RANK,
                                        string.Format("{0}:{1}", client.RoleID, client.FamilyRank));
                                }
                            }

                            break;
                        }

                    case "SetRankBangHoi":
                        {
                            var targetID = int.Parse(para[1]);
                            var Rank = int.Parse(para[2]);
                            var client = GameManager.ClientMgr.FindClient(targetID);

                            if (client != null)
                            {
                                var CMDBUILD = client.RoleID + ":" + client.GuildID + ":" + Rank;

                                var ByteSendToDB = Encoding.ASCII.GetBytes(CMDBUILD);

                                var result = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_GUILD_CHANGERANK,
                                    CMDBUILD, client.ServerId);

                                var Status = Int32.Parse(result[0]);

                                if (Status > 0)
                                {
                                    client.GuildRank = Rank;

                                    // Thông báo danh hiệu thay đổi
                                    KT_TCPHandler.NotifyOthersMyTitleChanged(client);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(client);

                                    var responseData = string.Format("{0}", client.GuildRank);
                                    client.SendPacket((int)TCPGameServerCmds.CMD_KT_GUILD_CHANGERANK, responseData);
                                }
                            }

                            break;
                        }

                    #endregion

                    #region Hoạt động hằng ngày

                    case "ResetAllActivity":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetAllActivity(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetAllActivity(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetMissonActivity":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetMission(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetMission(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetGiftActivityDay":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetGiftActivityDay(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetGiftActivityDay(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetGiftActivityMonth":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.ResetGiftActivityMonth(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.ResetGiftActivityMonth(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "SetScoreDayActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var Score = int.Parse(para[1]);
                                KTGMCommandManager.SetScoreDayActivity(player, Score);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var Score = int.Parse(para[2]);
                                var client = GameManager.ClientMgr.FindClient(targetID);
                                KTGMCommandManager.SetScoreDayActivity(client, Score);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "SetScoreMonthActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var Score = int.Parse(para[1]);
                                KTGMCommandManager.SetScoreMonthActivity(player, Score);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var Score = int.Parse(para[2]);
                                var client = GameManager.ClientMgr.FindClient(targetID);
                                KTGMCommandManager.SetScoreMonthActivity(client, Score);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "AddScoreDayActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var Score = int.Parse(para[1]);
                                KTGMCommandManager.AddScoreDayActivity(player, Score);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var Score = int.Parse(para[2]);
                                var client = GameManager.ClientMgr.FindClient(targetID);
                                KTGMCommandManager.AddScoreDayActivity(client, Score);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "AddScoreMonthActivity":
                        {
                            if (paramsCount == 1)
                            {
                                var Score = int.Parse(para[1]);
                                KTGMCommandManager.AddScoreMonthActivity(player, Score);
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var Score = int.Parse(para[2]);
                                var client = GameManager.ClientMgr.FindClient(targetID);
                                KTGMCommandManager.AddScoreMonthActivity(client, Score);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "FakeNewDayActivity":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.FakeNewDay(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.FakeNewDay(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "FakeNewMonthActivity":
                        {
                            if (paramsCount == 0)
                            {
                                KTGMCommandManager.FakeNewMonth(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                KTGMCommandManager.FakeNewMonth(targetID);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetAllActivity' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetVip":
                        {
                            player.SetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore, -1);
                            break;
                        }

                    #endregion

                    #region Nhiệm vụ phụ tuyến

                    case "ResetAllPT":
                        if (paramsCount == 0)
                        {
                            SideTaskManager.getInstance().ResetAllMission(player);
                        }

                        break;
                    case "ResetPT":
                        if (paramsCount == 1)
                        {
                            var targetID = int.Parse(para[1]);
                            SideTaskManager.getInstance().ResetMission(player, targetID);
                        }

                        break;

                    case "CompleteAllPT":
                        if (paramsCount == 0)
                        {
                            SideTaskManager.getInstance().CompleteAllMission(player);
                        }

                        break;
                    case "CompletePT":
                        if (paramsCount == 1)
                        {
                            var targetID = int.Parse(para[1]);
                            SideTaskManager.getInstance().CompleteMission(player, targetID);
                        }

                        break;
                    case "AddValueTaskPT":
                        if (paramsCount == 2)
                        {
                            var targetID = int.Parse(para[1]);
                            var value = int.Parse(para[2]);
                            SideTaskManager.getInstance().AddValueMission(player, targetID, value);
                        }

                        break;

                    #endregion

                    #region Xổ số sự kiện

                    case "AddNumberLottery":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            LotteryManager.AddNumberLottery(player, number);
                            LogManager.WriteLog(LogTypes.Lottery, $"{player.RoleID} get {number}");
                        }

                        break;
                    case "AddRandomLottery":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            LotteryManager.AddRandomNumber(player, number);
                        }

                        break;

                    case "SetNumberWinLottery":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            LotteryManager.SetNumberWin(number);
                        }

                        break;
                    case "FakeRecordLottery":
                        if (paramsCount == 0)
                        {
                            LotteryManager.FakeRecords();
                        }

                        break;

                    #endregion

                    #region Đồng Hành

                    case "AddExpAllPet":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            PetManager.AddExpAllPet(player, number);
                        }

                        break;
                    case "AddExpPet":
                        if (paramsCount == 2)
                        {
                            var number = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            PetManager.AddExpAllPet(player, number, id);
                        }

                        break;

                    case "AddLNAllPet":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            PetManager.AddLinhNgoAllPet(player, number);
                        }

                        break;
                    case "AddLNPet":
                        if (paramsCount == 2)
                        {
                            var number = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            PetManager.AddLinhNgoAllPet(player, number, id);
                        }

                        break;
                    case "AddTMAllPet":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            PetManager.AddThanMatAllPet(player, number);
                        }

                        break;
                    case "AddTMPet":
                        if (paramsCount == 2)
                        {
                            var number = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            PetManager.AddThanMatAllPet(player, number, id);
                        }

                        break;
                    case "LevelUpAllPet":
                        if (paramsCount == 1)
                        {
                            var number = int.Parse(para[1]);
                            PetManager.LevelUpAllPet(player, number);
                        }

                        break;
                    case "LevelUpPet":
                        if (paramsCount == 2)
                        {
                            var number = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            PetManager.LevelUpAllPet(player, number, id);
                        }

                        break;
                    case "ClearPet":
                        {
                            List<GoodsData> itemGDs;
                            lock (player.GoodsDataList)
                            {
                                itemGDs = player.GoodsDataList.Where(x => x.Using < 0 && x.IsPet()).ToList();
                            }

                            PlayerManager.ResolveRemoveItems(player, itemGDs, true);
                        }
                        break;
                    case "MinusSkillPet":
                        if (paramsCount == 2)
                        {
                            var playerID = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            var result = PetManager.MinusSkillPet(FindPlayer(playerID), id);
                            if (result == 1)
                            {
                                PlayerManager.ShowNotification(player, $"Bạn đồng hành đã hạ cấp kỹ năng ngẫu nhiên thành công!");
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, $"Bạn đồng hành đã hạ cấp kỹ năng ngẫu nhiên thành công!");
                            }
                        }
                        break;
                    case "LevelUpSkillPet":
                        if (paramsCount == 2)
                        {
                            var playerID = int.Parse(para[1]);
                            var id = int.Parse(para[2]);
                            var result = PetManager.LevelUpSkillPet(FindPlayer(playerID), id);
                            if (result == 1)
                            {
                                PlayerManager.ShowNotification(player, $"Bạn đồng hành đã thăng cấp kỹ năng ngẫu nhiên thành công!");
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, $"Bạn đồng hành đã thăng cấp kỹ năng ngẫu nhiên thành công!");
                            }
                        }
                        break;

                    #endregion

                    #region Event

                    case "SetEventDropRate":
                        {
                            if (double.TryParse(para[1], out var rate))
                            {
                                KTLuaLib_System.SetEventDropRate(rate);
                            }

                            break;
                        }
                    case "ShopDoanKet":
                        {
                            if (int.TryParse(para[1], out var itemID) && int.TryParse(para[2], out var addition))
                            {
                                UnionShop.UpdateTotalSell(player, itemID, addition);
                                PlayerManager.ShowNotification(player, "Cập nhật số lượng thành công");
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Cập nhật lỗi. Vui lòng thử lại.");
                            }

                            break;
                        }

                    case "DaoTacNPC":
                        {
                            var mapCode = player.MapCode;
                            var positionX = player.PosX;
                            var positionY = player.PosY;

                            var npctag = $"DaoTacThangTu_{StaticRandom.GetRandom(100000000)}";

                            NPCGeneralManager.AddNewNPC(mapCode, -1, 20199, positionX, positionY, "Đạo tặc Sinh nhật", "",
                                Entities.Direction.DOWN, 232, npctag, (npc) =>
                                {
                                    npc.MinimapName = "Sứ giả Hoa Đăng";
                                    npc.VisibleOnMinimap = true;
                                });
                            break;
                        }

                    case "DeleteDaoTacNPC":
                        {
                            NPCGeneralManager.RemoveNPCsByScriptID(232);
                            NPCGeneralManager.RemoveNPCsByScriptID(233);
                            Event_2024Apr.RemoveAllBoss();
                            break;
                        }



                    #endregion

                    case "AddBonusRemainPotentialPoint":
                        {
                            if (int.TryParse(para[1], out var point))
                            {
                                player.SetBonusRemainPotentialPoints(point);
                            }

                            break;
                        }

                    case "SlotMachineNextNumber":
                        {
                            if (int.TryParse(para[1], out var number))
                            {
                                if (number > 99999)
                                    PlayerManager.ShowNotification(player, "Invalid Number");
                                else
                                {
                                    player.SetValueOfDailyRecore((int)RecoreDef.SlotMachine_GMSetNextNumber, number);
                                    PlayerManager.ShowNotification(player, "Set Slot Machine Next Number: " + number);
                                }
                            }

                            break;
                        }
                    case "SetTowerTaskProgress":
                        {
                            if (int.TryParse(para[1], out var roleID) && int.TryParse(para[2], out var progress) && int.TryParse(para[3], out var taskID))
                            {
                                if (roleID == -1)
                                {
                                    roleID = player.RoleID;
                                }

                                FlowerTowerManager.Instance.SetTaskProgressByRoleID(roleID, taskID, progress);
                                PlayerManager.ShowNotification(player, "Set Tower Task Progress: " + progress + " for task ID: " + taskID + " of role ID: " + roleID);
                            }
                            else
                            {
                                PlayerManager.ShowNotification(player, "Invalid parameters");
                            }
                            break;
                        }
                    case "CheckInfoServer":
                        {
                            // so luong item trong server config
                            var numberItem = ItemManager.TotalItem.Count();
                            var content = "Số lượng Item: " + numberItem;
                            //so luong randombox trong server config
                            var numberRandomBox = ItemRandomBox._ConfigBox.Boxs.Count();
                            content += "\nSố lượng RandomBox: " + numberRandomBox;
                            // ngay cap nhat server.exe
                            var gameServerPath = "GameServer.exe";
                            content += "\nGamerServer.exe info:";
                            if (File.Exists(gameServerPath))
                            {
                                var lastModified = File.GetLastWriteTime(gameServerPath).ToString("dd/MM/yy HH:mm:ss");
                                content += "\n" + lastModified;
                                // lay md5
                                using (var md5 = MD5.Create())
                                {
                                    using (var stream = File.OpenRead(gameServerPath))
                                    {
                                        byte[] checksum = md5.ComputeHash(stream);
                                        var output = BitConverter.ToString(checksum).Replace("-", String.Empty).ToLower();
                                        content += "\n" + output;
                                    }
                                }
                            }
                            // ngay cap nhat dbGameServer
                            var dbServerPath = "../DBServer/GameDBServer.exe";
                            content += "\nDBGameServer info:";
                            if (File.Exists(dbServerPath))
                            {
                                var lastModified = File.GetLastWriteTime(dbServerPath).ToString("dd/MM/yy HH:mm:ss");
                                content += "\n" + lastModified;
                                // lay md5
                                using (var md5 = MD5.Create())
                                {
                                    using (var stream = File.OpenRead(dbServerPath))
                                    {
                                        byte[] checksum = md5.ComputeHash(stream);
                                        var output = BitConverter.ToString(checksum).Replace("-", String.Empty).ToLower();
                                        content += "\n" + output;
                                    }
                                }
                            }
                            KTGMCommandManager.SendSystemChat(content);
                            break;
                        }

                    case "CheckServerConfig":
                        {
                            // in ra tung gia tri trong file serverConfig.json
                            var ServerConfigPath_JSON = "ServerConfig.json";
                            var configStr = File.ReadAllText(ServerConfigPath_JSON);
                            KTGMCommandManager.SendSystemChat("ServerConfig Info:\n" + configStr);
                            break;
                        }
                    #region Bao Thach
                    case "CreateHole":
                        {
                            if (paramsCount == 1)
                            {
                                if (int.TryParse(para[1], out var id))
                                {
                                    int bagIndex1 = int.Parse(para[1]);
                                    var goods = player.GoodsDataList.Find(x => x.BagIndex == bagIndex1);
                                    if (goods == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy vật phẩm");
                                    }
                                    BaoThachManager.CreateItemHole(player, goods.Id, "GMCOMMAND");
                                }
                            }
                            break;
                        }
                    case "EquipBT":
                        {
                            if (paramsCount == 3)
                            {
                                try
                                {
                                    int bagIndex1 = int.Parse(para[1]);
                                    int bagIndex2 = int.Parse(para[2]);
                                    int idHole = int.Parse(para[3]);
                                    var goodsdata = player.GoodsDataList.Find(x => x.BagIndex == bagIndex1);
                                    if (goodsdata == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy vật phẩm");
                                        break;
                                    }

                                    var bt = player.GoodsDataList.Find(x => x.BagIndex == bagIndex2);
                                    if (bt == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy vật phẩm");
                                        break;
                                    }

                                    var result = BaoThachManager.Equip(player, goodsdata.Id, bt.Id,
                                        idHole, 1, "GMCOMMAND");
                                    player.GetPlayEquipBody().ClearAllEffecEquipBody();
                                    player.GetPlayEquipBody().AttackAllEquipBody();
                                    KTGMCommandManager.SendSystemChat("Kết quả: " + result);
                                }
                                catch (Exception es)
                                {
                                    KTGMCommandManager.SendSystemChat("Tính năng lỗi: " + es.Message);
                                }
                            }

                            break;
                        }
                    case "UnequipBT":
                        {
                            if (paramsCount == 2)
                            {
                                try
                                {
                                    int bagIndex1 = int.Parse(para[1]);
                                    int idHole = int.Parse(para[2]);
                                    var goodsdata = player.GoodsDataList.Find(x => x.BagIndex == bagIndex1);
                                    if (goodsdata == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy item");
                                        break;
                                    }
                                    var result = BaoThachManager.UnEquip(player, goodsdata.Id, idHole, "GMCOMMAND");
                                    player.GetPlayEquipBody().ClearAllEffecEquipBody();
                                    player.GetPlayEquipBody().AttackAllEquipBody();
                                    KTGMCommandManager.SendSystemChat("Kết quả: " + result);
                                }
                                catch (Exception es)
                                {
                                    KTGMCommandManager.SendSystemChat("Tính năng lỗi: " + es.Message);
                                }
                            }
                            break;
                        }
                    case "UpgradeBT":
                        {
                            if (paramsCount == 2)
                            {
                                try
                                {
                                    int goods_itemid = int.Parse(para[1]);
                                    int amount = int.Parse(para[2]);
                                    var result = BaoThachManager.Upgrade(player, goods_itemid, amount, "GMCOMMAND");
                                    KTGMCommandManager.SendSystemChat("Kết quả: " + result);
                                }
                                catch (Exception es)
                                {
                                    KTGMCommandManager.SendSystemChat("Tính năng lỗi: " + es.Message);
                                }
                            }
                            break;
                        }
                    case "ResetBT":
                        {
                            if (paramsCount == 2)
                            {
                                try
                                {
                                    int bagIndex1 = int.Parse(para[1]);
                                    int indexHole = int.Parse(para[2]);
                                    var goodsdata = player.GoodsDataList.Find(x => x.BagIndex == bagIndex1);
                                    if (goodsdata == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy item");
                                    }
                                    else
                                    {
                                        var result = BaoThachManager.ResetAndConfirm(player, goodsdata.Id, indexHole, "GMCOMMAND");
                                        player.GetPlayEquipBody().ClearAllEffecEquipBody();
                                        player.GetPlayEquipBody().AttackAllEquipBody();
                                        KTGMCommandManager.SendSystemChat("Kết quả: " + result);
                                    }
                                }
                                catch (Exception es)
                                {
                                    KTGMCommandManager.SendSystemChat("Tính năng lỗi: " + es.Message);
                                }
                            }
                            break;
                        }
                    case "GetHoleData":
                        {
                            if (paramsCount == 1)
                            {
                                try
                                {
                                    int bagIndex1 = int.Parse(para[1]);
                                    var goods_item = player.GoodsDataList.Find(x => x.BagIndex == bagIndex1);
                                    if (goods_item == null)
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm được item tương ứng");
                                        break;
                                    }

                                    if (goods_item.OtherParams == null || goods_item.OtherParams.Count == 0 ||
                                        !goods_item.OtherParams.ContainsKey(ItemPramenter.Hole))
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: Không tìm thấy hole data!");
                                    }
                                    else
                                    {
                                        KTGMCommandManager.SendSystemChat("Kết quả: " +
                                                                          goods_item.OtherParams[ItemPramenter.Hole]);
                                    }

                                }
                                catch (Exception es)
                                {
                                    KTGMCommandManager.SendSystemChat("Tính năng lỗi: " + es.Message);
                                }
                            }
                            break;

                        }
                    case "LogHoleDataByDbID":
                        {
                            if (paramsCount == 1 && int.TryParse(para[1], out var itemDbId))
                            {
                                var goods_item = Global.GetGoodsByDbID(player, itemDbId);
                                if (goods_item == null)
                                {
                                    KTGMCommandManager.SendSystemChat($"Kết quả: Không tìm được item DBID={itemDbId}");
                                    break;
                                }
                                var sb = new StringBuilder();
                                sb.AppendLine($"[GM][HoleDump][{player.ServerId}][{player.RoleID}] Role='{player.RoleName}' ItemDBID={goods_item.Id} GoodsID={goods_item.GoodsID} Name='{goods_item.ItemName}' BagIndex={goods_item.BagIndex} Binding={goods_item.Binding} Forge={goods_item.Forge_level}");

                                if (goods_item.OtherParams == null || !goods_item.OtherParams.TryGetValue(ItemPramenter.Hole, out var holeString) || string.IsNullOrEmpty(holeString))
                                {
                                    sb.AppendLine("HoleData=EMPTY");
                                    LogManager.WriteLog(LogTypes.BaoThach, sb.ToString());
                                    KTGMCommandManager.SendSystemChat($"Kết quả: HoleData rỗng (đã ghi log) DBID={goods_item.Id}");
                                    break;
                                }

                                sb.AppendLine("HoleDataRaw:");
                                sb.AppendLine(holeString);

                                var holeData = BaoThachUserData.GetFromStringData(holeString);
                                if (holeData == null)
                                {
                                    sb.AppendLine("HoleDataParsed=FAILED");
                                }
                                else
                                {
                                    sb.AppendLine($"Point={holeData.point} Holes={holeData.holes?.Count ?? 0}");
                                    if (holeData.holes != null)
                                    {
                                        for (int i = 0; i < holeData.holes.Count; i++)
                                        {
                                            var h = holeData.holes[i];
                                            if (h == null)
                                            {
                                                sb.AppendLine($"Hole[{i}]=null");
                                                continue;
                                            }

                                            sb.AppendLine($"Hole[{i}]: BT_GoodsID={h.itemGoodsId}, Type={h.type}, Key='{h.magicName}', Value={h.magicValue}");
                                        }
                                    }
                                }

                                LogManager.WriteLog(LogTypes.BaoThach, sb.ToString());
                                KTGMCommandManager.SendSystemChat($"Kết quả: Đã ghi log HoleData DBID={goods_item.Id}");
                            }
                            else
                            {
                                KTGMCommandManager.SendSystemChat("Usage: LogHoleDataByDbID [ItemDbID]");
                            }

                            break;
                        }
                    case "UpdateHoleDataByDbID":
                        {
                            if (paramsCount >= 3 && int.TryParse(para[1], out var targetRoleId) && int.TryParse(para[2], out var itemDbId))
                            {
                                var target = GameManager.ClientMgr.FindClient(targetRoleId);
                                if (target == null)
                                {
                                    KTGMCommandManager.SendSystemChat($"Kết quả: Người chơi RoleID={targetRoleId} không online");
                                    break;
                                }

                                var goods_item = Global.GetGoodsByDbID(target, itemDbId);
                                if (goods_item == null)
                                {
                                    KTGMCommandManager.SendSystemChat($"Kết quả: Không tìm được item DBID={itemDbId} trên RoleID={targetRoleId}");
                                    break;
                                }

                                string inputData = string.Join(" ", para.Skip(3));
                                string holeString = inputData;

                                if (!string.IsNullOrEmpty(inputData) && !inputData.StartsWith("<") && !inputData.StartsWith("{"))
                                {
                                    try
                                    {
                                        byte[] decodedBytes = Convert.FromBase64String(inputData);
                                        string decodedText = Encoding.UTF8.GetString(decodedBytes);
                                        if (!string.IsNullOrEmpty(decodedText))
                                        {
                                            holeString = decodedText;
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }

                                var holeData = BaoThachUserData.GetFromStringData(holeString);
                                if (holeData == null)
                                {
                                    KTGMCommandManager.SendSystemChat("Kết quả: HoleData không hợp lệ (chỉ hỗ trợ XML/JSON hoặc Base64 của XML/JSON)");
                                    break;
                                }

                                string oldHoleString = null;
                                goods_item.OtherParams?.TryGetValue(ItemPramenter.Hole, out oldHoleString);

                                if (!ItemManager.UpdateOtherParameter(target, ItemPramenter.Hole, holeString, goods_item, "GMCOMMAND", true))
                                {
                                    KTGMCommandManager.SendSystemChat("Kết quả: UpdateOtherParameter thất bại");
                                    break;
                                }

                                target.GetPlayEquipBody().ClearAllEffecEquipBody();
                                target.GetPlayEquipBody().AttackAllEquipBody();

                                LogManager.WriteLog(LogTypes.BaoThach,
                                    $"[GM][HoleUpdate][{player.ServerId}] GMRole={player.RoleID} TargetRole={target.RoleID} ItemDBID={goods_item.Id} GoodsID={goods_item.GoodsID} Old={oldHoleString} New={holeString}");

                                KTGMCommandManager.SendSystemChat($"Kết quả: Cập nhật HoleData thành công RoleID={target.RoleID} DBID={goods_item.Id}");
                            }
                            else
                            {
                                KTGMCommandManager.SendSystemChat("Usage: UpdateHoleDataByDbID [RoleID] [ItemDbID] [HoleDataXML/JSON|Base64]");
                            }

                            break;
                        }
                    case "DoThach":
                        {
                            if (paramsCount == 2)
                            {
                                try
                                {
                                    int spinerId = int.Parse(para[1]);
                                    int rollTimes = int.Parse(para[2]);
                                    Dictionary<int, int> rewards = new Dictionary<int, int>();
                                    for (int i = 0; i < rollTimes; i++)
                                    {
                                        ItemChance item = DoThachManager.GetRollItem(player, spinerId);
                                        if (item.itemId != 0)
                                        {
                                            if (rewards.ContainsKey(item.itemId))
                                                rewards[item.itemId] += item.amount;
                                            else
                                                rewards[item.itemId] = item.amount;
                                        }
                                    }

                                    int totalSpackNeed = rewards.Sum(x => ItemManager.TotalSpaceNeed(x.Key, x.Value));

                                    if (!KTGlobal.IsHaveSpace(totalSpackNeed, player))
                                    {
                                        PlayerManager.ShowMessageBox(player, "Thông báo", $"Túi đồ không đủ chỗ!");
                                        return;
                                    }

                                    foreach (var rewardItem in rewards)
                                    {
                                        CreateItem(player, rewardItem.Key, rewardItem.Value, 0);
                                    }
                                    string strcmd = string.Join(":", rewards.Select(kv => $"{kv.Key}-{kv.Value}"));
                                    player.SendPacket((int)TCPGameServerCmds.CMD_BT_ROLL, strcmd);
                                }
                                catch (Exception es)
                                {

                                    KTGMCommandManager.SendSystemChat("Lỗi: " + es.Message);
                                }
                            }
                            break;
                        }

                    case "SetDay":
                        {
                            if (paramsCount == 1)
                            {
                                try
                                {
                                    TimeUtil.fakeDays = int.Parse(para[1]);
                                    string currenDate = TimeUtil.NowDateTime().ToString("dd/MM/yyyy");
                                    PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn đã đổi sang ngày {currenDate}", () => { });
                                }
                                catch (Exception es)
                                {

                                    KTGMCommandManager.SendSystemChat("Lỗi: " + es.Message);
                                }
                            }
                            break;
                        }

                    case "ResetRefundData":
                        {
                            if (paramsCount == 0)
                            {
                                try
                                {
                                    player.ResetRefundData();
                                    PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn đã xóa dữ liệu sự kiện hoàn trả thành công", () => { });
                                }
                                catch (Exception es)
                                {

                                    KTGMCommandManager.SendSystemChat("Lỗi: " + es.Message);
                                }
                            }
                            break;
                        }
                    #endregion

                    case "RemovePreRoundRechargeData":
                        {
                            if (paramsCount == 1)
                            {
                                try
                                {
                                    var roleID = int.Parse(para[1]);
                                    var targetClient = GameManager.ClientMgr.FindClient(roleID);
                                    targetClient.ClearRechargePreRoundData();
                                }
                                catch (Exception es)
                                {

                                    LogManager.WriteLog(LogTypes.Error,
                                        $"RemovePreRoundRechargeData Error: {es.Message}\n{es.StackTrace}");
                                }

                            }
                            break;
                        }

                    case "AddPlayerAtt":
                        {
                            if (paramsCount == 2)
                            {
                                try
                                {
                                    string magicname = para[1];
                                    int value = int.Parse((para[2]));
                                    KTAttributesModifier.AttachProperty(
                                        BaoThachManager.CreateMagicAttribute(magicname, value), player, false); ;
                                    KTGMCommandManager.SendSystemChat("Thành công");
                                }
                                catch (Exception es)
                                {

                                    KTGMCommandManager.SendSystemChat("Lỗi: " + es.Message);
                                }
                            }

                            break;
                        }
                    case "DailyCheckin":
                        {
                            int paramentas1 = 0;
                            if (paramsCount == 1)
                            {
                                paramentas1 = int.Parse(para[1]);
                            }
                            DailyCheckinManager.Checkin(player, paramentas1, "GMCOMMAND");
                            break;
                        }
                    case "DailyCheckin_Update":
                        {
                            DailyCheckinManager.SendClientCheckinUpdate(player);
                            break;
                        }
                    case "DailyCheckin_Reward":
                        {
                            if (paramsCount == 1)
                            {
                                int day = int.Parse(para[1]);
                                DailyCheckinManager.GetRewardMethi(player, day);
                            }
                            break;
                        }
                    case "SetFakeDate":
                        {
                            if (paramsCount == 0)
                            {
                                KTGlobal.FakeDate = DateTime.Now;
                                KTGlobal.UpdateFakeDate = DateTime.Now;
                            }
                            else if (paramsCount == 3)
                            {
                                SetSystemDateTime(int.Parse(para[1]), int.Parse(para[2]), int.Parse(para[3]), 0, 0, 0);
                            }
                            else if (paramsCount == 5)
                            {
                                SetSystemDateTime(int.Parse(para[1]), int.Parse(para[2]), int.Parse(para[3]), int.Parse(para[4]), int.Parse(para[5]), 0);
                            }
                            else if (paramsCount == 6)
                            {
                                SetSystemDateTime(int.Parse(para[1]), int.Parse(para[2]), int.Parse(para[3]), int.Parse(para[4]), int.Parse(para[5]), int.Parse(para[6]));
                            }

                            SendSystemChat("Current Date: " + KTGlobal.GetDateNow());
                            break;
                        }
                    case "GetFakeDate":
                        {
                            SendSystemChat("Current Date: " + KTGlobal.GetDateNow());
                            break;
                        }
                        {
                            // in ra tung gia tri trong file serverConfig.json
                            var ServerConfigPath_JSON = "ServerConfig.json";
                            var configStr = File.ReadAllText(ServerConfigPath_JSON);
                            KTGMCommandManager.SendSystemChat("ServerConfig Info:\n" + configStr);
                            break;
                        }
                    #region BattlePass
                    case "AddBPExp":
                        {
                            if (paramsCount == 1)
                            {
                                var exp = int.Parse(para[1]);
                                BattlePassManager.AddExp(player, exp, BattlePassManager.GetBattlePassData(player));
                            }
                            else if (paramsCount == 2)
                            {
                                var targetID = int.Parse(para[1]);
                                var exp = int.Parse(para[2]);
                                var target = FindPlayer(targetID);
                                var bpData = BattlePassManager.GetBattlePassData(target);
                                BattlePassManager.AddExp(target, exp, bpData);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'AddBPExp' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }
                    case "ResetBPData":
                        {
                            if (paramsCount == 0)
                            {
                                BattlePassManager.ResetPlayerBattlePass(player);
                            }
                            else if (paramsCount == 1)
                            {
                                var targetID = int.Parse(para[1]);
                                var target = FindPlayer(targetID);
                                BattlePassManager.ResetPlayerBattlePass(target);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.Error,
                                    "Process GM Command 'ResetBPData' Failed...\n" + "Invalid param.");
                            }

                            break;
                        }

                    case "SkipBPTask":
                        {
                            if (paramsCount == 1)
                            {
                                var taskID = int.Parse(para[1]);
                                if (BattlePassManager.SkipTask(player, taskID))
                                {
                                    PlayerManager.ShowNotification(player, "Đã bỏ qua nhiệm vụ thành công!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Không thể bỏ qua nhiệm vụ này!");
                                }
                            }
                            else if (paramsCount == 2)
                            {
                                var taskID = int.Parse(para[1]);
                                var targetID = int.Parse(para[2]);
                                var target = FindPlayer(targetID);
                                if (BattlePassManager.SkipTask(target, taskID))
                                {
                                    PlayerManager.ShowNotification(player, "Đã bỏ qua nhiệm vụ thành công!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(player, "Không thể bỏ qua nhiệm vụ này!");
                                }
                            }

                            break;
                        }
                        #endregion
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, "Process GM Command Error...\n" + ex.ToString(), false);
            }
        }

        private static void SetSystemDateTime(int Y, int M, int D, int H, int m, int s)
        {
            KTGlobal.FakeDate = new DateTime(Y, M, D, H, m, s);
            KTGlobal.UpdateFakeDate = DateTime.Now;
        }


        #region Helper

        /// <summary>
        /// Tìm người chơi có tên tương ứng
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private static KPlayer FindPlayer(string playerName)
        {
            var player = GameManager.ClientMgr.FindClients(x => x.RoleName == playerName).FirstOrDefault();
            return player;
        }

        /// <summary>
        /// Tìm người chơi có ID tương ứng
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns></returns>
        private static KPlayer FindPlayer(int playerID)
        {
            return GameManager.ClientMgr.FindClient(playerID);
        }

        #endregion Helper

        #region Script-Lua

        /// <summary>
        /// Tải lại Script Lua mới nhất
        /// </summary>
        /// <param name="scriptID">ID Script Lua</param>
        private static void ReloadScriptLua(int scriptID)
        {
            KTLuaScript.Instance.ReloadScript(KTLuaEnvironment.LuaEnv, scriptID);
        }

        private static bool ReloadScriptLua(string name)
        {
            return KTLuaScript.Instance.ReloadScript(KTLuaEnvironment.LuaEnv, name);
        }

        #endregion Script-Lua

        #region Trạng thái ngũ hành

        /// <summary>
        /// Thêm trạng thái ngũ hành cho đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID"></param>
        /// <param name="stateID"></param>
        /// <param name="time"></param>
        private static void AddSeriesState(int targetID, string stateID, float time)
        {
            GameObject target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddSeriesState' Error...\n" + "Target ID '" + targetID + "' not found.");
                return;
            }

            KTGMCommandManager.AddSeriesState(target, stateID, time);
        }

        /// <summary>
        /// Thêm trạng thái ngũ hành
        /// </summary>
        /// <param name="target">Đối tượng</param>
        /// <param name="stateID">Trạng thái</param>
        /// <param name="time">Thời gian (giây)</param>
        private static void AddSeriesState(GameObject target, string stateID, float time)
        {
            // Kiểm tra trạng thái
            if (!Utils.TryParseEnum<KE_STATE>(stateID, out var state))
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddSeriesState' Error...\n" + "State ID '" + stateID + "' not found.");
                return;
            }

            // Kiểm tra thời gian
            if (time < 0 || time > 60)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddSeriesState' Error...\n" +
                    "Duration is INVALID, must be between 0 and 60s.");
                return;
            }

            // Thêm trạng thái vào đối tượng
            target.AddSpecialState(target, new UnityEngine.Vector2((int)target.CurrentPos.X, (int)target.CurrentPos.Y),
                state, (int)(time * 18), (int)(time * 18), true);
        }

        /// <summary>
        /// Xóa trạng thái ngũ hành khỏi đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID"></param>
        /// <param name="stateID"></param>
        private static void RemoveSeriesState(int targetID, string stateID)
        {
            GameObject target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'RemoveSeriesState' Error...\n" + "Target ID '" + targetID + "' not found.");
                return;
            }

            KTGMCommandManager.RemoveSeriesState(target, stateID);
        }

        /// <summary>
        /// Xóa trạng thái ngũ hành
        /// </summary>
        /// <param name="target">Đối tượng</param>
        /// <param name="stateID">ID trạng thái</param>
        private static void RemoveSeriesState(GameObject target, string stateID)
        {
            // Kiểm tra trạng thái
            if (!Utils.TryParseEnum<KE_STATE>(stateID, out var state))
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'RemoveSeriesState' Error...\n" + "State ID '" + stateID + "' not found.");
                return;
            }

            // Xóa trạng thái khỏi đối tượng
            target.RemoveSpecialState(state, true);
        }

        #endregion Trạng thái ngũ hành

        #region Gia nhập phái

        /// <summary>
        /// Thiết lập môn phái cho đối tượng tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="factionID">ID phái</param>
        private static void JoinFaction(int targetID, int factionID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'Heal' Error...\n" + "Target not found.");
                return;
            }

            // Thực hiện phục hồi cho mục tiêu
            KTGMCommandManager.JoinFaction(target, factionID);
        }

        /// <summary>
        /// Thiết lập môn phái cho đối tượng tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="factionID">ID phái</param>
        private static void JoinFaction(KPlayer player, int factionID)
        {
            // Gia nhập môn phái
            PlayerManager.JoinFaction(player, factionID);
        }

        #endregion Gia nhập phái

        #region Trị liệu

        /// <summary>
        /// Trị liệu phục hồi sinh lực, nội lực, thể lực cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        private static void Heal(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'Heal' Error...\n" + "Target not found.");
                return;
            }

            // Thực hiện phục hồi cho mục tiêu
            KTGMCommandManager.Heal(target);
        }

        /// <summary>
        /// Trị liệu phục hồi sinh lực, nội lực, thể lực
        /// </summary>
        /// <param name="player">Người chơi</param>
        private static void Heal(KPlayer player)
        {
            // Phục hồi sinh lực
            player.m_CurrentLife = player.m_CurrentLifeMax;
            // Phục hồi nội lực
            player.m_CurrentMana = player.m_CurrentManaMax;
            // Phục hồi thể lực
            player.m_CurrentStamina = player.m_CurrentStaminaMax;
        }

        #endregion Trị liệu

        #region Thông tin người chơi

        /// <summary>
        /// Trả ra thông tin người chơi tương ứng
        /// </summary>
        /// <param name="player">Thông tin hiển thị cho người chơi</param>
        /// <param name="playerName">Tên người chơi cần tìm</param>
        private static void GetPlayerInfo(KPlayer player, string playerName)
        {
            var target = KTGMCommandManager.FindPlayer(playerName);
            // Kiểm tra đối tượng
            if (target == null)
            {
                PlayerManager.ShowNotification(player,
                    string.Format("Không tìm thấy thông tin người chơi {0}!", playerName));
                return;
            }

            // Bản đồ tương ứng
            var gameMap = GameManager.MapMgr.GetGameMap(target.CurrentMapCode);

            var msg = string.Format("Thông tin người chơi: {0}({1}), vị trí: {4} ({2}, {3})", target.RoleName,
                target.RoleID, (int)target.CurrentPos.X, (int)target.CurrentPos.Y,
                gameMap == null ? "Chưa rõ" : gameMap.MapName);
            PlayerManager.ShowNotification(player, msg);
        }

        #endregion Thông tin người chơi

        #region Dịch chuyển

        /// <summary>
        /// Dịch chuyển đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="mapCode">ID bản đồ</param>
        /// <param name="posX">Vị trí X</param>
        /// <param name="posY">Vị trí Y</param>
        private static void GoTo(int targetID, int mapCode, int posX, int posY)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'GoTo' Error...\n" + "Target not found.");
                return;
            }

            // Thực hiện dịch chuyển đối tượng tương ứng
            KTGMCommandManager.GoTo(target, mapCode, posX, posY);
        }

        /// <summary>
        /// Dịch chuyển người chơi
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="mapCode">ID bản đồ</param>
        /// <param name="posX">Vị trí X</param>
        /// <param name="posY">Vị trí Y</param>
        private static void GoTo(KPlayer player, int mapCode, int posX, int posY)
        {
            // Lấy dữ liệu bản đồ đích đến
            if (GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out var gameMap))
            {
                // Nếu bản đồ đích khác bản đồ hiện tại
                if (player.CurrentMapCode != mapCode)
                {
                    GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, player, mapCode, posX, posY, (int)player.CurrentDir);
                }
                else
                {
                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, player, posX, posY, (int)player.CurrentDir);
                }
            }
            else
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'GoTo' Error...\n" + "Map not exist.");
            }
        }

        #endregion Dịch chuyển

        #region Kỹ năng và Buff

        /// <summary>
        /// Thêm kỹ năng cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        /// <param name="level">Cấp độ</param>
        private static void AddSkill(int targetID, int skillID, int level)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'AddSkill' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddSkill(target, skillID, level);
        }

        /// <summary>
        /// Thêm kỹ năng cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="skillID">ID kỹ năng</param>
        /// <param name="level">Cấp độ</param>
        private static void AddSkill(KPlayer player, int skillID, int level)
        {
            player.Skills.AddSkill(skillID);
            player.Skills.AddSkillLevel(skillID, level);
        }

        /// <summary>
        /// Xóa kỹ năng khỏi đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void RemoveSkill(int targetID, int skillID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'RemoveSkill' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.RemoveSkill(target, skillID);
        }

        /// <summary>
        /// Xóa kỹ năng khỏi người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void RemoveSkill(KPlayer player, int skillID)
        {
            player.Skills.RemoveSkill(skillID);
        }

        /// <summary>
        /// Thêm Buff cho đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        /// <param name="level">Cấp độ</param>
        private static void AddBuff(int targetID, int skillID, int level)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'AddSkill' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddBuff(target, skillID, level);
        }

        /// <summary>
        /// Thêm Buff tương ứng cho đối tượng
        /// </summary>
        /// <param name="target">Đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        /// <param name="level">Cấp độ</param>
        private static void AddBuff(GameObject target, int skillID, int level)
        {
            var skillData = KSkill.GetSkillData(skillID);
            if (skillData == null)
            {
                return;
            }

            var skill = new SkillLevelRef()
            {
                Data = skillData,
                AddedLevel = level,
                BonusLevel = 0,
                CanStudy = false,
            };

            var skillPd = skill.Properties;
            var duration = -1L;
            if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_skill_statetime))
            {
                if (skillPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.magic_skill_statetime).nValue[0] == -1)
                {
                    duration = -1;
                }
                else
                {
                    duration = skillPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.magic_skill_statetime).nValue[0] * 1000L /
                               18;
                }
            }

            target.Buffs.AddBuff(new BuffDataEx()
            {
                Skill = skill,
                Duration = duration,
                LoseWhenUsingSkill = false,
                SaveToDB = false,
                StartTick = KTGlobal.GetCurrentTimeMilis(),
            });

            /// Nếu là người chơi
            if (target is KPlayer player)
            {
                PlayerManager.ShowNotification(player, $"Thêm Buff [{skillData.Name}] cấp {level} thành công!");
            }
        }

        /// <summary>
        /// Xóa Buff khỏi đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void RemoveBuff(int targetID, int skillID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'RemoveBuff' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.RemoveBuff(target, skillID);
        }

        /// <summary>
        /// Xóa Buff khỏi đối tượng tương ứng
        /// </summary>
        /// <param name="target">Đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void RemoveBuff(GameObject target, int skillID)
        {
            target.Buffs.RemoveBuff(skillID);
        }

        /// <summary>
        /// Làm mới toàn bộ dữ liệu phục hồi kỹ năng của đối tượng có ID tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void ResetAllSkillCooldown(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ResetAllSkillCooldown' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetAllSkillCooldown(target);
        }

        /// <summary>
        /// Làm mới toàn bộ dữ liệu phục hồi kỹ năng của đối tượng tương ứng
        /// </summary>
        /// <param name="target">Đối tượng</param>
        /// <param name="skillID">ID kỹ năng</param>
        private static void ResetAllSkillCooldown(KPlayer target)
        {
            target.Skills.ClearSkillCooldownList();
        }

        #endregion Kỹ năng và Buff

        #region System Chat

        /// <summary>
        /// Gửi tin nhắn dưới kênh hệ thống
        /// </summary>
        /// <param name="message">Tin nhắn</param>
        public static void SendSystemChat(string message)
        {
            KTGlobal.SendSystemChat(message);
        }

        /// <summary>
        /// Gửi tin nhắn dưới kênh hệ thống đồng thời hiển thị ở dòng chữ chạy ngang trêu đầu
        /// </summary>
        /// <param name="message"></param>
        public static void SendSystemEventNotification(string message)
        {
            KTGlobal.SendSystemEventNotification(message);
        }

        #endregion System Chat

        #region Tạo vật phẩm

        /// <summary>
        /// Tạo vật phẩm số lượng tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="itemID">ID vật phẩm</param>
        /// <param name="quantity">Số lượng</param>
        private static void CreateItem(KPlayer player, int targetID, int itemID, int quantity, int EXP = -1)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'CreateItem' Error...\n" + "Target not found.");
                PlayerManager.ShowNotification(player, "Người chơi không tồn tại/online.");
                return;
            }

            KTGMCommandManager.CreateItem(target, itemID, quantity, EXP);
        }

        /// <summary>
        /// Tạo vật phẩm số lượng tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="itemID">ID vật phẩm</param>
        /// <param name="quantity">Số lượng</param>
        private static void CreateItem(KPlayer player, int itemID, int quantity, int EXP)
        {
            // Nếu vật phẩm tồn tại
            if (ItemManager._TotalGameItem.ContainsKey(itemID))
            {
                var TimeUsing = Global.ConstGoodsEndTime;

                if (EXP > 0)
                {
                    var dt = DateTime.Now.AddMinutes(EXP);

                    // "1900-01-01 12:00:00";
                    TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                var binding = 0;
                if (GameManager.IsLiveServer)
                    binding = 1;

                ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, itemID, quantity, 0, "GMCOMMAND",
                    true, binding, false, TimeUsing);
            }
            else
            {
                PlayerManager.ShowNotification(player, "Vật phẩm không tồn tại!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'CreateItem' Error...\n" + "Item ID = " + itemID + " not found.");
            }
        }

        #endregion Tạo vật phẩm

        #region Thiết lập cấp cường hóa cho trang bị

        private static void FakeEquipEnhance(KPlayer player, int targetID, int slot, int fakeIsSuccess, int fakePercent)
        {
            if (fakePercent < 10 || fakePercent > 120)
            {
                PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                return;
            }

            if (fakeIsSuccess == 0)
            {
                var target = KTGMCommandManager.FindPlayer(targetID);
                if (target == null)
                {
                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                    return;
                }

                var itemGD = target.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot).FirstOrDefault();


                // Kiểm tra đối tượng
                if (itemGD != null)
                {
                    if (!ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out var itemData))
                    {
                        PlayerManager.ShowNotification(player,
                            "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                        return;
                    }

                    if (!ItemManager.KD_ISEQUIP(itemData.Genre))
                    {
                        PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                        return;
                    }

                    var level = itemGD.Forge_level;
                    if (level < 8)
                    {
                        PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                        return;
                    }

                    var ShowHang = "Thật đáng tiếc! " + KTGlobal.CreateStringByColor(target.RoleName, ColorType.Green) +
                                   " với " + KTGlobal.CreateStringByColor(fakePercent + "%", ColorType.VanillaIce) +
                                   " đã thất bại khi cường hóa " + KTGlobal.GetItemDescInfoStringForChat(itemGD) +
                                   " lên " +
                                   KTGlobal.CreateStringByColor("+" + (level + 1) + "", ColorType.VanillaIce) + "!";
                    KTGlobal.SendSystemEventNotification(ShowHang, new List<GoodsData>() { itemGD });
                }
                else
                {
                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                    return;
                }
            }
            else
            {
                var target = KTGMCommandManager.FindPlayer(targetID);
                if (target == null)
                {
                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                    return;
                }

                var itemGD = target.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot).FirstOrDefault();

                // Kiểm tra đối tượng
                if (itemGD != null)
                {
                    if (!ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out var itemData))
                    {
                        PlayerManager.ShowNotification(player,
                            "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                        return;
                    }

                    if (itemGD.Binding == 0)
                    {
                        PlayerManager.ShowNotification(player, "Chỉ cường hóa được vật phẩm khóa");
                        return;
                    }

                    if (!ItemManager.KD_ISEQUIP(itemData.Genre))
                    {
                        PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                        return;
                    }

                    var level = itemGD.Forge_level;
                    if (level < 8)
                    {
                        PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                        return;
                    }

                    EquipEnhance(target.RoleID, slot);

                    var ShowHang = "Người chơi " + KTGlobal.CreateStringByColor(target.RoleName, ColorType.Green) +
                                   " với " + KTGlobal.CreateStringByColor(fakePercent + "%", ColorType.VanillaIce) +
                                   " đem " + KTGlobal.GetItemDescInfoStringForChat(itemGD) + " cường hóa lên " +
                                   KTGlobal.CreateStringByColor("+" + itemGD.Forge_level + "", ColorType.VanillaIce) +
                                   " thành công!";
                    KTGlobal.SendSystemEventNotification(ShowHang, new List<GoodsData>() { itemGD });
                }
                else
                {
                    PlayerManager.ShowNotification(player, "Lệnh GM Lỗi.");
                    return;
                }
            }
        }

        /// <summary>
        /// Tạo vật phẩm số lượng tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="slot">Vị trí trang bị trong túi</param>
        /// <param name="level">Cấp cường hóa</param>
        private static void EquipEnhance(int targetID, int slot)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'CreateItem' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.EquipEnhance(target, slot);
        }

        private static void EquipStrong(int targetID, int slot, int strong)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target != null)
            {
                var itemGD = target.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot).FirstOrDefault();
                if (itemGD != null)
                {
                    itemGD.Strong = strong;
                    // update vat pham
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, target.RoleID);
                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, itemGD.Id);
                    TotalUpdate.Add(UPDATEITEM.STRONG, strong);

                    ItemManager.UpdateItemPrammenter(TotalUpdate, itemGD, target);
                }
            }
        }

        /// <summary>
        /// Tạo vật phẩm số lượng tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="slot">Vị trí trang bị trong túi</param>
        /// <param name="level">Cấp cường hóa</param>
        private static void EquipEnhance(KPlayer player, int slot)
        {
            // Vật phẩm tại vị trí tương ứng trong túi
            var itemGD = player.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot).FirstOrDefault();
            // Nếu vật phẩm tại vị trí tương ứng không tồn tại
            if (itemGD == null)
            {
                PlayerManager.ShowNotification(player, "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + " has no item.");
                return;
            }

            // Nếu vật phẩm không tồn tại trong hệ thống
            if (!ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out var itemData))
            {
                PlayerManager.ShowNotification(player, "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + ", Item ID = " +
                    itemGD.GoodsID + " not found.");
                return;
            }

            // Nếu không phải trang bị
            if (!ItemManager.KD_ISEQUIP(itemData.Genre))
            {
                PlayerManager.ShowNotification(player,
                    "Vật phẩm tại vị trí " + slot + " trong túi đồ không phải trang bị, không thể cường hóa được!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + ", Item ID = " +
                    itemGD.GoodsID + " is not equip.");
                return;
            }

            var level = itemGD.Forge_level;

            ItemManager.SetEquipForgeLevel(itemGD, player, level + 1, 1);
        }

        /// <summary>
        /// Cường hóa thêm kỹ năng cho trang bị BN HK
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slot"></param>
        private static void EquipEnhanceArtifact(KPlayer player, int slot)
        {
            // Vật phẩm tại vị trí tương ứng trong túi
            var itemGD = player.GoodsDataList?.Where(x => x.Using == -1 && x.BagIndex == slot).FirstOrDefault();
            // Nếu vật phẩm tại vị trí tương ứng không tồn tại
            if (itemGD == null)
            {
                PlayerManager.ShowNotification(player, "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + " has no item.");
                return;
            }

            // Nếu vật phẩm không tồn tại trong hệ thống
            if (!ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out var itemData))
            {
                PlayerManager.ShowNotification(player, "Không tồn tại vật phẩm tại vị trí " + slot + " trong túi đồ!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + ", Item ID = " +
                    itemGD.GoodsID + " not found.");
                return;
            }

            // Nếu không phải trang bị
            if (!ItemManager.KD_ISEQUIP(itemData.Genre) || !itemData.IsArtifact)
            {
                PlayerManager.ShowNotification(player,
                    "Vật phẩm tại vị trí " + slot + " trong túi đồ không phải trang bị, không thể cường hóa được!");
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'EquipEnhance' Error...\n" + "BagPos = " + slot + ", Item ID = " +
                    itemGD.GoodsID + " is not equip.");
                return;
            }

            ItemEnhance.DoEnhArtifactItem(null, itemGD, player, MoneyType.Bac, true);
        }

        #endregion Thiết lập cấp cường hóa cho trang bị

        #region Thêm tiền

        /// <summary>
        /// Thêm tiền số lượng tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="type">Loại tiền (1: Bạc thường, 2: Bạc khóa, 3: Đồng thường, 4: Đồng khóa)</param>
        /// <param name="amount">Số lượng</param>
        private static void AddMoney(int targetID, int type, int amount)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'AddMoney' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddMoney(target, type, amount);
        }

        /// <summary>
        /// Thêm tiền số lượng tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="type">Loại tiền (1: Bạc thường, 2: Bạc khóa, 3: Đồng thường, 4: Đồng khóa)</param>
        /// <param name="amount">Số lượng</param>
        private static void AddMoney(KPlayer player, int type, int amount)
        {
            switch (type)
            {
                case 1:
                    {
                        GameManager.ClientMgr.AddMoney(player, amount, "GMCommand");
                        break;
                    }
                case 2:
                    {
                        GameManager.ClientMgr.AddUserBoundMoney(player, amount, "GMCommand");
                        break;
                    }
                case 3:
                    {
                        GameManager.ClientMgr.AddToken(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, player, amount,
                            "GMCommand");
                        break;
                    }
                case 4:
                    {
                        GameManager.ClientMgr.AddBoundToken(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, player, amount,
                            "GMCommand");
                        break;
                    }
                case 5:
                    {
                        // Add Tích lũy cá nhân ở bang hội
                        GameManager.ClientMgr.AddGuildMoney(Global._TCPManager.MySocketListener,
                            Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, player, amount,
                            "GMCommand");
                        break;
                    }
                case 6:
                    {
                        // Add Tích lũy cá nhân ở bang hội
                        KTGlobal.UpdateGuildMoney(500, player.GuildID, player);
                        break;
                    }
            }
        }

        #endregion Thêm tiền

        #region Thêm vật phẩm rơi ở MAP

        /// <summary>
        /// Thêm vật phẩm rơi ở MAP theo ID quái tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="monsterID">ID cấu hình quái tương ứng bọc</param>
        private static void AddDropItem(int targetID, int monsterID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddDropItem' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddDropItem(target, monsterID);
        }

        /// <summary>
        /// Thêm vật phẩm rơi ở MAP theo ID quái tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="monsterID">ID cấu hình quái tương ứng bọc</param>
        private static void AddDropItem(KPlayer player, int monsterID)
        {
            // ItemDropManager.GetDropMonsterDie(monsterID, player);
        }

        #endregion Thêm vật phẩm rơi ở MAP

        #region Hoạt động

        /// <summary>
        /// Chủ động bắt đầu sự kiện có ID tương ứng
        /// </summary>
        /// <param name="activityID"></param>
        private static void StartActivity(int activityID)
        {
            KTActivityManager.StartActivity(activityID);
        }

        /// <summary>
        /// Chủ động kết thúc sự kiện có ID tương ứng
        /// </summary>
        /// <param name="activityID"></param>
        private static void StopActivity(int activityID)
        {
            KTActivityManager.StopActivity(activityID);
        }

        #endregion Hoạt động

        #region Thiết lập sát khí

        /// <summary>
        /// Thiết lập số điểm sát khí tương ứng cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="value">Giá trị</param>
        private static void SetPKValue(int targetID, int value)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'SetPKValue' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetPKValue(target, value);
        }

        /// <summary>
        /// Thiết lập số điểm sát khí tương ứng cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="value">Giá trị</param>
        private static void SetPKValue(KPlayer player, int value)
        {
            if (value < 0 || value > 10)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetPKValue' Error...\n" + "Value must be between [0-10].");
                return;
            }

            player.PKValue = value;
        }

        #endregion Thiết lập sát khí

        #region Thiết lập cấp độ và kinh nghiệm kỹ năng sống

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="lifeSkillID">ID kỹ năng sống</param>
        /// <param name="level">Cấp độ</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void SetLifeSkillLevelAndExp(int targetID, int lifeSkillID, int level, int exp)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetLifeSkillLevelAndExp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetLifeSkillLevelAndExp(target, lifeSkillID, level, exp);
        }

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="lifeSkillID">ID kỹ năng sống</param>
        /// <param name="level">Cấp độ</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void SetLifeSkillLevelAndExp(KPlayer player, int lifeSkillID, int level, int exp)
        {
            if (level < 0 || level > 120)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetLifeSkillLevelAndExp' Error...\n" + "Value must be between [0-120].");
                return;
            }

            var lifeSkillParam = player.GetLifeSkill(lifeSkillID);
            if (lifeSkillParam == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetLifeSkillLevelAndExp' Error...\n" + "Can not find life skill ID = " +
                    lifeSkillID + ".");
                return;
            }

            var lifeSkillExp =
                ItemCraftingManager._LifeSkill.TotalExp.Where(x => x.Level == level).FirstOrDefault();
            if (lifeSkillExp == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetLifeSkillLevelAndExp' Error...\n" +
                    "Can not find max exp for life skill ID = " + lifeSkillID + ".");
                return;
            }

            if (exp < 0)
            {
                exp = 0;
            }

            if (exp > lifeSkillExp.Exp - 1)
            {
                exp = lifeSkillExp.Exp - 1;
            }

            player.SetLifeSkillParam(lifeSkillID, level, exp);
        }

        #endregion Thiết lập cấp độ và kinh nghiệm kỹ năng sống

        #region Reset cấp độ và kinh nghiệm kỹ năng sống

        /// <summary>
        /// Reset cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        private static void ResetLifeSkillLevelAndExp(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ResetLifeSkillLevelAndExp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetLifeSkillLevelAndExp(target);
        }

        /// <summary>
        /// Reset cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        private static void ResetLifeSkillLevelAndExp(KPlayer player)
        {
            var lifeSkills = new Dictionary<int, LifeSkillPram>();
            for (var i = 1; i < 12; i++)
            {
                var param = new LifeSkillPram();
                param.LifeSkillID = i;
                param.LifeSkillLevel = 1;
                param.LifeSkillExp = 0;
                lifeSkills[i] = param;
            }

            player.SetLifeSkills(lifeSkills);
        }

        #endregion Reset cấp độ và kinh nghiệm kỹ năng sống

        #region Thiết lập điểm tinh lực, hoạt lực

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="gatherPoint">Tinh lực</param>
        /// <param name="makePoint">Hoạt lực</param>
        private static void AddGatherMakePoint(int targetID, int gatherPoint, int makePoint)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddGatherMakePoint' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddGatherMakePoint(target, gatherPoint, makePoint);
        }

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="gatherPoint">Tinh lực</param>
        /// <param name="makePoint">Hoạt lực</param>
        private static void AddGatherMakePoint(KPlayer player, int gatherPoint, int makePoint)
        {
            player.ChangeCurMakePoint(makePoint);
            player.ChangeCurGatherPoint(gatherPoint);
        }

        #endregion Thiết lập điểm tinh lực, hoạt lực

        #region Thiết lập danh hiệu tạm thời

        /// <summary>
        /// Thiết lập danh hiệu tạm thời cho đối tượng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="title">Danh hiệu</param>
        private static void SetTempTitle(int targetID, string title)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetTempTitle' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetTempTitle(target, title);
        }

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="title">Danh hiệu</param>
        private static void SetTempTitle(KPlayer player, string title)
        {
            player.TempTitle = title;
        }

        #endregion Thiết lập danh hiệu tạm thời

        #region Gửi thư

        /// <summary>
        /// Gửi thư cho đối tượng tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="title">Tiêu đề</param>
        /// <param name="content">Nội dung</param>
        /// <param name="items">Danh sách vật phẩm đính kèm</param>
        /// <param name="boundMoney">Bạc khóa đính kèm</param>
        /// <param name="boundToken">Đồng khóa đính kèm</param>
        // private static bool SendSystemMail(int targetID, string title, string content, string items, int boundMoney,
        //     int boundToken)
        // {
        //     // var target = KTGMCommandManager.FindPlayer(targetID);
        //     // // Kiểm tra đối tượng
        //     // if (target == null)
        //     // {
        //     //     LogManager.WriteLog(LogTypes.Error, "Process GM Command 'SendMail' Error...\n" + "Target not found.");
        //     //     return false;
        //     // }

        //     return KTGMCommandManager.SendSystemMail(target, title, content, items, boundMoney, boundToken);
        // }

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm kỹ năng sống cho đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="title">Tiêu đề</param>
        /// <param name="content">Nội dung</param>
        /// <param name="items">Danh sách vật phẩm đính kèm</param>
        /// <param name="boundMoney">Bạc khóa đính kèm</param>
        /// <param name="boundToken">Đồng khóa đính kèm</param>
        public static bool SendSystemMail(int playerID, string title, string content, string items, int boundMoney,
            int boundToken)
        {
            // Nếu có vật phẩm đính kèm
            if (!string.IsNullOrEmpty(items))
            {
                // Danh sách vật phẩm đính kèm
                var stickItems = new List<Tuple<int, int, int, int, int>>();

                foreach (var itemInfo in items.Split('|'))
                {
                    var para = itemInfo.Split('_');

                    int itemID, count, binding, enhanceLevel, usageMinutes = 0;
                    if (para.Length != 4 && para.Length != 5)
                    {
                        LogManager.WriteLog(LogTypes.Error,
                            "Process GM Command 'SendMail' Error...\n" + "Incorrect item parameters.");
                        return false;
                    }
                    else if (!int.TryParse(para[0], out itemID) || !int.TryParse(para[1], out count) ||
                             !int.TryParse(para[2], out binding) || !int.TryParse(para[3], out enhanceLevel) ||
                             (para.Length == 5 && !int.TryParse(para[4], out usageMinutes)))
                    {
                        LogManager.WriteLog(LogTypes.Error,
                            "Process GM Command 'SendMail' Error...\n" + "Incorrect item parameters.");
                        return false;
                    }

                    stickItems.Add(new Tuple<int, int, int, int, int>(itemID, count, binding, enhanceLevel, usageMinutes));
                }

                // Thực hiện thêm vật phẩm
                return KTMailManager.SendSystemMailToPlayerWithItemIDs(playerID, stickItems, title, content, boundMoney,
                    boundToken) > 0;
            }
            // Nếu không có vật phẩm đính kèm
            else
            {
                // Thực hiện thêm vật phẩm
                return KTMailManager.SendSystemMailToPlayerWithItemIDs(playerID, new List<Tuple<int, int, int, int, int>>(),
                    title, content, boundMoney, boundToken) > 0;
            }
        }

        #endregion Gửi thư

        #region Thêm kinh nghiệm

        /// <summary>
        /// Thêm kinh nghiệm cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void AddExp(int targetID, int exp)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'AddExp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddExp(target, exp);
        }

        /// <summary>
        /// Thêm kinh nghiệm cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void AddExp(KPlayer player, int exp)
        {
            GameManager.ClientMgr.ProcessRoleExperience(player, exp, true, false, true, "GMCommand");
        }

        #endregion Thêm kinh nghiệm

        #region Thiết lập cấp độ

        /// <summary>
        /// Thiết lập cấp độ cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="level">Cấp độ</param>
        private static void SetLevel(int targetID, int level)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'SetLevel' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetLevel(target, level);
        }

        /// <summary>
        /// Thiết lập cấp độ cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="level">Cấp độ</param>
        private static void SetLevel(KPlayer player, int level)
        {
            GameManager.ClientMgr.SetRoleLevel(player, level);
            Global.ForceCloseClient(player, "GMCommand");
        }

        #endregion Thêm kinh nghiệm

        #region Reset số lượt Đoán hoa đăng

        /// <summary>
        /// Reset số lượt Đoán hoa đăng trong ngày
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="eventID">Loại phụ bản</param>
        private static void ResetKnowledgeChallengeQuestions(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ResetKnowledgeChallengeQuestions' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetKnowledgeChallengeQuestions(target);
        }

        /// <summary>
        /// Reset số lượt Đoán hoa đăng trong ngày của đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="eventID">Loại phụ bản</param>
        private static void ResetKnowledgeChallengeQuestions(KPlayer player)
        {
            player.SetValueOfDailyRecore((int)AcitvityRecore.KnowledgeChallenge_TotalQuestions, 0);
        }

        #endregion

        #region Thiết lập lại số lượt phụ bản

        /// <summary>
        /// Reset giá trị số lượt tham gia phụ bản tương ứng trong ngày
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="eventID">Loại phụ bản</param>
        private static void ResetCopySceneEnterTimes(int targetID, AcitvityRecore eventID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ResetCopySceneEnterTimes' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetCopySceneEnterTimes(target, eventID);
        }

        /// <summary>
        /// Reset giá trị số lượt tham gia phụ bản tương ứng trong ngày của đối tượng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="eventID">Loại phụ bản</param>
        private static void ResetCopySceneEnterTimes(KPlayer player, AcitvityRecore eventID)
        {
            CopySceneEventManager.SetCopySceneTotalEnterTimesToday(player, eventID, 0);
        }

        #endregion Thiết lập lại số lượt phụ bản

        #region Thêm uy danh

        /// <summary>
        /// Thêm uy danh cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="value">Số uy danh thêm vào</param>
        private static void AddPrestige(int targetID, int value)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddPrestige' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddPrestige(target, value);
        }

        /// <summary>
        /// Thêm uy danh cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="value">Số uy danh thêm vào</param>
        private static void AddPrestige(KPlayer player, int value)
        {
            var oldPrestige = player.Prestige;
            player.Prestige += value;
            LogManager.WriteLog(LogTypes.UyDanh,
                String.Format("PlayerID: {0} nhận được {1} uy danh ở Luascript, uy danh tăng từ {2} lên {3}",
                    player.RoleName, value, oldPrestige, player.Prestige));
        }

        #endregion Thêm uy danh

        #region Thêm danh vọng

        /// <summary>
        /// Thêm danh vọng cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="reputeID">Danh vọng</param>
        /// <param name="value">Giá trị</param>
        private static void AddRepute(int targetID, int reputeID, int value)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error, "Process GM Command 'AddRepute' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddRepute(target, reputeID, value);
        }

        /// <summary>
        /// Thêm danh vọng cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="reputeID">Danh vọng</param>
        /// <param name="value">Số uy danh thêm vào</param>
        private static void AddRepute(KPlayer player, int reputeID, int value)
        {
            KTGlobal.AddRepute(player, reputeID, value);
        }

        #endregion Thêm danh vọng

        #region Thêm/xóa danh hiệu nhân vật

        /// <summary>
        /// Thêm/xóa danh hiệu nhân vật cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="method">Loại thao tác (-1: Xóa, 1: Thêm)</param>
        /// <param name="titleID">ID danh hiệu</param>
        private static void ModRoleTitle(int targetID, int method, int titleID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ModRoleTitle' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ModRoleTitle(target, method, titleID);
        }

        /// <summary>
        /// Thêm/xóa danh hiệu nhân vật cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="method">Loại thao tác (-1: Xóa, 1: Thêm)</param>
        /// <param name="titleID">ID danh hiệu</param>
        private static void ModRoleTitle(KPlayer player, int method, int titleID)
        {
            // Nếu là thao tác thêm
            if (method == 1)
            {
                player.AddRoleTitleOld(titleID);
            }
            // Nếu là thao tác xóa
            else if (method == -1)
            {
                player.RemoveRoleTitle(titleID);
            }
        }

        #endregion Thêm/xóa danh hiệu nhân vật

        #region Thiết lập số lượt chúc phúc

        /// <summary>
        /// Thiết lập số lượt chúc phúc cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="value">Giá trị</param>
        private static void SetPrayTimes(int targetID, int value)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetPrayTimes' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetPrayTimes(target, value);
        }

        /// <summary>
        /// Thiết lập số lượt chúc phúc cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="value">Giá trị</param>
        private static void SetPrayTimes(KPlayer player, int value)
        {
            KTPlayerPrayManager.SetTotalTurnLeft(player, value);
        }

        #endregion Thiết lập số lượt chúc phúc

        #region Thêm điểm tích lũy tháng Tiêu Dao Cốc

        /// <summary>
        /// Thêm điểm tích lũy tháng Tiêu Dao Cốc cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="value">Giá trị</param>
        private static void AddXoYoMonthStoragePoint(int targetID, int value)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddXoYoMonthStoragePoint' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddXoYoMonthStoragePoint(target, value);
        }

        /// <summary>
        /// Thêm điểm tích lũy tháng Tiêu Dao Cốc cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="value">Giá trị</param>
        private static void AddXoYoMonthStoragePoint(KPlayer player, int value)
        {
            XoYo_EventScript.AddCurrentMonthStoragePoint(player, value);
        }

        #endregion

        #region Thiết lập sẽ quay vào rương xấu xí ở Bách Bảo Rương lượt tiếp theo

        /// <summary>
        /// Thiết lập sẽ quay vào rương xấu xí ở Bách Bảo Rương lượt tiếp theo cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="bet">Số sò cược tương ứng</param>
        private static void SetSeashellTreasureNextTurn(int targetID, int bet)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetSeashellTreasureNextTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetSeashellTreasureNextTurn(target, bet);
        }

        /// <summary>
        /// Thiết lập sẽ quay vào rương xấu xí ở Bách Bảo Rương lượt tiếp theo cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="bet">Số sò cược tương ứng</param>
        private static void SetSeashellTreasureNextTurn(KPlayer player, int bet)
        {
            player.GM_SetWillGetTreasureNextTurn = true;
            player.GM_SetWillGetTreasureNextTurnWithBet = bet;
        }

        #endregion

        #region Thêm kinh nghiệm Tu Luyện Châu

        /// <summary>
        /// Thêm kinh nghiệm Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void AddXiuLianZhu_Exp(int targetID, int exp)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddXiuLianZhu_Exp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddXiuLianZhu_Exp(target, exp);
        }

        /// <summary>
        /// Thêm kinh nghiệm Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void AddXiuLianZhu_Exp(KPlayer player, int exp)
        {
            player.XiuLianZhu_Exp += exp;
        }

        #endregion

        #region Thiết lập kinh nghiệm Tu Luyện Châu

        /// <summary>
        /// Thiết lập kinh nghiệm Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void SetXiuLianZhu_Exp(int targetID, int exp)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetXiuLianZhu_Exp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetXiuLianZhu_Exp(target, exp);
        }

        /// <summary>
        /// Thiết lập kinh nghiệm Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="exp">Kinh nghiệm</param>
        private static void SetXiuLianZhu_Exp(KPlayer player, int exp)
        {
            player.XiuLianZhu_Exp = exp;
        }

        #endregion

        #region Thiết lập số giờ Tu Luyện Châu

        /// <summary>
        /// Thiết lập số giờ Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="exp">Số giờ (* 10)</param>
        private static void SetXiuLianZhu_TimeLeft(int targetID, int hour10)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'AddXiuLianZhu_Exp' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetXiuLianZhu_TimeLeft(target, hour10);
        }

        /// <summary>
        /// Thiết lập số giờ Tu Luyện Châu cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="exp">Số giờ (* 10)</param>
        private static void SetXiuLianZhu_TimeLeft(KPlayer player, int hour10)
        {
            player.XiuLianZhu_TotalTime = hour10;
        }

        #endregion

        #region Trả về thông tin điểm tích lũy Tiêu Dao tháng trước và tháng này

        /// <summary>
        /// Trả về thông tin điểm tích lũy Tiêu Dao tháng trước và tháng này của người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        private static void GetXoYoMonthStoragePoint(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetXoYoMonthStoragePoint' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.GetXoYoMonthStoragePoint(target);
        }

        /// <summary>
        /// Trả về thông tin điểm tích lũy Tiêu Dao tháng trước và tháng này của người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        private static void GetXoYoMonthStoragePoint(KPlayer player)
        {
            var lastMonthStoragePoint = XoYo_EventScript.GetLastMonthStoragePoint(player);
            var isReceivedLastMonthStorageAward = XoYo_EventScript.HasAlreadyGottenLastMonthAward(player);
            var currentMonthStoragePoint = XoYo_EventScript.GetCurrentMonthStoragePoint(player);

            PlayerManager.ShowNotification(player,
                string.Format("Tháng trước: {0} ({2}), tháng này: {1}", lastMonthStoragePoint, currentMonthStoragePoint,
                    isReceivedLastMonthStorageAward ? "Đã nhận thưởng" : "Chưa nhận thưởng"));
        }

        #endregion

        #region Thiết lập trạng thái nhận thưởng liên đấu

        /// <summary>
        /// Thiết lập trạng thái nhận thưởng liên đấu cho chiến đội của người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        /// <param name="state">Trạng thái (0: chưa nhận, 1: đã nhận)</param>
        private static void SetTeamBattleAwardState(int targetID, int state)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'UpdateTeamBattleAwardState' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetTeamBattleAwardState(target, state);
        }

        /// <summary>
        /// Thiết lập trạng thái nhận thưởng liên đấu cho chiến đội của người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        /// <param name="state">Trạng thái (0: chưa nhận, 1: đã nhận)</param>
        private static void SetTeamBattleAwardState(KPlayer player, int state)
        {
            TeamBattle_ActivityScript.UpdateTeamAwardState(player, state == 1);
        }

        #endregion

        #region Mở Captcha

        /// <summary>
        /// Mở Captcha cho người chơi tương ứng
        /// </summary>
        /// <param name="targetID">ID đối tượng</param>
        private static void GenerateCaptcha(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GenerateCaptcha' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.GenerateCaptcha(target);
        }

        /// <summary>
        /// Mở Captcha cho người chơi tương ứng
        /// </summary>
        /// <param name="player">Người chơi</param>
        private static void GenerateCaptcha(KPlayer player)
        {
            player.GenerateCaptcha();
        }

        #endregion

        #region Thông tin Tần Lăng

        /// <summary>
        /// Trả về thông tin Tần Lăng trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player">Đối tượng theo dõi</param>
        /// <param name="targetID">ID người chơi cần</param>
        private static void GetEmperorTombData(KPlayer player, int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetEmperorTombData' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.GetEmperorTombData(player, target);
        }

        /// <summary>
        /// Trả về thông tin Tần Lăng trong ngày của người chơi tương ứng
        /// </summary>
        /// <param name="player">Đối tượng theo dõi</param>
        /// <param name="target">Người chơi</param>
        private static void GetEmperorTombData(KPlayer player, KPlayer target)
        {
            // Tổng số giờ còn lại
            var timeLeft = target.GetValueOfDailyRecore((int)AcitvityRecore.EmperorTomb_TodaySecLeft);
            if (timeLeft == -1)
            {
                timeLeft = EmperorTomb.Config.DurationPerDay;
            }
            else if (timeLeft < 0)
            {
                timeLeft = 0;
            }

            // Dữ liệu tầng
            var stageData = target.GetValueOfDailyRecore((int)AcitvityRecore.EmperorTomb_TodayData);
            string stagePassedString;
            if (stageData == -1)
            {
                stagePassedString = "Chưa có";
            }
            else
            {
                var str = new List<char>();
                foreach (var c in stageData.ToString())
                {
                    str.Add(c);
                }

                stagePassedString = string.Join(", ", str);
            }

            // Thông tin
            var infoText = string.Format("Thời gian còn: {0}, Tầng đã qua: {1}", timeLeft / 1000, stagePassedString);
            // Hiện thông tin
            PlayerManager.ShowNotification(player, infoText);
        }

        #endregion

        #region Reset số giờ Tần Lăng

        /// <summary>
        /// Reset số giờ còn lại trong Tần Lăng của người chơi tương ứng
        /// </summary>
        /// <param name="targetID"></param>
        public static void ResetEmperorTombTimeLeft(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'ResetEmperorTombTimeLeft' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetEmperorTombTimeLeft(target);
        }

        /// <summary>
        /// Reset số giờ còn lại trong Tần Lăng của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        public static void ResetEmperorTombTimeLeft(KPlayer player)
        {
            player.SetValueOfDailyRecore((int)AcitvityRecore.EmperorTomb_TodaySecLeft, -1);
        }

        #endregion

        #region Truy vấn Vòng quay may mắn

        /// <summary>
        /// Thiết lập số lượt đã quay Vòng quay may mắn của người chơi tương ứng
        /// </summary>
        /// <param name="targetID"></param>
        /// <param name="totalTurn"></param>
        public static void SetLuckyCircleTotalTurn(int targetID, int totalTurn)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'SetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetLuckyCircleTotalTurn(target, totalTurn);
        }

        /// <summary>
        /// Thiết lập số lượt đã quay Vòng quay may mắn của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="totalTurn"></param>
        public static void SetLuckyCircleTotalTurn(KPlayer player, int totalTurn)
        {
            player.LuckyCircle_TotalTurn = totalTurn;
        }

        /// <summary>
        /// Thiết lập số lượt đã quay Vòng quay may mắn của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetID"></param>
        public static void GetLuckyCircleTotalTurn(KPlayer player, int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            // Kiểm tra đối tượng
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.GetLuckyCircleTotalTurn(player, target);
        }

        /// <summary>
        /// Thiết lập số lượt đã quay Vòng quay may mắn của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        public static void GetLuckyCircleTotalTurn(KPlayer player, KPlayer target)
        {
            PlayerManager.ShowNotification(player,
                string.Format("Tổng số lượt đã quay: {0}", target.LuckyCircle_TotalTurn));
        }

        #endregion

        #region Hoạt động hằng ngày

        public static void ResetAllActivity(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetAllActivity(target);
        }

        public static void ResetAllActivity(KPlayer player)
        {
            player._DailyDataRecoreActivity.DayID -= 1;
            player._MonthDataRecore.MonthID -= 1;
            DayActivityManager.ResetDay(player);
            GlobalEventSource.getInstance().fireEvent(new PlayerLogonEventObject(player));
        }

        public static void SetScoreDayActivity(KPlayer player, int Score)
        {
            var data = player._DailyDataRecoreActivity;
            data.ScoreDay = Score;
        }

        public static void SetScoreDayActivity(int targetID, int Score)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetScoreDayActivity(target, Score);
        }

        public static void SetScoreMonthActivity(KPlayer player, int Score)
        {
            var data = player._MonthDataRecore;
            data.ScoreMonth = Score;
        }

        public static void SetScoreMonthActivity(int targetID, int Score)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.SetScoreMonthActivity(target, Score);
        }

        public static void AddScoreDayActivity(KPlayer player, int Score)
        {
            var data = player._DailyDataRecoreActivity;
            data.ScoreDay += Score;
        }

        public static void AddScoreDayActivity(int targetID, int Score)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddScoreDayActivity(target, Score);
        }

        public static void AddScoreMonthActivity(KPlayer player, int Score)
        {
            var data = player._MonthDataRecore;
            data.ScoreMonth += Score;
        }

        public static void AddScoreMonthActivity(int targetID, int Score)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.AddScoreMonthActivity(target, Score);
        }

        public static void ResetGiftActivityDay(KPlayer player)
        {
            var data = player._DailyDataRecoreActivity;
            for (var i = 0; i < data.DayActivityAwardStep.Count; i++)
            {
                data.DayActivityAwardStep[i] = false;
            }
        }

        public static void ResetGiftActivityDay(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetGiftActivityDay(target);
        }

        public static void ResetGiftActivityMonth(KPlayer player)
        {
            var data = player._MonthDataRecore;
            for (var i = 0; i < data.MonthActivityAwardStep.Count; i++)
            {
                data.MonthActivityAwardStep[i] = false;
            }
        }

        public static void ResetGiftActivityMonth(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.ResetGiftActivityMonth(target);
        }

        public static void ResetMission(KPlayer player)
        {
            DayActivityManager.ResetMission(player);
        }

        public static void ResetMission(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            DayActivityManager.ResetMission(target);
        }

        /// <summary>
        /// Thay đổi DayID để thử TH sang ngày mới mà vẫn còn trong game
        /// </summary>
        public static void FakeNewDay(KPlayer player)
        {
            player._DailyDataRecoreActivity.DayID -= 1;
            player._DailyDataRecore.DayID -= 1;
        }

        /// <summary>
        /// Thay đổi DayID để thử TH sang ngày mới mà vẫn còn trong game
        /// </summary>
        public static void FakeNewDay(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            FakeNewDay(target);
        }

        public static void FakeNewMonth(KPlayer player)
        {
            player._DailyDataRecoreActivity.DayID -= 1;
            player._DailyDataRecore.DayID -= 1;
            player._MonthDataRecore.MonthID -= 1;
        }

        /// <summary>
        /// Thay đổi MonthID để thử TH sang ngày mới mà vẫn còn trong game
        /// </summary>
        public static void FakeNewMonth(int targetID)
        {
            var target = KTGMCommandManager.FindPlayer(targetID);
            if (target == null)
            {
                LogManager.WriteLog(LogTypes.Error,
                    "Process GM Command 'GetLuckyCircleTotalTurn' Error...\n" + "Target not found.");
                return;
            }

            KTGMCommandManager.FakeNewMonth(target);
        }

        #endregion

        #endregion Thực thi lệnh GM
    }
}
