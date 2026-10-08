using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.Logic;
using GameServer.Logic.Name;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Class quản lý toàn bộ packet liên quan tới bang hội
    /// </summary>
    public static partial class KT_TCPHandler
    {
        /// <summary>
        /// Tạo 1 gia tộc
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_CREATE(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.m_cPlayerFaction.GetFactionId() == 0)
                {
                    PlayerManager.ShowNotification(client, "Phải vào phái mới có thể tạo lập bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var name = fields[0];

                // Kiểm tra xem tên có hợp lệ không
                if (!Utils.CheckValidString(name))
                {
                    PlayerManager.ShowNotification(client, "Tên bang không được chứa ký tự đặc biệt");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Kiểm tra độ dài của tên có hợp lệ hay không
                if (!NameManager.Instance().IsNameLengthOK(name))
                {
                    PlayerManager.ShowNotification(client, "Tên bang phải từ 6 tới 18 ký tự");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Kiểm tra xem có tiền không
                if (!KTGlobal.IsHaveMoney(client, 1000000, Entities.MoneyType.Bac))
                {
                    PlayerManager.ShowNotification(client, "Bạc trên người không đủ");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Kiểm tra xem có gia tộc chưa
                if (client.FamilyID <= 0)
                {
                    PlayerManager.ShowNotification(client, "Bạn phải có gia tộc trước khi tạo bang");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                if (client.FamilyRank != (int)FamilyRank.Master)
                {
                    PlayerManager.ShowNotification(client, "Chỉ có tộc trưởng mới có quyền tạo bang");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // Yêu cầu 50 uy danh mới có thể đăng nhập bang hội
                if (client.Prestige <= 50)
                {
                    PlayerManager.ShowNotification(client, "50 điểm uy danh mới có thể tạo bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                //-200 Cấp độ không đủ
                if (client.m_Level < 30)
                {
                    PlayerManager.ShowNotification(client, "Cấp độ tối thiểu là 30 mới có thể tạo bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var CMDBUILD = client.RoleID + ":" + name + ":" + client.ZoneID + ":" + client.FamilyID;

                var ByteSendToDB = Encoding.ASCII.GetBytes(CMDBUILD);

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, ByteSendToDB, count, out tcpOutPacket, client.ServerId);
                if (null != tcpOutPacket)
                {
                    tcpOutPacket.GetPacketCmdData(out var strCmdResult);
                    if (null != strCmdResult)
                    {
                        var Pram = strCmdResult.Split(':');

                        var Status = int.Parse(Pram[0]);

                        if (Status == -1)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Phải ở trong gia tộc mới có thể tạo bang hội");
                        }
                        else if (Status == -2)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Phải là tộc trưởng mới có thể tạo bang hội");
                        }
                        else if (Status == -3)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Tên bang hội đã tồn tại vui lòng chọn tên khác");
                        }
                        else if (Status == -4)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn đã có bang hội không thể tạo thêm");
                        }
                        else if (Status == 0)
                        {
                            KTGlobal.SubMoney(client, 1000000, Entities.MoneyType.Bac, "CREATE GUILD");
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Tạo bang hội thành công");
                            var GUILDID = int.Parse(Pram[1]);
                            var GUILDSTR = Pram[2];

                            KPlayer gc = null;

                            var index = 0;

                            while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                            {
                                if (gc.FamilyID == client.FamilyID)
                                {
                                    gc.GuildID = GUILDID;
                                    gc.GuildName = GUILDSTR;
                                    gc.GuildRank = (int)GuildRank.Member;

                                    KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);
                                }
                            }

                            client.GuildID = GUILDID;
                            client.GuildName = GUILDSTR;
                            client.GuildRank = (int)GuildRank.Master;

                            KT_TCPHandler.NotifyOthersMyTitleChanged(client);

                            // Thông báo cập nhật thông tin gia tộc và bang hội
                            KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(client);
                        }

                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strCmdResult, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_GETINFO(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowNotification(client, "Bạn không có bang hội");

                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Lấy danh sách thành viên trong bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_GETMEMBERLIST(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowNotification(client, "Bạn không có bang hội");

                    return TCPProcessCmdResults.RESULT_OK;
                }

                var RoleID = int.Parse(fields[0]);

                var GuildID = int.Parse(fields[1]);

                var PageIndex = int.Parse(fields[2]);

                if (client.GuildID != GuildID)
                {
                    PlayerManager.ShowNotification(client, "Bạn không phải là thành viên của bang hội");

                    return TCPProcessCmdResults.RESULT_OK;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Thay đổi chức vụ cho thành viên trong bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_CHANGERANK(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Gửi lên 2 cái gồm ID ROLE CUA THANG MUỐN SET, RANKMUONSET
                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không ở 1 bang hội nào");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildRank != (int)GuildRank.Master && client.GuildRank != (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ và phó bang chủ mới có quyền thanh đổi chức danh cho thành viên");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var RoleID = int.Parse(fields[0]);

                var RankSet = int.Parse(fields[1]);

                // Nếu bổ nhiệm cùng chức với mình
                if (RoleID == client.RoleID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không thể bổ nhiệm chính mình!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var findrole = GameManager.ClientMgr.FindClient(RoleID);

                if (findrole == null)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Người được thay đổi chức vụ phải đang online");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Nếu muốn bổ nhiệm làm phó bang chủ
                if (RankSet == (int)GuildRank.ViceMaster)
                {
                    // Nếu người chơi không phải tộc trưởng của gia tộc khác
                    if (findrole.FamilyRank != (int)FamilyRank.Master)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Phó Bang chủ bổ nhiệm phải là tộc trưởng của gia tộc thành viên!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                if (findrole.FamilyRank == (int)FamilyRank.Master)
                {
                    if (RankSet == (int)GuildRank.Elite || RankSet == (int)GuildRank.Member)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Tộc trưởng của một tộc không thể bị hạ chức vụ");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }
                // Nếu bản thân là phó bang chủ mà chức vụ bổ nhiệm lại là bang chủ hoặc phó bang chủ
                if ((RankSet == (int)GuildRank.ViceMaster || RankSet == (int)GuildRank.Master) && client.GuildRank == (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có quyền bổ nhiệm chức vụ này!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Nếu muốn nhường chức bang chủ cho người này
                if (RankSet == (int)GuildRank.Master)
                {
                    if (findrole != null)
                    {
                        if (findrole.FamilyID <= 0)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Người kế nhiệm bang chủ phải thuộc 1 gia tộc trong bang");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else
                        {
                            if (findrole.FamilyRank != (int)FamilyRank.Master)
                            {
                                PlayerManager.ShowMessageBox(client, "Thông báo", "Người kế nhiệm bang chủ phải là tộc trưởng");
                                return TCPProcessCmdResults.RESULT_OK;
                            }
                            else
                            {
                                // THỰC HIỆN HẠ CHỨC BANG CHỦ XUỐNG TINH ANH

                                var CMDBUILD = client.RoleID + ":" + client.GuildID + ":" + (int)GuildRank.Elite;

                                var ByteSendToDB = Encoding.ASCII.GetBytes(CMDBUILD);

                                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, ByteSendToDB, count, out tcpOutPacket, client.ServerId);

                                if (null != tcpOutPacket)
                                {
                                    tcpOutPacket.GetPacketCmdData(out var strCmdResult);
                                    if (null != strCmdResult)
                                    {
                                        var Pram = strCmdResult.Split(':');

                                        var Status = int.Parse(Pram[0]);

                                        if (Status > 0)
                                        {
                                            var CMDBUILD2 = findrole.RoleID + ":" + findrole.GuildID + ":" + (int)GuildRank.Ambassador;

                                            var ByteSendToDB2 = Encoding.ASCII.GetBytes(CMDBUILD2);

                                            //SEND TIẾP PHÁT NỮA CHUYỂN THẰNG KAI LÊN BANG CHỦ
                                            result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, ByteSendToDB2, count, out var tcpOutPacket2, client.ServerId);

                                            tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                                            if (strCmdResult2 != null)
                                            {
                                                var Pram2 = strCmdResult2.Split(':');

                                                Status = int.Parse(Pram2[0]);

                                                if (Status > 0)
                                                {
                                                    // THỰC HIỆN SET RANK LẦN CUỐI
                                                    client.GuildRank = (int)GuildRank.Elite;

                                                    findrole.GuildRank = (int)GuildRank.Ambassador;

                                                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chuyển giao chức vụ thành công");

                                                    // Thông báo lại cho Client
                                                    {
                                                        // Thông báo danh hiệu thay đổi
                                                        KT_TCPHandler.NotifyOthersMyTitleChanged(client);

                                                        // Thông báo cập nhật thông tin gia tộc và bang hội
                                                        KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(client);

                                                        var responseData = string.Format("{0}", client.GuildRank);
                                                        client.SendPacket(nID, responseData);
                                                    }
                                                    // Thông báo cho thằng kia
                                                    {
                                                        // Thông báo danh hiệu thay đổi
                                                        KT_TCPHandler.NotifyOthersMyTitleChanged(findrole);

                                                        // Thông báo cập nhật thông tin gia tộc và bang hội
                                                        KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(findrole);

                                                        var responseData = string.Format("{0}", findrole.GuildRank);
                                                        findrole.SendPacket(nID, responseData);
                                                    }

                                                    return TCPProcessCmdResults.RESULT_OK;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi trong quá tình chuyển giao");

                                            return TCPProcessCmdResults.RESULT_OK;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Người kế nhiệm không online không thể chuyển lại chức vụ");

                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {
                    // Nếu muốn set cho thằng này là phó bang chủ nhưng thằng này hiện tại lại ko phải phó bang chủ
                    if (RankSet == (int)GuildRank.Ambassador && findrole.GuildRank != (int)GuildRank.ViceMaster)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Trưởng lão là TRƯỞNG TỘC của các tộc khác,không thể thay đổi sang chức này");

                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else if (RankSet == (int)GuildRank.ViceMaster || RankSet == (int)GuildRank.Elite || RankSet == (int)GuildRank.Member)
                    {
                        var CMDBUILD2 = findrole.RoleID + ":" + findrole.GuildID + ":" + RankSet;

                        var ByteSendToDB2 = Encoding.ASCII.GetBytes(CMDBUILD2);

                        //SEND TIẾP PHÁT NỮA CHUYỂN THẰNG KAI LÊN BANG CHỦ
                        var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, ByteSendToDB2, count, out var tcpOutPacket2, client.ServerId);

                        tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                        if (strCmdResult2 != null)
                        {
                            var Pram2 = strCmdResult2.Split(':');

                            var Status = int.Parse(Pram2[0]);

                            if (Status > 0)
                            {
                                findrole.GuildRank = RankSet;

                                PlayerManager.ShowMessageBox(client, "Thông báo", "Thay đổi chức vụ cho thành viên thành công");

                                // Thông báo cho bản thân
                                {
                                    var responseData = string.Format("{0}", -1);
                                    client.SendPacket(nID, responseData);
                                }
                                // Thông báo cho thằng kia
                                {
                                    // Thông báo danh hiệu thay đổi
                                    KT_TCPHandler.NotifyOthersMyTitleChanged(findrole);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(findrole);

                                    var responseData = string.Format("{0}", findrole.GuildRank);
                                    findrole.SendPacket(nID, responseData);
                                }

                                return TCPProcessCmdResults.RESULT_OK;
                            }
                            else
                            {
                                PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi khi thay đổi chức vụ cho thành viên");

                                return TCPProcessCmdResults.RESULT_OK;
                            }
                        }
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Kick 1 tộc nào đó
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_KICKFAMILY(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);

                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var GUILDID = int.Parse(fields[0]);

                var FAMILYID = int.Parse(fields[1]);

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không thuộc 1 bang hội nào");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildID != GUILDID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không ở bang hội này");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildRank != (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ mới có quyền trục xuất gia tộc");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.FamilyID == FAMILYID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không thể tự trục xuất gia tộc của mình!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status == 0)
                    {
                        var index = 0;

                        KPlayer gc = null;

                        // Duyệt tất cả những thằng thuộc gia tộc bị kick để upate lại tên cho nó
                        while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                        {
                            if (gc.FamilyID == FAMILYID)
                            {
                                gc.GuildID = 0;
                                gc.GuildName = "";
                                gc.GuildRank = (int)GuildRank.Member;

                                // Thông báo cho thằng này
                                {
                                    // Thông báo danh hiệu thay đổi
                                    KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);

                                    var _responseData = string.Format("{0}", 1);
                                    gc.SendPacket(nID, _responseData);
                                }
                            }
                        }

                        // gửi thông báo tới gia tộc là đã kick thằng này thành công
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Trục xuất gia tộc thành công");

                        // Thông báo lại cho Client
                        var responseData = "-1";
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Gia tộc muốn kick không nằm trong bang của bạn");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Gói tin khi mở khung ưu tú
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_GETGIFTED(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var RoleID = int.Parse(fields[0]);

                var GuildID = int.Parse(fields[1]);

                var PageIndex = int.Parse(fields[2]);

                if (client.GuildID != GuildID)
                {
                    PlayerManager.ShowNotification(client, "Bạn không phải là thành viên của bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_OK;
        }

        /// <summary>
        /// Thực hiện vote ưu tú cho ai đó
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_VOTEGIFTED(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var RoleID = int.Parse(fields[0]);

                var GuildID = int.Parse(fields[1]);

                var RoleReviceVote = int.Parse(fields[2]);

                if (client.GuildID != GuildID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // TODO check thằng này đã vote cho đứa khác chưa

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Bầu chọn cho thành viên thành công");

                        // Gửi lại phản hồi
                        var responseData = string.Format("{0}", Pram2[1]);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else if (Status == -1)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Mỗi tuần bạn chỉ có thể bầu chọn cho 1 người");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗ khi bầu chọn cho thành viên này vui lòng thử lại sau");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Cống hiến vào bang
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_DONATE(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var MoneyWantDonate = int.Parse(fields[2]);

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn phải gia nhập bang hội trước!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (MoneyWantDonate <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Số bạc cống hiến phải lớn hơn 0");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                if (!KTGlobal.IsHaveMoney(client, MoneyWantDonate, MoneyType.Bac))
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Số bạc trong người không đủ");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var RoleID = int.Parse(fields[0]);

                var GuildID = int.Parse(fields[1]);

                var MoneyDonate = int.Parse(fields[2]);

                if (client.GuildID != GuildID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        KTGlobal.SubMoney(client, MoneyDonate, MoneyType.Bac, "DONATEGUILD");

                        PlayerManager.ShowMessageBox(client, "Thông báo", "Cống hiến bạc vào bang hội thành công!");
                        KTGlobal.SendGuildChat(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client.GuildID, $"đã cống hiến <color=yellow>{MoneyDonate}</color> bạc vào bang hội.", client, client.RoleName);

                        var responseData = string.Format("{0}", int.Parse(Pram2[1]));
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else if (Status == -5)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Số tiền trong bang đã quá nhiều không thể donate thêm");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else if (Status == -2)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Bang không tồn tại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi khi thực hiện cống hiến cho bang");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_OFFICE_RANK(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var ROLEID = int.Parse(fields[0]);

                var GUILDID = int.Parse(fields[1]);

                if (client.GuildID != GUILDID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không phải là người bang này");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_TERRITORY(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_SETCITY(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var GUILDID = int.Parse(fields[0]);

                var MAPID = int.Parse(fields[1]);

                if (client.GuildID != GUILDID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Nếu bản thân không có bang
                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowNotification(client, "Bạn không có bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // Nếu không phải bang chủ hoặc phó bang chủ
                else if (client.GuildRank != (int)GuildRank.Master && client.GuildRank != (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowNotification(client, "Chỉ có bang chủ và phó bang chủ mới có thể thao tác!");
                    return TCPProcessCmdResults.RESULT_OK;
                }


                if (GuidWarManager.getInstance().IsTanThuThon(MAPID))
                {

                    PlayerManager.ShowNotification(client, "Tân thủ thôn không thể thiết lập làm thành chính\nChiếm được tân thủ thôn là bước đầu để có thể Tuyên Chiến các lãnh thổ khác!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thành chính thành công");
                        // Gói tin gửi lại
                        var responseData = "";
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else if (Status == -3)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Quỹ bang không đủ 3000000 không thể thiết lập thành chính");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else if (Status == -1)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Bang hội không tồn tại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else if (Status == -2)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Bản đồ lãnh thổ không tồn tại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi khi thiết lập thành chiinsh");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_SETTAX(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                var GUILDID = int.Parse(fields[0]);

                var MAPID = int.Parse(fields[1]);

                var Tax = int.Parse(fields[2]);

                if (client.GuildID != GUILDID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Nếu bản thân không có bang
                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowNotification(client, "Bạn không có bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // Nếu không phải bang chủ hoặc phó bang chủ
                else if (client.GuildRank != (int)GuildRank.Master && client.GuildRank != (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowNotification(client, "Chỉ có bang chủ và phó bang chủ mới có thể thao tác!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thuế cho lãnh thổ thành công");
                        // Gói tin gửi lại
                        var responseData = "";
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi khi thực hiện cống hiến cho bang");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Thực hiện giải tán bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_QUIT(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có bang");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildRank != (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ mới có quyền giải tán bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        var index = 0;

                        KPlayer gc = null;

                        // Duyệt tất cả những thằng thuộc gia tộc bị kick để upate lại tên cho nó
                        while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                        {
                            if (gc.GuildID == client.GuildID)
                            {
                                gc.RoleGuildMoney = 0;
                                gc.GuildID = 0;
                                gc.GuildName = "";

                                gc.GuildRank = (int)GuildRank.Member;

                                // Cập nhật danh hiệu
                                KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                // Thông báo cập nhật thông tin gia tộc và bang hội
                                KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);

                                // Thông báo gia tộc bị kick
                                gc.SendPacket((int)TCPGameServerCmds.CMD_KT_GUILD_KICKFAMILY, "1");
                            }
                        }

                        PlayerManager.ShowMessageBox(client, "Thông báo", "Giải tán bang hội thành công");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Giải tán bang hội thất bại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Thực hiện gia tộc thoát khỏi bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_FAMILYQUIT(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có bang!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.FamilyID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có gia tộc!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Nếu không phải tộc trưởng
                if (client.FamilyRank != (int)FamilyRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có tộc trưởng mới có thể thoát khỏi bang!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Nếu bản thân là bang chủ
                if (client.GuildRank == (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Muốn thoát gia tộc khỏi bang, cần giải tán bang hội trước!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var dbCmd = string.Format("{0}:{1}", client.GuildID, client.FamilyID);
                var dbCmdByteData = new UTF8Encoding().GetBytes(dbCmd);

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, (int)TCPGameServerCmds.CMD_KT_GUILD_KICKFAMILY, dbCmdByteData, dbCmdByteData.Length, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status == 0)
                    {
                        var index = 0;

                        KPlayer gc = null;

                        // Duyệt tất cả những thằng thuộc gia tộc bị kick để upate lại tên cho nó
                        while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                        {
                            if (gc.FamilyID == client.FamilyID)
                            {
                                gc.RoleGuildMoney = 0;
                                gc.GuildID = 0;
                                gc.GuildName = "";

                                gc.GuildRank = (int)GuildRank.Member;

                                // Cập nhật danh hiệu
                                KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                // Thông báo cập nhật thông tin gia tộc và bang hội
                                KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);

                                // Thông báo gia tộc bị kick
                                gc.SendPacket((int)TCPGameServerCmds.CMD_KT_GUILD_KICKFAMILY, "1");
                            }
                        }

                        PlayerManager.ShowMessageBox(client, "Thông báo", $"Thoát khỏi bang hội thành công!\nGia tộc sau khi rời bang cần đợi {JOIN_NEW_FAMILY_INTERVAL_HOURS} giờ mới có thể vào lại bang!!!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thoát khỏi bang hội thất bại!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_CHANGE_MAXWITHDRAW(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var GuildID = int.Parse(fields[0]);

                var Percent = int.Parse(fields[1]);

                if (client.GuildID != GuildID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn phải trong bang");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Nếu không phải bang chủ hoặc phó bang chủ
                if (client.GuildRank != (int)GuildRank.Master && client.GuildRank != (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ hoặc phó bang chủ mới có thể thiết lập lợi tức bang!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (Percent < 50 || Percent > 100)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Lợi tức tối thiểu phải là 50%, tối đa là 100%");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thành công");

                        // Gói tin gửi lại
                        var responseData = string.Format("{0}", Percent);
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseData, nID);

                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thất bại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_CHANGE_NOTIFY(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            GuildChangeSlogan changeSlogan = null;
            try
            {
                // Giải mã gói tin đẩy về
                changeSlogan = DataHelper.BytesToObject<GuildChangeSlogan>(data, 0, data.Length);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Tôn chỉ
                var msg = changeSlogan.Slogan;

                // Nếu không có bang
                if (client.GuildID <= 0 || client.GuildID != changeSlogan.GuildID)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có bang!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // Nếu không phải bang chủ hoặc phó bang chủ
                else if (client.GuildRank != (int)GuildRank.Master && client.GuildRank != (int)GuildRank.ViceMaster)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ hoặc phó bang chủ mới có thể sửa tôn chỉ bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (msg.Length > 400)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Thông báo không thể vượt quá 400 ký tự");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // byte[] dbData = DataHelper.ObjectToBytes<GuildChangeSlogan>(changeSlogan);
                var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                if (strCmdResult2 != null)
                {
                    var Pram2 = strCmdResult2.Split(':');

                    var Status = int.Parse(Pram2[0]);

                    if (Status > 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thành công");

                        // Gói tin gửi lại
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Thiết lập thất bại");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Lấy ra danh sách cổ tức
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_GETSHARE(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // trả vào DB xử lý
                return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Gửi đơn xin gia nhập bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_ASKJOIN(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (GameManager.IsKuaFuServer)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không thể mời bang hội ở liên máy chủ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var GUILDWANTJOIN = int.Parse(fields[0]);

                var ROLEID = int.Parse(fields[1]);

                var RoleIDDest = GameManager.ClientMgr.FindClient(ROLEID);

                if (RoleIDDest == null)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phượng đã offline");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {
                    if (client.FamilyID <= 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có gia tộc");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (client.FamilyRank != (int)FamilyRank.Master)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có tộc trưởng mới có quyền xin vào bang");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (RoleIDDest.GuildID <= 0)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phương không có bang hội");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    if (RoleIDDest.GuildRank != (int)GuildRank.Master)
                    {
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phương không phải là bang chủ");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    // Thằng xin vào | Gia tộc của nó | Tên gia tộc
                    var responseData = string.Format("{0}:{1}:{2}:{3}", client.RoleID, client.RoleName, client.FamilyID, client.FamilyName);

                    // Gửi cho thằng kia biết là đang có người xin vào bang
                    RoleIDDest.SendPacket(nID, responseData);

                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đẫ gửi đơn xin gia nhập bang tới " + RoleIDDest.RoleName + "\nVui lòng chờ phản hồi");

                    return TCPProcessCmdResults.RESULT_OK;
                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        // <summary>
        //  Trả lời đơn xin gia nhập bang hội
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_RESPONSEASK(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 4)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildRank != (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có quyền cho tộc khác tham gia");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // ĐỒNG Ý HAY KO
                var Appect = int.Parse(fields[0]);

                // ID CỦA THẰNG ĐÃ XIN VÀO
                var RoleID = int.Parse(fields[1]);

                //ID TỘC CỦA THẰNG MUỐN XIN VÀO
                var FamilyID = int.Parse(fields[2]);

                // ID BANG CỦA BẢN THÂN
                var GuildID = int.Parse(fields[3]);

                // nếu là 0 thì là từ chối

                var WantJoin = GameManager.ClientMgr.FindClient(RoleID);

                if (Appect == 0)
                {
                    if (WantJoin != null)
                    {
                        PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Đối phương từ chối đơn xin gia nhập bang hội");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }
                else
                {
                    var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                    tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                    if (strCmdResult2 != null)
                    {
                        var Pram2 = strCmdResult2.Split(':');

                        var Status = int.Parse(Pram2[0]);

                        if (Status > 0)
                        {
                            var index = 0;

                            KPlayer gc = null;

                            // Duyệt tất cả những thằng thuộc gia tộc bị kick để upate lại tên cho nó
                            while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                            {
                                if (gc.FamilyID == WantJoin.FamilyID)
                                {
                                    gc.RoleGuildMoney = 0;
                                    gc.GuildID = client.GuildID;
                                    gc.GuildName = client.GuildName;

                                    if (gc.RoleID == WantJoin.RoleID)
                                    {
                                        gc.GuildRank = (int)GuildRank.Ambassador;
                                    }
                                    else
                                    {
                                        gc.GuildRank = (int)GuildRank.Member;
                                    }

                                    KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);
                                }
                            }

                            PlayerManager.ShowMessageBox(client, "Thông báo", "Gia tộc [" + WantJoin.FamilyName + "] đã gia nhập bang hội");

                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Gia nhập bang hội [" + client.GuildName + "] thành công!");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -1)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Thành viên chỉ định không có tộc");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -2)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Thành viên chỉ định không phải là tộc trưởng");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -3)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Bang không tồn tại");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -4)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Người chơi không tồn tại");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -5)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Số lượng tộc đã đạt tối đa");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -6)
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", $"Gia tộc sau khi rời bang cần đợi {JOIN_NEW_FAMILY_INTERVAL_HOURS} giờ mới có thể vào lại bang!!!");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else
                        {
                            PlayerManager.ShowMessageBox(WantJoin, "Thông báo", "Có lỗi xảy ra khi tham gia bang hội");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Chiêu mộ gia tộc
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CMD_KT_GUILD_INVITE(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (GameManager.IsKuaFuServer)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không thể mời bang hội ở liên máy chủ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var RoleID = int.Parse(fields[0]);

                var RoleIDInvite = GameManager.ClientMgr.FindClient(RoleID);

                if (null == RoleIDInvite)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phương đã rời mạng");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (RoleIDInvite.FamilyID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phương chưa vào gia tộc");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (RoleIDInvite.FamilyRank != (int)FamilyRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phượng không phỉa là tộc trưởng");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có 1 bang hội nào");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client.GuildRank != (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ mới có thể mời tộc khác gia nhập bang");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Phản hồi
                var responseData = string.Format("{0}:{1}:{2}:{3}", client.RoleID, client.RoleName, client.GuildID, client.GuildName);

                // Gửi cho thằng kia lời mời chiêu mộ bang hội
                RoleIDInvite.SendPacket(nID, responseData);

                PlayerManager.ShowMessageBox(client, "Thông báo", "Đã gửi đơn chiêu mộ tới [" + RoleIDInvite.RoleName + "]\nVui lòng chờ phản hồi");
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_RESPONSEINVITE(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!ServerConfig.Instance.AllowChangeFamilyAndGuild)
                {
                    PlayerManager.ShowNotification(client, "Hiện tại không cho phép điều chỉnh gia tộc/bang hội!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 5)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var Appect = int.Parse(fields[0]);

                //ID CỦA BAGN CHỦ
                var RoleID = int.Parse(fields[1]);

                // ID CỦ GUILD ĐỒNG Ý VÀO
                var GUILDID = int.Parse(fields[2]);

                // GIA TỘC CUẨ BẢN THÂN MÌNH
                var FAMILYID = int.Parse(fields[3]);

                var selfRoleID = int.Parse(fields[4]);

                var clientBangchu = GameManager.ClientMgr.FindClient(RoleID);
                if (null == clientBangchu)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Đối phương đã rời mạng");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (Appect == 0)
                {
                    PlayerManager.ShowMessageBox(clientBangchu, "Thông báo", "Người chơi từ chối gia nhập bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {
                    var result = Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out var tcpOutPacket2, client.ServerId);

                    tcpOutPacket2.GetPacketCmdData(out var strCmdResult2);

                    if (strCmdResult2 != null)
                    {
                        var Pram2 = strCmdResult2.Split(':');

                        var Status = int.Parse(Pram2[0]);

                        if (Status > 0)
                        {
                            var GuildID = int.Parse(Pram2[1]);
                            var GuildName = Pram2[2];

                            var index = 0;

                            KPlayer gc = null;

                            //Set cho toàn bộ người chơi ivaof bang mới
                            while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                            {
                                if (gc.FamilyID == client.FamilyID)
                                {
                                    gc.RoleGuildMoney = 0;
                                    gc.GuildID = GuildID;
                                    gc.GuildName = GuildName;

                                    if (gc.RoleID == client.RoleID)
                                    {
                                        gc.GuildRank = (int)GuildRank.Ambassador;
                                    }
                                    else
                                    {
                                        gc.GuildRank = (int)GuildRank.Member;
                                    }

                                    // Thông báo danh hiệu thay đổi
                                    KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                                    // Thông báo cập nhật thông tin gia tộc và bang hội
                                    KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);
                                }
                            }

                            PlayerManager.ShowMessageBox(clientBangchu, "Thông báo", "Gia tộc [" + clientBangchu.FamilyName + "] đã gia nhập bang hội");

                            PlayerManager.ShowMessageBox(client, "Thông báo", "Gia nhập bang hội [" + client.GuildName + "] thành công!");

                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -1)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Thành viên chỉ định không có tộc");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -2)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Thành viên chỉ định không phải là tộc trưởng");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -3)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Bang không tồn tại");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -4)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Người chơi không tồn tại");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -5)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Số lượng tộc đã đạt tối đa");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else if (Status == -6)
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", $"Gia tộc sau khi rời bang cần đợi {JOIN_NEW_FAMILY_INTERVAL_HOURS} giờ mới có thể vào lại bang!!!");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                        else
                        {
                            PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi xảy ra khi tham gia bang hội");
                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                }

                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILD_DOWTIHDRAW(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Có lỗi ở tham số gửi lên, CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn đang không ở trong bang hội");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Số tiền muốn rút
                var Money = int.Parse(fields[0]);

                var _SubRep = KTGlobal.SubMoney(client, Money, MoneyType.GuildMoney, "WITHDRAW");
                if (_SubRep.IsOK)
                {
                    KTGlobal.AddMoney(client, Money, MoneyType.BacKhoa);
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn đã rút thành công :" + Money + " tài sản cá nhân");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Có lỗi khi rút cổ tức cá nhân");
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Giải tán bang hội
        /// </summary>
        /// <param name="RoleID"></param>
        public static void DesotryGuild(int RoleID, int GuildID)
        {
            try
            {
                var client = GameManager.ClientMgr.FindClient(RoleID);
                if (null == client)
                {
                    return;
                }

                if (client.GuildID <= 0)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn không có bang");
                    return;
                }

                if (client.GuildRank != (int)GuildRank.Master)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Chỉ có bang chủ mới có quyền giải tán bang hội");
                    return;
                }

                var CMD = GuildID + "";

                var result = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_GUILD_QUIT, CMD, client.ServerId);

                var Status = int.Parse(result[0]);

                if (Status > 0)
                {
                    var index = 0;

                    KPlayer gc = null;

                    // Duyệt tất cả những thằng thuộc gia tộc bị kick để upate lại tên cho nó
                    while ((gc = GameManager.ClientMgr.GetNextClient(ref index)) != null)
                    {
                        if (gc.GuildID == client.GuildID)
                        {
                            gc.RoleGuildMoney = 0;
                            gc.GuildID = 0;
                            gc.GuildName = "";

                            gc.GuildRank = (int)GuildRank.Member;

                            // Cập nhật danh hiệu
                            KT_TCPHandler.NotifyOthersMyTitleChanged(gc);

                            // Thông báo cập nhật thông tin gia tộc và bang hội
                            KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(gc);

                            // Thông báo gia tộc bị kick
                            gc.SendPacket((int)TCPGameServerCmds.CMD_KT_GUILD_KICKFAMILY, "1");
                        }
                    }

                    PlayerManager.ShowMessageBox(client, "Thông báo", "Giải tán bang hội thành công");
                    return;
                }
                else
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Giải tán bang hội thất bại");
                    return;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, ex.ToString());
            }
        }

        public static TCPProcessCmdResults CMD_KT_GETTERRORY_DATA(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var GetAllMiniMapInfo = GuidWarManager.getInstance().GetAllMiniMapInfo(client);


                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<List<GuildWarMiniMap>>(GetAllMiniMapInfo, pool, nID);

                return TCPProcessCmdResults.RESULT_DATA;
                // trả vào DB xử lý
                //return Global.TransferRequestToDBServer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults CMD_KT_GUILDWAR_RANKING(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // trả vào DB xử lý
                var _GuildInfo = GuidWarManager.getInstance().GetReport(client);

                // Trả về thông tin bang hội
                tcpOutPacket = DataHelper.ObjectToTCPOutPacket<GuildWarReport>(_GuildInfo, pool, nID);

                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
    }
}
