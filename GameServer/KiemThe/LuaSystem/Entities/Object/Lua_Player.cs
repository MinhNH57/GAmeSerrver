using GameServer.Core.Executor;
using GameServer.KiemThe.CopySceneEvents;
using GameServer.KiemThe.Core.Activity.PlayerPray;
using GameServer.KiemThe.Core.Activity.SeashellCircle;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities.Math;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using GameServer.Server;
using MoonSharp.Interpreter;
using Server.Tools;
using System;
using System.Linq;
using GameServer.KiemThe.Entities.Player;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.KiemThe.GameEvents.EmperorTomb;
using GameServer.KiemThe.GameEvents.TangBaoDo;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameServer.KiemThe.GameEvents.LotteryEvent;
using GameServer.KiemThe.GameEvents.FactionBattle;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.Core.Task;
using Server.Data;
using Newtonsoft.Json.Bson;

namespace GameServer.KiemThe.LuaSystem.Entities
{
    /// <summary>
    /// Đối tượng người chơi
    /// </summary>
    [MoonSharpUserData]
    public class Lua_Player
    {
        #region Base for all objects

        /// <summary>
        /// Đối tượng tham chiếu trong hệ thống
        /// </summary>
        [MoonSharpHidden]
        public KPlayer RefObject { get; set; }

        /// <summary>
        /// Trả về ID đối tượng
        /// </summary>
        /// <returns></returns>
        public int GetID()
        {
            return this.RefObject.RoleID;
        }

        /// <summary>
        /// Trả về tên đối tượng
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return this.RefObject.RoleName;
        }

        public string GetUserID()
        {
            return this.RefObject.strUserID;
        }

        #endregion

        #region Base for all scene-objects

        /// <summary>
        /// Bản đồ hiện tại
        /// </summary>
        [MoonSharpHidden]
        public Lua_Scene CurrentScene { get; set; }

        /// <summary>
        /// Trả về vị trí của đối tượng
        /// </summary>
        /// <returns></returns>
        public Lua_Vector2 GetPos()
        {
            return new Lua_Vector2(this.RefObject.PosX, this.RefObject.PosY);
        }

        /// <summary>
        /// Trả về bản đồ hiện tại
        /// </summary>
        /// <returns></returns>
        public Lua_Scene GetScene()
        {
            return this.CurrentScene;
        }

        /// <summary>
        /// Trả về danh hiệu hiện tại
        /// </summary>
        /// <returns></returns>
        public string GetTitle()
        {
            return this.RefObject.Title;
        }

        /// <summary>
        /// Trả về loại đối tượng
        /// </summary>
        /// <returns></returns>
        public int GetObjectType()
        {
            return (int)ObjectTypes.OT_CLIENT;
        }

        #endregion


        /// <summary>
        /// Trả về ID môn phái của người chơi
        /// </summary>
        /// <returns></returns>
        public int GetFactionID()
        {
            return this.RefObject.m_cPlayerFaction.GetFactionId();
        }

        /// <summary>
        /// Trả về tên môn phái của người chơi
        /// </summary>
        /// <returns></returns>
        public string GetFactionName()
        {
            return this.RefObject.m_cPlayerFaction.GetFactionName();
        }

        public string GetNameFaction(int factionID)
        {
            return KFaction.GetName(factionID);
        }

        /// <summary>
        /// Trả về ID nhánh của người chơi
        /// </summary>
        /// <returns></returns>
        public int GetRouteID()
        {
            return this.RefObject.m_cPlayerFaction.GetRouteId();
        }

        /// <summary>
        /// Trả về tên nhánh của người chơi
        /// </summary>
        /// <returns></returns>
        public string GetRouteName()
        {
            return this.RefObject.m_cPlayerFaction.GetRouteName();
        }

        public int GetMainTaskID()
        {
            return this.RefObject.MainTaskID;
        }

        /// <summary>
        /// hàm kiểm tra xem đã hoàn thành hoặc có đang trong task này hay không
        /// chỉ dùng check item Trang Sách
        /// </summary>
        /// <param name="taskID"></param>
        /// <returns></returns>
        public bool IsDoingOrCompletedTaskID(int taskID)
        {
            var taskData = TaskManager.getInstance().GetTaskData(taskID, RefObject);
            if (taskData != null)
                return true;

            var currentTaskID = RefObject.MainTaskID;
            return currentTaskID >= taskID;
        }

        /// <summary>
        /// Trả về giá trị điểm tiềm năng được cộng thêm của nhân vật
        /// </summary>
        /// <returns></returns>
        public int GetBonusRemainPotentialPoint()
        {
            return this.RefObject.GetBonusRemainPotentialPoints();
        }

        /// <summary>
        /// Cấp độ
        /// </summary>
        /// <returns></returns>
        public int GetLevel()
        {
            return this.RefObject.m_Level;
        }

        /// <summary>
        /// Có kỹ năng ID tương ứng không
        /// </summary>
        /// <param name="skillID"></param>
        /// <returns></returns>
        public bool HasSkill(int skillID)
        {
            return this.RefObject.Skills.HasSkill(skillID);
        }

        /// <summary>
        /// Trả về cấp độ hiện tại của kỹ năng (bao gồm cả các cấp độ cộng thêm từ kỹ năng, trang bị khác)
        /// </summary>
        /// <param name="skillID"></param>
        /// <returns></returns>
        public int GetSkillLevel(int skillID)
        {
            SkillLevelRef skillRef = this.RefObject.Skills.GetSkillLevelRef(skillID);
            if (skillRef == null)
            {
                return -1;
            }

            return skillRef.Level;
        }

        /// <summary>
        /// Trả về giới tính của người chơi
        /// </summary>
        /// <returns></returns>
        public int GetSex()
        {
            return this.RefObject.RoleSex;
        }

        public int GetClientVer()
        {
            return this.RefObject.MainExeVer;
        }

        /// <summary>
        /// Gia nhập môn phái
        /// </summary>
        /// <param name="player"></param>
        /// <param name="factionID"></param>
        /// <returns>-1: Player NULL, -2: Môn phái không tồn tại, 0: Giới tính không phù hợp, 1: Thành công</returns>
        public int JoinFaction(int factionID)
        {
            return PlayerManager.JoinFaction(this.RefObject, factionID);
        }

        /// <summary>
        /// Đổi nhánh tu luyện
        /// </summary>
        /// <param name="routeID"></param>
        /// <returns>-9999: Lỗi không rõ, -1: Player NULL, -2: Môn phái không tồn tại, 0: Giới tính không phù hợp, 1: Thành công</returns>
        public int ChangeRoute(int routeID)
        {
            return PlayerManager.ChangeRoute(this.RefObject, routeID);
        }

        /// <summary>
        /// Tẩy điểm kỹ năng
        /// </summary>
        /// <param name="player"></param>
        /// <returns>-9999: Lỗi không rõ, -1: Player NULL, -2: Môn phái không tồn tại, 0: Giới tính không phù hợp, 1: Thành công</returns>
        public void ResetAllSkillsLevel()
        {
            PlayerManager.ChangeRoute(this.RefObject, GameDataDef.KD_FACTION_ROUTE_NONE);
        }

        /// <summary>
        /// Tẩy điểm tiềm năng
        /// </summary>
        public void UnAssignRemainPotentialPoints()
        {
            this.RefObject.UnAssignPotential();
        }

        #region SongTu

        public int EndSongTu()
        {
            int result = 1;
            PlayerManager.ShowMessageBox(this.RefObject, "Chú ý",
                "Sau khi đổi phái, người chơi sẽ bị mất kết nối, kết nối lại để vào lại game", () =>
                {
                    int roleID = this.RefObject.RoleID;
                    string otherRoleName = this.RefObject.RoleName;
                    if (-1 != roleID)
                    {
                        KPlayer otherClient = GameManager.ClientMgr.FindClient(roleID);
                        if (null != otherClient)
                        {
                            Global.ForceCloseClient(otherClient, "GMKICK");
                        }
                        else
                        {
                        }
                    }

                    PlayerManager.ShowNotification(this.RefObject, "Kick người chơi thành công");

                    string strcmd = string.Format("{0}", otherRoleName);
                    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GM_CLEAR_CACHE_USER, strcmd,
                        GameManager.LocalServerId);
                    if (null == dbFields)
                    {
                        PlayerManager.ShowNotification(this.RefObject, "Xóa cache người chơi lỗi");
                        result = -99999;
                    }
                    else
                    {
                        PlayerManager.ShowNotification(this.RefObject,
                            string.Format("Xóa cache người chơi: {0}", dbFields[1]));
                    }
                }, false);

            return result;
        }

        /// <summary>
        /// SongTu
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
        public int SongTu(int factionID, bool isSongTu)
        {
            int ret = -3;

            /// nếu đang ở thi đấu môn phái thì cho ra khỏi thi đấu môn phái
            FactionBattleManager.FactionBattleDeleteRegiste(this.RefObject);


            DateTime nowTime = DateTime.Now;
            int mainFacctionID = this.RefObject.GetValueOfForeverRecore(RecoreDef.MainFaction);
            int resFactionID = this.RefObject.GetValueOfForeverRecore(RecoreDef.ResFaction);
            // nếu đổi phái từ tlc mà lỗi thì cook
            if (!isSongTu)
            {
                if (factionID != mainFacctionID && factionID != resFactionID)
                {
                    LogManager.WriteLog(LogTypes.SongTu,
                        $"NGười chơi: {this.RefObject.RoleName} đổi phái lỗi vs factionID = {factionID}, phái chính id = {mainFacctionID}, phái phụ id = {resFactionID}");
                    return ret;
                }
            }

            int currentfactionID = this.RefObject.m_cPlayerFaction.GetFactionId();
            // lay ra nhanh cu neu co
            int subOldID = this.RefObject.GetValueOfForeverRecore(RecoreDef.SubMainID);

            // lay ra nhanh hien tai
            int subNewID = this.RefObject.m_cPlayerFaction.GetRouteId();
            LogManager.WriteLog(LogTypes.SongTu,
                string.Format(
                    "Người chơi: {0}, đổi từ môn phái {1}, nhánh tu luyện {2} sang môn phái {3}, nhánh tu luyện {4}, có phải đổi phụ tu hay ko: {5}",
                    this.RefObject.RoleName, this.RefObject.m_cPlayerFaction.GetFactionId(), subNewID, factionID,
                    subOldID, isSongTu));
            if (isSongTu)
            {
                subOldID = -1;
            }

            ret = PlayerManager.SongTu(this.RefObject, factionID, subOldID);

            LogManager.WriteLog(LogTypes.SongTu, $"Người chơi: {this.RefObject.RoleName} song tu thành công!!!");

            if (ret == 1)
            {
                if (isSongTu)
                {
                    /// thực hiện tẩy điểm tiềm năng
                    this.RefObject.UnAssignPotential();

                    LogManager.WriteLog(LogTypes.SongTu,
                        $"Người chơi: {this.RefObject.RoleName} đã được reset điểm tiềm năng!!!");

                    this.RefObject.SetValueOfForeverRecore(RecoreDef.ResFaction, factionID);

                    LogManager.WriteLog(LogTypes.SongTu,
                        string.Format("Người chơi: {0} đã đổi môn phái phụ tu thành {1}", this.RefObject.RoleName,
                            factionID));

                    if (mainFacctionID == currentfactionID)
                    {
                        //luu lai sub moi

                        this.RefObject.SetValueOfForeverRecore(RecoreDef.SubMainID, subNewID);
                    }
                }
                else
                {
                    int year = nowTime.Year % 100;

                    int time = year * 10000000 + nowTime.DayOfYear * 10000 + nowTime.Hour * 100 + nowTime.Minute;

                    this.RefObject.SetValueOfForeverRecore(RecoreDef.NumSongTu, time);

                    //luu lai sub moi

                    this.RefObject.SetValueOfForeverRecore(RecoreDef.SubMainID, subNewID);
                }

                ret = EndSongTu();
            }

            return ret;
        }

        public bool CheckDayBoTuLenh()
        {
            DateTime nowTime = DateTime.Now;

            int day = nowTime.Year * 1000 + nowTime.DayOfYear;

            if (day - this.RefObject.GetValueOfForeverRecore(RecoreDef.BoTuLenh) >= 3)
            {
                return true;
            }

            return false;
        }

        public void SetDayBoTuLenh()
        {
            DateTime nowTime = DateTime.Now;

            int day = nowTime.Year * 1000 + nowTime.DayOfYear;

            this.RefObject.SetValueOfForeverRecore(RecoreDef.BoTuLenh, day);

            LogManager.WriteLog(LogTypes.SongTu,
                string.Format("Người chơi: {0}, đã dúng Bổ tu lệnh vào ngày: {1}", this.RefObject.RoleName, day));
        }

        public bool CheckDaySongTu()
        {
            DateTime nowTime = DateTime.Now;

            int day = nowTime.Year * 1000 + nowTime.DayOfYear;

            int lastDayUseBTL = this.RefObject.GetValueOfForeverRecore(RecoreDef.DaySongTu);

            LogManager.WriteLog(LogTypes.SongTu,
                string.Format("Người chơi : {0}, đã vào tẩy tủy đảo đổi phụ tu vào ngày: {1}", this.RefObject.RoleName,
                    lastDayUseBTL));

            if (day - lastDayUseBTL >= 3)
            {
                this.RefObject.SetValueOfForeverRecore(RecoreDef.DaySongTu, day);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check xem đã SongTu chưa
        /// </summary>
        public int IsHasSongTu()
        {
            return this.RefObject.GetValueOfForeverRecore(RecoreDef.MainFaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetResFaction()
        {
            return this.RefObject.GetValueOfForeverRecore(RecoreDef.ResFaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void SetStateSongTu(int state)
        {
            this.RefObject.SetValueOfForeverRecore(RecoreDef.SongTuState, state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetStateSongTu()
        {
            return this.RefObject.GetValueOfForeverRecore(RecoreDef.SongTuState);
        }

        /// <summary>
        /// Lưu lại MainFaction, set trang thai đang song tu
        /// </summary>
        /// <param name="factionID"></param>
        public void SetHasSongTu(int factionID)
        {
            this.RefObject.SetValueOfForeverRecore(RecoreDef.MainFaction, factionID);
            LogManager.WriteLog(LogTypes.UyDanh,
                string.Format("Người chơi: {0} đã song tu, môn phái chính, cũng là môn phái ban đầu là: {1}",
                    this.RefObject.RoleName, factionID));
        }

        /// <summary>
        /// Lưu lại số lượt đỏi song tu
        /// </summary>
        public void SetNumChangeSongTu()
        {
            this.RefObject.SetValueOfForeverRecore(RecoreDef.NumChangeSongTu, 1);
        }

        /// <summary>
        /// Lấy số lượt đổi song tu
        /// </summary>
        /// <returns></returns>
        public int GetNumChangeSongTu()
        {
            return this.RefObject.GetValueOfForeverRecore(RecoreDef.NumChangeSongTu);
        }

        public void ResetNumChangeSongTu()
        {
            this.RefObject.SetValueOfForeverRecore(RecoreDef.NumChangeSongTu, 0);
        }

        /// <summary>
        /// trả về số lượng item trong túi
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public int IsHasItem(int itemID)
        {
            return ItemManager.GetItemCountInBag(this.RefObject, itemID);
        }

        public bool CheckSafeArea()
        {
            if (this.RefObject.CurrentMapCode < 30 || this.RefObject.CurrentMapCode == 224)
                // là các map thôn, thành, phái (224 là minh giáo)
                return true;
            else
                return false;
        }

        public bool CheckTimeSongTu()
        {
            DateTime nowTime = DateTime.Now;
            int year = nowTime.Year % 100;
            int time = year * 10000000 + nowTime.DayOfYear * 10000 + nowTime.Hour * 100 + nowTime.Minute;

            if (time - this.RefObject.GetValueOfForeverRecore(RecoreDef.NumSongTu) >= 30)
            {
                return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Thiết lập cấp độ cho người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="level"></param>
        public void SetLevel(int level)
        {
            this.RefObject.UnAssignPotential();
            GameManager.ClientMgr.SetRoleLevel(this.RefObject, level);
            this.RefObject.ClearAssignedPotential();
            this.ResetAllSkillsLevel();
            this.RefObject.Skills.RemoveInvalidSkillBasedOnLevel();
        }

        public void ResetPotentialPoints()
        {
            this.RefObject.UnAssignPotential();
            this.RefObject.ClearAssignedPotential();
        }

        /// <summary>
        /// Tăng kinh nghiệm cho người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="exp"></param>
        public void AddExp(long exp)
        {
            GameManager.ClientMgr.ProcessRoleExperience(this.RefObject, exp);
        }

        /// <summary>
        /// Thêm điểm tiềm năng do ăn bánh cho người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="point"></param>
        public void AddBonusRemainPotentialPoint(int point)
        {
            int currentPoint = this.RefObject.GetBonusRemainPotentialPoints();
            this.RefObject.SetBonusRemainPotentialPoints(currentPoint + point);
        }

        /// <summary>
        /// Thêm điểm kỹ năng do ăn bánh cho người chơi
        /// </summary>
        /// <param name="player"></param>
        /// <param name="point"></param>
        public void AddBonusRemainSkillPoint(int point)
        {
            int currentPoint = this.RefObject.GetBonusSkillPoint();
            this.RefObject.SetBonusSkillPoint(currentPoint + point);
        }

        /// <summary>
        /// Thêm kỹ năng cho nhân vật
        /// </summary>
        /// <param name="skillID"></param>
        public void AddSkill(int skillID)
        {
            this.RefObject.Skills.AddSkill(skillID);
        }

        /// <summary>
        /// Xóa kỹ năng của nhân vật
        /// </summary>
        /// <param name="skillID"></param>
        public void RemoveSkill(int skillID)
        {
            this.RefObject.Skills.RemoveSkill(skillID);
        }

        /// <summary>
        /// Thêm cấp độ cho kỹ năng
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="addLevel"></param>
        public void AddSkillLevel(int skillID, int addLevel)
        {
            this.RefObject.Skills.AddSkillLevel(skillID, addLevel);
        }

        /// <summary>
        /// Thêm Buff tương ứng
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="level"></param>
        /// <param name="stack"></param>
        /// <param name="isFromItem"></param>
        public void AddBuff(int skillID, int level, int stack = 1, bool isFromItem = true)
        {
            this.RefObject.Buffs.AddBuff(skillID, level, stack, isFromItem);
        }

        /// <summary>
        /// Thêm Buff tương ứng
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="level"></param>
        /// <param name="durationTicks"></param>
        /// <param name="stack"></param>
        /// <param name="saveToDB"></param>
        public void AddBuffWithDuration(int skillID, int level, long durationTicks, int stack = 1,
            bool saveToDB = false)
        {
            this.RefObject.Buffs.AddBuff(skillID, level, durationTicks, stack, saveToDB);
        }

        /// <summary>
        /// Xóa Buff có ID tương ứng
        /// </summary>
        /// <param name="skillID"></param>
        public void RemoveBuff(int skillID)
        {
            this.RefObject.Buffs.RemoveBuff(skillID);
        }

        /// <summary>
        /// Kiểm tra Buff ID tương ứng có tồn tại trên người không
        /// </summary>
        /// <param name="skillID"></param>
        /// <returns></returns>
        public bool HasBuff(int skillID)
        {
            return this.RefObject.Buffs.HasBuff(skillID);
        }

        /// <summary>
        /// Đối tượng còn sống không
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return !this.RefObject.IsDead();
        }

        /// <summary>
        /// Dịch chuyển người chơi sang bản đồ khác
        /// </summary>
        /// <param name="mapID"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <returns></returns>
        public void ChangeScene(int mapID, int posX, int posY, int kuafuSvId = -1)
        {
            /// Nếu đối tượng đã chết thì không chuyển Map được
            if (this.RefObject.IsDead())
            {
                return;
            }

            if (kuafuSvId > 0)
            {
                if (KTGlobal.LastTickTakePlayersBackVillage + 60000 * 30 >= KTGlobal.GetCurrentTimeMilis())
                {
                    PlayerManager.ShowNotification(this.RefObject, "Thời gian hoạt động liên server đã kết thúc. Hãy quay lại sau 1 phút. Xin cảm ơn!");
                    return;
                }    
                if (!ServerConfig.Instance.KuaFuServerEnable)
                {
                    PlayerManager.ShowNotification(this.RefObject, "Server chưa mở liên server");
                    return;
                }    
            }    

            /// Lấy dữ liệu bản đồ đích đến
            if (GameManager.MapMgr.DictMaps.TryGetValue(mapID, out GameMap gameMap))
            {
                /// Nếu bản đồ đích khác bản đồ hiện tại
                if (this.RefObject.CurrentMapCode != mapID)
                {
                    GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, this.RefObject, mapID, posX, posY,
                        (int)this.RefObject.CurrentDir, false, kuafuSvId);
                }
                else
                {
                    GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener,
                        Global._TCPManager.TcpOutPacketPool, this.RefObject, posX, posY,
                        (int)this.RefObject.CurrentDir);
                }
            }
            /// Thông báo bản đồ không tồn tại
            else
            {
                PlayerManager.ShowNotification(this.RefObject, "Bản đồ này chưa được mở!");
            }
        }

        /// <summary>
        /// Thiết lập tọa độ cho đối tượng
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <returns></returns>
        public void SetPos(int posX, int posY)
        {
            /// Nếu đối tượng đã chết thì không chuyển Map được
            if (this.RefObject.IsDead())
            {
                return;
            }

            /// Gửi gói tin thông báo đối tượng thay đổi vị trí
            GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener,
                Global._TCPManager.TcpOutPacketPool, this.RefObject, posX, posY, (int)this.RefObject.CurrentDir);
        }

        /// <summary>
        /// Gửi thông báo ToolTip đến người chơi
        /// </summary>
        /// <param name="message"></param>
        public void AddNotification(string message)
        {
            PlayerManager.ShowNotification(this.RefObject, message);
        }

        /// <summary>
        /// Thiết lập thông tin điểm về thành mặc định khi chết
        /// </summary>
        /// <param name="mapCode"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public void SetDefaultReliveInfo(int mapCode, int posX, int posY)
        {
            KT_TCPHandler.SetDefaultRelivePos(this.RefObject, mapCode, posX, posY);
        }

        /// <summary>
        /// Trả ID bản đồ điểm về thành khi trọng thương
        /// </summary>
        /// <returns></returns>
        public int GetDefaultReliveSceneID()
        {
            KT_TCPHandler.GetPlayerDefaultRelivePos(this.RefObject, out int mapCode, out int posX, out int posY);
            return mapCode;
        }

        /// <summary>
        /// Trả về tọa độ điểm về thành khi trọng thương
        /// </summary>
        /// <returns></returns>
        public Lua_Vector2 GetDefaultRelivePos()
        {
            KT_TCPHandler.GetPlayerDefaultRelivePos(this.RefObject, out int mapCode, out int posX, out int posY);
            return new Lua_Vector2(posX, posY);
        }

        /// <summary>
        /// Dùng kỹ năng tương ứng
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="skillLevel"></param>
        /// <param name="isChildSkill"></param>
        public bool UseSkill(int skillID, int skillLevel, bool isChildSkill = false)
        {
            SkillDataEx skillData = KSkill.GetSkillData(skillID);
            if (skillData == null)
            {
                return false;
            }

            SkillLevelRef skillRef = new SkillLevelRef()
            {
                Data = skillData,
                AddedLevel = skillLevel,
                BonusLevel = 0,
                CanStudy = false,
            };
            KTSkillManager.UseSkillResult
                result = KTSkillManager.UseSkill(this.RefObject, null, skillRef, isChildSkill);
            return result == KTSkillManager.UseSkillResult.Success;
        }

        /// <summary>
        /// Thiết lập trạng thái PK
        /// </summary>
        /// <param name="pkMode"></param>
        public void SetPKMode(int pkMode)
        {
            this.RefObject.PKMode = pkMode;
        }

        /// <summary>
        /// Trả về trạng thái PK
        /// </summary>
        /// <returns></returns>
        public int GetPKMode()
        {
            return this.RefObject.PKMode;
        }

        /// <summary>
        /// Thiết lập Camp
        /// </summary>
        /// <param name="camp"></param>
        public void SetCamp(int camp)
        {
            this.RefObject.Camp = camp;
        }

        /// <summary>
        /// Trả về Camp
        /// </summary>
        /// <returns></returns>
        public int GetCamp()
        {
            return this.RefObject.Camp;
        }

        /// <summary>
        /// Trả về trị PK
        /// </summary>
        /// <returns></returns>
        public int GetPKValue()
        {
            return this.RefObject.PKValue;
        }

        /// <summary>
        /// Thiết lập trị PK
        /// </summary>
        /// <param name="pkValue"></param>
        public void SetPKValue(int pkValue)
        {
            this.RefObject.PKValue = pkValue;
        }

        /// <summary>
        /// Trả về ID bang hội
        /// </summary>
        /// <returns></returns>
        public int GetGuildID()
        {
            return this.RefObject.GuildID;
        }

        /// <summary>
        /// Trả về chức vị trong bang
        /// </summary>
        /// <returns></returns>
        public int GetGuildRank()
        {
            return this.RefObject.GuildRank;
        }

        /// <summary>
        /// Trả về ID tộc
        /// </summary>
        /// <returns></returns>
        public int GetFamilyID()
        {
            return this.RefObject.FamilyID;
        }

        /// <summary>
        /// Trả về chức vị trong tộc
        /// </summary>
        /// <returns></returns>
        public int GetFamilyRank()
        {
            return this.RefObject.FamilyRank;
        }

        /// <summary>
        /// Trả về số uy danh hiện có
        /// </summary>
        /// <returns></returns>
        public int GetPrestige()
        {
            return this.RefObject.Prestige;
        }

        /// <summary>
        /// Thiết lập số uy danh hiện có
        /// </summary>
        /// <param name="value"></param>
        public void SetPrestige(int value)
        {
            this.RefObject.Prestige = value;
        }

        /// <summary>
        /// Reset số lượt đi phụ bản trong ngày
        /// </summary>
        /// <param name="eventID"></param>
        public void ResetCopySceneEnterTimes(int eventID)
        {
            CopySceneEventManager.SetCopySceneTotalEnterTimesToday(this.RefObject, (AcitvityRecore)eventID, 0);
        }

        /// <summary>
        /// Trả về số lượt quay chúc phúc trong ngày
        /// </summary>
        /// <returns></returns>
        public int GetPrayTimes()
        {
            return KTPlayerPrayManager.GetTotalTurnLeft(this.RefObject);
        }

        /// <summary>
        /// Thiết lập số lượt quay chúc phúc trong ngày
        /// </summary>
        /// <param name="value"></param>
        public void SetPrayTimes(int value)
        {
            KTPlayerPrayManager.SetTotalTurnLeft(this.RefObject, value);
        }

        /// <summary>
        /// Thiết lập kinh nghiệm Tu Luyện Châu
        /// </summary>
        /// <param name="exp"></param>
        public void SetXiuLianZhu_Exp(int exp)
        {
            this.RefObject.XiuLianZhu_Exp = exp;
        }

        /// <summary>
        /// Trả về giá trị Kinh nghiệm Tu luyện châu còn lại
        /// </summary>
        /// <returns></returns>
        public int GetXiuLianZhu_Exp()
        {
            return this.RefObject.XiuLianZhu_Exp;
        }

        /// <summary>
        /// Trả về thời gian Tu luyện còn lại (giờ * 10)
        /// </summary>
        /// <returns></returns>
        public int GetXiuLianZhu_TimeLeft()
        {
            return this.RefObject.XiuLianZhu_TotalTime;
        }

        /// <summary>
        /// Thiết lập thời gian Tu luyện còn lại (giờ * 10)
        /// </summary>
        /// <param name="hour10"></param>
        public void SetXiuLianZhu_TimeLeft(int hour10)
        {
            this.RefObject.XiuLianZhu_TotalTime = hour10;
        }

        /// <summary>
        /// Trả về giá trị kinh nghiệm tối đa có thể Tu Luyện
        /// </summary>
        /// <returns></returns>
        public int GetLimitXiuLianZhu_Exp()
        {
            return ItemXiuLianZhuManager.GetLimitExp(this.RefObject);
        }

        /// <summary>
        /// Trả về ID máy chủ
        /// </summary>
        /// <returns></returns>
        public int GetZoneID()
        {
            return this.RefObject.ZoneID;
        }

        /// <summary>
        /// Trả về số giờ (*10) Tu Luyện Châu có được thêm mỗi ngày
        /// </summary>
        /// <returns></returns>
        public int GetXiuLianZhu_TimeAddedPerDay()
        {
            return ItemXiuLianZhuManager.GetHourAddPerDay() * 10;
        }

        /// <summary>
        /// Trả về giá trị kinh nghiệm Tu Luyện có được mỗi giờ
        /// </summary>
        /// <param name="hour10"></param>
        /// <returns></returns>
        public int GetXiuLianZhu_ExpAddedByHour(int hour10)
        {
            return ItemXiuLianZhuManager.GetAddedExpByHour(this.RefObject, hour10);
        }

        /// <summary>
        /// Kiểm tra bản thân có nhóm không
        /// </summary>
        /// <returns></returns>
        public bool HasTeam()
        {
            return this.RefObject.TeamID != -1 && KTTeamManager.IsTeamExist(this.RefObject.TeamID);
        }

        /// <summary>
        /// Trả về số thành viên trong nhóm
        /// </summary>
        /// <returns></returns>
        public int GetTeamSize()
        {
            return KTTeamManager.GetTeamSize(this.RefObject.TeamID);
        }

        /// <summary>
        /// Kiểm tra bản thân có phải trưởng nhóm không
        /// </summary>
        /// <returns></returns>
        public bool IsTeamLeader()
        {
            return this.RefObject.TeamID != -1 && KTTeamManager.IsTeamExist(this.RefObject.TeamID) &&
                   this.RefObject.TeamLeader == this.RefObject;
        }

        public Lua_Player GetTeammateInPair()
        {
            if (this.GetTeamSize() != 2)
            {
                return null;
            }

            var other = RefObject.Teammates.First(x => x.RoleID != RefObject.RoleID);
            return new Lua_Player()
            {
                RefObject = other,
                CurrentScene = this.CurrentScene
            };
        }

        public int GetTeamDistanceInPair()
        {
            if (this.GetTeamSize() != 2)
            {
                return -1;
            }

            var other = RefObject.Teammates.First(x => x.RoleID != RefObject.RoleID);
            if (RefObject.CurrentMapCode != other.CurrentMapCode)
            {
                return -1;
            }

            return (int)KTGlobal.GetDistanceBetweenPlayers(RefObject, other);
        }

        public bool IsGM()
        {
            return KTGMCommandManager.IsGM(this.RefObject);
        }

        public void TestStartSeashellCircle()
        {
            if (IsGM())
                KTSeashellCircleManager.StartTurn(RefObject, 2, out _, out _, out _, out _);
        }

        public IDictionary<int, Lua_Item> GetItemsWithBagIndex(IDictionary<int, int> itemIDsToGet)
        {
            var items = RefObject.GoodsDataList.Where(x => itemIDsToGet.Values.Contains(x.GoodsID)).ToList();
            var result = new Dictionary<int, Lua_Item>();
            foreach (var item in items)
            {
                if (item.BagIndex == -1)
                    continue;
                result[item.BagIndex] = new Lua_Item
                {
                    RefObject = item
                };
            }

            if (result.Count == 0)
                return null;
            return result;
        }

        public int GetMantleValue()
        {
            if (RefObject.GoodsDataList != null)
            {
                // GoodsData equips;
                lock (RefObject.GoodsDataList)
                {
                    var mantle =
                        RefObject.GoodsDataList.FirstOrDefault(x =>
                            x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
                    if (mantle == null)
                    {
                        return -1;
                    }

                    var template = ItemManager.GetItemTemplate(mantle.GoodsID);
                    if (template == null)
                    {
                        return -1;
                    }

                    return template.ItemValue;
                }
            }

            return -1;
        }

        public int GetMantleLevel()
        {
            if (RefObject.GoodsDataList != null)
            {
                // GoodsData equips;
                lock (RefObject.GoodsDataList)
                {
                    var mantle =
                        RefObject.GoodsDataList.FirstOrDefault(x =>
                            x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE);
                    if (mantle == null)
                    {
                        return 0;
                    }

                    var template = ItemManager.GetItemTemplate(mantle.GoodsID);
                    if (template == null)
                    {
                        return 0;
                    }

                    return template.Level;
                }
            }

            return -1;
        }

        public int CanStartCoTrain(int reqLevelFrom, int reqLevelTo, int limitCount)
        {
            if (!CheckSafeArea())
            {
                // không phải bản đồ an toàn
                return -5;
            }

            if (RefObject.FamilyID <= 0)
            {
                // chưa có gia tộc
                return -6;
            }

            if (RefObject.TeamID < 0)
            {
                // chưa có tổ đội
                return -2;
            }

            if (RefObject.Teammates.Count != 2)
            {
                // thành viên tổ đội không hợp lệ
                return -2;
            }

            var srcCount = RefObject.GetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount);
            if (srcCount == -1)
                srcCount = 0;

            if (srcCount >= limitCount)
            {
                return -3;
            }

            var other = RefObject.Teammates.FirstOrDefault(x => x.RoleID != RefObject.RoleID);

            if (RefObject.m_Level <= other.m_Level)
            {
                // level không đạt yêu cầu
                return -7;
            }

            var desCount = other.GetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount);
            if (desCount == -1)
                desCount = 0;

            if (desCount >= limitCount)
            {
                return -10;
            }

            if (RefObject.m_Level < reqLevelFrom || other.m_Level < reqLevelTo)
            {
                return -8;
            }

            if (other.FamilyID != RefObject.FamilyID)
            {
                // thành viên tổ đội không cùng gia tộc
                return -2;
            }

            if (RefObject.CurrentAction != (int)GameServer.KiemThe.Entities.KE_NPC_DOING.do_sit ||
                other.CurrentAction != (int)GameServer.KiemThe.Entities.KE_NPC_DOING.do_sit)
            {
                // thành viên đang không ở trong trạng thái ngồi
                return -1;
            }

            if (RefObject.CurrentMapCode != other.CurrentMapCode)
            {
                // đang ở khác bản đồ
                return -9;
            }

            var currentDistance = KTGlobal.GetDistanceBetweenPlayers(RefObject, other);
            if (currentDistance > 1000)
            {
                // khoảng cách ở quá xa
                return -9;
            }

            if (RefObject.CurrentProgress != null || other.CurrentProgress != null)
            {
                return -11;
            }

            return 0;
        }

        public void StartCoTrain(int durationSecond, int expTickFrom, int expTickTo)
        {
            var other = RefObject.Teammates.FirstOrDefault(x => x.RoleID != RefObject.RoleID);
            if (other == null)
            {
                return;
            }

            var expBookReward = 3000;
            if (other.m_Level >= 100)
            {
                expBookReward = 10000;
            }
            else if (other.m_Level >= 80)
            {
                expBookReward = 5000;
            }

            // đánh dấu luôn là đã thực hiện truyền công -> tránh trick làm gián đoạn để không bị tính lượt
            var srcCount = RefObject.GetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount);
            if (srcCount == -1)
                srcCount = 0;
            RefObject.SetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount, srcCount + 1);

            var desCount = other.GetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount);
            if (desCount == -1)
                desCount = 0;
            other.SetValueOfDailyRecore((int)RecoreDef.CoTrainDailyCount, desCount + 1);

            // progress cho người truyền công lực, thực hiện logic + exp
            RefObject.CurrentProgress = new KPlayer_Progress()
            {
                Hint = $"Đang truyền công lực cho {other.RoleName}",
                InteruptIfTakeDamage = true,
                StartTick = KTGlobal.GetCurrentTimeMilis(),
                DurationTick = durationSecond * 1000,
                Tick = () =>
                {
                    // check trạng thái của RefObject và other, nếu không thỏa mãn thì dừng truyền công

                    if (RefObject.Teammates.Count != 2)
                    {
                        PlayerManager.ShowNotification(other, "Truyền công bị gián đoạn.");
                        RefObject.CurrentProgress = null;
                        other.CurrentProgress = null; // dừng truyền công
                        return;
                    }

                    foreach (var player in RefObject.Teammates)
                    {
                        if (player.CurrentAction != (int)GameServer.KiemThe.Entities.KE_NPC_DOING.do_sit)
                        {
                            RefObject.CurrentProgress = null;
                            other.CurrentProgress = null; // dừng truyền công
                            return;
                        }
                    }

                    GameManager.ClientMgr.ProcessRoleExperience(RefObject, expTickFrom * RefObject.m_Level, false,
                        false, false,
                        "CO-TRAIN"); //
                    GameManager.ClientMgr.ProcessRoleExperience(other, expTickTo * other.m_Level, false, false, false,
                        "CO-TRAIN");
                },
                Cancel = () =>
                {
                    // truyền công bị gián đoạn, đánh dấu số lượt dùng trong ngày (không cộng điểm mật tịch)
                    PlayerManager.ShowNotification(RefObject, "Truyền công bị gián đoạn.");
                },
                Complete = () =>
                {
                    // truyền công xong, cộng điểm tu luyện mật tịch và đánh dấu số lượt dùng trong ngày
                    // chia ra cộng 200 điểm mỗi lượt để có thể lên level từ từ
                    var times = expBookReward / 200;
                    var remain = expBookReward - expBookReward * 200;
                    for (var i = 0; i < times; ++i)
                    {
                        MonsterDeadHelper.BookProseccExp(RefObject, 200 / 4 * 100, false);
                        MonsterDeadHelper.BookProseccExp(other, 200 / 4 * 100, false);
                    }

                    if (remain > 0)
                    {
                        MonsterDeadHelper.BookProseccExp(RefObject, remain / 4 * 100, false);
                        MonsterDeadHelper.BookProseccExp(other, remain / 4 * 100, false);
                    }

                    PlayerManager.ShowNotification(RefObject,
                        $"Hoàn thành truyền công. Nhận được {expBookReward} điểm tu luyện mật tịch.");
                    PlayerManager.ShowNotification(other,
                        $"Hoàn thành truyền công. Nhận được {expBookReward} điểm tu luyện mật tịch.");
                }
            };

            // progress cho người nhận truyền công, chỉ thực hiện hiển thị, không thực hiện logic
            other.CurrentProgress = new KPlayer_Progress()
            {
                Hint = $"Đang nhận công lực từ {RefObject.RoleName}",
                InteruptIfTakeDamage = true,
                StartTick = KTGlobal.GetCurrentTimeMilis(),
                DurationTick = 180000,
                Tick = () =>
                {
                    // check nếu RefObject bị out thì ngừng truyền công. phải bắt sự kiện này vì khi người truyền công out sẽ không trigger bên progress kia
                    if (other.Teammates.Count != 2)
                    {
                        other.CurrentProgress = null; // dừng truyền công
                    }
                },
                Cancel = () => { PlayerManager.ShowNotification(other, "Truyền công bị gián đoạn."); }
            };
        }

        public int GetMapCode()
        {
            return this.RefObject.CurrentMapCode;
        }

        /// <summary>
        /// lấy ra thời gian tác dụng của dạ minh châu
        /// </summary>
        /// <returns></returns>
        public int GetMultiplyNightPearl()
        {
            int mapID = this.RefObject.CurrentMapCode;

            int numberDMC = EmperorTomb_ActivityScript.GetMultiplyNightPearl(this.RefObject);

            long totalValue = this.RefObject.GetTotalValue() / 10000L;

            if (mapID == 1537 || mapID == 1540)
            {
                if (totalValue >= 3000L)
                {
                    numberDMC += 2;
                }
                else
                {
                    numberDMC += 1;
                }
            }
            else if (mapID == 1538 || mapID == 1541)
            {
                if (totalValue >= 24000L)
                {
                    numberDMC = numberDMC * 2 + 1;
                }
                else if (totalValue >= 3000L)
                {
                    numberDMC += 4;
                }
                else
                {
                    numberDMC += 3;
                }
            }

            return numberDMC;
        }

        public int GetNumberDMCHasUsed()
        {
            var record = GameManager.IsKuaFuServer ? RecoreDef.DaMinhChauKuaFu : RecoreDef.DaMinhChau;
            return this.RefObject.GetValueOfForeverRecore(record);
        }

        public void SetNumberDMCUsed(int number)
        {
            var record = GameManager.IsKuaFuServer ? RecoreDef.DaMinhChauKuaFu : RecoreDef.DaMinhChau;
            this.RefObject.SetValueOfForeverRecore(record, number);
        }

        public void SetMapHasBuff(int mapID)
        {
            EmperorTomb_ActivityScript.mapIDHasBuff[this.RefObject.RoleID] = mapID;
            // lưu lại tài phú để check
            long totalValue = this.RefObject.GetTotalValue() / 10000;
            this.RefObject.SetValueOfDailyRecore((int)RecoreDef.SavedTotalValue, (int)totalValue);
        }

        /// <summary>
        /// Đọc tàng bảo đồ
        /// </summary>
        public void ReadTangBaoDo(int durationSecond, int itemID)
        {
            if (!TangBaoDo.IsHaveDataFromItem(this.RefObject, itemID))
            {
                TangBaoDo.GetRandomPosNew(this.RefObject, itemID, true);
                TangBaoDo.CreateDialogFindChest(this.RefObject, itemID);
            }
            else if (TangBaoDo.IsNearToFindChest(this.RefObject, itemID))
            {
                TangBaoDo.CreateProgessFindChest(this.RefObject, itemID);
            }
            else
            {
                TangBaoDo.CreateDialogFindChest(this.RefObject, itemID);
            }
            ///// Tạo Dialog để có bước đọc TBĐ như PC nhưng bỏ ko cần thiết
            //KNPCDialog dialog = new KNPCDialog();
            //dialog.Text = "Đây là Tàng bản đồ của Quân Mã Trương, bạn phải phân biệt được Tàng bản đồ mới thấy được kho báu";
            //dialog.Selections = new Dictionary<int, string>()
            //                    {
            //                        { 1, "Bắt đầu đọc"},
            //                        { 1000,"Để ta suy nghĩ thêm" }
            //                    };
            //dialog.OnSelect = (data) =>
            //{
            //    if (data.SelectID == 1)
            //    {
            //        KT_TCPHandler.CloseDialog(this.RefObject);
            //        KTNPCDialogManager.RemoveNPCDialog(dialog);
            //        TangBaoDo.GetRandomPosNew(this.RefObject, itemID, true);
            //        TangBaoDo.CreateDialogFindChest(this.RefObject, itemID);
            //    }
            //    else if (data.SelectID == 1000)
            //    {
            //        KT_TCPHandler.CloseDialog(this.RefObject);
            //        KTNPCDialogManager.RemoveNPCDialog(dialog);
            //    }
            //};

            //dialog.ShowDialog(this.RefObject);
            //this.RefObject.CurrentProgress = new KPlayer_Progress()
            //{
            //    Hint = $"Đang đọc bản đồ",
            //    InteruptIfTakeDamage = true,
            //    StartTick = KTGlobal.GetCurrentTimeMilis(),
            //    DurationTick = durationSecond * 1000,
            //    Tick = () =>
            //    {
            //        if (!TangBaoDo.IsHaveDataFromItem(this.RefObject, itemID))
            //        {
            //            TangBaoDo.GetRandomPosNew(this.RefObject, itemID);
            //        }
            //    },
            //    Complete = () =>
            //    {
            //        if (!TangBaoDo.IsHaveDataFromItem(this.RefObject, itemID))
            //        {
            //            TangBaoDo.GetRandomPosNew(this.RefObject, itemID, true);
            //        }
            //        TangBaoDo.CreateDialogFindChest(this.RefObject, itemID);
            //    },
            //};
        }

        public int CheckAwardTicket()
        {
            return LotteryManager.CheckAwardTicket(this.RefObject);
        }

        public int GetTicketWin()
        {
            return LotteryManager.GetTicketWin(this.RefObject);
        }

        public string GetTickedUsed(int date)
        {
            return LotteryManager.GetTickedUsed(this.RefObject, date);
        }

        /// <summary>
        /// Sử dụng item bắt pet
        /// </summary>
        public bool CanHaveNewPet()
        {
            return PetManager.CanHaveNewPet(this.RefObject);
        }

        /// <summary>
        /// Sử dụng item bắt pet
        /// </summary>
        public void UsedItemtakePet(int item, int IdItem)
        {
            PetManager.UsedItemTakePet(this.RefObject, item, IdItem);
        }

        /// <summary>
        /// Sử dụng item tinh phách tán tăng điểm TM
        /// </summary>
        public bool UpdateThanMatPet(int itemID, int itemLevel)
        {
            if (PetItemManager.AddThanMatAllPetItem(this.RefObject, itemLevel))
            {
                ItemManager.RemoveItem(this.RefObject, itemID);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UseBookExpPet(int itemID)
        {
            PetItemManager.UseBookItemExp(this.RefObject, itemID);
        }

        public void UseMTPet(int itemID)
        {
            PetItemManager.UseMTItem(this.RefObject, itemID);
        }

        public void UseTTKItem(int itemID)
        {
            PetItemManager.UseTTKItem(this.RefObject, itemID);
        }

        public void UseThuTienCuDongHanh(int itemID)
        {
            PetItemManager.UseThuDongHanhItem(this.RefObject, itemID);
        }

        public void UseBoDeQua(int itemID)
        {
            PetItemManager.UseBoDeQua(this.RefObject, itemID);
        }

        public void UseBinhTinhHoa(int itemID)
        {
            PetItemManager.UseBinhTinhHoa(this.RefObject, itemID);
        }

        public string GetInfoPet(int itemID)
        {
            return PetItemManager.GetInfoPetInItem(this.RefObject, itemID);
        }

        public bool UsePetIcon(int itemID)
        {
            return PetItemManager.InitPetFromData(this.RefObject, itemID);
        }

        public void ShowDialogRemovePetNPC()
        {
            PetManager.ShowDialogRemovePetNPC(this.RefObject);
        }

        public void ShowDialogUnRemovePetNPC()
        {
            PetManager.ShowDialogUnRemovePetNPC(this.RefObject);
        }

        public IDictionary<int, int> GetDailyChargeRecord()
        {
            return KTGlobal.GetPlayerTodayChargeRecord(this.RefObject, out _);
        }

        public int GetMakePoint() 
        {
            return RefObject.GetMakePoint();
        }       

        public int GetGatherPoint() 
        {
            return RefObject.GetGatherPoint();
        }

        public void ClearBag()
        {
            List<GoodsData> itemGDs;
            lock (this.RefObject.GoodsDataList)
            {
                itemGDs = this.RefObject.GoodsDataList.Where(x => x.Using < 0 && !x.IsPet()).ToList();
            }

            PlayerManager.ResolveRemoveItems(this.RefObject, itemGDs, true);
        }

        public int GetCreatedZoneID()
        {
            return this.RefObject.CreatedZoneID;
        }

        public bool CanProcessDowngradeLevel(int level)
        {
            // cấp độ ban đầu không được lớn hơn 100
            if (RefObject.m_Level > 100 && !KTGMCommandManager.IsGM(this.RefObject))
            {
                PlayerManager.ShowMessageBox(RefObject, "Thông báo", "Vật phẩm không thể sử dụng nếu cấp độ vượt quá 100!");
                return false;
            }
            // phải ở khu vực an toàn
            if (!CheckSafeArea())
            {
                PlayerManager.ShowMessageBox(RefObject, "Thông báo", "Bằng hữu hãy di chuyển tới các bản đồ an toàn (thôn, thành thị, môn phái) để sử dụng vật phẩm.");
                return false;
            }
            // không được đeo các trang bị có yêu cầu level cao hơn
            if (!RefObject.GetPlayEquipBody().IsEquipCompatibleToLevel(level))
            {
                PlayerManager.ShowMessageBox(RefObject, "Thông báo", $"Bằng hữu hãy tháo các trang bị chính/phụ không phù hợp với cấp {level} trước khi tiến hành.");
                return false;
            }

            return true;
        }

        public void SaveRoleParamsInt32ValueToDB(string paramName, int value, bool forceWrite)
        {
            Global.SaveRoleParamsInt32ValueToDB(this.RefObject, paramName, value, forceWrite);
        }

        public int GetRoleParamsInt32FromDB(string paramName, int defaultValue) 
        {
            return Global.GetRoleParamsInt32FromDB(this.RefObject, paramName, defaultValue);
        }

        public string GetRoleParamsStringWithDB(string paramName) 
        {
            return Global.GetRoleParamsStringWithDB(this.RefObject, paramName);
        }

        public void SaveRoleParamsStringToDB(string paramName, string value) 
        {
            Global.SaveRoleParamsStringToDB(this.RefObject, paramName, value, true);
        }

        public string GetRoleParamsDecodedStringWithDB(string paramName) 
        {
            var encodedString = Global.GetRoleParamsStringWithDB(this.RefObject, paramName);
            var decode64 = DataHelper.DecodeBase64(encodedString);
            return decode64;
        }

        public void SaveRoleParamsEncodedStringToDB(string paramName, string value) 
        {
            var encode64 = DataHelper.EncodeBase64(value);
            Global.SaveRoleParamsStringToDB(this.RefObject, paramName, encode64, true);
        }

        public void SaveBookUseCount(int type, int count)
        {
            int key = type == 1 ?
                (int)MonthRecoreDef.SkillBoundBookUsed : (int)MonthRecoreDef.PotentialBookUsed;

            int current = this.RefObject.GetValueOfMonthRecore(key) > 0 ? this.RefObject.GetValueOfMonthRecore(key) : 0;

            this.RefObject.SetValueOfMonthRecore(key, current + count);
        }

        public int GetBookUseCount(int type)
        {
            int key = type == 1 ?
                (int)MonthRecoreDef.SkillBoundBookUsed : (int)MonthRecoreDef.PotentialBookUsed;

            return this.RefObject.GetValueOfMonthRecore(key) > 0 ? this.RefObject.GetValueOfMonthRecore(key) : 0;
        }

        public void FixPetOut10Skill()
        {
            // lay danh sach pet cua nguoi choi
            var listPet = this.RefObject.GoodsDataList.Where(x => x.IsPet() && PetManager.GetNumberSkill(x) > 10).ToList();
            // ha cap xuong
            if (listPet == null || listPet.Count == 0)
            {
                PlayerManager.ShowNotification(this.RefObject, $"Bạn không có pet để reset");
            }
            else
            {
                foreach (var item in listPet)
                {
                    if (item.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
                    {
                        string[] fields = levelSkill.Split('|');
                        var newLevelSkill = "";
                        for (var i = 0; i < 10; i++)
                        {
                            newLevelSkill += fields[i] + "|";
                        }
                        // xoa ki tu cuoi
                        newLevelSkill = newLevelSkill.TrimEnd('|');
                        //luu lai thong tin pet
                        Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                        TotalUpdate.Add(UPDATEITEM.ROLEID, this.RefObject.RoleID);
                        TotalUpdate.Add(UPDATEITEM.ITEMDBID, item.Id);

                        Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                        if (item.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1))
                        {
                            OtherParams.Add(ItemPramenter.Pram_1, param1);
                        }
                        if (item.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                        {
                            OtherParams.Add(ItemPramenter.Pram_3, param3);
                        }
                        OtherParams.Add(ItemPramenter.Pram_2, newLevelSkill);

                        if (item.Creator != "")
                        {
                            OtherParams.Add(ItemPramenter.Creator, item.Creator);
                        }

                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                        if (!ItemManager.UpdateItemPrammenter(TotalUpdate, item, this.RefObject, "PET", true))
                        {
                            string cmd = $"Người chơi: {this.RefObject.RoleName} đặt lại các chỉ số pet {item.Id} thất bại";
                            LogManager.WriteLog(LogTypes.Error, cmd);
                        }
                    }
                }
                PlayerManager.ShowNotification(this.RefObject, $"Bạn đã reset thành công {listPet.Count} pet");
            }
        }

        public int CheckChangeServer()
        {
            // level
            //if (this.RefObject.m_Level < 90)
            //    return -1;
            // gia toc
            if (this.RefObject.FamilyID > 0)
                return -2;
            // bang hoi
            if (this.RefObject.GuildID > 0)
                return -3;
            return 1;
        }

        public void ChangeServer(int serverNewID)
        {
            var recordItem = this.RefObject.GetValueOfForeverRecore(25317);
            var itemNeed = 3;
            if (recordItem < 0)
                recordItem = 0;
            if (recordItem == 0)
                itemNeed = 1;
            else if (recordItem == 1)
                itemNeed = 2;
            else
                itemNeed = 3;
            PlayerManager.ShowMessageBox(this.RefObject, "Cảnh báo", $"Toàn bộ vật phẩm không khóa và bạc không khóa sẽ bị xóa bỏ khi chuyển server. Bạn đã chuyển {recordItem} lần trước đó, lần này cần {itemNeed} thẻ đăng ký chuyển Server, bạn có chắc chắn muốn tiếp tục chuyển đến Server {serverNewID}?", () =>
            {
                // check xem du item khong
                var listItem = this.RefObject.GoodsDataList.Where(x => x.GoodsID == 25317).ToList();
                var itemHas = listItem.Sum(x => x.GCount);
                
                if (itemNeed > itemHas)
                {
                    PlayerManager.ShowNotification(this.RefObject, "Không đủ thẻ đổi server, cần " + itemNeed + " thẻ đăng ký chuyển Server");
                    return;
                }
                // lưu vào record
                this.RefObject.SetValueOfForeverRecore(25317, recordItem + 1);

                // lưu lại bản ghi của thằng này
                //Global.SaveRoleParamsStringToDB(this.RefObject, RoleParamName.ForeverRecore, this.RefObject.ForeverRecoreString, true);
                KTLogic.UpdateRoleParamsInfo(this.RefObject);
                // gọi về db để thực hiện trên DB
                ProcessChangeServer(this.RefObject, serverNewID, (_serverNewID) =>
                {
                    // trừ thẻ đổi server
                    ItemManager.RemoveItemFromBag(this.RefObject, 25317, itemNeed, -1, "CHANGESERVER");
                    // bắn noti về lark thông báo ng chơi đổi sang server mới thành công
                    if (GameManager.IsLiveServer)
                        Tools.LarkNotificationHelper.PushNormalNotification("[" + GameManager.ServerLineID + "] Player " + this.RefObject.RoleName + " [" + this.RefObject.RoleID + "] change zoneID = " + _serverNewID);
                    // cho mất kết nối bắt kết nỗi lại
                    if (-1 != this.RefObject.RoleID)
                    {
                        KPlayer otherClient = GameManager.ClientMgr.FindClient(this.RefObject.RoleID);
                        if (null != otherClient)
                        {
                            Global.ForceCloseClient(otherClient, "GMKICK");
                        }
                    }

                    PlayerManager.ShowNotification(this.RefObject, "Kick người chơi thành công");

                    string strcmd = string.Format("{0}", serverNewID);
                    string[] dbFields = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_GM_CLEAR_CACHE_USER, strcmd,
                        GameManager.LocalServerId);
                    if (null == dbFields)
                    {
                        PlayerManager.ShowNotification(this.RefObject, "Xóa cache người chơi lỗi");
                    }
                    else
                    {
                        PlayerManager.ShowNotification(this.RefObject,
                            string.Format("Xóa cache người chơi: {0}", dbFields[1]));
                    }

                    GameManager.logDBCmdMgr.WriteLogDBForPlayer(this.RefObject,
                        "CHANGESERVER", "", "" + GameManager.LocalServerId, "" + _serverNewID, "",
                        "", "", "", "");
                });
            }, () =>
            {
                PlayerManager.ShowNotification(this.RefObject, "Bạn đã hủy chuyển đổi server");
                return;
            });
            

        }

        private void DoChangeServer(KPlayer player, int serverNewID, Action<int> onSuccess)
        {
            try
            {
                var cmd = string.Format("{0}:{1}:{2}:{3}", this.RefObject.strUserID, this.RefObject.ZoneID, this.RefObject.RoleID, serverNewID);
                // Kết quả đổi server
                string[] fields = Global.SendToDB((int)TCPGameServerCmds.CMD_CHANGE_SERVER, cmd, GameManager.LocalServerId);
                // Nếu có kết quả trả về
                if (fields != null && fields.Length == 2)
                {
                    // Mã kết quả
                    int ret = int.Parse(fields[0]);
                    // Tên cũ
                    string oldName = fields[1];

                    // Nếu tên không hợp lệ
                    if (ret == 2)
                    {
                        // xét lại record chuyển của thằng này
                        var recordItem = player.GetValueOfForeverRecore(25317);
                        player.SetValueOfForeverRecore(25317, recordItem - 1);
                        PlayerManager.ShowNotification(player, "Không thể tìm thấy nhân vật");
                    }
                    else if(ret == 3)
                    {
                        // xét lại record chuyển của thằng này
                        var recordItem = player.GetValueOfForeverRecore(25317);
                        player.SetValueOfForeverRecore(25317, recordItem - 1);
                        PlayerManager.ShowNotification(player, "Server đã đủ 4 nhân vật, không thể chuyển");
                    }
                    // Nếu tên không hợp lệ
                    else if (ret == 4)
                    {
                        // xét lại record chuyển của thằng này
                        var recordItem = player.GetValueOfForeverRecore(25317);
                        player.SetValueOfForeverRecore(25317, recordItem - 1);
                        PlayerManager.ShowNotification(player, "Lỗi hệ thống, hãy thử lại sau!");
                    }
                    // Nếu thành công
                    else if (ret == 0)
                    {
                        PlayerManager.ShowNotification(player, "Đổi server thành công!");
                        onSuccess?.Invoke(serverNewID);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
            
        }

        private void ProcessChangeServer(KPlayer player, int serverNewID, Action<int> onSuccess)
        {
            // Toác
            if (player == null)
            {
                return;
            }

            // Nếu là liên máy chủ
            if (player.ClientSocket.IsKuaFuLogin)
            {
                PlayerManager.ShowNotification(player, "Ở liên máy chủ không cho phép đổi tên!");
                return;
            }

            DoChangeServer(player, serverNewID, onSuccess);
        }
        
        public void TitlesToItems()
        {
            this.RefObject.ConvertTitlesToItems();
        }
    }
}
