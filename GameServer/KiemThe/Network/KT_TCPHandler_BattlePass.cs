using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using GameServer.KiemThe.Core.BattlePass;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        /// <summary>
        /// Lấy thông tin Battle Pass
        /// </summary>
        public static TCPProcessCmdResults ResponseGetBattlePassInfo(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = "";
            try
            {
                cmdData = new ASCIIEncoding().GetString(data);
                var client = GameManager.ClientMgr.FindClient(socket);
                if (client == null)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Không tìm thấy thông tin người chơi, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var bp = BattlePassManager.GetBattlePassDataString(client);
                var bytesData = DataHelper.ObjectToBytes(bp);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_STATUS);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                return TCPProcessCmdResults.RESULT_FAILED;
            }
        }

        /// <summary>
        /// Nhận thưởng Battle Pass
        /// </summary>
        public static TCPProcessCmdResults ResponseBattlePassClaimReward(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = "";
            try
            {
                cmdData = new ASCIIEncoding().GetString(data);
                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Error Socket params count not fit CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}, Recv={cmdData.Length}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var client = GameManager.ClientMgr.FindClient(socket);
                if (client == null)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Không tìm thấy thông tin người chơi, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int levelID = int.Parse(fields[0]);
                bool elite = fields[1] == "1";

                bool ok = BattlePassManager.ClaimReward(client, levelID, elite);
                var result = (ok ? "1" : "-1") + ":" + (elite ? "E" : "F");
                var bytesData = DataHelper.ObjectToBytes(result);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_REWARD);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                return TCPProcessCmdResults.RESULT_FAILED;
            }
        }

        /// <summary>
        /// Mua cấp độ Battle Pass
        /// </summary>
        public static TCPProcessCmdResults ResponseBattlePassBuyLevel(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {

                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                bool ok = BattlePassManager.BuyLevel(client);
                var result = ok ? "1" : "-1";
                var bytesData = DataHelper.ObjectToBytes(result);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_BUYLEVEL);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Mua Elite Pass hoặc Elite Bundle
        /// </summary>
        // public static TCPProcessCmdResults ResponseBattlePassBuyElite(TCPManager tcpMgr, TMSKSocket socket,
        //     TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
        //     out TCPOutPacket tcpOutPacket)
        // {
        //     tcpOutPacket = null;
        //     string cmdData = "";
        //     try
        //     {
        //         cmdData = new ASCIIEncoding().GetString(data);
        //         var fields = cmdData.Split(':');
        //         if (fields.Length != 1)
        //         {
        //             LogManager.WriteLog(LogTypes.Error,
        //                 $"Error Socket params count not fit CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}, Recv={cmdData.Length}");
        //             return TCPProcessCmdResults.RESULT_FAILED;
        //         }

        //         var client = GameManager.ClientMgr.FindClient(socket);
        //         if (client == null)
        //         {
        //             LogManager.WriteLog(LogTypes.Error,
        //                 $"Không tìm thấy thông tin người chơi, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
        //             return TCPProcessCmdResults.RESULT_FAILED;
        //         }

        //         int bundleID = int.Parse(fields[0]);
        //         bool ok = BattlePassManager.BuyElitePass(client, bundleID);

        //         var result = ok ? "1" : "-1" + ":" + bundleID;
        //         var bytesData = DataHelper.ObjectToBytes(result);
        //         GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_BUYELITE);

        //         return TCPProcessCmdResults.RESULT_OK;
        //     }
        //     catch (Exception ex)
        //     {
        //         DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
        //         return TCPProcessCmdResults.RESULT_FAILED;
        //     }
        // }

        public static TCPProcessCmdResults ResponseBattlePassClaimAll(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (client == null)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Không tìm thấy thông tin người chơi, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                bool ok = BattlePassManager.ClaimAllAvailableRewards(client);

                var result = ok ? "1" : "-1";
                var bytesData = DataHelper.ObjectToBytes(result);
                GameManager.ClientMgr.SendToClient(client, bytesData, (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_ALLREWARD);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                return TCPProcessCmdResults.RESULT_FAILED;
            }
        }


    }
}
