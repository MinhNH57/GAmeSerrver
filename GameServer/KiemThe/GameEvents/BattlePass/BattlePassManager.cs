﻿using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.GameEvents.TeamBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace GameServer.KiemThe.Core.BattlePass
{
    public class BattlePassManager : IEventListener
    {
        public static string ConfigPath = "Config/KT_Interactive/BattlePassSeason.xml";

        private static BattlePassManager instance = new BattlePassManager();
        public static BattlePassManager getInstance() => instance;

        private static BattlePassConfig config;
        private static Dictionary<int, BattlePassLevel> _levels = new Dictionary<int, BattlePassLevel>();
        private static Dictionary<int, BattlePassTask> _tasks = new Dictionary<int, BattlePassTask>();

        public static void Init()
        {
            LoadConfig();
            SetupEvent();
        }

        private static void LoadConfig()
        {
            using (var stream = File.OpenRead(Global.GameResPath(ConfigPath)))
            {
                var serializer = new XmlSerializer(typeof(BattlePassConfig));
                config = serializer.Deserialize(stream) as BattlePassConfig;
            }

            _levels = (config.Levels ?? new List<BattlePassLevel>()).ToDictionary(x => x.ID, x => x);
            _tasks = (config.Tasks ?? new List<BattlePassTask>()).ToDictionary(x => x.ID, x => x);
        }

        private static void SetupEvent()
        {
            GlobalEventSource eventSource = GlobalEventSource.getInstance();

            // eventSource.registerListener((int)EventTypes.LogOn, getInstance());
            eventSource.registerListener((int)EventTypes.ChucPhuc, getInstance());
            eventSource.registerListener((int)EventTypes.MonsterDead, getInstance());
            eventSource.registerListener((int)EventTypes.CompletedTask, getInstance());
            eventSource.registerListener((int)EventTypes.Join_BachHoDuong, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TieuDaoCoc, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TuLuyenBiCanh, getInstance());
            eventSource.registerListener((int)EventTypes.Join_EmperorTomb, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TranhDoatLanhTho, getInstance());
            eventSource.registerListener((int)EventTypes.Join_ThiDauMonPhai, getInstance());
            eventSource.registerListener((int)EventTypes.Join_DoanHoaDang, getInstance());
            eventSource.registerListener((int)EventTypes.RemoveItem, getInstance());
            eventSource.registerListener((int)EventTypes.Change_CurGatherPoint, getInstance());
            eventSource.registerListener((int)EventTypes.Turn_SeashellCircle, getInstance());
            eventSource.registerListener((int)EventTypes.FamilyFubenJoint, getInstance());
            eventSource.registerListener((int)EventTypes.Compose_Event_Item, getInstance());
            eventSource.registerListener((int)EventTypes.CheckIn_TongKim, getInstance());
            eventSource.registerListener((int)EventTypes.CheckIn_TeamBattle, getInstance());
            eventSource.registerListener((int)EventTypes.Spin_Lucky, getInstance());
        }

        void IEventListener.processEvent(EventObject eventObject)
        {
            switch ((EventTypes)eventObject.getEventType())
            {
                // case EventTypes.LogOn:
                //     PlayerLogonEventObject logonEvent = eventObject as PlayerLogonEventObject;
                //     ResetTaskRecords(logonEvent.player);
                //     ProcessTask(logonEvent.player, EventTypes.LogOn, GetBattlePassData(logonEvent.player), 1);
                //     break;

                case EventTypes.ChucPhuc:
                    ChucPhucEventObject chucphucEvent = eventObject as ChucPhucEventObject;
                    ProcessTask(chucphucEvent.player, EventTypes.ChucPhuc, GetBattlePassData(chucphucEvent.player), 1);
                    break;

                case EventTypes.MonsterDead:
                    MonsterDeadEventObject monsterEvent = eventObject as MonsterDeadEventObject;
                    var attacker = monsterEvent.getAttacker();
                    var monster = monsterEvent.getMonster();

                    if (attacker.Teammates != null && attacker.Teammates.Count > 0)
                    {
                        foreach (var teammate in attacker.Teammates)
                        {
                            ProcessTask(teammate, EventTypes.MonsterDead, GetBattlePassData(teammate), 1, 0, ((int)monster.MonsterType).ToString());
                        }
                    }
                    else
                    {
                        ProcessTask(attacker, EventTypes.MonsterDead, GetBattlePassData(attacker), 1, 0, ((int)monster.MonsterType).ToString());
                    }
                    break;

                case EventTypes.CompletedTask:
                    CompletedTaskEventObject completedTaskEvent = eventObject as CompletedTaskEventObject;
                    ProcessTask(completedTaskEvent.player, EventTypes.CompletedTask, GetBattlePassData(completedTaskEvent.player), 1, 0, ((int)completedTaskEvent.taskClass).ToString());
                    break;

                case EventTypes.Join_BachHoDuong:
                    BachHoDuongJoinEventObject bachHoEvent = eventObject as BachHoDuongJoinEventObject;
                    ProcessTask(bachHoEvent.player, EventTypes.Join_BachHoDuong, GetBattlePassData(bachHoEvent.player), 1, 0, bachHoEvent.IsKuaFuServer ? "1" : "0");
                    break;

                case EventTypes.Join_TieuDaoCoc:
                    TieuDaoCocEventObject tieuDaoCocEvent = eventObject as TieuDaoCocEventObject;
                    ProcessTask(tieuDaoCocEvent.player, EventTypes.Join_TieuDaoCoc, GetBattlePassData(tieuDaoCocEvent.player), 1);
                    break;

                case EventTypes.Join_TuLuyenBiCanh:
                    BiCanhEventObject biCanhEvent = eventObject as BiCanhEventObject;
                    ProcessTask(biCanhEvent.player, EventTypes.Join_TuLuyenBiCanh, GetBattlePassData(biCanhEvent.player), 1);
                    break;

                case EventTypes.Join_EmperorTomb:
                    JoinEmperorTombEventObject tombEvent = eventObject as JoinEmperorTombEventObject;
                    ProcessTask(tombEvent.player, EventTypes.Join_EmperorTomb, GetBattlePassData(tombEvent.player), 1);
                    break;

                case EventTypes.Join_TranhDoatLanhTho:
                    TranhDoatLanhThoEventObject tdltEvent = eventObject as TranhDoatLanhThoEventObject;
                    ProcessTask(tdltEvent.player, EventTypes.Join_TranhDoatLanhTho, GetBattlePassData(tdltEvent.player), 1);
                    break;

                case EventTypes.Join_ThiDauMonPhai:
                    ThiDauMonPhaiJoinEventObject tdmpEvent = eventObject as ThiDauMonPhaiJoinEventObject;
                    ProcessTask(tdmpEvent.player, EventTypes.Join_ThiDauMonPhai, GetBattlePassData(tdmpEvent.player), 1);
                    break;

                case EventTypes.Join_DoanHoaDang:
                    DoanHoaDangEventObject dhdEvent = eventObject as DoanHoaDangEventObject;
                    ProcessTask(dhdEvent.player, EventTypes.Join_DoanHoaDang, GetBattlePassData(dhdEvent.player), 1);
                    break;

                case EventTypes.RemoveItem:
                    RemoveItemEventObject removeItemEvent = eventObject as RemoveItemEventObject;
                    ProcessTask(removeItemEvent.player, EventTypes.RemoveItem, GetBattlePassData(removeItemEvent.player), removeItemEvent.amount, 0, removeItemEvent.itemId.ToString());
                    break;

                case EventTypes.Change_CurGatherPoint:
                    ChangeGatherPointEventObject gatherEvent = eventObject as ChangeGatherPointEventObject;
                    ProcessTask(gatherEvent.player, EventTypes.Change_CurGatherPoint, GetBattlePassData(gatherEvent.player), gatherEvent.amount);
                    break;

                case EventTypes.Turn_SeashellCircle:
                    SeaShellEventObject seaShellEvent = eventObject as SeaShellEventObject;
                    ProcessTask(seaShellEvent.player, EventTypes.Turn_SeashellCircle, GetBattlePassData(seaShellEvent.player), seaShellEvent.count);
                    break;

                case EventTypes.FamilyFubenJoint:
                    FamilyFubenJoinEventObject familyEvent = eventObject as FamilyFubenJoinEventObject;
                    ProcessTask(familyEvent.player, EventTypes.FamilyFubenJoint, GetBattlePassData(familyEvent.player), 1);
                    break;

                case EventTypes.Compose_Event_Item:
                    EventComposeEventObject composeEvent = eventObject as EventComposeEventObject;
                    ProcessTask(composeEvent.player, EventTypes.Compose_Event_Item, GetBattlePassData(composeEvent.player), composeEvent.multiple, 0, composeEvent.itemID.ToString());
                    break;

                case EventTypes.CheckIn_TongKim:
                    TongKimCheckInEventObject checkInTongKimEvent = eventObject as TongKimCheckInEventObject;
                    ProcessTask(checkInTongKimEvent.player, EventTypes.CheckIn_TongKim, GetBattlePassData(checkInTongKimEvent.player), 1, 0, checkInTongKimEvent.IsKuaFuServer ? "1" : "0");
                    break;
                case EventTypes.CheckIn_TeamBattle:
                    CheckInLienDau checkInLienDau = eventObject as CheckInLienDau;

                    if (checkInLienDau.teammates != null && checkInLienDau.teammates.Count > 0)
                    {
                        foreach (var teammate in checkInLienDau.teammates)
                        {
                            ProcessTask(teammate, EventTypes.CheckIn_TeamBattle, GetBattlePassData(teammate), 1, 0, GameManager.IsKuaFuServer ? "1" : "0");
                        }
                    }
                    else
                    {
                        ProcessTask(checkInLienDau.player, EventTypes.CheckIn_TeamBattle, GetBattlePassData(checkInLienDau.player), 1, 0, GameManager.IsKuaFuServer ? "1" : "0");
                    }
                    
                    break;

                case EventTypes.Spin_Lucky:
                    SpinLuckyEventObject spinEvent = eventObject as SpinLuckyEventObject;
                    ProcessTask(spinEvent.player, EventTypes.Spin_Lucky, GetBattlePassData(spinEvent.player), spinEvent.spinAmount);
                    break;
            }
        }

        #region Player Data
        public static string GetBattlePassDataString(KPlayer client)
        {
            //LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' GET_DATA_STRING");
            var bp = GetBattlePassData(client);

            var strdata = bp.GetProgressString() + "|" +
                          bp.GetClaimedString() + "|" +
                          bp.GetTaskRecordString(bp.DailyTasks) + "|" +
                          bp.GetTaskRecordString(bp.WeeklyTasks) + "|" +
                          bp.GetTaskRecordString(bp.SeasonTasks);
            return strdata;
        }

        public static bool ResetPlayerBattlePass(KPlayer client)
        {
            try
            {
                var bp = new BattlePassData
                {
                    Level = 1,
                    Exp = 0,
                    IsElite = false,
                    Claimed = new HashSet<string>(),
                    DailyTasks = new Dictionary<int, int>(),
                    WeeklyTasks = new Dictionary<int, int>(),
                    SeasonTasks = new Dictionary<int, int>()
                };

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassProgress,
                    bp.GetProgressString(), true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassClaimedReward,
                    "", true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassDailyRecord,
                    "", true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassWeeklyRecord,
                    "", true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassSeasonRecord,
                    "", true);

                client.SetValueOfDailyRecore((int)RecoreDef.BattlePass_DailyReset, -1);
                client.SetValueOfDailyRecore((int)RecoreDef.BattlePass_WeekReset, -1);

                // string log = $"BATTLEPASS_RESET: RoleID={client.RoleID}, RoleName='{client.RoleName}'";
                // LogManager.WriteLog(LogTypes.BattlePass, log);

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteException($"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' ERROR - {ex}");
                return false;
            }
        }
        public static void CheckMonthlyReset(KPlayer client)
        {
            var lastResetDay = client.GetValueOfForeverRecore((int)RecoreDef.BattlePass_LastReset);
            var today = DateTime.Now;

            if (lastResetDay != today.Month)
            {
                ResetPlayerBattlePass(client);
                client.SetValueOfForeverRecore((int)RecoreDef.BattlePass_LastReset, today.Month);
                LogManager.WriteLog(LogTypes.BattlePass,
                    $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' MONTHLY_RESET");
            }
        }

        /// <summary>
        /// Reset daily and weekly task records
        /// </summary>
        /// <param name="client"></param>
        public static void ResetTaskRecords(KPlayer client)
        {
            var canResetDaily = client.GetValueOfDailyRecore((int)RecoreDef.BattlePass_DailyReset) == -1;
            var canResetWeekly = client.GetValueOfWeekRecore((int)RecoreDef.BattlePass_WeekReset) == -1;

            if (canResetDaily)
            {
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassDailyRecord,
                    "", true);

                client.SetValueOfDailyRecore((int)RecoreDef.BattlePass_DailyReset, 1);
                LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' DAILY_TASKS_RESET");
            }

            if (canResetWeekly)
            {
                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassWeeklyRecord,
                    "", true);

                client.SetValueOfWeekRecore((int)RecoreDef.BattlePass_WeekReset, 1);
                LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' WEEKLY_TASKS_RESET");
            }
        }

        public static void OnChangeNewDay(KPlayer client)
        {
            CheckMonthlyReset(client);
            ResetTaskRecords(client);
            CheckCompleteAllWeekTasks(client);
        }

        public static BattlePassData GetBattlePassData(KPlayer client)
        {
            var progress = Global.GetRoleParamByName(client, RoleParamName.BattlePassProgress) ?? "";
            var rewardStatus = Global.GetRoleParamByName(client, RoleParamName.BattlePassClaimedReward) ?? "";

            var daily = Global.GetRoleParamByName(client, RoleParamName.BattlePassDailyRecord) ?? "";
            var weekly = Global.GetRoleParamByName(client, RoleParamName.BattlePassWeeklyRecord) ?? "";
            var season = Global.GetRoleParamByName(client, RoleParamName.BattlePassSeasonRecord) ?? "";

            //LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' GET_DATA - Progress='{progress}', Status='{rewardStatus}', Daily='{daily}', Weekly='{weekly}', Season='{season}'");

            var bp = BattlePassData.FromRecords(progress, rewardStatus, daily, weekly, season);

            return bp;
        }

        public static void AddExp(KPlayer client, int amount, BattlePassData bp)
        {
            if (_levels == null || _levels.Count == 0)
            {
                return;
            }

            bp.Exp += amount;

            DistributeExcessExp(client, bp);
        }

        private static void DistributeExcessExp(KPlayer client, BattlePassData bp)
        {
            if (bp == null) return;
            if (_levels == null || _levels.Count == 0) return;

            int maxLevel = _levels.Keys.Max();
            if (bp.Level < 1) bp.Level = 1;
            if (bp.Level > maxLevel) bp.Level = maxLevel;

            while (bp.Level < maxLevel)
            {
                if (!_levels.TryGetValue(bp.Level, out var curLevelCfg))
                {
                    break;
                }

                if (bp.Exp < curLevelCfg.ExpRequired)
                    break;

                bp.Exp -= curLevelCfg.ExpRequired;
                bp.Level++;

                PlayerManager.ShowNotification(client, $"Battle Pass +1 cấp! Hiện tại: {bp.Level}");
                // LogManager.WriteLog(LogTypes.BattlePass,
                //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' LEVEL_UP - NewLevel={bp.Level}, RemainingExp={bp.Exp}");

                if (bp.Level >= maxLevel)
                {
                    bp.Level = maxLevel;
                    break;
                }
            }

            var title = "Quà Bonus Kiếm Vương Lệnh";
            var content = "Hệ thống gửi phần thưởng Rương Bonus Kiếm Vương Lệnh! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";

            if (bp.Level >= maxLevel && bp.Exp >= config.BonusChest.ExpRequired)
            {
                int bonusCount = bp.Exp / config.BonusChest.ExpRequired;
                if (bonusCount > 0)
                {
                    for (int i = 0; i < bonusCount; i++)
                    {
                        SendMailWithRewards(client, config.BonusChest.Rewards, title, content);
                        if (bp.IsElite)
                            SendMailWithRewards(client, config.BonusChest.RewardsElite, title, content);                            
                    }

                    bp.Exp %= config.BonusChest.ExpRequired;

                    PlayerManager.ShowNotification(client, "Vui lòng kiểm tra thư để nhận Rương Bonus Kiếm Vương Lệnh!");

                    // LogManager.WriteLog(LogTypes.BattlePass,
                    //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' BONUS_CHEST - Count={bonusCount}");
                }
            }

            Save(client, bp, "DistributeExcessExp");
        }

        private static void SendMailWithRewards(KPlayer client, List<RewardItem> rewards, string title, string content)
        {
            var goodsData = new List<GoodsData>();
            foreach (var item in rewards)
            {
                var gd = new GoodsData
                {
                    GoodsID = item.ItemID,
                    GCount = item.Number,
                    Binding = 1
                };
                goodsData.Add(gd);
            }
            KTGlobal.SendMail(goodsData, client.RoleID, null, title, content, 0, 0);
        }

        private static void ProcessTask(KPlayer client, EventTypes eventType, BattlePassData bp, int numberItemUsed = 0, int numberItemSet = 0, string param = "-1")
        {
            if (_tasks == null) return;

            // LogManager.WriteLog(LogTypes.BattlePass,
            //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' PROCESS_TASK - EventType={eventType}, NumberItemUsed={numberItemUsed}, NumberItemSet={numberItemSet}, Param='{param}'");

            var matchingTasks = new List<BattlePassTask>();
            foreach (var task in _tasks.Values)
            {
                if (task.EventType != (int)eventType) continue;

                if (task.Param == "-1")
                {
                    matchingTasks.Add(task);
                }
                else
                {
                    var paramParts = task.Param.Split(':');
                    foreach (var part in paramParts)
                    {
                        if (part == param)
                        {
                            matchingTasks.Add(task);
                        }
                    }
                }
            }

            bool taskUpdated = false;
            foreach (var task in matchingTasks)
            {
                Dictionary<int, int> taskDict = null;
                if (task.IndexRepeat == 0) taskDict = bp.DailyTasks;
                else if (task.IndexRepeat == 1) taskDict = bp.WeeklyTasks;
                else if (task.IndexRepeat == 2) taskDict = bp.SeasonTasks;

                if (!taskDict.ContainsKey(task.ID))
                    taskDict[task.ID] = 0;

                var oldProgress = taskDict[task.ID];

                // LogManager.WriteLog(LogTypes.BattlePass,
                //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' TASK_CHECK - TaskID={task.ID}, Name={task.Name}, OldProgress={oldProgress}, Used={numberItemUsed}, Set={numberItemSet}");

                switch (task.Type)
                {
                    case 0:
                        if (oldProgress == 0)
                        {
                            taskDict[task.ID] = 1;
                            AddExp(client, task.Exp, bp);
                            taskUpdated = true;

                            // LogManager.WriteLog(LogTypes.BattlePass,
                            //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' TASK_COMPLETE - TaskID={task.ID}, Name={task.Name}, Type=OneTime, Exp={task.Exp}");
                        }
                        break;

                    case 1:
                        int newProgress = numberItemSet > 0
                            ? Math.Max(oldProgress, numberItemSet)
                            : Math.Min(Math.Max(oldProgress + numberItemUsed, oldProgress), task.NumberItem);

                        taskDict[task.ID] = newProgress;

                        if (oldProgress < task.NumberItem && newProgress >= task.NumberItem)
                            AddExp(client, task.Exp, bp);

                        if (oldProgress != newProgress)
                        {
                            taskUpdated = true;
                            // LogManager.WriteLog(LogTypes.BattlePass,
                            //     $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' TASK_PROGRESS - TaskID={task.ID}, Name={task.Name}, Progress={newProgress}/{task.NumberItem}, Exp={task.Exp}");
                        }
                        break;
                }
            }

            if (taskUpdated)
            {
                Save(client, bp, "ProcessTask");
            }
        }

        public static void ProcessLogOnTask(KPlayer client)
        {
            var bp = GetBattlePassData(client);
            ProcessTask(client, EventTypes.LogOn, bp, 1);
        }

        public static void CheckCompleteAllWeekTasks(KPlayer client)
        {
            var bp = GetBattlePassData(client);
            foreach (var task in _tasks.Values)
            {
                if (task.IndexRepeat != 1) continue;

                if (!bp.WeeklyTasks.ContainsKey(task.ID) || bp.WeeklyTasks[task.ID] < task.NumberItem)
                {
                    return;
                }
            }

            ProcessTask(client, EventTypes.Completed_BPWeekly, bp, 1);
        }

        public static bool SkipTask(KPlayer client, int taskId)
        {
            if (_tasks == null) return false;

            if (!_tasks.TryGetValue(taskId, out var task))
            {
                LogManager.WriteLog(LogTypes.BattlePass,
                    $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' SKIP_FAILED - TaskID={taskId} not found");
                return false;
            }

            var bp = GetBattlePassData(client);

            Dictionary<int, int> taskDict = null;
            if (task.IndexRepeat == 0) taskDict = bp.DailyTasks;
            else if (task.IndexRepeat == 1) taskDict = bp.WeeklyTasks;
            else if (task.IndexRepeat == 2) taskDict = bp.SeasonTasks;

            if (taskDict.TryGetValue(task.ID, out int progress))
            {
                if ((task.Type == 0 && progress >= 1) ||
                    (task.Type == 1 && progress >= task.NumberItem))
                {
                    LogManager.WriteLog(LogTypes.BattlePass,
                        $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' SKIP_FAILED - TaskID={taskId} already completed");
                    return false;
                }
            }

            taskDict[task.ID] = task.NumberItem;
            AddExp(client, task.Exp, bp);
            Save(client, bp, "SkipTask");
            return true;
        }

        public static bool IsElite(KPlayer client)
        {
            var bp = GetBattlePassData(client);
            return bp.IsElite;
        }

        #endregion

        #region Rewards
        public static bool ClaimReward(KPlayer client, int levelID, bool isElite)
        {
            var bp = GetBattlePassData(client);

            if (_levels == null || _levels.Count == 0)
            {
                return false;
            }

            if (!_levels.ContainsKey(levelID) || bp.Level < levelID)
                return false;

            if (isElite && !bp.IsElite)
                return false;

            var claimKey = $"{levelID};{(isElite ? "E" : "F")}";
            if (bp.Claimed.Contains(claimKey))
                return false;

            var levelCfg = _levels[levelID];
            var rewards = isElite ? levelCfg.EliteRewards : levelCfg.FreeRewards;
            if (rewards != null && rewards.Count > 0)
            {
                var ok = GrantRewardItems(client, rewards);
                if (!ok)
                {
                    return false;
                }
            }

            bp.Claimed.Add(claimKey);

            //LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' CLAIM - Level={levelID}, IsElite={isElite}");

            Save(client, bp, "ClaimReward");
            return true;
        }

        public static bool ClaimAllAvailableRewards(KPlayer client)
        {
            if (_levels == null || _levels.Count == 0)
                return false;

            var bp = GetBattlePassData(client);
            bool claimed = false;

            var availableLevels = _levels.Keys.Where(levelId => levelId <= bp.Level).OrderBy(x => x);

            foreach (var levelId in availableLevels)
            {
                var freeClaimKey = $"{levelId};F";
                if (!bp.Claimed.Contains(freeClaimKey))
                {
                    if (ClaimReward(client, levelId, false))
                    {
                        claimed = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (bp.IsElite)
                {
                    var eliteClaimKey = $"{levelId};E";
                    if (!bp.Claimed.Contains(eliteClaimKey))
                    {
                        if (ClaimReward(client, levelId, true))
                        {
                            claimed = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (claimed)
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Đã nhận tất cả phần thưởng!");
                // string log = string.Format("BATTLEPASS_CLAIM_ALL: RoleID={0}, RoleName='{1}', Level={2}, IsElite={3}",
                //     client.RoleID, client.RoleName, bp.Level, bp.IsElite);
                // LogManager.WriteLog(LogTypes.BattlePass, log);
            }
            else
            {
                PlayerManager.ShowNotification(client, "Không có phần thưởng nào để nhận!");
            }

            return claimed;
        }

        private static bool GrantRewardItems(KPlayer client, List<RewardItem> rewards)
        {
            if (rewards == null || rewards.Count == 0) return true;

            if (!KTGlobal.IsHaveSpace(rewards.Count, client))
            {
                PlayerManager.ShowNotification(client,
                    $"Cần tối thiểu {rewards.Count} ô trống trong túi đồ để nhận phần thưởng. Vui lòng dọn dẹp túi đồ và thử lại.");
                return false;
            }

            foreach (var item in rewards)
            {
                var ok = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client,
                    item.ItemID, item.Number, 0, "BATTLEPASS_REWARD", true, 1, false, Global.ConstGoodsEndTime);

                if (!ok)
                {
                    LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' GRANT_FAILED - ItemID={item.ItemID}, Amount={item.Number}");
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Monetization
        public static bool BuyElitePass(KPlayer client, int bundleID)
        {
            if (config?.Bundles == null) return false;

            var bp = GetBattlePassData(client);
            if (bp.IsElite)
            {
                PlayerManager.ShowMessageBox(client, "Thông báo", "Đã sở hữu Elite Pass!");
                return false;
            }

            var bundle = config.Bundles.FirstOrDefault(b => b.ID == bundleID);
            if (bundle == null) return false;

            var bundleRewardCount = bundle.InstantRewards.Count
                                    + bundle.ExclusiveRewards.Count
                                    + bundle.BundleExclusiveRewards.Count;

            if (!KTGlobal.IsHaveSpace(bundleRewardCount + 1, client))
            {
                PlayerManager.ShowNotification(client,
                    $"Cần tối thiểu {bundleRewardCount + 1} ô trống trong túi đồ để nhận phần thưởng Kiếm Vương Lệnh. Vui lòng dọn dẹp túi đồ và thử lại.");
                return false;
            }

            if (bundle.InstantRewards?.Count > 0)
            {
                GrantRewardItems(client, bundle.InstantRewards);
            }

            // if (bundle.ExclusiveRewards?.Count > 0)
            // {
            //     GrantRewardItems(client, bundle.ExclusiveRewards);
            // }

            // if (bundle.BundleExclusiveRewards?.Count > 0)
            // {
            //     GrantRewardItems(client, bundle.BundleExclusiveRewards);
            // }

            bp.IsElite = true;

            int maxLevel = (_levels != null && _levels.Count > 0) ? _levels.Keys.Max() : bp.Level;
            bp.Level = Math.Min(bp.Level + bundle.LevelGain, maxLevel);

            KTGlobal.AddMoney(client, bundle.MoneyBonus, Entities.MoneyType.Dong, "BATTLEPASS_BONUS_MONEY");

            LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' BUY_ELITE - BundleID={bundleID}, Price={bundle.Price}");

            Save(client, bp, "BuyElitePass");
            return true;
        }

        public static bool BuyLevel(KPlayer client)
        {
            if (KTGlobal.GetMoneyhave(client, Entities.MoneyType.Dong) < config.BuyLevelCost)
            {
                PlayerManager.ShowNotification(client, "Không đủ KNB");
                return false;
            }

            var bp = GetBattlePassData(client);
            //int maxLevel = _levels.Keys.Max();
            // if (bp.Level >= maxLevel && !KTGlobal.IsHaveSpace(config.BonusChest.Rewards.Count, client))
            // {
            //     PlayerManager.ShowNotification(client, $"Hãy dọn {config.BonusChest.Rewards.Count} ô túi đồ để nhận rương bonus Kiếm Vương Lệnh!");
            //     return false;
            // }

            KTGlobal.SubMoney(client, config.BuyLevelCost, Entities.MoneyType.Dong, "BATTLEPASS_BUY_LEVEL");
            AddExp(client, config.BuyLevelExp, bp);
            LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' BUY_LEVEL - Cost={config.BuyLevelCost}, ExpGained={config.BuyLevelExp}");

            return true;
        }
        #endregion

        private static void Save(KPlayer client, BattlePassData bp, string reason)
        {
            try
            {
                LogManager.WriteLog(LogTypes.BattlePass, $"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' SAVE - Data: {bp.GetProgressString()}|{bp.GetClaimedString()}|{bp.GetTaskRecordString(bp.DailyTasks)}|{bp.GetTaskRecordString(bp.WeeklyTasks)}|{bp.GetTaskRecordString(bp.SeasonTasks)} - Reason: {reason}");

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassProgress,
                    bp.GetProgressString(), true);

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassClaimedReward,
                    bp.GetClaimedString(), true);

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassDailyRecord,
                    bp.GetTaskRecordString(bp.DailyTasks), true);

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassWeeklyRecord,
                    bp.GetTaskRecordString(bp.WeeklyTasks), true);

                Global.SaveRoleParamsStringToDB(client, RoleParamName.BattlePassSeasonRecord,
                    bp.GetTaskRecordString(bp.SeasonTasks), true);
            }
            catch (Exception ex)
            {
                LogManager.WriteException($"[BATTLEPASS] RoleID={client.RoleID}, RoleName='{client.RoleName}' ERROR - {ex}");
            }
        }

    }

    #region Data Models
    public class BattlePassData
    {
        public int Level = 1;
        public int Exp = 0;
        public bool IsElite = false;

        public HashSet<string> Claimed = new HashSet<string>();

        public Dictionary<int, int> DailyTasks = new Dictionary<int, int>();
        public Dictionary<int, int> WeeklyTasks = new Dictionary<int, int>();
        public Dictionary<int, int> SeasonTasks = new Dictionary<int, int>();

        //"101;1,103;15,104;3"
        public string GetProgressString()
        {
            return $"{Level};{Exp};{(IsElite ? 1 : 0)}";
        }

        //"1;F,1;E,2;F,3;E"
        public string GetClaimedString()
        {
            return string.Join(",", Claimed);
        }

        //"101;1,103;15,104;3"
        public string GetTaskRecordString(Dictionary<int, int> tasks)
        {
            if (tasks == null || tasks.Count == 0) return "";
            var records = tasks.Select(x => $"{x.Key};{x.Value}");
            return string.Join(",", records);
        }

        public static BattlePassData FromRecords(string progress, string rewardStatus, string daily, string weekly, string season)
        {
            var bp = new BattlePassData
            {
                Level = 1,
                Exp = 0,
                IsElite = false,
                Claimed = new HashSet<string>(),
                DailyTasks = new Dictionary<int, int>(),
                WeeklyTasks = new Dictionary<int, int>(),
                SeasonTasks = new Dictionary<int, int>()
            };

            if (!string.IsNullOrEmpty(progress))
            {
                var parts = progress.Split(';');
                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[0], out var lvl))
                        bp.Level = lvl;
                    if (int.TryParse(parts[1], out var exp))
                        bp.Exp = exp;
                    bp.IsElite = parts[2] == "1";
                }
            }

            if (!string.IsNullOrEmpty(rewardStatus))
            {
                bp.Claimed = new HashSet<string>(rewardStatus.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            bp.DailyTasks = ParseTaskRecord(daily);
            bp.WeeklyTasks = ParseTaskRecord(weekly);
            bp.SeasonTasks = ParseTaskRecord(season);

            return bp;
        }

        private static Dictionary<int, int> ParseTaskRecord(string record)
        {
            var result = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(record))
            {
                var pairs = record.Split(',');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split(';');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var k) && int.TryParse(parts[1], out var v))
                    {
                        result[k] = v;
                    }
                }
            }
            return result;
        }
    }

    public class BattlePassBundle
    {
        [XmlAttribute] public int ID;
        [XmlAttribute] public string Title;
        [XmlAttribute] public int MoneyBonus;
        [XmlAttribute] public int LevelGain;
        [XmlAttribute] public float Price;
        [XmlAttribute] public string Currency;

        [XmlElement("InstantReward")]
        public List<RewardItem> InstantRewards = new List<RewardItem>();

        [XmlElement("ExclusiveReward")]
        public List<RewardItem> ExclusiveRewards = new List<RewardItem>();

        [XmlElement("BundleExclusiveReward")]
        public List<RewardItem> BundleExclusiveRewards = new List<RewardItem>();
    }

    [XmlRoot("BattlePass")]
    public class BattlePassConfig
    {
        [XmlAttribute] public string SeasonID;
        [XmlAttribute] public int BuyLevelCost;
        [XmlAttribute] public int BuyLevelExp;

        [XmlArray("Levels")]
        [XmlArrayItem("Level")]
        public List<BattlePassLevel> Levels;

        [XmlArray("Tasks")]
        [XmlArrayItem("Task")]
        public List<BattlePassTask> Tasks;

        [XmlArray("Bundles")]
        [XmlArrayItem("Bundle")]
        public List<BattlePassBundle> Bundles;

        [XmlElement("BonusChest")]
        public BattlePassBonus BonusChest;
    }

    public class BattlePassLevel
    {
        [XmlAttribute] public int ID;
        [XmlAttribute] public int ExpRequired;

        [XmlElement("FreeReward")]
        public List<RewardItem> FreeRewards;
        [XmlElement("EliteReward")]
        public List<RewardItem> EliteRewards;
    }

    public class BattlePassTask
    {
        [XmlAttribute] public int ID;
        [XmlAttribute] public string Name;
        [XmlAttribute] public int Type;
        [XmlAttribute] public int NumberItem;
        [XmlAttribute] public int IndexRepeat;

        [XmlAttribute] public int EventType;
        [XmlAttribute] public string Param;
        [XmlAttribute] public int Exp;
    }

    public class BattlePassBonus
    {
        [XmlAttribute] public int ExpRequired;
        [XmlElement("Reward")]
        public List<RewardItem> Rewards;

        [XmlElement("RewardElite")]
        public List<RewardItem> RewardsElite;
    }

    public class RewardItem
    {
        [XmlAttribute] public int ItemID;
        [XmlAttribute] public int Number;
    }
    #endregion
}
