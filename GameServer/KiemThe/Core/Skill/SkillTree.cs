using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System.Collections.Generic;
using System.Linq;
using System;
using GameServer.KiemThe.Utilities;
using GameServer.KiemThe.Logic.Manager.Pet;

namespace GameServer.KiemThe.Entities
{
    /// <summary>
    /// Danh sách kỹ năng của nhân vật
    /// </summary>
    public partial class SkillTree
    {
        /// <summary>
        /// Đối tượng chủ nhân
        /// </summary>
        public KPlayer Player { get; private set; }

        /// <summary>
        /// Danh sách kỹ năng của nhân vật
        /// </summary>
        /// <param name="player"></param>
        public SkillTree(KPlayer player)
        {
            this.Player = player;

            this.BuildSkillTree();

            // Đồng bộ các kỹ năng môn phái mới thêm trong cấu hình cho cả
            // những nhân vật đã gia nhập môn phái từ trước.
            this.AddAvailableFactionSkills();
        }

        private void AddAvailableFactionSkills()
        {
            List<int> skills = KFaction.GetFactionSkills(this.Player.m_cPlayerFaction.GetFactionId());

            foreach (int id in skills)
            {
                if (!this.listStudiedSkills.TryGetValue(id, out SkillLevelRef skillRef))
                {
                    SkillDataEx skillData = KSkill.GetSkillData(id);
                    if (skillData != null &&
                        skillData.RequireLevel <= ServerConfig.Instance.LimitLevel &&
                        !this.listStudiedSkills.ContainsKey(id))
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
        }


        /// <summary>
        /// Xây SkillTree dựa vào dữ liệu nhân vật
        /// </summary>
        private void BuildSkillTree()
        {
            List<int> skills = KFaction.GetSkillsOutFaction(this.Player.m_cPlayerFaction.GetFactionId());

            this.listStudiedSkills.Clear();

            if (this.Player.SkillDataList != null)
            {
                foreach (SkillData dbSkillData in this.Player.SkillDataList)
                {
                    // xem xét xem sv có cấm mật tịch hay pet ko, nếu có thì ko cần thêm skill của 2 cái này
                    if (!string.IsNullOrEmpty(ServerConfig.Instance.LimitEquipPos))
                    {
                        var listPos = ServerConfig.Instance.LimitEquipPos.Split(';');

                        if (listPos.Contains("20") && PetManager.InfoPets.PetSkills.Where(x => x.ID == dbSkillData.SkillID).FirstOrDefault() != null)
                        {
                            continue;
                        }

                        if (listPos.Contains("12") || listPos.Contains("112"))
                        {
                            SkillDataEx skillData = KSkill.GetSkillData(dbSkillData.SkillID);

                            if (skillData.IsExpSkill)
                            {
                                continue;
                            }    
                        }    
                    }
                    if (!skills.Contains(dbSkillData.SkillID))
                    {
                        SkillDataEx skillData = KSkill.GetSkillData(dbSkillData.SkillID);
                        if (skillData != null)
                        {
                            SkillLevelRef levelRef = new SkillLevelRef()
                            {
                                AddedLevel = dbSkillData.SkillLevel,
                                Data = skillData,
                                BonusLevel = this._AllSkillBonusLevel,
                                CanStudy = true,
                                Exp = dbSkillData.Exp,
                            };
                            this.listStudiedSkills[skillData.ID] = levelRef;
                        }

                        /// Phục hồi dữ liệu Cooldown lấy từ DB
                        this.AddSkillCooldown(dbSkillData.SkillID, dbSkillData.LastUsedTick, dbSkillData.CooldownTick);

                    }    
                    
                }
            }
        }

        /// <summary>
        /// Đồng bộ dữ liệu SkillTree vào DB
        /// </summary>
        public void ExportSkillTree()
        {
            int factionID = this.Player.m_cPlayerFaction.GetFactionId();
            List<int> skills = KFaction.GetSkillsOutFaction(factionID);

            List<SkillData> dbSkillDataList = new List<SkillData>();
            foreach (SkillLevelRef levelRef in this.listStudiedSkills.Values)
            {
                if (!skills.Contains(levelRef.SkillID))
                {
                    SkillData dbSkillData = new SkillData()
                    {
                        SkillID = levelRef.SkillID,
                        SkillLevel = levelRef.AddedLevel,
                        BonusLevel = this.Player.Skills.AllSkillBonusLevel + this.Player.Skills.GetBonusByID(levelRef.SkillID),
                        CanStudy = levelRef.CanStudy,
                        LastUsedTick = this.GetSkillLastUsedTick(levelRef.SkillID),
                        CooldownTick = this.GetSkillCooldown(levelRef.SkillID),
                        Exp = levelRef.Exp,
                    };
                    dbSkillDataList.Add(dbSkillData);
                } 
            }    

            this.Player.SkillDataList = dbSkillDataList;

            /// Gửi tín hiệu thay đổi danh sách kỹ năng về Client
            KT_TCPHandler.SendRenewSkillList(this.Player);
        }
    }
}
