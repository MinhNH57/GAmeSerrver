using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Thực thi hiệu ứng ngũ hành
    /// </summary>
    public class KSpecialStateManager
    {
        /// <summary>
        /// Danh sách trạng thái ngũ hành
        /// </summary>
        public static ArrayOfKSpecialState g_arSpecialState { get; private set; } = new ArrayOfKSpecialState();

        /// <summary>
        /// ID kỹ năng Vô Ngấn Mật Truyền.
        /// </summary>
        private const int VoNganMatTruyenSkillID = 17003;

        /// <summary>
        /// Khởi tạo dữ liệu
        /// </summary>
        public static void Init()
        {
            string file = Global.GameResPath("Config/KT_Skill/SpecialState.xml");
            using (TextReader reader = new StreamReader(file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfKSpecialState));
                KSpecialStateManager.g_arSpecialState = (ArrayOfKSpecialState) serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Phản lại một phần thời gian làm chậm hoặc choáng
        /// lên người đã gây trạng thái.
        /// </summary>
        private static void TryReflectControlState(
            GameObject target,
            GameObject attacker,
            KE_STATE eState,
            int receivedTime,
            KMagicAttrib originalMagic,
            SkillLevelRef originalSkill)
        {
            // Không có người gây trạng thái.
            if (target == null || attacker == null)
            {
                return;
            }

            // Không phản lên chính mình.
            if (ReferenceEquals(target, attacker))
            {
                return;
            }

            // Chỉ phản làm chậm toàn diện và choáng.
            if (eState != KE_STATE.emSTATE_SLOWALL &&
                eState != KE_STATE.emSTATE_STUN)
            {
                return;
            }

            // Không phản cho đồng đội hoặc đối tượng cùng phe.
            if (!KTLogic.IsOpposite(target, attacker))
            {
                return;
            }

            // Trạng thái nguồn không có thời gian hợp lệ.
            if (receivedTime <= 0)
            {
                return;
            }

            int reflectPercent = GetControlStateReflectPercent(target);
            if (reflectPercent <= 0)
            {
                return;
            }

            int reflectedTime = receivedTime * reflectPercent / 100;
            if (reflectedTime <= 0)
            {
                return;
            }

            KMagicAttrib reflectedMagic = new KMagicAttrib
            {
                nAttribType = originalMagic.nAttribType,
                nValue = new int[]
                {
            100,            // Chắc chắn thử áp dụng trạng thái phản lại
            reflectedTime,  // Thời gian phản lại
            reflectedTime,  // Giới hạn tối đa
                },
            };

            UnityEngine.Vector2 reflectedSkillPos = new UnityEngine.Vector2(
                (float)target.CurrentPos.X,
                (float)target.CurrentPos.Y
            );

            // Dùng PropertyDictionary rỗng để không áp dụng lại
            // các thuộc tính hỗ trợ của kỹ năng tấn công ban đầu.
            PropertyDictionary emptyProperties = new PropertyDictionary();

            // target ban đầu trở thành nguồn của trạng thái phản lại.
            // allowReflect=false để không tạo vòng phản phệ vô hạn.
            OnReceiveState(
                attacker,
                reflectedMagic,
                eState,
                target,
                originalSkill,
                reflectedSkillPos,
                emptyProperties,
                false
            );
        }

        /// <summary>
        /// Lấy tỷ lệ phản phệ thời gian làm chậm/choáng.
        /// Param1: tỷ lệ cấp 1.
        /// Param2: tỷ lệ cấp tối đa.
        /// </summary>
        private static int GetControlStateReflectPercent(GameObject target)
        {
            if (target == null || target.Buffs == null)
            {
                return 0;
            }

            BuffDataEx buff = target.Buffs.GetBuff(VoNganMatTruyenSkillID);
            if (buff == null || buff.Skill == null || buff.Skill.Data == null)
            {
                return 0;
            }

            int level = Math.Max(1, buff.Level);
            int maxLevel = Math.Max(1, buff.Skill.Data.MaxSkillLevel);

            int firstPercent = buff.Skill.Data.Params[0];
            int lastPercent = buff.Skill.Data.Params[1];

            if (firstPercent <= 0 || lastPercent <= 0)
            {
                return 0;
            }

            level = Math.Min(level, maxLevel);

            if (maxLevel == 1)
            {
                return firstPercent;
            }

            int percent = firstPercent
                + (lastPercent - firstPercent)
                * (level - 1)
                / (maxLevel - 1);

            return Math.Max(0, Math.Min(100, percent));
        }

        /// <summary>
        /// Nhận trạng thái ngũ hành
        /// </summary>
        /// <param name="target"></param>
        /// <param name="magic"></param>
        /// <param name="eState"></param>
        /// <param name="attacker"></param>
        /// <param name="skill"></param>
        /// <param name="skillPos"></param>
        /// <param name="skillPd"></param>
        public static void OnReceiveState(GameObject target, KMagicAttrib magic, KE_STATE eState, GameObject attacker, SkillLevelRef skill, UnityEngine.Vector2 skillPos, PropertyDictionary skillPd, bool allowReflect = true)
        {
            if (eState < KE_STATE.emSTATE_BEGIN || eState >= KE_STATE.emSTATE_NUM)
            {
                return;
            }

            /// Nếu đối tượng đang trong trạng thái khinh công thì bỏ qua
            if (target.m_eDoing == KE_NPC_DOING.do_jump)
            {
                return;
            }

            int nRate = magic.nValue[0];
            int nTime = magic.nValue[1];

            if (nRate <= 0 || nTime <= 0)
            {
                return;
            }
            int nMaxFrame = magic.nValue[1];

            if (magic.nValue[2] <= 0)
            {
                string param = KSpecialStateManager.g_arSpecialState.KSpecialState.Where(x => x.StateID == Enum.GetName(eState.GetType(), eState)).FirstOrDefault()?.MaxFrame;
                if (!string.IsNullOrEmpty(param))
                {
                    nMaxFrame = int.Parse(param);
                }
            }
            else
            {
                nMaxFrame = magic.nValue[2];
            }

            /// Trạng thái của thằng Target
            KNpcAttribGroup_State targetstate = target.m_state[(int) eState];

            /// Nếu đang bị trạng thái này rồi thì thôi
            if (targetstate.Duration > 0)
			{
                return;
            }
            /// Nếu có thể bỏ qua trạng thái này
            if (targetstate.IgnoreRate)
			{
                return;
            }

            int nPercent = 0;
            int nNewTime = 0;

            int nStateBaseRateParam = KTGlobal.StateBaseRateParam;
            int nStateBaseTimeParam = KTGlobal.StateBaseTimeParam;

            /// Toác gì đó
            if (targetstate.StateRestRate + nStateBaseRateParam <= 0 || targetstate.StateRestRate + nStateBaseTimeParam <= 0)
            {
                return;
            }

            /// Trừ đi rate từ thằng bị tấn công
            int stateRestRateFinal = targetstate.StateRestRate + nStateBaseRateParam;
            nPercent = nRate - (stateRestRateFinal == 0 ? 0 : (nRate * targetstate.StateRestRate) / stateRestRateFinal);
            /// Trừ đi thời gian của thằng phòng thủ
            int stateRestTimeFinal = targetstate.StateRestTime + nStateBaseTimeParam;
            nNewTime = nTime - (stateRestTimeFinal == 0 ? 0 : (nTime * targetstate.StateRestTime) / stateRestTimeFinal);

            /// Nếu có Symbol căn cứ khoảng cách gây trạng thái tương ứng
            if (skillPd.ContainsKey((int) MAGIC_ATTRIB.magic_skilladdition_addmagicbydist))
            {
                KMagicAttrib magicAttrib = skillPd.Get<KMagicAttrib>((int) MAGIC_ATTRIB.magic_skilladdition_addmagicbydist);

                /// Loại hiệu ứng
                int stateID = magicAttrib.nValue[0];

                /// Nếu trùng với trạng thái ngũ hành hiện tại
                if (stateID == (int) eState)
                {
                    /// Khoảng cách giữa bản thân và mục tiêu
                    float distance = KTLogic.GetDistance(attacker, target);

                    /// Lấy Min giữa khoảng cách này và khoảng cách tối đa
                    distance = Math.Min(distance, magicAttrib.nValue[2]);

                    /// Hệ số nhân
                    float percent = magicAttrib.nValue[1] / 100f;

                    /// Tỷ lệ % hiệu ứng cộng thêm
                    nNewTime = (int) (nNewTime + distance * percent / 100f);
                }
            }

            /// Nếu vượt quá giá trị Max
            if (nNewTime > nMaxFrame)
            {
                nNewTime = nMaxFrame;
            }

            int nRand = KTGlobal.GetRandomNumber(0, 99);
            if (nRand < nPercent)
            {
                if (skill.Data.StartPoint == 6)
                {
                    skillPos = new UnityEngine.Vector2(
                        (int)attacker.CurrentPos.X,
                        (int)attacker.CurrentPos.Y
                    );
                }

                bool stateAdded = target.AddSpecialState(
                    attacker,
                    skillPos,
                    eState,
                    nNewTime,
                    magic.nValue[2],
                    true
                );

                // Chỉ phản khi trạng thái đã thực sự được thêm thành công.
                if (stateAdded && allowReflect)
                {
                    TryReflectControlState(
                        target,
                        attacker,
                        eState,
                        nNewTime,
                        magic,
                        skill
                    );
                }
            }
        }
    }
}