using GameServer.KiemThe.Entities;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static GameServer.Logic.Monster;

namespace GameServer.KiemThe.GameEvents.BossUnicon
{
    /// <summary>
    /// Sự kiện 
    /// </summary>
    public static class BossUnicon
    {
        #region Define
        /// <summary>
        /// Thiết lập sự kiện
        /// </summary>
        public class EventConfig
        {
            /// <summary>
            /// Cấp độ tối thiểu yêu cầu
            /// </summary>
            public int LimitLevel { get; set; }

            /// <summary>
            /// Thời gian được vào mỗi ngày
            /// </summary>
            public int DurationPerDay { get; set; }

            /// <summary>
            /// ID vật phẩm  Dạ Minh Châu
            /// </summary>
            //public int NightPearlItemID { get; set; }

            /// <summary>
            /// Cấp độ quái
            /// </summary>
            public int MonsterLevel { get; set; }

            /// <summary>
            /// Quà khi đạt top damage giết boss hỏa kỳ lân
            /// </summary>
            public int AwardUnicon { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static EventConfig Parse(XElement xmlNode)
            {
                return new EventConfig()
                {
                    LimitLevel = int.Parse(xmlNode.Attribute("LimitLevel").Value),
                    DurationPerDay = int.Parse(xmlNode.Attribute("DurationPerDay").Value),
                    //NightPearlItemID = int.Parse(xmlNode.Attribute("NightPearlItemID").Value),
                    MonsterLevel = int.Parse(xmlNode.Attribute("MonsterLevel").Value),
                    AwardUnicon = int.Parse(xmlNode.Attribute("AwardUnicon").Value),
                };
            }
        }

        /// <summary>
        /// Thông tin sự kiện
        /// </summary>
        public class EventDetail
        {
            /// <summary>
            /// Thông tin điểm hồi sinh
            /// </summary>
            public class ReliveInfo
            {
                /// <summary>
                /// ID bản đồ hồi sinh
                /// </summary>
                public int MapID { get; set; }

                /// <summary>
                /// Vị trí X điểm hồi sinh
                /// </summary>
                public int PosX { get; set; }

                /// <summary>
                /// Vị trí Y điểm hồi sinh
                /// </summary>
                public int PosY { get; set; }

                /// <summary>
                /// Chuyển đối tượng từ XMLNode
                /// </summary>
                /// <param name="xmlNode"></param>
                /// <returns></returns>
                public static ReliveInfo Parse(XElement xmlNode)
                {
                    return new ReliveInfo()
                    {
                        MapID = int.Parse(xmlNode.Attribute("MapID").Value),
                        PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                        PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                    };
                }
            }

            /// <summary>
            /// Thông tin tầng
            /// </summary>
            public class Stage
            {
                /// <summary>
                /// Thông tin khu an toàn
                /// </summary>
                public class SafeZoneInfo
                {
                    /// <summary>
                    /// Vị trí X
                    /// </summary>
                    public int PosX { get; set; }

                    /// <summary>
                    /// Vị trí Y
                    /// </summary>
                    public int PosY { get; set; }

                    /// <summary>
                    /// Bán kính khu an toàn
                    /// </summary>
                    public int Radius { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static SafeZoneInfo Parse(XElement xmlNode)
                    {
                        return new SafeZoneInfo()
                        {
                            PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                            PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                            Radius = int.Parse(xmlNode.Attribute("Radius").Value),
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
                    public int ID { get; set; }

                    /// <summary>
                    /// Vị trí X
                    /// </summary>
                    public int PosX { get; set; }

                    /// <summary>
                    /// Vị trí Y
                    /// </summary>
                    public int PosY { get; set; }

                    /// <summary>
                    /// ID Script
                    /// </summary>
                    public int ScriptID { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static NPCInfo Parse(XElement xmlNode)
                    {
                        return new NPCInfo()
                        {
                            ID = int.Parse(xmlNode.Attribute("ID").Value),
                            PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                            PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                            ScriptID = int.Parse(xmlNode.Attribute("ScriptID").Value),
                        };
                    }
                }

                /// <summary>
                /// Thời gian (giờ, phút)
                /// </summary>
                public class TimeSpan
                {
                    /// <summary>
                    /// Ngày
                    /// </summary>
                    public int Day { get; set; }

                    /// <summary>
                    /// Giờ
                    /// </summary>
                    public int Hour { get; set; }

                    /// <summary>
                    /// Phút
                    /// </summary>
                    public int Minute { get; set; }
                }

                /// <summary>
                /// Thông tin Boss
                /// </summary>
                public class BossInfo
                {
                    /// <summary>
                    /// ID quái
                    /// </summary>
                    public int ID { get; set; }

                    /// <summary>
                    /// Tên quái
                    /// </summary>
                    public string Name { get; set; }

                    /// <summary>
                    /// Danh hiệu quái
                    /// </summary>
                    public string Title { get; set; }

                    /// <summary>
                    /// Vị trí X
                    /// </summary>
                    public int PosX { get; set; }

                    /// <summary>
                    /// Vị trí Y
                    /// </summary>
                    public int PosY { get; set; }

                    /// <summary>
                    /// Sinh lực cơ bản
                    /// </summary>
                    public int BaseHP { get; set; }

                    /// <summary>
                    /// Sinh lực tăng thêm mỗi cấp
                    /// </summary>
                    public int HPIncreaseEachLevel { get; set; }

                    /// <summary>
                    /// Loại AI
                    /// </summary>
                    public MonsterAIType AIType { get; set; }

                    /// <summary>
                    /// Ngũ hành
                    /// </summary>
                    public int Series { get; set; }

                    /// <summary>
                    /// ID Script AI điều khiển
                    /// </summary>
                    public int AIScriptID { get; set; }

                    /// <summary>
                    /// Thời gian xuất hiện
                    /// </summary>
                    public TimeSpan SpawnAt { get; set; }

                    /// <summary>
                    /// Danh sách kỹ năng sẽ sử dụng
                    /// </summary>
                    public List<SkillLevelRef> Skills { get; set; }

                    /// <summary>
                    /// Danh sách vòng sáng sẽ sử dụng
                    /// </summary>
                    public List<SkillLevelRef> Auras { get; set; }

                    /// <summary>
                    /// Chỉ số kháng ngũ hành
                    /// </summary>
                    public int IgnoreSerie { get; set; } = 0;

                    /// <summary>
                    /// Uy danh sau khi bị tiêu diệt
                    /// </summary>
                    public int Prestige { get; set; }

                    
                    /// <summary>
                    /// Lượng sát thương cá nhân được nhận thưởng
                    /// </summary>
                    public int DamagePlayer { get; set; }

                    /// <summary>
                    /// Lượng sát thương của bang hội nhận được thưởng
                    /// </summary>
                    public int DamageGuild { get; set; }

                    /// <summary>
                    /// Chỉ số Giảm sát thương nhận vào
                    /// </summary>
                    public int Reduce { get; set; }

                    /// <summary>
                    /// Drop nếu cần
                    /// </summary>
                    public string DropItem { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static BossInfo Parse(XElement xmlNode)
                    {
                        BossInfo monsterInfo = new BossInfo()
                        {
                            ID = int.Parse(xmlNode.Attribute("ID").Value),
                            Name = xmlNode.Attribute("Name").Value,
                            Title = xmlNode.Attribute("Title").Value,
                            PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                            PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                            BaseHP = int.Parse(xmlNode.Attribute("BaseHP").Value),
                            HPIncreaseEachLevel = int.Parse(xmlNode.Attribute("HPIncreaseEachLevel").Value),
                            AIType = (MonsterAIType)int.Parse(xmlNode.Attribute("AIType").Value),
                            Series = int.Parse(xmlNode.Attribute("Series").Value),
                            AIScriptID = int.Parse(xmlNode.Attribute("AIScriptID").Value),
                            Skills = new List<SkillLevelRef>(),
                            Auras = new List<SkillLevelRef>(),
                            Prestige = int.Parse(xmlNode.Attribute("Prestige").Value),
                            IgnoreSerie = int.Parse(xmlNode.Attribute("IgnoreSerie").Value),
                            DamagePlayer = int.Parse(xmlNode.Attribute("DamagePlayer").Value),
                            DamageGuild = int.Parse(xmlNode.Attribute("DamageGuild").Value),
                            Reduce = int.Parse(xmlNode.Attribute("Reduce").Value),
                        };

                        // Nếu tồn tại thời gian xuất hiện
                        if (xmlNode.Attribute("SpawnAt") != null)
                        {
                            string timeSpanString = xmlNode.Attribute("SpawnAt").Value;
                            if (!string.IsNullOrEmpty(timeSpanString))
                            {
                                string[] fields = timeSpanString.Split(':');
                                int hour = int.Parse(fields[1]);
                                int minute = int.Parse(fields[2]);
                                int day = int.Parse(fields[0]);
                                monsterInfo.SpawnAt = new TimeSpan()
                                {
                                    Day = day,
                                    Hour = hour,
                                    Minute = minute,
                                };
                            }
                        }

                        if (xmlNode.Attribute("DropItem") != null)
                        {
                            monsterInfo.DropItem = xmlNode.Attribute("DropItem").Value;
                        }

                        // Chuỗi mã hóa danh sách kỹ năng sử dụng
                        string skillsString = xmlNode.Attribute("Skills").Value;
                        // Nếu có kỹ năng
                        if (!string.IsNullOrEmpty(skillsString))
                        {
                            // Duyệt danh sách kỹ năng
                            foreach (string skillStr in skillsString.Split(';'))
                            {
                                string[] fields = skillStr.Split('_');
                                try
                                {
                                    int skillID = int.Parse(fields[0]);
                                    int skillLevel = int.Parse(fields[1]);
                                    int cooldown = int.Parse(fields[2]);

                                    // Thông tin kỹ năng tương ứng
                                    SkillDataEx skillData = KSkill.GetSkillData(skillID);
                                    // Nếu kỹ năng không tồn tại
                                    if (skillData == null)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} not found!", skillID));
                                    }

                                    // Nếu cấp độ dưới 0
                                    if (skillLevel <= 0)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} level must be greater than 0", skillID));
                                    }

                                    // Kỹ năng theo cấp
                                    SkillLevelRef skillRef = new SkillLevelRef()
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
                        string aurasString = xmlNode.Attribute("Auras").Value;
                        // Nếu có kỹ năng
                        if (!string.IsNullOrEmpty(aurasString))
                        {
                            // Duyệt danh sách kỹ năng
                            foreach (string skillStr in aurasString.Split(';'))
                            {
                                string[] fields = skillStr.Split('_');
                                try
                                {
                                    int skillID = int.Parse(fields[0]);
                                    int skillLevel = int.Parse(fields[1]);

                                    // Thông tin kỹ năng tương ứng
                                    SkillDataEx skillData = KSkill.GetSkillData(skillID);
                                    // Nếu kỹ năng không tồn tại
                                    if (skillData == null)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} not found!", skillID));
                                    }

                                    // Nếu cấp độ dưới 0
                                    if (skillLevel <= 0)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} level must be greater than 0", skillID));
                                    }

                                    // Nếu không phải vòng sáng
                                    if (!skillData.IsArua)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} is not Aura!", skillID));
                                    }

                                    // Kỹ năng theo cấp
                                    SkillLevelRef skillRef = new SkillLevelRef()
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
                /// Thông tin Boss Mini, sẽ chọn ngẫu nhiên 1 trong số này để xuất hiện sau mỗi khoảng tương ứng
                /// </summary>
                public class MiniBossInfo
                {
                    /// <summary>
                    /// Thời gian ngẫu nhiên tối thiểu sau khi Boss cũ bị giết
                    /// </summary>
                    public int MinDuration { get; set; }

                    /// <summary>
                    /// Thời gian ngẫu nhiên tối đa sau khi Boss cũ bị giết
                    /// </summary>
                    public int MaxDuration { get; set; }

                    /// <summary>
                    /// Danh sách Boss ngẫu nhiên
                    /// </summary>
                    public List<BossInfo> Bosses { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static MiniBossInfo Parse(XElement xmlNode)
                    {
                        MiniBossInfo miniBossInfo = new MiniBossInfo()
                        {
                            MinDuration = int.Parse(xmlNode.Attribute("MinDuration").Value),
                            MaxDuration = int.Parse(xmlNode.Attribute("MaxDuration").Value),
                            Bosses = new List<BossInfo>(),
                        };

                        foreach (XElement node in xmlNode.Elements("Boss"))
                        {
                            BossInfo bossInfo = BossInfo.Parse(node);
                            miniBossInfo.Bosses.Add(bossInfo);
                        }

                        return miniBossInfo;
                    }
                }

                /// <summary>
                /// Thông tin quái
                /// </summary>
                public class MonsterInfo
                {
                    /// <summary>
                    /// ID quái
                    /// </summary>
                    public int ID { get; set; }

                    /// <summary>
                    /// Tên quái
                    /// </summary>
                    public string Name { get; set; }

                    /// <summary>
                    /// Danh hiệu quái
                    /// </summary>
                    public string Title { get; set; }

                    /// <summary>
                    /// Vị trí X
                    /// </summary>
                    public int PosX { get; set; }

                    /// <summary>
                    /// Vị trí Y
                    /// </summary>
                    public int PosY { get; set; }

                    /// <summary>
                    /// Sinh lực cơ bản
                    /// </summary>
                    public int BaseHP { get; set; }

                    /// <summary>
                    /// Sinh lực tăng thêm mỗi cấp
                    /// </summary>
                    public int HPIncreaseEachLevel { get; set; }

                    /// <summary>
                    /// Loại AI
                    /// </summary>
                    public MonsterAIType AIType { get; set; }

                    /// <summary>
                    /// Ngũ hành
                    /// </summary>
                    public int Series { get; set; }

                    /// <summary>
                    /// ID Script AI điều khiển
                    /// </summary>
                    public int AIScriptID { get; set; }

                    /// <summary>
                    /// Thời gian tái sinh
                    /// </summary>
                    public int RespawnTicks { get; set; }

                    /// <summary>
                    /// Danh sách kỹ năng sẽ sử dụng
                    /// </summary>
                    public List<SkillLevelRef> Skills { get; set; }

                    /// <summary>
                    /// Danh sách vòng sáng sẽ sử dụng
                    /// </summary>
                    public List<SkillLevelRef> Auras { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static MonsterInfo Parse(XElement xmlNode)
                    {
                        MonsterInfo monsterInfo = new MonsterInfo()
                        {
                            ID = int.Parse(xmlNode.Attribute("ID").Value),
                            Name = xmlNode.Attribute("Name").Value,
                            Title = xmlNode.Attribute("Title").Value,
                            PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                            PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                            BaseHP = int.Parse(xmlNode.Attribute("BaseHP").Value),
                            HPIncreaseEachLevel = int.Parse(xmlNode.Attribute("HPIncreaseEachLevel").Value),
                            AIType = (MonsterAIType)int.Parse(xmlNode.Attribute("AIType").Value),
                            Series = int.Parse(xmlNode.Attribute("Series").Value),
                            AIScriptID = int.Parse(xmlNode.Attribute("AIScriptID").Value),
                            RespawnTicks = int.Parse(xmlNode.Attribute("RespawnTicks").Value),
                            Skills = new List<SkillLevelRef>(),
                            Auras = new List<SkillLevelRef>(),
                        };

                        // Chuỗi mã hóa danh sách kỹ năng sử dụng
                        string skillsString = xmlNode.Attribute("Skills").Value;
                        // Nếu có kỹ năng
                        if (!string.IsNullOrEmpty(skillsString))
                        {
                            // Duyệt danh sách kỹ năng
                            foreach (string skillStr in skillsString.Split(';'))
                            {
                                string[] fields = skillStr.Split('_');
                                try
                                {
                                    int skillID = int.Parse(fields[0]);
                                    int skillLevel = int.Parse(fields[1]);
                                    int cooldown = int.Parse(fields[2]);

                                    // Thông tin kỹ năng tương ứng
                                    SkillDataEx skillData = KSkill.GetSkillData(skillID);
                                    // Nếu kỹ năng không tồn tại
                                    if (skillData == null)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} not found!", skillID));
                                    }

                                    // Nếu cấp độ dưới 0
                                    if (skillLevel <= 0)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} level must be greater than 0", skillID));
                                    }

                                    // Kỹ năng theo cấp
                                    SkillLevelRef skillRef = new SkillLevelRef()
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
                        string aurasString = xmlNode.Attribute("Auras").Value;
                        // Nếu có kỹ năng
                        if (!string.IsNullOrEmpty(aurasString))
                        {
                            // Duyệt danh sách kỹ năng
                            foreach (string skillStr in aurasString.Split(';'))
                            {
                                string[] fields = skillStr.Split('_');
                                try
                                {
                                    int skillID = int.Parse(fields[0]);
                                    int skillLevel = int.Parse(fields[1]);

                                    // Thông tin kỹ năng tương ứng
                                    SkillDataEx skillData = KSkill.GetSkillData(skillID);
                                    // Nếu kỹ năng không tồn tại
                                    if (skillData == null)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} not found!", skillID));
                                    }

                                    // Nếu cấp độ dưới 0
                                    if (skillLevel <= 0)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} level must be greater than 0", skillID));
                                    }

                                    // Nếu không phải vòng sáng
                                    if (!skillData.IsArua)
                                    {
                                        throw new Exception(string.Format("Skill ID = {0} is not Aura!", skillID));
                                    }

                                    // Kỹ năng theo cấp
                                    SkillLevelRef skillRef = new SkillLevelRef()
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
                /// Thông tin cổng dịch chuyển
                /// </summary>
                public class TeleportInfo
                {
                    /// <summary>
                    /// Tên cổng
                    /// </summary>
                    public string Name { get; set; }

                    /// <summary>
                    /// Vị trí X
                    /// </summary>
                    public int PosX { get; set; }

                    /// <summary>
                    /// Vị trí Y
                    /// </summary>
                    public int PosY { get; set; }

                    /// <summary>
                    /// ID bản đồ đích đến
                    /// </summary>
                    public int ToMapID { get; set; }

                    /// <summary>
                    /// Vị trí X đích đến
                    /// </summary>
                    public int ToPosX { get; set; }

                    /// <summary>
                    /// Vị trí Y đích đến
                    /// </summary>
                    public int ToPosY { get; set; }

                    /// <summary>
                    /// Chuyển đối tượng từ XMLNode
                    /// </summary>
                    /// <param name="xmlNode"></param>
                    /// <returns></returns>
                    public static TeleportInfo Parse(XElement xmlNode)
                    {
                        return new TeleportInfo()
                        {
                            Name = xmlNode.Attribute("Name").Value,
                            PosX = int.Parse(xmlNode.Attribute("PosX").Value),
                            PosY = int.Parse(xmlNode.Attribute("PosY").Value),
                            ToMapID = int.Parse(xmlNode.Attribute("ToMapID").Value),
                            ToPosX = int.Parse(xmlNode.Attribute("ToPosX").Value),
                            ToPosY = int.Parse(xmlNode.Attribute("ToPosY").Value),
                        };
                    }
                }

                /// <summary>
                /// ID tầng
                /// </summary>
                public int ID { get; set; }

                /// <summary>
                /// ID bản đồ
                /// </summary>
                public int MapID { get; set; }

                /// <summary>
                /// Danh sách khu an toàn
                /// </summary>
                public List<SafeZoneInfo> SafeZones { get; set; }

                /// <summary>
                /// Danh sách NPC
                /// </summary>
                public List<NPCInfo> NPCs { get; set; }

                /// <summary>
                /// Danh sách quái
                /// </summary>
                public List<MonsterInfo> Monsters { get; set; }

                /// <summary>
                /// Danh sách Boss Mini
                /// </summary>
                public List<MiniBossInfo> MiniBosses { get; set; }

                /// <summary>
                /// Danh sách Boss
                /// </summary>
                public List<BossInfo> Bosses { get; set; }

                /// <summary>
                /// Danh sách cổng dịch chuyển
                /// </summary>
                public List<TeleportInfo> Teleports { get; set; }

                /// <summary>
                /// Chuyển đối tượng từ XMLNode
                /// </summary>
                /// <param name="xmlNode"></param>
                /// <returns></returns>
                public static Stage Parse(XElement xmlNode)
                {
                    Stage stage = new Stage()
                    {
                        ID = int.Parse(xmlNode.Attribute("ID").Value),
                        MapID = int.Parse(xmlNode.Attribute("MapID").Value),
                        SafeZones = new List<SafeZoneInfo>(),
                        NPCs = new List<NPCInfo>(),
                        Monsters = new List<MonsterInfo>(),
                        MiniBosses = new List<MiniBossInfo>(),
                        Bosses = new List<BossInfo>(),
                        Teleports = new List<TeleportInfo>(),
                    };

                    // Duyệt danh sách khu an toàn
                    foreach (XElement node in xmlNode.Element("SafeZones").Elements("Zone"))
                    {
                        SafeZoneInfo safeZoneInfo = SafeZoneInfo.Parse(node);
                        stage.SafeZones.Add(safeZoneInfo);
                    }

                    // Duyệt danh sách NPC
                    foreach (XElement node in xmlNode.Element("NPCs").Elements("NPC"))
                    {
                        NPCInfo npcInfo = NPCInfo.Parse(node);
                        stage.NPCs.Add(npcInfo);
                    }

                    // Duyệt danh sách quái
                    foreach (XElement node in xmlNode.Element("Monsters").Elements("Monster"))
                    {
                        MonsterInfo monsterInfo = MonsterInfo.Parse(node);
                        stage.Monsters.Add(monsterInfo);
                    }

                    // Duyệt danh sách MiniBoss
                    foreach (XElement node in xmlNode.Element("Monsters").Elements("MiniBoss"))
                    {
                        MiniBossInfo miniBossInfo = MiniBossInfo.Parse(node);
                        stage.MiniBosses.Add(miniBossInfo);
                    }

                    // Duyệt danh sách Boss
                    foreach (XElement node in xmlNode.Element("Monsters").Elements("Boss"))
                    {
                        BossInfo bossInfo = BossInfo.Parse(node);
                        stage.Bosses.Add(bossInfo);
                    }

                    // Duyệt danh sách cổng dịch chuyển
                    foreach (XElement node in xmlNode.Element("Teleports").Elements("Teleport"))
                    {
                        TeleportInfo teleportInfo = TeleportInfo.Parse(node);
                        stage.Teleports.Add(teleportInfo);
                    }

                    return stage;
                }
            }

            /// <summary>
            /// Thông tin điểm hồi sinh khi chết
            /// </summary>
            public ReliveInfo Relive { get; set; }

            /// <summary>
            /// Danh sách tầng
            /// </summary>
            public Dictionary<int, Stage> Stages { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static EventDetail Parse(XElement xmlNode)
            {
                EventDetail eventDetail = new EventDetail()
                {
                    Relive = ReliveInfo.Parse(xmlNode.Element("Relive")),
                    Stages = new Dictionary<int, Stage>(),
                };

                // Duyệt danh sách tầng
                foreach (XElement node in xmlNode.Elements("Stage"))
                {
                    Stage stage = Stage.Parse(node);
                    eventDetail.Stages[stage.MapID] = stage;
                }

                return eventDetail;
            }
        }

        /// <summary>
        /// danh sách ID các boss trong 
        /// </summary>
        private static HashSet<int> BossIDs;

        /// <summary>
        ///  Danh sách prestige theo id của boss
        /// </summary>
        private static Dictionary<int, int> prestigeKillBoss = new Dictionary<int, int>();

        public static ConcurrentDictionary<int, int> bigBossIDs;

        /// <summary>
        /// Danh sách ID các Player gây sát thương lên boss
        /// </summary>
        public static List<DamgeGroup> dameGroups = new List<DamgeGroup>();

        /// <summary>
        /// Danh sách damage của các bang tham gia hạ gục Boss Hỏa Kỳ Lân
        /// </summary>
        public static List<DamageGuild> dameGuilds = new List<DamageGuild>();
        #endregion

        #region Properties
        /// <summary>
        /// Thiết lập sự kiện
        /// </summary>
        public static EventConfig Config { get; set; }

        /// <summary>
        /// Thông tin sự kiện
        /// </summary>
        public static EventDetail Event { get; set; }
        #endregion

        #region Init
        /// <summary>
        /// Khởi tạo dữ liệu
        /// </summary>
        public static void Init()
        {
            BossIDs = new HashSet<int>();
            XElement xmlNode = null;
            if (GameManager.IsKuaFuServer)
            {
                xmlNode = Global.GetGameResXml("Config/KT_GameEvents/BossUniconKuafu.xml");
            }
            else
            {
                xmlNode = Global.GetGameResXml("Config/KT_GameEvents/BossUnicon.xml");
            }
            BossUnicon.Config = EventConfig.Parse(xmlNode.Element("Config"));
            BossUnicon.Event = EventDetail.Parse(xmlNode.Element("Event"));
            bigBossIDs = new ConcurrentDictionary<int, int>();
            try
            {
                foreach (var stage in Event.Stages.Values)
                {
                    foreach (var boss in stage.Bosses)
                    {
                        BossIDs.Add(boss.ID);
                        prestigeKillBoss[boss.ID] = boss.Prestige;
                    }

                    foreach (var boss in stage.MiniBosses.SelectMany(miniBossInfo => miniBossInfo.Bosses))
                    {
                        BossIDs.Add(boss.ID);
                        prestigeKillBoss[boss.ID] = boss.Prestige;
                    }
                }
            }
            catch
            {
                // nếu phát sinh lỗi gì đó thì bỏ qua, không quan trọng
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Kiểm tra người chơi có đang ở trong bản đồ  không
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsInsideBossUniconMap(KPlayer player)
        {
            return BossUnicon.Event.Stages.ContainsKey(player.CurrentMapCode);
        }

        public static bool IsBoss(int monsterID)
        {
            return BossIDs.Contains(monsterID);
        }

        public static int PrestigeKillBoss(int monterID)
        {
            if (prestigeKillBoss.TryGetValue(monterID, out int prestige))
            {
                return prestige;
            }
            return 0;
        }

        #endregion
    }
}
