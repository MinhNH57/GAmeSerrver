using GameServer.Core.Executor;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Net;
using GameServer.KiemThe.Entities;
using GameServer.Server;
using System.Collections.Generic;

namespace GameServer.KiemThe.Core.Item
{
    public class GiftCodeManager
    {
        /// <summary>
        /// Hàm xử lý Giftcode nhận từ API
        /// </summary>
        public static int ActiveGiftCodeFromAPI (string strRoleID, string strSvID, string strAccountID, int giftCodeId, string orderId, string strGiftCode)
        {
            ///kiểm tra người chơi đã sử dụng giftcode ko?

            string giftItem = "";
            int boundMoney = 0;
            int boundToken = 0;
            string title = "";
            string content = "";
            GiftCode giftCode;
            if (KTGlobal.giftCodes.TryGetValue(giftCodeId, out giftCode))
            {
                // lấy ra list vật phẩm
                giftItem = giftCode.item;
                boundMoney = giftCode.boundMoney;
                boundToken = giftCode.boundToken;
                title = giftCode.name;
                content = giftCode.msg;
            }
            else
            {
                return -1;
            }
           

            /// danh sách vật phẩm gửi qua thư
            List<GoodsData> goodsData = new List<GoodsData>();
            string[] itemList = giftItem.Split('#');
            
            if (giftItem != "")
            {
                foreach (string item in itemList)
                {
                    string[] paramItem = item.Split('|');
                    GoodsData goodData = new GoodsData();
                    goodData.GoodsID = Int32.Parse(paramItem[0]);
                    goodData.Binding = Int32.Parse(paramItem[1]);
                    goodData.GCount = Int32.Parse(paramItem[2]);
                    goodsData.Add(goodData);
                }
            }

            ///// gửi thư về db kể cả đang off
            //string title = "GiftCode";
            //string content = "Hệ thống gửi phần thưởng GiftCode " + strGiftCode + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
            

            if (giftCodeId == 2000079 || giftCodeId == 2000080)
            {
                title = "Gói Kiếm Vương Lệnh";
                content = "Chúc mừng bạn đã mua thành công Gói Kiếm Vương Lệnh - " + (giftCodeId == 2000080 ? "Siêu Cấp" : "Cao Cấp")
                        + "\nVui lòng vào túi đồ và sử dụng vật phẩm để kích hoạt đặc quyền Kiếm Vương Lệnh."
                        +"\nChúc bạn chơi game vui vẻ!";
            }

            if (title == "")
            {
                title = "GiftCode";
            }

            if (content == "")
            {
                content = "Hệ thống gửi phần thưởng GiftCode " + strGiftCode + "! Hãy sắp xếp túi đồ trước khi nhận thưởng đính kèm trong thư.";
            }
            return KTGlobal.SendMail(goodsData, Convert.ToInt32(strRoleID), null, title, content, boundToken, boundMoney, orderId, giftCodeId.ToString());
        }

        /// <summary>
        ///  Hàm check xem người chơi đủ điều kiện nhận giftcode ko?
        /// </summary>
        public static bool IsCanActiveGiftCode(KPlayer player, string stringRoleID)
        {
            if (stringRoleID == "")
            {
                return true;
            }

            string[] roleListID = stringRoleID.Split(';');
            foreach (string role in roleListID)
            {
                int roleID = Int32.Parse(role);
                if (roleID == player.RoleID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Hàm check xem người chơi đã nhận giftcode đó chưa?
        /// </summary>
        public static bool IsActiveGiftCode(KPlayer player, GiftCodeRequest codeActive)
        {
            // giao tiếp với db xem người chơi đã dùng code chưa?
            string strDbCmd = string.Format("{0}:{1}", player.RoleID, codeActive.CodeActive);
            string[] fieldsData = null;
            /// Thực thi gửi thư
            fieldsData = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_CHECKGIFTCODE, strDbCmd, player.ServerId);

            //ClientManager.CheckEmailCount(client);

            /// Nếu có lỗi gì đó
            if (null == fieldsData || fieldsData.Length != 2)
            {
                return false;
            }
            if (Convert.ToInt32(fieldsData[1]) == 0)
            {
                return true;
            }
            return false;
            
        }

        /// <summary>
        /// Hàm ghi lại thông tin người chơi đã dùng giftcode
        /// </summary>
        public static void SetActiveGiftCode(KPlayer player, GiftCodeRequest codeActive)
        {
            string strDbCmd = string.Format("{0}:{1}", player.RoleID, codeActive.CodeActive);
            string[] fieldsData = null;
            /// Thực thi gửi thư
            fieldsData = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_KT_SAVEGIFTCODE, strDbCmd, player.ServerId);

            //ClientManager.CheckEmailCount(client);

            /// Nếu có lỗi gì đó
            if (null == fieldsData || fieldsData.Length != 2)
            {
                return;
            }
            if(!Convert.ToBoolean(fieldsData[1]))
            {
                return;

            }
        }
    }
}