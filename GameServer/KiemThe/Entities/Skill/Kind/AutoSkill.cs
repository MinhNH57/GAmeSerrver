using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameServer.KiemThe.Entities
{
    /// <summary>
    /// Đối tượng kỹ năng tự động xuất dựa vào các điều kiện
    /// </summary>
    public class AutoSkill
    {
        /// <summary>
        /// Giá trị theo cấp độ
        /// </summary>
        public class ValueByLevel
        {
            /// <summary>
            /// Cấp độ
            /// </summary>
            public int Level { get; set; }

            /// <summary>
            /// Giá trị
            /// </summary>
            public int Value { get; set; }
        }

        /// <summary>
        /// Số lần cộng dồn dùng để kiểm tra điều kiện yêu cầu số lần thỏa mãn nhất định
        /// </summary>
        public int StackCount { get; set; } = 0;

        /// <summary>
        /// ID kỹ năng
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Tên kỹ năng
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Loại kỹ năng
        /// <para>8: Kích hoạt khi khiên nội lực bị phá</para>
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Danh sách tỷ lệ xuất theo cấp độ
        /// </summary>
        public Dictionary<int, ValueByLevel> CastPercentByLevel { get; private set; }

        /// <summary>
        /// ID kỹ năng xuất kèm
        /// </summary>
        public int CastSkillID { get; set; }

        /// <summary>
        /// Cấp độ kỹ năng xuất kèm
        /// </summary>
        public Dictionary<int, ValueByLevel> CastSkillLevelByLevel { get; set; }

        /// <summary>
        /// ID kỹ năng cha
        /// </summary>
        public int ParentSkillID { get; set; }

        /// <summary>
        /// Thời gian giãn cách giữa các lần thi triển theo cấp độ
        /// </summary>
        public Dictionary<int, ValueByLevel> DelayPerCastByLevel { get; set; }

        /// <summary>
        /// Số chiêu xuất tối đa mỗi lần theo cấp
        /// </summary>
        public Dictionary<int, ValueByLevel> MaxCastCountByLevel { get; set; }

        /// <summary>
        /// Kích hoạt tự động khi mất trạng thái Buff ID tương ứng
        /// <para>-1: Vô hiệu</para>
        /// </summary>
        public int ActivateIfLostBuff { get; set; }

        /// <summary>
        /// Kích hoạt khi đánh trúng mục tiêu
        /// </summary>
        public bool ActivateIfHitTarget { get; set; }

        /// <summary>
        /// Kích hoạt khi % sinh lực giảm xuống dưới ngưỡng
        /// </summary>
        public int ActivateWhenHPPercentDropBelow { get; set; }

        /// <summary>
        /// Kích hoạt khi bị đánh trúng
        /// </summary>
        public bool ActivateWhenBeHit { get; set; }

        /// <summary>
        /// Kiểm tra cự ly và vũ khí của kỹ năng con trước khi tự động thi triển
        /// </summary>
        public bool EnforceCastSkillConditions { get; set; }

        /// <summary>
        /// Kích hoạt khi đánh chí mạng
        /// </summary>
        public bool ActivateWhenDoCritHit { get; set; }

        /// <summary>
        /// Kích hoạt khi bị trọng thương
        /// </summary>
        public bool ActivateAfterDie { get; set; }

        /// <summary>
        /// Kích hoạt sau khi đánh đủ số đòn chí mạng lên mục tiêu
        /// </summary>
        public int ActivateAfterDoTotalCritHit { get; set; }

        /// <summary>
        /// Kích hoạt khi bị đánh trúng bởi các kỹ năng
        /// </summary>
        public List<int> ActivateWhenBeHitBySkills { get; set; }

        /// <summary>
        /// Kích hoạt khi đánh trúng mục tiêu bằng các kỹ năng
        /// </summary>
        public List<int> ActivateWhenHitWithSkills { get; set; }

        /// <summary>
        /// Kích hoạt nếu không dùng kỹ năng trong số Frame tương ứng
        /// </summary>
        public int ActivateIfNoUseSkillForFrame { get; set; }

        /// <summary>
        /// Kích hoạt nếu không dùng kỹ năng làm mất trạng thái tàng hình trong số Frame tương ứng
        /// </summary>
        public int ActivateIfNoUseSkillCauseLostInvisibilityForFrame { get; set; }

        /// <summary>
        /// Kích hoạt mỗi khoảng thời gian
        /// </summary>
        public int ActivateEachFrame { get; set; }

        /// <summary>
        /// Chuyển đối tượng từ XML Node
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="defaultValues"></param>
        /// <returns></returns>
        public static AutoSkill Parse(XElement xmlNode, Dictionary<string, object> defaultValues)
        {
            var autoSkill = new AutoSkill
            {
                ID = KTGlobal.GetXMLAttribute<int>(xmlNode, "ID", defaultValues),
                Name = KTGlobal.GetXMLAttribute<string>(xmlNode, "Name", defaultValues),
                Type = KTGlobal.GetXMLAttribute<int>(xmlNode, "Type", defaultValues),
                CastPercentByLevel = AnalysisValueByLevel(KTGlobal.GetXMLAttribute<string>(xmlNode, "CastPercent", defaultValues)),
                CastSkillID = KTGlobal.GetXMLAttribute<int>(xmlNode, "CastSkillID", defaultValues),
                CastSkillLevelByLevel =
                    AnalysisValueByLevel(KTGlobal.GetXMLAttribute<string>(xmlNode, "CastSkillLevel", defaultValues)),
                ParentSkillID =
                    KTGlobal.GetXMLAttribute<int>(xmlNode, "ParentSkillID", defaultValues),
                DelayPerCastByLevel =
                    AnalysisValueByLevel(KTGlobal.GetXMLAttribute<string>(xmlNode, "DelayPerCast", defaultValues)),
                MaxCastCountByLevel = AnalysisValueByLevel(KTGlobal.GetXMLAttribute<string>(xmlNode, "MaxCastCount", defaultValues)),
                ActivateIfLostBuff = KTGlobal.GetXMLAttribute<int>(xmlNode, "ActivateIfLostBuff",
                    defaultValues),
                ActivateIfHitTarget = KTGlobal.GetXMLAttribute<bool>(xmlNode, "ActivateIfHitTarget",
                    defaultValues),
                ActivateWhenHPPercentDropBelow = KTGlobal.GetXMLAttribute<int>(xmlNode,
                    "ActivateWhenHPPercentDropBelow", defaultValues),
                ActivateWhenBeHit =
                    KTGlobal.GetXMLAttribute<bool>(xmlNode, "ActivateWhenBeHit", defaultValues),
                EnforceCastSkillConditions = xmlNode.Attribute("EnforceCastSkillConditions") != null &&
                    bool.Parse(xmlNode.Attribute("EnforceCastSkillConditions").Value),
                ActivateWhenDoCritHit = KTGlobal.GetXMLAttribute<bool>(xmlNode, "ActivateWhenDoCritHit",
                    defaultValues),
                ActivateAfterDie =
                    KTGlobal.GetXMLAttribute<bool>(xmlNode, "ActivateAfterDie", defaultValues),
                ActivateAfterDoTotalCritHit = KTGlobal.GetXMLAttribute<int>(xmlNode, "ActivateAfterDoTotalCritHit",
                    defaultValues),
                ActivateWhenBeHitBySkills = new List<int>(),
                ActivateWhenHitWithSkills = new List<int>(),
                ActivateIfNoUseSkillForFrame = KTGlobal.GetXMLAttribute<int>(xmlNode, "ActivateIfNoUseSkillForFrame",
                    defaultValues),
                ActivateIfNoUseSkillCauseLostInvisibilityForFrame = KTGlobal.GetXMLAttribute<int>(xmlNode,
                    "ActivateIfNoUseSkillCauseLostInvisibilityForFrame",
                    defaultValues),
                ActivateEachFrame =
                    KTGlobal.GetXMLAttribute<int>(xmlNode, "ActivateEachFrame", defaultValues)
            };
            var activateWhenByHitBySkills = KTGlobal.GetXMLAttribute<string>(xmlNode, "ActivateWhenBeHitBySkills",
                defaultValues).Split(',');
            foreach (var str in activateWhenByHitBySkills)
            {
                var skillID = int.Parse(str);
                if (skillID != -1)
                {
                    autoSkill.ActivateWhenBeHitBySkills.Add(skillID);
                }
            }

            var activateWhenHitWithSkills = KTGlobal.GetXMLAttribute<string>(xmlNode, "ActivateWhenHitWithSkills",
                defaultValues).Split(',');
            foreach (var str in activateWhenHitWithSkills)
            {
                var skillID = int.Parse(str);
                if (skillID != -1)
                {
                    autoSkill.ActivateWhenHitWithSkills.Add(skillID);
                }
            }

            return autoSkill;
        }

        /// <summary>
        /// Phân tích dữ liệu theo cấp độ 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static Dictionary<int, ValueByLevel> AnalysisValueByLevel(string data)
        {
            var array = new Dictionary<int, ValueByLevel>();
            const string regex = @"\{(\d+),(\d+)\}";
            if (Regex.IsMatch(data, regex))
            {
                foreach (Match match in Regex.Matches(data, regex))
                {
                    var level = int.Parse(match.Groups[1].Value);
                    var value = int.Parse(match.Groups[2].Value);
                    array[level] = new ValueByLevel
                    {
                        Level = level,
                        Value = value,
                    };
                }

                var list = MakeChildLevel(array.Values.ToList());
                foreach (var val in list)
                {
                    array[val.Level] = val;
                }
            }
            else
            {
                for (var i = 1; i <= SkillDataEx.SystemMaxLevel; i++)
                {
                    array[i] = new ValueByLevel
                    {
                        Level = i,
                        Value = int.Parse(data),
                    };
                }
            }

            return array;
        }

        /// <summary>
        /// Tạo danh sách giá trị các cấp độ con
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static List<ValueByLevel> MakeChildLevel(List<ValueByLevel> array)
        {
            var list = new List<ValueByLevel>();

            array.Sort((o1, o2) => { return o1.Level - o2.Level; });

            list.Add(array[0]);

            float lastDelta = 0;
            for (var i = 1; i < array.Count; i++)
            {
                var dLevel = array[i].Level - array[i - 1].Level;
                var dValue = array[i].Value - array[i - 1].Value;
                var delta = dValue * 1f / dLevel;
                for (var j = array[i - 1].Level + 1; j < array[i].Level; j++)
                {
                    list.Add(new ValueByLevel
                    {
                        Level = j,
                        Value = (int)(array[i - 1].Value + (j - array[i - 1].Level) * delta),
                    });
                }

                list.Add(array[i]);

                lastDelta = Math.Max(lastDelta, delta);
            }

            for (var j = array[array.Count - 1].Level + 1; j <= SkillDataEx.SystemMaxLevel; j++)
            {
                list.Add(new ValueByLevel
                {
                    Level = j,
                    Value = (int)(array[array.Count - 1].Value + (j - array[array.Count - 1].Level) * lastDelta),
                });
            }

            return list;
        }
    }
}
