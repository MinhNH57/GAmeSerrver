using System;
using System.Collections.Generic;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    internal sealed class KuaFuBalanceOriginalSnapshot
    {
        public int OriginalFactionId { get; set; }

        public int OriginalRouteId { get; set; }

        public int OriginalLevel { get; set; }

        public int OriginalKuafuMoney { get; set; }

        public int OriginalBonusSkillPoints { get; set; }

        public int OriginalBonusPotentialPoints { get; set; }

        public int OriginalStrength { get; set; }

        public int OriginalDexterity { get; set; }

        public int OriginalVitality { get; set; }

        public int OriginalEnergy { get; set; }

        public List<int> OriginalEquipDbIds { get; set; } = new List<int>();

        public Dictionary<int, int> OriginalEquipPositions { get; set; } = new Dictionary<int, int>();

        public Dictionary<int, KuaFuBalanceSkillSnapshot> OriginalSkillSnapshots { get; set; } = new Dictionary<int, KuaFuBalanceSkillSnapshot>();

        /// <summary>
        /// Mã hóa Snapshot thành chuỗi compact siêu ngắn
        /// </summary>
        public string SerializeToCompactString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 1. Thông tin cơ bản
            sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                OriginalFactionId, OriginalRouteId, OriginalLevel, OriginalKuafuMoney,
                OriginalBonusSkillPoints, OriginalBonusPotentialPoints,
                OriginalStrength, OriginalDexterity, OriginalVitality, OriginalEnergy);
            sb.Append("|");

            // 2. Trang bị (DbId:Position)
            List<string> equipList = new List<string>();
            foreach (var dbId in OriginalEquipDbIds)
            {
                int pos = OriginalEquipPositions.ContainsKey(dbId) ? OriginalEquipPositions[dbId] : -1;
                equipList.Add(string.Format("{0}:{1}", dbId, pos));
            }
            sb.Append(string.Join(",", equipList));
            sb.Append("|");

            // 3. Kỹ năng (SkillId:Level:Exp)
            List<string> skillList = new List<string>();
            foreach (var kvp in OriginalSkillSnapshots)
            {
                var skill = kvp.Value;
                if (skill.Exp > 0)
                {
                    skillList.Add(string.Format("{0}:{1}:{2}", skill.SkillId, skill.AddedLevel, skill.Exp));
                }
                else
                {
                    skillList.Add(string.Format("{0}:{1}", skill.SkillId, skill.AddedLevel));
                }
            }
            sb.Append(string.Join(",", skillList));

            return sb.ToString();
        }

        /// <summary>
        /// Giải mã chuỗi compact ngược lại thành đối tượng Snapshot
        /// </summary>
        public static KuaFuBalanceOriginalSnapshot DeserializeFromCompactString(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            try
            {
                string[] parts = data.Split('|');
                if (parts.Length < 3) return null;

                KuaFuBalanceOriginalSnapshot snapshot = new KuaFuBalanceOriginalSnapshot();

                // 1. Parse thông tin cơ bản
                string[] basicFields = parts[0].Split(',');
                if (basicFields.Length >= 10)
                {
                    snapshot.OriginalFactionId = int.Parse(basicFields[0]);
                    snapshot.OriginalRouteId = int.Parse(basicFields[1]);
                    snapshot.OriginalLevel = int.Parse(basicFields[2]);
                    snapshot.OriginalKuafuMoney = int.Parse(basicFields[3]);
                    snapshot.OriginalBonusSkillPoints = int.Parse(basicFields[4]);
                    snapshot.OriginalBonusPotentialPoints = int.Parse(basicFields[5]);
                    snapshot.OriginalStrength = int.Parse(basicFields[6]);
                    snapshot.OriginalDexterity = int.Parse(basicFields[7]);
                    snapshot.OriginalVitality = int.Parse(basicFields[8]);
                    snapshot.OriginalEnergy = int.Parse(basicFields[9]);
                }

                // 2. Parse trang bị
                if (!string.IsNullOrEmpty(parts[1]))
                {
                    string[] equips = parts[1].Split(',');
                    foreach (var equip in equips)
                    {
                        string[] eqParts = equip.Split(':');
                        if (eqParts.Length >= 2)
                        {
                            int dbId = int.Parse(eqParts[0]);
                            int pos = int.Parse(eqParts[1]);
                            snapshot.OriginalEquipDbIds.Add(dbId);
                            snapshot.OriginalEquipPositions[dbId] = pos;
                        }
                    }
                }

                // 3. Parse kỹ năng
                if (!string.IsNullOrEmpty(parts[2]))
                {
                    string[] skills = parts[2].Split(',');
                    foreach (var skill in skills)
                    {
                        string[] skParts = skill.Split(':');
                        if (skParts.Length >= 2)
                        {
                            int skillId = int.Parse(skParts[0]);
                            int addedLevel = int.Parse(skParts[1]);
                            int exp = skParts.Length >= 3 ? int.Parse(skParts[2]) : 0;

                            snapshot.OriginalSkillSnapshots[skillId] = new KuaFuBalanceSkillSnapshot()
                            {
                                SkillId = skillId,
                                HadSkill = true,
                                AddedLevel = addedLevel,
                                Exp = exp
                            };
                        }
                    }
                }

                return snapshot;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
