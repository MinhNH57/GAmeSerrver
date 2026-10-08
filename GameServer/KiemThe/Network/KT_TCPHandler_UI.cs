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
using GameServer.KiemThe.GameEvents.CheckIn;
using GameServer.KiemThe.GameEvents.GiftExchange;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.GameEvents.SlotMachine;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.GameEvents.ThienPhuc;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý UI
    /// </summary>
    public static partial class KT_TCPHandler
    {
        #region Thông báo NotificationTip

        /// <summary>
        /// Thông báo NotificationTip gửi về Client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        public static void ShowClientNotificationTip(KPlayer client, string message)
        {
            try
            {
                byte[] cmdData = new UTF8Encoding().GetBytes(message);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int) TCPGameServerCmds.CMD_KT_SHOW_NOTIFICATIONTIP);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        #endregion Thông báo NotificationTip

        #region Mở/đóng khung bất kỳ

        /// <summary>
        /// Gửi yêu cầu mở khung bất kỳ
        /// </summary>
        /// <param name="player"></param>
        /// <param name="uiName"></param>
        /// <param name="parameters"></param>
        public static void SendOpenUI(KPlayer player, string uiName, int[] parameters, List<string> strParams = null)
        {
            G2C_OpenUI openUI = new G2C_OpenUI()
            {
                UIName = uiName,
                Parameters = parameters.ToList(),
                StrParams = strParams
            };
            byte[] cmdData = DataHelper.ObjectToBytes<G2C_OpenUI>(openUI);
            GameManager.ClientMgr.SendToClient(player, cmdData, (int) TCPGameServerCmds.CMD_KT_G2C_OPEN_UI);
        }

        /// <summary>
        /// Gửi yêu cầu đóng khung bất kỳ
        /// </summary>
        /// <param name="player"></param>
        /// <param name="uiName"></param>
        public static void SendCloseUI(KPlayer player, string uiName)
        {
            byte[] cmdData = new ASCIIEncoding().GetBytes(uiName);
            GameManager.ClientMgr.SendToClient(player, cmdData, (int) TCPGameServerCmds.CMD_KT_G2C_CLOSE_UI);
        }

        #endregion Mở/đóng khung bất kỳ

        #region Message Box
        /// <summary>
        /// Kết quả tương tác của người chơi ở bảng thông báo MessageBox
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
        public static TCPProcessCmdResults ResponseUIMessageBoxResult(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            string cmdData = "";
            try
            {
                /// Giải mã gói tin đẩy về dạng string
                cmdData = new UTF8Encoding().GetString(data);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 4)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// ID MessageBox
                int msgBoxID = int.Parse(fields[0]);
                /// Loại gói tin
                int type = int.Parse(fields[1]);
                /// Button được Click
                string buttonType = fields[2];
                /// Giá trị trả về
                string retValue = fields[3];

                /// Tìm chủ nhân của gói tin tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Nếu là mở MessageBox thông thường
                if (type == 0)
                {
                    KMessageBox box = KTMessageBoxManager.FindMessageBox(msgBoxID);
                    /// Nếu không tìm thấy thì toác
                    if (box == null)
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    /// Nếu không phải Type 0 thì toác
                    if (!(box is KTMessageBox))
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    KTMessageBox msgBox = box as KTMessageBox;
                    /// Thực hiện sự kiện tương ứng
                    if (buttonType == "OK")
                    {
                        msgBox.OK?.Invoke();
                    }
                    else if (buttonType == "Cancel")
                    {
                        msgBox.Cancel?.Invoke();
                    }
                    else
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }
                }
                /// Nếu là mở InputNumber
                else if (type == 1)
                {
                    KMessageBox box = KTMessageBoxManager.FindMessageBox(msgBoxID);
                    /// Nếu không tìm thấy thì toác
                    if (box == null)
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    /// Nếu không phải Type 1 thì toác
                    if (!(box is KTInputNumberBox))
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    KTInputNumberBox msgBox = box as KTInputNumberBox;
                    /// Thực hiện sự kiện tương ứng
                    if (buttonType == "OK")
                    {
                        msgBox.OK?.Invoke(int.Parse(retValue));
                    }
                    else if (buttonType == "Cancel")
                    {
                        msgBox.Cancel?.Invoke();
                    }
                    else
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }
                }
                /// Nếu là mở InputString
                else if (type == 2)
				{
                    KMessageBox box = KTMessageBoxManager.FindMessageBox(msgBoxID);
                    /// Nếu không tìm thấy thì toác
                    if (box == null)
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    /// Nếu không phải Type 2 thì toác
                    if (!(box is KTInputStringBox))
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }

                    KTInputStringBox msgBox = box as KTInputStringBox;
                    /// Thực hiện sự kiện tương ứng
                    if (buttonType == "OK")
                    {
                        msgBox.OK?.Invoke(retValue);
                    }
                    else if (buttonType == "Cancel")
                    {
                        msgBox.Cancel?.Invoke();
                    }
                    else
                    {
                        return TCPProcessCmdResults.RESULT_FAILED;
                    }
                }
                /// Không phải loại nào thì cho toác
                else
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
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
        /// Gửi yêu cầu mở bảng thông báo MessageBox
        /// </summary>
        /// <param name="player"></param>
        /// <param name="type"></param>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="parameters"></param>
        public static void SendOpenMessageBox(KPlayer player, int id, int type, string title, string text, List<string> parameters)
        {
            G2C_ShowMessageBox showMessageBox = new G2C_ShowMessageBox()
            {
                ID = id,
                MessageType = type,
                Text = text,
                Title = title,
                Params = parameters,
            };
            byte[] cmdData = DataHelper.ObjectToBytes<G2C_ShowMessageBox>(showMessageBox);
            GameManager.ClientMgr.SendToClient(player, cmdData, (int) TCPGameServerCmds.CMD_KT_SHOW_MESSAGEBOX);
        }

        public static TCPProcessCmdResults ResponseUIInteractResult(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            string cmdData;
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
                // Tìm chủ nhân của gói tin tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                var uiName = fields[0];
                var action = fields[1];

                switch (uiName)
                {
                    case "Offering":
                        if (action == "TopRanking")
                            OfferingManager.ShowOfferingRanking(client);

                        break;
                    case "EventCompose":
                        if (action == "TopRanking")
                        {
                            var topRankingStr = ComposeEventManager.Instance.GetTopRankingStr();
                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, topRankingStr, nID);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        if (action == "Consume") 
                        {
                            var returnData = ComposeEventManager.Instance.GetComposeStatusOfPlayer(client);
                            var returnByteData = DataHelper.ObjectToBytes(returnData);

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, returnByteData, nID);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        if (action == "Accumulate")
                        {
                            ComposeEventManager.Instance.PlayerReceiveAccumulateRewards(client);
                            var returnData = ComposeEventManager.Instance.GetComposeStatusOfPlayer(client);
                            var returnByteData = DataHelper.ObjectToBytes(returnData);

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, returnByteData, nID);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        break;
                    case "Seashells":
                        if (action == "TopRanking")
                            TopEventRanking.ShowTopSeashells(client);
                        break;
                    case "CheckIn":
                        if (action == "Show")
                        {
                            tcpOutPacket = CheckInManager.Instance.ResponseCheckInStatus(client, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        else if (action.StartsWith("Buy"))
                        {
                            var packageID = int.Parse(action.Split('_')[1]);
                            tcpOutPacket = CheckInManager.Instance.PlayerBuy(client, packageID, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        } else if (action.StartsWith("Accumulate"))
                        {
                            // var packageID = int.Parse(action.Split('_')[1]);
                            tcpOutPacket = CheckInManager.Instance.PlayerReceiveAccumulate(client, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        break;
                    case "ComeBack":
                        if (action == "Show")
                        {
                            tcpOutPacket = ComeBackManager.ResponseComebackStatus(client, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        if (action.StartsWith("Buy"))
                        {
                            var packageID = int.Parse(action.Split('_')[1]);
                            tcpOutPacket = ComeBackManager.PlayerBuy(client, packageID, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        break;
                    case "ServerMergeRewards":
                        if (action == "Show")
                        {
                            tcpOutPacket = ServerMergeRewardManager.ResponseRewardStatus(client, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        if (action.StartsWith("Buy"))
                        {
                            var packageID = int.Parse(action.Split('_')[1]);
                            tcpOutPacket = ServerMergeRewardManager.ClaimReward(client, packageID, pool);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        if (action == "Level")
                        {
                            ServerMergeRewardManager.SetLevel(client);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }
                        break;
                    case "ExchangeTicketEvent":
                        if (action == "Exchange")
                        {
                            ExchangeTicketManager.Instance.HandleExchangeTicket(client);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        else if (action == "CheckStatus")
                        {
                            ExchangeTicketManager.Instance.CheckExchangeStatus(client);
                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        break;
                    case "ThienPhuc":
                    {
                            if (action == "Show")
                            {
                                ThienPhucInfo info =
                                    ThienPhucManager.Instance.GetPlayerInfo(client);

                                byte[] responseData =
                                    DataHelper.ObjectToBytes(info);
                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(
                                    pool,
                                    responseData,
                                    nID
                                );

                                return TCPProcessCmdResults.RESULT_DATA;
                            }

                            if (action.StartsWith("Spin_"))
                        {
                            string numberText =
                                action.Substring("Spin_".Length);

                            if (!int.TryParse(numberText, out int guessedNumber))
                            {
                                PlayerManager.ShowNotification(
                                    client,
                                    "Số dự đoán không hợp lệ!"
                                );

                                return TCPProcessCmdResults.RESULT_OK;
                            }

                            ThienPhucSpinResult result =
                                ThienPhucManager.Instance.Spin(
                                    client,
                                    guessedNumber
                                );

                            byte[] responseData =
                                DataHelper.ObjectToBytes(result);

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(
                                pool,
                                responseData,
                                nID
                            );

                            return TCPProcessCmdResults.RESULT_DATA;
                        }

                        break;
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
        #endregion
        
        public static TCPProcessCmdResults ResponseGiftExchange(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            List<int> cmdData;
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = DataHelper.BytesToObject<List<int>>(data, 0, data.Length);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // Tìm chủ nhân của gói tin tương ứng
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var action = cmdData[0];
                if (action == 0)
                {
                    tcpOutPacket = GiftExchangeManager.ResponseGiftExchange_Status(client, (TCPGameServerCmds)nID, pool);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 1)
                {
                    tcpOutPacket = GiftExchangeManager.ResponseGiftExchange_SendGift(client, cmdData[1], cmdData[2], (TCPGameServerCmds)nID, pool);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 2)
                {
                    tcpOutPacket = GiftExchangeManager.ResponseGiftExchange_ReceiveReward(client, (TCPGameServerCmds)nID, pool);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
                
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        
        public static TCPProcessCmdResults ResponseGiftExchangePlayer(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            List<int> cmdData;
            try
            {
                // Giải mã gói tin đẩy về dạng string
                cmdData = DataHelper.BytesToObject<List<int>>(data, 0, data.Length);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                // Tìm chủ nhân của gói tin tương ứng
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var action = cmdData[0];
                if (action == 0)
                {
                    tcpOutPacket = GiftExchangePlayerManager.ResponseGiftExchangePlayer_Status(client, (TCPGameServerCmds)nID, pool);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 1)
                {
                    tcpOutPacket = GiftExchangePlayerManager.ResponseGiftExchange_ReceiveReward(client, (TCPGameServerCmds)nID, pool);
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                if (action == 2)
                {
                    var type = cmdData[1];
                    GiftExchangePlayerManager.ResponseGiftExchange_TopRanking(client, type == 0);
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
    }
}
