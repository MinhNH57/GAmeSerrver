using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.GameEvents.Core;
using GameServer.Logic;
using Server.Tools;
using GameServer.Core.GameEvent;

namespace GameServer.KiemThe.Core.Activity.ChargePromotion
{
    public enum RewardType
    {
        BY_PACKAGE = 1,
        ACCUMULATE = 2
    }

    public class ChargePromotionManager : BaseGameEvent<ChargePromotionData>
    {
        private const string RechargeAwardDir = "Config/RechargeAward.xml";

        public static readonly ChargePromotionManager Instance = new ChargePromotionManager();

        public string RawData { get; private set; }

        public override string EventKey => "DayRecharge";
        protected override string ConfigFilePath => RechargeAwardDir;

        public override bool IsActive()
        {
            if (Config == null) return false;
            return EventTimeManager.IsEventEnabled(Config.ByPackagePromotion.TimeKey) || 
                   EventTimeManager.IsEventEnabled(Config.AccumulatePromotion.TimeKey);
        }

        protected override void RegisterEventListeners() { }
        public override void processEvent(EventObject eventObject) { }

        protected override ChargePromotionData ParseConfig(string xmlContent)
        {
            var serializer = new XmlSerializer(typeof(ChargePromotionData));
            ChargePromotionData config;
            using (var reader = new StringReader(xmlContent))
            {
                config = serializer.Deserialize(reader) as ChargePromotionData;
            }

            if (config != null)
            {
                // Post-processing: Accumulate
                config.AccumulatePromotion.RewardDict = new Dictionary<int, AwardData>();
                foreach (var award in config.AccumulatePromotion.Awards)
                {
                    var boxContent = ItemRandomBox.GetStaticBoxContent(award.RewardItemID);
                    award.GoodsID = string.Join("|", boxContent.Select(x => $"{x.Key},{x.Value}"));

                    var bonusBoxContent = ItemRandomBox.GetStaticBoxContent(award.BonusItem);
                    award.BonusItemContent = string.Join("|", bonusBoxContent.Select(x => $"{x.Key},{x.Value}"));

                    config.AccumulatePromotion.RewardDict[award.KNB] = award;
                }

                // Post-processing: By Package
                config.ByPackagePromotion.RewardDict = new Dictionary<int, AwardData>();
                foreach (var award in config.ByPackagePromotion.Awards)
                {
                    var boxContent = ItemRandomBox.GetStaticBoxContent(award.RewardItemID);
                    award.GoodsID = string.Join("|", boxContent.Select(x => $"{x.Key},{x.Value}"));

                    var bonusBoxContent = ItemRandomBox.GetStaticBoxContent(award.BonusItem);
                    award.BonusItemContent = string.Join("|", bonusBoxContent.Select(x => $"{x.Key},{x.Value}"));

                    config.ByPackagePromotion.RewardDict[award.KNB] = award;
                }

                // Generate RawData for clients
                using (var sw = new StringWriter())
                {
                    serializer.Serialize(sw, config);
                    RawData = sw.ToString();
                }
            }

            return config;
        }



        private PromotionData GetPromotionData(RewardType option)
        {
            if (Config == null) return null;
            switch (option)
            {
                case RewardType.BY_PACKAGE: return Config.ByPackagePromotion;
                case RewardType.ACCUMULATE: return Config.AccumulatePromotion;
            }
            return null;
        }

        public bool IsEventActive(KPlayer client, RewardType option)
        {
            var promotionData = GetPromotionData(option);
            if (promotionData == null) return false;

            var currentDate = DateTime.Now;
            EventTimeManager.Event evt = EventTimeManager.GetEvent(promotionData.TimeKey);
            if (evt == null || evt.StartDate >= evt.EndDate) return false;

            if (evt.StartDate > currentDate)
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Hoạt động chưa bắt đầu!");
                return false;
            }

            if (evt.EndDate < currentDate)
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Hoạt động đã kết thúc!");
                return false;
            }

            return true;
        }

        public int[] GetRewardStatus(KPlayer client, RewardType option)
        {
            var promotionData = GetPromotionData(option);
            if (promotionData == null) return new int[0];

            KTGlobal.GetPlayerTodayChargeRecord(client, out var totalRecharge);
            var statuses = promotionData.Awards.Select(reward =>
            {
                var status = client.GetValueOfDailyRecore(reward.DailyKeyRecord);
                if (status != -1) return status;
                switch (option)
                {
                    case RewardType.ACCUMULATE:
                        if (totalRecharge >= reward.KNB) return 0;
                        break;
                    case RewardType.BY_PACKAGE:
                        if (KTGlobal.GetPlayerTodayChargeRecord(client, out _).ContainsKey(reward.KNB)) return 0;
                        break;
                }
                return status;
            });
            return statuses.ToArray();
        }

        public void GiveAwardToPlayer(KPlayer client, int awardValue, RewardType option)
        {
            if (!IsEventActive(client, option)) return;

            var todayRecord = KTGlobal.GetPlayerTodayChargeRecord(client, out var todayRecharge);
            switch (option)
            {
                case RewardType.ACCUMULATE:
                    if (todayRecharge >= awardValue) GiveAward(client, option, awardValue);
                    break;
                case RewardType.BY_PACKAGE:
                    if (todayRecord.ContainsKey(awardValue)) GiveAward(client, option, awardValue);
                    break;
            }
        }

        private int GetTotalReceivedReward(KPlayer player, RewardType option, int rewardID)
        {
            var roleParamName = option == RewardType.BY_PACKAGE ? RoleParamName.ChargePromotion_Packages : RoleParamName.ChargePromotion_Accumulate;
            var record = Global.GetRoleParamsInt64FromDB(player, roleParamName);
            var exp10 = (long)Math.Pow(10, rewardID);
            return (int)(record / exp10 % 10);
        }

        private void UpdateTotalReceivedReward(KPlayer player, RewardType option, int rewardID, int value)
        {
            var roleParamName = option == RewardType.BY_PACKAGE ? RoleParamName.ChargePromotion_Packages : RoleParamName.ChargePromotion_Accumulate;
            var record = Global.GetRoleParamsInt64FromDB(player, roleParamName);
            var exp10 = (long)Math.Pow(10, rewardID);
            var mod = record % exp10;
            record = (((record / exp10 / 10 * 10) + value) * exp10) + mod;
            Global.SaveRoleParamsInt64ValueToDB(player, roleParamName, record, true);
        }

        private void GiveAward(KPlayer client, RewardType option, int awardValue)
        {
            if (!KTGlobal.IsHaveSpace(2, client))
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Bạn cần 2 ô trống trong túi đồ để nhận thưởng!\nVui lòng dọn dẹp túi đồ và mở lại giao diện hoạt động để nhận thưởng!");
                return;
            }

            var promotionData = GetPromotionData(option);
            if (promotionData == null || !promotionData.RewardDict.TryGetValue(awardValue, out var rewardData)) return;

            EventTimeManager.Event evt = EventTimeManager.GetEvent(promotionData.TimeKey);
            if (evt == null) return;

            var timeExpire = evt.EndDate + TimeSpan.FromDays(7);
            var TimeUsing = timeExpire.ToString("yyyy-MM-dd HH:mm:ss");

            var isReceived = client.GetValueOfDailyRecore(rewardData.DailyKeyRecord) == 1;
            if (awardValue == rewardData.KNB && !isReceived)
            {
                var totalPackReceived = GetTotalReceivedReward(client, option, rewardData.ID);
                totalPackReceived += 1;
                var shouldReceiveBonus = false;
                if (totalPackReceived == promotionData.BonusCount)
                {
                    shouldReceiveBonus = true;
                    totalPackReceived = 0;
                }

                var isReceivedAward = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, rewardData.RewardItemID, 1, 0, "RECHARGE_AWARD", true, 1, false, TimeUsing);
                if (shouldReceiveBonus) isReceivedAward = isReceivedAward && ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, rewardData.BonusItem, 1, 0, "RECHARGE_AWARD", true, 1, false, TimeUsing);

                LogManager.WriteLog(LogTypes.Rechage, $"[CHARGE_PROMOTION] {client.RoleName}[{client.RoleID}] Award: {rewardData.RewardItemID}, Bonus: {rewardData.BonusItem}, PackCount: {totalPackReceived}");

                if (isReceivedAward)
                {
                    if (shouldReceiveBonus) PlayerManager.ShowNotification(client, "Tích lũy nhận thưởng 5 lần, nhận thêm quà!");
                    client.SetValueOfDailyRecore(rewardData.DailyKeyRecord, 1);
                    UpdateTotalReceivedReward(client, option, rewardData.ID, totalPackReceived);
                }
            }
        }
    }
}