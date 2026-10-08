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

using GameServer.KiemThe.GameEvents.PetRace;
using System.Xml.Serialization;
using GameServer.KiemThe.Entities;
using UnityEngine;
using System.Numerics;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ResponseGetRacingPetBetStatus(TCPManager tcpMgr, TMSKSocket socket,
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
                var fields = cmdData.Split(':');
                if(fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;

                }

                if (!int.TryParse(fields[0], out var racerId) || !int.TryParse(fields[1], out var packId))
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var script = PetRace_ActivityScript.activity_script;
                if (script == null)
                {
                    KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Hiện tại không có cuộc đua nào đang diễn ra!" });
                    return TCPProcessCmdResults.RESULT_OK;
                }
                    
                if((int)script.currentRaceState == -1)
                {
                    SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox",new int[0],new List<string> { "Message", "Hiện tại không có cuộc đua nào đang diễn ra!" });
                    return TCPProcessCmdResults.RESULT_OK;
                }
                else if((int)script.currentRaceState != 1)
                {
                    SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Hiện tại không phải thời gian bình chọn!" });
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", nID);
                    return TCPProcessCmdResults.RESULT_DATA;
                }
            
                bool success = script.Bet(client, racerId, packId);
                if (success)
                {
                    GlobalEventSource.getInstance().fireEvent(new ThamGiaDuaThuEventObject(client, packId));
                }

                var returnData = success ? "1" : "0";
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, returnData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ResponseGetRacingPetRewardReceive(TCPManager tcpMgr, TMSKSocket socket,
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

                var script = PetRace_ActivityScript.activity_script;
                if (script == null)
                {
                    KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], new List<string> { "Message", "Hiện tại không có cuộc đua nào đang diễn ra!" });
                    return TCPProcessCmdResults.RESULT_OK;
                }

                bool success = script.SetRewardReceived(client);
                var returnData = success ? "1" : "0";

                if (success)
                {
                    var betRecord = script.GetBetDescription(client);
                    var betId = betRecord.GetY();
                    var betPack = BetPackData.packs[(int)betId - 1];
                    var isWinner = script.IsWinner(client);
                    var rewardItem = isWinner ? betPack.winItem : betPack.loseItem;
                    AddItemAndNotify(client, rewardItem, 1, 0, 1);


                    // record chuỗi thắng
                    // để phục vụ cho hàm Lua nhận thưởng chuỗi
                    if (isWinner) 
                    {
                        // update chain record
                        var recordID = -1;
                        if (betPack.number == 150 || betPack.number == 1000) {
                            recordID = betPack.winRecord;
                        }
                        if (recordID != -1) {
                            var currentChain = client.GetValueOfForeverRecore(recordID);
                            if (currentChain < 0) {
                                currentChain = 0;
                            }
                            client.SetValueOfForeverRecore(recordID, currentChain + 1);
                        }
                    }
                }
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, returnData, nID);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }


        public static TCPProcessCmdResults ResponseGetAccumulateRewardReceive(TCPManager tcpMgr, TMSKSocket socket,
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

                var fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), cmdData.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;

                }

                if (!int.TryParse(fields[0], out var packId) || !int.TryParse(fields[1], out var chestId))
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                BetPack pack = BetPackData.packs[packId - 1];
                int itemId = pack.GetChestItem(chestId);
                int totalWin = client.GetValueOfForeverRecore(pack.winRecord);
                if (totalWin < 0) totalWin = 0;
                ReceiItemOnce(client, packId, itemId, totalWin, chestId);

                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static bool ReceiItemOnce(KPlayer client,int packId, int itemId,int total,int interval)
        {
            if (!KTGlobal.IsHaveSpace(1, client))
            {
                KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0], 
                        new List<string> { "Message", "Bằng hữu cần 1 ô trống trong túi đồ để nhận vật phẩm." });
                return false;
            }
            BetPack pack = BetPackData.packs[packId - 1];
            int record = BetPackData.SEED_RECORD * 100000 + itemId;
            int received = client.GetValueOfForeverRecore(record);
            if (received < 0) received = 0;
            if(total < interval)
            {
                KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0],
                        new List<string> { "Message", "Bằng hữu chưa đạt đủ điểm tích lũy!" });
                return false;
            }
            if (received >= Mathf.FloorToInt((total + 15 - interval) / 15))
            {
                KT_TCPHandler.SendOpenUI(client, "MainGame/EventPopup/PetRace/PetRace_MessageBox", new int[0],
                        new List<string> { "Message", "Bằng hữu đã nhận hết tích lũy vật phẩm này rồi!" });
                return false;
            }
            else
                client.SetValueOfForeverRecore(record, received + 1);

            AddItemAndNotify(client, itemId, 1, 0, 1);
            return true;
        }


        public static bool AddItemAndNotify(KPlayer player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, string ExpriesTime = "")
        {
            int SpaceNeed = ItemManager.TotalSpaceNeed(ItemID, Number);

            if (!KTGlobal.IsHaveSpace(SpaceNeed, player))
            {
                PlayerManager.ShowNotification(player, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (!string.IsNullOrEmpty(ExpriesTime))
                {

                    // "1900-01-01 12:00:00";
                    TimeUsing = ExpriesTime;
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, ItemID, Number, 0, "LUAITEMADD", true, LockStatus, false, TimeUsing, "", Series, "", enhanceLevel, 1, true))
                {
                    PlayerManager.ShowNotification(player, "Có lỗi khi add vật phẩm");

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }


    }


    public static class BetPackData
    {
        public static int SEED_RECORD = 3823;
        private static string packDataFile = "Config/KT_GameEvents/PetRace_Pack.xml";

        public static List<BetPack> packs = new List<BetPack>();

        public static void SetUp()
        {
            string packFile = Global.GameResPath(packDataFile);
            using (var stream = System.IO.File.OpenRead(packFile))
            {
                var serializer = new XmlSerializer(typeof(List<BetPack>));

                packs = serializer.Deserialize(stream) as List<BetPack>;
            }

        }
    }


    public class BetPack
    {
        [XmlAttribute(AttributeName = "ID")]
        public string id;
        [XmlAttribute(AttributeName = "PackName")]
        public string packName;
        [XmlAttribute(AttributeName = "Amount")]
        public int number;
        [XmlAttribute(AttributeName = "MoneyType")]
        public int moneyType;
        [XmlAttribute(AttributeName = "WinItem")]
        public int winItem;
        [XmlAttribute(AttributeName = "LoseItem")]
        public int loseItem;

        [XmlAttribute(AttributeName = "JoinRecord")]
        public int joinRecord;
        [XmlAttribute(AttributeName = "WinRecord")]
        public int winRecord;
        [XmlAttribute(AttributeName = "LuckyChest5")]
        public int luckyChest5;
        [XmlAttribute(AttributeName = "LuckyChest10")]
        public int luckyChest10;
        [XmlAttribute(AttributeName = "LuckyChest15")]
        public int luckyChest15;

        [XmlAttribute(AttributeName = "TotalWin")]
        public int totalWin;


        public int GetChestItem(int chestId)
        {
            switch(chestId)
            {
                case 5:
                    return luckyChest5;
                case 10:
                    return luckyChest10;
                default:
                    return luckyChest15;
            }
        }
    }

}