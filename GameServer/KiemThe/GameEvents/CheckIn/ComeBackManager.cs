using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.CheckIn
{
    public static class ComeBackManager
    {
        private enum PACKAGE_STATUS
        {
            INVALID = -1,
            AVAILABLE = 0,
            RECEIVED = 1,
        }
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

        [XmlRoot(ElementName = "Package")]
        public class ComeBackPackage
        {
            [XmlAttribute(AttributeName = "Name")] public string Name;

            [XmlAttribute(AttributeName = "Limit")]
            public int DailyLimit;

            [XmlAttribute(AttributeName = "Price")]
            public int Price;

            [XmlAttribute(AttributeName = "Money")]
            public int Money;

            [XmlElement(ElementName = "Item")] public List<CheckInItem> Items;
        }

        [XmlRoot(ElementName = "ComebackEvent")]
        public class ComeBackConfig
        {
            [XmlElement(ElementName = "Package")]
            public List<ComeBackPackage> ComeBackPackages;
            // [XmlIgnore]
            // public DateTime StartDate;
            // [XmlIgnore]
            // public DateTime EndDate;

            [XmlIgnore]
            public Dictionary<int, ComeBackPackage> Packages;
        }

        private const string CONFIG_PATH = "Config/KT_Interactive/ComeBack.xml";

        private static ComeBackConfig Config;

        public static void Init()
        {
            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(ComeBackConfig));
                Config = xmlSerializer.Deserialize(fs) as ComeBackConfig;
                Config.Packages = new Dictionary<int, ComeBackPackage>();
                for (var i = 0; i < Config.ComeBackPackages.Count; ++i)
                {
                    Config.Packages.Add(i, Config.ComeBackPackages[i]);
                }

                // Config.StartDate = ServerConfig.Instance.OpenServerTime;
                // Config.EndDate = ServerConfig.Instance.OpenServerTime.AddDays(14).AddSeconds(-1);
            }
        }

        private static Dictionary<int, int> GetComebackStatus(KPlayer player)
        {
            var r = new Dictionary<int, int>();
            var comebackStatus = Global.GetRoleParamsInt32FromDB(player, "Comeback");
            for (var i = 0; i < Config.Packages.Count; i++)
            {
                var exp10 = (long)Math.Pow(10, i);
                var packageStatus = comebackStatus / exp10 % 10;
                r[i] = (int)packageStatus;
            }

            if (!KTGlobal.CanGetReturnAwards(player.strUserID) || player.m_Level < 10)
            {
                r[0] = -1;
            }

            if (!KTGlobal.CanGetReturnAwardsVIP(player.strUserID) || player.m_Level < 10)
            {
                r[1] = -1;
            }

            return r;
        }

        private static void UpdateComebackStatus(KPlayer player, int packageIndex)
        {
            var comebackStatus = Global.GetRoleParamsInt32FromDB(player, "Comeback");
            var exp10 = (int)Math.Pow(10, packageIndex);
            var mod = comebackStatus % exp10;
            comebackStatus = ((comebackStatus / exp10 / 10) * 10 + 1) * exp10 + mod;
            Global.SaveRoleParamsInt32ValueToDB(player, "Comeback", comebackStatus, true);
        }

        public static TCPOutPacket ResponseComebackStatus(KPlayer player, TCPOutPacketPool pool)
        {
            var statuses = GetComebackStatus(player);

            var data = DataHelper.ObjectToBytes(statuses);
            var outPacket = TCPOutPacket.MakeTCPOutPacket(pool, data, (int)TCPGameServerCmds.CMD_KT_UI_INTERACT);
            return outPacket;
        }

        public static TCPOutPacket PlayerBuy(KPlayer player, int packageID, TCPOutPacketPool pool)
        {
            // var now = TimeUtil.NowDateTime();
            if (!EventTimeManager.IsEventEnabled("ComebackEvent"))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không phải thời gian diễn ra hoạt động");
                return null;
            }
            var statuses = GetComebackStatus(player);
            if (!statuses.TryGetValue(packageID, out var buyStatus))
            {
                return null;
            }

            if (buyStatus == (int)PACKAGE_STATUS.INVALID)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bằng hữu không đủ điều kiện nhận gói vật phẩm này.");
                return null;
            }

            if (buyStatus == (int)PACKAGE_STATUS.RECEIVED)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bằng hữu dã nhận gói vật phẩm này rồi.");
                return null;
            }

            if (!Config.Packages.TryGetValue(packageID, out var package))
            {
                return null;
            }

            if (!KTGlobal.IsHaveMoney(player, package.Price, (MoneyType)package.Money))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bằng hữu không có đủ tiền.");
                return null;
            }

            var spaceNeeded = 0;
            foreach (var item in package.Items)
            {
                spaceNeeded += KTGlobal.GetTotalSpacesNeedToTakeItem(item.ID, item.Count);
            }

            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm. Vui lòng dọn dẹp túi đồ và thử lại.");
                return null;
            }

            var subResult = KTGlobal.SubMoney(player, package.Price, (MoneyType)package.Money, "CHECK-IN");
            if (!subResult.IsOK && package.Price != 0)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra. Vui lòng liên hệ quản trị viên.");
                return null;
            }

            UpdateComebackStatus(player, packageID);

            foreach (var item in package.Items)
            {
                var endTime = Global.ConstGoodsEndTime;
                if (item.TimeLimit > 0)
                {
                    endTime = DateTime.Now.AddMinutes(item.TimeLimit).ToString("yyyy-MM-dd HH:mm:ss");
                }
                ItemManager.CreateSimpleItem(player, item.ID, item.Count, 0, 1, Math.Max(item.EnhancedLevel, 0), endTime);
            }

            return ResponseComebackStatus(player, pool);
        }
    }
}