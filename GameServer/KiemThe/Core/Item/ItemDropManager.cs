using GameServer.Core.Executor;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.KuaFuBalance;
using GameServer.KiemThe.Logic.Manager.Battle;
using Newtonsoft.Json;
using GameServer.KiemThe.GameEvents.MonthlyEvents;

namespace GameServer.KiemThe.Core.Item
{
    public class ItemDropManager
    {
        public static Dictionary<int, int> DropExpiryConfig;
        public static string DropExpiry_JSON = "Config/KT_Drop/drop_expiry.json";
        public static string DropBuild_XML = "Config/KT_Drop/DropBuild.xml";

        public static string MapDropProfile_XML = "Config/KT_Drop/MapDropProfile.xml";

        public static string PickUpItemNotifyfile_XML = "Config/KT_Drop/PickUpItemNotify.xml";

        /// <summary>
        /// File quy định danh sách tỷ lệ rơi ở quái
        /// </summary>
        public static string MonsterDropRateFile_XML = "Config/KT_Drop/MonsterDropRate.xml";

        public static List<DropProfile> TotalProfile = new List<DropProfile>();

        public static List<MapDropInfo> TotalMapDrop = new List<MapDropInfo>();

        /// <summary>
        /// Danh sách tỷ lệ rơi đồ ở quái
        /// </summary>
        public static Dictionary<int, MonsterDropRate> MonsterDrops { get; } = new Dictionary<int, MonsterDropRate>();

        /// <summary>
        /// Danh sách vật phẩm sẽ thông báo khi nhặt
        /// </summary>
        public static PickUpItemNotify PickUpItemNotify { get; private set; }

        public static int ItemRandomRager = 1000000;

        /// <summary>
        /// Loading all Drop
        /// </summary>
        public static void Setup()
        {
            Console.WriteLine("Loading Drop Profile..");
            string Files = Global.GameResPath(DropBuild_XML);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<DropProfile>));
                TotalProfile = serializer.Deserialize(stream) as List<DropProfile>;
            }

            Console.WriteLine("Loading Monster Drop Config..");
            Files = Global.GameResPath(MapDropProfile_XML);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<MapDropInfo>));
                TotalMapDrop = serializer.Deserialize(stream) as List<MapDropInfo>;
            }

            Console.WriteLine("Loading Pick Up Item Notify..");
            Files = Global.GameResPath(PickUpItemNotifyfile_XML);
            XElement pickUpItemNotifyNode = XElement.Parse(File.ReadAllText(Files));
            ItemDropManager.PickUpItemNotify = PickUpItemNotify.Parse(pickUpItemNotifyNode);

            Console.WriteLine("Loading Monster Drop Rate..");
            Files = Global.GameResPath(MonsterDropRateFile_XML);
            XElement monsterDropRateNode = XElement.Parse(File.ReadAllText(Files));
            foreach (XElement node in monsterDropRateNode.Elements("DropInfo"))
            {
                MonsterDropRate dropRate = MonsterDropRate.Parse(node);
                MonsterDrops[dropRate.MonsterID] = dropRate;
            }

            DropExpiryConfig = new Dictionary<int, int>();
            Files = Global.GameResPath(DropExpiry_JSON);
            if (File.Exists(Files))
            {
                var configStr = File.ReadAllText(Files);
                var settings = new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                DropExpiryConfig = JsonConvert.DeserializeObject<Dictionary<int, int>>(configStr, settings);
            }
        }

        private static bool IsKuaFuBalanceMonsterDrop(Monster monster)
        {
            return monster != null && KuaFuBalanceManager.Instance.IsBalanceMap(monster.CurrentMapCode);
        }

        /// <summary>
        /// Gọi đến khi quái chết để rơi vật phẩm tương ứng
        /// </summary>
        /// <param name="MonsertID"></param>
        /// <param name="Player"></param>
        public static void GetDropMonsterDie(Monster Monster, KPlayer Player)
        {
            if (IsKuaFuBalanceMonsterDrop(Monster))
            {
                return;
            }

            //Lấy ra may mắn hiện tại
            int Lucky = Player.m_nCurLucky;

            if ((int)Monster.MonsterType == 4)
            {
               if (!PirateTaskManager.getInstance().IsHavePitateQuest(Player, Monster))
               {
                   return;
               }
               else
               {
                    ///// rơi theo file dropHaiTac
                    //getDropHaiTac(Monster, Player);
                    //return;
               }
            }

            /// nếu là boss ở bạch hổ đường thì thôi
            List<int> mapBHD = new List<int>() { 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 346, 347, 348, 349, 350, 351, 352, 354, 355, 356, 357, 358, 359, 360 };

            if ((int)Monster.MonsterType == 3 && mapBHD.Contains(Player.CurrentMapCode))
            {
                /// ko cho rơi theo dropbuild nữa
                return;
            }    

            // nếu là boss ở tiêu dao cốc thì chỉ cho rơi 4 món

            if ((int)Monster.MonsterType == 3 && Player.CurrentMapCode >= 298 && Player.CurrentMapCode <=324)
            {
                Lucky = -1;
            }    

            /// nếu là boss ở tần lăng
            if (Monster.IsEmperorTombBoss())
            {
                if (Monster.RoleName == "Tần Thủy Hoàng")
                {
                    Lucky = -2;

                }   
                else
                {
                    Lucky = -3;
                }    
            }    

            if (Monster.IsBossUnicon())
            {
                Lucky = -2;
            }    

            List<DropProfile> _TotalDropCount = ItemDropManager.GetDropItem(Monster, Lucky);
            if (_TotalDropCount == null)
            {
                return;
            }

            // Tổng số lượng Bạc Khóa sẽ nhận
            int BindMoney = 0;

            // Tổng số lượng Bạc sẽ nhận
            int Money = 0;

            List<ItemData> ListItemDataDrop = new List<ItemData>();

            if (_TotalDropCount.Count > 0)
            {
                foreach (DropProfile _Drop in _TotalDropCount)
                {
                    if (_Drop.BindMoney > 0)
                    {   // Tổng lượng bạc khóa
                        BindMoney += _Drop.BindMoney;
                    }
                    if (_Drop.Money > 0)
                    {   // Tổng lượng bạc nhận được
                        Money += _Drop.Money;
                    }

                    if (_Drop.ItemID > 0)
                    {
                        if (ItemManager._TotalGameItem.ContainsKey(_Drop.ItemID))
                        {
                            ItemData _TMPITEM = ItemManager._TotalGameItem[_Drop.ItemID];
                            ListItemDataDrop.Add(_TMPITEM);
                        }
                    }
                }
            }

            // Thực hiện add bạc khóa cho người chơi
            if (BindMoney > 0)
            {
                GameManager.ClientMgr.AddUserBoundMoney(Player, BindMoney, "MONSTERDROP | " + Monster.MonsterInfo.ExtensionID + "");
            }

            // Thực hiện add bạc cho người chơi
            if (Money > 0)
            {
                GameManager.ClientMgr.AddMoney(Player, Money, "MONSTERDROP | " + Monster.MonsterInfo.ExtensionID + "");
            }

            // Thực hiện cho DROP ở đây
            if (ListItemDataDrop.Count > 0)
            {
                CreateDropToMap(Player, ListItemDataDrop, Monster);
            }
        }

        public static void getDropHaiTac(Monster Monster, KPlayer Player)
        {
            // lấy ra danh sách huyền tinh và trang bị rớt
            string huyentinhIDStr = "";
            string ItemIDStr = "";
            string ItemHTStr = "";
            int rateHaiTac = 0;
            foreach (DropHaiTac dropHaiTac in KTGlobal.dropProfileHT)
            {
                if (dropHaiTac.MonsterLevel == Monster.m_Level)
                {
                    huyentinhIDStr = dropHaiTac.HTID;
                    ItemIDStr = dropHaiTac.ItemID;
                    ItemHTStr = dropHaiTac.ItemHTID;
                    rateHaiTac = dropHaiTac.Rate;
                }    
            }
            int rateDoThuong = 100 - rateHaiTac;
            List<ItemData> ListItemDataDrop = new List<ItemData>();

            //string[] huyentinhID = huyentinhIDStr.Split('#');
            string[] itemId = ItemIDStr.Split('#');
            string[] itemHT = ItemHTStr.Split('#');

            //List<int> htList = new List<int>();
            //
            //for (int i = 0; i < huyentinhID.Length; i++)
            //{
            //    string[] htID = huyentinhID[i].Split('_');
            //    for (int j = 0; j < int.Parse(htID[1]); j++)
            //    {
            //        htList.Add(int.Parse(htID[0]));
            //    }    
            //}    
            /// bỏ rơi ht từ boss khi nhận nv Hải tặc
            //for (int i = 0; i < 5; i++)
            //{
            //    int _Random1 = KTGlobal.GetRandomNumber(0, 99);
            //    if (htList[_Random1] > 0)
            //    {
            //        if (ItemManager._TotalGameItem.ContainsKey(htList[_Random1]))
            //        {
            //            ItemData _TMPITEM = ItemManager._TotalGameItem[htList[_Random1]];
            //            ListItemDataDrop.Add(_TMPITEM);
            //        }
            //    }
            //    int _Random2 = KTGlobal.GetRandomNumber(0, itemId.Length - 1);
            //
            //    if (int.Parse(itemId[_Random2]) > 0)
            //    {
            //        if (ItemManager._TotalGameItem.ContainsKey(int.Parse(itemId[_Random2])))
            //        {
            //            ItemData _TMPITEM = ItemManager._TotalGameItem[int.Parse(itemId[_Random2])];
            //            ListItemDataDrop.Add(_TMPITEM);
            //        }
            //    }
            //}    

            List<int> itemList = new List<int>();

            int numItemHaiTac = rateHaiTac * (itemHT.Length + itemId.Length) / 100;

            while (numItemHaiTac > 0)
            {
                int _Random1 = KTGlobal.GetRandomNumber(0, itemHT.Length - 1);
                itemList.Add(int.Parse(itemHT[_Random1]));
                numItemHaiTac--;
            }

            int numItemThuong = rateDoThuong * (itemHT.Length + itemId.Length) / 100;

            while (numItemThuong > 0)
            {
                int _Random1 = KTGlobal.GetRandomNumber(0, itemId.Length - 1);
                itemList.Add(int.Parse(itemId[_Random1]));
                numItemThuong--;
            }

            int _Random = KTGlobal.GetRandomNumber(0, itemList.Count - 1);

            if (itemList[_Random] > 0)
            {
                if (ItemManager._TotalGameItem.ContainsKey(itemList[_Random]))
                {
                    ItemData _TMPITEM = ItemManager._TotalGameItem[itemList[_Random]];
                    ListItemDataDrop.Add(_TMPITEM);
                }
            }

            if (ListItemDataDrop.Count > 0)
            {
                CreateDropToMap(Player, ListItemDataDrop, Monster);
            }
        }

        public static GoodsData CreateGoodDataFromItemData(ItemData ListItemDataDrop)
        {
            GoodsData _GoodData = new GoodsData();
            _GoodData.Id = -1;
            _GoodData.GoodsID = ListItemDataDrop.ItemID;
            _GoodData.GCount = 1;
            _GoodData.Props = ItemManager.GenerateProbs(ListItemDataDrop);
            _GoodData.Series = ItemManager.GetItemSeries(ListItemDataDrop.Series);
            _GoodData.Site = 0;
            _GoodData.Strong = 100;
            _GoodData.Using = -1;
            _GoodData.BagIndex = -1;
            _GoodData.Binding = 0;
            _GoodData.Forge_level = 0;

            if (_GoodData.GoodsID == 20831 || _GoodData.GoodsID == 20832)
                _GoodData.Binding = 1;

            if (_GoodData.GoodsID == 20875)
                _GoodData.Binding = ListItemDataDrop.BindType;

            return _GoodData;
        }

        public static void CreateDropMapFromSingerGooods(KPlayer client, GoodsData _Data)
        {
            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            GoodsPackItem goodsPackItem = new GoodsPackItem()
            {
                AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                OwnerRoleID = client.RoleID,
                OwnerRoleName = client.RoleName,
                GoodsPackType = 1,
                ProduceTicks = TimeUtil.NOW(),
                LockedRoleID = -1,
                ItemInPacker = _Data,
                TeamRoleIDs = null,
                MapCode = client.MapCode,
                CopyMapID = client.CopyMapID,
                // SET BELLONG = -1 để không check sở hữu
                BelongTo = -1,
                FallLevel = 0,
                TeamID = -1,
            };


            if ((_Data.GoodsID >= 190 && _Data.GoodsID <= 194) || (_Data.GoodsID >= 393 && _Data.GoodsID <= 396))
            {
                ItemData _Template = ItemManager.GetItemTemplate(_Data.GoodsID);
                if (_Template != null)
                {
                    LogManager.WriteLog(LogTypes.Item, "[" + client.strUserID + "][" + client.RoleID + "][" + client.RoleName + "][VỨT VẬT PHẨM][" + _Data.GoodsID + "][" + _Template.Name + "] ra ngoài đất");
                }

            }



            // Rơi ra ngay vị trí thằng vứt
            goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, client.MapCode, gridDict, new Point((int)(client.CurrentGrid.X), (int)(client.CurrentGrid.Y)), client.CopyMapID, client);

            lock (GameManager.GoodsPackMgr.GoodsPackDict)
            {
                GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
            }
            GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, goodsPackItem, 1);
        }

        /// <summary>
        /// Tạo Drop ở MAP
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ListItemDataDrop"></param>
        /// <param name="Monster"></param>
        public static void CreateDropToMap(KPlayer client, List<ItemData> ListItemDataDrop, Monster Monster)
        {
            if (IsKuaFuBalanceMonsterDrop(Monster))
            {
                return;
            }

            KPlayer TmpClient = client;

            bool IsDropFromBoss = false;
            
            if ((int)Monster.MonsterType == 3)
            {
                var FindTop = Monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).FirstOrDefault();

                if (FindTop != null)
                {
                    if (FindTop.TotalDamage > 0)
                    {
                        if (!FindTop.IsTeam)
                        {
                            KPlayer _client = GameManager.ClientMgr.FindClient(FindTop.ID);
                            if (_client != null)
                            {
                                client = _client;
                            }
                        }
                        else
                        {
                            KPlayer _client = KTTeamManager.GetTeamLeader(FindTop.ID);
                            if (_client != null)
                            {
                                client = _client;
                            }
                        }

                    }
                }

                IsDropFromBoss = true;
            }

            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < ListItemDataDrop.Count; i++)
            {
                GoodsData _GoodDrop = CreateGoodDataFromItemData(ListItemDataDrop[i]);

                if (!ComposeEventManager.Instance.IsMaterialDrop(_GoodDrop.GoodsID))
                {
                    /// nếu ko phai là đồ hải tặc và lệnh bài thì khóa
                    if (_GoodDrop.GoodsID < 15022 && (int)Monster.MonsterType != 3)
                    {
                        _GoodDrop.Binding = 1;
                    }

                    // nếu là huyền tinh khóa thì khóa
                    if (_GoodDrop.GoodsID >= 385 && _GoodDrop.GoodsID <= 396)
                    {
                        _GoodDrop.Binding = 1;
                    }
                    // nếu là huyền tinh ko khóa thì thôi
                    if (_GoodDrop.GoodsID >= 183 && _GoodDrop.GoodsID <= 194)
                    {
                        _GoodDrop.Binding = 0;
                    }
                }

                if (ComposeEventManager.Instance.IsMaterialDrop(_GoodDrop.GoodsID))
                {
                    _GoodDrop.Binding = 0;
                    if (_GoodDrop.GoodsID == 20875 && (client.CurrentMapCode < 1536 || client.CurrentMapCode > 1541))
                    {
                        _GoodDrop.Binding = 1;
                    }
                }
                if (Monster.MonsterType == MonsterAIType.Boss || ComposeEventManager.Instance.IsMaterialDrop(_GoodDrop.GoodsID) || _GoodDrop.GoodsID == 20831 || _GoodDrop.GoodsID == 20832)
                {
                    // không xử lý
                    // - nếu là boss thì bỏ tỷ lệ
                    // - nếu là vật phẩm nguyên liệu hoạt động ghép thì chắc chắn drop
                }
                else if (Monster.MonsterType == MonsterAIType.Pirate)
                {
                    LogManager.WriteLog(LogTypes.Item, $"{client.RoleName} has {Monster.RoleName} dropCount = {ListItemDataDrop.Count}");
                }    
                else
                {
                    int _Random = KTGlobal.GetRandomNumber(0, 100);
                    // nếu là túi phúc giảm 1/3 tỷ lệ ra
                    if (_GoodDrop.GoodsID == 337)
                    {
                        if (_Random <= 67)
                        {
                            continue;
                        }
                    }    // nếu là huyền tinh và lệnh bài TH giảm tỷ lệ ra map
                    else if ((_GoodDrop.GoodsID >= 183 && _GoodDrop.GoodsID <= 194) || (_GoodDrop.GoodsID >= 385 && _GoodDrop.GoodsID <= 396))
                    {
                        if (_Random <= 65)
                        {
                            continue;
                        }
                    }
                    else if (_GoodDrop.GoodsID >= 643 && _GoodDrop.GoodsID <= 645)
                    {
                        // Lệnh bài Tống Kim không khoóa
                        _GoodDrop.Binding = 0;

                        // điều chỉnh tỉ lệ rơi
                        var dropRate_LBDaiTuong = 40;
                        var dropRate_LBThuong = 70;
                        // if (Battel_SonJin_Manager.IsInBattle(client))
                        // {
                        var songJinLevel = Battel_SonJin_Manager.GetBattleLevel(client.m_Level);
                        var songJinMaxLevel =
                            Battel_SonJin_Manager.GetBattleLevel(ServerConfig.Instance.LimitLevel);
                        if (songJinLevel < songJinMaxLevel)
                        {
                            // giảm 80% tỉ lệ rơi lệnh bài tống kim nếu ko phải chiến trường tống kim level cao nhất
                            dropRate_LBThuong /= 5;
                            dropRate_LBDaiTuong /= 5;
                        }
                        // }
                        if (_GoodDrop.GoodsID == 643) // lệnh bài đại tướng
                        {
                            if (_Random <= (100 - dropRate_LBDaiTuong)) // 40% -> 8%
                            {
                                continue;
                            }
                        }
                        else // lệnh bài phó tướng + thống lĩnh
                        {
                            if (_Random <= (100 - dropRate_LBThuong)) // 70% -> 14%
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (_Random <= 90)
                        {
                            continue;
                        }
                    }
                }
                // neu la lsv thi ko khoa
                if (GameManager.IsKuaFuServer)
                {
                    _GoodDrop.Binding = 0;

                    // nếu là ht thì giảm tỷ lệ đi 80% nếu là quái thường

                    if (Monster.MonsterType != MonsterAIType.Boss)
                    {
                        if ((_GoodDrop.GoodsID >= 183 && _GoodDrop.GoodsID <= 194) || _GoodDrop.GoodsID >= 385 && _GoodDrop.GoodsID <= 396)
                        {
                            int _Random = KTGlobal.GetRandomNumber(0, 100);
                            if (_Random <= 80)
                            {
                                continue;
                            }
                        }
                    }    
                }
                
               
                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                    OwnerRoleID = client.RoleID,
                    OwnerRoleName = client.RoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    ItemInPacker = _GoodDrop,
                    TeamRoleIDs = null,
                    MapCode = TmpClient.MapCode,
                    CopyMapID = TmpClient.CopyMapID,
                    BelongTo = 1,
                    FallLevel = 0,
                    TeamID = client.TeamID,
                    DropFromBoss = IsDropFromBoss,

                };

                goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, Monster.CurrentMapCode, gridDict, new Point((int)(Monster.CurrentGrid.X), (int)(Monster.CurrentGrid.Y)), TmpClient.CopyMapID, TmpClient);
                goodsPackItemList.Add(goodsPackItem);

                lock (GameManager.GoodsPackMgr.GoodsPackDict)
                {
                    GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }

                
            }

            // if (Monster.MonsterType == MonsterAIType.Boss)
            {
                foreach (var t in goodsPackItemList)
                {
                    LogManager.WriteLog(LogTypes.Diagnostic, 
                        $"Monster {Monster.RoleName}[{Monster.RoleID}] dropped {t.ItemInPacker.ItemName} for {t.OwnerRoleName}");
                }
            }
            foreach (var goodsPackItem in goodsPackItemList)
            {
                GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, goodsPackItem, 1);
            }
        }

        public static void CreateDroptoMap(List<GoodsPackItem> goodsPackItemList, KPlayer client, Monster monster)
        {
            if (IsKuaFuBalanceMonsterDrop(monster))
            {
                return;
            }

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < goodsPackItemList.Count; i++)
            {
                goodsPackItemList[i].FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, monster.CurrentMapCode, gridDict, new Point((int)(monster.CurrentGrid.X), (int)(monster.CurrentGrid.Y)), client.CopyMapID, client);

                GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, goodsPackItemList[i], 1);
            }    
        }

        /// <summary>
        /// Tạo Drop ở MAP
        /// </summary>
        /// <param name="ListItemDataDrop"></param>
        /// <param name="Monster"></param>
        public static void CreateDropToMap(List<ItemData> ListItemDataDrop, Monster monster, KPlayer owner = null)
        {
            if (IsKuaFuBalanceMonsterDrop(monster))
            {
                return;
            }

            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < ListItemDataDrop.Count; i++)
            {
                GoodsData _GoodDrop = CreateGoodDataFromItemData(ListItemDataDrop[i]);
                _GoodDrop.Binding = ListItemDataDrop[i].BindType;

                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                    OwnerRoleID = owner == null ? -1 : owner.RoleID,
                    OwnerRoleName = owner == null ? "" : owner.RoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    ItemInPacker = _GoodDrop,
                    TeamRoleIDs = null,
                    MapCode = monster.CurrentMapCode,
                    CopyMapID = monster.CurrentCopyMapID,
                    BelongTo = 1,
                    FallLevel = 0,
                    TeamID = owner == null ? -1 : owner.TeamID,
                };

                goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, monster.CurrentMapCode, gridDict, new Point((int)(monster.CurrentGrid.X), (int)(monster.CurrentGrid.Y)), monster.CurrentCopyMapID, null);
                goodsPackItemList.Add(goodsPackItem);

                lock (GameManager.GoodsPackMgr.GoodsPackDict)
                {
                    GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }

                for (int j = 0; j < goodsPackItemList.Count; j++)
                {
                    GameManager.GoodsPackMgr.ProcessGoodsPackItem(null, null, goodsPackItemList[i], 1);
                }
            }
        }

        /// <summary>
        /// Tạo Drop ở MAP
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ListItemDataDrop"></param>
        /// <param name="Monster"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public static void CreateDropToMap(KPlayer client, List<ItemData> ListItemDataDrop, int posX, int posY)
        {
            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < ListItemDataDrop.Count; i++)
            {
                GoodsData _GoodDrop = CreateGoodDataFromItemData(ListItemDataDrop[i]);
                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                    OwnerRoleID = client.RoleID,
                    OwnerRoleName = client.RoleName,
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    ItemInPacker = _GoodDrop,
                    TeamRoleIDs = null,
                    MapCode = client.MapCode,
                    CopyMapID = client.CopyMapID,
                    BelongTo = 1,
                    FallLevel = 0,
                    TeamID = client.TeamID,
                };

                goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, client.MapCode, gridDict, new Point(posX, posY), client.CopyMapID, client);
                goodsPackItemList.Add(goodsPackItem);

                lock (GameManager.GoodsPackMgr.GoodsPackDict)
                {
                    GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }

                for (int j = 0; j < goodsPackItemList.Count; j++)
                {
                    GameManager.GoodsPackMgr.ProcessGoodsPackItem(client, client, goodsPackItemList[i], 1);
                }
            }
        }

        /// <summary>
        /// Tạo Drop ở MAP
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapCode"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="items"></param>
        public static void CreateDropToMap(int mapCode, int copyMapCode, int posX, int posY, List<ItemData> items)
        {
            List<GoodsPackItem> goodsPackItemList = new List<GoodsPackItem>();

            Dictionary<string, bool> gridDict = new Dictionary<string, bool>();

            for (int i = 0; i < items.Count; i++)
            {
                GoodsData _GoodDrop = CreateGoodDataFromItemData(items[i]);
                GoodsPackItem goodsPackItem = new GoodsPackItem()
                {
                    AutoID = GameManager.GoodsPackMgr.GetNextAutoID(),
                    OwnerRoleID = -1,
                    OwnerRoleName = "",
                    GoodsPackType = 0,
                    ProduceTicks = TimeUtil.NOW(),
                    LockedRoleID = -1,
                    ItemInPacker = _GoodDrop,
                    TeamRoleIDs = null,
                    MapCode = mapCode,
                    CopyMapID = copyMapCode,
                    BelongTo = 1,
                    FallLevel = 0,
                    TeamID = -1,
                };

                goodsPackItem.FallPoint = GameManager.GoodsPackMgr.GetFallGoodsPosition(ObjectTypes.OT_GOODSPACK, mapCode, gridDict, new Point(posX, posY), copyMapCode, null);
                goodsPackItemList.Add(goodsPackItem);

                lock (GameManager.GoodsPackMgr.GoodsPackDict)
                {
                    GameManager.GoodsPackMgr.GoodsPackDict[goodsPackItem.AutoID] = goodsPackItem;
                }

                for (int j = 0; j < goodsPackItemList.Count; j++)
                {
                    GameManager.GoodsPackMgr.ProcessGoodsPackItem(null, null, goodsPackItemList[i], 1);
                }
            }
        }

        public static int GetMaxCount(int TYPEMON, int LucKey)
        {
            if (TYPEMON == 0)
            {
                return 1;
            }
            else if (TYPEMON == 1)
            {
                int Min = 3;

                int _Random = KTGlobal.GetRandomNumber(0, 100);

                //Tăng tỉ lệ rơi đồ nếu LUCKY > 0
                if (LucKey > 0)
                {
                    _Random = _Random + (_Random * LucKey / 100);
                }
                if (_Random < (100 - KTGlobal.DropRate))
                {
                    return Min;
                }

                return 5;
            }
            else if (TYPEMON == 2)
            {
                int Min = 4;

                int _Random = KTGlobal.GetRandomNumber(0, 100);

                //Tăng tỉ lệ rơi đồ nếu LUCKY > 0
                if (LucKey > 0)
                {
                    _Random = _Random + (_Random * LucKey / 100);
                }
                if (_Random < (100 - KTGlobal.DropRate))
                {
                    return Min;
                }

                return 6;
            }
            else if (TYPEMON == 3)
            {
                // nếu là boss tiêu dao cốc thì giảm số lượng rơi đồ
                if (LucKey == -1)
                    return 4;

                return StaticRandom.AccumulatedProbabilitySelection(
                    Enumerable.Range(8, 5).ToList(),
                    new List<int> { 256, 32, 16, 8, 4 },
                    LucKey
                );
            }
            else if (TYPEMON == 4)
            {
                
                int Min = 3;

                int _Random = KTGlobal.GetRandomNumber(0, 100);

                //Tăng tỉ lệ rơi đồ nếu LUCKY > 0
                if (LucKey > 0)
                {
                    _Random = _Random + (_Random * LucKey / 100);
                }
                if (_Random < (100 - KTGlobal.DropRate))
                {
                    return Min;
                }

                return Min;
            }

            return 1;
        }

        public static List<DropProfile> GetDropItem(Monster Monster, int LucKey = 0)
        {
            List<DropProfile> _DropSelect = new List<DropProfile>();


            MapDropInfo MonsterFind = TotalMapDrop.Where(x => x.MonsterID == Monster.MonsterInfo.ExtensionID).FirstOrDefault();

            if (MonsterFind == null)
            {
                return null;
            }

            int ItemCount = ItemDropManager.GetMaxCount((int)Monster.MonsterType, LucKey);

            ///Nếu là tần thủy hoàng thì cho drop 5 cái
            if (LucKey == -2 )
            {
                // drop cho boss TTH: 5 rương TTH + 10 rương dạ minh châu (20)
                // TODO: đẩy cấu hình ra file config
                ItemCount = 5;
                if (Monster.GetMonsterData().ExtensionID == 7384)
                {
                    var dropTTH = MonsterFind.TotalFile.FirstOrDefault();
                    var itemIDDrop = 20003;
                    var numberDrop = 5;
                    if (dropTTH != null)
                    {
                        itemIDDrop = dropTTH.ItemID;
                        numberDrop = MonsterFind.DropCount;
                    }                   
                    
                    for (var i = 0; i < numberDrop; ++i)
                    {
                        _DropSelect.Add(new DropProfile
                        {
                            ItemID = itemIDDrop
                        });
                    }

                    // chỉ drop thêm rương DMC ở S1
                    // if (GameManager.ServerLineID == 1)
                    // {
                    //     for (var i = 0; i < 10; ++i)
                    //     {
                    //         _DropSelect.Add(new DropProfile
                    //         {
                    //             ItemID = 20307
                    //         });
                    //     }
                    // }

                    return _DropSelect;
                }    
                if (Monster.GetMonsterData().ExtensionID == 22648)
                {
                    ItemCount = 3;
                }
            }    
            /// nếu là thủ vệ, tinh anh cho rơi 1 món
            if (LucKey == -3)
            {
                ItemCount = 1;
            }    

            if (MonsterFind != null)
            {
                //Sắp xếp theo tỉ lệ giảm dàn của RATE
                List<DropProfile> TotalDrop = MonsterFind.TotalFile.OrderByDescending(x => x.Rate).ToList();

                //Lấy ra tổng RATE
                int SumDrop = TotalDrop.Sum(x => x.Rate);

                // Lần ramdom này xác định đồ có rơi hay không rơi
                int _Random = KTGlobal.GetRandomNumber(0, 100);

                //Tăng tỉ lệ rơi đồ nếu LUCKY > 0
                if (LucKey > 0)
                {
                    _Random = _Random + (_Random * LucKey / 100);
                }


                /// Nếu có tỷ lệ trong File XML
                if (ItemDropManager.MonsterDrops.TryGetValue(Monster.MonsterInfo.ExtensionID, out MonsterDropRate dropRate))
                {
                    /// Tỷ lệ rơi cố định
                    int nRate = KTGlobal.GetRandomNumber(0, 100);
                    /// Nếu nhân phẩm đéo đủ
                    if (nRate > dropRate.Rate)
                    {
                        /// Toác
                        return null;
                    }
                }
                /// Loại khác
                else
                {
                    // Nếu là quái thường xác định xem lần giết này có rơi vật phẩm hay không
                    if ((int)Monster.MonsterType == 0 || (int)Monster.MonsterType == 5 || (int)Monster.MonsterType == 6)
                    {
                        if ((int)Monster.MonsterType == 5 || (int)Monster.MonsterType == 6)
                        {
                            if (_Random < 50)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            // Nếu random mà nhỏ hơn tỉ lệ rơi glolal thì ko rơi gì

                            if (_Random < (100 - KTGlobal.DropRate))
                            {
                                return null;
                            }
                        }

                    }
                }


                {
                    // Thực hiện rơi đủ các vật phẩm
                    for (int j = 0; j < ItemCount; j++)
                    {
                        // Nếu mà đã rơi rồi thì thực hiện random lại để lấy ra vật phẩm theo RATE
                        int RandomSelect = KTGlobal.GetRandomNumber(0, SumDrop);

                        //SET CHỈ SỐ BAN ĐẦU
                        int Count = 0;

                        foreach (var TmpDrop in TotalDrop)
                        {
                            // MỖI LẦN + THÊM 1 LƯỢNG MAY MẮN NẾU ĐỎ THÌ SẼ NHẬN ĐƯỢC ĐỒ PHẨM CHẤT CAO
                            int RateL = TmpDrop.Rate + LucKey;

                            //MỖI LẦN TĂNG THÊM TÝ
                            Count += RateL;

                            //NẾU ĐÃ CHẠM NGƯỠNG RƠI THÌ ADD ĐỒ RƠI VÀO
                            if (Count > RandomSelect)
                            {
                                _DropSelect.Add(TmpDrop);
                                break;
                            }
                        }
                    }
                }
            }

            return _DropSelect;
        }
    }
}
