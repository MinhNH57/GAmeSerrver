using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Tools;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.Core.Item;
using Server.Data;

namespace GameServer.KiemThe.Entities
{
    /// <summary>
    /// Quản lý Logic kỹ năng
    /// </summary>
    public partial class SkillTree
    {
        /// <summary>
        /// Danh sách kỹ năng mà nhân vật học được
        /// <para>Key: ID kỹ năng</para>
        /// <para>Value: Cấp độ kỹ năng</para>
        /// </summary>
        private readonly ConcurrentDictionary<int, SkillLevelRef> listStudiedSkills = new ConcurrentDictionary<int, SkillLevelRef>();

        /// <summary>
        /// Điểm kỹ năng đã cộng vào
        /// </summary>
        public int AddedSkillPoints
        {
            get
            {
                List<int> skills = KFaction.GetFactionSkills(this.Player.m_cPlayerFaction.GetFactionId());
                return this.listStudiedSkills.Where(x => x.Value.CanAddPoint && skills.Contains(x.Value.SkillID)).Sum(x => x.Value.AddedLevel);
            }
        }

        /// <summary>
        /// Danh sách kỹ năng của người chơi
        /// </summary>
        public List<SkillLevelRef> ListSkills
        {
            get
            {
                return this.listStudiedSkills.Values.ToList<SkillLevelRef>();
            }
        }
        

        private int _AllSkillBonusLevel = 0;
        /// <summary>
        /// Điểm cộng thêm cho tất cả kỹ năng
        /// </summary>
        public int AllSkillBonusLevel
		{
			get
			{
                return this._AllSkillBonusLevel;
			}
			set
			{
                this._AllSkillBonusLevel = value;

                /// Mức máu hiện tại
                int currentHP = this.Player.m_CurrentLife;
                /// Mức khí hiện tại
                int currentMP = this.Player.m_CurrentMana;
                /// Mức thể lực hiện tại
                int currentStamina = this.Player.m_CurrentStamina;

                /// ID kỹ năng vòng sáng trước
                int lastAuraSkill = -1;
                /// Nếu có vòng sáng đang kích hoạt
                if (this.Player.Buffs.CurrentArua != null)
                {
                    lastAuraSkill = this.Player.Buffs.CurrentArua.Skill.SkillID;
                }
                
                this.Player.SaveLastTicksAutoSkill();

                /// Hủy kỹ năng bị động và vòng sáng
                this.Player.DeactivateAuraPassiveAndEnchantSkills();

                /// Duyệt danh sách kỹ năng và thêm điểm cộng vào
                foreach (SkillLevelRef skillRef in this.ListSkills)
                {
                    bool isSkillPet = false;
                    skillRef.BonusLevel = 0;
                    if (PetManager.InfoPets.PetSkills.Where(x => x.ID == skillRef.SkillID).FirstOrDefault() != null)
                    {
                        isSkillPet = true;
                    }
                    if (!isSkillPet)
                    {
                        skillRef.BonusLevel = value;
                    }
                }

                /// Kích hoạt lại kỹ năng bị động và vòng sáng
                this.Player.ReactivateAuraPassiveAndEnchantSkills();

                /// Nếu có kỹ năng vòng sáng hiện tại
                if (lastAuraSkill != -1 && this.HasSkill(lastAuraSkill))
                {
                    /// Thực hiện kỹ năng vòng sáng
                    KTSkillManager.UseSkill(this.Player, this.Player, this.GetSkillLevelRef(lastAuraSkill), true);
                }

                /// Cập nhật mức máu
                this.Player.m_CurrentLife = Math.Min(this.Player.m_CurrentLifeMax, currentHP);
                /// Cập nhật mức khí
                this.Player.m_CurrentMana = Math.Min(this.Player.m_CurrentManaMax, currentMP);
                /// Cập nhật mức thể lực
                this.Player.m_CurrentStamina = Math.Min(this.Player.m_CurrentStaminaMax, currentStamina);

                /// Cập nhật lại danh sách kỹ năng gửi về Client
                this.ExportSkillTree();
            }
        }

        /// <summary>
        /// Thêm kinh nghiệm cho kỹ năng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="exp"></param>
        public void AddSkillExp(int id, int exp)
        {
            /// Nếu kỹ năng tồn tại
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                /// Kỹ năng tương ứng
                SkillDataEx skillData = KSkill.GetSkillData(id);
                if (skillData != null)
                {
                    /// Nếu không phải kỹ năng cần mật tịch để thăng cấp
                    if (!skillData.IsExpSkill)
                    {
                        return;
                    }

                    if (skillRef.AddedLevel == 0)
                        skillRef.AddedLevel = 1;
                    /// ProDict tương ứng
                    PropertyDictionary skillPd = skillData.Properties[skillRef.AddedLevel];
                    /// Nếu không tồn tại Symbol exp
                    if (!skillPd.ContainsKey((int) MAGIC_ATTRIB.magic_skill_skillexp_v))
                    {
                        return;
                    }

                    /// Cấp hiện tại
                    int currentLevel = skillRef.AddedLevel;
                    /// Kinh nghiệm thêm vào
                    int nAddExp = skillRef.Exp + exp;
                    do
                    {
                        /// Nếu cấp độ vượt quá ngưỡng
                        if (skillRef.AddedLevel >= skillData.MaxSkillLevel)
                        {
                            break;
                        }

                        LogManager.WriteLog(LogTypes.Book, $"Player {this.Player.RoleName} [{this.Player.RoleID}] has skillID {skillRef.SkillID} [level = {skillRef.Level}, exp = {skillRef.Exp}] + {nAddExp} exp");

                        /// ProDict tương ứng
                        skillPd = skillData.Properties[skillRef.AddedLevel];
                        /// Kinh nghiệm cấp hiện tại cần
                        int thisLevelMaxExp = skillPd.Get<KMagicAttrib>((int) MAGIC_ATTRIB.magic_skill_skillexp_v).nValue[0];

                        /// Nếu kinh nghiệm vượt quá ngưỡng thăng cấp
                        if (nAddExp >= thisLevelMaxExp)
                        {
                            nAddExp -= thisLevelMaxExp;
                            /// Thăng cấp kỹ năng
                            skillRef.AddedLevel++;
                        }
                        else
                        {
                            /// Cập nhật lượng kinh nghiệm thêm vào
                            skillRef.Exp = nAddExp;
                            break;
                        }
                    }
                    while (true);
                    /// Cập nhật lượng kinh nghiệm thêm vào
                    skillRef.Exp = nAddExp;

                    /// Nếu cấp độ thay đổi
                    if (currentLevel != skillRef.Level)
                    {
                        /// Nếu là kỹ năng bị động hoặc vòng sáng đang được kích hoạt
                        if (skillRef.Data.Type == 3 || (skillRef.Data.IsArua && this.Player.Buffs.CurrentArua != null && this.Player.Buffs.CurrentArua.Skill.SkillID == skillRef.SkillID))
                        {
                            /// Thực hiện lại kỹ năng bị động
                            KTSkillManager.ProcessPassiveSkill(this.Player, skillRef);
                        }

                        /// Thực hiện cập nhật danh sách hỗ trợ sát thương cho kỹ năng khác
                        this.DoProcessSkillAppendDamages(skillRef.Data, skillRef.Level, skillRef.Level);

                        /// Đổ dữ liệu về RoleDataEx
                        this.ExportSkillTree();

                        /// Thực hiện lại các kỹ năng được hỗ trợ
                        this.ProcessEnchantSkills();
                    }

                    ///// Cập nhật thông tin kỹ năng vào DB
                    //KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, id, skillRef.AddedLevel, this.GetSkillLastUsedTick(id), this.GetSkillCooldown(id), skillRef.Exp);
                }
            }
        }

        /// <summary>
        /// Thêm kỹ năng
        /// <para>Kỹ năng được thêm vào sẽ mang cấp 0</para>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="level"></param>
        public void AddSkill(int id)
        {
            if (!this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                SkillDataEx skillData = KSkill.GetSkillData(id);
                if (skillData != null && !this.listStudiedSkills.ContainsKey(id))
                {
                    SkillLevelRef levelRef = new SkillLevelRef()
                    {
                        AddedLevel = 0,
                        Data = skillData,
                        BonusLevel = this._AllSkillBonusLevel,
                        CanStudy = true,
                        Exp = 0,
                    };
                    this.listStudiedSkills[skillData.ID] = levelRef;

                    if (!levelRef.CanStudy)
                    {
                        /// Cái này đảm bảo cái hàm Verify đằng sau chắc chắn nó sẽ trả ra lỗi
                        levelRef.AddedLevel = 999999;
                    }
                    else
                    {
                        KT_TCPHandler.AddSkillToDB(this.Player, id);
                    }
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Them ky nang cung cap do
        /// </summary>
        /// <param name="id"></param>
        /// <param name="level"></param>
        public void AddSkillWithLevel(int id, int level)
        {
            if (!this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                SkillDataEx skillData = KSkill.GetSkillData(id);
                if (skillData != null && !this.listStudiedSkills.ContainsKey(id))
                {
                    SkillLevelRef levelRef = new SkillLevelRef()
                    {
                        AddedLevel = level,
                        Data = skillData,
                        BonusLevel = this._AllSkillBonusLevel,
                        CanStudy = true,
                        Exp = 0,
                    };
                    this.listStudiedSkills[skillData.ID] = levelRef;

                    if (!levelRef.CanStudy)
                    {
                        /// Cái này đảm bảo cái hàm Verify đằng sau chắc chắn nó sẽ trả ra lỗi
                        levelRef.AddedLevel = 999999;
                    }
                    else
                    {
                        KT_TCPHandler.AddSkillToDB(this.Player, id);
                    }
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Thêm danh sách kỹ năng
        /// <para>Kỹ năng được thêm vào sẽ mang cấp 0</para>
        /// </summary>
        /// <param name="skillIds"></param>
        /// <param name="notifyToClient"></param>
        public void AddSkills(ICollection<int> skillIds)
        {
            foreach (int id in skillIds)
            {
                if (!this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
                {
                    SkillDataEx skillData = KSkill.GetSkillData(id);
                    if (skillData != null && !this.listStudiedSkills.ContainsKey(id))
                    {
                        SkillLevelRef levelRef = new SkillLevelRef()
                        {
                            AddedLevel = 0,
                            Data = skillData,
                            BonusLevel = this._AllSkillBonusLevel,
                            CanStudy = true,
                            Exp = 0,
                        };
                        this.listStudiedSkills[skillData.ID] = levelRef;

                        if (!levelRef.CanStudy)
                        {
                            /// Cái này đảm bảo cái hàm Verify đằng sau chắc chắn nó sẽ trả ra lỗi
                            levelRef.AddedLevel = 9999999;

                            break;
                        }

                        KT_TCPHandler.AddSkillToDB(this.Player, id);
                    }
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Cập nhật dữ liệu cấp độ kỹ năng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="level"></param>
        public void AddSkillLevel(int id, int level)
        {
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                /// Nếu là kỹ năng bị động hoặc vòng sáng đang được kích hoạt
                if (skillRef.Data.Type == 3 || (skillRef.Data.IsArua && this.Player.Buffs.CurrentArua != null && this.Player.Buffs.CurrentArua.Skill.SkillID == skillRef.SkillID))
                {
                    /// Lấy giá trị Buff cũ
                    BuffDataEx buff = this.Player.Buffs.GetBuff(skillRef.SkillID);
                    if (buff != null)
                    {
                        /// Xóa Buff cũ đi
                        this.Player.Buffs.RemoveBuff(buff, true, false);
                    }
                }
                if (PetManager.InfoPets.PetSkills.Where(x => x.ID == skillRef.SkillID).FirstOrDefault() != null)
                    skillRef.isSkillPet = true;

                int oldLevel = skillRef.Level;
                skillRef.AddedLevel += level;
                skillRef.AddedLevel = System.Math.Min(skillRef.AddedLevel, skillRef.Data.MaxSkillLevel);

                /// Nếu là kỹ năng bị động hoặc vòng sáng đang được kích hoạt
                if (skillRef.Data.Type == 3 || (skillRef.Data.IsArua && this.Player.Buffs.CurrentArua != null && this.Player.Buffs.CurrentArua.Skill.SkillID == skillRef.SkillID))
                {
                    /// Thực hiện lại kỹ năng bị động
                    KTSkillManager.ProcessPassiveSkill(this.Player, skillRef);
                }

                /// Thực hiện cập nhật danh sách hỗ trợ sát thương cho kỹ năng khác
                this.DoProcessSkillAppendDamages(skillRef.Data, oldLevel, skillRef.Level);

                /// Cập nhật thông tin kỹ năng vào DB
                KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, id, skillRef.AddedLevel, this.GetSkillLastUsedTick(id), this.GetSkillCooldown(id), skillRef.Exp);
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        public void ResetSkillLevel( int id)
        {
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skill))
            {
                /// Xóa Buff tương ứng nếu là kỹ năng bị động
                if (skill.Data.Type == 3)
                {
                    this.Player.Buffs.RemoveBuff(skill.SkillID);
                }

                /// Thiết lập cấp độ về 0
                this.listStudiedSkills[id].AddedLevel = 0;

                /// Xóa sát thương hỗ trợ
                this.appendSkillDamages.TryRemove(id, out _);
                this.appendSkillDamagesPercent.TryRemove(id, out _);

                /// Lưu kỹ năng vào DB
                KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, skill.SkillID, skill.AddedLevel, this.GetSkillLastUsedTick(skill.SkillID), this.GetSkillCooldown(skill.SkillID), skill.Exp);
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Cập nhật dữ liệu danh sách cấp độ kỹ năng
        /// </summary>
        /// <param name="data"></param>
        public void AddSkillsLevel(ICollection<KeyValuePair<int, int>> data)
        {
            foreach (KeyValuePair<int, int> pair in data)
            {
                if (this.listStudiedSkills.TryGetValue(pair.Key, out SkillLevelRef skillRef))
                {
                    /// Nếu là kỹ năng bị động hoặc vòng sáng đang được kích hoạt
                    if (skillRef.Data.Type == 3 || (skillRef.Data.IsArua && this.Player.Buffs.CurrentArua != null && this.Player.Buffs.CurrentArua.Skill.SkillID == skillRef.SkillID))
                    {
                        /// Lấy giá trị Buff cũ
                        BuffDataEx buff = this.Player.Buffs.GetBuff(skillRef.SkillID);
                        if (buff != null)
                        {
                            /// Xóa Buff cũ đi
                            this.Player.Buffs.RemoveBuff(buff, true, false);
                        }
                    }

                    int oldLevel = skillRef.Level;
                    skillRef.AddedLevel += pair.Value;
                    skillRef.AddedLevel = System.Math.Min(skillRef.AddedLevel, skillRef.Data.MaxSkillLevel);

                    /// Nếu là kỹ năng bị động hoặc vòng sáng đang được kích hoạt
                    if (skillRef.Data.Type == 3 || (skillRef.Data.IsArua && this.Player.Buffs.CurrentArua != null && this.Player.Buffs.CurrentArua.Skill.SkillID == skillRef.SkillID))
                    {
                        /// Thực hiện lại kỹ năng bị động
                        KTSkillManager.ProcessPassiveSkill(this.Player, skillRef);
                    }

                    /// Thực hiện cập nhật danh sách hỗ trợ sát thương cho kỹ năng khác
                    this.DoProcessSkillAppendDamages(skillRef.Data, oldLevel, skillRef.Level);

                    /// Cập nhật thông tin kỹ năng vào DB
                    KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, pair.Key, skillRef.AddedLevel, this.GetSkillLastUsedTick(skillRef.SkillID), this.GetSkillCooldown(skillRef.SkillID), skillRef.Exp);
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Tẩy điểm kỹ năng trong danh sách tương ứng
        /// </summary>
        /// <param name="data"></param>
        public void ResetSkillsLevel(ICollection<int> data)
        {
            foreach (int id in data)
            {
                if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skill))
                {
                    /// Xóa Buff tương ứng nếu là kỹ năng bị động
                    if (skill.Data.Type == 3)
                    {
                        this.Player.Buffs.RemoveBuff(skill.SkillID);
                    }

                    /// Thiết lập cấp độ về 0
                    this.listStudiedSkills[id].AddedLevel = 0;

                    /// Xóa sát thương hỗ trợ
                    this.appendSkillDamages.TryRemove(id, out _);
                    this.appendSkillDamagesPercent.TryRemove(id, out _);

                    /// Lưu kỹ năng vào DB
                    KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, skill.SkillID, skill.AddedLevel, this.GetSkillLastUsedTick(skill.SkillID), this.GetSkillCooldown(skill.SkillID), skill.Exp);
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Tẩy lại toàn bộ điểm kỹ năng
        /// </summary>
        public void ResetAllSkillsLevel()
        {
            /// Duyệt toàn bộ danh sách
            foreach (SkillLevelRef skillRef in this.listStudiedSkills.Values)
            {
                /// Xóa Buff tương ứng nếu là kỹ năng bị động
                if (skillRef.Data.Type == 3)
                {
                    this.Player.Buffs.RemoveBuff(skillRef.SkillID);
                }

                /// Thiết lập cấp độ về 0
                skillRef.AddedLevel = 0;

                /// Xóa sát thương hỗ trợ
                this.appendSkillDamages.Clear();
                this.appendSkillDamagesPercent.Clear();

                /// Lưu kỹ năng vào DB
                KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, skillRef.SkillID, skillRef.AddedLevel, this.GetSkillLastUsedTick(skillRef.SkillID), this.GetSkillCooldown(skillRef.SkillID), skillRef.Exp);
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        public void RestoreSkillLevelAndExp(int id, int addedLevel, int exp)
        {
            SkillDataEx skillData = KSkill.GetSkillData(id);
            if (skillData == null)
            {
                return;
            }

            if (!this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                skillRef = new SkillLevelRef()
                {
                    AddedLevel = 0,
                    Data = skillData,
                    BonusLevel = this._AllSkillBonusLevel,
                    CanStudy = true,
                    Exp = 0,
                };

                this.listStudiedSkills[skillData.ID] = skillRef;
                KT_TCPHandler.AddSkillToDB(this.Player, id);
            }

            if (skillRef.Data.Type == 3)
            {
                this.Player.Buffs.RemoveBuff(skillRef.SkillID);
            }

            int oldLevel = skillRef.Level;
            int maxLevel = skillRef.Data.MaxSkillLevel > 0 ? skillRef.Data.MaxSkillLevel : addedLevel;
            skillRef.AddedLevel = Math.Max(0, Math.Min(addedLevel, maxLevel));
            skillRef.Exp = Math.Max(0, exp);

            if (skillRef.Level > 0 && skillRef.Data.Type == 3)
            {
                KTSkillManager.ProcessPassiveSkill(this.Player, skillRef);
            }

            this.DoProcessSkillAppendDamages(skillRef.Data, oldLevel, skillRef.Level);
            KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, skillRef.SkillID, skillRef.AddedLevel, this.GetSkillLastUsedTick(skillRef.SkillID), this.GetSkillCooldown(skillRef.SkillID), skillRef.Exp);

            this.Player.RefreshSkillPoints();
            this.ExportSkillTree();
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Xóa kỹ năng
        /// </summary>
        public void RemoveSkill(int id)
        {
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skill))
            {
                /// Xóa Buff tương ứng nếu là kỹ năng bị động
                if (skill.Data.Type == 3)
                {
                    this.Player.Buffs.RemoveBuff(skill.SkillID);
                }

                /// Xóa kỹ năng khỏi danh sách
                this.listStudiedSkills.TryRemove(id, out _);

                /// Thực hiện giảm sát thương được hỗ trợ
                this.DoProcessSkillAppendDamages(skill.Data, skill.Level, 0);

                /// Xóa kỹ năng khỏi DB
                KT_TCPHandler.DeleteSkillFromDB(this.Player, id);
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// xóa tất cả các kỹ năng có yêu cầu level cao hơn level hiện tại
        /// dùng khi sử dụng vật phẩm cho phép hạ cấp độ
        /// </summary>
        public void RemoveInvalidSkillBasedOnLevel()
        {
            var currentLevel = this.Player.m_Level;
            var factionId = this.Player.m_cPlayerFaction.GetFactionId();
            var factionSkills = KFaction.GetFactionSkills(factionId);
            
            // add lại các skill môn phái nếu trước đó đã bị xoá
            this.Player.Skills.AddSkills(factionSkills);
            
            // xoá các skill không phải của môn phái 
            var skillsToRemove = this.listStudiedSkills.Values
                .Where(x => !factionSkills.Contains(x.SkillID) &&  x.Data.RequireLevel > currentLevel)
                .Select(x => x.SkillID)
                .ToList();
            this.RemoveSkills(skillsToRemove);
        }

        /// <summary>
        /// Xóa kỹ năng
        /// </summary>
        /// <param name="skillIds"></param>
        public void RemoveSkills(ICollection<int> skillIds)
        {
            foreach (int id in skillIds)
            {
                if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skill))
                {
                    /// Xóa Buff tương ứng nếu là kỹ năng bị động
                    if (skill.Data.Type == 3)
                    {
                        this.Player.Buffs.RemoveBuff(skill.SkillID);
                    }

                    /// Xóa kỹ năng khỏi danh sách
                    this.listStudiedSkills.TryRemove(id, out _);

                    /// Xóa kỹ năng khỏi DB
                    KT_TCPHandler.DeleteSkillFromDB(this.Player, id);
                }
            }
            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }
        
        public void RemoveSkillsWithout(int subID = -1)
        {
            //foreach (int id in skillsIDs)
            //{
            //    if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skill))
            //    {
            //        int mainFaction = this.Player.MainFaction;
            //        int resFaction = this.Player.ResFaction;
            //        if (skill.Faction != mainFaction && skill.Faction != resFaction && skill.Faction != 0)
            //        {
            //            /// Xóa Buff tương ứng nếu là kỹ năng bị động
            //            if (skill.Data.Type == 3)
            //            {
            //                this.Player.Buffs.RemoveBuff(skill.SkillID);
            //            }
            //
            //            /// Xóa kỹ năng khỏi danh sách
            //            this.listStudiedSkills.TryRemove(id, out _);
            //
            //            /// Xóa kỹ năng khỏi DB
            //            KT_TCPHandler.DeleteSkillFromDB(this.Player, id);
            //        }
            //    }
            //}
            int mainFaction = this.Player.GetValueOfForeverRecore(GameServer.Logic.RecoreDef.MainFaction);
            int resFaction = this.Player.GetValueOfForeverRecore(GameServer.Logic.RecoreDef.ResFaction);

            List<int> skillsMainFaction = KFaction.GetSkillsOutFaction(mainFaction);
            List<int> skillsResFaction = KFaction.GetSkillsOutFaction(resFaction);

            List<int> skills = new List<int>();
            if (subID == 1)
            {
                subID = 2;
            }
            else if (subID == 2)
            {
                subID = 1;
            }
            if (subID > 0)
            {
                skills = KFaction.GetSkillsWithFactionAndSubID(this.Player.m_cPlayerFaction.GetFactionId(), subID);
                if (skills != null)
                {

                    for (int i = 0; i < skills.Count; i++)
                    {
                        LogManager.WriteLog(LogTypes.SongTu, string.Format("Người chơi: {0}, có skill {1} thuộc nhánh tu luyện khác", this.Player.RoleName, i));
                    }
                }    
            }

            foreach (SkillLevelRef skill in this.listStudiedSkills.Values)
            {
                if (skillsMainFaction.Contains(skill.SkillID) && skillsResFaction.Contains(skill.SkillID) && skill.SkillID >=10)
                {
                    /// Xóa Buff tương ứng nếu là kỹ năng bị động
                    if (skill.Data.Type == 3)
                    {
                        this.Player.Buffs.RemoveBuff(skill.SkillID);
                    }

                    /// Xóa kỹ năng khỏi danh sách
                    this.listStudiedSkills.TryRemove(skill.SkillID, out _);

                    LogManager.WriteLog(LogTypes.SongTu, string.Format("Người chơi: {0}, đã bị xóa skill {1} khi song tu", this.Player.RoleName, skill.Data.Name));

                    /// Xóa kỹ năng khỏi DB
                    KT_TCPHandler.DeleteSkillFromDBWithFaction(this.Player, skill.SkillID);
                }

                if (skills.Contains(skill.SkillID) && skills != null)
                {
                    /// Xóa Buff tương ứng nếu là kỹ năng bị động
                    if (skill.Data.Type == 3)
                    {
                        this.Player.Buffs.RemoveBuff(skill.SkillID);
                    }

                    /// Xóa kỹ năng khỏi danh sách
                    this.listStudiedSkills.TryRemove(skill.SkillID, out _);

                    LogManager.WriteLog(LogTypes.SongTu, string.Format("Người chơi: {0}, đã bị xóa skill {1} khi song tu", this.Player.RoleName, skill.Data.Name));

                    /// Xóa kỹ năng khỏi DB
                    KT_TCPHandler.DeleteSkillFromDBWithFaction(this.Player, skill.SkillID);
                }
            }    

            this.Player.RefreshSkillPoints();

            /// Đổ dữ liệu về RoleDataEx và lưu vào DB
            this.ExportSkillTree();

            /// Thực hiện lại các kỹ năng được hỗ trợ
            this.ProcessEnchantSkills();
        }

        /// <summary>
        /// Trả về thông tin kỹ năng
        /// </summary>
        /// <param name="id"></param>
        public SkillLevelRef GetSkillLevelRef(int id)
        {
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef levelRef))
            {
                return levelRef;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra trong SkillTree có kỹ năng tương ứng không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasSkill(int id)
        {
            return this.GetSkillLevelRef(id) != null;
        }
        
        
        public List<SkillLevelBonus> BonusLevelSkills = new List<SkillLevelBonus>();
        public List<SkillLevelBonus> BonusBookLevelSkills = new List<SkillLevelBonus>();
        // danh sách skill theo book
        public List<(int idSkill,int itemLevel)> BookSkillIDs = new List<(int,int)>();

        public int GetBonusByID(int skillID)
        {
            int bonus = 0;
            var data = this.BonusLevelSkills.FindAll(x=>x.id == skillID);
            if (data != null)
            {
                foreach (var VARIABLE in data)
                {
                    bonus += VARIABLE.bonus;
                }
            }
            return bonus;
        }

        /// <summary>
        /// Tính toán lại cấp độ kỹ năng mật tịch dựa trên lượng EXP hiện có (phục hồi lỗi reset về 0/1)
        /// </summary>
        /// <summary>
        /// Tính toán lại cấp độ kỹ năng mật tịch dựa trên lượng EXP hiện có (phục hồi lỗi reset về 0/1)
        /// </summary>
        public void RecomputeBookSkillLevel(int id)
        {
            if (this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
            {
                SkillDataEx skillData = KSkill.GetSkillData(id);
                if (skillData != null && skillData.IsExpSkill)
                {
                    int totalExpFromBooks = 0;
                    HashSet<int> processedBookDbIds = new HashSet<int>();

                    // 1. Quét toàn bộ túi đồ và rương đồ của nhân vật
                    if (this.Player.GoodsDataList != null)
                    {
                        lock (this.Player.GoodsDataList)
                        {
                            foreach (GoodsData goods in this.Player.GoodsDataList)
                            {
                                if (goods != null && goods.GCount > 0 && !processedBookDbIds.Contains(goods.Id))
                                {
                                    ItemData itemTemplate = ItemManager.GetItemTemplate(goods.GoodsID);
                                    if (itemTemplate != null && itemTemplate.BookProperty != null)
                                    {
                                        // Kiểm tra xem mật tịch này có chứa kỹ năng đang xét không
                                        if (itemTemplate.BookProperty.SkillID1 == id ||
                                            itemTemplate.BookProperty.SkillID2 == id ||
                                            itemTemplate.BookProperty.SkillID3 == id ||
                                            itemTemplate.BookProperty.SkillID4 == id)
                                        {
                                            if (goods.OtherParams != null && goods.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var strLvl))
                                            {
                                                int lvl = int.Parse(strLvl);
                                                int expVal = ItemManager._TotalExpBook.Where(x => x.Level <= lvl && x.BookLevel == itemTemplate.Level).Sum(x => x.Exp);
                                                totalExpFromBooks += expVal;
                                                processedBookDbIds.Add(goods.Id);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 2. Kiểm tra thêm mật tịch đang đeo trên người đề phòng trường hợp trang bị đang đeo không nằm trong GoodsDataList
                    KPlayerEquipBody equipBody = this.Player.GetPlayEquipBody();
                    if (equipBody != null)
                    {
                        // Kiểm tra mật tịch chính đang đeo
                        if (equipBody.EquipBody.TryGetValue(KE_EQUIP_POSITION.emEQUIPPOS_BOOK, out KItem mainBook))
                        {
                            if (mainBook != null && mainBook.SkillsFromBook != null && mainBook.SkillsFromBook.Contains(id) && mainBook._GoodDatas != null && !processedBookDbIds.Contains(mainBook._GoodDatas.Id))
                            {
                                if (mainBook._GoodDatas.OtherParams != null && mainBook._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var strLvl))
                                {
                                    int lvl = int.Parse(strLvl);
                                    ItemData itemTemplate = ItemManager.GetItemTemplate(mainBook._GoodDatas.GoodsID);
                                    if (itemTemplate != null)
                                    {
                                        int expVal = ItemManager._TotalExpBook.Where(x => x.Level <= lvl && x.BookLevel == itemTemplate.Level).Sum(x => x.Exp);
                                        totalExpFromBooks += expVal;
                                        processedBookDbIds.Add(mainBook._GoodDatas.Id);
                                    }
                                }
                            }
                        }
                        // Kiểm tra mật tịch phụ đang đeo
                        if (equipBody.PreventiveEquipBody.TryGetValue(KE_EQUIP_POSITION_SUB.emEQUIPPOS_BOOK, out KItem subBook))
                        {
                            if (subBook != null && subBook.SkillsFromBook != null && subBook.SkillsFromBook.Contains(id) && subBook._GoodDatas != null && !processedBookDbIds.Contains(subBook._GoodDatas.Id))
                            {
                                if (subBook._GoodDatas.OtherParams != null && subBook._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var strLvl))
                                {
                                    int lvl = int.Parse(strLvl);
                                    ItemData itemTemplate = ItemManager.GetItemTemplate(subBook._GoodDatas.GoodsID);
                                    if (itemTemplate != null)
                                    {
                                        int expVal = ItemManager._TotalExpBook.Where(x => x.Level <= lvl && x.BookLevel == itemTemplate.Level).Sum(x => x.Exp);
                                        totalExpFromBooks += expVal;
                                        processedBookDbIds.Add(subBook._GoodDatas.Id);
                                    }
                                }
                            }
                        }
                    }

                    // CHỈ KHÔI PHỤC NẾU EXP HIỆN TẠI TRONG DB BỊ THẤP HƠN TỔNG EXP CỦA CÁC QUYỂN MẬT TỊCH ĐANG SỞ HỮU
                    if (skillRef.Exp < totalExpFromBooks)
                    {
                        skillRef.Exp = totalExpFromBooks;
                        skillRef.AddedLevel = 1;
                        int exp = skillRef.Exp;
                        while (skillRef.AddedLevel < skillData.MaxSkillLevel)
                        {
                            PropertyDictionary skillPd = skillData.Properties[skillRef.AddedLevel];
                            if (!skillPd.ContainsKey((int) MAGIC_ATTRIB.magic_skill_skillexp_v))
                            {
                                break;
                            }
                            int thisLevelMaxExp = skillPd.Get<KMagicAttrib>((int) MAGIC_ATTRIB.magic_skill_skillexp_v).nValue[0];
                            if (exp >= thisLevelMaxExp)
                            {
                                exp -= thisLevelMaxExp;
                                skillRef.AddedLevel++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        skillRef.Exp = exp;
                        KT_TCPHandler.UpdateSkillInfoFromDB(this.Player, id, skillRef.AddedLevel, this.GetSkillLastUsedTick(id), this.GetSkillCooldown(id), skillRef.Exp);
                        this.Player.RefreshSkillPoints();
                        this.ExportSkillTree();
                        this.ProcessEnchantSkills();
                    }
                }
            }
        }

        /// <summary>
        /// Quét toàn bộ kỹ năng mật tịch đã học và tính toán lại cấp độ (tự động sửa lỗi mất level)
        /// </summary>
        public void RecomputeAllBookSkillsLevel()
        {
            List<int> bookSkillIds = new List<int>();
            lock (this.listStudiedSkills)
            {
                foreach (var skillRef in this.listStudiedSkills.Values)
                {
                    SkillDataEx skillData = KSkill.GetSkillData(skillRef.SkillID);
                    if (skillData != null && skillData.IsExpSkill)
                    {
                        bookSkillIds.Add(skillRef.SkillID);
                    }
                }
            }

            foreach (int skillId in bookSkillIds)
            {
                this.RecomputeBookSkillLevel(skillId);
            }
        }

        public void AddBookSkill(int idSkill, int itemLevel)
        {
            this.BookSkillIDs.Add((idSkill, itemLevel));
        }
    }

    public struct SkillLevelBonus
    {
        public int id;
        public int bonus;

        public SkillLevelBonus(int id, int bonus)
        {
            this.id = id;
            this.bonus = bonus;
        }
    }
}
