using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
// using System.Web;
using Tmsk.Tools;
using GameServer.Core.Executor;
using GameServer.Tools;

namespace Server.Tools
{
    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogTypes
    {
        Ignore = -1, //忽略
        Info = 0,
        Warning = 1,
        Error = 2,
        SQL = 3,
        Robot = 4,
        Analysis = 5, // 分析日志
        Attack = 6, // 攻击、伤害日志
        Data = 7,
        FileBan = 8,
        Exception = 9, //异常日志
        Alert = 10, //警告日志

        task = 11, //进程名称列表
        Item = 12, //进程名称列表
        EquipBody = 14, //进程名称列表
        Skill = 13, //进程名称列表
        BuyNpc = 14, //警告日志

        /// <summary>
        /// Giao dịch
        /// </summary>
        Trade = 15,


        /// <summary>
        /// Log vị trí
        /// </summary>
        RolePosition = 16,

        /// <summary>
        /// Nhiệm vụ
        /// </summary>
        Quest = 17,

        /// <summary>
        /// Bách Bảo Rương
        /// </summary>
        SeashellCircle = 18,

        /// <summary>
        /// Tống kim
        /// </summary>
        SongJinBattle = 19,

        /// <summary>
        /// Lua
        /// </summary>
        Lua = 20,

        /// <summary>
        /// Sự kiện
        /// </summary>
        GameMapEvents = 21,

        /// <summary>
        /// Phụ bản
        /// </summary>
        CopyScene = 22,


        /// <summary>
        /// Thi đấu môn phái
        /// </summary>
        FactionBattle = 23,

        /// <summary>
        /// Report Timer
        /// </summary>
        TimerReport = 24,

        /// <summary>
        /// Bang hội
        /// </summary>
        Guild = 25,

        /// <summary>
        /// Nạp thẻ
        /// </summary>
        Rechage = 26,

        /// <summary>
        /// Ngũ hành hồn thạch
        /// </summary>
        NHHT = 27,

        /// <summary>
        /// Chúc phúc
        /// </summary>
        PlayerPray = 28,

        /// <summary>
        /// Cường hóa
        /// </summary>
        Enhance = 29,

        /// <summary>
        /// Du Long Các
        /// </summary>
        YouLongGe = 30,

        /// <summary>
        /// Đăng xuất
        /// </summary>
        Logout = 31,



        /// <summary>
        /// Kỹ năng sống
        /// </summary>
        LifeSkill = 32,

        /// <summary>
        /// Chat
        /// </summary>
        Chat = 33,

        /// <summary>
        /// Tranh đoạt lãnh thổ
        /// </summary>
        GuildWarManager = 34,

        /// <summary>
        /// Võ lâm liên đấu
        /// </summary>
        TeamBattle = 35,

        /// <summary>
        /// Thông tin thiết bị đăng nhập
        /// </summary>
        DeviceInfo = 36,

        /// <summary>
        /// Tần Lăng
        /// </summary>
        EmperorTomb = 37,

        /// <summary>
        /// Vòng quay may mắn
        /// </summary>
        LuckyCircle = 38,

        /// <summary>
        /// Sự kiện đặc biệt
        /// </summary>
        SpecialEvent = 39,

        /// <summary>
        /// nạp xu qua web api
        /// </summary>
        NapXuWebApi = 40,
        /// <summary>
        /// MAIL
        /// </summary>
        Mail = 41,

        GM_CMD = 42,

        Shop = 43,

        Diagnostic = 99,

        //这是最后一个，永远都是最大的
        //有新加的加在前面，这个数值要变大
        KNB = 100,
        API = 101,

        /// <summary>
        /// ghi log cho uy danh
        /// </summary>
        UyDanh = 102,

        /// <summary>
        /// log cho sự kiện tết 2023
        /// </summary>
        Tet2023 = 103,

        /// <summary>
        /// Log cho song tu
        /// </summary>
        SongTu = 104,

        /// <summary>
        /// Log cho điểm năng động
        /// </summary>
        DiemNangDong = 105,

        /// <summary>
        /// Log cho hoạt động người chơi (chết, truyền tống phù)
        /// </summary>
        ActivityPlayer = 106,

        /// <summary>
        /// Log cho hoạt động của quái loại Boss trở lên
        /// </summary>
        BossMonster = 107,

        /// <summary>
        /// Log các thông báo hệ thống
        /// </summary>
        SystemChat = 108,

        /// <summary>
        /// Log các thông báo của huyền tinh
        /// </summary>
        Crystals = 109,

        /// <summary>
        /// Log các thông báo của nhiệm vụ phụ tuyến
        /// </summary>
        SideTask = 110,

        /// <summary>
        /// Log xổ số
        /// </summary>
        Lottery = 111,

        LotteryAward = 112,

        RandomBox = 113,

        Buff = 114,

        Pet = 115,

        PetRace = 116,

        /// <summary>
        /// Log giải đấu
        /// </summary>
        LeagueBattle = 117,

        BetLeagueBattle = 118,

        BossUnicon = 119,

        ChanNguyen = 120,

        FlowerBet = 121,

        UnionShop = 122,

        FlowerTower = 123,
        SlotMachine = 124,
        FishingEvent = 125,
        CrystalBag = 126,
        DanPhong = 127,
        BagSort = 128,

        /// <summary>
        /// Thêm log ghi lại thêm, trừ các loại tiền 
        /// </summary>
        Money = 129,
        DailyCheckin = 130,
        BaoThach = 131,
        TitleToItem = 132,
        BattlePass = 133,

        /// <summary>
        /// them log ghi lai +- exp mat tich, va skill mat tich
        /// </summary>
        Book = 134,
        EventTypes = 135,
        RefundEvent = 136,
        TCPInfo = 137,
        /// <summary>
        /// Log cho jackpot
        /// </summary>
        JackPot = 138,
        LuckySpin = 139,
        KuaFuReward = 140,
        KuaFuBalance = 141,

        /// <summary>
        /// Debug: log toàn bộ packet của người chơi đang điều tra bug
        /// </summary>
        PacketDebug = 142,
        Fatal = 1000,

    }

    public class LogFilePoolItem
    {
        public FileStream _FileStream;
        public StreamWriter _StreamWriter;
        public long OpenTimeOnHours; //打开的时间
        public long OpenTimeOnDayOfYear; //打开的时间

        ~LogFilePoolItem()
        {
            if (null != _StreamWriter)
            {
                try
                {
                    _StreamWriter.Close();
                }
                catch { }
                _StreamWriter = null;
            }
        }
    }

    /// <summary>
    /// 日志管理类
    /// </summary>
    public static class LogManager
    {
        public static Action<LogTypes, string, bool> OnLogWritten;
        /// <summary>
        /// 异常日志字典,防止相同异常重复写过多日志
        /// </summary>
        private static Dictionary<string, int> ExceptionCacheDict = new Dictionary<string, int>();

        /// <summary>
        /// 上次清理日志缓存的时间,定时清理是为了保证手动删除所有日志文件后,一段时间后还可以再次生成
        /// </summary>
        private static DateTime LastClearCacheTime;

        /// <summary>
        /// 日志文件按类型缓存对象字典
        /// </summary>
        public static Dictionary<LogTypes, LogFilePoolItem> LogType2FileDict { get; set; } = new Dictionary<LogTypes, LogFilePoolItem>();

        /// <summary>
        /// 允许实际写的日志级别
        /// </summary>
        public static LogTypes LogTypeToWrite
        {
            get;
            set;
        }

        /// <summary>
        /// 是否允许输出到dbgView窗口
        /// </summary>
        public static bool EnableDbgView = false;

        public static int ServerID = -1;

        /// <summary>
        /// 日志输出目录
        /// </summary>
        private static string _LogPath = string.Empty;

        /// <summary>
        /// 日志输出目录
        /// </summary>
        public static string LogPath
        {
            get
            {
                lock (mutex)
                {
                    if (_LogPath == string.Empty)
                    {
                        _LogPath = DataHelper2.CurrentDirectory + @"log/";
                        if (!System.IO.Directory.Exists(_LogPath))
                        {
                            System.IO.Directory.CreateDirectory(_LogPath);
                        }
                    }
                }

                return _LogPath;
            }
            set
            {
                lock (mutex)
                {
                    _LogPath = value;
                }

                if (!System.IO.Directory.Exists(_LogPath))
                {
                    System.IO.Directory.CreateDirectory(_LogPath);
                }
            }
        }

        /// <summary>
        /// 异常输出目录
        /// </summary>
        private static string _ExceptionPath = string.Empty;

        /// <summary>
        /// 异常个数
        /// </summary>
        private static int _ExceptionCount;

        /// <summary>
        /// 异常输出目录
        /// </summary>
        public static string ExceptionPath
        {
            get
            {
                lock (mutex)
                {
                    if (_ExceptionPath == string.Empty)
                    {
                        _ExceptionPath = DataHelper2.CurrentDirectory + @"Exception/";
                        if (!System.IO.Directory.Exists(_ExceptionPath))
                        {
                            System.IO.Directory.CreateDirectory(_ExceptionPath);
                        }
                    }
                }

                return _ExceptionPath;
            }
            set
            {
                lock (mutex)
                {
                    _ExceptionPath = value;
                }

                if (!System.IO.Directory.Exists(_ExceptionPath))
                {
                    System.IO.Directory.CreateDirectory(_ExceptionPath);
                }
            }
        }

        /// <summary>
        /// 将日志写入指定的文件
        /// </summary>
        private static void WriteLogEx(LogTypes logType, string logMsg)
        {
            try
            {
                StreamWriter sw = GetStreamWriter(logType);

                // 分析日志有特殊的日志格式
                //string text = (logType == LogTypes.Analysis) ? logMsg : (TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss: ") + logMsg);
                string text = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss: ") + logMsg;
                if (EnableDbgView)
                {
                    System.Diagnostics.Debug.WriteLine(text);
                }

                sw.WriteLine(text);

                if (logType == LogTypes.Exception)
                {
                    LarkNotificationHelper.PushDevNotification(logMsg);
                }
            }
            catch (Exception ex)
            {
                string exstr = ex.ToString();
                LarkNotificationHelper.PushDevNotification(exstr);
            }
        }

        /// <summary>
        /// 将异常写入指定的文件
        /// </summary>
        private static void _WriteException(string exceptionMsg)
        {
            WriteLog(LogTypes.Exception, "##exception##\r\n" + exceptionMsg);
        }

        /// <summary>
        /// 写日志的锁对象
        /// </summary>
        private static object mutex = new object();

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="logMsg">日志信息</param>

        public static void WriteLog(LogTypes logType, string logMsg, Exception ex = null, bool bConsole = true, bool pushToLoki = true)
        {
            if ((int)logType < (int)LogTypeToWrite) //不必记录
            {
                return;
            }

            lock (mutex)
            {
                WriteLogEx(logType, logMsg);
            }

            // Push structured log to Loki via delegate callback
            try
            {
                OnLogWritten?.Invoke(logType, logMsg, pushToLoki);
            }
            catch { }

            if (logType >= LogTypes.Fatal && bConsole)
            //if (logType != LogTypes.Alert && logType != LogTypes.TimerReport && bConsole)
            {
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                SysConOut.WriteLine("Logtype " + logType.ToString() + ":___" + logMsg);
                Console.ForegroundColor = color;
            }

            if (null != ex)
            {
                WriteException(logMsg + ex.ToString());
            }
        }


        // public static string UrlEncode(string str)
        // {
        //     if (str == null)
        //     {
        //         return null;
        //     }
        //     return HttpUtility.UrlEncode(str, Encoding.UTF8);
        // }

        public static void SendDevTelegram(string content)
        {
            // #if !DEBUG
            // try
            // {
            //     var URL = "https://api.telegram.org/bot5546153554:AAFWHlN-ik47A2VYK7nQxSNbUt9irivFXFo/sendMessage?chat_id=-1001971028225&text=" + UrlEncode($"Server {ServerID}: {content}");
            //     var wc = new WebClient();
            //     wc.DownloadString(URL);
            // }
            // catch (Exception ex)
            // {
            //     SysConOut.WriteLine(ex.ToString());
            // }
            // #endif
        }

        /// <summary>
        /// 写异常
        /// </summary>
        /// <param name="exceptionMsg">异常信息</param>
        public static void WriteException(string exceptionMsg)
        {
            _WriteException(exceptionMsg);
        }

        private static StreamWriter GetStreamWriter(LogTypes logType)
        {
            LogFilePoolItem item;
            DateTime now = TimeUtil.NowDateTime();
            lock (mutex)
            {
                if (!LogType2FileDict.TryGetValue(logType, out item))
                {
                    item = new LogFilePoolItem();
                    LogType2FileDict.Add(logType, item);
                }
                if (now.Hour != item.OpenTimeOnHours || now.DayOfYear != item.OpenTimeOnDayOfYear || item._StreamWriter == null)
                {
                    if (null != item._StreamWriter)
                    {
                        item._StreamWriter.Close();
                        item._StreamWriter = null;
                    }
                    item._StreamWriter = File.AppendText(LogPath + logType.ToString() + "_" + now.ToString("yyyyMMdd") + ".log");
                    item.OpenTimeOnHours = now.Hour;
                    item.OpenTimeOnDayOfYear = now.DayOfYear;
                    item._StreamWriter.AutoFlush = true;
                }
                return item._StreamWriter;
            }
        }
        public static string[] ReadLog(LogTypes logType, DateTime time)
        {
            try
            {
                var lines = File.ReadAllLines(LogPath + logType.ToString() + "_" + time.ToString("yyyyMMdd") + ".log");
                return lines;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 写入异常信息,使用减少不可预知的原因导致有过多重复的异常日志
        /// </summary>
        /// <param name="exStr"></param>
        public static void WriteExceptionUseCache(string exStr)
        {
            try
            {
                DateTime now = TimeUtil.NowDateTime();
                lock (ExceptionCacheDict)
                {
                    if (now.Hour != LastClearCacheTime.Hour || ExceptionCacheDict.Count > 10000)
                    {
                        ExceptionCacheDict.Clear();
                        LastClearCacheTime = now;
                    }

                    int count = 0;
                    if (!ExceptionCacheDict.TryGetValue(exStr, out count))
                    {
                        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(2, true);
                        WriteException(exStr + stackTrace.ToString());
                    }

                    ExceptionCacheDict[exStr] = ++count;
                }
            }
            catch (Exception ex)
            {
                WriteException(ex.ToString());
            }
        }
    }
}
