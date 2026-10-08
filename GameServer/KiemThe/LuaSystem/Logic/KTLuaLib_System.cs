using GameServer.KiemThe.Entities;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using Server.Tools;
using GameServer.KiemThe.Logic.Manager.Battle;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.GameEvents.SpecialEvent;
using GameServer.Core.Executor;

namespace GameServer.KiemThe.LuaSystem.Logic
{
    /// <summary>
    /// Cung cấp thư viện dùng cho Lua, hệ thống
    /// </summary>
    [MoonSharpUserData]
    public static partial class KTLuaLib_System
    {
        #region Dynamic Script

        /// <summary>
        /// Đăng ký Script mới vào hệ thống
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="obj"></param>
        public static void ReloadScript(int scriptID)
        {
            KTLuaScript.Instance.ReloadScript(KTLuaEnvironment.LuaEnv, scriptID);
        }

        /// <summary>
        /// Thực thi hàm tương ứng ở script chỉ định
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="functionName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static void CallScriptFunction(int scriptID, string functionName, params object[] param)
        {
            KTLuaScript.Instance.ExecuteFunctionAsync(KTLuaEnvironment.LuaEnv, scriptID, functionName, param, null);
        }

        #endregion

        #region Date and time

        /// <summary>
        /// Trả về Tick hiện tại của hệ thống
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentTimeMilis()
        {
            return KTGlobal.GetCurrentTimeMilis();
        }

        public static bool IsDateTimeInRange(string startDate, string endDate)
        {
            var dateStart = DateTime.Parse(startDate, CultureInfo.InvariantCulture);
            var dateEnd = DateTime.Parse(endDate, CultureInfo.InvariantCulture);
            var now = TimeUtil.NowDateTime();
            return now >= dateStart && now <= dateEnd;
        }

        /// <summary>
        /// Trả về giờ hệ thống
        /// </summary>
        /// <returns></returns>
        public static int GetHour()
        {
            return DateTime.Now.Hour;
        }

        /// <summary>
        /// Trả về phút hệ thống
        /// </summary>
        /// <returns></returns>
        public static int GetMinute()
        {
            return DateTime.Now.Minute;
        }

        /// <summary>
        /// Trả về số giây hệ thống
        /// </summary>
        /// <returns></returns>
        public static int GetSecond()
        {
            return DateTime.Now.Second;
        }

        /// <summary>
        /// Trả về ngày trong tháng
        /// </summary>
        /// <returns></returns>
        public static int GetDate()
        {
            return DateTime.Now.Day;
        }

        /// <summary>
        /// Trả về ngày trong năm
        /// </summary>
        /// <returns></returns>
        public static int GetDateOfYear()
        {
            return DateTime.Now.DayOfYear;
        }

        /// <summary>
        /// Trả về tháng trong năm
        /// </summary>
        /// <returns></returns>
        public static int GetMonth()
        {
            return DateTime.Now.Month;
        }

        public static int GetMonthsFromDateTime(string datetime)
        {
            var start = DateTime.Parse(datetime);

            if (start > DateTime.Now)
                return 0;

            var now = DateTime.Now;
            return (now.Year - start.Year) * 12 + now.Month - start.Month;
        }

        /// <summary>
        /// Trả về năm
        /// </summary>
        /// <returns></returns>
        public static int GetYear()
        {
            return DateTime.Now.Year;
        }

        /// <summary>
        /// Trả về ngày trong tuần
        /// <para>Chủ nhật: 0</para>
        /// <para>...</para>
        /// <para>Thứ 7: 6</para>
        /// </summary>
        /// <returns></returns>
        public static int GetDayOfWeek()
        {
            return (int)DateTime.Now.DayOfWeek;
        }

        /// <summary>
        /// Trả về thứ tự tuần trong tháng
        /// </summary>
        /// <returns></returns>
        public static int GetWeek()
        {
            return KTGlobal.GetCurrentWeekOfMonth();
        }

        public static string GetWeekEndDateTime()
        {
            var now = DateTime.Now;
            // DateTime.DayOfWeek bắt đầu từ Sunday nên đổi về Monday
            var dayOfWeek = ((int)now.DayOfWeek + 6) % 7;
            var weekEnd = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(7 - (int)dayOfWeek).AddSeconds(-1);

            return weekEnd.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static int GetMinutesFromNowToWeekEnd()
        {
            var now = DateTime.Now;
            // DateTime.DayOfWeek bắt đầu từ Sunday nên đổi về Monday
            var dayOfWeek = ((int)now.DayOfWeek + 6) % 7;
            var weekEnd = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(7 - (int)dayOfWeek).AddSeconds(-1);
            return (int)(weekEnd - DateTime.Now).TotalMinutes;
        }

        public static int GetMinutesFromNowToDateTime(string datetime)
        {
            var now = DateTime.Now;
            var param = DateTime.Parse(datetime);
            return (int)(param - now).TotalMinutes;
        }

        #endregion

        #region Debug

        /// <summary>
        /// In ra Console
        /// </summary>
        /// <param name="str"></param>
        public static void WriteToConsole(string str)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LUA]: " + str);
            Console.ResetColor();
#endif
        }

        public static void WriteDBLog(Lua_Player player, string recordType, string recordDesc, string source,
            string target, string optType, string otherParam_1 = "", string otherParam_2 = "", string otherParam_3 = "",
            string otherParam_4 = "")
        {
            GameManager.logDBCmdMgr.WriteLogDBForPlayer(player.RefObject,
                recordType, recordDesc, source, target, optType,
                otherParam_1, otherParam_2, otherParam_3, otherParam_4);
        }

        public static void WriteLog(int logTypes, string message)
        {
            LogManager.WriteLog((LogTypes)logTypes, message);
        }

        #endregion

        public static int GetServerID()
        {
            var actualId = GameManager.ServerId;
            if (actualId >= 900 && actualId < 1000 && ServerConfig.Instance.FakeServerID != 0)
            {
                return ServerConfig.Instance.FakeServerID;
            }

            return actualId;
        }

        public static bool IsKuaFuServer()
        {
            return GameManager.IsKuaFuServer;
        }

        static double EventDropRate = 4;
        public static double GetEventDropRate() 
        {
            return EventDropRate;
        }

        public static void SetEventDropRate(double value) 
        {
            EventDropRate = value;
        }

        public static string GetGuildEventGroup(int camp)
        {
            if (camp == 10)
            {
                return Battle_SongJin.GuildEvent_SongGroup;
            }
            else
            {
                return Battle_SongJin.GuildEvent_JinGroup;
            }
        }

        public static void NotifyAll(string message)
        {
            KTGlobal.SendSystemEventNotification(message);
        }

        public static string GetMantleNameFromLevel(int mantleLevel)
        {
            var MANTLE_NAMEs = new List<string>
            {
                "Bất Kỳ",
                "Siêu Phàm",
                "Xuất Trần",
                "Lăng Tuyệt",
                "Kinh Thế",
                "Ngự Không",
                "Hỗn Thiên",
                "Sồ Phượng",
                "Tiềm Long",
                "Chí Tôn",
                "Vô Song",
            };
            if (mantleLevel > 0 && mantleLevel < MANTLE_NAMEs.Count)
            {
                return MANTLE_NAMEs[mantleLevel].ToUpper();
            }
            else
            {
                return "";
            }
        }

        public static string GetItemName(int itemID)
        {
            var itemTemplate = ItemManager.GetItemTemplate(itemID);
            if (itemTemplate == null)
            {
                return "";
            }

            if (ItemEnhance.IsHuyenTinh(itemTemplate))
            {
                if (itemTemplate.ItemID > 300)
                {
                    return $"{itemTemplate.Name} - Khóa";
                }
            }

            return itemTemplate.Name;
        }

        public static bool CanGetReturnAwards(Lua_Player player)
        {
            return KTGlobal.CanGetReturnAwards(player.RefObject.strUserID);
        }

        public static bool CanGetReturnAwardsVIP(Lua_Player player)
        {
            return KTGlobal.CanGetReturnAwardsVIP(player.RefObject.strUserID);
        }

        public static bool CanJoinHeroComeback(Lua_Player player)
        {
            return KTGlobal.IsEligibleForHeroComeback(player.RefObject.RoleID);
        }

        public static bool CanGetMergeServerSupportPack(Lua_Player player) 
        {
            return KTGlobal.CanGetMergeServerSupportPack(player.RefObject.RoleID);
        }

        public static int GetServerIDBeforeMerge(Lua_Player player) 
        {
            return KTGlobal.GetServerIDBeforeMerge(player.RefObject.RoleID);
        }

        public static bool StartCountdownGood(Lua_Player player, Lua_Item item, int minutes)
        {
            return Global.StartCountdownGoods(player.RefObject, item.GetID(), minutes);
        }

        public static string GetSkillName(int skillID)
        {
            var skillData = KSkill.GetSkillData(skillID);
            if (skillData == null)
                return "";
            return skillData.Name;
        }

        #region Tiên đồng Ngọc Nữ

        public static string GetTDNNLuaMessage()
        {
            return KTGlobal.GetTDNNLuaMessage();
        }

        public static bool IsTDNNWinner(int playerID)
        {
            return KTGlobal.IsTDNNWinner(playerID);
        }

        public static void GoToNPC(Lua_Player player, int npcId, int mapId)
        {
            KTGlobal.PlayerGoToNPC(player.RefObject, npcId, mapId);
        }

        #endregion


        #region Hoa Hồng Truyền Tình

        public static IDictionary<string, int> GetRoseRanking()
        {
            return SpecialEventManager.GetRoseRanking();
        }

        #endregion

        #region Global Variables

        /// <summary>
        /// Danh sách các biến dữ liệu được lưu lại trong hệ thống
        /// </summary>
        private static readonly Dictionary<int, long> systemGlobalVariables = new Dictionary<int, long>();

        /// <summary>
        /// Trả về giá trị biến toàn cục hệ thống tại vị trí tương ứng
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static long GetGlobalVariable(int idx)
        {
            lock (KTLuaLib_System.systemGlobalVariables)
            {
                if (KTLuaLib_System.systemGlobalVariables.TryGetValue(idx, out long value))
                {
                    return value;
                }

                KTLuaLib_System.systemGlobalVariables[idx] = 0;
                return 0;
            }
        }

        /// <summary>
        /// Thiết lập giá trị biến toàn cục hệ thống tại vị trí tương ứng
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void SetGlobalVariable(int idx, long value)
        {
            lock (KTLuaLib_System.systemGlobalVariables)
            {
                KTLuaLib_System.systemGlobalVariables[idx] = value;
            }
        }

        public static string ReadAllText(string path)
        {
            if (!File.Exists(path))
                return "";
            return File.ReadAllText(path);
        }

        public static void SetGlobalGamePreference(string paramName, string paramValue, bool force = false)
        {
            GameManager.GameConfigMgr.UpdateGameConfigItem(paramName, paramValue, force);
        }

        public static int GetGlobalGamePreference_Int(string paramName)
        {
            return GameManager.GameConfigMgr.GetGameConfigItemInt(paramName, 0);
        }

        #endregion

        public static IDictionary<int, int> GetRandomBoxPreview(Lua_Player player, int boxID)
        {
            var findBox = ItemRandomBox._ConfigBox.Boxs.FirstOrDefault(x => x.IDBox == boxID);
            if (findBox == null)
                return null;

            var result = new Dictionary<int, int>();
            var itemsToPreview = findBox.Items.Where(x => x.IsPreview);
            foreach (var item in itemsToPreview)
            {
                if (item.Sex != -1 && item.Sex != player.RefObject.RoleSex)
                    continue;
                var itemID = item.ItemID;
                // đổi lại item id để hiện preview
                if (item.Type == 1)
                {
                    itemID = 1;
                }

                if (item.Type == 2)
                {
                    itemID = 2;
                }

                if (item.Type == 3)
                {
                    itemID = 4;
                }

                if (item.Type == 3)
                {
                    itemID = 4;
                }

                if (item.Type == 9)
                {
                    itemID = 6;
                }

                if (item.Type == 10)
                {
                    itemID = 5;
                }

                if (item.Type == 11)
                {
                    itemID = 11;
                }

                result[itemID] = item.Number;
            }

            if (result.Any())
                return result;

            return null;
        }
    }
}
