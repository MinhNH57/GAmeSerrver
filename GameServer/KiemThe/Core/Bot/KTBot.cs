using GameServer.KiemThe.Core.Bot;
using GameServer.Interface;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Logic
{
    /// <summary>
    /// Đối tượng BOT
    /// </summary>
    public partial class KTBot : GameObject
    {
        /// <summary>
        /// BaseID của Bot
        /// </summary>
        public BotQuestController QuestController { get; }
        public BotMainQuestActions MainQuestActions { get; } = new BotMainQuestActions();
        private readonly System.Threading.CancellationTokenSource questCancellation =
            new System.Threading.CancellationTokenSource();
        private int questDisabled;
        public bool QuestExecutionDisabled => System.Threading.Volatile.Read(ref questDisabled) != 0;
        public string QuestDisabledReason { get; private set; }
        public System.Threading.CancellationToken QuestCancellation => questCancellation.Token;
        public string QuestOperation = "Idle";
        public DateTime NextQuestActionAtUtc { get; set; }
        public enum BotActivity {Idle,MainQuest,SongJin}
        public BotActivity CurrentActivity { get; set; } = BotActivity.MainQuest;
        public BotSongJinController SongJinController { get; set; }
        public void DisableQuestExecution(string reason)
        {
            if (System.Threading.Interlocked.CompareExchange(ref questDisabled, 1, 0) != 0) return;
            QuestDisabledReason = reason;
            questCancellation.Cancel();
        }

        public void CheckQuestCancellation(string operation)
        {
            System.Threading.Volatile.Write(ref QuestOperation, operation);
            QuestCancellation.ThrowIfCancellationRequested();
        }

        private const int BaseID = 0x7F450000;

        public int FactionSkillId { get; set; } = 0;

        public int FactionSkillLevel { get; set; } = 1;

        /// <summary>
        /// ID tự động
        /// </summary>
        private static int AutoID = -1;

        /// <summary>
        /// Danh sách trang bị
        /// </summary>
        public List<GoodsData> Equips { get; set; }

        /// <summary>
        /// ID môn phái
        /// </summary>
        public int FactionID { get; set; }

        /// <summary>
        /// ID nhánh
        /// </summary>
        public int RouteID { get; set; }

        /// <summary>
        /// ID Avarta
        /// </summary>
        public int RolePic { get; set; }

        /// <summary>
        /// Giới tính
        /// </summary>
        public int RoleSex { get; set; }

        /// <summary>
        /// ID vũ khí
        /// </summary>
        public int WeaponID
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData weapon = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon || itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (weapon == null)
                {
                    return -1;
                }
                else
                {
                    return weapon.GoodsID;
                }
            }
        }

        /// <summary>
        /// Ngũ hành vũ khí
        /// </summary>
        public int WeaponSeries
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData weapon = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon || itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (weapon == null)
                {
                    return -1;
                }
                else
                {
                    return weapon.Series;
                }
            }
        }

        /// <summary>
        /// Cấp cường hóa vũ khí
        /// </summary>
        public int WeaponEnhanceLevel
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData weapon = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon || itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (weapon == null)
                {
                    return -1;
                }
                else
                {
                    return weapon.Forge_level;
                }
            }
        }

        /// <summary>
        /// ID áo
        /// </summary>
        public int ArmorID
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData armor = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_armor;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (armor == null)
                {
                    return -1;
                }
                else
                {
                    return armor.GoodsID;
                }
            }
        }

        /// <summary>
        /// ID mũ
        /// </summary>
        public int HelmID
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData helm = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_helm;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (helm == null)
                {
                    return -1;
                }
                else
                {
                    return helm.GoodsID;
                }
            }
        }

        /// <summary>
        /// ID ngựa
        /// </summary>
        public int HorseID
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData horse = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_horse;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (horse == null)
                {
                    return -1;
                }
                else
                {
                    return horse.GoodsID;
                }
            }
        }

        /// <summary>
        /// ID phi phong
        /// </summary>
        public int MantleID
        {
            get
            {
                if (this.Equips == null)
                {
                    return -1;
                }

                GoodsData mantle = this.Equips.Where((itemGD) => {
                    /// Vật phẩm tương ứng
                    if (ItemManager._TotalGameItem.TryGetValue(itemGD.GoodsID, out ItemData itemData))
                    {
                        return itemData.DetailType == (int) KE_ITEM_EQUIP_DETAILTYPE.equip_mantle;
                    }
                    /// Nếu không tồn tại
                    return false;
                }).FirstOrDefault();
                if (mantle == null)
                {
                    return -1;
                }
                else
                {
                    return mantle.GoodsID;
                }
            }
        }

        /// <summary>
        /// Có đang trong trạng thái cưỡi không
        /// </summary>
        public bool IsRiding { get; set; }

        /// <summary>
        /// ID Script Lua điều khiển
        /// </summary>
        public int ScriptID { get; set; } = -1;

        /// <summary>
        /// ID Script AI điều khiển
        /// </summary>
        public int AIID { get; set; } = -1;

        /// <summary>
        /// Phạm vi đuổi mục tiêu
        /// </summary>
        public int SeekRange { get; set; } = 1000;

        /// <summary>
        /// Loại đối tượng
        /// </summary>
        public override ObjectTypes ObjectType
        {
            get
            {
                return ObjectTypes.OT_BOT;
            }
        }

        private int _CurrentMapCode = -1;
        /// <summary>
        /// ID bản đồ hiện tại
        /// </summary>
        public void SetCurrentMapCode(int mapcode)
        {

            this._CurrentMapCode = mapcode;
        }

        public override int CurrentMapCode
        {
            get
            {
                return this._CurrentMapCode;
            }
        }

        /// <summary>
        /// Tag của đối tượng
        /// </summary>
        public string Tag { get; set; } = "";

        /// <summary>
        /// Danh sách các biến cục bộ của đối tượng
        /// <para>Sử dụng ở Script Lua</para>
        /// </summary>
        private readonly Dictionary<int, long> LocalVariables = new Dictionary<int, long>();

        /// <summary>
        /// Trả về biến cục bộ tại vị trí tương ứng của đối tượng
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public long GetLocalParam(int idx)
        {
            lock (this.LocalVariables)
            {
                if (this.LocalVariables.TryGetValue(idx, out long value))
                {
                    return value;
                }
                else
                {
                    this.LocalVariables[idx] = 0;
                    return 0;
                }
            }
        }

        // BOT đang chờ hồi sinh.
        public bool IsWaitingForRevive { get; set; }

        // Mốc thời gian được phép hồi sinh.
        // Dùng mili giây từ đồng hồ đơn điệu của bộ quản lý hồi sinh.
        public long ReviveAtMilliseconds { get; set; }

        // Map và tọa độ hồi sinh.
        public int ReviveMapCode { get; set; } = -1;
        public System.Windows.Point RevivePosition { get; set; }

        // Các thời điểm chết gần đây, dùng cùng đồng hồ với ReviveAtMilliseconds.
        public Queue<long> RecentDeathTimes { get; } = new Queue<long>();

        // Số lần thử hồi sinh thất bại.
        public int ReviveRetryCount { get; set; }

        // Tạm nghỉ nhiệm vụ vì chết quá nhiều lần.
        public bool IsRestingAfterDeaths { get; set; }
        /// <summary>
        /// Thiết lập biến cục bộ tại vị trí tương ứng của đối tượng
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetLocalParam(int idx, long value)
        {
            lock (this.LocalVariables)
            {
                this.LocalVariables[idx] = value;
            }
        }

        /// <summary>
        /// Xóa rỗng toàn bộ danh sách biến cục bộ
        /// </summary>
        public void RemoveAllLocalParams()
        {
            lock (this.LocalVariables)
            {
                this.LocalVariables.Clear();
            }
        }

        public int ChallengePlayerID { get; set; } = -1;

        public bool IsInChallenge
        {
            get { return ChallengePlayerID > 0; }
        }

        public void BeginChallengeWith(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            this.BeginChaseTarget(player);
        }

        /// <summary>
        /// Số lượng người chơi xung quanh
        /// </summary>
        public int VisibleClientsNum { get; set; }

        /// <summary>
        /// Tạo đối tượng BOT mới
        /// </summary>
        public override void OnKillObject(GameObject deadObj)
        {
            base.OnKillObject(deadObj);
            if (deadObj is Monster monster && this.QuestController.IsRunning &&
                this.QuestController.State == BotQuestState.ExecuteQuest)
                this.MainQuestActions.RecordKill(monster);
        }

        public KTBot() : base()
        {
            this.QuestController = new BotQuestController(this);
            this.SongJinController = new BotSongJinController(this);

            KTBot.AutoID = (KTBot.AutoID + 1) % 100000007;
            this.RoleID = KTBot.BaseID + KTBot.AutoID;

            /// Tạo mới Buff
            this.Buffs = new BuffTree(this);
            this.Buffs.AttachAllBuffs();
            /// Thực hiện động tác đứng
            this.m_eDoing = KE_NPC_DOING.do_stand;
        }
    }
}
