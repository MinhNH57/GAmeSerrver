using GameServer.Logic;
using GameServer.Tools;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.BaoThach;
using System.Collections.Generic;
using System.Linq;
using static GameServer.KiemThe.GameEvents.BaoThach.BaoThachConfig.Item;
using GameServer.KiemThe.GameEvents.DoThach;
using GameServer.KiemThe.GameEvents.DoThach.Data;
using Server.Data;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ResponseBaoThachCreateHole(TCPManager tcpMgr, TMSKSocket socket,
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

                int goodsid = 0;
                int.TryParse(fields[0], out goodsid);
                int result = BaoThachManager.CreateItemHole(client, goodsid);
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

        public static TCPProcessCmdResults ResponseBaoThachEquip(TCPManager tcpMgr, TMSKSocket socket,
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

                int goodids = 0;
                int goodids_bt = 0;
                int idHole = 0;
                int.TryParse(fields[0], out goodids);
                int.TryParse(fields[1], out goodids_bt);
                int.TryParse(fields[2], out idHole);
                int result = BaoThachManager.Equip(client, goodids, goodids_bt, idHole, 1, "Equip_Baothach");
                if (result != 0)
                {
                    var bytesData = DataHelper.ObjectToBytes("");
                    GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                }
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ResponseBaoThachUpgrade(TCPManager tcpMgr, TMSKSocket socket,
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
                if (fields.Length != 2)
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

                int goodids = 0;
                int amount = 0;
                int.TryParse(fields[0], out goodids);
                int.TryParse(fields[1], out amount);
                int result = BaoThachManager.Upgrade(client, goodids, amount, "Upgrade_Baothach");
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

        public static TCPProcessCmdResults ResponseBaoThachUnequip(TCPManager tcpMgr, TMSKSocket socket,
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
                if (fields.Length != 2)
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

                int goodsid_item = 0;
                int indexHole = 0;
                int.TryParse(fields[0], out goodsid_item);
                int.TryParse(fields[1], out indexHole);
                int result = BaoThachManager.UnEquip(client, goodsid_item, indexHole, "Unequip_Baothach");
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

        public static TCPProcessCmdResults ResponseBaoThachReset(TCPManager tcpMgr, TMSKSocket socket,
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
                if (fields.Length != 2)
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

                int goodsid_item = 0;
                int indexHole = 0;
                int.TryParse(fields[0], out goodsid_item);
                int.TryParse(fields[1], out indexHole);
                var result = BaoThachManager.Reset(client, goodsid_item, indexHole, "Reset_Baothach");
                if (result != 0)
                {
                    GameManager.ClientMgr.SendToClient(client, "", nID);
                }
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }



        public static TCPProcessCmdResults ResponseBaoThachConfirmReset(TCPManager tcpMgr, TMSKSocket socket,
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
                if (fields.Length != 2)
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

                int goodsid_item = 0;
                int indexHole = 0;
                int.TryParse(fields[0], out goodsid_item);
                int.TryParse(fields[1], out indexHole);
                int result = BaoThachManager.ConfirmReset(client, goodsid_item, indexHole, "Reset_Baothach");
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

        public static TCPProcessCmdResults ResponseDoThach_Update(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Do thach loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                strcmd = $"{DoThachManager.GetRemainingRollByKNBString(client)}:{DoThachManager.GetSpinersPityCountDownString(client)}";

                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ResponseDoThach(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Do thach loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }



                int roleID = Convert.ToInt32(fields[0]);
                int spinerId = Convert.ToInt32(fields[1]);
                int action = Convert.ToInt32(fields[2]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (GameManager.IsKuaFuServer)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", "Không thể đổ thạch trong liên server!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                SpinnerConfig spinner = DoThachManager.GetSpiner(spinerId);

                if (!spinner.active)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format($"{spinner.name} tạm thời đóng!, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                Fee fee = DoThachManager.GetRollFee(spinerId, action);
                if (fee == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find fee roll, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int[] cachePitys = DoThachManager.GetSpinerCounters(client, spinerId);
                if (cachePitys == null)
                {
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Lỗi dữ liệu đổ thạch!");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                List<ItemChance> rewards = new List<ItemChance>();
                for (int i = 0; i < fee.spinTimes; i++)
                {
                    ItemChance item = DoThachManager.GetRollItem(client, spinerId);

                    if (item.itemId != 0)
                    {
                        rewards.Add(item);
                    }
                }
                int totalSpackNeed = rewards.Sum(x => ItemManager.TotalSpaceNeed(x.itemId, x.amount));
                if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                {
                    //nếu túi đồ không đủ chỗ thì reset pity
                    DoThachManager.ResetSpinnerCounter(client, spinerId, cachePitys);
                    PlayerManager.ShowMessageBox(client, "Thông báo", $"Túi đồ không đủ chỗ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (action == 2)// quay bằng knb
                {
                    if (!KTGlobal.IsHaveMoney(client, fee.amount, (MoneyType)fee.id))
                    {
                        //nếu không đủ KNB thì reset pity
                        DoThachManager.ResetSpinnerCounter(client, spinerId, cachePitys);
                        PlayerManager.ShowMessageBox(client, "Thông báo", "Không đủ KNB!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }

                    int remainingRollTimes = DoThachManager.GetRemainingRollByKNB(client, spinerId);
                    if (remainingRollTimes >= fee.spinTimes)
                    {
                        client.SetValueOfDailyRecore((int)AcitvityRecore.DoThach_KNB_0 + spinerId, remainingRollTimes - fee.spinTimes);
                    }
                    else
                    {
                        DoThachManager.ResetSpinnerCounter(client, spinerId, cachePitys);
                        PlayerManager.ShowMessageBox(client, "Thông báo", $"Hôm nay bạn đã quay hêt lượt quay bằng KNB!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    KTGlobal.SubMoney(client, fee.amount, (MoneyType)fee.id, "Đổ thạch");
                }
                else
                {
                    if ((ItemManager.GetItemCountInBag(client, fee.id) < fee.amount))
                    {
                        DoThachManager.ResetSpinnerCounter(client, spinerId, cachePitys);
                        PlayerManager.ShowMessageBox(client, "Thông báo", $"Không đủ bảo thạch thô!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    ItemManager.RemoveItemFromBag(client, fee.id, fee.amount, -1, "Đổ thạch");
                }



                foreach (var reward in rewards)
                {
                    if (!ItemManager._TotalGameItem.TryGetValue(reward.itemId, out ItemData itemData))
                    {
                        LogManager.WriteLog(LogTypes.Error, string.Format("Can not find item corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                        return TCPProcessCmdResults.RESULT_FAILED;

                    }
                    if (DoThachManager.CheckNotification(client, spinner.id, reward.itemId))
                        KTGlobal.SendSystemEventNotification($"Chúc mừng {client.RoleName} {spinner.name} nhận được {itemData.Name}");

                    ItemManager.CreateItem(pool, client, reward.itemId, reward.amount, 0, "DO_THACH",
                    true, 1, false, Global.ConstGoodsEndTime, HitItem: 0);

                    LogManager.WriteLog(LogTypes.BaoThach, $"{client.RoleName}({client.RoleID}) {spinner.name} nhận được item({reward.itemId})", pushToLoki: false);

                    // [DEMO] Structured log — mỗi field query/filter riêng trên Grafana
                    SerilogBootstrap.Logger?.WriteStructured("BaoThach", "Đổ thạch nhận thưởng",
                        ("RoleID", client.RoleID),
                        ("RoleName", client.RoleName),
                        ("SpinnerName", spinner.name),
                        ("ItemID", reward.itemId),
                        ("ItemName", itemData.Name),
                        ("Amount", reward.amount)
                    );
                    //PlayerManager.ShowNotification(client, $"Chúc mừng bạn nhận được vật phẩm {rewardItem.ID}");
                }

                string rewardsStr = string.Join("|", rewards.Select(x => $"{x.itemId}-{x.amount}"));
                strcmd = $"{rewardsStr}:{DoThachManager.GetRemainingRollByKNBString(client)}:{DoThachManager.GetSpinerPityCountDownString(client, spinerId)}";
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
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