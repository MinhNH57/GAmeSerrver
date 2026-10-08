using GameServer.KiemThe.GameEvents.SlotMachine;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using GameServer.Core.GameEvent;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.DaoPhong;
using Server.Data;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ResponseGetDanphongInfo(TCPManager tcpMgr, TMSKSocket socket,
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

                int id = 0;
                int.TryParse(fields[0], out id);
                id = id / 100 * 100;
                int[] result = client.GetDanphongNumberArray(id);
                var bytesData = DataHelper.ObjectToBytes(result);

                GameManager.ClientMgr.SendToClient(client, bytesData, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ResponseDanphongMaking(TCPManager tcpMgr, TMSKSocket socket,
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

                int idPack = 0;
                int.TryParse(fields[0], out idPack);
                int amount = client.GetDanphongNumber(idPack);
                if (amount >= DanphongManager.GetMaxCreatable(idPack))
                {
                    return SendToClientError(nID, client, 1); // vượt quá lượt tạo
                }

                int[] items = DanphongManager.GetMaterials(idPack);
                int[] numberRequires = DanphongManager.GetNumbers(idPack);

                // check số lượng item trước khi tạo
                for (int i = 0; i < items.Length; i++)
                {
                    int amountItem = ItemManager.GetItemCountInBag(client, items[i]);

                    if (amountItem < numberRequires[i])
                    {
                        return SendToClientError(nID, client, 2); // thiếu nguyên liệu
                    }
                }

                // tiến hành trừ item
                for (int i = 0; i < items.Length; i++)
                {
                    ItemManager.RemoveItemFromBag(client, items[i], numberRequires[i], From:"Danphong-Making");
                }

                //cộng hiệu ứng
                var effects = DanphongManager.GetOutputs(idPack);
                for (int i = 0; i < effects.Length; i++)
                {
                    // ex: băng đan có output "0,0,5,0,0"
                    client.AddCurResist((DAMAGE_TYPE)i, effects[i]); //  i = 2, tương ứng DAMAGE_TYPE.damage_cold
                    client.AddDanphongNumber((i + 1) * 100, effects[i]); //  id = (2+1)*100 = 300 tương đương id của băng đan phòng
                }
                
                // lưu curResist vào dB
                //client.SaveCurResistToDB();

                client.AddDanphongNumber(idPack, 1);
                LogManager.WriteLog(LogTypes.DanPhong, $"[{client.strUserID}][{client.RoleID}][{client.RoleName}][Danphong] Making idpack:{idPack}, number:{client.GetDanphongNumber(idPack)}");
                return SendToClientError(nID, client, 0); // thành công
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        private static TCPProcessCmdResults SendToClientError(int nID, KPlayer client, int errorCode)
        {
            var result = new
            {
                error = errorCode
            };
            var bytesData = DataHelper.ObjectToBytes(result);
            GameManager.ClientMgr.SendToClient(client, bytesData, nID);
            return TCPProcessCmdResults.RESULT_OK;
        }
    }
}