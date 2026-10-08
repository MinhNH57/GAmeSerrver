using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.GameEvents.LeagueBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using Newtonsoft.Json;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GameServer.KiemThe.GameEvents.KuaFuBalance
{
    /// <summary>
    /// Quản lý trạng thái Liên server cân bằng.
    /// </summary>
    public sealed partial class KuaFuBalanceManager
    {
        private const string ConfigPath = "Config/KT_Battle/KuaFuBalanceConfig.xml";
        private const string SourceName = "KuaFuBalance";
        private const int DefaultFirstEntryLevel = 1;

        // Khởi tạo sau các fields phụ thuộc để tránh null reference trong constructor
        private static readonly KuaFuBalanceManager InstanceValue = new KuaFuBalanceManager();

        private readonly ConcurrentDictionary<int, KuaFuBalanceSession> sessions = new ConcurrentDictionary<int, KuaFuBalanceSession>();
        private readonly object configLock = new object();

        private KuaFuBalanceConfig config = KuaFuBalanceConfig.CreateDefault();
        private bool configLoaded;
        private HashSet<int> configuredEquipItemIds = new HashSet<int>();

        public static KuaFuBalanceManager Instance
        {
            get { return InstanceValue; }
        }

        public bool IsEnabled
        {
            get
            {
                EnsureConfigLoaded();
                return ServerConfig.Instance != null && ServerConfig.Instance.IsKuaFuBalance && GameManager.IsKuaFuServer;
            }
        }

        private KuaFuBalanceManager()
        {
        }

        public void Init()
        {
            ReloadConfig();
        }

        public void ReloadConfig()
        {
            lock (configLock)
            {
                try
                {
                    XElement xml = Global.GetGameResXml(ConfigPath);
                    config = KuaFuBalanceConfig.Parse(xml);
                    configuredEquipItemIds = config.GetConfiguredItemIds();
                    configLoaded = true;
                    _cachedLimitedEquipPositions = null;
                    LogManager.WriteLog(LogTypes.KuaFuBalance, string.Format("[KuaFuBalance] Loaded config. SeriesSets={0}", config.SeriesEquipSets.Count));
                }
                catch (Exception ex)
                {
                    config = KuaFuBalanceConfig.CreateDefault();
                    configuredEquipItemIds = new HashSet<int>();
                    configLoaded = true;
                    LogManager.WriteLog(LogTypes.KuaFuBalance, "[KuaFuBalance] Load config failed: " + ex);
                }
            }
        }

        public bool IsBalanceMap(KPlayer player)
        {
            if (player == null || !IsEnabled)
            {
                return false;
            }

            return IsBalanceMap(player.CurrentMapCode);
        }

        public bool IsBalanceMap(int mapCode)
        {
            if (!IsEnabled)
            {
                return false;
            }

            return mapCode == config.Join.MapId || IsBalanceActivityMap(mapCode);
        }

        public bool IsBalanceMapIgnoreKuaFu(int mapCode)
        {
            EnsureConfigLoaded();
            return mapCode == config.Join.MapId || IsBalanceActivityMap(mapCode);
        }

        private void EnsureConfigLoaded()
        {
            if (configLoaded)
            {
                return;
            }

            ReloadConfig();
        }

        private static bool IsBalanceActivityMap(int mapCode)
        {
            return GuidWarManager.getInstance().IsKuaFuBalanceMap(mapCode)
                || LeagueBattleManager.IsKuaFuBalanceMap(mapCode);
        }

        public bool IsInBalance(KPlayer player)
        {
            return player != null && sessions.ContainsKey(player.RoleID);
        }

        public bool TryGetLobbyRelivePoint(KPlayer player, out int mapCode, out int posX, out int posY)
        {
            EnsureConfigLoaded();

            mapCode = config.Join.LeaveMapId;
            posX = config.Join.LeaveX;
            posY = config.Join.LeaveY;

            if (player == null || (!IsInBalance(player) && !IsBalanceMap(player)))
            {
                return false;
            }

            if (TryGetGuildWarRelivePoint(player.CurrentMapCode, out KuaFuBalanceRelivePoint relivePoint))
            {
                mapCode = relivePoint.MapId;
                posX = relivePoint.PosX;
                posY = relivePoint.PosY;
            }

            return true;
        }

        private bool TryGetGuildWarRelivePoint(int currentMapCode, out KuaFuBalanceRelivePoint relivePoint)
        {
            relivePoint = null;

            if (!GuidWarManager.getInstance().IsKuaFuBalanceMap(currentMapCode)
                || config.GuildWarRelivePoints == null
                || config.GuildWarRelivePoints.Count <= 0)
            {
                return false;
            }

            List<KuaFuBalanceRelivePoint> points = config.GuildWarRelivePoints
                .Where(x => x != null && x.MapId > 0 && x.PosX > 0 && x.PosY > 0)
                .ToList();
            if (points.Count <= 0)
            {
                return false;
            }

            relivePoint = points[KTGlobal.GetRandomNumber(0, points.Count - 1)];
            return true;
        }

        public bool CanModifyTemporaryEquipment(KPlayer player, GoodsData item, int modType, int targetUsing)
        {
            if (player == null || item == null)
            {
                return false;
            }

            if (modType != (int)ModGoodsTypes.EquipLoad && modType != (int)ModGoodsTypes.EquipUnload)
            {
                return false;
            }

            if (!IsTemporaryEquipment(player, item))
            {
                return false;
            }

            if (modType == (int)ModGoodsTypes.EquipUnload)
            {
                return item.Using >= 0;
            }

            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            if (itemData == null || !ItemManager.KD_ISEQUIP(itemData.Genre))
            {
                return false;
            }

            int expectedPosition = ItemManager.g_anEquipPos[itemData.DetailType];
            return item.Using < 0 && targetUsing == expectedPosition;
        }

    }
}
