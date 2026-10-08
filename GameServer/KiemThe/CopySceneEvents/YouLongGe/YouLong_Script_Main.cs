using GameServer.KiemThe.CopySceneEvents.Model;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using System;
using Server.Data;
using GameServer.Server;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.LuaSystem.Entities;
using MoonSharp.Interpreter;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.KiemThe.CopySceneEvents.YouLongGe
{
    

    /// <summary>
    /// Script điều khiển phụ bản Du Long Các
    /// </summary>
    public class YouLong_Script_Main : CopySceneEvent
    {

        // public static List<PlayerRanking> rankingYoulongKuafu;

        #region Private fields

        /// <summary>
        /// Thời điểm tỷ thí lần trước
        /// </summary>
        private long lastChallengeTicks;

        /// <summary>
        /// Bước chiến đấu hiện tại
        /// </summary>
        private int fightStep;

        /// <summary>
        /// Danh sách phần thưởng ngẫu nhiên
        /// </summary>
        private List<YouLong.AwardInfo.Award> randomAwards = new List<YouLong.AwardInfo.Award>();

        /// <summary>
        /// Phần thưởng hiện tại
        /// </summary>
        private YouLong.AwardInfo.Award currentAward;

        /// <summary>
        /// Số lần đã tỷ thí lại
        /// </summary>
        private int rechallengeTimes;

        #endregion

        #region Constructor

        /// <summary>
        /// Script điều khiển phụ bản Du Long Các
        /// </summary>
        /// <param name="copyScene"></param>
        public YouLong_Script_Main(KTCopyScene copyScene) : base(copyScene)
        {
            // Đánh dấu thời điểm tỷ thí lần trước
            this.lastChallengeTicks = KTGlobal.GetCurrentTimeMilis();
        }

        #endregion

        #region Core CopySceneEvent

        /// <summary>
        /// Hàm này gọi đến khi bắt đầu phụ bản
        /// </summary>
        protected override void OnStart()
        {
            // Tạo NPC
            this.CreateNPC();
        }

        /// <summary>
        /// Hàm này gọi liên tục mỗi nửa giây chừng nào phụ bản còn tồn tại
        /// </summary>
        protected override void OnTick()
        {
            // Nếu thời điểm lần cuối tỷ thí vượt quá thời gian cho phép
            if (KTGlobal.GetCurrentTimeMilis() - this.lastChallengeTicks < YouLong.Data.KickOutIfNoChallengeFor) return;
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu có người chơi
            if (player != null)
            {
                // Thông báo
                PlayerManager.ShowNotification(player,
                    "Đã quá thời gian không phát sinh tỷ thí mới, tự rời phụ bản!");
            }

            // Hủy phụ bản
            this.Dispose();
        }

        /// <summary>
        /// Hàm này gọi đến khi đóng phụ bản
        /// </summary>
        protected override void OnClose()
        {
            this.randomAwards.Clear();
            this.randomAwards = null;
            this.currentAward = null;
        }

        /// <summary>
        /// Hàm này gọi đến khi người chơi vào phụ bản
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerEnter(KPlayer player)
        {
            base.OnPlayerEnter(player);

            // Cập nhật thông tin sự kiện
            this.UpdateEventDetailsToPlayers(this.CopyScene.Name, YouLong.Data.Duration - this.LifeTimeTicks,
                "Thời gian còn lại");

            // this.updateRankData();
        }

        /// <summary>
        /// Hàm này gọi đến khi người chơi rời phụ bản
        /// </summary>
        /// <param name="player"></param>
        /// <param name="toMap"></param>
        public override void OnPlayerLeave(KPlayer player, GameMap toMap)
        {
            base.OnPlayerLeave(player, toMap);

            // Đóng bảng thông báo hoạt động
            this.CloseEventBroadboard(player, YouLong.Data.EventID);
        }

        /// <summary>
        /// Sự kiện khi người chơi giết Boss
        /// </summary>
        /// <param name="player"></param>
        /// <param name="obj"></param>
        public override void OnKillObject(KPlayer player, GameObject obj)
        {
            base.OnKillObject(player, obj);

            // Nếu là Boss
            if (!(obj is Monster monster) || monster.Tag != "Boss") return;
            var todayPassedStages = player.GetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily);
            if (todayPassedStages == -1)
            {
                todayPassedStages = 0;
            }
            todayPassedStages++;
            player.SetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily, todayPassedStages);


            var thisMonthPassedStages = player.GetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount);
            if (thisMonthPassedStages == -1)
            {
                thisMonthPassedStages = 0;
            }
            thisMonthPassedStages++;
            player.SetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount, thisMonthPassedStages);

            var nServerPassed = YouLong.UpdateServerTotalPassedByValue(1);

            // thêm log xem thằng này là thằng thứ mấy khiêu chiến thành công
            LogManager.WriteLog(LogTypes.YouLongGe, $"Player {player.RoleName} [{player.RoleID}] là người thứ {nServerPassed} khiêu chiến thành công.");

            GameDb.ForceProcessUpdateDBRanking(player);

            if (YouLong.serverReceiveAwardTime < YouLong.Data.ServerAwards.Count)
            {
                for(var i = YouLong.serverReceiveAwardTime; i< YouLong.Data.ServerAwards.Count; i++)
                {
                    if(nServerPassed>= YouLong.Data.ServerAwards.CAwards[i].Require)
                    {
                        YouLong.serverReceiveAwardTime = i + 1;
                        if (nServerPassed == YouLong.Data.ServerAwards.CAwards[i].Require)
                        {
                            
                            //PlayerManager.ShowNotification(player, $"Chúc mừng bạn đã là người may mắn hoàn thành lượt khiêu chiến thứ {nServerPassed} toàn máy chủ hàng ngày");

                            // danh sách vật phẩm gửi qua thư
                            var goodsData = new List<GoodsData>();
                            var itemList = YouLong.Data.ServerAwards.CAwards[i].Items.Split('#');

                            var rewardStr = "";
                            foreach (var item in itemList)
                            {
                                var paramItem = item.Split('|');
                                var goodData = new GoodsData
                                {
                                    GoodsID = int.Parse(paramItem[0]),
                                    Binding = 1,
                                    GCount = int.Parse(paramItem[1])
                                };
                                goodsData.Add(goodData);

                                if (rewardStr == "")
                                {
                                    rewardStr = $"{goodData.ItemName} x{goodData.GCount}";
                                }
                                else
                                {
                                    rewardStr += $", {goodData.ItemName} x{goodData.GCount}";
                                }
                            }
                            
                            var congratMsg = $"Chúc mừng {player.RoleName} đã là người may mắn hoàn thành lượt khiêu chiến thứ {nServerPassed} toàn máy chủ hàng ngày. Nhận được phần thưởng may mắn là {rewardStr}. Xin chúc mừng!";
                            LogManager.WriteLog(LogTypes.YouLongGe, $"Chúc mừng {player.RoleName} đã là người may mắn hoàn thành lượt khiêu chiến thứ {nServerPassed} toàn máy chủ hàng ngày. Nhận được phần thưởng may mắn là {rewardStr}. Xin chúc mừng!");
                            if (i == 3)
                                congratMsg = $"Nhân phẩm bộc phát, {player.RoleName} khiêu chiến Du Long may mắn đã nhận được Rương Du Long May Mắn 4 qua thư, bên trong chứa Rương Danh Vọng Tiểu Du Long ngẫu nhiên. Xin chúc mừng!";
                            else if (i == 4) 
                                congratMsg = $"Thần Tài gõ cửa, server không khỏi trầm trồ khi {player.RoleName} khiêu chiến Thiên Thiên may mắn nhận được 01 Rương Du Long Thần Bí. Bên trong chứa Rương Du Long Lệnh ngẫu nhiên, khiến trăm người thán phục, vạn người ao ước.";
                            //phat thuong qua thu
                            KTGlobal.SendSystemEventNotification(congratMsg);
                            LogManager.WriteLog(LogTypes.YouLongGe, congratMsg);

                            // gửi thư về db kể cả đang off
                            var content = $"Chúc mừng bạn là người may mắn hoàn thành lượt khiêu chiến thứ {nServerPassed} toàn máy chủ ngày hôm nay.\nHãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
                            KTGlobal.SendMail(goodsData, player.RoleID, player.RoleName, "Du Long Các", content, 0, 0);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            

            // Tạo NPC
            this.CreateNPC();
            // Mở khung phần thưởng
            this.OpenAwardsBox();
            // Cập nhật thông tin sự kiện
            this.UpdateEventDetailsToPlayers(this.CopyScene.Name, YouLong.Data.Duration - this.LifeTimeTicks,
                "Thời gian còn lại");
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Trả về danh sách vật phẩm thưởng ngẫu nhiên
        /// </summary>
        /// <returns></returns>
        private List<YouLong.AwardInfo.Award> GetRandomAwardsList()
        {
            // Tạo mới danh sách
            var awards = new List<YouLong.AwardInfo.Award>();

            // Lặp từ đầu đến số lượng phải lấy
            for (var i = 1; i <= YouLong.Data.Awards.Count; i++)
            {
                // Tỷ lệ hiện tại
                var currentRate = KTGlobal.GetRandomNumber(1, 100);
                // Chọn các vật phẩm thuộc tỷ lệ tương ứng
                var groupRateAwards = YouLong.Data.Awards.Awards.Where(x => x.AppearRate >= currentRate).ToList();
                // Vật phẩm được chọn
                var awardInfo = groupRateAwards.Count <= 0
                    // Nếu không tồn tại, chọn vật phẩm có tỷ lệ cao nhất
                    ? YouLong.Data.Awards.Awards.MaxBy(x => x.AppearRate)
                    // Nếu tồn tại thì chọn ngẫu nhiên trong danh sách
                    :groupRateAwards[StaticRandom.GetRandom(groupRateAwards.Count)];

                // Thêm vào danh sách
                awards.Add(awardInfo);
            }

            // Trả về kết quả
            return awards;
        }

        /// <summary>
        /// Mở khung phần thưởng
        /// </summary>
        private void OpenAwardsBox()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Cập nhật bước chiến đấu hiện tại
            this.fightStep = 2;

            // Gửi yêu cầu mở khung phần thưởng
            KT_TCPHandler.SendOpenYouLongAwardsBox(player, this.randomAwards);
        }

        /// <summary>
        /// Bắt đầu tỷ thí
        /// </summary>
        private void BeginChallenge()
        {
            // đánh dấu là đã thêm 1 lượt
            var player = this.teamPlayers.FirstOrDefault();
            var challengeTimes = CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong);
            CopySceneEventManager.SetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong, challengeTimes + 1);
            // Cập nhật bước chiến đấu hiện tại
            this.fightStep = 1;
            // Cập nhật thông tin sự kiện
            this.UpdateEventDetailsToPlayers(this.CopyScene.Name, YouLong.Data.ChallengeDuration - this.LifeTimeTicks,
                "Thời gian tỷ thí");
            // Đánh dấu thời điểm tỷ thí lần trước
            this.lastChallengeTicks = KTGlobal.GetCurrentTimeMilis();

            // Xóa toàn bộ NPC
            this.RemoveAllNPCs();
            // Xóa toàn bộ quái
            this.RemoveAllMonsters();
            // Tạo Boss tương ứng
            this.CreateBoss();
        }

        /// <summary>
        /// Tạo Boss
        /// </summary>
        private void CreateBoss()
        {
            var youlongBoss = YouLong.Data.Bosses[StaticRandom.GetRandom(YouLong.Data.Bosses.Count)];
            // Mức máu
            var hp = youlongBoss.BaseHP + youlongBoss.HPIncreaseEachLevel * this.CopyScene.Level;
            // Tạo quái
            GameManager.MonsterZoneMgr.AddDynamicMonsters(this.CopyScene.MapCode, youlongBoss.ID, this.CopyScene.ID, 1,
                youlongBoss.PosX, youlongBoss.PosY, youlongBoss.Name, youlongBoss.Title, hp, this.CopyScene.Level, Entities.Direction.NONE,
                (KE_SERIES_TYPE)youlongBoss.Series, youlongBoss.AIType, -1, -1, youlongBoss.AIScriptID, "Boss",
                callBack: monster =>
                {
                    // Nếu có kỹ năng
                    if (youlongBoss.Skills.Count > 0)
                    {
                        // Duyệt danh sách kỹ năng
                        foreach (var skill in youlongBoss.Skills)
                        {
                            // Thêm vào danh sách kỹ năng dùng của quái
                            monster.CustomAISkills.Add(skill);
                        }
                    }

                    // Nếu có vòng sáng
                    if (youlongBoss.Auras.Count > 0)
                    {
                        // Duyệt danh sách vòng sáng
                        foreach (var aura in youlongBoss.Auras)
                        {
                            // Kích hoạt vòng sáng
                            monster.UseSkill(aura.SkillID, aura.Level, monster);
                        }
                    }
                }, 65535);
        }

        /// <summary>
        /// Tạo NPC
        /// </summary>
        private void CreateNPC()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            

            // Tạo NPC
            NPCGeneralManager.AddNewNPC(this.CopyScene.MapCode, this.CopyScene.ID, YouLong.Data.NPC.ID,
                YouLong.Data.NPC.PosX, YouLong.Data.NPC.PosY, YouLong.Data.NPC.Name, YouLong.Data.NPC.Title,
                Entities.Direction.DOWN_RIGHT, -1, "", (npc) =>
                {
                    npc.Click = () =>
                    {
                        var todayPassedStages = player.GetValueOfDailyRecore((int)AcitvityRecore.YouLongPassedCountDaily);
                        if (todayPassedStages == -1)
                        {
                            todayPassedStages = 0;
                        }

                        var thisMonthPassedStages = player.GetValueOfMonthRecore((int)MonthRecoreDef.YouLongMonthPassedCount);
                        if (thisMonthPassedStages == -1)
                        {
                            thisMonthPassedStages = 0;
                        }

                        // string xephang = "";
                        // for(int i = 0; i< YouLong_Script_Main.rankingYoulongKuafu.Count; i++)
                        // {
                        //     var playerRank = YouLong_Script_Main.rankingYoulongKuafu[i];
                        //     string svName = "";
                        //     string curRank = $"Hạng {playerRank.ID + 1} - {playerRank.RoleName} - {playerRank.Value} -{svName}";
                        //     if (xephang == "")
                        //     {
                        //         xephang += curRank;
                        //     }
                        //     else
                        //     {
                        //         xephang += ("\n" + curRank);
                        //     }
                        // }

                        var dialog = new KNPCDialog
                        {
                            Owner = player,
                            Text = $"Ngươi đến đây tỷ thí với Du Long Các - Tiểu Long Nữ ta? Tốt lắm. Bảo vật ở đây rất nhiều, tuy nhiên ngươi phải vượt qua thử thách của ta, mói đủ tư cách nhận!\nHôm nay ngươi đã hoàn thành {todayPassedStages} lần khiêu chiến!"
                            // $"\n\nBảng xếp hạng hiện tại:\n" + xephang
                        };
                        if (fightStep == 0)
                        {
                            dialog.Selections = new Dictionary<int, string>()
                            {
                                { -1, "Ta đến tỷ võ" },
                                { -10, "Xem bảng xếp hạng Du Long"},
                                { -2, "Nguyệt Ảnh Thạch đổi Chiến Thư" },
                                { -3, "Đổi Du Long Danh Vọng Lệnh" },
                                { -7, "Ta muốn nhận thưởng mốc tích lũy khiêu chiến hàng ngày" },
                                { -8, "Ta muốn nhận thưởng mốc tích lũy khiêu chiến hàng tháng" },
                                { -4, "Ta muốn rời khỏi đây..." }
                            };
                            if (KTGMCommandManager.IsGM(player))
                            {
                                dialog.Selections.Add(-9, "test 1000 lượt");
                            }
                        }
                        else
                            dialog.Selections = new Dictionary<int, string>()
                            {
                                { -10, "Xem bảng xếp hạng Du Long"},
                                { -5, "Ta muốn nhận thưởng" },
                                { -6, "Mở thương khố" },
                                { -7, "Ta muốn nhận thưởng mốc tích lũy khiêu chiến hàng ngày" },
                                { -8, "Ta muốn nhận thưởng mốc tích lũy khiêu chiến hàng tháng" },
                                { -4, "Ta muốn rời khỏi đây..." }
                            };
                        dialog.OnSelect = (x) =>
                        {
                            if (x.SelectID == -10)
                            {
                                KTLuaLib_GUI.OpenRanking(new Lua_Player
                                {
                                    RefObject = player
                                }, (int)RankMode.Youlong);
                                KT_TCPHandler.CloseDialog(player);
                            }
                            if (x.SelectID == -6)
                            {
                                KTLuaLib_Player.PortableGoods(npc, player, (int)SiteItemType.warehouse);
                                KT_TCPHandler.CloseDialog(player);
                            }

                            if (x.SelectID == -9)
                            {
                                var msg = "Danh sách phần thưởng";
                                for (var i =0; i<1000; i++)
                                {
                                    var temprandomAwards = this.GetRandomAwardsList();
                                    var tempcurrentAward = StaticRandom.AccumulatedProbabilitySelection(temprandomAwards, temprandomAwards.Select(it => it.Rate).ToList());
                                    var itemData = ItemManager.GetItemTemplate(tempcurrentAward.ID);
                                    msg += $"\r\n{itemData.Name} - {tempcurrentAward.Number}";
                                }
                                KT_TCPHandler.SendOpenUI(player, "MainGame/UITextArea", Array.Empty<int>(), new List<string> { "InputField", msg });
                            }

                            if (x.SelectID == -7)
                            {
                                var todayReceiveReward = player.GetValueOfDailyRecore((int)AcitvityRecore.YouLongReceiveDailyRewardCount);
                                if (todayReceiveReward == -1)
                                {
                                    todayReceiveReward = 0;
                                }
                                if(todayReceiveReward>= YouLong.Data.DailyAwards.Count)
                                {
                                    // đã nhận hết thưởng rồi
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Ngươi đã nhận hết phần thưởng tích lũy ngày rồi!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }
                                else
                                {
                                    if(todayPassedStages >= YouLong.Data.DailyAwards.CAwards[todayReceiveReward].Require)
                                    {
                                        //cho nhận thưởng
                                        var itemList = YouLong.Data.DailyAwards.CAwards[todayReceiveReward].Items.Split('#');
                                        foreach (var item in itemList)
                                        {
                                            var paramItem = item.Split('|');

                                            var itemID = int.Parse(paramItem[0]);
                                            var itemNum = int.Parse(paramItem[1]);

                                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, itemID,
                                                itemNum, 0, "YouLong_Script_Main_Daily_Award", true, 1, false,
                                                Global.ConstGoodsEndTime))
                                            {
                                                PlayerManager.ShowNotification(player, "Có lỗi khi tạo phần thưởng! Vui lòng liên hệ với quản trị viên.");
                                                LogManager.WriteLog(LogTypes.Error, $"YouLong_Script_Main_Daily_Award: Nhận thưởng tích lũy ngày mốc {YouLong.Data.DailyAwards.CAwards[todayReceiveReward].Require}: {itemID}, số lượng: {itemNum}");
                                            }

                                        }
                                        todayReceiveReward++;
                                        player.SetValueOfDailyRecore((int)AcitvityRecore.YouLongReceiveDailyRewardCount, todayReceiveReward);
                                    }
                                    else
                                    {
                                        // mốc nhận thưởng tiếp theo cần hoàn thành n lần khiêu chiến
                                        // đã nhận hết thưởng rồi
                                        var dialogEx = new KNPCDialog
                                        {
                                            Owner = player,
                                            Text = $"Ngươi cần hoàn thành {YouLong.Data.DailyAwards.CAwards[todayReceiveReward].Require} lần khiêu chiến để nhận mốc thưởng tích lũy tiếp theo!"
                                        };
                                        KTNPCDialogManager.AddNPCDialog(dialogEx);
                                        dialogEx.Show(npc, player);
                                        return;
                                    }
                                }
                                KTLuaLib_Player.PortableGoods(npc, player, (int)SiteItemType.warehouse);
                                KT_TCPHandler.CloseDialog(player);
                            }

                            if (x.SelectID == -8)
                            {
                                var thisMonthReceiveReward = player.GetValueOfMonthRecore((int)MonthRecoreDef.YouLongReceiveMothyRewardCount);
                                if (thisMonthReceiveReward == -1)
                                {
                                    thisMonthReceiveReward = 0;
                                }
                                if (thisMonthReceiveReward >= YouLong.Data.MonthAwards.Count)
                                {
                                    // đã nhận hết thưởng rồi
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Ngươi đã nhận hết phần thưởng tích lũy tháng rồi!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }
                                else
                                {
                                    if (thisMonthPassedStages >= YouLong.Data.MonthAwards.CAwards[thisMonthReceiveReward].Require)
                                    {
                                        //cho nhận thưởng
                                        var itemList = YouLong.Data.MonthAwards.CAwards[thisMonthReceiveReward].Items.Split('#');
                                        foreach (var item in itemList)
                                        {
                                            var paramItem = item.Split('|');

                                            var itemID = int.Parse(paramItem[0]);
                                            var itemNum = int.Parse(paramItem[1]);

                                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, itemID,
                                                itemNum, 0, "YouLong_Script_Main_Monthy_Award", true, 1, false,
                                                Global.ConstGoodsEndTime))
                                            {
                                                PlayerManager.ShowNotification(player, "Có lỗi khi tạo phần thưởng! Vui lòng liên hệ với quản trị viên.");
                                                LogManager.WriteLog(LogTypes.Error, $"YouLong_Script_Main_Monthy_Award: Nhận thưởng tích lũy tháng mốc  {YouLong.Data.MonthAwards.CAwards[thisMonthReceiveReward].Require}: {itemID}, số lượng: {itemNum}");
                                            }

                                        }
                                        thisMonthReceiveReward++;
                                        player.SetValueOfMonthRecore((int)MonthRecoreDef.YouLongReceiveMothyRewardCount, thisMonthReceiveReward);
                                    }
                                    else
                                    {
                                        // mốc nhận thưởng tiếp theo cần hoàn thành n lần khiêu chiến
                                        // đã nhận hết thưởng rồi
                                        var dialogEx = new KNPCDialog
                                        {
                                            Owner = player,
                                            Text = $"Ngươi cần hoàn thành {YouLong.Data.MonthAwards.CAwards[thisMonthReceiveReward].Require} lần khiêu chiến để nhận mốc thưởng tích lũy tiếp theo!"
                                        };
                                        KTNPCDialogManager.AddNPCDialog(dialogEx);
                                        dialogEx.Show(npc, player);
                                        return;
                                    }
                                }
                                KTLuaLib_Player.PortableGoods(npc, player, (int)SiteItemType.warehouse);
                                KT_TCPHandler.CloseDialog(player);
                            }

                            if (x.SelectID == -5)
                            {
                                // Nếu là bước 2
                                if (this.fightStep == 2 && this.randomAwards.Count > 0)
                                {
                                    // Gửi yêu cầu mở khung phần thưởng
                                    KT_TCPHandler.SendOpenYouLongAwardsBox(player, this.randomAwards);
                                    // return;
                                }
                                // Nếu là bước 3
                                else if (this.fightStep == 3 && this.currentAward != null)
                                {
                                    // Gửi yêu cầu mở khung phần thưởng
                                    KT_TCPHandler.SendOpenYouLongAwardsBox(player, this.randomAwards);
                                    // Gửi yêu cầu cập nhật danh sách thưởng
                                    KT_TCPHandler.SendUpdateYouLongAward(player, this.currentAward.ID,
                                        this.currentAward.Number, this.currentAward.NumberOfCoins, false);
                                    // return;
                                }
                                // Nếu là bước 4
                                else if (this.fightStep == 4)
                                {
                                    // Gửi yêu cầu mở khung phần thưởng
                                    KT_TCPHandler.SendOpenYouLongAwardsBox(player, this.randomAwards);
                                    // Gửi thông báo hoàn tất nhận thưởng
                                    KT_TCPHandler.SendGetYouLongAwardSuccessfully(player);
                                    // return;
                                }

                                KT_TCPHandler.CloseDialog(player);
                                return;
                            }

                            // Bắt đầu tỷ võ
                            if (x.SelectID == -1)
                            {
                                // Nếu không phải bước đầu
                                if (this.fightStep != 0)
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Ngươi không thể thao tác lúc này!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }
                                // Nếu không có chiến thư
                                else if (ItemManager.GetItemCountInBag(player, YouLong.Data.RequireItem) <= 0)
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Không có Chiến Thư, không thể khiêu chiến. Nếu ngươi có Nguyệt Ảnh Thạch, ngươi có thể đổi Chiến Thư ngay tại đây, sau đó tiến hành tỷ thí lại với ta!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }

                                // Thực hiện xóa chiến thư
                                if (!ItemManager.RemoveItemFromBag(player, YouLong.Data.RequireItem, 1, -1, "YouLong"))
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Có lỗi khi thực hiện xóa Chiến Thư!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }

                                GlobalEventSource.getInstance().fireEvent(new DuLongChienThuEventObject(player));
                                
                                // Hủy thông tin vật phẩm hiện tại
                                this.currentAward = null;
                                // Hủy danh sách vật phẩm thưởng
                                this.randomAwards.Clear();
                                // Cập nhật tổng số lần tỷ thí lại
                                this.rechallengeTimes = 0;

                                // Danh sách vật phẩm ngẫu nhiên
                                this.randomAwards = this.GetRandomAwardsList();

                                // Thực hiện tỷ thí
                                this.BeginChallenge();

                                // Gửi gói tin đóng khung thưởng
                                KT_TCPHandler.SendCloseYouLongAwardsBox(player);
                                // Đóng NPCDialog
                                KT_TCPHandler.CloseDialog(player);
                            }
                            // Đổi chiến thư
                            else if (x.SelectID == -2)
                            {
                                // Nếu không phải bước đầu
                                if (this.fightStep != 0)
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Ngươi không thể thao tác lúc này!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }
                                // Nếu không có Nguyệt Ảnh Thạch
                                else if (ItemManager.GetItemCountInBag(player, YouLong.Data.Items.MoonStoneID) <= 0)
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Không có Nguyệt Ảnh Thạch, không thể đổi chiến thư!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }

                                // Hiện bảng nhập
                                PlayerManager.ShowInputNumberBox(player, "Nhập số lượng muốn đổi.", (number) =>
                                {
                                    // Nếu không đủ số lượng Nguyệt Ảnh Thạch tương ứng
                                    if (ItemManager.GetItemCountInBag(player, YouLong.Data.Items.MoonStoneID) < number)
                                    {
                                        var dialogEx = new KNPCDialog
                                        {
                                            Owner = player,
                                            Text = "Số lượng Nguyệt Ảnh Thạch không đủ, không thể đổi chiến thư!"
                                        };
                                        KTNPCDialogManager.AddNPCDialog(dialogEx);
                                        dialogEx.Show(npc, player);
                                        return;
                                    }

                                    // Xóa Nguyệt Ảnh Thạch
                                    if (!ItemManager.RemoveItemFromBag(player, YouLong.Data.Items.MoonStoneID, number,
                                            -1, "YouLong"))
                                    {
                                        var dialogEx = new KNPCDialog
                                        {
                                            Owner = player,
                                            Text = "Có lỗi khi xóa Nguyệt Ảnh Thạch!"
                                        };
                                        KTNPCDialogManager.AddNPCDialog(dialogEx);
                                        dialogEx.Show(npc, player);
                                        return;
                                    }

                                    // Thêm Chiến Thư
                                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                                            YouLong.Data.RequireItem, number, 0, "YouLong_Script_Main", true, 1, false,
                                            Global.ConstGoodsEndTime))
                                    {
                                        var dialogEx = new KNPCDialog
                                        {
                                            Owner = player,
                                            Text = "Có lỗi khi nhận Chiến Thư Du Long"
                                        };
                                        KTNPCDialogManager.AddNPCDialog(dialogEx);
                                        dialogEx.Show(npc, player);
                                        return;
                                    }

                                    // Thông báo đổi thành công
                                    var _dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Đổi Chiến Thư thành công!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(_dialogEx);
                                    _dialogEx.Show(npc, player);
                                });
                            }
                            // Đổi Du Long Danh Vọng Lệnh
                            else if (x.SelectID == -3)
                            {
                                // Nếu không phải bước đầu
                                if (this.fightStep != 0)
                                {
                                    var dialogEx = new KNPCDialog
                                    {
                                        Owner = player,
                                        Text = "Ngươi không thể thao tác lúc này!"
                                    };
                                    KTNPCDialogManager.AddNPCDialog(dialogEx);
                                    dialogEx.Show(npc, player);
                                    return;
                                }

                                var _dialogEx = new KNPCDialog
                                {
                                    Owner = player,
                                    Text = "Chức năng này đang được phát triển!"
                                };
                                KTNPCDialogManager.AddNPCDialog(_dialogEx);
                                _dialogEx.Show(npc, player);
                            }
                            // Rời khỏi
                            else if (x.SelectID == -4)
                            {
                                var dialogEx = new KNPCDialog
                                {
                                    Owner = player,
                                    Text = "Xác nhận rời khỏi đây?",
                                    Selections = new Dictionary<int, string>()
                                    {
                                        { -1, "Xác nhận" },
                                        { -2, "Hủy bỏ" },
                                    },
                                    OnSelect = (xx) =>
                                    {
                                        // Nếu xác nhận
                                        if (xx.SelectID == -1)
                                        {
                                            // Đóng Dialog
                                            KT_TCPHandler.CloseDialog(player);
                                            // Hủy phụ bản
                                            this.Dispose();
                                        }
                                        // Nếu hủy bỏ
                                        else
                                        {
                                            // Đóng Dialog
                                            KT_TCPHandler.CloseDialog(player);
                                        }
                                    }
                                };
                                KTNPCDialogManager.AddNPCDialog(dialogEx);
                                dialogEx.Show(npc, player);
                            }
                        };
                        KTNPCDialogManager.AddNPCDialog(dialog);
                        dialog.Show(npc, player);
                    };
                });

            // Cập nhật thông tin sự kiện
            this.UpdateEventDetailsToPlayers(this.CopyScene.Name, YouLong.Data.Duration - this.LifeTimeTicks,
                "Thời gian còn lại");
        }

        #endregion

        #region Public methods

        // public void updateRankData(int needUpdateFromDb = 0)
        // {
        //     Ranking rankData;
        //     rankData = Global.SendToDB<Ranking, string>((int)TCPGameServerCmds.CMD_KT_QUERY_PLAYERRANKING, $"{0}:{(int)RankMode.Youlong}:{needUpdateFromDb}:{GameManager.LocalServerId}", GameManager.LocalServerId);
        //     YouLong_Script_Main.rankingYoulongKuafu = rankData.Players;
        //
        // }

        /// <summary>
        /// Lấy một phần thưởng ngẫu nhiên trong danh sách
        /// </summary>
        public void GetRandomAward()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 2)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi nhận phần thưởng ngẫu nhiên!");
                return;
            }

            // Nếu danh sách rỗng
            if (this.randomAwards.Count <= 0)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi nhận phần thưởng ngẫu nhiên!");
                return;
            }

            // Cập nhật bước kế tiếp
            this.fightStep = 3;

            // Tỷ lệ may mắn
            // int luckyRate = KTGlobal.GetRandomNumber(1, 1000);

            this.currentAward = StaticRandom.AccumulatedProbabilitySelection(this.randomAwards,
                this.randomAwards.Select(x => x.Rate).ToList());

            // /// Danh sách vật phẩm có tỷ lệ may mắn nhận được
            // List<YouLong.AwardInfo.Award> awards = this.randomAwards.Where(x => x.Rate >= luckyRate).ToList();
            // /// Nếu không tìm được cái nào thỏa mãn
            // if (awards.Count <= 0)
            // {
            // 	/// Chọn thằng có Rate cao nhất
            // 	YouLong.AwardInfo.Award award = this.randomAwards.MaxBy(x => x.Rate);
            // 	/// Thêm vào danh sách
            // 	awards.Add(award);
            // }
            // /// Chọn một vật phẩm trong danh sách
            // this.currentAward = awards[KTGlobal.GetRandomNumber(0, awards.Count - 1)];

            // Xóa vật phẩm vừa nhận khỏi danh sách
            this.randomAwards.Remove(this.currentAward);
            
            
            LogManager.WriteLog(LogTypes.YouLongGe,
                $"{player.RoleName} (ID: {player.RoleID}) quay vào {this.currentAward.Number} cái {KTGlobal.GetItemName(this.currentAward.ID)}.");

            // Gửi gói tin thông báo vật phẩm nhận được
            KT_TCPHandler.SendUpdateYouLongAward(player, this.currentAward.ID, this.currentAward.Number,
                this.currentAward.NumberOfCoins, true);
        }

        /// <summary>
        /// Nhận quà thưởng của người chơi
        /// </summary>
        public void GetAward()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 3)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi nhận phần thưởng!");
                return;
            }

            // Nếu không có quà thưởng
            if (this.currentAward == null)
            {
                PlayerManager.ShowNotification(player, "Ngươi không có phần thưởng!");
                return;
            }

            // Số ô đồ trống cần
            var totalSpacesNeed = KTGlobal.GetTotalSpacesNeedToTakeItem(this.currentAward.ID, this.currentAward.Number);

            // Kiểm tra túi đã đầy chưa
            if (!KTGlobal.IsHaveSpace(totalSpacesNeed, player))
            {
                PlayerManager.ShowNotification(player,
                    "Túi đồ không đủ chỗ trống, hãy sắp xếp lại ít nhất " + this.currentAward.Number + " và thử lại!");
                return;
            }

            // Cập nhật bước kế tiếp
            this.fightStep = 4;

            // UPDATE: luôn khóa phần thưởng Du Long
            var isBinding = 1;

            // Thực hiện thêm phần thưởng tương ứng
            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, this.currentAward.ID,
                    this.currentAward.Number, 0, "YouLong_Script_Main", true, isBinding, false,
                    Global.ConstGoodsEndTime))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi tạo phần thưởng! Vui lòng liên hệ với quản trị viên.");
                //return;
            }
            else
            {
                if (currentAward.ShouldNotify)
                {
                    var playerName = KTGlobal.CreateStringByColor(player.RoleName, ColorType.AppleGreen);
                    var goodsTemplate = ItemManager.GetItemTemplate(currentAward.ID);
                    var rewardStr = KTGlobal.CreateStringByColor($"{currentAward.Number} {goodsTemplate.Name}", ColorType.PeachYellow);
                    KTGlobal.SendSystemEventNotification($"Chúc mừng {playerName} khiêu chiến Du Long Các nhận được {rewardStr}. Xin chúc mừng!");
                }
            }

            // Ghi LOG
            LogManager.WriteLog(LogTypes.YouLongGe,
                string.Format("{0} (ID: {1}) mở được {2} cái {3}.", player.RoleName, player.RoleID,
                    this.currentAward.Number, KTGlobal.GetItemName(this.currentAward.ID)));

            // Hủy quà
            this.currentAward = null;

            // Thông báo
            PlayerManager.ShowNotification(player, "Nhận thành công!");

            // Gửi gói tin thông báo nhận và đổi thưởng thành công
            KT_TCPHandler.SendGetYouLongAwardSuccessfully(player);
        }

        /// <summary>
        /// Đổi Tiền Du Long
        /// </summary>
        public void ExchangeCoin()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 3)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi đổi Tiền Du Long!");
                return;
            }

            // Nếu không có quà thưởng
            if (this.currentAward == null)
            {
                PlayerManager.ShowNotification(player, "Ngươi không có phần thưởng!");
                return;
            }

            // Số ô đồ trống cần
            var totalSpacesNeed =
                KTGlobal.GetTotalSpacesNeedToTakeItem(YouLong.Data.Items.YouLongCoinID,
                    this.currentAward.NumberOfCoins);

            // Kiểm tra túi đã đầy chưa
            if (!KTGlobal.IsHaveSpace(totalSpacesNeed, player))
            {
                PlayerManager.ShowNotification(player,
                    "Túi đồ không đủ chỗ trống, hãy sắp xếp lại ít nhất " + this.currentAward.NumberOfCoins +
                    " và thử lại!");
                return;
            }

            // Cập nhật bước kế tiếp
            this.fightStep = 4;

            // Thực hiện thêm phần thưởng tương ứng
            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, YouLong.Data.Items.YouLongCoinID,
                    this.currentAward.NumberOfCoins, 0, "YouLong_Script_Main", true, 1, false, Global.ConstGoodsEndTime))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi tạo Tiền Du Long!");
                //return;
            }

            // Ghi LOG
            LogManager.WriteLog(LogTypes.YouLongGe,
                string.Format("{0} (ID: {1}) đổi được {2} Tiền du long.", player.RoleName, player.RoleID,
                    this.currentAward.Number));

            // Hủy quà
            this.currentAward = null;

            // Thông báo
            PlayerManager.ShowNotification(player, "Đổi thành công!");

            // Gửi gói tin thông báo nhận và đổi thưởng thành công
            KT_TCPHandler.SendGetYouLongAwardSuccessfully(player);
        }

        /// <summary>
        /// Bắt đầu vòng khiêu chiến mới
        /// </summary>
        public void StartNewRound()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 4)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi bắt đầu vòng khiêu chiến mới!");
                return;
            }

            // Số lượt đã tham gia trong ngày
            var challengeTimes = CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong);

            // Nếu đã tham gia trong ngày rồi thì thôi
            if (challengeTimes >= YouLong_EventScript.GetDailyLimitYouLong(player))
            {
                PlayerManager.ShowNotification(player,
                    "Ngươi đã tham gia đủ số lượt trong ngày, không thể tham gia nữa!");
                return;
            }

            // Nếu không có chiến thư
            if (ItemManager.GetItemCountInBag(player, YouLong.Data.RequireItem) <= 0)
            {
                PlayerManager.ShowNotification(player, "Không có Chiến Thư, không thể khiêu chiến!");
                return;
            }

            // Thực hiện xóa chiến thư
            if (!ItemManager.RemoveItemFromBag(player, YouLong.Data.RequireItem, 1, -1, "YouLong"))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi thực hiện xóa Chiến Thư!");
                return;
            }

            GlobalEventSource.getInstance().fireEvent(new DuLongChienThuEventObject(player));
            // Tăng số lượt đi trong ngày
            // CopySceneEventManager.SetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong, challengeTimes + 1);

            // Xóa dữ liệu phần thưởng
            this.currentAward = null;
            this.randomAwards.Clear();

            // Cập nhật bước
            this.fightStep = 0;
            // Cập nhật tổng số lần tỷ thí lại
            this.rechallengeTimes = 0;

            // Danh sách vật phẩm ngẫu nhiên
            this.randomAwards = this.GetRandomAwardsList();

            // Bắt đầu tỷ thí
            this.BeginChallenge();

            // Gửi gói tin đóng khung thưởng
            KT_TCPHandler.SendCloseYouLongAwardsBox(player);

            // Ghi LOG
            LogManager.WriteLog(LogTypes.YouLongGe,
                $"{player.RoleName} (ID: {player.RoleID}) bắt đầu lượt khiêu chiến mới.");
        }

        /// <summary>
        /// Tỷ thí lại
        /// </summary>
        public void Rechallenge()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 4)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi bắt đầu vòng khiêu chiến mới!");
                return;
            }

            // Nếu số lần tỷ thí đã vượt quá giới hạn
            if (this.rechallengeTimes >= YouLong.Data.Awards.MaxTryTime)
            {
                PlayerManager.ShowNotification(player, "Đã vượt quá số lần có thể tỷ thí lại, hãy chọn Tỷ thí mới!");
                return;
            }

            // Số lượt đã tham gia trong ngày
            var challengeTimes = CopySceneEventManager.GetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong);
            if (challengeTimes < 0)
                challengeTimes = 0;
            // Nếu đã tham gia đủ ngày rồi thì thôi
            if (challengeTimes >= YouLong_EventScript.GetDailyLimitYouLong(player))
            {
                PlayerManager.ShowNotification(player,
                    "Ngươi đã tham gia đủ số lượt trong ngày, không thể tham gia nữa!");
                return;
            }

            // Nếu không có chiến thư
            if (ItemManager.GetItemCountInBag(player, YouLong.Data.RequireItem) <= 0)
            {
                PlayerManager.ShowNotification(player, "Không có Chiến Thư, không thể khiêu chiến!");
                return;
            }

            // Thực hiện xóa chiến thư
            if (!ItemManager.RemoveItemFromBag(player, YouLong.Data.RequireItem, 1, -1, "YouLong"))
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi thực hiện xóa Chiến Thư!");
                return;
            }

            GlobalEventSource.getInstance().fireEvent(new DuLongChienThuEventObject(player));
            // Tăng số lượt đi trong ngày
            // CopySceneEventManager.SetCopySceneTotalEnterTimesToday(player, AcitvityRecore.YouLong, challengeTimes + 1);

            // Hủy thông tin vật phẩm hiện tại
            this.currentAward = null;
            // Tăng tổng số lần tỷ thí lại
            this.rechallengeTimes++;
            // Bắt đầu tỷ thí
            this.BeginChallenge();

            // Gửi gói tin đóng khung thưởng
            KT_TCPHandler.SendCloseYouLongAwardsBox(player);

            // Ghi LOG
            LogManager.WriteLog(LogTypes.YouLongGe,
                $"{player.RoleName} (ID: {player.RoleID}) tái chiến.");
        }

        /// <summary>
        /// Rời khỏi phụ bản
        /// </summary>
        public void Leave()
        {
            // Người chơi tương ứng
            var player = this.teamPlayers.FirstOrDefault();
            // Nếu không có người chơi
            if (player == null)
            {
                return;
            }

            // Nếu không phải bước này
            if (this.fightStep != 4)
            {
                PlayerManager.ShowNotification(player, "Có lỗi khi thoát phụ bản!");
                return;
            }

            // Hủy phụ bản
            this.Dispose();

            // Gửi gói tin đóng khung thưởng
            KT_TCPHandler.SendCloseYouLongAwardsBox(player);
        }

        #endregion
    }
}