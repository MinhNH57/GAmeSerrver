using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Quản lý gói tin sự kiện Bách Bảo Rương
    /// </summary>
    public static partial class KT_TCPHandler
    {
        /// <summary>
        /// Xử lý yêu cầu từ Client thao tác Bách Bảo Rương
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
        public static TCPProcessCmdResults ResponseSeashellCircleRequest(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData;

            try
            {
                cmdData = new ASCIIEncoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                // Người chơi tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player, CMD={0}, Client={1}", (TCPGameServerCmds) nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (GameManager.IsKuaFuServer) {
                    PlayerManager.ShowNotification(client, "Chức năng đóng trên liên máy chủ.");
                    return TCPProcessCmdResults.RESULT_DATA;
                }

                // Nếu chưa mở
                if (!EventTimeManager.IsEventEnabled(KTSeashellCircleManager.SeashellEventKey))
				{
                    PlayerManager.ShowMessageBox(client, "Thông báo",
                        "Hoạt động chưa mở.\nVui lòng quay trở lại sau.");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                // Loại thao tác
                int type = int.Parse(fields[0]);
                // Tham biến đi kèm
                int value = int.Parse(fields[1]);

                // Nếu thao tác quá nhanh
                if (KTGlobal.GetCurrentTimeMilis() - client.LastSeashellCircleTicks < 500)
                {
                    PlayerManager.ShowNotification(client, "Thao tác quá nhanh, hãy bình tĩnh!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // Đánh dấu thời điểm quay
                client.LastSeashellCircleTicks = KTGlobal.GetCurrentTimeMilis();

                // Gói tin phản hồi
                string _cmdData = null;

                // Nếu là truy vấn số sò tích lũy hệ thống
                if (type == 0)
                {
                    KTSeashellCircleManager.OpenSeashellUI(client);
                }
                // Nếu là bắt đầu quay
                else if (type == 1)
                {
                    // Nếu thực hiện quay thành công
                    if (KTSeashellCircleManager.StartTurn(client, value, out int lastStopPos, out int durationTick, out int totalCells, out bool isFirstTurn))
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        long totalStorageSeashells = KTSeashellCircleManager.GetSystemStorageSeashells();
                        var currentScore = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SeashellScore, 0);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}", type, 1, lastStopPos, durationTick, totalCells, client.LastSeashellCircleStage, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0, totalStorageSeashells, isFirstTurn ? 1 : 0, currentScore);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);

                        GlobalEventSource.getInstance().fireEvent(new SeaShellEventObject(client, value));
                    }
                    // Nếu thực hiện quay thất bại
                    else
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", type, -1, client.LastSeashellCircleStage, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);
                    }
                }
                // Nếu là nhận
                else if (type == 2)
                {
                    // Nếu thực hiện nhận quà thành công
                    if (KTSeashellCircleManager.GetAward(client, out int awardType, out int itemID, out int itemNumber))
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        var currentScore = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SeashellScore, 0);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}", type, 1, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0, awardType, itemID, itemNumber, currentScore);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);
                    }
                    // Nếu thực hiện nhận quà thất bại
                    else
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", type, -1, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);
                    }
                }
                // Nếu là đổi sò
                else if (type == 3)
                {
                    // Nếu thực hiện đổi sò thành công
                    if (KTSeashellCircleManager.GetSeashell(client, out int totalSeashells))
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        var currentScore = Global.GetRoleParamsInt32FromDB(client, RoleParamName.SeashellScore, 0);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", type, 1, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0, totalSeashells, currentScore);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);
                    }
                    // Nếu thực hiện đổi sò thất bại
                    else
                    {
                        bool enableGet = KTSeashellCircleManager.CanGet(client);
                        bool enableExchange = KTSeashellCircleManager.CanExchange(client);
                        bool enableBet = KTSeashellCircleManager.CanBet(client);
                        bool enableStartTurn = KTSeashellCircleManager.CanStartTurn(client);
                        _cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", type, -1, enableStartTurn ? 1 : 0, enableGet ? 1 : 0, enableExchange ? 1 : 0, enableBet ? 1 : 0);
                        KTSeashellCircleManager.UpdateSeashellsInBag(client);
                    }
                }
                else if (type == 4) 
                {
                    // nhận xu miễn phí trong hoạt động chuyển xu nhận vé
                    _cmdData = KTSeashellCircleManager.ReceiveFreeTicket(client);
                }
                else if (type == 5) 
                {
                    KTSeashellCircleManager.OpenExchangeCouponShop(client);
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else if (type == 6)
                {
                    _cmdData = string.Format("{0}:{1}:{2}", type, "0", KTSeashellCircleManager.TopSeashellXML);
                }

                // Nếu có Data
                if (!string.IsNullOrEmpty(_cmdData))
                {
                    // Gửi gói tin về Client
                    client.SendPacket(nID, _cmdData);
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
