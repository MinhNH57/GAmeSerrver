using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using static GameServer.KiemThe.Logic.KTSkillManager;
using GameServer.KiemThe.GameEvents.BossUnicon;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Server;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Thực thi LOGIC tính toán Damage
    /// </summary>
    public class AlgorithmProperty
    {
        /// <summary>
        /// ID kỹ năng Linh Hư Tụ Khí của Tiêu Dao Kiếm.
        /// Param1/Param2 trong SkillData lần lượt là điểm né đòn tầm xa
        /// ở cấp 1 và cấp tối đa.
        /// </summary>
        private const int LinhHuTuKhiSkillID = 17004;

        /// <summary>
        /// Bát Quái Kính Pháp của Tiêu Dao Chưởng.
        /// Param1/Param2: tỷ lệ phản sát thương nội công cấp 1/cấp tối đa.
        /// Param3/Param4: tỷ lệ gây suy yếu khi bị đánh cấp 1/cấp tối đa.
        /// Param5: thời gian suy yếu theo frame (18 frame = 1 giây).
        /// </summary>
        private const int BatQuaiKinhPhapSkillID = 17015;

        private static int GetScaledSkillParam(SkillLevelRef skill, int firstValue, int lastValue)
        {
            if (skill == null || skill.Data == null)
            {
                return 0;
            }

            int maxLevel = Math.Max(1, skill.Data.MaxSkillLevel);
            int level = Math.Min(Math.Max(1, skill.Level), maxLevel);
            if (maxLevel <= 1)
            {
                return firstValue;
            }

            return firstValue + (lastValue - firstValue) * (level - 1) / (maxLevel - 1);
        }

        private static int GetSkillPropertyValue(PropertyDictionary properties, MAGIC_ATTRIB attribute)
        {
            if (properties == null || !properties.ContainsKey((int)attribute))
            {
                return 0;
            }

            KMagicAttrib value = properties.Get<KMagicAttrib>((int)attribute);
            return value?.nValue != null && value.nValue.Length > 0 ? value.nValue[0] : 0;
        }

        private static void ApplyBatQuaiKinhPhapEffects(GameObject attacker, GameObject target, SkillLevelRef attackSkill, int receivedDamage)
        {
            if (attacker == null || target == null || attackSkill == null || receivedDamage <= 0)
            {
                return;
            }

            BuffDataEx buff = target.Buffs.GetBuff(BatQuaiKinhPhapSkillID);
            if (buff == null || buff.Skill == null || buff.Skill.Data == null)
            {
                return;
            }

            int[] parameters = buff.Skill.Data.Params;
            if (parameters == null || parameters.Length < 5)
            {
                return;
            }

            // Chỉ phản sát thương do kỹ năng nội công gây ra.
            if (!attackSkill.Data.IsPhysical && !attacker.IsDead())
            {
                int reflectPercent = GetScaledSkillParam(buff.Skill, parameters[0], parameters[1]);
                int reflectDamage = receivedDamage * reflectPercent / 100;
                if (reflectDamage > 0)
                {
                    int oldLife = attacker.m_CurrentLife;
                    AlgorithmProperty.CalcDamage(target, attacker, reflectDamage, reflectDamage,
                        DAMAGE_TYPE.damage_return, false, null, false, 100, true);

                    int actualReflectDamage = Math.Max(0, oldLife - attacker.m_CurrentLife);
                    if (actualReflectDamage > 0)
                    {
                        AlgorithmProperty.SyncDamage(target, attacker, actualReflectDamage);
                    }

                    if (attacker.m_CurrentLife <= 0)
                    {
                        attacker.m_CurrentLife = 0;
                        attacker.DoDeath(target);
                    }
                }
            }

            // Tài liệu không quy định thời gian suy yếu; Param5 giữ thời gian cấu hình,
            // hiện đặt 18 frame tương đương 1 giây.
            if (!attacker.IsDead())
            {
                int weakChance = GetScaledSkillParam(buff.Skill, parameters[2], parameters[3]);
                if (weakChance > 0 && KTGlobal.GetRandomNumber(1, 100) <= weakChance)
                {
                    int weakDuration = Math.Max(1, parameters[4]);
                    KMagicAttrib weakState = new KMagicAttrib
                    {
                        nAttribType = MAGIC_ATTRIB.state_weak_attack,
                        nValue = new int[] { 100, weakDuration, weakDuration },
                    };

                    KSpecialStateManager.OnReceiveState(attacker, weakState, KE_STATE.emSTATE_WEAK,
                        target, buff.Skill,
                        new UnityEngine.Vector2((float)target.CurrentPos.X, (float)target.CurrentPos.Y),
                        buff.Properties, false);
                }
            }
        }

        private static int GetLinhHuTuKhiRangedDefense(GameObject target)
        {
            if (!(target is KPlayer player))
            {
                return 0;
            }

            SkillLevelRef passive = player.Skills.GetSkillLevelRef(LinhHuTuKhiSkillID);
            if (passive == null || passive.Level <= 0 || passive.Data == null)
            {
                return 0;
            }

            int level = Math.Min(passive.Level, Math.Max(1, passive.Data.MaxSkillLevel));
            int firstValue = passive.Data.Params[0];
            int lastValue = passive.Data.Params[1];
            if (passive.Data.MaxSkillLevel <= 1)
            {
                return firstValue;
            }

            return firstValue
                + (lastValue - firstValue) * (level - 1)
                / (passive.Data.MaxSkillLevel - 1);
        }

        // Bảng hằng số hóa giải quan hàm
        private static readonly int[] ChopAbsorbDamagesPercent = new int[]
        {
            0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50
        };

        /// <summary>
        /// [DEBUG] True nếu cần trace chi tiết cách tính damage lên target này
        /// (target là người chơi nằm trong danh sách PacketMonitoringUsers.xml).
        /// <para>Check IsPlayer() để tránh trùng RoleID giữa quái và người chơi.</para>
        /// <para>Caller phải kèm điều kiện attacker.IsPlayer() để chỉ trace damage từ người chơi,
        /// tránh nhiễu log khi quái đánh target.</para>
        /// <para>Log ghi vào file PacketDebug_YYYYMMDD.log với prefix [DmgTrace].</para>
        /// </summary>
        private static bool ShouldTraceDamage(GameObject target)
        {
            return !PacketMonitoringManager.IsEmpty && target != null && target.IsPlayer()
                && PacketMonitoringManager.IsMonitored(target.RoleID);
        }

        /// <summary>
        /// [DEBUG] Ghi 1 dòng trace damage vào PacketDebug_YYYYMMDD.log.
        /// Chỉ gọi khi ShouldTraceDamage đã trả về true (caller tự guard để tránh tốn string interpolation trên hot path).
        /// Chỉ ghi file local, không đẩy lên Loki để tránh tốn băng thông/storage khi trace dồn dập trong PvP.
        /// </summary>
        private static void TraceDamage(string message)
        {
            LogManager.WriteLog(LogTypes.PacketDebug, "[DmgTrace] " + message, bConsole: false, pushToLoki: false);
        }

        /// <summary>
        /// Tính lực tay
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rcSkill"></param>
        /// <param name="additionDamagePercent"></param>
        /// <returns></returns>
        public static int GetDamageInfo(GameObject client, SkillLevelRef rcSkill, int additionDamagePercent)
        {
            try
            {
                // Nếu Client NULL
                if (client == null)
                {
                    return 0;
                }
                // Nếu Skill NULL
                else if (rcSkill == null)
                {
                    return 0;
                }

                // Nếu không phải skill gây sát thương return 0 luôn
                if (!rcSkill.Data.IsDamageSkill)
                {
                    return 0;
                }

                // Nếu không có ProDict
                if (rcSkill.Properties == null)
                {
                    return 0;
                }

                Dictionary<MAGIC_ATTRIB, KMagicAttrib> AppendDameDic = new Dictionary<MAGIC_ATTRIB, KMagicAttrib>();

                bool IsDeadlyStrike = false;

                // % sát thương cộng thêm từ kỹ năng khác
                int nDamageAddPercent = AlgorithmProperty.GetAddSkillDamagePercent(client, rcSkill.Data.ID);
                // Cộng thêm % sát thương từ ngoài vào (có thể do xuyên suốt mục tiêu)
                nDamageAddPercent += additionDamagePercent;
                // Sát thương cộng thêm từ kỹ năng khác
                int nDamageAdd = AlgorithmProperty.GetAddSkillDamage(client, rcSkill.Data.ID);

                // Property Dictionary của kỹ năng
                PropertyDictionary skillPd = rcSkill.Properties.Clone();

                // Cộng toàn bộ thuộc tính hỗ trợ
                if (client is KPlayer)
                {
                    //Console.WriteLine("SkillPD before = " + skillPd.ToString());
                    KPlayer player = client as KPlayer;
                    PropertyDictionary enchantPd = player.Skills.GetEnchantProperties(rcSkill.SkillID);
                    if (enchantPd != null)
                    {
                        skillPd.AddProperties(enchantPd);
                    }
                    //Console.WriteLine("SkillPD after = " + skillPd.ToString());
                }

                // Check xem có phải skill dame vật lý không
                bool bIsPhysical = rcSkill.Data.IsPhysical;

                // Tỷ lệ thiệt hại ban đầu
                int nDamagePercent = 100;

                // Phát huy lực tấn công kỹ năng
                int m_nSkillSelfDamagePTrim = client.m_nSkillSelfDamagePTrim;

                // Phát huy lực tấn công cơ bản :  Lấy từ các skill bị động + được apply vào nhân vật
                int m_nSkillDamagePTrim = client.m_nSkillDamagePTrim;

                if (client.IsPlayer())
                {
                    m_nSkillSelfDamagePTrim += ((KPlayer)client).Skills.GetAppendDamageToSkill(rcSkill.SkillID);
                }

                int nSkillDamageT = PropertyDictionaryManager.GetSkillDamageP(skillPd) * 10 + PropertyDictionaryManager.GetSkillDamageP(skillPd) * m_nSkillSelfDamagePTrim * 10 / 100;  // Dame từ Skill
                var m_pSkillDamagePTrim = client.m_pSkillDamagePTrim;
                var m_pSkillSelfDamagePTrim = client.m_pSkillSelfDamagePTrim;
                int nAppendDamageT = (m_pSkillDamagePTrim + PropertyDictionaryManager.GetAppendDamageP(skillPd)) * 10 + (m_pSkillDamagePTrim + PropertyDictionaryManager.GetAppendDamageP(skillPd)) * m_nSkillDamagePTrim * 10 / 100;
                if (!rcSkill.Data.IsPhysical && rcSkill.Data.ElementalSeries == 5)
                    nAppendDamageT = CoMoEffects.Scale(nAppendDamageT, CoMoEffects.PassivePercent(client, 16022));   //
                LogManager.WriteLog(LogTypes.Skill, $"Player {client.RoleName} damage = {nAppendDamageT} place 2");
                int nSkillDamageTrimP = (m_pSkillSelfDamagePTrim + PropertyDictionaryManager.GetSkillDamageP(skillPd)) * m_nSkillSelfDamagePTrim / 100;
                int nAngerATK = 0;
                int nAngerPoiTime = 0;
                // Nếu thằng đánh bị suy yếu thì dame sẽ bị giảm
                if (client.HaveState(KE_STATE.emSTATE_WEAK))
                {
                    int nNewPercent = nDamagePercent * KTGlobal.WeakDamagePercent / 100;
                    nDamagePercent = nNewPercent;
                }

                // 1. Nếu skill có sử dụng tỉ lệ chính xác
                if (rcSkill.Data.IsUseAR)
                {
                    // Tạo mới KMagicAttribute
                    KMagicAttrib attrib = new KMagicAttrib();
                    // Loại là tỷ lệ đánh trúng
                    attrib.nAttribType = MAGIC_ATTRIB.magic_damage_append_hitrate;

                    // Chính xác ban đầu
                    int hitValue = 0;
                    // Nếu kỹ năng có dòng % tỷ lệ đánh trúng
                    if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_attackrating_p))
                    {
                        // Chính xác cơ bản
                        int m_AttackRating = client.m_AttackRating;
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating + m_AttackRating * PropertyDictionaryManager.GetHiteRateP(skillPd) / 100;
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }
                    // Nếu kỹ năng có dòng tăng điểm đánh trúng
                    else if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_attackrating_v))
                    {
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating + PropertyDictionaryManager.GetHiteRateV(skillPd);
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }
                    // Nếu kỹ năng không có chính xác
                    else
                    {
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating;
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }

                    // Bỏ qua né tránh đối thủ
                    int m_CurrentIgnoreDefense = client.m_CurrentIgnoreDefense;
                    // Tỷ lệ % bỏ qua né tránh đối thủ
                    int m_CurrentIgnoreDefensePercent = client.m_CurrentIgnoreDefensePercent;

                    // Lưu lại vào KMagicAttribute
                    attrib.nValue[1] = PropertyDictionaryManager.GetIgnoreDefenseV(skillPd) + m_CurrentIgnoreDefense;
                    attrib.nValue[2] = PropertyDictionaryManager.GetIgnoreDefenseP(skillPd) + m_CurrentIgnoreDefensePercent;

                    // Thêm vào ProDict
                    AppendDameDic.Add(attrib.nAttribType, attrib);
                }

                if (rcSkill.Data.IsDamageSkill)
                {
                    // 1. TÍnh dame nộ nếu skill support
                    int AngerDamage = PropertyDictionaryManager.GetAngerDamage(skillPd);

                    bool bIsAnger = false;

                    if (AngerDamage > 0)
                    {
                        bIsAnger = true;
                        int nBaseAngerATK = KPlayerSetting.GetLevelBaseAngerATK(client.m_Level);

                        nAngerATK = (int)((AngerDamage / 100.0) * nBaseAngerATK) * nDamagePercent / 100 * nSkillDamageT / 1000;

                        nAngerPoiTime = KTGlobal.AngerTime;
                    }

                    // 2. Sát thương ngũ hành

                    if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_seriesdamage_r)) // Nếu skill có chứa sát thương ngũ hành
                    {
                        //FILL CHỈ SỐ VÀO ĐÂY
                        // Dame khắc hệ của nhân vật
                        int m_CurrentSeriesConquar = client.m_CurrentSeriesConquar;  // SIMBOY : magic_seriesconquar_r
                        int m_CurrentSeriesEnhance = client.m_CurrentSeriesEnhance;  // SIMBOY : magic_seriesenhance_r  Tấn công người chơi khắc hệ, trị tương khắc của kỹ năng

                        int cSrcSeriesDamage = PropertyDictionaryManager.GetSeriesDamage(skillPd);

                        KMagicAttrib cSeriesDamage = new KMagicAttrib();

                        cSeriesDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_series;

                        cSeriesDamage.nValue[0] = cSrcSeriesDamage + m_CurrentSeriesConquar;
                        cSeriesDamage.nValue[1] = m_CurrentSeriesEnhance;

                        AppendDameDic.Add(cSeriesDamage.nAttribType, cSeriesDamage);
                    }

                    // 3. Tính toán sát thương chí mạng
                    double m_CurrentFatallyStrike = (1 + client.m_CurrentFatallyStrike / 100f) * client.m_CurrentDeadlyStrike; // SYMBOY : magic_fatallystrikeenhance_p : Xác suất tấn công chí mạng

                    int PercenActive = KTGlobal.GetRandomNumber(0, 99);

                    double PERCENT = (m_CurrentFatallyStrike / KTGlobal.DeadlyStrikeBaseRate) * 100;

                    if (PERCENT > 0 && PERCENT > PercenActive)
                    {
                        KMagicAttrib cFatallyStrike = new KMagicAttrib();
                        cFatallyStrike.nAttribType = MAGIC_ATTRIB.magic_damage_append_fatallystrike;
                        cFatallyStrike.nValue[0] = 1;
                        IsDeadlyStrike = true;
                        AppendDameDic.Add(cFatallyStrike.nAttribType, cFatallyStrike);
                    }

                    // 4. Tính toán dame vật lý
                    {
                        KMagicAttrib cPhysicsDamage = new KMagicAttrib();
                        cPhysicsDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_physics;

                        int nMinDamage = nDamageAdd;
                        int nMaxDamage = nDamageAdd;

                        if (bIsAnger) // NẾU CÓ DAME NỘ
                        {
                            // NGŨ HÀNH CỦA NHÂN VẬT
                            // FILL CHỈ SỐ VÀO ĐÂY
                            KE_SERIES_TYPE m_Series = client.m_Series;

                            if (nAngerATK > 0 && (m_Series == KE_SERIES_TYPE.series_metal || m_Series == KE_SERIES_TYPE.series_none))
                            {
                                cPhysicsDamage.nValue[0] += nAngerATK;
                                cPhysicsDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            // Nếu là sát thương ngoại công
                            if (bIsPhysical)
                            {
                                // Vật công ngoại Min
                                nMinDamage = client.m_PhysicsDamage.nValue[0];
                                // Vật công ngoại Max
                                nMaxDamage = client.m_PhysicsDamage.nValue[2];

                                // Đánh dấu có phải người chơi không
                                bool IsPlayer = client.IsPlayer();

                                // Sát thương cộng thêm từ ngoài nếu là người chơi
                                if (IsPlayer)
                                {
                                    int nEnhance = AlgorithmProperty.GetPhysicsEnhance((KPlayer)client);
                                    nMinDamage += nMinDamage * nEnhance / 100;
                                    nMaxDamage += nMaxDamage * nEnhance / 100;
                                }

                                // Sát thương cộng thêm %
                                nMinDamage += (client.m_PhysicsDamage.nValue[0] * client.m_CurrentPhysicsDamageEnhanceP / 100);
                                nMaxDamage += (client.m_PhysicsDamage.nValue[2] * client.m_CurrentPhysicsDamageEnhanceP / 100);

                                // Sát thương cơ bản của vũ khí
                                nMinDamage += (client.m_PhysicPhysic.nValue[0] + client.m_nWeaponBaseDamageTrim);
                                nMaxDamage += (client.m_PhysicPhysic.nValue[2] + client.m_nWeaponBaseDamageTrim);

                                // Sát thương cộng thêm từ kỹ năng khác
                                nMinDamage = nMinDamage * nAppendDamageT / 1000;
                                nMaxDamage = nMaxDamage * nAppendDamageT / 1000;

                                // Biến tạm
                                int nTemp = 0;

                                // Kỹ năng hỗ trợ
                                int GetPhysicsDamageEnhanceP = PropertyDictionaryManager.GetPhysicsDamageEnhanceP(skillPd);
                                int GetPhysicsDamageEnhanceV = PropertyDictionaryManager.GetPhysicsDamageEnhanceV(skillPd);

                                // % Sát thương từ các kỹ năng hỗ trợ
                                if (GetPhysicsDamageEnhanceP > 0)
                                {
                                    nTemp = (client.m_PhysicsDamage.nValue[0] * GetPhysicsDamageEnhanceP / 100);
                                }
                                // Sát thương từ kỹ năng hỗ trợ
                                else if (GetPhysicsDamageEnhanceV > 0)
                                {
                                    nTemp = client.m_PhysicsDamage.nValue[2] + GetPhysicsDamageEnhanceV;
                                }

                                // Tăng thêm ngoại công do cái gì khác
                                int nTempMin = nTemp + PropertyDictionaryManager.GetPhysicsTrimDamage(skillPd).nValue[0];
                                int nTempMax = nTemp + PropertyDictionaryManager.GetPhysicsTrimDamage(skillPd).nValue[2];

                                // % sát thương cộng thêm
                                nTempMin += nTempMin * nDamageAddPercent / 100;
                                nTempMax += nTempMax * nDamageAddPercent / 100;
                                nTempMin *= nSkillDamageT / 1000;
                                nTempMax *= nSkillDamageT / 1000;

                                nMinDamage += nTempMin;
                                nMaxDamage += nTempMax;

                                // Kết quả cuối cùng
                                nMinDamage += (client.m_AddPhysicsDamage * nAppendDamageT / 1000);
                                nMaxDamage += (client.m_AddPhysicsDamage * nAppendDamageT / 1000);
                            }
                            // Nếu là sát thương nội công
                            else
                            {
                                // Vật công nội Min
                                nMinDamage = client.m_MagicDamage.nValue[0];
                                // Vật công nội Max
                                nMaxDamage = client.m_MagicDamage.nValue[2];

                                // % vật công nội từ vũ khí
                                nMinDamage += (client.m_MagicDamage.nValue[0] * client.m_CurrentMagicPhysicsEnhanceP / 100);
                                nMaxDamage += (client.m_MagicDamage.nValue[2] * client.m_CurrentMagicPhysicsEnhanceP / 100);

                                // % nội công riêng được khai báo trực tiếp trên kỹ năng đang thi triển.
                                int skillMagicEnhanceP = GetSkillPropertyValue(skillPd,
                                    MAGIC_ATTRIB.magic_addphysicsmagic_p);
                                nMinDamage += client.m_MagicDamage.nValue[0] * skillMagicEnhanceP / 100;
                                nMaxDamage += client.m_MagicDamage.nValue[2] * skillMagicEnhanceP / 100;

                                // Sát thương cơ bản của vũ khí
                                nMinDamage += (client.m_PhysicsMagic.nValue[0] + client.m_nWeaponBaseDamageTrim);
                                nMaxDamage += (client.m_PhysicsMagic.nValue[2] + client.m_nWeaponBaseDamageTrim);

                                // Sát thương cộng thêm từ kỹ năng hỗ trợ
                                nMinDamage = nMinDamage * nAppendDamageT / 1000;
                                nMaxDamage = nMaxDamage * nAppendDamageT / 1000;

                                // Biến tạm
                                int nTemp = 0;

                                // Kỹ năng hỗ trợ
                                int GetPhysicsDamageEnhanceP = PropertyDictionaryManager.GetPhysicsDamageEnhanceP(skillPd);
                                int GetPhysicsDamageEnhanceV = PropertyDictionaryManager.GetPhysicsDamageEnhanceV(skillPd);

                                // % Sát thương từ các kỹ năng hỗ trợ
                                if (GetPhysicsDamageEnhanceP > 0)
                                {
                                    nTemp = (client.m_MagicDamage.nValue[0] * GetPhysicsDamageEnhanceP / 100);
                                }
                                // Sát thương từ kỹ năng hỗ trợ
                                else if (GetPhysicsDamageEnhanceV > 0)
                                {
                                    nTemp = client.m_MagicDamage.nValue[0] + GetPhysicsDamageEnhanceV;
                                }

                                // Tăng thêm nội công do cái gì khác
                                int nTempMin = nTemp + PropertyDictionaryManager.GetMagicTrimDamage(skillPd).nValue[0];
                                int nTempMax = nTemp + PropertyDictionaryManager.GetMagicTrimDamage(skillPd).nValue[2];

                                // % sát thương cộng thêm
                                nTempMin += nTempMin * nDamageAddPercent / 100;
                                nTempMax += nTempMax * nDamageAddPercent / 100;
                                nTempMin *= nSkillDamageT / 1000;
                                nTempMax *= nSkillDamageT / 1000;

                                nMinDamage += nTempMin;
                                nMaxDamage += nTempMax;

                                // Kết quả cuối cùng
                                nMinDamage += (client.m_MagicPhysicsDamage.nValue[0] * nAppendDamageT / 1000);
                                nMaxDamage += (client.m_MagicPhysicsDamage.nValue[2] * nAppendDamageT / 1000);
                            }

                            // Nhận thêm sát thương bởi dame phép
                            if (client.m_nAttackAddedByMana > 0)
                            {
                                nMinDamage += client.m_CurrentMana * client.m_nAttackAddedByMana / 100;
                                nMaxDamage += client.m_CurrentMana * client.m_nAttackAddedByMana / 100;
                            }

                            cPhysicsDamage.nValue[0] = nMinDamage * nDamagePercent / 100;
                            cPhysicsDamage.nValue[2] = nMaxDamage * nDamagePercent / 100;
                        }

                        AppendDameDic.Add(cPhysicsDamage.nAttribType, cPhysicsDamage);
                    }

                    int nSkillPercent = nDamageAddPercent + 100; //% sức mạnh kỹ năng

                    // 5. Tính toán dame băng công
                    {
                        KMagicAttrib cColdDamage = new KMagicAttrib();
                        cColdDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_cold;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_water)
                            {
                                cColdDamage.nValue[0] += nAngerATK;
                                cColdDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cColdDamage = CalcAttackDamage(PropertyDictionaryManager.GetColdDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_cold]);

                            cColdDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_cold;
                        }

                        AppendDameDic.Add(cColdDamage.nAttribType, cColdDamage);
                    }
                    // 6. Tính toán dame hỏa công
                    {
                        KMagicAttrib cFireDamage = new KMagicAttrib();
                        cFireDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_fire;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_fire)
                            {
                                cFireDamage.nValue[0] += nAngerATK;
                                cFireDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cFireDamage = CalcAttackDamage(PropertyDictionaryManager.GetFireDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_fire]);

                            cFireDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_fire;
                        }

                        AppendDameDic.Add(cFireDamage.nAttribType, cFireDamage);
                    }
                    // 7. Tính toán dane lôi công
                    {
                        KMagicAttrib cLightDamage = new KMagicAttrib();
                        cLightDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_light;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_fire)
                            {
                                cLightDamage.nValue[0] += nAngerATK;
                                cLightDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cLightDamage = CalcAttackDamage(PropertyDictionaryManager.GetLightDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_light]);

                            cLightDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_light;
                        }

                        AppendDameDic.Add(cLightDamage.nAttribType, cLightDamage);
                    }

                    // 8. Tính toán dame độc
                    {
                        KMagicAttrib cPoisonDamage = new KMagicAttrib();
                        cPoisonDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                        // Nếu là sát thương nộ
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_wood)
                            {
                                KMagicAttrib angermagic = new KMagicAttrib();
                                angermagic.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                                angermagic.nValue[0] = nAngerATK * nSkillDamageT / 1000;
                                angermagic.nValue[1] = nAngerPoiTime;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, angermagic); //
                            }
                        }
                        else
                        {
                            cPoisonDamage.nValue[0] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[0] * nSkillPercent / 100 * nSkillDamageT / 1000 * nDamagePercent / 100;
                            cPoisonDamage.nValue[1] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[1];
                            cPoisonDamage.nValue[2] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[2] * nSkillPercent / 100 * nSkillDamageT / 1000 * nDamagePercent / 100;
                            if (cPoisonDamage.nValue[2] == 0)
                            {
                                cPoisonDamage.nValue[2] = cPoisonDamage.nValue[0] * cPoisonDamage.nValue[1];
                            }

                            KMagicAttrib magic = new KMagicAttrib();
                            magic.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                            if (bIsPhysical)// Nếu là skill vật lý thif tính độc ngoại công
                            {
                                // Tổng sát thương độc ngoại công
                                magic.nAttribType = client.m_CurrentPoisonDamage.nAttribType;
                                magic.nValue[0] = client.m_CurrentPoisonDamage.nValue[0] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                magic.nValue[1] = client.m_CurrentPoisonDamage.nValue[1];
                                magic.nValue[2] = client.m_CurrentPoisonDamage.nValue[2] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, magic);
                            }
                            else
                            {
                                // TỔng sát thương độc nội công
                                magic.nAttribType = client.m_MagicPoisonDamage.nAttribType;
                                magic.nValue[0] = client.m_MagicPoisonDamage.nValue[0] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                magic.nValue[1] = client.m_MagicPoisonDamage.nValue[1];
                                magic.nValue[2] = client.m_MagicPoisonDamage.nValue[2] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, magic);
                            }
                        }

                        // Nếu mà mà có dame độc
                        if (cPoisonDamage.nValue[0] > 0)
                        {
                            cPoisonDamage.nValue[1] += cPoisonDamage.nValue[1] * client.m_nPoisonTimeEnhanceP / 100;
                            if (cPoisonDamage.nValue[1] <= 0)
                            {
                                cPoisonDamage.nValue[0] = 0;
                                cPoisonDamage.nValue[1] = 0;
                                cPoisonDamage.nValue[2] = 0;
                            }
                        }

                        AppendDameDic.Add(cPoisonDamage.nAttribType, cPoisonDamage);
                    }
                    // 9. TÍnh toán sát thương PHÉP
                    if (!bIsAnger)
                    {
                        KMagicAttrib cMagicDamage = new KMagicAttrib();
                        cMagicDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_magic;

                        cMagicDamage = CalcAttackDamage(PropertyDictionaryManager.GetMagicDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_magic]);

                        AppendDameDic.Add(cMagicDamage.nAttribType, cMagicDamage);
                    }
                    // 10.Hút hít máu
                    {
                        KMagicAttrib cStealLife = new KMagicAttrib();
                        cStealLife.nAttribType = MAGIC_ATTRIB.magic_damage_append_steallife;

                        cStealLife.nValue[0] = PropertyDictionaryManager.GetLifeStealP(skillPd).nValue[0] + client.m_CurrentLifeStolen;
                        cStealLife.nValue[1] = PropertyDictionaryManager.GetLifeStealP(skillPd).nValue[1] + client.m_nStealLifeRate;
                        cStealLife.nValue[2] = 0;

                        AppendDameDic.Add(cStealLife.nAttribType, cStealLife);
                    }

                    // 11. Hút mana
                    {
                        KMagicAttrib cStealMana = new KMagicAttrib();
                        cStealMana.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealmana;
                        cStealMana.nValue[0] = PropertyDictionaryManager.GetManaStealP(skillPd).nValue[0] + client.m_CurrentManaStolen;
                        cStealMana.nValue[1] = PropertyDictionaryManager.GetManaStealP(skillPd).nValue[1] + client.m_nStealManaRate;
                        cStealMana.nValue[2] = 0;
                        AppendDameDic.Add(cStealMana.nAttribType, cStealMana);
                    }

                    // 12. Hút thể lực
                    {
                        KMagicAttrib cStealStamina = new KMagicAttrib();
                        cStealStamina.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealstamina;
                        cStealStamina.nValue[0] = PropertyDictionaryManager.GetStealStaminaP(skillPd).nValue[0] + client.m_CurrentStaminaStolen;
                        cStealStamina.nValue[1] = PropertyDictionaryManager.GetStealStaminaP(skillPd).nValue[1] + client.m_nStealStaminaRate;
                        cStealStamina.nValue[2] = 0;
                        AppendDameDic.Add(cStealStamina.nAttribType, cStealStamina);
                    }
                }

                // 13. Trạng thái hút
                {
                    KMagicAttrib cStealState = new KMagicAttrib();
                    cStealState.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealstate;
                    cStealState.nValue[0] = PropertyDictionaryManager.GetStealState(skillPd).nValue[0];
                    cStealState.nValue[1] = PropertyDictionaryManager.GetStealState(skillPd).nValue[1];
                    cStealState.nValue[2] = PropertyDictionaryManager.GetStealState(skillPd).nValue[2];

                    AppendDameDic.Add(cStealState.nAttribType, cStealState);
                }

                // 14.Apply toàn bộ trạng thái ngũ hành vào DIC
                List<KMagicAttrib> TotalEffectOfSkill = PropertyDictionaryManager.GetAllSkillEffectAppend(skillPd);

                foreach (KMagicAttrib _magic in TotalEffectOfSkill)
                {
                    KE_STATE _State = AlgorithmProperty.ConvertSkillEffectToState(_magic.nAttribType);
                    KMagicAttrib Final = AlgorithmProperty.AppendAttackAttrib(_magic, client, client.m_state[(int)_State], rcSkill);
                    AppendDameDic.Add(Final.nAttribType, Final);
                }

                int nSeries = rcSkill.Data.ElementalSeries;
                bool bIsMelee = rcSkill.Data.IsMelee;
                int dwSkillStyle = KTGlobal.GetSkillStyleDef(rcSkill.Data.SkillStyle);

                //for (int i = (int)MAGIC_ATTRIB.magic_state_effect_begin; i < (int)MAGIC_ATTRIB.magic_end; i++)
                //{
                //    if(AppendDameDic.ContainsKey(i))
                //    {
                //    }
                //}
                //100 : Hiệu suất tấn công.

                double MinDamge = 0;
                double MaxDamge = 0;
                int pAR = 0;

                foreach (KeyValuePair<MAGIC_ATTRIB, KMagicAttrib> entry in AppendDameDic)
                {
                    switch (entry.Value.nAttribType)
                    {
                        case MAGIC_ATTRIB.magic_damage_append_hitrate:
                            {
                                if (rcSkill.Data.IsUseAR)
                                {
                                    pAR = entry.Value.nValue[0];
                                }
                            }
                            break;

                        case MAGIC_ATTRIB.magic_damage_append_physics:
                        case MAGIC_ATTRIB.magic_damage_append_cold:
                        case MAGIC_ATTRIB.magic_damage_append_fire:
                        case MAGIC_ATTRIB.magic_damage_append_light:
                        case MAGIC_ATTRIB.magic_damage_append_magic:
                            {
                                MinDamge += entry.Value.nValue[0];
                                MaxDamge += entry.Value.nValue[2];
                            }
                            break;

                        case MAGIC_ATTRIB.magic_damage_append_poison:
                            {
                                int nValue = entry.Value.nValue[0];
                                int nPoisonDamage = entry.Value.nValue[2] * (100 + client.m_nPoisonTimeEnhanceP) / (9 * 100) + nValue;
                                MinDamge += nPoisonDamage;
                                MaxDamge += nPoisonDamage;
                            }
                            break;
                            // do something with entry.Value or entry.Key
                    }
                }

                if (MinDamge > 0 && MaxDamge > 0)
                {
                    // Sát thương đầu ra
                    int nDamage = (int)((MinDamge + MaxDamge) / 2);
                    ///// Để làm chuẩn hiển thị về Client thì cho chia 4
                    //nDamage /= 4;
                    return nDamage;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Skill, ex.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Đánh mục tiêu
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        /// <param name="rcSkill"></param>
        /// <param name="additionDamagePercent"></param>
        /// <param name="skillPos"></param>
        public static bool AttackEnemy(GameObject client, GameObject enemy, SkillLevelRef rcSkill, int additionDamagePercent, UnityEngine.Vector2 skillPos, bool isSubPhrase)
        {
            // Toác
            if (client == null || enemy == null || rcSkill == null)
            {
                return false;
            }

            // [DEBUG] Trace cách tính damage nếu target nằm trong PacketMonitoringUsers.xml và bị NGƯỜI CHƠI tấn công
            bool bTrace = AlgorithmProperty.ShouldTraceDamage(enemy) && client.IsPlayer();
            if (bTrace)
            {
                TraceDamage($"AttackEnemy BEGIN: attacker={client.RoleName}[{client.RoleID}] lv={client.m_Level} series={client.m_Series} -> target={enemy.RoleName}[{enemy.RoleID}] lv={enemy.m_Level} series={enemy.m_Series} HP={enemy.m_CurrentLife}/{enemy.m_CurrentLifeMax} | skill={rcSkill.SkillID}({rcSkill.Data.Name}) physical={rcSkill.Data.IsPhysical} alwaysHit={rcSkill.Data.AlwaysHit} damageSkill={rcSkill.Data.IsDamageSkill} | additionDamagePercent={additionDamagePercent} isSubPhrase={isSubPhrase}");
            }

            // Nếu đang có Buff tân thủ
            if (enemy is KPlayer && enemy.Buffs.HasBuff(200000))
            {
                if (client is KPlayer player)
                {
                    PlayerManager.ShowNotification(player, "Người chơi đang trong trạng thái bảo vệ, không thể tấn công!");
                    if (bTrace)
                    {
                        TraceDamage($"AttackEnemy EXIT: target {enemy.RoleName}[{enemy.RoleID}] có buff bảo vệ tân thủ (200000) => bỏ qua");
                    }
                    return false;
                }
            }

            // Nếu đang trong khu an toàn
            if (enemy is KPlayer)
            {
                KPlayer player = (KPlayer)enemy;
                if (player.IsInsideSafeZone)
                {
                    if (bTrace)
                    {
                        TraceDamage($"AttackEnemy EXIT: target {enemy.RoleName}[{enemy.RoleID}] đang trong khu an toàn => bỏ qua");
                    }
                    return false;
                }
            }

            try
            {
                // Nếu không phải kỹ năng đánh luôn trúng
                if (!rcSkill.Data.AlwaysHit)
                {
                    // Nếu đối tượng đang trong trạng thái khinh công thì bỏ qua
                    if (enemy.m_eDoing == KE_NPC_DOING.do_jump)
                    {
                        // Tỷ lệ né sát thương khi khinh công
                        int flyDodgePercent = 70;
                        // Tỷ lệ random
                        int randRate = KTGlobal.GetRandomNumber(1, 100);
                        // Nếu có thể né được
                        if (randRate <= flyDodgePercent)
                        {
                            if (bTrace)
                            {
                                TraceDamage($"AttackEnemy EXIT: target đang khinh công, né thành công (roll={randRate} <= {flyDodgePercent}) => bỏ qua");
                            }
                            // Bỏ qua
                            return false;
                        }
                        if (bTrace)
                        {
                            TraceDamage($"AttackEnemy: target đang khinh công nhưng né thất bại (roll={randRate} > {flyDodgePercent}) => tiếp tục");
                        }
                    }
                }

                // Nếu cả 2 cùng là người chơi
                if (client is KPlayer attackerPlayer && enemy is KPlayer enemyPlayer)
                {
                    // Quan hàm của thằng tấn công
                    int nAttackerChopLevel = attackerPlayer.m_ChopLevel;
                    // Quan hàm của thằng bị tấn công
                    int nEnemyChopLevel = enemyPlayer.m_ChopLevel;

                    // Độ lệch
                    int nDiff = nEnemyChopLevel - nAttackerChopLevel;
                    // Nếu độ lệch dưới 0
                    if (nDiff < 0)
                    {
                        nDiff = 0;
                    }
                    // Nếu độ lệch quá 10
                    if (nDiff > 10)
                    {
                        nDiff = 10;
                    }

                    // Nếu có thể hóa giải
                    if (nDiff > 0)
                    {
                        // Tỷ lệ hóa giải
                        int nAbsorbDamagesPercent = AlgorithmProperty.ChopAbsorbDamagesPercent[nDiff];
                        // Nếu có thể hóa giải
                        if (KTGlobal.GetRandomNumber(1, 100) <= nAbsorbDamagesPercent)
                        {
                            if (bTrace)
                            {
                                TraceDamage($"AttackEnemy EXIT: hóa giải quan hàm thành công (chênh lệch={nDiff}, tỷ lệ={nAbsorbDamagesPercent}%) => bỏ qua");
                            }
                            // Thông báo hóa giải
                            AlgorithmProperty.SyncAdjust(client, enemy);
                            // Bỏ qua
                            return false;
                        }
                    }
                }

                // Nếu là kỹ năng ngoại
                if (rcSkill.Data.IsPhysical)
                {
                    // Nếu đối phương miễn nhiễm sát thương ngoại
                    if (enemy.m_ImmuneToPhysicDamage)
                    {
                        if (bTrace)
                        {
                            TraceDamage($"AttackEnemy EXIT: target miễn nhiễm sát thương ngoại => bỏ qua");
                        }
                        // Thông báo hóa giải
                        AlgorithmProperty.SyncAdjust(client, enemy);
                        // Bỏ qua
                        return false;
                    }
                }
                // Nếu là kỹ năng nội
                else
                {
                    // Nếu đối phương miễn nhiễm sát thương nội
                    if (enemy.m_ImmuneToMagicDamage)
                    {
                        if (bTrace)
                        {
                            TraceDamage($"AttackEnemy EXIT: target miễn nhiễm sát thương nội => bỏ qua");
                        }
                        // Thông báo hóa giải
                        AlgorithmProperty.SyncAdjust(client, enemy);
                        // Bỏ qua
                        return false;
                    }
                }

                // Danh sách sát thương
                Dictionary<MAGIC_ATTRIB, KMagicAttrib> AppendDameDic = new Dictionary<MAGIC_ATTRIB, KMagicAttrib>();

                bool IsDeadlyStrike = false;

                // % sát thương cộng thêm từ kỹ năng khác
                int nDamageAddPercent = AlgorithmProperty.GetAddSkillDamagePercent(client, rcSkill.Data.ID);
                // Cộng thêm % sát thương từ ngoài vào (có thể do xuyên suốt mục tiêu)
                nDamageAddPercent += additionDamagePercent;
                // Sát thương cộng thêm từ kỹ năng khác
                int nDamageAdd = AlgorithmProperty.GetAddSkillDamage(client, rcSkill.Data.ID);

                // Property Dictionary của kỹ năng
                PropertyDictionary skillPd = rcSkill.Properties.Clone();

                // Cộng toàn bộ thuộc tính hỗ trợ
                if (client is KPlayer)
                {
                    //Console.WriteLine("SkillPD before = " + skillPd.ToString());
                    KPlayer player = client as KPlayer;
                    PropertyDictionary enchantPd = player.Skills.GetEnchantProperties(rcSkill.SkillID);
                    if (enchantPd != null)
                    {
                        skillPd.AddProperties(enchantPd);
                    }
                    //Console.WriteLine("SkillPD after = " + skillPd.ToString());
                }

                // Check xem có phải skill dame vật lý không
                bool bIsPhysical = rcSkill.Data.IsPhysical;

                // Tỷ lệ thiệt hại ban đầu
                int nDamagePercent = 100;

                // Phát huy lực tấn công kỹ năng
                int m_nSkillSelfDamagePTrim = client.m_nSkillSelfDamagePTrim;

                // Phát huy lực tấn công cơ bản :  Lấy từ các skill bị động + được apply vào nhân vật
                int m_nSkillDamagePTrim = client.m_nSkillDamagePTrim;

                if (client.IsPlayer())
                {
                    m_nSkillSelfDamagePTrim = ((KPlayer)client).Skills.GetAppendDamageToSkill(rcSkill.SkillID);
                }

                int nSkillDamageT = PropertyDictionaryManager.GetSkillDamageP(skillPd) * 10 + PropertyDictionaryManager.GetSkillDamageP(skillPd) * m_nSkillSelfDamagePTrim * 10 / 100;  // Dame từ Skill
                var m_pSkillDamagePTrim = client.m_pSkillDamagePTrim;
                var m_pSkillSelfDamagePTrim = client.m_pSkillSelfDamagePTrim;
                int nAppendDamageT = (m_pSkillDamagePTrim + PropertyDictionaryManager.GetAppendDamageP(skillPd)) * 10 + (m_pSkillDamagePTrim + PropertyDictionaryManager.GetAppendDamageP(skillPd)) * m_nSkillDamagePTrim * 10 / 100;
                if (!rcSkill.Data.IsPhysical && rcSkill.Data.ElementalSeries == 5)
                    nAppendDamageT = CoMoEffects.Scale(nAppendDamageT, CoMoEffects.PassivePercent(client, 16022));   //
                LogManager.WriteLog(LogTypes.Skill, $"Player {client.RoleName} damage = {nAppendDamageT}");
                int nSkillDamageTrimP = (m_pSkillSelfDamagePTrim + PropertyDictionaryManager.GetSkillDamageP(skillPd)) * m_nSkillSelfDamagePTrim / 100;
                int nAngerATK = 0;
                int nAngerPoiTime = 0;
                // Nếu thằng đánh bị suy yếu thì dame sẽ bị giảm
                if (client.HaveState(KE_STATE.emSTATE_WEAK))
                {
                    int nNewPercent = nDamagePercent * KTGlobal.WeakDamagePercent / 100;
                    nDamagePercent = nNewPercent;
                    if (bTrace)
                    {
                        TraceDamage($"AttackEnemy: attacker bị suy yếu => nDamagePercent={nDamagePercent} (WeakDamagePercent={KTGlobal.WeakDamagePercent})");
                    }
                }

                if (enemy.IsPlayer())
                {
                    var m_pSkillDamagePReduce = client.m_pSkillDamageReduce;
                    nDamagePercent = nDamagePercent > m_pSkillDamagePReduce ? nDamagePercent - m_pSkillDamagePReduce : 0;
                    if (bTrace && m_pSkillDamagePReduce != 0)
                    {
                        TraceDamage($"AttackEnemy: attacker bị giảm damage (m_pSkillDamageReduce={m_pSkillDamagePReduce}) => nDamagePercent={nDamagePercent}");
                    }
                }

                // [DEBUG] Hệ số nền dùng xuyên suốt các bước tính damage bên dưới
                if (bTrace)
                {
                    TraceDamage($"AttackEnemy hệ số: nDamageAddPercent={nDamageAddPercent} nDamageAdd={nDamageAdd} nDamagePercent={nDamagePercent} | nSkillDamageT={nSkillDamageT} nAppendDamageT={nAppendDamageT} nSkillDamageTrimP={nSkillDamageTrimP} | skillDamageP={PropertyDictionaryManager.GetSkillDamageP(skillPd)} selfTrim={m_nSkillSelfDamagePTrim} baseTrim={m_nSkillDamagePTrim}");
                }

                // 1. Nếu skill có sử dụng tỉ lệ chính xác
                if (rcSkill.Data.IsUseAR)
                {
                    // Tạo mới KMagicAttribute
                    KMagicAttrib attrib = new KMagicAttrib();
                    // Loại là tỷ lệ đánh trúng
                    attrib.nAttribType = MAGIC_ATTRIB.magic_damage_append_hitrate;

                    // Chính xác ban đầu
                    int hitValue = 0;
                    // Nếu kỹ năng có dòng % tỷ lệ đánh trúng
                    if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_attackrating_p))
                    {
                        // Chính xác cơ bản
                        int m_AttackRating = client.m_AttackRating;
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating + m_AttackRating * PropertyDictionaryManager.GetHiteRateP(skillPd) / 100;
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }
                    // Nếu kỹ năng có dòng tăng điểm đánh trúng
                    else if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_attackrating_v))
                    {
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating + PropertyDictionaryManager.GetHiteRateV(skillPd);
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }
                    // Nếu kỹ năng không có chính xác
                    else
                    {
                        // Chính xác hiện có
                        int m_CurrentAttackRating = client.m_CurrentAttackRating;
                        // Giá trị chính xác mới
                        hitValue = m_CurrentAttackRating;
                        // Lưu lại giá trị vào KMagicAttribute
                        attrib.nValue[0] = hitValue;
                    }

                    // Bỏ qua né tránh đối thủ
                    int m_CurrentIgnoreDefense = client.m_CurrentIgnoreDefense;
                    // Tỷ lệ % bỏ qua né tránh đối thủ
                    int m_CurrentIgnoreDefensePercent = client.m_CurrentIgnoreDefensePercent;

                    // Lưu lại vào KMagicAttribute
                    attrib.nValue[1] = PropertyDictionaryManager.GetIgnoreDefenseV(skillPd) + m_CurrentIgnoreDefense;
                    attrib.nValue[2] = PropertyDictionaryManager.GetIgnoreDefenseP(skillPd) + m_CurrentIgnoreDefensePercent;

                    // Thêm vào ProDict
                    AppendDameDic.Add(attrib.nAttribType, attrib);

                    if (bTrace)
                    {
                        TraceDamage($"AttackEnemy chính xác: hitValue={hitValue} (base AR={client.m_AttackRating}, current AR={client.m_CurrentAttackRating}) ignoreDefenseV={attrib.nValue[1]} ignoreDefenseP={attrib.nValue[2]}");
                    }
                }

                if (rcSkill.Data.IsDamageSkill)
                {
                    // 1. TÍnh dame nộ nếu skill support
                    int AngerDamage = PropertyDictionaryManager.GetAngerDamage(skillPd);

                    bool bIsAnger = false;

                    if (AngerDamage > 0)
                    {
                        bIsAnger = true;
                        int nBaseAngerATK = KPlayerSetting.GetLevelBaseAngerATK(client.m_Level);

                        nAngerATK = (int)((AngerDamage / 100.0) * nBaseAngerATK) * nDamagePercent / 100 * nSkillDamageT / 1000;

                        nAngerPoiTime = KTGlobal.AngerTime;

                        if (bTrace)
                        {
                            TraceDamage($"AttackEnemy nộ: AngerDamage={AngerDamage} baseAngerATK={nBaseAngerATK} => nAngerATK={nAngerATK} series={client.m_Series}");
                        }
                    }

                    // 2. Sát thương ngũ hành

                    if (skillPd.ContainsKey((int)MAGIC_ATTRIB.magic_trice_eff_seriesdamage_r)) // Nếu skill có chứa sát thương ngũ hành
                    {
                        //FILL CHỈ SỐ VÀO ĐÂY
                        // Dame khắc hệ của nhân vật
                        int m_CurrentSeriesConquar = client.m_CurrentSeriesConquar;  // SIMBOY : magic_seriesconquar_r
                        int m_CurrentSeriesEnhance = client.m_CurrentSeriesEnhance;  // SIMBOY : magic_seriesenhance_r  Tấn công người chơi khắc hệ, trị tương khắc của kỹ năng

                        int cSrcSeriesDamage = PropertyDictionaryManager.GetSeriesDamage(skillPd);

                        KMagicAttrib cSeriesDamage = new KMagicAttrib();

                        cSeriesDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_series;

                        cSeriesDamage.nValue[0] = cSrcSeriesDamage + m_CurrentSeriesConquar;
                        cSeriesDamage.nValue[1] = m_CurrentSeriesEnhance;

                        AppendDameDic.Add(cSeriesDamage.nAttribType, cSeriesDamage);
                    }

                    // 3. Tính toán sát thương chí mạng
                    //Fill CHI SỐ VÀO ĐÂY
                    double m_CurrentFatallyStrike = client.m_CurrentDeadlyStrike; // SYMBOY : magic_fatallystrikeenhance_p : Xác suất tấn công chí mạng
                    m_CurrentFatallyStrike -= enemy.m_StrikeResistValue;

                    int PercenActive = KTGlobal.GetRandomNumber(0, 99);

                    double PERCENT = (m_CurrentFatallyStrike / KTGlobal.DeadlyStrikeBaseRate) * 100;

                    if (PERCENT > 0 && PERCENT > PercenActive)
                    {
                        KMagicAttrib cFatallyStrike = new KMagicAttrib();
                        cFatallyStrike.nAttribType = MAGIC_ATTRIB.magic_damage_append_fatallystrike;
                        cFatallyStrike.nValue[0] = 1;
                        IsDeadlyStrike = true;
                        AppendDameDic.Add(cFatallyStrike.nAttribType, cFatallyStrike);
                    }

                    if (bTrace)
                    {
                        TraceDamage($"AttackEnemy chí mạng: fatallyStrike={m_CurrentFatallyStrike} (deadlyStrike={client.m_CurrentDeadlyStrike} - strikeResist={enemy.m_StrikeResistValue}) PERCENT={PERCENT:F2} roll={PercenActive} => crit={IsDeadlyStrike}");
                    }

                    // 4. Tính toán dame vật lý
                    {
                        KMagicAttrib cPhysicsDamage = new KMagicAttrib();
                        cPhysicsDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_physics;

                        int nMinDamage = nDamageAdd;
                        int nMaxDamage = nDamageAdd;

                        if (bIsAnger) // NẾU CÓ DAME NỘ
                        {
                            // NGŨ HÀNH CỦA NHÂN VẬT
                            // FILL CHỈ SỐ VÀO ĐÂY
                            KE_SERIES_TYPE m_Series = client.m_Series;

                            if (nAngerATK > 0 && (m_Series == KE_SERIES_TYPE.series_metal || m_Series == KE_SERIES_TYPE.series_none))
                            {
                                cPhysicsDamage.nValue[0] += nAngerATK;
                                cPhysicsDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            // Nếu là sát thương ngoại công
                            if (bIsPhysical)
                            {
                                // Vật công ngoại Min
                                nMinDamage = client.m_PhysicsDamage.nValue[0];
                                // Vật công ngoại Max
                                nMaxDamage = client.m_PhysicsDamage.nValue[2];

                                // Đánh dấu có phải người chơi không
                                bool IsPlayer = client.IsPlayer();

                                // Sát thương cộng thêm từ ngoài nếu là người chơi
                                if (IsPlayer)
                                {
                                    int nEnhance = AlgorithmProperty.GetPhysicsEnhance((KPlayer)client);
                                    nMinDamage += nMinDamage * nEnhance / 100;
                                    nMaxDamage += nMaxDamage * nEnhance / 100;
                                }

                                // % vật công nội từ vũ khí
                                nMinDamage += (client.m_PhysicsDamage.nValue[0] * client.m_CurrentPhysicsDamageEnhanceP / 100);
                                nMaxDamage += (client.m_PhysicsDamage.nValue[2] * client.m_CurrentPhysicsDamageEnhanceP / 100);

                                // Sát thương cơ bản của vũ khí
                                nMinDamage += (client.m_PhysicPhysic.nValue[0] + client.m_nWeaponBaseDamageTrim);
                                nMaxDamage += (client.m_PhysicPhysic.nValue[2] + client.m_nWeaponBaseDamageTrim);

                                // Sát thương cộng thêm từ kỹ năng khác
                                nMinDamage = nMinDamage * nAppendDamageT / 1000;
                                nMaxDamage = nMaxDamage * nAppendDamageT / 1000;

                                // Biến tạm
                                int nTemp = 0;

                                // Kỹ năng hỗ trợ
                                int GetPhysicsDamageEnhanceP = PropertyDictionaryManager.GetPhysicsDamageEnhanceP(skillPd);
                                int GetPhysicsDamageEnhanceV = PropertyDictionaryManager.GetPhysicsDamageEnhanceV(skillPd);

                                // % Sát thương từ các kỹ năng hỗ trợ
                                if (GetPhysicsDamageEnhanceP > 0)
                                {
                                    nTemp = (client.m_PhysicsDamage.nValue[0] * GetPhysicsDamageEnhanceP / 100);
                                }
                                // Sát thương từ kỹ năng hỗ trợ
                                else if (GetPhysicsDamageEnhanceV > 0)
                                {
                                    nTemp = client.m_PhysicsDamage.nValue[2] + GetPhysicsDamageEnhanceV;
                                }

                                // Tăng thêm ngoại công do cái gì khác
                                int nTempMin = nTemp + PropertyDictionaryManager.GetPhysicsTrimDamage(skillPd).nValue[0];
                                int nTempMax = nTemp + PropertyDictionaryManager.GetPhysicsTrimDamage(skillPd).nValue[2];

                                // % sát thương cộng thêm
                                nTempMin += nTempMin * nDamageAddPercent / 100;
                                nTempMax += nTempMax * nDamageAddPercent / 100;
                                nTempMin *= nSkillDamageT / 1000;
                                nTempMax *= nSkillDamageT / 1000;

                                nMinDamage += nTempMin;
                                nMaxDamage += nTempMax;

                                // Kết quả cuối cùng
                                nMinDamage += (client.m_AddPhysicsDamage * nAppendDamageT / 1000);
                                nMaxDamage += (client.m_AddPhysicsDamage * nAppendDamageT / 1000);
                            }
                            // Nếu là sát thương nội công
                            else
                            {
                                // Vật công nội Min
                                nMinDamage = client.m_MagicDamage.nValue[0];
                                // Vật công nội Max
                                nMaxDamage = client.m_MagicDamage.nValue[2];

                                // % vật công nội từ vũ khí
                                nMinDamage += (client.m_MagicDamage.nValue[0] * client.m_CurrentMagicPhysicsEnhanceP / 100);
                                nMaxDamage += (client.m_MagicDamage.nValue[2] * client.m_CurrentMagicPhysicsEnhanceP / 100);

                                // % nội công riêng được khai báo trực tiếp trên kỹ năng đang thi triển.
                                int skillMagicEnhanceP = GetSkillPropertyValue(skillPd,
                                    MAGIC_ATTRIB.magic_addphysicsmagic_p);
                                nMinDamage += client.m_MagicDamage.nValue[0] * skillMagicEnhanceP / 100;
                                nMaxDamage += client.m_MagicDamage.nValue[2] * skillMagicEnhanceP / 100;

                                // Sát thương cơ bản của vũ khí
                                nMinDamage += (client.m_PhysicsMagic.nValue[0] + client.m_nWeaponBaseDamageTrim);
                                nMaxDamage += (client.m_PhysicsMagic.nValue[2] + client.m_nWeaponBaseDamageTrim);

                                // Sát thương cộng thêm từ kỹ năng hỗ trợ
                                nMinDamage = nMinDamage * nAppendDamageT / 1000;
                                nMaxDamage = nMaxDamage * nAppendDamageT / 1000;

                                // Biến tạm
                                int nTemp = 0;

                                // Kỹ năng hỗ trợ
                                int GetPhysicsDamageEnhanceP = PropertyDictionaryManager.GetPhysicsDamageEnhanceP(skillPd);
                                int GetPhysicsDamageEnhanceV = PropertyDictionaryManager.GetPhysicsDamageEnhanceV(skillPd);

                                // % Sát thương từ các kỹ năng hỗ trợ
                                if (GetPhysicsDamageEnhanceP > 0)
                                {
                                    nTemp = (client.m_MagicDamage.nValue[0] * GetPhysicsDamageEnhanceP / 100);
                                }
                                // Sát thương từ kỹ năng hỗ trợ
                                else if (GetPhysicsDamageEnhanceV > 0)
                                {
                                    nTemp = client.m_MagicDamage.nValue[0] + GetPhysicsDamageEnhanceV;
                                }

                                // Tăng thêm nội công do cái gì khác
                                int nTempMin = nTemp + PropertyDictionaryManager.GetMagicTrimDamage(skillPd).nValue[0];
                                int nTempMax = nTemp + PropertyDictionaryManager.GetMagicTrimDamage(skillPd).nValue[2];

                                // % sát thương cộng thêm
                                nTempMin += nTempMin * nDamageAddPercent / 100;
                                nTempMax += nTempMax * nDamageAddPercent / 100;
                                nTempMin *= nSkillDamageT / 1000;
                                nTempMax *= nSkillDamageT / 1000;

                                nMinDamage += nTempMin;
                                nMaxDamage += nTempMax;

                                // Kết quả cuối cùng
                                nMinDamage += (client.m_MagicPhysicsDamage.nValue[0] * nAppendDamageT / 1000);
                                nMaxDamage += (client.m_MagicPhysicsDamage.nValue[2] * nAppendDamageT / 1000);
                            }

                            // Nhận thêm sát thương bởi dame phép
                            if (client.m_nAttackAddedByMana > 0)
                            {
                                nMinDamage += client.m_CurrentMana * client.m_nAttackAddedByMana / 100;
                                nMaxDamage += client.m_CurrentMana * client.m_nAttackAddedByMana / 100;
                            }

                            cPhysicsDamage.nValue[0] = nMinDamage * nDamagePercent / 100;
                            cPhysicsDamage.nValue[2] = nMaxDamage * nDamagePercent / 100;
                        }

                        AppendDameDic.Add(cPhysicsDamage.nAttribType, cPhysicsDamage);
                    }

                    int nSkillPercent = nDamageAddPercent + 100; //% sức mạnh kỹ năng

                    // 5. Tính toán dame băng công
                    {
                        KMagicAttrib cColdDamage = new KMagicAttrib();
                        cColdDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_cold;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_water)
                            {
                                cColdDamage.nValue[0] += nAngerATK;
                                cColdDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cColdDamage = CalcAttackDamage(PropertyDictionaryManager.GetColdDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_cold]);

                            cColdDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_cold;
                        }

                        AppendDameDic.Add(cColdDamage.nAttribType, cColdDamage);
                    }
                    // 6. Tính toán dame hỏa công
                    {
                        KMagicAttrib cFireDamage = new KMagicAttrib();
                        cFireDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_fire;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_fire)
                            {
                                cFireDamage.nValue[0] += nAngerATK;
                                cFireDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cFireDamage = CalcAttackDamage(PropertyDictionaryManager.GetFireDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_fire]);

                            cFireDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_fire;
                        }

                        AppendDameDic.Add(cFireDamage.nAttribType, cFireDamage);
                    }
                    // 7. Tính toán dane lôi công
                    {
                        KMagicAttrib cLightDamage = new KMagicAttrib();
                        cLightDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_light;
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_fire)
                            {
                                cLightDamage.nValue[0] += nAngerATK;
                                cLightDamage.nValue[2] += nAngerATK;
                            }
                        }
                        else
                        {
                            cLightDamage = CalcAttackDamage(PropertyDictionaryManager.GetLightDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_light]);

                            cLightDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_light;
                        }

                        AppendDameDic.Add(cLightDamage.nAttribType, cLightDamage);
                    }

                    // 8. Tính toán dame độc
                    {
                        KMagicAttrib cPoisonDamage = new KMagicAttrib();
                        cPoisonDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                        // Nếu là sát thương nộ
                        if (bIsAnger)
                        {
                            if (nAngerATK > 0 && client.m_Series == KE_SERIES_TYPE.series_wood)
                            {
                                KMagicAttrib angermagic = new KMagicAttrib();
                                angermagic.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                                angermagic.nValue[0] = nAngerATK * nSkillDamageT / 1000;
                                angermagic.nValue[1] = nAngerPoiTime;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, angermagic); //
                            }
                        }
                        else
                        {
                            cPoisonDamage.nValue[0] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[0] * nSkillPercent / 100 * nSkillDamageT / 1000 * nDamagePercent / 100;
                            cPoisonDamage.nValue[1] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[1];
                            cPoisonDamage.nValue[2] = PropertyDictionaryManager.GetPoisonDamage(skillPd).nValue[2] * nSkillPercent / 100 * nSkillDamageT / 1000 * nDamagePercent / 100;
                            if (cPoisonDamage.nValue[2] == 0)
                            {
                                cPoisonDamage.nValue[2] = cPoisonDamage.nValue[0] * cPoisonDamage.nValue[1];
                            }

                            KMagicAttrib magic = new KMagicAttrib();
                            magic.nAttribType = MAGIC_ATTRIB.magic_damage_append_poison;
                            if (bIsPhysical)// Nếu là skill vật lý thif tính độc ngoại công
                            {
                                // Tổng sát thương độc ngoại công
                                magic.nAttribType = client.m_CurrentPoisonDamage.nAttribType;
                                magic.nValue[0] = client.m_CurrentPoisonDamage.nValue[0] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                magic.nValue[1] = client.m_CurrentPoisonDamage.nValue[1];
                                magic.nValue[2] = client.m_CurrentPoisonDamage.nValue[2] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, magic);
                            }
                            else
                            {
                                // TỔng sát thương độc nội công
                                magic.nAttribType = client.m_MagicPoisonDamage.nAttribType;
                                magic.nValue[0] = client.m_MagicPoisonDamage.nValue[0] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                magic.nValue[1] = client.m_MagicPoisonDamage.nValue[1];
                                magic.nValue[2] = client.m_MagicPoisonDamage.nValue[2] * nDamagePercent / 100 * nAppendDamageT / 1000;
                                cPoisonDamage = MixPoisonDamage(cPoisonDamage, magic);
                            }
                        }

                        // Nếu mà mà có dame độc
                        if (cPoisonDamage.nValue[0] > 0)
                        {
                            cPoisonDamage.nValue[1] += cPoisonDamage.nValue[1] * client.m_nPoisonTimeEnhanceP / 100;
                            if (cPoisonDamage.nValue[1] <= 0)
                            {
                                cPoisonDamage.nValue[0] = 0;
                                cPoisonDamage.nValue[1] = 0;
                                cPoisonDamage.nValue[2] = 0;
                            }
                        }

                        // Lượng hóa giải sát thương độc
                        int enemyAbsorbPoisonDamage = enemy.m_nPosionWeakenPoint;
                        // % tối đa sát thương ban đầu hóa giải được
                        int enemyAbsorbPoisonDamageMaxBaseDamageP = enemy.m_nPoisonWeakenMaxDamageP;

                        // Nếu có thể hóa giải sát thương
                        if (enemyAbsorbPoisonDamage > 0 && enemyAbsorbPoisonDamageMaxBaseDamageP > 0)
                        {
                            // Sát thương tối đa có thể hóa giải được
                            int maxAbsorbPoisonDamage_Min = enemyAbsorbPoisonDamageMaxBaseDamageP * cPoisonDamage.nValue[0] / 100;
                            int maxAbsorbPoisonDamage_Max = enemyAbsorbPoisonDamageMaxBaseDamageP * cPoisonDamage.nValue[2] / 100;

                            // Tính toán lại lượng sát thương độc gây ra
                            cPoisonDamage.nValue[0] -= Math.Min(enemyAbsorbPoisonDamage, maxAbsorbPoisonDamage_Min);
                            cPoisonDamage.nValue[2] -= Math.Min(enemyAbsorbPoisonDamage, maxAbsorbPoisonDamage_Max);
                        }

                        AppendDameDic.Add(cPoisonDamage.nAttribType, cPoisonDamage);
                    }
                    // 9. TÍnh toán sát thương PHÉP
                    if (!bIsAnger)
                    {
                        KMagicAttrib cMagicDamage = new KMagicAttrib();
                        cMagicDamage.nAttribType = MAGIC_ATTRIB.magic_damage_append_magic;

                        cMagicDamage = CalcAttackDamage(PropertyDictionaryManager.GetMagicDamage(skillPd), nSkillPercent, bIsPhysical, nDamagePercent, nSkillDamageT, nAppendDamageT, client.m_damage[(int)DAMAGE_TYPE.damage_magic]);

                        AppendDameDic.Add(cMagicDamage.nAttribType, cMagicDamage);
                    }
                    // 10.Hút hít máu
                    {
                        KMagicAttrib cStealLife = new KMagicAttrib();
                        cStealLife.nAttribType = MAGIC_ATTRIB.magic_damage_append_steallife;

                        cStealLife.nValue[0] = PropertyDictionaryManager.GetLifeStealP(skillPd).nValue[0] + client.m_CurrentLifeStolen;
                        cStealLife.nValue[1] = PropertyDictionaryManager.GetLifeStealP(skillPd).nValue[1] + client.m_nStealLifeRate;
                        cStealLife.nValue[2] = 0;

                        AppendDameDic.Add(cStealLife.nAttribType, cStealLife);
                    }

                    // 11. Hút mana
                    {
                        KMagicAttrib cStealMana = new KMagicAttrib();
                        cStealMana.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealmana;
                        cStealMana.nValue[0] = PropertyDictionaryManager.GetManaStealP(skillPd).nValue[0] + client.m_CurrentManaStolen;
                        cStealMana.nValue[1] = PropertyDictionaryManager.GetManaStealP(skillPd).nValue[1] + client.m_nStealManaRate;
                        cStealMana.nValue[2] = 0;
                        AppendDameDic.Add(cStealMana.nAttribType, cStealMana);
                    }

                    // 12. Hút thể lực
                    {
                        KMagicAttrib cStealStamina = new KMagicAttrib();
                        cStealStamina.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealstamina;
                        cStealStamina.nValue[0] = PropertyDictionaryManager.GetStealStaminaP(skillPd).nValue[0] + client.m_CurrentStaminaStolen;
                        cStealStamina.nValue[1] = PropertyDictionaryManager.GetStealStaminaP(skillPd).nValue[1] + client.m_nStealStaminaRate;
                        cStealStamina.nValue[2] = 0;
                        AppendDameDic.Add(cStealStamina.nAttribType, cStealStamina);
                    }
                }

                // 13. Trạng thái hút
                {
                    KMagicAttrib cStealState = new KMagicAttrib();
                    cStealState.nAttribType = MAGIC_ATTRIB.magic_damage_append_stealstate;
                    cStealState.nValue[0] = PropertyDictionaryManager.GetStealState(skillPd).nValue[0];
                    cStealState.nValue[1] = PropertyDictionaryManager.GetStealState(skillPd).nValue[1];
                    cStealState.nValue[2] = PropertyDictionaryManager.GetStealState(skillPd).nValue[2];

                    AppendDameDic.Add(cStealState.nAttribType, cStealState);
                }

                // 14.Apply toàn bộ trạng thái ngũ hành vào DIC
                List<KMagicAttrib> TotalEffectOfSkill = PropertyDictionaryManager.GetAllSkillEffectAppend(skillPd);

                foreach (KMagicAttrib _magic in TotalEffectOfSkill)
                {
                    KE_STATE _State = AlgorithmProperty.ConvertSkillEffectToState(_magic.nAttribType);
                    KMagicAttrib Final = AlgorithmProperty.AppendAttackAttrib(_magic, client, client.m_state[(int)_State], rcSkill);
                    AppendDameDic.Add(Final.nAttribType, Final);
                }

                int nSeries = rcSkill.Data.ElementalSeries;
                bool bIsMelee = rcSkill.Data.IsMelee;
                int dwSkillStyle = KTGlobal.GetSkillStyleDef(rcSkill.Data.SkillStyle);

                // [DEBUG] Tổng hợp damage thô từng loại trước khi vào ReceiveDamage
                if (bTrace)
                {
                    System.Text.StringBuilder sbDamage = new System.Text.StringBuilder();
                    foreach (KeyValuePair<MAGIC_ATTRIB, KMagicAttrib> entry in AppendDameDic)
                    {
                        sbDamage.Append($"{entry.Key}=[{entry.Value.nValue[0]}, {entry.Value.nValue[1]}, {entry.Value.nValue[2]}] ");
                    }
                    TraceDamage($"AttackEnemy tổng hợp damage thô: skillSeries={nSeries} isMelee={bIsMelee} skillStyle={dwSkillStyle} crit={IsDeadlyStrike} | {sbDamage}");
                }

                //100 : Hiệu suất tấn công.
                return ReceiveDamage(client, enemy, nSeries, bIsMelee, AppendDameDic, dwSkillStyle, IsDeadlyStrike, rcSkill, 100, skillPd, skillPos, isSubPhrase);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Thực hiện chịu sát thương
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        /// <param name="nSeries"></param>
        /// <param name="bIsMelee"></param>
        /// <param name="AppendDameDic"></param>
        /// <param name="dwSkillStyle"></param>
        /// <param name="bIsDeadlyStrike"></param>
        /// <param name="rcSkill"></param>
        /// <param name="nAdditionP"></param>
        /// <param name="SkillPD"></param>
        /// <param name="skillPos"></param>
        /// <returns></returns>
        private static bool ReceiveDamage(GameObject attacker, GameObject target, int nSeries, bool bIsMelee, Dictionary<MAGIC_ATTRIB, KMagicAttrib> AppendDameDic, int dwSkillStyle, bool bIsDeadlyStrike, SkillLevelRef rcSkill, int nAdditionP, PropertyDictionary SkillPD, UnityEngine.Vector2 skillPos, bool isSubPhrase)
        {
            // [DEBUG] Trace cách tính damage nếu target nằm trong PacketMonitoringUsers.xml và bị NGƯỜI CHƠI tấn công
            bool bTrace = AlgorithmProperty.ShouldTraceDamage(target) && attacker.IsPlayer();

            // Nếu mục tiêu đang trong trạng thái miễn nhiếm toàn bộ sát thương và hiệu ứng
            if (target.IsImmuneToAllDamageAndStates())
            {
                if (bTrace)
                {
                    TraceDamage($"ReceiveDamage MISS: target {target.RoleName}[{target.RoleID}] miễn nhiễm toàn bộ sát thương và hiệu ứng");
                }
                // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                AlgorithmProperty.SyncMiss(attacker, target);
                return false;
            }

            // Khởi tạo biến break khỏi loop
            bool bBreak = false;

            // Đoạn này bảo vệ người chơi ko bị chịu sát thương trong thời gian bảo vệ.Hoặc bỏ qua toàn bộ tấn công.
            if (target.m_nIgnoreAttackOnTime > 0 && rcSkill.Data.IsDamageSkill)
            {
                // Thời điểm Tick bỏ qua công kích lần trước
                long lastIgnoreAttackTick = target.m_nLastIgnoreAttackTime;
                // Thời gian hiện tại của hệ thống
                long currentTick = KTGlobal.GetCurrentTimeMilis();
                // Khoảng thời gian bỏ qua công kích
                long ignoreAttackTick = (long)(target.m_nIgnoreAttackOnTime / 18f * 1000);
                // Khoảng thời gian đã trôi qua
                long nIntervalTime = currentTick - lastIgnoreAttackTick;
                // Khoảng thời gian duy trì hiệu ứng bỏ qua công kích
                long ignoreAttackDurationTick = (long)(target.m_nIgnoreAttackDuration / 18f * 1000);

                // Nếu đã đến thời điểm bỏ qua công kích
                if (nIntervalTime >= ignoreAttackTick)
                {
                    // Nếu đã nằm ngoài khoảng bỏ qua công kích
                    if (nIntervalTime - ignoreAttackTick > ignoreAttackDurationTick)
                    {
                        // Cập nhật thời điểm bỏ qua nửa giây công kích
                        target.m_nLastIgnoreAttackTime = KTGlobal.GetCurrentTimeMilis();
                    }
                    else
                    {
                        //if (target is KPlayer)
                        //{
                        //    PlayerManager.ShowNotification(target as KPlayer, "Ignore ATTACK");
                        //}

                        if (bTrace)
                        {
                            TraceDamage($"ReceiveDamage MISS: target đang trong khoảng thời gian bỏ qua công kích (ignoreAttackOnTime={target.m_nIgnoreAttackOnTime})");
                        }

                        // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                        AlgorithmProperty.SyncMiss(attacker, target);
                        return false;
                    }
                }
            }

            // Nếu không phải là kỹ năng đánh luôn trúng
            if (!rcSkill.Data.AlwaysHit)
            {
                // Nếu là damage ngoại
                if (rcSkill.Data.IsPhysical)
                {
                    int nRand = KTGlobal.GetRandomNumber(1, 100);
                    // Nếu mục tiêu có thể tránh công ngoại
                    if (nRand <= target.m_sIgnorePhysicDamage)
                    {
                        //if (target is KPlayer)
                        //{
                        //    PlayerManager.ShowNotification(target as KPlayer, "Ignore Physic ATTACK");
                        //}

                        if (bTrace)
                        {
                            TraceDamage($"ReceiveDamage MISS: target tránh công ngoại thành công (roll={nRand} <= {target.m_sIgnorePhysicDamage})");
                        }

                        // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                        AlgorithmProperty.SyncMiss(attacker, target);
                        return false;
                    }
                }
                // Nếu là damage nội
                else
                {
                    int nRand = KTGlobal.GetRandomNumber(1, 100);
                    // Nếu mục tiêu có thể tránh công nội
                    if (nRand <= target.m_sIgnoreMagicDamage)
                    {
                        //if (target is KPlayer)
                        //{
                        //    PlayerManager.ShowNotification(target as KPlayer, "Ignore Magic ATTACK");
                        //}

                        if (bTrace)
                        {
                            TraceDamage($"ReceiveDamage MISS: target tránh công nội thành công (roll={nRand} <= {target.m_sIgnoreMagicDamage})");
                        }

                        // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                        AlgorithmProperty.SyncMiss(attacker, target);
                        return false;
                    }
                }
            }

            // Nếu đối phương có dòng xác suất tránh công tầm xa hoặc gần
            if (target.m_sIgnoreMeleeDamage > 0 || target.m_sIgnoreRangerDamage > 0)
            {
                // Khoảng cách với đối phương
                float distanceToEnemy = KTLogic.GetDistance(attacker, target);
                // Nếu dưới 300 là tầm gần
                if (distanceToEnemy <= 300)
                {
                    int nRand = KTGlobal.GetRandomNumber(1, 100);
                    // Nếu mục tiêu có thể tránh công
                    if (nRand <= target.m_sIgnoreMeleeDamage)
                    {
                        if (bTrace)
                        {
                            TraceDamage($"ReceiveDamage MISS: target tránh công tầm gần thành công (khoảng cách={distanceToEnemy:F0}, roll={nRand} <= {target.m_sIgnoreMeleeDamage})");
                        }
                        // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                        AlgorithmProperty.SyncMiss(attacker, target);
                        return false;
                    }
                }
                // Nếu là tầm xa
                else
                {
                    int nRand = KTGlobal.GetRandomNumber(1, 100);
                    // Nếu mục tiêu có thể tránh công
                    if (nRand <= target.m_sIgnoreRangerDamage)
                    {
                        if (bTrace)
                        {
                            TraceDamage($"ReceiveDamage MISS: target tránh công tầm xa thành công (khoảng cách={distanceToEnemy:F0}, roll={nRand} <= {target.m_sIgnoreRangerDamage})");
                        }
                        // GỬI LẠI PACKET MISS CHO NGƯỜI CHƠI
                        AlgorithmProperty.SyncMiss(attacker, target);
                        return false;
                    }
                }
            }

            // Nếu có dòng tỷ lệ đánh trúng
            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_hitrate))
            {
                // Giá trị chính xác của bản thân
                KMagicAttrib cHitRate = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_hitrate];

                // Chính xác
                int nHitRate = cHitRate.nValue[0];
                // Né tránh của đối thủ
                int nDefense = (target.m_CurrentDefend - cHitRate.nValue[1]) * (100 - cHitRate.nValue[2]) / 100;

                // Linh Hư Tụ Khí chỉ tăng né tránh khi đòn đánh là tầm xa.
                // Cộng sau phần bỏ qua né tránh để bonus riêng này không bị triệt tiêu
                // bởi chỉ số bỏ qua né tránh chung của người tấn công.
                if (!bIsMelee)
                {
                    nDefense += GetLinhHuTuKhiRangedDefense(target);
                }

                // Tỷ lệ đánh trúng
                int nPercent;
                // Nếu độ chính xác dưới 0 thì độ chính xác = Min
                if (nHitRate <= 0)
                {
                    nPercent = KTGlobal.HitPercentMin;
                }
                // Nếu nẽ tránh của đối thủ dưới 0 thì chính xác của bản thân = Max
                else if (nDefense <= 0)
                {
                    nPercent = KTGlobal.HitPercentMax;
                }
                // Trường hợp còn lại sẽ tính toán
                else
                {
                    // Nếu là phái Đường Môn
                    if (target is KPlayer targetPlayer && targetPlayer.m_cPlayerFaction.GetFactionId() == 3)
                    {
                        nPercent = nHitRate * 100 / (nHitRate + (int)(nDefense * 1.5f));
                    }
                    // Trường hợp khác
                    else
                    {
                        // Tỷ lệ đánh trúng (Sói add nhân thêm hệ số 3 vào né tránh để tạo khác biệt)
                        nPercent = nHitRate * 100 / (nHitRate + (int)(nDefense * 3));
                    }


                    // Nếu tỷ lệ dưới Min thì thiết lập = Min
                    if (nPercent < KTGlobal.HitPercentMin)
                    {
                        nPercent = KTGlobal.HitPercentMin;
                    }
                    // Nếu tỷ lệ dưới Max thì thiết lập = Max
                    else if (nPercent > KTGlobal.HitPercentMax)
                    {
                        nPercent = KTGlobal.HitPercentMax;
                    }
                }

                // Số ngẫu nhiên tỷ lệ đánh trúng
                int randomHitRate = KTGlobal.GetRandomNumber(0, 99);

                if (bTrace)
                {
                    TraceDamage($"ReceiveDamage tỷ lệ trúng: nHitRate={nHitRate} nDefense={nDefense} (targetDefend={target.m_CurrentDefend} - ignoreV={cHitRate.nValue[1]}, ignoreP={cHitRate.nValue[2]}%) => nPercent={nPercent} roll={randomHitRate} => {(randomHitRate > nPercent ? "MISS" : "HIT")}");
                }

                // Nếu đánh trượt
                if (randomHitRate > nPercent)
                {
                    // Syns miss
                    AlgorithmProperty.SyncMiss(attacker, target);
                    return false;
                }
            }

            // Tổng sát thương nhận được ban đầu
            int nDamagePercent = 100;
            nDamagePercent *= nAdditionP / 100;

            // Sát thương nhận thêm khi bị tấn công bởi quái
            if (target is Monster)
            {
                nDamagePercent += attacker.m_nDamageAddedPercentWhenHitNPC;
            }

            // Loại skill gây dame bởi khoảng cách.
            if (target.m_sRdcLifeWithDis.nDamageSkillId == rcSkill.Data.ID)
            {
                nDamagePercent = target.m_sRdcLifeWithDis.nDamageAddedP;
            }

            // Resolve target-dependent Co Mo critical effects after hit validation.
            // Copy the dictionary before changing its crit marker: one cast may hit many targets.
            bool forcedCoMoCrit = CoMoEffects.IsForcedCritical(rcSkill, target);
            int critResistance = CoMoEffects.PassivePercent(target, 16004);
            if (forcedCoMoCrit || (bIsDeadlyStrike && critResistance > 0))
            {
                bIsDeadlyStrike = forcedCoMoCrit || KTGlobal.GetRandomNumber(1, 100) > critResistance;
                AppendDameDic = new Dictionary<MAGIC_ATTRIB, KMagicAttrib>(AppendDameDic);
                if (bIsDeadlyStrike)
                    AppendDameDic[MAGIC_ATTRIB.magic_damage_append_fatallystrike] = new KMagicAttrib
                    { nAttribType = MAGIC_ATTRIB.magic_damage_append_fatallystrike, nValue = new[] { 1, 0, 0 } };
                else AppendDameDic.Remove(MAGIC_ATTRIB.magic_damage_append_fatallystrike);
            }

            // Nếu là đòn đánh chí mạng
            if (bIsDeadlyStrike)
            {
                int nNewPercent = nDamagePercent * (KTGlobal.DeadlyStrikeDamagePercent + attacker.m_DeadlystrikeDamagePercent + CoMoEffects.CriticalDamageBonus(attacker, rcSkill) - target.m_nDefenceDeadlyStrikeDamageTrim) / 100;

                nDamagePercent = nNewPercent;

                if (bTrace)
                {
                    TraceDamage($"ReceiveDamage chí mạng: nDamagePercent={nDamagePercent} (base={KTGlobal.DeadlyStrikeDamagePercent} + attackerCrit={attacker.m_DeadlystrikeDamagePercent} - targetTrim={target.m_nDefenceDeadlyStrikeDamageTrim})");
                }
            }

            NPC_CALC_DAMAGE_PARAM CALC_DAMAGE_PARAM = new NPC_CALC_DAMAGE_PARAM();
            int nTotalDamage = 0;
            for (int i = 0; i < AppendDameDic.Count; ++i)
            {
                KMagicAttrib cDamage = AppendDameDic.Values.ElementAt(i);

                int nDamage = (cDamage.nValue[0] + cDamage.nValue[2]) / 2;
                if (nDamage < 0)
                {
                    nDamage = 0;
                }
                nTotalDamage += (nDamage * nDamagePercent / 100);
            }

            CALC_DAMAGE_PARAM.nTotalDamage = nTotalDamage;

            if (bTrace)
            {
                TraceDamage($"ReceiveDamage: nDamagePercent={nDamagePercent} nTotalDamage={nTotalDamage} targetHP={target.m_CurrentLife}/{target.m_CurrentLifeMax}");
            }

            bBreak = false;

            // Tính toán dame ngũ hành
            int nSeriesConquarRes = 0;

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_series))
            {
                KMagicAttrib cSeriesDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_series];
                nSeriesConquarRes = cSeriesDamage.nValue[0];

                // NẾU HỆ CỦA SKILL ĐÁNH VÀO NGƯỜI KHẮC HỆ==> SKILL ĐƯỢC TĂNG CƯỜNG SỨC TẤN CÔNG
                if (KTGlobal.g_IsConquer(nSeries, (int)target.m_Series))
                {
                    int nTempParam = KTGlobal.SeriesTrimParam4 * attacker.m_nSeriesEnhance + target.m_Level * KTGlobal.SeriesTrimParam2 + KTGlobal.SeriesTrimParam3;

                    if (nTempParam == 0)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "Bug 1");
                        return false;
                    }
                    // CƯỜNG HÓA DAME CỦA THẰNG TẤN CÔNG nAppendAttack
                    int nAppendAttack = (int)(nSeriesConquarRes * KTGlobal.SeriesTrimParam1 * attacker.m_nSeriesEnhance / nTempParam); // %x
                    if (attacker.m_nSeriesEnhance != 0)
                        nAppendAttack += nSeriesConquarRes * KTGlobal.SeriesTrimParam5 / 100;

                    nTempParam = (int)KTGlobal.SeriesTrimParam4 * target.m_nSeriesAbate + attacker.m_Level * KTGlobal.SeriesTrimParam2 + KTGlobal.SeriesTrimParam3;

                    if (nTempParam == 0)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "Bug 2");
                        return false;
                    }

                    // KHÁNG NGŨ HÀNH BAO NHIÊU
                    int nAppendDefend = (int)(nSeriesConquarRes * KTGlobal.SeriesTrimParam1 * target.m_nSeriesAbate / nTempParam); // %b

                    if (target.m_nSeriesAbate != 0) // KHÁNG NGŨ HÀNH
                        nAppendDefend += nSeriesConquarRes * KTGlobal.SeriesTrimParam5 / 100;

                    int nAppend = nAppendAttack - nAppendDefend;

                    int nApMax = nSeriesConquarRes * KTGlobal.SeriesTrimMax / 100;

                    int nSymbol = nAppend > 0 ? 1 : -1;
                    nAppend = Math.Abs(nAppend) > nApMax ? (nApMax * nSymbol) : nAppend;

                    nSeriesConquarRes = nSeriesConquarRes + nAppend;

                    nSeriesConquarRes += cSeriesDamage.nValue[1] - target.m_CurrentSeriesResist;

                    if (nSeriesConquarRes > 0)
                        nSeriesConquarRes = -nSeriesConquarRes;
                    else
                        nSeriesConquarRes = 0;

                    // KẾT THÚC TÍNH DAME TƯƠNG KHẮC
                }
                else if (KTGlobal.g_IsConquer((int)target.m_Series, nSeries))
                {
                    int nTempParam = (int)KTGlobal.SeriesTrimParam4 * attacker.m_nSeriesAbate + target.m_Level * KTGlobal.SeriesTrimParam2 + KTGlobal.SeriesTrimParam3;
                    if (nTempParam == 0)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "Bug 3");
                        return false;
                    }
                    int nAppendAttack = (int)(nSeriesConquarRes * KTGlobal.SeriesTrimParam1 * attacker.m_nSeriesAbate / nTempParam);
                    if (attacker.m_nSeriesAbate != 0)
                        nAppendAttack += nSeriesConquarRes * KTGlobal.SeriesTrimParam5 / 100;

                    nTempParam = KTGlobal.SeriesTrimParam4 * target.m_nSeriesEnhance + attacker.m_Level * KTGlobal.SeriesTrimParam2 + KTGlobal.SeriesTrimParam3;
                    if (nTempParam == 0)
                    {
                        LogManager.WriteLog(LogTypes.Fatal, "Bug 4");
                        return false;
                    }
                    int nAppendDefend = (int)(nSeriesConquarRes * KTGlobal.SeriesTrimParam1 * target.m_nSeriesEnhance / nTempParam);   // %a
                    if (target.m_nSeriesEnhance != 0)
                        nAppendDefend += nSeriesConquarRes * KTGlobal.SeriesTrimParam5 / 100;

                    int nAppend = nAppendAttack - nAppendDefend;
                    int nApMax = nSeriesConquarRes * KTGlobal.SeriesTrimMax / 100;

                    int nSymbol = nAppend > 0 ? 1 : -1;
                    nAppend = Math.Abs(nAppend) > nApMax ? (nApMax * nSymbol) : nAppend;

                    nSeriesConquarRes = nSeriesConquarRes - nAppend;

                    nSeriesConquarRes += cSeriesDamage.nValue[1] - target.m_CurrentSeriesResist;

                    if (nSeriesConquarRes < 0)
                        nSeriesConquarRes = 0;
                }
                else
                {
                    nSeriesConquarRes = 0;
                }

                CALC_DAMAGE_PARAM.nSeriesConquarRes = nSeriesConquarRes;

                if (bTrace)
                {
                    TraceDamage($"ReceiveDamage ngũ hành: skillSeries={nSeries} targetSeries={target.m_Series} attackerEnhance={attacker.m_nSeriesEnhance} targetAbate={target.m_nSeriesAbate} targetResist={target.m_CurrentSeriesResist} => nSeriesConquarRes={nSeriesConquarRes}");
                }
            }

            // 3.TÍnh Toán Dame chí mạng

            int nTempDecRes = 0;

            DAMAGE_TYPE eDamageType = DAMAGE_TYPE.damage_num;

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_fatallystrike))
            {
                KMagicAttrib cFatallyStrikeDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_fatallystrike];

                switch (nSeries)
                {
                    case (int)KE_SERIES_TYPE.series_metal:
                        eDamageType = DAMAGE_TYPE.damage_physics;
                        break;

                    case (int)KE_SERIES_TYPE.series_wood:
                        eDamageType = DAMAGE_TYPE.damage_poison;
                        break;

                    case (int)KE_SERIES_TYPE.series_water:
                        eDamageType = DAMAGE_TYPE.damage_cold;
                        break;

                    case (int)KE_SERIES_TYPE.series_fire:
                        eDamageType = DAMAGE_TYPE.damage_fire;
                        break;

                    case (int)KE_SERIES_TYPE.series_earth:
                        eDamageType = DAMAGE_TYPE.damage_light;
                        break;
                }
                if (eDamageType < DAMAGE_TYPE.damage_num)
                {
                    nTempDecRes = KTGlobal.FatallyStrikePercent * target.m_damage[(int)eDamageType].GetCurResist() / 100;

                    target.m_damage[(int)eDamageType].AddCurResist(-nTempDecRes);
                }
            }

            int nLife = target.m_CurrentLife;
            bool bIsOver = false;
            int nMinLife = 0;

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_physics))
            {
                KMagicAttrib cPhysicsDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_physics];

                bIsOver = !CalcDamage(attacker, target, cPhysicsDamage.nValue[0], cPhysicsDamage.nValue[2], DAMAGE_TYPE.damage_physics, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);

                nMinLife = Math.Max(target.m_CurrentLife, 0);
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_cold))
            {
                KMagicAttrib cColdDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_cold];

                bIsOver = !CalcDamage(attacker, target, cColdDamage.nValue[0], cColdDamage.nValue[2], DAMAGE_TYPE.damage_cold, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);

                nMinLife = Math.Max(target.m_CurrentLife, 0);
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_fire))
            {
                KMagicAttrib cFireDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_fire];

                bIsOver = !CalcDamage(attacker, target, cFireDamage.nValue[0], cFireDamage.nValue[2], DAMAGE_TYPE.damage_fire, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);

                nMinLife = Math.Max(target.m_CurrentLife, 0);
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_light))
            {
                KMagicAttrib cLightDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_light];

                bIsOver = !CalcDamage(attacker, target, cLightDamage.nValue[0], cLightDamage.nValue[2], DAMAGE_TYPE.damage_light, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);

                nMinLife = Math.Max(target.m_CurrentLife, 0);
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_poison))
            {
                KMagicAttrib cPoisonDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_poison];

                if (!bIsOver)
                {
                    int nPosionDamage = cPoisonDamage.nValue[0];
                    int nReceivePercent = CalcReceivePercent(CALC_DAMAGE_PARAM.nSeriesConquarRes, attacker, target, DAMAGE_TYPE.damage_poison, target.m_damage[(int)DAMAGE_TYPE.damage_poison]); //target.m_damage[DAMAGE_TYPE.damage_poison].CalcReceivePercent(CALC_DAMAGE_PARAM.nSeriesConquarRes, Npc[nLauncher], damage_poison);
                    nPosionDamage = nPosionDamage * nReceivePercent / 100;
                    bIsOver = !CalcDamage(attacker, target, nPosionDamage, nPosionDamage, DAMAGE_TYPE.damage_poison, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);

                    if (!target.IsDead() && !bIsOver)
                    {
                        if (nPosionDamage > 0)
                        {
                            target.AddPoisonState(attacker, nPosionDamage * nDamagePercent / 100, cPoisonDamage.nValue[1]);
                        }
                    }
                }
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_magic))
            {
                KMagicAttrib cMagicDamage = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_magic];

                if (!bIsOver)
                    bIsOver = !CalcDamage(attacker, target, cMagicDamage.nValue[0], cMagicDamage.nValue[2], DAMAGE_TYPE.damage_magic, bIsMelee, CALC_DAMAGE_PARAM, false, nDamagePercent, isSubPhrase);
            }
            // Nếu phòng thủ bị giảm thì thêm
            if (nTempDecRes != 0)
            {
                target.m_damage[(int)eDamageType].AddCurResist(nTempDecRes);
                nTempDecRes = 0;
            }

            // Hút sinh lực
            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_steallife))
            {
                KMagicAttrib cStealLife = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_steallife];

                int nRate = cStealLife.nValue[0];
                ///// Nếu giá trị hút dương hoặc thằng tấn công là người, đánh vào người
                //if (nRate > 0 || (attacker is KPlayer && target is KPlayer))
                {
                    // Nếu giá trị hút dương
                    if (nRate > 0)
                    {
                        int RandomSteal = KTGlobal.GetRandomNumber(0, 99);
                        if (RandomSteal < cStealLife.nValue[1])
                        {
                            attacker.m_CurrentLife += (nLife - target.m_CurrentLife) * nRate / 100;
                            if (attacker.m_CurrentLife > attacker.m_CurrentLifeMax)
                            {
                                attacker.m_CurrentLife = attacker.m_CurrentLifeMax;
                            }
                        }
                    }
                    ///// Nếu là người đánh người
                    //else
                    //{
                    //    int nRandomSteal = KTGlobal.GetRandomNumber(0, 99);
                    //    if (nRandomSteal < cStealLife.nValue[1])
                    //    {
                    //        /// Lượng máu cũ
                    //        int nOldLife = attacker.m_CurrentLife;
                    //        /// Sát thương gây ra
                    //        int nReflectDamage = (nLife - target.m_CurrentLife) * nRate / 100;
                    //        AlgorithmProperty.CalcDamage(target, attacker, nReflectDamage, nReflectDamage, DAMAGE_TYPE.damage_return, false, null, false, 100, true);
                    //        AlgorithmProperty.SyncDamage(target, attacker, nOldLife - attacker.m_CurrentLife);

                    //        /// Sói Fix BUG chết không hiện khung
                    //        if (attacker.m_CurrentLife <= 0)
                    //        {
                    //            attacker.m_CurrentLife = 0;
                    //            attacker.DoDeath(target);
                    //            return true;
                    //        }
                    //    }
                    //}
                }
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_stealmana))
            {
                KMagicAttrib cStealMana = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_stealmana];

                int nRate = cStealMana.nValue[0];
                if (nRate > 0)
                {
                    int RandomSteal = KTGlobal.GetRandomNumber(0, 99);

                    if (cStealMana.nValue[1] > RandomSteal)
                    {
                        attacker.m_CurrentMana += (nLife - target.m_CurrentLife) * nRate / 100;
                        if (attacker.m_CurrentMana > attacker.m_CurrentManaMax)
                            attacker.m_CurrentMana = attacker.m_CurrentManaMax;
                    }
                }
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_stealstamina))
            {
                KMagicAttrib cStealStamina = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_stealstamina];

                int nRate = cStealStamina.nValue[0];
                if (nRate > 0)
                {
                    int RandomSteal = KTGlobal.GetRandomNumber(0, 99);

                    if (cStealStamina.nValue[1] > RandomSteal)
                    {
                        attacker.m_CurrentStamina += (nLife - target.m_CurrentLife) * nRate / 100;
                        if (attacker.m_CurrentStamina > attacker.m_CurrentStaminaMax)
                            attacker.m_CurrentStamina = attacker.m_CurrentStaminaMax;
                    }
                }
            }

            if (AppendDameDic.ContainsKey(MAGIC_ATTRIB.magic_damage_append_stealstate))
            {
                KMagicAttrib cStealState = AppendDameDic[MAGIC_ATTRIB.magic_damage_append_stealstate];

                int nRate = cStealState.nValue[1];

                if (nRate != 0)
                {
                    bool bSucSteal = false;

                    int RandomSteal = KTGlobal.GetRandomNumber(0, 99);

                    if (RandomSteal < nRate)
                    {
                        // nếu mà cường háo trạng thái thành công thì
                    }
                }
            }

            int actualReceivedDamage = Math.Max(0, nLife - Math.Max(0, target.m_CurrentLife));
            if (actualReceivedDamage > 0)
            {
                ApplyBatQuaiKinhPhapEffects(attacker, target, rcSkill, actualReceivedDamage);
            }

            bool bDeath = target.IsDead();

            if (!bDeath && !bIsOver && !isSubPhrase)
            {
                for (int i = 0; i < AppendDameDic.Count; ++i)
                {
                    if (target.IsDead())
                        break;

                    KMagicAttrib cState = AppendDameDic.Values.ElementAt(i);
                    if (cState == null)
                    {
                        continue;
                    }

                    MAGIC_ATTRIB Atribute = AppendDameDic.Keys.ElementAt(i);

                    KE_STATE state = ConvertSkillEffectToState(Atribute);

                    if (cState.nValue[0] != 0)
                    {
                        // SYSNS SATE VỀ CLIENT
                        KSpecialStateManager.OnReceiveState(target, cState, state, attacker, rcSkill, skillPos, SkillPD);
                    }
                }
            }

            if (target.m_CurrentLife < nLife)
            {
                if (bTrace)
                {
                    TraceDamage($"ReceiveDamage KẾT QUẢ: attacker={attacker.RoleName}[{attacker.RoleID}] gây tổng {nLife - target.m_CurrentLife} damage lên {target.RoleName}[{target.RoleID}] (HP {nLife} -> {target.m_CurrentLife}/{target.m_CurrentLifeMax}) crit={bIsDeadlyStrike} chết={target.IsDead()}");
                }

                // Nếu là đòn chí mạng
                if (bIsDeadlyStrike)
                {
                    // Thực hiện sự kiện gọi khi đánh chí mạng
                    attacker.OnDoCritOnTarget(target);
                    AlgorithmProperty.SynDeadly(attacker, target, nLife - target.m_CurrentLife);
                }
                else
                {
                    // SYSN Dame Về
                    AlgorithmProperty.SyncDamage(attacker, target, nLife - target.m_CurrentLife);
                }

                // Nếu là đối tượng bị đánh là monster | Lưu lại thằng nào gây damge
                if (target is Monster monster && attacker is KPlayer player)
                {


                    //Chỉ có boss mới ghi tích lũy damge cho nó nhẹ
                    if (monster.MonsterType == MonsterAIType.Boss)
                    {
                        int nTeamID = player.TeamID;
                        // thêm tính damage theo ID player cá nhân
                        if (BossUnicon.IsBoss(monster.MonsterInfo.ExtensionID))
                        {
                            int nGuildID = player.GuildID;

                            ((Monster)target).AddDmageGuild(attacker.RoleID, nLife - target.m_CurrentLife, nGuildID);
                        }

                        // if (EmperorTomb.IsMiniBoss(_SelectMonster.MonsterInfo.ExtensionID)) 
                        // {
                        //     FlowerTowerManager.UpdateTaskProgress(attacker as KPlayer, 18, nLife - target.m_CurrentLife);
                        // }

                        if (Event_2024Apr.IsEventBoss(monster.MonsterInfo.ExtensionID))
                        {
                            // event tháng 4 2024
                            // chỉ tính damage theo cá nhân
                            ((Monster)target).AddDmageToQueue(attacker.RoleID, nLife - target.m_CurrentLife, false);
                            Event_2024Apr.AddNameLookup(attacker.RoleID, attacker.RoleName);
                        }
                        else
                        {
                            // Nếu thằng tấn công có đội thì ID add vào QUEUE = ID đội
                            if (nTeamID != -1)
                            {
                                ((Monster)target).AddDmageToQueue(nTeamID, nLife - target.m_CurrentLife, true);
                            }
                            else
                            {
                                ((Monster)target).AddDmageToQueue(attacker.RoleID, nLife - target.m_CurrentLife, false);
                            }   
                        }

                        GlobalEventSource.getInstance().fireEvent(new MonsterBloodChangeEventObject(monster, player, nLife, monster.m_CurrentLife));
                    }
                }
            }

            // Thực thi Buff tương ứng nếu có Symbol magic_skill_statetime
            if (SkillPD.ContainsKey((int)MAGIC_ATTRIB.magic_skill_statetime))
            {
                KMagicAttrib magicAttrib = SkillPD.Get<KMagicAttrib>((int)MAGIC_ATTRIB.magic_skill_statetime);
                var buffDurationTick = magicAttrib.nValue[0] * 1000L / 18;
                BuffDataEx buff = new BuffDataEx()
                {
                    Skill = rcSkill,
                    Duration = buffDurationTick,
                    LoseWhenUsingSkill = false,
                    SaveToDB = false,
                    StartTick = KTGlobal.GetCurrentTimeMilis(),
                };
                // Nếu không phải bùa chú
                if (buff.Skill.Data.TargetType == "self" || buff.Skill.Data.TargetType == "selfnothide" || (buff.Skill.Data.TargetType == "ally" || buff.Skill.Data.TargetType == "team" || buff.Skill.Data.TargetType == "teamnoself" || buff.Skill.Data.TargetType == "allyandnpc" || buff.Skill.Data.TargetType == "allynoself" || buff.Skill.Data.TargetType == "npcteamnoself") || (buff.Skill.Data.TargetType == "camp") || buff.Skill.Data.TargetType == "revivable")
                {
                    buff.CurseOwner = null;
                    // Thêm Buff vào mục tiêu
                    target.Buffs.AddBuff(buff);
                }
                // Nếu là bùa chú
                else
                {
                    // LogManager.WriteLog(LogTypes.Skill, $"SkillID={rcSkill.SkillID} bước 1");
                    /// nếu mà là bùa chú tạo mộc nhân thì
                    if (buff.Skill.Data.SkillStyle == "nonpcskill" && target is KPlayer && !target.Buffs.HasBuff(rcSkill.SkillID))
                    {
                        // LogManager.WriteLog(LogTypes.Skill, $"SkillID={rcSkill.SkillID} bước 2");
                        /// add buff vào cho target
                        target.Buffs.AddBuff(buff);
                        /// tạo 1 con mộc nhân bằng máu, ngũ hành như địch 
                        /// 
                        KiemThe.Entities.Direction dir = target.CurrentDir;
                        int hpMonster = target.m_CurrentLifeMax * 30 / 100;
                        KE_SERIES_TYPE seriMonster = target.m_Series;
                        int sumHpMinus = 0;
                        GameManager.MonsterZoneMgr.AddDynamicMonsters(target.CurrentMapCode, 22646, -1, 1, (int) target.CurrentPos.X, (int) target.CurrentPos.Y, $"Mộc Nhân", target.RoleName, hpMonster, target.m_Level, dir, seriMonster, MonsterAIType.Normal, -1, -1, 100001, "MocNhan", (monster) =>
                        {
                            /// nếu quái mất máu thì người mất máu theo
                            monster.OnTick = () =>
                            {
                                float distance = KTGlobal.GetDistanceBetweenPoints(monster.CurrentPos, target.CurrentPos);
                                LogManager.WriteLog(LogTypes.Skill, $"Target {target.RoleName}");
                                if (distance > 1000)
                                {
                                    target.m_CurrentLife -= monster.m_CurrentLife;
                                    monster.DoDeath(target);
                                }    
                                else
                                {
                                    if (target.Buffs.HasBuff(rcSkill.SkillID))
                                    {
                                        int minusHP = hpMonster - monster.m_CurrentLife;
                                        if (target.m_CurrentLife > 0)
                                        {
                                            target.m_CurrentLife -= minusHP;
                                            sumHpMinus += minusHP;
                                        }
                                    }
                                    else
                                    {
                                        monster.DoDeath(target);
                                    }
                                }
                            };
                        }, 65535, null, (_GameObject) =>
                        {
                            hpMonster -= sumHpMinus;
                            if (_GameObject.RoleID != target.RoleID && target.m_CurrentLife > 0 && hpMonster > 0)
                            {
                                target.m_CurrentLife -= hpMonster;
                            }
                        });
                    }
                    else
                    {
                        // Tỷ lệ phản đòn bùa chú của đối phương
                        int targetReflectCurseP = target.m_CurrentReturnSkillPercent;
                        // Tỷ lệ ngẫu nhiên
                        int nPercent = KTGlobal.GetRandomNumber(1, 100);
                        // Nếu có thể phản đòn bùa chú
                        if (nPercent <= targetReflectCurseP)
                        {
                            // Đánh dấu đối tượng gây trạng thái bất lợi này
                            buff.CurseOwner = target;
                            attacker.Buffs.AddBuff(buff);
                        }
                        // Nếu không thể phản đòn bùa chú
                        else
                        {
                            // Đánh dấu đối tượng gây trạng thái bất lợi này
                            buff.CurseOwner = attacker;
                            target.Buffs.AddBuff(buff);
                        }
                    }    
                    
                }
            }
            // Thực hiện hàm gọi đến khi bị tấn công bởi kỹ năng
            target.OnBeHitBySkill(attacker, rcSkill);
            // Thực hiện hàm gọi đến khi tấn công đối tượng khác bằng kỹ năng tương ứng
            if (!isSubPhrase && target.m_CurrentLife < nLife)
                CoMoEffects.ApplyHitDebuff(attacker, target, rcSkill);
            attacker.OnHitTargetWithSkill(target, rcSkill);

            return true;
        }

        /// <summary>
        /// Gửi gói tin thông báo Crit về Client
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        /// <param name="damage"></param>
        private static void SynDeadly(GameObject attacker, GameObject target, int damage)
        {
            // Gửi gói tin sát thương về Client
            KTSkillManager.AppendSkillResult(attacker, target, SkillResult.Crit, damage);

            // Thông báo đối tượng vừa tấn công
            target.TakeDamage(attacker, damage);

            // Gọi hàm thực thi sự kiện khi đối tượng mất máu
            target.OnHPDropped(attacker);
        }

        /// <summary>
        /// Tính sát thương gây ra
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <param name="nType"></param>
        /// <param name="bIsMelee"></param>
        /// <param name="pCALCDamage_Param"></param>
        /// <param name="bStateHurt"></param>
        /// <param name="nDamagePercent"></param>
        /// <param name="isSubPhrase"></param>
        /// <returns></returns>
        public static bool CalcDamage(GameObject attacker, GameObject target, int nMin, int nMax, DAMAGE_TYPE nType, bool bIsMelee, NPC_CALC_DAMAGE_PARAM pCALCDamage_Param, bool bStateHurt, int nDamagePercent, bool isSubPhrase)
        {
            try
            {
                // Nếu không có Attacker
                if (attacker == null)
                {
                    return false;
                }

                // if (attacker != null && target != null && attacker.IsPlayer() && target.IsPlayer() && (nMin + nMax) / 2 > ServerConfig.Instance.DamageLogThreshold)
                // // if (attacker != null && target != null && (nMin + nMax) / 2 > ServerConfig.Instance.DamageLogThreshold) 
                // {
                //     if (pCALCDamage_Param == null) 
                //         LogManager.WriteLog(LogTypes.Skill,
                //             $"{attacker.RoleName}[{attacker.RoleID}] gây damage [{nMin} - {nMax}] cho {target.RoleID}[{target.RoleName}], loại {nType}, param null, null, {bIsMelee}, {bStateHurt}, {nDamagePercent}, {isSubPhrase}");
                //     else 
                //         LogManager.WriteLog(LogTypes.Skill,
                //             $"{attacker.RoleName}[{attacker.RoleID}] gây damage [{nMin} - {nMax}] cho {target.RoleID}[{target.RoleName}], loại {nType}, param {pCALCDamage_Param.nTotalDamage}, {pCALCDamage_Param.nSeriesConquarRes}, {bIsMelee}, {bStateHurt}, {nDamagePercent}, {isSubPhrase}");
                // }

                nMin = nMin * nDamagePercent / 100;
                nMax = nMax * nDamagePercent / 100;

                // [DEBUG] Trace cách tính damage nếu target nằm trong PacketMonitoringUsers.xml và bị NGƯỜI CHƠI tấn công
                bool bTrace = AlgorithmProperty.ShouldTraceDamage(target) && attacker.IsPlayer() && nMin + nMax > 0;
                if (bTrace)
                {
                    TraceDamage($"CalcDamage BEGIN: type={nType} min={nMin} max={nMax} (nDamagePercent={nDamagePercent}) melee={bIsMelee} stateHurt={bStateHurt} isSubPhrase={isSubPhrase} | attacker={attacker.RoleName}[{attacker.RoleID}]");
                }

                // Nếu đối tượng đã chết hoặc đang bất tử, hoặc đang ở chế độ hòa bình
                if (target.IsDead() || target.m_CurrentInvincibility == 1 || (target is KPlayer player && player.GM_Immortality) || (attacker.IsPlayer() && target.IsPlayer() && !KTLogic.IsOpposite(attacker, target)))
                {
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage EXIT: target chết/bất tử/hòa bình (dead={target.IsDead()} invincibility={target.m_CurrentInvincibility}) => không nhận damage");
                    }
                    return false;
                }

                // Nếu đang miễn nhiễm sát thương
                if (target.IsPlayer() && attacker.IsPlayer() && (target as KPlayer).IsImmuneToAllDamagesOf(attacker as KPlayer))
                {
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage EXIT: target miễn nhiễm toàn bộ sát thương từ attacker này => không nhận damage");
                    }
                    return false;
                }

                if (nMin + nMax <= 0)
                    return true;

                int nAvgDamage = (nMin + nMax) / 2;
                if (nAvgDamage < 0)
                    nAvgDamage = 0;

                int nDamageRange = nMax - nMin;
                int nDamage = 0;
                if (nDamageRange < 0)
                    nDamage = nMax + KTGlobal.GetRandomNumber(-nDamageRange, 0);
                else
                    nDamage = nMin + KTGlobal.GetRandomNumber(0, nDamageRange);

                if (nDamage <= 0)
                    return true;

                //LogManager.WriteLog(LogTypes.Fatal, "Damage Type: [" + nType + "], " + nDamage + "");

                int nReceivePercent = 100;
                if (nType != DAMAGE_TYPE.damage_poison)
                {
                    nReceivePercent = CalcReceivePercent(pCALCDamage_Param == null ? 0 : pCALCDamage_Param.nSeriesConquarRes, attacker, target, nType, target.m_damage[(int)nType]);
                }
                if (nReceivePercent <= 0)
                {
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage EXIT: type={nType} kháng hấp thụ toàn bộ (nReceivePercent={nReceivePercent}, targetResist={target.m_damage[(int)nType].GetCurResist()})");
                    }
                    return true;
                }

                if (target.HaveState(KE_STATE.emSTATE_BURN))
                {
                    int nNewPercent = nReceivePercent * KTGlobal.BurnDamagePercent / 100;

                    //LogManager.WriteLog(LogTypes.Fatal, "BurnState ReceivePercent: " + nReceivePercent + " => " + nNewPercent + "");
                    nReceivePercent = nNewPercent;
                }

                if (bTrace)
                {
                    // Sát thương độc không dùng kháng tại đây (đã trừ trước ở ReceiveDamage) => không in resist để tránh gây hiểu nhầm
                    string traceResist = nType != DAMAGE_TYPE.damage_poison ? $"targetResist={target.m_damage[(int)nType].GetCurResist()}, " : "";
                    TraceDamage($"CalcDamage: type={nType} roll={nDamage} (min={nMin}, max={nMax}) nReceivePercent={nReceivePercent} ({traceResist}burn={target.HaveState(KE_STATE.emSTATE_BURN)}) => damage={((nDamage > 1000000) ? (nDamage / 100 * nReceivePercent) : (nDamage * nReceivePercent / 100))}");
                }

                if (nDamage > 1000000)
                    nDamage = nDamage / 100 * nReceivePercent;
                else
                    nDamage = nDamage * nReceivePercent / 100;

                if (nDamage <= 0)
                    return true;

                // Nếu không phải là phản Damage
                if (nType != DAMAGE_TYPE.damage_return)
                {
                    int nRetPercent = 0, nRetPoint = 0;
                    if (bStateHurt)
                    {
                        if (nType == DAMAGE_TYPE.damage_poison) // Nếu là phản lại dame độc
                        {
                            nRetPercent = target.m_CurrentPoisonDmgRetPercent;
                            nRetPoint = target.m_CurrentPoisonDmgRet;
                        }
                    }
                    else if (bIsMelee)  // Phản lại dame cận chiến
                    {
                        nRetPercent = target.m_CurrentMeleeDmgRetPercent;
                        nRetPoint = target.m_CurrentMeleeDmgRet;
                    }
                    else   // Phản lại dame tầm xa
                    {
                        nRetPercent = target.m_CurrentRangeDmgRetPercent;
                        nRetPoint = target.m_CurrentRangeDmgRet;
                    }
                    // Nếu bản thân có bùa bị phản sát thương khi tấn công mục tiêu
                    if (attacker.m_sReflectDamageWhenHitTargetID != -1 && attacker.m_sReflectDamageWhenHit > 0)
                    {
                        // Nếu đối phương là mục tiêu bị tấn công
                        if (target.RoleID == attacker.m_sReflectDamageWhenHitTargetID)
                        {
                            // Cộng thêm vào lượng phản
                            nRetPercent += attacker.m_sReflectDamageWhenHit;
                        }
                    }

                    // Lượng phản lại
                    nRetPoint += nDamage * nRetPercent / 100;
                    // Nếu bản thân có bỏ qua phản đòn thì 
                    if (attacker.m_sIgnoreReflectDamage > 0)
                    {
                        nRetPoint *= (100 - attacker.m_sIgnoreReflectDamage);
                    }    

                    if (nRetPoint > 0)
                    {
                        int nOldLife = attacker.m_CurrentLife;

                        if (bTrace)
                        {
                            TraceDamage($"CalcDamage: target phản đòn {nRetPoint} damage lại attacker (retPercent={nRetPercent}, stateHurt={bStateHurt}, melee={bIsMelee})");
                        }

                        AlgorithmProperty.CalcDamage(target, attacker, nRetPoint, nRetPoint, DAMAGE_TYPE.damage_return, false, null, false, 100, true);
                        AlgorithmProperty.SyncDamage(target, attacker, nOldLife - attacker.m_CurrentLife);

                        // Sói Fix BUG chết không hiện khung
                        if (attacker.m_CurrentLife <= 0)
                        {
                            attacker.m_CurrentLife = 0;
                            attacker.DoDeath(target);
                            return true;
                        }
                    }
                }

                // Xử lý PK Dame nhân thêm 1 hệ số
                if ((target.m_Kind == NPCKIND.kind_player || target.m_Kind == NPCKIND.kind_partner) &&
                    (attacker.m_Kind == NPCKIND.kind_player || attacker.m_Kind == NPCKIND.kind_partner))
                {
                    nDamage = nDamage * KTGlobal.PKDamageRate / 100;
                    nDamage = nDamage > 0 ? nDamage : 1;
                }

                if (target.m_Kind != NPCKIND.kind_player && attacker.m_Kind != NPCKIND.kind_player) // Nếu là 2 con quái có quan hệ khác phhe
                {
                    //if (GetVirtualRelation().eRelationType == Npc[nAttacker].GetVirtualRelation().eRelationType && GetVirtualRelation().eRelationType == emNPCVRELATIONTYPE_TONE) // CHEKC PHE CHECK SAU
                    {
                        nDamage = nDamage * KTGlobal.NpcPKDamageRate / 100;
                    }
                }

                ///// Test đấm phát chết luôn
                //if (target.IsPlayer())
                //{
                //    nDamage = 1000000;
                //}

                // Nếu là người đánh người thì sát thương giảm 4 lần
                if (target is KPlayer && attacker is KPlayer)
                {
                    if (GameManager.ServerId == 9001) {
                        nDamage /= 6;
                    } else {
                       nDamage /= 5;
                    }
                }
                
                // nếu là quái đánh người và quái cấp tân thủ thì sát thương giảm 2 lần (hỗ trợ tân thủ)
                if (target is KPlayer && attacker is Monster && attacker.m_Level <= 15)
                {
                    nDamage /= 2;
                }

                // nếu là boss đạo tặc đánh người chơi thì giảm sức mạnh
                if (target is KPlayer && attacker is Monster bossDaoTac && bossDaoTac.GetMonsterData().ExtensionID == Event_2024Apr.BossIDFree && (GameManager.ServerId == 21 || GameManager.ServerId == 47))
                {
                    nDamage /= 10;
                }

                if (attacker is KPlayer && !isSubPhrase)
                {
                    var percent = attacker.m_IncreaseOutputDamageP;
                    if (percent > 0) {
                        nDamage += nDamage * percent / 100;
                    }
                }

                if (target is KPlayer)
                {
                    var percent = isSubPhrase ? CoMoEffects.SecondaryDamageReduction(target) : target.m_ReduceReceivedDamageP;
                    if (percent >= 100)
                    {
                        percent = 99;
                    }
                    if (percent > 0)
                    {
                        nDamage -= nDamage * percent / 100;
                    }

                    if (nDamage <= 0)
                        nDamage = 1;
                }

                // [DEBUG] Log damage sau các hệ số PK/PvP/tăng-giảm damage (trước khi trừ khiên)
                if (bTrace)
                {
                    TraceDamage($"CalcDamage: type={nType} damage sau hệ số PK/PvP={nDamage} (PKDamageRate={KTGlobal.PKDamageRate}, attackerIncreaseP={attacker.m_IncreaseOutputDamageP}, targetReduceP={target.m_ReduceReceivedDamageP})");
                }

                ///// Nếu là quái thì sát thương giảm 3 lần
                //else
                //{
                //    nDamage /= 3;
                //}

                /// Vừa bị hút mana vừa trúng độc
                if (target.m_CurrentPoison2Mana > 0 && nType == DAMAGE_TYPE.damage_poison)
                {
                    target.m_CurrentMana -= target.m_CurrentPoison2Mana * nDamage / 100;
                    if (target.m_CurrentMana > target.m_CurrentManaMax)
                    {
                        target.m_CurrentMana = target.m_CurrentManaMax;
                    }
                    else if (target.m_CurrentMana < 0)
                    {
                        target.m_CurrentMana = 0;
                    }
                }

                // Nếu có hiệu ứng hóa giải % sát thương (SÓI VIẾT THÊM)
                if (target.m_CurrentDynamicShield > 0 && target.m_CurrentDynamicShieldMaxP > 0)
                {
                    // Điểm sát thương hóa giải
                    int nShield = target.m_CurrentDynamicShield;
                    // Tối đa sát thương ban đầu có thể hóa giải
                    int maxShieldAbsorbDamage = nDamage * target.m_CurrentDynamicShieldMaxP / 100;
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage: khiên hóa giải hấp thụ {Math.Min(nShield, maxShieldAbsorbDamage)} damage (shield={nShield}, maxP={target.m_CurrentDynamicShieldMaxP}%)");
                    }
                    // Tính sát thương hóa giải
                    nDamage -= Math.Min(nShield, maxShieldAbsorbDamage);
                    // Nếu không còn sát thương
                    if (nDamage <= 0)
                    {
                        //LogManager.WriteLog(LogTypes.Warning, string.Format("{0}'s damage taken = 0, by dynamicshield", target.RoleName));
                        // Thực hiện hàm gọi đến khi tấn công mục tiêu
                        attacker.OnHitTarget(target, nDamage);
                        // Thực hiện hàm gọi đến khi bị tấn công
                        target.OnBeHit(attacker, nDamage);
                        return false;
                    }
                }

                // Nếu có khiên nội lực (SÓI VIẾT LẠI)
                if (target.m_CurrentDynamicMagicShield > 0)
                {
                    // Sinh mệnh khiên nội lực
                    int nShield = target.m_CurrentDynamicMagicShield;
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage: khiên nội lực hấp thụ {Math.Min(nShield, nDamage)} damage (shield={nShield})");
                    }
                    // Nếu sinh mệnh của khiên lớn hơn sát thương phải chịu
                    if (nShield >= nDamage)
                    {
                        nShield -= nDamage;
                        nDamage = 0;
                    }
                    else
                    {
                        nDamage -= nShield;
                        nShield = 0;
                    }

                    // Cập nhật sinh mệnh cho khiên
                    target.m_CurrentDynamicMagicShield = nShield;

                    //if (target is KPlayer)
                    //{
                    //    PlayerManager.ShowNotification(target as KPlayer, "Sinh mệnh khiên nội lực -> " + nShield);
                    //}

                    // Nếu khiên bị phá
                    if (nShield <= 0)
                    {
                        // Hủy hiệu ứng tương ứng
                        target.Buffs.RemoveBuff(target.m_CurrentDynamicMagicShield_SkillID);
                    }
                }

                // Sát thương chuyển hóa thành nội lực (SÓI VIẾT LẠI)
                int nManaDamage = 0;
                if (target.m_CurrentManaShield > 0)
                {
                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage: khiên chuyển hóa nội lực (manaShield={target.m_CurrentManaShield}%, mana={target.m_CurrentMana}/{target.m_CurrentManaMax}) damage trước khi hấp thụ={nDamage}");
                    }
                    // Lượng sát thương tối đa sẽ hấp thụ được
                    nManaDamage = nDamage * target.m_CurrentManaShield / 100;
                    // Nếu lượng sát thương vào nội lực dưới 1
                    if (nManaDamage < 1)
                    {
                        nManaDamage = 1;
                    }

                    // % nội lực hiện tại
                    int nManaPercent = target.m_CurrentMana * 100 / target.m_CurrentManaMax;
                    // Nếu lượng nội lực hiện tại của đối tượng trên 15%
                    if (nManaPercent >= 15)
                    {
                        // Nếu lượng nội lực hiện tại của đối tượng nhỏ hơn sát thương sẽ hấp thụ được
                        if (nManaDamage > target.m_CurrentMana)
                        {
                            nManaDamage = target.m_CurrentMana;
                        }

                        // Giảm sát thương tương đương nội lực đã mất
                        nDamage -= nManaDamage;
                        // Nội lực trước đó
                        int manaBefore = target.m_CurrentMana;
                        // Giảm nội lực tương ứng của đối tượng
                        target.m_CurrentMana -= nManaDamage;
                    }

                    ///// Nếu % nội lực hiện tại dưới 15%
                    //if (nManaPercent < 15)
                    //{
                    //    /// Hủy Buff tương ứng dùng nội lực hút sát thương
                    //    target.Buffs.RemoveBuff(target.m_CurrentManaShield_SkillID);
                    //}

                    //if (target is KPlayer)
                    //{
                    //    PlayerManager.ShowNotification(target as KPlayer, "Nội lực đỡ sát thương -> " + nManaDamage + " - Mana Before = " + manaBefore + " - Mana After = " + target.m_CurrentMana);
                    //}


                    // Nếu hết sát thương
                    if (nDamage <= 0)
                    {
                        //LogManager.WriteLog(LogTypes.Warning, string.Format("{0}'s damage taken = 0, by manashield", target.RoleName));

                        // Nếu không phải damage bị phản
                        if (nType != DAMAGE_TYPE.damage_return)
                        {
                            // Thực hiện hàm gọi đến khi tấn công mục tiêu
                            attacker.OnHitTarget(target, nDamage);
                            // Thực hiện hàm gọi đến khi bị tấn công
                            target.OnBeHit(attacker, nDamage);
                        }

                        return false;
                    }
                }

                // if (nDamage > ServerConfig.Instance.DamageLogThreshold)
                // {
                //     if (attacker != null && target != null && attacker.IsPlayer() && target.IsPlayer())
                //     {
                //         LogManager.WriteLog(LogTypes.Skill,
                //             $"{attacker.RoleName}[{attacker.RoleID}] gây damage [{nDamage}] cho {target.RoleID}[{target.RoleName}]");
                //     }

                //     var targetMonster = target as Monster;
                //     if (attacker != null && targetMonster != null && attacker.IsPlayer() && (targetMonster.IsEmperorTombBoss() || targetMonster.IsWorldBoss() || targetMonster.IsBaiHutang3Boss()))
                //     {
                //         LogManager.WriteLog(LogTypes.Skill,
                //             $"{attacker.RoleName}[{attacker.RoleID}] gây damage [{nDamage}] cho {target.RoleID}[{target.RoleName}]");
                //     }
                // }

                // Nếu có sát thương
                if (nDamage > 0)
                {
                    // Nếu lượng sát thương vượt quá sinh mệnh của mục tiêu hiện tại
                    if (nDamage > target.m_CurrentLife)
                    {
                        nDamage = target.m_CurrentLife;
                    }

                    if (bTrace)
                    {
                        TraceDamage($"CalcDamage KẾT QUẢ: type={nType} damage cuối={nDamage} trừ vào HP {target.m_CurrentLife} -> {target.m_CurrentLife - nDamage} của {target.RoleName}[{target.RoleID}]");
                    }

                    target.m_CurrentLife -= nDamage;

                    // Nếu không phản damage bị phản
                    if (nType != DAMAGE_TYPE.damage_return && !isSubPhrase)
                    {
                        // Thực hiện hàm gọi đến khi tấn công mục tiêu
                        attacker.OnHitTarget(target, nDamage);
                        // Thực hiện hàm gọi đến khi bị tấn công
                        target.OnBeHit(attacker, nDamage);
                    }
                }

                // Nếu máu mục tiêu về 0 tức đã chết
                if (target.m_CurrentLife <= 0)
                {
                    
                    if (target is Monster monster && attacker is KPlayer)
                    {
                        // nếu quái đã chết thì ghi nhận damage cuối cùng bằng chỉ số HP của quái trước khi chết
                        if (monster.MonsterType == MonsterAIType.Boss)
                        {
                            if (Event_2024Apr.IsEventBoss(monster.MonsterInfo.ExtensionID))
                            {
                                // event tháng 4 2024
                                // chỉ tính damage theo cá nhân
                                monster.AddDmageToQueue(attacker.RoleID, target.m_CurrentLife + nDamage, false);
                                // Event_2024Apr.SaveDamageRecord(monster.Tag, monster.DamageTakeRecord);
                            }
                        }
                    }
                    
                    target.m_CurrentLife = 0;
                    target.DoDeath(attacker);
                }
                // Nếu máu mục tiêu lớn hơn máu tối đa
                else if (target.m_CurrentLife > target.m_CurrentLifeMax)
                {
                    target.m_CurrentLife = target.m_CurrentLifeMax;
                }

                return true;
            }
            catch (Exception exx)
            {
                LogManager.WriteLog(LogTypes.Exception, exx.ToString());
                return false;
            }
        }

        /// <summary>
        /// Gửi lại cho tằng target lượng dame gây ra do phản dame
        /// </summary>
        /// <param name="target"></param>
        /// <param name="attacker"></param>
        /// <param name="damage"></param>
        public static void SyncDamage(GameObject attacker, GameObject target, int damage)
        {
            //PlayerManager.ShowNotification(attacker as KPlayer, "Damage = " + damage);

            // Gửi gói tin sát thương về Client
            KTSkillManager.AppendSkillResult(attacker, target, SkillResult.Normal, damage);

            // Nếu đối tượng còn tồn tại
            if (target != null && !target.IsDead())
            {
                // Thông báo đối tượng vừa tấn công
                target.TakeDamage(attacker, damage);
                // Gọi hàm thực thi sự kiện khi đối tượng mất máu
                target.OnHPDropped(attacker);
            }

        }

        /// <summary>
        /// Thông báo cho người tấn công là đánh mục tiêu miss
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        private static void SyncMiss(GameObject attacker, GameObject target)
        {
            // Gửi gói tin sát thương về Client
            KTSkillManager.AppendSkillResult(attacker, target, SkillResult.Miss, 0);

            // Thông báo đối tượng vừa tấn công
            target.TakeDamage(attacker, 0);
        }

        /// <summary>
        /// Thông báo cho người tấn công là đánh mục tiêu bị hóa giải
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        private static void SyncAdjust(GameObject attacker, GameObject target)
        {
            // Gửi gói tin sát thương về Client
            KTSkillManager.AppendSkillResult(attacker, target, SkillResult.Adjust, 0);

            // Thông báo đối tượng vừa tấn công
            target.TakeDamage(attacker, 0);
        }

        /// <summary>
        /// Chuyển trạng thái ngũ hành kỹ năng sang đối tượng KE_STATE
        /// </summary>
        /// <param name="arrt"></param>
        /// <returns></returns>
        public static KE_STATE ConvertSkillEffectToState(MAGIC_ATTRIB arrt)
        {
            KE_STATE _State = KE_STATE.emSTATE_BEGIN;

            switch (arrt)
            {
                case MAGIC_ATTRIB.state_hurt_attack:
                    _State = KE_STATE.emSTATE_HURT;
                    break;

                case MAGIC_ATTRIB.state_slowall_attack:
                    _State = KE_STATE.emSTATE_SLOWALL;
                    break;

                case MAGIC_ATTRIB.state_burn_attack:
                    _State = KE_STATE.emSTATE_BURN;
                    break;

                case MAGIC_ATTRIB.state_stun_attack:
                    _State = KE_STATE.emSTATE_STUN;
                    break;

                case MAGIC_ATTRIB.state_fixed_attack:
                    _State = KE_STATE.emSTATE_FIXED;
                    break;

                case MAGIC_ATTRIB.state_palsy_attack:
                    _State = KE_STATE.emSTATE_PALSY;
                    break;

                case MAGIC_ATTRIB.state_slowrun_attack:
                    _State = KE_STATE.emSTATE_SLOWRUN;
                    break;

                case MAGIC_ATTRIB.state_freeze_attack:
                    _State = KE_STATE.emSTATE_FREEZE;
                    break;

                case MAGIC_ATTRIB.state_confuse_attack:
                    _State = KE_STATE.emSTATE_CONFUSE;
                    break;

                case MAGIC_ATTRIB.state_knock_attack:
                    _State = KE_STATE.emSTATE_KNOCK;
                    break;

                case MAGIC_ATTRIB.state_drag_attack:
                    _State = KE_STATE.emSTATE_DRAG;
                    break;

                case MAGIC_ATTRIB.state_silence_attack:
                    _State = KE_STATE.emSTATE_SILENCE;
                    break;

                case MAGIC_ATTRIB.state_zhican_attack:
                    _State = KE_STATE.emSTATE_ZHICAN;
                    break;

                case MAGIC_ATTRIB.state_float_attack:
                    _State = KE_STATE.emSTATE_FLOAT;
                    break;

                case MAGIC_ATTRIB.state_weak_attack:
                    _State = KE_STATE.emSTATE_WEAK;
                    break;
            }

            return _State;
        }

        /// <summary>
        /// Trả về sát thương nhận được tính kèm cả ngũ hành
        /// </summary>
        /// <param name="nSeriesConquarRes"></param>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        /// <param name="nType"></param>
        /// <param name="damageGroup"></param>
        /// <returns></returns>
        public static int CalcReceivePercent(int nSeriesConquarRes, GameObject attacker, GameObject target, DAMAGE_TYPE nType, KNpcAttribGroup_Damage damageGroup)
        {
            // [DEBUG] Trace cách tính damage nếu target nằm trong PacketMonitoringUsers.xml và bị NGƯỜI CHƠI tấn công
            bool bTrace = AlgorithmProperty.ShouldTraceDamage(target) && attacker.IsPlayer();

            // Nếu không phải sát thương phản lại
            if (nType != DAMAGE_TYPE.damage_return)
            {
                // Tổng kháng của mục tiêu
                int nAllRes = nSeriesConquarRes + damageGroup.m_nCurResist;
                if (nAllRes > 0)
                    nAllRes = CoMoEffects.Scale(nAllRes, CoMoEffects.PassivePercent(target, 16010));
                // Kháng gốc
                int nOriginRes = nAllRes;

                // Ngũ hành tương ứng của sát thương
                int nSeries = (int)KE_SERIES_TYPE.series_metal;
                // Nếu là sát thương vật công
                if (nType == DAMAGE_TYPE.damage_physics)
                {
                    nSeries = (int)KE_SERIES_TYPE.series_metal;
                }
                // Nếu là sát thương băng công
                else if (nType == DAMAGE_TYPE.damage_cold)
                {
                    nSeries = (int)KE_SERIES_TYPE.series_water;
                }
                // Nếu là sát thương hỏa công
                else if (nType == DAMAGE_TYPE.damage_fire)
                {
                    nSeries = (int)KE_SERIES_TYPE.series_fire;
                }
                // Nếu là sát thương lôi công
                else if (nType == DAMAGE_TYPE.damage_light)
                {
                    nSeries = (int)KE_SERIES_TYPE.series_earth;
                }
                // Nếu là sát thương độc công
                else if (nType == DAMAGE_TYPE.damage_poison)
                {
                    nSeries = (int)KE_SERIES_TYPE.series_wood;
                }

                // Nếu có dòng bỏ qua kháng hệ tương ứng
                if (attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMin > 0 && attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMax > 0 && KTGlobal.IngoreResistMaxP > 0)
                {
                    // Tỷ lệ bỏ qua kháng hệ tương ứng
                    int nIgnoreRate = KTGlobal.GetRandomNumber(attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMin, attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMax);
                    if (nIgnoreRate > KTGlobal.IngoreResistMaxP)
                    {
                        nIgnoreRate = KTGlobal.IngoreResistMaxP;
                    }

                    // Kháng mục tiêu
                    int nResist = damageGroup.m_nCurResist;

                    // Tính toán Max Min gì đó
                    int nValue1 = KTGlobal.IngoreResistMaxP - 1;
                    if (nValue1 != 0)
                    {
                        int nValue2 = (2 * KTGlobal.DefenceMaxPercent / nValue1);

                        if (nValue2 != 0)
                        {
                            int nMaxR = (10 * attacker.m_Level + 200) / nValue2;
                            if (nResist > nMaxR)
                            {
                                nResist = nMaxR;
                            }
                        }
                    }

                    // Tính toán lại kháng của mục tiêu
                    nAllRes = nSeriesConquarRes + damageGroup.m_nCurResist - nResist * nIgnoreRate / 100;

                    if (bTrace)
                    {
                        TraceDamage($"CalcReceivePercent: attacker bỏ qua kháng hệ {(KE_SERIES_TYPE)nSeries}: roll={nIgnoreRate}% (min={attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMin}, max={attacker.m_sIgnoreResists[nSeries].nIgnoreResistPMax}, cap={KTGlobal.IngoreResistMaxP}) khángBịTrừ={nResist * nIgnoreRate / 100} (nResist sau cap={nResist})");
                    }
                }

                // nếy attacker có chỉ số bỏ qua kháng
                // 2025/17/06 14:23
                if (attacker.m_IgnoreResistsValue[nSeries] > 0)
                {
                    nAllRes -= attacker.m_IgnoreResistsValue[nSeries];
                }

                // Tỷ lệ sát thương nhận được
                int nResPercent = KTGlobal.DefenceMaxPercent * 2 * Math.Abs(nAllRes) / (Math.Abs(nAllRes) / 3 * 2 + attacker.m_Level * 10 + 200);
                if (nResPercent > KTGlobal.DefenceMaxPercent)
                {
                    nResPercent = KTGlobal.DefenceMaxPercent;
                }

                // Nếu âm kháng
                if (nAllRes <= 0)
                {
                    nResPercent = 100 + nResPercent;
                }
                else
                {
                    // Trả về kết quả
                    nResPercent = 100 - nResPercent;
                }

                //if (attacker is KPlayer)
                //{
                //    if (nSeries == (int) KE_SERIES_TYPE.series_earth)
                //    {
                //        PlayerManager.ShowNotification(attacker as KPlayer, string.Format("{0}'s OriginRes = {1}, FinalRes = {2}, Series = {3}, ResPercent = {4}", target.RoleName, nOriginRes, nAllRes, (KE_SERIES_TYPE) nSeries, nResPercent));
                //    }
                //}

                if (bTrace)
                {
                    TraceDamage($"CalcReceivePercent: type={nType} series={(KE_SERIES_TYPE)nSeries} | khángGốc={nOriginRes} (ngũHành={nSeriesConquarRes} + curResist={damageGroup.m_nCurResist}) ignoreResistValue={attacker.m_IgnoreResistsValue[nSeries]} => khángCuối={nAllRes} | attackerLv={attacker.m_Level} => nhận {nResPercent}% damage");
                }

                // Trả về kết quả
                return nResPercent;
            }
            // Nếu là sát thương phản lại
            else
            {
                if (bTrace)
                {
                    TraceDamage($"CalcReceivePercent: type=damage_return => nhận {damageGroup.m_nReceivePercent + damageGroup.m_nCurResist}% (receivePercent={damageGroup.m_nReceivePercent} + curResist={damageGroup.m_nCurResist})");
                }
                // Trả về tỷ lệ phản
                return damageGroup.m_nReceivePercent + damageGroup.m_nCurResist;
            }
        }

        /// <summary>
        /// Thêm trạng thái ngũ hành
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="client"></param>
        /// <param name="_State"></param>
        /// <param name="skill"></param>
        /// <returns></returns>
        public static KMagicAttrib AppendAttackAttrib(KMagicAttrib magic, GameObject client, KNpcAttribGroup_State _State, SkillLevelRef skill)
        {
            int nAttackRate = _State.StateAddRate;
            int nAttackTime = _State.StateAddTime;
            int nAddPercent = 0;
            int nAddFrame = 0;

            // Cộng chỉ số từ kỹ năng hỗ trợ
            KE_STATE eState = _State.State;
            // Nếu là người chơi
            if (client is KPlayer)
            {
                KPlayer player = client as KPlayer;
                // ProDict kỹ năng hỗ trợ
                PropertyDictionary enchantPd = player.Skills.GetEnchantProperties(skill.SkillID);
                // Nếu có kỹ năng hỗ trợ
                if (enchantPd != null)
                { 
                    switch (eState)
                    {
                        case KE_STATE.emSTATE_HURT:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_hurt_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_hurt_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_hurt_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_hurt_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_hurt_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_hurt_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_WEAK:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_weak_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_weak_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_weak_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_weak_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_weak_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_weak_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_SLOWALL:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowall_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowall_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowall_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowall_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowall_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowall_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_BURN:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_burn_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_burn_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_burn_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_burn_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_burn_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_burn_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_STUN:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_stun_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_stun_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_stun_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_stun_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_stun_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_stun_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_FIXED:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_fixed_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_fixed_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_fixed_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_fixed_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_fixed_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_fixed_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_PALSY:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_palsy_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_palsy_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_palsy_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_palsy_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_palsy_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_palsy_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_SLOWRUN:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowrun_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowrun_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowrun_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowrun_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_slowrun_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_slowrun_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_FREEZE:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_freeze_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_freeze_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_freeze_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_freeze_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_freeze_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_freeze_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_CONFUSE:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_confuse_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_confuse_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_confuse_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_confuse_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_confuse_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_confuse_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_KNOCK:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_knock_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_knock_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += (magicAttrib.nValue[1] * magicAttrib.nValue[2]);
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_knock_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_knock_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_knock_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_knock_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_SILENCE:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_silence_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_silence_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += magicAttrib.nValue[1];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_silence_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_silence_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_silence_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_silence_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                        case KE_STATE.emSTATE_DRAG:
                            {
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_drag_attack))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_drag_attack);
                                    nAddPercent += magicAttrib.nValue[0];
                                    nAddFrame += (magicAttrib.nValue[1] * magicAttrib.nValue[2]);
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_drag_attackrate))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_drag_attackrate);
                                    nAttackRate += magicAttrib.nValue[0];
                                }
                                if (enchantPd.ContainsKey((int)MAGIC_ATTRIB.state_drag_attacktime))
                                {
                                    KMagicAttrib magicAttrib = enchantPd.Get<KMagicAttrib>((int)MAGIC_ATTRIB.state_drag_attacktime);
                                    nAttackTime += magicAttrib.nValue[0];
                                }
                                break;
                            }
                    }
                }
            }

            KMagicAttrib magicFilnal = new KMagicAttrib();
            magicFilnal.nAttribType = magic.nAttribType;

            int nBaseRateParam = KTGlobal.StateBaseRateParam;
            int nBaseTimeParam = KTGlobal.StateBaseTimeParam;

            magicFilnal.nValue[0] = magic.nValue[0] + ((nAttackRate + nBaseRateParam) == 0 ? 0 : (magic.nValue[0] * nAttackRate) / (nAttackRate + nBaseRateParam));
            magicFilnal.nValue[1] = magic.nValue[1] + ((nAttackTime + nBaseTimeParam) == 0 ? 0 : (magic.nValue[1] * nAttackTime) / (nAttackTime + nBaseTimeParam));
            // Vị trí này chứa tham biến phụ sẽ dùng về sau
            magicFilnal.nValue[2] = magic.nValue[2];

            // Cộng thêm hỗ trợ
            magicFilnal.nValue[0] += nAddPercent;
            magicFilnal.nValue[1] += nAddFrame;

            return magicFilnal;
        }

        /// <summary>
        /// Tính toán sát thương độc
        /// </summary>
        /// <param name="pDes"></param>
        /// <param name="pSrc"></param>
        /// <param name="stackCount"></param>
        /// <returns></returns>
        public static KMagicAttrib MixPoisonDamage(KMagicAttrib pDes, KMagicAttrib pSrc, int stackCount = 1)
        {
            if (pDes.nValue[2] == 0)
            {
                if (pDes.nValue[0] < 0 && pDes.nValue[1] < 0)
                {
                    pDes.nValue[2] = -pDes.nValue[0] * pDes.nValue[1];
                }
                else
                {
                    pDes.nValue[2] = pDes.nValue[0] * pDes.nValue[1];
                }
            }

            pDes.nValue[0] += pSrc.nValue[0];
            if (pDes.nValue[0] == 0)
            {
                pDes.nValue[1] = 0;
                pDes.nValue[2] = 0;

                return pDes;
            }

            if (pSrc.nValue[2] == 0)
            {
                if (pSrc.nValue[0] < 0 && pSrc.nValue[1] < 0)
                {
                    pDes.nValue[2] -= (pSrc.nValue[0] * pSrc.nValue[1]);
                }
                else
                {
                    pDes.nValue[2] += (pSrc.nValue[0] * pSrc.nValue[1]);
                }
            }
            else
            {
                pDes.nValue[2] += pSrc.nValue[2];
            }

            pDes.nValue[1] = pDes.nValue[2] / pDes.nValue[0];

            pDes *= stackCount;

            return pDes;
        }

        /// <summary>
        /// Tính sát thương gây ra
        /// </summary>
        /// <param name="magicSrc"></param>
        /// <param name="nSkillPercent"></param>
        /// <param name="bIsPhysical"></param>
        /// <param name="nDamagePercent"></param>
        /// <param name="nSrcT"></param>
        /// <param name="nAppendT"></param>
        /// <param name="_GroupDame"></param>
        /// <returns></returns>
        public static KMagicAttrib CalcAttackDamage(KMagicAttrib magicSrc, int nSkillPercent, bool bIsPhysical, int nDamagePercent, int nSrcT, int nAppendT, KNpcAttribGroup_Damage _GroupDame)
        {
            KMagicAttrib magicDes = new KMagicAttrib();

            magicDes.nValue[0] = magicSrc.nValue[0] * nSkillPercent * nSrcT / 1000 / 100;
            magicDes.nValue[2] = magicSrc.nValue[2] * nSkillPercent * nSrcT / 1000 / 100;

            if (bIsPhysical)
            {
                magicDes.nValue[0] += (_GroupDame.m_nEnhanceDamage * nAppendT / 1000);
                magicDes.nValue[2] += (_GroupDame.m_nEnhanceDamage * nAppendT / 1000);
            }
            else
            {
                magicDes.nValue[0] += (_GroupDame.m_nEnhanceMagic * nAppendT / 1000);
                magicDes.nValue[2] += (_GroupDame.m_nEnhanceMagic * nAppendT / 1000);
            }
            magicDes.nValue[0] = magicDes.nValue[0] * nDamagePercent / 100;
            magicDes.nValue[2] = magicDes.nValue[2] * nDamagePercent / 100;

            return magicDes;
        }

        /// <summary>
        /// Lấy tổng % sát thương được hỗ trợ bởi các kỹ năng khác
        /// </summary>
        /// <param name="SKILLID"></param>
        /// <returns></returns>
        public static int GetAddSkillDamagePercent(GameObject client, int nSkillId)
        {
            if (!(client is KPlayer))
            {
                return 0;
            }

            KPlayer player = client as KPlayer;
            return player.Skills.GetAppendDamageToSkillPercent(nSkillId);
        }

        /// <summary>
        /// Lấy tổng sát thương được hỗ trơ bởi các kỹ năng khác
        /// </summary>
        /// <param name="SKILLID"></param>
        /// <returns></returns>
        public static int GetAddSkillDamage(GameObject client, int nSkillId)
        {
            if (!(client is KPlayer))
            {
                return 0;
            }

            KPlayer player = client as KPlayer;
            return player.Skills.GetAppendDamageToSkill(nSkillId);
        }

        /// <summary>
        /// Trả về % sát thương vật công ngoại của vũ khí
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetPhysicsEnhance(KPlayer client)
        {
            KE_EQUIP_WEAPON_CATEGORY eWeaponCategory = client.GetWeaponCategory();

            if (eWeaponCategory < KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_ALL || eWeaponCategory >= KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_NUM)
            {
                return 0;
            }
            int nEnhance = KTGlobal.m_arPhysicsEnhance[(int)KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_ALL];
            if (eWeaponCategory == KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_ALL)
            {
                return nEnhance;
            }
            if (eWeaponCategory > KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_MELEE && eWeaponCategory < KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_RANGE)
            {
                nEnhance += KTGlobal.m_arPhysicsEnhance[(int)KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_MELEE];
            }
            else if (eWeaponCategory > KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_RANGE)
            {
                nEnhance += KTGlobal.m_arPhysicsEnhance[(int)KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_RANGE];
            }

            return nEnhance + KTGlobal.m_arPhysicsEnhance[(int)eWeaponCategory];
        }


    }
}
