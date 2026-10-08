using GameServer.Core.Executor;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameServer.KiemThe.GameDbController;
using GameServer.KiemThe.Core.Task;
using System.Reflection;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.CopySceneEvents;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.GameEvents.Medal;

namespace GameServer.KiemThe.Core.Item
{
    /// <summary>
    /// Class xử lý việc tháo đồ mặc đồ của Player
    /// </summary>
    public class KPlayerEquipBody
    {
        /// <summary>
        /// Lượng máu trước khi Detach
        /// </summary>
        private int hpBeforeDetach = 0;

        /// <summary>
        /// Lượng khí trước khi Detach
        /// </summary>
        private int mpBeforeDetach = 0;

        /// <summary>
        /// Lượng thể lực trước khi Detach
        /// </summary>
        private int staminaBeforeDetach = 0;

        private int lastDisplayOfficeRank = 0;

        public KPlayer _Player { get; set; }

        public List<KMagicAttrib> SuitUpActive = new List<KMagicAttrib>();

        /// <summary>
        /// Set đang mặc
        /// </summary>
        public Dictionary<KE_EQUIP_POSITION, KItem> EquipBody = new Dictionary<KE_EQUIP_POSITION, KItem>();

        /// <summary>
        /// Set dự phòng
        /// </summary>
        public Dictionary<KE_EQUIP_POSITION_SUB, KItem> PreventiveEquipBody =
            new Dictionary<KE_EQUIP_POSITION_SUB, KItem>();

        public KPlayerEquipBody(KPlayer player)
        {
            this._Player = player;
        }

        /// <summary>
        ///  Làm giảm độ bền của 1 đồ bất kỳ trên người
        /// </summary>
        /// <param name="nPercent"></param>
        public void AbradeInDeath(int nPercent)
        {
            int MaxBugItem = 4;

            if (nPercent <= 0)
                return;

            if (nPercent >= 100)
                nPercent = 100;
            else
                nPercent = nPercent % 100;

            if (this._Player.m_nCurLucky > 20)
            {
                MaxBugItem = 1;
            }

            for (int i = 0; i < (int)KE_EQUIP_POSITION.emEQUIPPOS_NUM; ++i)
            {
                EquipBody.TryGetValue((KE_EQUIP_POSITION)i, out KItem ItemOut);

                if (ItemOut == null)
                {
                    continue;
                }

                if ((KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_HORSE ||
                    (KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_MANTLE ||
                    (KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_ZHEN ||
                    (KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_SIGNET ||
                    (KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_BOOK ||
                    (KE_EQUIP_POSITION)i == KE_EQUIP_POSITION.emEQUIPPOS_CHOP)
                {
                    continue;
                }

                if (ItemOut._GoodDatas.Strong < 50)
                {
                    PlayerManager.ShowNotification(this._Player,
                        "Độ bền trang bị [" + ItemOut._ItemData.Name +
                        "] đang quá thấp vui lòng sửa chữa trước khi hỏng trang bị");
                }

                MaxBugItem--;
                if (MaxBugItem > 0)
                {
                    ItemOut.AbradeInDeath(nPercent, this._Player);
                }
            }
        }

        /// <summary>
        /// Hàm swap lại vị trí đồ
        /// </summary>
        public void SwapSet()
        {
            lock (PreventiveEquipBody)
            lock (EquipBody)
            {
                // xóa các thuộc tính của bộ chính
                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from SwapSet() in KPlayerEquipBody.cs");
                ClearAllEffecEquipBody();

                for (var key = 0; key < (int)KE_EQUIP_POSITION.emEQUIPPOS_HORSE; ++key)
                {
                    var mainEquipKey = (KE_EQUIP_POSITION)key;
                    var subEquipKey = (KE_EQUIP_POSITION_SUB)key + 100;
                    var haveMainEquip = EquipBody.TryGetValue(mainEquipKey, out var mainEquip);
                    var haveSubEquip = PreventiveEquipBody.TryGetValue(subEquipKey, out var subEquip);

                    if (haveMainEquip)
                    {
                        PreventiveEquipBody[subEquipKey] = mainEquip;
                            if (mainEquip.SkillsArtifact != null)
                            {
                                // xóa skill của trang bị ra khỏi ng khỏi hiển thị
                                foreach (var skillID in mainEquip.SkillsArtifact)
                                {
                                    this._Player.Skills.RemoveSkill(skillID);
                                }

                                if (this._Player.artifactInfos != null)
                                {
                                    var itemRemove = this._Player.artifactInfos.SingleOrDefault(x => x.GoodID == mainEquip._GoodDatas.GoodsID);
                                    if (itemRemove != null)
                                        this._Player.artifactInfos.Remove(itemRemove);
                                }
                            }
                        }
                    else
                    {
                        PreventiveEquipBody.Remove(subEquipKey);
                    }

                    if (haveSubEquip)
                    {
                        EquipBody[mainEquipKey] = subEquip;
                            if (subEquip.SkillsArtifact != null)
                            {
                                //kiểm tra xem còn hạn ko, nếu ko thì xóa ở nv(nếu có)
                                if (subEquip._GoodDatas.OtherParams != null)
                                {
                                    if (subEquip._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var level))
                                    {
                                        var param3 = level.Split('|');
                                        if (param3.Length >= 2 && int.TryParse(param3[0], out var skillLevel) && TryParseArtifactEndTime(param3[1], out var endtime))
                                        {
                                            if (endtime < DateTime.Now)
                                            {
                                                if (this._Player.artifactInfos != null)
                                                {
                                                    var itemRemove = this._Player.artifactInfos.SingleOrDefault(x => x.GoodID == subEquip._GoodDatas.GoodsID);
                                                    if (itemRemove != null)
                                                        this._Player.artifactInfos.Remove(itemRemove);
                                                }
                                            }
                                            else
                                            {
                                                foreach (var skillID in subEquip.SkillsArtifact)
                                                {
                                                    if (!this._Player.Skills.HasSkill(skillID))
                                                    {
                                                        this._Player.Skills.AddSkillWithLevel(skillID, skillLevel);
                                                    }
                                                    else
                                                    {
                                                        this._Player.Skills.ResetSkillLevel(skillID);
                                                        this._Player.Skills.AddSkillLevel(skillID, skillLevel);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (this._Player.artifactInfos != null)
                                            {
                                                var itemRemove = this._Player.artifactInfos.SingleOrDefault(x => x.GoodID == subEquip._GoodDatas.GoodsID);
                                                if (itemRemove != null)
                                                    this._Player.artifactInfos.Remove(itemRemove);
                                            }
                                        }
                                    }
                                }                                       
                            }    
                    }
                    else
                    {
                        EquipBody.Remove(mainEquipKey);
                    }
                }

                foreach (var item in EquipBody)
                {
                    UpdatePosition(item.Value._GoodDatas, (int)item.Key);
                }

                foreach (var item in PreventiveEquipBody)
                {
                    UpdatePosition(item.Value._GoodDatas, (int)item.Key);
                }

                //ATTACK LẠI CHỈ SỐ TRANG BỊ
                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from SwapSet() in KPlayerEquipBody.cs");
                AttackAllEquipBody();
            }
        }

        private static bool TryParseArtifactEndTime(string raw, out DateTime endTime)
        {
            raw = raw?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                endTime = default;
                return false;
            }

            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "dd-MM-yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "M/d/yyyy H:mm:ss",
                "M/d/yyyy HH:mm:ss",
            };

            return DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out endTime)
                   || DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces, out endTime)
                   || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out endTime);
        }

        /// <summary>
        /// Hàm update lại vị trí nếu thích dùng
        /// </summary>
        /// <param name="goodsData"></param>
        /// <param name="Postion"></param>
        public void UpdatePosition(GoodsData goodsData, int Postion)
        {
            string[] dbFields = null;

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
            // Update lại vị trí cho vật phẩm hiện tại
            TotalUpdate.Add(UPDATEITEM.BAGINDEX, -1);
            //Set lại giá tri cho việc không sử dụng nữa
            TotalUpdate.Add(UPDATEITEM.USING, Postion);
            if (ItemManager.IsMedal(goodsData))
            {
                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, goodsData.OtherParams);
            }

            string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);

            TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields, this._Player.ServerId);

            if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
            {
            }
            else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
            {
                KTGlobal.ModifyGoods(this._Player, goodsData.Id, TotalUpdate);

                // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên

                SCModGoods scData = new SCModGoods()
                {
                    BagIndex = -1,
                    Count = goodsData.GCount,
                    IsUsing = Postion,
                    ModType = (int)ModGoodsTypes.EquipLoad,
                    ID = goodsData.Id,
                    NewHint = 0,
                    Site = 0,
                    State = 0,
                };
                // Gửi thông báo về cho việc mặc đồ thành công
                this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
            }
        }

        /// <summary>
        /// Hàm tháo đồ
        /// </summary>
        public bool EquipUnloadMain(GoodsData goodsData, int isUsing)
        {
            KItem ItemFind = new KItem(goodsData);

            int Postion = 0;
            if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
            {
                Postion = goodsData.Using;

                int checkResult = KTGlobal.CheckCanUnLoadExtBag(this._Player, ItemFind);
                if (checkResult == -1)
                {
                    string strMsg = string.Format("Cần để trống {0} ô cuối trong túi đồ để tháo túi phụ!",
                        ItemFind.GetBaseItem.ParticularType);
                    PlayerManager.ShowNotification(this._Player, strMsg);
                    return false;
                }
                else if (checkResult == -2)
                {
                    PlayerManager.ShowNotification(this._Player, "Túi đồ không đủ chỗ trống, không thể tháo trang bị!");
                    return false;
                }
            }
            else
            {
                Postion = ItemManager.g_anEquipPos[ItemFind.GetBaseItem.DetailType];
            }

            string[] dbFields = null;

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            // Check xem có đủ chỗ trống trong túi đồ không
            int BagIndex = KTGlobal.GetIdleSlotOfBagGoods(this._Player, goodsData.Site);

            if (Postion == 20)
            {
                // nếu chưa đến thời gian thay đổi thì tko cho thay đổi
                if (KTGlobal.GetCurrentTimeMilis() - this._Player.LastChoosePet < 5000)
                {
                    PlayerManager.ShowNotification(this._Player, "Thao tác quá nhanh!");
                    return false;
                }

                this._Player.LastChoosePet = KTGlobal.GetCurrentTimeMilis();
                BagIndex = -1;
            }


            int maxBagNum = (goodsData.Site == (int)SiteItemType.kuafubalancebag) ? 50 : this._Player.BagNum;
            if ((BagIndex < 0 || BagIndex >= maxBagNum) && Postion != 20)
            {
                PlayerManager.ShowNotification(this._Player, "Túi đồ không đủ chỗ trống,không thể tháo trang bị");
                return false;
            }

            // Kiểm tra xem có vị trí đấy không
            if (EquipBody.ContainsKey((KE_EQUIP_POSITION)Postion))
            {
                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = EquipBody[(KE_EQUIP_POSITION)Postion];

                if (ItemFind.ItemDBID != _ItemFromDic.ItemDBID)
                {
                    PlayerManager.ShowNotification(this._Player, "Trang bị muốn tháo bị sai CSDL");

                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + ItemFind.ItemDBID + "][" + ItemFind.GetBaseItem.ItemID +
                        "] : Vật phẩm gửi lên không chính xác! ");

                    return false;
                }
                else
                {
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);

                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id);
                    // Update lại vị trí cho vật phẩm hiện tại
                    TotalUpdate.Add(UPDATEITEM.BAGINDEX, BagIndex);
                    //Set lại giá tri cho việc không sử dụng nữa
                    TotalUpdate.Add(UPDATEITEM.USING, -1);

                    // nếu là chân nguyên
                    if (KE_EQUIP_POSITION.emEQUIPPOS_CHANNGUYEN == (KE_EQUIP_POSITION)Postion ||
                        KE_EQUIP_POSITION.emEQUIPPOS_MEDAL == (KE_EQUIP_POSITION)Postion)
                    {
                        TotalUpdate.Add(UPDATEITEM.PROPS, _ItemFromDic._GoodDatas.Props);
                        TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, _ItemFromDic._GoodDatas.Forge_level);
                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, _ItemFromDic._GoodDatas.OtherParams);
                        TotalUpdate.Add(UPDATEITEM.BINDING, _ItemFromDic._GoodDatas.Binding);
                    }


                    string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                        ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Bắt đầu tháo trang bị! ");
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                        this._Player.ServerId);

                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        PlayerManager.ShowNotification(this._Player, "Máy chủ bận vui lòng thử lại sau!");

                        LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : tháo trang bị lỗi! ");
                        return false;
                    }
                    else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Tháo trang bị thành công! ");
                        KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                        SCModGoods scData = new SCModGoods()
                        {
                            BagIndex = BagIndex,
                            Count = _ItemFromDic._GoodDatas.GCount,
                            IsUsing = -1,
                            ModType = (int)ModGoodsTypes.EquipUnload,
                            ID = _ItemFromDic._GoodDatas.Id,
                            NewHint = 0,
                            Site = 0,
                            State = 0,
                        };
                        // Gửi thông báo về là tháo đồ ra thành công
                        this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                    }

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from EquipUnloadMain() in KPlayerEquipBody.cs");
                    this.ClearAllEffecEquipBody();
                    // THỰC HIỆN REMOVE ITEM KHỎI ARAY
                    if (_ItemFromDic.SkillsArtifact != null)
                    {
                        // xóa skill của trang bị ra khỏi ng khỏi hiển thị
                        foreach (var skillID in _ItemFromDic.SkillsArtifact)
                        {
                            this._Player.Skills.RemoveSkill(skillID);
                        }
                        var itemRemove = this._Player.artifactInfos.SingleOrDefault(x => x.GoodID == _ItemFromDic._GoodDatas.GoodsID);
                        if (itemRemove != null)
                            this._Player.artifactInfos.Remove(itemRemove);
                    }

                    lock (EquipBody)
                    {
                        EquipBody.Remove((KE_EQUIP_POSITION)Postion);
                        if (goodsData.IsPet())
                        {
                            // Xóa hết kích hoạt hiệu ứng từ đồ của pet
                            foreach (var pos in ItemManager.g_anEquipPetPos)
                            {
                                if (EquipBody.ContainsKey((KE_EQUIP_POSITION)pos))
                                {
                                    EquipBody.Remove((KE_EQUIP_POSITION)pos);
                                }
                            }
                        }
                    }

                    if (KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura == (KE_EQUIP_POSITION)Postion)
                    {
                        if (ItemManager._TotalGameItem.TryGetValue(goodsData.GoodsID, out ItemData itemData))
                        {
                            if (this._Player.Buffs.HasBuff(itemData.ResMale))
                                this._Player.Buffs.RemoveBuff(itemData.ResMale);
                        }
                    }

                    /// Nếu là ngựa
                    if (KE_EQUIP_POSITION.emEQUIPPOS_HORSE == (KE_EQUIP_POSITION)Postion)
                    {
                        if (this._Player.IsRiding)
                        {
                            /// Chuyển trạng thái sang xuống ngựa
                            this._Player.IsRiding = false;

                            /// Cập nhật thời gian thay trạng thái cưỡi
                            this._Player.LastTickToggleHorseState = KTGlobal.GetCurrentTimeMilis();

                            /// Lưu vào DB
                            Global.SaveRoleParamsInt32ValueToDB(this._Player, RoleParamName.HorseToggleOn,
                                this._Player.IsRiding ? 1 : 0, true);

                            /// Gửi thông báo tới tất cả người chơi xung quanh
                            KT_TCPHandler.SendHorseStateChanged(this._Player);
                        }
                    }

                    //nếu là túi 
                    if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
                    {
                        this._Player.extBagBonus -= ItemFind.GetBaseItem.ParticularType;
                    }

                    // nếu là mật tịch thì lưu lại thông tin trước khi tháo
                    if (KE_EQUIP_POSITION.emEQUIPPOS_BOOK == (KE_EQUIP_POSITION)Postion)
                    {
                        int LevelBook = 0;
                        if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string BookLevel))
                        {
                            LevelBook = Int32.Parse(BookLevel);
                        }

                        if (LevelBook < 100)
                        {
                            ItemData BookData = ItemManager.GetItemTemplate(goodsData.GoodsID);

                            int BoookExp = 0;
                            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string ExpLevel))
                            {
                                BoookExp = Int32.Parse(ExpLevel);
                            }

                            int ExpMax = ItemManager.GetMaxbookEXP(LevelBook, BookData.Level);

                            // LƯU LẠI BOOK

                            Dictionary<UPDATEITEM, object> ItemlUpdate = new Dictionary<UPDATEITEM, object>();

                            ItemlUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                            ItemlUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);

                            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();

                            OtherParams.Add(ItemPramenter.Pram_1, LevelBook + "");
                            OtherParams.Add(ItemPramenter.Pram_2, BoookExp + "");
                            OtherParams.Add(ItemPramenter.Pram_3, ExpMax + "");

                            LogManager.WriteLog(LogTypes.Error,
                                $"Người chơi: {this._Player.RoleName} tháo mật tịch với id = {goodsData.GoodsID}, level = {LevelBook}, exp = {BoookExp}");

                            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, this._Player, "BOOKEXPCHANGE",
                                    true))
                            {
                                string cmd = $"Người chơi: {this._Player.RoleName} lưu mật tịch thất bại";
                                LogManager.WriteLog(LogTypes.Error, cmd);
                            }
                        }
                    }

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from EquipUnloadMain() in KPlayerEquipBody.cs");
                    this.AttackAllEquipBody();
                }

                // THỰC HIỆN DETACK TOÀN BỘ THUỘC TÍNH CỦA ITEM HIỆN TẠI

                // THỰC HIỆN CALL LẠI RECACLU ALL ITEM VÀ ACTIVE HIIDEN PROB()
            }
            // Nếu là item pet thì check xem nếu ko có pet thì vẫn có thể tháo được
            else if (goodsData.IsItemPet())
            {
                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = new KItem(goodsData);

                if (ItemFind.ItemDBID != _ItemFromDic.ItemDBID)
                {
                    PlayerManager.ShowNotification(this._Player, "Trang bị muốn tháo bị sai CSDL");

                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + ItemFind.ItemDBID + "][" + ItemFind.GetBaseItem.ItemID +
                        "] : Vật phẩm gửi lên không chính xác! ");

                    return false;
                }
                else
                {
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);

                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id);
                    // Update lại vị trí cho vật phẩm hiện tại
                    TotalUpdate.Add(UPDATEITEM.BAGINDEX, BagIndex);
                    //Set lại giá tri cho việc không sử dụng nữa
                    TotalUpdate.Add(UPDATEITEM.USING, -1);

                    string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                        ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Bắt đầu tháo trang bị! ");
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                        this._Player.ServerId);

                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        PlayerManager.ShowNotification(this._Player, "Máy chủ bận vui lòng thử lại sau!");

                        LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : tháo trang bị lỗi! ");
                        return false;
                    }
                    else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Tháo trang bị thành công! ");
                        KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                        SCModGoods scData = new SCModGoods()
                        {
                            BagIndex = BagIndex,
                            Count = _ItemFromDic._GoodDatas.GCount,
                            IsUsing = -1,
                            ModType = (int)ModGoodsTypes.EquipUnload,
                            ID = _ItemFromDic._GoodDatas.Id,
                            NewHint = 0,
                            Site = 0,
                            State = 0,
                        };
                        // Gửi thông báo về là tháo đồ ra thành công
                        this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                    }
                }
            }
            else if (goodsData.IsCN() || ItemManager.IsMedal(goodsData))
            {
                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = new KItem(goodsData);

                if (ItemFind.ItemDBID != _ItemFromDic.ItemDBID)
                {
                    PlayerManager.ShowNotification(this._Player, "Trang bị muốn tháo bị sai CSDL");

                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + ItemFind.ItemDBID + "][" + ItemFind.GetBaseItem.ItemID +
                        "] : Vật phẩm gửi lên không chính xác! ");

                    return false;
                }
                else
                {
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);

                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id);
                    // Update lại vị trí cho vật phẩm hiện tại
                    TotalUpdate.Add(UPDATEITEM.BAGINDEX, BagIndex);
                    //Set lại giá tri cho việc không sử dụng nữa
                    TotalUpdate.Add(UPDATEITEM.USING, -1);

                    TotalUpdate.Add(UPDATEITEM.PROPS, _ItemFromDic._GoodDatas.Props);
                    TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, _ItemFromDic._GoodDatas.Forge_level);
                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, _ItemFromDic._GoodDatas.OtherParams);
                    TotalUpdate.Add(UPDATEITEM.BINDING, _ItemFromDic._GoodDatas.Binding);

                    string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                        ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Bắt đầu tháo trang bị! ");
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                        this._Player.ServerId);

                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        PlayerManager.ShowNotification(this._Player, "Máy chủ bận vui lòng thử lại sau!");

                        LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : tháo trang bị lỗi! ");
                        return false;
                    }
                    else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Tháo trang bị thành công! ");
                        KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                        SCModGoods scData = new SCModGoods()
                        {
                            BagIndex = BagIndex,
                            Count = _ItemFromDic._GoodDatas.GCount,
                            IsUsing = -1,
                            ModType = (int)ModGoodsTypes.EquipUnload,
                            ID = _ItemFromDic._GoodDatas.Id,
                            NewHint = 0,
                            Site = 0,
                            State = 0,
                        };
                        // Gửi thông báo về là tháo đồ ra thành công
                        this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                    }

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from EquipUnloadMain() in KPlayerEquipBody.cs");
                    this.ClearAllEffecEquipBody();

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from EquipUnloadMain() in KPlayerEquipBody.cs");
                    this.AttackAllEquipBody();
                }
            }
            else
            {
                PlayerManager.ShowNotification(this._Player, "Trang bị hiện không tồn trại trên người");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tháo đồ khỏi slot phụ
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public bool EquipUnloadSub(GoodsData goodsData)
        {
            KItem ItemFind = new KItem(goodsData);

            if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
            {
                PlayerManager.ShowNotification(this._Player, "Không thể trang bị túi phụ cho bộ phụ");
                return false;
            }

            int dt = ItemFind.GetBaseItem.DetailType;
            if (dt < 0 || dt >= ItemManager.g_anEquipSubPos.Length)
            {
                LogManager.WriteLog(LogTypes.Error, "[EquipUnloadSub] DetailType out of range: " + dt + " goods=" + ItemFind.GetBaseItem.ItemID);
                PlayerManager.ShowNotification(this._Player, "Trang bị không hợp lệ, không thể tháo trang bị phụ");
                return false;
            }
            int Postion = ItemManager.g_anEquipSubPos[dt];

            string[] dbFields = null;

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            // Check xem có đủ chỗ trống trong túi đồ không
            int BagIndex = KTGlobal.GetIdleSlotOfBagGoods(this._Player, goodsData.Site);

            int maxBagNum = (goodsData.Site == (int)SiteItemType.kuafubalancebag) ? 50 : this._Player.BagNum;
            if (BagIndex < 0 || BagIndex >= maxBagNum)
            {
                PlayerManager.ShowNotification(this._Player, "Túi đồ không đủ chỗ trống,không thể tháo trang bị");
                return false;
            }

            // Kiểm tra xem có vị trí đấy không
            if (PreventiveEquipBody.ContainsKey((KE_EQUIP_POSITION_SUB)Postion))
            {
                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = PreventiveEquipBody[(KE_EQUIP_POSITION_SUB)Postion];

                if (ItemFind.ItemDBID != _ItemFromDic.ItemDBID)
                {
                    PlayerManager.ShowNotification(this._Player, "Trang bị muốn tháo bị sai CSDL");

                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + ItemFind.ItemDBID + "][" + ItemFind.GetBaseItem.ItemID +
                        "] : Vật phẩm gửi lên không chính xác! ");

                    return false;
                }
                else
                {
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                        ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Bắt đầu tháo trang bị phụ! ");
                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);

                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id);
                    // Update lại vị trí cho vật phẩm hiện tại
                    TotalUpdate.Add(UPDATEITEM.BAGINDEX, BagIndex);
                    //Set lại giá tri cho việc không sử dụng nữa
                    TotalUpdate.Add(UPDATEITEM.USING, -1);

                    string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);

                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                        this._Player.ServerId);

                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        PlayerManager.ShowNotification(this._Player, "Máy chủ bận vui lòng thử lại sau!");

                        LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex + "] : Tháo trang bị phụ lỗi !");
                        return false;
                    }
                    else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipUnload][" + this._Player.RoleID + "][" + ItemFind.ItemDBID + "][" +
                            ItemFind.GetBaseItem.ItemID + "][BagIndex " + BagIndex +
                            "] : Tháo trang bị phụ thành công !");
                        KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                        SCModGoods scData = new SCModGoods()
                        {
                            BagIndex = BagIndex,
                            Count = _ItemFromDic._GoodDatas.GCount,
                            IsUsing = -1,
                            ModType = (int)ModGoodsTypes.EquipUnload,
                            ID = _ItemFromDic._GoodDatas.Id,
                            NewHint = 0,
                            Site = 0,
                            State = 0,
                        };
                        // Gửi thông báo về là tháo đồ ra thành công
                        this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                    }

                    // Vì là set phụ nên sẽ ko active gì
                    // this.ClearAllEffecEquipBody();
                    // THỰC HIỆN REMOVE ITEM KHỎI ARAY
                    lock (PreventiveEquipBody)
                    {
                        PreventiveEquipBody.Remove((KE_EQUIP_POSITION_SUB)Postion);
                    }

                    /// Nếu là ngựa
                    //if (KE_EQUIP_POSITION.emEQUIPPOS_HORSE == (KE_EQUIP_POSITION)Postion)
                    //{
                    //    if (this._Player.IsRiding)
                    //    {
                    //        /// Chuyển trạng thái sang xuống ngựa
                    //        this._Player.IsRiding = false;

                    //        /// Cập nhật thời gian thay trạng thái cưỡi
                    //        this._Player.LastTickToggleHorseState = KTGlobal.GetCurrentTimeMilis();

                    //        /// Lưu vào DB
                    //        Global.SaveRoleParamsInt32ValueToDB(this._Player, RoleParamName.HorseToggleOn, this._Player.IsRiding ? 1 : 0, true);

                    //        /// Gửi thông báo tới tất cả người chơi xung quanh
                    //        KT_TCPHandler.SendHorseStateChanged(this._Player);
                    //    }

                    //}

                    // VÌ LÀ SET PHỤ LÊN KO KÍCH HOẠT GÌ CHỈ KICSK LẠI HÀM RELOAD LẠI TÀI PHÚ
                    this.ReloadTotalValue();
                }

                // THỰC HIỆN DETACK TOÀN BỘ THUỘC TÍNH CỦA ITEM HIỆN TẠI

                // THỰC HIỆN CALL LẠI RECACLU ALL ITEM VÀ ACTIVE HIIDEN PROB()
            }
            else
            {
                PlayerManager.ShowNotification(this._Player, "Trang bị hiện không tồn trại trên người");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Hàm xử lý mặc đồ vào người
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public bool EquipLoadMain(GoodsData goodsData, int isUsing)
        {
            /// Khóa luôn trang bị vào
            goodsData.Binding = 1;

            KItem ItemFind = new KItem(goodsData);

            // lấy ra vị trí của vật phẩm
            int Postion = 0;
            if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
            {
                if (isUsing < (int)KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_1 ||
                    isUsing > (int)KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_3)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc túi sai vị trí");

                    return false;
                }

                Postion = isUsing;
            }
            else
            {
                Postion = ItemManager.g_anEquipPos[ItemFind.GetBaseItem.DetailType];
            }

            string[] dbFields = null;

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            // Nếu là pet thì kiểm tra xem đủ độ thân mật không
            if (isUsing == 20)
            {
                if (!PetManager.CanUsePet(_Player, goodsData.Id))
                {
                    PlayerManager.ShowNotification(this._Player,
                        "Độ thân mật của bạn với bạn đồng hành không đủ để gọi ra giúp đỡ!");
                    return false;
                }

                // nếu chưa đến thời gian thay đổi thì tko cho thay đổi
                if (KTGlobal.GetCurrentTimeMilis() - this._Player.LastChoosePet < 5000)
                {
                    PlayerManager.ShowNotification(this._Player, "Thao tác quá nhanh!");
                    return false;
                }

                this._Player.LastChoosePet = KTGlobal.GetCurrentTimeMilis();
            }

            // nếu là chân nguyên
            if (isUsing == 32)
            {
                // nếu chưa đến thời gian thay đổi thì tko cho thay đổi
                if (KTGlobal.GetCurrentTimeMilis() - this._Player.lastChooseCn < 5000)
                {
                    PlayerManager.ShowNotification(this._Player, "Thao tác quá nhanh!");
                    return false;
                }

                this._Player.lastChooseCn = KTGlobal.GetCurrentTimeMilis();
            }

            // Trường hợp này chắc chắn sẽ còn chỗ

            /// Kiểm tra xem có đang mặc 1 món đồ khác trên người mà cùng vị trí không
            /// Nếu có thì thực hiện DETACK món đồ cũ trước
            if (EquipBody.ContainsKey((KE_EQUIP_POSITION)Postion))
            {
                //nếu muốn thay túi vào ô đã có rồi thì phải tháo túi cũ ra
                if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
                {
                    PlayerManager.ShowNotification(this._Player, "Muốn thay thế túi phụ, cần tháo túi cũ ra trước!");
                    return false;
                }


                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = EquipBody[(KE_EQUIP_POSITION)Postion];

                // Thực hiện Detack toàn bộ hiệu ứng đang mặc của bộ hiện tại

                int GetBagIndexFormNewItem = goodsData.BagIndex;

                var TotalUpdate = new Dictionary<UPDATEITEM, object>
                {
                    { UPDATEITEM.ROLEID, this._Player.RoleID },
                    { UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id },
                    // Update lại vị trí cho vật phẩm hiện tại
                    { UPDATEITEM.BAGINDEX, GetBagIndexFormNewItem },
                    //Set lại giá tri cho việc không sử dụng nữa
                    { UPDATEITEM.USING, -1 }
                };

                // nếu là chân nguyên
                if (KE_EQUIP_POSITION.emEQUIPPOS_CHANNGUYEN == (KE_EQUIP_POSITION)Postion ||
                    KE_EQUIP_POSITION.emEQUIPPOS_MEDAL == (KE_EQUIP_POSITION)Postion)
                {
                    TotalUpdate.Add(UPDATEITEM.PROPS, _ItemFromDic._GoodDatas.Props);
                    TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, _ItemFromDic._GoodDatas.Forge_level);
                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, _ItemFromDic._GoodDatas.OtherParams);
                    TotalUpdate.Add(UPDATEITEM.BINDING, _ItemFromDic._GoodDatas.Binding);
                }

                // nếu là mật tịch thì lưu lại giá trị trước khi tháo
                if (KE_EQUIP_POSITION.emEQUIPPOS_BOOK == (KE_EQUIP_POSITION)Postion)
                {
                    var LevelBook = 0;
                    if (_ItemFromDic._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var BookLevel))
                    {
                        LevelBook = int.Parse(BookLevel);
                    }

                    if (LevelBook < 100)
                    {
                        var BookData = ItemManager.GetItemTemplate(_ItemFromDic._GoodDatas.GoodsID);

                        var BoookExp = 0;
                        if (_ItemFromDic._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string ExpLevel))
                        {
                            BoookExp = Int32.Parse(ExpLevel);
                        }

                        var ExpMax = ItemManager.GetMaxbookEXP(LevelBook, BookData.Level);

                        var OtherParams = new Dictionary<ItemPramenter, string>
                        {
                            { ItemPramenter.Pram_1, LevelBook + "" },
                            { ItemPramenter.Pram_2, BoookExp + "" },
                            { ItemPramenter.Pram_3, ExpMax + "" }
                        };

                        LogManager.WriteLog(LogTypes.Error,
                            $"Người chơi: {this._Player.RoleName} tháo mật tịch với id = {_ItemFromDic._GoodDatas.GoodsID}, level = {LevelBook}, exp = {BoookExp}");

                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);
                    }
                }

                if (KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura == (KE_EQUIP_POSITION)Postion)
                {
                    if (ItemManager._TotalGameItem.TryGetValue(_ItemFromDic._GoodDatas.GoodsID, out ItemData itemData))
                    {
                        if (this._Player.Buffs.HasBuff(itemData.ResMale))
                            this._Player.Buffs.RemoveBuff(itemData.ResMale);
                    }
                }

                string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                LogManager.WriteLog(LogTypes.Item,
                    "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID + "][bag_index " +
                    GetBagIndexFormNewItem + "] : Bắt đầu tháo trang bị - để đeo trang bị mới ");

                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                    this._Player.ServerId);

                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID +
                        "][bag_index " + GetBagIndexFormNewItem + "] : Tháo trang bị - để đeo trang bị mới -- lỗi ");

                    return false;
                }
                else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                {
                    // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID +
                        "][bag_index " + GetBagIndexFormNewItem +
                        "] : Tháo trang bị - để đeo trang bị mới -- Thành công ");

                    KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                    SCModGoods scData = new SCModGoods()
                    {
                        BagIndex = GetBagIndexFormNewItem,
                        Count = _ItemFromDic._GoodDatas.GCount,
                        IsUsing = -1,
                        ModType = (int)ModGoodsTypes.EquipUnload,
                        ID = _ItemFromDic._GoodDatas.Id,
                        NewHint = 0,
                        Site = 0,
                        State = 0,
                    };
                    // Gửi thông báo về là tháo đồ ra thành công
                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                }

                // NẾu thực hiện tháo đồ ra thành công

                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from EquipLoadMain() in KPlayerEquipBody.cs");
                this.ClearAllEffecEquipBody();
                // Thực hiện tháo đồ cũ ra khỏi người
                if (_ItemFromDic.SkillsArtifact != null)
                {
                    // xóa skill của trang bị ra khỏi ng khỏi hiển thị
                    foreach (var skillID in _ItemFromDic.SkillsArtifact)
                    {
                        if (skillID > 0)
                            this._Player.Skills.RemoveSkill(skillID);
                    }
                    var itemRemove = this._Player.artifactInfos.SingleOrDefault(x => x.GoodID == _ItemFromDic._GoodDatas.GoodsID);
                    if (itemRemove != null)
                        this._Player.artifactInfos.Remove(itemRemove);
                }

                lock (EquipBody)
                {
                    EquipBody.Remove((KE_EQUIP_POSITION)Postion);
                }

                // Thực hiện mặc đồ mới vòa người

                TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
                // Update lại vị trí cho vật phẩm hiện tại
                TotalUpdate.Add(UPDATEITEM.BAGINDEX, -1);
                /// Khóa luôn
                TotalUpdate.Add(UPDATEITEM.BINDING, 1);
                //Set lại giá tri cho việc không sử dụng nữa
                TotalUpdate.Add(UPDATEITEM.USING, Postion);

                // nếu là chân nguyên
                if (KE_EQUIP_POSITION.emEQUIPPOS_CHANNGUYEN == (KE_EQUIP_POSITION)Postion ||
                    KE_EQUIP_POSITION.emEQUIPPOS_MEDAL == (KE_EQUIP_POSITION)Postion)
                {
                    TotalUpdate.Add(UPDATEITEM.PROPS, goodsData.Props);
                    TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, goodsData.Forge_level);
                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, goodsData.OtherParams);
                }

                if (KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura == (KE_EQUIP_POSITION)Postion)
                {
                    if (ItemManager._TotalGameItem.TryGetValue(goodsData.GoodsID, out ItemData itemData))
                    {
                        this._Player.Buffs.AddBuff(itemData.ResMale, 1);
                    }
                }

                ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                LogManager.WriteLog(LogTypes.Item,
                    "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " + Postion +
                    "][from bag_index " + GetBagIndexFormNewItem + "] : Bắt đầu đeo trang bị");

                dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                    this._Player.ServerId);

                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + GetBagIndexFormNewItem + "] : Đeo trang bị lỗi");
                    return false;
                }
                else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                {
                    KTGlobal.ModifyGoods(this._Player, goodsData.Id, TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + GetBagIndexFormNewItem + "] : Đeo trang bị thành công");
                    // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên

                    SCModGoods scData = new SCModGoods()
                    {
                        BagIndex = -1,
                        Count = goodsData.GCount,
                        IsUsing = Postion,
                        ModType = (int)ModGoodsTypes.EquipLoad,
                        ID = goodsData.Id,
                        NewHint = 0,
                        Site = 0,
                        State = 0,
                    };
                    // Gửi thông báo về cho việc mặc đồ thành công
                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                }

                lock (EquipBody)
                {
                    EquipBody.Add((KE_EQUIP_POSITION)Postion, ItemFind);
                }

                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from EquipLoadMain() in KPlayerEquipBody.cs");
                this.AttackAllEquipBody();
            }
            /// Nếu vị trí trước đó chưa từng có đồ nào
            else
            {
                try
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from EquipLoadMain() in KPlayerEquipBody.cs");
                    this.ClearAllEffecEquipBody();

                    // Phòng trường hợp có item pet nhưng đang ko mặc pet nên ko check đc đang mặc trên người
                    if (goodsData.IsItemPet())
                    {
                        lock (this._Player.GoodsDataList)
                        {
                            var itemOld = this._Player.GoodsDataList.FirstOrDefault(x => x.Using == isUsing);
                            if (itemOld != null)
                            {
                                int GetBagIndexFormNewItem = goodsData.BagIndex;
                                var totalUpdate = new Dictionary<UPDATEITEM, object>
                                {
                                    { UPDATEITEM.ROLEID, this._Player.RoleID },
                                    { UPDATEITEM.ITEMDBID, itemOld.Id },
                                    { UPDATEITEM.BAGINDEX, GetBagIndexFormNewItem },
                                    { UPDATEITEM.USING, -1 }
                                };
                                string scriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(totalUpdate);
                                LogManager.WriteLog(LogTypes.Item,
                                    "[EquipUnload][" + this._Player.RoleID + "][" + itemOld.Id + "][bag_index " +
                                    GetBagIndexFormNewItem + "] : Bắt đầu tháo trang bị - để đeo trang bị mới ");

                                TCPProcessCmdResults DbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, scriptUpdateBuild, out dbFields,
                                    this._Player.ServerId);

                                if (DbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                                {
                                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                                    LogManager.WriteLog(LogTypes.Item,
                                        "[EquipUnload][" + this._Player.RoleID + "][" + itemOld.Id + "][bag_index " +
                                        GetBagIndexFormNewItem + "] : Tháo trang bị - để đeo trang bị mới -- lỗi ");

                                    return false;
                                }
                                else if (DbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                                {
                                    // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                                    LogManager.WriteLog(LogTypes.Item,
                                        "[EquipUnload][" + this._Player.RoleID + "][" + itemOld.Id + "][bag_index " +
                                        GetBagIndexFormNewItem +
                                        "] : Tháo trang bị - để đeo trang bị mới -- Thành công ");

                                    KTGlobal.ModifyGoods(this._Player, itemOld.Id, totalUpdate);

                                    SCModGoods scData = new SCModGoods()
                                    {
                                        BagIndex = GetBagIndexFormNewItem,
                                        Count = itemOld.GCount,
                                        IsUsing = -1,
                                        ModType = (int)ModGoodsTypes.EquipUnload,
                                        ID = itemOld.Id,
                                        NewHint = 0,
                                        Site = 0,
                                        State = 0,
                                    };
                                    // Gửi thông báo về là tháo đồ ra thành công
                                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                                }
                            }
                        }
                    }

                    Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                    TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                    TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
                    // Update lại vị trí cho vật phẩm hiện tại
                    TotalUpdate.Add(UPDATEITEM.BAGINDEX, -1);
                    /// Khóa luôn
                    TotalUpdate.Add(UPDATEITEM.BINDING, 1);
                    //Set lại giá tri cho việc không sử dụng nữa
                    TotalUpdate.Add(UPDATEITEM.USING, Postion);

                    // nếu là chân nguyên
                    if (KE_EQUIP_POSITION.emEQUIPPOS_CHANNGUYEN == (KE_EQUIP_POSITION)Postion ||
                        KE_EQUIP_POSITION.emEQUIPPOS_MEDAL == (KE_EQUIP_POSITION)Postion)
                    {
                        TotalUpdate.Add(UPDATEITEM.PROPS, goodsData.Props);
                        TotalUpdate.Add(UPDATEITEM.FORGE_LEVEL, goodsData.Forge_level);
                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, goodsData.OtherParams);
                    }

                    if (KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura == (KE_EQUIP_POSITION)Postion)
                    {
                        if (ItemManager._TotalGameItem.TryGetValue(goodsData.GoodsID, out ItemData itemData))
                        {
                            this._Player.Buffs.AddBuff(itemData.ResMale, 1);
                        }
                    }

                    string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion +
                        "][from bag_index " + goodsData.BagIndex + "] : Bắt đầu đeo trang bị");

                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                        this._Player.ServerId);

                    if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                    {
                        LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                            Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị lỗi");

                        return false;
                    }
                    else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        KTGlobal.ModifyGoods(this._Player, goodsData.Id, TotalUpdate);
                        // Gửi packet mặc đồ thành công về
                        LogManager.WriteLog(LogTypes.Item,
                            "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                            Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị thành công");

                        SCModGoods scData = new SCModGoods()
                        {
                            BagIndex = -1,
                            Count = goodsData.GCount,
                            IsUsing = Postion,
                            ModType = (int)ModGoodsTypes.EquipLoad,
                            ID = goodsData.Id,
                            NewHint = 0,
                            Site = 0,
                            State = 0,
                        };
                        // Gửi thông báo về cho việc mặc đồ thành công
                        this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);

                        if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
                        {
                            this._Player.extBagBonus += ItemFind.GetBaseItem.ParticularType;
                        }
                    }

                    if (KE_EQUIP_POSITION.emEQUIPPOS_MANTLE == (KE_EQUIP_POSITION)Postion)
                    {
                        //SideTaskManager.getInstance().ProcessTask(_Player, (int)SideTaskName.Mantle);
                    }

                    //Nếu là Pet thì attack đồ pet đang mặc vào
                    if (goodsData.IsPet())
                    {
                        InitEquipPet();
                    }

                    lock (EquipBody)
                    {
                        if (ItemManager.g_anEquipPetPos.Contains(isUsing))
                        {
                            // nếu là đồ của pet thì check xem có pet ko để kích hoạt
                            if (!EquipBody.ContainsKey(KE_EQUIP_POSITION.emEQUIPPOS_PET))
                            {
                                PlayerManager.ShowNotification(this._Player,
                                    "Phải đang gọi bạn đồng hành mới có thể kích hoạt trang bị đồng hành!");
                            }
                            else
                            {
                                EquipBody.Add((KE_EQUIP_POSITION)Postion, ItemFind);
                            }
                        }
                        else
                        {
                            EquipBody.Add((KE_EQUIP_POSITION)Postion, ItemFind);
                        }
                    }

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from EquipLoadMain() in KPlayerEquipBody.cs");
                    this.AttackAllEquipBody();
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Fatal, ex.ToString());
                }
            }

            ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, _Player, -1,
                -1, goodsData.GoodsID, TaskTypes.EquipSomething);

            GlobalEventSource.getInstance().fireEvent(new EquipItemEventObject(_Player,ItemFind));

            return true;
        }

        /// <summary>
        /// Hàm mặc đồ phụ
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public bool EquipLoadSub(GoodsData goodsData)
        {
            /// Khóa luôn trang bị vào
            goodsData.Binding = 1;

            KItem ItemFind = new KItem(goodsData);

            if (ItemFind.GetBaseItem.Genre == (int)KE_ITEM_GENRE.item_extbag)
            {
                PlayerManager.ShowNotification(this._Player, "Không thể trang bị túi phụ cho bộ phụ");
                return false;
            }

            /// kiểm tra nếu là ấn , mật tịch, trận, ff, ... thì cút
            if (ItemFind.GetBaseItem.DetailType >= 12)
            {
                PlayerManager.ShowNotification(this._Player,
                    "Không thể trang bị trận, ấn, ngựa, phi phong, mật tịch, cho bộ phụ!!!!");
                return false;
            }

            // lấy ra vị trí của vật phẩm
            int Postion = ItemManager.g_anEquipSubPos[ItemFind.GetBaseItem.DetailType];

            string[] dbFields = null;

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            // Trường hợp này chắc chắn sẽ còn chỗ

            /// Kiểm tra xem có đang mặc 1 món đồ khác trên người mà cùng vị trí không
            /// Nếu có thì thực hiện DETACK món đồ cũ trước
            if (PreventiveEquipBody.ContainsKey((KE_EQUIP_POSITION_SUB)Postion))
            {
                // Verify Item xem có đúng vật phẩm không
                KItem _ItemFromDic = PreventiveEquipBody[(KE_EQUIP_POSITION_SUB)Postion];

                // Thực hiện Detack toàn bộ hiệu ứng đang mặc của bộ hiện tại

                int GetBagIndexFormNewItem = goodsData.BagIndex;

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);

                TotalUpdate.Add(UPDATEITEM.ITEMDBID, _ItemFromDic._GoodDatas.Id);
                // Update lại vị trí cho vật phẩm hiện tại
                TotalUpdate.Add(UPDATEITEM.BAGINDEX, GetBagIndexFormNewItem);
                //Set lại giá tri cho việc không sử dụng nữa
                TotalUpdate.Add(UPDATEITEM.USING, -1);

                string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                LogManager.WriteLog(LogTypes.Item,
                    "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID + "][bag_index " +
                    GetBagIndexFormNewItem + "] : Bắt đầu tháo trang bị phụ - để đeo trang bị phụ mới ");

                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                    this._Player.ServerId);

                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID +
                        "][bag_index " + GetBagIndexFormNewItem +
                        "] : Tháo trang bị phụ - để đeo trang bị phụ mới lỗi ");

                    return false;
                }
                else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                {
                    // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipUnload][" + this._Player.RoleID + "][" + _ItemFromDic._GoodDatas.GoodsID +
                        "][bag_index " + GetBagIndexFormNewItem +
                        "] : Tháo trang bị phụ - để đeo trang bị phụ mới thành công");

                    KTGlobal.ModifyGoods(this._Player, _ItemFromDic._GoodDatas.Id, TotalUpdate);

                    SCModGoods scData = new SCModGoods()
                    {
                        BagIndex = GetBagIndexFormNewItem,
                        Count = _ItemFromDic._GoodDatas.GCount,
                        IsUsing = -1,
                        ModType = (int)ModGoodsTypes.EquipUnload,
                        ID = _ItemFromDic._GoodDatas.Id,
                        NewHint = 0,
                        Site = 0,
                        State = 0,
                    };
                    // Gửi thông báo về là tháo đồ ra thành công
                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                }

                // NẾu thực hiện tháo đồ ra thành công
                // Vì là hàm phụ lên ko detack gì cả
                //this.ClearAllEffecEquipBody();
                // Thực hiện tháo đồ cũ ra khỏi người
                lock (PreventiveEquipBody)
                {
                    PreventiveEquipBody.Remove((KE_EQUIP_POSITION_SUB)Postion);
                }

                // Thực hiện mặc đồ mới vòa người

                TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
                // Update lại vị trí cho vật phẩm hiện tại
                TotalUpdate.Add(UPDATEITEM.BAGINDEX, -1);
                /// Khóa luôn
                TotalUpdate.Add(UPDATEITEM.BINDING, 1);
                //Set lại giá tri cho việc không sử dụng nữa
                TotalUpdate.Add(UPDATEITEM.USING, Postion);

                ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                LogManager.WriteLog(LogTypes.Item,
                    "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " + Postion +
                    "][from bag_index " + goodsData.BagIndex + "] : Bắt đầu đeo trang bị phụ");

                dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                    this._Player.ServerId);

                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị phụ lỗi");

                    return false;
                }
                else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                {
                    KTGlobal.ModifyGoods(this._Player, goodsData.Id, TotalUpdate);
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị phụ thành công");

                    // Gửi packet thứ nhất - Tháo đồ trước đó ra vị trí của đồ chuẩn bị mặc lên

                    SCModGoods scData = new SCModGoods()
                    {
                        BagIndex = -1,
                        Count = goodsData.GCount,
                        IsUsing = Postion,
                        ModType = (int)ModGoodsTypes.EquipLoad,
                        ID = goodsData.Id,
                        NewHint = 0,
                        Site = 0,
                        State = 0,
                    };
                    // Gửi thông báo về cho việc mặc đồ thành công
                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                }

                // ADD ĐỒ VÀO SLOT DỰ PHÒNG
                lock (PreventiveEquipBody)
                {
                    PreventiveEquipBody.Add((KE_EQUIP_POSITION_SUB)Postion, ItemFind);
                }

                // CHỈ TÍNH LẠI TÀI PHÚ
                this.ReloadTotalValue();
                //this.AttackAllEquipBody();
            }
            /// Nếu vị trí trước đó chưa từng có đồ nào
            else
            {
                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
                // Update lại vị trí cho vật phẩm hiện tại
                TotalUpdate.Add(UPDATEITEM.BAGINDEX, -1);
                /// Khóa luôn
                TotalUpdate.Add(UPDATEITEM.BINDING, 1);
                //Set lại giá tri cho việc không sử dụng nữa
                TotalUpdate.Add(UPDATEITEM.USING, Postion);

                string ScriptUpdateBuild = KTGlobal.ItemUpdateScriptBuild(TotalUpdate);
                LogManager.WriteLog(LogTypes.Item,
                    "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " + Postion +
                    "][from bag_index " + goodsData.BagIndex + "] : Bắt đầu đeo trang bị phụ");

                TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                    (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, ScriptUpdateBuild, out dbFields,
                    this._Player.ServerId);

                if (dbRequestResult == TCPProcessCmdResults.RESULT_FAILED)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "Mặc đồ lỗi do không thể tháo đồ cũ ra");
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị phụ lỗi");

                    return false;
                }
                else if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                {
                    KTGlobal.ModifyGoods(this._Player, goodsData.Id, TotalUpdate);
                    // Gửi packet mặc đồ thành công về
                    LogManager.WriteLog(LogTypes.Item,
                        "[EquipLoad][" + this._Player.RoleID + "][" + ItemFind._ItemData.ItemID + "][Postion " +
                        Postion + "][from bag_index " + goodsData.BagIndex + "] : Đeo trang bị phụ thành công");

                    SCModGoods scData = new SCModGoods()
                    {
                        BagIndex = -1,
                        Count = goodsData.GCount,
                        IsUsing = Postion,
                        ModType = (int)ModGoodsTypes.EquipLoad,
                        ID = goodsData.Id,
                        NewHint = 0,
                        Site = 0,
                        State = 0,
                    };
                    // Gửi thông báo về cho việc mặc đồ thành công
                    this._Player.SendPacket((int)TCPGameServerCmds.CMD_SPR_MOD_GOODS, scData);
                }

                lock (PreventiveEquipBody)
                {
                    PreventiveEquipBody.Add((KE_EQUIP_POSITION_SUB)Postion, ItemFind);
                }

                // KO ATTACK GÌ HẾT MÀ CHỈ RECACLULATION LẠI TÀI PHÚ
                // this.AttackAllEquipBody();

                this.ReloadTotalValue();
            }

            return true;
        }

        /// <summary>
        /// Trả về trang bị ở vị trí tương ứng
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        public KItem GetItemByPostion(int Position)
        {
            if (EquipBody.ContainsKey((KE_EQUIP_POSITION)Position))
            {
                return EquipBody[(KE_EQUIP_POSITION)Position];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gọi khi nhân vật vào Game, Attach chỉ số của tất cả trang bị
        /// </summary>
        public void InitEquipBody()
        {
            if (null != this._Player.GoodsDataList && this._Player.GoodsDataList.Count > 0)
            {
                lock (this._Player.GoodsDataList)
                {
                    List<GoodsData> toCorrectGoodsDataList = new List<GoodsData>();

                    PetManager.TruThanMat(this._Player);

                    var pet =
                        this._Player.GoodsDataList.FirstOrDefault(x =>
                            x.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_PET);

                    // add pet trước để vòng lặp dưới còn attach pet equip
                    if (pet != null)
                    {
                        lock (EquipBody)
                        {
                            if (!string.IsNullOrEmpty(ServerConfig.Instance.LimitEquipPos))
                            {
                                var listPos = ServerConfig.Instance.LimitEquipPos.Split(';');

                                if (!listPos.Contains(pet.Using.ToString()))
                                {
                                    EquipBody.Add(KE_EQUIP_POSITION.emEQUIPPOS_PET, new KItem(pet));
                                }
                            }
                            else
                            {
                                EquipBody.Add(KE_EQUIP_POSITION.emEQUIPPOS_PET, new KItem(pet));
                            }
                        }
                    }

                    foreach (var good in this._Player.GoodsDataList)
                    {
                        //Nếu mà vật phẩm không sử dụng thì thôi
                        if (good.Using < 0)
                        {
                            if (ItemManager._TotalGameItem.TryGetValue(good.GoodsID, out ItemData itemData))
                            {
                                if (itemData.DetailType == (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura))
                                {
                                    if (this._Player.Buffs.HasBuff(itemData.ResMale))
                                        this._Player.Buffs.RemoveBuff(itemData.ResMale);
                                }
                            }

                            continue;
                        }

                        // nếu là pet thì bỏ qua (đã add)
                        if (good.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_PET)
                            continue;

                        if (good.Using < 100)
                        {
                            KE_EQUIP_POSITION _Position = (KE_EQUIP_POSITION)good.Using;
                            // Nếu ko có pet thì ko kích hoạt các trang bị pet
                            if (good.Using > 20 && !EquipBody.ContainsKey(KE_EQUIP_POSITION.emEQUIPPOS_PET) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitArmor) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitHelmet) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitWeapon) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitRoleTitle) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura) &&
                                good.Using != (int)(KE_EQUIP_POSITION.emEQUIPPOS_MEDAL))
                                continue;

                            if (EquipBody.ContainsKey(_Position))
                                continue;

                            if (good.Using == (int)(KE_EQUIP_POSITION.emEQUIPPOS_OutfitBuffAura))
                            {
                                if (ItemManager._TotalGameItem.TryGetValue(good.GoodsID, out ItemData itemData))
                                {
                                    if (this._Player.Buffs.HasBuff(itemData.ResMale))
                                        this._Player.Buffs.RemoveBuff(itemData.ResMale);
                                    this._Player.Buffs.AddBuff(itemData.ResMale, 1);
                                }
                            }

                            lock (EquipBody)
                            {
                                if (!string.IsNullOrEmpty(ServerConfig.Instance.LimitEquipPos))
                                {
                                    var listPos = ServerConfig.Instance.LimitEquipPos.Split(';');

                                    if (!listPos.Contains(good.Using.ToString()))
                                    {
                                        EquipBody.Add(_Position, new KItem(good));
                                    }
                                }
                                else
                                {
                                    EquipBody.Add(_Position, new KItem(good));
                                }
                            }
                        }
                        else if (good.Using >= 100)
                        {
                            KE_EQUIP_POSITION_SUB _Positon = (KE_EQUIP_POSITION_SUB)good.Using;

                            if (PreventiveEquipBody.ContainsKey(_Positon))
                                continue;

                            lock (PreventiveEquipBody)
                            {
                                if (!string.IsNullOrEmpty(ServerConfig.Instance.LimitEquipPos))
                                {
                                    var listPos = ServerConfig.Instance.LimitEquipPos.Split(';');

                                    if (!listPos.Contains(good.Using.ToString()))
                                    {
                                        PreventiveEquipBody.Add(_Positon, new KItem(good));
                                    }
                                }
                                else
                                {
                                    PreventiveEquipBody.Add(_Positon, new KItem(good));
                                }
                            }
                        }
                    }

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from InitEquipBody() in KPlayerEquipBody.cs");
                    this.AttackAllEquipBody();

                    // check xem nếu ko đủ độ thân mật thì tự tháo 
                    if (pet != null && !PetManager.CanUsePet(this._Player, pet.Id))
                    {
                        _Player.GetPlayEquipBody().EquipUnloadMain(pet, pet.Using);
                    }
                }
            }
        }

        /// <summary>
        /// Khi trang bị pet thì các trang bị của pet khác cũng đc active
        /// </summary>
        public void InitEquipPet()
        {
            if (null != this._Player.GoodsDataList && this._Player.GoodsDataList.Count > 0)
            {
                lock (this._Player.GoodsDataList)
                {
                    for (int i = 0; i < this._Player.GoodsDataList.Count; i++)
                    {
                        //Nếu mà vật phẩm không sử dụng thì thôi
                        if (this._Player.GoodsDataList[i].Using < 0)
                        {
                            continue;
                        }

                        if (this._Player.GoodsDataList[i].Using < 100 && this._Player.GoodsDataList[i].Using > 20)
                        {
                            KE_EQUIP_POSITION _Positon = (KE_EQUIP_POSITION)this._Player.GoodsDataList[i].Using;

                            lock (EquipBody)
                            {
                                if (!EquipBody.ContainsKey(_Positon))
                                    EquipBody.Add(_Positon, new KItem(this._Player.GoodsDataList[i]));
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Thay đổi ngũ hành các trang bị đặc biệt tương ứng với ngũ hành hiện tại của nhân vật
        /// </summary>
        public void ChangeEquipsCorrespondingToSeries()
        {
            /// Nếu có đồ
            if (null != this._Player.GoodsDataList && this._Player.GoodsDataList.Count > 0)
            {
                /// Danh sách trang bị đặc biệt
                List<GoodsData> specialEquips = new List<GoodsData>();

                lock (this._Player.GoodsDataList)
                {
                    specialEquips = this._Player.GoodsDataList.Where((itemGD) =>
                    {
                        /// Nếu là phi phong
                        if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE ||
                            itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_MANTLE)
                        {
                            return true;
                        }
                        /// Nếu là ngũ hành ấn
                        else if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_SIGNET ||
                                 itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_SIGNET)
                        {
                            return true;
                        }
                        /// Nếu là quan ấn
                        else if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_CHOP ||
                                 itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_CHOP)
                        {
                            return true;
                        }

                        /// Không thỏa mãn
                        return false;
                    }).ToList();
                }

                /// Môn phái nhân vật
                int factionID = this._Player.m_cPlayerFaction.GetFactionId();
                /// Giới tính nhân vật
                int sex = this._Player.RoleSex;
                /// Ngũ hành nhân vật
                KE_SERIES_TYPE series = this._Player.m_Series;
                /// Thông tin TCP
                TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
                TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

                /// Duyệt danh sách trang bị đặc biệt
                foreach (GoodsData itemGD in specialEquips)
                {
                    /// Thông tin vật phẩm
                    ItemData goodsItemData = ItemManager.GetItemTemplate(itemGD.GoodsID);
                    /// Toác
                    if (goodsItemData == null)
                    {
                        continue;
                    }

                    /// Danh sách cập nhật chỉ số trang bị
                    Dictionary<UPDATEITEM, object> updateProperties = new Dictionary<UPDATEITEM, object>();
                    updateProperties.Add(UPDATEITEM.ROLEID, this._Player.RoleID);
                    updateProperties.Add(UPDATEITEM.ITEMDBID, itemGD.Id);

                    /// ID vật phẩm
                    int goodsID = itemGD.GoodsID;
                    /// Ngũ hành
                    int goodsSeries = itemGD.Series;
                    /// Nếu là ngũ hành ấn
                    if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_SIGNET ||
                        itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_SIGNET)
                    {
                        /// Tìm ngũ hành ấn tương ứng phái
                        ItemData itemData = ItemManager.Signets.Where((goods) =>
                        {
                            /// Nếu không cùng cấp độ
                            if (goods.Level != goodsItemData.Level)
                            {
                                return false;
                            }
                            /// Nếu phái không khớp
                            else if (!goods.ListReqProp.Any(x =>
                                         x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_FACTION &&
                                         x.ReqPropValue == factionID))
                            {
                                return false;
                            }

                            /// Thỏa mãn
                            return true;
                        }).FirstOrDefault();
                        /// Nếu tìm thấy
                        if (itemData != null)
                        {
                            /// Thay đổi ID vật phẩm
                            goodsID = itemData.ItemID;
                            updateProperties.Add(UPDATEITEM.GOODSID, itemData.ItemID);
                            /// Thay đổi ngũ hành
                            goodsSeries = itemData.Series;
                            updateProperties.Add(UPDATEITEM.SERIES, itemData.Series);
                        }
                    }
                    /// Nếu là phi phong
                    else if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_MANTLE ||
                             itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_MANTLE)
                    {
                        /// Tìm phi phong tương ứng hệ
                        ItemData itemData = ItemManager.Mantles.Where((goods) =>
                        {
                            /// Nếu không cùng cấp độ
                            if (goods.Level != goodsItemData.Level)
                            {
                                return false;
                            }
                            /// Nếu yêu cầu giới tính không thỏa mãn
                            else if (!goods.ListReqProp.Any(x =>
                                         x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_SEX &&
                                         x.ReqPropValue == sex))
                            {
                                return false;
                            }
                            /// Nếu yêu cầu ngũ hành không thỏa mãn
                            else if (!goods.ListReqProp.Any(x =>
                                         x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_SERIES &&
                                         x.ReqPropValue == (int)series))
                            {
                                return false;
                            }
                            else if (goodsItemData.ItemID >= 13156 && goods.ItemID < 13156)
                            {
                                // phi phong sự kiện chỉ được đổi sang phi phong sự kiện
                                return false;
                            }

                            /// Thỏa mãn
                            return true;
                        }).FirstOrDefault();
                        /// Nếu tìm thấy
                        if (itemData != null)
                        {
                            /// Thay đổi ID vật phẩm
                            goodsID = itemData.ItemID;
                            updateProperties.Add(UPDATEITEM.GOODSID, itemData.ItemID);
                            /// Thay đổi ngũ hành
                            goodsSeries = itemData.Series;
                            updateProperties.Add(UPDATEITEM.SERIES, itemData.Series);
                        }
                    }
                    /// Nếu là quan ấn
                    else if (itemGD.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_CHOP ||
                             itemGD.Using == (int)KE_EQUIP_POSITION_SUB.emEQUIPPOS_CHOP)
                    {
                        /// Tìm quan ấn tương ứng hệ
                        ItemData itemData = ItemManager.Chops.Where((goods) =>
                        {
                            /// Nếu không cùng cấp độ
                            if (goods.Level != goodsItemData.Level)
                            {
                                return false;
                            }
                            /// Nếu yêu cầu ngũ hành không thỏa mãn
                            else if (!goods.ListReqProp.Any(x =>
                                         x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_SERIES &&
                                         x.ReqPropValue == (int)series))
                            {
                                return false;
                            }

                            /// Thỏa mãn
                            return true;
                        }).FirstOrDefault();
                        /// Nếu tìm thấy
                        if (itemData != null)
                        {
                            /// Thay đổi ID vật phẩm
                            goodsID = itemData.ItemID;
                            updateProperties.Add(UPDATEITEM.GOODSID, itemData.ItemID);
                            /// Thay đổi ngũ hành
                            goodsSeries = itemData.Series;
                            updateProperties.Add(UPDATEITEM.SERIES, itemData.Series);
                        }
                    }

                    /// Nếu không có gì thay đổi thì thôi
                    if (goodsID == itemGD.GoodsID)
                    {
                        continue;
                    }

                    string cmdData = KTGlobal.ItemUpdateScriptBuild(updateProperties);
                    TCPProcessCmdResults dbRequestResult = Global.RequestToDBServer(tcpClientPool, pool,
                        (int)TCPGameServerCmds.CMD_DB_UPDATEGOODS_CMD, cmdData, out string[] dbFields,
                        this._Player.ServerId);
                    /// Nếu thay đổi thành công
                    if (dbRequestResult == TCPProcessCmdResults.RESULT_DATA)
                    {
                        /// Cập nhật thay đổi ở GS
                        itemGD.GoodsID = goodsID;
                        itemGD.Series = goodsSeries;
                        /// Cập nhật thay đổi về Client
                        this._Player.SendPacket<GoodsData>((int)TCPGameServerCmds.CMD_SPR_NOTIFYGOODSINFO, itemGD);
                    }
                }
            }
        }

        private void AttachMedalEffect(KItem item, bool isDetach)
        {
            if (item == null || item._GoodDatas == null)
            {
                return;
            }

            List<KMagicAttrib> attributes = MedalManager.BuildMagicAttributes(item._GoodDatas);
            foreach (KMagicAttrib attribute in attributes)
            {
                KTAttributesModifier.AttachProperty(attribute, this._Player, isDetach);
            }
        }

        /// <summary>
        /// Xóa hết toàn bộ Effect item đang mặc
        /// <para>justAbrade</para>
        /// </summary>
        public void ClearAllEffecEquipBody(KItem justAbrade = null)
        {
            this.hpBeforeDetach = this._Player.m_CurrentLife;
            this.mpBeforeDetach = this._Player.m_CurrentMana;
            this.staminaBeforeDetach = this._Player.m_CurrentStamina;

            lock (EquipBody)
            {
                _Player.Skills.BonusLevelSkills = new List<SkillLevelBonus>();
                _Player.Skills.BookSkillIDs = new List<(int idSkill, int itemLevel)>();
                _Player.Skills.BonusBookLevelSkills = new List<SkillLevelBonus>();
                // For thực hiện DETRACK toàn bộ thuộc tính của người đi
                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    if (entry.Value._GoodDatas.Strong <= 0) // nếu là đồ đã hỏng
                    {
                        if (justAbrade ==
                            null) // nếu không có tham số thể hiện đồ vừa mới hỏng thì không detach (vì đã detach trước đó)
                            continue;

                        if (entry.Value.ItemDBID !=
                            justAbrade
                                .ItemDBID) // nếu đồ không phải vừa mới hỏng thì không detach (vì đã detach trước đó)
                            continue;
                    }

                    KItem _Item = entry.Value;
                    // CODE ATTACK TOÀN BỘ THUỘC TÍNH CỦA ITEM VÀO NGƯỜI CHƠI
                    _Item.AttackEffect(this._Player, true);
                    this.AttachMedalEffect(_Item, true);

            
                    // Nếu là ngựa thì
                    if (entry.Key == KE_EQUIP_POSITION.emEQUIPPOS_HORSE)
                    {
                        // Nếu đang cưỡi ngựa thì detack toàn bộ thuộc tính của ngựa
                        if (this._Player.IsRiding)
                        {
                            _Item.AttachRiderEffect(this._Player, true);
                        }
                        else
                        {
                            // kích hoạt lại chỉ số ở ngựa
                            if (_Item.TotalRiderProp.Count > 0)
                            {
                                // var listMagicReattach = new List<MAGIC_ATTRIB> {
                                //     MAGIC_ATTRIB.magic_allskill_v,
                                //     MAGIC_ATTRIB.magic_adddefense_v,
                                //     MAGIC_ATTRIB.magic_damage_all_resist,
                                //     MAGIC_ATTRIB.magic_skilldamageptrim
                                // };
                                var magicsToReattach = _Item.TotalRiderProp.Where(x =>
                                    x.nAttribType != MAGIC_ATTRIB.magic_fastwalkrun_p &&
                                    x.nAttribType != MAGIC_ATTRIB.magic_fastwalkrun_v);
                                foreach (var magic in magicsToReattach)
                                {
                                    if (magic != null)
                                    {
                                        KTAttributesModifier.AttachProperty(magic, this._Player, true);
                                    }
                                }
                            }
                        }
                    }
                }

                /// Reset lại quan hàm
                this._Player.m_ChopLevel = 0;

                // Detack toàn bộ thuộc tính bộ khỏi người
                foreach (KMagicAttrib _KMagic in this.SuitUpActive)
                {
                    KTAttributesModifier.AttachProperty(_KMagic, _Player, true);
                }

                // Detack thuộc tính của trang bị bạch ngân hoàng kim sau nâng cấp
                var ArtifactProps = new List<KMagicAttrib>();

                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    if (ItemManager.IsMedal(entry.Value._GoodDatas))
                    {
                        continue;
                    }

                    if (entry.Value._GoodDatas.OtherParams != null)
                    {
                        var otherParams = entry.Value._GoodDatas.OtherParams;
                        if (otherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                        {
                            var arrParam3 = param3.Split('|');
                            if (arrParam3.Length > 2)
                            {
                                for (int i = 2; i < arrParam3.Length; i++)
                                {
                                    var propInfo = arrParam3[i].Split(';');
                                    int nValue = int.Parse(propInfo[1]);
                                    var attribute = new KMagicAttrib();
                                    attribute.Init(propInfo[0], nValue, 0, 0);
                                    ArtifactProps.Add(attribute);
                                }

                            }
                        }
                    }
                }

                foreach (KMagicAttrib _KMagic in ArtifactProps)
                {
                    KTAttributesModifier.AttachProperty(_KMagic, _Player, true);
                }
                
            
                _Player.Skills.ExportSkillTree();
                _Player.Skills.ProcessEnchantSkills();
            }
        }

        /// <summary>
        /// Attack all effect của item đang mặc
        /// </summary>
        public void AttackAllEquipBody()
        {
            lock (EquipBody)
            {
                bool active_all_ornament = false;
                bool active_suit = false;
                
                _Player.Skills.BonusLevelSkills = new List<SkillLevelBonus>();
                _Player.Skills.BookSkillIDs = new List<(int idSkill, int itemLevel)>();
                _Player.Skills.BonusBookLevelSkills = new List<SkillLevelBonus>();

                var FindOrnament = EquipBody.Values.Where(x => x.active_all_ornament == true).FirstOrDefault();

                if (FindOrnament != null)
                {
                    active_all_ornament = true;
                }

                var FindActive_suit = EquipBody.Values.Where(x => x.active_suit == true).FirstOrDefault();

                if (FindActive_suit != null)
                {
                    active_suit = true;
                }

                // Vòng for đầu tiên để kích hoạt các dòng ẩn của các trang bị đang mặc trước
                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    if (entry.Value._GoodDatas.Strong <= 0)
                    {
                        continue;
                    }

                    KItem _Item = entry.Value;

                    _Item.Update(this._Player, active_all_ornament, active_suit);
                }

                // Vòng for thứ 2 bắt đầu ATTACK các thuộc tính của dòng vào người
                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    if (entry.Value._GoodDatas.Strong <= 0)
                    {
                        continue;
                    }

                    KItem _Item = entry.Value;

                    if (this.CanUsingEquip(_Item._GoodDatas, _Item._GoodDatas.Using))
                    {
                        _Item.AttackEffect(this._Player, false);
                        this.AttachMedalEffect(_Item, false);
                    }

                    if (entry.Key == KE_EQUIP_POSITION.emEQUIPPOS_HORSE)
                    {
                        // Nếu đang cưỡi ngựa thì detack toàn bộ thuộc tính của ngựa
                        if (this._Player.IsRiding)
                        {
                            _Item.AttachRiderEffect(this._Player, false);
                        }
                        else
                        {
                            // kích hoạt lại cộng chỉ số ở ngựa
                            if (_Item.TotalRiderProp.Count > 0)
                            {
                                // var listMagicReattach = new List<MAGIC_ATTRIB> {
                                //     MAGIC_ATTRIB.magic_allskill_v,
                                //     MAGIC_ATTRIB.magic_adddefense_v,
                                //     MAGIC_ATTRIB.magic_damage_all_resist,
                                //     MAGIC_ATTRIB.magic_skilldamageptrim
                                // };
                                var magicsToReattach = _Item.TotalRiderProp.Where(x =>
                                    x.nAttribType != MAGIC_ATTRIB.magic_fastwalkrun_p &&
                                    x.nAttribType != MAGIC_ATTRIB.magic_fastwalkrun_v);
                                foreach (var magic in magicsToReattach)
                                {
                                    if (magic != null)
                                    {
                                        KTAttributesModifier.AttachProperty(magic, this._Player, false);
                                    }
                                }
                            }
                        }
                    }
                }

                /// Create New list After Clear Effect
                this.SuitUpActive = new List<KMagicAttrib>();

                // Kích hoạt thuộc tính kích hoạt theo bộ nếu đang mặc 1 set
                List<SuiteActiveProp> TotalProbs = ItemManager.TotalSuiteActiveProp.ToList();

                foreach (SuiteActiveProp _Active in TotalProbs)
                {
                    int Active = 0;

                    int SuiteID = _Active.SuiteID;

                    foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                    {
                        KItem _Item = entry.Value;

                        if (_Item.SuiteID == SuiteID)
                        {
                            Active++;
                        }
                    }

                    foreach (var suiteActive in _Active.ListActive)
                    {
                        if (Active >= suiteActive.RequestNum)
                        {
                            var attribute = new KMagicAttrib();
                            attribute.Init(suiteActive.SuiteName, suiteActive.SuiteMAPA1, suiteActive.SuiteMAPA2,
                                suiteActive.SuiteMAPA3);
                            this.SuitUpActive.Add(attribute);
                        }
                    }
                }

                // attack toàn bộ thuộc tính của thuộc tính bộ vào
                foreach (KMagicAttrib _KMagic in this.SuitUpActive)
                {
                    KTAttributesModifier.AttachProperty(_KMagic, _Player, false);
                }

                // kích hoạt thuộc tính của trang bị bạch ngân hoàng kim sau nâng cấp
                var ArtifactProps = new List<KMagicAttrib>();

                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    if (ItemManager.IsMedal(entry.Value._GoodDatas))
                    {
                        continue;
                    }
                    
                    if (entry.Value._GoodDatas.OtherParams != null)
                    {
                        var otherParams = entry.Value._GoodDatas.OtherParams;
                        if (otherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                        {
                            var arrParam3 = param3.Split('|');
                            if (arrParam3.Length > 2)
                            {
                                for (int i = 2; i < arrParam3.Length; i++)
                                {
                                    var propInfo = arrParam3[i].Split(';');
                                    int nValue = int.Parse(propInfo[1]);
                                    var attribute = new KMagicAttrib();
                                    attribute.Init(propInfo[0], nValue, 0, 0);
                                    ArtifactProps.Add(attribute);
                                }    
                                
                            }    
                        }    
                    }    
                }    

                foreach (KMagicAttrib _KMagic in ArtifactProps)
                {
                    KTAttributesModifier.AttachProperty(_KMagic, _Player, false);
                }

                /// Reset lại quan hàm
                this._Player.m_ChopLevel = 0;
                /// Quan hàm
                if (this.EquipBody.TryGetValue(KE_EQUIP_POSITION.emEQUIPPOS_CHOP, out KItem itemMgr))
                {
                    /// Thông tin vật phẩm tương ứng
                    GoodsData itemGD = itemMgr._GoodDatas;
                    /// Nếu tồn tại
                    if (itemGD != null)
                    {
                        /// Thông tin vật phẩm
                        ItemData itemData = ItemManager.GetItemTemplate(itemGD.GoodsID);
                        /// Nếu tồn tại
                        if (itemData != null)
                        {
                            /// Đánh dấu cấp độ quan hàm tương ứng
                            this._Player.m_ChopLevel = itemData.Level;
                        }
                    }
                }

                if (this.hpBeforeDetach > 0)
                {
                    this._Player.m_CurrentLife = Math.Min(this.hpBeforeDetach, this._Player.m_CurrentLifeMax);
                }

                if (this.mpBeforeDetach > 0)
                {
                    this._Player.m_CurrentMana = Math.Min(this.mpBeforeDetach, this._Player.m_CurrentManaMax);
                }

                if (this.staminaBeforeDetach > 0)
                {
                    this._Player.m_CurrentStamina =
                        Math.Min(this.staminaBeforeDetach, this._Player.m_CurrentStaminaMax);
                }

                if (this._Player.Skills.BookSkillIDs.Count > 0)
                {
                    foreach (var item in this._Player.Skills.BookSkillIDs)
                    {
                        var bonus = this._Player.Skills.BonusBookLevelSkills.Find(x=>x.id == item.itemLevel);
                        this._Player.Skills.BonusLevelSkills.Add(new SkillLevelBonus(item.idSkill, bonus.bonus));
                    }
                }

                this.ReloadTotalValue();
            
                _Player.Skills.ExportSkillTree();
                _Player.Skills.ProcessEnchantSkills();
            }

            int displayOfficeRank = this._Player.DisplayOfficeRank;
            if (this.lastDisplayOfficeRank != displayOfficeRank)
            {
                this.lastDisplayOfficeRank = displayOfficeRank;
                KT_TCPHandler.NotifyOtherMyGuildAndFamilyRankChanged(this._Player, true);
            }
        }

        /// <summary>
        /// Tính toán lại tổng tài phú từ trang bị
        /// </summary>
        public void ReloadTotalValue()
        {
            long TotalValue = 0;

            lock (EquipBody)
            {
                // Vòng for đầu tiên để kích hoạt các dòng ẩn của các trang bị đang mặc trước
                foreach (KeyValuePair<KE_EQUIP_POSITION, KItem> entry in EquipBody)
                {
                    KItem _Item = entry.Value;

                    long TaiPhu = 0;
                    // nếu là pet
                    if (entry.Key == KE_EQUIP_POSITION.emEQUIPPOS_PET)
                    {
                        PetAttr petAttr = ItemManager.GetItemTemplate(entry.Value._GoodDatas.GoodsID).PetProperty;
                        if (petAttr != null)
                        {
                            int totalLevelSkill = PetManager.GetTotalLevelSkill(entry.Value._GoodDatas,
                                entry.Value.SkillsFromPet.Count);
                            Value_Skill valueInfo = PetManager.InfoPets.Value_Skill
                                .Where(x => x.ID == entry.Value.SkillsFromPet.Count).FirstOrDefault();
                            if (valueInfo != null)
                            {
                                int value = valueInfo.Level * entry.Value._GoodDatas.Forge_level +
                                            valueInfo.ValueSkill * totalLevelSkill;
                                TaiPhu = value;
                                LogManager.WriteLog(LogTypes.Pet,
                                    $"Player: {this._Player.RoleName} petID= {entry.Value._GoodDatas.GoodsID} has value = {value}");
                            }
                        }
                        else
                        {
                            LogManager.WriteLog(LogTypes.Pet,
                                $"Player: {this._Player.RoleName} không tìm thấy petID={entry.Value._GoodDatas.GoodsID}");
                        }

                        TotalValue += TaiPhu * 10000L;
                    }
                    else
                    {
                        StarLevelStruct _ItemData = ItemManager.ItemValueCalculation(_Item._GoodDatas, out TaiPhu);
                        TaiPhu += ItemManager.GetTaiPhuBaoThachItem(_Item._GoodDatas);
                        TotalValue += TaiPhu;
                    }
                }

                foreach (KeyValuePair<KE_EQUIP_POSITION_SUB, KItem> entry in PreventiveEquipBody)
                {
                    KItem _Item = entry.Value;

                    long TaiPhu = 0;

                    StarLevelStruct _ItemData = ItemManager.ItemValueCalculation(_Item._GoodDatas, out TaiPhu);
                    TaiPhu += ItemManager.GetTaiPhuBaoThachItem(_Item._GoodDatas);
                    TotalValue += TaiPhu;
                }
                
                // Vòng for thứ 2 bắt đầu ATTACK các thuộc tính của dòng vào người

                this._Player.SetTotalValue(TotalValue);

                /// Notify về Client
                KT_TCPHandler.NotifySelfRoleValueChanged(this._Player, TotalValue);

                GameDb.ForceProcessUpdateDBRanking(this._Player);
            }
        }

        /// <summary>
        /// Kích hoạt hoặc hủy kích hoạt trạng thái cưỡi
        /// </summary>
        public bool IsCanUsing(int ROUTE, int LEVEL, int FACTION, int SERIES, int SEX, List<KREQUIRE_ATTR> TotalRequest)
        {
            if (TotalRequest == null)
            {
                return true;
            }

            foreach (KREQUIRE_ATTR Request in TotalRequest)
            {
                if (Request.eRequire == KE_ITEM_REQUIREMENT.emEQUIP_REQ_FACTION)
                {
                    if (FACTION != Request.nValue)
                    {
                        PlayerManager.ShowNotification(this._Player,
                            "Trang bị chỉ có thể sử dụng cho phái :" + KFaction.GetName(Request.nValue));
                        return false;
                    }
                }

                if (Request.eRequire == KE_ITEM_REQUIREMENT.emEQUIP_REQ_LEVEL)
                {
                    if (LEVEL < Request.nValue)
                    {
                        PlayerManager.ShowNotification(this._Player,
                            "Đại hiệp phải đạt cấp độ " + Request.nValue + " mới có thể sử dụng vật phẩm này");
                        return false;
                    }
                }

                if (Request.eRequire == KE_ITEM_REQUIREMENT.emEQUIP_REQ_ROUTE)
                {
                    if (ROUTE != Request.nValue)
                    {
                        PlayerManager.ShowNotification(this._Player,
                            "Nhánh tu luyện không phù hợp không thể trang bị vật phẩm");
                        return false;
                    }
                }

                if (Request.eRequire == KE_ITEM_REQUIREMENT.emEQUIP_REQ_SERIES)
                {
                    if (SERIES != Request.nValue)
                    {
                        PlayerManager.ShowNotification(this._Player,
                            "Trang bị yêu cầu ngũ hành: " + KTGlobal.GetSeriesText(Request.nValue) +
                            " mới có thể sử dụng");
                        return false;
                    }
                }

                if (Request.eRequire == KE_ITEM_REQUIREMENT.emEQUIP_REQ_SEX)
                {
                    if (SEX != Request.nValue)
                    {
                        PlayerManager.ShowNotification(this._Player,
                            "Trang bị yêu cầu giới tính : " + KTGlobal.SexToString(Request.nValue) +
                            " mới có thể sử dụng");
                        return false;
                    }
                }
            }

            return true;
        }

        public long m_LastTicks = 0;

        /// <summary>
        /// Hỏng đồ
        /// </summary>
        public void InjuredSomebody()
        {
            long startTicks = TimeUtil.NOW();

            // Cứ 20s hỏng phát
            if (startTicks - m_LastTicks < 500000)
            {
                return;
            }

            m_LastTicks = startTicks;

            if (EquipBody.Count <= 0)
            {
                return;
            }


            // Nếu nhân vật có hơn 20 điểm may mắn
            if (this._Player.m_nCurLucky > 0)
            {
                int Random = new Random().Next(0, 100);

                // Nếu may thì không hỏng
                if (this._Player.m_nCurLucky * 2 > Random)
                {
                    return;
                }
            }

            KItem goodsData = null;

            /// Sói Fix BUG tràn mảng
            int nRandom = KTGlobal.GetRandomNumber(0, (int)KE_EQUIP_POSITION.emEQUIPPOS_NUM - 1);

            // Nếu random vào mấy cái kia thì ko hỏng
            if ((KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_HORSE ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_MANTLE ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_ZHEN ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_SIGNET ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_BOOK ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_CHOP ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_1 ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_2 ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_EXT_BAG_3 ||
                (KE_EQUIP_POSITION)nRandom == KE_EQUIP_POSITION.emEQUIPPOS_PET)
            {
                return;
            }

            lock (EquipBody)
            {
                this.EquipBody.TryGetValue((KE_EQUIP_POSITION)nRandom, out goodsData);
            }

            if (goodsData != null)
            {
                goodsData.Abrade(2, this._Player);
            }
            //GameManager.ClientMgr.AddEquipStrong(this._Player, goodsData._GoodDatas, 1);
        }

        /// <summary>
        /// Có thể dùng trang bị không
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public bool CanUsingEquip(GoodsData goodsData, int position)
        {
            if (goodsData == null)
            {
                return false;
            }

            KItem ItemFind = new KItem(goodsData);

            if (IsCanUsing(this._Player.m_cPlayerFaction.GetRouteId(), this._Player.m_Level,
                    this._Player.m_cPlayerFaction.GetFactionId(), (int)this._Player.m_Series, this._Player.RoleSex,
                    ItemFind.TotalRequest))
            {
                if (ItemManager.KD_ISWEAPON(ItemFind._ItemData.DetailType) && position == (int) KE_EQUIP_POSITION.emEQUIPPOS_WEAPON)
                {
                    var currentOutfitCategory = this._Player.GetWeaponOutfitCategory();
                    if (currentOutfitCategory != KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_ALL && ItemFind._ItemData.Category != (int)currentOutfitCategory)
                    {
                        PlayerManager.ShowMessageBox(this._Player, "Thông báo",
                            $"Vũ khí muốn trang bị không phù hợp với ngoại trang vũ khí đang sử dụng.\nVũ khí là {ItemManager.GetWeaponKind(ItemFind._ItemData.Category)}, ngoại trang là {ItemManager.GetWeaponKind((int)currentOutfitCategory)}");
                        return false;
                    }
                }

                if (ItemFind._ItemData.DetailType == (int)KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitWeapon)
                {
                    // check điều kiện sử dụng ngoại trang vũ khí
                    var currentWeaponCategory = this._Player.GetWeaponCategory();
                    if (ItemFind._ItemData.Category != (int)KE_EQUIP_WEAPON_CATEGORY.emKEQUIP_WEAPON_CATEGORY_ALL && ItemFind._ItemData.Category != (int)currentWeaponCategory)
                    {
                        PlayerManager.ShowMessageBox(this._Player, "Thông báo",
                            $"Vật phẩm yêu cầu trang bị vũ khí loại {ItemManager.GetWeaponKind(ItemFind._ItemData.Category)}. Vui lòng điều chỉnh lại vũ khí trước khi sử dụng.");
                        return false;
                    }

                    return true;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void RefreshEquip()
        {
            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from RefreshEquip() in KPlayerEquipBody.cs");
            this.ClearAllEffecEquipBody();

            LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from RefreshEquip() in KPlayerEquipBody.cs");
            this.AttackAllEquipBody();
        }

        public bool IsEquipCompatibleToLevel(int level) 
        {
            // check yêu cầu level của bộ chính
            lock (EquipBody)
            {
                foreach (var equip in EquipBody.Values)
                {
                    var requirementLevel = equip._ItemData.ListReqProp.FirstOrDefault(x => x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_LEVEL);
                    if (requirementLevel == null)
                        continue;
                    if (requirementLevel.ReqPropValue > level)
                        return false;
                }
            }
            // check yêu cầu level của bộ phụ
            lock (PreventiveEquipBody)
            {
                foreach (var equip in PreventiveEquipBody.Values)
                {
                    var requirementLevel = equip._ItemData.ListReqProp.FirstOrDefault(x => x.ReqPropType == (int)KE_ITEM_REQUIREMENT.emEQUIP_REQ_LEVEL);
                    if (requirementLevel == null)
                        continue;
                    if (requirementLevel.ReqPropValue > level)
                        return false;
                }
            }
            return true;
        }
    }
}
