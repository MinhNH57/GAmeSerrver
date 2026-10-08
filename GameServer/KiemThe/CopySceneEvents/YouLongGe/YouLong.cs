using GameServer.KiemThe.Entities;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GameServer.KiemThe.CopySceneEvents.YouLongGe
{
    /// <summary>
    /// Định nghĩa phụ bản Du Long Các
    /// </summary>
    public static class YouLong
    {
        #region Define

        /// <summary>
        /// Thông tin phần thưởng
        /// </summary>
        public class TopAwardInfo
        {
            /// <summary>
            /// Thông tin từng vật phẩm thưởng
            /// </summary>
            public class TopAward
            {
                /// <summary>
                /// ID vật phẩm thưởng
                /// </summary>
                public int RankMin { get; private set; }

                /// <summary>
                /// số lần hoàn thành yêu cầu
                /// </summary>
                public int RankMax { get; private set; }

                /// <summary>
                /// số lần hoàn thành yêu cầu
                /// </summary>
                public int Require { get; private set; }

                /// <summary>
                /// Số lượng
                /// </summary>
                public string Items { get; private set; }


                /// <summary>
                /// Chuyển đối tượng từ XMLNode
                /// </summary>
                /// <param name="xmlNode"></param>
                /// <returns></returns>
                public static TopAward ParseAward(XElement xmlNode)
                {
                    return new TopAward()
                    {
                        RankMin = xmlNode.SafeGetAttribute<int>("RankMin"),
                        RankMax = xmlNode.SafeGetAttribute<int>("RankMax"),
                        Require = xmlNode.SafeGetAttribute<int>("Require"),
                        Items = xmlNode.SafeGetAttribute<string>("Items"),
                    };
                }
            }

            /// <summary>
            /// Số lượng trong khung
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// Danh sách phần thưởng
            /// </summary>
            public List<TopAward> TAwards { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static TopAwardInfo Parse(XElement xmlNode)
            {
                var awardInfo = new TopAwardInfo()
                {
                    Count = KTGlobal.GetXMLAttribute<int>(xmlNode, "Count"),
                    TAwards = new List<TopAward>(),
                };

                foreach (var node in xmlNode.Elements("TopAward"))
                {
                    awardInfo.TAwards.Add(TopAward.ParseAward(node));
                }

                return awardInfo;
            }
        }


        /// <summary>
        /// Thông tin phần thưởng
        /// </summary>
        public class DailyAwardInfo
        {
            /// <summary>
            /// Thông tin từng vật phẩm thưởng
            /// </summary>
            public class DailyAward
            {
                /// <summary>
                /// ID vật phẩm thưởng
                /// </summary>
                public int ID { get; private set; }

                /// <summary>
                /// số lần hoàn thành yêu cầu
                /// </summary>
                public int Require { get; private set; }

                /// <summary>
                /// Số lượng
                /// </summary>
                public string Items { get; private set; }


                /// <summary>
                /// Chuyển đối tượng từ XMLNode
                /// </summary>
                /// <param name="xmlNode"></param>
                /// <returns></returns>
                public static DailyAward ParseAward(XElement xmlNode)
                {
                    return new DailyAward()
                    {
                        ID = xmlNode.SafeGetAttribute<int>("ID"),
                        Require = xmlNode.SafeGetAttribute<int>("Require"),
                        Items = xmlNode.SafeGetAttribute<string>("Items"),
                    };
                }
            }

            /// <summary>
            /// Số lượng trong khung
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// Danh sách phần thưởng
            /// </summary>
            public List<DailyAward> CAwards { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static DailyAwardInfo Parse(XElement xmlNode)
            {
                var awardInfo = new DailyAwardInfo()
                {
                    Count = KTGlobal.GetXMLAttribute<int>(xmlNode, "Count"),
                    CAwards = new List<DailyAward>(),
                };

                foreach (var node in xmlNode.Elements("DailyAward"))
                {
                    awardInfo.CAwards.Add(DailyAward.ParseAward(node));
                }

                return awardInfo;
            }
        }

        /// <summary>
        /// Thông tin phần thưởng
        /// </summary>
        public class AwardInfo
        {
            /// <summary>
            /// Thông tin từng vật phẩm thưởng
            /// </summary>
            public class Award
            {
                /// <summary>
                /// ID vật phẩm thưởng
                /// </summary>
                public int ID { get; private set; }

                /// <summary>
                /// Số lượng
                /// </summary>
                public int Number { get; private set; }

                /// <summary>
                /// Tỷ lệ nhận được
                /// </summary>
                public int Rate { get; private set; }

                /// <summary>
                /// Tỷ lệ xuất hiện trong danh sách
                /// </summary>
                public int AppearRate { get; private set; }

                /// <summary>
                /// Số tiền đổi được
                /// </summary>
                public int NumberOfCoins { get; private set; }

                /// <summary>
                /// Phần thưởng này có cần thông báo chạy chữ toàn server không
                /// </summary>
                public bool ShouldNotify { get; private set; }

                /// <summary>
                /// Chuyển đối tượng từ XMLNode
                /// </summary>
                /// <param name="xmlNode"></param>
                /// <returns></returns>
                public static Award ParseAward(XElement xmlNode)
                {
                    return new Award()
                    {
                        ID = xmlNode.SafeGetAttribute<int>("ID"),
                        Number = xmlNode.SafeGetAttribute<int>("Number"),
                        Rate = xmlNode.SafeGetAttribute<int>("Rate"),
                        AppearRate = xmlNode.SafeGetAttribute<int>("AppearRate"),
                        NumberOfCoins = xmlNode.SafeGetAttribute<int>("NumberOfCoins"),
                        ShouldNotify = xmlNode.SafeGetAttribute<bool>("Notify")
                    };
                }
            }

            /// <summary>
            /// Số lượng trong khung
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// Số lần chạy lại tối đa
            /// </summary>
            public int MaxTryTime { get; private set; }

            /// <summary>
            /// Danh sách phần thưởng
            /// </summary>
            public List<Award> Awards { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static AwardInfo Parse(XElement xmlNode)
            {
                var awardInfo = new AwardInfo()
                {
                    Count = KTGlobal.GetXMLAttribute<int>(xmlNode, "Count"),
                    MaxTryTime = KTGlobal.GetXMLAttribute<int>(xmlNode, "MaxTryTime"),
                    Awards = new List<Award>(),
                };

                foreach (var node in xmlNode.Elements("Award"))
                {
                    awardInfo.Awards.Add(Award.ParseAward(node));
                }

                return awardInfo;
            }
        }

        /// <summary>
        /// Thông tin bản đồ
        /// </summary>
        public class MapInfo
        {
            /// <summary>
            /// ID bản đồ
            /// </summary>
            public int ID { get; private set; }

            /// <summary>
            /// Vị trí tiến vào X
            /// </summary>
            public int EnterPosX { get; private set; }

            /// <summary>
            /// Vị trí tiến vào Y
            /// </summary>
            public int EnterPosY { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static MapInfo Parse(XElement xmlNode)
            {
                return new MapInfo()
                {
                    ID = xmlNode.SafeGetAttribute<int>("ID"),
                    EnterPosX = xmlNode.SafeGetAttribute<int>("EnterPosX"),
                    EnterPosY = xmlNode.SafeGetAttribute<int>("EnterPosY")
                };
            }
        }

        /// <summary>
        /// Thông tin NPC
        /// </summary>
        public class NPCInfo
        {
            /// <summary>
            /// ID NPC
            /// </summary>
            public int ID { get; private set; }

            /// <summary>
            /// Tên NPC
            /// <para>Bỏ trống sẽ lấy ở File cấu hình</para>
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Danh hiệu NPC
            /// <para>Bỏ trống sẽ lấy ở File cấu hình</para>
            /// </summary>
            public string Title { get; private set; }

            /// <summary>
            /// Vị trí X
            /// </summary>
            public int PosX { get; private set; }

            /// <summary>
            /// Vị trí Y
            /// </summary>
            public int PosY { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static NPCInfo Parse(XElement xmlNode)
            {
                return new NPCInfo()
                {
                    ID = xmlNode.SafeGetAttribute<int>("ID"),
                    Name = xmlNode.SafeGetAttribute<string>("Name"),
                    Title = xmlNode.SafeGetAttribute<string>("Title"),
                    PosX = xmlNode.SafeGetAttribute<int>("PosX"),
                    PosY = xmlNode.SafeGetAttribute<int>("PosY")
                };
            }
        }

        /// <summary>
        /// Thông tin Boss
        /// </summary>
        public class BossInfo
        {
            /// <summary>
            /// ID NPC
            /// </summary>
            public int ID { get; private set; }

            /// <summary>
            /// Tên NPC
            /// <para>Bỏ trống sẽ lấy ở File cấu hình</para>
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Danh hiệu NPC
            /// <para>Bỏ trống sẽ lấy ở File cấu hình</para>
            /// </summary>
            public string Title { get; private set; }

            /// <summary>
            /// Vị trí X
            /// </summary>
            public int PosX { get; private set; }

            /// <summary>
            /// Vị trí Y
            /// </summary>
            public int PosY { get; private set; }

            /// <summary>
            /// Sinh lực cơ bản
            /// </summary>
            public int BaseHP { get; private set; }

            /// <summary>
            /// Sinh lực tăng thêm mỗi cấp
            /// </summary>
            public int HPIncreaseEachLevel { get; private set; }

            /// <summary>
            /// Loại AI
            /// </summary>
            public MonsterAIType AIType { get; private set; }

            /// <summary>
            /// ID Script AI điều khiển
            /// </summary>
            public int AIScriptID { get; private set; }

            public int Series { get; private set; }

            /// <summary>
            /// Danh sách kỹ năng sẽ sử dụng
            /// </summary>
            public List<SkillLevelRef> Skills { get; private set; }

            /// <summary>
            /// Danh sách vòng sáng sẽ sử dụng
            /// </summary>
            public List<SkillLevelRef> Auras { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static BossInfo Parse(XElement xmlNode)
            {
                var monsterInfo = new BossInfo()
                {
                    ID = xmlNode.SafeGetAttribute<int>("ID"),
                    Name = xmlNode.SafeGetAttribute<string>("Name"),
                    Title = xmlNode.SafeGetAttribute<string>("Title"),
                    PosX = xmlNode.SafeGetAttribute<int>("PosX"),
                    PosY = xmlNode.SafeGetAttribute<int>("PosY"),
                    BaseHP = xmlNode.SafeGetAttribute<int>("BaseHP"),
                    HPIncreaseEachLevel = xmlNode.SafeGetAttribute<int>("HPIncreaseEachLevel"),
                    AIType = (MonsterAIType)xmlNode.SafeGetAttribute<int>("AIType"),
                    AIScriptID = xmlNode.SafeGetAttribute<int>("AIScriptID"),
                    Series = xmlNode.SafeGetAttribute<int>("Series", 0),
                    Skills = new List<SkillLevelRef>(),
                    Auras = new List<SkillLevelRef>(),
                };

                // Chuỗi mã hóa danh sách kỹ năng sử dụng
                var skillsString = xmlNode.SafeGetAttribute<string>("Skills");
                // Nếu có kỹ năng
                if (!string.IsNullOrEmpty(skillsString))
                {
                    // Duyệt danh sách kỹ năng
                    foreach (var skillStr in skillsString.Split(';'))
                    {
                        var fields = skillStr.Split('_');
                        try
                        {
                            var skillID = int.Parse(fields[0]);
                            var skillLevel = int.Parse(fields[1]);
                            var cooldown = int.Parse(fields[2]);

                            // Thông tin kỹ năng tương ứng
                            var skillData = KSkill.GetSkillData(skillID);
                            // Nếu kỹ năng không tồn tại
                            if (skillData == null)
                            {
                                throw new Exception($"Skill ID = {skillID} not found!");
                            }

                            // Nếu cấp độ dưới 0
                            if (skillLevel <= 0)
                            {
                                throw new Exception($"Skill ID = {skillID} level must be greater than 0");
                            }

                            // Kỹ năng theo cấp
                            var skillRef = new SkillLevelRef()
                            {
                                Data = skillData,
                                AddedLevel = skillLevel,
                                Exp = cooldown,
                            };

                            // Thêm vào danh sách
                            monsterInfo.Skills.Add(skillRef);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                        }
                    }
                }

                // Chuỗi mã hóa danh sách vòng sáng sử dụng
                var aurasString = xmlNode.SafeGetAttribute<string>("Auras");
                // Nếu có kỹ năng
                if (!string.IsNullOrEmpty(aurasString))
                {
                    // Duyệt danh sách kỹ năng
                    foreach (var skillStr in aurasString.Split(';'))
                    {
                        var fields = skillStr.Split('_');
                        try
                        {
                            var skillID = int.Parse(fields[0]);
                            var skillLevel = int.Parse(fields[1]);

                            // Thông tin kỹ năng tương ứng
                            var skillData = KSkill.GetSkillData(skillID);
                            // Nếu kỹ năng không tồn tại
                            if (skillData == null)
                            {
                                throw new Exception($"Skill ID = {skillID} not found!");
                            }

                            // Nếu cấp độ dưới 0
                            if (skillLevel <= 0)
                            {
                                throw new Exception($"Skill ID = {skillID} level must be greater than 0");
                            }

                            // Nếu không phải vòng sáng
                            if (!skillData.IsArua)
                            {
                                throw new Exception($"Skill ID = {skillID} is not Aura!");
                            }

                            // Kỹ năng theo cấp
                            var skillRef = new SkillLevelRef()
                            {
                                Data = skillData,
                                AddedLevel = skillLevel,
                            };

                            // Thêm vào danh sách
                            monsterInfo.Auras.Add(skillRef);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                        }
                    }
                }

                // Trả về kết quả
                return monsterInfo;
            }
        }

        /// <summary>
        /// Thông tin vật phẩm
        /// </summary>
        public class ItemInfo
        {
            /// <summary>
            /// ID Nguyệt Ảnh Thạch
            /// </summary>
            public int MoonStoneID { get; private set; }

            /// <summary>
            /// ID Tiền Du Long
            /// </summary>
            public int YouLongCoinID { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static ItemInfo Parse(XElement xmlNode)
            {
                return new ItemInfo()
                {
                    MoonStoneID = xmlNode.SafeGetAttribute<int>("MoonStoneID"),
                    YouLongCoinID = xmlNode.SafeGetAttribute<int>("YouLongCoinID")
                };
            }
        }

        /// <summary>
        /// Thông tin phụ bản Du Long
        /// </summary>
        public class YouLongCopyScene
        {
            /// <summary>
            /// ID sự kiện
            /// </summary>
            public int EventID { get; private set; }

            /// <summary>
            /// Thời gian tồn tại (Mili-giây)
            /// </summary>
            public int Duration { get; private set; }

            /// <summary>
            /// Cấp độ yêu cầu
            /// </summary>
            public int RequireLevel { get; private set; }

            /// <summary>
            /// Số lượt tham gia tối đa trong ngày
            /// </summary>
            public int LimitRoundPerDay { get; private set; }

            /// <summary>
            /// Thời gian tự đẩy ra khi không có khiêu chiến mới (Mili-giây)
            /// </summary>
            public int KickOutIfNoChallengeFor { get; private set; }

            /// <summary>
            /// Thời gian tỷ thí tối đa (Mili-giây)
            /// </summary>
            public int ChallengeDuration { get; private set; }

            /// <summary>
            /// Vật phẩm yêu cầu
            /// </summary>
            public int RequireItem { get; private set; }

            /// <summary>
            /// Danh sách vật phẩm yêu cầu hoặc gì đó
            /// </summary>
            public ItemInfo Items { get; private set; }

            /// <summary>
            /// Thông tin bản đồ
            /// </summary>
            public MapInfo Map { get; private set; }

            /// <summary>
            /// Thông tin NPC
            /// </summary>
            public NPCInfo NPC { get; private set; }

            /// <summary>
            /// Thông tin bosses random
            /// </summary>
            public List<BossInfo> Bosses { get; private set; }

            /// <summary>
            /// Danh sách vật phẩm thưởng
            /// </summary>
            public AwardInfo Awards { get; private set; }

            public DailyAwardInfo DailyAwards { get; private set; }

            public DailyAwardInfo MonthAwards { get; private set; }

            public DailyAwardInfo ServerAwards { get; private set; }

            public TopAwardInfo TopAwards { get; private set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static YouLongCopyScene Parse(XElement xmlNode)
            {
                var data = new YouLongCopyScene()
                {
                    EventID = xmlNode.SafeGetAttribute<int>("EventID"),
                    Duration = xmlNode.SafeGetAttribute<int>("Duration"),
                    RequireLevel = xmlNode.SafeGetAttribute<int>("RequireLevel"),
                    LimitRoundPerDay = xmlNode.SafeGetAttribute<int>("LimitRoundPerDay"),
                    KickOutIfNoChallengeFor = xmlNode.SafeGetAttribute<int>("KickOutIfNoChallengeFor"),
                    ChallengeDuration = xmlNode.SafeGetAttribute<int>("ChallengeDuration"),
                    RequireItem = xmlNode.SafeGetAttribute<int>("RequireItem"),
                    Items = ItemInfo.Parse(xmlNode.Element("Item")),
                    Map = MapInfo.Parse(xmlNode.Element("Map")),
                    NPC = NPCInfo.Parse(xmlNode.Element("NPC")),
                    Awards = AwardInfo.Parse(xmlNode.Element("Awards")),
                    DailyAwards = DailyAwardInfo.Parse(xmlNode.Element("DailyAwards")),
                    MonthAwards = DailyAwardInfo.Parse(xmlNode.Element("MonthAwards")),
                    ServerAwards = DailyAwardInfo.Parse(xmlNode.Element("ServerAwards")),
                    TopAwards = TopAwardInfo.Parse(xmlNode.Element("TopAwards")),
                    Bosses = new List<BossInfo>()
                };

                foreach (var node in xmlNode.Elements("Bosses").Elements("Boss"))
                {
                    data.Bosses.Add(BossInfo.Parse(node));
                }

                return data;
            }
        }

        #endregion

        /// <summary>
        /// Dữ liệu phụ bản Du Long
        /// </summary>
        public static YouLongCopyScene Data { get; private set; }

        private static int _serverReceiveAwardTime;

        public static int serverReceiveAwardTime
        {
            get => _serverReceiveAwardTime;
            set
            {
                _serverReceiveAwardTime = value;
                GameManager.GameConfigMgr.UpdateGameConfigItem($"youlong_line_{GameManager.ServerId}_received_daily",
                    value.ToString());
            }
        }

        /// <summary>
        /// cập nhật số lượt đi du long trong ngày của toàn server 
        /// </summary>
        /// <param name="value">giá trị muốn cập nhật, thường là 1. hỗ trợ giá trị khác để cập nhật bằng GM</param>
        /// <returns>số lượt đi du long trong ngày của toàn server sau khi cập nhật</returns>
        public static int UpdateServerTotalPassedByValue(int value = 1)
        {
            var now = DateTime.Now;
            var nServerPassed = 0;
            var strServerPassed =
                GameManager.GameConfigMgr.GetGameConfigItemStr($"youlong_line_{GameManager.ServerId}_passed_daily", "");
            if (strServerPassed != "")
            {
                var fields = strServerPassed.Split('_');
                var dayTime = int.Parse(fields[1]);
                nServerPassed = dayTime == now.DayOfYear ? int.Parse(fields[0]) : 0;
            }

            nServerPassed += value;
            strServerPassed = $"{nServerPassed}_{now.DayOfYear}";
            GameManager.GameConfigMgr.UpdateGameConfigItem($"youlong_line_{GameManager.ServerId}_passed_daily",
                strServerPassed);
            return nServerPassed;
        }

        #region Core

        /// <summary>
        /// Khởi tạo dữ liệu
        /// </summary>
        public static void Init()
        {
            var xmlNode = Global.GetGameResXml("Config/KT_CopyScenes/YouLong.xml");
            YouLong.Data = YouLongCopyScene.Parse(xmlNode);
            YouLong.serverReceiveAwardTime =
                GameManager.GameConfigMgr.GetGameConfigItemInt($"youlong_line_{GameManager.ServerId}_received_daily",
                    0);
        }

        #endregion
    }
}