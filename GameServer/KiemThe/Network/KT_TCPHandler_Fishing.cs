using GameServer.KiemThe.GameEvents;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using System.Collections.Generic;
using GameServer.KiemThe.Core.Item;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ProcessFishing(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = "";
            try
            {
                cmdData = new ASCIIEncoding().GetString(data);
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                int param = int.Parse(fields[0]);
                
                if (param == 0)
                {
                    var status = FishingManager.GetFishStatus(client);
                    var bytesData = DataHelper.ObjectToBytes(status);
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                    
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else if (param == 1 && fields.Length == 3)
                {
                    int multiplier = int.Parse(fields[1]);
                    string zone = fields[2];
                    string caughtFish = FishingManager.GetRandomFishesAndConsumeBait(client, zone, multiplier);
                    // SysConOut.WriteLine($"Caught fish: {caughtFish}");

                    var bytesData = DataHelper.ObjectToBytes(caughtFish);
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }
                else if (param == 2 && fields.Length == 3)
                {
                    int fishId = int.Parse(fields[1]);
                    int multiplier = int.Parse(fields[2]);
                
                    string result = FishingManager.ExchangeFish(client, fishId, multiplier);
                    var bytesData = DataHelper.ObjectToBytes(result);
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                    
                    return TCPProcessCmdResults.RESULT_OK;
                }

                return TCPProcessCmdResults.RESULT_FAILED;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                return TCPProcessCmdResults.RESULT_FAILED;
            }
        }
    }
}