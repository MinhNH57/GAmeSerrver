using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using Server.Tools;

namespace GameServer.KiemThe
{
    /// <summary>
    /// Thiết lập hệ thống
    /// </summary>
    public class ServerConfig
    {
        private const string ServerConfigPath_JSON = "ServerConfig.json";
        private const string ServerConfigPath_XML = "ServerConfig.xml";

        #region Singleton - Instance

        /// <summary>
        /// Thiết lập hệ thống
        /// </summary>
        public static ServerConfig Instance { get; private set; }

        /// <summary>
        /// Thiết lập hệ thống
        /// </summary>
        public ServerConfig()
        {
        }

        #endregion

        /// <summary>
        /// CCU tối đa
        /// </summary>
        public int MaxCCU { get; set; }

        /// <summary>
        /// Giới hạn tài khoản trên địa chỉ IP
        /// </summary>
        public int LimitAccountPerIPAddress { get; set; }

        /// <summary>
        /// Mở luồng thực thi di chuyển ngẫu nhiên của quái
        /// </summary>
        public bool EnableMonsterAIRandomMove { get; set; }

        /// <summary>
        /// Mở luồng thực thi AI quái
        /// </summary>
        public bool EnableMonsterAI { get; set; }


        public bool KuaFuServerEnable { get; set; }

        /// <summary>
        /// Đây là LSV cân bằng
        /// </summary>
        public bool IsKuaFuBalance { get; set; }

        /// <summary>
        /// Kích hoạt Jackpot khi KuaFuServerEnable = false
        /// </summary>
        public bool EnableJackpot { get; set; }

        /// <summary>
        /// Số luồng UpdateGrid chạy đồng thời
        /// </summary>
        public int MaxUpdateGridThread { get; set; }

        /// <summary>
        /// Số luồng StoryBoard chạy đồng thời
        /// </summary>
        public int MaxPlayerStoryBoardThread { get; set; }

        /// <summary>
        /// SỐ luồng Monster chạy đồng thời
        /// </summary>
        public int MaxMonsterTimer { get; set; }

        /// <summary>
        /// Số luồng Buff chạy đồng thời
        /// </summary>
        public int MaxBuffTimer { get; set; }

        /// <summary>
        /// Giới hạn cấp độ
        /// </summary>
        public int LimitLevel { get; set; }

        /// <summary>
        /// Kích hoạt Captcha
        /// </summary>
        public bool EnableCaptcha { get; set; }

        /// <summary>
        /// Kích hoạt Captcha cho các thiết bị iOS
        /// </summary>
        public bool EnableCaptchaForIOS { get; set; }

        /// <summary>
        /// Thời gian xuất hiện tối thiểu
        /// </summary>
        public int CaptchaAppearMinPeriod { get; set; }

        /// <summary>
        /// Thời gian xuất hiện tối đa
        /// </summary>
        public int CaptchaAppearMaxPeriod { get; set; }

        /// <summary>
        /// Captcha chỉ xuất hiện ở người chơi có nhóm
        /// </summary>
        public bool CaptchaTeamPlayersOnly { get; set; }

        /// <summary>
        /// Thời gian xuất hiện Captcha trong ngày
        /// </summary>
        public DateTime CaptchaAppearFromTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc xuất hiện Captcha trong ngày
        /// </summary>
        public DateTime CaptchaAppearToTime { get; set; }

        /// <summary>
        /// Kinh nghiệm nhận được khi trả lời đúng Captcha
        /// </summary>
        public int CaptchaExpAddPerLevel { get; set; }

        /// <summary>
        /// Bạc khóa nhận được khi trả lời đúng Captcha
        /// </summary>
        public int CaptchaBoundMoneyAddPerLevel { get; set; }

        /// <summary>
        /// cho phép giao dịch toàn server
        /// </summary>
        public bool AllowTradeAll { get; set; }

        /// <summary>
        /// cho phép bày bán toàn server
        /// </summary>
        public bool AllowStallAll { get; set; }

        /// <summary>
        /// bật jwt verification
        /// </summary>
        public bool JWTEnable { get; set; } = true;

        /// <summary>
        /// cho phép tạo nhân vật mới
        /// </summary>
        public bool AllowCreateRole { get; set; }

        /// <summary>
        /// giới hạn level boss thế giới
        /// </summary>
        public int LimitBossLevel { get; set; }

        /// <summary>
        /// cho phép thay đổi bang/tộc, mặc định là true
        /// </summary>
        public bool AllowChangeFamilyAndGuild { get; set; } = true;

        public int DamageLogThreshold { get; set; }

        // fake server id của server hiện tại (dùng cho LuaLib_System)
        public int FakeServerID { get; set; }

        public int MantleLevelForSongJin { get; set; } = 6;
        public int MantleLevelForBaiHuTang { get; set; } = 6;
        public int MantleLevelForGuildWar { get; set; } = 6;
        public int MantleLevelForEmperorTomb { get; set; } = 6;
        public int MantleLevelForTeamBattle { get; set; } = 6;
        public int MantleLevelForKuaFu { get; set; } = 6;

        /// <summary>
        /// detack trang bị ở vị trí nào đó khi vào server
        /// </summary>
        public string LimitEquipPos { get; set; }

        /// <summary>
        /// server nhận được buff khi vào lsv
        /// </summary>
        public string IDServersHasBuff { get; set; }

        /// <summary>
        /// ID skill buff hỗ trợ server khi vào máy chủ liên thông(LSV)
        /// </summary>
        public int IDBuffSupportLSV { get; set; } = 0;

        /// <summary>
        /// ID role có thể nhận buff hỗ trợ server khi vào máy chủ liên thông(LSV)
        /// </summary>
        public string IDRolesCanReceiveBuffSupportLSV { get; set; }

        public bool OptimizedBagSorting { get; set; } = true;

        public DateTime OpenServerTime { get; set; } = DateTime.MinValue;
        public double ElapsedDaySinceServerOpen;
        public string GMEAppID { get; set; }
        public string GMEAuthKey { get; set; }
        public string PrefixRoom { get; set; }
        /// <summary>
        /// Khởi tạo
        /// </summary>
        public static void Init()
        {
            if (!File.Exists(ServerConfigPath_JSON))
            {
                SysConOut.WriteLine("Server config file doesn't exist.");
                return;
            }
            
            SysConOut.WriteLine($"Đọc config server từ file {ServerConfigPath_JSON}");
            // parse json cho phép có comment
            // var deSerializerOptions = new System.Text.Json.JsonSerializerOptions
            // {
            //     ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            // };
            var configStr = File.ReadAllText(ServerConfigPath_JSON);
            Instance = JsonConvert.DeserializeObject<ServerConfig>(configStr);
            var now = DateTime.Now;
            Instance.CaptchaAppearFromTime = new DateTime(now.Year, now.Month, now.Day,
                Instance.CaptchaAppearFromTime.Hour, Instance.CaptchaAppearFromTime.Minute, 0);
            Instance.CaptchaAppearToTime = new DateTime(now.Year, now.Month, now.Day,
                Instance.CaptchaAppearToTime.Hour, Instance.CaptchaAppearToTime.Minute, 0);

            // chuẩn hóa về nửa đêm để tính thời gian một số hoạt động
            Instance.OpenServerTime = new DateTime(Instance.OpenServerTime.Year, Instance.OpenServerTime.Month, Instance.OpenServerTime.Day, 0, 0,
                0);
            
            Instance.ElapsedDaySinceServerOpen = (now - Instance.OpenServerTime).TotalDays;
            if (Instance.OpenServerTime != DateTime.MinValue)
            {
                // có khai báo thời gian mở server
                // overwrite một số biến

                // sau 7 ngày thì mở giới hạn cấp 89, boss 79
                if (Instance.ElapsedDaySinceServerOpen < 7)
                {
                    SysConOut.WriteLine("Server chưa đủ 7 ngày, giới hạn cấp độ 89, boss 79");
                    Instance.LimitLevel = 89;
                    Instance.LimitBossLevel = 79;
                } else if (Instance.ElapsedDaySinceServerOpen < 14)
                {
                    SysConOut.WriteLine("Server chưa đủ 14 ngày, giới hạn cấp độ 99, boss 99");
                    Instance.LimitLevel = 99;
                    Instance.LimitBossLevel = 99;
                }
                else
                {
                    SysConOut.WriteLine("Server đã đủ 14 ngày, giới hạn cấp độ 129, boss 129");
                    Instance.LimitLevel = 129;
                    Instance.LimitBossLevel = 129;
                }
            }
            // else
            // {
            //     SysConOut.WriteLine($"Đọc config server từ file {ServerConfigPath_XML}");
            //     var xmlNode = XElement.Parse(File.ReadAllText(ServerConfigPath_XML));
            //     ServerConfig.Instance = new ServerConfig()
            //     {
            //         MaxCCU = int.Parse(xmlNode.Element("LimitAccount").Attribute("MaxCCU").Value),
            //         LimitAccountPerIPAddress =
            //             int.Parse(xmlNode.Element("LimitAccount").Attribute("LimitAccountPerIPAddress").Value),
            //         EnableMonsterAIRandomMove =
            //             bool.Parse(xmlNode.Element("MonsterAI").Attribute("EnableMonsterAIRandomMove").Value),
            //         EnableMonsterAI = bool.Parse(xmlNode.Element("MonsterAI").Attribute("EnableMonsterAI").Value),
            //         MaxUpdateGridThread =
            //             int.Parse(xmlNode.Element("Threading").Attribute("MaxUpdateGridThread").Value),
            //         MaxPlayerStoryBoardThread =
            //             int.Parse(xmlNode.Element("Threading").Attribute("MaxPlayerStoryBoardThread").Value),
            //         MaxMonsterTimer = int.Parse(xmlNode.Element("Threading").Attribute("MaxMonsterTimer").Value),
            //         MaxBuffTimer = int.Parse(xmlNode.Element("Threading").Attribute("MaxBuffTimer").Value),
            //         LimitLevel = int.Parse(xmlNode.Element("GameConfig").Attribute("LimitLevel").Value),
            //         KuaFuServerEnable = bool.Parse(xmlNode.Element("GameConfig").Attribute("KuaFuServerEnable").Value),
            //         AllowTradeAll = bool.Parse(xmlNode.Element("GameConfig").Attribute("AllowTradeAll").Value),
            //         AllowStallAll = bool.Parse(xmlNode.Element("GameConfig").Attribute("AllowStallAll").Value),
            //
            //         EnableCaptcha = bool.Parse(xmlNode.Element("Captcha").Attribute("EnableCaptcha").Value),
            //         EnableCaptchaForIOS =
            //             bool.Parse(xmlNode.Element("Captcha").Attribute("EnableCaptchaForIOS").Value),
            //         CaptchaAppearMinPeriod =
            //             int.Parse(xmlNode.Element("Captcha").Attribute("CaptchaAppearMinPeriod").Value),
            //         CaptchaAppearMaxPeriod =
            //             int.Parse(xmlNode.Element("Captcha").Attribute("CaptchaAppearMaxPeriod").Value),
            //         CaptchaTeamPlayersOnly =
            //             bool.Parse(xmlNode.Element("Captcha").Attribute("CaptchaTeamPlayersOnly").Value),
            //         CaptchaExpAddPerLevel =
            //             int.Parse(xmlNode.Element("Captcha").Attribute("CaptchaExpAddPerLevel").Value),
            //         CaptchaBoundMoneyAddPerLevel =
            //             int.Parse(xmlNode.Element("Captcha").Attribute("CaptchaBoundMoneyAddPerLevel").Value),
            //         LimitEquipPos = xmlNode.Element("LimitEquipPos").Attribute("Pos").Value,
            //     };
            //
            //     try
            //     {
            //         Instance.FakeServerID = int.Parse(xmlNode.Element("GameConfig").Attribute("FakeServerID").Value);
            //     }
            //     catch
            //     {
            //         Instance.FakeServerID = 0;
            //     }
            //
            //     try
            //     {
            //         Instance.DamageLogThreshold =
            //             int.Parse(xmlNode.Element("GameConfig").Attribute("DamageLogThreshold").Value);
            //     }
            //     catch
            //     {
            //         Instance.DamageLogThreshold = 20000;
            //     }
            //
            //     try
            //     {
            //         Instance.LimitBossLevel =
            //             int.Parse(xmlNode.Element("GameConfig").Attribute("LimitBossLevel").Value);
            //     }
            //     catch
            //     {
            //         Instance.LimitBossLevel = 129;
            //     }
            //
            //     try
            //     {
            //         Instance.AllowCreateRole =
            //             bool.Parse(xmlNode.Element("GameConfig").Attribute("AllowCreateRole").Value);
            //     }
            //     catch
            //     {
            //         // nếu không có khai báo thì mặc định cho phép tạo nhân vật mới
            //         Instance.AllowCreateRole = true;
            //     }
            //
            //     try
            //     {
            //         Instance.AllowChangeFamilyAndGuild =
            //             bool.Parse(xmlNode.Element("GameConfig").Attribute("AllowChangeFamilyAndGuild").Value);
            //     }
            //     catch
            //     {
            //         Instance.AllowChangeFamilyAndGuild = true;
            //     }
            //
            //     // Thời gian bắt đầu xuất hiện
            //     {
            //         var timeString = xmlNode.Element("Captcha").Attribute("CaptchaAppearFromTime").Value;
            //         var fields = timeString.Split(':');
            //         var hour = int.Parse(fields[0]);
            //         var minute = int.Parse(fields[1]);
            //         // Ngày hôm nay
            //         var now = DateTime.Now;
            //         ServerConfig.Instance.CaptchaAppearFromTime =
            //             new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            //     }
            //
            //     // Thời gian kết thúc xuất hiện
            //     {
            //         var timeString = xmlNode.Element("Captcha").Attribute("CaptchaAppearToTime").Value;
            //         var fields = timeString.Split(':');
            //         var hour = int.Parse(fields[0]);
            //         var minute = int.Parse(fields[1]);
            //         // Ngày hôm nay
            //         var now = DateTime.Now;
            //         ServerConfig.Instance.CaptchaAppearToTime =
            //             new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            //     }
            //
            //     var str = System.Text.Json.JsonSerializer.Serialize(ServerConfig.Instance);
            //     File.WriteAllText(ServerConfigPath_JSON, str);
            // }
        }
    }
}