using GameServer.Logic;
using GameServer.Tools;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using System.Collections.Generic;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.Medal;
using GameServer.KiemThe.GameEvents.KuaFuBalance;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý packet Huy Chương (Medal)
    /// </summary>
    public static partial class KT_TCPHandler
    {
        /// <summary>
        /// Xử lý các yêu cầu nghiệp vụ Huy Chương gửi từ Client
        /// </summary>
        public static TCPProcessCmdResults ResponseMedalOp(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = "";
            try
            {
                // Giải mã gói tin đẩy về dạng string (hỗ trợ UTF-8 tiếng Việt)
                cmdData = new UTF8Encoding().GetString(data, 0, count);
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
                string[] fields = cmdData.Split(':');
                if (fields.Length < 1)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (!int.TryParse(fields[0], out int actionType))
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("ActionType không hợp lệ, CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields[0]));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if ((actionType >= 1 && actionType <= 4) || actionType == 8 || actionType == 9)
                {
                    if (GameManager.IsKuaFuServer || KuaFuBalanceManager.Instance.IsBalanceMap(client) || KuaFuBalanceManager.Instance.IsInBalance(client))
                    {
                        PlayerManager.ShowNotification(client, "Trong Liên Server hoặc Liên Server Cân Bằng không thể thực hiện thao tác này!");
                        string errResponse = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                            0,
                            "Trong Liên Server hoặc Liên Server Cân Bằng không thể thực hiện thao tác này.",
                            actionType,
                            0,
                            0,
                            " ",
                            0,
                            0
                        );
                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, errResponse, nID);
                        return TCPProcessCmdResults.RESULT_DATA;
                    }
                }

                MedalOperationResult result = null;

                switch (actionType)
                {
                    case 1: // Nâng cấp thuộc tính
                        {
                            if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            result = MedalManager.UpgradeAttribute(client, medalDbId);
                            break;
                        }
                    case 2: // Nâng phẩm chất
                        {
                            if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            result = MedalManager.UpgradeQuality(client, medalDbId);
                            break;
                        }
                    case 3: // Tẩy luyện thuộc tính
                        {
                            if (fields.Length < 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            List<int> lockedIds = new List<int>();
                            if (fields.Length == 3 && !string.IsNullOrEmpty(fields[2]))
                            {
                                string[] ids = fields[2].Split('_');
                                foreach (string idStr in ids)
                                {
                                    if (int.TryParse(idStr, out int id))
                                    {
                                        lockedIds.Add(id);
                                    }
                                }
                            }
                            result = MedalManager.RefineMedal(client, medalDbId, lockedIds);
                            break;
                        }
                    case 4: // Mở khóa
                        {
                            if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            result = MedalManager.UnlockMedal(client, medalDbId);
                            break;
                        }
                    case 5: // Lấy thông tin công trạng (Get Merit Snapshot)
                        {
                            result = MedalOperationResult.Ok("Lấy thông tin công trạng thành công.", null, null);
                            break;
                        }
                    case 6: // Đổi thuộc tính Hoàng Kim (chưa hỗ trợ)
                    case 7: // Phân giải Huy Chương (chưa hỗ trợ)
                        {
                            result = MedalOperationResult.Fail("Tính năng này sẽ được cập nhật sau.");
                            break;
                        }
                    case 8: // Xác nhận tẩy luyện
                        {
                            if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            result = MedalManager.AcceptRefine(client, medalDbId);
                            break;
                        }
                    case 9: // Hủy tẩy luyện
                        {
                            if (fields.Length != 2) return TCPProcessCmdResults.RESULT_FAILED;
                            if (!int.TryParse(fields[1], out int medalDbId))
                            {
                                return TCPProcessCmdResults.RESULT_FAILED;
                            }
                            result = MedalManager.CancelRefine(client, medalDbId);
                            break;
                        }
                    default:
                        LogManager.WriteLog(LogTypes.Error,
                            string.Format("ActionType={0} không được hỗ trợ trong CMD={1}, Client={2}",
                                actionType, (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                        return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (result == null)
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Lấy thông tin công trạng mới nhất của người chơi để đồng bộ về client
                MedalMeritSnapshot meritSnapshot = MedalManager.GetMeritSnapshot(client);

                // Đóng gói dữ liệu phản hồi dạng chuỗi
                int successFlag = result.Success ? 1 : 0;
                string message = result.Message ?? "";
                string itemDbIdStr = result.ItemDbId.ToString();
                string itemIdStr = result.ItemId.ToString();
                string payloadStr = string.IsNullOrEmpty(result.Payload) ? " " : result.Payload;

                string responseStr = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                    successFlag,
                    message,
                    actionType,
                    itemDbIdStr,
                    itemIdStr,
                    payloadStr,
                    meritSnapshot.Free,
                    meritSnapshot.Paid
                );

                // Tạo gói tin phản hồi
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, responseStr, nID);
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
