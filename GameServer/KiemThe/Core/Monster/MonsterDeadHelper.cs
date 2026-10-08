using GameServer.Core.Executor;
using GameServer.KiemThe;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.Logic.Manager.Battle;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GameServer.KiemThe.GameEvents.BaiHuTang;
using GameServer.KiemThe.LuaSystem;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Logic.Manager.Pet;
using System.Threading;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.KiemThe.GameEvents.FlowerTower;

namespace GameServer.Logic
{
    public static class StaticRandom
    {
        static int seed = Environment.TickCount;

        static readonly System.Threading.ThreadLocal<System.Random> random =
            new System.Threading.ThreadLocal<System.Random>(() => new System.Random(System.Threading.Interlocked.Increment(ref seed)));

        public static bool PositiveOutcome(int rate)
        {
            const int RANGE = 1000000;
            const int SEG = 10000;
            var point = random.Value.Next(RANGE);
            var anchor = random.Value.Next(RANGE - SEG * rate);
            return point >= anchor && point < anchor + rate * SEG;
        }
        
        public static bool PositiveOutcome(double rate)
        {
            const int RANGE = 1000000;
            const int SEG = 10000;
            var point = random.Value.Next(RANGE);
            var anchor = random.Value.Next(RANGE - (int)(SEG * rate));
            return point >= anchor && point < anchor + rate * SEG;
        }

        public static T AccumulatedProbabilitySelection<T>(List<T> item, List<int> weight, int luck = 0)
        {
            var poolSize = weight.Sum();
            var r = random.Value.Next(poolSize);
            r = (int)(r + luck / 100f * r);
            var accumulatedProbability = 0;
            for (var i = 0; i < item.Count; ++i)
            {
                accumulatedProbability += weight[i];
                if (r < accumulatedProbability)
                    return item[i];
            }

            return item.Last();
        }

        public static int GetRandom(int PoolSize)
        {
            return random.Value.Next(PoolSize);
        }

        public static int GetRandom(int minValue, int maxValue)
        {
            return random.Value.Next(minValue, maxValue + 1);
        }

        public static int testing()
        {
                
            var t = new KeyValuePair<int, int>(8, 256);
            return AccumulatedProbabilitySelection(
                Enumerable.Range(8, 5).ToList(),
                new List<int> { 256, 32, 16, 8, 4 });
        }
        
        public static void Shuffle<T> (List<T> array)
        {
            var n = array.Count;
            while (n > 1) 
            {
                var k = random.Value.Next(n--);
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
    }
    
    /// <summary>
    /// Classs thực hiện toàn bộ các sự kiện khi quái chết
    /// </summary>
    public class MonsterDeadHelper
    {
        public static int TotalMonsterDie = 0;
        public static int TotalFireCampDropped = 0;
        public static int TotalEliteMonsterSpawned = 0;
        public static int TotalAttempToSpawnElite = 0;

        // Exp trên 1 level
        public static Dictionary<int, int> DefaultExpPerLevel = new Dictionary<int, int>();

        /// <summary>
        /// Hồi phục trên level
        /// </summary>
        public static Dictionary<int, int> LifeReplenish = new Dictionary<int, int>();

        /// <summary>
        /// Chính xác trên 1 level
        /// </summary>
        public static Dictionary<int, int> AR = new Dictionary<int, int>();

        /// <summary>
        /// Phòng thủ trên 1 level
        /// </summary>
        public static Dictionary<int, int> Defense = new Dictionary<int, int>();

        /// <summary>
        /// MIn Dagme
        /// </summary>
        public static Dictionary<int, int> MinDamage = new Dictionary<int, int>();

        /// <summary>
        /// Max Dagme
        /// </summary>
        public static Dictionary<int, int> MaxDamage = new Dictionary<int, int>();

        public static Dictionary<int, int> Resist = new Dictionary<int, int>();

        public static Dictionary<int, int> Damage = new Dictionary<int, int>();

        public static Dictionary<int, int> BossExpPerLevel = new Dictionary<int, int>();

        public static void ExpInit()
        {
            DefaultExpPerLevel.Add(1, 50);
            DefaultExpPerLevel.Add(9, 50);

            DefaultExpPerLevel.Add(10, 100);
            DefaultExpPerLevel.Add(19, 100);

            DefaultExpPerLevel.Add(20, 150);
            DefaultExpPerLevel.Add(29, 150);

            DefaultExpPerLevel.Add(30, 200);
            DefaultExpPerLevel.Add(39, 200);

            DefaultExpPerLevel.Add(40, 300);
            DefaultExpPerLevel.Add(49, 300);

            DefaultExpPerLevel.Add(50, 400);
            DefaultExpPerLevel.Add(59, 400);

            DefaultExpPerLevel.Add(60, 500);
            DefaultExpPerLevel.Add(69, 500);

            DefaultExpPerLevel.Add(70, 650);
            DefaultExpPerLevel.Add(79, 650);

            DefaultExpPerLevel.Add(80, 800);
            DefaultExpPerLevel.Add(89, 800);

            DefaultExpPerLevel.Add(90, 850);
            DefaultExpPerLevel.Add(99, 850);

            DefaultExpPerLevel.Add(100, 900);
            DefaultExpPerLevel.Add(109, 900);

            DefaultExpPerLevel.Add(110, 950);
            DefaultExpPerLevel.Add(119, 950);

            DefaultExpPerLevel.Add(120, 1000);
            DefaultExpPerLevel.Add(129, 1000);

            DefaultExpPerLevel.Add(130, 1100);
            DefaultExpPerLevel.Add(139, 1100);

            DefaultExpPerLevel.Add(140, 1250);
            DefaultExpPerLevel.Add(150, 1250);

            LifeReplenish.Add(1, 5);
            LifeReplenish.Add(10, 10);
            LifeReplenish.Add(20, 65);
            LifeReplenish.Add(60, 250);
            LifeReplenish.Add(100, 750);
            LifeReplenish.Add(150, 750);

            AR.Add(1, 30);
            AR.Add(10, 70);
            AR.Add(100, 700);
            AR.Add(150, 1050);

            Defense.Add(1, 5);
            Defense.Add(10, 5);
            Defense.Add(11, 10);
            Defense.Add(100, 200);
            Defense.Add(150, 300);

            Resist.Add(1, 20);
            Resist.Add(9, 28);
            Resist.Add(10, 65);
            Resist.Add(100, 245);
            Resist.Add(150, 245);

            //Damage.Add(1, 1);
            //Damage.Add(9, 10);
            //Damage.Add(10, 20);
            //Damage.Add(30, 45);
            //Damage.Add(60, 120);
            //Damage.Add(100, 230);
            //Damage.Add(150, 230);

            Damage.Add(1, 1);
            Damage.Add(9, 2);
            Damage.Add(10, 5);
            Damage.Add(30, 10);
            Damage.Add(60, 30);
            Damage.Add(100, 50);
            Damage.Add(150, 50);
        }

        public static int GetMaxDamgePerLevel(int MonsterLevel)
        {
            return 10;
        }

        public static int GetMinDamgePerLevel(int MonsterLevel)
        {
            return 1;
        }

        public static int GetDefenseLevel(int MonsterLevel)
        {
            if (Defense.TryGetValue(MonsterLevel, out int Key))
            {
                return Key;
            }

            var findmin = Defense.OrderByDescending(x => x.Key).Where(x => x.Key <= MonsterLevel).FirstOrDefault();

            var findmax = Defense.OrderBy(x => x.Key).Where(x => x.Key >= MonsterLevel).FirstOrDefault();

            double LeveDif = findmax.Key - findmin.Key;

            double ValueDif = findmax.Value - findmin.Value;

            double PerValue = (ValueDif / LeveDif);

            int DamgeFind = MonsterLevel - findmin.Key;

            double ValueAdd = (PerValue * DamgeFind) + findmin.Value;

            return (int)ValueAdd;
        }

        public static int GetARLevel(int MonsterLevel)
        {
            if (AR.TryGetValue(MonsterLevel, out int Key))
            {
                return Key;
            }

            var findmin = AR.OrderByDescending(x => x.Key).FirstOrDefault(x => x.Key <= MonsterLevel);

            var findmax = AR.OrderBy(x => x.Key).FirstOrDefault(x => x.Key >= MonsterLevel);

            double LeveDif = findmax.Key - findmin.Key;

            double ValueDif = findmax.Value - findmin.Value;

            double PerValue = (ValueDif / LeveDif);

            int DamgeFind = MonsterLevel - findmin.Key;

            double ValueAdd = (PerValue * DamgeFind) + findmin.Value;

            return (int)ValueAdd;
        }

        public static int GetLifeReplenishLevel(int MonsterLevel)
        {
            if (LifeReplenish.TryGetValue(MonsterLevel, out int Key))
            {
                return Key;
            }

            var findmin = LifeReplenish.OrderByDescending(x => x.Key).Where(x => x.Key <= MonsterLevel)
                .FirstOrDefault();

            var findmax = LifeReplenish.OrderBy(x => x.Key).Where(x => x.Key >= MonsterLevel).FirstOrDefault();

            double LeveDif = findmax.Key - findmin.Key;

            double ValueDif = findmax.Value - findmin.Value;

            double PerValue = (ValueDif / LeveDif);

            int DamgeFind = MonsterLevel - findmin.Key;

            double ValueAdd = (PerValue * DamgeFind) + findmin.Value;

            return (int)ValueAdd;
        }

        public static int GetResistPerLevel(int MonsterLevel)
        {
            if (Resist.TryGetValue(MonsterLevel, out int Key))
            {
                return Key;
            }

            var findmin = Resist.OrderByDescending(x => x.Key).Where(x => x.Key <= MonsterLevel).FirstOrDefault();

            var findmax = Resist.OrderBy(x => x.Key).Where(x => x.Key >= MonsterLevel).FirstOrDefault();

            double LeveDif = findmax.Key - findmin.Key;

            double ValueDif = findmax.Value - findmin.Value;

            double PerValue = (ValueDif / LeveDif);

            int DamgeFind = MonsterLevel - findmin.Key;

            double ValueAdd = (PerValue * DamgeFind) + findmin.Value;

            return (int)ValueAdd;
        }

        public static double GetDamgeBYSeries(DAMAGE_TYPE DamgeType, KE_SERIES_TYPE Series)
        {
            double OutputData = 1;

            switch (Series)
            {
                case KE_SERIES_TYPE.series_earth:
                    if (DamgeType != DAMAGE_TYPE.damage_light)
                    {
                        return 0.5;
                    }

                    break;

                case KE_SERIES_TYPE.series_fire:
                    if (DamgeType != DAMAGE_TYPE.damage_fire)
                    {
                        return 0.5;
                    }

                    break;

                case KE_SERIES_TYPE.series_metal:
                    if (DamgeType != DAMAGE_TYPE.damage_physics)
                    {
                        return 0.5;
                    }

                    break;

                case KE_SERIES_TYPE.series_wood:
                    if (DamgeType != DAMAGE_TYPE.damage_poison)
                    {
                        return 0.5;
                    }

                    break;

                case KE_SERIES_TYPE.series_water:
                    if (DamgeType != DAMAGE_TYPE.damage_cold)
                    {
                        return 0.5;
                    }

                    break;
            }

            return OutputData;
        }

        public static int GetDamagePerLevel(int MonsterLevel, DAMAGE_TYPE DamgeType, KE_SERIES_TYPE Series)
        {
            if (Damage.TryGetValue(MonsterLevel, out int Key))
            {
                return Key;
            }

            var findmin = Damage.OrderByDescending(x => x.Key).Where(x => x.Key <= MonsterLevel).FirstOrDefault();

            var findmax = Damage.OrderBy(x => x.Key).Where(x => x.Key >= MonsterLevel).FirstOrDefault();

            double LeveDif = findmax.Key - findmin.Key;

            double ValueDif = findmax.Value - findmin.Value;

            double PerValue = (ValueDif / LeveDif);

            int DamgeFind = MonsterLevel - findmin.Key;

            double ValueAdd = (PerValue * DamgeFind) + findmin.Value;

            double FinalValue = GetDamgeBYSeries(DamgeType, Series) * ValueAdd;

            return (int)ValueAdd;
        }

        public static int _CalcPlayerExpByLevel(int nExp, int nSelfLevel, int nTarLevel)
        {
            int nSubLevel = nSelfLevel - nTarLevel;
            int nGetExp = 0;

            if (nSelfLevel >= 100) // Nếu cấp bản thân mà trên 100
            {
                // Nếu mà Con quái có cấp >= 90 thì thôi ko giảm exp
                if (nTarLevel >= 90)
                    nGetExp = nExp;
                else
                    nGetExp = 1;
            }
            else if (nSubLevel < -13) // (-~,-13)		Nếu mà đánh quái > 13 level thì EXP nhận được là 1
            {
                nGetExp = 1;
            }
            else if (nSubLevel < -5) //Nếu đánh quái > 5 cấp thì EXP bị chia ra
            {
                nGetExp = nExp * (14 + nSubLevel) / 10;
            }
            else if (nSubLevel <= 5) //Nếu đánh quái <= 5 cấp thì EXP giữ nguyên 100
            {
                nGetExp = nExp;
            }
            else if (nSubLevel <= 20) //Nếu cấp bản thân mà > 20 cấp quái thì EXP bị chia ra
            {
                nGetExp = nExp * (30 - nSubLevel) / 25;
            }
            else //Các trường hợp còn lại EXP / 2.5
            {
                nGetExp = nExp * 2 / 5;
            }

            if (nGetExp <= 0) // Nếu EXP chia ra nhỏ hơn 0 thì expx nhận được là 1
                nGetExp = 1;

            return nGetExp;
        }

        public static void GetBlankSpace(GameMap gameMap, int XFrom, int YFrom, int XTo, int YTo, out int XResult, out int YResult) 
        {
            XResult = XTo;
            YResult = YTo;
            if (!gameMap.CanMove(XTo / gameMap.MapGridWidth, YTo / gameMap.MapGridHeight)) 
            {
                return;
            }

            while (true) 
            {
                var XMid = (XFrom + XTo) / 2;
                var YMid = (YFrom + YTo) / 2;
                var canMove = gameMap.CanMove(XMid / gameMap.MapGridWidth, YMid / gameMap.MapGridHeight);
                if (canMove)
                {
                    XTo = XMid;
                    YTo = YMid;
                    XResult = XTo;
                    YResult = YTo;
                } else {
                    XFrom = XMid;
                    YFrom = YMid;
                }

                if (Math.Abs(XFrom - XTo) <= 10 && Math.Abs(YFrom - YTo) <= 10) 
                {
                    break;
                }
            }
        }

        public static void CreateFireCampStep1(GameMap gameMap, int XPos, int YPos, KPlayer Host, int copySceneID = -1, int camFireLevel = 0)
        {
            if (TotalFireCampDropped < int.MaxValue)
                TotalFireCampDropped++;

            GrowPointXML _Config = new GrowPointXML();
            _Config.CollectTick = 10000;
            _Config.Name = "Đống củi";
            _Config.ResID = 20016;
            _Config.RespawnTime = -100;
            _Config.InteruptIfTakeDamage = false;
            _Config.ScriptID = -1;

            Action<KPlayer> ActionWork = (x) => CreateFireCampStep2(gameMap, Host, XPos, YPos, copySceneID, camFireLevel);

            if (!gameMap.CanMove(XPos / gameMap.MapGridWidth, YPos / gameMap.MapGridHeight))
            {
                LogManager.WriteLog(LogTypes.Diagnostic, $"Tạo đống củi tại vị trí lỗi {gameMap.MapName}, ({XPos}, {YPos}), grid ({XPos / gameMap.MapGridWidth}, {YPos / gameMap.MapGridHeight})");
                GetBlankSpace(gameMap, XPos, YPos, Host.PosX, Host.PosY, out var newPosX, out var newPosY);
                XPos = newPosX;
                YPos = newPosY;
            }

            // Tạo 1 lửa trại
            GrowPoint growPoint = new GrowPoint()
            {
                ID = KTGrowPointManager.AutoIndexManager.GetNewID(),
                Data = _Config,
                Name = _Config.Name,
                ObjectType = ObjectTypes.OT_GROWPOINT,
                MapCode = gameMap.MapCode,
                CurrentCopyMapID = copySceneID,
                CurrentPos = new System.Windows.Point(XPos, YPos),
                CurrentGrid = new System.Windows.Point(XPos / gameMap.MapGridWidth, YPos / gameMap.MapGridHeight),
                RespawnTime = _Config.RespawnTime,
                ScriptID = _Config.ScriptID,
                LifeTime = 300000,
                Alive = true,
                ConditionCheck = (player) =>
                {
                    if (Host.RoleID != player.RoleID)
                    {
                        if (Host.TeamID == -1)
                        {
                            PlayerManager.ShowNotification(player, "Lửa trại này thuộc về :" + Host.RoleName);
                            return false;
                        }
                        else if (Host.TeamID != -1)
                        {
                            if (Host.TeamID != player.TeamID)
                            {
                                PlayerManager.ShowNotification(player, "Lửa trại này thuộc về :" + Host.RoleName);
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                },

                GrowPointCollectCompleted = ActionWork,
            };
            // Thực hiện tự động xóa
            growPoint.ProcessAutoRemoveTimeout();
            KTGrowPointManager.GrowPoints[growPoint.ID] = growPoint;
            // Thêm điểm thu thập vào đối tượng quản lý map
            KTGrowPointManager.AddToMap(growPoint);
        }

        public static void CreateFireCampStep2(GameMap gameMap, KPlayer host, int PosX, int PosY, int copySceneID, int campFireLevel)
        {
            KDynamicArea dynArea = KTDynamicAreaManager.Add(gameMap.MapCode, copySceneID, "Lửa Trại", 20022, PosX, PosY,
                60 * 5, 5, 500, -1, "LUATRAI_" + host.RoleID + "");
            dynArea.OnStayTick = (go) =>
            {
                if (go is KPlayer player)
                {
                    if (host.TeamID != ((KPlayer)go).TeamID || host.TeamID == -1) return;
                    if (player.IsDead())
                        return;
                    
                    MonsterDeadHelper.FireCampTick(gameMap, player, campFireLevel);
                    player.m_nTickCampFire = KTGlobal.GetCurrentTimeMilis();
                    
                    var elapsed = DateTime.Now.Ticks / 10000 - dynArea.StartTicks;
                    var buffDuration = (long)(60 * 5 * 1000 - elapsed);
                    if (!player.Buffs.HasBuff(377))
                    {
                        player.Buffs.AddBuff(377, 1, buffDuration, 1, false);
                    }
                }
            };
            dynArea.OnLeave = (go) =>
            {
                if (!(go is KPlayer player))
                    return;
            
                if (host.TeamID == -1)
                    return; // host không có party
            
                if (host.TeamID != player.TeamID)
                    return; // không phải lửa trại của mình
                
                if (player.Buffs.HasBuff(377))
                {
                    player.Buffs.RemoveBuff(377);
                }
            };
        }

        private static void FireCampTick(GameMap gameMap, KPlayer player, int campFireLevel)
        {
            // Nếu không thấy người chơi
            if (player == null)
            {
                return;
            }

            ExpProsec(gameMap, player, campFireLevel);
        }

        private static void ExpProsec(GameMap gameMap, KPlayer x, int campFireLevel)
        {
            int EXPGAIN;

            if (x.m_Level <= 50)
            {
                EXPGAIN = 900;
            }
            else if (x.m_Level <= 89)
            {
                EXPGAIN = 1500;
            }
            else
            {
                EXPGAIN = 2500;
            }

            // nếu chênh lệch level của người chơi và lửa trại trên 15 cấp thì chỉ được nhận 1 exp
            if (Math.Abs(x.m_Level - campFireLevel) > 15)
            {
                EXPGAIN = 1;
            }
            else
            {
                EXPGAIN += EXPGAIN * (x as KPlayer).m_nExpEnhancePercent / 100;
                EXPGAIN += EXPGAIN * (x as KPlayer).m_nExpAddtionP / 100;

                // Nếu có rượu thì tăng x2 kinh nghiệm, 378 là ID skill buff rượu
                if (x.Buffs.HasBuff(378))
                {
                    var teamMatesCount = 0;
                    if (x.Teammates != null)
                    {
                        // đếm số lượng thành viên tổ đội có uống rượu
                        teamMatesCount = x.Teammates.Count(p => p.Buffs.HasBuff(378));
                    }
                    // mỗi thành viên trong tổ đội mà có rượu được nhận thêm 20% exp
                    var expRate = teamMatesCount * 0.2f;
                    // nhưng không được quá 60%
                    expRate = Math.Min(expRate, 0.6f);
            
                    EXPGAIN = (int)(EXPGAIN * (1f + expRate));
                }
            }
            if (x.Buffs.HasBuff(332))
            {
                // trừ kinh nghiệm của tu luyện châu
                if (x.m_longMaxExpXiulianzhu < 0)
                {
                    x.Buffs.RemoveBuff(332);
                }
                else
                {
                    x.m_longMaxExpXiulianzhu -= EXPGAIN;
                    PlayerManager.ShowNotification(x, string.Format("Quỹ kinh nghiệm tu luyện châu còn lại: {0}", x.m_longMaxExpXiulianzhu));
                }
            }    
            else
            {
                // reset lại quỹ kn đánh quái
                x.m_longMaxExpXiulianzhu = 0;
            }    
                
            GameManager.ClientMgr.ProcessRoleExperience(x as KPlayer, EXPGAIN, true, false, true, "LUATRAI");
        }

        /// <summary>
        /// Thực hiện tăng kinh nghiệm tu luyện mật tịch
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ExpInput"></param>
        public static void BookProseccExp(KPlayer player, int ExpInput, bool includeXiuLianZhu = true)
        {
            if (includeXiuLianZhu)
            {
                // Nếu không có thời gian Tu Luyện
                if (player.XiuLianZhu_Exp <= 0)
                {
                    return;
                }

                if (player.XiuLianZhu_CurTimeSecond < (int)(KTGlobal.GetCurrentTimeMilis() / 1000))
                {
                    player.XiuLianZhu_Exp = 0;
                    return;
                }

                if (!player.Buffs.HasBuff(332)) {
                    // 332 là ID của buff x2 kinh nghiệm từ tu luyện châu
                    // nếu không có buff này thì không được + exp cho mật tịch
                    return;
                }
            }


            if (player.GoodsDataList != null)
            {
                GoodsData findbook = null;
                lock (player.GoodsDataList)
                {
                    findbook = player.GoodsDataList.Where(x => x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_BOOK)
                        .FirstOrDefault();
                }

                if (findbook != null)
                {
                    int LevelBook = 0;
                    if (findbook.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string BookLevel))
                    {
                        LevelBook = Int32.Parse(BookLevel);
                    }

                    int BoookExp = 0;
                    if (findbook.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string ExpLevel))
                    {
                        BoookExp = Int32.Parse(ExpLevel);
                    }

                    if (LevelBook < 100)
                    {
                        ItemData BookData = ItemManager.GetItemTemplate(findbook.GoodsID);
                        if (BookData != null)
                        {
                            int ExpMax = ItemManager.GetMaxbookEXP(LevelBook, BookData.Level);
                            int ExpCanGain = ItemManager.GetEXPEarnPerExp(ExpInput);
                            //int ExpCanGain = 1000; /// TẠM THỜI SET CHẾT
                            int MaxExpNext = 0;

                            bool IsWriterToDb = false;

                            long Now = TimeUtil.NOW();

                            // Nếu số exp nhận được mà lớn hơn 0
                            if (ExpCanGain > 0)
                            {
                                // ghi log 
                                LogManager.WriteLog(LogTypes.Book, $"Player {player.RoleName} [{player.RoleID}] has book {findbook.ItemName} [dbid = {findbook.Id}, goodID = {findbook.GoodsID}] + {ExpCanGain} exp");
                                if (ExpCanGain + BoookExp > ExpMax)
                                {
                                    LevelBook = LevelBook + 1;
                                    BoookExp = (ExpCanGain + BoookExp) - ExpMax;
                                    MaxExpNext = ItemManager.GetMaxbookEXP(LevelBook, BookData.Level);

                                    // nếu mà cấp độ tăng lên thì cũng lưu vào DB
                                    IsWriterToDb = true;
                                    // đánh dấu thời gian lưu lại dưới đỡ phải check
                                    player.LastBookExpTicks = Now;
                                }
                                else
                                {
                                    BoookExp = BoookExp + ExpCanGain;
                                    MaxExpNext = ExpMax;
                                }

                                if (includeXiuLianZhu)
                                {
                                    // Lấy Min của kinh nghiệm còn lại
                                    ExpCanGain = Math.Min(ExpCanGain, player.XiuLianZhu_Exp);
                                    // Giảm kinh nghiệm mật tịch tương ứng
                                    player.XiuLianZhu_Exp -= ExpCanGain;
                                }


                                // LƯU LẠI BOOK

                                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                                TotalUpdate.Add(UPDATEITEM.ROLEID, player.RoleID);
                                TotalUpdate.Add(UPDATEITEM.ITEMDBID, findbook.Id);

                                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();

                                OtherParams.Add(ItemPramenter.Pram_1, LevelBook + "");
                                OtherParams.Add(ItemPramenter.Pram_2, BoookExp + "");
                                OtherParams.Add(ItemPramenter.Pram_3, MaxExpNext + "");

                                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);


                                // Nếu train quái liên tục thì phải 120s mới được ghi vào DB 1 lần tránh spam db
                                if (Now - player.LastBookExpTicks > 120000)
                                {
                                    player.LastBookExpTicks = Now;
                                    IsWriterToDb = true;
                                }

                                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, findbook, player, "BOOKEXPCHANGE",
                                        IsWriterToDb))
                                {
                                    PlayerManager.ShowNotification(player, "Có lỗi khi nhận kinh nghiệm mật tịch");
                                }
                                else
                                {
                                    if (includeXiuLianZhu) {
                                        PlayerManager.ShowNotification(player,
                                            "Nhận được " + ExpCanGain + " kinh nghiệm tu luyện, kinh nghiệm còn lại " +
                                            player.XiuLianZhu_Exp + ".");
                                    }

                                    // Thêm kinh nghiệm cho kỹ năng tương ứng
                                    if (KSkill.GetSkillData(BookData.BookProperty.SkillID1) != null)
                                    {
                                        player.Skills.AddSkillExp(BookData.BookProperty.SkillID1, ExpCanGain);
                                    }

                                    if (KSkill.GetSkillData(BookData.BookProperty.SkillID2) != null)
                                    {
                                        player.Skills.AddSkillExp(BookData.BookProperty.SkillID2, ExpCanGain);
                                    }

                                    if (KSkill.GetSkillData(BookData.BookProperty.SkillID3) != null)
                                    {
                                        player.Skills.AddSkillExp(BookData.BookProperty.SkillID3, ExpCanGain);
                                    }

                                    if (KSkill.GetSkillData(BookData.BookProperty.SkillID4) != null)
                                    {
                                        player.Skills.AddSkillExp(BookData.BookProperty.SkillID4, ExpCanGain);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Thực hiện tăng kinh nghiệm tu luyện cho pet
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ExpInput"></param>
        public static void PetProseccExp(KPlayer player, int ExpInput, int ShareExpP, int ExpEnhancePercent, int ExpAddtionP, int levelMonster)
        {
            if (player.GoodsDataList != null)
            {
                GoodsData findpet = null;
                lock (player.GoodsDataList)
                {
                    findpet = player.GoodsDataList.Where(x => x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_PET)
                        .FirstOrDefault();
                }

                if (findpet != null && levelMonster - findpet.Forge_level <= 15 && levelMonster - findpet.Forge_level >= 0)
                {
                    int final = _CalcPlayerExpByLevel(ExpInput, findpet.Forge_level, levelMonster);
                    if (ShareExpP > 0)
                    {
                        final += final * ShareExpP / 100;
                    }

                    int ExpCanGain = ItemManager.GetEXPEarnPerExp(final);
                    LogManager.WriteLog(LogTypes.Pet, $"player: {player.RoleName} có {ExpCanGain} KN PET tính toán xong");

                    ItemData PetData = ItemManager.GetItemTemplate(findpet.GoodsID);

                    PetManager.UpdateExpPet(player, findpet.Id, ExpCanGain);
                }

            }
        }

        /// <summary>
        /// Thực hiện Logic khi quái chết
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        public static void ProcessMonsterDeadByClient(SocketListener sl, TCPOutPacketPool pool, KPlayer client,
            Monster monster)
        {
            if (TotalMonsterDie < int.MaxValue)
                TotalMonsterDie++;
            
            GameMap Map = GameManager.MapMgr.DictMaps[monster.CurrentMapCode];

            KTLuaEnvironment.ExecuteMonsterEventScript_OnDie(Map, monster, client);
            
            // Gọi vào hàm quản lý quái
            EliteMonsterManager.getInstance().OnMonsterDie(monster);

            // Tổng lượng EXP nhận được khi đánh quái
            var MonsterExpRevice = (int)monster.m_Experience;

            // Nếu monster không có EXP được thiết lập sẵn thực hiện tính toán đựa trên TABLE EXP mặc định
            if (MonsterExpRevice == 0)
            {
                var find = DefaultExpPerLevel.Where(x => x.Key > monster.m_Level).FirstOrDefault();

                MonsterExpRevice = find.Value;
            }

            if (Event_2024Apr.IsEventBoss(monster.MonsterInfo.ExtensionID))
            {
                Event_2024Apr.SaveDamageRecord($"{monster.Tag}", monster.DamageTakeRecord);
            }

            int PrestigeCanGet = 0;
            int MoneyPresinal = 0;
            int MoneyGuild = 0;
            int Money = 0; // bạc ko khóa
            if (monster.IsWorldBoss() || monster.IsCustomBoss())
            {
                if (monster.m_Level == 55)
                {
                    MoneyPresinal = 10000; // chỉnh lại vì chưa pt tính năng old(3150)
                    MoneyGuild = 100; // 15750
                    PrestigeCanGet = 2;
                    Money = 10000;
                }
                else if (monster.m_Level == 75)
                {
                    MoneyPresinal = 10000; // 5250
                    MoneyGuild = 100; // 26250
                    PrestigeCanGet = 3;
                    Money = 15000;
                }
                else if (monster.m_Level == 95)
                {
                    MoneyPresinal = 10000; //15750
                    MoneyGuild = 100; // 78750
                    PrestigeCanGet = 5;
                    Money = 20000;
                }

                MonsterExpRevice = MonsterExpRevice * 250;
            }

            if (monster.IsEmperorTombBoss())
            {
                PrestigeCanGet = monster.PrestigeKillBossInEmperorTomp();
            }    

            if (monster.IsWorldBoss() || monster.IsCustomBoss() || monster.IsBaiHutang3Boss() || monster.IsEmperorTombBoss() || monster.IsBossUnicon()) 
            {
                var FindTop = monster.DamageTakeRecord.Values.OrderByDescending(x => x.TotalDamage).FirstOrDefault();
                const string congratNotificationFormat = "Chúc mừng {0} {1} đã đánh bại {2}.";
                var playerTitle = "";

                if (FindTop != null)
                {
                    if (FindTop.TotalDamage > 0)
                    {
                        if (!FindTop.IsTeam)
                        {
                            KPlayer _client = GameManager.ClientMgr.FindClient(FindTop.ID);
                            if (_client != null)
                            {
                                client = _client;
                            }

                            playerTitle = client.RoleSex == 1 ? "nữ hiệp" : "đại hiệp";
                        }
                        else
                        {
                            KPlayer _client = KTTeamManager.GetTeamLeader(FindTop.ID);
                            if (_client != null)
                            {
                                client = _client;
                            }

                            playerTitle = "tổ đội của";
                        }
                    }
                }
                
                var congratNotification = string.Format(
                    congratNotificationFormat,
                    playerTitle, 
                    KTGlobal.CreateStringByColor(client.RoleName, ColorType.Yellow),
                    KTGlobal.CreateStringByColor(monster.RoleName, ColorType.Importal));
            
                KTGlobal.SendSystemEventNotification(congratNotification);   

            }

            if ((new[] { MonsterAIType.Elite, MonsterAIType.Leader, MonsterAIType.Boss }).Contains(monster.MonsterType))
            {
                if (Battel_SonJin_Manager.IsInBattle(client) // tống kim
                    || Map.MapCode == 273 // vượt ải gia tộc
                    || BaiHuTang.IsInBaiHuTang(client) // bạch hổ đường
                    || EmperorTomb.IsInsideEmperorTombMap(client) // tần lăng
                    || Event_2024Apr.IsEventBoss(monster.MonsterInfo.ExtensionID)) // boss event tháng 4 2024
                {
                    // bỏ qua không tạo lửa trại
                }
                else
                {
                    CreateFireCampStep1(Map, (int)monster.CurrentPos.X, (int)monster.CurrentPos.Y, client,
                        monster.CurrentCopyMapID, monster.m_Level);
                }
            }
            else if (monster.MonsterType == MonsterAIType.Normal)
            {
                // Nếu ở trong tống kim thì không ra tinh anh
                if (!Battel_SonJin_Manager.IsInBattle(client))
                {
                    var canSpawnElite = StaticRandom.PositiveOutcome(1);
                    if (monster.m_Level < 30)
                    {
                        //  giảm tỉ lệ ra tinh anh xuống 1 nửa nếu quái level dưới 30
                        canSpawnElite = canSpawnElite && StaticRandom.PositiveOutcome(50);
                    }
                    if (canSpawnElite)
                    {
                        if (TotalAttempToSpawnElite < int.MaxValue)
                            TotalAttempToSpawnElite++;
                        var canSpawnLeader = StaticRandom.PositiveOutcome(10);
                        if (EliteMonsterManager.getInstance().CreateMonster(monster,
                            canSpawnLeader
                                ? MonsterAIType.Leader
                                : MonsterAIType.Elite,
                            monster.MonsterZoneNode.ID))
                            if (TotalEliteMonsterSpawned < int.MaxValue)
                            {
                                TotalEliteMonsterSpawned++;
                            }
                    }
                }
            }

            // NẾu tema có đội thì lấy ra
            if (client.TeamID != -1)
            {
                double ExpLess = MonsterExpRevice;
                List<KPlayer> TotalMember = client.Teammates;
                if (TotalMember.Count > 1)
                {
                    // Tính ra số EXP Cuối cùng sau khi được X lên với rate của tổ đội
                    double ExpRate = 1.0 * (100 + TotalMember.Count + 1) / 100;

                    double nMainExp = (MonsterExpRevice * ExpRate);

                    // Lấy ra tối đa số EXP có thể nhận từ tổng lượng sát thương gây lên quái
                    int ToalExpCanGet = 90;


                    // tính ra số EXP cuối cùng
                    ExpLess = (nMainExp * ToalExpCanGet) / 100;
                }

                // Lấy ra số exp tối đa mà thành viên được SHARE nếu không phải thằng dứt điểm
                double ExpShare = (ExpLess * KTGlobal.KD_MAX_TEAMATE_EXP_SHARE) / 100;

                foreach (KPlayer member in TotalMember)
                {
                    // nếu thằng thành viên này mà khác với map của thằng host thì đéo được exp
                    if (member.MapCode != client.MapCode)
                    {
                        continue;
                    }

                    // Nếu mà không trong phạm vi quái chết thì đéo được EXP
                    if (!Global.InCircle(new Point(member.PosX, member.PosY), monster.CurrentPos, 1000))
                    {
                        continue;
                    }

                    // nếu đã chết thì đéo được EXP
                    if (member.IsDead())
                    {
                        continue;
                    }

                    int FinalExp = 0;
                    // Nếu không phải thằng đã lasthit quái
                    if (member.RoleID != client.RoleID)
                    {
                        FinalExp = _CalcPlayerExpByLevel((int)ExpShare, member.m_Level, monster.m_Level);
                        // Kinh nghiệm nhận được từ đồng đội khi đánh quái
                        if (member.m_nShareExpP > 0)
                        {
                            // Cộng thêm lượng kinh nghiệm tương ứng từ đồng đội khi đánh quái
                            FinalExp += FinalExp * member.m_nShareExpP / 100;
                        }
                    }
                    else // Nếu là thằng đã lasthit quái
                    {
                        FinalExp = _CalcPlayerExpByLevel((int)ExpLess, member.m_Level, monster.m_Level);
                    }


                    if (PrestigeCanGet > 0)
                    {
                        int oldPrestige = member.Prestige;
                        member.Prestige = member.Prestige + PrestigeCanGet;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở Boss Thế Giới, uy danh tăng từ {2} lên {3}", member.RoleName, PrestigeCanGet, oldPrestige, member.Prestige));
                        KTGlobal.SendDefaultChat(member, string.Format("Nhận được <color=yellow>{0} Uy danh</color>.", PrestigeCanGet));
                    }

                    if (MoneyPresinal > 0 && member.GuildID > 0)
                    {
                        //Update tài sản bang hội
                        KTGlobal.AddMoney(member, MoneyPresinal, MoneyType.GuildMoney, "BOSSVLCT");
                    }

                    if (MoneyGuild > 0 && member.GuildID > 0)
                    {
                        //Update tài sản bang hội
                        KTGlobal.UpdateGuildMoney(MoneyGuild, member.GuildID, client);
                    }

                    if (Money > 0)
                    {
                        KTGlobal.AddMoney(member, Money, MoneyType.Bac, "BOSSVLCT");
                    }    

                    // Console.WriteLine("EXP :" + FinalExp + "| NAME :" + member.RoleName);

                    

                    // EXP cộng thêm từ TU LUYỆN châu buff các kiểu
                    FinalExp += FinalExp * member.m_nExpEnhancePercent / 100;
                    FinalExp += FinalExp * member.m_nExpAddtionP / 100;

                    // kiểm tra xem có buff tu luyện châu ko nếu quá exp từ tu luyện châu thì hủy buff
                    if (member.Buffs.HasBuff(332))
                    {
                        // so sánh với kinh nghiệm tối đa nhận được từ tu luyệ châu theo công thức lv * hour * 20000
                        if (member.m_longMaxExpXiulianzhu < 0)
                        {
                            member.Buffs.RemoveBuff(332);
                        }
                        else
                        {
                            member.m_longMaxExpXiulianzhu -= FinalExp;
                            PlayerManager.ShowNotification(member, string.Format("Quỹ kinh nghiệm tu luyện châu còn lại: {0}", member.m_longMaxExpXiulianzhu));
                        }
                    }
                    else
                    {
                        member.m_longMaxExpXiulianzhu = 0;
                    }    
                    //FIXME: tăng x10 kinh nghiệm cho bản test
                    // FinalExp *= 10;

                    // Thực hiện AddExp nhưng không ghi vào DB vì sẽ rất COST
                    GameManager.ClientMgr.ProcessRoleExperience(member, FinalExp, true, false, true, "MONSTERKILL");

                    MonsterDeadHelper.BookProseccExp(client, FinalExp);

                    if(monster.MonsterType == MonsterAIType.Elite || monster.MonsterType == MonsterAIType.Leader)
                    {
                        MonsterDeadHelper.PetProseccExp(member, (int)ExpLess, 0, member.m_nExpEnhancePercent, member.m_nExpAddtionP, monster.m_Level);
                        LogManager.WriteLog(LogTypes.Pet, $"member: {member.RoleName} có {ExpLess} KN PET chưa tính toán");
                    }    
                        

                    // Do Task khi giết quái xong
                    ProcessTask.Process(sl, pool, member, monster.RoleID, monster.MonsterInfo.ExtensionID, -1,
                        TaskTypes.KillMonster);

                    // TODO : add các sự kiện khác cần giết quái ở đây
                }
            }
            else // Nếu là giết quái solo một mình
            {
                // Lấy ra tối đa số EXP có thể nhận từ tổng lượng sát thương gây lên quái
                int ToalExpCanGet = 100;

                // tính ra số EXP cuối cùng
                int ExpLess = (MonsterExpRevice * ToalExpCanGet) / 100;

                // EXP cộng thêm từ TU LUYỆN châu buff các kiểu
                ExpLess += ExpLess * client.m_nExpEnhancePercent / 100;
                ExpLess += ExpLess * client.m_nExpAddtionP / 100;

                int FinalExp = _CalcPlayerExpByLevel(ExpLess, client.m_Level, monster.m_Level);


                // kiểm tra xem có buff tu luyện châu ko nếu quá exp từ tu luyện châu thì hủy buff
                if (client.Buffs.HasBuff(332))
                {
                    // so sánh với kinh nghiệm tối đa nhận được từ tu luyệ châu theo công thức lv * hour * 20000
                    if (client.m_longMaxExpXiulianzhu < 0)
                    {
                        client.Buffs.RemoveBuff(332);
                    }
                    else
                    {
                        client.m_longMaxExpXiulianzhu -= FinalExp;
                        PlayerManager.ShowNotification(client, string.Format("Quỹ kinh nghiệm tu luyện châu còn lại: {0}", client.m_longMaxExpXiulianzhu));
                    }
                }
                else
                {
                    client.m_longMaxExpXiulianzhu = 0;
                }
                //FIXME: tăng x10 kinh nghiệm cho bản test
                // FinalExp *= 10;

                if (client.IsDead())
                {
                    return;
                }

                // Cộng điểm uy danh
                if (PrestigeCanGet > 0)
                {
                    int oldPrestige = client.Prestige;
                    client.Prestige = client.Prestige + PrestigeCanGet;
                    LogManager.WriteLog(LogTypes.UyDanh,
                        $"PlayerID: {client.RoleName} nhận được {PrestigeCanGet} uy danh ở Boss Thế Giới, uy danh tăng từ {oldPrestige} lên {client.Prestige}");
                    KTGlobal.SendDefaultChat(client, $"Nhận được <color=yellow>{PrestigeCanGet} Uy danh</color>.");
                }

                // Cộng tài sản cá nhân
                if (MoneyPresinal > 0 && client.GuildID > 0)
                {
                    //Update tài sản bang hội
                    KTGlobal.AddMoney(client, MoneyPresinal, MoneyType.GuildMoney, "BOSSVLCT");
                }

                // Cộng tài sản bang
                if (MoneyGuild > 0 && client.GuildID > 0)
                {
                    //Update tài sản bang hội
                    KTGlobal.UpdateGuildMoney(MoneyGuild, client.GuildID, client);
                }

                if (Money > 0 )
                {
                    KTGlobal.AddMoney(client, Money, MoneyType.Bac, "BOSSVLCT");
                }    


                //TODO CHECK lại book nếu có tu luyện châu
                //Console.WriteLine("FINAL EXP :" + FinalExp);

                // Thực hiện AddExp nhưng không ghi vào DB vì sẽ rất COST
                GameManager.ClientMgr.ProcessRoleExperience(client, FinalExp, true, false, true);

                //TODO CHECK lại book nếu có tu luyện châu

                MonsterDeadHelper.BookProseccExp(client, FinalExp);

                if ((monster.MonsterType == MonsterAIType.Elite || monster.MonsterType == MonsterAIType.Leader))
                {
                    MonsterDeadHelper.PetProseccExp(client, MonsterExpRevice, 0, client.m_nExpEnhancePercent, client.m_nExpAddtionP, monster.m_Level);
                    LogManager.WriteLog(LogTypes.Pet, $"client: {client.RoleName} có {ExpLess} KN PET chưa tính toán");
                }    

                // Do Task khi giết quái xong
                ProcessTask.Process(sl, pool, client, monster.RoleID, monster.MonsterInfo.ExtensionID, -1,
                    TaskTypes.KillMonster);
                // TODO : add các sự kiện khác cần giết quái ở đây
            }

            if (client.Teammates == null || client.Teammates.Count == 0)
            {
                //SideTaskManager.getInstance().ProcessTaskMonster(client);
            }
            else
            {
                foreach (KPlayer _client in client.Teammates)
                {
                    // Nếu mà không trong phạm vi quái chết thì ko đc tính
                    if (!Global.InCircle(new Point(_client.PosX, _client.PosY), monster.CurrentPos, 1000))
                    {
                        continue;
                    }
                    //SideTaskManager.getInstance().ProcessTaskMonster(_client);
                }
            }

        }
    }
}