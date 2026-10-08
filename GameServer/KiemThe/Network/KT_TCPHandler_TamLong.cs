using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using System.Web;
using GameServer.Core.Executor;
using GameServer.KiemThe.GameDbController;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý UI
    /// </summary>
    public static partial class KT_TCPHandler
    {
        #region Tam Long Tranh Bá

        public static void InitTamLong()
        {
            // đọc log event từ file json 10'/lần
            // json được generate từ script python
            ScheduleExecutor2.Instance.scheduleCancle("TopTamLongRanking");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopGiftRanking", ReadTopTamLongRanking), 1000,
                60000);
        }

        private static string TopSongLongXML;
        public static void ReadTopTamLongRanking() 
        {
            TopSongLongXML = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_SONGLONG_VSL, true);
        }

        public static TCPProcessCmdResults ResponseTamLongResult(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            string cmdData;
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new UTF8Encoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error,
                    string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID,
                        Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // Tìm chủ nhân của gói tin tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');

                if (!int.TryParse(fields[0], out var action))
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (action == 0)
                {
                    // lấy thông tin chiến đội
                    var teamInfoEncoded = Global.GetRoleParamsStringWithDB(client, RoleParamName.TamLongTeam);
                    if (string.IsNullOrEmpty(teamInfoEncoded))
                    {
                        var returnData = $"0::";
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, returnData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    var teamInfo = DataHelper.DecodeBase64(teamInfoEncoded);
                    var teamFields = teamInfo.Split(':'); 
                    // 0: tên chiến đội
                    // 1: id đội trưởng
                    // 2 -> 5: thông tin thành viên: id_tên_cấp
                    var memberIDs = new List<int>();
                    for (var i = 2; i < teamFields.Length; ++i)
                    {
                        var memberFields = teamFields[i].Split('_');
                        var memberID = int.Parse(memberFields[0]);
                        
                        memberIDs.Add(memberID);
                    }

                    var result = $"0:{teamInfoEncoded}";
                    try
                    {
                        var output = Global.SendToDB<Dictionary<int, int>, List<int>>(
                            (int)TCPGameServerCmds.CMD_KT_TAMLONG_DB,
                            memberIDs,
                            GameManager.ServerId);

                        var outputEncoded =
                            DataHelper.EncodeBase64(string.Join(":", output.Select(x => $"{x.Key}_{x.Value}")));
                        result = $"0:{teamInfoEncoded}:{outputEncoded}";
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, result, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 1)
                {
                    //if (client.TeamID == -1)
                    //{
                    //    PlayerManager.ShowMessageBox(client, "Thông báo",
                    //        "Bạn cần phải là đội trưởng của tổ đội 2 người để báo danh.");
                    //    return TCPProcessCmdResults.RESULT_OK;
                    //}
                    //
                    //if (client.Teammates.Count != 2)
                    //{
                    //    PlayerManager.ShowMessageBox(client, "Thông báo",
                    //        "Bạn cần phải là đội trưởng của tổ đội 2 người để báo danh.");
                    //    return TCPProcessCmdResults.RESULT_OK;
                    //}
                    //
                    //if (client.TeamLeader.RoleID != client.RoleID)
                    //{
                    //    PlayerManager.ShowMessageBox(client, "Thông báo",
                    //        "Bạn cần phải là đội trưởng của tổ đội 2 người để báo danh.");
                    //    return TCPProcessCmdResults.RESULT_OK;
                    //}
                    //
                    //foreach (var member in client.Teammates)
                    //{
                    //    var memberTeamInfo = Global.GetRoleParamsStringWithDB(member, RoleParamName.TamLongTeam);
                    //    if (!string.IsNullOrEmpty(memberTeamInfo))
                    //    {
                    //        PlayerManager.ShowMessageBox(client, "Thông báo",
                    //            "Thành viên trong tổ đội của bạn đã đăng ký tham gia với đội khác.");
                    //        return TCPProcessCmdResults.RESULT_OK;
                    //    }
                    //}
                    if (client.m_Level < 80)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo",
                            "Bạn phải đạt cấp độ 80 trở lên để đăng ký");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (client.GoodsDataList != null)
                    {
                        lock (client.GoodsDataList)
                        {
                            var mantle =
                                client.GoodsDataList.FirstOrDefault(x =>
                                    x.Using == 15);
                            if (mantle == null)
                            {
                                PlayerManager.ShowMessageBox(client, "Thông báo",
                                    "Bạn phải trang bị phi phong Ngự Phong trở lên");
                                return TCPProcessCmdResults.RESULT_OK;
                            }

                            var template = ItemManager.GetItemTemplate(mantle.GoodsID);

                            if (template == null)
                            {
                                PlayerManager.ShowMessageBox(client, "Thông báo",
                                    "Bạn phải trang bị phi phong Ngự Phong trở lên");
                                return TCPProcessCmdResults.RESULT_OK;
                            }

                            if (template.ItemValue < 18000000)
                            {
                                PlayerManager.ShowMessageBox(client, "Thông báo",
                                    "Bạn phải trang bị phi phong Ngự Phong trở lên");
                                return TCPProcessCmdResults.RESULT_OK;
                            }
                        }
                    }

                    var memberTeamInfo = Global.GetRoleParamsStringWithDB(client, RoleParamName.TamLongTeam);
                    if (!string.IsNullOrEmpty(memberTeamInfo))
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo",
                            "Bạn đã đăng ký tham gia trước đó.");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    //var teamMembers = string.Join(":",
                    //    client.Teammates.Select(x => $"{x.RoleID}_{x.RoleName}_{x.m_Level}"));
                    var teamMembers = $"{client.RoleID}_{client.RoleName}_{client.m_Level}";
                    var teamString = $"{fields[1]}:{client.RoleID}:{teamMembers}";
                    var teamStringEncoded = DataHelper.EncodeBase64(teamString);

                    //foreach (var teammate in client.Teammates)
                    //{
                    //    Global.SaveRoleParamsStringToDB(teammate, RoleParamName.TamLongTeam, teamStringEncoded, true);
                    //}
                    Global.SaveRoleParamsStringToDB(client, RoleParamName.TamLongTeam, teamStringEncoded, true);

                    var result = $"1:{teamStringEncoded}";
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, result, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // nộp vô song lệnh
                if (action == 2)
                {
                    if (!int.TryParse(fields[1], out var vslSubmit))
                    {
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "2:-1", nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    var vslHave = ItemManager.GetItemCountInBag(client, 20072);
                    if (vslSubmit <= 0 || vslSubmit > vslHave)
                    {
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "2:-2", nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }

                    ItemManager.RemoveItemFromBag(client, 20072, vslSubmit, -1, "TAMLONG");
                    var currentVSL = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TamLongVSL, 0);
                    currentVSL += vslSubmit;
                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TamLongVSL, currentVSL, true);
                    
                    var result = $"2:{vslSubmit}";
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, result, nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 3)
                {
                    KT_TCPHandler.SendOpenUI(client, "EventRanking", Array.Empty<int>(), new List<string>{"Bảng xếp hạng Song Long", TopSongLongXML});
                    return TCPProcessCmdResults.RESULT_OK;
                    // var output = Global.SendToDB<List<string>, string>(
                    //     (int)TCPGameServerCmds.CMD_KT_TAMLONG_TOP_RANKING,
                    //     "",
                    //     GameManager.ServerId);
                    // var result = $"3:{string.Join(":", output)}";
                    // tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, result, nID);
                    // return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 4)
                {
                    var matchID = fields[1];
                    var routeID = fields[2];
                    var banInfoEncoded = Global.GetRoleParamsStringWithDB(client, RoleParamName.TamLongBan);
                    string banInfo;
                    if (!string.IsNullOrEmpty(banInfoEncoded))
                    {
                        banInfo = DataHelper.DecodeBase64(banInfoEncoded);
                        var banFields = banInfo.Split('_');
                        foreach (var banMatchInfo in banFields) 
                        {
                            if (banMatchInfo.StartsWith($"{matchID}:"))
                            {
                                // đã có thông tin ban
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, $"4:1:{banMatchInfo}", nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                        }
                        banInfo += $"_{fields[1]}:{fields[2]}";
                    }
                    else
                    {
                        banInfo = $"{fields[1]}:{fields[2]}";
                    }

                    LogManager.WriteLog(LogTypes.SpecialEvent, $"[TAMLONG] {client.RoleName}[{client.RoleID}] cấm phái {fields[2]} trận {fields[1]}");

                    var banString = DataHelper.EncodeBase64(banInfo);
                    Global.SaveRoleParamsStringToDB(client, RoleParamName.TamLongBan, banString, true);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, $"4:0:{fields[1]}:{fields[2]}", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 5)
                {
                    var matchID = fields[1];
                    var teamOrder = fields[2];
                    var betValue = int.Parse(fields[3]);
                    var teamName = fields[4];

                    if (KTGlobal.GetMoneyhave(client, Entities.MoneyType.Dong) < betValue)
                    {
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, $"5:-1", nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    
                    var betInfoEncoded = Global.GetRoleParamsStringWithDB(client, RoleParamName.TamLongBet);
                    string betInfo;
                    if (!string.IsNullOrEmpty(betInfoEncoded))
                    {
                        betInfo = DataHelper.DecodeBase64(betInfoEncoded);
                        var betFields = betInfo.Split('_');
                        foreach (var betMatchInfo in betFields) 
                        {
                            if (betMatchInfo.StartsWith($"{matchID}:"))
                            {
                                // đã có thông tin ban
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, $"5:1:{betMatchInfo}", nID);
                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                        }
                        betInfo += $"_{fields[1]}:{fields[2]}:{fields[3]}:{fields[4]}";
                        KTGlobal.SubMoney(client, betValue, Entities.MoneyType.Dong, "TAMLONGBET");
                    }
                    else
                    {
                        betInfo = $"{fields[1]}:{fields[2]}:{fields[3]}:{fields[4]}";
                        KTGlobal.SubMoney(client, betValue, Entities.MoneyType.Dong, "TAMLONGBET");
                    }

                    LogManager.WriteLog(LogTypes.SpecialEvent, $"[TAMLONG] {client.RoleName}[{client.RoleID}] cược trận {matchID}, số tiền {betValue}, đội {teamOrder}/{teamName} thắng");

                    var betString = DataHelper.EncodeBase64(betInfo);
                    Global.SaveRoleParamsStringToDB(client, RoleParamName.TamLongBet, betString, true);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, $"5:0:{fields[1]}:{fields[2]}:{fields[3]}:{fields[4]}", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 6) 
                {
                    if (client.IsDead())
                    {
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (!ServerConfig.Instance.KuaFuServerEnable)
                    {
                        PlayerManager.ShowNotification(client, "Server chưa mở liên server, không thể tham gia");
                        return TCPProcessCmdResults.RESULT_OK;
                    }    

                    GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, client, 1616, 7545, 4454,
                        (int)client.CurrentDir, false, 9999);
                    return TCPProcessCmdResults.RESULT_OK;
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        #endregion
    }
}