using GameServer.KiemThe.GameEvents.SlotMachine;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using GameServer.KiemThe.LuaSystem.Logic;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        
        public static TCPProcessCmdResults ResponseGetSlotMachineNumber(TCPManager tcpMgr, TMSKSocket socket,
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

                int numberToGen = int.Parse(fields[0]);

                var numbers = SlotMachineManager.Instance.SpinSlotMachine(client, numberToGen);

                var bytesData = DataHelper.ObjectToBytes<int[]>(numbers);

                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        
        public static TCPProcessCmdResults ResponseGetSlotMachineStatus(TCPManager tcpMgr, TMSKSocket socket,
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
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var status = SlotMachineManager.Instance.GetSlotMachineStatus(client);

                var bytesData = DataHelper.ObjectToBytes<Tuple<int[], string>>(status);

                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        
        
        public static TCPProcessCmdResults ResponseGetSlotMachineGlobal(TCPManager tcpMgr, TMSKSocket socket,
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
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                string cmd = SlotMachineManager.Instance.DailyGlobalRecord;
                GameManager.ClientMgr.SendToClient(client, cmd, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }


        public static TCPProcessCmdResults ResponseGetSlotMachineRanking(TCPManager tcpMgr, TMSKSocket socket,
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

                string isNumberRanking = fields[0];

                string cmd = SlotMachineManager.Instance.GetRankingData(isNumberRanking);
                GameManager.ClientMgr.SendToClient(client, cmd, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ReceiveSlotMachineFreeTicket(TCPManager tcpMgr, TMSKSocket socket,
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

                SlotMachineManager.Instance.ReceiveFreeTicket(client);

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