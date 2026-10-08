using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using Server.Data;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.TCP;
using Server.Tools;
using GameServer.Logic.RefreshIconState;
using GameServer.Core.Executor;
using Tmsk.Contract;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Entities;

namespace GameServer.Logic
{
    /// <summary>
    /// Đối tượng quy định thời gian xuất hiện của quái vật
    /// </summary>
    public class BirthTimePoint
    {
        /// <summary>
        /// Giờ
        /// </summary>
        public int BirthHour = 0;

        /// <summary>
        /// Phút
        /// </summary>
        public int BirthMinute = 0;
    };

    /// <summary>
    /// Thời gian xuất hiện của quái vật theo ngày trong tuâng
    /// </summary>
    public class BirthTimeForDayOfWeek
    {
        /// <summary>
        /// Thời gian xuất hiện
        /// </summary>
        public BirthTimePoint BirthTime;

        /// <summary>
        /// Ngày trong tuần
        /// </summary>
        public int BirthDayOfWeek = 0;
    };

    /// <summary>
    /// Dữ liệu quái vật
    /// </summary>
    public class MonsterStaticInfo
    {
        /// <summary>
        /// Tên
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ID khu
        /// </summary>
        public int ExtensionID { get; set; }

        /// <summary>
        /// Cấp độ
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Danh hiệu quái
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Kinh nghiệm có được khi giết
        /// </summary>
        public int Exp { get; set; }

        /// <summary>
        /// Sinh lực tối đa
        /// </summary>
        public int MaxHP { get; set; }

        /// <summary>
        /// Phạm vi hoạt động (Đuổi mục tiêu)
        /// </summary>
        public int SeekRange { get; set; }

        /// <summary>
        /// Ngũ hành
        /// </summary>
        public KE_SERIES_TYPE Elemental { get; set; }

        /// <summary>
        /// Vật công
        /// </summary>
        public int Atk { get; set; }

        /// <summary>
        /// Băng công
        /// </summary>
        public int IceAtk { get; set; }

        /// <summary>
        /// Hỏa công
        /// </summary>
        public int FireAtk { get; set; }

        /// <summary>
        /// Lôi công
        /// </summary>
        public int LightningAtk { get; set; }

        /// <summary>
        /// Độc công
        /// </summary>
        public int PoisonAtk { get; set; }

        /// <summary>
        /// Vật phòng
        /// </summary>
        public int Def { get; set; }

        /// <summary>
        /// Băng phòng
        /// </summary>
        public int IceRes { get; set; }

        /// <summary>
        /// Hỏa phòng
        /// </summary>
        public int FireRes { get; set; }

        /// <summary>
        /// Lôi phòng
        /// </summary>
        public int LightningRes { get; set; }

        /// <summary>
        /// Độc phòng
        /// </summary>
        public int PoisonRes { get; set; }

        /// <summary>
        /// Chính xác
        /// </summary>
        public int Hit { get; set; }

        /// <summary>
        /// Né tránh
        /// </summary>
        public int Dodge { get; set; }

        /// <summary>
        /// Rơi ra túi vật phẩm ID
        /// </summary>
        public int DropID { get; set; }

        /// <summary>
        /// Tốc độ di chuyển
        /// </summary>
        public int MoveSpeed { get; set; }

        /// <summary>
        /// Tốc đánh
        /// </summary>
        public int AtkSpeed { get; set; }

        /// <summary>
        /// Chí mạng
        /// </summary>
        public int Crit { get; set; }

        /// <summary>
        /// Kháng chí mạng
        /// </summary>
        public int CritDef { get; set; }

        /// <summary>
        /// Cự ly tấn công
        /// </summary>
        public int AttackRange { get; set; }

        /// <summary>
        /// Cờ PK
        /// </summary>
        public int Camp { get; set; }

        /// <summary>
        /// ID Script AI điều khiển
        /// </summary>
        public int AIID { get; set; }

        /// <summary>
        /// ID Script Lua điều khiển
        /// </summary>
        public int ScriptID { get; set; }

        /// <summary>
        /// Loại AI
        /// </summary>
        public MonsterAIType AIType { get; set; }
    }

    /// <summary>
    /// Quản lý điểm xuất hiện quái vật
    /// </summary>
    public class MonsterZone
    {
        #region Thuộc tính cơ bản

        /// <summary>
        /// ID bản đồ
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// ID khu
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// ID quái trong khu
        /// </summary>
        public int Code
        {
            get;
            set;
        }

        /// <summary>
        /// Vị trí X
        /// </summary>
        public int ToX
        {
            get;
            set;
        }

        /// <summary>
        /// Vị trí Y
        /// </summary>
        public int ToY
        {
            get;
            set;
        }

        /// <summary>
        /// Phạm vi xuất hiện xung quanh
        /// </summary>
        public int Radius
        {
            get;
            set;
        }

        /// <summary>
        /// Tổng số quái
        /// </summary>
        public int TotalNum
        {
            get;
            set;
        }

        /// <summary>
        /// Thời gian xuất hiện quái
        /// </summary>
        public int Timeslot
        {
            get;
            set;
        }

        /// <summary>
        /// Khoảng cách đuổi mục tiêu tối đa
        /// </summary>
        public int PursuitRadius
        {
            get;
            set;
        }

        /// <summary>
        /// Loại sản sinh quái, 0: Sản sinh theo thời gian, 1: 按照时间点爆怪, 2: Hệ thống điều khiển，người dùng chủ động gọi, 3 用户主动召唤区域，和2的功能一致，但比
        /// 2的功能丰富，2主要用于实现之前的生肖宝典刷怪，而3召唤出来的怪都是一次性的，可由玩家控制或者不控制的系统零时怪
        /// </summary>
        public int BirthType
        {
            get;
            set;
        }

        /// <summary>
        /// Loại sản sinh quái vật (0-1)
        /// </summary>
        public int ConfigBirthType
        {
            get;
            set;
        }

        /// <summary>
        /// Sau bao nhiêu ngày từ lúc mở máy chủ thì bắt đầu sinh quái vật
        /// <para>Nhỏ hơn 0: Ngay khi mở máy chủ</para>
        /// <para>Lớn hơn 0: Số ngày tiếp theo đến thời điểm sinh quái vật</para>
        /// </summary>
        public int SpawnMonstersAfterKaiFuDays
        {
            get;
            set;
        }

        /// <summary>
        /// Số ngày sinh quái
        /// <para>Nhỏ hơn 0: Sinh liên tục</para>
        /// <para>Lớn hơn 0: SỐ ngày đếm ngược đến lần sinh tiếp theo</para>
        /// </summary>
        public int SpawnMonstersDays
        {
            get;
            set;
        }

        /// <summary>
        /// Ngày trong tuần sinh quái vật
        /// <para>Nếu để trống tức là liên tục</para>
        /// </summary>
        public List<BirthTimeForDayOfWeek> SpawnMonstersDayOfWeek
        {
            get;
            set;
        }

        /// <summary>
        /// Danh sách các thời điểm sản sinh quái
        /// </summary>
        public List<BirthTimePoint> BirthTimePointList
        {
            get;
            set;
        }

        /// <summary>
        /// Tỷ lệ sinh quái (tối đa 100)
        /// </summary>
        public int BirthRate
        {
            get;
            set;
        }

        /// <summary>
        /// Đã giết toàn bộ quái trong khu vực này chưa.
        /// <para>Sử dụng khi loại sản sinh quái là 4</para>
        /// </summary>
        private bool HasSystemKilledAllOfThisZone = false;

        /// <summary>
        /// Có phải phụ bản không
        /// </summary>
        public bool IsFuBenMap = false;

        /// <summary>
        /// Loại quái
        /// </summary>
        public MonsterTypes MonsterType = MonsterTypes.None;

        /// <summary>
        /// Thời điểm phục sinh cuối của khu vực
        /// </summary>
        private long LastReloadTicks = 0;

        /// <summary>
        /// Thời điểm tiêu diệt quái cuối của khu vực
        /// </summary>
        private long LastDestroyTicks = 0;

        /// <summary>
        /// Ngày tạo quái cuối
        /// </summary>
        private int LastBirthDayID = -1;

        /// <summary>
        /// Thời điểm sinh quái cuối
        /// </summary>
        private BirthTimePoint LastBirthTimePoint = null;

        /// <summary>
        /// ID điểm sinh quái cuối
        /// </summary>
        private int LastBirthTimePointIndex = -1;

        /// <summary>
        /// ID Script Lua điều khiển quái
        /// </summary>
        public int ScriptID { get; set; }

        #endregion

        #region Thông tin quái

        /// <summary>
        /// Dữ liệu quái
        /// </summary>
        private MonsterStaticInfo MonsterInfo = new MonsterStaticInfo();

        /// <summary>
        /// Trả về dữ liệu quái
        /// </summary>
        public MonsterStaticInfo GetMonsterInfo()
        {
            return this.MonsterInfo;
        }
        #endregion

        #region Danh sách quái

        /// <summary>
        /// Danh sách quái
        /// </summary>
        private List<Monster> MonsterList = new List<Monster>(100);

        #endregion

        #region Quản lý phụ bản

        /// <summary>
        /// Quái vật trong phụ bản
        /// </summary>
        private Monster SeedMonster = null;

        #endregion

        #region Tải quái

        /// <summary>
        /// Tải dữ liệu quái vật
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="monsterZone"></param>
        /// <param name="monsterInfo"></param>
        /// <param name="roleID"></param>
        /// <param name="name"></param>
        /// <param name="hp"></param>
        /// <param name="coordinate"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="attackRange"></param>
        /// <param name="aiType"></param>
        private void LoadMonster(Monster monster, MonsterZone monsterZone, MonsterStaticInfo monsterInfo, int roleID, string name, string title, int hp, Point coordinate, int moveSpeed, int attackRange, MonsterAIType aiType)
        {
            monster.RoleName = monsterInfo.Name;
            monster.Title = title;
            monster.FormatSystemName = name;
            monster.MonsterZoneNode = monsterZone;
            monster.MonsterInfo = monsterInfo;
            monster.RoleID = roleID;
            monster.m_CurrentLife = hp;
            monster.ChangeLifeMax(hp, 0, 0);
            monster.CurrentPos = coordinate;
            monster.ChangeRunSpeed(moveSpeed, 0, 0);
            monster.MonsterType = (int)MonsterTypes.Noraml;
            monster.AIType = aiType;
            monster.m_Level = monsterInfo.Level;
            monster.ChangeAttackSpeed(monsterInfo.AtkSpeed, 0);
            monster.ChangeCastSpeed(monsterInfo.AtkSpeed, 0);
            monster.ChangeAttackRating(monsterInfo.Hit, 0,0);
            monster.ChangeDefend(monsterInfo.Dodge, 0, 0);
            monster.SetPhysicsDamage(monsterInfo.Atk, monsterInfo.Atk);

            monster.SetSeriesDamagePhysics(DAMAGE_TYPE.damage_physics, monsterInfo.Atk);
            monster.SetSeriesDamagePhysics(DAMAGE_TYPE.damage_cold, monsterInfo.IceAtk);
            monster.SetSeriesDamagePhysics(DAMAGE_TYPE.damage_fire, monsterInfo.FireAtk);
            monster.SetSeriesDamagePhysics(DAMAGE_TYPE.damage_light, monsterInfo.LightningAtk);
            monster.SetSeriesDamagePhysics(DAMAGE_TYPE.damage_poison, monsterInfo.PoisonAtk);


            monster.SetResist(DAMAGE_TYPE.damage_cold, monsterInfo.IceRes);
            monster.SetResist(DAMAGE_TYPE.damage_fire, monsterInfo.FireRes);
            monster.SetResist(DAMAGE_TYPE.damage_light, monsterInfo.LightningRes);
            monster.SetResist(DAMAGE_TYPE.damage_poison, monsterInfo.PoisonRes);
            monster.SetResist(DAMAGE_TYPE.damage_physics, monsterInfo.Def);

            //  monster.m_damage

            /// Ngũ hành
            if (this.MonsterInfo.Elemental <= KE_SERIES_TYPE.series_none || this.MonsterInfo.Elemental >= KE_SERIES_TYPE.series_num)
            {
                monster.m_Series = (KE_SERIES_TYPE)Global.GetRandomNumber((int)KE_SERIES_TYPE.series_none + 1, (int)KE_SERIES_TYPE.series_num - 1);
            }
            else
            {
                monster.m_Series = this.MonsterInfo.Elemental;
            }
        }

        /// <summary>
        /// Copy dữ liệu quái vật
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        private Monster CopyMonster(Monster oldMonster)
        {
            Monster monster = oldMonster.Clone();

            monster.MoveToComplete += this.MoveToComplete;

            return monster;
        }

        /// <summary>
        /// Hủy quái vật
        /// <para>Khi hủy quái vật, tham chiếu sẽ bị xóa bỏ, đồng thời cũng giải phóng bộ nhớ</para>
        /// </summary>
        /// <param name="monster"></param>
        private void DestroyMonster(Monster monster)
        {
            monster.MoveToComplete -= this.MoveToComplete;

            /// Xóa khỏi bản đồ
            GameManager.MapGridMgr.DictGrids[MapCode].RemoveObject(monster);

            /// Xoas khỏi danh sách quái của khu vực
            bool ret = this.MonsterList.Remove(monster);

            /// Xóa khỏi đối tượng quản lý quái
            GameManager.MonsterMgr.RemoveMonster(monster);

            /// Trả lại ID quái cho hệ thống để tái sử dụng
            GameManager.MonsterIDMgr.PushBack(monster.RoleID);

            /// Nếu xóa thành công quái khỏi danh sách khu vực
            if (ret)
            {
                /// Giảm số lượng quái
                Monster.DecMonsterCount();
            }
        }

        /// <summary>
        /// Đọc dữ liệu quái từ file Monster trong bản đồ
        /// </summary>
        /// <param name="monsterXml"></param>
        /// <param name="maxLifeV"></param>
        /// <param name="maxMagicV"></param>
        /// <param name="xmlFrameConfig"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="attachEvent"></param>
        /// <returns></returns>
        private Monster InitMonster(XElement monsterXml)
        {
            /// Lấy dữ liệu bản đồ tương ứng
            GameMap gameMap = GameManager.MapMgr.DictMaps[MapCode];

            /// Tạo mới quái
            Monster monster = new Monster();

            /// Cập nhật ID cho quái
            int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
            monster.UniqueID = Global.GetUniqueID();

            /// Tải xuống dữ liệu quái
            this.LoadMonster(
                    monster,
                    this,
                    this.MonsterInfo,
                    roleID,
                    string.Format("Role_{0}", roleID),
                    monsterXml.Attribute("Title").Value,
                    int.Parse(monsterXml.Attribute("MaxHP").Value),
                    Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, this.MapCode, this.ToX, this.ToY, this.Radius, 0, true),
                    int.Parse(monsterXml.Attribute("MoveSpeed").Value),
                    int.Parse(monsterXml.Attribute("AttackRange").Value),
                    (MonsterAIType)int.Parse(monsterXml.Attribute("AIType").Value)
                );

            monster.MoveToComplete += MoveToComplete;

            return monster;
        }

        /// <summary>
        /// Kiểm tra với tỷ lệ tương ứng có thể tái sinh quái không
        /// </summary>
        private bool CanRealiveByRate()
        {
            if (BirthRate >= 10000)
            {
                return true;
            }

            int randNum = Global.GetRandomNumber(1, 10001);
            if (randNum <= BirthRate)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tải thông tin cấu hình quái
        /// </summary>
        public void LoadStaticMonsterInfo()
        {
            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("Can not load file: {0}, file not found!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", this.Code.ToString());
            this.InitMonsterStaticInfo(monsterXml);
        }

        /// <summary>
        /// Tải danh sách quái
        /// </summary>
        public void LoadMonsters()
        {
            string fileName = string.Format("Config/Monsters.xml");

            XElement xml = GameManager.MonsterZoneMgr.AllMonstersXml;
            if (xml == null)
            {
                throw new Exception(string.Format("Can not load file: {0}, file not found!", fileName));
            }

            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            /// Thêm quái vào danh sách
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "Name"));

            /// Tạo đối tượng quái
            Monster monster = null;

            /// Có phải phụ bản không
            if (!this.IsFuBenMap)
            {
                for (int i = 0; i < this.TotalNum; i++)
                {
                    /// Phân tích dữ liệu quái
                    monster = this.InitMonster(monsterXml);
                    this.MonsterType = (MonsterTypes)monster.MonsterType;

                    /// Thêm quái vào danh sách
                    this.MonsterList.Add(monster);

                    /// Thêm quái vào đối tượng quản lý
                    GameManager.MonsterMgr.AddMonster(monster);
                }
            }
            /// Nếu là phụ bản
            else
            {
                /// Phân tích dữ liệu quái
                monster = InitMonster(monsterXml);
                MonsterType = (MonsterTypes)monster.MonsterType;

                this.SeedMonster = monster;
            }
        }

        /// <summary>
        /// 加载 动态怪物种子，这个函数会被调用多次，每一次被调用之前，其相关参数已经被更改
        /// 返回的结果将用于 怪物的动态创建
        /// 这样写可以用之前 LoadMonsters()代码
        /// 程序初始化的时候会调用这个函数，执行过程中找不到怪物种子的时候可能会重新初始化调用
        /// </summary>
        /// <returns></returns>
        public Monster LoadDynamicMonsterSeed()
        {

            string fileName = string.Format("Config/Monsters.xml");
            XElement xml = null;

            try
            {
                xml = XElement.Load(Global.GameResPath(fileName));
                if (null == xml)
                {
                    throw new Exception(string.Format("Can not load file: {0}, file not found!", fileName));
                }
            }
            catch (Exception)
            {
                throw new Exception(string.Format("Can not load file: {0}, file not found!", fileName));
            }


            XElement monsterXml = Global.GetSafeXElement(xml, "Monster", "ID", Code.ToString());

            /// Thêm vào danh sách quản lý
            MonsterNameManager.AddMonsterName(Code, Global.GetSafeAttributeStr(monsterXml, "SName"));

            /// Tải dữ liệu
            return InitMonster(monsterXml);
        }

        /// <summary>
        /// Sự kiện khi hoàn tất di chuyển
        /// </summary>
        /// <param name="sender"></param>
        private void MoveToComplete(object sender)
        {
            Console.WriteLine("MONSTER :" + (sender as Monster).FormatSystemName + " MOVE DONE!");
            //(sender as Monster).MoveToPos = new Point(-1, -1); //防止重入
            (sender as Monster).DestPoint = new Point(-1, -1);
            (sender as Monster).m_eDoing = KiemThe.Entities.KE_NPC_DOING.do_stand; //不需要通知，同一会执行动作
            Global.RemoveStoryboard((sender as Monster).FormatSystemName);
        }

        /// <summary>
        /// Sự kiện khi tọa độ quái thay đổi
        /// </summary>
        /// <param name="sprite"></param>
        private void UpdateMonsterEvent(Monster monster)
        {

        }

        #endregion

        #region Quản lý tái sinh

        /// <summary>
        /// Trả về thời điểm tái sinh tiếp theo
        /// </summary>
        /// <returns></returns>
        public string GetNextBirthTimePoint()
        {
            if (BirthType == (int)MonsterBirthTypes.CreateDayOfWeek && SpawnMonstersDayOfWeek != null)
            {
                DateTime nowTime = TimeUtil.NowDateTime();
                int dayOfWeek = 0;
                int birthHour = 0;
                int birthMinite = 0;

                int nextIndex = -1;
                if (LastBirthTimePointIndex >= 0)
                {
                    nextIndex = (LastBirthTimePointIndex + 1) % SpawnMonstersDayOfWeek.Count;
                }
                else
                {
                    DateTime now = TimeUtil.NowDateTime();
                    int time1 = (int)now.DayOfWeek * 1440 + now.Hour * 60 + now.Minute;

                    for (int i = 0; i < SpawnMonstersDayOfWeek.Count; i++)
                    {
                        int time2 = SpawnMonstersDayOfWeek[i].BirthDayOfWeek * 1440 + SpawnMonstersDayOfWeek[i].BirthTime.BirthHour * 60 + SpawnMonstersDayOfWeek[i].BirthTime.BirthMinute;
                        if (time1 <= time2)
                        {
                            nextIndex = i;
                            break;
                        }
                    }
                    if (nextIndex < 0)
                    {
                        nextIndex = 0;
                    }
                }

                dayOfWeek = (int)SpawnMonstersDayOfWeek[nextIndex].BirthDayOfWeek;
                birthHour = SpawnMonstersDayOfWeek[nextIndex].BirthTime.BirthHour;
                birthMinite = SpawnMonstersDayOfWeek[nextIndex].BirthTime.BirthMinute;

                return string.Format("{0}${1}${2}", birthHour, birthMinite, dayOfWeek);
            }
            else
            {
                if (null == BirthTimePointList)
                {
                    return "";
                }

                //原子操作
                int lastBirthTimePointIndex = LastBirthTimePointIndex;

                //上次爆怪物的时间点的索引
                int nextIndex = 0;
                if (lastBirthTimePointIndex >= 0)
                {
                    nextIndex = (lastBirthTimePointIndex + 1) % BirthTimePointList.Count;
                }
                else
                {
                    DateTime now = TimeUtil.NowDateTime();
                    int time2 = now.Hour * 60 + now.Minute;
                    for (int i = 0; i < BirthTimePointList.Count; i++)
                    {
                        int time1 = BirthTimePointList[i].BirthHour * 60 + BirthTimePointList[i].BirthMinute;
                        if (time2 <= time1)
                        {
                            nextIndex = i;
                            break;
                        }
                    }
                }

                return string.Format("{0}${1}", BirthTimePointList[nextIndex].BirthHour, BirthTimePointList[nextIndex].BirthMinute);
            }
        }

        /// <summary>
        /// Có thể tái sinh ở thời điểm tương ứng
        /// </summary>
        /// <param name="birthTimePoint"></param>
        /// <returns></returns>
        private bool CanBirthOnTimePoint(DateTime now, BirthTimePoint birthTimePoint)
        {
            if (now.DayOfYear == LastBirthDayID) //如果天数相同
            {
                if (null != LastBirthTimePoint)
                {
                    if (LastBirthTimePoint.BirthHour == birthTimePoint.BirthHour &&
                            LastBirthTimePoint.BirthMinute == birthTimePoint.BirthMinute)
                    {
                        return false; //说明今天的这个时间点已经爆过怪物了
                    }
                }
            }

            if (now.Hour != birthTimePoint.BirthHour)
            {
                return false;
            }

            int minMinute = birthTimePoint.BirthMinute;
            int maxMinute = birthTimePoint.BirthMinute + 1;
            return (now.Minute >= minMinute && now.Minute <= maxMinute);
        }

        /// <summary>
        /// Có thể tái sinh ở thời điểm tương ứng trong ngày của tuần
        /// </summary>
        /// <param name="birthTimePoint"></param>
        /// <returns></returns>
        private bool CanBirthOnTimePointForWeekOfDay(DateTime now, BirthTimePoint birthTimePoint)
        {
            if (now.DayOfYear == LastBirthDayID) //如果天数相同
            {
                if (null != LastBirthTimePoint)
                {
                    if (LastBirthTimePoint.BirthHour == birthTimePoint.BirthHour &&
                            LastBirthTimePoint.BirthMinute == birthTimePoint.BirthMinute)
                    {
                        return false; //说明今天的这个时间点已经爆过怪物了
                    }
                }
            }

            if (now.Hour != birthTimePoint.BirthHour)
            {
                return false;
            }

            int minMinute = birthTimePoint.BirthMinute;
            int maxMinute = birthTimePoint.BirthMinute + 1;
            return (now.Minute >= minMinute && now.Minute <= maxMinute);
        }

        /// <summary>
        /// 根据当前的怪剩余个数，重新爆怪 1.普通地图，不能是副本地图 2.怪物复活
        /// 怪物复活机制 a.非副本地图，只有birth 类型为 0 和 为1 的区域 才会被循环线程定期判断并复活
        /// b 副本地图，基础规则是副本地图不管什么怪物一旦加载，不再复活,如果调用相应reload函数，则意味着
        /// 相关区域不管什么birth类型，怪物死掉的全部复活
        /// c 存在 birth类型为2的怪物，如果在副本地图中，和其它类型处理上没区别，如果在非副本地图中，则不会
        /// 被定期复活，除非明显调用相应的reload函数
        /// 因此，总共有三个reload函数，针对非副本地图2个，针对副本地图一个
        /// 
        /// 针对上述原则，为了实现动态刷怪，进入如下处理
        /// birth为2的区域，强行不管是否副本地图，都不允许用旧的函数进行reload操作，必须采用新的函数进行reload，
        /// 目前出去非副本地图有一个对birth为2的区域的支持函数外，不需要其它函数，暂时不用写。
        /// </summary>
        public void ReloadMonsters(SocketListener sl, TCPOutPacketPool pool)
        {
            //副本地图无复活机制
            if (IsFuBenMap)
            {
                return;
            }

            DateTime now = TimeUtil.NowDateTime();

            //判断在指定的地图上是否在许可的时间段内
            if (!Global.CanMapInLimitTimes(MapCode, now)) //没有在刷怪的时间内
            {
                return;
            }

            //今天是否能刷怪,今天不能刷怪，直接返回，活着的全杀死
            if (!CanTodayReloadMonsters() || !CanTodayReloadMonstersForDayOfWeek())
            {
                if (!HasSystemKilledAllOfThisZone)
                {
                    SystemKillAllMonstersOfThisZone();
                    HasSystemKilledAllOfThisZone = true;
                }
                return;
            }

            //重置系统杀死怪物标志，这样保证在未刷怪时间点怪物全死
            HasSystemKilledAllOfThisZone = false;

            // code == -1的动态区域，有异常
            if (Code > 0 && ConfigBirthType == (int)MonsterBirthTypes.AfterJieRiDays)
            {
                try
                {
                    // 如果是节日boss复活，那么重新加载一下MonsterInfo
                    // 前提是策划已经热更了Config/Monsters.xml
                    LoadStaticMonsterInfo();
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, "reload jieri boss monster failed", ex);
                }
            }

            if ((int)MonsterBirthTypes.TimeSpan == BirthType) //按照时间段的爆怪机制
            {
                long ticks = now.Ticks;
                if (LastReloadTicks <= 0 || ticks - LastReloadTicks >= (1 * 1000 * 10000))
                {
                    LastReloadTicks = ticks;

                    //重新刷新怪
                    MonsterRealive(sl, pool);
                }
            }
            else if ((int)MonsterBirthTypes.TimePoint == BirthType) //按照时间点的爆怪机制
            {
                //时间有爆怪的时间点
                if (null != BirthTimePointList)
                {
                    //上次爆怪物的时间点的索引
                    int nextIndex = 0;
                    if (LastBirthTimePointIndex >= 0)
                    {
                        nextIndex = (LastBirthTimePointIndex + 1) % BirthTimePointList.Count;
                    }
                    else
                    {
                        for (int i = 0; i < BirthTimePointList.Count; i++)
                        {
                            if (CanBirthOnTimePoint(now, BirthTimePointList[i]))
                            {
                                nextIndex = i;
                                break;
                            }
                        }
                    }

                    //查找下一个爆怪的时间点
                    BirthTimePoint birthTimePoint = BirthTimePointList[nextIndex];

                    //判断是否能够爆怪
                    if (CanBirthOnTimePoint(now, birthTimePoint))
                    {
                        //记住这次爆怪的索引
                        LastBirthTimePointIndex = nextIndex;

                        //记住这次爆怪的时间点
                        LastBirthTimePoint = birthTimePoint;

                        //上次爆怪物的天ID
                        LastBirthDayID = TimeUtil.NowDateTime().DayOfYear;

                        //重新刷新怪
                        MonsterRealive(sl, pool);
                    }
                }
            }
            else if ((int)MonsterBirthTypes.CreateDayOfWeek == BirthType) // 每周几刷怪 [1/10/2014 LiaoWei]
            {
                if (SpawnMonstersDayOfWeek == null)
                    return;

                DateTime nowTime = TimeUtil.NowDateTime();
                DayOfWeek nDayOfWeek = nowTime.DayOfWeek;

                for (int i = 0; i < SpawnMonstersDayOfWeek.Count; ++i)
                {
                    int nDay = SpawnMonstersDayOfWeek[i].BirthDayOfWeek;

                    if (nDay == (int)nDayOfWeek)
                    {
                        BirthTimePoint time = SpawnMonstersDayOfWeek[i].BirthTime;

                        //判断是否能够爆怪
                        if (CanBirthOnTimePoint(now, time))
                        {
                            //记住这次爆怪的索引
                            LastBirthTimePointIndex = i;

                            //记住这次爆怪的时间点
                            LastBirthTimePoint = time;

                            //上次爆怪物的天ID
                            LastBirthDayID = TimeUtil.NowDateTime().DayOfYear;

                            //重新刷新怪
                            MonsterRealive(sl, pool);
                        }

                    }
                }


            }
        }

        /// <summary>
        /// 判断现在是否能刷怪，主要针对原始刷怪类型为4的配置进行开服后多少天的刷怪控制
        /// 如果不能刷怪，则外部需要系统强行杀死所有本区域的怪
        /// </summary>
        /// <returns></returns>
        public Boolean CanTodayReloadMonsters()
        {
            if (SpawnMonstersAfterKaiFuDays <= 0 && SpawnMonstersDays <= 0)
            {
                return true;
            }

            DateTime kaifuTime = Global.GetKaiFuTime();
            if (ConfigBirthType == (int)MonsterBirthTypes.AfterHeFuDays)
            {
                // 检查是否开启了该活动
                HeFuActivityConfig config = HuodongCachingMgr.GetHeFuActivityConfing();
                if (null == config)
                    return false;
                if (!config.InList((int)ActivityTypes.HeFuBossAttack))
                    return false;

                kaifuTime = Global.GetHefuStartDay();
            }
            else if (ConfigBirthType == (int)MonsterBirthTypes.AfterJieRiDays)
            {
                // 检查是否开启了该活动
                JieriActivityConfig config = HuodongCachingMgr.GetJieriActivityConfig();
                if (null == config)
                    return false;
                if (!config.InList((int)ActivityTypes.JieriBossAttack))
                    return false;

                kaifuTime = Global.GetJieriStartDay();
            }

            DateTime now = TimeUtil.NowDateTime();
            int days2Kaifu = Global.GetDaysSpanNum(now, kaifuTime) + 1;

            if (SpawnMonstersAfterKaiFuDays <= 0 || days2Kaifu >= SpawnMonstersAfterKaiFuDays)
            {
                if (SpawnMonstersDays <= 0 || days2Kaifu < SpawnMonstersDays + SpawnMonstersAfterKaiFuDays)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断现在是否能刷怪 针对MU新增的一周哪天能刷怪 [1/10/2014 LiaoWei]
        /// </summary>
        /// <returns></returns>
        public bool CanTodayReloadMonstersForDayOfWeek()
        {
            if (SpawnMonstersDayOfWeek == null)
                return true;

            if (ConfigBirthType != (int)MonsterBirthTypes.CreateDayOfWeek) // 周几刷怪[1/10/2014 LiaoWei]
                return true;

            DateTime now = TimeUtil.NowDateTime();
            DayOfWeek nDayOfWeek = now.DayOfWeek;

            for (int i = 0; i < SpawnMonstersDayOfWeek.Count; ++i)
            {
                int nDay = SpawnMonstersDayOfWeek[i].BirthDayOfWeek;

                if (nDay == (int)nDayOfWeek)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Giết toàn bộ quái ở khu này
        /// </summary>
        /// <returns></returns>
        public void SystemKillAllMonstersOfThisZone()
        {
            for (int n = 0; n < MonsterList.Count; n++)
            {
                if (null == MonsterList[n])
                {
                    continue;
                }
                if (MonsterList[n].Alive)
                {
                    Global.SystemKillMonster(MonsterList[n]);
                }
            }
        }

        /// <summary>
        /// Cập nhật lại tọa độ quái
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        private void RepositionMonster(Monster monster, int toX, int toY)
        {
            GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, toX, toY, monster);
        }

        /// <summary>
        /// Tái sinh quái vật
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        private void MonsterRealive(SocketListener sl, TCPOutPacketPool pool, int copyMapID = -1, int birthCount = 65535)
        {
            int haveBirthCount = 0;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    continue;
                }
                //超过了最大的召唤个数
                if (haveBirthCount >= birthCount)
                {
                    break;
                }

                if (-1 != copyMapID)
                {
                    if (MonsterList[i].CopyMapID != copyMapID)
                    {
                        continue;
                    }
                }

                if (!MonsterList[i].Alive) //如果怪物已经死亡
                {
                    if (((int)MonsterBirthTypes.TimeSpan == BirthType || (int)MonsterBirthTypes.CopyMapLike == BirthType) && Timeslot > 0) //如果是按照时间段来刷怪的这里要进行判断 // 增加副本复活时间 [3/3/2014 LiaoWei]
                    {
                        long monsterRealiveTimeslot = ((long)Timeslot * 1000L * 10000L);
                        if (TimeUtil.NOW() * 10000 - MonsterList[i].LastDeadTicks < monsterRealiveTimeslot)
                        {
                            continue;
                        }
                    }

                    //根据爆怪的概率是否能爆怪
                    if (CanRealiveByRate())
                    {
                        // 转入界面线程
                        Point pt = MonsterList[i].Realive();

                        //未来避免怪物复活时奇怪的隐身操作(可能坐标点不对，导致无法在事件中处理)
                        RepositionMonster(MonsterList[i], (int)pt.X, (int)pt.Y);

                        List<Object> listObjs = Global.GetAll9Clients(MonsterList[i]);
                        GameManager.ClientMgr.NotifyMonsterRealive(sl, pool, MonsterList[i], MapCode, MonsterList[i].CopyMapID, MonsterList[i].RoleID, (int)MonsterList[i].CurrentPos.X, (int)MonsterList[i].CurrentPos.Y, (int)MonsterList[i].CurrentDir, listObjs);

                        haveBirthCount++;

                        //System.Diagnostics.Debug.WriteLine(string.Format("刷出{0}个怪", haveBirthCount));
                        if ((int)MonsterTypes.Boss == MonsterList[i].MonsterType)
                        {
                            if ((int)MonsterBirthTypes.TimeSpan == BirthType && Timeslot >= (30 * 60)) //大于30分钟的才报告
                            {
                                //通知客户端boss刷新了
                                GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("[{0}]出现在[{1}],请各位勇士速速前往击杀。"), MonsterList[i].MonsterInfo.Name, Global.GetMapName(MonsterList[i].CurrentMapCode)), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                            }
                        }

                        if (Global.IsGongGaoReliveMonster(MonsterList[i].MonsterInfo.ExtensionID))
                        {
                            GameManager.ClientMgr.NotifyAllImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    null, StringUtil.substitute(Global.GetLang("【{0}】出现了【{1}】的身影，战胜它就有机会获得珍贵宝藏哦！"), Global.GetMapName(MonsterList[i].CurrentMapCode), MonsterList[i].MonsterInfo.Name), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                        }

                        // 刷黄金部队或世界BOSS
                        if (BirthType == (int)MonsterBirthTypes.TimePoint || BirthType == (int)MonsterBirthTypes.CreateDayOfWeek)
                        {
                            /// 处理BOSS复活时图标状态刷新
                            TimerBossManager.getInstance().AddBoss(BirthType, MonsterList[i].MonsterInfo.ExtensionID);
                        }
                    }
                }
            }
        }

        #endregion

        #region Phụ bản

        /// <summary>
        /// Tải xuống danh sách quái của phụ bản
        /// </summary>
        public void LoadCopyMapMonsters(int copyMapID)
        {
            if (!IsFuBenMap)
            {
                return; //不是副本不处理
            }

            if (null == SeedMonster)
            {
                return; //没有种子怪物, 不处理
            }

            Monster monster = null;
            for (int i = 0; i < TotalNum; i++)
            {
                monster = CopyMonster(SeedMonster);

                int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
                monster.RoleID = roleID;
                monster.UniqueID = Global.GetUniqueID();
                monster.FormatSystemName = string.Format("Role_{0}", roleID);
                monster.CopyMapID = copyMapID;
                monster.CurrentPos = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, ToX, ToY, Radius, 0, true); //人的位置X/Y坐标

                if (Global.InOnlyObsByXY(ObjectTypes.OT_MONSTER, monster.CurrentMapCode, (int)monster.CurrentPos.X, (int)monster.CurrentPos.Y))
                {
                    System.Diagnostics.Debug.WriteLine("abc");
                }

                //monster.Coordinate = monster.FirstCoordinate;
                monster.CurrentDir = (KiemThe.Entities.Direction)Global.GetRandomNumber(0, 8);

                //加入当前区域队列
                MonsterList.Add(monster);

                //添加到全局的队列
                GameManager.MonsterMgr.AddMonster(monster);

                //将精灵放入格子
                //if (!GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, monster))
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的范围错误，超出了地图的最大限制: MonsterID={0}, MonsterName={1}",
                //        monster.MonsterInfo.ExtensionID, monster.VSName));
                //}

                //怪物生命计时器开始
                monster.Awake(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活

                /// 怪物进行了移动
                //Global.MonsterMoveGrid(monster);
            }
        }

        /// <summary>
        /// 立刻重新刷新副本怪物(特殊使用)---->复活副本怪物【1.必须是副本， 2.怪物必须死亡才会复活】
        /// </summary>
        public void ReloadCopyMapMonsters(SocketListener sl, TCPOutPacketPool pool, int copyMapID)
        {
            //非副本地图不执行
            if (!IsFuBenMap)
            {
                return;
            }


            if ((int)MonsterBirthTypes.CopyMapLike != BirthType) //是否是用户召唤的爆怪机制
            {
                return;
            }

            //重新刷新怪
            MonsterRealive(sl, pool, copyMapID);
        }

        /// <summary>
        /// 清除副本地图怪
        /// </summary>
        public void ClearCopyMapMonsters(int copyMapID)
        {
            //不是副本地图无销毁机制
            if (!IsFuBenMap)
            {
                return;
            }

            List<Monster> monsterList = new List<Monster>();
            bool bExistNull = false;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    bExistNull = true;
                    continue;
                }
                if (MonsterList[i].CopyMapID == copyMapID)
                {
                    monsterList.Add(MonsterList[i]);
                }
            }

            for (int i = 0; i < monsterList.Count; i++)
            {
                DestroyMonster(monsterList[i]);
            }

            if (bExistNull)
            {
                MonsterList.RemoveAll((x) => { return null == x; });
            }
        }

        /// <summary>
        /// 遍历判断副本地图怪是否需要销毁(主线程调用)
        /// 如果仅仅是副本才销毁，而当前地图不是副本地图，则无销毁机制
        /// onlyFuBen 默认值true 兼容老的执行方式
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        public void DestroyDeadMonsters(bool onlyFuBen = true)
        {
            //如果仅仅是副本才销毁，而当前地图不是副本地图，则无销毁机制
            if (!IsFuBenMap && onlyFuBen)
            {
                return;
            }

            if (BirthType == (int)MonsterBirthTypes.CopyMapLike)
            {
                return;
            }

            long ticks = TimeUtil.NOW() * 10000;
            long monsterDestroyTimeslot = (30 * 1000 * 10000);
            if (ticks - LastDestroyTicks < monsterDestroyTimeslot)
            {
                return;
            }

            LastDestroyTicks = ticks;

            List<Monster> monsterList = new List<Monster>();
            bool bExistNull = false;
            for (int i = 0; i < MonsterList.Count; i++)
            {
                if (null == MonsterList[i])
                {
                    bExistNull = true;
                    continue;
                }
                if (!MonsterList[i].Alive) //如果怪物已经死亡
                {
                    monsterList.Add(MonsterList[i]);
                }
            }

            for (int i = 0; i < monsterList.Count; i++)
            {
                DestroyMonster(monsterList[i]);
            }

            if (bExistNull)
            {
                MonsterList.RemoveAll((x) => { return null == x; });
                LogManager.WriteLog(LogTypes.Error, string.Format("DestroyDeadMonsters MonsterList Exist Null!!!"));
            }
        }

        #region Quái chữ đỏ
        /// <summary>
        /// 销毁死掉的动态生成的怪物,不管副本不副本，只要 birthtype == 3的，都销毁
        /// </summary>
        public void DestroyDeadDynamicMonsters()
        {
            if (IsDynamicZone())
            {
                DestroyDeadMonsters(false);
            }
        }

        /// <summary>
        /// 判断是否动态刷怪区域 动态刷怪区域刷出的怪，都是属于玩家的怪，玩家可以带走并穿越地图 每个地图都有一个动态
        /// 刷怪区域，便于玩家带怪穿越地图
        /// </summary>
        /// <returns></returns>
        public Boolean IsDynamicZone()
        {
            return (int)MonsterBirthTypes.CrossMap == BirthType;
        }

        /// <summary>
        /// 初始化动态生成的地图怪物
        /// </summary>
        public Monster LoadDynamicRobot(MonsterZoneQueueItem monsterZoneQueueItem)
        {
            Monster monster = monsterZoneQueueItem.seedMonster;
            //int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
            //monster.RoleID = roleID;
            //monster.UniqueID = Global.GetUniqueID();
            monster.Tag = monsterZoneQueueItem.Tag;
            monster.ManagerType = monsterZoneQueueItem.ManagerType;
            //monster.Name = string.Format("Role_{0}", monster.RoleID);
            monster.CopyMapID = monsterZoneQueueItem.CopyMapID;

            //这儿一定要配置区域
            monster.MonsterZoneNode = this;
            monster.MoveToComplete += MoveToComplete;

            monster.DynamicMonster = true;
            monster.DynamicPursuitRadius = monsterZoneQueueItem.PursuitRadius;

            monster.CurrentPos = Global.GetMapPointByGridXY(ObjectTypes.OT_MONSTER, MapCode, monsterZoneQueueItem.ToX, monsterZoneQueueItem.ToY, monsterZoneQueueItem.Radius, 0, true); //人的位置X/Y坐标

            monster.CurrentDir = (KiemThe.Entities.Direction)Global.GetRandomNumber(0, 8);

            //加入当前区域队列
            MonsterList.Add(monster);

            //添加到全局的队列
            GameManager.MonsterMgr.AddMonster(monster);

            //怪物生命计时器开始
            monster.Awake(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活


       

            return monster;
        }

        /// <summary>
        /// 初始化动态生成的地图怪物
        /// </summary>
        public void LoadDynamicMonsters(MonsterZoneQueueItem monsterZoneQueueItem)
        {
            if (!IsDynamicZone())
            {
                return; //不是动态刷怪区域则返回，调用已经保证，这儿简单再次验证
            }

            if (null == monsterZoneQueueItem || null == monsterZoneQueueItem.seedMonster)
            {
                return; //没有种子怪物, 不处理
            }

            Monster monster = null;
            for (int i = 0; i < monsterZoneQueueItem.BirthCount; i++)
            {
                monster = CopyMonster(monsterZoneQueueItem.seedMonster);
                int roleID = (int)GameManager.MonsterIDMgr.GetNewID(MapCode);
                monster.RoleID = roleID;
                monster.UniqueID = Global.GetUniqueID();
                monster.Tag = monsterZoneQueueItem.Tag;
                monster.ManagerType = monsterZoneQueueItem.ManagerType;
                monster.FormatSystemName = string.Format("Role_{0}", roleID);
                monster.CopyMapID = monsterZoneQueueItem.CopyMapID;

                //这儿一定要配置区域
                monster.MonsterZoneNode = this;
                monster.MoveToComplete += MoveToComplete;

                monster.DynamicMonster = true;
                monster.DynamicPursuitRadius = monsterZoneQueueItem.PursuitRadius;

                //monster.Coordinate = monster.FirstCoordinate;
                monster.CurrentDir = (KiemThe.Entities.Direction)Global.GetRandomNumber(0, 8);

                // 天使神殿 [3/25/2014 LiaoWei]
                if (monsterZoneQueueItem.MyMonsterZone.MapCode == GameManager.AngelTempleMgr.m_AngelTempleData.MapCode)
                {
                    GameManager.AngelTempleMgr.OnLoadDynamicMonsters(monster);
                }
                else if (monsterZoneQueueItem.MyMonsterZone.MapCode == MoRi.MoRiJudgeManager.Instance().MapCode)
                {
                    MoRi.MoRiJudgeManager.Instance().OnLoadDynamicMonsters(monster);
                }

                // 剧情副本 [7/25/2014 LiaoWei]
                if (Global.IsStoryCopyMapScene(monsterZoneQueueItem.MyMonsterZone.MapCode))
                {
                    CopyMap mapInfo = null;
                    mapInfo = GameManager.CopyMapMgr.FindCopyMap(monster.CopyMapID);

                    if (mapInfo == null)
                        return;

                    SystemXmlItem systemFuBenItem = null;
                    if (GameManager.systemFuBenMgr.SystemXmlItemDict.TryGetValue(mapInfo.FubenMapID, out systemFuBenItem) && systemFuBenItem != null)
                    {
                        int nBossID = -1;
                        nBossID = systemFuBenItem.GetIntValue("BossID");

                        if (nBossID == monster.MonsterInfo.ExtensionID)
                            Global.NotifyClientStoryCopyMapInfo(monster.CopyMapID, 2);
                    }
                }

                //加入当前区域队列
                MonsterList.Add(monster);

                //添加到全局的队列
                GameManager.MonsterMgr.AddMonster(monster);

                GlobalEventSource4Scene.getInstance().fireEvent(new OnCreateMonsterEventObject(monster), (int)monster.ManagerType);

                //将精灵放入格子
                //if (!GameManager.MapGridMgr.DictGrids[MapCode].MoveObject(-1, -1, (int)monster.Coordinate.X, (int)monster.Coordinate.Y, monster))
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("怪物部署的范围错误，超出了地图的最大限制: MonsterID={0}, MonsterName={1}",
                //        monster.MonsterInfo.ExtensionID, monster.VSName));
                //}

                //怪物生命计时器开始
                monster.Awake(); //放在MoveGrid前边，否则不会发送, 因为Alive还未激活

                /// 怪物进行了移动
                //Global.MonsterMoveGrid(monster);

                //判断如果是特殊的怪物类型，则播放出生特效
                if ((int)MonsterTypes.DSPetMonster == monster.MonsterType)
                {
                    GameManager.ClientMgr.NotifyOthersMyDeco(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, 160, (int)GDecorationTypes.AutoRemove, -1, (int)monster.CurrentPos.X, (int)monster.CurrentPos.Y, 0, -1, -1, 0, 0, null);
                }
            }
        }

        // 以下俩函数在多线程环境下调用
        // 可能会导致MonsterList出错
        // 风险很大 先注释掉

        /// <summary>
        /// 将怪物从我的区域移除
        /// </summary>
        /// <param name="monster"></param>
        //         public void RemoveMonsterFromMyZone(Monster monster)
        //         {
        //             //从全局队列移除
        //             GameManager.MonsterMgr.RemoveMonster(monster);
        // 
        //             //列表中移除
        //             MonsterList.Remove(monster);
        // 
        //             //移除坐标响应
        //             monster.CoordinateChanged -= UpdateMonsterEvent;
        // 
        //             //移除移动结束事件
        //             monster.MoveToComplete -= MoveToComplete;
        // 
        //             //区域设置 暂时保留
        //             //monster.MonsterZoneNode = null;
        //         }

        //将怪物添加到我的区域
        //         public void AddMonsterToMyZone(Monster monster)
        //         {
        //             monster.MonsterZoneNode = this;
        // 
        //             //添加坐标响应
        //             monster.CoordinateChanged += UpdateMonsterEvent;
        // 
        //             //添加移动结束事件
        //             monster.MoveToComplete += MoveToComplete;
        // 
        //             MonsterList.Add(monster);
        // 
        //             //添加到全局的队列
        //             GameManager.MonsterMgr.AddMonster(monster);
        //         }

        /// <summary>
        /// 返回怪物信息字符串
        /// </summary>
        /// <returns></returns>
        public String GetMonstersInfoString()
        {
            int aliveCount = 0;
            int deadCount = 0;

            foreach (var monster in MonsterList)
            {
                if (null == monster)
                {
                    continue;
                }
                if (monster.Alive)
                {
                    aliveCount++;
                }
                else
                {
                    deadCount++;
                }
            }

            if (aliveCount + deadCount > 0)
            {
                return String.Format("地图{0}, {1}副本, aliveCount={2}, deadCount={3}, total={4}",
                    MapCode, IsFuBenMap ? "是" : "不是", aliveCount, deadCount, aliveCount + deadCount);
            }

            return "";
        }
        #endregion 动态召唤怪物相关

        #endregion 副本地图区域怪物动态生成和回收

        #region Bản đồ thường

        /// <summary>
        /// Tải lại danh sách quái trong bản đồ (BirthType phải = 2)
        /// </summary>
        public void ReloadNormalMapMonsters(SocketListener sl, TCPOutPacketPool pool, int birthCount)
        {
            //副本地图不执行
            if (IsFuBenMap)
            {
                return;
            }

            if ((int)MonsterBirthTypes.CopyMapLike != BirthType) //是否是用户召唤的爆怪机制
            {
                return;
            }

            //重新刷新怪
            MonsterRealive(sl, pool, -1, birthCount);
        }

        #endregion

        #region Quái vật chết
        /// <summary>
        /// Gọi khi quái vật chết
        /// </summary>
        public void OnReallyDied(Monster deadMonster)
        {
        }

        #endregion

        #region Đọc dữ liệu cấu hình quái

        /// <summary>
        /// Đọc dữ liệu quái vật từ XMLNode
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private void InitMonsterStaticInfo(XElement xmlNode)
        {
            this.MonsterInfo.Name = xmlNode.Attribute("Name").Value;
            this.MonsterInfo.ExtensionID = int.Parse(xmlNode.Attribute("ID").Value);
            this.MonsterInfo.SeekRange = int.Parse(xmlNode.Attribute("SeekRange").Value);
            this.MonsterInfo.Level = int.Parse(xmlNode.Attribute("Level").Value);
            this.MonsterInfo.Title = xmlNode.Attribute("Title").Value;
            int elementalIdx = int.Parse(xmlNode.Attribute("Elemental").Value);
            this.MonsterInfo.Elemental = elementalIdx <= (int)KE_SERIES_TYPE.series_none || elementalIdx >= (int)KE_SERIES_TYPE.series_num ? KE_SERIES_TYPE.series_none : (KE_SERIES_TYPE)elementalIdx;
            this.MonsterInfo.Exp = int.Parse(xmlNode.Attribute("Level").Value);
            this.MonsterInfo.MaxHP = int.Parse(xmlNode.Attribute("MaxHP").Value);
            this.MonsterInfo.Atk = int.Parse(xmlNode.Attribute("Atk").Value);
            this.MonsterInfo.IceAtk = int.Parse(xmlNode.Attribute("IceAtk").Value);
            this.MonsterInfo.FireAtk = int.Parse(xmlNode.Attribute("FireAtk").Value);
            this.MonsterInfo.LightningAtk = int.Parse(xmlNode.Attribute("LightningAtk").Value);
            this.MonsterInfo.PoisonAtk = int.Parse(xmlNode.Attribute("PoisonAtk").Value);
            this.MonsterInfo.Def = int.Parse(xmlNode.Attribute("Def").Value);
            this.MonsterInfo.IceRes = int.Parse(xmlNode.Attribute("IceRes").Value);
            this.MonsterInfo.FireRes = int.Parse(xmlNode.Attribute("FireRes").Value);
            this.MonsterInfo.LightningRes = int.Parse(xmlNode.Attribute("LightningRes").Value);
            this.MonsterInfo.PoisonRes = int.Parse(xmlNode.Attribute("PoisonRes").Value);
            this.MonsterInfo.Dodge = int.Parse(xmlNode.Attribute("Dodge").Value);
            this.MonsterInfo.Hit = int.Parse(xmlNode.Attribute("Hit").Value);
            this.MonsterInfo.Crit = int.Parse(xmlNode.Attribute("Crit").Value);
            this.MonsterInfo.CritDef = int.Parse(xmlNode.Attribute("CritDef").Value);
            this.MonsterInfo.MoveSpeed = int.Parse(xmlNode.Attribute("MoveSpeed").Value);
            this.MonsterInfo.AtkSpeed = int.Parse(xmlNode.Attribute("AtkSpeed").Value);
            this.MonsterInfo.AttackRange = int.Parse(xmlNode.Attribute("AttackRange").Value);
            this.MonsterInfo.Camp = int.Parse(xmlNode.Attribute("Camp").Value);
            this.MonsterInfo.AIID = string.IsNullOrEmpty(xmlNode.Attribute("AIID").Value) ? -1 : int.Parse(xmlNode.Attribute("AIID").Value);
            this.MonsterInfo.DropID = int.Parse(xmlNode.Attribute("DropID").Value);
            this.MonsterInfo.ScriptID = string.IsNullOrEmpty(xmlNode.Attribute("ScriptID").Value) ? -1 : int.Parse(xmlNode.Attribute("ScriptID").Value);
            this.MonsterInfo.AIType = string.IsNullOrEmpty(xmlNode.Attribute("AIType").Value) ? MonsterAIType.Normal : (MonsterAIType)int.Parse(xmlNode.Attribute("AIType").Value);
        }
        #endregion
    }
}
