using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GameServer.Logic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.GameDbController;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using GameServer.KiemThe.Entities.Player;
using static UnityEngine.GridBrushBase;
using System.Security.Cryptography;
using GameServer.KiemThe.Entities;
using Server.Data;
using System.Windows;
using UnityEngine;
using System.Reflection.Emit;
using GameServer.KiemThe.Core.Task;

namespace GameServer.KiemThe.GameEvents.TangBaoDo
{
    /// <summary>
    /// Định nghĩa Tàng bảo đồ  
    /// </summary>
    public static class TangBaoDo
    {
        #region Define
        /// <summary>
        /// Thông tin Tàng Bảo Đồ
        /// </summary>
        [ProtoContract]
        [XmlRoot(ElementName = "TangBaoDo")]
        public class TangBaoDoInfo
        {
            [ProtoMember(1)]
            [XmlElement(ElementName = "Item")]
            public AllAwardInfo _Award;

            [ProtoMember(2)]
            [XmlElement(ElementName = "Map")]
            public AllMapInfo _MapInfo;
        }
        #endregion

        #region Properties
        public static string KTTangBaoDo = "Config/KT_GameEvents/TangBaoDo.xml";
        public static TangBaoDoInfo _TangBaoDoInfo;
        // Các map code để random vị trí
        public static List<int> AllMapCode = new List<int>() { 8, 33, 40, 43 };
        #endregion

        #region Public methods
        public static void Init()
        {
            string FileMisson = Global.GameResPath(KTTangBaoDo);

            using (var stream = System.IO.File.OpenRead(FileMisson))
            {
                var serializer = new XmlSerializer(typeof(TangBaoDoInfo));
                _TangBaoDoInfo = serializer.Deserialize(stream) as TangBaoDoInfo;
            }
        }

        //public static int GetRandomPos()
        //{
        //    int current = -1;
        //    current = KTGlobal.GetRandomNumber(0, _TangBaoDoInfo._MapInfo._MapInfo.Count - 1);
        //    return current;
        //} 
        /// <summary>
        /// Lấy vị trí theo tọa độ thực
        /// </summary>
        public static bool GetRandomPosNew(KPlayer player, int itemID, bool isDone = false)
        {
            int current = KTGlobal.GetRandomNumber(0, _TangBaoDoInfo._MapInfo._MapInfo.Count - 1);
            //int MapCode = KTGlobal.GetRandomNumber(0, AllMapCode.Count - 1);            
            int MapCode = _TangBaoDoInfo._MapInfo._MapInfo[current].MapCode;
            GameMap Map = GameManager.MapMgr.DictMaps[MapCode];
            int posX = KTGlobal.GetRandomNumber(0, Map.MapWidth);
            int posY = KTGlobal.GetRandomNumber(0, Map.MapHeight);
            if (isDone)
            {
                MapCode = _TangBaoDoInfo._MapInfo._MapInfo[current].MapCode;
                posX = _TangBaoDoInfo._MapInfo._MapInfo[current].PosX;
                posY = _TangBaoDoInfo._MapInfo._MapInfo[current].PosY;
                SaveDataToItem(player, itemID, MapCode, posX, posY);
                return true;
            }
            if (!Map.CanMove(new System.Windows.Point(posX, posY)))
            {
                return false;
            }
            else
            {
                SaveDataToItem(player, itemID, MapCode, posX, posY);
                return true;
            }
        }
        /// <summary>
        /// kiểm tra vị trí người chơi có gần không
        /// </summary>
        public static bool IsNearToFindChest(KPlayer player, int itemID)
        {
            MapInfo map = GetDataFromItem(player, itemID);
            if(map == null)
            {
                return false;
            }
            Vector2 posPlayer = new Vector2(player.PosX, player.PosY);
            Vector2 posChest = new Vector2(map.PosX, map.PosY);
            if(map.MapCode == player.MapCode && Vector2.Distance(posPlayer,posChest) < 60)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Thêm thông tin Tàng Bảo Đồ
        /// </summary>
        public static void SaveDataToItem(KPlayer player, int itemID, int MapCode, int posX, int posY)
        {
            if (player.GoodsDataList != null)
            {
                GoodsData tangBaoDoItem = null;
                lock (player.GoodsDataList)
                {
                    tangBaoDoItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
                }

                if(tangBaoDoItem != null)
                {
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, tangBaoDoItem.Id);

                    Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();

                    OtherParams.Add(ItemPramenter.Pram_1, MapCode + "");
                    OtherParams.Add(ItemPramenter.Pram_2, posX + "");
                    OtherParams.Add(ItemPramenter.Pram_3, posY + "");

                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                    if (!ItemManager.UpdateItemPrammenter(TotalUpdate, tangBaoDoItem, player, "TANGBAODO", true))
                    {
                        string cmd = $"Người chơi: {player.RoleName} lưu vị trí vào tàng bảo đồ {tangBaoDoItem.Id} thất bại";
                        LogManager.WriteLog(LogTypes.Error, cmd);
                    }
                }
            }
        }
        public static MapInfo GetDataFromItem(KPlayer player, int itemID)
        {
            if (player.GoodsDataList != null)
            {
                GoodsData tangBaoDoItem = null;
                lock (player.GoodsDataList)
                {
                    tangBaoDoItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
                }
                int posX = 0;
                int mapCode = 0;
                int posY = 0;
                try 
                {
                    if (tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string MapCode) &&
                        tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string PosX) &&
                        tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string PosY))
                    {
                        mapCode = Int32.Parse(MapCode);
                        posX = Int32.Parse(PosX);
                        posY = Int32.Parse(PosY);
                    }
                    return new MapInfo() { MapCode = mapCode, PosX = posX, PosY = posY };
                }
                catch(Exception ex)
                {
                    return null;
                }
            }
            return null;
        }
        /// <summary>
        /// kiểm tra xem thông tin Tàng Bảo Đồ đã có chưa 
        /// </summary>
        public static bool IsHaveDataFromItem(KPlayer player, int itemID)
        {
            if (player.GoodsDataList != null)
            {
                GoodsData tangBaoDoItem = null;
                lock (player.GoodsDataList)
                {
                    tangBaoDoItem = player.GoodsDataList.Where(x => x.Id == itemID).FirstOrDefault();
                }
                int posX = 0;
                int mapCode = 0;
                int posY = 0;
                if (tangBaoDoItem.OtherParams == null)
                    return false;
                if (tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string MapCode) &&
                    tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string PosX) &&
                    tangBaoDoItem.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string PosY))
                {
                    try
                    {
                        mapCode = Int32.Parse(MapCode);
                        posX = Int32.Parse(PosX);
                        posY = Int32.Parse(PosY);
                        return true;
                    }
                    catch(Exception ex)
                    {
                        return false;
                    }
                }
            }
                
            return false;
        }
        public static void CreateProgessFindChest(KPlayer client, int itemID)
        {
            KNPCDialog dialog = new KNPCDialog();
            dialog.Text = "Kho báu ngay ở dưới chân. Chọn Bắt đầu đào để mở kho báu nào!";
            dialog.Selections = new Dictionary<int, string>()
                                    {
                                        { 1, "Bắt đầu đào"},
                                        { 1000,"Để ta suy nghĩ thêm" }
                                    };
            dialog.OnSelect = (data) =>
            {
                if (data.SelectID == 1)
                {
                    // check thêm lần nữa 
                    if (!IsNearToFindChest(client, itemID))
                    {
                        PlayerManager.ShowNotification(client, "Vị trí không chính xác, không tìm thấy kho báu ở đây hãy thử lại");
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                        return;
                    }
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                    int durationSecond = 10;
                    client.CurrentProgress = new KPlayer_Progress()
                    {
                        Hint = $"Đang đào kho báu",
                        InteruptIfTakeDamage = true,
                        StartTick = KTGlobal.GetCurrentTimeMilis(),
                        DurationTick = durationSecond * 1000,
                        Complete = () =>
                        {
                            AddGiftToPlayer(client, itemID);
                        },
                        Cancel = () =>
                        {
                            PlayerManager.ShowNotification(client, "Đã ngừng đào kho báu");
                        }
                    };
                }
                else if (data.SelectID == 1000)
                {
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                }
            };
            dialog.ShowDialog(client);

        }
        public static void CreateDialogFindChest(KPlayer player, int itemID)
        {
            MapInfo map;
            map = GetDataFromItem(player, itemID);
            if (map == null) 
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi đọc tàng bảo đồ xin thử lại sau.");
                return; 
            }
            //int IDMap = this.RefObject.GetValueOfRecoreTangBaoDo(itemID);
            GameMap Map = GameManager.MapMgr.DictMaps[map.MapCode];

            /// Tạo Dialog
            KNPCDialog dialog = new KNPCDialog();

            UnityEngine.Vector2Int gridPos = KTGlobal.WorldPositionToGridPosition(Map, new Vector2Int(map.PosX, map.PosY));
            string strcmd = string.Format("<color=#3dfff9><link=\"GoTo_{0}_{1}_{2}\">[{3} ({4}, {5})]</link></color>", Map.MapCode, map.PosX, map.PosY, Map.MapName, gridPos.x, gridPos.y);

            dialog.Text = $"Đây là Tàng bảo đồ của {Map.MapName}. \n" +
                "Vị trí kho báu bí ẩn nằm tại " + strcmd + "\n";
            dialog.Text += "Hãy đến vị trí đó sử dụng tàng bảo đồ lần nữa để đào kho báu.";
            //int labanCount = ItemManager.GetItemCountInBag(player, 331);
            //if(labanCount > 0)
            //{
            //    dialog.Selections = new Dictionary<int, string>()
            //                        {
            //                            { 1, "Dùng 1 cái la bàn để đi đến nơi đào kho báu." },
            //                            { 1000, "Để ta tự tìm đường đến đó." }
            //                        };
            //    dialog.OnSelect = (data) =>
            //    {
            //        if (data.SelectID == 1)
            //        {
            //            if(!ItemManager.RemoveItemFromBag(player, 331, 1))
            //            {
            //                PlayerManager.ShowNotification(player, "Không tìm thấy la bàn");
            //                KT_TCPHandler.CloseDialog(player);
            //                KTNPCDialogManager.RemoveNPCDialog(dialog);
            //                return;
            //            }
            //            // Gửi gói tin về client để đọc các vị trí cho người chơi chạy đến
            //            string dataStrcmd = string.Format("{0}:{1}:{2}:{3}:{4}", player.RoleID, map.MapCode, map.PosX, map.PosY, itemID);
            //            player.SendPacket((int)TCPGameServerCmds.CMD_SPR_AUTOMOVETOPOS, dataStrcmd);
            //            KT_TCPHandler.CloseDialog(player);
            //            KTNPCDialogManager.RemoveNPCDialog(dialog);
            //        }
            //        else if (data.SelectID == 1000)
            //        {
            //            KT_TCPHandler.CloseDialog(player);
            //            KTNPCDialogManager.RemoveNPCDialog(dialog);
            //        }
            //    };
            //}
            //else
            //{
            //    dialog.Text += "Hãy đến vị trí đó sử dụng tàng bảo đồ lần nữa để đào kho báu. Ngươi có thể mua la bàn ở tạp hóa để đến vị trí đó nhanh hơn";
            //}


            dialog.ShowDialog(player);
        }
        #endregion

        #region Private methods

        private static List<AwardInfo> GetItemAward()
        {
            AllAwardInfo _GiftSelect = _TangBaoDoInfo._Award;
            List<AwardInfo> RollAwardItem = new List<AwardInfo>();
            AwardInfo _SelectItem = null;
            foreach(var item in _GiftSelect._Award)
            {
                if(item.Rate == -1)
                {
                    RollAwardItem.Add(item);
                }
            }
            if (_GiftSelect != null)
            {
                int Random = KTGlobal.GetRandomNumber(0, 100);

                int idx = 0;
                foreach (AwardInfo _Item in _GiftSelect._Award.Where(x => x.Rate != -1).ToList())
                {
                    Random = Random - _Item.Rate;

                    if (Random <= 0)
                    {
                        _SelectItem = _Item;
                        RollAwardItem.Add(_Item);
                        break;
                    }
                    idx++;
                }
            }
            
            return RollAwardItem;
        }

        private static void GetAwardByTime(int Sec, AwardInfo ItemGet, KPlayer player)
        {
            var ReviveItem = KTKTAsyncTask.Instance.ScheduleExecuteAsync(new DelayAsyncTask("TangBaoDo", ItemGet, player, TimerProc), Sec * 1000);
        }

        private static void TimerProc(object sender, EventArgs e)
        {
            DelayAsyncTask _Task = (DelayAsyncTask)sender;

            KPlayer _Player = _Task.Player;

            AwardInfo _ItemGet = (AwardInfo)_Task.Tag;


            if (_ItemGet != null)
            {
                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Task.Player, _ItemGet.ItemID, _ItemGet.Number, 0, "TANGBAODO", false, 1, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
                {
                    PlayerManager.ShowNotification(_Player, "Có lỗi khi nhận vật phẩm chế tạo");
                }
            }

            //throw new NotImplementedException();
        }
        private static void AddGiftToPlayer(KPlayer client, int itemID)
        {
            List<AwardInfo> GetAllGiftCanGet = _TangBaoDoInfo._Award._Award;

            // Nếu có quà có thể nhận
            if (GetAllGiftCanGet.Count > 0)
            {
                List<AwardInfo> items = TangBaoDo.GetItemAward();
                int totalSpaceNeeded = 1;

                foreach (var item in items)
                {
                    if(item.ItemID > 0)
                        totalSpaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ItemID, item.Number);
                }

                if (KTGlobal.IsHaveSpace(totalSpaceNeeded, client))
                {
                    bool AddOK = true;
                    foreach (var item in items)
                    {
                        if (item.ItemID > 0)
                        {
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, item.ItemID, item.Number, 0, "TANGBAODO", false, 1, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
                            {
                                PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm chế tạo");
                                AddOK = false;
                            }
                        }
                    }
                    if (AddOK)
                    {
                        foreach (var item in items)
                        {
                            if (item.ItemID == 0)
                            {
                                GameManager.ClientMgr.ProcessRoleExperience(client, item.Number, true, false, true, "TANGBAODO_" + itemID);
                            }
                        }
                        ItemManager.RemoveItem(client, itemID);
                    }
                }
                else
                {
                    PlayerManager.ShowNotification(client, $"Cần sắp xếp {totalSpaceNeeded} ô trống trong túi đồ để nhận thưởng!");
                }
            }
        }
        #endregion

        #region TCP_Network
        public static TCPProcessCmdResults ProcessSpriteStartFindTangBaoDoAwardGiftCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                int itemID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                CreateProgessFindChest(client, itemID);
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        #endregion
    }

    /// <summary>
    /// Vật phẩm nhận được Tàng Bảo Đồ
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Item")]
    public class AllAwardInfo
    {
        [ProtoMember(1)]
        [XmlElement(ElementName = "RollAwardItem")]
        public List<AwardInfo> _Award;
    }

    /// <summary>
    /// Thông tin các vị trí để đào kho báu Tàng Bảo Đồ
    /// </summary>
    [ProtoContract]
    [XmlRoot(ElementName = "Map")]
    public class AllMapInfo
    {
        [ProtoMember(1)]
        [XmlElement(ElementName = "MapInfo")]
        public List<MapInfo> _MapInfo;
    }
    [ProtoContract]
    [XmlRoot(ElementName = "RollAwardItem")]
    public class AwardInfo
    {
        /// <summary>
        /// ID vật phẩm
        /// </summary>
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "ItemID")]
        public int ItemID { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "Number")]
        public int Number { get; set; }

        /// <summary>
        /// RATE sẽ random ra
        /// </summary>
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "Rate")]
        public int Rate { get; set; }
    }
    [ProtoContract]
    [XmlRoot(ElementName = "MapInfo")]
    public class MapInfo
    {
        [ProtoMember(1)]
        [XmlAttribute(AttributeName = "MapCode")]
        public int MapCode { get; set; }
        /// <summary>
        /// tọa độ của X
        /// </summary>
        [ProtoMember(2)]
        [XmlAttribute(AttributeName = "PosX")]
        public int PosX { get; set; }
        /// <summary>
        /// tọa độ của Y
        /// </summary>
        [ProtoMember(3)]
        [XmlAttribute(AttributeName = "PosY")]
        public int PosY { get; set; }
        /// <summary>
        /// tọa độ random của X
        /// </summary>
        [ProtoMember(4)]
        [XmlAttribute(AttributeName = "RateX")]
        public int RateX { get; set; }
        /// <summary>
        /// tọa độ random của Y
        /// </summary>
        [ProtoMember(5)]
        [XmlAttribute(AttributeName = "RateY")]
        public int RateY { get; set; }
    }
}
