using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.GameEvents
{
    /// <summary>
    /// Central orchestrator for auto-resetting per-player event data on login
    /// and global event data on server startup.
    /// </summary>
    public static class EventAutoResetManager
    {
        /// <summary>
        /// Registry: maps TimeKey → reset action for per-player data
        /// </summary>
        private static readonly Dictionary<string, Action<KPlayer>> ResetRegistry = new Dictionary<string, Action<KPlayer>>
        {
            { "CheckInEvent",                EventDataResetHelper.ResetCheckIn },
            { "SeashellEvent",               EventDataResetHelper.ResetSeashell },
            { "EventCompose",                EventDataResetHelper.ResetEventCompose },
            { "SlotMachine",                 EventDataResetHelper.ResetSlotMachine },
            { "LuckySpin",                   EventDataResetHelper.ResetLuckySpin },
            { "RechargeEvent",               EventDataResetHelper.ResetRechargeEvent },
            { "DayRecharge",                 EventDataResetHelper.ResetDayRecharge },
            { "AccumulateRecharge",          EventDataResetHelper.ResetAccumulateRecharge },
            { "FlowerTower",                 EventDataResetHelper.ResetFlowerTower },
            { "UuDaiDoanKet",               EventDataResetHelper.ResetUnionShop },
            { "OfferingEvent",              EventDataResetHelper.ResetOffering },
            { "GiftExchange",                EventDataResetHelper.ResetGiftExchange },
            { "QuestEvent",                  EventDataResetHelper.ResetQuestEvent },
            { "SlotMachine_ExchangeTicket",  EventDataResetHelper.ResetSlotMachineExchangeTicket },
            { "BaoThach_ExchangeTicket",     EventDataResetHelper.ResetBaoThachExchangeTicket },
        };

        /// <summary>
        /// Called once per player login, after all event data is loaded.
        /// Checks each registered event and resets data if the event has a new period.
        /// </summary>
        public static void ProcessPlayerLogin(KPlayer client)
        {
            if (client.RoleEventResetTimestamps == null)
            {
                client.RoleEventResetTimestamps = new KPlayer.EventResetTimestamps();
            }

            bool isFirstDeploy = (client.RoleEventResetTimestamps.LastResetTicks == null ||
                                   client.RoleEventResetTimestamps.LastResetTicks.Count == 0);
            bool changed = false;

            foreach (var entry in ResetRegistry)
            {
                string timeKey = entry.Key;
                Action<KPlayer> resetAction = entry.Value;

                var evt = EventTimeManager.GetEvent(timeKey);
                if (evt == null) continue;

                if (isFirstDeploy)
                {
                    // First deploy: mark all active events as "already reset"
                    // Prevents resetting mid-event data for existing players
                    client.SetLastResetTime(timeKey, DateTime.Now);
                    changed = true;
                    continue;
                }

                DateTime lastReset = client.GetLastResetTime(timeKey);
                if (lastReset < evt.StartDate)
                {
                    try
                    {
                        resetAction(client);
                        client.SetLastResetTime(timeKey, DateTime.Now);
                        changed = true;
                        LogManager.WriteLog(LogTypes.Info,
                            $"[EventAutoReset] Reset [{timeKey}] for player [{client.RoleID}] {client.RoleName}");
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.Error,
                            $"[EventAutoReset] Failed reset [{timeKey}] for [{client.RoleID}]: {ex.Message}");
                    }
                }
            }

            if (changed)
            {
                client.SaveEventResetTimestamps();
            }
        }

        #region Global Data Reset (Server Startup)

        /// <summary>
        /// Global reset timestamp key in t_systemglobalvalue
        /// </summary>
        private const int GLOBAL_RESET_TIMESTAMP_ID = 100;

        /// <summary>
        /// Mapping: TimeKey → list of t_systemglobalvalue IDs to reset
        /// </summary>
        private static readonly Dictionary<string, int[]> GlobalResetRegistry = new Dictionary<string, int[]>
        {
            { "SeashellEvent",  new[] { 1, 7 } },           // STORAGE_SEASHELL, TOP_RANKING_SEASHELL
            { "SlotMachine",    new[] { 8, 9 } },           // TOP_RANKING_SLOT_MAX, TOP_RANKING_SLOT_POINT
            { "GiftExchange",   new[] { 3, 4, 5, 6, 10, 11 } }, // TOP_RANKING_GROUP_0-X, GIFT_SENT/RECEIVE
            { "OfferingEvent",  new[] { 13 } },             // TOP_RANKING_OFFERING
            { "LuckySpin",      new[] { 14 } },             // TOP_RANKING_LUCKYSPIN
        };

        /// <summary>
        /// Called once on server startup, after EventTimeManager.Init().
        /// Resets global data (t_systemglobalvalue, t_config) for events with new periods.
        /// </summary>
        public static void ProcessServerStartup()
        {
            try
            {
                string lastResetStr = GameDb.GetSystemGlobalParameter(GLOBAL_RESET_TIMESTAMP_ID);
                DateTime lastGlobalReset = DateTime.MinValue;
                if (!string.IsNullOrEmpty(lastResetStr) && long.TryParse(lastResetStr, out long ticks))
                {
                    lastGlobalReset = new DateTime(ticks);
                }

                bool anyReset = false;

                foreach (var entry in GlobalResetRegistry)
                {
                    string timeKey = entry.Key;
                    int[] globalIds = entry.Value;

                    var evt = EventTimeManager.GetEvent(timeKey);
                    if (evt == null) continue;

                    if (lastGlobalReset < evt.StartDate)
                    {
                        foreach (int id in globalIds)
                        {
                            KTGlobal.SetSystemGlobalParameter(id, 0);
                        }
                        anyReset = true;
                        LogManager.WriteLog(LogTypes.Info,
                            $"[EventAutoReset] Global reset for [{timeKey}]: IDs [{string.Join(",", globalIds)}]");
                    }
                }

                // FlowerTower t_config reset
                var flowerEvt = EventTimeManager.GetEvent("FlowerTower");
                if (flowerEvt != null && lastGlobalReset < flowerEvt.StartDate)
                {
                    ResetFlowerTowerConfig();
                    anyReset = true;
                    LogManager.WriteLog(LogTypes.Info,
                        "[EventAutoReset] Global reset for FlowerTower t_config entries");
                }

                if (anyReset)
                {
                    KTGlobal.SetSystemGlobalParameter(GLOBAL_RESET_TIMESTAMP_ID, DateTime.Now.Ticks);
                }

                LogManager.WriteLog(LogTypes.Info,
                    $"[EventAutoReset] Server startup global reset completed. Reset performed: {anyReset}");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"[EventAutoReset] Server startup global reset failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reset flowertower speed reward counters in t_config
        /// </summary>
        private static void ResetFlowerTowerConfig()
        {
            try
            {
                if (FlowerTowerManager.SpeedRewardList == null) return;
                foreach (var speedReward in FlowerTowerManager.SpeedRewardList)
                {
                    string key = $"flowertower_{GameManager.ServerId}_speed{speedReward.FloorID}";
                    GameManager.GameConfigMgr.UpdateGameConfigItem(key, "0");
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"[EventAutoReset] FlowerTower config reset error: {ex.Message}");
            }
        }

        #endregion
    }
}
