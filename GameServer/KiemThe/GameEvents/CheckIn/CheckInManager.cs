using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using GameServer.Server;
using GameServer.KiemThe.GameEvents.Core;
using Server.Protocol;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.CheckIn
{
    [XmlRoot(ElementName = "Item")]
    public class CheckInItem
    {
        [XmlAttribute(AttributeName = "ID")] 
        public int ID;

        [XmlAttribute(AttributeName = "Count")]
        public int Count;

        [XmlAttribute(AttributeName = "TimeLimit")]
        public int TimeLimit = -1;
        
        [XmlAttribute(AttributeName = "EnhancedLevel")]
        public int EnhancedLevel = -1;
    }

    [XmlRoot(ElementName = "CheckInPackage")]
    public class CheckInPackage
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        [XmlAttribute(AttributeName = "Limit")]
        public int DailyLimit;

        [XmlAttribute(AttributeName = "Price")]
        public int Price;

        [XmlAttribute(AttributeName = "Money")]
        public int Money;
        
        [XmlAttribute(AttributeName = "Record")]
        public string Record;
        
        [XmlAttribute(AttributeName = "RecordAccumulate")]
        public string RecordAccumulate;

        [XmlAttribute(AttributeName = "ID")] public int ID;

        [XmlElement(ElementName = "Item")] public List<CheckInItem> Items;
        
        [XmlElement(ElementName = "AccumulatedReward")] public List<CheckInItem> AccumulatedReward;
    }

    [XmlRoot(ElementName = "CheckInEvent")]
    public class CheckInConfig
    {
        [XmlElement(ElementName = "CheckInPackage")]
        public List<CheckInPackage> CheckInPackages;
        
        [XmlAttribute(AttributeName = "TimeKey")]
        public string TimeKey;
        
        [XmlIgnore]
        public Dictionary<int, CheckInPackage> Packages;
    }

    public class CheckInManager : BaseGameEvent<CheckInConfig>
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static CheckInManager Instance { get; private set; } = new CheckInManager();

        private const string CONFIG_PATH = "Config/KT_Interactive/CheckIn.xml";
        private const string CONFIG_PATH_OPEN = "Config/KT_Interactive/CheckIn_OpenServer.xml";

        private CheckInConfig ConfigData;

        /// <summary>
        /// Đường dẫn file cấu hình XML
        /// </summary>
        protected override string ConfigFilePath
        {
            get
            {
                if (GameManager.ServerId == 999 || GameManager.ServerId == 997)
                {
                    return CONFIG_PATH;
                }
                else if (EventTimeManager.IsNewServer())
                {
                    return CONFIG_PATH_OPEN;
                }
                return CONFIG_PATH;
            }
        }

        /// <summary>
        /// Key sự kiện trong EventTimeManager
        /// </summary>
        public override string EventKey => ConfigData?.TimeKey ?? "CheckIn";

        /// <summary>
        /// Hàm parse cấu hình XML
        /// </summary>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        protected override CheckInConfig ParseConfig(string xmlContent)
        {
            var config = base.ParseConfig(xmlContent);
            if (config != null)
            {
                config.Packages = new Dictionary<int, CheckInPackage>();
                foreach (var package in config.CheckInPackages)
                {
                    config.Packages.Add(package.ID, package);
                }
                this.ConfigData = config;
            }
            return config;
        }

        private Dictionary<int, List<int>> GetCheckInStatus(KPlayer player)
        {
            var statuses = new Dictionary<int, List<int>>();
            if (this.ConfigData?.Packages == null) return statuses;

            foreach (var package in this.ConfigData.Packages.Values)
            {
                var buyToday = player.GetValueOfDailyRecore(package.ID);
                if (buyToday < 0)
                    buyToday = 0;

                var buyTotal = Global.GetRoleParamsInt32FromDB(player, package.Record);
                if (buyTotal < 0)
                    buyTotal = 0;
                
                var accumulateReceived = Global.GetRoleParamsInt32FromDB(player, package.RecordAccumulate);
                statuses[package.ID] = new List<int>{buyToday, buyTotal, accumulateReceived};
            }

            return statuses;
        }

        public TCPOutPacket ResponseCheckInStatus(KPlayer player, TCPOutPacketPool pool)
        {
            var statuses = GetCheckInStatus(player);

            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)TCPGameServerCmds.CMD_KT_UI_INTERACT);
            return outPacket;
        }

        public TCPOutPacket PlayerBuy(KPlayer player, int packageID, TCPOutPacketPool pool)
        {
            if (!this.IsActive())
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Không phải thời gian diễn ra hoạt động");
                return null;
            }
            var statuses = GetCheckInStatus(player);
            if (!statuses.TryGetValue(packageID, out var buyStatus))
            {
                return null;
            }

            if (buyStatus.Count != 3)
                return null;

            var buyCount = buyStatus[0];
            var buyTotal = buyStatus[1];

            if (!this.ConfigData.Packages.TryGetValue(packageID, out var package))
            {
                return null;
            }

            if (buyCount >= package.DailyLimit)
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Đã hết số lượt check-in trong ngày.");
                return null;
            }

            if (!KTGlobal.IsHaveMoney(player, package.Price, (MoneyType)package.Money))
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Bằng hữu không có đủ tiền.");
                return null;
            }

            var spaceNeeded = 0;
            foreach (var item in package.Items)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, item.Count);
            }

            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Check-In",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return null;
            }

            var subResult = KTGlobal.SubMoney(player, package.Price, (MoneyType)package.Money, "CHECK-IN");
            if (!subResult.IsOK)
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Đã có lỗi xảy ra. Vui lòng liên hệ quản trị viên.");
                return null;
            }

            player.SetValueOfDailyRecore(packageID, buyCount + 1);
            Global.SaveRoleParamsInt32ValueToDB(player, package.Record, buyTotal + 1, true);

            foreach (var item in package.Items)
            {
                var endTime = Global.ConstGoodsEndTime;
                if (item.TimeLimit > 0) 
                {
                    endTime = DateTime.Now.AddMinutes(item.TimeLimit).ToString("yyyy-MM-dd HH:mm:ss");
                }
                ItemManager.CreateSimpleItem(player, item.ID, item.Count, 0, 1, 0, endTime);
            }

            return ResponseCheckInStatus(player, pool);
        }
        
        public TCPOutPacket PlayerReceiveAccumulate(KPlayer player, TCPOutPacketPool pool)
        {
            if (!this.IsActive())
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Không phải thời gian diễn ra hoạt động");
                return null;
            }
            var statuses = GetCheckInStatus(player);
            
            var itemsToReceive = new List<CheckInItem>();

            foreach (var package in this.ConfigData.CheckInPackages)
            {
                var packageID = package.ID;
                if (!statuses.TryGetValue(packageID, out var buyStatus))
                {
                    continue;
                }
                
                if (buyStatus.Count != 3)
                {
                    return null;
                }

                var buyTotal = buyStatus[1];
                var accumulateReceive = buyStatus[2];

                foreach (var reward in package.AccumulatedReward)
                {
                    if (buyTotal < reward.Count)
                        continue;
                    
                    if (accumulateReceive >= reward.Count)
                        continue;
                    
                    itemsToReceive.Add(reward);
                }
            }

            var spaceNeeded = 0;

            foreach (var item in itemsToReceive)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, 1);
            }

            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Check-In",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return null;
            }

            var haveReward = false;

            foreach (var item in itemsToReceive)
            {
                var endTime = Global.ConstGoodsEndTime;
                if (item.TimeLimit > 0) 
                {
                    endTime = DateTime.Now.AddMinutes(item.TimeLimit).ToString("yyyy-MM-dd HH:mm:ss");
                }

                var enhancedLevel = 0;
                if (item.EnhancedLevel > 0)
                    enhancedLevel = item.EnhancedLevel;
                ItemManager.CreateSimpleItem(player, item.ID, 1, 0, 1, enhancedLevel, endTime);
                haveReward = true;
            }
            
            if (!haveReward)
            {
                PlayerManager.ShowMessageBox(player, "Check-In", "Không có phần thưởng tích luỹ có thể nhận!");
                return ResponseCheckInStatus(player, pool);
            }

            foreach (var package in this.ConfigData.CheckInPackages)
            {
                var packageID = package.ID;
                if (!statuses.TryGetValue(packageID, out var buyStatus))
                {
                    continue;
                }
                
                if (buyStatus.Count != 3)
                {
                    return null;
                }

                var buyTotal = buyStatus[1];
                var accumulateReceive = buyStatus[2];
                
                Global.SaveRoleParamsInt32ValueToDB(player, package.RecordAccumulate, buyTotal, true);
            }
            
            return ResponseCheckInStatus(player, pool);
        }
    }
}