using GameServer.KiemThe.Core.Activity.RechageEvent;
using GameServer.KiemThe.Core.Item;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.XR;
using System.Web.UI.WebControls;
using System.Linq;
using System.Xml.XPath;
using GameServer.KiemThe.Core.Activity.ChargePromotion;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ResponseOpenRechargeAward(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            var cmdData = "";
            try
            {
                /// Giải mã gói tin đẩy về dạng string
                cmdData = new ASCIIEncoding().GetString(data);
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
                var fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var option = int.Parse(fields[0]);
                //bool isClientDataLoaded = bool.Parse(fields[1]);

                // if (!ChargePromotionManager.IsEventActive(client, (RewardType)option))
                // {
                //     LogManager.WriteLog(LogTypes.Error,
                //         string.Format("Sự kiện không trong thời gian hoạt động, CMD={0}, Client={1}",
                //             (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                //     return TCPProcessCmdResults.RESULT_OK;
                // }

                //var rawdata = "";
                //if (!isClientDataLoaded)
                //    rawdata = ChargePromotionManager.RawData;
                var rawdata = ChargePromotionManager.Instance.RawData;
                var optionArray = new int[1] { option };
                var checks = ChargePromotionManager.Instance.GetRewardStatus(client, (RewardType)option);

                var RechargeAwardData = new Tuple<string, int[], int[]>
                (
                    rawdata,
                    optionArray,
                    checks
                );

                var bytesData = DataHelper.ObjectToBytes<Tuple<string, int[], int[]>>(RechargeAwardData);
                GameManager.ClientMgr.SendToClient(client, bytesData,
                    (int)TCPGameServerCmds.CMD_KT_OPEN_RECHARGE_AWARD);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ResponseReceiveRechargeAward(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
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
                LogManager.WriteLog(LogTypes.Error,
                    string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID,
                        Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                var fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var awardValue = int.Parse(fields[0]);
                var awardIndex = int.Parse(fields[1]);
                var option = int.Parse(fields[2]);

                ChargePromotionManager.Instance.GiveAwardToPlayer(client, awardValue, (RewardType)option);

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