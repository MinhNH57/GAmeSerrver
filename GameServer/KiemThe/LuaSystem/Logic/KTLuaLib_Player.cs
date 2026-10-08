﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Activity.EveryDayActivity;
using GameServer.KiemThe.Core.BattlePass;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Rechage;
using GameServer.KiemThe.Core.Shop;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.BaiHuTang;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.FactionBattle;
using GameServer.KiemThe.GameEvents.GuildWarManager;
using GameServer.KiemThe.GameEvents.KuaFuBalance;
using GameServer.KiemThe.GameEvents.LeagueBattle;
using GameServer.KiemThe.GameEvents.LotteryEvent;
using GameServer.KiemThe.GameEvents.Medal;
using GameServer.KiemThe.GameEvents.TeamBattle;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.Battle;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.KiemThe.Logic.Manager.Shop;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using GameServer.Logic.Name;
using GameServer.Server;
using MoonSharp.Interpreter;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameServer.KiemThe.LuaSystem.Logic
{
    /// <summary>
    /// Cung cấp thư viện dùng cho Lua, tương tác với người chơi
    /// </summary>
    [MoonSharpUserData]
    public static class KTLuaLib_Player
    {
        /// <summary>
        /// Mở Shop tương ứng
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <param name="shopID"></param>
        public static void OpenShop(Lua_NPC npc, Lua_Player player, int shopID)
        {
            if (player == null || player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, Player is NULL."));
                return;
            }

            if (npc == null || npc.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, NPC is NULL."));
                return;
            }

            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.RefObject.CurrentPos.X, (int)player.RefObject.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.RefObject.CurrentPos.X, (int)npc.RefObject.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);
            if (Dis > 100)
            {
                PlayerManager.ShowNotification(player.RefObject, "Khoảng cách quá xa");
                return;
            }

            /// Đánh dấu vị trí mở Shop lần trước từ NPC
            player.RefObject.LastShopNPC = npc.RefObject;

            player.RefObject.LastOpenNPCShopTicks = KTGlobal.GetCurrentTimeMilis();

            ShopTab _Shop = ShopManager.GetShopTable(shopID, player.RefObject);

            if (_Shop != null)
            {
                KT_TCPHandler.SendShopData(player.RefObject, _Shop);
            }
            else
            {
                PlayerManager.ShowNotification(player.RefObject, "Cửa hàng bạn tìm không tồn tại");
                return;
            }
        }

        public static void TokenTransClick(Lua_NPC npc, Lua_Player player)
        {
            RechageServiceManager.ClickRechage(npc.RefObject, player.RefObject);
        }


        public static void BuyDiscount(Lua_NPC npc, Lua_Player player)
        {
            ShopSalePrestige.ClickNpcBuyRequest(npc.RefObject, player.RefObject);
        }

        public static void BuyGatherMakerPoint(Lua_NPC npc, Lua_Player player)
        {
            ShopSalePrestige.BuyGatherMakerPoint(npc.RefObject, player.RefObject);
        }

        public static void ChangeMoney(Lua_NPC npc, Lua_Player player)
        {
            ShopSalePrestige.ChangeMoney(npc.RefObject, player.RefObject);
        }

        public static void ChangeBoundToken(Lua_NPC npc, Lua_Player player)
        {
            ShopSalePrestige.ChangeBoundToken(npc.RefObject, player.RefObject);
        }

        /// <summary>
        /// Gửi thông báo về client
        /// </summary>
        /// <param name="MSG"></param>
        /// <param name="RefObject"></param>
        /// <param name="npc"></param>
        public static void SendMSG(string MSG, KPlayer RefObject, NPC npc)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();

            _NpcDialog.Text = MSG;

            _NpcDialog.Show(npc, RefObject);
        }

        public static void SendItemMsg(string MSG, GoodsData data, KPlayer RefObject)
        {
            KItemDialog _NpcDialog = new KItemDialog();

            _NpcDialog.Text = MSG;

            _NpcDialog.Show(data, RefObject);
        }

        /// <summary>
        /// Ghi nhật ký ngày theo key
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static int GetValueOfDailyRecore(Lua_Player player, int Key)
        {
            int VALUE = player.RefObject.GetValueOfDailyRecore(Key);
            return VALUE;
        }

        /// <summary>
        /// Getmoney
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static int CheckMoney(Lua_Player player, int MoneyType)
        {
            int VALUE = KTGlobal.GetMoneyhave(player.RefObject, (KiemThe.Entities.MoneyType)MoneyType);

            return VALUE;
        }




        public static int CheckGuildWarState()
        {
            int STATE = (int)GuidWarManager.getInstance().BATTLESTATE;
            return STATE;
        }

        public static bool IsInKuaFuGuildWarMap(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return GuidWarManager.getInstance().IsInKuaFuGuildWarMap(player.RefObject);
        }

        public static bool KuaFuBalanceIsActive(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.IsInBalance(player.RefObject)
                || KuaFuBalanceManager.Instance.IsBalanceMap(player.RefObject);
        }

        public static bool KuaFuBalanceReceiveEquipment(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.ReceiveTemporaryEquipment(player.RefObject);
        }

        public static bool KuaFuBalanceHasSelectedFaction(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.HasSelectedTemporaryFaction(player.RefObject);
        }

        public static bool KuaFuBalanceSelectFaction(Lua_Player player, int factionID, int routeID)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.SelectTemporaryFaction(player.RefObject, factionID, routeID);
        }

        public static bool KuaFuBalanceReissue(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.ReissueTemporaryEquipment(player.RefObject);
        }

        public static bool KuaFuBalanceSetTemporaryLevel(Lua_Player player, int level)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.SetTemporaryLevel(player.RefObject, level);
        }

        public static bool KuaFuBalanceResetPotential(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.ResetTemporaryPotential(player.RefObject);
        }

        public static bool KuaFuBalanceResetSkills(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.ResetTemporarySkillPoints(player.RefObject);
        }

        public static bool KuaFuBalanceDeleteTemporaryEquipment(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.DeleteAllTemporaryEquipment(player.RefObject);
        }

        public static void KuaFuBalanceLeave(Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                return;
            }

            KuaFuBalanceManager.Instance.LeaveBalance(player.RefObject, true);
        }

        public static bool KuaFuBalanceAddTemporaryItem(Lua_Player player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, int ExpriesTime = -1)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.AddTemporarySessionItem(player.RefObject, ItemID, Number, Series, LockStatus, enhanceLevel, ExpriesTime);
        }

        public static bool KuaFuBalanceSetTemporaryBookLevelAndExp(Lua_Player player, int level, int exp)
        {
            if (player == null || player.RefObject == null)
            {
                return false;
            }

            return KuaFuBalanceManager.Instance.SetTemporaryBookLevelAndExp(player.RefObject, level, exp);
        }

        /// <summary>
        /// Hàm trừ tiền của người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="MoneyType"></param>
        /// <param name="SubNumbber"></param>
        /// <returns></returns>
        public static bool MinusMoney(Lua_Player player, int MoneyType, int SubNumbber)
        {
            if (MoneyType > (int)KiemThe.Entities.MoneyType.KuafuMoney || MoneyType < (int)KiemThe.Entities.MoneyType.Bac)
            {
                return false;
            }
            else
            {
                SubRep _Submoney = KTGlobal.SubMoney(player.RefObject, SubNumbber, (KiemThe.Entities.MoneyType)MoneyType, "LUASCRIPT");

                return _Submoney.IsOK;
            }
        }

        /// <summary>
        /// Set trị Pk cho nhân vật
        /// </summary>
        /// <param name="player"></param>
        /// <param name="PkValue"></param>
        public static void SetPKValue(Lua_Player player, int PkValue)
        {
            player.RefObject.PKValue = PkValue;
        }

        /// <summary>
        /// Set giá trị ngày theo key
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public static void SetValueOfDailyRecore(Lua_Player player, int Key, int Value)
        {
            player.RefObject.SetValueOfDailyRecore(Key, Value);
        }

        public static long GetBaseExp(Lua_Player player, int Level)
        {
            return KPlayerSetting.GetBaseExpLevel(Level);
        }

        public static int GetBCH(Lua_Player player, int Level)
        {
            if (Level == 1)
            {
                return player.RefObject.baijuwan;
            }
            else if (Level == 2)
            {
                return player.RefObject.baijuwanpro;
            }    
            else if (Level == 3)
            {
                return player.RefObject.baijuwanpromax;
            }    
            else
            {
                return player.RefObject.baijuwanvip;
            }
        }

        public static void SetBCH(Lua_Player player, int Value, int Level)
        {
            if (Level == 1)
            {
                player.RefObject.baijuwan = player.RefObject.baijuwan + Value;
            }
            else if (Level == 2)
            {
                player.RefObject.baijuwanpro = player.RefObject.baijuwanpro + Value;
            }    
            else if (Level == 3)
            {
                player.RefObject.baijuwanpromax = player.RefObject.baijuwanpromax + Value;
            }    
            else
            {
                player.RefObject.baijuwanvip = player.RefObject.baijuwanvip + Value;
            }
        }

        /// <summary>
        /// Tăng điểm tiềm năng tương ứng
        /// </summary>
        /// <param name="player"></param>
        public static bool UsingPotentialPoints(Lua_Player player, int bookLevel = 1)
        {
            var itemID = bookLevel == 1 ? 492 : 493;
            var recordDef = bookLevel == 1 ? RecoreDef.UsingPotentialBook : RecoreDef.UsingPotentialBook2;
            var itemCount = ItemManager.GetItemCountInBag(player.RefObject, itemID);
            if (itemCount < 1)
            {
                PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Không tìm thấy Tẩy Tủy Kinh");
                return false;
            }
            else
            {
                if (bookLevel == 2)
                {
                    var usingCount_level1 = player.RefObject.GetValueOfForeverRecore(RecoreDef.UsingPotentialBook);
                    if (usingCount_level1 < 5)
                    {
                        PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Cần dùng hết lượt Tẩy Tủy Kinh (sơ) trước.");
                        return false;
                    }
                }
                int UsingCount = player.RefObject.GetValueOfForeverRecore(recordDef);
                int increaseLimit = bookLevel == 2 ?
                    Global.GetRoleParamsInt32FromDB(player.RefObject, RoleParamName.PotentialBookLimitIncrease) : 0;

                if (increaseLimit == -1)
                {
                    increaseLimit = 0;
                }

                if (UsingCount < 5 + increaseLimit)
                {
                    if (ItemManager.RemoveItemFromBag(player.RefObject, itemID, 1, -1, "UsingPotentialBook"))
                    {
                        if (UsingCount == -1)
                        {
                            UsingCount = 0;
                        }
                        player.RefObject.SetValueOfForeverRecore(recordDef, UsingCount + 1);

                        int Before = player.RefObject.GetBonusRemainPotentialPoints();
                        player.RefObject.SetBonusRemainPotentialPoints(Before + 10);
                        return true;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Không thể xóa vật phẩm");
                        return false;
                    }
                }
                else
                {
                    PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", $"Bạn đã sử dụng hết {5 + increaseLimit} lần");
                }
            }

            return false;
        }

        /// <summary>
        /// Thêm điểm kỹ năng tương ứng cho nhân vật
        /// </summary>
        /// <param name="player"></param>
        /// <param name="nAdd"></param>
        public static void AddBonusSkillPoint(Lua_Player player, int nAdd)
        {
            int currentSkillPoint = player.RefObject.GetBonusSkillPoint();
            currentSkillPoint += nAdd;
            player.RefObject.SetBonusSkillPoint(currentSkillPoint);
        }

        /// <summary>
        /// Tăng điểm kỹ năng tương ứng
        /// </summary>
        /// <param name="player"></param>
        public static bool UsingSkillBoundPoints(Lua_Player player, int bookLevel = 1)
        {
            var itemID = bookLevel == 1 ? 490 : 491;
            var recordDef = bookLevel == 1 ? RecoreDef.UsingSkillBoundBook : RecoreDef.UsingSkillBoundBook2;
            
            var  itemCount = ItemManager.GetItemCountInBag(player.RefObject, itemID);
            if (itemCount < 1)
            {
                PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Không tìm thấy Võ Lâm Mật Tịch");
                return false;
            }
            else
            {
                if (bookLevel == 2)
                {
                    var usingCount_level1 = player.RefObject.GetValueOfForeverRecore(RecoreDef.UsingSkillBoundBook);
                    if (usingCount_level1 < 5)
                    {
                        PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Cần dùng hết lượt Võ Lâm Mật Tịch (sơ) trước.");
                        return false;
                    }
                }
                int UsingCount = player.RefObject.GetValueOfForeverRecore(recordDef);
                int increaseLimit = bookLevel == 2 ?
                    Global.GetRoleParamsInt32FromDB(player.RefObject, RoleParamName.SkillBoundBookLimitIncrease) : 0;

                if (increaseLimit < 0)
                {
                    increaseLimit = 0;
                }
                
                if (UsingCount < 5 + increaseLimit)
                {
                    if (ItemManager.RemoveItemFromBag(player.RefObject, itemID, 1, -1, "UsingSkillBoundBook"))
                    {
                        if (UsingCount == -1)
                        {
                            UsingCount = 0;
                        }
                        player.RefObject.SetValueOfForeverRecore(recordDef, UsingCount + 1);

                        int Before = player.RefObject.GetBonusSkillPoint();
                        player.RefObject.SetBonusSkillPoint(Before + 1);
                        return true;
                    }
                    else
                    {
                        PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", "Không thể xóa vật phẩm");
                        return false;
                    }
                }
                else
                {
                    PlayerManager.ShowMessageBox(player.RefObject, "Thông báo", $"Bạn đã sử dụng hết {5 + increaseLimit} lần");
                }
            }

            return false;
        }

        public static bool IncreaseBookUseLimit(Lua_Player player, int itemID, int type, int limitIncreased)
        {
            var key = type == 1 ?
                RoleParamName.SkillBoundBookLimitIncrease : RoleParamName.PotentialBookLimitIncrease;

            var currentLimit = Global.GetRoleParamsInt32FromDB(player.RefObject, key);
            if (currentLimit == -1)
            {
                currentLimit = 0;
            }

            currentLimit += limitIncreased;

            if (ItemManager.RemoveItemFromBag(player.RefObject, itemID, 1, -1, "IncreaseBookUseLimit"))
            {
                Global.SaveRoleParamsInt32ValueToDB(player.RefObject, key, currentLimit, true);

                var bookType = type == 1 ? "Võ Lâm Mật Tịch [Trung]" : "Tẩy Tủy Kinh [Trung]";
                PlayerManager.ShowMessageBox(player.RefObject, "Thông báo",
                    $"Đã tăng giới hạn sử dụng {bookType} thêm {limitIncreased} lần. " +
                    $"Tổng giới hạn hiện tại: {currentLimit + 5}");

                return true;
            }
            
            return false;
        }

        public static int GetSeries(Lua_Player player)
        {
            return (int)player.RefObject.m_Series;
        }

        public static void OpenBatchRandomBox(Lua_Player player, int boxID, int times)
        {
            ItemRandomBox.OpenBoxRandomInBatch(player.RefObject, boxID, times);
        }
        public static void OpenDialogLottery(Lua_Player player)
        {

        }

        public static void OpenAllRandomBox(Lua_Player player, int goodId, int quantity) 
        {
            ItemRandomBox.OpenAllRandomBox(player.RefObject, goodId, quantity);
        }

        /// <summary>
        /// Mở rương tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="boxDbID"></param>
        /// <returns></returns>
        public static bool OpenRandomBox(Lua_Player player, int boxDbID)
        {
            return ItemRandomBox.OpenBoxRandom(player.RefObject, boxDbID);
        }


        public static bool OpenRandomBoxByConfigID(Lua_Player player, Lua_Item item, int boxID) 
        {
            return ItemRandomBox.OpenRandomBoxByConfigID(player.RefObject, boxID, item.RefObject.GoodsID, item.RefObject.Id);
        }

        public static bool OpenRandomBoxSpecial(Lua_Player player, int boxDbID)
        {
            return ItemBoxSpecial.OpenBoxSpecial(player.RefObject, boxDbID);
        }
        public static void CheckRemoveRandomBoxSpecial(Lua_Player player, int boxDbID)
        {
            ItemBoxSpecial.CheckRemoveItemBoxSpecial(player.RefObject, boxDbID);
        }
        public static void AddMoreDayOpenItem(Lua_Player player, int boxDbID)
        {
            ItemBoxSpecial.AddMoreDayOpenItem(player.RefObject, boxDbID);
        }
        public static int GetNumberDayUsedSpecialBox(Lua_Player player, int boxDbID)
        {
            int numberDay = -1;
            if (!ItemBoxSpecial.IsHaveDataBoxSpecial(player.RefObject, boxDbID))
                numberDay = -1;
            else
                numberDay = DateTime.Now.DayOfYear - ItemBoxSpecial.GetDataBoxSpecial(player.RefObject, boxDbID, DataGet.TimeOpen);
            return numberDay;
        }
        public static int GetStepCollectSpecialBox(Lua_Player player, int boxDbID)
        {
            return ItemBoxSpecial.GetDataBoxSpecial(player.RefObject, boxDbID, DataGet.Step);
        }
        public static int GetMaxStepCanCollectSpecialBox(Lua_Player player, int boxDbID)
        {
            return ItemBoxSpecial.GetMaxStepCanCollect(player.RefObject, boxDbID);
        }

        public static int GetValueWeeklyRecore(Lua_Player player, int Key)
        {
            return player.RefObject.GetValueOfWeekRecore(Key);
        }

        public static void SetValueOfWeekRecore(Lua_Player player, int Key, int Value)
        {
            player.RefObject.SetValueOfWeekRecore(Key, Value);
        }

        /// <summary>
        /// Lấy ra đánh dấu vĩnh viễn của nhân vật
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static int GetValueForeverRecore(Lua_Player player, int Key)
        {
            return player.RefObject.GetValueOfForeverRecore((RecoreDef)Key);
        }

        /// <summary>
        /// set giá trị cho đánh dấu vĩnh viễn của nhân vật
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public static void SetValueOfForeverRecore(Lua_Player player, int Key, int Value)
        {
            player.RefObject.SetValueOfForeverRecore((RecoreDef)Key, Value);
        }

        /// <summary>
        /// Thêm danh vọng tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key"></param>
        /// <param name="point"></param>
        public static void AddRetupeValue(Lua_Player player, int key, int point)
        {
            KTGlobal.AddRepute(player.RefObject, key, point);
        }

        /// <summary>
        /// Trả về danh vọng tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static int GetRepute(Lua_Player player, int key)
        {
            ReputeInfo reputeInfo = player.RefObject.GetRepute().Where(x => x.DBID == key).FirstOrDefault();
            if (reputeInfo != null)
            {
                return reputeInfo.Level;
            }
            return -1;
        }

        /// <summary>
        /// Add tiền từ lua
        /// </summary>
        /// <param name="player"></param>
        /// <param name="MoneyAdd"></param>
        /// <param name="_MoneyType"></param>
        /// <returns></returns>
        public static bool AddMoney(Lua_Player player, int MoneyAdd, int _MoneyType)
        {
            MoneyType _Money = (MoneyType)_MoneyType;

            SubRep _SUB = KTGlobal.AddMoney(player.RefObject, MoneyAdd, _Money);
            return _SUB.IsOK;
        }

        public static bool SubMoney(Lua_Player player, int MoneySub, int _MoneyType, string from)
        {
            MoneyType _Money = (MoneyType)_MoneyType;

            SubRep _SUB = KTGlobal.SubMoney(player.RefObject, MoneySub, _Money, from);
            return _SUB.IsOK;
        }

        /// <summary>
        /// Add tiền từ lua
        /// </summary>
        /// <param name="player"></param>
        /// <param name="MoneyAdd"></param>
        /// <param name="_MoneyType"></param>
        /// <returns></returns>
        public static bool AddMoney(Lua_Player player, int MoneyAdd, int _MoneyType, string source)
        {
            MoneyType _Money = (MoneyType)_MoneyType;

            SubRep _SUB = KTGlobal.AddMoney(player.RefObject, MoneyAdd, _Money, source);
            return _SUB.IsOK;
        }

        /// <summary>
        /// Sử dụng Trục Cuốn Công Trạng từ Lua.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemDbID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool UseMedalMeritScroll(Lua_Player player, int itemDbID, int amount)
        {
            if (player == null || player.RefObject == null || itemDbID <= 0 || amount <= 0)
            {
                return false;
            }

            GoodsData goodsData = Global.GetGoodsByDbID(player.RefObject, itemDbID);
            if (goodsData == null)
            {
                return false;
            }

            if (!ItemManager.RemoveItemByCount(player.RefObject, goodsData, 1, "MEDAL_MERIT_SCROLL"))
            {
                return false;
            }

            MedalManager.AddPaidMerit(player.RefObject, amount, string.Format("MeritScroll:{0}", goodsData.GoodsID));
            ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player.RefObject, -1, -1, goodsData.GoodsID, TaskTypes.UseSomething);
            return true;
        }

        /// <summary>
        /// Sử dụng nhiều Trục Cuốn Công Trạng từ Lua.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemDbID"></param>
        /// <param name="amount"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool UseMedalMeritScrollMultiple(Lua_Player player, int itemDbID, int amount, int count)
        {
            if (player == null || player.RefObject == null || itemDbID <= 0 || amount <= 0 || count <= 0)
            {
                return false;
            }

            if (count > 100)
            {
                count = 100;
            }

            GoodsData goodsData = Global.GetGoodsByDbID(player.RefObject, itemDbID);
            if (goodsData == null || goodsData.GCount < count)
            {
                return false;
            }

            if (!ItemManager.RemoveItemByCount(player.RefObject, goodsData, count, "MEDAL_MERIT_SCROLL"))
            {
                return false;
            }

            MedalManager.AddPaidMerit(player.RefObject, amount * count, string.Format("MeritScroll:{0}", goodsData.GoodsID));
            for (int i = 0; i < count; i++)
            {
                ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player.RefObject, -1, -1, goodsData.GoodsID, TaskTypes.UseSomething);
            }
            return true;
        }

        /// <summary>
        /// Trả về khoảng trống trong túi người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool HasFreeBagSpaces(Lua_Player player, int number)
        {
            return KTGlobal.IsHaveSpace(number, player.RefObject);
        }

        /// <summary>
        /// Trả về số ô trống trong túi cần để lấy vật phẩm
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int GetTotalSpacesNeedToTakeItem(int itemID, int number)
        {
            return KTGlobal.GetTotalSpacesNeedToTakeItem(itemID, number);
        }
        
        public static bool AddItemLuaAndNotify(Lua_Player player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, string ExpriesTime = "")
        {
            int SpaceNeed = ItemManager.TotalSpaceNeed(ItemID, Number);

            if (!KTGlobal.IsHaveSpace(SpaceNeed, player.RefObject))
            {
                PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (!string.IsNullOrEmpty(ExpriesTime))
                {

                    // "1900-01-01 12:00:00";
                    TimeUsing = ExpriesTime;
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, ItemID, Number, 0, "LUAITEMADD", true, LockStatus, false, TimeUsing, "", Series, "", enhanceLevel, 1, true))
                {
                    PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        
        public static bool AddItemLuaAndNotify(Lua_Player player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, int ExpriesTime = -1)
        {
            int SpaceNeed = ItemManager.TotalSpaceNeed(ItemID, Number);

            if (!KTGlobal.IsHaveSpace(SpaceNeed, player.RefObject))
            {
                PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (ExpriesTime != -1)
                {
                    DateTime dt = DateTime.Now.AddMinutes(ExpriesTime);

                    // "1900-01-01 12:00:00";
                    TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, ItemID, Number, 0, "LUAITEMADD", true, LockStatus, false, TimeUsing, "", Series, "", enhanceLevel, 1, true))
                {
                    PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

         public static bool AddItemLua(Lua_Player player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, string ExpriesTime = "")
        {
            int SpaceNeed = ItemManager.TotalSpaceNeed(ItemID, Number);

            if (!KTGlobal.IsHaveSpace(SpaceNeed, player.RefObject))
            {
                PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (!string.IsNullOrEmpty(ExpriesTime))
                {

                    // "1900-01-01 12:00:00";
                    TimeUsing = ExpriesTime;
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, ItemID, Number, 0, "LUAITEMADD", true, LockStatus, false, TimeUsing, "", Series, "", enhanceLevel, 0, true))
                {
                    PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Add vật phẩm từ lua
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ItemID"></param>
        /// <param name="Number"></param>
        /// <param name="LockStatus"></param>
        /// <param name="enhanceLevel"></param>
        /// <returns></returns>
        public static bool AddItemLua(Lua_Player player, int ItemID, int Number, int Series, int LockStatus, int enhanceLevel = 0, int ExpriesTime = -1)
        {
            int SpaceNeed = ItemManager.TotalSpaceNeed(ItemID, Number);

            if (!KTGlobal.IsHaveSpace(SpaceNeed, player.RefObject))
            {
                PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (ExpriesTime != -1)
                {
                    DateTime dt = DateTime.Now.AddMinutes(ExpriesTime);

                    // "1900-01-01 12:00:00";
                    TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, ItemID, Number, 0, "LUAITEMADD", false, LockStatus, false, TimeUsing, "", Series, "", enhanceLevel, 0, true))
                {
                    PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Add list vật phẩm từ lua
        /// </summary>
        /// <param name="player"></param>
        /// <param name="items"></param>
        /// <param name="totalNum"></param>
        /// <param name="ExpriesTime"></param>
        /// <returns></returns>
        public static bool AdddItemsLua(Lua_Player player, string items, int totalNum, int ExpriesTime = -1)
        {
            if (!KTGlobal.IsHaveSpace(totalNum, player.RefObject))
            {
                PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                return false;
            }
            else
            {
                string TimeUsing = Global.ConstGoodsEndTime;
                if (ExpriesTime != -1)
                {
                    DateTime dt = DateTime.Now.AddMinutes(ExpriesTime);

                    // "1900-01-01 12:00:00";
                    TimeUsing = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (!String.IsNullOrEmpty(items))
                {
                    string[] itemList = items.Split(';');
                    foreach (string item in itemList)
                    {
                        string[] itemInfo = item.Split('_');
                        int itemID = Convert.ToInt32(itemInfo[0]);
                        int number = Convert.ToInt32(itemInfo[1]);
                        int binding = Convert.ToInt32(itemInfo[2]);
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, itemID, number, 0, "LUAITEMADD", false, binding, false, TimeUsing, "", 0, "", 0, 0, true))
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");
                            LogManager.WriteLog(LogTypes.Error, $"Người chơi: {player.RefObject.RoleName} nhận vật phẩm thất bại từ Tần Lăng itemID={itemID}, số lượng={number}, khóa={binding}");
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Add Exp cho người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Exp"></param>
        public static void AddRoleExp(Lua_Player player, int Exp)
        {
            GameManager.ClientMgr.ProcessRoleExperience(player.RefObject, Exp, true, false, true, "LuaItem");
        }

        public static bool IsHaveFamily(Lua_Player player)
        {
            if (player != null)
            {
                if (player.RefObject.FamilyID > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///  Tìm một vật phẩm từ TEMPALTE
        /// </summary>
        /// <param name="Genre"></param>
        /// <param name="DetailType"></param>
        /// <param name="ParticularType"></param>
        /// <param name="Level"></param>
        /// <returns></returns>
        public static int FindItemID(int Genre, int DetailType, int ParticularType, int Level)
        {
            var find = ItemManager.TotalItem.Where(x => x.Genre == Genre && x.DetailType == DetailType && x.ParticularType == ParticularType && x.Level == Level).FirstOrDefault();
            if (find != null)
            {
                return find.ItemID;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// aDD HOẠT LỰC CHO NHÂN VẬT
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Point"></param>
        public static void AddMakePoint(Lua_Player player, int Point)
        {
            player.RefObject.ChangeCurMakePoint(Point);
        }

        /// <summary>
        ///aDD TINH LỰC CHO NHÂN VẬT
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Point"></param>
        public static void AddCurGatherPoint(Lua_Player player, int Point)
        {
            player.RefObject.ChangeCurGatherPoint(Point);
        }

        /// <summary>
        /// Đếm số lượng vật phẩm theo ITEMID
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ItemID"></param>
        /// <returns></returns>
        public static int CountItemInBag(Lua_Player player, int ItemID)
        {
            return ItemManager.GetItemCountInBag(player.RefObject, ItemID);
        }

        public static int CountCrystalInBag(Lua_Player player, int crystal_level) 
        {
            var crystal_id_1 = crystal_level + 182;
            var crystal_id_2 = crystal_level + 384;
            var total = ItemManager.GetItemCountInBag(player.RefObject, crystal_id_1) + ItemManager.GetItemCountInBag(player.RefObject, crystal_id_2);
            return total;
        }

        public static int CountItemInBag(Lua_Player player, int itemID, int binding) 
        {
            return ItemManager.GetItemCountInBag(player.RefObject, itemID, binding);
        }

        public static Lua_Item GetItemWithLevel(Lua_Player player, int itemID, int level)
        {
            var item = ItemManager.GetItemWithLevel(player.RefObject, itemID, level);
            if (item == null)
                return null;
            
            return new Lua_Item
            {
                RefObject = item,
                CurrentScene = player.CurrentScene
            };
        }

        public static bool RemoveCrystal(Lua_Player player, int crystal_level, int number) 
        {
            var crystal_id_1 = crystal_level + 182;
            var crystal_id_2 = crystal_level + 384;
            var count_2 = ItemManager.GetItemCountInBag(player.RefObject, crystal_id_2);

            if (count_2 > number) 
            {
                return RemoveItem(player, crystal_id_2, number);
            }

            var result = RemoveItem(player, crystal_id_2, count_2);
            var count_left = number - count_2;
            return result && RemoveItem(player, crystal_id_1, count_left);
        }

        /// <summary>
        /// Xóa vật phẩm từ LUA
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ItemDbID"></param>
        /// <returns></returns>
        public static bool RemoveItem(Lua_Player player, int ItemDbID)
        {
            GoodsData gd = Global.GetGoodsByDbID(player.RefObject, ItemDbID);
            if (gd == null)
            {
                return false;
            }

            if (ItemManager.RemoveItemByCount(player.RefObject, gd, 1, "LUASCRIPT"))
            {
                ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player.RefObject, -1, -1, gd.GoodsID, TaskTypes.UseSomething);

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RemoveItemBySlotCount(Lua_Player player, int ItemDbID)
        {
            GoodsData gd = Global.GetGoodsByDbID(player.RefObject, ItemDbID);
            if (gd == null)
            {
                return false;
            }

            if (ItemManager.RemoveItemByCount(player.RefObject, gd, gd.GCount, "LUASCRIPT"))
            {
                ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player.RefObject, -1, -1, gd.GoodsID, TaskTypes.UseSomething);

                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Hàm sửa toàn bộ trang bị đang mang bằng kim tê
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ItemDbID"></param>
        public static void RepairAllItem(Lua_Player player, int ItemDbID)
        {

            GoodsData Good = Global.GetGoodsByDbID(player.RefObject, ItemDbID);

            if (Good != null)
            {
                int Quanity = 0;
                if (ItemManager.KD_ISJINXI(Good.GoodsID))
                {
                    if (Good.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string NumberCanRepair))
                    {
                        Quanity = Int32.Parse(NumberCanRepair);
                    }
                }

                int TotalValueNeedFix = 0;

                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in player.RefObject._KPlayerEquipBody.EquipBody)
                {
                    if (entry.Value._GoodDatas.Strong < 100)
                    {
                        //double nTypeRate = 100;
                        //
                        //Equip_Type_Rate TypeRate = ItemManager._Calutaion.List_Equip_Type_Rate.Where(x => (int)x.EquipType == entry.Value._ItemData.DetailType).FirstOrDefault();
                        //if (TypeRate != null)
                        //{
                        //    nTypeRate = TypeRate.Value;
                        //}
                        //
                        //double nRate = 1.0 * nTypeRate / 100;
                        //
                        //double nFinalValue = Math.Max(entry.Value._ItemData.ItemValue, ItemEnhance.ALL_EQUIP_MIN_VALUE * nRate / ItemEnhance.EQUIP_TOTAL_RATE);
                        //nFinalValue = Math.Min(nFinalValue, ItemEnhance.ALL_EQUIP_MAX_VALUE * nRate / ItemEnhance.EQUIP_TOTAL_RATE);
                        //double nCoinCostPerDur = (nFinalValue * ItemEnhance.VALUEPERCEN_PER_YEAR / ItemEnhance.DUR_COST_PER_YEAR) / 100;

                        //TotalValueNeedFix += (100 - entry.Value._GoodDatas.Strong) * nCoinCostPerDur;
                        TotalValueNeedFix += (100 - entry.Value._GoodDatas.Strong);
                    }
                }

                var equipPet = player.RefObject.GoodsDataList.Where(x => x.Using >= (int)KE_EQUIP_POSITION.emEQUIPPOS_PET_HEAD && x.Using <= (int)KE_EQUIP_POSITION.emEQUIPPOS_PET_SIGNET && x.IsItemPet()).ToList();
                if (equipPet != null && equipPet.Count > 0)
                {
                    foreach (var equip in equipPet)
                    {
                        TotalValueNeedFix += (100 - equip.Strong);
                    }    
                }    
                if (TotalValueNeedFix > 0)
                {
                    if (TotalValueNeedFix <= Quanity)
                    {
                        Action _OK = new Action(() =>
                        {
                            // update lại giá trị cho kim tê
                            Quanity = Quanity - TotalValueNeedFix;
                            if (Quanity > 0)
                            {
                                var TotalUpdateKT = new Dictionary<UPDATEITEM, object>
                                {
                                    { UPDATEITEM.ROLEID, player.RefObject.RoleID },
                                    { UPDATEITEM.ITEMDBID, Good.Id }
                                };
                                var OtherParams = new Dictionary<ItemPramenter, string>
                                {
                                    { ItemPramenter.Pram_1, Quanity + "" }
                                };
                                TotalUpdateKT.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                                ItemManager.UpdateItemPrammenter(TotalUpdateKT, Good, player.RefObject);
                            }    
                            else
                            {
                                // xóa kim tê
                                ItemManager.AbadonItem(ItemDbID, player.RefObject, false, "KIMTEUSING");
                            }    

                            // update độ bền cho các trang bị
                            foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in player.RefObject._KPlayerEquipBody.EquipBody)
                            {
                                if (entry.Value._GoodDatas.Strong < 100)
                                {
                                    entry.Value._GoodDatas.Strong = 100;
                                    // update lại giá trị
                                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                                    TotalUpdate.Add(UPDATEITEM.ROLEID, player.RefObject.RoleID);
                                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, entry.Value._GoodDatas.Id);
                                    TotalUpdate.Add(UPDATEITEM.STRONG, 100);

                                    ItemManager.UpdateItemPrammenter(TotalUpdate, entry.Value._GoodDatas, player.RefObject);
                                }
                            }

                            if (equipPet != null && equipPet.Count > 0)
                            {
                                foreach (var equip in equipPet)
                                {
                                    equip.Strong = 100;
                                    // update lại giá trị
                                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                                    TotalUpdate.Add(UPDATEITEM.ROLEID, player.RefObject.RoleID);
                                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, equip.Id);
                                    TotalUpdate.Add(UPDATEITEM.STRONG, 100);

                                    ItemManager.UpdateItemPrammenter(TotalUpdate, equip, player.RefObject);
                                }
                            }
                        });
                        PlayerManager.ShowMessageBox(player.RefObject, "Sửa đồ", "Độ bền cần sửa :" + TotalValueNeedFix + ". Ngươi có chắc chắn muốn sửa không", _OK);
                    }    
                    else
                    {
                        PlayerManager.ShowNotification(player.RefObject, "Kim tê không đủ điểm để sửa toàn bộ trang bị");
                    }    
                }
                else
                {
                    PlayerManager.ShowNotification(player.RefObject, "Không có trang bị nào cần sửa");
                }
            }
        }

        /// <summary>
        ///  Hàm sử dụng kim tê
        /// </summary>
        public static void UsingJINXI(Lua_Player player, int ItemDbID)
        {
            GoodsData Good = Global.GetGoodsByDbID(player.RefObject, ItemDbID);

            if (Good != null)
            {
                ItemData _Item = ItemManager.GetItemTemplate(Good.GoodsID);
                if (_Item != null)
                {
                    if (ItemManager.KD_ISJINXI(Good.GoodsID))
                    {
                        int Quanity = 0;
                        if (Good.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string NumberCanRepair))
                        {
                            Quanity = Int32.Parse(NumberCanRepair);
                        }

                        KItemDialog _NpcDialog = new KItemDialog();

                        Dictionary<int, string> Selections = new Dictionary<int, string>();

                        string Text = "Sử dụng Kim Tê để sửa trang bị\nĐộ bền của kim tê hao hụt khi bạn sử dụng để sửa trạng bị\nĐộ bền hiện tại của kim tê là :" + KTGlobal.CreateStringByColor(Quanity + "", ColorType.Yellow) + "\nChọn vật phẩm muốn sửa :";

                        foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in player.RefObject._KPlayerEquipBody.EquipBody)
                        {
                            Selections.Add(entry.Value._GoodDatas.Id, entry.Value._ItemData.Name + "| Độ bền :" + entry.Value._GoodDatas.Strong);
                        }

                        Action<TaskCallBack> ActionWork = (x) => DoActionSelect(player, ItemDbID, x);

                        _NpcDialog.OnSelect = ActionWork;

                        _NpcDialog.Selections = Selections;

                        _NpcDialog.Text = Text;

                        _NpcDialog.ShowDialog(Good, player.RefObject);
                    }
                }
            }
        }

        /// <summary>
        ///  Hàm sử dụng càn khôn phù
        /// </summary>
        public static void UsingQianKunFu(Lua_Player player, int ItemDbID)
        {
            GoodsData Good = Global.GetGoodsByDbID(player.RefObject, ItemDbID);

            if (Good != null)
            {
                ItemData _Item = ItemManager.GetItemTemplate(Good.GoodsID);
                if (_Item != null)
                {
                    // Nếu đây đúng là càn khôn phù
                    if (ItemManager.IsQianKunFu(Good.GoodsID))
                    {
                        // Lấy ra số lượt có thể sử dụng
                        int Quanity = 0;
                        if (Good.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string NumberCanRepair))
                        {
                            Quanity = Int32.Parse(NumberCanRepair);
                        }

                        KItemDialog _NpcDialog = new KItemDialog();

                        Dictionary<int, string> Selections = new Dictionary<int, string>();

                        string Text = "Sử dụng càn khôn phù có thể phù tới vị trí của đồng đội trong giây lát\nSố lần sử dụng còn lại của càn khôn phù :" + KTGlobal.CreateStringByColor(Quanity + "", ColorType.Yellow) + "\nChọn đồng đội muốn dịch chuyển tới :";

                        if (player.RefObject.TeamID != -1)
                        {
                            List<KPlayer> TotalMember = player.RefObject.Teammates;

                            foreach (KPlayer Member in TotalMember)
                            {
                                GameMap gameMap = GameManager.MapMgr.DictMaps[Member.MapCode];
                                if (Member.RoleID != player.RefObject.RoleID && gameMap.MapCode != 255 && gameMap.MapCode != 256)
                                { 
                                    UnityEngine.Vector2 gridPos = KTGlobal.WorldPositionToGridPosition(gameMap, new UnityEngine.Vector2(Member.PosX, Member.PosY));
                                    Selections.Add(Member.RoleID, string.Format("{0} - <color=green>{1}</color> <color=#0afffb>({2}, {3})", Member.RoleName, gameMap.MapName, (int)gridPos.x, (int)gridPos.y));
                                }
                            }
                        }
                        else
                        {
                            Text += "\n\nBạn không có tổ đội không thể sử dụng vật phẩm này";
                        }

                        Action<TaskCallBack> ActionWork = (x) => DoActionKunFu(player, ItemDbID, x);

                        _NpcDialog.OnSelect = ActionWork;

                        _NpcDialog.Selections = Selections;

                        _NpcDialog.Text = Text;
                        _NpcDialog.ShowDialog(Good, player.RefObject);
                    }
                }
            }
        }

        private static void DoActionKunFu(Lua_Player player, int itemDbID, TaskCallBack x)
        {
            // Lấy ra kim tê
            GoodsData Good = Global.GetGoodsByDbID(player.RefObject, itemDbID);

            if (Good != null)
            {
                if (ItemManager.IsQianKunFu(Good.GoodsID))
                {
                    int Quanity = 0;

                    if (Good.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string NumberCanRepair))
                    {
                        Quanity = Int32.Parse(NumberCanRepair);
                    }

                    int RoleID = x.SelectID;

                    // tỉma người chơi
                    KPlayer client = GameManager.ClientMgr.FindClient(RoleID);
                    if (client != null)
                    {
                        if (FactionBattleManager.IsInFactionBattle(client) || BaiHuTang.IsInBaiHuTang(client) || Battel_SonJin_Manager.IsInBattle(client) || TeamBattle.IsInTeamBattleMap(client) || EmperorTomb.IsInsideEmperorTombMap(client) || client.CopyMapID > 0)
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Người chơi đang ở trong bản đồ sự kiện không thể dịch chuyển tới");
                            //Đóng khung
                            KT_TCPHandler.CloseDialog(player.RefObject);
                            KT_TCPHandler.CloseDialog(client);
                        }
                        else if (client.WaitingForChangeMap || client.MapCode == 255)
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Người chơi đã di chuyển đến map khác");
                            //Đóng khung
                            KT_TCPHandler.CloseDialog(player.RefObject);
                            KT_TCPHandler.CloseDialog(client);
                        }
                        else
                        {
                            //Đóng khung
                            KT_TCPHandler.CloseDialog(client);

                            Quanity = Quanity - 1;

                            if (Quanity <= 0)
                            {
                                ItemManager.AbadonItem(itemDbID, player.RefObject, false, "KIMTEUSING");
                            }

                            //Updat lại số lượt sử dụng càn khôn phù
                            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                            TotalUpdate.Add(UPDATEITEM.ROLEID, player.RefObject.RoleID);
                            TotalUpdate.Add(UPDATEITEM.ITEMDBID, Good.Id);

                            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();

                            OtherParams.Add(ItemPramenter.Pram_1, Quanity + "");

                            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                            ItemManager.UpdateItemPrammenter(TotalUpdate, Good, player.RefObject);
                               
                            GameManager.ClientMgr.NotifyChangeMap(TCPManager.getInstance().MySocketListener, TCPOutPacketPool.getInstance(), player.RefObject, client.MapCode, client.PosX, client.PosY, 0, false);
                        }
                    }
                    else
                    {
                        PlayerManager.ShowNotification(player.RefObject, "Người chơi này đã offline bạn không thể dịch chuyển tới");
                    }
                }
            }
        }

        private static void DoActionSelect(Lua_Player player, int itemDbID, TaskCallBack x)
        {
            // Lấy ra kim tê
            GoodsData Good = Global.GetGoodsByDbID(player.RefObject, itemDbID);

            if (Good != null)
            {
                if (ItemManager.KD_ISJINXI(Good.GoodsID))
                {
                    int Quanity = 0;

                    if (Good.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string NumberCanRepair))
                    {
                        Quanity = Int32.Parse(NumberCanRepair);
                    }
                    // vật phẩm cần sửa độ bền
                    GoodsData ItemRepair = Global.GetGoodsByDbID(player.RefObject, x.SelectID);
                    if (ItemRepair != null)
                    {
                        var oldStrong = ItemRepair.Strong;
                        if (ItemRepair.Strong == 100)
                        {
                            SendItemMsg("Vật phẩm đã đạt độ bền tốn đa không cần sửa chữa!", Good, player.RefObject);
                            return;
                        }

                        int Dulation = ItemRepair.Strong;

                        int DualtionLoss = 100 - Dulation;

                        if (oldStrong <= 0)
                        {
                            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from DoActionSelect() in KTLuaLib_Player.cs");
                            player.RefObject.GetPlayEquipBody().ClearAllEffecEquipBody();
                        }

                        // Nếu kim tê còn giá trị
                        if (Quanity > DualtionLoss)
                        {
                            ItemRepair.Strong = 100;

                            var TotalUpdate = new Dictionary<UPDATEITEM, object>
                            {
                                { UPDATEITEM.ROLEID, player.RefObject.RoleID },
                                { UPDATEITEM.ITEMDBID, ItemRepair.Id },
                                { UPDATEITEM.STRONG, ItemRepair.Strong }
                            };

                            ItemManager.UpdateItemPrammenter(TotalUpdate, ItemRepair, player.RefObject);

                            Quanity = Quanity - DualtionLoss;
                            //Update lại giá trị cho kim tê
                            TotalUpdate = new Dictionary<UPDATEITEM, object>
                            {
                                { UPDATEITEM.ROLEID, player.RefObject.RoleID },
                                { UPDATEITEM.ITEMDBID, Good.Id }
                            };

                            var OtherParams = new Dictionary<ItemPramenter, string>
                            {
                                { ItemPramenter.Pram_1, Quanity + "" }
                            };

                            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                            ItemManager.UpdateItemPrammenter(TotalUpdate, Good, player.RefObject);

                            SendItemMsg("Sửa trang bị thành công !", Good, player.RefObject);
                        }
                        else
                        {
                            ItemRepair.Strong = DualtionLoss - Quanity;

                            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                            TotalUpdate.Add(UPDATEITEM.ROLEID, player.RefObject.RoleID);
                            TotalUpdate.Add(UPDATEITEM.ITEMDBID, ItemRepair.Id);
                            TotalUpdate.Add(UPDATEITEM.STRONG, ItemRepair.Strong);

                            ItemManager.UpdateItemPrammenter(TotalUpdate, ItemRepair, player.RefObject);

                            ItemManager.AbadonItem(itemDbID, player.RefObject, false, "KIMTEUSING");

                            SendItemMsg("Sửa trang bị thành công,Kim tê đã biến mất", Good, player.RefObject);
                        }

                        if (oldStrong <= 0) { // sửa từ đồ hỏng {
                            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from DoActionSelect() in KTLuaLib_Player.cs");
                            player.RefObject.GetPlayEquipBody().AttackAllEquipBody();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Thực hiện tuyên chiến
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        public static void NpcGuildWarBattle(Lua_NPC npc, Lua_Player player, int option)
        {

            if (player == null || player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, Player is NULL."));
                return;
            }

            if (npc == null || npc.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, NPC is NULL."));
                return;
            }

            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.RefObject.CurrentPos.X, (int)player.RefObject.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.RefObject.CurrentPos.X, (int)npc.RefObject.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);

            if (Dis > 100)
            {
                PlayerManager.ShowNotification(player.RefObject, "Khoảng cách quá xa");
                return;
            }
            else
            {

                if(option==1)//tuyên chiến
                {
                    if (GameManager.IsKuaFuServer)
                    {
                        GuidWarManager.getInstance().JoinKuaFuGuildWar(npc.RefObject, player.RefObject);
                    }
                    else
                    {
                        GuidWarManager.getInstance().NpcClick(npc.RefObject, player.RefObject);
                    }
                }    
                else if(option == 2) // thiết lập thành chính
                {
                    GuidWarManager.getInstance().SetupMainCity(npc.RefObject, player.RefObject);
                }
                else if(option == 3)
                {
                    GuidWarManager.getInstance().UpgradeGuildOfficeRank(npc.RefObject, player.RefObject);
                }
                else if (option == 4)
                {
                    GuidWarManager.getInstance().ReceiveMunitionGuildWar(npc.RefObject, player.RefObject);
                }
                else if (option == 5)
                {
                    GuidWarManager.getInstance().LeaveKuaFuGuildWar(player.RefObject);
                }
            }



        }

        /// <summary>
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <param name="Camp"></param>
        /// <param name="BattleID"></param>
        public static void JoinSongJinBattle(Lua_NPC npc, Lua_Player player, string Camp, int BattleID)
        {
            if (player == null || player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, Player is NULL."));
                return;
            }

            if (npc == null || npc.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, NPC is NULL."));
                return;
            }

            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.RefObject.CurrentPos.X, (int)player.RefObject.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.RefObject.CurrentPos.X, (int)npc.RefObject.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);

            if (Dis > 100)
            {
                PlayerManager.ShowNotification(player.RefObject, "Khoảng cách quá xa");
                return;
            }

            int CampInt = 10;

            if (Camp == "KIM")
            {
                CampInt = 20;
            }

            int Register = Battel_SonJin_Manager.BattleRegister(player.RefObject, CampInt, BattleID);

            var ErrorDict = new Dictionary<int, string>()
            {
                {
                    -100, "Số lượng báo danh vào phe Kim đang quá đông,Xin hãy quay lại sau hoặc báo danh sang bên Tống"
                },
                {
                    -200, "Số lượng báo danh vào phe Tống đang quá đông,Xin hãy quay lại sau hoặc báo danh sang bên Kim"
                },
                { -2, "Người đã đăng ký rồi không thể đăng ký lại" },
                { -3, "Chiến trường hiện chưa mở" },
                { -4, "Không phải thời gian báo danh vui lòng quay lại sau" },
                { -1000, "Cấp độ của bạn không phù hợp với chiến trường này" },
                { -1001, $"Hoạt động này yêu cầu trang bị phi phong cấp {KTLuaLib_System.GetMantleNameFromLevel(ServerConfig.Instance.MantleLevelForSongJin)}"},
                { -5, "Bạn không thể đổi phe khi chiến trường đang diễn ra" },
                { -10, "Bang của bạn không thể tham gia chiến trường phe Tống" },
                { -20, "Bang của bạn không thể tham gia chiến trường phe Kim" },
                { -30, "Số lượng người đăng ký tham gia chiến trường đã đủ" },
            };
            if (ErrorDict.TryGetValue(Register, out var errorDescription))
            {
                SendMSG(errorDescription, player.RefObject, npc.RefObject);
            }
        }

        /// <summary>
        /// Tham gia thi đấu môn phái
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <param name="Camp"></param>
        /// <param name="BattleID"></param>
        public static void JoinFactionBattle(Lua_NPC npc, Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, Player is NULL."));
                return;
            }

            if (npc == null || npc.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, NPC is NULL."));
                return;
            }

            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.RefObject.CurrentPos.X, (int)player.RefObject.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.RefObject.CurrentPos.X, (int)npc.RefObject.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);

            if (Dis > 100)
            {
                PlayerManager.ShowNotification(player.RefObject, "Khoảng cách quá xa");
                return;
            }

            int CampInt = 10;

            int Register = FactionBattleManager.FactionBattleJoin(player.RefObject);

            if (Register == -1)
            {
                SendMSG("Hiện tại không có trận thi đấu môn phái nào", player.RefObject, npc.RefObject);
            }
            else if (Register == -2)
            {
                SendMSG("Thi đấu môn phái đã bắt đầu lúc 20:00h，hiện tại không thể tiếp nhận đăng ký", player.RefObject, npc.RefObject);
            }
            else if (Register == -3)
            {
                SendMSG("Bạn không phải là đệ tử của môn phái này，không thể tham gia thi đấu!", player.RefObject, npc.RefObject);
            }
            else if (Register == -4)
            {
                SendMSG("Cấp độ của bạn không đủ không thể tham gia thi đấu môn phái", player.RefObject, npc.RefObject);
            }
            else if (Register == -5)
            {
                SendMSG("Số người đăng ký đã đến mức giới hạn 400 người，không thể tiếp nhận đăng ký，mời bạn tham gia các hoạt động khác tại đây", player.RefObject, npc.RefObject);
            }
            else if (Register == -6)
            {
                SendMSG("Ngươi phải vào 1 môn phái", player.RefObject, npc.RefObject);
            }
            else if (Register == -40)
            {
                SendMSG("Vui lòng giải tán tổ đội trước khi giam gia", player.RefObject, npc.RefObject);
            }
        }

        public static void PortableGoods(NPC npc, KPlayer player, int site = (int)SiteItemType.bag)
        {
            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.CurrentPos.X, (int)player.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.CurrentPos.X, (int)npc.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);

            if (Dis > 100)
            {
                PlayerManager.ShowNotification(player, "Khoảng cách quá xa");
                return;
            }

            int roleID = player.RoleID;
            

            byte[] sendBytesCmd = new UTF8Encoding().GetBytes(string.Format("{0}:{1}", player.RoleID, site));
            byte[] bytesData = null;

            Thread _OpenThread = new Thread(() =>
            {
                TCPProcessCmdResults result = Global.ReadDataFromDb((int)TCPGameServerCmds.CMD_GETGOODSLISTBYSITE, sendBytesCmd, sendBytesCmd.Length, out bytesData, player.ServerId);
                //{
                //    LogManager.WriteLog(LogTypes.Error, string.Format("Không kết nối được với DBServer, CMD={0}", (int)TCPGameServerCmds.CMD_SPR_COMPTASK));

                //    KT_TCPHandler.CloseDialog(client);

                //    PlayerManager.ShowNotification(client, "Không kết nối được với DBServer");

                //    return;
                //}

                if (TCPProcessCmdResults.RESULT_FAILED != result)
                {
                    //Get đồ từ DB ra trả về client
                    List<GoodsData> goodsDataList = DataHelper.BytesToObject<List<GoodsData>>(bytesData, 6, bytesData.Length - 6);

                    player.PortableGoodsDataList = goodsDataList;

                    GoodsListWithSite dataSend = new GoodsListWithSite();
                    dataSend.ListGoodsData = goodsDataList;
                    dataSend.Site = site;
                    player.SendPacket<GoodsListWithSite>((int)TCPGameServerCmds.CMD_GETGOODSLISTBYSITE, dataSend);
                }
            });
            _OpenThread.IsBackground = false;
            _OpenThread.Start();

            //TCPProcessCmdResults result = Global.TransferRequestToDBServer(Global._TCPManager, player.RefObject.ClientSocket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket, client.ServerId);
        }

        public static void PortableGoods(Lua_NPC npc, Lua_Player player, int site)
        {
            PortableGoods(npc.RefObject, player.RefObject, site);
        }

        /// <summary>
        /// Kick người chơi tương ứng ra khỏi Game
        /// </summary>
        /// <param name="player"></param>
        public static void KickOut(Lua_Player player)
        {
            PlayerManager.KickOut(player.RefObject);
        }

        /// <summary>
        /// Cường hóa trang bị
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        /// <param name="enhanceLevel"></param>
        public static void EquipEnhance(Lua_Player player, Lua_Item item, int enhanceLevel, int binding)
        {
            ItemManager.SetEquipForgeLevel(item.RefObject, player.RefObject, enhanceLevel, binding);
        }

        /// <summary>
        /// nap tien co phan - tang tai san ca nhan trong bang de mua quan an
        /// </summary>
        /// <param name="player"></param>
        public static int InvestInGuild(Lua_Player player, int money)
        {
            /// Nếu không có bang hội
            if (player.RefObject.GuildID <= 0)
            {
                return -1;
            }
            
            /// Nếu không đủ tiền
            if (money > KTLuaLib_Player.CheckMoney(player, (int)MoneyType.Bac))
            {
                return -2;
            }

            if (KTLuaLib_Player.SubMoney(player, money, (int)MoneyType.Bac, "InvestInGuild"))
            {
                if (KTLuaLib_Player.AddMoney(player, money, (int)MoneyType.GuildMoney, "InvestInGuild"))
                {
                    return 1;
                }
                else
                {
                    return -4;
                }
            }
            else
                return -3;
            
        }

        /// <summary>
        /// Giải tán bang hội
        /// </summary>
        /// <param name="player"></param>
        public static void RetainGuild(Lua_Player player)
        {
            /// Nếu không có bang hội
            if (player.RefObject.GuildID <= 0)
            {
                return;
            }
            /// Nếu không phải bang chủ
            else if (player.RefObject.GuildRank != (int)GuildRank.Master)
            {
                return;
            }

            /// Hiện bảng xác nhận
            PlayerManager.ShowMessageBox(player.RefObject, "Xác nhận", "Xác nhận giải tán bang hội hiện tại không?", () =>
            {
                KT_TCPHandler.DesotryGuild(player.RefObject.RoleID, player.RefObject.GuildID);
            }, true);
        }

        /// <summary>
        /// Giải tán gia tộc
        /// </summary>
        /// <param name="player"></param>
        public static void RetainFamily(Lua_Player player)
        {
            /// Nếu không có tộc
            if (player.RefObject.FamilyID <= 0)
            {
                return;
            }
            /// Nếu không phải tộc trưởng
            else if (player.RefObject.FamilyRank != (int)FamilyRank.Master)
            {
                return;
            }
            /// Nếu có bang hội
            else if (player.RefObject.GuildID > 0)
            {
                return;
            }

            /// Hiện bảng xác nhận
            PlayerManager.ShowMessageBox(player.RefObject, "Xác nhận", "Xác nhận giải tán gia tộc hiện tại không?", () =>
            {
                KT_TCPHandler.DestroyFamily(player.RefObject.RoleID, player.RefObject.FamilyID);
            }, true);
        }

        /// <summary>
        /// Xóa vật phẩm có ID với số lượng tương ứng khỏi túi đồ của người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemID"></param>
        /// <param name="count"></param>
        public static bool RemoveItem(Lua_Player player, int itemID, int count)
        {
            return ItemManager.RemoveItemFromBag(player.RefObject, itemID, count, -1, "LUASCRIPTCALL");
        }

        public static bool RemoveItem(Lua_Player player, int itemID, int binding, int count) 
        {
            return ItemManager.RemoveItemFromBag(player.RefObject, itemID, count, binding, "LUASCRIPT");
        }

        /// <summary>
        /// Sử dụng Tu Luyện Châu
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hour10"></param>
        /// <returns></returns>
        public static bool UseXiuLianZhu(Lua_Player player, int hour10)
        {
            return ItemXiuLianZhuManager.UseXiuLianZhu(player.RefObject, hour10);
        }

        /// <summary>
        /// Thiết lập NPC dã luyện đại sư được Click lần trước
        /// </summary>
        /// <param name="player"></param>
        /// <param name="npc"></param>
        public static void SetLastEquipMasterNPC(Lua_Player player, Lua_NPC npc)
        {
            player.RefObject.LastEquipMasterNPC = npc.RefObject;
        }

        /// <summary>
        /// Thiết lập cấp độ và kinh nghiệm tu luyện mật tịch tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="level"></param>
        /// <param name="exp"></param>
        public static bool SetBookLevelAndExp(Lua_Player player, int level, int exp)
        {
            if (player.RefObject.GoodsDataList != null)
            {
                GoodsData findbook = null;
                lock (player.RefObject.GoodsDataList)
                {
                    findbook = player.RefObject.GoodsDataList.Where(x => x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_BOOK).FirstOrDefault();
                }

                level = Math.Min(100, level);

                if (findbook != null)
                {
                    ItemData BookData = ItemManager.GetItemTemplate(findbook.GoodsID);
                    if (BookData != null)
                    {
                        Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                        TotalUpdate.Add(UPDATEITEM.ROLEID, player.RefObject.RoleID);
                        TotalUpdate.Add(UPDATEITEM.ITEMDBID, findbook.Id);

                        Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                        OtherParams.Add(ItemPramenter.Pram_1, level + "");
                        OtherParams.Add(ItemPramenter.Pram_2, exp + "");
                        OtherParams.Add(ItemPramenter.Pram_3, "0");
                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                        int nExpAdded = ItemManager._TotalExpBook.Where(x => x.Level <= level && x.BookLevel == BookData.Level).Sum(x => x.Exp);

                        if (ItemManager.UpdateItemPrammenter(TotalUpdate, findbook, player.RefObject, "Lua_SetBookLevelAndExp", true))
                        {
                            /// Thêm kinh nghiệm cho kỹ năng tương ứng
                            if (KSkill.GetSkillData(BookData.BookProperty.SkillID1) != null)
                            {
                                player.RefObject.Skills.AddSkillExp(BookData.BookProperty.SkillID1, nExpAdded);
                            }
                            if (KSkill.GetSkillData(BookData.BookProperty.SkillID2) != null)
                            {
                                player.RefObject.Skills.AddSkillExp(BookData.BookProperty.SkillID2, nExpAdded);
                            }
                            if (KSkill.GetSkillData(BookData.BookProperty.SkillID3) != null)
                            {
                                player.RefObject.Skills.AddSkillExp(BookData.BookProperty.SkillID3, nExpAdded);
                            }
                            if (KSkill.GetSkillData(BookData.BookProperty.SkillID4) != null)
                            {
                                player.RefObject.Skills.AddSkillExp(BookData.BookProperty.SkillID4, nExpAdded);
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Thực hiện đổi tên
        /// </summary>
        /// <param name="player"></param>
        /// <param name="newName"></param>
        /// <param name="onSuccess"></param>
        public static void ProcessChangeName(Lua_Player player, string newName, Closure onSuccess)
        {
            NameManager.Instance().ProcessChangeName(player.RefObject, newName, (_oldName, _newName) =>
            {
                object[] parameters = new object[]
                {
                    _oldName, _newName
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("Player.ProcessChangeName", onSuccess, parameters, null);
            });
        }

        /// <summary>
        /// Mở bảng nhập Captcha
        /// </summary>
        /// <param name="player"></param>
        /// <param name="onSuccess"></param>
        /// <param name="delayTicks"></param>
        /// <returns></returns>
        public static bool OpenCaptcha(Lua_Player player, Closure onSuccess, int delayTicks = 30000)
        {
            return PlayerManager.OpenCaptcha(player.RefObject, (isCorrect) => {
                object[] parameters = new object[]
                {
                    isCorrect
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("Player.OpenCaptcha", onSuccess, parameters, null);
            }, delayTicks);
        }

        /// <summary>
        /// Thêm kinh nghiệm tu luyện mật tịch cho người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="nExp"></param>
        /// <returns></returns>
        public static bool AddBookExp(Lua_Player player, int nExp)
        {
            /// Nếu không có vật phẩm
            if (player.RefObject.GoodsDataList == null)
            {
                return false;
            }

            /// Sách tương ứng
            GoodsData book = null;
            /// Thông tin sách tương ứng
            lock (player.RefObject.GoodsDataList)
            {
                book = player.RefObject.GoodsDataList.Where(x => x.Using == (int) KE_EQUIP_POSITION.emEQUIPPOS_BOOK).FirstOrDefault();
            }
            /// Nếu không có sách
            if (book == null)
            {
                return false;
            }

            /// Thực hiện thêm kinh nghiệm mật tịch tương ứng
            MonsterDeadHelper.BookProseccExp(player.RefObject, nExp, false);

            /// Trả về kết quả
            return true;
        }

        /// <summary>
        /// xem nhiệm vụ chính tuyến hiện tại
        /// </summary>
        /// <param name="player"></param>
        /// <param name="taskID"></param>
        /// <returns></returns>
        public static bool IsCompleteTask(Lua_Player player, int taskID)
        {
            if (player.RefObject.MainTaskID >= taskID)
            {
                return true;
            }
            return false;
        }

        public static string GetListTaskItemToBeRemoved(Lua_Player player)
        {
            var r = "";
            foreach (var oldTask in player.RefObject.OldTasks)
            {
                var taskData = TaskManager.getInstance().FindTaskById(oldTask.TaskID);
                if (taskData == null) 
                    continue;

                if (taskData.TargetType != (int)TaskTypes.MonsterSomething &&
                    taskData.TargetType != (int)TaskTypes.Collect)
                    continue;
                
                if (!int.TryParse(taskData.PropsName, out var taskItemId)) 
                    continue;

                var itemData = ItemManager.GetItemTemplate(taskItemId);
                if (itemData == null)
                    continue;

                var taskItemInBags = CountItemInBag(player, taskItemId);
                if (taskItemInBags == 0) 
                    continue;

                r += $"- {taskItemInBags} {itemData.Name}\n";
            }

            if (string.IsNullOrEmpty(r))
            {
                return "Bằng hữu không có vật phẩm cần tiêu hủy.";
            }

            r = $"Các vật phẩm sẽ được tiêu hủy:\n{r}";
            return r;
        }

        public static void RemoveCompletedTaskItem(Lua_Player player)
        {
            foreach (var oldTask in player.RefObject.OldTasks)
            {
                var taskData = TaskManager.getInstance().FindTaskById(oldTask.TaskID);
                if (taskData == null)
                    continue;

                if (taskData.TargetType == (int)TaskTypes.MonsterSomething || taskData.TargetType == (int)TaskTypes.Collect)
                {
                    if (!int.TryParse(taskData.PropsName, out var taskItemId))
                        continue;

                    var itemData = ItemManager.GetItemTemplate(taskItemId);
                    if (itemData == null)
                        continue;

                    var taskItemInBags = CountItemInBag(player, taskItemId);
                    if (taskItemInBags == 0)
                        continue;

                    RemoveItem(player, taskItemId, taskItemInBags);
                    player.AddNotification($"Đã tiêu hủy {KTGlobal.CreateStringByColor($"{taskItemInBags} {itemData.Name}", ColorType.VanillaIce)}!");

                }

            }
        }
        /// <summary>
        /// Cắm cờ gia tộc từ lua
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool PutFamilyFlag(Lua_Player player, int mapCode, int range, int duration, int timePerTick, int expPerTick, int dbItemId)
        {
            return KTGlobal.PutFamilyFlag(player.RefObject, mapCode, range, duration, timePerTick, expPerTick, dbItemId);
        }

        public static int GetValueOfMonthRecord(Lua_Player player, int key) {
            return player.RefObject.GetValueOfMonthRecore(key);
        }

        public static void SetValueOfMonthRecord(Lua_Player player, int Key, int Value)
        {
            player.RefObject.SetValueOfMonthRecore(Key, Value);
        }
        public static bool SetRoleTitle(Lua_Player player, int title) 
        {
            return player.RefObject.AddRoleTitle(title);
        }
        public static bool HadLotteryYesterday()
        {
            return LotteryManager.HadLotteryYesterday();
        }
        public static int NumberWinLottery()
        {
            return LotteryManager.NumberWinLotteryYesterday();
        }
        //public static void AddGiftLotteryToPlayer(Lua_Player player, int money)
        //{
        //    LotteryManager.AddGiftToPlayer(player.RefObject, money);
        //}
        public static int GetNumberPlayerWin(int number)
        {
            return LotteryManager.GetNumberPlayerWin(number);
        }
        
        public static int GetTotalCharge(Lua_Player player, string startTime, string endTime)
        {
            return (int)KTGlobal.GetPlayerTotalSumRechargeByTime(player.RefObject, startTime, endTime);
        }

        public static void LevelUpEquipPet(Lua_Player player)
        {
            // kiểm tra xem có vật phẩm nào có đủ 3 món ko
            var dialog = new KNPCDialog();
            var listItem = player.RefObject.GoodsDataList.Where(x => x.IsItemPet()).ToList();
            List<int> itemsID = new List<int>();
            foreach (var item in listItem)
            {
                var items = listItem.Where(x => x.GoodsID == item.GoodsID).ToList();
                if (items.Count >= 3 && !itemsID.Contains(item.GoodsID))
                {
                    itemsID.Add(item.GoodsID);
                }
            }
            if (itemsID.Count > 0)
            {
                foreach (var item in itemsID)
                {
                    //var itemInfo = listItem.Where(x => x.GoodsID == item).FirstOrDefault();
                    var itemTemplate = ItemManager.GetItemTemplate(item);
                    if (itemTemplate.Genre == 24)
                    {
                        continue;
                    }    
                    // lấy ra vật phẩm khi nâng cấp
                    var itemLevelUP = ItemManager.TotalItem.Where(x => x.DetailType == itemTemplate.DetailType && x.Level == itemTemplate.Level + 1 && x.Level < 9).FirstOrDefault();
                    if (itemLevelUP != null)
                    {
                        dialog.Items.Add(new DialogItemSelectionInfo
                        {
                            ItemID = itemLevelUP.ItemID,
                            Quantity = 1,
                            Binding = 1,
                        });
                    }
                }
                if (dialog.Items.Count > 0)
                {
                    dialog.Text = "Bạn có thể ghép được những trang bị sau";
                }  
                else
                {
                    dialog.Text = "Bạn không có đủ 3 món nào để ghép";
                }    
            }
            else
            {
                dialog.Text = "Bạn không có đủ 3 món nào để ghép";
            }
            dialog.ItemSelectable = true;
            dialog.OnItemSelect = (data) =>
            {
                    
                // xóa vật phẩm 
                var itemInfo = ItemManager.TotalItem.Where(x => x.ItemID == data.ItemID).FirstOrDefault();
                var itemDelete = ItemManager.TotalItem.Where(x => x.DetailType == itemInfo.DetailType && x.Level == itemInfo.Level - 1).FirstOrDefault();
                if (!ItemManager.RemoveItemFromBag(player.RefObject, itemDelete.ItemID, 3, -1, "Ghép trang bị"))
                {
                    PlayerManager.ShowNotification(player.RefObject, "Ghép trang bị thất bại");
                    return;
                }    
                else
                {
                    var random = KTGlobal.GetRandomNumber(1, 100);
                    if (random >= 50)
                    {
                        //add vật phẩm mới vào
                        if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, data.ItemID, data.Quantity, 0, "LUAITEMADD", true, data.Binding, false, Global.ConstGoodsEndTime))
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Ghép thành công! Hãy trang bị cho đồng hành ngay nào.");
                            return;
                        }
                        else
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Có lỗi khí ghép");
                            LogManager.WriteLog(LogTypes.Item, $"Player:{player.RefObject.RoleName} ghép trang bị {itemInfo.Name} thất bại");
                            return;
                        }
                    }
                    else
                    {
                        PlayerManager.ShowNotification(player.RefObject, "Ghép thất bại! Chúc bạn may mắn lần sau.");
                        return;
                    }    
                }    
            };
            dialog.ShowDialog(player.RefObject);
        }

        public static void JoinLeagueBattle(Lua_NPC npc, Lua_Player player)
        {
            if (player == null || player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, Player is NULL."));
                return;
            }

            if (npc == null || npc.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on NPCDialog:Show, NPC is NULL."));
                return;
            }

            UnityEngine.Vector2 _SelfPos = new UnityEngine.Vector2((int)player.RefObject.CurrentPos.X, (int)player.RefObject.CurrentPos.Y);

            UnityEngine.Vector2 NpcPos = new UnityEngine.Vector2((int)npc.RefObject.CurrentPos.X, (int)npc.RefObject.CurrentPos.Y);

            float Dis = UnityEngine.Vector2.Distance(_SelfPos, NpcPos);

            if (Dis > 100)
            {
                LogManager.WriteLog(LogTypes.LeagueBattle, $"Player: {player.RefObject.RoleName} đứng xa NPC");
                PlayerManager.ShowNotification(player.RefObject, "Khoảng cách quá xa");
                return;
            }

            int Register = LeagueBattleManager.LeagueBattleJoin(player.RefObject);

            if (Register == -1)
            {
                SendMSG("Hiện tại không có trận thi đấu nào", player.RefObject, npc.RefObject);
            }
            else if (Register == -2)
            {
                SendMSG("Giải đấu đang diễn ra, không thể đăng ký", player.RefObject, npc.RefObject);
            }
            else if (Register == -3)
            {
                SendMSG("Bạn chưa đủ level để tham gia giải đấu", player.RefObject, npc.RefObject);
            }
            else if (Register == -4)
            {
                SendMSG("Bạn chưa đủ tài phú để tham gia giải đấu", player.RefObject, npc.RefObject);
            }
            else if (Register == -5)
            {
                SendMSG("Bạn phải mang phi phong trước khi tham gia giải đấu", player.RefObject, npc.RefObject);
            }
            else if (Register == -6)
            {
                SendMSG("Phi phong mặc trên người phải dạt cấp Kinh Thế trở lên", player.RefObject, npc.RefObject);
            }
            else if (Register == -7)
            {
                SendMSG("Giải tán tổ đội", player.RefObject, npc.RefObject);
            }
            else if (Register == -8)
            {
                SendMSG("Giải đấu đã đạt số lượng tối đa thành viên tham gia", player.RefObject, npc.RefObject);
            }
            else if (Register == -9)
            {
                SendMSG("Bạn phải tham gia môn phái trước khi tham gia giải đấu", player.RefObject, npc.RefObject);
            }
        }

        public static int StateLeagueBattle()
        {
            return LeagueBattleManager.StateLeagueBattle();
        }

        public static void GetAwardBetting(Lua_Player player)
        {
            LeagueBattleManager.GetAwardBetting(player.RefObject);
        }
        
        public static void GetAwardParticipation(Lua_Player player)
        {
            LeagueBattleManager.GetAwardParticipation(player.RefObject);
        }
 
        public static void GetAwardTop(Lua_Player player)
        {
            LeagueBattleManager.GetAwardTop(player.RefObject);
        }

        public static bool MinusExperience(Lua_Player player, int experience)
        {
            return Global.MinusExperience(player.RefObject, experience);
        }

        public static void OpenUpdateSkillForArtifactItem(Lua_Player player)
        {
            // yêu cầu client mở giao diện nâng cấp trang bị bahcj ngân hoàng kim
        }

        public static void ResetBaiscProps(Lua_Player player)
        {
            ChanNguyenManager.ResetBaiscProps(player.RefObject);
        }
        public static void CheckHistoryItem(Lua_Player player)
        {
            var listString = player.RefObject.GetHistoryItemUsed(player.RefObject);

            KNPCDialog dialog = new KNPCDialog();
            dialog.Text = "Danh sách lịch sử dùng các vật phẩm:";

            foreach (var item in listString)
            {
                dialog.Items.Add(new DialogItemSelectionInfo
                {
                    ItemID = item.Key,
                    Binding = 0,
                    Quantity = 1
                });
            }
            dialog.ItemSelectable = true;
            dialog.OnItemSelect = (data) =>
            {
                var itemInfo = listString.Where(x => x.Key == data.ItemID).FirstOrDefault();

                var number = player.RefObject.GetValueOfForeverRecore(itemInfo.Value.RecordID);

                if (number < 0)
                    number = 0;
               var text= $"Vật phẩm {itemInfo.Value.Name} đã sử dụng {number} lần";
                PlayerManager.ShowMessageBox(player.RefObject, "Thông tin", text);
            };
            dialog.ShowDialog(player.RefObject);
        }
        public static void AddScoreMonthActivity(Lua_Player player, int Score)
        {
            player.RefObject._MonthDataRecore.ScoreMonth += Score;
        }
        public static bool UnlockBattlePassElite(Lua_Player player, int bundleID)
        {
            return BattlePassManager.BuyElitePass(player.RefObject, bundleID);
        }

        public static void AddBPExp(Lua_Player player, int amount)
        {
            BattlePassManager.AddExp(player.RefObject, amount, BattlePassManager.GetBattlePassData(player.RefObject));
            PlayerManager.ShowNotification(player.RefObject, $"Thêm {amount} kinh nghiệm Kiếm Vương Lệnh thành công!");
        }

        public static bool AddEquipTanLang(Lua_Player player)
        {
            var listItem = new List<int>() { 11389, 11392, 11399, 11402, 11409, 11412, 11419, 11422, 11429, 11432, 11443, 11449, 11450, 11451, 11453, 12781, 12782, 12783, 12784, 12785, 12787, 11388, 11391, 11398, 11401, 11408, 11411, 11418, 11421, 11428, 11431, 11434, 11438, 11440, 11447, 11448, 11387, 11397, 11407, 11417, 11427, 11435, 11444, 11386, 11393, 11396, 11403, 11406, 11413, 11416, 11423, 11426, 11433, 11439, 11441, 11442, 11446, 11385, 11395, 11405, 11415, 11425, 11437, 11452, 11384, 11390, 11394, 11400, 11404, 11410, 11414, 11420, 11424, 11430, 11436, 11445, 12776, 12777, 12778, 12779, 12780, 12786 };
            // nếu chưa gia nhập phái thì cook
            if (player.RefObject.m_cPlayerFaction.GetFactionId() == 0)
            {
                return false;
            }
            else
            {
                var seriesPlayer = player.RefObject.m_Series;
                foreach (var item in listItem)
                {
                    var itemData = ItemManager._TotalGameItem.Where(x => x.Value.ItemID == item).FirstOrDefault();
                    if (itemData.Value.Series == (int)seriesPlayer)
                    {
                        if (!KTGlobal.IsHaveSpace(1, player.RefObject))
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Túi đồ không đủ chỗ");
                            return false;
                        }

                        string TimeUsing = Global.ConstGoodsEndTime;
                        if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player.RefObject, item, 1, 0, "LUAITEMADD", true, 0, false, TimeUsing, "", (int)seriesPlayer, "", 16, 0, true))
                        {
                            PlayerManager.ShowNotification(player.RefObject, "Có lỗi khi add vật phẩm");

                            return false;
                        }
                    }
                }
                PlayerManager.ShowNotification(player.RefObject, "Bạn đã nhận vũ khí thành công");
                return true;
            }
            return false;
        }

    }
}
