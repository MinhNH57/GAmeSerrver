using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    internal sealed class KuaFuBalanceConfig
    {
        public bool Enabled { get; set; }

        public KuaFuBalanceJoinConfig Join { get; set; } = new KuaFuBalanceJoinConfig();

        public int WingBuffSkillId { get; set; }

        public int WingBuffDurationMinutes { get; set; }

        public bool AutoEquipTemporaryEquipment { get; set; }

        public bool KeepPreviousTemporaryEquipment { get; set; }

        public List<KuaFuBalanceWingBuffConfig> WingBuffs { get; private set; } = new List<KuaFuBalanceWingBuffConfig>();

        public List<KuaFuBalanceRelivePoint> GuildWarRelivePoints { get; private set; } = new List<KuaFuBalanceRelivePoint>();

        public List<KuaFuBalanceEquipSet> SeriesEquipSets { get; private set; } = new List<KuaFuBalanceEquipSet>();

        public static KuaFuBalanceConfig CreateDefault()
        {
            return new KuaFuBalanceConfig()
            {
                Enabled = false,
                Join = new KuaFuBalanceJoinConfig()
                {
                    MinMantleLevel = 6,
                    MapId = 1642,
                    EnterX = 9052,
                    EnterY = 4385,
                    LeaveMapId = 1642,
                    LeaveX = 9052,
                    LeaveY = 4385,
                },
                WingBuffSkillId = 2068,
                WingBuffDurationMinutes = 420,
                AutoEquipTemporaryEquipment = true,
                KeepPreviousTemporaryEquipment = false,
                WingBuffs =
                {
                    new KuaFuBalanceWingBuffConfig() { MantleLevel = 7, BuffLevel = 1 },
                    new KuaFuBalanceWingBuffConfig() { MantleLevel = 8, BuffLevel = 2 },
                    new KuaFuBalanceWingBuffConfig() { MantleLevel = 9, BuffLevel = 3 },
                    new KuaFuBalanceWingBuffConfig() { MantleLevel = 10, BuffLevel = 4 },
                },
                GuildWarRelivePoints =
                {
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 1120, PosY = 6409 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 1110, PosY = 5214 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 1763, PosY = 4464 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 4715, PosY = 4278 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 5768, PosY = 6204 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 3794, PosY = 7358 },
                    new KuaFuBalanceRelivePoint() { MapId = 406, PosX = 6030, PosY = 5460 },
                },
            };
        }

        public static KuaFuBalanceConfig Parse(XElement xml)
        {
            KuaFuBalanceConfig result = CreateDefault();
            if (xml == null)
            {
                return result;
            }

            result.Enabled = GetBool(xml, "Enabled", true);

            XElement join = xml.Element("Join");
            if (join != null)
            {
                result.Join = new KuaFuBalanceJoinConfig()
                {
                    MinMantleLevel = GetInt(join, "MinMantleLevel", 6),
                    MapId = GetInt(join, "MapId", 1642),
                    EnterX = GetInt(join, "EnterX", 9052),
                    EnterY = GetInt(join, "EnterY", 4385),
                    LeaveMapId = GetInt(join, "LeaveMapId", 1642),
                    LeaveX = GetInt(join, "LeaveX", 9052),
                    LeaveY = GetInt(join, "LeaveY", 4385),
                };
            }

            XElement wingBuffs = xml.Element("WingBuffs");
            if (wingBuffs != null)
            {
                result.WingBuffSkillId = GetInt(wingBuffs, "SkillId", 0);
                result.WingBuffDurationMinutes = GetInt(wingBuffs, "DurationMinutes", 420);
                result.WingBuffs.Clear();

                foreach (XElement buff in wingBuffs.Elements("Buff"))
                {
                    result.WingBuffs.Add(new KuaFuBalanceWingBuffConfig()
                    {
                        MantleLevel = GetInt(buff, "MantleLevel", 0),
                        BuffLevel = GetInt(buff, "BuffLevel", 0),
                    });
                }
            }

            XElement guildWarRelivePoints = xml.Element("GuildWarRelivePoints");
            if (guildWarRelivePoints != null)
            {
                result.GuildWarRelivePoints.Clear();
                foreach (XElement pointNode in guildWarRelivePoints.Elements("Point"))
                {
                    KuaFuBalanceRelivePoint relivePoint = new KuaFuBalanceRelivePoint()
                    {
                        MapId = GetInt(pointNode, "MapId", 0),
                        PosX = GetInt(pointNode, "X", 0),
                        PosY = GetInt(pointNode, "Y", 0),
                    };

                    if (relivePoint.MapId > 0 && relivePoint.PosX > 0 && relivePoint.PosY > 0)
                    {
                        result.GuildWarRelivePoints.Add(relivePoint);
                    }
                }
            }

            XElement equipment = xml.Element("Equipment");
            if (equipment != null)
            {
                result.AutoEquipTemporaryEquipment = GetBool(equipment, "AutoEquip", true);
                result.KeepPreviousTemporaryEquipment = GetBool(equipment, "KeepPrevious", false);
                int defaultBinding = GetInt(equipment, "Binding", 1);
                int defaultEnhanceLevel = GetInt(equipment, "EnhanceLevel", 14);
                int defaultExpireMinutes = GetInt(equipment, "ExpireMinutes", 420);

                XElement seriesSets = equipment.Element("SeriesSets");
                if (seriesSets != null)
                {
                    result.SeriesEquipSets.Clear();
                    foreach (XElement setNode in seriesSets.Elements("Set"))
                    {
                        KuaFuBalanceEquipSet set = new KuaFuBalanceEquipSet()
                        {
                            SeriesId = GetInt(setNode, "SeriesId", 0),
                        };

                        foreach (XElement itemNode in setNode.Elements("Item"))
                        {
                            KuaFuBalanceItemConfig item = ParseItem(itemNode, defaultBinding, defaultEnhanceLevel, defaultExpireMinutes);
                            if (item.ItemId > 0)
                            {
                                set.Items.Add(item);
                            }
                        }

                        if (set.SeriesId > 0 && set.Items.Count > 0)
                        {
                            result.SeriesEquipSets.Add(set);
                        }
                    }
                }

            }

            return result;
        }

        public KuaFuBalanceEquipSet GetEquipSet(int seriesId, int factionId, int routeId)
        {
            KuaFuBalanceEquipSet baseSet = SeriesEquipSets.FirstOrDefault(x => x.SeriesId == seriesId);
            if (baseSet == null)
            {
                return null;
            }

            return baseSet.Clone();
        }

        public HashSet<int> GetConfiguredItemIds()
        {
            HashSet<int> itemIds = new HashSet<int>();
            foreach (KuaFuBalanceEquipSet set in SeriesEquipSets)
            {
                foreach (KuaFuBalanceItemConfig item in set.Items)
                {
                    itemIds.Add(item.ItemId);
                }
            }


            return itemIds;
        }

        public int GetWingBuffLevel(int mantleLevel)
        {
            KuaFuBalanceWingBuffConfig buff = WingBuffs.FirstOrDefault(x => x.MantleLevel == mantleLevel);
            if (buff == null)
            {
                return 0;
            }

            return buff.BuffLevel;
        }

        private static int GetInt(XElement node, string name, int defaultValue)
        {
            XAttribute attribute = node.Attribute(name);
            if (attribute == null || !int.TryParse(attribute.Value, out int value))
            {
                return defaultValue;
            }

            return value;
        }

        private static bool GetBool(XElement node, string name, bool defaultValue)
        {
            XAttribute attribute = node.Attribute(name);
            if (attribute == null || !bool.TryParse(attribute.Value, out bool value))
            {
                return defaultValue;
            }

            return value;
        }

        private static KuaFuBalanceItemConfig ParseItem(XElement itemNode, int defaultBinding, int defaultEnhanceLevel, int defaultExpireMinutes)
        {
            return new KuaFuBalanceItemConfig()
            {
                ItemId = GetInt(itemNode, "ItemId", 0),
                Count = GetInt(itemNode, "Count", 1),
                Series = GetInt(itemNode, "Series", -1),
                Sex = GetInt(itemNode, "Sex", -1),
                Binding = GetInt(itemNode, "Binding", defaultBinding),
                EnhanceLevel = GetInt(itemNode, "EnhanceLevel", defaultEnhanceLevel),
                ExpireMinutes = GetInt(itemNode, "ExpireMinutes", defaultExpireMinutes),
            };
        }

    }

    internal sealed class KuaFuBalanceJoinConfig
    {
        public int MinMantleLevel { get; set; }

        public int MapId { get; set; }

        public int EnterX { get; set; }

        public int EnterY { get; set; }

        public int LeaveMapId { get; set; }

        public int LeaveX { get; set; }

        public int LeaveY { get; set; }
    }

    internal sealed class KuaFuBalanceRelivePoint
    {
        public int MapId { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }
    }

    internal sealed class KuaFuBalanceWingBuffConfig
    {
        public int MantleLevel { get; set; }

        public int BuffLevel { get; set; }
    }

    internal sealed class KuaFuBalanceEquipSet
    {
        public int SeriesId { get; set; }

        public List<KuaFuBalanceItemConfig> Items { get; private set; } = new List<KuaFuBalanceItemConfig>();

        public KuaFuBalanceEquipSet Clone()
        {
            KuaFuBalanceEquipSet result = new KuaFuBalanceEquipSet() { SeriesId = SeriesId };
            foreach (KuaFuBalanceItemConfig item in Items)
            {
                result.Items.Add(item.Clone());
            }

            return result;
        }
    }

    internal sealed class KuaFuBalanceItemConfig
    {
        public int ItemId { get; set; }

        public int Count { get; set; }

        public int Series { get; set; }

        public int Sex { get; set; }

        public int Binding { get; set; }

        public int EnhanceLevel { get; set; }

        public int ExpireMinutes { get; set; }

        public KuaFuBalanceItemConfig Clone()
        {
            return new KuaFuBalanceItemConfig()
            {
                ItemId = ItemId,
                Count = Count,
                Series = Series,
                Sex = Sex,
                Binding = Binding,
                EnhanceLevel = EnhanceLevel,
                ExpireMinutes = ExpireMinutes,
            };
        }
    }
}
