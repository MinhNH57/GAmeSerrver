using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Linq;
using Server.Data;
using GameServer.Logic.RefreshIconState;

namespace GameServer.KiemThe
{

    public static class XMLNodeExtension
    {
        public static T SafeGetAttribute<T>(this XElement xmlNode, string attributeName, object default_value = null)
        {
            var attribute = xmlNode.Attribute(attributeName);
            if (default_value == null)
            {
                default_value = default(T);
            }
            if (attribute == null)
                return (T)Convert.ChangeType(default_value, typeof(T));;
            try
            {
                return (T)Convert.ChangeType(attribute.Value, typeof(T));
            }
            catch
            {
                return (T)Convert.ChangeType(default_value, typeof(T));;
            }
        }
    }
    /// <summary>
    /// Các phương thức và đối tượng toàn cục của Kiếm Thế
    /// </summary>
    public static partial class KTGlobal
    {
        private const string LIST_VIP_10M = "Config/Config/KT_Setting/Users_VIP_10m.txt";
        private const string LIST_ACTIVE_USERS = "Config/Config/KT_Setting/Users_Active.txt";
        private const string LIST_CHARGE_USERS = "Config/Config/KT_Setting/Users_Charged.txt";
        private const string LIST_REDUCE_RATE_IDS = "Config/Config/KT_Setting/IDs_RateReduce.txt";
        private const string LIST_MONITORED_IDS = "Config/Config/KT_Setting/IDs_Monitored.txt";
        private const string LIST_ALLOW_CREATE_ROLES = "Config/Config/KT_Setting/Users_Allowed_CreateRole.txt";

        private const string LIST_ACTIVE_ROLES = "Config/Config/KT_Setting/IDs_Active.txt";
        private const string LIST_500_CHARGED_ROLES = "Config/Config/KT_Setting/IDs_Charged_500.txt";
        private const string LIST_S17_ROLES = "Config/Config/KT_Setting/IDs_S17.txt";
        private const string LIST_S18_ROLES = "Config/Config/KT_Setting/IDs_S18.txt";
        private const string SPECIAL_BAN = "Config/Config/KT_Setting/special_ban.json";

        private static SortedList<string, int> ListVIP10M;
        private static SortedList<string, int> ListActiveUsers;
        private static SortedList<string, int> ListChargedUsers;
        private static SortedList<int, int> ListReduceRate;
        private static SortedList<int, int> ListMonitored;
        private static SortedList<string, int> ListAllowedCreateRole;
        private static SortedList<int, int> ListActiveRoles;
        private static SortedList<int, int> List500ChargedRoles;
        private static SortedList<int, int> ListS17Roles;
        private static SortedList<int, int> ListS18Roles;
        public static DateTime FakeDate = DateTime.Now;
        public static DateTime UpdateFakeDate = DateTime.Now;

        public static DateTime GetDateNow()
        {
#if DEBUG
            return DateTime.Now;
            //return KTGlobal.FakeDate + (DateTime.Now - KTGlobal.UpdateFakeDate);
#else
            return DateTime.Now;
#endif
        }

        public class BanCondition
        {
            public string condition;
            public long value;
        }

        private static List<List<BanCondition>> BanConditionSets;

        #region Tiên đồng Ngọc nữ

        [Serializable]
        internal class TDNN_Data
        {
            public string lua_message;
            public string system_message;
            public List<int> winners;
        }

        private static TDNN_Data tdnnData;

        public static void LoadTienDongNgocNu()
        {
            try
            {
                var path = "tiendongngocnu_today.json";
                if (!File.Exists(path))
                {
                    tdnnData = null;
                    return;
                }

                var jsonStr = File.ReadAllText(path);
                tdnnData = JsonConvert.DeserializeObject<TDNN_Data>(jsonStr);
            }
            catch
            {
                tdnnData = null;
            }
        }

        public static bool IsTDNNWinner(int playerID)
        {
            return tdnnData != null && tdnnData.winners.Contains(playerID);
        }

        public static string GetTDNNLuaMessage()
        {
            return tdnnData == null ? "" : tdnnData.lua_message;
        }

        public static string GetTDNNSystemMessage()
        {
            return tdnnData == null ? "" : tdnnData.system_message;
        }

        #endregion

        private static SortedList<T, int> LoadEntries<T>(string path)
        {
            var list = new SortedList<T, int>();
            if (!File.Exists(path))
                return list;
            var lines = File.ReadLines(path);
            foreach (var line in lines)
            {
                list.Add((T)Convert.ChangeType(line, typeof(T)), 0);
            }
            SysConOut.WriteLine($"loaded {list.Count} entries from {path}");
            return list;
        }

        public static void LoadSpecialUsers()
        {
            ListVIP10M = LoadEntries<string>(LIST_VIP_10M);
            ListActiveUsers = LoadEntries<string>(LIST_ACTIVE_USERS);
            ListChargedUsers = LoadEntries<string>(LIST_CHARGE_USERS);
            ListReduceRate = LoadEntries<int>(LIST_REDUCE_RATE_IDS);
            ListMonitored = LoadEntries<int>(LIST_MONITORED_IDS);
            ListAllowedCreateRole = LoadEntries<string>(LIST_ALLOW_CREATE_ROLES);
            ListActiveRoles = LoadEntries<int>(LIST_ACTIVE_ROLES);
            List500ChargedRoles = LoadEntries<int>(LIST_500_CHARGED_ROLES);
            ListS17Roles = LoadEntries<int>(LIST_S17_ROLES);
            ListS18Roles = LoadEntries<int>(LIST_S18_ROLES);

            if (File.Exists(SPECIAL_BAN))
            {
                var content = File.ReadAllText(SPECIAL_BAN);
                BanConditionSets = JsonConvert.DeserializeObject<List<List<BanCondition>>>(content);
            }
        }

        public static bool IsEligibleForHeroComeback(int rid)
        {
            return !ListActiveRoles.ContainsKey(rid) && List500ChargedRoles.ContainsKey(rid);
        }

        public static bool IsAllowedCreateRole(string userid) 
        {
            return ListAllowedCreateRole.ContainsKey(userid);
        }

        public static bool IsRoleMonitored(int rid)
        {
            return ListMonitored.ContainsKey(rid);
        }

        public static void PlayerGoToNPC(KPlayer player, int npcId, int mapId)
        {
            var strCmd = $"{player.RoleID}:{npcId}:{mapId}";
            player.SendPacket((int)TCPGameServerCmds.CMD_SPR_GOTONPC, strCmd);
        }

        public static bool CanGetReturnAwards(string userid)
        {
            return ListChargedUsers.ContainsKey(userid) && !ListActiveUsers.ContainsKey(userid);
        }

        public static bool CanGetReturnAwardsVIP(string userid)
        {
            return ListVIP10M.ContainsKey(userid);
        }

        public static bool IsRateReduce(int rid) 
        {
            return ListReduceRate.ContainsKey(rid);
        }

        /// <summary>
        /// thêm buff thức ăn tân thủ
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool AddMedicineBuff3Days(KPlayer player)
        {
            var regTime = new DateTime(player.RegTime * 10000);
            var currentTime = DateTime.Now;
            var effectTime = regTime.AddDays(3);
            #if DEBUG
            effectTime = currentTime.AddDays(3);
            #endif
            if (effectTime < currentTime)
            {
                // đã quá thời gian hiệu lực
                return false;
            }
            
            // ID kỹ năng tương ứng
            const int skillID = 100020;
            // Thời gian tồn tại
            var duration = (int)((effectTime - currentTime).TotalMilliseconds);

            // Kỹ năng tương ứng
            var skill = KSkill.GetSkillData(skillID);

            // Nếu kỹ năng không tồn tại
            if (skill == null)
            {
                return false;
            }

            // Dữ liệu kỹ năng tương ứng
            var skillRef = new SkillLevelRef()
            {
                Data = skill,
                AddedLevel = 1,
                BonusLevel = 0,
                CanStudy = false,
            };
            var buff = new BuffDataEx()
            {
                Skill = skillRef,
                Duration = duration,
                LoseWhenUsingSkill = false,
                SaveToDB = true,
                StackCount = 1,
                StartTick = KTGlobal.GetCurrentTimeMilis(),
                CustomProperties = new PropertyDictionary(),
            };

            var props = new List<Medicine>
            {
                new Medicine
                {
                    MagicName = "lifegrow_v",
                    Value = 15,
                    Time = 32400,
                    Index = 1
                },
                new Medicine
                {
                    MagicName = "managrow_v",
                    Value = 15,
                    Time = 32400,
                    Index = 2
                },
            };

            // Duyệt toàn bộ thuộc tính của thuốc
            foreach (var prop in props)
            {
                var propName = prop.MagicName;
                var value = prop.Value;

                // Lấy thuộc tính tương ứng theo tên
                if (!PropertyDefine.PropertiesBySymbolName.TryGetValue(propName, out var property))
                    continue;
                var magicAttrib = new KMagicAttrib()
                {
                    nAttribType = (MAGIC_ATTRIB) property.ID,
                    nValue = new int[] { value, duration / 1000, 0 },
                };

                buff.CustomProperties.Set<KMagicAttrib>(property.ID, magicAttrib);
            }

            if (player.Buffs.HasBuff(buff.Skill.SkillID))
            {
                var buffOld = player.Buffs.GetBuff(buff.Skill.SkillID);
                if ((KTGlobal.GetCurrentTimeMilis() - buffOld.StartTick) < 1000)
                {
                    PlayerManager.ShowNotification(player, "Thao tác quá nhanh, xin chờ 1s");
                    return false;
                }
            }

            // Thêm Buff vào cho người chơi
            player.Buffs.AddBuff(buff);

            return true;
        }

        public static T GetXMLAttribute<T>(XElement xmlNode, string attributeName, object default_value = null)
        {
            var attribute = xmlNode.Attribute(attributeName);
            if (default_value == null)
            {
                default_value = default(T);
            }
            if (attribute == null)
                return (T)Convert.ChangeType(default_value, typeof(T));;
            try
            {
                return (T)Convert.ChangeType(attribute.Value, typeof(T));
            }
            catch
            {
                return (T)Convert.ChangeType(default_value, typeof(T));;
            }
        }
        
        public static T GetXMLAttribute<T>(XElement xmlNode, string attributeName, Dictionary<string, object> default_values)
        {
            var attribute = xmlNode.Attribute(attributeName);
            if (!default_values.TryGetValue(attributeName, out var default_value))
            {
                return default(T);
            }
            
            try
            {
                if (attribute == null)
                    return (T)Convert.ChangeType(default_value, typeof(T));;
                
                return (T)Convert.ChangeType(attribute.Value, typeof(T));
            }
            catch
            {
                return (T)Convert.ChangeType(default_value, typeof(T));;
            }
        }

        public static Dictionary<string, object> GetDefaultValues(XElement xmlNode) 
        {
            var default_values = new Dictionary<string, object>();
            foreach (var node in xmlNode.Elements("Default")) {
                var key = KTGlobal.GetXMLAttribute<string>(node, "Key", "");
                var value = KTGlobal.GetXMLAttribute<string>(node, "Value", "");
                if (int.TryParse(value, out var intValue)) {
                    default_values[key] = intValue;
                } else if (bool.TryParse(value, out var boolValue)) {
                    default_values[key] = boolValue;
                } else {
                    default_values[key] = value;
                }
            }

            return default_values;
        }

        public static bool CanGetMergeServerSupportPack(int rid) 
        {
            return List500ChargedRoles.ContainsKey(rid) && (ListS17Roles.ContainsKey(rid) || ListS18Roles.ContainsKey(rid));
        }

        public static int GetServerIDBeforeMerge(int rid) 
        {
            if (ListS17Roles.ContainsKey(rid)) 
            {
                return 17;
            }
            if (ListS18Roles.ContainsKey(rid))
            {
                return 18;
            }
            return -1;
        }

        public static bool IsSpecialBanned(RoleData roleData)
        {
            foreach (var set in BanConditionSets)
            {
                var isBanned = true;
                foreach (var rule in set)
                {
                    switch (rule.condition)
                    {
                        case "family":
                            isBanned = isBanned && (roleData.FamilyID == rule.value);
                            break;
                        
                        case "roleid":
                            isBanned = isBanned && (roleData.RoleID == rule.value);
                            break;
                        
                        case "totalvalue_smaller":
                            isBanned = isBanned && (roleData.TotalValue < rule.value);
                            break;
                        
                        case "totalvalue_greater":
                            isBanned = isBanned && (roleData.TotalValue > rule.value);
                            break;
                        
                        case "guild":
                            isBanned = isBanned && (roleData.GuildID == rule.value);
                            break;
                    }

                }
                if (isBanned)
                    return true;
            }
            return false;
        }

        public static void SendNewMailNotification(KPlayer player)
        {
            IconStateManager.SysnIcon(player);
        }

        public static void SendNewMailNotificationPopup(KPlayer player,MailData mailData)
        {
            byte[] _cmdData = DataHelper.ObjectToBytes(mailData);
            GameManager.ClientMgr.SendToClient(player, _cmdData, (int)TCPGameServerCmds.CMD_KT_UI_NEW_MAIL);
        }

        public static int IsCrystal(int goodsID)
        {
            if (!ItemManager._TotalGameItem.TryGetValue(goodsID, out var itemData))
            {
                return -1;
            }
            if (itemData.Genre == 18 && itemData.DetailType == 1 && (itemData.ParticularType == 1 || itemData.ParticularType == 114))
            {
                if (itemData.ItemID >= 384) 
                {
                    return itemData.ItemID - 384;
                }
                return itemData.ItemID - 182;
            }
            else
            {
                return -1;
            }
        }

        public static int CountCrystalInBagByLevel(KPlayer player, int crystalLevel) 
        {
            var crystal_id_1 = crystalLevel + 182;
            var crystal_id_2 = crystalLevel + 384;
            var total = ItemManager.GetItemCountInBag(player, crystal_id_1) + ItemManager.GetItemCountInBag(player, crystal_id_2);
            return total;
        }

        public static bool RemoveCrystalByLevel(KPlayer player, int crystalLevel, int number) 
        {
            var crystal_id_1 = crystalLevel + 182;
            var crystal_id_2 = crystalLevel + 384;
            var count_2 = ItemManager.GetItemCountInBag(player, crystal_id_2);

            if (count_2 > number) 
            {
                return ItemManager.RemoveItemFromBag(player, crystal_id_2, number);
            }

            var result = ItemManager.RemoveItemFromBag(player, crystal_id_2, count_2);
            var count_left = number - count_2;
            return result && ItemManager.RemoveItemFromBag(player, crystal_id_1, count_left);
        }

        public static void InitNewbieEquipments(KPlayer player)
        {
            var playerReceivedFirstEquipments =
                Global.GetRoleParamsInt32FromDB(player, RoleParamName.PlayerReceivedFirstEquipments, 0) == 1;

            // nếu đã nhận rồi thì thôi
            if (playerReceivedFirstEquipments) 
                return;
            
            // save lại tình trạng đã nhận
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.PlayerReceivedFirstEquipments, 1, true);

            // nếu đã có trang bị thì thôi
            if (player.GoodsDataList != null && player.GoodsDataList.Count > 0) 
                return;
            
            // khởi tạo đồ tân thủ
            var endTime = DateTime.Now.AddDays(7).AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
            
            // truyền tống phù 7 ngày
            ItemManager.CreateSimpleItem(player, 496, 1, 0, 1, 0, endTime);
            // ngoại trang theo giới tính
            var outfitId = player.RoleSex == (int)Entities.Sex.Male ? 15723 : 15722;
            ItemManager.CreateSimpleItem(player, outfitId, 1, 0, 1, 0, endTime);
            // phi phong tân thủ
            ItemManager.CreateSimpleItem(player, 3371, 1, 0, 1, 0, endTime);
            // ngựa tân thủ
            ItemManager.CreateSimpleItem(player, 15507, 1, 0, 1, 0, endTime);

            // nếu không tạo được 4 đồ trên thì thôi
            if (player.GoodsDataList == null || player.GoodsDataList.Count < 4)
            {
                return;
            }
            
            // mặc đồ lên người
            player.GetPlayEquipBody().EquipLoadMain(player.GoodsDataList[1], (int)KE_EQUIP_POSITION.emEQUIPPOS_OutfitArmor);
            player.GetPlayEquipBody().EquipLoadMain(player.GoodsDataList[2], (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
            // riêng ngựa thì cần cập nhật trạng thái lên ngựa trước khi load
            player.IsRiding = true;
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.HorseToggleOn, 1, true);
            player.GetPlayEquipBody().EquipLoadMain(player.GoodsDataList[3], (int)KE_EQUIP_POSITION.emEQUIPPOS_HORSE);
                    
            // cập nhật lại hp, mp, stamina
            player.m_CurrentLife = Math.Max(Global.GetRoleParamsInt32FromDB(player, RoleParamName.CurHP), player.m_CurrentLifeMax);
            player.m_CurrentMana = Math.Max(Global.GetRoleParamsInt32FromDB(player, RoleParamName.CurMP), player.m_CurrentManaMax);
            player.m_CurrentStamina = Math.Max(Global.GetRoleParamsInt32FromDB(player, RoleParamName.CurStamina), player.m_CurrentStaminaMax);
        }
    }
}