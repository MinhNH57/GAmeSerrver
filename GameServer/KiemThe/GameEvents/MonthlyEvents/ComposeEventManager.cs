using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.BattlePass;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using GameServer.KiemThe.GameEvents.Core;
using Newtonsoft.Json.Linq;
using Server.Tools;
using Recipe = GameServer.Data.Recipe;
using Server.Data;

namespace GameServer.KiemThe.GameEvents.MonthlyEvents
{
    [XmlRoot(ElementName = "ComposeData")]
    public class EventComposeConfig
    {
        [XmlAttribute(AttributeName = "MainItem")]
        public int MainItemID;
        
        [XmlAttribute(AttributeName = "BonusItem")]
        public int BonusItemID;

        [XmlAttribute(AttributeName = "BonusStep")]
        public int BonusStep = 20;

        [XmlAttribute(AttributeName = "BonusMaxDaily")]
        public int BonusMaxDaily = 5;

        [XmlAttribute(AttributeName = "RecordSeed")]
        public int RecordSeed;

        [XmlElement(ElementName = "Milestones")]
        public MilestoneConfig MilestoneConfig;

        [XmlElement(ElementName = "Compose")] public List<ComposeConfig> ComposeConfigs;

        [XmlElement(ElementName = "MantleRequirement")]
        public List<MantleRequirement> MantleRequirements;
    }

    public class MantleRequirement
    {
        [XmlAttribute(AttributeName = "Level")]
        public int ID;
        [XmlAttribute(AttributeName = "ServerMin")]
        public int ServerMin;
        [XmlAttribute(AttributeName = "ServerMax")]
        public int ServerMax;
    }

    public class ComposeConfig
    {
        [XmlAttribute(AttributeName = "Output")]
        public int OutputID;

        [XmlAttribute(AttributeName = "OutputNumber")]
        public int OutputNumber;

        [XmlAttribute(AttributeName = "LimitType")]
        public Entities.RoleRecordType LimitType;

        [XmlAttribute(AttributeName = "LimitValue")]
        public int LimitValue;

        [XmlAttribute(AttributeName = "Group")]
        public int Group;
        [XmlElement(ElementName = "Material")] public List<ItemXML> Materials;
        [XmlElement(ElementName = "Fee")] public List<ItemXML> Fees;
    }

    public class MilestoneConfig
    {
        [XmlAttribute(AttributeName = "GoodsID")]
        public int GoodsID;

        [XmlElement(ElementName = "Milestone")]
        public List<Milestone> Milestones;
    }

    public class Milestone
    {
        [XmlAttribute(AttributeName = "Count")]
        public int Count;

        [XmlElement(ElementName = "Item")] public List<ItemXML> Items;
    }

    public class ItemXML
    {
        [XmlAttribute(AttributeName = "ID")] public int ID;

        [XmlAttribute(AttributeName = "Number")]
        public int Number = 1;
    }

    public class ComposeEventManager : BaseGameEvent<EventComposeConfig>
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ComposeEventManager Instance { get; private set; } = new ComposeEventManager();

        private const string MILESTONE_CONFIG = "Config/KT_Interactive/EventCompose.xml";
        private const string CONFIG_PATH = "Config/KT_GameEvents/MonthlyCompose.json";
        private const string BOX_CONFIG_PATH = "Config/KT_Item/Events/Items.json";
        private string TopRankingStr = "";

        private Dictionary<int, ComposeConfig> ComposeDict = new Dictionary<int, ComposeConfig>();

        private List<int> BoxIDs;
        private List<int> Materials;
        private DateTime StartDate;
        private DateTime EndDate;
        private int TokenItemID;
        private int TokenItemPrice;

        private int TokenBindItemID;
        private int TokenBindItemPrice;
        private int TokenBindItemLimitPerDay;

        private int FinalMantleRequirement = 0;

        /// <summary>
        /// Đường dẫn file cấu hình XML
        /// </summary>
        protected override string ConfigFilePath => MILESTONE_CONFIG;

        /// <summary>
        /// Key sự kiện trong EventTimeManager
        /// </summary>
        public override string EventKey => "EventCompose";

        /// <summary>
        /// Hàm khởi tạo, nạp XML và các config JSON bổ sung
        /// </summary>
        public override void Init()
        {
            base.Init(); // Gọi BaseGameEvent.Init() để nạp và parse XML mặc định

            if (this.Config == null) return;

            this.ComposeDict.Clear();
            this.BoxIDs = new List<int>();

            // Load RandomBoxes config
            var path = Global.GameResPath(BOX_CONFIG_PATH);
            if (File.Exists(path))
            {
                var str = File.ReadAllText(path);
                var jObj = JObject.Parse(str);
                foreach (var box in jObj["RandomBoxes"].Value<JArray>())
                {
                    this.BoxIDs.Add((box["ItemID"] ?? -1).Value<int>());
                }
            }

            // Sync with EventTimeManager
            var evt = EventTimeManager.GetEvent(this.EventKey);
            if (evt == null)
            {
                this.StartDate = DateTime.MinValue;
                this.EndDate = DateTime.MinValue;
                Console.WriteLine($"[ComposeEventManager] Event '{this.EventKey}' not found in EventTimeConfig.xml.");
            }
            else
            {
                this.StartDate = evt.StartDate;
                this.EndDate = evt.EndDate;
            }

            // Load MonthlyCompose JSON config
            path = Global.GameResPath(CONFIG_PATH);
            if (File.Exists(path))
            {
                var str = File.ReadAllText(path);
                var jObj = JObject.Parse(str);

                this.TokenItemID = jObj["TokenItem"].Value<int>();
                this.TokenItemPrice = jObj["TokenItemPrice"].Value<int>();
                this.TokenBindItemID = jObj["TokenBindItem"].Value<int>();
                this.TokenBindItemPrice = jObj["TokenBindItemPrice"].Value<int>();
                this.TokenBindItemLimitPerDay = jObj["TokenBindItemLimitPerDay"].Value<int>();

                this.Materials = new List<int>();
                foreach (var item in jObj["MaterialRate"].Value<JObject>())
                {
                    this.Materials.Add(int.Parse(item.Key));
                }
            }

            // Process Recipes
            var composeID = 100000;
            foreach (var compose in this.Config.ComposeConfigs)
            {
                var recipe = new Recipe
                {
                    compose_id = composeID,
                    compose_name = $"Event Ghép {composeID}",
                    source = null,
                    materials = compose.Materials.Select(x => new GameServer.Data.Material
                    {
                        id = x.ID,
                        number = x.Number
                    }).ToList(),
                    rate_up_items = null,
                    insure_items = null,
                    output = new Data.OutputItem
                    {
                        id = compose.OutputID,
                        number = compose.OutputNumber,
                    },
                    fees = compose.Fees.Select(x => new Data.Fee
                    {
                        type = x.ID,
                        number = x.Number
                    }).ToList(),
                    allow_multiple = false,
                    default_rate = 100,
                    actual_rate = 100,
                    enable_rate_up = false,
                    enable_source = false,
                    output_based_on_input = false
                };
                this.ComposeDict[composeID] = compose;
                KT_TCPHandler.ComposeDictionary.Remove(composeID);
                KT_TCPHandler.ComposeDictionary.Add(composeID, recipe);
                composeID += 1;
            }

            // Process Mantle Requirements
            foreach (var mantle in this.Config.MantleRequirements)
            {
                if (mantle.ServerMin <= GameManager.ServerId && mantle.ServerMax >= GameManager.ServerId)
                {
                    this.FinalMantleRequirement = mantle.ID;
                    break;
                }
            }

            // Schedule top ranking update
            ScheduleExecutor2.Instance.scheduleCancle("TopEventCompose");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopEventCompose", this.ReadEventData), 1000, 60000);
        }

        public void ReadEventData()
        {
            this.TopRankingStr = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_GROUP_0, true);
        }

        public string GetTopRankingStr()
        {
            return this.TopRankingStr;
        }

        public void AddTokenItem()
        {
            if (this.TokenItemID == 0)
                return;

            var now = DateTime.Now;
            if (now >= this.StartDate && now <= this.EndDate)
            {
                var newShopITem = new ShopItem
                {
                    ID = 9999,
                    ItemID = this.TokenItemID,
                    GoodsPrice = this.TokenItemPrice
                };
                ShopManager.ForceAddShopItem(newShopITem);

                var tokenSale = ShopManager.TokenShop.Token.FirstOrDefault(x => x.ShopID == 203);
                tokenSale?.ShopItem.Remove(9999);
                tokenSale?.ShopItem.Insert(0, 9999);
                tokenSale?.Items.Insert(0, newShopITem);

                newShopITem = new ShopItem
                {
                    ID = 9990,
                    ItemID = this.TokenBindItemID,
                    GoodsPrice = this.TokenBindItemPrice,
                    LimitType = LimitType.BuyCountPerDay,
                    LimitValue = this.TokenBindItemLimitPerDay
                };
                ShopManager.ForceAddShopItem(newShopITem);

                var bindTokenSale = ShopManager.TokenShop.BoundToken.FirstOrDefault(x => x.ShopID == 207);
                bindTokenSale?.ShopItem.Remove(9990);
                bindTokenSale?.ShopItem.Insert(0, 9990);
                bindTokenSale?.Items.Insert(0, newShopITem);
            }
        }

        public bool IsMaterialDrop(int itemID)
        {
            if (this.Materials == null)
                return false;
            return this.Materials.Contains(itemID) || itemID == this.TokenBindItemID;
        }

        public bool IsRecipeInEvent(int recipeID)
        {
            return this.ComposeDict.ContainsKey(recipeID);
        }

        private int GetComposedNumberForRecipe(KPlayer player, int recipeID)
        {
            if (!this.ComposeDict.TryGetValue(recipeID, out var config))
                return -1;

            if (config.OutputID == this.Config.MainItemID)
                return Global.GetRoleParamsInt32FromDB(player, RoleParamName.MonthlyBox1);

            if (config.LimitType == RoleRecordType.NONE)
                return -1;

            var composed = player.GetValueOfRecord(this.Config.RecordSeed + recipeID, config.LimitType);
            if (composed < 0)
                composed = 0;

            return composed;
        }

        public bool CanPlayerParticipate(KPlayer player, int composeID, out string message)
        {
            message = string.Empty;
            if (player == null)
            {
                message = "Người chơi không hợp lệ.";
                return false;
            }

            if (!this.IsActive())
            {
                message = "Hoạt động không khả dụng trong thời gian này.";
                return false;
            }

            if (this.IsGroupCompose(composeID, 1))
            {
                if (!BattlePassManager.IsElite(player))
                {
                    message = "Cần mua gói Kiếm Vương Lệnh Cao Cấp để đổi quà";
                    return false;
                }
            }

            if (Global.GetMantleLevel(player) < this.FinalMantleRequirement)
            {
                var mantleName = KTLuaLib_System.GetMantleNameFromLevel(this.FinalMantleRequirement);
                message = $"Cần trang bị phi phong cấp độ {mantleName} để tham gia hoạt động này.";
                return false;
            }

            return true;
        }

        public bool IsGroupCompose(int composeID, int group)
        {
            return this.ComposeDict.TryGetValue(composeID, out var config) && config.Group == group;
        }

        public bool IsRecipeLimitReached(KPlayer player, int recipeID)
        {
            if (!this.ComposeDict.TryGetValue(recipeID, out var config))
                return true;

            if (config.LimitType == RoleRecordType.NONE)
                return false;

            return this.GetComposedNumberForRecipe(player, recipeID) >= config.LimitValue;
        }

        public void UpdateComposeRecord(KPlayer player, int recipeID, int value)
        {
            if (!this.ComposeDict.TryGetValue(recipeID, out var config))
                return;

            var composed = this.GetComposedNumberForRecipe(player, recipeID);

            if (config.OutputID == this.Config.MainItemID)
            {
                int mainItemComposedDaily = player.GetValueOfDailyRecore((int)RecoreDef.EventCompose_MainItemComposedDaily) == -1
                ? 0
                : player.GetValueOfDailyRecore((int)RecoreDef.EventCompose_MainItemComposedDaily);

                int step = this.Config.BonusStep;
                int maxDaily = this.Config.BonusMaxDaily;

                int milestonesCompleted = mainItemComposedDaily / step;
                if (milestonesCompleted < maxDaily)
                {
                    int newMainItemComposedDaily = mainItemComposedDaily + value;
                    int newMilestonesCompleted = newMainItemComposedDaily / step;

                    int milestonesGained = Math.Min(maxDaily, newMilestonesCompleted) - milestonesCompleted;

                    if (milestonesGained > 0)
                    {
                        var goodsData = new List<GoodsData>
                        {
                            new GoodsData
                            {
                                GoodsID = this.Config.BonusItemID,
                                GCount = milestonesGained,
                                Binding = 1
                            }
                        };
                        KTGlobal.SendMail(goodsData, player.RoleID, null, "Quà Mốc Ghép Sự Kiện", "Hệ thống gửi phần thưởng mốc ghép túi quà sự kiện. Vui lòng nhận đính kèm.", 0, 0);
                        PlayerManager.ShowNotification(player, "Nhận được phần thưởng mốc ghép, vui lòng kiểm tra thư!");
                    }
                    player.SetValueOfDailyRecore((int)RecoreDef.EventCompose_MainItemComposedDaily, newMainItemComposedDaily);
                }

                Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MonthlyBox1, composed + value, true);
                return;
            }

            if (config.LimitType == RoleRecordType.NONE)
                return;

            player.SetValueOfRecord(this.Config.RecordSeed + recipeID, composed + value, config.LimitType);
        }

        public void PlayerReceiveAccumulateRewards(KPlayer player)
        {
            var totalUsed = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MonthlyBox1);
            var accumulateReceived = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MonthlyBox1_Accumulate);

            var itemsToReceive = new List<ItemXML>();

            foreach (var milestone in this.Config.MilestoneConfig.Milestones)
            {
                if (totalUsed < milestone.Count)
                    continue;
                if (accumulateReceived >= milestone.Count)
                    continue;
                itemsToReceive.AddRange(milestone.Items);
            }

            var spaceNeeded = 0;
            foreach (var item in itemsToReceive)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, item.Number);
            }

            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return;
            }

            var haveReward = false;
            foreach (var item in itemsToReceive)
            {
                ItemManager.CreateSimpleItem(player, item.ID, item.Number, 0, 1);
                haveReward = true;
            }

            if (!haveReward)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không có phần thưởng tích luỹ có thể nhận!");
                return;
            }

            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MonthlyBox1_Accumulate, totalUsed, true);
        }

        public Dictionary<int, int> GetComposeStatusOfPlayer(KPlayer player)
        {
            var status = new Dictionary<int, int>();
            foreach (var k in this.ComposeDict.Keys)
            {
                var composed = this.GetComposedNumberForRecipe(player, k);
                status[k] = composed;
            }

            status[-1] = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MonthlyBox1_Accumulate, 0);

            return status;
        }

        public int GetEventComposeItemID()
        {
            return this.Config.MainItemID;
        }
    }
}