using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.BaoThach;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.Medal
{
    public static class MedalManager
    {
        public const int MedalDetailType = 38;
        public const string RuntimePrefix = "MEDAL:v2:";
        public const string ConfigPath = "Config/KT_Interactive/Medal.xml";
        public const string PropsConfigPath = "Config/KT_Interactive/MedalProps.xml";

        private const int MeritFreeDailyRecordKey = 73670001;
        private const int MeritOnlineMinutesDailyRecordKey = 73670002;
        private const int StarCopperItemID = 29007;
        private const int StarSilverItemID = 29012;
        private const int StarRedItemID = 29014;

        private static MedalConfig config = new MedalConfig();
        private static MedalPropsConfig propsConfig = new MedalPropsConfig();
        private static Dictionary<int, MedalQualityConfig> qualitiesByItemID = new Dictionary<int, MedalQualityConfig>();
        private static Dictionary<string, List<MedalAttributeConfig>> propsByQualityLevel = new Dictionary<string, List<MedalAttributeConfig>>();
        private static Dictionary<string, MedalAttributeConfig> attributesByIdLevel = new Dictionary<string, MedalAttributeConfig>();

        public static void Init()
        {
            config = BaoThachManager.LoadConfig<MedalConfig>(ConfigPath) ?? new MedalConfig();
            propsConfig = BaoThachManager.LoadConfig<MedalPropsConfig>(PropsConfigPath) ?? new MedalPropsConfig();
            BuildLookups();
            LogManager.WriteLog(LogTypes.Info, string.Format("[Medal] Loaded qualities={0}, props={1}", config.Qualities.Count, propsConfig.Props.Count));
        }

        public static bool IsMedal(ItemData itemData)
        {
            return itemData != null && itemData.DetailType == MedalDetailType;
        }

        public static string CreateInitialOtherParam(ItemData itemData)
        {
            if (itemData == null)
            {
                LogManager.WriteLog(LogTypes.Item, "[Medal] CreateInitialOtherParam failed: itemData is null");
                return string.Empty;
            }

            MedalQualityConfig qualityConfig = GetQualityByItemID(itemData.ItemID);
            if (qualityConfig == null)
            {
                LogManager.WriteLog(LogTypes.Item, string.Format("[Medal] CreateInitialOtherParam failed: missing quality config itemID={0}", itemData.ItemID));
                return string.Empty;
            }

            int minCount = qualityConfig.InitialAttributeCount;
            int maxCount = qualityConfig.MaxAttributeCount;
            if (maxCount < minCount)
            {
                maxCount = minCount;
            }

            int initialCount = minCount;
            if (maxCount > minCount)
            {
                initialCount = KTGlobal.GetRandomNumber(minCount, maxCount);
            }

            // Pool mở rộng: bao gồm tất cả phẩm chất từ 1 đến phẩm hiện tại, giới hạn 3 dòng/phẩm
            List<MedalAttributeConfig> pool = BuildMultiQualityPool(qualityConfig.Quality, new Dictionary<int, int>());

            int count = Math.Min(initialCount, pool.Count);
            if (count <= 0)
            {
                LogManager.WriteLog(LogTypes.Item, string.Format("[Medal] CreateInitialOtherParam failed: missing props quality={0}, itemID={1}", qualityConfig.Quality, itemData.ItemID));
                return string.Empty;
            }

            MedalData data = new MedalData
            {
                Quality = qualityConfig.Quality,
                Attributes = new List<MedalAttributeData>(count)
            };

            Dictionary<int, int> qualityCount = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                // Lọc lại pool: loại bỏ những thuộc tính có phẩm chất đã đủ 3 dòng
                List<MedalAttributeConfig> available = pool.Where(x =>
                {
                    int cnt;
                    return !qualityCount.TryGetValue(x.Quality, out cnt) || cnt < MaxAttributesPerQuality;
                }).ToList();

                if (available.Count <= 0)
                {
                    break;
                }

                int index = KTGlobal.GetRandomNumber(0, available.Count - 1);
                MedalAttributeConfig selected = available[index];
                pool.RemoveAll(x => x.Id == selected.Id);

                int cnt2;
                qualityCount[selected.Quality] = qualityCount.TryGetValue(selected.Quality, out cnt2) ? cnt2 + 1 : 1;

                data.Attributes.Add(new MedalAttributeData
                {
                    Id = selected.Id,
                    Level = selected.Level,
                    Value = GetRandomValue(selected),
                    Locked = false
                });
            }

            return Serialize(data);
        }

        public static bool TryGetMedalData(GoodsData goodsData, out MedalData medalData)
        {
            medalData = null;
            if (goodsData == null || goodsData.OtherParams == null)
            {
                return false;
            }

            string rawData;
            if (!goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out rawData))
            {
                return false;
            }

            return TryDeserialize(rawData, out medalData);
        }

        public static MedalOperationResult UpgradeAttribute(KPlayer player, int medalDbId, string reason = "MEDAL_UPGRADE_ATTRIBUTE")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            List<MedalAttributeData> candidates = medalData.Attributes
                .Where(x => x.Level > 0 && x.Level < qualityConfig.MaxAttributeLevel)
                .ToList();
            if (candidates.Count <= 0)
            {
                return MedalOperationResult.Fail("Tất cả thuộc tính đã đạt cấp tối đa.", medal);
            }

            MedalAttributeData selected = candidates[KTGlobal.GetRandomNumber(0, candidates.Count - 1)];
            MedalAttributeConfig attributeConfig = GetAttribute(selected.Id, medalData.Quality, selected.Level);
            int step;
            if (!TryGetWholeNumberStep(attributeConfig, out step))
            {
                return MedalOperationResult.Fail("Thuộc tính có bước tăng thập phân, cần chốt scale runtime trước khi nâng cấp.", medal, medalData);
            }

            MedalAttributeUpgradeCostConfig cost = GetAttributeUpgradeCost(medalData.Quality, selected.Level);
            if (cost == null)
            {
                return MedalOperationResult.Fail("Thiếu cấu hình tiêu hao nâng cấp thuộc tính.", medal, medalData);
            }

            string message;
            if (!CanSpendCost(player, cost.Merit, cost.StarCopper, cost.StarSilver, 0, cost.Money, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            if (!SpendCost(player, cost.Merit, cost.StarCopper, cost.StarSilver, 0, cost.Money, reason, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            selected.Level++;
            selected.Value += step;

            if (!SaveMedalData(player, medal, medalData, reason))
            {
                return MedalOperationResult.Fail("Cập nhật dữ liệu huy chương thất bại.", medal, medalData);
            }

            RefreshMedalIfEquipped(player, medal);
            return MedalOperationResult.Ok("Nâng cấp thuộc tính huy chương thành công.", medal, medalData, selected);
        }

        public static MedalOperationResult UpgradeQuality(KPlayer player, int medalDbId, string reason = "MEDAL_UPGRADE_QUALITY")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            MedalQualityUpgradeRecipeConfig recipe = config.QualityUpgradeRecipes
                .FirstOrDefault(x => x.FromQuality == medalData.Quality);
            if (recipe == null)
            {
                return MedalOperationResult.Fail("Thiếu cấu hình nâng phẩm huy chương.", medal, medalData);
            }

            if (medal.GoodsID != recipe.MainItemID)
            {
                return MedalOperationResult.Fail("Huy chương chính không đúng cấu hình nâng phẩm.", medal, medalData);
            }

            if (medalData.Attributes.Any(x => x.Level < recipe.MinAttributeLevel))
            {
                return MedalOperationResult.Fail("Toàn bộ thuộc tính chưa đạt cấp yêu cầu để nâng phẩm.", medal, medalData);
            }

            MedalQualityConfig nextQualityConfig = config.Qualities.FirstOrDefault(x => x.Quality == recipe.ToQuality);
            if (nextQualityConfig == null)
            {
                return MedalOperationResult.Fail("Thiếu cấu hình phẩm chất kế tiếp.", medal, medalData);
            }

            string message;
            if (!CanSpendCost(player, recipe.Merit, 0, recipe.StarSilver, 0, recipe.Money, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            if (!HasItemCount(player, recipe.MaterialItemID, recipe.MaterialCount, medal.Id))
            {
                return MedalOperationResult.Fail("Không đủ phôi huy chương để nâng phẩm.", medal, medalData);
            }

            bool materialBound;
            if (!ConsumeItemCountByItemID(player, recipe.MaterialItemID, recipe.MaterialCount, reason, out materialBound, medal.Id))
            {
                return MedalOperationResult.Fail("Trừ phôi huy chương thất bại.", medal, medalData);
            }

            if (!SpendCost(player, recipe.Merit, 0, recipe.StarSilver, 0, recipe.Money, reason, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            medalData.Quality = recipe.ToQuality;
            foreach (MedalAttributeData attribute in medalData.Attributes)
            {
                MedalAttributeConfig attributeConfig = GetAttribute(attribute.Id, medalData.Quality, 1);
                attribute.Level = 1;
                if (attributeConfig != null)
                {
                    attribute.Value = GetRandomValue(attributeConfig);
                }
                attribute.Locked = false;
            }
            AddQualityUpgradeAttributes(medalData, nextQualityConfig);

            int resultBinding = medal.Binding > 0 || materialBound ? 1 : 0;
            if (!UpdateMedalItem(player, medal, recipe.ResultItemID, resultBinding, Serialize(medalData), reason))
            {
                return MedalOperationResult.Fail("Cập nhật phẩm chất huy chương thất bại.", medal, medalData);
            }

            medal.GoodsID = recipe.ResultItemID;
            medal.Binding = resultBinding;
            RefreshMedalIfEquipped(player, medal);
            return MedalOperationResult.Ok("Nâng phẩm huy chương thành công.", medal, medalData);
        }

        public static MedalOperationResult RefineMedal(KPlayer player, int medalDbId, IList<int> lockedAttributeIds, string reason = "MEDAL_REFINE")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            MedalRefineCostConfig cost = config.RefineCosts.FirstOrDefault(x => x.Quality == medalData.Quality);
            if (cost == null)
            {
                return MedalOperationResult.Fail("Thiếu cấu hình tẩy luyện huy chương.", medal, medalData);
            }

            if (medalData.Attributes.Any(x => x.Level < cost.MinAttributeLevel))
            {
                return MedalOperationResult.Fail("Toàn bộ thuộc tính chưa đạt cấp yêu cầu để tẩy luyện.", medal, medalData);
            }

            HashSet<int> lockedIds = new HashSet<int>();
            if (lockedAttributeIds != null)
            {
                foreach (int val in lockedAttributeIds)
                {
                    if (val >= 0 && val < medalData.Attributes.Count)
                    {
                        lockedIds.Add(medalData.Attributes[val].Id);
                    }
                    else if (val >= 100)
                    {
                        lockedIds.Add(val);
                    }
                }
            }

            if (lockedIds.Any(x => medalData.Attributes.All(attribute => attribute.Id != x)))
            {
                return MedalOperationResult.Fail("Danh sách khóa có thuộc tính không tồn tại trên huy chương.", medal, medalData);
            }

            string message;
            int lockItemCount = lockedIds.Count * Math.Max(0, cost.LockItemPerAttribute);
            if (!CanSpendCost(player, 0, 0, cost.StarSilver, 0, cost.Money, out message) ||
                !HasItemCount(player, cost.RefineItem, 1) ||
                (lockItemCount > 0 && !HasItemCount(player, cost.LockItem, lockItemCount)))
            {
                return MedalOperationResult.Fail("Không đủ nguyên liệu tẩy luyện huy chương.", medal, medalData);
            }

            MedalData refinedData;
            if (!TryBuildRefinedData(medalData, lockedIds, out refinedData, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            bool ignoredBound;
            if (!ConsumeItemCountByItemID(player, cost.RefineItem, 1, reason, out ignoredBound))
            {
                return MedalOperationResult.Fail("Trừ Tẩy Luyện Châu thất bại.", medal, medalData);
            }

            if (lockItemCount > 0 && !ConsumeItemCountByItemID(player, cost.LockItem, lockItemCount, reason, out ignoredBound))
            {
                return MedalOperationResult.Fail("Trừ Khóa Cố Định thất bại.", medal, medalData);
            }

            if (!SpendCost(player, 0, 0, cost.StarSilver, 0, cost.Money, reason, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            // Lưu thuộc tính mới vào TempAttributes và giữ lại Attributes cũ
            medalData.TempAttributes = refinedData.Attributes;

            if (!SaveMedalData(player, medal, medalData, reason))
            {
                return MedalOperationResult.Fail("Cập nhật dữ liệu tẩy luyện thất bại.", medal, medalData);
            }

            RefreshMedalIfEquipped(player, medal);
            return MedalOperationResult.Ok("Tẩy luyện huy chương thành công. Vui lòng Xác nhận hoặc Hủy để lưu kết quả.", medal, medalData);
        }

        public static MedalOperationResult AcceptRefine(KPlayer player, int medalDbId, string reason = "MEDAL_ACCEPT_REFINE")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            if (medalData.TempAttributes == null || medalData.TempAttributes.Count == 0)
            {
                return MedalOperationResult.Fail("Không có thuộc tính tẩy luyện tạm thời cần xác nhận.", medal, medalData);
            }

            // Ghi đè thuộc tính mới lên thuộc tính cũ
            medalData.Attributes = medalData.TempAttributes;
            medalData.TempAttributes = new List<MedalAttributeData>(); // Xóa thuộc tính tạm thời

            if (!SaveMedalData(player, medal, medalData, reason))
            {
                return MedalOperationResult.Fail("Xác nhận kết quả tẩy luyện thất bại.", medal, medalData);
            }

            RefreshMedalIfEquipped(player, medal);
            return MedalOperationResult.Ok("Xác nhận thay thế thuộc tính mới thành công.", medal, medalData);
        }

        public static MedalOperationResult CancelRefine(KPlayer player, int medalDbId, string reason = "MEDAL_CANCEL_REFINE")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            if (medalData.TempAttributes == null || medalData.TempAttributes.Count == 0)
            {
                return MedalOperationResult.Fail("Không có thuộc tính tẩy luyện tạm thời cần hủy.", medal, medalData);
            }

            // Chỉ cần xóa thuộc tính tạm thời, giữ nguyên thuộc tính cũ
            medalData.TempAttributes = new List<MedalAttributeData>();

            if (!SaveMedalData(player, medal, medalData, reason))
            {
                return MedalOperationResult.Fail("Hủy kết quả tẩy luyện thất bại.", medal, medalData);
            }

            RefreshMedalIfEquipped(player, medal);
            return MedalOperationResult.Ok("Hủy kết quả tẩy luyện thành công, giữ lại thuộc tính cũ.", medal, medalData);
        }

        public static MedalOperationResult UnlockMedal(KPlayer player, int medalDbId, string reason = "MEDAL_UNLOCK")
        {
            GoodsData medal;
            MedalData medalData;
            MedalQualityConfig qualityConfig;
            MedalOperationResult validation = ValidateMedal(player, medalDbId, out medal, out medalData, out qualityConfig);
            if (!validation.Success)
            {
                return validation;
            }

            if (medal.Binding <= 0)
            {
                return MedalOperationResult.Fail("Huy chương đang không khóa.", medal, medalData);
            }

            MedalUnlockCostConfig unlock = config.Unlock ?? new MedalUnlockCostConfig();
            string message;
            if (!CanSpendCost(player, 0, 0, 0, unlock.StarRed, unlock.Money, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            if (!SpendCost(player, 0, 0, 0, unlock.StarRed, unlock.Money, reason, out message))
            {
                return MedalOperationResult.Fail(message, medal, medalData);
            }

            if (!UpdateMedalItem(player, medal, medal.GoodsID, 0, Serialize(medalData), reason))
            {
                return MedalOperationResult.Fail("Mở khóa huy chương thất bại.", medal, medalData);
            }

            medal.Binding = 0;
            return MedalOperationResult.Ok("Mở khóa huy chương thành công.", medal, medalData);
        }

        public static List<KMagicAttrib> BuildMagicAttributes(GoodsData goodsData)
        {
            MedalData medalData;
            if (!TryGetMedalData(goodsData, out medalData))
            {
                return new List<KMagicAttrib>();
            }

            List<KMagicAttrib> result = new List<KMagicAttrib>();
            foreach (MedalAttributeData attributeData in medalData.Attributes)
            {
                MedalAttributeConfig configLine = GetAttribute(attributeData.Id, medalData.Quality, attributeData.Level);
                string alias = configLine != null ? configLine.Alias : null;

                int value = attributeData.Value;
                if (value <= 0)
                {
                    if (configLine != null)
                    {
                        value = configLine.Score;
                    }
                }

                if (value <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(alias))
                {
                    continue;
                }

                result.Add(BaoThachManager.CreateMagicAttribute(alias, value));
            }

            return result;
        }

        public static bool TryConsumeMerit(KPlayer player, int amount, string reason)
        {
            if (player == null || amount <= 0)
            {
                return false;
            }

            EnsureDailyMerit(player);
            int free = GetDailyMeritFree(player);
            int paid = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MedalMeritPaid, 0);
            if ((long)free + paid < amount)
            {
                return false;
            }

            int consumeFree = Math.Min(free, amount);
            int consumePaid = amount - consumeFree;
            SetDailyMeritFree(player, free - consumeFree);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MedalMeritPaid, paid - consumePaid, true);
            NotifyMeritChanged(player, string.Format("Công Trạng -{0}. Còn lại: {1}.", amount, (long)free + paid - amount));
            LogManager.WriteLog(LogTypes.Item, string.Format("[Medal][{0}][{1}] Consume merit={2}, free={3}, paid={4}, reason={5}",
                player.RoleID, player.RoleName, amount, consumeFree, consumePaid, reason));
            return true;
        }

        public static void AddPaidMerit(KPlayer player, int amount, string reason)
        {
            if (player == null || amount <= 0)
            {
                return;
            }

            int paid = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MedalMeritPaid, 0);
            int newPaid = (int)Math.Min(int.MaxValue, (long)paid + amount);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MedalMeritPaid, newPaid, true);
            if (newPaid > paid)
            {
                NotifyMeritChanged(player, string.Format("Công Trạng tích lũy +{0}. Hiện có: {1}.", newPaid - paid, newPaid));
            }
            LogManager.WriteLog(LogTypes.Item, string.Format("[Medal][{0}][{1}] Add paid merit={2}, reason={3}", player.RoleID, player.RoleName, amount, reason));
        }

        public static int AddOnlineMerit(KPlayer player, int totalOnlineMinutes, string reason)
        {
            if (player == null || totalOnlineMinutes <= 0)
            {
                return 0;
            }

            EnsureDailyMerit(player);
            MedalMeritConfig meritConfig = config.Merit ?? new MedalMeritConfig();
            int minutesPerReward = Math.Max(0, meritConfig.OnlineMinutesPerReward);
            int reward = Math.Max(0, meritConfig.OnlineReward);
            int dailyFreeMax = Math.Max(0, meritConfig.DailyFreeMax);
            if (minutesPerReward <= 0 || reward <= 0 || dailyFreeMax <= 0)
            {
                return 0;
            }

            int currentMinutes = Math.Max(0, totalOnlineMinutes);
            int countedMinutes = GetDailyMeritOnlineMinutes(player);
            
            int maxSteps = dailyFreeMax / reward; // 100 / 10 = 10 mốc

            int oldRewardSteps = countedMinutes / minutesPerReward;
            int newRewardSteps = currentMinutes / minutesPerReward;

            if (oldRewardSteps > maxSteps)
            {
                oldRewardSteps = maxSteps;
            }
            if (newRewardSteps > maxSteps)
            {
                newRewardSteps = maxSteps;
            }

            if (newRewardSteps <= oldRewardSteps)
            {
                return 0;
            }

            int addable = (newRewardSteps - oldRewardSteps) * reward;
            SetDailyMeritOnlineMinutes(player, newRewardSteps * minutesPerReward);
            if (addable <= 0)
            {
                return 0;
            }

            // Cộng thẳng vào PaidMerit (Công Trạng duy nhất)
            int paid = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MedalMeritPaid, 0);
            int newPaid = (int)Math.Min(int.MaxValue, (long)paid + addable);
            Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MedalMeritPaid, newPaid, true);

            NotifyMeritChanged(player, string.Format("Công Trạng online +{0}. Nhận từ online hôm nay: {1}/{2}. Tổng hiện có: {3}.", addable, newRewardSteps * reward, dailyFreeMax, newPaid));
            LogManager.WriteLog(LogTypes.Item, string.Format("[Medal][{0}][{1}] Add online merit={2}, totalMinutes={3}, reason={4}",
                player.RoleID, player.RoleName, addable, currentMinutes, reason));
            return addable;
        }

        public static MedalMeritSnapshot GetMeritSnapshot(KPlayer player)
        {
            if (player == null)
            {
                return new MedalMeritSnapshot();
            }

            EnsureDailyMerit(player);
            return new MedalMeritSnapshot
            {
                DayId = GetCurrentDayID(),
                Free = 0,
                Paid = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MedalMeritPaid, 0),
                OnlineMinutes = GetDailyMeritOnlineMinutes(player)
            };
        }

        public static void EnsureDailyMerit(KPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (player.GetValueOfDailyRecore(MeritOnlineMinutesDailyRecordKey) < 0)
            {
                SetDailyMeritOnlineMinutes(player, 0);
            }
        }

        private static MedalOperationResult ValidateMedal(KPlayer player, int medalDbId, out GoodsData medal, out MedalData medalData, out MedalQualityConfig qualityConfig)
        {
            medal = null;
            medalData = null;
            qualityConfig = null;

            if (player == null)
            {
                return MedalOperationResult.Fail("Người chơi không hợp lệ.");
            }

            medal = Global.GetGoodsByDbID(player, medalDbId);
            if (medal == null)
            {
                return MedalOperationResult.Fail("Không tìm thấy huy chương.");
            }

            if (!ItemManager.IsMedal(medal))
            {
                return MedalOperationResult.Fail("Vật phẩm không phải huy chương.", medal);
            }

            if (!TryGetMedalData(medal, out medalData))
            {
                return MedalOperationResult.Fail("Dữ liệu huy chương không hợp lệ.", medal);
            }

            int quality = medalData.Quality;
            qualityConfig = config.Qualities.FirstOrDefault(x => x.Quality == quality);
            if (qualityConfig == null)
            {
                return MedalOperationResult.Fail("Thiếu cấu hình phẩm chất huy chương.", medal, medalData);
            }

            return MedalOperationResult.Ok(string.Empty, medal, medalData);
        }

        private static MedalAttributeUpgradeCostConfig GetAttributeUpgradeCost(int quality, int fromLevel)
        {
            return config.AttributeUpgradeCosts.FirstOrDefault(x =>
                x.Quality == quality &&
                fromLevel >= x.FromLevel &&
                fromLevel < x.ToLevel);
        }

        private static bool TryGetWholeNumberStep(MedalAttributeConfig attributeConfig, out int step)
        {
            step = 0;
            if (attributeConfig == null || string.IsNullOrEmpty(attributeConfig.Step))
            {
                return false;
            }

            decimal decimalStep;
            if (!decimal.TryParse(attributeConfig.Step.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimalStep))
            {
                return false;
            }

            if (decimalStep <= 0 || decimalStep != Math.Truncate(decimalStep))
            {
                return false;
            }

            step = (int)decimalStep;
            return step > 0;
        }

        private static bool CanSpendCost(KPlayer player, int merit, int starCopper, int starSilver, int starRed, string money, out string message)
        {
            message = string.Empty;
            if (player == null)
            {
                message = "Người chơi không hợp lệ.";
                return false;
            }

            MedalMeritSnapshot snapshot = GetMeritSnapshot(player);
            if ((long)snapshot.Free + snapshot.Paid < merit)
            {
                message = "Không đủ Điểm Công Trạng.";
                return false;
            }

            if (starCopper > 0 && !HasItemCount(player, StarCopperItemID, starCopper))
            {
                message = "Không đủ Sao Đồng.";
                return false;
            }

            if (starSilver > 0 && !HasItemCount(player, StarSilverItemID, starSilver))
            {
                message = "Không đủ Sao Bạc.";
                return false;
            }

            if (starRed > 0 && !HasItemCount(player, StarRedItemID, starRed))
            {
                message = "Không đủ Sao Đỏ.";
                return false;
            }

            MoneyType moneyType;
            int amount;
            if (TryParseMoneyCost(money, out moneyType, out amount) && amount > 0)
            {
                if (moneyType == MoneyType.Bac)
                {
                    int totalMoney = player.Money + player.BoundMoney;
                    if (totalMoney < amount)
                    {
                        message = "Không đủ Bạc.";
                        return false;
                    }
                }
                else if (!KTGlobal.IsHaveMoney(player, amount, moneyType))
                {
                    message = "Không đủ bạc/đồng.";
                    return false;
                }
            }

            return true;
        }

        private static bool SpendCost(KPlayer player, int merit, int starCopper, int starSilver, int starRed, string money, string reason, out string message)
        {
            message = string.Empty;
            bool ignoredBound;
            if (starCopper > 0 && !ConsumeItemCountByItemID(player, StarCopperItemID, starCopper, reason, out ignoredBound))
            {
                message = "Trừ Sao Đồng thất bại.";
                return false;
            }

            if (starSilver > 0 && !ConsumeItemCountByItemID(player, StarSilverItemID, starSilver, reason, out ignoredBound))
            {
                message = "Trừ Sao Bạc thất bại.";
                return false;
            }

            if (starRed > 0 && !ConsumeItemCountByItemID(player, StarRedItemID, starRed, reason, out ignoredBound))
            {
                message = "Trừ Sao Đỏ thất bại.";
                return false;
            }

            MoneyType moneyType;
            int amount;
            if (TryParseMoneyCost(money, out moneyType, out amount) && amount > 0)
            {
                if (moneyType == MoneyType.Bac)
                {
                    int boundMoney = player.BoundMoney;
                    int subBound = Math.Min(boundMoney, amount);
                    int subMoney = amount - subBound;

                    if (subBound > 0)
                    {
                        if (!KTGlobal.SubMoney(player, subBound, MoneyType.BacKhoa, reason).IsOK)
                        {
                            message = "Trừ Bạc khóa thất bại.";
                            return false;
                        }
                    }

                    if (subMoney > 0)
                    {
                        if (!KTGlobal.SubMoney(player, subMoney, MoneyType.Bac, reason).IsOK)
                        {
                            message = "Trừ Bạc thường thất bại.";
                            return false;
                        }
                    }
                }
                else if (!KTGlobal.SubMoney(player, amount, moneyType, reason).IsOK)
                {
                    message = "Trừ bạc/đồng thất bại.";
                    return false;
                }
            }

            if (merit > 0 && !TryConsumeMerit(player, merit, reason))
            {
                message = "Trừ Điểm Công Trạng thất bại.";
                return false;
            }

            return true;
        }

        private static bool TryParseMoneyCost(string money, out MoneyType moneyType, out int amount)
        {
            moneyType = MoneyType.Bac;
            amount = 0;
            if (string.IsNullOrEmpty(money))
            {
                return false;
            }

            string[] fields = money.Split(':');
            if (fields.Length != 2)
            {
                return false;
            }

            int type;
            if (!int.TryParse(fields[0], out type) || !int.TryParse(fields[1], out amount))
            {
                return false;
            }

            if (amount <= 0)
            {
                amount = 0;
                return true;
            }

            moneyType = (MoneyType)type;
            return true;
        }

        private static bool HasItemCount(KPlayer player, int itemID, int count, int excludeDbId = -1)
        {
            if (itemID <= 0 || count <= 0)
            {
                return count <= 0;
            }

            if (excludeDbId <= 0)
            {
                return Global.GetTotalGoodsCountByID(player, itemID) >= count;
            }

            int total = 0;
            if (player == null || player.GoodsDataList == null)
            {
                return false;
            }

            lock (player.GoodsDataList)
            {
                foreach (GoodsData item in player.GoodsDataList)
                {
                    if (item == null || item.GoodsID != itemID || item.GCount <= 0 || item.Id == excludeDbId)
                    {
                        continue;
                    }

                    if (Global.IsGoodsTimeOver(item) || Global.IsGoodsNotReachStartTime(item))
                    {
                        continue;
                    }

                    total += item.GCount;
                }
            }

            return total >= count;
        }

        private static bool ConsumeItemCountByItemID(KPlayer player, int itemID, int count, string reason, out bool hasBoundItem, int excludeDbId = -1)
        {
            hasBoundItem = false;
            if (count <= 0)
            {
                return true;
            }

            List<GoodsData> candidates = GetBagItems(player, itemID);
            if (excludeDbId > 0)
            {
                candidates = candidates.Where(x => x.Id != excludeDbId).ToList();
            }

            candidates = candidates
                .OrderBy(x => x.Binding > 0 ? 1 : 0)
                .ThenBy(x => x.Id)
                .ToList();

            int left = count;
            foreach (GoodsData item in candidates)
            {
                if (left <= 0)
                {
                    break;
                }

                int removeCount = Math.Min(left, item.GCount);
                if (item.Binding > 0)
                {
                    hasBoundItem = true;
                }

                if (!ItemManager.RemoveItemByCount(player, item, removeCount, reason))
                {
                    return false;
                }

                left -= removeCount;
            }

            return left <= 0;
        }

        private static List<GoodsData> GetBagItems(KPlayer player, int itemID)
        {
            List<GoodsData> result = new List<GoodsData>();
            if (player == null || player.GoodsDataList == null)
            {
                return result;
            }

            lock (player.GoodsDataList)
            {
                foreach (GoodsData item in player.GoodsDataList)
                {
                    if (item == null || item.GoodsID != itemID || item.GCount <= 0)
                    {
                        continue;
                    }

                    if (Global.IsGoodsTimeOver(item) || Global.IsGoodsNotReachStartTime(item))
                    {
                        continue;
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        private static bool SaveMedalData(KPlayer player, GoodsData medal, MedalData medalData, string reason)
        {
            return ItemManager.UpdateOtherParameter(player, ItemPramenter.Pram_3, Serialize(medalData), medal, reason, true);
        }

        private static bool UpdateMedalItem(KPlayer player, GoodsData medal, int itemID, int binding, string payload, string reason)
        {
            Dictionary<UPDATEITEM, object> updates = new Dictionary<UPDATEITEM, object>
            {
                { UPDATEITEM.ROLEID, player.RoleID },
                { UPDATEITEM.ITEMDBID, medal.Id },
                { UPDATEITEM.GOODSID, itemID },
                { UPDATEITEM.BINDING, binding },
                { UPDATEITEM.OTHER_PRAM, new Dictionary<ItemPramenter, string> { { ItemPramenter.Pram_3, payload } } }
            };

            return ItemManager.UpdateItemPrammenter(updates, medal, player, reason, true);
        }

        private static void RefreshMedalIfEquipped(KPlayer player, GoodsData medal)
        {
            if (player == null || medal == null)
            {
                return;
            }

            KPlayerEquipBody equipBody = player.GetPlayEquipBody();
            if (equipBody == null)
            {
                return;
            }

            lock (equipBody.EquipBody)
            {
                if (!equipBody.EquipBody.Values.Any(x => x != null && x._GoodDatas != null && x._GoodDatas.Id == medal.Id))
                {
                    return;
                }
            }

            equipBody.RefreshEquip();
        }

        private static void AddQualityUpgradeAttributes(MedalData medalData, MedalQualityConfig qualityConfig)
        {
            int capacity = Math.Max(0, qualityConfig.MaxAttributeCount - medalData.Attributes.Count);
            if (capacity <= 0)
            {
                return;
            }

            int addCount = Math.Min(capacity, KTGlobal.GetRandomNumber(1, 2));

            // Đếm số dòng hiện tại theo phẩm chất
            Dictionary<int, int> qualityCount = CountQualityInAttributes(medalData.Attributes);

            // Pool mở rộng: bao gồm tất cả phẩm chất từ 1 đến phẩm hiện tại, loại bỏ ID đã có
            List<MedalAttributeConfig> pool = BuildMultiQualityPool(qualityConfig.Quality, qualityCount)
                .Where(x => medalData.Attributes.All(a => a.Id != x.Id))
                .ToList();

            for (int i = 0; i < addCount && pool.Count > 0; i++)
            {
                List<MedalAttributeConfig> available = pool.Where(x =>
                {
                    int cnt;
                    return !qualityCount.TryGetValue(x.Quality, out cnt) || cnt < MaxAttributesPerQuality;
                }).ToList();

                if (available.Count <= 0)
                {
                    break;
                }

                int index = KTGlobal.GetRandomNumber(0, available.Count - 1);
                MedalAttributeConfig selected = available[index];
                pool.RemoveAll(x => x.Id == selected.Id);

                int cnt2;
                qualityCount[selected.Quality] = qualityCount.TryGetValue(selected.Quality, out cnt2) ? cnt2 + 1 : 1;

                medalData.Attributes.Add(new MedalAttributeData
                {
                    Id = selected.Id,
                    Level = 1,
                    Value = GetRandomValue(selected),
                    Locked = false
                });
            }
        }

        private static bool TryBuildRefinedData(MedalData source, HashSet<int> lockedIds, out MedalData refinedData, out string message)
        {
            message = string.Empty;
            refinedData = new MedalData
            {
                Quality = source.Quality,
                Attributes = new List<MedalAttributeData>(source.Attributes.Count)
            };

            // Giữ lại các dòng đã khóa và tính số dòng phẩm chất đã chốt
            Dictionary<int, int> qualityCount = new Dictionary<int, int>();
            foreach (MedalAttributeData attribute in source.Attributes)
            {
                if (!lockedIds.Contains(attribute.Id))
                {
                    continue;
                }

                refinedData.Attributes.Add(new MedalAttributeData
                {
                    Id = attribute.Id,
                    Level = attribute.Level,
                    Value = attribute.Value,
                    Locked = attribute.Locked
                });

                // Tìm phẩm chất của thuộc tính đã khóa từ propsConfig
                MedalAttributeConfig lockedCfg;
                if (attributesByIdLevel.TryGetValue(BuildAttributeKey(attribute.Id, 1), out lockedCfg))
                {
                    int cnt;
                    qualityCount[lockedCfg.Quality] = qualityCount.TryGetValue(lockedCfg.Quality, out cnt) ? cnt + 1 : 1;
                }
            }

            // Pool mở rộng: tất cả phẩm chất từ 1 đến phẩm hiện tại, loại bỏ ID đã khóa
            HashSet<int> selectedIds = new HashSet<int>(lockedIds);
            List<MedalAttributeConfig> pool = BuildMultiQualityPool(source.Quality, qualityCount)
                .Where(x => !selectedIds.Contains(x.Id))
                .ToList();

            // Roll lại các dòng chưa khóa
            foreach (MedalAttributeData attribute in source.Attributes)
            {
                if (lockedIds.Contains(attribute.Id))
                {
                    continue;
                }

                List<MedalAttributeConfig> available = pool.Where(x =>
                {
                    int cnt;
                    return !qualityCount.TryGetValue(x.Quality, out cnt) || cnt < MaxAttributesPerQuality;
                }).ToList();

                if (available.Count <= 0)
                {
                    message = "Không đủ pool thuộc tính để tẩy luyện.";
                    return false;
                }

                MedalAttributeConfig selected = available[KTGlobal.GetRandomNumber(0, available.Count - 1)];
                selectedIds.Add(selected.Id);
                pool.RemoveAll(x => x.Id == selected.Id);

                int cnt2;
                qualityCount[selected.Quality] = qualityCount.TryGetValue(selected.Quality, out cnt2) ? cnt2 + 1 : 1;

                int value = GetRandomValue(selected);
                if (attribute.Level > 1)
                {
                    int step;
                    if (!TryGetWholeNumberStep(selected, out step))
                    {
                        message = "Thuộc tính tẩy luyện có bước tăng thập phân, cần chốt scale runtime trước khi giữ cấp.";
                        return false;
                    }

                    value += step * (attribute.Level - 1);
                }

                refinedData.Attributes.Add(new MedalAttributeData
                {
                    Id = selected.Id,
                    Level = attribute.Level,
                    Value = value,
                    Locked = false
                });
            }

            return true;
        }

        private static int GetDailyMeritFree(KPlayer player)
        {
            return 0;
        }

        private static void SetDailyMeritFree(KPlayer player, int value)
        {
        }

        private static int GetDailyMeritOnlineMinutes(KPlayer player)
        {
            return Math.Max(0, player.GetValueOfDailyRecore(MeritOnlineMinutesDailyRecordKey));
        }

        private static void SetDailyMeritOnlineMinutes(KPlayer player, int value)
        {
            player.SetValueOfDailyRecore(MeritOnlineMinutesDailyRecordKey, Math.Max(0, value));
        }

        private static void NotifyMeritChanged(KPlayer player, string message)
        {
            if (player == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                PlayerManager.ShowNotification(player, message);
            }

            // Đồng bộ công trạng mới nhất về client
            MedalMeritSnapshot meritSnapshot = GetMeritSnapshot(player);
            string responseStr = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}",
                1,
                "",
                5,
                -1,
                -1,
                " ",
                meritSnapshot.Free,
                meritSnapshot.Paid
            );

            GameManager.ClientMgr.SendToClient(player, responseStr, (int)TCPGameServerCmds.CMD_KT_MEDAL_OP);
        }

        private static int GetCurrentDayID()
        {
            DateTime now = DateTime.Now;
            return now.Year * 10000 + now.Month * 100 + now.Day;
        }

        private static MedalQualityConfig GetQualityByItemID(int itemID)
        {
            MedalQualityConfig qualityConfig;
            return qualitiesByItemID.TryGetValue(itemID, out qualityConfig) ? qualityConfig : null;
        }

        private static MedalAttributeConfig GetAttribute(int id, int quality, int level)
        {
            MedalAttributeConfig byId;
            if (attributesByIdLevel.TryGetValue(BuildAttributeKey(id, level), out byId))
            {
                return byId;
            }

            if (level > 1 && attributesByIdLevel.TryGetValue(BuildAttributeKey(id, 1), out byId))
            {
                return byId;
            }

            List<MedalAttributeConfig> byQuality = GetPropsByQualityLevel(quality, level);
            return byQuality != null && byQuality.Count > 0 ? byQuality[0] : null;
        }

        private static List<MedalAttributeConfig> GetPropsByQualityLevel(int quality, int level)
        {
            List<MedalAttributeConfig> props;
            return propsByQualityLevel.TryGetValue(BuildPropsKey(quality, level), out props) ? props : null;
        }

        private static void BuildLookups()
        {
            Dictionary<int, MedalQualityConfig> qualityLookup = new Dictionary<int, MedalQualityConfig>();
            foreach (MedalQualityConfig quality in config.Qualities)
            {
                if (quality == null || qualityLookup.ContainsKey(quality.ItemID))
                {
                    continue;
                }

                qualityLookup.Add(quality.ItemID, quality);
            }

            Dictionary<string, List<MedalAttributeConfig>> propsLookup = new Dictionary<string, List<MedalAttributeConfig>>();
            Dictionary<string, MedalAttributeConfig> attributeLookup = new Dictionary<string, MedalAttributeConfig>();
            foreach (MedalAttributeConfig prop in propsConfig.Props)
            {
                if (prop == null)
                {
                    continue;
                }

                string propsKey = BuildPropsKey(prop.Quality, prop.Level);
                List<MedalAttributeConfig> props;
                if (!propsLookup.TryGetValue(propsKey, out props))
                {
                    props = new List<MedalAttributeConfig>();
                    propsLookup.Add(propsKey, props);
                }
                props.Add(prop);

                string attributeKey = BuildAttributeKey(prop.Id, prop.Level);
                if (!attributeLookup.ContainsKey(attributeKey))
                {
                    attributeLookup.Add(attributeKey, prop);
                }
            }

            qualitiesByItemID = qualityLookup;
            propsByQualityLevel = propsLookup;
            attributesByIdLevel = attributeLookup;
        }

        private static string BuildPropsKey(int quality, int level)
        {
            return quality + "|" + level;
        }

        private static string BuildAttributeKey(int id, int level)
        {
            return id + "|" + level;
        }

        /// <summary>
        /// Số dòng thuộc tính tối đa được phép cùng phẩm chất trên một Huy Chương.
        /// ponytail: hằng số này cần khớp với quy tắc thiết kế trong hợp đồng, hiện đang là 3.
        /// </summary>
        private const int MaxAttributesPerQuality = 3;

        /// <summary>
        /// Đếm số dòng thuộc tính theo từng phẩm chất trong danh sách hiện tại.
        /// </summary>
        private static Dictionary<int, int> CountQualityInAttributes(IEnumerable<MedalAttributeData> attributes)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            foreach (MedalAttributeData attr in attributes)
            {
                MedalAttributeConfig cfg;
                if (!attributesByIdLevel.TryGetValue(BuildAttributeKey(attr.Id, 1), out cfg))
                {
                    continue;
                }

                int cnt;
                result[cfg.Quality] = result.TryGetValue(cfg.Quality, out cnt) ? cnt + 1 : 1;
            }

            return result;
        }

        /// <summary>
        /// Xây dựng pool thuộc tính mở rộng từ phẩm chất 1 đến <paramref name="maxQuality"/>,
        /// loại bỏ những phẩm chất mà <paramref name="currentQualityCount"/> đã đạt giới hạn.
        /// </summary>
        private static List<MedalAttributeConfig> BuildMultiQualityPool(int maxQuality, Dictionary<int, int> currentQualityCount)
        {
            List<MedalAttributeConfig> result = new List<MedalAttributeConfig>();
            for (int q = 1; q <= maxQuality; q++)
            {
                int cnt;
                if (currentQualityCount.TryGetValue(q, out cnt) && cnt >= MaxAttributesPerQuality)
                {
                    continue;
                }

                List<MedalAttributeConfig> props = GetPropsByQualityLevel(q, 1);
                if (props != null)
                {
                    result.AddRange(props);
                }
            }

            return result;
        }

        private static int GetRandomValue(MedalAttributeConfig attributeConfig)
        {
            if (attributeConfig.Score > 0)
            {
                return attributeConfig.Score;
            }

            if (attributeConfig.MaxScore <= attributeConfig.MinScore)
            {
                return attributeConfig.MinScore;
            }

            return KTGlobal.GetRandomNumber(attributeConfig.MinScore, attributeConfig.MaxScore);
        }

        private static string Serialize(MedalData data)
        {
            StringBuilder builder = new StringBuilder(48 + (data.Attributes.Count + data.TempAttributes.Count) * 36);
            builder.Append(data.Quality)
                .Append(';');

            for (int i = 0; i < data.Attributes.Count; i++)
            {
                MedalAttributeData attribute = data.Attributes[i];
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append(attribute.Id)
                    .Append('/')
                    .Append(attribute.Level)
                    .Append('/')
                    .Append(attribute.Value)
                    .Append('/')
                    .Append(attribute.Locked ? 1 : 0);
            }

            builder.Append(';');

            for (int i = 0; i < data.TempAttributes.Count; i++)
            {
                MedalAttributeData attribute = data.TempAttributes[i];
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append(attribute.Id)
                    .Append('/')
                    .Append(attribute.Level)
                    .Append('/')
                    .Append(attribute.Value)
                    .Append('/')
                    .Append(attribute.Locked ? 1 : 0);
            }

            return RuntimePrefix + EncodePayload(builder.ToString());
        }

        private static bool TryDeserialize(string rawData, out MedalData data)
        {
            data = null;
            if (string.IsNullOrEmpty(rawData) || !rawData.StartsWith(RuntimePrefix))
            {
                return false;
            }

            try
            {
                string payload = DecodePayload(rawData.Substring(RuntimePrefix.Length));
                string[] sections = payload.Split(new[] { ';' }, 3);
                if (sections.Length < 2)
                {
                    return false;
                }

                int quality;
                if (!int.TryParse(sections[0], out quality))
                {
                    return false;
                }

                string attributePayload = sections[1];
                data = new MedalData
                {
                    Quality = quality,
                    Attributes = new List<MedalAttributeData>(),
                    TempAttributes = new List<MedalAttributeData>()
                };

                if (!string.IsNullOrEmpty(attributePayload))
                {
                    string[] attributeRows = attributePayload.Split('|');
                    foreach (string attributeRow in attributeRows)
                    {
                        if (string.IsNullOrEmpty(attributeRow))
                        {
                            continue;
                        }

                        string[] fields = attributeRow.Split('/');
                        if (fields.Length < 4)
                        {
                            return false;
                        }

                        int id;
                        int level;
                        int value;
                        int locked;
                        if (!int.TryParse(fields[0], out id) ||
                            !int.TryParse(fields[1], out level) ||
                            !int.TryParse(fields[2], out value) ||
                            !int.TryParse(fields[3], out locked))
                        {
                            return false;
                        }

                        data.Attributes.Add(new MedalAttributeData
                        {
                            Id = id,
                            Level = level,
                            Value = value,
                            Locked = locked > 0
                        });
                    }
                }

                if (sections.Length == 3 && !string.IsNullOrEmpty(sections[2]))
                {
                    string tempAttributePayload = sections[2];
                    string[] tempAttributeRows = tempAttributePayload.Split('|');
                    foreach (string attributeRow in tempAttributeRows)
                    {
                        if (string.IsNullOrEmpty(attributeRow))
                        {
                            continue;
                        }

                        string[] fields = attributeRow.Split('/');
                        if (fields.Length < 4)
                        {
                            return false;
                        }

                        int id;
                        int level;
                        int value;
                        int locked;
                        if (!int.TryParse(fields[0], out id) ||
                            !int.TryParse(fields[1], out level) ||
                            !int.TryParse(fields[2], out value) ||
                            !int.TryParse(fields[3], out locked))
                        {
                            return false;
                        }

                        data.TempAttributes.Add(new MedalAttributeData
                        {
                            Id = id,
                            Level = level,
                            Value = value,
                            Locked = locked > 0
                        });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, "[Medal] Parse MedalData failed: " + ex);
                return false;
            }
        }

        private static string EncodePayload(string payload)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string DecodePayload(string payload)
        {
            string base64 = payload.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }

    public class MedalMeritSnapshot
    {
        public int DayId;
        public int Free;
        public int Paid;
        public int OnlineMinutes;
    }

    public class MedalOperationResult
    {
        public bool Success;
        public string Message;
        public int ItemDbId;
        public int ItemId;
        public string Payload;
        public MedalData MedalData;
        public MedalAttributeData ChangedAttribute;

        public static MedalOperationResult Fail(string message, GoodsData medal = null, MedalData medalData = null)
        {
            return Build(false, message, medal, medalData, null);
        }

        public static MedalOperationResult Ok(string message, GoodsData medal, MedalData medalData, MedalAttributeData changedAttribute = null)
        {
            return Build(true, message, medal, medalData, changedAttribute);
        }

        private static MedalOperationResult Build(bool success, string message, GoodsData medal, MedalData medalData, MedalAttributeData changedAttribute)
        {
            return new MedalOperationResult
            {
                Success = success,
                Message = message,
                ItemDbId = medal != null ? medal.Id : 0,
                ItemId = medal != null ? medal.GoodsID : 0,
                Payload = medal != null && medal.OtherParams != null && medal.OtherParams.ContainsKey(ItemPramenter.Pram_3)
                    ? medal.OtherParams[ItemPramenter.Pram_3]
                    : string.Empty,
                MedalData = medalData,
                ChangedAttribute = changedAttribute
            };
        }
    }

    [XmlRoot("Medal")]
    public class MedalConfig
    {
        [XmlArray("Qualities")]
        [XmlArrayItem("Quality")]
        public List<MedalQualityConfig> Qualities = new List<MedalQualityConfig>();

        [XmlArray("AttributeUpgradeCosts")]
        [XmlArrayItem("Cost")]
        public List<MedalAttributeUpgradeCostConfig> AttributeUpgradeCosts = new List<MedalAttributeUpgradeCostConfig>();

        [XmlArray("QualityUpgradeRecipes")]
        [XmlArrayItem("Recipe")]
        public List<MedalQualityUpgradeRecipeConfig> QualityUpgradeRecipes = new List<MedalQualityUpgradeRecipeConfig>();

        [XmlArray("RefineCosts")]
        [XmlArrayItem("Cost")]
        public List<MedalRefineCostConfig> RefineCosts = new List<MedalRefineCostConfig>();

        [XmlElement("Merit")]
        public MedalMeritConfig Merit = new MedalMeritConfig();

        [XmlElement("Unlock")]
        public MedalUnlockCostConfig Unlock = new MedalUnlockCostConfig();
    }

    public class MedalQualityConfig
    {
        [XmlAttribute("Quality")] public int Quality;
        [XmlAttribute("Name")] public string Name;
        [XmlAttribute("ItemID")] public int ItemID;
        [XmlAttribute("MaterialItemID")] public int MaterialItemID;
        [XmlAttribute("MaxAttributeLevel")] public int MaxAttributeLevel;
        [XmlAttribute("InitialAttributeCount")] public int InitialAttributeCount;
        [XmlAttribute("MaxAttributeCount")] public int MaxAttributeCount;
    }

    public class MedalAttributeUpgradeCostConfig
    {
        [XmlAttribute("Quality")] public int Quality;
        [XmlAttribute("FromLevel")] public int FromLevel;
        [XmlAttribute("ToLevel")] public int ToLevel;
        [XmlAttribute("Merit")] public int Merit;
        [XmlAttribute("StarCopper")] public int StarCopper;
        [XmlAttribute("StarSilver")] public int StarSilver;
        [XmlAttribute("Money")] public string Money;
    }

    public class MedalQualityUpgradeRecipeConfig
    {
        [XmlAttribute("FromQuality")] public int FromQuality;
        [XmlAttribute("ToQuality")] public int ToQuality;
        [XmlAttribute("MainItemID")] public int MainItemID;
        [XmlAttribute("ResultItemID")] public int ResultItemID;
        [XmlAttribute("MaterialItemID")] public int MaterialItemID;
        [XmlAttribute("MaterialCount")] public int MaterialCount;
        [XmlAttribute("StarSilver")] public int StarSilver;
        [XmlAttribute("Merit")] public int Merit;
        [XmlAttribute("Money")] public string Money;
        [XmlAttribute("MinAttributeLevel")] public int MinAttributeLevel;
    }

    public class MedalRefineCostConfig
    {
        [XmlAttribute("Quality")] public int Quality;
        [XmlAttribute("RefineItem")] public int RefineItem;
        [XmlAttribute("StarSilver")] public int StarSilver;
        [XmlAttribute("Money")] public string Money;
        [XmlAttribute("MinAttributeLevel")] public int MinAttributeLevel;
        [XmlAttribute("LockItem")] public int LockItem;
        [XmlAttribute("LockItemPerAttribute")] public int LockItemPerAttribute;
    }

    public class MedalUnlockCostConfig
    {
        [XmlAttribute("StarRed")] public int StarRed;
        [XmlAttribute("Money")] public string Money;
    }

    public class MedalMeritConfig
    {
        [XmlAttribute("DailyFreeMax")] public int DailyFreeMax = 100;
        [XmlAttribute("OnlineMinutesPerReward")] public int OnlineMinutesPerReward = 60;
        [XmlAttribute("OnlineReward")] public int OnlineReward = 10;
    }

    [XmlRoot("Props")]
    public class MedalPropsConfig
    {
        [XmlElement("Prop")]
        public List<MedalAttributeConfig> Props = new List<MedalAttributeConfig>();
    }

    public class MedalAttributeConfig
    {
        [XmlAttribute("Id")] public int Id;
        [XmlAttribute("Quality")] public int Quality;
        [XmlAttribute("Level")] public int Level;
        [XmlAttribute("Alias")] public string Alias;
        [XmlAttribute("MinScore")] public int MinScore;
        [XmlAttribute("MaxScore")] public int MaxScore;
        [XmlAttribute("Score")] public int Score;
        [XmlAttribute("Step")] public string Step;
        [XmlAttribute("Point")] public int Point;
        [XmlAttribute("Type")] public int Type;
        [XmlAttribute("Condition")] public string Condition;
    }

    public class MedalData
    {
        public int Quality;
        public List<MedalAttributeData> Attributes = new List<MedalAttributeData>();
        public List<MedalAttributeData> TempAttributes = new List<MedalAttributeData>();
    }

    public class MedalAttributeData
    {
        public int Id;
        public int Level;
        public int Value;
        public bool Locked;
    }
}
