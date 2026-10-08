using GameServer.KiemThe.Logic;
using GameServer.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameServer.KiemThe.Entities;
using System;
using System.Collections.Concurrent;
using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.GameDbController;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.SlotMachine
{
    public static class OfferingManager
    {
        private class OfferingStage
        {
            public int MinCount { get; set; }
            public int MaxCount { get; set; }
            public string Currency { get; set; }
            public int Cost { get; set; }
            public string MeritPoints { get; set; }
            public string MeritWeight { get; set; }
            public string MessageFormat { get; set; }
            public int ServerRangeMin { get; set; }
            public int ServerRangeMax { get; set; }
        }

        private class Milestone
        {
            public int ID { get; set; }
            public int PointsRequired { get; set; }
            public string MessageFormat { get; set; }
            public List<KeyValuePair<int, int>> Rewards { get; set; }
        }

        private static string TimeKey = "OfferingEvent";
        private static List<OfferingStage> BacStages = new List<OfferingStage>();
        private static List<OfferingStage> KNBStages = new List<OfferingStage>();
        private static List<Milestone> Milestones = new List<Milestone>();
        private static int DailyLimit = 90;
        private static int OfferingItemID = 20846;
        private static int OfferingBuffID = 2060;

        public static void Init()
        {
            LoadConfig();
            // đọc log event từ file json 10'/lần
            // json được generate từ script python
            ScheduleExecutor2.Instance.scheduleCancle("TopOffering");
            ScheduleExecutor2.Instance.scheduleExecute(new NormalScheduleTask("TopOffering", ReadTopOffering), 1000,
                60000);
        }

        private static string TopOfferingXML;

        private static void ReadTopOffering()
        {
            TopOfferingXML = GameDb.GetSystemGlobalParameter(Entities.SystemGlobalParameterIndexes.TOP_RANKING_OFFERING + GameManager.ServerLineID, true);
        }

        public static void ShowOfferingRanking(KPlayer player)
        {
            KT_TCPHandler.SendOpenUI(player, "EventRanking", Array.Empty<int>(), new List<string>{"Bảng xếp hạng Dâng Hương", TopOfferingXML});
        }

        private static void LoadConfig()
        {
            var xml = Global.GetGameResXml("Config/KT_Interactive/Offering.xml");

            TimeKey = xml.Attribute("TimeKey").Value;
            DailyLimit = KTGlobal.GetXMLAttribute<int>(xml.Element("OfferingLimits"), "DailyLimit");
            OfferingItemID = KTGlobal.GetXMLAttribute<int>(xml.Element("OfferingReward"), "ItemID");
            OfferingBuffID = KTGlobal.GetXMLAttribute<int>(xml.Element("OfferingReward"), "BuffID");

            BacStages = xml.Element("OfferingStagesBac")
                .Elements("Stage")
                .Select(e => new OfferingStage()
                {
                    MinCount = int.Parse(e.Attribute("Range").Value.Split('-')[0]),
                    MaxCount = int.Parse(e.Attribute("Range").Value.Split('-')[1]),
                    Currency = e.Attribute("Currency").Value,
                    Cost = int.Parse(e.Attribute("Cost").Value),
                    MeritPoints = e.Attribute("MeritPoints").Value,
                    MeritWeight = e.Attribute("MeritWeight").Value,
                    MessageFormat = e.Attribute("Message").Value,
                    ServerRangeMin = int.Parse(e.Attribute("ServerRange").Value.Split('-')[0]),
                    ServerRangeMax = int.Parse(e.Attribute("ServerRange").Value.Split('-')[1])

                })
                .ToList();

            KNBStages = xml.Element("OfferingStagesKNB")
                .Elements("Stage")
                .Select(e => new OfferingStage()
                {
                    MinCount = int.Parse(e.Attribute("Range").Value.Split('-')[0]),
                    MaxCount = int.Parse(e.Attribute("Range").Value.Split('-')[1]),
                    Currency = e.Attribute("Currency").Value,
                    Cost = int.Parse(e.Attribute("Cost").Value),
                    MeritPoints = e.Attribute("MeritPoints").Value,
                    MeritWeight = e.Attribute("MeritWeight").Value,
                    MessageFormat = e.Attribute("Message").Value,
                    ServerRangeMin = int.Parse(e.Attribute("ServerRange").Value.Split('-')[0]),
                    ServerRangeMax = int.Parse(e.Attribute("ServerRange").Value.Split('-')[1])
                })
                .ToList();

            // TODO: chuyển sang file config
            // bạc ở s24 trở lên chỉ tốn 20 vạn
            // if (GameManager.ServerLineID >= 24)
            // {
            //     BacStages[0].Cost = 20000;
            // } 
            
            Milestones = xml.Element("Milestones")
                .Elements("Milestone")
                .Select(e => new Milestone()
                {
                    ID = KTGlobal.GetXMLAttribute<int>(e, "ID"),
                    PointsRequired = int.Parse(e.Attribute("Points").Value),
                    MessageFormat = e.Attribute("Message").Value,
                    Rewards = e.Elements("Reward")
                        .Select(r => new KeyValuePair<int, int>(
                            int.Parse(r.Attribute("ItemID").Value),
                            int.Parse(r.Attribute("Quantity").Value))
                        ).ToList()
                }).OrderBy(m => m.PointsRequired).ToList();
        }

        public static bool ProcessOffering(KPlayer player, string currency)
        {
            var message = string.Empty;

            var dailyCount = player.GetValueOfDailyRecore((int)RecoreDef.Offering_CurrentDailyLimit);
            var totalMerit = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_TotalMerit, 0);
            var totalCount = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Total, 0);
            if (dailyCount == -1)
            {
                dailyCount = 0;
                player.SetValueOfDailyRecore((int)RecoreDef.Offering_CurrentDailyLimit, 0);
            }

            if (dailyCount >= DailyLimit)
            {
                message = "Đã đạt giới hạn dâng hương hôm nay";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            var stage = GetCurrentStage(dailyCount + 1, currency);
            if (stage == null)
            {
                message = "Đã đạt giới hạn dâng hương bằng tài nguyên này";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            if (!KTGlobal.IsHaveSpace(1, player))
            {
                message = "Không đủ chỗ trong túi đồ";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            if (!HandlePayment(player, stage, currency))
            {
                message = "Không đủ tài nguyên để dâng hương";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            dailyCount++;
            totalCount++;
            var meritPoints = GetMeritPoints(stage);

            // TODO: đọc từ file config
            // switch (stage.MaxCount)
            // {
            //     case 60:
            //         meritPoints = StaticRandom.AccumulatedProbabilitySelection(new List<int> { 1, 2 }, new List<int>{70, 30});
            //         break;
            //     case 90:
            //         meritPoints = StaticRandom.AccumulatedProbabilitySelection(new List<int> { 1, 2, 3 }, new List<int>{76, 20, 4});
            //         break;
            // }
            
            totalMerit += meritPoints;

            UpdatePlayerRecords(player, dailyCount, totalMerit);

            ItemManager.CreateSimpleItem(player, OfferingItemID, 1, 0, 1);
            player.Buffs.AddBuff(OfferingBuffID, 1, 90 * 60 * 1000, 1, true);

            message = string.Format(stage.MessageFormat, player.RoleName, meritPoints);
            KT_TCPHandler.SendOpenUI(player, "Messages/UIOfferingMessage", Array.Empty<int>(), new List<string> { "Message", message });
            return true;
        }

        private static int GetMeritPoints(OfferingStage stage)
        {
            var pointsRange = stage.MeritPoints.Split('-').Select(int.Parse).ToList();
            var weightRange = stage.MeritWeight.Split('-').Select(int.Parse).ToList();

            return StaticRandom.AccumulatedProbabilitySelection(pointsRange, weightRange);
        }

        private static OfferingStage GetCurrentStage(int nextCount, string currency)
        {
            int currentServer = GameManager.ServerLineID;
            SysConOut.WriteLine("currentServer: " + currentServer);
            if (currency == "Bac")
            {
                var stage = BacStages
                    .Where(s => currentServer >= s.ServerRangeMin && currentServer <= s.ServerRangeMax)
                    .Where(s => nextCount >= s.MinCount && nextCount <= s.MaxCount)
                    .FirstOrDefault();
                return stage;
            }
            else
            {
                var stage = KNBStages
                    .Where(s => currentServer >= s.ServerRangeMin && currentServer <= s.ServerRangeMax)
                    .Where(s => nextCount >= s.MinCount && nextCount <= s.MaxCount)
                    .FirstOrDefault();
                return stage;
            }
        }

        private static bool HandlePayment(KPlayer player, OfferingStage stage, string currency)
        {
            if (stage.Currency != currency)
            {
                return false;
            }

            

            int cost = stage.Cost;
            

            switch (currency)
            {
                case "Bac" when KTGlobal.GetMoneyhave(player, Entities.MoneyType.Bac) < cost:
                    return false;
                case "Bac":
                    KTGlobal.SubMoney(player, cost, MoneyType.Bac, "Offering");
                    return true;
                case "KNB" when KTGlobal.GetMoneyhave(player, Entities.MoneyType.Dong) < cost:
                    return false;
                case "KNB":
                    KTGlobal.SubMoney(player, cost, MoneyType.Dong, "Offering");
                    return true;
                default:
                    return false;
            }
        }

        private static void UpdatePlayerRecords(KPlayer player, int dailyCount, int totalMerit)
        {
            player.SetValueOfDailyRecore((int)RecoreDef.Offering_CurrentDailyLimit, dailyCount);

            var totalOffer = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Total, 0);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.Offering_Total, totalOffer + 1, false);

            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.Offering_TotalMerit, totalMerit, false);
        }

        private static int GetMilestoneState(KPlayer player, int milestone)
        {
            var status = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Milestone);
            return (status >> milestone) & 1;
        }

        private static void SetMilestoneState(KPlayer player, int milestone)
        {
            var status = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Milestone);
            status |= (1 << milestone);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.Offering_Milestone, status, true);
        }

        public static bool ClaimMilestone(KPlayer player, int milestoneId)
        {
            var milestone = Milestones.FirstOrDefault(x => x.ID == milestoneId);
            if (milestone == null)
            {
                PlayerManager.ShowMessageBox(player, "Thông báo", "Không tìm thấy mốc thành tựu");
                return false;
            }

            var currentState = GetMilestoneState(player, milestoneId);

            if (currentState != 0)
            {
                var message = currentState == 1 ? "Phần thưởng đã nhận" : "Chưa đạt được thành tựu";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            var totalCount = Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Total, 0);
            if (totalCount < milestone.PointsRequired)
            {
                var message = "Không đủ lượt dâng hương yêu cầu";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                return false;
            }

            var randomIndex = Global.GetRandomNumber(0, milestone.Rewards.Count - 1);
            var selectedReward = milestone.Rewards[randomIndex];
            var successClaim = ItemManager.CreateSimpleItem(player, selectedReward.Key, selectedReward.Value, 0, 1);

            if (successClaim)
            {
                SetMilestoneState(player, milestone.ID);
                var message = $"Nhận thưởng thành tựu thành công!";
                PlayerManager.ShowMessageBox(player, "Thông báo", message);
                KTGlobal.SendSystemEventNotification(string.Format(milestone.MessageFormat, player.RoleName));
            }
         
            return successClaim;
        }

        public static List<int> GetCurrentOfferingState(KPlayer player)
        {
            var dailyCount = player.GetValueOfDailyRecore((int)RecoreDef.Offering_CurrentDailyLimit);
            dailyCount = Math.Max(0, dailyCount);
            var result = new List<int>
            {
                Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_Total, 0),
                dailyCount,
                Global.GetRoleParamsInt32FromDB(player, RoleParamName.Offering_TotalMerit, 0),
            };

            foreach (var milestone in Milestones)
            {
                result.Add(GetMilestoneState(player, milestone.ID));
            }

            return result;
        }
    }
}