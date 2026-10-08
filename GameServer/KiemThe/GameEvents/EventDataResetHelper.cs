using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;

namespace GameServer.KiemThe.GameEvents
{
    /// <summary>
    /// Static helper with per-event reset methods.
    /// Each method clears all per-player data for one event.
    /// Equivalent to SQL in scripts/reset_before_event.py.
    /// </summary>
    public static class EventDataResetHelper
    {
        /// <summary>
        /// CheckInEvent: idx=10160 v1-v6
        /// </summary>
        public static void ResetCheckIn(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_Low, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_Mid, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_High, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_Low_Acc, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_Mid_Acc, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.CheckIn_High_Acc, 0, true);
        }

        /// <summary>
        /// SeashellEvent: idx=10160 v7, v10 (score + free ticket)
        /// </summary>
        public static void ResetSeashell(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SeashellScore, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SeashellFreeTicket, 0, true);
        }

        /// <summary>
        /// EventCompose: idx=10120 v37,v38 + idx=10160 v21
        /// </summary>
        public static void ResetEventCompose(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MonthlyBox1, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MonthlyBox2, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.MonthlyBox1_Accumulate, 0, true);
        }

        /// <summary>
        /// SlotMachine: idx=10160 v8,v9 + pname SlotMachine_FreeTicket
        /// </summary>
        public static void ResetSlotMachine(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SlotMachine_Point, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SlotMachine_MaxNumber, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.SlotMachine_FreeTicket, 0, true);
        }

        /// <summary>
        /// LuckySpin: pname LuckySpinPoint + LuckySpinRecord
        /// Delegates to existing ResetLuckySpinData()
        /// </summary>
        public static void ResetLuckySpin(KPlayer client)
        {
            client.ResetLuckySpinData();
        }

        /// <summary>
        /// RechargeEvent: pname DailyRechargeRecord
        /// Delegates to existing ClearRechargeAccumulateEventData()
        /// </summary>
        public static void ResetRechargeEvent(KPlayer client)
        {
            client.ClearRechargeAccumulateEventData();
        }

        /// <summary>
        /// DayRecharge: idx=10160 v0
        /// </summary>
        public static void ResetDayRecharge(KPlayer client)
        {
            Global.SaveRoleParamsInt64ValueToDB(client, RoleParamName.ChargePromotion_Packages, 0, true);
        }

        /// <summary>
        /// AccumulateRecharge: idx=10120 v39
        /// </summary>
        public static void ResetAccumulateRecharge(KPlayer client)
        {
            Global.SaveRoleParamsInt64ValueToDB(client, RoleParamName.ChargePromotion_Accumulate, 0, true);
        }

        /// <summary>
        /// FlowerTower: pname FlowerTower (protobuf)
        /// </summary>
        public static void ResetFlowerTower(KPlayer client)
        {
            Global.SaveRoleParamsStringToDB(client, RoleParamName.FlowerTowerRecord, "", true);
            client._TowerStatusData = new TowerStatusData();
        }

        /// <summary>
        /// UuDaiDoanKet: idx=10160 v11
        /// </summary>
        public static void ResetUnionShop(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.UnionShopRefunded, 0, true);
        }

        /// <summary>
        /// OfferingEvent: idx=10160 v18-v20
        /// </summary>
        public static void ResetOffering(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.Offering_Total, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.Offering_Milestone, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.Offering_TotalMerit, 0, true);
        }

        /// <summary>
        /// GiftExchange: idx=10160 v12-v14 + dynamic pname keys from config
        /// </summary>
        public static void ResetGiftExchange(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GiftExchange_Sent, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GiftExchange_Received, 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.GiftExchange_Sent_Milestone, 0, true);

            try
            {
                if (GiftExchange.GiftExchangeManager.Config?.Exchange != null)
                {
                    foreach (var exchangeData in GiftExchange.GiftExchangeManager.Config.Exchange)
                    {
                        if (!string.IsNullOrEmpty(exchangeData.RoleParam))
                        {
                            Global.SaveRoleParamsStringToDB(client, exchangeData.RoleParam, "", true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"[EventReset] Failed clearing GiftExchange pnames for [{client.RoleID}]: {ex.Message}");
            }
        }

        /// <summary>
        /// QuestEvent: pname QuestEventRecord (protobuf)
        /// </summary>
        public static void ResetQuestEvent(KPlayer client)
        {
            Global.SaveRoleParamsStringToDB(client, RoleParamName.QuestEventRecord, "", true);
            client._QuestEventRecord = new QuestEventRecord();
        }

        /// <summary>
        /// SlotMachine_ExchangeTicket: pname + _LastResetMonth
        /// </summary>
        public static void ResetSlotMachineExchangeTicket(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, "SlotMachine_ExchangeTicket", 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, "SlotMachine_ExchangeTicket_LastResetMonth", 0, true);
        }

        /// <summary>
        /// BaoThach_ExchangeTicket: pname + _LastResetMonth
        /// </summary>
        public static void ResetBaoThachExchangeTicket(KPlayer client)
        {
            Global.SaveRoleParamsInt32ValueToDB(client, "BaoThach_ExchangeTicket", 0, true);
            Global.SaveRoleParamsInt32ValueToDB(client, "BaoThach_ExchangeTicket_LastResetMonth", 0, true);
        }
    }
}
