using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents
{
    public static class ServerMergeRewardManager
    {
        private const string CONFIG_PATH = "Config/KT_Interactive/ServerMergeRewards.xml";
        private const string RECORD_KEY = "ServerMergeReward";
        private static MergeRewardsConfig _config;

        public static void Init()
        {
            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(MergeRewardsConfig));
                _config = serializer.Deserialize(fs) as MergeRewardsConfig;
            }

            if (_config != null && _config.Packages != null)
            {
                foreach (var package in _config.Packages)
                {
                    package.Prepare();
                }
            }
        }

        public static Dictionary<int, int> GetRewardStatus(KPlayer player, string status = null)
        {
            var result = new Dictionary<int, int>();
            if (status == null)
            {
                status = Global.GetRoleParamsStringWithDB(player, RECORD_KEY) ?? "";
            }
            var statusMap = new Dictionary<int, int>();

            var entries = status.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                var parts = entry.Split(';');
                if (parts.Length == 2 && int.TryParse(parts[0], out var id) &&
                    int.TryParse(parts[1], out var limit))
                {
                    statusMap[id] = limit;
                }
            }

            foreach (var package in _config.Packages)
            {
                if (!package.PreparedZoneIDs.Contains(player.CreatedZoneID))
                {
                    result[package.ID] = -1;
                    continue;
                }

                if (statusMap.TryGetValue(package.ID, out var claimed))
                {
                    result[package.ID] = claimed >= package.Limit ? 1 : 0;
                }
                else
                {
                    result[package.ID] = 0;
                }
            }

            return result;
        }

        public static TCPOutPacket ResponseRewardStatus(KPlayer player, TCPOutPacketPool pool, string status = null)
        {
            var createdZoneID = player.CreatedZoneID;
            if (status == null)
            {
                status = Global.GetRoleParamsStringWithDB(player, RECORD_KEY) ?? "";
            }
            Tuple<int, string> statusArray = new Tuple<int, string>(createdZoneID, status);
            var data = DataHelper.ObjectToBytes(statusArray);
            return TCPOutPacket.MakeTCPOutPacket(pool, data, (int)TCPGameServerCmds.CMD_KT_UI_INTERACT);
        }

        public static void SetLevel(KPlayer player)
        {
            if (!EventTimeManager.IsEventEnabled("MergeServerReward"))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không phải thời gian diễn ra sự kiện.");
                return;
            }

            if (!IsConditionMet(player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                $"Người chơi chưa đủ điều kiện để nhận hỗ trợ. Yêu cầu trang bị Phi Phong cấp {_config.MantleRequirement} và Tích lũy chuyển xu từ {_config.RechargeStartDate} đến {_config.RechargeEndDate} đạt {_config.RechargeRequirement} KNB.");
                return;
            }

            if (player.m_Level >= _config.SetLevel)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                $"Cấp độ nhận vật không phù hợp để nhận hỗ trợ.");
                return;
            }

            player.UnAssignPotential();
            GameManager.ClientMgr.SetRoleLevel(player, _config.SetLevel);
            player.ClearAssignedPotential();
            PlayerManager.ChangeRoute(player, GameDataDef.KD_FACTION_ROUTE_NONE);
            player.Skills.RemoveInvalidSkillBasedOnLevel();
        }

        private static bool IsConditionMet(KPlayer player)
        {
            long rechargeAmount = KTGlobal.GetPlayerTotalSumRechargeByTime(player, _config.RechargeStartDate, _config.RechargeEndDate);
            bool conditionMet = rechargeAmount >= _config.RechargeRequirement && Global.GetMantleLevel(player) >= _config.MantleRequirement;

            return conditionMet;
        }

        public static TCPOutPacket ClaimReward(KPlayer player, int packageId, TCPOutPacketPool pool)
        {
            if (!EventTimeManager.IsEventEnabled("MergeServerReward"))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không phải thời gian diễn ra sự kiện.");
                return null;
            }

            var package = _config.Packages.FirstOrDefault(p => p.ID == packageId);
            if (package == null) return null;

            var statusClaim = Global.GetRoleParamsStringWithDB(player, RECORD_KEY) ?? "";
            var statuses = GetRewardStatus(player, statusClaim);

            if (package.ApplyCondition)
            {
                if (!IsConditionMet(player))
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo",
                        $"Người chơi chưa đủ điều kiện để nhận hỗ trợ. Yêu cầu trang bị Phi Phong cấp {_config.MantleRequirement} và Tích lũy chuyển xu từ {_config.RechargeStartDate} đến {_config.RechargeEndDate} đạt {_config.RechargeRequirement} KNB.");
                    return null;
                }
            }

            if (!statuses.TryGetValue(packageId, out var status) || status != 0)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                    status < 0 ? "Bằng hữu không thể nhận gói phần thưởng này." : "Bằng hữu đã nhận gói phần thưởng này rồi.");
                return null;
            }

            if (package.Money > 0 && !KTGlobal.IsHaveMoney(player, package.Money, (MoneyType)package.MoneyType))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Bằng hữu không có đủ tiền.");
                return null;
            }

            var spaceNeeded = package.Items.Sum(i => KTGlobal.GetTotalSpacesNeedToTakeItem(i.ItemID, i.Number));
            if (!KTGlobal.IsHaveSpace(spaceNeeded, player))
            {
                PlayerManager.ShowMessageBox(player, "Thông báo",
                    $"Cần tối thiểu {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm.");
                return null;
            }

            if (package.Money > 0)
            {
                if (!KTGlobal.SubMoney(player, package.Money, (MoneyType)package.MoneyType, "SERVER_MERGE").IsOK)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Đã có lỗi xảy ra khi trừ tiền.");
                    return null;
                }
            }

            foreach (var item in package.Items)
            {
                ItemManager.CreateSimpleItem(player, item.ItemID, item.Number, 0, item.Binding);
            }

            var key = $"{package.ID};";
            var idx = statusClaim.IndexOf(key);
            if (idx >= 0)
            {
                var endIdx = statusClaim.IndexOf("|", idx);
                var countStr = statusClaim.Substring(idx + key.Length,
                    endIdx < 0 ? statusClaim.Length - (idx + key.Length) : endIdx - (idx + key.Length));
                var claimed = int.Parse(countStr) + 1;
                statusClaim = statusClaim.Substring(0, idx) + key + claimed +
                    (endIdx < 0 ? "" : statusClaim.Substring(endIdx));
            }
            else
            {
                statusClaim += (statusClaim.Length > 0 ? "|" : "") + key + "1";
            }

            Global.SaveRoleParamsStringToDB(player, RECORD_KEY, statusClaim, true);

            return ResponseRewardStatus(player, pool, statusClaim);
        }
    }

    [XmlRoot(ElementName = "Item")]
    public class RewardItem
    {
        [XmlAttribute("ItemID")]
        public int ItemID;

        [XmlAttribute("Number")]
        public int Number;

        [XmlAttribute("Binding")]
        public int Binding;
    }

    [XmlRoot(ElementName = "Package")]
    public class MergePackage
    {
        [XmlAttribute("ID")]
        public int ID;

        [XmlAttribute("CreatedZoneID")]
        public string CreatedZoneID;

        [XmlAttribute("Name")]
        public string Name;

        [XmlAttribute("MoneyType")]
        public int MoneyType;

        [XmlAttribute("Money")]
        public int Money;

        [XmlAttribute("Limit")]
        public int Limit;

        [XmlAttribute("ApplyCondition")]
        public bool ApplyCondition;

        [XmlArray("Items")]
        [XmlArrayItem("Item")]
        public List<RewardItem> Items;

        [XmlIgnore]
        public HashSet<int> PreparedZoneIDs { get; private set; }

        public void Prepare()
        {
            if (string.IsNullOrEmpty(CreatedZoneID))
            {
                PreparedZoneIDs = new HashSet<int>();
                return;
            }
            PreparedZoneIDs = new HashSet<int>(CreatedZoneID.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s.Trim())));
        }

        [XmlIgnore]
        public List<int> ZoneIDs => PreparedZoneIDs.ToList();
    }

    [XmlRoot(ElementName = "MergeRewards")]
    public class MergeRewardsConfig
    {
        [XmlAttribute("RechargeStartDate")]
        public string RechargeStartDate;

        [XmlAttribute("RechargeEndDate")]
        public string RechargeEndDate;

        [XmlAttribute("RechargeRequirement")]
        public int RechargeRequirement;

        [XmlAttribute("MantleRequirement")]
        public int MantleRequirement;
        
        [XmlAttribute("SetLevel")]
        public int SetLevel;

        [XmlElement("Package")]
        public List<MergePackage> Packages;
    }

}