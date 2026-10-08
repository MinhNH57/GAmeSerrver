using GameServer.KiemThe.GameEvents.BaiHuTang;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.KnowledgeChallenge;
using GameServer.KiemThe.GameEvents.LotteryEvent;
using GameServer.KiemThe.GameEvents.Model;
using GameServer.KiemThe.GameEvents.TeamBattle;
using GameServer.KiemThe.Logic;
using System;
using System.Collections.Concurrent;
using GameServer.KiemThe.GameEvents.OTT;
using GameServer.KiemThe.GameEvents.PetRace;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.GameEvents.BossUnicon;
using GameServer.KiemThe.GameEvents.FlowerBet;
using GameServer.Logic;

namespace GameServer.KiemThe.GameEvents
{
    /// <summary>
    /// Khai báo các hoạt động trong Game
    /// </summary>
    public static partial class GameMapEventsManager
    {
        /// <summary>
        /// Danh sách Script đang thực thi
        /// </summary>
        private static readonly ConcurrentDictionary<int, IActivityScript> activityScripts = new ConcurrentDictionary<int, IActivityScript>();

        /// <summary>
        /// Bắt đầu hoạt động tương ứng
        /// </summary>
        /// <param name="activity"></param>
        public static IActivityScript StartGameMapEvent(KTActivity activity)
        {
            /// Đánh dấu đã bắt đầu hoạt động
            activity.IsStarted = true;
            switch (activity.Data.CoreScript)
            {
                /// Nếu là Bạch Hổ Đường
                case "BaiHuTang":
                {
                    BaiHuTang_ActivityScript script = new BaiHuTang_ActivityScript();
                    script.Activity = activity;
                    /// Thêm sự kiện vào danh sách
                    GameMapEventsManager.activityScripts[activity.Data.ID] = script;
                    script.Begin();
                    return script;
                }
                /// Nếu là Võ Lâm Liên Đấu
                case "TeamBattle":
                {
                    TeamBattle_ActivityScript.NotifyActivity(activity.Data.ID);
                    break;
                }
                /// Nếu là Đoán hoa đăng
                case "KnowledgeChallenge":
                {
                    KnowledgeChallenge_ActivityScript script = new KnowledgeChallenge_ActivityScript();
                    script.Activity = activity;
                    /// Thêm sự kiện vào danh sách
                    GameMapEventsManager.activityScripts[activity.Data.ID] = script;
                    script.Begin();
                    return script;
                }
                /// Nếu là Tần Lăng
                case "EmperorTomb":
                {
                    EmperorTomb_ActivityScript script = new EmperorTomb_ActivityScript();
                    script.Activity = activity;
                    /// Thêm sự kiện vào danh sách
                    GameMapEventsManager.activityScripts[activity.Data.ID] = script;
                    script.Begin();
                    return script;
                }
                case "PetRace":
                {
                    var script = new PetRace_ActivityScript
                    {
                        Activity = activity
                    };
                    activityScripts[activity.Data.ID] = script;
                    script.Begin();
                    return script;
                }
                case "OTT":
                {
                    var script = new OTT_ActivityScript
                    {
                        Activity = activity
                    };
                    activityScripts[activity.Data.ID] = script;
                    script.Begin();
                    return script;
                }
                //// Quay số trúng thưởng
                //case "LotteryEvent":
                //    {
                //        LotteryManager.StartEvent(activity.Data.ID);
                //        break;
                //    }
                case "DaoTac":
                {
                    switch (activity.Data.ID) {
                        case 700:
                            KTActivityManager.StopActivity(701);
                            KTActivityManager.StopActivity(702);
                            Event_2024Apr.NotifyPrepare();
                            break;
                        case 701:
                            KTActivityManager.StopActivity(700);
                            Event_2024Apr.StartBossesWave();
                            break;
                        case 702:
                            KTActivityManager.StopActivity(701);
                            Event_2024Apr.EndBossesWave();
                            break;
                    }
                    break;
                }
                case "BossUnicon":
                    {
                        BossUnicon_ActivityScript script = new BossUnicon_ActivityScript();
                        script.Activity = activity;
                        GameMapEventsManager.activityScripts[activity.Data.ID] = script;
                        script.Begin();
                        return script;
                    }
                case "FlowerBet":
                    {
                        switch (activity.Data.ID)
                        {
                            case 800:
                                KTActivityManager.StopActivity(801);
                                KTActivityManager.StopActivity(802);
                                FlowerBetManager.StartFlowerBet(activity.Data.DurationTicks);
                                break;
                            case 801:
                                KTActivityManager.StopActivity(800);
                                FlowerBetManager.RewardFlowerBet(activity.Data.DurationTicks);
                                break;
                            case 802:
                                KTActivityManager.StopActivity(801);
                                FlowerBetManager.EndFlowerBet();
                                break;
                        }
                        break;
                    }
                case "BackVillage":
                    {
                        
                        if (GameManager.IsKuaFuServer)
                        {
                            KTGlobal.TakePlayersBackVillage();   
                        }
                        break;
                    }
            }

            return null;
        }

        /// <summary>
        /// Ngừng hoạt động tương ứng
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static void StopGameMapEvent(KTActivity activity)
		{
            /// Nếu không tồn tại thì thôi
            if (!GameMapEventsManager.activityScripts.TryGetValue(activity.Data.ID, out IActivityScript script))
            {
                return;
            }

            /// Thực hiện đóng sự kiện
            script.Close();
            /// Xóa khỏi danh sách
            GameMapEventsManager.activityScripts.TryRemove(activity.Data.ID, out _);
        }
    }
}
