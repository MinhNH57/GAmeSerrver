using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ProtoBuf;
using GameServer.Logic;
using System.IO;
using Server.Tools;
using GameServer.KiemThe.GameEvents.DoThach.Data;
using GameServer.Server;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Reflection;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.GameEvents.DailyCheckin.Data;
using Server.Data;
using System.Runtime.CompilerServices;

namespace GameServer.KiemThe.GameEvents.DoThach
{
    namespace Data
    {
        [XmlRoot("DoThach")]
        public class DoThachConfig
        {
            [ProtoMember(1)]
            [XmlElement(ElementName = "Spinner")] public List<SpinnerConfig> spiners;
        }

        [XmlRoot(ElementName = "Spinner")]
        public class SpinnerConfig
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "id")]
            public int id;

            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "name")]
            public string name;

            [ProtoMember(3)]
            [XmlAttribute(AttributeName = "limitKNB")]
            public int limitKNB;

            [ProtoMember(4)]
            [XmlAttribute(AttributeName = "active")]
            public bool active;

            [ProtoMember(4)]
            [XmlArray("Fees")]
            [XmlArrayItem("Fee")]
            public List<Fee> fees;


            [ProtoMember(5)]
            [XmlArray("Pitys")]
            [XmlArrayItem("Pity")]
            public List<Pity> pityMilestones;

            [ProtoMember(6)]
            [XmlArray("Rates")]
            [XmlArrayItem("Rate")]
            public List<ItemChance> itemChances = new List<ItemChance>();

        }

        public class Pity
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "level")]
            public int level;

            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "milestone")] 
            public int milestone;

            [ProtoMember(3)]
            [XmlAttribute(AttributeName = "guaranteed")]
            public string guaranteed;

            [ProtoMember(4)]
            [XmlAttribute(AttributeName = "resetItem")]
            public string reset;


            [XmlIgnore]
            public List<int> guaranteedItems = new List<int>();
            [XmlIgnore]
            public List<int> resetItems = new List<int>();
        }

        public class Fee
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "id")]
            public int id;

            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "amount")]
            public int amount;

            [ProtoMember(3)]
            [XmlAttribute(AttributeName = "spinTimes")]
            public int spinTimes;
        }

        public struct ItemChance
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "item")]
            public int itemId;
            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "amount")]
            public int amount;
            [ProtoMember(3)]
            [XmlAttribute(AttributeName = "weight")]
            public int weight;

        }

    }

    class DoThachManager
    {
        public const string ConfigPath = "Config/KT_Interactive/DoThach.xml";

        private static DoThachConfig Config;

        public static void Init()
        {
            var path = Global.GameResPath(ConfigPath);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(DoThachConfig));
                Config = xmlSerializer.Deserialize(fs) as DoThachConfig;
                foreach (var spiner in Config.spiners)
                {
                    foreach (var pity in spiner.pityMilestones)
                    {
                        if (!string.IsNullOrEmpty(pity.guaranteed))
                        {
                            string[] guaranteedItems = pity.guaranteed.Split(',');
                            foreach (string item in guaranteedItems)
                            {
                                pity.guaranteedItems.Add(Convert.ToInt32(item));
                            }
                        }
                        

                        if(!string.IsNullOrEmpty(pity.reset))
                        {
                            string[] resetItems = pity.reset.Split(',');
                            foreach (string item in resetItems)
                            {
                                pity.resetItems.Add(Convert.ToInt32(item));
                            }
                        }
                            
                    }
                }
            }
        }


        public static bool CheckNotification(KPlayer client,int spinnerId,int itemId)
        {
            if (ItemManager._TotalGameItem.TryGetValue(itemId, out ItemData itemData))
            {
                if (spinnerId == 0) // Đổ thạch thường
                {
                    if (itemData.Level >= 3)
                        return true;
                }
                else if(spinnerId == 1) // Đổ thạch thần bí
                {
                    if (itemData.Level >= 4 || itemData.ItemValue == 3 || itemData.ItemValue == 4)
                        return true;
                }
                return false;
            }
            return false;
        }
        

        public static Fee GetRollFee(int spinnerId, int action)
        {
            SpinnerConfig spiner = GetSpiner(spinnerId);
            if (spiner != null && spiner.fees.Count > action)
            {
                return spiner.fees[action];
            }
            return null;
        }

        public static int GetRemainingRollByKNB(KPlayer client, int spinnerId)
        {
            SpinnerConfig spiner = GetSpiner(spinnerId);
            if (spiner != null)
            {
                int remaining = client.GetValueOfDailyRecore((int)AcitvityRecore.DoThach_KNB_0 + spinnerId);
                if (remaining == -1)
                    return spiner.limitKNB;
                return remaining;
            }
            return 0;
        }

        public static string GetRemainingRollByKNBString(KPlayer client)
        {
            return string.Join("|", Config.spiners.Select(x => GetRemainingRollByKNB(client, x.id)));
        }

        public static string GetSpinerPityCountDownString(KPlayer client, int spinnerId)
        {
            SpinnerConfig spiner = GetSpiner(spinnerId);
            int[] countdowns; 
            if (spiner != null)
            {
                countdowns = spiner.pityMilestones.Select(x => x.milestone - GetSpinnerCounter(client, spinnerId, x.level)).ToArray();
                return string.Join(",", countdowns);
            }
            return "";
        }

        public static string GetSpinersPityCountDownString(KPlayer client)
        {
            return string.Join("|", Config.spiners.Select(x => GetSpinerPityCountDownString(client, x.id)));
        }

        private static int GetSpinnerCounter(KPlayer client, int spinnerId, int level)
        {
            String valueString = Global.GetRoleParamByName(client, RoleParamName.DoThach_Counter_Record);
            if (!String.IsNullOrEmpty(valueString))
            {
                byte[] Base64Decode = Convert.FromBase64String(valueString);
                Dictionary<int, Dictionary<int, int>> data = DataHelper.BytesToObject<Dictionary<int, Dictionary<int, int>>>(Base64Decode, 0, Base64Decode.Length);
                if (data.TryGetValue(spinnerId, out var counters))
                {
                    if (counters.TryGetValue(level, out var status))
                        return status;
                }

            }
            return 0;
        }

        public static int[] GetSpinerCounters(KPlayer client, int spinnerId)
        {
            SpinnerConfig spiner = GetSpiner(spinnerId);
            if(spiner != null)
            {
                return spiner.pityMilestones.Select(x => GetSpinnerCounter(client, spinnerId, x.level)).ToArray();
            }
            return null;
        }


        public static void SetSpinnerCounter(KPlayer client, int spinerId, int level, int counter)
        {
            String valueString = Global.GetRoleParamByName(client, RoleParamName.DoThach_Counter_Record);
            Dictionary<int, Dictionary<int, int>> data = new Dictionary<int, Dictionary<int, int>>();
            if (!String.IsNullOrEmpty(valueString))
            {
                byte[] Base64Decode = Convert.FromBase64String(valueString);
                data = DataHelper.BytesToObject<Dictionary<int, Dictionary<int, int>>>(Base64Decode, 0, Base64Decode.Length);
            }
            if (!data.ContainsKey(spinerId))
            {
                data[spinerId] = new Dictionary<int, int>();
            }
            data[spinerId][level] = counter;
            byte[] bytes = DataHelper.ObjectToBytes(data);
            string encodedData = Convert.ToBase64String(bytes);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.DoThach_Counter_Record, encodedData, false);
        }

        public static void ResetSpinnerCounter(KPlayer client, int spinerId, int[] cachePitys)
        {
            for (int i = 1; i <= cachePitys.Length; i++)
            {
                SetSpinnerCounter(client, spinerId, i, cachePitys[i - 1]);// reset lại pity
            }
        }



        public static SpinnerConfig GetSpiner(int spinerId)
        {
            return Config.spiners.Find(s => s.id == spinerId);
        }


        public static ItemChance GetRollItem(KPlayer client, int spinnerId)
        {
            SpinnerConfig spiner = GetSpiner(spinnerId);
            if (spiner == null)
            {
                return default;
            }

            // Không rơi vào mốc nào thì roll bình thường
            int[] allItems = spiner.itemChances.Select(x => x.itemId).ToArray();
            ItemChance normalItem = GetRandomItemByWeight(spinnerId, allItems);

            // Tăng counter của tất cả mốc
            for (int i = spiner.pityMilestones.Count - 1; i >= 0; i--)
            {
                Data.Pity pity = spiner.pityMilestones[i];
                if (pity.guaranteedItems.Contains(normalItem.itemId) || pity.resetItems.Contains(normalItem.itemId))
                {
                    // Khi quay được phần thưởng trong mốc này thì reset counter của tất cả mốc trước về 0
                    for (int j = pity.level; j > 0; j--)
                    {
                        SetSpinnerCounter(client, spinnerId, j, 0);
                    }
                    break;
                }
                int counter = GetSpinnerCounter(client, spinnerId, pity.level);
                SetSpinnerCounter(client, spinnerId, pity.level, counter + 1);
            }

            for (int i = spiner.pityMilestones.Count - 1; i >= 0; i--)
            {
                Data.Pity pity = spiner.pityMilestones[i];
                int counter = GetSpinnerCounter(client, spinnerId, pity.level);
                if (counter >= pity.milestone)
                {
                    ItemChance itemChance = GetRandomItemByWeight(spinnerId, pity.guaranteedItems.ToArray());
                    itemChance.amount = 1; // nếu quay ra do bảo hiểm thì chỉ được 1 item
                    // Khi quay được phần thưởng trong mốc này thì reset counter của tất cả mốc trước về 0
                    for (int j = pity.level; j > 0; j--)
                    {
                        SetSpinnerCounter(client, spinnerId, j, 0);
                    }
                    return itemChance;
                }
            }

            

            return normalItem;
        }



        private static ItemChance GetRandomItemByWeight(int spinnerId, int[] itemsPool)
        {
            SpinnerConfig spinner = GetSpiner(spinnerId);
            if (spinner != null)
            {
                ItemChance[] pool = spinner.itemChances.Where(x => itemsPool.Contains(x.itemId)).ToArray();

                float random = (float)Global.GetRandomNumber(0, 1000);

                int allChances = 0;
                foreach (var itemChance in pool)
                {
                    allChances += itemChance.weight;
                }

                float chancePart = 1000f / allChances;
                float checkedChances = 0f;

                foreach (var itemChance in pool)
                {
                    checkedChances += chancePart * itemChance.weight;
                    if (random < checkedChances)
                    {
                        return itemChance;
                    }
                }
            }

            return default;
        }
    }
}