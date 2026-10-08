using GameServer.Core.Executor;
using GameServer.KiemThe;
using GameServer.KiemThe.Core.Activity.CardMonth;
using GameServer.KiemThe.Core.Activity.DaySeriesLoginEvent;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Activity.EveryDayOnlineEvent;
using GameServer.KiemThe.Core.Activity.LevelUpEvent;
using GameServer.KiemThe.Core.Activity.RechageEvent;
using GameServer.KiemThe.Core.Activity.TotalLoginEvent;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.GameEvents;
using GameServer.KiemThe.GameEvents.QuestEvent;
using GameServer.KiemThe.GameEvents.RechargeEvent;
using GameServer.KiemThe.GameEvents.TangBaoDo;
using GameServer.KiemThe.GameEvents.VongQuayMayMan;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.Logic;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Server
{
    /// <summary>
    /// Kết quả gói tin
    /// </summary>
    public enum TCPProcessCmdResults { RESULT_OK = 0, RESULT_FAILED = 1, RESULT_DATA = 2, RESULT_UNREGISTERED = 3, };

    /// <summary>
    /// Xử lý Packet tương tác qua lại với Client
    /// </summary>
    public class TCPCmdHandler
    {
        /// <summary>
        /// [DEBUG] Danh sách CMD noise tần suất cao KHÔNG log (heartbeat, di chuyển, đồng bộ thời gian...).
        /// Thêm/bớt CMD tại đây để điều chỉnh độ nhiễu của file PacketDebug_*.log.
        /// </summary>
        private static readonly HashSet<int> DebugPacketIgnoreCmds = new HashSet<int>
        {
            (int)TCPGameServerCmds.CMD_SPR_CLIENTHEART,
            (int)TCPGameServerCmds.CMD_SPR_CLIENTHEART_OLD,
            (int)TCPGameServerCmds.CMD_SPR_CHECK,
            (int)TCPGameServerCmds.CMD_SPR_MOVE,
            (int)TCPGameServerCmds.CMD_SPR_MOVE2,
            (int)TCPGameServerCmds.CMD_SPR_MOVEEND,
            (int)TCPGameServerCmds.CMD_SPR_STOPMOVE,
            (int)TCPGameServerCmds.CMD_SPR_AUTOMOVETOPOS,
            (int)TCPGameServerCmds.CMD_SPR_POSITION,
            (int)TCPGameServerCmds.CMD_SPR_PETPOS,
            (int)TCPGameServerCmds.CMD_SYNC_TIME,
            (int)TCPGameServerCmds.CMD_SYNC_TIME_BY_CLIENT,
            (int)TCPGameServerCmds.CMD_SYNC_TIME_BY_SERVER,
        };

        /// <summary>
        /// Tổng số Packet đang được xử lý
        /// </summary>
        public static long TotalHandledCmdsNum = 0;

        /// <summary>
        /// ID Packet tốn nhiều thời gian xử lý nhất
        /// </summary>
        public static int MaxUsedTicksCmdID = 0;

        /// <summary>
        /// Thời gian xử lý packet dài nhất
        /// </summary>
        public static long MaxUsedTicksByCmdID = 0;

        /// <summary>
        /// Danh sách Socket hiện có
        /// </summary>
        private static Dictionary<TMSKSocket, int> HandlingCmdDict = new Dictionary<TMSKSocket, int>();

        /// <summary>
        /// Trả về tổng số Socket hiện có
        /// </summary>
        /// <returns></returns>
        public static int GetHandlingCmdCount()
        {
            lock (HandlingCmdDict)
            {
                return HandlingCmdDict.Count;
            }
        }

        /// <summary>
        /// Thực hiện kết nối với 1 session mới của GAMESERVER
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        public static void ProcessCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count)
        {
            TCPOutPacket tcpOutPacket = null;
            TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;

            result = TCPCmdHandler.ProcessCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);

            /// Nếu có dữ liệu
            if (result == TCPProcessCmdResults.RESULT_DATA && null != tcpOutPacket)
            {
                tcpMgr.MySocketListener.SendData(socket, tcpOutPacket);
            }
            /// Nếu toác
            else if (result == TCPProcessCmdResults.RESULT_FAILED)
            {
                if (nID != (int)TCPGameServerCmds.CMD_LOG_OUT)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Resolve packet faild: {0},{1}, close socket", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                }

                tcpMgr.MySocketListener.CloseSocket(socket);
            }
        }

        /// <summary>
        /// Nhận gói tin từ CLient gửi về và giải mã nó (tương đương PlayZone_Network ở client)
        /// </summary>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            long startTicks = TimeUtil.NOW();

            lock (HandlingCmdDict)
            {
                HandlingCmdDict[socket] = 1;
            }

            TCPProcessCmdResults result = TCPProcessCmdResults.RESULT_FAILED;
            tcpOutPacket = null;

            socket.session.CmdID = nID;
            socket.session.CmdTime = startTicks;

            // [DEBUG] Log đầy đủ nội dung packet của các người chơi cấu hình trong PacketMonitoringUsers.xml.
            // File rỗng/không tồn tại => PacketMonitoringManager.IsEmpty = true => bỏ qua sớm, không lookup player.
            // Bỏ qua các CMD noise tần suất cao (heartbeat, di chuyển, đồng bộ thời gian...)
            if (!PacketMonitoringManager.IsEmpty && !DebugPacketIgnoreCmds.Contains(nID))
            {
                try
                {
                    KPlayer dbgPlayer = GameManager.ClientMgr.FindClient(socket);
                    if (dbgPlayer != null && PacketMonitoringManager.IsMonitored(dbgPlayer.RoleID))
                    {
                        // Cắt đúng số byte thực nhận (data buffer có thể lớn hơn count)
                        int dbgLen = (data != null && count > 0) ? System.Math.Min(count, data.Length) : 0;
                        string dbgHex = "";
                        string dbgText = "";
                        if (dbgLen > 0)
                        {
                            byte[] dbgBytes = new byte[dbgLen];
                            System.Array.Copy(data, 0, dbgBytes, 0, dbgLen);
                            dbgHex = DataHelper.Bytes2HexString(dbgBytes);
                            // Thử decode text (nhiều CMD dùng payload chuỗi UTF8, vd "skillID_status", "id:goodsid:...")
                            try
                            {
                                dbgText = new System.Text.UTF8Encoding(false, false).GetString(dbgBytes).Replace("\r", " ").Replace("\n", " ");
                            }
                            catch { dbgText = ""; }
                        }
                        LogManager.WriteLog(LogTypes.PacketDebug, $"{dbgPlayer.RoleName}[{dbgPlayer.RoleID}] CMD [{nID}]{(TCPGameServerCmds)nID} | size={count} | text=[{dbgText}] | hex={dbgHex}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteException(ex.ToString());
                }
            }

            #region Danh sách Packet

            result = TCPCmdDispatcher.getInstance().dispathProcessor(socket, nID, data, count);

            long tick = KTGlobal.GetCurrentTimeMilis();

            //#if DEBUG
            //            if (nID != 112 && nID != 613 && nID != 211 && nID != 45009 && nID != 50008)
            //            {
            //                Console.WriteLine("Received: {0} (ID: {1})", (TCPGameServerCmds)nID, nID);
            //            }
            //#endif

            if (result == TCPProcessCmdResults.RESULT_UNREGISTERED)
            {
                switch (nID)
                {
                    case (int)TCPGameServerCmds.CMD_LOGIN_ON2:
                        {
                            result = KT_TCPHandler.ProcessUserLogin2Cmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_LOGIN_ON:
                        {
                            result = KT_TCPHandler.ProcessUserLoginCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_LOG_OUT:
                        {
                            result = TCPProcessCmdResults.RESULT_FAILED;
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_ROLE_LIST:
                        {
                            result = KT_TCPHandler.ProcessGetRoleListCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_CREATE_ROLE:
                        {
                            result = KT_TCPHandler.ProcessCreateRoleCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_INIT_GAME:
                        {
                            result = KT_TCPHandler.ProcessInitGameCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SYNC_TIME:
                        {
                            result = KT_TCPHandler.ProcessTimeSyncGameCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_PUSH_VERSION:
                        {
                            result = KT_TCPHandler.ProcessClientPushVersionCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_PLAY_GAME:
                        {
                            result = KT_TCPHandler.ProcessStartPlayGameCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_SPECIALMACHINE:
                        {
                            result = KT_TCPHandler.ProcessSpecialDeviceName(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_MOVE:
                        {
                            result = KT_TCPHandler.ProcessSpriteMoveCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_STOPMOVE:
                        {
                            result = KT_TCPHandler.ProcessSpriteStopMoveCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_POSITION:
                        {
                            result = KT_TCPHandler.ProcessSpritePosCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_MAPCHANGE:
                        {
                            result = KT_TCPHandler.ProcessSpriteMapChangeCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_ENTERMAP:
                        {
                            result = KT_TCPHandler.ProcessSpriteEnterMap(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_UPDATE_LOCALMAP_MONSTER:
                        {
                            result = KT_TCPHandler.ProcessGetLocalMapSpecialMonsters(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_NPC_BUY:
                        {
                            result = KT_TCPHandler.ProcessSpriteNPCBuyCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_NPC_SALEOUT:
                        {
                            result = KT_TCPHandler.ProcessSpriteNPCSaleOutCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_MOD_GOODS:
                        {
                            result = KT_TCPHandler.ProcessSpriteModGoodsCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_MERGE_GOODS:
                        {
                            result = KT_TCPHandler.ProcessSpriteMergeGoodsCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GETFRIENDS:
                        {
                            result = KT_TCPHandler.ProcessSpriteGetFriendsCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_ADDFRIEND:
                        {
                            result = KT_TCPHandler.ProcessSpriteAddFriendCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_REMOVEFRIEND:
                        {
                            result = KT_TCPHandler.ProcessSpriteRemoveFriendCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_ASKFRIEND:
                        {
                            result = KT_TCPHandler.ProcessSpriteAskFriend(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_REJECTFRIEND:
                        {
                            result = KT_TCPHandler.ProcessSpriteRejectFriend(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GETTHING:
                        {
                            result = KT_TCPHandler.ProcessSpriteGetThingCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_CHGPKMODE:
                        {
                            result = KT_TCPHandler.ProcessSpriteChangePKModeCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_CHAT:
                        {
                            result = KT_TCPHandler.ProcessSpriteChatCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_USEGOODS:
                        {
                            result = KT_TCPHandler.ProcessSpriteUseGoodsCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_USEGOODS2:
                        {
                            result = KT_TCPHandler.ProcessSpriteUseGoodsCmd2(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_OTHER_ROLE_DATA:
                        {
                            result = KT_TCPHandler.ResponseGetOtherRoleEquipInfo(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GOODSEXCHANGE:
                        {
                            result = KT_TCPHandler.ProcessSpriteGoodsExchangeCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GOODSSTALL:
                        {
                            result = KT_TCPHandler.ProcessSpriteGoodsStallCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_GETGOODSLISTBYSITE:
                        {
                            result = KT_TCPHandler.ProcessSpriteGetGoodsListBySiteCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_LOADALREADY:
                        {
                            result = KT_TCPHandler.ProcessSpriteLoadAlreadyCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_RESETBAG:
                        {
                            result = KT_TCPHandler.ProcessSpriteResetBagCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_RESETPORTABLEBAG:
                        {
                            result = KT_TCPHandler.ProcessSpriteResetPortableBagCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GETSONGLIGIFT:
                        {
                            result = KT_TCPHandler.ProcessSpriteGetSongLiGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }


                    case (int)TCPGameServerCmds.CMD_SPR_CLIENTHEART:
                        {
                            result = KT_TCPHandler.ProcessSpriteClientHeartCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GETPAIHANGLIST:
                        {
                            result = KT_TCPHandler.ProcessGetPaiHangListCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_GETUSERMAILLIST:
                        {
                            result = KT_TCPHandler.ProcessGetUserMailListCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETUSERMAILDATA:
                        {
                            result = KT_TCPHandler.ProcessGetUserMailDataCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_FETCHMAILGOODS:
                        {
                            result = KT_TCPHandler.ProcessFetchMailGoodsCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_DELETEUSERMAIL:
                        {
                            result = KT_TCPHandler.ProcessDeleteUserMailCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_EXECUTERECOMMENDPROPADDPOINT:
                        {
                            result = KT_TCPHandler.ProcessExecuteRecommendPropAddPointCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_UPDATEEVERYDAYONLINEAWARDGIFTINFO:
                        {
                            result = EveryDayOnlineManager.ProcessSpriteUpdateEverydayOnlineAwardGiftInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_UPDATEACTIVITYDAYINFO:
                        {
                            result = DayActivityManager.ProcessMissionDayActivityInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETACTIVITYDAYAWARDGIFT:
                        {
                            result = DayActivityManager.ProcessSpriteGetDayActivityAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETACTIVITYMONTHAWARDGIFT:
                        {
                            result = DayActivityManager.ProcessSpriteGetMonthActivityAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_BUYVIPACTIVITYSCORE:
                        {
                            result = DayActivityManager.ProcessSpriteBuyVIPActivity(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_COLLECTVIPACTIVITYSCORE:
                        {
                            result = DayActivityManager.ProcessSpriteCollectScoreVIPActivity(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETEVERYDAYONLINEAWARDGIFT:
                        {
                            result = EveryDayOnlineManager.ProcessSpriteGetEveryDayOnLineAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_UPDATEEVERYDAYSERIESLOGININFO:
                        {
                            result = SevenDayLoginManager.ProcessSpriteUpdateEverydaySeriesLoginInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETEVERYDAYSERIESLOGINAWARDGIFT:
                        {
                            result = SevenDayLoginManager.ProcessSpriteGetSeriesLoginAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_QUERYTOTALLOGININFO:
                        {
                            result = TotalLoginManager.ProcessSpriteQueryTotalLoginInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETTOTALLOGINAWARD:
                        {
                            result = TotalLoginManager.ProcessSpriteGetTotalLoginAwardCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_BONUS_DOWNLOAD:
                        {
                            result = KT_TCPHandler.ProcessDownloadBonus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_FINDCHEST_TANGBAODO:
                        {
                            result = TangBaoDo.ProcessSpriteStartFindTangBaoDoAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETBAITANLOG:
                        {
                            result = TCPCmdDispatcher.getInstance().transmission(socket, nID, data, count);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_CHECK:
                        {
                            result = KT_TCPHandler.ProcessCheck(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_QUERYUPLEVELGIFTINFO:
                        {
                            result = LevelUpEventManager.ProcessQueryUpLevelGiftFlagList(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_QUERYSIDETASKINFO:
                        {
                            result = SideTaskManager.ProcessSideTaskInfoCmd(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETSIDETASKAWARD:
                        {
                            result = SideTaskManager.ProcessSpriteGetSideTaskAwardGiftCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GETUPLEVELGIFTAWARD:
                        {
                            result = LevelUpEventManager.ProcessGetUpLevelGiftAward(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_QUERY_REPAYACTIVEINFO:
                        {
                            result = RechageManager.QueryRechargeRepayActive(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GET_REPAYACTIVEAWARD:
                        {
                            result = RechageManager.ProcessGetRepayAwardCmd(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_QUERY_ALLREPAYACTIVEINFO:
                        {
                            result = RechageManager.QueryAllRechargeRepayActiveInfo(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_SEASHELL_CIRCLE:
                        {
                            result = KT_TCPHandler.ResponseSeashellCircleRequest(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GET_STORE_MONEY:
                        {
                            result = KT_TCPHandler.ProcessModifyStoreMoney(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_WING_ZHUHUN:
                        {
                            result = CardMonthManager.ActiveCardMoth(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);

                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GET_YUEKA_DATA:
                        {
                            result = CardMonthManager.ProcessGetYueKaData(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_GET_YUEKA_AWARD:
                        {
                            result = CardMonthManager.ProcessGetYueKaAward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_NEWBIE_VILLAGES:
                        {
                            result = KT_TCPHandler.PrecessGetNewbieVillages(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_ROLE_ATRIBUTES:
                        {
                            result = KT_TCPHandler.GetRoleAttributes(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CLICKON_NPC:
                        {
                            result = KT_TCPHandler.ProcessClickOnNPC(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_NPCDIALOG:
                        {
                            result = KT_TCPHandler.ResponseNPCDialog(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_ITEMDIALOG:
                        {
                            result = KT_TCPHandler.ResponseItemDialog(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_CHANGEACTION:
                        {
                            result = KT_TCPHandler.ResponseSpriteChangeAction(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SKILL_ADDPOINT:
                        {
                            result = KT_TCPHandler.ResponseDistributeSkillPoints(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SET_SKILL_TO_QUICKKEY:
                        {
                            result = KT_TCPHandler.ResponseSetQuickKey(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SET_AND_ACTIVATE_AURA:
                        {
                            result = KT_TCPHandler.ResponseSetAndActivateAura(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_USESKILL:
                        {
                            result = KT_TCPHandler.ResponseUseSkill(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GM_COMMAND:
                        {
                            result = KT_TCPHandler.ResponseGMCommand(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_CLIENTREVIVE:
                        {
                            result = KT_TCPHandler.ResponseClientRevive(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SAVESYSTEMSETTINGS:
                        {
                            result = KT_TCPHandler.ResponseSaveSystemSettings(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SAVEAUTOSETTINGS:
                        {
                            result = KT_TCPHandler.ResponseSaveAutoSettings(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_INVITETOTEAM:
                        {
                            result = KT_TCPHandler.ResponseInviteTeammate(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CREATETEAM:
                        {
                            result = KT_TCPHandler.ResponseCreateTeam(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_AGREEJOINTEAM:
                        {
                            result = KT_TCPHandler.ResponseAgreeJoinTeam(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_REFUSEJOINTEAM:
                        {
                            result = KT_TCPHandler.ResponseRefuseJoinTeam(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GETTEAMINFO:
                        {
                            result = KT_TCPHandler.ResponseGetTeamInfo(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_KICKOUTTEAMMATE:
                        {
                            result = KT_TCPHandler.ResponseKickOutTeammate(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_LEAVETEAM:
                        {
                            result = KT_TCPHandler.ResponseLeaveTeam(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_APPROVETEAMLEADER:
                        {
                            result = KT_TCPHandler.ResponseApproveTeamLeader(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_ASKTOJOINTEAM:
                        {
                            result = KT_TCPHandler.ResponseAskToJoinTeam(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_ASK_CHALLENGE:
                        {
                            result = KT_TCPHandler.ResponseAskChallenge(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_RESPONSE_CHALLENGE:
                        {
                            result = KT_TCPHandler.ResponseResponseChallenge(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_AUTOPATH_CHANGEMAP:
                        {
                            result = KT_TCPHandler.ResponseClientAutoPathTransferMap(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_GROWPOINT_CLICK:
                        {
                            result = KT_TCPHandler.ResponseGrowPointClick(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_OPEN_TOKENSHOP:
                        {
                            result = KT_TCPHandler.ResponseOpenTokenShop(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_UI_INTERACT:
                        {
                            result = KT_TCPHandler.ResponseUIInteractResult(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GIFT_EXCHANGE:
                    {
                        result = KT_TCPHandler.ResponseGiftExchange(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }
                    
                    case (int)TCPGameServerCmds.CMD_KT_GIFT_EXCHANGE_PLAYER:
                    {
                        result = KT_TCPHandler.ResponseGiftExchangePlayer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }

                    case (int)TCPGameServerCmds.CMD_KT_TOGGLE_HORSE_STATE:
                        {
                            result = KT_TCPHandler.ResponseToggleHorseState(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_EQUIP_ENHANCE:
                        {
                            result = KT_TCPHandler.ResponseEquipEnhance(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_EQUIP_ENHANCE_ARTIFACT:
                        {
                            result = KT_TCPHandler.ResponseEquipEnhanceArtifact(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    
                    case (int)TCPGameServerCmds.CMD_KT_EQUIP_ENHANCE_ADVANCE:
                        {
                            result = KT_TCPHandler.ResponseEquipAdvanceEnhance(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_EQUIP_UP_ARTIFACT_COMPLETE:
                        {
                            result = KT_TCPHandler.ResponseUpArtifactItem(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_EQUIP_UP_ARTIFACT_GET_INFO:
                        {
                            result = KT_TCPHandler.ResponseGetInfoUpArtifactItem(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SIGNET_ENHANCE:
                        {
                            result = KT_TCPHandler.ResponseSignetEnhance(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CLIENT_DO_REFINE:
                        {
                            result = KT_TCPHandler.CMD_KT_CLIENT_DO_REFINE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_COMPOSE_CRYSTALSTONES:
                        {
                            result = KT_TCPHandler.ResponseComposeCrystalStones(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SPLIT_CRYSTALSTONES:
                        {
                            result = KT_TCPHandler.ResponseEquipSplitCrystalStones(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_ASK_ACTIVEFIGHT:
                        {
                            result = KT_TCPHandler.ResponseActiveFight(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CHANGE_AVARTA:
                        {
                            result = KT_TCPHandler.ResponseRoleAvartaChange(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_BEGIN_CRAFT:
                        {
                            result = KT_TCPHandler.ResponseCraftItem(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SHOW_MESSAGEBOX:
                        {
                            result = KT_TCPHandler.ResponseUIMessageBoxResult(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SONGJINBATTLE_RANKING:
                        {
                            result = KT_TCPHandler.SongJinBattleGetRanking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_FACTION_PVP_RANKING_INFO:
                        {
                            result = KT_TCPHandler.FactionBattleRanking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LEAGUE_PVP_RANKING_INFO:
                        {
                            result = KT_TCPHandler.LeagueBattleRanking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LEAGUE_OPEN_BET:
                        {
                            result = KT_TCPHandler.LeagueBattleOpenBet(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_BROWSE_PLAYER:
                        {
                            result = KT_TCPHandler.ResponseBrowsePlayers(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CHECK_PLAYER_LOCATION:
                        {
                            result = KT_TCPHandler.ResponseCheckPlayerLocation(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GET_PLAYER_INFO:
                        {
                            result = KT_TCPHandler.ResponseCheckPlayerInfo(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_QUERY_PLAYERRANKING:
                        {
                            result = KT_TCPHandler.ResponseQueryPlayerRanking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_TESTPACKET:
                        {
                            result = KT_TCPHandler. ResponseCMDTestFromClient(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_CHANGE_SUBSET:
                        {
                            result = KT_TCPHandler.ResponseChangeSubEquipSet(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SHOW_INPUTITEMS:
                        {
                            result = KT_TCPHandler.ResponseInputItems(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_COMPOSE_ITEMS:
                        {
                            result = KT_TCPHandler.ResponseComposeItems(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_SHOW_INPUTEQUIPANDMATERIALS:
                        {
                            result = KT_TCPHandler.ResponseInputEquipAndMaterials(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_CREATE:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_CREATE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_REQUESTJOIN:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_REQUESTJOIN(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_KICKMEMBER:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_KICKMEMBER(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_GETLISTFAMILY:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_GETLISTFAMILY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_DESTROY:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_DESTROY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_CHANGENOTIFY:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_CHANGENOTIFY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_CHANGE_REQUESTJOIN_NOTIFY:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_CHANGE_REQUESTJOIN_NOTIFY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_CHANGE_RANK:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_CHANGE_RANK(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_RESPONSE_REQUEST:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_RESPONSE_REQUEST(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_QUIT:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_QUIT(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_FAMILY_OPEN:
                        {
                            result = KT_TCPHandler.CMD_KT_FAMILY_OPEN(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_CREATE:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_CREATE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_GETINFO:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_GETINFO(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_GETMEMBERLIST:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_GETMEMBERLIST(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_CHANGERANK:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_CHANGERANK(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_KICKFAMILY:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_KICKFAMILY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_GETGIFTED:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_GETGIFTED(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_OFFICE_RANK:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_OFFICE_RANK(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_VOTEGIFTED:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_VOTEGIFTED(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_DONATE:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_DONATE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_TERRITORY:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_TERRITORY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_SETCITY:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_SETCITY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_SETTAX:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_SETTAX(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_QUIT:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_QUIT(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_CHANGE_MAXWITHDRAW:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_CHANGE_MAXWITHDRAW(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_CHANGE_NOTIFY:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_CHANGE_NOTIFY(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_GETSHARE:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_GETSHARE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_ASKJOIN:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_ASKJOIN(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_RESPONSEASK:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_RESPONSEASK(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_INVITE:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_INVITE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_RESPONSEINVITE:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_RESPONSEINVITE(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_DOWTIHDRAW:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_DOWTIHDRAW(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILD_FAMILYQUIT:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILD_FAMILYQUIT(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GETTERRORY_DATA:
                        {
                            result = KT_TCPHandler.CMD_KT_GETTERRORY_DATA(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_GUILDWAR_RANKING:
                        {
                            result = KT_TCPHandler.CMD_KT_GUILDWAR_RANKING(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_YOULONG:
                        {
                            result = KT_TCPHandler.ProcessYouLongRequest(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_UPDATE_CURRENT_ROLETITLE:
                        {
                            result = KT_TCPHandler.ResponseChangeCurrentRoleTitle(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_G2C_PLAYERPRAY:
                        {
                            result = KT_TCPHandler.ResponsePlayerPray(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CLIENT_SERVER_LUA:
                        {
                            result = KT_TCPHandler.ProcessClientLua(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_C2G_SPLIT_EQUIP_INTO_FS:
                        {
                            result = KT_TCPHandler.ResponseRefineEquipToFS(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_INPUT_SECONDPASSWORD:
                        {
                            result = KT_TCPHandler.ResponseInputSecondPassword(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_CAPTCHA:
                        {
                            result = KT_TCPHandler.ResponseCaptchaAnswer(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_KT_LUCKYCIRCLE:
                        {
                            result = KT_TCPHandler.ResponseLuckyCircleRequest(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_GETRANDOMNAME:
                        {
                            result = KT_TCPHandler.PrecessGetPreRandomName(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_PLAYER_REFRESH_CACHE:
                        {
                            result = KT_TCPHandler.PlayerRefreshCache(tcpMgr, socket, tcpClientPool, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_REQUEST_JOIN_FACTION:
                        {
                            result = KT_TCPHandler.PlayerRequestJoinFaction(tcpMgr, socket, tcpClientPool, pool, nID, data,
                                count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_LEVELUPPET:
                        {
                            PetManager.ProcessSpriteLevelUpPetCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_REMOVEPET:
                        {
                            PetManager.ProcessSpriteRemovePetCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_STARTTAKEPET:
                        {
                            PetManager.ProcessStartTakePetCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_SENDGIFTPET:
                        {
                            PetManager.ResponseSendGiftPet(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_SENDMAKEEQUIPPET:
                        {
                            PetManager.ResponseMakeEquip(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SYNC_TIME_BY_CLIENT:
                        {
                            // không xử lý
                            result = TCPProcessCmdResults.RESULT_OK;
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_CREATE_CHANNGUYEN:
                        {
                            PetManager.CreateChanNguyen(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }

                    case (int)TCPGameServerCmds.CMD_SPR_ENHANCE_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseEquipEnhance(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_CRAFTING_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseCNCrafting(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_HERO_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseCNHero(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_UNBINDING_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseCNUnBinding(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_RESET_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseCNReset(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_SPR_COMPLETE_RESET_CHANNGUYEN:
                        {
                            result = ChanNguyenManager.ResponseCNResetComplete(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_OPEN_RECHARGE_AWARD:
                        {
                            result = KT_TCPHandler.ResponseOpenRechargeAward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_RECEIVE_DAILY_RECHARGE_AWARD:
                        {
                            result = KT_TCPHandler.ResponseReceiveRechargeAward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_TAMLONG:
                        {
                            result = KT_TCPHandler.ResponseTamLongResult(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_FLOWER_STATUS:
                        {
                            result = KT_TCPHandler.ResponseGetFlowerBetStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            Console.WriteLine("CMD_KT_GET_FLOWER_STATUS: " +result);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_FLOWER_REWARD:
                        {
                            result = KT_TCPHandler.ResponseGetFlowerBetReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_PLACE_FLOWER_BET:
                        {
                            result = KT_TCPHandler.ResponsePlaceFlowerBet(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_TOWER_STATUS:
                        {
                            result = KT_TCPHandler.ResponseGetFlowerTowerStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_C2G_OPENSHOP:
                        {
                            result = KT_TCPHandler.ResponsePlayerOpenShop(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_UNION_STATUS:
                        {
                            result = KT_TCPHandler.ResponseGetUnionStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_UNION_REFUND:
                        {
                            result = KT_TCPHandler.ResponseGetUnionRefund(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_GET_TOWER_REWARD:
                        {
                            result = KT_TCPHandler.ResponseGetTowerReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_SKIP_TOWER_TASK:
                        {
                            result = KT_TCPHandler.ResponseSkipTowerTask(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_PET_RACING_BET:
                        {
                            result = KT_TCPHandler.ResponseGetRacingPetBetStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_PET_RACE_RECEIVE_REWARD:
                        {
                            result = KT_TCPHandler.ResponseGetRacingPetRewardReceive(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_PET_RACE_GET_ACCUMULATE_REWARD:
                        {
                            result = KT_TCPHandler.ResponseGetAccumulateRewardReceive(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_RESET_SKILL_POINT:
                        {
                            result = KT_TCPHandler.ResetSkillPoint(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_CHECK_UNREAD_MAIL:
                        {
                            result = KT_TCPHandler.ProcessGetMailCountCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_OPEN_RECHARGE_ACCUMULATE_EVENT:
                        result = RechargeEventManager.Instance.ProcessRechargeEventInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_KT_RECEIVE_RECHARGE_ACUMMULATE_REWARD:
                        result = RechargeEventManager.Instance.ProcessReceiveRechargeAccumulateReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_KT_GET_SLOTMACHINE_NUMBER:
                    {
                        result = KT_TCPHandler.ResponseGetSlotMachineNumber(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_GET_SLOTMACHINE_STATUS:
                    {
                        result = KT_TCPHandler.ResponseGetSlotMachineStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_GET_SLOTMACHINE_GLOBAL:
                    {
                        result = KT_TCPHandler.ResponseGetSlotMachineGlobal(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_GET_SLOTMACHINE_RANKING:
                    {
                        result = KT_TCPHandler.ResponseGetSlotMachineRanking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_SLOTMACHINE_FREE_TICKET:
                    {
                        result = KT_TCPHandler.ReceiveSlotMachineFreeTicket(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_SEND_OFFERING:
                    {
                        result = KT_TCPHandler.ResponseSendOffering(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int) TCPGameServerCmds.CMD_KT_GET_OFFERINGS_STATUS:
                    {
                        result = KT_TCPHandler.ResponseGetOfferingStatus(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }

                    case (int) TCPGameServerCmds.CMD_KT_CLAIM_OFFERINGS_MILESTONE:
                    {
                        result = KT_TCPHandler.ResponseClaimOfferingMilestone(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMT_KT_OPEN_QUEST_EVENT:
                        {
                            result = QuestEventManager.ProcessQuestEventInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_RECEIVE_QUEST_EVENT_REWARD:
                        {
                            result = QuestEventManager.ProcessReceiveQuestEventReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int) TCPGameServerCmds.CMD_KT_FISHING:
                    {
                        result = KT_TCPHandler.ProcessFishing(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_OPEN_DAILY_RECHARGE:
                    {
                        result = DailyRechargeEventManager.ProcessDailyRechargeEventInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }

                    case (int)TCPGameServerCmds.CMD_KT_RECEIVE_DAILY_RECHARGE_REWARD:
                    {
                        result = DailyRechargeEventManager.ProcessReceiveDailyRechargeReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_KT_CRYSTAL_BAG:
                    {
                        result = KT_TCPHandler.ResponseCrystalBag(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_DANPHONG_GETINFO:
                    {
                        result = KT_TCPHandler.ResponseGetDanphongInfo(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_DANPHONG_CREATE:
                    {
                        result = KT_TCPHandler.ResponseDanphongMaking(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_BT_CREATE_HOLE:
                    {
                        result = KT_TCPHandler.ResponseBaoThachCreateHole(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int)TCPGameServerCmds.CMD_BT_EQUIP:
                    {
                        result = KT_TCPHandler.ResponseBaoThachEquip(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    }
                    case (int) TCPGameServerCmds.CMD_BT_UPGRADE:
                        result = KT_TCPHandler.ResponseBaoThachUpgrade(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_BT_UNEQUIP:
                        result = KT_TCPHandler.ResponseBaoThachUnequip(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_BT_RESET:
                        result = KT_TCPHandler.ResponseBaoThachReset(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_BT_CONFIRM_RESET:
                        result = KT_TCPHandler.ResponseBaoThachConfirmReset(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_BT_ROLL:
                        result = KT_TCPHandler.ResponseDoThach(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_BT_ROLL_UPDATE:
                        result = KT_TCPHandler.ResponseDoThach_Update(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_KT_APPROVE_VC:
                        result = KT_TCPHandler.SendInitVoiceChatToPlayer(GameManager.ClientMgr.FindClient(socket));
                        break;
                    case (int) TCPGameServerCmds.CMD_KT_DISABLE_VC:
                        result = KT_TCPHandler.SendForceQuitRoomVoiceChatToPlayer(GameManager.ClientMgr.FindClient(socket));
                        break;
                    case (int) TCPGameServerCmds.CMD_KT_DAILYCHECKIN_UPDATE:
                        result = KT_TCPHandler.ResponseDailyCheckinUpdate(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_KT_DAILYCHECKIN_CHECKIN:
                        result = KT_TCPHandler.ResponseDailyCheckinCheckin(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int) TCPGameServerCmds.CMD_KT_DAILYCHECKIN_REWARD:
                        result = KT_TCPHandler.ResponseDailyCheckinReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool,
                            nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_DELETE_ACCOUNT:
                        result = KT_TCPHandler.ProcessDeleteAccountCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                        break;
                    case (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_STATUS:
                        {
                            result = KT_TCPHandler.ResponseGetBattlePassInfo(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_REWARD:
                        {
                            result = KT_TCPHandler.ResponseBattlePassClaimReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_BUYLEVEL:
                        {
                            result = KT_TCPHandler.ResponseBattlePassBuyLevel(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    // case (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_BUYELITE:
                    //     {
                    //         result = KT_TCPHandler.ResponseBattlePassBuyElite(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                    //         break;
                    //     }
                    case (int)TCPGameServerCmds.CMD_KT_BATTLEPASS_ALLREWARD:
                        {
                            result = KT_TCPHandler.ResponseBattlePassClaimAll(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_UPDATE_REFUND_EVENT:
                        {
                            result = RefundEventManager.ProcessRefundEventInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_BUY_REFUND_PACK:
                        {
                            result = RefundEventManager.ProcessPurchaseRefundEventPack(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_RECEIVE_REFUND_REWARD:
                        {
                            result = RefundEventManager.ProcessClaimRefundEventReward(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_STATUS:
                        {
                            result = LuckySpinManager.ProcessLuckySpinInfoCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_SPIN:
                        {
                            result = LuckySpinManager.ProcessSpinCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_CLAIM_ACCUMULATE:
                        {
                            result = LuckySpinManager.ProcessLuckySpinClaimAccumulateCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_LUCKYSPIN_RANKING:
                        {
                            result = LuckySpinManager.ProcessLuckySpinRankingCmd(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_MEDAL_OP:
                        {
                            result = KT_TCPHandler.ResponseMedalOp(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket);
                            break;
                        }
                    case (int)TCPGameServerCmds.CMD_KT_C2G_CHANGE_AUTO_STATE:
                        {
                            string cmdData = new UTF8Encoding().GetString(data, 0, count);
                            bool isValidState = cmdData == "0" || cmdData == "1";
                            string response = $"{cmdData}:{(isValidState ? 1 : 0)}";

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, response, nID);
                            result = TCPProcessCmdResults.RESULT_DATA;
                            break;
                        }

                    default:
                        {
                            LogManager.WriteLog(LogTypes.Error, string.Format("Undefined packet, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                            if (GameManager.FlagAlowUnRegistedCmd)
                            {
                                result = TCPProcessCmdResults.RESULT_OK;
                            }
                            else
                            {
                                result = TCPProcessCmdResults.RESULT_FAILED;
                            }
                            break;
                        }
                }
            }

            #endregion Danh sách Packet

            //         if (KTGlobal.GetCurrentTimeMilis() - tick >= 100)
            //{
            //             LogManager.WriteLog(LogTypes.RolePosition, "Packet " + (TCPProcessCmdResults) nID + " took more than 0.1s...");
            //}

            TotalHandledCmdsNum++;

            long nowTicks = TimeUtil.NOW();
            long usedTicks = nowTicks - startTicks;
            if (usedTicks > 0)
            {
                if (usedTicks > MaxUsedTicksByCmdID)
                {
                    MaxUsedTicksCmdID = nID;
                    MaxUsedTicksByCmdID = usedTicks;
                }
            }

            lock (HandlingCmdDict)
            {
                HandlingCmdDict.Remove(socket);
            }

            return result;
        }
    }
}
