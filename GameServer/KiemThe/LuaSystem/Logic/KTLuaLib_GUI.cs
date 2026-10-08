using GameServer.KiemThe.Core.Activity.LuckyCircle;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using GameServer.Logic.Name;
using GameServer.Server;
using MoonSharp.Interpreter;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.KiemThe.Core.Activity.SeashellCircle;

namespace GameServer.KiemThe.LuaSystem.Logic
{
    /// <summary>
    /// Cung cấp thư viện dùng cho Lua, giao diện
    /// </summary>
    [MoonSharpUserData]
    public static class KTLuaLib_GUI
    {
        #region NPC, Item Dialog
        /// <summary>
        /// Tạo mới cửa sổ hội thoại của NPC gồm danh sách các sự lựa chọn, và danh sách vật phẩm cần chọn ra 1 cái hoặc chỉ hiện ra mà không cho chọn
        /// </summary>
        /// <returns></returns>
        public static Lua_NPCDialog CreateNPCDialog()
        {
            return new Lua_NPCDialog();
        }


     
        /// <summary>
        /// Tạo mới cửa sổ hội thoại của vật phẩm gồm danh sách các sự lựa chọn, và danh sách vật phẩm cần chọn ra 1 cái hoặc chỉ hiện ra mà không cho chọn
        /// </summary>
        /// <returns></returns>
        public static Lua_ItemDialog CreateItemDialog()
        {
            return new Lua_ItemDialog();
        }

        /// <summary>
        /// Đóng bảng hội thoại từ NPCDialog hoặc ItemDialog đã mở
        /// </summary>
        public static void CloseDialog(Lua_Player player)
        {
            KT_TCPHandler.CloseDialog(player.RefObject);
        }
        #endregion

        #region Notification
        /// <summary>
        /// Hiển thị thông báo lên người chơi
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="message"></param>
        public static void ShowNotification(Lua_Player player, string message)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.ShowNotification, Player is NULL."));
                return;
            }

            PlayerManager.ShowNotification(player.RefObject, message);
        }
        #endregion

        #region Open UI
        /// <summary>
        /// Mở khung bất kỳ cho người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="uiName"></param>
        /// <param name="parameters"></param>
        public static void OpenUI(Lua_Player player, string uiName, params int[] parameters)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }

            KT_TCPHandler.SendOpenUI(player.RefObject, uiName, parameters);
        }

        /// <summary>
        /// Mở khung bất kỳ cho người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="uiName"></param>
        /// <param name="parameters"></param>
        public static void OpenUIWithStringParams(Lua_Player player, string uiName, params string[] strParams)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }

            KT_TCPHandler.SendOpenUI(player.RefObject, uiName, Array.Empty<int>(), strParams.ToList());
        }

        public static void OpenUIWithStringDict(Lua_Player player, string uiName, IDictionary<string, string> strDict) 
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }
            
            var strParams = new List<string>();
            foreach (var e in strDict)
            {
                strParams.Add(e.Key);
                strParams.Add(e.Value);
            }

            KT_TCPHandler.SendOpenUI(player.RefObject, uiName, Array.Empty<int>(), strParams.ToList());            
        }

        /// <summary>
        /// Đóng khung bất kỳ của người chơi tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="uiName"></param>
        public static void CloseUI(Lua_Player player, string uiName)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.CloseUI, Player is NULL."));
                return;
            }

            KT_TCPHandler.SendCloseUI(player.RefObject, uiName);
        }
        #endregion

        #region Send System message
        /// <summary>
        /// Gửi tin nhắn hệ thống tới tất cả người chơi
        /// </summary>
        /// <param name="message"></param>
        public static void SendSystemMessage(string message)
        {
            KTGlobal.SendSystemChat(message);
        }

        /// <summary>
        /// Gửi tin nhắn kênh hệ thống kèm dòng chữ chạy trên đầu tới tất cả người chơi
        /// </summary>
        /// <param name="message"></param>
        public static void SendSystemEventNotification(string message)
        {
            KTGlobal.SendSystemEventNotification(message);
        }
        #endregion

        #region Message Box
        /// <summary>
        /// Hiện MessageBox tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="text"></param>
        public static void ShowMessageBox(Lua_Player player, string title, string text)
        {
            PlayerManager.ShowMessageBox(player.RefObject, title, text);
        }

        /// <summary>
        /// Hiện MessageBox tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="ok"></param>
        public static void ShowMessageBox(Lua_Player player, string title, string text, Closure ok)
        {
            PlayerManager.ShowMessageBox(player.RefObject, title, text, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("MessageBox:OK", ok, null, null);
            });
        }

        /// <summary>
        /// Hiện MessageBox tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="ok"></param>
        /// <param name="cancel"></param>
        public static void ShowMessageBox(Lua_Player player, string title, string text, Closure ok, Closure cancel)
        {
            PlayerManager.ShowMessageBox(player.RefObject, title, text, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("MessageBox:OK", ok, null, null);
            }, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("MessageBox:Cancel", cancel, null, null);
            });
        }

        /// <summary>
        /// Hiện InputNumber tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="text"></param>
        /// <param name="ok"></param>
        public static void ShowInputNumber(Lua_Player player, string text, Closure ok)
        {
            PlayerManager.ShowInputNumberBox(player.RefObject, text, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputNumber:OK", ok, parameters, null);
            });
        }

        /// <summary>
        /// Hiện InputNumber tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="text"></param>
        /// <param name="ok"></param>
        /// <param name="cancel"></param>
        public static void ShowInputNumber(Lua_Player player, string text, Closure ok, Closure cancel)
        {
            PlayerManager.ShowInputNumberBox(player.RefObject, text, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputNumber:OK", ok, parameters, null);
            }, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("InputNumber:Cancel", cancel, null, null);
            });
        }

        /// <summary>
        /// Hiện InputString tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="ok"></param>
        /// <param name="cancel"></param>
        public static void ShowInputString(Lua_Player player, string description, Closure ok, Closure cancel)
        {
            PlayerManager.ShowInputStringBox(player.RefObject, description, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:OK", ok, parameters, null);
            }, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:Cancel", cancel, null, null);
            });
        }

        /// <summary>
        /// Hiện InputString tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="initValue"></param>
        /// <param name="ok"></param>
        public static void ShowInputString(Lua_Player player, string description, Closure ok)
        {
            PlayerManager.ShowInputStringBox(player.RefObject, description, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:OK", ok, parameters, null);
            });
        }

        /// <summary>
        /// Hiện InputString tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="initValue"></param>
        /// <param name="ok"></param>
        public static void ShowInputString(Lua_Player player, string description, string initValue, Closure ok)
		{
            PlayerManager.ShowInputStringBox(player.RefObject, description, initValue, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:OK", ok, parameters, null);
            });
        }

        /// <summary>
        /// Hiện InputString tương ứng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="initValue"></param>
        /// <param name="ok"></param>
        /// <param name="cancel"></param>
        public static void ShowInputString(Lua_Player player, string description, string initValue, Closure ok, Closure cancel)
		{
            PlayerManager.ShowInputStringBox(player.RefObject, description, initValue, (value) => {
                object[] parameters = new object[]
                {
                    value,
                };
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:OK", ok, parameters, null);
            }, () => {
                KTLuaScript.Instance.ExecuteFunctionAsync("InputString:Cancel", cancel, null, null);
            });
        }
		#endregion

		#region Input Second Password
        /// <summary>
        /// Mở khung nhập mật khẩu cấp 2
        /// </summary>
        /// <param name="player"></param>
        public static void OpenInputSecondPassword(Lua_Player player)
		{
            KT_TCPHandler.SendOpenInputSecondPassword(player.RefObject);
		}
		#endregion

		#region Danh sách xóa vật phẩm
        /// <summary>
        /// Mở khung tiêu hủy vật phẩm
        /// </summary>
        /// <param name="player"></param>
        public static void OpenRemoveItems(Lua_Player player)
		{
            PlayerManager.OpenRemoveItems(player.RefObject);
		}
        #endregion

        #region Danh sách ghép vật phẩm
        /// <summary>
        /// Mở khung ghép vật phẩm
        /// </summary>
        /// <param name="player"></param>
        public static void OpenMergeItems(Lua_Player player)
		{
            PlayerManager.OpenMergeItems(player.RefObject);
		}
        #endregion

        #region Đổi quan ấn, phi phong, ngũ hành ấn về đúng hệ
        /// <summary>
        /// Mở khung đổi quan ấn, phi phong, ngũ hành ấn về đúng hệ
        /// </summary>
        /// <param name="player"></param>
        public static void OpenChangeSignetMantleAndChopstick(Lua_Player player)
        {
            PlayerManager.OpenChangeSignetMantleAndChopstick(player.RefObject);
        }
        #endregion

        #region Đổi tên
        /// <summary>
        /// Mở khung đổi tên nhân vật
        /// </summary>
        /// <param name="player"></param>
        public static void OpenChangeName(Lua_Player player)
        {
            /// Nếu không có thẻ đổi tên
            if (ItemManager.GetItemCountInBag(player.RefObject, KTGlobal.ChangeNameCardItemID) < 1)
			{
                PlayerManager.ShowNotification(player.RefObject, "Cần có [Thẻ đổi tên] mới có thể sử dụng chức năng này!");
                return;
			}

            /// Mở khung nhập tên cần đổi
            PlayerManager.ShowInputStringBox(player.RefObject, "Nhập tên cần đổi (từ 6 đến 18 ký tự).", (newName) => {
                /// Thực hiện đổi tên
                NameManager.Instance().ProcessChangeName(player.RefObject, newName, (_oldName, _newName) => {
                    /// Xóa Thẻ đổi tên
                    ItemManager.RemoveItemFromBag(player.RefObject, KTGlobal.ChangeNameCardItemID, 1);

                    /// cho mất kết nối bắt kết nỗi lại để đổi tên
                    int roleID = player.RefObject.RoleID;
                    string otherRoleName = player.RefObject.RoleName;
                    if (-1 != roleID)
                    {
                        KPlayer otherClient = GameManager.ClientMgr.FindClient(roleID);
                        if (null != otherClient)
                        {
                            Global.ForceCloseClient(otherClient, "GMKICK");
                        }
                    }

                    PlayerManager.ShowNotification(player.RefObject, "Kick người chơi thành công");

                    string strcmd = string.Format("{0}", otherRoleName);
                    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GM_CLEAR_CACHE_USER, strcmd,
                        GameManager.LocalServerId);
                    if (null == dbFields)
                    {
                        PlayerManager.ShowNotification(player.RefObject, "Xóa cache người chơi lỗi");
                    }
                    else
                    {
                        PlayerManager.ShowNotification(player.RefObject,
                            string.Format("Xóa cache người chơi: {0}", dbFields[1]));
                    }

                    GameManager.logDBCmdMgr.WriteLogDBForPlayer(player.RefObject, 
                        "RENAME", "", _oldName, _newName, "",
                        "", "", "", "");
                    ///// Show hàng
                    //KTGlobal.SendSystemChat(string.Format("Người chơi <color=#38c0ff>[{0}]</color> đã đổi tên thành <color=#38c0ff>[{1}]</color>", _oldName, _newName));
                });
            });
		}
        #endregion

        #region Mở Vòng quay may mắn
        /// <summary>
        /// Mở khung Vòng quay may mắn
        /// </summary>
        /// <param name="player"></param>
        public static void OpenLuckyCircle(Lua_Player player)
        {
            KTLuckyCircleManager.OpenCircle(player.RefObject);
        }

        public static void OpenSeashell(Lua_Player player) 
        {
            KTSeashellCircleManager.OpenSeashellUI(player.RefObject);
        }
        #endregion

        public static void OpenRanking(Lua_Player player, int mode = (int)RankMode.CapDo)
        {
            KT_TCPHandler.SendOpenUI(player.RefObject, "Ranking", new [] { mode });
        }

        public static void OpenInputEquipAndMaterials(Lua_Player player)
        {
            KT_TCPHandler.SendOpenInputEquipAndMaterials(player.RefObject, "Khung vật phẩm", "Mô tả", "Chi tiết", true, "");
        }

        public static void OpenInputItems(Lua_Player player) 
        {
            KT_TCPHandler.SendOpenInputItems(player.RefObject, "Khung vật phẩm", "Mô tả", "Chi tiết", "");
        }
        
        public static void OpenComposeDialog(Lua_Player player, string uiName, params int[] parameters)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }

            KT_TCPHandler.SendOpenComposeDialog(player.RefObject, uiName, parameters.ToList());
        }

        public static void OpenDanphongDialog(Lua_Player player, string template)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }
            KT_TCPHandler.SendOpenDanPhongDialog(player.RefObject, template);
        }
        
        public static void OpenBaoThachUI(Lua_Player player, int  index)
        {
            if (player.RefObject == null)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lua error on GUI.OpenUI, Player is NULL."));
                return;
            }
            KT_TCPHandler.SendOpenUI(player.RefObject, "UIBaoThach", new int[]{index});
        }
    }
}
