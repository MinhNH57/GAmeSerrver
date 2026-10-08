using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.CopySceneEvents;
using GameServer.KiemThe.Core.Activity.EveryDayOnlineEvent;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.GameEvents.BaiHuTang;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.GameEvents.TeamBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Battle;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static GameServer.KiemThe.CopySceneEvents.YouLongGe.YouLong.AwardInfo;

namespace GameServer.KiemThe.Core.Activity.EveryDayActivity
{
    /// <summary>
    /// Điểm năng động
    /// </summary>
    public class DayActivityManager:IEventListener
    {
        private static ActivityInfo _ActivityInfo;
        public static string KTDayActivities = "Config/KT_Activity/KTDayActivities.xml";
        private static DayActivityManager instance = new DayActivityManager();

        public static DayActivityManager getInstance()
        {
            return instance;
        }

        public static void Setup()
        {
            SetupEvent();
            string FileMisson = Global.GameResPath(KTDayActivities);

            using (var stream = System.IO.File.OpenRead(FileMisson))
            {
                var serializer = new XmlSerializer(typeof(ActivityInfo));
                _ActivityInfo = serializer.Deserialize(stream) as ActivityInfo;
            }
            _DayActivity = _ActivityInfo._ActivityEvent;
            _DayActivityCollect = _ActivityInfo._DayActivityCollect;
            _MonthActivityCollect = _ActivityInfo._MonthActivityCollect;

        }

        /// <summary>
        /// Khởi tạo data khi người chơi đăng nhập vào game, có rồi thì dùng cái cũ
        /// </summary>
        public static void InitData(KPlayer client)
        {
            
            int nDate = TimeUtil.NowDateTime().DayOfYear;
            if (client._DailyDataRecoreActivity.DayID != nDate)
            {
                ResetDay(client);
            }
            else
            {
                GetAllInfoActivity(client);
            }
        }

        public static void SetupEvent()
        {
            GlobalEventSource eventSource = GlobalEventSource.getInstance();
            eventSource.registerListener((int)EventTypes.LogOn, getInstance());
            eventSource.registerListener((int)EventTypes.FamilyFubenJoint, getInstance());
            eventSource.registerListener((int)EventTypes.ChucPhuc, getInstance());
            eventSource.registerListener((int)EventTypes.RemoveItem, getInstance());
            eventSource.registerListener((int)EventTypes.MonsterDead, getInstance());
            eventSource.registerListener((int)EventTypes.PlayEnterMap, getInstance());
            eventSource.registerListener((int)EventTypes.Join_DoanHoaDang, getInstance());
            eventSource.registerListener((int)EventTypes.CompletedTask, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TieuDaoCoc, getInstance());
            eventSource.registerListener((int)EventTypes.Join_BachHoDuong, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TuLuyenBiCanh, getInstance());
            eventSource.registerListener((int)EventTypes.Join_DuLongChienThu, getInstance());
            eventSource.registerListener((int)EventTypes.Move_ToArena, getInstance());
            eventSource.registerListener((int)EventTypes.Change_CurGatherPoint, getInstance());
            eventSource.registerListener((int)EventTypes.Join_ThiDauMonPhai, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TranhDoatLanhTho, getInstance());
            eventSource.registerListener((int)EventTypes.Join_LeagueBattle, getInstance());

        }


        void IEventListener.processEvent(EventObject eventObject)
        {
            switch ((EventTypes)eventObject.getEventType()) 
            {
                case EventTypes.LogOn:
                    if(eventObject is PlayerLogonEventObject logonEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.LOG_ON, logonEvent.player);
                    }
                    break;
                case EventTypes.FamilyFubenJoint:
                    if(eventObject is FamilyFubenJoinEventObject giaTocEvent)
                        CheckMissionDone((int)ActivityDayName.VUOT_AI_GIA_TOC, giaTocEvent.player);
                    break;
                case EventTypes.ChucPhuc:
                    if(eventObject is ChucPhucEventObject chucPhucEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.CHUCPHUC_1, chucPhucEvent.player);
                        CheckMissionDone((int)ActivityDayName.CHUCPHUC_5, chucPhucEvent.player,1);
                    }
                    break;
                case EventTypes.RemoveItem:
                    if(eventObject is RemoveItemEventObject removeItemEvent)
                    {
                        if(removeItemEvent.itemId == 337)
                        {
                            CheckMissionDone((int)ActivityDayName.MO_TUI_PHUC, removeItemEvent.player, removeItemEvent.amount);
                        }
                        if(removeItemEvent.itemId == 1031)
                        {
                            CheckMissionDone((int)ActivityDayName.TRUNG_DU_LONG, removeItemEvent.player, removeItemEvent.amount);
                            
                        }
                    }
                    break;
                case EventTypes.MonsterDead:
                    if (eventObject is MonsterDeadEventObject monsterDeadEvent)
                    {
                        Monster monster = monsterDeadEvent.getMonster();
                        KPlayer player = monsterDeadEvent.getAttacker();

                        if (monster.MonsterType == MonsterAIType.Elite || monster.MonsterType == MonsterAIType.Leader)
                        {
                            CheckMissionDone((int)ActivityDayName.TINH_ANH_THU_LINH, player, 1);
                        }
                        if(monster.IsWorldBoss())
                        {
                         
                            if (player.Teammates == null || player.Teammates.Count == 0)
                                CheckMissionDone((int)ActivityDayName.BOSS_CAO_THU, player);
                            else
                            {
                                foreach (KPlayer _player in player.Teammates)
                                {
                                    CheckMissionDone((int)ActivityDayName.BOSS_CAO_THU, _player);
                                }
                            }
                        }
                    }
                    
                    break;
                case EventTypes.PlayEnterMap:
                    if(eventObject is PlayerEnterMap enterMapEvent)
                    {
                        if(enterMapEvent.mapId == EmperorTomb.Event.Relive.MapID)
                        {
                            CheckMissionDone((int)ActivityDayName.TAN_LANG, enterMapEvent.player);
                        }
                        if(enterMapEvent.mapId == TeamBattle.Config.EnterMapID)
                        {
                            CheckMissionDone((int)ActivityDayName.LIEN_DAU, enterMapEvent.player);
                        }
                    }
                    break;
                case EventTypes.Join_DoanHoaDang:
                    if (eventObject is DoanHoaDangEventObject hoaDangEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.HOA_DANG, hoaDangEvent.player);
                    }
                    break;
                case EventTypes.CompletedTask:
                    if (eventObject is CompletedTaskEventObject completedTaskEvent)
                    {
                        switch(completedTaskEvent.taskClass)
                        {
                            case TaskClasses.HaiTac:
                                CheckMissionDone((int)ActivityDayName.TRUY_NA, completedTaskEvent.player, 1);
                                break;
                            case TaskClasses.NghiaQuan:
                                CheckMissionDone((int)ActivityDayName.BA0_VAN_DONG_10, completedTaskEvent.player, 1);
                                CheckMissionDone((int)ActivityDayName.BA0_VAN_DONG_50, completedTaskEvent.player, 1);
                                break;
                        }

                    }
                    break;
                case EventTypes.Join_TieuDaoCoc:
                    if (eventObject is TieuDaoCocEventObject tieuDaoCocEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.TIEU_DAO_COC, tieuDaoCocEvent.player);
                    }
                    break;
                case EventTypes.Join_BachHoDuong:
                    if (eventObject is BachHoDuongJoinEventObject bachHoDuongEvent)
                    {
                        if(GameManager.IsKuaFuServer)
                            CheckMissionDone((int)ActivityDayName.BACH_HO_DUONG_LIEN_SERVER, bachHoDuongEvent.player);
                        else
                            CheckMissionDone((int)ActivityDayName.BACH_HO_DUONG, bachHoDuongEvent.player);
                    }
                    break;
                case EventTypes.Join_TuLuyenBiCanh:
                    if(eventObject is BiCanhEventObject tuLuyenBiCanhEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.TU_LUYEN_BI_CANH, tuLuyenBiCanhEvent.player);
                    }
                    break;
                case EventTypes.Join_DuLongChienThu:
                    if (eventObject is DuLongChienThuEventObject duLongChienThuEvent) 
                    {
                        CheckMissionDone((int)ActivityDayName.DU_LONG_CHIEN_THU, duLongChienThuEvent.player, 1);
                    }
                    break;
                case EventTypes.Move_ToArena:
                    if (eventObject is MoveToArenaEventObject moveToArenaEvent) 
                    {
                        if(GameManager.IsKuaFuServer)
                            CheckMissionDone((int)ActivityDayName.TONG_KIM, moveToArenaEvent.player);
                    }
                    break;
                case EventTypes.Change_CurGatherPoint:
                    if(eventObject is ChangeGatherPointEventObject changeGatherPointEvent) 
                    {
                        CheckMissionDone((int)ActivityDayName.TINH_LUC_HOAT_LUC, changeGatherPointEvent.player, 0, changeGatherPointEvent.repeat);
                    }
                    break;
                case EventTypes.Join_ThiDauMonPhai:
                    if(eventObject is ThiDauMonPhaiJoinEventObject thiDauMonPhaiEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.THI_DAU_MON_PHAI, thiDauMonPhaiEvent.player);
                    }
                    break;
                case EventTypes.Join_TranhDoatLanhTho:
                    if (eventObject is TranhDoatLanhThoEventObject tranhDoatLanhThoEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.TRANH_DOAT_LANH_THO, tranhDoatLanhThoEvent.player);
                    }
                    break;
                case EventTypes.Join_LeagueBattle:
                    if(eventObject is ThamGiaGiaiDauEventObject thamGiaGiaiDauEvent)
                    {
                        CheckMissionDone((int)ActivityDayName.GIAI_DAU, thamGiaGiaiDauEvent.player);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Làm mới nhiệm vụ và điểm khi qua ngày mới, check xem đã đến tháng mới chưa thì reset nốt
        /// </summary>
        public static void ResetDay(KPlayer client)
        {
            LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} reset điểm năng động ngày, ngày cũ {2} điểm ngày cũ {3}, ngày mới {4} ", client.RoleID, client.RoleName, client._DailyDataRecoreActivity.DayID, client._DailyDataRecoreActivity.ScoreDay, TimeUtil.NowDateTime().DayOfYear));
            client._DailyDataRecoreActivity.DayID = TimeUtil.NowDateTime().DayOfYear;
            client._DailyDataRecoreActivity.EventActivityRecoding = new Dictionary<int, DayActivity>();
            client._DailyDataRecoreActivity.ScoreDay = 0;
            client._DailyDataRecoreActivity.DayActivityAwardStep = new Dictionary<int, bool>();
            client._DailyDataRecoreActivity.DayActivityAwardGoodsID = "";

            // check xem đã qua tháng mới chưa ID = 0 thì có thể chưa nhận dữ liệu nên bỏ qua
            int nMonth = TimeUtil.NowDateTime().Month;
            if ((client._MonthDataRecore == null || client._MonthDataRecore.MonthID != nMonth) && client._MonthDataRecore.MonthID != 0)
            {
                LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} reset điểm năng động tháng, tháng cũ {2} điểm tháng cũ {3}, tháng mới {4}", client.RoleID, client.RoleName, client._MonthDataRecore.MonthID, client._MonthDataRecore.ScoreMonth, nMonth));
                client._MonthDataRecore.MonthID = nMonth;
                client._MonthDataRecore.ScoreMonth = 0;
                client._MonthDataRecore.MonthActivityAwardStep = new Dictionary<int, bool>();
                client._MonthDataRecore.MonthActivityAwardGoodsID = "";
                client._MonthDataRecore.EventRecoding = new Dictionary<int, int>();
            }

            GetAllInfoActivity(client);
            GlobalEventSource.getInstance().fireEvent(new PlayerLogonEventObject(client));
        }
        /// <summary>
        /// reset lại danh sách nhiệm vụ (GM)
        /// </summary>
        public static void ResetMission(KPlayer client)
        {
            client._DailyDataRecoreActivity.EventActivityRecoding = new Dictionary<int, DayActivity>();
            var dataDaily = client._DailyDataRecoreActivity;

            var dayActivities = new List<DayActivity>();
            // Tạo mới luôn danh sách nhiệm vụ khác
            foreach (var m in _DayActivity.dayActivities)
            {
                dayActivities.Add(new DayActivity()
                {
                    ID = m.ID,
                    Type = m.Type,
                    NumberItem = m.NumberItem,
                    Score = m.Score,
                    Repeat = m.Repeat,
                    IndexRepeat = m.IndexRepeat,
                    isDone = false,
                    numberItemUsed = 0,
                    indexComplete = 0,
                });
            }
            if (dataDaily.EventActivityRecoding.Count == 0)
            {
                foreach (var d in dayActivities)
                {
                    // Kiểm tra 1 lần nhỡ đâu có key rồi thì lấy ra dữ liệu mới
                    if (dataDaily.EventActivityRecoding.ContainsKey(d.ID))
                    {
                        dataDaily.EventActivityRecoding[d.ID] = d;
                    }
                    else
                    {
                        dataDaily.EventActivityRecoding.Add(d.ID, d);
                    }
                }
            }
            else
            {
                if (dataDaily.EventActivityRecoding.Count < _DayActivity.dayActivities.Count)
                {
                    foreach (DayActivity d in _DayActivity.dayActivities)
                    {
                        if (!dataDaily.EventActivityRecoding.Keys.Contains(d.ID))
                        {
                            d.isDone = false;
                            d.numberItemUsed = 0;
                            d.indexComplete = 0;
                            dataDaily.EventActivityRecoding.Add(d.ID, d);
                        }
                    }
                }
                else if (dataDaily.EventActivityRecoding.Count > _DayActivity.dayActivities.Count)
                {
                    var IDMissionSaveOld = new List<int>();
                    foreach (var value in dataDaily.EventActivityRecoding.Values)
                    {
                        IDMissionSaveOld.Add(value.ID);
                    }
                    foreach (var id in IDMissionSaveOld)
                    {
                        var mission = _DayActivity.dayActivities.FirstOrDefault(x => x.ID == id);
                        if (mission == null)
                        {
                            dataDaily.EventActivityRecoding.Remove(id);
                        }
                        else
                        {
                            dataDaily.EventActivityRecoding[id].Score = mission.Score;
                            dataDaily.EventActivityRecoding[id].Type = mission.Type;
                            dataDaily.EventActivityRecoding[id].NumberItem = mission.NumberItem;

                        }
                    }
                }
            }
        }
        #region Quản lý nhiệm vụ

        public static ActivityEvent _DayActivity = new ActivityEvent();

        //public static List<DayActivity> _AllMissionCanWork { } = new List<DayActivity>();

        /// <summary>
        /// Trả về toàn bộ nhiệm vụ
        /// </summary>
        public static List<DayActivity> _GetAllMission(DailyDataRecoreActivity dataDaily)
        {
            List<DayActivity> _Total = new List<DayActivity>();

            foreach(var mission in dataDaily.EventActivityRecoding.Values)
            {
                _Total.Add(mission);
            }

            return _Total;
        }

        /// <summary>
        /// Lấy ra nhiệm vụ theo ID (enum ở dưới)
        /// </summary>
        private static DayActivity GetValueMission(KPlayer client, int ID) 
        {
            // check tránh tình trạng lỗi reset
            if (client._DailyDataRecoreActivity.DayID == 0)
                return null;

            List<DayActivity> _Total = _GetAllMission(client._DailyDataRecoreActivity);
            DayActivity activity = _Total.Where(x => x.ID == ID).FirstOrDefault();

            //Kiểm tra xem qua ngày mới chưa thì reset
            int nDate = TimeUtil.NowDateTime().DayOfYear;
            if (client._DailyDataRecoreActivity.DayID != nDate)
            {
                ResetDay(client);
                // Lấy lại thông tin nhiệm vụ
                _Total = _GetAllMission(client._DailyDataRecoreActivity);
                activity = _Total.Where(x => x.ID == ID).FirstOrDefault();
            }

            return activity;
        }
        /// <summary>
        /// Lấy ra toàn bộ danh sách nhiệm vụ  
        /// </summary>
        public static void GetAllInfoActivity(KPlayer client)
        {
            try 
            {
                DailyDataRecoreActivity dataDaily = client._DailyDataRecoreActivity;
                MonthDataRecore dataMonth = client._MonthDataRecore;
                var dayActivities = new List<DayActivity>();
                
                // Tạo mới luôn danh sách nhiệm vụ khác từ file config
                foreach(var m in _DayActivity.dayActivities)
                {
                    dayActivities.Add(new DayActivity()
                    {
                        ID = m.ID,
                        Type = m.Type,
                        NumberItem= m.NumberItem,
                        Score = m.Score,
                        Repeat= m.Repeat,
                        IndexRepeat= m.IndexRepeat,
                        isDone = false,
                        numberItemUsed = 0,
                        indexComplete = 0,
                    });
                }
                // Nếu reset rồi thì khởi tạo danh sách
                if (dataDaily.EventActivityRecoding.Count == 0)
                {
                    foreach(var d in dayActivities)
                    {
                        var strdata = "";
                        // Kiểm tra 1 lần nhỡ đâu có key rồi thì lấy ra dữ liệu mới
                        if (dataDaily.EventActivityRecoding.ContainsKey(d.ID))
                        {
                            dataDaily.EventActivityRecoding[d.ID] = d;
                        }
                        else
                        {
                            dataDaily.EventActivityRecoding.Add(d.ID, d);
                        }
                        if (d.Type == 1)
                        {
                            strdata = $" đã sử dụng {d.numberItemUsed}";
                        }
                        LogManager.WriteLog(LogTypes.DiemNangDong, $"Player({client.RoleID}/{client.RoleName}) Reset nv: {d.ID}, hoàn thành: {d.isDone}" + strdata);
                    }
                }
                else
                {
                    // có danh sách rồi nhưng file config cật nhật thêm, xóa nhiệm vụ check để them hoặc xóa nhiệm vụ đấy đi
                    if (dataDaily.EventActivityRecoding.Count < _DayActivity.dayActivities.Count)
                    {
                        foreach (DayActivity d in _DayActivity.dayActivities)
                        {
                            if (!dataDaily.EventActivityRecoding.Keys.Contains(d.ID))
                            {
                                d.isDone = false;
                                d.numberItemUsed = 0;
                                d.indexComplete = 0;
                                dataDaily.EventActivityRecoding.Add(d.ID, d);
                            }
                        }
                    }
                    else if (dataDaily.EventActivityRecoding.Count > _DayActivity.dayActivities.Count)
                    {
                        var IDMissionSaveOld = new List<int>();
                        foreach (var value in dataDaily.EventActivityRecoding.Values)
                        {
                            IDMissionSaveOld.Add(value.ID);
                        }
                        foreach(var id in IDMissionSaveOld)
                        {
                            var mission = _DayActivity.dayActivities.FirstOrDefault(x => x.ID == id);
                            if(mission == null)
                            {
                                dataDaily.EventActivityRecoding.Remove(id);
                            }
                            else
                            {
                                dataDaily.EventActivityRecoding[id].Score = mission.Score;
                                dataDaily.EventActivityRecoding[id].Type = mission.Type;
                                dataDaily.EventActivityRecoding[id].NumberItem = mission.NumberItem;

                            }
                        }
                    }
                }
                

                // Thêm vào danh sách các mốc thưởng có thể nhận được
                // Nếu mới reset ngày thì khởi tạo lại mốc thưởng
                if (dataDaily.DayActivityAwardStep == null || dataDaily.DayActivityAwardStep.Count == 0)
                {
                    // sửa để tránh bị exception add nhiều lần do bị gọi ở nhiều chỗ chưa rõ ???
                    foreach (var step in _DayActivityCollect.Item)
                    {
                        if(dataDaily.DayActivityAwardStep.ContainsKey(step.StepID - 1))
                        {
                            dataDaily.DayActivityAwardStep[step.StepID - 1] = false;
                        }
                        else
                        {
                            dataDaily.DayActivityAwardStep.Add(step.StepID - 1, false);
                        }

                    }
                }
                // Cũng check có thêm hay xóa mốc thưởng không
                else if (dataDaily.DayActivityAwardStep.Count < _DayActivityCollect.Item.Count)
                {
                    foreach (var award in _DayActivityCollect.Item)
                    {
                        if (!dataDaily.DayActivityAwardStep.ContainsKey(award.StepID - 1))
                        {
                            dataDaily.DayActivityAwardStep.Add(award.StepID - 1, false);
                        }
                    }
                }
                else if (dataDaily.DayActivityAwardStep.Count > _DayActivityCollect.Item.Count)
                {
                    var keys = new List<int>();

                    foreach(var id in dataDaily.DayActivityAwardStep.Keys)
                    {
                        keys.Add(id);
                    }

                    foreach (var key in keys)
                    {
                        var award = _DayActivityCollect.Item.FirstOrDefault(x => x.StepID == key + 1);
                        if (award == null)
                            dataDaily.DayActivityAwardStep.Remove(key);
                    }
                }
                // Mốc thưởng tháng cũng giống thưởng ngày
                if (dataMonth.MonthActivityAwardStep == null || dataMonth.MonthActivityAwardStep.Count == 0)
                {
                    foreach(var step in _MonthActivityCollect.Item)
                    {
                        if (dataMonth.MonthActivityAwardStep.ContainsKey(step.StepID - 1))
                        {
                            dataMonth.MonthActivityAwardStep[step.StepID - 1] = false;
                        }
                        else
                        {
                            dataMonth.MonthActivityAwardStep.Add(step.StepID - 1, false);
                        }
                    }
                }
                else if (dataMonth.MonthActivityAwardStep.Count < _MonthActivityCollect.Item.Count)
                {
                    foreach(var award in _MonthActivityCollect.Item)
                    {
                        if (!dataMonth.MonthActivityAwardStep.ContainsKey(award.StepID -1))
                        {
                            dataMonth.MonthActivityAwardStep.Add(award.StepID - 1, false);
                        }
                    }
                }
                else if (dataMonth.MonthActivityAwardStep.Count > _MonthActivityCollect.Item.Count)
                {
                    var keys = new List<int>();

                    foreach (var id in dataMonth.MonthActivityAwardStep.Keys)
                    {
                        keys.Add(id);
                    }

                    foreach (var key in keys)
                    {
                        var award = _MonthActivityCollect.Item.FirstOrDefault(x => x.StepID == key + 1);
                        if(award == null)
                            dataMonth.MonthActivityAwardStep.Remove(key);
                    }
                }
                if (dataMonth.ScoreMonth < dataDaily.ScoreDay)
                    dataMonth.ScoreMonth = dataDaily.ScoreDay;
            } catch (Exception e) 
            {
                LogManager.WriteException(e.ToString());
            }

        }

        /// <summary>
        /// Thêm dữ liệu khi làm nhiệm vụ  
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="client"></param>
        /// <param name="numberItemUsed"></param>
        /// <returns></returns>
        public static void CheckMissionDone(int ID, KPlayer client, int numberItemUsed = 0, int numberRepeat = 0)
        {
            //List<DayActivity> _Total = _GetAllMission(client._DailyDataRecoreActivity);
            //DayActivity activity = _Total.Where(x => x.ID == ID).FirstOrDefault();

            DayActivity activity = GetValueMission(client, ID);
            if (activity == null || activity.isDone)
            {
                return;
            }

            int ScoreDayOld = client._DailyDataRecoreActivity.ScoreDay;
            int ScoreMonthOld = client._MonthDataRecore.ScoreMonth;
            int numberUsedOld = activity.numberItemUsed;
            switch (activity.Type)
            {
                // Loại làm 1 lần là xong VD: đăng nhập
                case 0:
                    client._DailyDataRecoreActivity.ScoreDay += activity.Score;
                    client._MonthDataRecore.ScoreMonth += activity.Score;
                    if (activity.Repeat == 1)
                    {
                        activity.indexComplete++;
                        if (activity.IndexRepeat <= activity.indexComplete)
                            activity.isDone = true;
                    }
                    else
                    {
                        activity.isDone = true;
                    }
                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID:{0}, tên:{1} làm xong nhiệm vụ {2}, điểm {3}; điểm ngày cũ {4}, điểm ngày mới {5}; điểm tháng cũ {6}, điểm tháng mới {7}",
                                        client.RoleID, client.RoleName, activity.ID, activity.Score, ScoreDayOld, client._DailyDataRecoreActivity.ScoreDay, ScoreMonthOld, client._MonthDataRecore.ScoreMonth));
                    break;
                // Loại làm cần sử dụng nhiều item VD sử dụng 20 túi phúc
                case 1:
                    activity.numberItemUsed += numberItemUsed;
                    if (activity.numberItemUsed >= activity.NumberItem)
                    {
                        client._DailyDataRecoreActivity.ScoreDay += activity.Score;
                        client._MonthDataRecore.ScoreMonth += activity.Score;
                        activity.numberItemUsed = 0;
                        if (activity.Repeat == 1)
                        {
                            activity.indexComplete++;
                            if (activity.IndexRepeat <= activity.indexComplete)
                                activity.isDone = true;
                        }
                        else
                        {
                            activity.isDone = true;
                        }
                        LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} làm xong nhiệm vụ {2}, điểm {3}; điểm ngày cũ {4}, điểm ngày mới {5}; điểm tháng cũ {6}, điểm tháng mới {7}",
                                                                   client.RoleID, client.RoleName, activity.ID, activity.Score, ScoreDayOld, client._DailyDataRecoreActivity.ScoreDay, ScoreMonthOld, client._MonthDataRecore.ScoreMonth));
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0},tên: {1} đang làm nhiệm vụ {2}, đã sử dụng {3} vật phẩm, mới sử dụng thêm {4}, hiện tại {5}",
                                            client.RoleID, client.RoleName, activity.ID, numberUsedOld, numberItemUsed, activity.numberItemUsed));
                    }
                    break;
                // Loại làm xong cần làm 2 nhiệm vụ cùng lúc VD Nhận cả Tinh lực, Hoạt lực
                case 2:
                    activity.indexComplete += numberRepeat;
                    if (activity.indexComplete == activity.IndexRepeat)
                    {
                        activity.isDone = true;
                        client._DailyDataRecoreActivity.ScoreDay += activity.Score;
                        client._MonthDataRecore.ScoreMonth += activity.Score;
                        LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} làm xong nhiệm vụ {2}, điểm {3}; điểm ngày cũ {4}, điểm ngày mới {5}; điểm tháng cũ {6}, điểm tháng mới {7}",
                                            client.RoleID, client.RoleName, activity.ID, activity.Score, ScoreDayOld, client._DailyDataRecoreActivity.ScoreDay, ScoreMonthOld, client._MonthDataRecore.ScoreMonth));
                    }
                    else
                    {
                        if (activity.indexComplete != numberRepeat)
                            activity.indexComplete -= numberRepeat;
                    }
                    break;
            }
            List<DayActivity> _Total = _GetAllMission(client._DailyDataRecoreActivity);
            SaveAllDataToDB(client, _Total);

            try
            {
                Global.SaveRoleParamsStringToDB(client, RoleParamName.MonthRecore, client.MonthRecoreString, true);
                Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyRecoreActivity, client.DailyRecoreActivityString, true);
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// Lưu dữ liệu vào DB
        /// </summary>
        public static void SaveAllDataToDB(KPlayer client, List<DayActivity> _total)
        {
            int nDate = TimeUtil.NowDateTime().DayOfYear;
            if (client._DailyDataRecoreActivity == null || client._DailyDataRecoreActivity.EventActivityRecoding.Count == 0 || client._DailyDataRecoreActivity.DayID != nDate)
            {
                ResetDay(client);
            }
            else
            {
                for (int i = 0; i < _GetAllMission(client._DailyDataRecoreActivity).Count; i++)
                {
                    int ID = _GetAllMission(client._DailyDataRecoreActivity)[i].ID;
                    client._DailyDataRecoreActivity.EventActivityRecoding[ID] = _GetAllMission(client._DailyDataRecoreActivity)[ID];
                }
            }
        }
        #endregion

        #region Quản lý quà tặng

        public static DayActivityCollect _DayActivityCollect = new DayActivityCollect();

        public static MonthActivityCollect _MonthActivityCollect = new MonthActivityCollect();

        /// <summary>
        /// Lấy ra danh sách quà tặng theo mốc thưởng
        /// </summary>
        /// <param name="Step"></param>
        /// <returns></returns>
        public static List<DayActivityAwardItem> GetGiftCanGetByStep(int Step, bool getMonth = false)
        {
            List<DayActivityAwardItem> _Total = new List<DayActivityAwardItem>();
            if (!getMonth)
                _Total = _DayActivityCollect.Item.Where(x => x.StepID == Step + 1).OrderBy(x => x.StepID).ToList();
            else
                _Total = _MonthActivityCollect.Item.Where(x => x.StepID == Step + 1).OrderBy(x => x.StepID).ToList();
            return _Total;
        }

        /// <summary>
        /// Trả thưởng cho người chơi
        /// </summary>
        public static void GetAwardByTime(int Sec, AwardItem ItemGet, KPlayer player)
        {
            var ReviveItem = KTKTAsyncTask.Instance.ScheduleExecuteAsync(new DelayAsyncTask("ActivityRecvice", ItemGet, player, TimerProc), Sec * 1000);
        }

        private static void TimerProc(object sender, EventArgs e)
        {
            DelayAsyncTask _Task = (DelayAsyncTask)sender;

            KPlayer _Player = _Task.Player;

            AwardItem _ItemGet = (AwardItem)_Task.Tag;

            // Console.WriteLine(_ItemGet.ItemID);

            if (_ItemGet != null)
            {
                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, _Task.Player, _ItemGet.ItemID, _ItemGet.Number, 0, "DAYACTIVITY", false, 1, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
                {
                    PlayerManager.ShowNotification(_Player, "Có lỗi khi nhận vật phẩm chế tạo");
                }
            }

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Lấy ra quà tặng (có thể đổi sang quay Random đang để cố định)
        /// </summary>
        /// <param name="StepID"></param>
        /// <param name="getMonth"></param>
        /// <returns></returns>
        public static AwardItem GetItemAward(int StepID, bool getMonth = false, bool isRate = false)
        {
            DayActivityAwardItem _GiftSelect = new DayActivityAwardItem();

            if (!getMonth)
                _GiftSelect = _DayActivityCollect.Item.Where(x => x.StepID == StepID).FirstOrDefault();
            else
                _GiftSelect = _MonthActivityCollect.Item.Where(x => x.StepID == StepID).FirstOrDefault();

            AwardItem _SelectItem = null;

            if (_GiftSelect != null)
            {
                // Nếu là quay ngẫu nhiên thưởng
                if (isRate)
                {
                    List<AwardItem> RollAwardItem = _GiftSelect.RollAwardItem.ToList();

                    int Random = KTGlobal.GetRandomNumber(0, 100);

                    int idx = 0;
                    foreach (AwardItem _Item in RollAwardItem)
                    {
                        Random = Random - _Item.Rate;

                        if (Random <= 0)
                        {
                            _SelectItem = _Item;
                            break;
                        }
                        idx++;
                    }
                }
                // Đang dùng phần thưởng cố định
                else
                {
                    List<AwardItem> RollAwardItem = _GiftSelect.RollAwardItem.ToList();
                    _SelectItem = RollAwardItem[0];
                }
            }

            return _SelectItem;
        }

        #endregion

        #region TCP_Network
        /// <summary>
        /// Class xử lý việc hiện điểm hoạt động hằng ngày, trả về thông tin cho client
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessMissionDayActivityInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lam nhieu vu loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nDate = TimeUtil.NowDateTime().DayOfYear;

                // Nếu mà ngày hôm nay mà khác ngày trong CSDL đẩy ra thì create ngày mới ==> Đoạn code này đề phòng nếu qua 12h đêm chuyển sang ngày mới thì F5 lại danh sách nhận cho nó
                if (client._DailyDataRecoreActivity.DayID != nDate)
                {
                    ResetDay(client);
                }

                // đổ thông tin vào để gửi về client
                ActivityEvent _Activity = new ActivityEvent();
                _Activity.dayActivities = _GetAllMission(client._DailyDataRecoreActivity);
                _Activity.ScoreDay = client._DailyDataRecoreActivity.ScoreDay;
                _Activity.DayActivityAwardStep = client._DailyDataRecoreActivity.DayActivityAwardStep;
                _Activity.ScoreMonth = client._MonthDataRecore.ScoreMonth;
                _Activity.DayActivityAward = _DayActivityCollect;
                _Activity.BonusScore = _DayActivity.BonusScore;
                _Activity.MonthActivityAward = _MonthActivityCollect;
                _Activity.MonthActivityAwardStep = client._MonthDataRecore.MonthActivityAwardStep;
                _Activity.MaxScoreDayActivity = _DayActivity.MaxScoreDayActivity;
                _Activity.MaxScoreMonthActivity = _DayActivity.MaxScoreMonthActivity;
                _Activity.PriceVip = _DayActivity.PriceVip;
                _Activity.TypeMoney = _DayActivity.TypeMoney;
                _Activity.VipActivity = client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore);
                _Activity.VipCollectScore = client.GetValueOfDailyRecore((int)AcitvityRecore.CollectScoreVIPActivity);

                client.SendPacket<ActivityEvent>(nID, _Activity);
                return TCPProcessCmdResults.RESULT_DATA;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Class xử lý việc nhận thưởng ngày
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSpriteGetDayActivityAwardGiftCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nDate = TimeUtil.NowDateTime().DayOfYear;

                if (client._DailyDataRecoreActivity.DayID != nDate)
                {
                    strcmd = "";
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên {1} nhận thưởng không thành công do sang ngày mới ngày cũ {2}, ngày mới {3}",
                                         client.RoleID, client.RoleName, client._DailyDataRecoreActivity.DayID, nDate));
                    return TCPProcessCmdResults.RESULT_DATA;
                    //DayActivityManager.ResetDay(client);
                }

                //int CurentStep = client._DailyDataRecoreActivity.DayActivityAwardStep;

                int CurentStep = Convert.ToInt32(fields[1]);
                // Check lại các điều kiện trả thưởng
                var _ItemReward = _DayActivityCollect.Item.FirstOrDefault(x => x.StepID == CurentStep + 1);

                if(_ItemReward == null)
                {
                    PlayerManager.ShowNotification(client, "Có lỗi xảy ra vui lòng thử lại sau");

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi nhận thưởng ngày điểm hiện tại {2}, mốc thưởng {3}",
                                        client.RoleID, client.RoleName, client._DailyDataRecoreActivity.ScoreDay, CurentStep + 1));
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client._DailyDataRecoreActivity.ScoreDay < _ItemReward.Score)
                {
                    PlayerManager.ShowNotification(client, "Bạn chưa đủ điểm để nhận thưởng!");

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút, điểm hiện tại {2}, điểm cần {3}, mốc thưởng {4}",
                                        client.RoleID, client.RoleName, client._DailyDataRecoreActivity.ScoreDay, _DayActivityCollect.Item[CurentStep].Score, CurentStep + 1));
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client._DailyDataRecoreActivity.DayActivityAwardStep[CurentStep])
                {
                    PlayerManager.ShowNotification(client, "Bạn đã nhận mốc thưởng này rồi!");
                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút ngày, mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep + 1));

                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }
                else 
                {
                    // Check thêm nếu là quà của vip thì check xem có phải là vip ko để trả thưởng
                    if(_ItemReward.Vip > 0)
                    {
                        if(client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore) != 1)
                        {
                            PlayerManager.ShowNotification(client, "Bạn phải nâng cấp VIP điểm năng động mới có thể nhận phần quà này!");
                            LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút VIP ngày, mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep + 1));

                            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                    //List<DayActivityAwardItem> GetAllGiftCanGet = DayActivityManager.GetAllGiftCanGet(client._DailyDataRecoreActivity.ScoreDay, CurentStep);
                    List<DayActivityAwardItem> GetAllGiftCanGet = DayActivityManager.GetGiftCanGetByStep(CurentStep);

                    if (GetAllGiftCanGet.Count > 0)
                    {
                        DayActivityAwardItem _SelectGift = GetAllGiftCanGet.FirstOrDefault();

                        int spaceNeed = 1;

                        if (KTGlobal.IsHaveSpace(spaceNeed, client))
                        {
                            AwardItem _ItemSelect = DayActivityManager.GetItemAward(_SelectGift.StepID);

                            if (_ItemSelect != null)
                            {
                                // Thực hiện DELAY xong add quà
                                DayActivityManager.GetAwardByTime(0, _ItemSelect, client);

                                client._DailyDataRecoreActivity.DayActivityAwardStep[CurentStep] = true;

                                client._DailyDataRecoreActivity.DayActivityAwardGoodsID = client._DailyDataRecoreActivity.DayActivityAwardGoodsID + "|" + _ItemSelect.ItemID + "," + _ItemSelect.Number;

                                // GHI LẠI TÍCH LŨY VÀO DB ĐÃ LIẾM QUÀ TRÁNH ROLLBACK LẠI ĂN LẠI ĐƯỢC LẦN NỮA
                                Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyRecoreActivity, client.DailyRecoreActivityString, true);

                                //Gửi về client 5 PRAMENTER
                                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, 1, _SelectGift.StepID, client._DailyDataRecoreActivity.ScoreDay, _ItemSelect.ItemID + "," + _ItemSelect.Number);

                                LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1}  nhận thưởng hoạt động ngày mốc thưởng {2}, đồ nhận được Item có ID: {3} số lượng {4}", client.RoleID, client.RoleName, CurentStep + 1, _ItemSelect.ItemID, _ItemSelect.Number));

                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                        }
                        else
                        {
                            PlayerManager.ShowNotification(client, "Cần sắp xếp 1 ô trống trong túi đồ để nhận thưởng!");

                            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                    else // NẾU ĐÉO CÓ MỐC NÀO CÓ THỂ NHẬN NỮA ===> TRƯỜNG HỢP NÀY KHÔNG THỂ VÀO NHƯNG ĐỂ CHẮC CHẮN CLIENT CHECK KHÔNG CHUẨN TIMER THÌ GỬI VỀ THÔNG BÁO BẠN KHÔNG THỂ NHẬN QUÀ
                    {
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._DailyDataRecoreActivity.ScoreDay, "");

                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                        return TCPProcessCmdResults.RESULT_OK;
                    }

                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// Class xử lý việc nhận thưởng tháng
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSpriteGetMonthActivityAwardGiftCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int nMonth = TimeUtil.NowDateTime().Month;

                if (client._MonthDataRecore.MonthID != nMonth)
                {
                    strcmd = "";

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} nhận thưởng không thành công do sang tháng mới tháng cũ {2}, tháng mới {3}",
                      client.RoleID, client.RoleName, client._MonthDataRecore.MonthID, nMonth));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_DATA;
                }

                int CurentStep = Convert.ToInt32(fields[1]);

                var _ItemReward = _MonthActivityCollect.Item.FirstOrDefault(x => x.StepID == CurentStep + 1);

                if (_ItemReward == null)
                {
                    PlayerManager.ShowNotification(client, "Có lỗi xảy ra vui lòng thử lại sau");

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi nhận thưởng tháng điểm hiện tại {2}, mốc thưởng {3}",
                                        client.RoleID, client.RoleName, client._DailyDataRecoreActivity.ScoreDay, CurentStep + 1));
                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client._MonthDataRecore.ScoreMonth < _ItemReward.Score)
                {
                    PlayerManager.ShowNotification(client, "Bạn chưa đủ điểm để nhận thưởng!");

                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút tháng, điểm hiện tại {2}, điểm cần {3}, mốc thưởng {4}",
                                         client.RoleID, client.RoleName, client._MonthDataRecore.ScoreMonth, _DayActivityCollect.Item[CurentStep].Score, CurentStep + 1));

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                if (client._MonthDataRecore.MonthActivityAwardStep[CurentStep])
                {

                    PlayerManager.ShowNotification(client, "Bạn đã nhận mốc thưởng này rồi!");

                    LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút, mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep + 1));

                    strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }
                else
                {
                    // Check thêm nếu là quà của vip thì check xem có phải là vip ko để trả thưởng
                    if (_ItemReward.Vip > 0)
                    {
                        if (client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore) != 1)
                        {
                            PlayerManager.ShowNotification(client, "Bạn phải nâng cấp VIP điểm năng động mới có thể nhận phần quà này!");
                            LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} lỗi hiện nút VIP tháng, mốc thưởng {2}", client.RoleID, client.RoleName, CurentStep + 1));

                            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                    List<DayActivityAwardItem> GetAllGiftCanGet = DayActivityManager.GetGiftCanGetByStep(CurentStep, true);

                    if (GetAllGiftCanGet.Count > 0)
                    {
                        DayActivityAwardItem _SelectGift = GetAllGiftCanGet.FirstOrDefault();

                        int spaceNeed = 1;

                        if (KTGlobal.IsHaveSpace(spaceNeed, client))
                        {
                            AwardItem _ItemSelect = DayActivityManager.GetItemAward(_SelectGift.StepID, true);

                            if (_ItemSelect != null)
                            {
                                // Thực hiện DELAY xong add quà
                                DayActivityManager.GetAwardByTime(0, _ItemSelect, client);

                                // Update Step đã nhận cho client biết
                                client._MonthDataRecore.MonthActivityAwardStep[CurentStep] = true;

                                //Update đã nhận được cái gì rồi dựa trên những thứ đã nhận trước đó

                                client._MonthDataRecore.MonthActivityAwardGoodsID = client._MonthDataRecore.MonthActivityAwardGoodsID + "|" + _ItemSelect.ItemID + "," + _ItemSelect.Number;

                                // GHI LẠI TÍCH LŨY VÀO DB ĐÃ LIẾM QUÀ TRÁNH ROLLBACK LẠI ĂN LẠI ĐƯỢC LẦN NỮA
                                Global.SaveRoleParamsStringToDB(client, RoleParamName.MonthRecore, client.MonthRecoreString, true);

                                //Gửi về client 6 PRAMENTER
                                //ID THẰNG QUAY - 1 : QUAY THÀNH CÔNG | MỐC SẼ THỰC HIỆN ANIMATION QUAY | THỜI GIAN ONLINE HIỆN TẠI | VẬT PHẨM SẼ QUAY VÀO
                                strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, 1, _SelectGift.StepID, client._MonthDataRecore.ScoreMonth, _ItemSelect.ItemID + "," + _ItemSelect.Number);

                                LogManager.WriteLog(LogTypes.DiemNangDong, String.Format("PlayerID: {0}, tên: {1} nhận thưởng hoạt động tháng mốc thưởng {2}, đồ nhận được Item có ID: {3} số lượng {4}", client.RoleID, client.RoleName, CurentStep + 1, _ItemSelect.ItemID, _ItemSelect.Number));

                                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                                return TCPProcessCmdResults.RESULT_DATA;
                            }
                        }
                        else
                        {
                            PlayerManager.ShowNotification(client, "Cần sắp xếp 1 ô trống trong túi đồ để nhận thưởng!");

                            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                            tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                            return TCPProcessCmdResults.RESULT_OK;
                        }
                    }
                    else // NẾU ĐÉO CÓ MỐC NÀO CÓ THỂ NHẬN NỮA ===> TRƯỜNG HỢP NÀY KHÔNG THỂ VÀO NHƯNG ĐỂ CHẮC CHẮN CLIENT CHECK KHÔNG CHUẨN TIMER THÌ GỬI VỀ THÔNG BÁO BẠN KHÔNG THỂ NHẬN QUÀ
                    {
                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", roleID, -1, CurentStep, client._MonthDataRecore.ScoreMonth, "");

                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);

                        return TCPProcessCmdResults.RESULT_OK;
                    }

                }
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Class xử lý việc mua gói VIP điểm năng động
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSpriteBuyVIPActivity(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                bool isDone = false;
                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                string strMoney = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                if(client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore) == 1)
                {
                    PlayerManager.ShowNotification(client, $"Bạn đã nâng cấp VIP rồi!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                
                // todo)): sau khi ra mắt thì xóa đoạn code này
                // if (DateTime.Now.Month < 4 && GameManager.ServerId == 21)
                // {
                //     PlayerManager.ShowNotification(client, "Vật phẩm sẽ mở bán trong tháng 4!");
                //     return TCPProcessCmdResults.RESULT_OK;
                // }

                if (!KTGlobal.IsHaveSpace(1, client)) {
                    PlayerManager.ShowNotification(client, $"Bạn cần 1 ô trống trong túi đồ để nhận Rương Ưu đãi Năng động VIP!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                switch (_DayActivity.TypeMoney)
                {
                    case 1:
                        {
                            isDone = GameManager.ClientMgr.SubMoney(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, _DayActivity.PriceVip,
                                "VIPActivityScore");
                            strMoney = "Bạc";
                            break;
                        }
                    case 2:
                        {
                            isDone = GameManager.ClientMgr.AddUserBoundMoney(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, _DayActivity.PriceVip,
                                "VIPActivityScore");
                            strMoney = "Bạc khóa";
                            break;
                        }
                    case 3:
                        {
                            isDone = GameManager.ClientMgr.SubToken(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, _DayActivity.PriceVip,
                                "VIPActivityScore");
                            strMoney = "Kim Nguyên Bảo";
                            break;
                        }
                    case 4:
                        {
                            isDone = GameManager.ClientMgr.SubBoundToken(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, _DayActivity.PriceVip,
                                "VIPActivityScore");
                            strMoney = "Kim Nguyên Bảo khóa";
                            break;
                        }
                }

                if (isDone)
                {
                    client.SetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore, 1);
                    var addBoxVIP = ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, 20037, 1, 0, "ACTIVITY_VIP", true, 1, false, Global.ConstGoodsEndTime);
                    if (addBoxVIP)
                    {
                        LogManager.WriteLog(LogTypes.DiemNangDong, $"Player ({client.RoleID}/{client.RoleName}) đã nhận rương VIP năng động.");
                        strcmd = $"{client.RoleID}:{1}";

                        PlayerManager.ShowNotification(client, "Bạn đã nâng cấp VIP điểm năng động thành công!");
                        LogManager.WriteLog(LogTypes.DiemNangDong, $"Player ({client.RoleID}/{client.RoleName}) nâng cấp VIP điểm năng động");

                        tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                    }
                }
                else
                {
                    PlayerManager.ShowNotification(client, $"Bạn không đủ {strMoney} để nâng cấp VIP điểm năng động!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Class xử lý việc nhận điểm thưởng VIP
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults ProcessSpriteCollectScoreVIPActivity(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {

            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }


                if (client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore) == 1 && client.GetValueOfDailyRecore((int)AcitvityRecore.CollectScoreVIPActivity) == -1)
                {
                    strcmd = $"{client.RoleID}:{1}";

                    PlayerManager.ShowNotification(client, $"Bạn đã nhận thành công {_DayActivity.BonusScore} điểm năng động hôm nay");
                    client.SetValueOfDailyRecore((int)AcitvityRecore.CollectScoreVIPActivity, 0);
                    LogManager.WriteLog(LogTypes.DiemNangDong, $"Player ({client.RoleID}/{client.RoleName}) nhận điểm thưởng VIP hằng ngày");
                    client._DailyDataRecoreActivity.ScoreDay += _DayActivity.BonusScore;
                    client._MonthDataRecore.ScoreMonth += _DayActivity.BonusScore;

                    //Lưu lại vào DB
                    try
                    {
                        Global.SaveRoleParamsStringToDB(client, RoleParamName.MonthRecore, client.MonthRecoreString, true);
                        Global.SaveRoleParamsStringToDB(client, RoleParamName.DailyRecoreActivity, client.DailyRecoreActivityString, true);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteException(ex.ToString());
                    }

                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
                }
                else
                {
                    strcmd = $"{client.RoleID}:{1}";

                    if(client.GetValueOfMonthRecore((int)MonthRecoreDef.VIPAcitvityScore) == -1)
                        PlayerManager.ShowNotification(client, $"Bạn cần nâng cấp VIP điểm năng động để nhận thưởng!");
                    if(client.GetValueOfDailyRecore((int)AcitvityRecore.CollectScoreVIPActivity) != -1)
                        PlayerManager.ShowNotification(client, $"Bạn đã nhận thưởng ngày hôm nay rồi! ");

                    return TCPProcessCmdResults.RESULT_DATA;
                }

                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        

        #endregion
    }
}
/// <summary>
/// Danh sách ID nhiệm vụ viết theo enum cho dễ theo dõi
/// </summary>
public enum ActivityDayName
{
    LOG_ON = 0,
    CHUCPHUC_1 = 1,
    CHUCPHUC_5 = 2,
    MO_TUI_PHUC,
    TINH_ANH_THU_LINH,
    HOA_DANG,
    TRUY_NA,
    TIEU_DAO_COC,
    BOSS_CAO_THU,
    BACH_HO_DUONG,
    TU_LUYEN_BI_CANH,
    DU_LONG_CHIEN_THU,
    TRUNG_DU_LONG,
    TONG_KIM,
    BACH_HO_DUONG_LIEN_SERVER,
    TINH_LUC_HOAT_LUC,
    THI_DAU_MON_PHAI,
    TRANH_DOAT_LANH_THO,
    TAN_LANG,
    BOSS_LIEN_SERVER,
    LIEN_DAU,
    BA0_VAN_DONG_10,
    BA0_VAN_DONG_50,
    VUOT_AI_GIA_TOC,
    GIAI_DAU,
}
