using GameServer.Core.Executor;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using ProtoBuf;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameServer.KiemThe.Core.Shop
{
    /// <summary>
    /// Thực thể quản lý điểm uy danh
    /// </summary>
    public class ShopSalePrestige
    {
        public static void ClickNpcBuyRequest(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();

            string Text = "";

            long Now = TimeUtil.NOW();

            if (Now - client.LastRequestKTCoin > 10000)
            {
                //Lấy ra rank hiện tại của người
                int Rank = ShopSalePrestige.GetRankOfPlayerFromDatabase(client);

                int PERCENT_GROUP1 = 0;
                int PERCENT_GROUP2 = 0;

                string Title = "";
                if (Rank == -1)
                {
                    Title = "Hệ thống bảng xếp hạng đang cập nhật\nVui lòng quay lại sau";
                }
                else if (Rank == -100)
                {
                    Title = "Bạn không nằm trong TOP 100 người uy danh,Bạn không thể mua tinh hoạt lực ưu đãi";
                }
                else
                {
                    if (Rank >= 0 && Rank < 30)
                    {
                        PERCENT_GROUP1 = 80;
                        PERCENT_GROUP2 = 30;
                        Title = "<b><color=#00ff2a>Bạn đang xếp hạng thứ <color=red>" + Rank + "</color>\nBạn được mua Tinh Hoạt lực giảm giá " + PERCENT_GROUP1 + "% tối đa <color=red>5</color> bình\nThỏi vàng giảm giá " + PERCENT_GROUP2 + "% tối đa <color=red>1</color> thỏi</color></b>";
                    }
                    else if (Rank >= 30 && Rank < 60)
                    {
                        PERCENT_GROUP1 = 40;
                        PERCENT_GROUP2 = 20;
                        Title = "<b><color=#00ff2a>Bạn đang xếp hạng thứ <color=red>" + Rank + "</color>\nBạn được mua Tinh Hoạt lực giảm giá " + PERCENT_GROUP1 + "% tối đa <color=red>5</color> bình\nThỏi vàng giảm giá " + PERCENT_GROUP2 + "% tối đa <color=red>1</color> thỏi</color></b>";
                    }
                    else if (Rank >= 60 && Rank < 100)
                    {
                        PERCENT_GROUP1 = 20;
                        PERCENT_GROUP2 = 10;
                        Title = "<b><color=#00ff2a>Bạn đang xếp hạng thứ <color=red>" + Rank + "</color>\nBạn được mua Tinh Hoạt lực giảm giá " + PERCENT_GROUP1 + "% tối đa <color=red>5</color> bình\nThỏi vàng giảm giá " + PERCENT_GROUP2 + "% tối đa <color=red>1</color> thỏi</color></b>";
                    }
                    else
                    {
                        PERCENT_GROUP1 = 0;
                        PERCENT_GROUP2 = 0;
                        Title = "<b><color=#00ff2a>Bạn không nằm trong TOP 100 người được mua tinh hoạt lực ưu đãi</color></b>";
                    }
                }
                // Set số lần gần đây nhất mới thao tác
                client.LastRequestKTCoin = Now;

                Dictionary<int, string> Selections = new Dictionary<int, string>();

                Text = "Xin chào <b>" + client.RoleName + "</b>" + " bạn hiện có  <b>" + KTGlobal.CreateStringByColor(client.Prestige + " Uy Danh", ColorType.Yellow) + "</b>\n\nTop 100 người chơi có uy danh cao nhất sẽ được mua tinh hoạt lực giảm giá\n\n" + Title;

                if (PERCENT_GROUP1 > 0)
                {
                    int OLDPRINT_1 = 40;

                    int NEWPRINT_1 = (OLDPRINT_1 * (100 - PERCENT_GROUP1)) / 100;

                    Selections.Add((int)AcitvityRecore.BuyDiscount_1, "<b><color=#00ff2a>Mua Tinh Lực (Tiểu)</color></b><b> " + NEWPRINT_1 + " KNB(giảm " + PERCENT_GROUP1 + "%)</b>");
                    Selections.Add((int)AcitvityRecore.BuyDiscount_2, "<b><color=#00ff2a>Mua Hoạt Lực (Tiểu)</color></b><b> " + NEWPRINT_1 + " KNB(giảm " + PERCENT_GROUP1 + "%)</b>");

                    int OLDPRINT_2 = 1000;

                    int NEWPRINT_2 = (OLDPRINT_2 * (100 - PERCENT_GROUP2)) / 100;

                    Selections.Add((int)AcitvityRecore.BuyDiscount_3, "<b><color=#00ff2a>Thỏi vàng</color></b><b> " + NEWPRINT_2 + " KNB(giảm " + PERCENT_GROUP2 + "%)</b>");
                }

                Action<TaskCallBack> ActionWork = (x) => ClickBuyCheck(client, npc, x, Rank);

                _NpcDialog.OnSelect = ActionWork;

                _NpcDialog.Selections = Selections;
            }
            else
            {
                long DIV = Now - client.LastRequestKTCoin;

                float TimeLess = (10000 - DIV) / 1000;

                Text = "Vui lòng quay sau :" + TimeLess + " giây nữa";
            }

            _NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        private static void ClickBuyCheck(KPlayer client, NPC npc, TaskCallBack x, int Rank)
        {
            KT_TCPHandler.CloseDialog(client);

            int PERCENT_GROUP1 = 0;
            int PERCENT_GROUP2 = 0;

            if (Rank >= 0 && Rank < 30)
            {
                PERCENT_GROUP1 = 80;
                PERCENT_GROUP2 = 30;
            }
            else if (Rank >= 30 && Rank < 60)
            {
                PERCENT_GROUP1 = 40;
                PERCENT_GROUP2 = 20;
            }
            else if (Rank >= 60 && Rank < 100)
            {
                PERCENT_GROUP1 = 20;
                PERCENT_GROUP2 = 10;
            }
            else
            {
                PERCENT_GROUP1 = 0;
            }

            if (x.SelectID == (int)AcitvityRecore.BuyDiscount_1 || x.SelectID == (int)AcitvityRecore.BuyDiscount_2)
            {
                int ITEMID = 0;
                int OLDPRINT_1 = 40;
                int NEWPRINT_1 = (OLDPRINT_1 * (100 - PERCENT_GROUP1)) / 100;

                bool CanBuy = false;

                int Recore = 0;

                if (x.SelectID == (int)AcitvityRecore.BuyDiscount_1)
                {
                    Recore = client.GetValueOfDailyRecore((int)AcitvityRecore.BuyDiscount_1);
                    if (Recore < 4)
                    {
                        CanBuy = true;
                    }
                    ITEMID = 348;
                }
                else if (x.SelectID == (int)AcitvityRecore.BuyDiscount_2)
                {
                    Recore = client.GetValueOfDailyRecore((int)AcitvityRecore.BuyDiscount_2);
                    if (Recore < 4)
                    {
                        CanBuy = true;
                    }
                    ITEMID = 351;
                }

                if (!KTGlobal.IsHaveMoney(client, NEWPRINT_1, Entities.MoneyType.Dong))
                {
                    PlayerManager.ShowNotification(client, "Tiền không đủ");
                }
                else
                {
                    if (CanBuy)
                    {
                        client.SetValueOfDailyRecore(x.SelectID, Recore + 1);

                        if (KTGlobal.SubMoney(client, NEWPRINT_1, Entities.MoneyType.Dong, "DISCOUNTSHOP").IsOK)
                        {
                            string TimeUsing = Global.ConstGoodsEndTime;
                            // Tọa vật phẩm đã mua
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, ITEMID, 1, 0, "DISCOUNTSHOP", true, 1, false, TimeUsing, "", -1, "", 0, 1, false))
                            {
                                PlayerManager.ShowNotification(client, "Có lỗi khi thực hiện thêm vật phẩm vào túi đồ vui lòng liên hệ ADM để được giúp đỡ!");

                                LogManager.WriteLog(LogTypes.BuyNpc, "Có lỗi khi thực hiện add vật phẩm :" + client.RoleID + "|" + ITEMID + " x " + 1);
                            }
                        }
                    }
                    else
                    {
                        PlayerManager.ShowNotification(client, "Hôm nay bạn đã hết lượt mua");
                    }
                }
            }
            else if (x.SelectID == (int)AcitvityRecore.BuyDiscount_3)
            {
                int ITEMID = 0;
                int OLDPRINT_1 = 1000;

                int NEWPRINT_1 = (OLDPRINT_1 * (100 - PERCENT_GROUP2)) / 100;

                bool CanBuy = false;

                int Recore = 0;

                if (x.SelectID == (int)AcitvityRecore.BuyDiscount_3)
                {
                    Recore = client.GetValueOfDailyRecore((int)AcitvityRecore.BuyDiscount_3);
                   
                    if (Recore < 0)
                    {
                        CanBuy = true;
                    }
                    ITEMID = 402;
                }
               

                if (!KTGlobal.IsHaveMoney(client, NEWPRINT_1, Entities.MoneyType.Dong))
                {
                    PlayerManager.ShowNotification(client, "Tiền không đủ");
                }
                else
                {
                    if (CanBuy)
                    {
                        client.SetValueOfDailyRecore(x.SelectID, Recore + 1);

                        if (KTGlobal.SubMoney(client, NEWPRINT_1, Entities.MoneyType.Dong, "DISCOUNTSHOP").IsOK)
                        {
                            string TimeUsing = Global.ConstGoodsEndTime;
                            // Tọa vật phẩm đã mua
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, ITEMID, 1, 0, "DISCOUNTSHOP", true, 1, false, TimeUsing, "", -1, "", 0, 1, false))
                            {
                                PlayerManager.ShowNotification(client, "Có lỗi khi thực hiện thêm vật phẩm vào túi đồ vui lòng liên hệ ADM để được giúp đỡ!");

                                LogManager.WriteLog(LogTypes.BuyNpc, "Có lỗi khi thực hiện add vật phẩm :" + client.RoleID + "|" + ITEMID + " x " + 1);
                            }
                        }
                    }
                    else
                    {
                        PlayerManager.ShowNotification(client, "Hôm nay bạn đã hết lượt mua");
                    }
                }
            }
        }

        /// <summary>
        /// Lấy ra ranking tương ứng của người chơi
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetRankOfPlayerFromDatabase(KPlayer client)
        {
            int RankType = (int)RankMode.UyDanh;

            int RoleID = client.RoleID;

            string CMDBUILD = RankType + ":" + RoleID + ":" + GameManager.ServerLineID;

            string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_RANKING_CHECKING, CMDBUILD, GameManager.ServerId);
            if (null == dbFields)
            {
                return -100;
            }
            if (dbFields.Length != 2)
            {
                return -100;
            }

            int RANKRETURN = Convert.ToInt32(dbFields[1]);

            return RANKRETURN;
        }
        
        public static int GetRankOfPlayerFromDB1Time1Week(KPlayer player)
        {
            string CMD = player.RoleID + ":" + GameManager.ServerLineID;
            string[] result = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_RANKING_CHECKING_WEEK, CMD, GameManager.ServerId);
            if (null == result)
            {
                return -100;
            }

            if (result.Length != 2)
            {
                return -100;
            }
            
            return Convert.ToInt32(result[1]);
        }

        public static void BuyGatherMakerPoint(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            string Text = "";
            string Title = "";
            Dictionary<int, string> Selections = new Dictionary<int, string>();
            int Rank = ShopSalePrestige.GetRankOfPlayerFromDB1Time1Week(client);
            // check xem đủ điều kiện mua ko?
            if (client.Prestige < 80)
            {
                Title = "Bạn cần 80 Uy Danh để mua Tinh Hoạt Lực giá chiết khấu 50% mỗi ngày!";
            }
            else if (Rank == -100 || Rank == -1)
            {
                Title = "Bạn không nằm trong top 100 BXH uy danh tuần trước!";
            }    
            else
            {
                Selections.Add((int)AcitvityRecore.BuyGatherPoint, "Mua Tinh Lực 500 (Khuyến mãi - 15 KNB)");
                Selections.Add((int)AcitvityRecore.BuyMakerPoint, "Mua Hoạt Lực 500 (Khuyến mãi - 15 KNB)");
            }    
            Text = "Xin chào <b>" + client.RoleName + "</b>" + " bạn hiện có  <b>" + KTGlobal.CreateStringByColor(client.Prestige + " Uy Danh", ColorType.Yellow) + "</b>\n\n" + Title;
            Action<TaskCallBack> ActionWork = (x) => ClickBuyPoint(client, npc, x);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            _NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        private static void ClickBuyPoint(KPlayer client, NPC npc, TaskCallBack x)
        {
            // xem đã mua tối đa trong ngày chưa?
            int check = client.GetValueOfDailyRecore(x.SelectID);
            bool canBuy = true;

            if (check < 0)
            {
                check = 0;
            }    

            if (check >= 5)
            {
                canBuy = false;

            }
            // xem số tiền trong ng có đủ để mua hay ko?
            if (!KTGlobal.IsHaveMoney(client, 15, Entities.MoneyType.Dong))
            {
                PlayerManager.ShowNotification(client, "Tiền không đủ");
            }
            else
            {
                if (canBuy)
                {
                    if (!KTGlobal.IsHaveSpace(1, client))
                    {
                        PlayerManager.ShowNotification(client, "Cần 1 ô trống để mua");

                    }    
                    else
                    {
                        int itemID = 348;
                        if (x.SelectID == (int)AcitvityRecore.BuyMakerPoint)
                        {
                            itemID = 351;
                        }
                        // ghi lại số lần mua hôm nay
                        client.SetValueOfDailyRecore(x.SelectID, check + 1);

                        if (KTGlobal.SubMoney(client, 15, Entities.MoneyType.Dong, "DISCOUNTSHOP").IsOK)
                        {
                            string TimeUsing = Global.ConstGoodsEndTime;
                            // Tọa vật phẩm đã mua
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, itemID, 1, 0, "DISCOUNTSHOP", true, 1, false, TimeUsing, "", -1, "", 0, 1, false))
                            {
                                PlayerManager.ShowNotification(client, "Có lỗi khi thực hiện thêm vật phẩm vào túi đồ vui lòng liên hệ ADM để được giúp đỡ!");

                                LogManager.WriteLog(LogTypes.BuyNpc, "Có lỗi khi thực hiện add vật phẩm :" + client.RoleID + "|" + itemID + " x " + 1);
                            }
                        }
                    }    
                }  
                else
                {
                    PlayerManager.ShowNotification(client, "Mỗi ngày chỉ có thể mua 5 bình!");
                }
            }    
        }

        public static void ChangeMoney(NPC npc, KPlayer client)
        {
            KNPCDialog _NpcDialog = new KNPCDialog();
            string Text = "";
            string Title = "";
            Dictionary<int, string> Selections = new Dictionary<int, string>();
            // check xem đủ điều kiện ko?
            //Lấy ra rank hiện tại của người
            int Rank = ShopSalePrestige.GetRankOfPlayerFromDB1Time1Week(client);

            if (Rank == -100)
            {
                Title = "Hệ thống bảng xếp hạng đang cập nhật\nVui lòng quay lại sau";
            }
            else if (Rank == -1)
            {
                Title = "Bạn không nằm trong top 100 BXH uy danh tuần trước!";
            }
            else
            {
                Title = "Bạn nằm trong top 100 BXH uy danh tuần trước. Bạn có thể đổi 20 vạn bạc khóa lấy 10 vạn bạc thường. (Cần có ít nhất 80 uy danh để đổi)";
                Selections.Add((int)AcitvityRecore.ChangeMoney, "Đổi 20 vạn bạc khóa lấy 10 vạn bạc thường");
            }

            Text = "Xin chào <b>" + client.RoleName + "</b>" + " bạn hiện có  <b>" + KTGlobal.CreateStringByColor(client.Prestige + " Uy Danh", ColorType.Yellow) + "</b>\n\n" + Title;

            Action<TaskCallBack> ActionWork = (x) => ClickChangeMoney(client, npc, x);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            _NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        private static void ClickChangeMoney(KPlayer client, NPC npc, TaskCallBack x)
        {
            // xem đã mua tối đa trong ngày chưa?
            int check = client.GetValueOfWeekRecore(x.SelectID);
            bool canBuy = true;

            if (check > 0)
            {
                canBuy = false;
            }

            // xem số tiền trong ng có đủ để mua hay ko?
            if (!KTGlobal.IsHaveMoney(client, 200000, Entities.MoneyType.BacKhoa))
            {
                PlayerManager.ShowNotification(client, "Tiền không đủ");
            }
            else if (client.Prestige < 80)
            {
                PlayerManager.ShowNotification(client, "Uy danh không đủ");
            }    
            else
            {
                if (canBuy)
                {
                    // ghi lại số lần mua hôm nay
                    client.SetValueOfWeekRecore(x.SelectID, 1);
                    if (KTGlobal.SubMoney(client, 200000, Entities.MoneyType.BacKhoa, "DISCOUNTSHOP").IsOK)
                    {
                        // thêm bạc thường cho ng chơi
                        if (!KTGlobal.AddMoney(client, 100000, Entities.MoneyType.Bac, "DISCOUNTSHOP").IsOK)
                        {
                            PlayerManager.ShowNotification(client, "Có lỗi khi đổi, vui lòng liên hệ ADMIN để được giúp đỡ");
                            LogManager.WriteLog(LogTypes.BuyNpc, "Có lỗi khi thực hiện đổi :" + client.RoleID + "|" + " x " + 1);
                        }    
                    }

                } 
                else
                {
                    PlayerManager.ShowNotification(client, "Một tuần chỉ có thể đổi 1 lần!");
                }    
            }    
        }

        public static void ChangeBoundToken(NPC npc, KPlayer client)
        {

            KNPCDialog _NpcDialog = new KNPCDialog();
            string Text = "";
            string Title = "";
            Dictionary<int, string> Selections = new Dictionary<int, string>();
            // check xem đủ điều kiện ko?
            //Lấy ra rank hiện tại của người
            int Rank = ShopSalePrestige.GetRankOfPlayerFromDB1Time1Week(client);

            if (Rank == -100)
            {
                Title = "Hệ thống bảng xếp hạng đang cập nhật\nVui lòng quay lại sau";
            }
            else if (Rank == -1)
            {
                Title = "Bạn không nằm trong top 100 BXH uy danh tuần trước!";
            }
            else
            {
                if (Rank >=0 && Rank < 10)
                {
                    Title = "Bạn nằm trong top 10 BXH uy danh tuần trước, có thể đổi 120 uy danh lấy 2000 KNB khóa. (Cần có ít nhất 80 uy danh để đổi)";
                    Selections.Add((int)AcitvityRecore.ChangeBoundToken, "Đổi 120 uy danh lấy 2000 KNB khóa");
                }    
                else if (Rank >= 10 && Rank <20)
                {
                    Title = "Bạn nằm trong top 20 BXH uy danh tuần trước, có thể đổi 60 uy danh lấy 1000 KNB khóa. (Cần có ít nhất 80 uy danh để đổi)";
                    Selections.Add((int)AcitvityRecore.ChangeBoundToken, "Đổi 60 uy danh lấy 1000 KNB khóa");
                }    
                else
                {
                    Title = "Bạn nằm trong top 100 BXH uy danh tuần trước, có thể đổi 30 uy danh lấy 500 KNB khóa. (Cần có ít nhất 80 uy danh để đổi)";
                    Selections.Add((int)AcitvityRecore.ChangeBoundToken, "Đổi 30 uy danh lấy 500 KNB khóa");
                }    
            }
            Text = "Xin chào <b>" + client.RoleName + "</b>" + " bạn hiện có  <b>" + KTGlobal.CreateStringByColor(client.Prestige + " Uy Danh", ColorType.Yellow) + "</b>\n\n" + Title;

            Action<TaskCallBack> ActionWork = (x) => ClickChangeBoundToken(client, npc, x, Rank);

            _NpcDialog.OnSelect = ActionWork;

            _NpcDialog.Selections = Selections;

            _NpcDialog.Text = Text;

            _NpcDialog.Show(npc, client);
        }

        private static void ClickChangeBoundToken(KPlayer client, NPC npc, TaskCallBack x, int Rank)
        {
            // xem đã mua tối đa trong ngày chưa?
            int check = client.GetValueOfWeekRecore(x.SelectID);
            bool canBuy = true;

            if (check > 0)
            {
                PlayerManager.ShowNotification(client, "Mỗi tuần chỉ đổi được 1 lần!");
            }
            else
            {

                int boundToken = 500;
                int numPrestige = 30;
                if (client.Prestige < 80)
                {
                    canBuy = false;
                }
                if (Rank < 20)
                {
                    if (client.Prestige < 80)
                    {
                        canBuy = false;
                    }
                    boundToken = 1000;
                    numPrestige = 60;
                }
                if (Rank < 10)
                {
                    if (client.Prestige < 120)
                    {
                        canBuy = false;
                    }
                    boundToken = 2000;
                    numPrestige = 120;
                }

                if (canBuy)
                {
                    // ghi lại
                    client.SetValueOfWeekRecore(x.SelectID, 1);
                    int oldPrestige = client.Prestige;
                    // trừ uy danh
                    client.Prestige -= numPrestige;
                    LogManager.WriteLog(LogTypes.UyDanh, string.Format("PlayerID : {0} đã bị trừ {1} uy danh đổi sang {2} KNB khóa, uy danh tăng từ {3} xuống {4}", client.RoleName, numPrestige, boundToken, oldPrestige, client.Prestige));
                    // thêm đồng khóa
                    if (!KTGlobal.AddMoney(client, boundToken, Entities.MoneyType.DongKhoa, "DISCOUNTSHOP").IsOK)
                    {
                        PlayerManager.ShowNotification(client, "Có lỗi khi đổi, vui lòng liên hệ ADMIN để được giúp đỡ");
                        LogManager.WriteLog(LogTypes.BuyNpc, "Có lỗi khi thực hiện đổi :" + client.RoleID + "|" + " x " + boundToken);
                    }
                }
                else
                {
                    PlayerManager.ShowNotification(client, "Bạn không đủ uy danh");
                }
            }   
        }
    }
}