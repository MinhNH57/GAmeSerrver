using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.KuaFuBalance;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using KF.Client;
using KF.Contract.Data;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.KiemThe.GameEvents.GuildWarManager
{
    /// <summary>
    /// Logic riêng cho tranh đoạt lãnh thổ liên server.
    /// </summary>
    internal class KuaFuGuildWarLogic
    {
        public const int EnterWarSelectionId = 2001;

        public const int LeaveWarSelectionId = -2;

        private const string KuaFuConfigPath = "Config/KT_Battle/KuaFuGuildWarConfig.xml";

        private const bool DisableRewardsTemporarily = false;

        private const bool BypassGuildForGmTest = false;

        private const int TestGuildSeed = 100000000;

        private const int EnterWarPosX = 3754;

        private const int EnterWarPosY = 5114;

        private const int KuaFuCityMapId = 1642;

        private const int KuaFuCityPosX = 9052;

        private const int KuaFuCityPosY = 4385;

        private readonly object _rewardLock = new object();

        public bool IsEnabled
        {
            get
            {
                return GameManager.IsKuaFuServer && KuaFuBalanceManager.Instance.IsEnabled;
            }
        }

        public string GetConfigPath(string localConfigPath)
        {
            return IsEnabled ? KuaFuConfigPath : localConfigPath;
        }

        public OpenConfig GetWarMapConfig(GuildWardConfig config)
        {
            if (config == null || config.WarMapConfigs == null || config.WarMapConfigs.Count <= 0)
            {
                return null;
            }

            return config.WarMapConfigs.FirstOrDefault();
        }

        public void SetupMiniMap(GuildWardConfig config, List<GuildWarMiniMap> miniMaps)
        {
            OpenConfig warMap = GetWarMapConfig(config);
            if (warMap == null || miniMaps == null)
            {
                return;
            }

            miniMaps.Add(new GuildWarMiniMap()
            {
                GuildID = 0,
                GuildName = "Liên server",
                Tax = warMap.Star,
                MapID = warMap.ReadMapID,
                MapType = 3,
                HexColor = "#115c1e"
            });
        }

        public void CreateActiveWarMap(GuildWardConfig config, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar)
        {
            OpenConfig warMap = GetWarMapConfig(config);
            if (warMap == null)
            {
                WriteGuildWarLog("Không tìm thấy cấu hình map chiến trường.");
                return;
            }

            if (totalMapWillBeWar.ContainsKey(warMap.ReadMapID))
            {
                return;
            }

            GuildWarMap guildWarMap = new GuildWarMap(config)
            {
                MapId = warMap.ReadMapID,
                IsFightMap = warMap.IsFightMapID,
                MapName = warMap.MapName,
                BeLongGuild = -1,
                BeLongGuildName = "Liên server"
            };

            totalMapWillBeWar.TryAdd(warMap.ReadMapID, guildWarMap);
            WriteGuildWarLog(string.Format("Khởi tạo map chiến trường. MapID={0}, MapName={1}.", warMap.ReadMapID, warMap.MapName));
        }

        public bool TryShowNpcDialog(NPC npc, KPlayer client, GUILDWARDSTATUS battleState, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, Action<TaskCallBack> onSelect)
        {
            if (!IsEnabled)
            {
                return false;
            }

            KNPCDialog npcDialog = new KNPCDialog();
            Dictionary<int, string> selections = new Dictionary<int, string>();

            if (CanEnterByState(battleState))
            {
                npcDialog.Text = "Tranh đoạt lãnh thổ liên server đang mở. Chọn Hoa Sơn để vào chiến trường.";
                selections.Add(EnterWarSelectionId, "Vào Hoa Sơn liên server");
            }
            else if (battleState == GUILDWARDSTATUS.STATUS_END)
            {
                npcDialog.Text = "Tranh đoạt lãnh thổ liên server đã kết thúc. Phần thưởng sẽ được gửi qua thư về máy chủ gốc.";
            }
            else
            {
                npcDialog.Text = "Tranh đoạt lãnh thổ liên server chưa mở.";
            }

            if (IsInWarMap(client, totalMapWillBeWar))
            {
                selections.Add(LeaveWarSelectionId, "Ta muốn rời khỏi chiến trường");
            }

            selections.Add(1000, "Ta sẽ quay lại sau!");
            npcDialog.OnSelect = onSelect;
            npcDialog.Selections = selections;
            npcDialog.Show(npc, client);
            return true;
        }

        public bool TryHandleNpcSelection(GuildWardConfig config, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, KPlayer client, NPC npc, TaskCallBack callback, GUILDWARDSTATUS battleState, Action<KPlayer, int> changeState)
        {
            if (!IsEnabled)
            {
                return false;
            }

            if (callback.SelectID == 1000)
            {
                KT_TCPHandler.CloseDialog(client);
                return true;
            }

            if (callback.SelectID == EnterWarSelectionId)
            {
                EnterWarMap(config, totalMapWillBeWar, client, npc, battleState);
                return true;
            }

            if (callback.SelectID == LeaveWarSelectionId)
            {
                LeaveWarMap(totalMapWillBeWar, client, changeState);
                return true;
            }

            return true;
        }

        public bool TryShowOfficeRankNotice(NPC npc, KPlayer client)
        {
            if (!IsEnabled)
            {
                return false;
            }

            KNPCDialog npcDialog = new KNPCDialog();
            npcDialog.Text = "Liên server không xử lý nâng Quan hàm bang hội. Quan Ấn cấp 5-8 có thể mua tại cửa hàng Lãnh thổ tạp hóa [Không danh vọng].";
            npcDialog.Selections = new Dictionary<int, string>()
            {
                { 1000000, "Kết thúc đối thoại" }
            };
            npcDialog.Show(npc, client);
            return true;
        }

        public void EnterWarMapFromNpc(GuildWardConfig config, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, KPlayer client, NPC npc, GUILDWARDSTATUS battleState)
        {
            EnterWarMap(config, totalMapWillBeWar, client, npc, battleState);
        }

        public void LeaveWarMapFromNpc(ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, KPlayer client, Action<KPlayer, int> changeState)
        {
            LeaveWarMap(totalMapWillBeWar, client, changeState);
        }

        public void MoveAllPlayersBackToCity(ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, Action<KPlayer, int> changeState)
        {
            if (totalMapWillBeWar == null || totalMapWillBeWar.Count <= 0)
            {
                return;
            }

            List<KPlayer> players = totalMapWillBeWar.Keys
                .SelectMany(mapId => GameManager.ClientMgr.GetMapClients(mapId) ?? new List<KPlayer>())
                .Where(x => x != null && x.IsOnline())
                .GroupBy(x => x.RoleID)
                .Select(x => x.First())
                .ToList();

            foreach (KPlayer player in players)
            {
                player.PKMode = (int)PKMode.Peace;
                player.ForbidUsingSkill = false;
                player.Camp = -1;
                player.TempTitle = "";
                changeState(player, 0);

                WriteLeaveWarLog(player, string.Format("Kết thúc hoạt động, tự đưa về thành liên server. MapID={0}.", KuaFuCityMapId));
                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, KuaFuCityMapId, KuaFuCityPosX, KuaFuCityPosY, (int)player.CurrentDir);
            }
        }

        public int GetSourceServerId(KPlayer client)
        {
            if (client == null)
            {
                return GameManager.LocalServerId;
            }

            if (GameManager.IsKuaFuServer)
            {
                Tmsk.Contract.KuaFuServerLoginData kuaFuLoginData = Global.GetClientKuaFuServerLoginData(client);
                if (kuaFuLoginData != null && kuaFuLoginData.ServerId > 0)
                {
                    return kuaFuLoginData.ServerId;
                }
            }

            return client.ServerId;
        }

        public bool CanJoin(KPlayer client)
        {
            return client != null && (client.GuildID > 0 || CanBypassGuildForTest(client));
        }

        public bool CanBypassGuildForTest(KPlayer client)
        {
            return client != null && IsEnabled && BypassGuildForGmTest && KTGMCommandManager.IsGM(client);
        }

        public int GetGuildWarGuildId(KPlayer client)
        {
            if (client == null)
            {
                return 0;
            }

            if (client.GuildID > 0)
            {
                return client.GuildID;
            }

            if (CanBypassGuildForTest(client))
            {
                return TestGuildSeed + client.RoleID;
            }

            return client.GuildID;
        }

        public string GetGuildWarGuildName(KPlayer client)
        {
            if (client == null)
            {
                return "";
            }

            if (client.GuildID > 0)
            {
                return client.GuildName;
            }

            if (CanBypassGuildForTest(client))
            {
                return "TestGuild-" + client.RoleName;
            }

            return client.GuildName;
        }

        public void RegisterPlayer(KPlayer client, ConcurrentDictionary<int, string> totalGuildInfo, ConcurrentDictionary<int, ConcurrentDictionary<int, GuildWarPlayer>> warScore)
        {
            int sourceServerId = GetSourceServerId(client);
            int guildWarGuildId = GetGuildWarGuildId(client);
            string guildWarGuildName = GetGuildWarGuildName(client);

            if (!totalGuildInfo.ContainsKey(guildWarGuildId))
            {
                totalGuildInfo.TryAdd(guildWarGuildId, guildWarGuildName);
            }

            ConcurrentDictionary<int, GuildWarPlayer> guildPlayers;
            if (warScore.TryGetValue(guildWarGuildId, out guildPlayers))
            {
                GuildWarPlayer currentPlayer;
                guildPlayers.TryGetValue(client.RoleID, out currentPlayer);

                if (currentPlayer != null)
                {
                    currentPlayer._Player = client;
                    currentPlayer.ServerId = sourceServerId;
                    WriteGuildWarLog(string.Format(
                        "Cập nhật người tham chiến đã đăng ký. RoleID={0}, RoleName={1}, SourceServer={2}, GuildID={3}, GuildName={4}, Score={5}.",
                        client.RoleID,
                        client.RoleName,
                        sourceServerId,
                        guildWarGuildId,
                        guildWarGuildName,
                        currentPlayer.Score));
                    return;
                }
            }
            else
            {
                guildPlayers = new ConcurrentDictionary<int, GuildWarPlayer>();
                warScore.TryAdd(guildWarGuildId, guildPlayers);
            }

            GuildWarPlayer playerJoinWar = new GuildWarPlayer();
            playerJoinWar.GuildID = guildWarGuildId;
            playerJoinWar.ServerId = sourceServerId;
            playerJoinWar.CurentRank = -1;
            playerJoinWar.KillCount = 0;
            playerJoinWar.DestroyCount = 0;
            playerJoinWar.MaxKillSteak = 0;
            playerJoinWar.IsReviceReward = false;
            playerJoinWar.CurentKillSteak = 0;
            playerJoinWar.Score = 0;
            playerJoinWar._Player = client;

            if (guildPlayers.TryAdd(client.RoleID, playerJoinWar))
            {
                WriteGuildWarLog(string.Format(
                    "Đăng ký người tham chiến. RoleID={0}, RoleName={1}, SourceServer={2}, GuildID={3}, GuildName={4}.",
                    client.RoleID,
                    client.RoleName,
                    sourceServerId,
                    guildWarGuildId,
                    guildWarGuildName));
            }
        }

        public void SendRewards(
            GuildWardConfig config,
            ConcurrentDictionary<int, ConcurrentDictionary<int, GuildWarPlayer>> warScore,
            ConcurrentDictionary<int, List<GuildTotalScore>> totalScore)
        {
            lock (_rewardLock)
            {
                if (DisableRewardsTemporarily)
                {
                    WriteGuildWarLog("Tam thoi tat phat thuong TDLT-LSV.");
                    return;
                }

                if (warScore == null)
                {
                    WriteGuildWarLog("Bỏ qua phát thưởng vì WarScore không tồn tại.");
                    return;
                }

                List<GuildTotalScore> guildRanking = GetGuildRewardRanking(config, totalScore);
                int registeredPlayerCount = warScore.Values.Where(x => x != null).Sum(x => x.Count);
                int queuedRewardCount = 0;
                int failedQueueCount = 0;
                int alreadyRewardedCount = 0;
                int missingAwardCount = 0;

                WriteGuildWarLog(string.Format(
                    "Bắt đầu phát thưởng. GuildRankCount={0}, RegisteredPlayerCount={1}.",
                    guildRanking.Count,
                    registeredPlayerCount));

                if (guildRanking.Count <= 0)
                {
                    WriteGuildWarLog("Không có bang có điểm hợp lệ để phát thưởng.");
                }

                for (int guildIndex = 0; guildIndex < guildRanking.Count; guildIndex++)
                {
                    GuildTotalScore guildScore = guildRanking[guildIndex];
                    if (guildScore == null || !warScore.TryGetValue(guildScore.GuildID, out ConcurrentDictionary<int, GuildWarPlayer> guildPlayers))
                    {
                        continue;
                    }

                    int guildRank = guildIndex + 1;
                    List<GuildWarPlayer> memberRanking = GetGuildMemberRewardRanking(guildPlayers);
                    WriteGuildWarLog(string.Format(
                        "Xếp hạng bang cuối trận. GuildRank={0}, GuildID={1}, GuildName={2}, GuildScore={3}, ScoreOrder={4}, EligibleMemberCount={5}.",
                        guildRank,
                        guildScore.GuildID,
                        guildScore.GuildName,
                        guildScore.Score,
                        guildScore.ScoreReachedOrder,
                        memberRanking.Count));

                    WriteMemberRankingLog(guildRank, guildScore, memberRanking);

                    if (!HasGuildRankAward(config, guildRank))
                    {
                        missingAwardCount += memberRanking.Count;
                        WriteGuildWarLog(string.Format(
                            "Bang không có cấu hình thưởng theo hạng. GuildRank={0}, GuildID={1}, GuildName={2}.",
                            guildRank,
                            guildScore.GuildID,
                            guildScore.GuildName));
                        continue;
                    }

                    for (int memberIndex = 0; memberIndex < memberRanking.Count; memberIndex++)
                    {
                        GuildWarPlayer player = memberRanking[memberIndex];
                        if (player == null || player._Player == null)
                        {
                            continue;
                        }

                        int memberRank = memberIndex + 1;
                        if (player.IsReviceReward)
                        {
                            alreadyRewardedCount++;
                            continue;
                        }

                        WardConfigAward guildRankAward = GetGuildRankAward(config, guildRank, memberRank);
                        if (guildRankAward == null)
                        {
                            missingAwardCount++;
                            WriteGuildWarLog(string.Format(
                                "Thiếu cấu hình thưởng thành viên. GuildRank={0}, MemberRank={1}, GuildID={2}, RoleID={3}, RoleName={4}.",
                                guildRank,
                                memberRank,
                                guildScore.GuildID,
                                player._Player.RoleID,
                                player._Player.RoleName));
                            continue;
                        }

                        List<GoodsData> goods = new List<GoodsData>();
                        AddAwardGoods(goods, guildRankAward);
                        int boundMoney = guildRankAward.BacKhoa;
                        if (goods.Count <= 0 && boundMoney <= 0)
                        {
                            continue;
                        }

                        long rewardId = SubmitRewardMail(
                            player._Player,
                            goods,
                            0,
                            boundMoney,
                            BuildRewardKey(guildScore.GuildID, guildRank, memberRank, player),
                            guildRank,
                            memberRank,
                            player.Score);
                        if (rewardId <= 0)
                        {
                            failedQueueCount++;
                            WriteGuildWarLog(string.Format(
                                "Dua thuong vao queue that bai. GuildRank={0}, MemberRank={1}, GuildID={2}, RoleID={3}, RoleName={4}, Score={5}, Items={6}.",
                                guildRank,
                                memberRank,
                                guildScore.GuildID,
                                player._Player.RoleID,
                                player._Player.RoleName,
                                player.Score,
                                GetRewardGoodsLog(goods)));
                            continue;
                        }

                        player.IsReviceReward = true;
                        queuedRewardCount++;
                        WriteGuildWarLog(string.Format(
                            "Da dua thuong vao queue. RewardID={0}, GuildRank={1}, MemberRank={2}, GuildID={3}, RoleID={4}, RoleName={5}, SourceServer={6}, Score={7}, Items={8}.",
                            rewardId,
                            guildRank,
                            memberRank,
                            guildScore.GuildID,
                            player._Player.RoleID,
                            player._Player.RoleName,
                            player.ServerId,
                            player.Score,
                            GetRewardGoodsLog(goods)));
                    }
                }

                WriteGuildWarLog(string.Format(
                    "Kết thúc phát thưởng. QueuedRewardCount={0}, FailedQueueCount={1}, AlreadyRewardedCount={2}, MissingAwardCount={3}.",
                    queuedRewardCount,
                    failedQueueCount,
                    alreadyRewardedCount,
                    missingAwardCount));
            }
        }

        private bool CanEnterByState(GUILDWARDSTATUS battleState)
        {
            return battleState == GUILDWARDSTATUS.STATUS_PREPARSTART
                || battleState == GUILDWARDSTATUS.STATUS_START
                || battleState == GUILDWARDSTATUS.STATUS_PREPAREEND;
        }

        public bool IsInWarMap(KPlayer client, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar)
        {
            return client != null && totalMapWillBeWar.ContainsKey(client.CurrentMapCode);
        }

        private void EnterWarMap(GuildWardConfig config, ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, KPlayer client, NPC npc, GUILDWARDSTATUS battleState)
        {
            KNPCDialog npcDialog = new KNPCDialog();
            OpenConfig warMap = GetWarMapConfig(config);
            if (warMap == null)
            {
                WriteEnterWarLog(client, "Từ chối vào map vì thiếu cấu hình chiến trường.");
                npcDialog.Text = "Không tìm thấy cấu hình bản đồ chiến trường liên server.";
                npcDialog.Show(npc, client);
                return;
            }

            if (!CanEnterByState(battleState))
            {
                WriteEnterWarLog(client, "Từ chối vào map vì trạng thái hoạt động không cho phép.");
                npcDialog.Text = "Tranh đoạt lãnh thổ liên server chưa trong thời gian vào chiến trường.";
                npcDialog.Show(npc, client);
                return;
            }

            if (!CanJoin(client))
            {
                WriteEnterWarLog(client, "Từ chối vào map vì người chơi chưa có bang hợp lệ.");
                npcDialog.Text = "Hãy gia nhập bang hội trước khi vào chiến trường.";
                npcDialog.Show(npc, client);
                return;
            }

            CreateActiveWarMap(config, totalMapWillBeWar);
            WriteEnterWarLog(client, string.Format("Yêu cầu vào map. MapID={0}, MapName={1}, BattleState={2}.", warMap.ReadMapID, warMap.MapName, battleState));
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, warMap.ReadMapID, EnterWarPosX, EnterWarPosY, (int)client.CurrentDir);
        }

        private void LeaveWarMap(ConcurrentDictionary<int, GuildWarMap> totalMapWillBeWar, KPlayer client, Action<KPlayer, int> changeState)
        {
            if (!IsInWarMap(client, totalMapWillBeWar))
            {
                WriteLeaveWarLog(client, "Từ chối rời map vì người chơi không ở chiến trường.");
                PlayerManager.ShowNotification(client, "Hiện không ở trong chiến trường lãnh thổ.");
                return;
            }

            client.PKMode = (int)PKMode.Peace;
            client.ForbidUsingSkill = false;
            client.Camp = -1;
            client.TempTitle = "";
            changeState(client, 0);

            WriteLeaveWarLog(client, string.Format("Rời map về thành liên server. MapID={0}.", KuaFuCityMapId));
            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, KuaFuCityMapId, KuaFuCityPosX, KuaFuCityPosY, (int)client.CurrentDir);
        }

        private List<GuildTotalScore> GetGuildRewardRanking(
            GuildWardConfig config,
            ConcurrentDictionary<int, List<GuildTotalScore>> totalScore)
        {
            OpenConfig warMap = GetWarMapConfig(config);
            if (warMap == null
                || totalScore == null
                || !totalScore.TryGetValue(warMap.ReadMapID, out List<GuildTotalScore> guildScores)
                || guildScores == null)
            {
                return new List<GuildTotalScore>();
            }

            return guildScores
                .Where(x => x != null && x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.ScoreReachedOrder)
                .ThenBy(x => x.GuildID)
                .ToList();
        }

        private List<GuildWarPlayer> GetGuildMemberRewardRanking(ConcurrentDictionary<int, GuildWarPlayer> guildPlayers)
        {
            if (guildPlayers == null)
            {
                return new List<GuildWarPlayer>();
            }

            return guildPlayers.Values
                .Where(x => x != null && x._Player != null && x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.ScoreReachedOrder)
                .ThenBy(x => x._Player.RoleID)
                .ToList();
        }

        private void WriteMemberRankingLog(int guildRank, GuildTotalScore guildScore, List<GuildWarPlayer> memberRanking)
        {
            if (guildScore == null || memberRanking == null)
            {
                return;
            }

            for (int memberIndex = 0; memberIndex < memberRanking.Count; memberIndex++)
            {
                GuildWarPlayer player = memberRanking[memberIndex];
                if (player == null || player._Player == null)
                {
                    continue;
                }

                WriteGuildWarLog(string.Format(
                    "Xếp hạng người chơi cuối trận. GuildRank={0}, GuildID={1}, GuildName={2}, MemberRank={3}, RoleID={4}, RoleName={5}, SourceServer={6}, Score={7}, ScoreOrder={8}, KillCount={9}, DestroyCount={10}, MaxKillSteak={11}.",
                    guildRank,
                    guildScore.GuildID,
                    guildScore.GuildName,
                    memberIndex + 1,
                    player._Player.RoleID,
                    player._Player.RoleName,
                    player.ServerId,
                    player.Score,
                    player.ScoreReachedOrder,
                    player.KillCount,
                    player.DestroyCount,
                    player.MaxKillSteak));
            }
        }

        private void AddAwardGoods(List<GoodsData> goods, WardConfigAward award)
        {
            if (goods == null || award == null || award.Items == null)
            {
                return;
            }

            foreach (WardConfigAwardItem awardItem in award.Items)
            {
                if (awardItem.ItemID <= 0 || awardItem.Count <= 0)
                {
                    continue;
                }

                string endTime = awardItem.ExpiryDays > 0
                    ? DateTime.Now.AddDays(awardItem.ExpiryDays).ToString("yyyy-MM-dd HH:mm:ss")
                    : Global.ConstGoodsEndTime;

                goods.Add(new GoodsData()
                {
                    GoodsID = awardItem.ItemID,
                    Binding = 1,
                    GCount = awardItem.Count,
                    Forge_level = 0,
                    Endtime = endTime
                });
            }
        }

        private WardConfigAward GetGuildRankAward(GuildWardConfig config, int guildRank, int memberRank)
        {
            if (config == null || config.Award == null)
            {
                return null;
            }

            foreach (WardConfigAward award in config.Award.Where(x => string.Equals(x.type, "guildrank", StringComparison.OrdinalIgnoreCase)))
            {
                if (IsRankMatched(award.GuildRank, guildRank) && IsRankMatched(award.Rank, memberRank))
                {
                    return award;
                }
            }

            return null;
        }

        private bool HasGuildRankAward(GuildWardConfig config, int guildRank)
        {
            return config != null
                && config.Award != null
                && config.Award.Any(x => string.Equals(x.type, "guildrank", StringComparison.OrdinalIgnoreCase) && IsRankMatched(x.GuildRank, guildRank));
        }

        private bool IsRankMatched(string rankConfig, int rank)
        {
            if (string.IsNullOrEmpty(rankConfig))
            {
                return false;
            }

            string[] ranks = rankConfig.Split(':');
            if (ranks.Length == 1)
            {
                int singleRank;
                return int.TryParse(ranks[0], out singleRank) && rank == singleRank;
            }

            int fromRank;
            int toRank;
            return ranks.Length == 2
                && int.TryParse(ranks[0], out fromRank)
                && int.TryParse(ranks[1], out toRank)
                && rank >= fromRank
                && rank <= toRank;
        }

        private long SubmitRewardMail(KPlayer client, List<GoodsData> goodsData, int boundToken, int boundMoney, string rewardKey, int guildRank, int memberRank, int score)
        {
            int sourceServerId = GetSourceServerId(client);
            KuaFuRewardMailData reward = new KuaFuRewardMailData
            {
                RewardKey = rewardKey,
                EventType = "TDLT-LSV",
                SourceServer = sourceServerId,
                RoleId = client.RoleID,
                TemplateId = KuaFuRewardTemplateIds.TdltLsvMemberReward,
                TemplateArgs = BuildTemplateArgs(
                    "guild_rank", guildRank,
                    "member_rank", memberRank,
                    "score", score),
                BoundMoney = boundMoney,
                BoundToken = boundToken,
                MailGoods = BuildMailGoodsString(goodsData),
                ItemLog = GetRewardGoodsLog(goodsData),
            };

            KuaFuRewardSubmitResult result = KuaFuRewardClient.getInstance().SubmitMailReward(reward);
            if (result == null || !result.Success)
            {
                WriteGuildWarLog(string.Format(
                    "Dua thuong vao queue that bai. RewardKey={0}, GuildRank={1}, MemberRank={2}, RoleID={3}, RoleName={4}, SourceServer={5}, Detail={6}",
                    rewardKey,
                    guildRank,
                    memberRank,
                    client.RoleID,
                    client.RoleName,
                    sourceServerId,
                    result != null ? result.Message : "null"));
                return 0;
            }

            LogManager.WriteLog(LogTypes.Mail, string.Format("[TDLT-LSV][QUEUE]-[{0}][{1}][server:{2}][reward:{3}][template:{4}][args:{5}] [items:{6}]", client.RoleID, client.RoleName, sourceServerId, rewardKey, reward.TemplateId, reward.TemplateArgs, reward.ItemLog));
            return result.RewardId;
        }

        private string BuildTemplateArgs(params object[] keyValues)
        {
            if (keyValues == null || keyValues.Length <= 1)
            {
                return string.Empty;
            }

            List<string> pairs = new List<string>();
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                pairs.Add(string.Format("{0}={1}", keyValues[i], keyValues[i + 1]));
            }

            return string.Join(";", pairs.ToArray());
        }

        private string BuildRewardKey(int guildId, int guildRank, int memberRank, GuildWarPlayer player)
        {
            return string.Format(
                "TDLT-LSV:{0:yyyyMMdd}:{1}:{2}:{3}:{4}:{5}:{6}",
                DateTime.Now,
                GetSourceServerId(player._Player),
                guildId,
                guildRank,
                memberRank,
                player._Player.RoleID,
                player.ScoreReachedOrder);
        }

        private string BuildMailGoodsString(List<GoodsData> goodsData)
        {
            StringBuilder mailGoodsBuilder = new StringBuilder();
            if (goodsData == null)
            {
                return string.Empty;
            }

            foreach (GoodsData itemGD in goodsData)
            {
                if (itemGD == null)
                {
                    continue;
                }

                if (mailGoodsBuilder.Length > 0)
                {
                    mailGoodsBuilder.Append("|");
                }

                mailGoodsBuilder.Append(KTMailManager.GoodsToMailGoodsString(itemGD));
            }

            return mailGoodsBuilder.ToString();
        }

        private string GetRewardGoodsLog(List<GoodsData> goods)
        {
            if (goods == null || goods.Count <= 0)
            {
                return "none";
            }

            return string.Join(",", goods.Select(x => string.Format("{0}x{1}", x.GoodsID, x.GCount)).ToArray());
        }

        private void WriteEnterWarLog(KPlayer client, string message)
        {
            WritePlayerLog("Vào chiến trường", client, message);
        }

        private void WriteLeaveWarLog(KPlayer client, string message)
        {
            WritePlayerLog("Rời chiến trường", client, message);
        }

        private void WritePlayerLog(string action, KPlayer client, string message)
        {
            if (client == null)
            {
                WriteGuildWarLog(string.Format("{0}. RoleID=0. {1}", action, message));
                return;
            }

            WriteGuildWarLog(string.Format(
                "{0}. RoleID={1}, RoleName={2}, SourceServer={3}, GuildID={4}, GuildName={5}. {6}",
                action,
                client.RoleID,
                client.RoleName,
                GetSourceServerId(client),
                GetGuildWarGuildId(client),
                GetGuildWarGuildName(client),
                message));
        }

        private void WriteGuildWarLog(string message)
        {
            LogManager.WriteLog(LogTypes.GuildWarManager, "[TDLT-LSV] " + message);
        }
    }
}
