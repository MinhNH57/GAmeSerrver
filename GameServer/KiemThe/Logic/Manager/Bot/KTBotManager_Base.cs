using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Utilities.Algorithms;
using GameServer.Logic;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GameServer.KiemThe.Logic.Manager
{
    /// <summary>
    /// Quản lý thêm sửa xóa
    /// </summary>
    public static partial class KTBotManager
    {

        private static List<string> randBotName = new List<string> { "HồngMinh", "ThiênViễn", "ThiênÂn", "NamCung", "VănNhân", "ChưCát", "ĐạmĐài", "CôngDã", "ThânĐồ", "CôngTôn", "MộDung", "ChungLy", "TiênVu", "TiKhông", "LưKhâu", "KỳQuan", "TiKhấu", "CôngTây", "TấtĐiêu", "ThácBạt", "GiápCốc", "LệnhHồ", "BáchLý", "HôDiên", "NamMôn", "ViSinh", "CôngHộ", "CôngMôn", "CôngSơn", "TảKhâu", "CôngBá", "TâyMôn", "CôngTổ", "NamVinh", "ĐôngLý", "TửTang", "TứcMặc", "TuKiệt", "DiệpVĩ", "NhĩMạn", "LậpHuy", "TríViễn", "ThiênTư", "HữuLục", "TuKhiết", "DiệpHoa", "ChấnGia", "ViệtBân", "TửHiên", "VĩThần", "TấnBằng", "HảiDiệc", "UyểnBác", "NiệmBa", "TuấnHi", "TuấnTrì", "NamTùng", "UyênTư", "NhạcCâu", "DuệUyên", "BácSiêu", "DạBạch", "ThuBạch", "BằngPhi", "VĩTrạch", "VũTrạch", "ThiVân", "ĐạiĐào", "TríThần", "HiểuBác", "TĩnhKỳ", "QuânHạo", "LãnhAn", "BánSơn", "BăngAn", "HạoHiên", "DậpĐồng", "DiệpLỗi", "VấnQuân", "DiệuHải", "BìnhVăn", "ThiênVũ", "VănBác", "MinhHuy", "HànhÁc", "TửKhiên", "ẤuToàn", "VĩBang", "NhưBáo", "LiênHổ", "ThậpTam", "ThùHải", "NhấtĐức", "KhaiSơn", "VôThanh", "BấtKhả", "ThếQuan", "ĐạiChu", "TưViễn", "KýLinh", "HồngĐào", "CôPhong", "PhánThu", "LiênYên", "HạoDiễm", "VĩThành", "BằngĐào", "HâmLỗi", "NhĩLam", "TĩnhCừu", "HàoAnh", "ỨcTiên", "ThếĐức", "ThếBình", "BấtNgôn", "NanTồi", "ThếLập", "NanĐịch", "HạoLan", "ChúHải", "LãoĐầu", "KìnhVũ", "HạoHiên", "BácĐào", "DiệpLâm", "ThưCần", "TòngAn", "TầmHạm", "ThuHàn", "CốcHòe", "VănHiên", "LậpQuả", "ViêmBân", "PháiSơn", "KiếmSầu", "ThiênKỳ", "ĐạoChi", "ThừaVân", "SĩTiêu", "VănLong", "KiếmQuỷ", "BánTiên", "VạnNgôn", "ThiênDữ", "ThếKhai", "BấtPhàm", "ĐạoTiêu", "ĐoạnThu", "ViễnSơn", "QuỷThần", "TúySơn", "HâmBằng", "VănHạo", "TùngTư", "SơnLiễu", "KhỉĐồng", "MịchVân", "LộDương", "LậpHiên", "KimHâm", "KiếnHuy", "HạcHiên", "ViễnXâm", "NhấtĐao", "VôHuyết", "VôKiếm", "VôĐịch", "VạnOán", "VạnCừu", "TamVấn", "PhiTiếu", "ÁcThiên", "BánQuỷ", "VănĐào", "TúyDịch", "HànMộng", "HàmDịch", "HàmLăng", "HảiVân", "HảiĐông", "PhiTrân", "PhiHòe", "LụcHải", "TưThiên", "NhưTùng", "BạchAn", "BánPhàm", "BấtBình", "KýPhàm", "MịchHải", "CốcLam", "MộngThu", "MịchSơn", "PhiVân", "PhiĐiểu", "VôNhân", "ThiênVũ", "TháiHư", "BáThiên", "TảMinh", "VịƯơng", "TửNhai", "VôGian", "ThiênBá", "DiệtThế", "ChỉThủy", "TúcMệnh", "VôThiên", "VôPháp", "PhongVũ", "NamSơn", "CứuThục", "DạHành", "VôSong", "AnhKiệt", "TuấnNam", "HạoNam", "QuânHạo", "MặcTrì", "ẨnThủy", "HồNhược", "VănLiễu", "HànhLự", "SùngLẫm", "BộNhai", "LộngẢnh", "PhùSinh", "TrìMinh", "BánVân", "KhêĐiệp", "MạcTrần", "QuânMặc", "TưThông", "TiếuNgu", "TríUyên", "TưMiểu", "DiệpVĩ", "UyểnBác", "KiếnHuy", "TríViễn", "TuấnTrì", "VũTrạch", "DiệpLỗi", "QuốcHào", "VănBác", "VănHạo", "TuKiệt", "LêHân", "AnhKiệt", "TuấnNam", "HồngĐào", "HạoVũ", "TấnBằng", "BáNghị", "TửKhiên", "TuấnHi", "LongÂn", "ViêmÂn", "HiThành", "TửDiệp", "TửLong", "TửHách", "VănĐằng", "HạoThần", "NhạcVĩ", "KínhVĩ", "KiếnTề", "BânVân", "VũGiang", "HạoDiên", "ThiệuTư", "KínhHãn", "HàngDụ", "MậuDiên", "NhạcHoa", "ThầnDuy", "ThầnHào", "KhâmHào", "KínhTư", "ThầnHàn", "BânThần", "ThầnHải", "LâmBằng", "VănHạo", "BânHạo", "HàngVĩ", "KếTường", "HàoTấn", "QuanDuệ", "CốcXán", "LâmNgôn", "TúHồng", "NhamHữu", "HữuViễn", "HạoDiệu", "TinhDật", "HữuLong", "HiềnNam", "HạoHạo", "HàngHạo", "XươngDư", "VănSiêu", "KiếnBưu", "LâmKhôn", "HữuXán", "BânQuốc", "HiềnTư", "BáKhiêm", "ĐôngSam", "KiếnDu", "TríBân", "HàHồng", "BáHồng", "VũThần", "HữuHồng", "ThầnHãn", "HạoChấn", "MinhHữu", "KiếnLỗi", "HàngVân", "BânĐống", "BânThạc", "TuấnHuy", "HoaThất", "QuaĐỗng", "TuyếtTừ", "LiênPhú", "CốTịch", "CốtTửu", "NanPhá", "ĐứcĐịa", "TửHuyên", "TửSương", "TửLăng", "TửThúy", "ChỉDung", "ChỉXảo", "ChỉHủy", "ChỉHà", "ChiĐào", "NgữSơn", "NgữDung", "NgữCầm", "NgữHải", "NgữPhù", "NgữNhi", "NgữĐiệp", "VũTuyết", "VũLiên", "ẤuPhong", "HựuHạm", "HữuMai", "HữuNhi", "DịchXảo", "DiệcTi", "DiệcXảo", "ỨcTuyết", "ỨcPhong", "DạNgọc", "DạMộng", "DạXuân", "NhạnHà", "NhãĐồng", "NhãCầm", "TầmMai", "TầmĐông", "TiếuLam", "HiểuHạ", "HạChân", "TíchNhị", "VấnHạ", "VấnNhị", "VấnMai", "TưTùng", "TưLăng", "ThủyDao", "ThưTrúc", "ThưDịch", "ThiĐào", "ThiSong", "ThiSan", "ThiNhị", "SơnHạm", "SơnĐiệp", "NhưĐông", "ThuNhu", "XảoNhị", "KhỉCầm", "KhỉMai", "BìnhLộ", "PháiNhi", "PhánYên", "NgưngAn", "NiệmChi", "DiệuChi", "MộngChi", "MộngĐào", "MộngKỳ", "MộngLộ", "MạnDung", "MạnHàn", "LụcChân", "LăngVăn", "LăngBa", "LiênSan", "LãnhHà", "TĩnhPhù", "KýCầm", "HàmNgọc", "HàmYên", "HàmLinh", "HàmLôi", "CốcNhị" };
        private static int randNameIndex = 0;
        private static Random random = new Random();
        /// <summary>
        /// Đối tượng quản lý ID tự tăng có thể tái sử dụng
        /// </summary>
        private static readonly AutoIndexReusablePattern AutoIndexManager = new AutoIndexReusablePattern();

        /// <summary>
        /// Thêm khu bot vào danh sách quản lý
        /// </summary>
        /// <param name="bot">Đối tượng BOT</param>
        public static void MoveQuestBotThroughPortal(KTBot bot, MapTeleport portal)
        {
            bot.CheckQuestCancellation("PortalTransfer");
            using (new BotMutationLease(bot))
            {
                var source = GameManager.MapMgr.DictMaps[bot.CurrentMapCode];
                if (!source.MapTeleportDict.Values.Contains(portal) ||
                    (bot.CurrentPos - new Point(portal.X, portal.Y)).Length > Math.Max(20, portal.Radius) ||
                    bot.CurrentCopyMapID != -1 || (portal.Camp > 0 && portal.Camp != bot.Camp))
                    throw new InvalidOperationException("BOT is not at an accessible world portal.");
                var destination = GameManager.MapMgr.DictMaps[portal.ToMapID];
                if (GameManager.MapGridMgr.GetMapGrid(portal.ToMapID) == null ||
                    Global.InObs(ObjectTypes.OT_MONSTER, portal.ToMapID, portal.ToX, portal.ToY, 1))
                    throw new InvalidOperationException("Portal destination is blocked or unavailable.");
                int oldMap = bot.CurrentMapCode;
                Point oldPos = bot.CurrentPos;
                Point oldGrid = bot.CurrentGrid;
                KTOtherObjectStoryBoard.Instance.Remove(bot);
                RemoveFromMap(bot);
                bot.SetCurrentMapCode(portal.ToMapID);
                bot.CurrentPos = new Point(portal.ToX, portal.ToY);
                bot.CurrentGrid = new Point(portal.ToX / destination.MapGridWidth, portal.ToY / destination.MapGridHeight);
                if (!AddToMap(bot))
                {
                    bot.SetCurrentMapCode(oldMap);
                    bot.CurrentPos = oldPos;
                    bot.CurrentGrid = oldGrid;
                    AddToMap(bot);
                    throw new InvalidOperationException("Could not transfer BOT through portal.");
                }
                NotifyNearClientsToAddSelf(bot);
            }
        }
        private sealed class BotMutationLease : IDisposable
        {
            private readonly KTBot bot;
            public BotMutationLease(KTBot bot)
            {
                this.bot = bot;
                if (!System.Threading.Monitor.TryEnter(bot, 100))
                    throw new TimeoutException("BOT map mutation lock busy: " + bot.RoleID);
            }
            public void Dispose() { System.Threading.Monitor.Exit(bot); }
        }

        public static void Add(KTBot bot)
        {
            using (new BotMutationLease(bot))
            {
                if (!bots.TryAdd(bot.RoleID, bot)) throw new InvalidOperationException("Duplicate BOT ID.");
                try
                {
                    if (!AddToMap(bot)) throw new InvalidOperationException("Cannot place BOT on map " + bot.CurrentMapCode);
                }
                catch { bots.TryRemove(bot.RoleID, out _); throw; }
            }
        }

        public static void AddBotByGm(int mapCode, int posX, int posY, int direction, Point curGrid, bool mainQuestsEnabled = true, int firstMainQuestId = 100)
        {

            var ArmorFemale = new List<int> { 3181, 3182, 3183, 3184, 3185, 3186, 3187, 3188, 3189, 3190, 3191 };
            var ArmorMale = new List<int> { 3171, 3172, 3173, 3174, 3175, 3176, 3177, 3178, 3179, 3180 };
        

            var randomWeapon = new List<int> { 3766, 3776, 3786, 3736, 3746, 3756 };

           
            var helmFemale =new List<int> {3429, 3430, 3431, 3432, 3433, 3434, 3435, 3436, 3437, 3438};
            var helmMale = new List<int> { 3419, 3420, 3421, 3422, 3423, 3424, 3425, 3426, 3427, 3428 };



            var mantleFemale = new List<int> { 3669, 3670, 3671, 3672, 3673, 3674, 3675, 3676, 3677, 3678 };
            var mantleMale = new List<int> { 3619, 3620, 3621, 3622, 3623, 3624, 3625, 3626, 3627, 3628 };
            var randomHorse = new List<int> { 3459, 3460, 3461, 3462, 3463, 3464, 3465, 3466, 3467, 3468, 3469, 3470 };

            KTBot bot = new KTBot();
            /// Nếu có phụ bản
            bot.CurrentCopyMapID = -1;
            bot.SetCurrentMapCode(mapCode);

            /// Hướng quay
            KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
            /// Nếu có thiết lập hướng quay
            if (direction > (int)KiemThe.Entities.Direction.NONE && direction < (int)KiemThe.Entities.Direction.Count)
            {
                bot.CurrentDir = (KiemThe.Entities.Direction)direction;
            }

            if (KTBotManager.randNameIndex < KTBotManager.randBotName.Count)
            {
                bot.RoleName = KTBotManager.randBotName[KTBotManager.randNameIndex];
            }
            else
            {
                bot.RoleName = string.Format("ADNX_{0}", KTBotManager.randNameIndex);
            }
            KTBotManager.randNameIndex++;
            Random rand = new Random();
            
            bot.NextQuestActionAtUtc = DateTime.UtcNow.AddSeconds(RandomDelaySeconds(120, 360));
            bot.m_Level = 100;
            bot.CurrentPos = new System.Windows.Point(posX, posY);
            bot.CurrentGrid = curGrid;
            bot.RouteID = 1;
            bot.RoleSex = rand.Next(0,1);
            if (bot.RoleSex == 0)
            {
                bot.RolePic = 2;
            }
            else
            {
                bot.RolePic = 101;
            }
            var _lst_Faction = KFaction.GetFactions().Where(x => x.nID != 0).ToList();
            if (_lst_Faction.Count() > 0)
            {
                lock(random)
                {
                    bot.FactionID = _lst_Faction[random.Next(_lst_Faction.Count)].nID;
                }
            }
            var skillIds = KFaction.GetRouteSkills(bot.FactionID, bot.RouteID);
            foreach (int skillId in skillIds)
            {
                var skill = KSkill.GetSkillData(skillId);
                if (skill == null)
                    continue;
                if(skill.IsPhysical)
                {
                    bot.FactionSkillId = skill.ID;
                    bot.FactionSkillLevel = skill.MaxSkillLevel;
                    Console.WriteLine(
                    $"[BotSkill] Bot={bot.RoleName}" +
                    $" | Faction={bot.FactionID}" +
                    $" | Route={bot.RouteID}" +
                    $" | SkillID={skill.ID}" +
                    $" | Name={skill.Name}" +
                    $" | Type={skill.Type}" +
                    $" | WeaponLimit={skill.WeaponLimit}");
                    break;
                }
            }
            bot.IsRiding = rand.Next(0,1)==0;
            if (bot.IsRiding)
            {
                bot.ChangeRunSpeed(20, 0, 0);
            }
            else
            {
                bot.ChangeRunSpeed(10, 0, 0);
            }
            bot.ScriptID = -1;
            bot.AIID = -1;
            bot.Camp = -1;
            bot.SeekRange = 1000;
            bot.Tag = "";
            bot.ChangeLifeMax(2000, 0, 0);
            bot.m_CurrentLife = 2000;
            int randSeries = rand.Next(1, 5);
            bot.Equips = new List<GoodsData>();
            var selectedSkill = KSkill.GetSkillData(bot.FactionSkillId);
            // A stationary BOT must not require a faction skill to spawn.
            var availableWeapons = randomWeapon.Where(id =>
            {
                if (!ItemManager._TotalGameItem.TryGetValue(id, out ItemData item))
                    return false;
                return item.DetailType == (int)KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon
                    || item.DetailType == (int)KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon;
            }).ToList();

            var suitableWeapons = availableWeapons.Where(id =>
                selectedSkill != null &&
                ItemManager._TotalGameItem[id].Category == selectedSkill.WeaponLimit)
                .ToList();

            if (suitableWeapons.Count == 0)
            {
                suitableWeapons = availableWeapons;
                bot.FactionSkillId = 0;
                bot.FactionSkillLevel = 1;
                selectedSkill = null;
                Console.WriteLine($"[BotWeaponFallback] Bot={bot.RoleName} Faction={bot.FactionID} Route={bot.RouteID}: spawn without faction skill.");
            }

            if (suitableWeapons.Count == 0)
                throw new InvalidOperationException("BOT weapon list contains no valid weapons.");

            int weaponId = suitableWeapons[rand.Next(suitableWeapons.Count)];

            GoodsData itemGD = new GoodsData()
            {
                GoodsID = weaponId,
                Series = randSeries,
                Forge_level = 16,
            };

            bot.Equips.Add(itemGD);

            // Equip a horse so the riding appearance has a valid HorseID.
            GoodsData itemGDhorse = new GoodsData()
            {
                GoodsID = randomHorse[rand.Next(randomHorse.Count)],
                Series = 0,
                Forge_level = 0,
            };
            bot.Equips.Add(itemGDhorse);

            Console.WriteLine(
                $"[BotWeapon] Bot={bot.RoleName}" +
                $" | Skill={selectedSkill?.Name}" +
                $" | WeaponLimit={selectedSkill?.WeaponLimit}" +
                $" | WeaponID={bot.WeaponID}");


            if (bot.RoleSex == 0)
            {
                GoodsData itemGD1 = new GoodsData()
                {
                    GoodsID = ArmorMale[rand.Next(0, ArmorMale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD1);
                GoodsData itemGD2 = new GoodsData()
                {
                    GoodsID = helmMale[rand.Next(0, helmMale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD2);
                GoodsData itemGD3 = new GoodsData()
                {
                    GoodsID = mantleMale[rand.Next(0, mantleMale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD3);
            }
            else
            {
                GoodsData itemGD1 = new GoodsData()
                {
                    GoodsID = ArmorFemale[rand.Next(0, ArmorFemale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD1);
                GoodsData itemGD2 = new GoodsData()
                {
                    GoodsID = helmFemale[rand.Next(0, helmFemale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD2);
                GoodsData itemGD3 = new GoodsData()
                {
                    GoodsID = mantleFemale[rand.Next(0, mantleFemale.Count - 1)],
                    Series = randSeries,
                    Forge_level = 0,
                };
                bot.Equips.Add(itemGD3);
            }
            

            if (mainQuestsEnabled)
            {
                // Initial BOT combat baseline, not player equipment progression.
                bot.ChangePhysicsDamage(100);
                bot.ChangeAttackRating(1000, 0, 0);
            }
            if (mainQuestsEnabled)
                bot.MainQuestActions.Start(bot, firstMainQuestId);

            KTBotManager.Add(bot);
            try
            {
                KTBotTimerManager.Instance.Add(bot, false);
            }
            catch
            {
                KTBotTimerManager.Instance.Remove(bot);
                KTBotManager.Remove(bot);
                throw;
            }

            Console.WriteLine(
                "[BOT Created] Name={0} | ID={1} | FactionID= {2} | Map={3} | Pos=({4},{5}) | MainQuest={6} | QuestID={7} | State={7}",
                bot.RoleName,
                bot.RoleID,
                bot.FactionID,
                bot.CurrentMapCode,
                bot.CurrentPos.X,
                bot.CurrentPos.Y,
                mainQuestsEnabled,
                bot.QuestController.QuestId,
                bot.QuestController.State);

            global::Server.Tools.LogManager.WriteLog(
                global::Server.Tools.LogTypes.Info,
                string.Format(
                    "[BOT Created] {0} ({1}) MainQuest={2}, QuestID={3}, State={4}",
                    bot.RoleName,
                    bot.RoleID,
                    mainQuestsEnabled,
                    bot.QuestController.QuestId,
                    bot.QuestController.State));
        }

        /// <summary>
        /// Xóa BOT khỏi danh sách
        /// </summary>
        /// <param name="areaID">ID BOT</param>
        public static void Remove(int areaID)
        {
            if (bots.TryGetValue(areaID, out KTBot bot)) Remove(bot);
        }

        public static void Remove(KTBot bot)
        {
            if (bot == null) return;
            using (new BotMutationLease(bot))
            {
                if (!bots.TryGetValue(bot.RoleID, out KTBot existing) || !ReferenceEquals(existing, bot)) return;
                // Do not free the population slot before map removal succeeds.
                RemoveFromMap(bot);
                if (bots.TryRemove(bot.RoleID, out _)) AutoIndexManager.ReturnID(bot.RoleID);
            }
        }

        private static bool AddToMap(KTBot bot)
        {
            MapGrid mapGrid;
            lock (GameManager.MapGridMgr.DictGrids)
            {
                mapGrid = GameManager.MapGridMgr.GetMapGrid(bot.CurrentMapCode);
            }

            if (null == mapGrid)
            {
                return false;
            }

            /// Thêm BOT vào Map
            if (mapGrid.MoveObject(-1, -1, (int) bot.CurrentPos.X, (int) (bot.CurrentPos.Y), bot))
            {
                //  KTBotManager.NotifyNearClientsToAddSelf(bot);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Xóa BOT tương ứng khỏi bản đồ
        /// </summary>
        /// <param name="bot">Đối tượng BOT</param>
        /// <returns></returns>
        private static bool RemoveFromMap(KTBot bot)
        {
            MapGrid mapGrid;
            lock (GameManager.MapGridMgr.DictGrids)
            {
                mapGrid = GameManager.MapGridMgr.GetMapGrid(bot.CurrentMapCode);
            }

            if (null == mapGrid)
            {
                return false;
            }

            if (KTBotManager.bots.TryGetValue(bot.RoleID, out _))
            {
                /// Xóa BOT khỏi Map
                mapGrid.RemoveObject(bot);
                KTBotManager.NotifyNearClientsToRemoveSelf(bot);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Trả về danh sách BOT trong bản đồ hoặc phụ bản tương ứng
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapID"></param>
        public static List<KTBot> GetMapBots(int mapCode, int copyMapID = -1)
        {
            List<KTBot> results = new List<KTBot>();

            if (mapCode <= 0)
            {
                return results;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            if (null == mapGrid)
            {
                return results;
            }

            /// Duyệt toàn bộ danh sách NPC
            foreach (KeyValuePair<int, KTBot> item in KTBotManager.bots)
            {
                if (item.Value.CurrentMapCode == mapCode && (copyMapID == -1 || item.Value.CurrentCopyMapID == copyMapID))
                {
                    results.Add(item.Value);
                }
            }

            /// Trả về kết quả
            return results;
        }

        /// <summary>
        /// Xóa toàn bộ BOT khỏi bản đồ hoặc phụ bản tương ứng
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="copyMapID"></param>
        public static void RemoveMapBots(int mapCode, int copyMapID = -1)
        {
            if (mapCode <= 0)
            {
                return;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[mapCode];
            if (null == mapGrid)
            {
                return;
            }

            List<int> keysToDel = new List<int>();

            /// Duyệt toàn bộ danh sách NPC
            foreach (KeyValuePair<int, KTBot> item in KTBotManager.bots)
            {
                if (item.Value.CurrentMapCode == mapCode && (copyMapID == -1 || item.Value.CurrentCopyMapID == copyMapID))
                {
                    mapGrid.RemoveObject(item.Value);
                    keysToDel.Add(item.Key);
                }
            }

            /// Xóa các bản ghi đã tìm bên trên
            foreach (int key in keysToDel)
            {
                KTBotManager.bots.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Thông báo toàn bộ người chơi xung quanh thêm đối tượng vào danh sách hiển thị
        /// </summary>
        /// <param name="bot">Đối tượng BOT</param>
        private static void NotifyNearClientsToAddSelf(KTBot bot)
        {
            List<Object> objsList = Global.GetAll9Clients(bot);

            if (null == objsList)
                return;

            KPlayer client = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                client = objsList[i] as KPlayer;
                if (null == client)
                {
                    continue;
                }

                /// Thực hiện cập nhật đối tượng
                //Global.GameClientMoveGrid(client);

                GameManager.ClientMgr.NotifyMySelfBot(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, bot);
            }
        }

        /// <summary>
        /// Thông báo toàn bộ người chơi xung quanh xóa đối tượng khỏi danh sách hiển thị
        /// </summary>
        /// <param name="bot">Đối tượng BOT</param>
        private static void NotifyNearClientsToRemoveSelf(KTBot bot)
        {
            List<Object> objsList = Global.GetAll9Clients2(bot.CurrentMapCode, (int) bot.CurrentPos.X, (int) bot.CurrentPos.Y, bot.CurrentCopyMapID);
            if (null == objsList)
                return;

            KPlayer client = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                client = objsList[i] as KPlayer;
                if (null == client)
                {
                    continue;
                }

                /// Thực hiện cập nhật đối tượng
                //Global.GameClientMoveGrid(client);

                GameManager.ClientMgr.NotifyMySelfDelBot(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, bot.CurrentMapCode, bot.RoleID);
            }
        }

        /// <summary>
        /// Gửi danh sách BOT xung quanh người chơi
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList">Danh sách BOT</param>
        public static void SendMySelfBots(SocketListener sl, TCPOutPacketPool pool, KPlayer client, List<Object> objsList)
        {
            if (null == objsList)
            {
                return;
            }
            KTBot bot = null;
            for (int i = 0; i < objsList.Count; i++)
            {
                bot = objsList[i] as KTBot;
                if (null == bot)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfBot(sl, pool, client, bot);
            }
        }

        /// <summary>
        /// Xóa BOT với người chơi tương ứng
        /// </summary>
        /// <param name="sl"></param>
        /// <param name="pool"></param>
        /// <param name="client"></param>
        /// <param name="objsList">Danh sách BOT</param>
        public static void DelMySelfBots(SocketListener sl, TCPOutPacketPool pool, KPlayer client, List<Object> objsList)
        {
            if (null == objsList)
            {
                return;
            }
            KTBot bot;
            for (int i = 0; i < objsList.Count; i++)
            {
                bot = objsList[i] as KTBot;
                if (null == bot)
                {
                    continue;
                }

                GameManager.ClientMgr.NotifyMySelfDelBot(sl, pool, client, client.CurrentMapCode, bot.RoleID);
            }
        }
    }
}
