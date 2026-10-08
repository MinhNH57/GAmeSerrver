using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.GameEvents.BaoThach;
using GameServer.KiemThe.Utilities;
using static GameServer.KiemThe.Core.Item.ItemEnhance;

namespace GameServer.KiemThe.Core.Item
{
    /// <summary>
    /// Định nghĩa khi gọi 1 item mới in game để tính toán +- thuộc tính
    /// </summary>
    public class KItem
    {
        public int ItemDBID
        {
            get
            {
                return _GoodDatas.Id;
            }
        }

        public int SuiteID
        {
            get
            {
                return GetBaseItem.SuiteID;
            }
        }

        public ItemData GetBaseItem
        {
            get
            {
                return _ItemData;
            }
        }

        public bool active_all_ornament { get; set; }

        public bool active_suit { get; set; }

        public List<KMagicAttrib> TotalBaseAttrib { get; set; }

        public List<KREQUIRE_ATTR> TotalRequest { get; set; }

        public List<KSOCKET> TotalEnhance { get; set; }

        public List<KMagicAttrib> TotalGreenProp { get; set; }
        public List<KMagicAttrib> TotalExtProp { get; set; }
        public List<KSkillAttrib> TotalSkillProp { get; set; }
        public List<KSkillAttrib> TotalMTSkillProp { get; set; }
        public List<KSkillAttrib> TotalGreenSkillProp { get; set; }

        public List<KSOCKET> TotalHiddenProp { get; set; }

        public List<KMagicAttrib> BookAtriibute { get; set; }

        public List<KMagicAttrib> PetAtribute { get; set; }

        public List<KMagicAttrib> TotalRiderProp { get; set; }

        /// <summary>
        /// Các kỹ năng có được từ mật tịch
        /// </summary>
        public List<int> SkillsFromBook
        {
            get
            {
                /// Nếu BookProperty NULL thì toác
                if (this._ItemData.BookProperty == null)
                {
                    return null;
                }

                return new List<int>()
                {
                    this._ItemData.BookProperty.SkillID1,
                    this._ItemData.BookProperty.SkillID2,
                    this._ItemData.BookProperty.SkillID3,
                    this._ItemData.BookProperty.SkillID4,
                };
            }
        }
        /// <summary>
        /// Danh sách kỹ năng của pet
        /// </summary>
        public List<int> SkillsFromPet { get; set; }

        public List<int> SkillsArtifact { 
            get {
                if (this._ItemData.ArtifactProperty == null)
                {
                    var skillID1 = ItemManager.GetSkillTBBNHK(this._ItemData.ItemID);
                    if ( skillID1== -1)
                        return null;
                    else
                    {
                        return new List<int>
                        {
                            skillID1,
                            -1,
                            -1,
                            -1
                        };
                    }
                }
                return new List<int>
                {
                    this._ItemData.ArtifactProperty.SkillID1,
                    this._ItemData.ArtifactProperty.SkillID2,
                    this._ItemData.ArtifactProperty.SkillID3,
                    this._ItemData.ArtifactProperty.SkillID4,
                };
            } 
        }

        public ItemData _ItemData { get; set; }

        public KE_ITEM_EQUIP_DETAILTYPE TypeItem { get; set; }

        public bool ISEQUIP { get; set; }

        /// <summary>
        /// Cấp độ cường hóa
        /// </summary>
        public int Forge_Level { get; set; }

        public GoodsData _GoodDatas { get; set; }

        /// <summary>
        /// Attack Effect TO PLAYER
        /// </summary>
        /// <param name="_Player"></param>
        public void AttackEffect(KPlayer _Player, bool IsDetack)
        {
            /// Nếu là Ngũ Hành Ấn
            if (ItemManager.KD_ISSIGNET(this._ItemData.DetailType))
            {
                /// Lấy ra thông tin dòng cường hóa ngũ hành tương khắc
                if (this._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string seriesEnhance))
                {
                    try
                    {
                        string[] fields = seriesEnhance.Split('|');
                        int level = int.Parse(fields[0]);

                        /// Tạo KMagicAttrib
                        KMagicAttrib magicAttrib = new KMagicAttrib()
                        {
                            nAttribType = MAGIC_ATTRIB.magic_seriesenhance,
                            nValue = new int[] { level, 0, 0 }
                        };
                        /// Attach thuộc tính
                        KTAttributesModifier.AttachProperty(magicAttrib, _Player, IsDetack);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                    }
                }
                /// Lấy ra thông tin dòng nhược hóa ngũ hành tương khắc
                if (this._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string seriesConque))
                {
                    try
                    {
                        string[] fields = seriesConque.Split('|');
                        int level = int.Parse(fields[0]);

                        /// Tạo KMagicAttrib
                        KMagicAttrib magicAttrib = new KMagicAttrib()
                        {
                            nAttribType = MAGIC_ATTRIB.magic_seriesabate,
                            nValue = new int[] { level, 0, 0 }
                        };
                        /// Attach thuộc tính
                        KTAttributesModifier.AttachProperty(magicAttrib, _Player, IsDetack);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                    }
                }
            }
            // else
            {
                if (this.TotalBaseAttrib != null)
                {
                    if (this.TotalBaseAttrib.Count > 0)
                    {
                        // Attack Base EFFECT
                        foreach (KMagicAttrib _KMagic in this.TotalBaseAttrib) {

                            if (ItemManager.KD_ISSIGNET(this._ItemData.DetailType) &&
                                (_KMagic.nAttribType == MAGIC_ATTRIB.magic_seriesabate
                                 || _KMagic.nAttribType == MAGIC_ATTRIB.magic_seriesenhance))
                            {
                                // đã attach ở trên, không tính ở dưới
                                continue;
                            }

                            KTAttributesModifier.AttachProperty(_KMagic, _Player, IsDetack);
                        }
                    }
                }
            }

            if (TotalGreenProp != null)
            {
                if (this.TotalGreenProp.Count > 0)
                {
                    //Attack Green Prob
                    foreach (KMagicAttrib _KMagic in this.TotalGreenProp)
                    {
                        KTAttributesModifier.AttachProperty(_KMagic, _Player, IsDetack);
                    }
                }
            }

            if (this.TotalEnhance != null)
            {
                if (this.TotalEnhance.Count > 0)
                {
                    // Attack Enhance Efect
                    foreach (KSOCKET _KSocket in this.TotalEnhance)
                    {
                        if (_KSocket.bActive)
                        {
                            KTAttributesModifier.AttachProperty(_KSocket.sMagicAttrib, _Player, IsDetack);
                        }
                    }
                }
            }
            
            if (this.TotalHiddenProp != null)
            {
                if (this.TotalHiddenProp.Count > 0)
                {
                    // Attack Hidden Prob
                    foreach (KSOCKET _KSocket in this.TotalHiddenProp)
                    {
                        if (_KSocket.bActive)
                        {
                            KTAttributesModifier.AttachProperty(_KSocket.sMagicAttrib, _Player, IsDetack);
                        }
                    }
                }
            }

            if (this.SkillsArtifact != null)
            {
                var itemArtifact = new ArtifactInfo()
                {
                    GoodID = this._GoodDatas.GoodsID,
                    Skills = new List<int>(),
                    Param3 = ""
                };

                if (_Player.artifactInfos == null)
                    _Player.artifactInfos = new List<ArtifactInfo>();
                   
                /// Duyệt danh sách kỹ năng có
                foreach (int skillID in this.SkillsArtifact)
                {
                    /// Nếu kỹ năng không tồn tại
                    if (KSkill.GetSkillData(skillID) == null)
                    {
                        continue;
                    }
                    itemArtifact.Skills.Add(skillID);
                    if (this._GoodDatas.OtherParams != null)
                    {
                        if (this._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var level))
                        {
                            var param3 = level.Split('|');
                            if (Convert.ToInt32(param3[0]) > 0)
                            {
                                var endtime = DateTime.Parse(param3[1]);
                                if (endtime >= DateTime.Now)
                                {
                                    /// Nếu chưa có kỹ năng trên người
                                    if (!_Player.Skills.HasSkill(skillID))
                                    {
                                        _Player.Skills.AddSkillWithLevel(skillID, int.Parse(param3[0]));
                                    }
                                    else
                                    {
                                        _Player.Skills.ResetSkillLevel(skillID);
                                        _Player.Skills.AddSkillLevel(skillID, int.Parse(param3[0]));
                                    }

                                }
                            }
                            itemArtifact.Param3 = level;
                        }
                    }
                    
                    if (TotalGreenSkillProp != null)
                    {
                        foreach (var e in TotalGreenSkillProp )
                        {
                            _Player.Skills.BonusLevelSkills.Add(new SkillLevelBonus(skillID,
                                e.value3));
                        }
                    }
                    //else
                    //{
                    //    _Player.Skills.AddSkillLevel(skillID, 1);
                    //}
                    
                }
                if (_Player.artifactInfos.Find(x => x.GoodID == this._GoodDatas.GoodsID) == null)
                {
                    _Player.artifactInfos.Add(itemArtifact);
                }                
            }    

            if (this.BookAtriibute != null)
            {
                if (this.BookAtriibute.Count > 0)
                {   // Attack Efffect OffBOOK
                    foreach (KMagicAttrib _KMagic in this.BookAtriibute)
                    {
                        KTAttributesModifier.AttachProperty(_KMagic, _Player, IsDetack);
                    }
                }

                /// Duyệt danh sách kỹ năng có
                foreach (int skillID in this.SkillsFromBook)
                {
                    /// Nếu kỹ năng không tồn tại
                    if (KSkill.GetSkillData(skillID) == null)
                    {
                        continue;
                    }

                    /// Nếu chưa có kỹ năng trên người
                    if (!_Player.Skills.HasSkill(skillID))
                    {
                        /// Thêm kỹ năng
                        _Player.Skills.AddSkill(skillID);
                        _Player.Skills.AddSkillLevel(skillID, 1);
                    }
                    else if (!IsDetack)
                    {
                        /// Tự động tính toán lại level để tránh lỗi reset về level 1 hoặc level 0
                        //_Player.Skills.RecomputeBookSkillLevel(skillID);
                    }
                    _Player.Skills.AddBookSkill(skillID, _ItemData.Level);
                }
            }

            if (this.PetAtribute != null)
            {
                if (this.PetAtribute.Count > 0)
                {   // Attack Efffect Pet
                    foreach (KMagicAttrib _KMagic in this.PetAtribute)
                    {
                        KTAttributesModifier.AttachProperty(_KMagic, _Player, IsDetack);
                    }
                }

                var LevelSkill = new List<int>();

                if (this._GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
                {
                    try
                    {
                        string[] fields = levelSkill.Split('|');
                        for (int i = 0; i < this.SkillsFromPet.Count; i++)
                        {
                            LevelSkill.Add(int.Parse(fields[i]));
                        }

                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                        return;
                    }
                }

                int index = 0;
                /// Duyệt danh sách kỹ năng có
                foreach (int skillID in this.SkillsFromPet)
                {
                    /// Nếu kỹ năng không tồn tại
                    if (KSkill.GetSkillData(skillID) == null)
                    {
                        continue;
                    }

                    if (!IsDetack)
                    {
                        /// Nếu chưa có kỹ năng trên người
                        if (!_Player.Skills.HasSkill(skillID))
                        {
                            /// Thêm kỹ năng
                            _Player.Skills.AddSkill(skillID);
                            _Player.Skills.AddSkillLevel(skillID, LevelSkill[index]);
                        }
                        index++;
                    }
                    else
                    {
                        // Xóa kĩ năng buff của Pet
                        _Player.Skills.RemoveSkill(skillID);
                    }
                }
            }
            
            // duyet danh sach cong them chi so

            if (this.TotalExtProp != null)
            {
                foreach (var magic in this.TotalExtProp)
                {
                    KTAttributesModifier.AttachProperty(magic, _Player, IsDetack);
                }
            }
            
            if (this.TotalSkillProp != null)
            {
                SkillDataEx ex = null;
                KSkillAttrib prop = null;
                foreach (var e in _Player.SkillDataList)
                {
                    ex = KSkill.GetSkillData(e.SkillID);
                    prop = TotalSkillProp.Find(x => x.value1 == ex.ID || x.value2 == ex.RequireLevel);
                    if (prop != null)
                    {
                        _Player.Skills.BonusLevelSkills.Add(new SkillLevelBonus(e.SkillID, prop.value3));
                    }
                }

            }
            
            // chỉ số cộng thêm
            if (TotalMTSkillProp != null)
            {
                foreach (var e in TotalMTSkillProp )
                {
                    _Player.Skills.BonusBookLevelSkills.Add(new SkillLevelBonus(e.value2, e.value3));
                }
            }
        }


        /// <summary>
        /// Thăng cấp Skill cho Pet, Kích hoạt luôn nếu đang mặc
        /// </summary>
        public void LevelUpSkillPet(KPlayer _Player, int index, int level)
        {
            if (this.SkillsFromPet == null || this.SkillsFromPet.Count <= index)
                return;

            int skillID = this.SkillsFromPet[index];
            _Player.Skills.AddSkillLevel(skillID, level);
        }
        /// <summary>
        /// Lấy ra tên skill của pet để thông báo
        /// </summary>
        public string GetNameSkill(KPlayer _Player, int index)
        {
            if (this.SkillsFromPet == null || this.SkillsFromPet.Count <= index)
                return "";

            int skillID = this.SkillsFromPet[index];
            var name = KSkill.GetSkillData(skillID).Name;
            return name;
        }
        /// <summary>
        /// Kích hoạt thuộc tính cưỡi ngựa
        /// </summary>
        /// <param name="player"></param>
        /// <param name="isDetach"></param>
        public void AttachRiderEffect(KPlayer player, bool isDetach)
        {
            if (this.TotalRiderProp.Count > 0)
            {
                foreach (KMagicAttrib _KMagic in this.TotalRiderProp)
                {
                    KTAttributesModifier.AttachProperty(_KMagic, player, isDetach);
                }
            }
        }

        public void InitHorseProperty()
        {
            List<RiderProp> RiderProp = this._ItemData.RiderProp;
            List<KMagicAttrib> TotalRiderPropEx = new List<KMagicAttrib>();

            var isPropEnhanced = this._ItemData.Category == 2;
            var forgeLevel = this._GoodDatas.Forge_level;
            
            if (RiderProp.Count > 0)
            {
                foreach (RiderProp _Prob in RiderProp)
                {
                    if (isPropEnhanced && (forgeLevel < _Prob.RidePropPA2Min || forgeLevel > _Prob.RidePropPA2Max))
                        continue;
                    
                    KMagicAttrib Atribute = new KMagicAttrib();

                    Atribute.Init(_Prob.RidePropType, _Prob.RidePropPA1Min, _Prob.RidePropPA2Min, _Prob.RidePropPA3Min);
                    TotalRiderPropEx.Add(Atribute);
                }
            }

            this.TotalRiderProp = TotalRiderPropEx;
        }

        /// <summary>
        /// DONE
        /// </summary>
        public void InitTotalEnhance()
        {
            this.Forge_Level = _GoodDatas.Forge_level;

            List<KSOCKET> TotalEnhance = new List<KSOCKET>();

            List<ENH> ListEnhance = this._ItemData.ListEnhance;

            if (ListEnhance.Count > 0)
            {
                foreach (ENH _Ech in ListEnhance)
                {
                    KSOCKET _Sock = new KSOCKET();

                    _Sock.Index = _Ech.Index;

                    KMagicAttrib Magic = new KMagicAttrib();

                    if (Magic.Init(_Ech.EnhMAName, _Ech.EnhMAPA1Min, _Ech.EnhMAPA2Min, _Ech.EnhMAPA3Min))
                    {
                        _Sock.sMagicAttrib = Magic;
                        if (this.Forge_Level >= _Ech.EnhTimes)
                        {
                            if (_Ech.EnhMAName == "active_all_ornament")
                            {
                                active_all_ornament = true;
                            }
                            if (_Ech.EnhMAName == "active_suit")
                            {
                                active_suit = true;
                            }

                            /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                            if (_Ech.EnhMAName == "damage_series_resist")
                            {
                                Magic.nValue[2] = GetResValue(this._GoodDatas.Series);
                            }
                            /// Nếu là dòng tỷ lệ bỏ qua kháng
                            else if (_Ech.EnhMAName == "ignoreresist_p")
                            {
                                Magic.nValue[2] = this._GoodDatas.Series;
                            }

                            _Sock.bActive = true;
                        }
                        else
                        {
                            _Sock.bActive = false;
                        }

                        TotalEnhance.Add(_Sock);
                    }
                    else
                    {
                        throw new System.ArgumentException("Symbol not found : " + _Ech.EnhMAName);
                    }
                }
            }

            this.TotalEnhance = TotalEnhance;
        }

        public void Update(KPlayer _Player, bool IsActiveOrnament = false, bool IsActiveAllSuite = false)
        {
            int ACTIVE = 0;

            int PlayerSeri = (int)_Player.m_Series;

            int ItemSeri = this._GoodDatas.Series;

            // Nếu nhân vật và vật phẩm có quan hệ tương sinh thì + 1
            if (KTGlobal.g_IsAccrue(PlayerSeri, ItemSeri))
            {
                ACTIVE++;
            }

            int nPlace = ItemManager.g_anEquipPos[_ItemData.DetailType];

            /// Nếu không có dòng ẩn
            if (!ItemManager.g_anEquipActive.TryGetValue(nPlace, out ActiveByItem activeCheck))
            {
                return;
            }

            int Postion1 = activeCheck.Pos1;

            int Postion2 = activeCheck.Pos2;

            KItem _Item1 = _Player._KPlayerEquipBody.GetItemByPostion(Postion1);

            if (_Item1 != null)
            {
                // Nếu vật phẩm và vật phẩm thứ 1 yêu cầu kích hoạt có quan hệ tương sinh thì +1
                if (KTGlobal.g_IsAccrue(_Item1._GoodDatas.Series, ItemSeri))
                {
                    ACTIVE++;
                }
            }

            KItem _Item2 = _Player._KPlayerEquipBody.GetItemByPostion(Postion2);
            if (_Item2 != null)
            {
                // Nếu vật phẩm và vật phẩm thứ 2 yêu cầu kích hoạt có quan hệ tương sinh thì +1
                if (KTGlobal.g_IsAccrue(_Item2._GoodDatas.Series, ItemSeri))
                {
                    ACTIVE++;
                }
            }

            // DETACK ALL SOCKET
            if (this.TotalHiddenProp != null)
            {
                if (this.TotalHiddenProp.Count > 0)
                {
                    foreach (KSOCKET _Socket in this.TotalHiddenProp)
                    {
                        _Socket.bActive = false;
                    }
                }
            }

            if (ACTIVE >= 1)
            {
                if (this.TotalHiddenProp != null)
                {
                    if (this.TotalHiddenProp.Count > 0)
                    {
                        this.TotalHiddenProp[0].bActive = true;
                    }
                }
            }

            if (ACTIVE >= 2)
            {
                if (this.TotalHiddenProp != null)
                {
                    if (this.TotalHiddenProp.Count > 1)
                    {
                        this.TotalHiddenProp[1].bActive = true;
                    }
                }
            }
            if (ACTIVE >= 3)
            {
                if (this.TotalHiddenProp != null)
                {
                    if (this.TotalHiddenProp.Count > 2)
                    {
                        this.TotalHiddenProp[2].bActive = true;
                    }
                }
            }

            if (IsActiveOrnament)
            {
                if (ItemManager.KD_ISORNAMENT(this._ItemData.DetailType))
                {
                    if (this.TotalHiddenProp != null)
                    {
                        if (this.TotalHiddenProp.Count > 0)
                        {
                            this.TotalHiddenProp[0].bActive = true;
                        }

                        if (this.TotalHiddenProp.Count > 1)
                        {
                            this.TotalHiddenProp[1].bActive = true;
                        }
                        if (this.TotalHiddenProp.Count > 2)
                        {
                            this.TotalHiddenProp[2].bActive = true;
                        }
                    }
                }
            }

            if (IsActiveAllSuite)
            {
                if (this.TotalHiddenProp != null)
                {
                    if (this.TotalHiddenProp.Count > 0)
                    {
                        this.TotalHiddenProp[0].bActive = true;
                    }

                    if (this.TotalHiddenProp.Count > 1)
                    {
                        this.TotalHiddenProp[1].bActive = true;
                    }
                    if (this.TotalHiddenProp.Count > 2)
                    {
                        this.TotalHiddenProp[2].bActive = true;
                    }
                }
            }
        }

        public void InitHiddenProbs()
        {
            byte[] Base64Decode;

            try
            {
                Base64Decode = Convert.FromBase64String(_GoodDatas.Props);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, "KItem.InitHiddenProbs: Props is not Base64 string");
                return;
            }

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);

            if (_ItemBuild != null)
            {
                if (_ItemBuild.HiddenProbsCount > 0)
                {
                    List<KSOCKET> TotalHiddenEffect = new List<KSOCKET>();

                    List<PropMagic> List = _ItemData.HiddenProp.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < _ItemBuild.HiddenProbsCount; i++)
                    {
                        KSOCKET _Sock = new KSOCKET();

                        int PostionGet = (i * 3);

                        KMagicAttrib Atribute = new KMagicAttrib();

                        int VALUE0 = _ItemBuild.HiddenProbsValue[PostionGet];
                        int VALUE1 = _ItemBuild.HiddenProbsValue[PostionGet + 1];
                        int VALUE2 = _ItemBuild.HiddenProbsValue[PostionGet + 2];

                        int LevelMagic = this._GoodDatas.Forge_level + List[i].MagicLevel;

                        //ORIGIN VALUE
                        MagicAttribLevel FindOrginalValue = ItemManager.TotalMagicAttribLevel.Where(x => x.MagicName == List[i].MagicName && x.Level == List[i].MagicLevel).FirstOrDefault();

                        MagicAttribLevel FindMagic = ItemManager.TotalMagicAttribLevel.Where(x => x.MagicName == List[i].MagicName && x.Level == LevelMagic).FirstOrDefault();

                        int _Value0 = 0, _Value1 = 0, _Value2 = 0;

                        if (VALUE0 != -1)
                        {
                            if (FindMagic != null)
                            {
                                int Percent0 = RecaculationPercent(FindOrginalValue.MA1Min, FindOrginalValue.MA1Max, VALUE0);

                                if (Percent0 > 0)
                                {
                                    int AddValue = ((FindMagic.MA1Max - FindMagic.MA1Min) * Percent0) / 100;

                                    _Value0 = FindMagic.MA1Min + AddValue;
                                }
                                else
                                {
                                    _Value0 = FindMagic.MA1Min;
                                }
                            }
                        }

                        if (VALUE1 != -1)
                        {
                            if (FindMagic != null)
                            {
                                int Percent1 = RecaculationPercent(FindOrginalValue.MA2Min, FindOrginalValue.MA2Max, VALUE1);
                                if (Percent1 > 0)
                                {
                                    int AddValue = ((FindMagic.MA2Max - FindMagic.MA2Min) * Percent1) / 100;

                                    _Value1 = FindMagic.MA2Min + AddValue;
                                }
                                else
                                {
                                    _Value1 = FindMagic.MA2Min;
                                }
                            }
                        }
                        if (VALUE2 != -1)
                        {
                            if (FindMagic != null)
                            {
                                int Percent2 = RecaculationPercent(FindOrginalValue.MA3Min, FindOrginalValue.MA3Max, VALUE2);

                                if (Percent2 > 0)
                                {
                                    int AddValue = ((FindMagic.MA3Max - FindMagic.MA3Min) * Percent2) / 100;

                                    _Value2 = FindMagic.MA3Min + AddValue;
                                }
                                else
                                {
                                    _Value2 = FindMagic.MA3Min;
                                }
                            }
                        }

                        /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                        if (List[i].MagicName == "damage_series_resist")
                        {
                            _Value2 = GetResValue(this._GoodDatas.Series);
                        }
                        /// Nếu là dòng tỷ lệ bỏ qua kháng
                        else if (List[i].MagicName == "ignoreresist_p")
                        {
                            _Value2 = this._GoodDatas.Series;
                        }

                        if (Atribute.Init(List[i].MagicName, _Value0, _Value1, _Value2))
                        {
                            // SET ALL ACTIVE FAIL
                            _Sock.Index = _ItemData.HiddenProp[i].Index;
                            _Sock.sMagicAttrib = Atribute;
                            _Sock.bActive = false;

                            TotalHiddenEffect.Add(_Sock);
                        }
                        else
                        {
                            throw new System.ArgumentException("Symboy not found : " + List[i].MagicName);
                        }
                    }

                    this.TotalHiddenProp = TotalHiddenEffect;
                }
            }
        }

        public void InitBookProperty()
        {
            byte[] Base64Decode = Convert.FromBase64String(_GoodDatas.Props);

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);
            List<KMagicAttrib> BookProfile = new List<KMagicAttrib>();

            if (_ItemBuild.HaveBookProperties)
            {
                if (_GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string BookLevel))
                {
                    int LevelBook = Int32.Parse(BookLevel);

                    BookAttr FindBook = ItemManager.GetItemTemplate(_GoodDatas.GoodsID).BookProperty;

                    int StrBase = FindBook.StrInitMin / 100;
                    int DevBase = FindBook.DexInitMin / 100;
                    int VitBase = FindBook.VitInitMin / 100;
                    int EngBase = FindBook.EngInitMin / 100;

                    if (_ItemBuild.BookPropertyValue[0] != -1)
                    {
                        int PowerDiv = _ItemData.FightPower;

                        int PointRevice = PowerDiv * LevelBook;

                        int STRFILNALADD = StrBase + _ItemBuild.BookPropertyValue[0] + PointRevice;

                        KMagicAttrib Atribute = new KMagicAttrib();
                        Atribute.nAttribType = MAGIC_ATTRIB.magic_strength_v;
                        Atribute.nValue[0] = STRFILNALADD;
                        Atribute.nValue[1] = -1;
                        Atribute.nValue[2] = -1;

                        BookProfile.Add(Atribute);
                    }

                    if (_ItemBuild.BookPropertyValue[1] != -1)
                    {
                        int PowerDiv = _ItemData.FightPower;

                        int PointRevice = PowerDiv * LevelBook;

                        int DEVFINAL = DevBase + _ItemBuild.BookPropertyValue[1] + PointRevice;

                        KMagicAttrib Atribute = new KMagicAttrib();
                        Atribute.nAttribType = MAGIC_ATTRIB.magic_dexterity_v;
                        Atribute.nValue[0] = DEVFINAL;
                        Atribute.nValue[1] = -1;
                        Atribute.nValue[2] = -1;

                        BookProfile.Add(Atribute);
                    }

                    if (_ItemBuild.BookPropertyValue[2] != -1)
                    {
                        int PowerDiv = _ItemData.FightPower;

                        int PointRevice = PowerDiv * LevelBook;

                        int VitFinal = VitBase + _ItemBuild.BookPropertyValue[2] + PointRevice;

                        KMagicAttrib Atribute = new KMagicAttrib();
                        Atribute.nAttribType = MAGIC_ATTRIB.magic_vitality_v;
                        Atribute.nValue[0] = VitFinal;
                        Atribute.nValue[1] = -1;
                        Atribute.nValue[2] = -1;

                        BookProfile.Add(Atribute);
                    }
                    if (_ItemBuild.BookPropertyValue[3] != -1)
                    {
                        int PowerDiv = _ItemData.FightPower;

                        int PointRevice = PowerDiv * LevelBook;

                        int EngFinal = EngBase + _ItemBuild.BookPropertyValue[3] + PointRevice;

                        KMagicAttrib Atribute = new KMagicAttrib();
                        Atribute.nAttribType = MAGIC_ATTRIB.magic_energy_v;
                        Atribute.nValue[0] = EngFinal;
                        Atribute.nValue[1] = -1;
                        Atribute.nValue[2] = -1;

                        BookProfile.Add(Atribute);
                    }
                }
            }
            this.BookAtriibute = BookProfile;
        }

        /// <summary>
        /// khởi tạo thuộc tính của pet
        /// </summary>
        public void InitPetProperty()
        {
            byte[] Base64Decode = Convert.FromBase64String(_GoodDatas.Props);

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);
            List<KMagicAttrib> PetProfile = new List<KMagicAttrib>();

            if (_ItemBuild.HavePetProperties)
            {
                int LevelPet = _GoodDatas.Forge_level;

                PetAttr FindPet = ItemManager.GetItemTemplate(_GoodDatas.GoodsID).PetProperty;
                int numberSkill = PetManager.GetNumberSkill(_GoodDatas);

                var info = PetManager.InfoPets.Pet_Point.Where(x => x.Star == numberSkill).FirstOrDefault();
                if(PetManager.IsYenTieuLau(_GoodDatas.GoodsID))
                {
                    info = PetManager.YenTieuLauOverride.Pet_Point.Where( x => x.Star == numberSkill ).FirstOrDefault();
                }    
                int power = 5;
                if (info != null)
                {
                    power = info.Point;
                }
                //Tinh tổng chỉ số rồi chia theo tỷ lệ đã lưu trong pet từ trước
                int TotalValue = FindPet.TotalAttr + power * LevelPet * 100;


                if (_ItemBuild.PetPropertyValue[0] != -1)
                {
                    int StrBase = TotalValue * _ItemBuild.PetPropertyValue[0] / 10000;

                    int STRFILNALADD = StrBase;

                    KMagicAttrib Atribute = new KMagicAttrib();
                    Atribute.nAttribType = MAGIC_ATTRIB.magic_strength_v;
                    Atribute.nValue[0] = STRFILNALADD;
                    Atribute.nValue[1] = -1;
                    Atribute.nValue[2] = -1;

                    PetProfile.Add(Atribute);
                }

                if (_ItemBuild.PetPropertyValue[1] != -1)
                {
                    int DevBase = TotalValue * _ItemBuild.PetPropertyValue[1] / 10000;

                    int DEVFINAL = DevBase;

                    KMagicAttrib Atribute = new KMagicAttrib();
                    Atribute.nAttribType = MAGIC_ATTRIB.magic_dexterity_v;
                    Atribute.nValue[0] = DEVFINAL;
                    Atribute.nValue[1] = -1;
                    Atribute.nValue[2] = -1;

                    PetProfile.Add(Atribute);
                }

                if (_ItemBuild.PetPropertyValue[2] != -1)
                {
                    int VitBase = TotalValue * _ItemBuild.PetPropertyValue[2] / 10000;

                    int VitFinal = VitBase;

                    KMagicAttrib Atribute = new KMagicAttrib();
                    Atribute.nAttribType = MAGIC_ATTRIB.magic_vitality_v;
                    Atribute.nValue[0] = VitFinal;
                    Atribute.nValue[1] = -1;
                    Atribute.nValue[2] = -1;

                    PetProfile.Add(Atribute);
                }

                if (_ItemBuild.PetPropertyValue[3] != -1)
                {
                    int EngBase = TotalValue * _ItemBuild.PetPropertyValue[3] / 10000;

                    int EngFinal = EngBase;

                    KMagicAttrib Atribute = new KMagicAttrib();
                    Atribute.nAttribType = MAGIC_ATTRIB.magic_energy_v;
                    Atribute.nValue[0] = EngFinal;
                    Atribute.nValue[1] = -1;
                    Atribute.nValue[2] = -1;

                    PetProfile.Add(Atribute);
                }
            }

            this.PetAtribute = PetProfile;

            if (_ItemBuild.PetPropertyValue.Count <= 4 || SkillsFromPet != null)
                return;
            // lưu lại danh sách kỹ năng của pet
            SkillsFromPet = new List<int>();
            for (int i = _ItemBuild.PetPropertyValue.Count - 1; i > 3; i--)
            {
                SkillsFromPet.Add(_ItemBuild.PetPropertyValue[i]);
            }
        }

        public int RecaculationPercent(int MinValue, int MaxValue, int CurenValue)
        {
            if (MaxValue == MinValue)
            {
                return 0;
            }
            return (CurenValue * 100) / (MaxValue - MinValue);
        }

        /// <summary>
        /// Khởi tạo thuộc tính Base của Item
        /// Bao gồm Base Atrib + Green Atrib
        /// </summary>
        public void InitBaseAttribParse()
        {
            byte[] Base64Decode = null;

            try
            {
                Base64Decode = Convert.FromBase64String(_GoodDatas.Props);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, "KItem.InitBaseAttribParse: Props is not Base64 string");
                return;
            }

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);

            var isPropEnhanced = this._ItemData.DetailType == (int)KE_ITEM_EQUIP_DETAILTYPE.equip_mask && this._ItemData.Category == 2;
            isPropEnhanced = isPropEnhanced || (
                // trường hợp ngoại trang vũ khí
                this._ItemData.DetailType == (int)KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitWeapon && this._ItemData.Category == 0
            );
            var forgeLevel = this._GoodDatas.Forge_level;

            if (_ItemBuild != null)
            {
                if (_ItemBuild.BasicPropCount > 0)
                {
                    List<KMagicAttrib> TotalBaseAttrib = new List<KMagicAttrib>();

                    List<BasicProp> List = _ItemData.ListBasicProp.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < _ItemBuild.BasicPropCount; i++)
                    {
                        // nếu là trang bị có chỉ số lọc theo cấp cường hoá thì check cấp cường hoá
                        if (isPropEnhanced && (forgeLevel < List[i].BasicPropPA2Min || forgeLevel > List[i].BasicPropPA2Max))
                            continue;
                        
                        int PostionGet = (i * 3);

                        KMagicAttrib Atribute = new KMagicAttrib();

                        int VALUE0 = _ItemBuild.BasicPropValue[PostionGet];
                        int VALUE1 = _ItemBuild.BasicPropValue[PostionGet + 1];
                        int VALUE2 = _ItemBuild.BasicPropValue[PostionGet + 2];

                        int _Value0 = 0, _Value1 = 0, _Value2 = 0;

                        if (VALUE0 != -1)
                        {
                            _Value0 = List[i].BasicPropPA1Min + VALUE0;
                        }

                        if (VALUE1 != -1)
                        {
                            _Value1 = List[i].BasicPropPA2Min + VALUE1;
                        }
                        if (VALUE2 != -1)
                        {
                            _Value2 = List[i].BasicPropPA3Min + VALUE2;
                        }

                        /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                        if (List[i].BasicPropType == "damage_series_resist")
                        {
                            _Value2 = GetResValue(this._GoodDatas.Series);
                        }
                        /// Nếu là dòng tỷ lệ bỏ qua kháng
                        else if (List[i].BasicPropType == "ignoreresist_p")
                        {
                            _Value2 = this._GoodDatas.Series;
                        }

                        if (Atribute.Init(List[i].BasicPropType, _Value0, _Value1, _Value2))
                        {
                            TotalBaseAttrib.Add(Atribute);
                        }
                        else
                        {
                            throw new System.ArgumentException("Symbol not found : " + List[i].BasicPropType);
                        }
                    }

                    this.TotalBaseAttrib = TotalBaseAttrib;
                }

                //TODO CÁI NÀY CẦN TÍNH MIN MAX
                if (_ItemBuild.GreenPropCount > 0)
                {
                    List<KMagicAttrib> TotalGreenProb = new List<KMagicAttrib>();

                    List<PropMagic> List = _ItemData.GreenProp.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < _ItemBuild.GreenPropCount; i++)
                    {
                        int PostionGet = (i * 3);

                        KMagicAttrib Atribute = new KMagicAttrib();

                        int VALUE0 = _ItemBuild.GreenPropValue[PostionGet];
                        int VALUE1 = _ItemBuild.GreenPropValue[PostionGet + 1];
                        int VALUE2 = _ItemBuild.GreenPropValue[PostionGet + 2];

                        // tính toán ra percent max min

                        MagicAttribLevel FindOrginalValue = ItemManager.TotalMagicAttribLevel.Where(x => x.MagicName == List[i].MagicName && x.Level == List[i].MagicLevel).FirstOrDefault();

                        int _Value0 = 0, _Value1 = 0, _Value2 = 0;

                        int LevelMagic = this._GoodDatas.Forge_level + List[i].MagicLevel;
                        int maxLevel = MaxLevelGreenProp.GetMaxLevel(_ItemData.ItemID, List[i].MagicName);
                        if (maxLevel > 0)
                        {
                            LevelMagic = Math.Min(LevelMagic, maxLevel);
                        }

                        MagicAttribLevel FindMagic = ItemManager.TotalMagicAttribLevel.Where(x => x.MagicName == List[i].MagicName && x.Level == LevelMagic).FirstOrDefault();

                        if (VALUE0 != -1)
                        {
                            if (FindMagic != null)
                            {
                                int Percent0 = RecaculationPercent(FindOrginalValue.MA1Min, FindOrginalValue.MA1Max, VALUE0);

                                if (Percent0 > 0)
                                {
                                    int AddValue = ((FindMagic.MA1Max - FindMagic.MA1Min) * Percent0) / 100;

                                    _Value0 = FindMagic.MA1Min + AddValue;
                                }
                                else
                                {
                                    _Value0 = FindMagic.MA1Min;
                                }
                            }
                        }

                        if (VALUE1 != -1)
                        {
                            int Percent1 = RecaculationPercent(FindOrginalValue.MA2Min, FindOrginalValue.MA2Max, VALUE1);
                            if (Percent1 > 0)
                            {
                                int AddValue = ((FindMagic.MA2Max - FindMagic.MA2Min) * Percent1) / 100;

                                _Value1 = FindMagic.MA2Min + AddValue;
                            }
                            else
                            {
                                _Value1 = FindMagic.MA2Min;
                            }
                        }
                        if (VALUE2 != -1)
                        {
                            int Percent2 = RecaculationPercent(FindOrginalValue.MA3Min, FindOrginalValue.MA3Max, VALUE2);

                            if (Percent2 > 0)
                            {
                                int AddValue = ((FindMagic.MA3Max - FindMagic.MA3Min) * Percent2) / 100;

                                _Value2 = FindMagic.MA3Min + AddValue;
                            }
                            else
                            {
                                _Value2 = FindMagic.MA3Min;
                            }
                        }

                        /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                        if (List[i].MagicName == "damage_series_resist")
                        {
                            _Value2 = GetResValue(this._GoodDatas.Series);
                        }
                        /// Nếu là dòng tỷ lệ bỏ qua kháng
                        else if (List[i].MagicName == "ignoreresist_p")
                        {
                            _Value2 = this._GoodDatas.Series;
                        }

                        if (Atribute.Init(List[i].MagicName, _Value0, _Value1, _Value2))
                        {
                            TotalGreenProb.Add(Atribute);
                        }
                        else
                        {
                            throw new System.ArgumentException("Symboy not found : " + List[i].MagicName);
                        }
                    }

                    this.TotalGreenProp = TotalGreenProb;
                }
            }
        }

        public void InitExternAttribute()
        {
            if (this.TotalExtProp == null)
                this.TotalExtProp = new List<KMagicAttrib>();
            if (this.TotalSkillProp == null)
                this.TotalSkillProp = new List<KSkillAttrib>();
            if (this.TotalMTSkillProp == null)
                this.TotalMTSkillProp = new List<KSkillAttrib>();
            if (this.TotalGreenSkillProp == null)
                this.TotalGreenSkillProp = new List<KSkillAttrib>();
            var total = new List<KMagicAttrib>(); // for magic
            var totalSkill = new List<KSkillAttrib>(); // for skill normal
            var totalMT = new List<KSkillAttrib>(); // for skill mat tich
            var totalGreen = new List<KSkillAttrib>(); // for skill green
            if (_GoodDatas.OtherParams == null || this._GoodDatas.OtherParams.Count == 0)
                return;
            if (_GoodDatas.OtherParams.TryGetValue(ItemPramenter.Hole, out string stringdata))
            {
                BaoThachUserData data = BaoThachUserData.GetFromStringData(stringdata);
                if (data != null)
                {
                    foreach (var hole in data.holes)
                    {
                        if (hole == null || hole.itemGoodsId == 0)
                            continue;
                        switch (hole.type)
                        {
                            case (int)BaoThachType.Magic:
                                var att = BaoThachManager.CreateMagicAttribute(hole.magicName, hole.magicValue);
                                total.Add(att);
                                break;
                            case (int)BaoThachType.Level:
                                totalSkill.Add(new KSkillAttrib(-1, int.Parse(hole.magicName), hole.magicValue));
                                break;
                            case (int)BaoThachType.MTLevel:
                                totalMT.Add(new KSkillAttrib(-1, int.Parse(hole.magicName), hole.magicValue));
                                break;
                            case (int)BaoThachType.EnhArtifact:
                                totalGreen.Add(new KSkillAttrib(int.Parse(hole.magicName), -1, hole.magicValue));
                                break;
                        }
                    }
                }
            }
            this.TotalSkillProp.AddRange(totalSkill);
            this.TotalExtProp.AddRange(total);
            this.TotalMTSkillProp.AddRange(totalMT);
            this.TotalGreenSkillProp.AddRange(totalGreen);
        }
        
        public void InitChanNguyen()
        {
            byte[] Base64Decode = Convert.FromBase64String(_GoodDatas.Props);

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);
            if (_ItemData.Level == 2 && _GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
            {
                _ItemData.ListBasicProp.Clear();
                // thực hiện tạo danh sách thuộc tính
                var listType = param3.Split(';');
                for (var i = 0; i < listType.Count(); i++)
                {
                    _ItemData.ListBasicProp.Add(new BasicProp
                    {
                        BasicPropType = listType[i],
                        Index = (sbyte)(i + 1)
                    });
                }
            }

            if (_ItemBuild != null)
            {
                if (_ItemBuild.BasicPropCount > 0)
                {
                    List<KMagicAttrib> TotalBaseAttrib = new List<KMagicAttrib>();

                    List<BasicProp> List = _ItemData.ListBasicProp.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < _ItemBuild.BasicPropCount; i++)
                    {
                        int PostionGet = (i * 3);

                        KMagicAttrib Atribute = new KMagicAttrib();

                        int VALUE0 = _ItemBuild.BasicPropValue[PostionGet];
                        int VALUE1 = _ItemBuild.BasicPropValue[PostionGet + 1];
                        int VALUE2 = _ItemBuild.BasicPropValue[PostionGet + 2];

                        float _Value0 = 0, _Value1 = 0, _Value2 = 0;

                        if (VALUE0 != -1)
                        {
                            _Value0 = List[i].BasicPropPA1Min + VALUE0;
                        }

                        if (VALUE1 != -1)
                        {
                            _Value1 = List[i].BasicPropPA2Min + VALUE1;
                        }
                        if (VALUE2 != -1)
                        {
                            _Value2 = List[i].BasicPropPA3Min + VALUE2;
                        }
                        var propInfo = ChanNguyenManager._ListPropInfo.Where(x => x.Name == List[i].BasicPropType).FirstOrDefault();
                        if (propInfo != null)
                        {
                            float star = 0;
                            if (_GoodDatas.OtherParams != null)
                            {
                                if(_GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                                {
                                    var listProp = levelProps.Split(';');
                                    star = float.Parse(listProp[i]);
                                }
                            }
                            _Value0 += (star * 2 * propInfo.Point1);
                        }
                        if (propInfo.Name == "life_mana_replenish_p" || propInfo.Name == "life_mana_max_p" || propInfo.Name == "melee_range_damagereturn_p")
                        {
                            float star = 0;
                            if (_GoodDatas.OtherParams != null)
                            {
                                if (_GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                                {
                                    var listProp = levelProps.Split(';');
                                    star = float.Parse(listProp[i]);
                                }
                            }
                            _Value1 += (star * 2 * propInfo.Point2);
                        }

                        /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                        if (List[i].BasicPropType == "damage_series_resist")
                        {
                            _Value2 = GetResValue(this._GoodDatas.Series);
                        }
                        /// Nếu là dòng tỷ lệ bỏ qua kháng
                        else if (List[i].BasicPropType == "ignoreresist_p")
                        {
                            _Value2 = this._GoodDatas.Series;
                        }

                        if (Atribute.Init(List[i].BasicPropType, (int)_Value0, (int)_Value1, (int)_Value2))
                        {
                            TotalBaseAttrib.Add(Atribute);
                        }
                        else
                        {
                            throw new System.ArgumentException("Symbol not found : " + List[i].BasicPropType);
                        }
                    }

                    this.TotalBaseAttrib = TotalBaseAttrib;
                }
            }
        }

        private static int GetResValue(int series)
        {
            int resValue = 0;

            switch (series)
            {
                case 1:
                    resValue = 4;
                    break;

                case 2:
                    resValue = 3;
                    break;

                case 3:
                    resValue = 1;
                    break;

                case 4:
                    resValue = 0;
                    break;

                case 5:
                    resValue = 2;
                    break;
            }

            return resValue;
        }

        public void InitRequestItem()
        {
            List<KREQUIRE_ATTR> TotalRequest = new List<KREQUIRE_ATTR>();

            if (_ItemData.ListReqProp.Count > 0)
            {
                foreach (ReqProp req in _ItemData.ListReqProp)
                {
                    KREQUIRE_ATTR _Att = new KREQUIRE_ATTR();
                    _Att.eRequire = (KE_ITEM_REQUIREMENT)req.ReqPropType;
                    _Att.nValue = req.ReqPropValue;
                    TotalRequest.Add(_Att);
                }
                this.TotalRequest = TotalRequest;
            }
        }

        /// <summary>
        /// Khởi tạo thuộc tính Base của Item
        /// Bao gồm Base Atrib + Green Atrib
        /// </summary>
        public void InitBaseAttribSignetParse()
        {
            byte[] Base64Decode = Convert.FromBase64String(_GoodDatas.Props);

            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);

            if (_ItemBuild != null)
            {
                if (_ItemBuild.BasicPropCount > 0)
                {
                    List<KMagicAttrib> TotalBaseAttrib = new List<KMagicAttrib>();

                    List<BasicProp> List = _ItemData.ListBasicProp.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < _ItemBuild.BasicPropCount; i++)
                    {
                        int PostionGet = (i * 3);

                        KMagicAttrib Atribute = new KMagicAttrib();

                        int VALUE0 = _ItemBuild.BasicPropValue[PostionGet];
                        int VALUE1 = _ItemBuild.BasicPropValue[PostionGet + 1];
                        int VALUE2 = _ItemBuild.BasicPropValue[PostionGet + 2];

                        int _Value0 = 0, _Value1 = 0, _Value2 = 0;

                        if (VALUE0 != -1)
                        {
                            _Value0 = List[i].BasicPropPA1Min + VALUE0;
                        }

                        if (VALUE1 != -1)
                        {
                            _Value1 = List[i].BasicPropPA2Min + VALUE1;
                        }
                        if (VALUE2 != -1)
                        {
                            _Value2 = List[i].BasicPropPA3Min + VALUE2;
                        }

                        /// Nếu là dòng ngũ hành tương khắc hệ gì đó
                        if (List[i].BasicPropType == "damage_series_resist")
                        {
                            _Value2 = GetResValue(this._GoodDatas.Series);
                        }
                        /// Nếu là dòng tỷ lệ bỏ qua kháng
                        else if (List[i].BasicPropType == "ignoreresist_p")
                        {
                            _Value2 = this._GoodDatas.Series;
                        }

                        if (List[i].BasicPropType == "seriesenhance")
                        {
                            if (_GoodDatas.OtherParams != null && _GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string enhance))
                            {
                                string[] enhancePram = enhance.Split('|');

                                int nLevel = Int32.Parse(enhancePram[0]);

                                if (nLevel > 1)
                                {
                                    VALUE0 = VALUE0 + nLevel - 1;
                                }
                            }
                        }

                        if (List[i].BasicPropType == "seriesabate")
                        {
                            if (_GoodDatas.OtherParams != null && _GoodDatas.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string enhance))
                            {
                                string[] enhancePram = enhance.Split('|');

                                int nLevel = Int32.Parse(enhancePram[0]);

                                if (nLevel > 1)
                                {
                                    VALUE0 = VALUE0 + nLevel - 1;
                                     VALUE0 = VALUE0 + nLevel - 1;
                                }
                            }
                        }

                        if (Atribute.Init(List[i].BasicPropType, _Value0, _Value1, _Value2))
                        {
                            TotalBaseAttrib.Add(Atribute);
                        }
                        else
                        {
                            throw new System.ArgumentException("Symboy not found : " + List[i].BasicPropType);
                        }
                    }

                    this.TotalBaseAttrib = TotalBaseAttrib;
                }
            }
        }

        public void AbradeInDeath(int nPercent, KPlayer client)
        {
            // nếu độ bên fhiện tịa đã nhỏ hơn không thì bỏ qua
            if (this._GoodDatas.Strong <= 0)
                return;

            int nSub = this._GoodDatas.Strong * nPercent / 100;

            if (nSub > 0)
            {
                nSub = 1;

                this._GoodDatas.Strong = Math.Max(this._GoodDatas.Strong - 1, 0);

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, this._GoodDatas.Id);
                TotalUpdate.Add(UPDATEITEM.STRONG, this._GoodDatas.Strong);

                ItemManager.UpdateItemPrammenter(TotalUpdate, this._GoodDatas, client, "", false);

                PlayerManager.ShowNotification(client, "Độ bền của [" + ItemManager.GetNameItem(this._GoodDatas) + "] giảm đi 1");

                // Nếu độ bền mà khác không thì reload lại các chỉ số
                if (this._GoodDatas.Strong <= 0)
                {
                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from AbradeInDeath() in KItem.cs");
                    client.GetPlayEquipBody().ClearAllEffecEquipBody(this);

                    LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from AbradeInDeath() in KItem.cs");
                    client.GetPlayEquipBody().AttackAllEquipBody();
                }
            }
        }

        public void Abrade(int Value, KPlayer client)
        {
            // nếu độ bên fhiện tịa đã nhỏ hơn không thì bỏ qua
            if (this._GoodDatas.Strong <= 0)
                return;

            this._GoodDatas.Strong = Math.Max(this._GoodDatas.Strong - Value, 0);

            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, this._GoodDatas.Id);
            TotalUpdate.Add(UPDATEITEM.STRONG, this._GoodDatas.Strong);

            ItemManager.UpdateItemPrammenter(TotalUpdate, this._GoodDatas, client, "", false);

            PlayerManager.ShowNotification(client, "Độ bền của [" + ItemManager.GetNameItem(this._GoodDatas) + "] giảm đi 1");

            // Nếu độ bền mà khác không thì reload lại các chỉ số
            if (this._GoodDatas.Strong <= 0)
            {
                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] ClearAllEffecEquipBody called from Abrade() in KItem.cs");
                client.GetPlayEquipBody().ClearAllEffecEquipBody(this);
                LogManager.WriteLog(LogTypes.EquipBody, "[CallSite Log] AttackAllEquipBody called from Abrade() in KItem.cs");
                client.GetPlayEquipBody().AttackAllEquipBody();
            }
        }

        public KItem(GoodsData goodsData)
        {
            // SetItemData
            _ItemData = ItemManager.GetItemTemplate(goodsData.GoodsID);

            this.ISEQUIP = ItemManager.KD_ISEQUIP(_ItemData.Genre);
            this._GoodDatas = goodsData;
            this.TypeItem = ItemManager.GetItemType(_ItemData.DetailType);
            if (ISEQUIP)
            {
                this.active_all_ornament = false;
                this.active_suit = false;

                switch (TypeItem)
                {
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_meleeweapon:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_rangeweapon:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_armor:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_ring:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_necklace:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_amulet:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_boots:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_belt:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_helm:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_mask:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitArmor:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitHelmet:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitWeapon:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitRoleTitle:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_OutfitBuffAura:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_cuff:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_mantle:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_chop:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pendant:
                    case KE_ITEM_EQUIP_DETAILTYPE.equiq_pet_weapon:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_armor:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_ring:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_necklace:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_amulet:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_boots:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_belt:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_helm:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_cuff:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_pendant:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_medal:
                        {
                            InitBaseAttribParse();
                            InitExternAttribute();
                            InitRequestItem();
                            InitTotalEnhance();
                            InitHiddenProbs();
                            break;
                        }
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_book:
                        {
                            InitRequestItem();
                            InitBookProperty();

                            break;
                        }
                    case KE_ITEM_EQUIP_DETAILTYPE.equiq_pet:
                        {
                            InitRequestItem();
                            InitPetProperty();

                            break;
                        }
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_horse:
                        {
                            InitRequestItem();
                            InitHorseProperty();

                            break;
                        }
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_signet:
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_pet_signet:
                        {
                            InitRequestItem();
                            InitBaseAttribSignetParse();
                            InitBaseAttribParse();
                            InitTotalEnhance();

                            break;
                        }
                    case KE_ITEM_EQUIP_DETAILTYPE.equip_chan_nguyen:
                        {
                            InitRequestItem();
                            InitChanNguyen();
                            break;
                        }
                }
            }
            else // NẾU Không phải vật phẩm để mặc thì GS ko quan tâm. Client chỉ cần lấy để hiển thị ra các dòng DESC. Các vật phẩm kích hoạt sẽ sử dụng lua để kích hoạt script
            {
            }
        }
    }
}