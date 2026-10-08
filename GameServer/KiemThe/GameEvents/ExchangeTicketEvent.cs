using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.GameEvents.Core;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GameServer.Core.GameEvent;

namespace GameServer.KiemThe.GameEvents
{
    [XmlRoot(ElementName = "ExchangeTicket")]
    public class ExchangeTicketConfig
    {
        [XmlElement(ElementName = "Exchange")]
        public List<ExchangeConfig> Exchanges;
    }

    public class ExchangeConfig
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;

        [XmlAttribute(AttributeName = "RateKNB")]
        public int RateKNB;

        [XmlAttribute(AttributeName = "ItemID")]
        public int ItemID;

        [XmlAttribute(AttributeName = "ItemCount")]
        public int ItemCount;

        [XmlAttribute(AttributeName = "Message")]
        public string Message;

        [XmlAttribute(AttributeName = "TimeKey")]
        public string TimeKey;
    }

    public class ExchangeTicketManager : BaseGameEvent<ExchangeTicketConfig>
    {
        private const string ExchangeTicketDir = "Config/KT_Interactive/ExchangeTicketEvent.xml";

        public static readonly ExchangeTicketManager Instance = new ExchangeTicketManager();

        public override string EventKey => "ExchangeTicket";
        protected override string ConfigFilePath => ExchangeTicketDir;

        public override bool IsActive()
        {
            if (Config == null || Config.Exchanges == null) return false;
            return Config.Exchanges.Any(c => EventTimeManager.IsEventEnabled(c.TimeKey));
        }

        protected override void RegisterEventListeners() { }

        public override void processEvent(EventObject eventObject) { }

        public void HandleExchangeTicket(KPlayer player)
        {
            if (Config == null || Config.Exchanges == null) return;

            foreach (var config in Config.Exchanges)
            {
                if (!EventTimeManager.IsEventEnabled(config.TimeKey)) continue;

                var evt_active = EventTimeManager.GetEvent(config.TimeKey);
                if (evt_active == null) continue;

                CheckMonthlyReset(player, config.TimeKey);

                var totalXuCharged = (int)KTGlobal.GetPlayerTotalSumRechargeByTime(player, evt_active.StartDate_str, evt_active.EndDate_str);
                var totalTicketReceived = Global.GetRoleParamsInt32FromDB(player, config.TimeKey, 0);

                var ticketsToReceive = Math.Max(0, (totalXuCharged / config.RateKNB) - totalTicketReceived);

                if (ticketsToReceive <= 0)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", $"{config.Message}");
                    continue;
                }

                if (!KTGlobal.IsHaveSpace(1, player))
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Vui lòng dọn trống túi đồ.");
                    return;
                }

                var itemCountToCreate = ticketsToReceive * config.ItemCount;
                if (!ItemManager.CreateSimpleItem(player, config.ItemID, itemCountToCreate, 0, 1, 0))
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Có lỗi khi tạo vật phẩm.");
                    return;
                }

                Global.SaveRoleParamsInt32ValueToDB(player, config.TimeKey, totalXuCharged / config.RateKNB, true);
                LogManager.WriteLog(LogTypes.SpecialEvent, $"[EXCHANGE_TICKET] {player.RoleName} [{player.RoleID}] Key: {config.TimeKey}, Xu: {totalXuCharged}, Items: {itemCountToCreate}");
                PlayerManager.ShowMessageBox(player, "Thông báo", $"Bạn đã nhận được {itemCountToCreate} {config.Name}.");
            }
        }

        public void CheckExchangeStatus(KPlayer client)
        {
            if (Config == null || Config.Exchanges == null) return;
            List<string> statusMessages = new List<string>();

            foreach (var config in Config.Exchanges)
            {
                if (!EventTimeManager.IsEventEnabled(config.TimeKey)) continue;

                var evt_active = EventTimeManager.GetEvent(config.TimeKey);
                if (evt_active == null) continue;

                CheckMonthlyReset(client, config.TimeKey);

                var totalXuCharged = (int)KTGlobal.GetPlayerTotalSumRechargeByTime(client, evt_active.StartDate_str, evt_active.EndDate_str);
                var totalTicketReceived = Global.GetRoleParamsInt32FromDB(client, config.TimeKey, 0);

                var ticketsToReceive = Math.Max(0, (totalXuCharged / config.RateKNB) - totalTicketReceived);
                var itemTemplate = ItemManager.GetItemTemplate(config.ItemID);

                statusMessages.Add($"{config.Name}:\n- KNB đã nạp: {totalXuCharged}\n- {itemTemplate.Name} đã nhận: {totalTicketReceived}\n- {itemTemplate.Name} có thể nhận: {ticketsToReceive}");
            }

            if (statusMessages.Count > 0)
            {
                var finalMessage = string.Join("\n", statusMessages);
                PlayerManager.ShowMessageBox(client, "Trạng thái đổi vé", finalMessage);
            }
        }

        private void CheckMonthlyReset(KPlayer client, string timeKey)
        {
            var resetKey = $"{timeKey}RS";

            var lastResetMonth = Global.GetRoleParamsInt32FromDB(client, resetKey, 0);
            var today = DateTime.Now;
            if (lastResetMonth != today.Month)
            {
                Global.SaveRoleParamsInt32ValueToDB(client, timeKey, 0, true);
                Global.SaveRoleParamsInt32ValueToDB(client, resetKey, today.Month, true);

                LogManager.WriteLog(LogTypes.SpecialEvent, $"[EXCHANGE_TICKET] {client.RoleName} [{client.RoleID}] TimeKey: {timeKey} LastResetMonth: {lastResetMonth} , Reset monthly {today.Month}");
            }
        }
    }
}
