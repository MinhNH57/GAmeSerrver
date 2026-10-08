using System;
using System.Collections.Generic;
using GameServer.KiemThe.Logic.Manager.Shop;
using UnityEngine.Serialization;

namespace GameServer.Data
{
    public enum FeeType
    {
        BACKHOA = 0,
        BAC,
        KNB,
        KNBKHOA,
        TINHLUC,
        HOATLUC
    }
    [Serializable]
    public class SourceItem
    {
        public List<int> ids;
        public int forge_level;
        public int total_value;
    }

    [Serializable]
    public class Material
    {
        public int id;
        public int number;
        public bool non_consumable;
    }

    [Serializable]
    public class OutputItem
    {
        public int id;
        public int forge_level;
        public int number;
        public int expiry;
        public int series;
        public int force_binding = -1;
    }

    [Serializable]
    public class Fee
    {
        public int type;
        public int number;
    }

    [Serializable]
    public class Recipe
    {
        public int compose_id;
        public string compose_name;
        public string compose_description;
        public SourceItem source;
        public List<Material> materials;
        public List<int> rate_up_items;
        public List<int> insure_items;
        public OutputItem output;
        public List<Fee> fees;
        public bool allow_multiple;
        public double default_rate;
        public double actual_rate;
        public bool enable_rate_up;
        public bool enable_source;
        public bool output_based_on_input;
        public Limit limit;
    }

    [Serializable]
    public class Limit
    {
        public LimitType type;
        public int number;

        public override string ToString()
        {
            string output = number.ToString();
            switch (type)
            {
                case LimitType.LimitCount:
                    output += " lần";
                    break;
                case LimitType.BuyCountPerDay:
                    output += " lần một ngày";
                    break;
                case LimitType.BuyCountPerWeek:
                    output += " lần một tuần";
                    break;
                case LimitType.BuycountPerMonth:
                    output += " lần một tháng";
                    break;
            }

            return output;
        }
    }
    
    [Serializable]
    public class ComposeData
    {
        public List<Recipe> recipes;
    }
}