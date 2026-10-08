using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Entities.Player;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.KiemThe.Logic.Manager.ChanNguyen;
using GameServer.KiemThe.Network.Data.Item;
using GameServer.Logic;
using GameServer.Server;
using Newtonsoft.Json;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Random = System.Random;

namespace GameServer.KiemThe.Logic.Manager.Pet
{
    public static class PetManager
    {
        #region Properties

        private static string KTPetInfo = "Config/KT_Item/PetItem/PetInfo.xml";
        private static string KTYenTieuLauInfo = "Config/KT_Item/PetItem/Pet_YenTieuLau.xml";

        /// <summary>
        /// Số bạn đồng hành tối đa có thể sở hữu
        /// </summary>
        public static int MaxPetUse = 10;

        /// <summary>
        /// giá trị nhỏ nhất của điểm thân mật để gọi bạn đồng hành
        /// </summary>
        public static int MinThanMatPet = 40;

        /// <summary>
        /// Danh sách quái có thể bắt theo ID quái
        /// </summary>
        public static readonly Dictionary<int, PetInfo> ListPetCanTake = new Dictionary<int, PetInfo>();

        /// <summary>
        /// Thông tin các config của pet
        /// </summary>
        public static PetInfos InfoPets = new PetInfos();

        public static YenTieuLauInfo YenTieuLauOverride = new YenTieuLauInfo();

        /// <summary>
        /// số điểm lĩnh ngộ nhận được từ bình tinh hoa theo kỹ năng
        /// </summary>
        public static List<TinhHoaValue> TinhHoaValue= new List<TinhHoaValue>();

        /// <summary>
        /// thông tin tất cả skill của pet, tỷ lệ % random sẽ ra được khi tạo pet mới
        /// </summary>
        private static readonly Dictionary<int, List<NumberSkillRate>> InfoNumberSkillRate = new Dictionary<int, List<NumberSkillRate>>();

        /// <summary>
        /// số điểm Thân mật nhận được từ tinh phách tán theo số kỹ năng và loại tinh phách
        /// </summary>
        public static readonly Dictionary<int, List<TMValue>> ItemTMValue = new Dictionary<int, List<TMValue>>();
        #endregion

        public static bool IsYenTieuLau(int petID)
        {
            return petID == YenTieuLauOverride.YenTieuLau_ID;
        }
        public static void SetUpYenTieuLau() 
        {
            var fileConfig = Global.GameResPath(KTYenTieuLauInfo);
            using (var stream = System.IO.File.OpenRead(fileConfig))
            {
                var serializer = new XmlSerializer(typeof(YenTieuLauInfo));
                YenTieuLauOverride = serializer.Deserialize(stream) as YenTieuLauInfo;

                if (YenTieuLauOverride == null)
                    return;
                
                YenTieuLauOverride.ItemValueGift = new Dictionary<int, List<GiftValue>>();
                YenTieuLauOverride.TinhHoaValue = new List<TinhHoaValue>();
                
                //Thông tin tặng quà YTL
                foreach (var info in YenTieuLauOverride.GiftValues)
                {
                    var fields = info.Value.Split('_');
                    List<GiftValue> list = new List<GiftValue>();
                    int index = 1;
                    //Mỗi field là giá trị điểm lĩnh ngộ sẽ nhận được theo số lượng kỹ năng
                    foreach(var field in fields)
                    {
                        list.Add(new GiftValue()
                        {
                            Value = int.Parse(field),
                            numberSkill = index
                        }) ;
                        index++;
                    }
                    YenTieuLauOverride.ItemValueGift.Add(info.IDItem, list);
                }

                //Config bình tinh hoa nhận được và sử dụng
                foreach (var info in YenTieuLauOverride.TinhHoa)
                {
                    YenTieuLauOverride.TinhHoaValue.Add(new TinhHoaValue()
                    {
                        Value = info.DiemLinhNgo,
                        ID = info.ID,
                        Kind = info.Kind,
                    });
                }
            }

        }

        #region Public methods
        /// <summary>
        /// set up các thông tin của pet từ file xml
        /// </summary>
        public static void SetUp()
        {
            string File = Global.GameResPath(KTPetInfo);
            
            ListPetCanTake.Clear();
            ItemTMValue.Clear();
            InfoNumberSkillRate.Clear();
            TinhHoaValue.Clear();
            PetGiftManager.ItemValueGift.Clear();
            PetGiftManager.PetValueLinhNgo.Clear();

            using (var stream = System.IO.File.OpenRead(File))
            {
                var serializer = new XmlSerializer(typeof(PetInfos));
                InfoPets = serializer.Deserialize(stream) as PetInfos;
            }
            foreach(var info in InfoPets.Pets)
            {
                var cardRate = info.CardRateStr.Split('_');
                info.CardRate = cardRate.Select(x => int.Parse(x)).ToList();
                ListPetCanTake.Add(info.IDMonster, info);
            }

            //Thông tin tặng quà pet
            foreach (var info in InfoPets.GiftValues)
            {
                var fields = info.Value.Split('_');
                List<GiftValue> list = new List<GiftValue>();
                int index = 1;
                //Mỗi field là giá trị điểm lĩnh ngộ sẽ nhận được theo số lượng kỹ năng
                foreach(var field in fields)
                {
                    list.Add(new GiftValue()
                    {
                        Value = int.Parse(field),
                        numberSkill = index
                    }) ;
                    index++;
                }
                PetGiftManager.ItemValueGift.Add(info.IDItem, list);
            }

            //Thông thân mật pet (khi sử dụng tinh phách tán)
            foreach (var info in InfoPets.TMValues)
            {
                var fields = info.Value.Split('_');
                List<TMValue> list = new List<TMValue>();
                int index = 1;
                foreach (var field in fields)
                {
                    list.Add(new TMValue()
                    {
                        Value = int.Parse(field),
                        Level = index
                    });
                    index++;
                }
                ItemTMValue.Add(info.Level, list);
            }
            //tổng giá trị lĩnh ngộ của pet tính theo level(config tặng quà)
            foreach (var info in InfoPets.LinhNgo_Values)
            {
                PetGiftManager.PetValueLinhNgo.Add(info.LevelPet, info.Value);
            }
            //số kỹ năng của pet và % tỷ lệ ra được 
            foreach(var info in InfoPets.NumberSkill)
            {
                var fields = info.SkillRate.Split('_');
                var fieldSkills = info.IndexSkill.Split('_');

                List<NumberSkillRate> dtRate = new List<NumberSkillRate>();
                int index = 0;
                foreach (var field in fields)
                {
                    dtRate.Add(new NumberSkillRate()
                    {
                        numberSkill = int.Parse(fieldSkills[index]),
                        Rate = int.Parse(field)
                    });
                    index++;
                }
                InfoNumberSkillRate.Add(info.TypePet * 100 + info.CardLevel, dtRate);
            }
            //Config bình tinh hoa nhận được và sử dụng
            foreach (var info in InfoPets.TinhHoa)
            {
                TinhHoaValue.Add(new TinhHoaValue()
                {
                    Value = info.DiemLinhNgo,
                    ID = info.ID,
                    Kind = info.Kind,
                });

            }

            SetUpYenTieuLau();
        }
        /// <summary>
        /// kiểm tra xem có thể thêm thân mật pet không
        /// </summary>
        public static bool CanAddMoreTM(KPlayer client, GoodsData pet)
        {
            int thanMat = 0;

            try
            {
                if (pet.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd))
                {
                    var data = strcmd.Split('|');
                    thanMat = Int32.Parse(data[1]);
                }

                if (thanMat >= 100)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lấy ra random Skill để khởi tạo Pet
        /// </summary>
        public static int GetRandomNumberSkill(KPlayer player, int Type, int CardLevel = 1)
        {
            if(!InfoNumberSkillRate.ContainsKey(Type * 100 + CardLevel))
                return -1;

            if (!InfoNumberSkillRate.TryGetValue(Type * 100 + CardLevel, out var rate))
            {
                return -1;
            }

            var data = rate.OrderBy(x => x.Rate).ToList();
            return StaticRandom.AccumulatedProbabilitySelection(data.Select(x => x.numberSkill).ToList(), data.Select(x => x.Rate).ToList());
        }

        public static int GetNewSkillAddToPet(List<int> existedSkill)
        {
            var listSkill = InfoPets.PetSkills.OrderBy(x => x.Rate).ToList();
            listSkill.RemoveAll(x => existedSkill.Contains(x.ID));
            if (listSkill.Count == 0)
                return -1;
            
            var newSkill =
                StaticRandom.AccumulatedProbabilitySelection(listSkill, listSkill.Select(x => x.Rate).ToList());
            
            if (existedSkill.Contains(newSkill.ID))
                return -1;
            
            return newSkill.ID;
        }

        /// <summary>
        /// Lấy ra list Skill đã random xong để khởi tạo Pet
        /// </summary>
        public static List<int> GetListSkillAddToPet(int numberSkill, List<int> excludedSkills = null)
        {
            var list = new List<int>();
            var listSkill = InfoPets.PetSkills.OrderBy(x => x.Rate).ToList();
            if (excludedSkills != null)
                listSkill.RemoveAll(x => excludedSkills.Contains(x.ID));

            while (numberSkill > 0)
            {
                var newSkill =
                    StaticRandom.AccumulatedProbabilitySelection(listSkill, listSkill.Select(x => x.Rate).ToList());
                list.Add(newSkill.ID);
                listSkill.Remove(newSkill);
                numberSkill--;
            }

            foreach (var skill in list)
            {
                LogManager.WriteLog(LogTypes.Pet, $"Tạo đồng hành với skill 1 {skill}");
            }
            if (excludedSkills != null)
            {
                foreach (var skill in excludedSkills)
                {
                    LogManager.WriteLog(LogTypes.Pet, $"Tạo đồng hành với skill 2 {skill}");
                }
            }
            return list;
        }

        public static int GetSkillAddToPet(List<int> skillFromPet)
        {
            int skillID = 0;

            var listSkill = InfoPets.PetSkills.Where(x => !skillFromPet.Contains(x.ID)).OrderBy(x => x.Rate).ToList();

            int totalRate = 0;

            if (listSkill.Count <= 0)
                return 0;

            foreach (var skill in listSkill)
            {
                totalRate += skill.Rate;
            }

            int rate = KTGlobal.GetRandomNumber(0, totalRate);

            var info = listSkill.Where(x => x.Rate >= rate).FirstOrDefault();

            if (info!= null)
            {
                skillID = info.ID;
            }
            else
            {
                skillID = listSkill[listSkill.Count - 1].ID;
            }
            LogManager.WriteLog(LogTypes.Pet, $"skillID = {skillID}");
            return skillID;
        }

        /// <summary>
        /// Trả về số kỹ năng của pet
        /// </summary>
        public static int GetNumberSkill(GoodsData goodsData)
        {
            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
            {
                try
                {
                    string[] fields = levelSkill.Split('|');
                    return fields.Length;
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            return 0;
        }

        public static int GetTotalLevelSkill(GoodsData goodsData, int numberSkill)
        {
            int result = 0;
            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
            {
                try
                {
                    string[] fields = levelSkill.Split('|');
                    for (int i = 0; i < numberSkill; i++)
                    {
                        result += Int32.Parse(fields[i]);
                    }   
                }
                catch (Exception ex)
                {
                    return result;
                }
            }
            return result;
        }
        /// <summary>
        /// Trả về điểm lĩnh ngộ
        /// </summary>
        public static int GetLN(GoodsData goodsData)
        {
            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string data))
            {
                try
                {
                    string[] fields = data.Split('|');
                    return int.Parse(fields[2]);
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            return 0;
        }
        /// <summary>
        /// lấy ra 1 chỉ số ngẫu nhiên mà pet cho player
        /// </summary>
        public static PetAttrData GetPetAttrData()
        {
            if (InfoPets.PetAttrData == null || InfoPets.PetAttrData.Count == 0)
                return null;
            int ran = KTGlobal.GetRandomNumber(0, InfoPets.PetAttrData.Count - 1);

            return InfoPets.PetAttrData[ran];
        }
        /// <summary>
        /// Trừ thân mật
        /// </summary>
        public static void TruThanMat(KPlayer client)
        {
            int day = (int)DateTime.Now.DayOfYear;
            int checkMinite = client.GetValueOfForeverRecore(RecoreDef.PetThanMat);
            if (client.GoodsDataList == null)
            {
                return;
            }


            if (checkMinite < 0)
            {
                checkMinite = day;
            }

            if(checkMinite < day)
            {
                int numDay = day - checkMinite;
                lock (client.GoodsDataList)
                {
                    var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                    if (listPet != null && listPet.Count > 0)
                    {
                        foreach (var pet in listPet)
                        {
                            KItem petItem = new KItem(pet);

                            int minusTM = 2;
                            if (petItem.SkillsFromPet.Count > 4)
                                minusTM = 4;
                            else if (petItem.SkillsFromPet.Count > 6)
                                minusTM = 6;

                            UpdateThanMatPet(client, pet.Id, -numDay * minusTM);

                            LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has petID trừ {minusTM * numDay} theo số ngày {numDay}");
                        }
                    }
                }
            }

            client.SetValueOfForeverRecore(RecoreDef.PetThanMat, day);

        }
        /// <summary>
        /// kiểm tra xem đã tối đa pet chưa
        /// </summary>
        public static bool CanHaveNewPet(KPlayer client)
        {
            if (client.GoodsDataList == null)
            {
                return false;
            }
            
            #if DEBUG
            return true;
            #endif

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                if (listPet.Count >= MaxPetUse)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// kiểm tra xem có thể sử dụng pet này ko
        /// </summary>
        public static bool CanUsePet(KPlayer client, int id)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, id);
            int thanMat = 0;
            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd))
                {
                    var data = strcmd.Split('|');
                    thanMat = Int32.Parse(data[1]);
                }
                if(thanMat < MinThanMatPet)
                {
                    return false;
                }
            }
            catch 
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tăng giảm kinh nghiệm cho pet
        /// </summary>
        public static void UpdateExpPet(KPlayer client, int id, int expBonus)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, id);
            int Exp = 0;
            int thanMat = 0;
            int linhNgo = 0;
            int linhNgoLe = 0;
            int mattichthuong = 0;
            int mattichvip = 0;
            int ExpNeed = 0;
            int ExpMax = 0;
            string DataSkill = "";


            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
                {
                    var data = strcmd.Split('|');
                    DataSkill = dataSkill;
                    Exp = Int32.Parse(data[0]);
                    thanMat = Int32.Parse(data[1]);
                    linhNgo = Int32.Parse(data[2]);
                    linhNgoLe = Int32.Parse(data[3]);
                    mattichthuong = Int32.Parse(data[4]);
                    mattichvip = Int32.Parse(data[5]);
                    ExpNeed = Int32.Parse(expNeed);
                }
                ItemData itemData = ItemManager.GetItemTemplate(goodsData.GoodsID);

                if(goodsData.Forge_level >= itemData.PetProperty.MaxLevel)
                {
                    return;
                }
                KItem petItem = new KItem(goodsData);
                // lấy ra exp của 10 level tiếp theo
                for (int i = goodsData.Forge_level; i < goodsData.Forge_level + 10; i++)
                {
                    ExpMax += ItemManager.GetMaxPetEXP(i, petItem.SkillsFromPet.Count);
                }

                if(Exp + expBonus > ExpMax)
                {
                    Exp = ExpMax;
                }
                else
                {
                    Exp += expBonus;
                }

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);

                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                OtherParams.Add(ItemPramenter.Pram_1, Exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());

                if (goodsData.Creator != "")
                {
                    OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                }

                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                {
                    string cmd = $"Người chơi: {client.RoleName} lưu tăng exp pet {id} thất bại";
                    LogManager.WriteLog(LogTypes.Error, cmd);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }

        }
        /// <summary>
        /// Tăng giảm thân mật cho pet
        /// </summary>
        public static void UpdateThanMatPet(KPlayer client, int id, int thanmatBonus)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, id);
            int Exp = 0;
            int thanMat = 0;
            int linhNgo = 0;
            int linhNgoLe = 0;
            int mattichthuong = 0;
            int mattichvip = 0;
            int ExpNeed = 0;
            string DataSkill = "";


            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
                {
                    var data = strcmd.Split('|');
                    DataSkill = dataSkill;
                    Exp = Int32.Parse(data[0]);
                    thanMat = Int32.Parse(data[1]);
                    linhNgo = Int32.Parse(data[2]);
                    linhNgoLe = Int32.Parse(data[3]);
                    mattichthuong = Int32.Parse(data[4]);
                    mattichvip = Int32.Parse(data[5]);
                    ExpNeed = Int32.Parse(expNeed);
                }

                thanMat += thanmatBonus;
                if (thanMat > 100)
                    thanMat = 100;
                if (thanMat < 0)
                    thanMat = 0;

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);

                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                OtherParams.Add(ItemPramenter.Pram_1, Exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());

                if (goodsData.Creator != "")
                {
                    OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                }

                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                {
                    string cmd = $"Người chơi: {client.RoleName} lưu tăng exp pet {id} thất bại";
                    LogManager.WriteLog(LogTypes.Error, cmd);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }
        /// <summary>
        /// Tăng giảm linh ngộ cho pet
        /// </summary>
        public static void UpdateLinhNgoPetStr(KPlayer client, int id, string linhngoBonus)
        {
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);
            int Exp = 0;
            int thanMat = 0;
            int linhNgo = 0;
            int linhNgoLe = 0;
            int mattichthuong = 0;
            int mattichvip = 0;
            int ExpNeed = 0;
            string DataSkill = "";


            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
                {
                    var data = strcmd.Split('|');
                    DataSkill = dataSkill;
                    Exp = Int32.Parse(data[0]);
                    thanMat = Int32.Parse(data[1]);
                    linhNgo = Int32.Parse(data[2]);
                    linhNgoLe = Int32.Parse(data[3]);
                    mattichthuong = Int32.Parse(data[4]);
                    mattichvip = Int32.Parse(data[5]);
                    ExpNeed = Int32.Parse(expNeed);
                }



                linhNgo += Int32.Parse(linhngoBonus.Split('|')[0]);
                linhNgoLe += Int32.Parse(linhngoBonus.Split('|')[1]);

                if (linhNgoLe >=3)
                {
                    linhNgo += 1;
                    linhNgoLe -= 3;
                }    

                if (linhNgo > 100)
                {
                    linhNgo = 100;
                    linhNgoLe = 0;
                }    
                    
                if (linhNgo < 0)
                {
                    linhNgo = 0;
                    linhNgoLe = 0;
                }    
                    

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);

                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                OtherParams.Add(ItemPramenter.Pram_1, Exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());

                if (goodsData.Creator != "")
                {
                    OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                }

                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                {
                    string cmd = $"Người chơi: {client.RoleName} lưu tăng exp pet {id} thất bại";
                    LogManager.WriteLog(LogTypes.Error, cmd);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        public static void UpdateLinhNgoPet(KPlayer client, int id, int linhngoBonus)
        {
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);
            int Exp = 0;
            int thanMat = 0;
            int linhNgo = 0;
            int linhNgoLe = 0;
            int mattichthuong = 0;
            int mattichvip = 0;
            int ExpNeed = 0;
            string DataSkill = "";


            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill) &&
                    goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
                {
                    var data = strcmd.Split('|');
                    DataSkill = dataSkill;
                    Exp = Int32.Parse(data[0]);
                    thanMat = Int32.Parse(data[1]);
                    linhNgo = Int32.Parse(data[2]);
                    linhNgoLe = Int32.Parse(data[3]);
                    mattichthuong = Int32.Parse(data[4]);
                    mattichvip = Int32.Parse(data[5]);
                    ExpNeed = Int32.Parse(expNeed);
                }



                linhNgo += linhngoBonus;

                if(linhNgoLe >= 3)
                {
                    linhNgo += 1;
                    linhNgoLe -= 3;
                }

                if (linhNgo > 100)
                {
                    linhNgo = 100;
                    linhNgoLe = 0;
                }

                if (linhNgo < 0)
                {
                    linhNgo = 0;
                    linhNgoLe = 0;
                }


                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);

                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                OtherParams.Add(ItemPramenter.Pram_1, Exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());

                if (goodsData.Creator != "")
                {
                    OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                }

                TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                {
                    string cmd = $"Người chơi: {client.RoleName} lưu tăng exp pet {id} thất bại";
                    LogManager.WriteLog(LogTypes.Error, cmd);
                }

                LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} LinhNgo={linhNgo}");
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }

        /// <summary>
        /// Sử dụng item bắt pet bắn gói tin về client
        /// </summary>
        public static void UsedItemTakePet(KPlayer client, int item, int IDItem)
        {
            if (item > 10 || item < 0)
                return;
            string strcmd = "";
            strcmd = $"{client.RoleID}:{item}:{IDItem}";
            int nID = (int)TCPGameServerCmds.CMD_SPR_USEITEMTAKEPET;
            GameManager.ClientMgr.SendToClient(client, strcmd, nID);
        }

        /// <summary>
        /// bắt đầu sử dụng item bắt pet
        /// </summary>
        public static void StartTakePet(KPlayer client, int RoleIdMonster, int itemID)
        {
            var monster = GameManager.MonsterMgr.FindMonster(client.MapCode, RoleIdMonster);

            // Không tìm thấy quái thì bỏ qua
            if(monster == null)
            {
                PlayerManager.ShowNotification(client, "Thiết lập quan hệ thất bại hãy thử lại sau!");
                return;
            }
            // Check lại các điều kiện
            var Data = monster.GetMonsterData();

            if(Data.CanTake < 0 || !ListPetCanTake.ContainsKey(Data.ExtensionID))
            {
                PlayerManager.ShowNotification(client, "Bạn phải chọn loại quái vật thích hợp để tạo quan hệ đồng hành!");
                return;
            }
            // nếu là tinh anh hoặc boss thì bỏ qua
            if (Data.MonsterType == (int) MonsterAIType.Elite || Data.MonsterType == (int)MonsterAIType.Boss || Data.MonsterType == (int)MonsterAIType.Leader)
            {
                PlayerManager.ShowNotification(client, "Bạn không thể thiết lập với quái vật cấp độ cao như này");
                return ;
            }    

            // Show Dialog bắt pet
            KNPCDialog dialog = new KNPCDialog();
            dialog.Text = $"Bạn có chắc chắn muốn tạo quan hệ đồng hành với {Data.RoleName} không?";
            dialog.Selections = new Dictionary<int, string>()
                                    {
                                        { 1, "Chắc chắn."},
                                        { 1000,"Để ta suy nghĩ thêm" }
                                    };
            dialog.OnSelect = (data) =>
            {
                if (data.SelectID == 1)
                {
                    // random ra tỷ lệ
                    int ran = KTGlobal.GetRandomNumber(0, 100);
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                    int durationSecond = 5;
                    // nếu là quái động thì cho time chạy nhanh hơn 1 chút
                    if (Data.MonsterType == (int)MonsterAIType.Hater)
                    {
                        durationSecond = 1;
                    }    
                    LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] bắt đầu sử dụng thiệp đồng hành {itemID}.");
                    client.CurrentProgress = new KPlayer_Progress()
                    {
                        Hint = $"Đang kết giao bạn đồng hành",
                        InteruptIfTakeDamage = true,
                        StartTick = KTGlobal.GetCurrentTimeMilis(),
                        DurationTick = durationSecond * 1000,
                        Complete = () =>
                        {
                            var IdPet = ListPetCanTake[Data.ExtensionID].ID;
                            var good = Global.GetGoodsByDbID(client, itemID);
                            if (good == null) 
                            {
                                LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] thiết lập đồng hành {IdPet} lỗi: Không tìm thấy thiệp đồng hành {itemID}.");
                                return;
                            }
                            var itemCard = ItemManager.TotalItem.Where(x => x.ItemID == good.GoodsID).FirstOrDefault();
                            if (itemCard == null)
                            {
                                LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] thiết lập đồng hành {IdPet} lỗi: Không tìm thấy thiệp đồng hành {itemID}.");
                                return;
                            }    
                            // nếu là thiệp như ý và pet ko có tỷ lệ ra chân nguyên thì bỏ qua
                            if (itemCard.Level == 5 && ListPetCanTake[Data.ExtensionID].CNRate <= 0)
                            {
                                PlayerManager.ShowNotification(client, "Đối tượng này không có tỷ lệ ra chân nguyên, hãy đổi đối tượng khác");
                                return;
                            }    
                            
                            var itemTemplate = ItemManager.GetItemTemplate(good.GoodsID);
                            var itemLevel = itemTemplate.Level;
                            var successRate = ListPetCanTake[Data.ExtensionID].BaseRate *
                                              ListPetCanTake[Data.ExtensionID].CardRate[itemLevel - 1] / 100;
                            
                            //nếu thất bại
                            if (ran > successRate)
                            {
                                PlayerManager.ShowNotification(client, "Đối tượng không muốn thiết lập quan hệ đồng hành với bạn!");
                                ItemManager.RemoveItem(client, itemID);
                            }
                            // Thành công
                            else
                            {
                                /// tỷ lệ có thể thành chân nguyên
                                int ranCN = KTGlobal.GetRandomNumber(0, 100);
                                var creator = "";
                                if (ranCN < ListPetCanTake[Data.ExtensionID].CNRate && itemCard.Level == 5)
                                {
                                    creator = "CN";
                                }    
                                string from = $"DONGHANH|{itemLevel}";

                                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, IdPet, 1, 0, from, false, 1, false, Global.ConstGoodsEndTime, "", -1, creator, 0, 1, false))
                                {
                                    PlayerManager.ShowNotification(client, "Có lỗi khi thiết lập quan hệ!");
                                    LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] thiết lập đồng hành {IdPet}lỗi: Không tạo được đồng hành.");
                                }
                                else
                                {
                                    if (ItemManager.RemoveItem(client, itemID)) 
                                    {
                                        LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] xóa thiệp lụa {itemID} thành công.");
                                    } else {
                                        LogManager.WriteLog(LogTypes.Pet, $"{client.RoleName}[{client.RoleID}] xóa thiệp lụa {itemID} thất bại.");
                                    }
                                    //monster.MonsterZoneNode?.DestroyMonster(monster);

                                    GlobalEventSource.getInstance().fireEvent(new CapturePetEventObject(client, IdPet));
                                }
                            }
                        },
                        Cancel = () =>
                        {
                            PlayerManager.ShowNotification(client, "Đã ngừng kết giao!");
                        }
                    };
                }
                else if (data.SelectID == 1000)
                {
                    KT_TCPHandler.CloseDialog(client);
                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                }
            };
            dialog.ShowDialog(client);

        }
        #endregion

        #region Private Methods


        /// <summary>
        /// Giải tán pet
        /// </summary>
        private static bool RemovePet(KPlayer client, int id, bool IsVip = false)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, id);
            if (goodsData.Using > 0)
            {
                PlayerManager.ShowNotification(client, "Cần thu hồi Bạn đồng hành trước khi giải tán!");
                return false;
            }
            // Kiểm tra xem có phải đh từ 6 sao trở lên ko
            if (!IsVip)
            {
                if (GetNumberSkill(goodsData) >= 6)
                {
                    PlayerManager.ShowNotification(client, "Đẳng cấp đồng hành quá cao, hãy đến Long Ngũ Thái Gia để giải tán đồng hành!");
                    return false;
                }
                ItemManager.RemoveItem(client, id);
                PlayerManager.ShowNotification(client, "Giải tán pet thành công");
            }
            else
            {
                // giải tán đồng hành vip
                if (GetNumberSkill(goodsData) < 6)
                {
                    PlayerManager.ShowNotification(client, "Chỉ có thể xin giải tán bạn đồng hành đẳng cấp 6 sao trở lên!");
                    return false;
                }
                //3 tiếng sau mới tự động giải tán để hạn sử dụng tăng thêm 3h
                DateTime dt = DateTime.Now.AddMinutes(30);

                string TmpEnd = dt.ToString("yyyy-MM-dd HH#mm#ss");

                Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

                TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
                TotalUpdate.Add(UPDATEITEM.END_TIME, TmpEnd);

                if (ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "REMOVEDONGHANHVIP", true))
                {
                    LogManager.WriteLog(LogTypes.Item, "[" + client.RoleID + "][" + client.RoleName + "] xin xóa đồng hành :" + goodsData.ItemName + goodsData.Id);
                }
                else 
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Hoàn tác giải tán pet VIP ở Long Ngũ 
        /// </summary>
        private static bool UnRemovePet(KPlayer client, int id)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, id);
            if (GetNumberSkill(goodsData) <= 6)
            {
                PlayerManager.ShowNotification(client, "Chỉ có thể hủy giải tán bạn đồng hành đẳng cấp 6 sao trở lên!");
                return false;
            }

            string TmpEnd = "1900-01-01 12#00#00";

            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);
            TotalUpdate.Add(UPDATEITEM.END_TIME, TmpEnd);

            if (ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "UNREMOVEDONGHANHVIP", true))
            {
                LogManager.WriteLog(LogTypes.Item, "[" + client.RoleID + "][" + client.RoleName + "] xin xóa đồng hành :" + goodsData.ItemName + goodsData.Id);
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Show diaolog khi click vào giải tán pet vip ở Long ngũ
        /// </summary>
        public static void ShowDialogRemovePetNPC(KPlayer client)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            var listPet = new List<GoodsData>();
            //Lấy ra danh sách
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet() && GetNumberSkill(x) >= 6 && x.Endtime == "1900-01-01 12:00:00").ToList();
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành cấp cao để giải tán. Sau 30 phút từ lúc bạn chấp nhận giải tán bạn đồng hành sẽ rời đi!";
                dialog.Selections = new Dictionary<int, string>();

                foreach (var pet in listPet)
                {
                    dialog.Selections.Add(pet.Id, $"{pet.ItemName} - {pet.Forge_level}");
                }
                dialog.Selections.Add(-1, "Để ta suy nghĩ thêm!");

                dialog.OnSelect = (data) =>
                {
                    if (data.SelectID == -1)
                    {
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                    }
                    if (data.SelectID > 0)
                    {
                        var pet = Global.GetGoodsByDbID(client, data.SelectID);
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                        KNPCDialog Dialog = new KNPCDialog();
                        Dialog.Text = $"Ngươi có chắc chắn muốn giải tán bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} không?";
                        Dialog.Text += $"\n Lưu ý: Sau 30 phút bạn đồng hành sẽ đi!";
                        Dialog.Selections = new Dictionary<int, string>();

                        Dialog.Selections.Add(1, "Chắc chắn");
                        Dialog.Selections.Add(-1, "Để ta suy nghĩ lại");

                        Dialog.OnSelect = (Data) =>
                        {
                            if (Data.SelectID == -1)
                            {
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(dialog);
                            }
                            if (Data.SelectID == 1)
                            {
                                if(RemovePet(client, data.SelectID, true))
                                {
                                    PlayerManager.ShowNotification(client, $"Giải bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} thành công. Sau 30 phút bạn đồng hành sẽ biến mất!");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(client, $"Có lỗi xảy ra hãy thử lại sau!");
                                }
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(dialog);
                            }
                        };
                        Dialog.ShowDialog(client);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào đủ điều kiện để sử dụng chức năng này hãy thử lại sau.";
            }

            dialog.ShowDialog(client);
        }

        /// <summary>
        /// Show diaolog khi click vào hoàn tác giải tán pet vip ở Long ngũ
        /// </summary>
        public static void ShowDialogUnRemovePetNPC(KPlayer client)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            var listPet = new List<GoodsData>();
            // lấy ra danh sách
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet() && GetNumberSkill(x) >= 6 && x.Endtime != "1900-01-01 12:00:00").ToList();
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành cấp cao để hủy giải tán.";
                dialog.Selections = new Dictionary<int, string>();

                foreach (var pet in listPet)
                {
                    dialog.Selections.Add(pet.Id, $"{pet.ItemName} - {pet.Forge_level}");
                }
                dialog.Selections.Add(-1, "Để ta suy nghĩ thêm!");

                dialog.OnSelect = (data) =>
                {
                    if (data.SelectID == -1)
                    {
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                    }
                    if (data.SelectID > 0)
                    {
                        var pet = Global.GetGoodsByDbID(client, data.SelectID);
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                        KNPCDialog Dialog = new KNPCDialog();
                        Dialog.Text = $"Ngươi có chắc chắn muốn hủy giải tán bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} không?";
                        Dialog.Selections = new Dictionary<int, string>();

                        Dialog.Selections.Add(1, "Chắc chắn");
                        Dialog.Selections.Add(-1, "Để ta suy nghĩ lại");

                        Dialog.OnSelect = (Data) =>
                        {
                            if (Data.SelectID == -1)
                            {
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(dialog);
                            }
                            if (Data.SelectID == 1)
                            {
                                if (UnRemovePet(client, data.SelectID))
                                {
                                    PlayerManager.ShowNotification(client, $"Hủy giải bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} - {Global.GetGoodsByDbID(client, data.SelectID).Forge_level} thành công.");
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(client, $"Có lỗi xảy ra hãy thử lại sau!");
                                }
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(dialog);
                            }
                        };
                        Dialog.ShowDialog(client);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn đang không có bạn đồng hành cấp cao nào chuẩn bị rời đi.";
            }

            dialog.ShowDialog(client);
        }

        /// <summary>
        /// Kiểm tra xem có thể tăng cấp skill ko
        /// </summary>
        public static bool CanLevelUpSkill(GoodsData goodsData, int numberSkill)
        {
            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill))
            {
                try
                {
                    string[] fields = levelSkill.Split('|');
                    var skillCanLevelUp = new List<int>();

                    for (int i = 0; i < numberSkill; i++)
                    {
                        int level = int.Parse(fields[i]);
                        if (level < 6)
                        {
                            skillCanLevelUp.Add(i);
                        }
                    }

                    if (skillCanLevelUp.Count == 0)
                    {
                        //Không còn skill nào để nâng cấp nữa
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// Thăng cấp cho pet
        /// </summary>
        private static int LevelUpPet(KPlayer client, int id, bool isDone = false)
        {
            // isDone là dùng lệnh GM nếu = true bỏ qua mọi điều kiện tự tăng
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);
            KItem petItem = new KItem(goodsData);
            int exp = 0;
            int thanMat = 0;
            int linhNgo = 0;
            int linhNgoLe = 0;
            int ExpNeed = 0;
            int mattichthuong = 0;
            int mattichvip = 0;
            string DataSkill = "";

            ItemData itemData = ItemManager.GetItemTemplate(goodsData.GoodsID);

            if (goodsData.Using > 0)
            {
                return -50;
            }

            if(goodsData.Forge_level >= itemData.PetProperty.MaxLevel)
            {
                return -3;
            }

            try
            {
                if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
                        goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill) &&
                        goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
                {
                    var data = strcmd.Split('|');
                    DataSkill = dataSkill;
                    exp = Int32.Parse(data[0]);
                    thanMat = Int32.Parse(data[1]);
                    linhNgo = Int32.Parse(data[2]);
                    mattichthuong = Int32.Parse(data[3]);
                    mattichvip = Int32.Parse(data[4]);
                    ExpNeed = Int32.Parse(expNeed);
                }

                bool checkUp = false;
                int ran = KTGlobal.GetRandomNumber(0, 100);

                int level = goodsData.Forge_level;

                if (linhNgo >= 94)
                {
                    checkUp = true;
                }    
                else
                {
                    if (linhNgo > ran)
                    {
                        checkUp = true;
                    }    
                }
                // nâng cấp skill
                if ((level + 1) % itemData.PetProperty.LVSkill == 0 && exp >= ExpNeed && PetManager.CanLevelUpSkill(goodsData, petItem.SkillsFromPet.Count))
                {
                    if (linhNgo > 30)
                    {
                        if (checkUp)
                        {
                            // nâng cấp skill
                            // Lấy ra danh sách cấp kỹ năng
                            string[] fields = DataSkill.Split('|');
                            var skillCanLevelUp = new List<int>();
                            //Kiểm tra xem có kỹ năng nào max chưa
                            for (int i = 0; i < petItem.SkillsFromPet.Count; i++)
                            {
                                if (int.Parse(fields[i]) < 6)
                                {
                                    skillCanLevelUp.Add(i);
                                }
                            }

                            if (skillCanLevelUp.Count > 0)
                            {
                                int index = KTGlobal.GetRandomNumber(0, skillCanLevelUp.Count - 1);

                                int levelup = int.Parse(fields[skillCanLevelUp[index]]) + 1;

                                fields[skillCanLevelUp[index]] = levelup.ToString();
                                var strdata = string.Empty;

                                foreach (string field in fields)
                                {
                                    if (strdata == "")
                                    {
                                        strdata += field;
                                    }
                                    else
                                    {
                                        strdata += "|" + field;
                                    }
                                }

                                DataSkill = strdata;

                                KItem item = new KItem(goodsData);

                                PlayerManager.ShowNotification(client, $"Bạn đồng hành đã thăng cấp kỹ năng {item.GetNameSkill(client, skillCanLevelUp[index])}!");

                                LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} levelUpSkill {item.GetNameSkill(client, skillCanLevelUp[index])}");

                                linhNgo -= 10;
                            }    
                        }
                        else
                        {
                            linhNgo -= 5;
                            PlayerManager.ShowNotification(client, $"Bạn đồng hành thăng cấp kỹ năng thất bại");
                        }
                    }
                    else
                    {
                        return -1;
                    }   
                }

                // tăng cấp đồng hành
                if (exp >= ExpNeed)
                {
                    exp -= ExpNeed;

                    if (ItemManager.SetEquipForgeLevel(goodsData, client, goodsData.Forge_level + 1, 1))
                    {
                        ExpNeed = ItemManager.GetMaxPetEXP(goodsData.Forge_level, petItem.SkillsFromPet.Count);

                        Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                        TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                        TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);

                        Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                        OtherParams.Add(ItemPramenter.Pram_1, exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                        OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                        OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());

                        if (goodsData.Creator != "")
                        {
                            OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                        }

                        TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                        if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                        {
                            string cmdStr = $"Người chơi: {client.RoleName} lưu thăng cấp pet {id} thất bại";
                            LogManager.WriteLog(LogTypes.Error, cmdStr);
                            return -100;
                        }

                        LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} levelUp = {goodsData.Forge_level}");
                    }
                    else
                    {
                        return -100;
                    }
                }   
                else
                {
                    return -1;
                }    
                

                //if (linhNgo >= 94)
                //{
                //    checkUp = true;
                //
                //    if ((level + 1) % itemData.PetProperty.LVSkill == 0 && exp >= ExpNeed && PetManager.CanLevelUpSkill(goodsData))
                //    {
                //        // lên cấp skill
                //        // Lấy ra danh sách cấp kỹ năng
                //        string[] fields = DataSkill.Split('|');
                //        var skillCanLevelUp = new List<int>();
                //        //Kiểm tra xem có kỹ năng nào max chưa
                //        for (int i = 0; i < fields.Length; i++)
                //        {
                //            if (int.Parse(fields[i]) < 6)
                //            {
                //                skillCanLevelUp.Add(i);
                //            }
                //        }
                //
                //        int index = KTGlobal.GetRandomNumber(0, skillCanLevelUp.Count - 1);
                //
                //        int levelup = int.Parse(fields[skillCanLevelUp[index]]) + 1;
                //
                //        fields[skillCanLevelUp[index]] = levelup.ToString();
                //        var strdata = string.Empty;
                //
                //        foreach (string field in fields)
                //        {
                //            if (strdata == "")
                //            {
                //                strdata += field;
                //            }
                //            else
                //            {
                //                strdata += "|" + field;
                //            }
                //        }
                //
                //        DataSkill = strdata;
                //
                //        KItem item = new KItem(goodsData);
                //
                //        PlayerManager.ShowNotification(client, $"Bạn đồng hành đã thăng cấp kỹ năng {item.GetNameSkill(client, skillCanLevelUp[index])}!");
                //
                //        LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} levelUpSkill {item.GetNameSkill(client, skillCanLevelUp[index])}");
                //
                //        linhNgo -= 10;
                //    }    
                //}    
                //else
                //{
                //    if ((level + 1) % itemData.PetProperty.LVSkill != 0)
                //    {
                //        checkUp = true;
                //    }
                //}
                //
                //if (checkUp)
                //{     
                //    if (exp >= ExpNeed)
                //    {
                //        exp -= ExpNeed;
                //
                //        if (ItemManager.SetEquipForgeLevel(goodsData, client, goodsData.Forge_level + 1, 1))
                //        {
                //            KItem petItem = new KItem(goodsData);
                //            ExpNeed = ItemManager.GetMaxPetEXP(goodsData.Forge_level, petItem.SkillsFromPet.Count);
                //
                //            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
                //            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
                //            TotalUpdate.Add(UPDATEITEM.ITEMDBID, id);
                //
                //            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                //            OtherParams.Add(ItemPramenter.Pram_1, exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip);
                //            OtherParams.Add(ItemPramenter.Pram_2, DataSkill);
                //            OtherParams.Add(ItemPramenter.Pram_3, ExpNeed.ToString());
                //
                //            TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);
                //
                //            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                //            {
                //                string cmdStr = $"Người chơi: {client.RoleName} lưu thăng cấp pet {id} thất bại";
                //                LogManager.WriteLog(LogTypes.Error, cmdStr);
                //                return -100;
                //            }
                //
                //            LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} levelUp = {goodsData.Forge_level}");
                //        }
                //        else
                //        {
                //            return -100;
                //        }
                //    }  
                //    else
                //    {
                //        return -1;
                //    }    
                //}  
                //else
                //{
                //    return -1;
                //}    

            }
            catch (Exception e)
            {
                LogManager.WriteLog(LogTypes.Error, e.ToString());
                return -100;
            }
            return 1;
        }

        /// <summary>
        /// Thăng cấp skill cho pet
        /// </summary>
        /// <param name="client">Người chơi</param>
        /// <param name="id">ID của pet</param>
        /// <param name="matTichType">Loại mật tịch đã dùng: 1 = mật tịch thường, 2 = mật tịch VIP, 0 hoặc < 0 = không cập nhật số lượng mật tịch</param>
        public static int LevelUpSkillPet(KPlayer client, int id, int matTichType = 0)
        {
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);

            KItem petITem = new KItem(goodsData);
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();


            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string petAttr) &&
                goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill) &&
                goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
            {
                try
                {
                    //Lấy ra danh sách cấp kỹ năng
                    string[] fields = levelSkill.Split('|');
                    var skillCanLevelUp = new List<int>();
                    //Kiểm tra xem có kỹ năng nào max chưa
                    for (int i = 0; i < petITem.SkillsFromPet.Count ; i++)
                    {
                        int level = int.Parse(fields[i]);
                        if (level < 6)
                        {
                            skillCanLevelUp.Add(i);
                        }
                    }
                    if (skillCanLevelUp.Count <= 0)
                    {
                        return -1;
                    }    

                    int index = KTGlobal.GetRandomNumber(0, skillCanLevelUp.Count - 1);

                    int oldLevel = int.Parse(fields[skillCanLevelUp[index]]);
                    int levelup = oldLevel + 1;

                    fields[skillCanLevelUp[index]] = levelup.ToString();
                    var strdata = string.Empty;

                    foreach (string field in fields)
                    {
                        if (strdata == "")
                        {
                            strdata += field;
                        }
                        else
                        {
                            strdata += "|" + field;
                        }
                    }

                    // Xử lý cập nhật số lượng mật tịch đã dùng nếu có
                    if (matTichType > 0)
                    {
                        var data = petAttr.Split('|');
                        // Kiểm tra data có đủ 6 phần tử (Exp|thanMat|linhNgo|linhNgoLe|mattichthuong|mattichvip)
                        if (data.Length >= 6)
                        {
                            int Exp = Int32.Parse(data[0]);
                            int thanMat = Int32.Parse(data[1]);
                            int linhNgo = Int32.Parse(data[2]);
                            int linhNgoLe = Int32.Parse(data[3]);
                            int mattichthuong = Int32.Parse(data[4]);
                            int mattichvip = Int32.Parse(data[5]);

                            // Tăng số lượng mật tịch đã dùng
                            if (matTichType == 1)
                            {
                                mattichthuong += 1;
                            }
                            else if (matTichType == 2)
                            {
                                mattichvip += 1;
                            }

                            // Cập nhật lại petAttr với số lượng mật tịch mới
                            petAttr = Exp + "|" + thanMat + "|" + linhNgo + "|" + linhNgoLe + "|" + mattichthuong + "|" + mattichvip;
                        }
                        else
                        {
                            LogManager.WriteLog(LogTypes.Error, $"Pet {id} có dữ liệu Pram_1 không hợp lệ khi cập nhật số lượng mật tịch: {petAttr}");
                        }
                    }

                    OtherParams.Add(ItemPramenter.Pram_1, petAttr);
                    OtherParams.Add(ItemPramenter.Pram_2, strdata);
                    OtherParams.Add(ItemPramenter.Pram_3, expNeed);

                    KItem item = new KItem(goodsData);

                    PlayerManager.ShowNotification(client, $"Bạn đồng hành đã thăng cấp kỹ năng {item.GetNameSkill(client, skillCanLevelUp[index])}!");

                    if (goodsData.Creator != "")
                    {
                        OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                    }

                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                    if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                    {
                        string cmd = $"Người chơi: {client.RoleName} thăng cấp skill cho pet {goodsData.Id} thất bại";
                        LogManager.WriteLog(LogTypes.Error, cmd);
                        return -100;
                    }

                    LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} [{goodsData.Id}] levelUpSkill {item.GetNameSkill(client, skillCanLevelUp[index])}");

                    // Cập nhật skill level trong skill tree của player nếu pet đang được trang bị
                    if (goodsData.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_PET)
                    {
                        int skillID = petITem.SkillsFromPet[skillCanLevelUp[index]];
                        if (client.Skills.HasSkill(skillID))
                        {
                            // Cập nhật skill level trong skill tree (levelDiff = +1)
                            client.Skills.AddSkillLevel(skillID, 1);
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                    return -100;
                }
            }

            
            // trừ điểm lĩnh ngộ khi tăng thành công
            UpdateLinhNgoPet(client, id, -10);

            return 1;
        }

        public static int MinusSkillPet(KPlayer client, int id)
        {
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);

            KItem petITem = new KItem(goodsData);
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, goodsData.Id);

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();


            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string petAttr) &&
                goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string levelSkill) &&
                goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_3, out string expNeed))
            {
                try
                {
                    //Lấy ra danh sách cấp kỹ năng
                    string[] fields = levelSkill.Split('|');
                    var skillCanLevelUp = new List<int>();
                    //Kiểm tra xem có kỹ năng nào max chưa
                    for (int i = 0; i < petITem.SkillsFromPet.Count; i++)
                    {
                        int level = int.Parse(fields[i]);
                        if (level > 1)
                        {
                            skillCanLevelUp.Add(i);
                        }
                    }
                    if (skillCanLevelUp.Count <= 0)
                    {
                        return -1;
                    }

                    int index = KTGlobal.GetRandomNumber(0, skillCanLevelUp.Count - 1);

                    int oldLevel = int.Parse(fields[skillCanLevelUp[index]]);
                    int levelup = oldLevel - 1;

                    fields[skillCanLevelUp[index]] = levelup.ToString();
                    var strdata = string.Empty;

                    foreach (string field in fields)
                    {
                        if (strdata == "")
                        {
                            strdata += field;
                        }
                        else
                        {
                            strdata += "|" + field;
                        }
                    }

                    OtherParams.Add(ItemPramenter.Pram_1, petAttr);
                    OtherParams.Add(ItemPramenter.Pram_2, strdata);
                    OtherParams.Add(ItemPramenter.Pram_3, expNeed);

                    KItem item = new KItem(goodsData);

                    //PlayerManager.ShowNotification(client, $"Bạn đồng hành đã thăng cấp kỹ năng {item.GetNameSkill(client, skillCanLevelUp[index])}!");

                    if (goodsData.Creator != "")
                    {
                        OtherParams.Add(ItemPramenter.Creator, goodsData.Creator);
                    }

                    TotalUpdate.Add(UPDATEITEM.OTHER_PRAM, OtherParams);

                    if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                    {
                        string cmd = $"Người chơi: {client.RoleName} hạ cấp skill cho pet {goodsData.Id} thất bại";
                        LogManager.WriteLog(LogTypes.Error, cmd);
                        return -100;
                    }

                    LogManager.WriteLog(LogTypes.Pet, $"Player: {client.RoleName} has pet {goodsData.ItemName} minusSkill {item.GetNameSkill(client, skillCanLevelUp[index])} -1");

                    // Cập nhật skill level trong skill tree của player nếu pet đang được trang bị
                    if (goodsData.Using == (int)KE_EQUIP_POSITION.emEQUIPPOS_PET)
                    {
                        int skillID = petITem.SkillsFromPet[skillCanLevelUp[index]];
                        if (client.Skills.HasSkill(skillID))
                        {
                            // Cập nhật skill level trong skill tree (levelDiff = -1)
                            client.Skills.AddSkillLevel(skillID, -1);
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                    return -100;
                }
            }


            // trừ điểm lĩnh ngộ khi tăng thành công
            //UpdateLinhNgoPet(client, id, -10);

            return 1;
        }

        public static int MakeEquipPet(List<GoodsData> crystalStones, KPlayer client)
        {
            ItemData itemData = ItemManager.GetItemTemplate(crystalStones[0].GoodsID);
            if (itemData != null)
            {
                // xóa mảnh khỏi người
                ItemManager.RemoveItemFromBag(client, itemData.ItemID, 100);

                int random = new Random().Next(0, 100);
                if (random <25)
                {
                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, itemData.ItemValue, 1, 0, "MakeEquip", false, 1, false, ""))
                    {
                        PlayerManager.ShowNotification(client, "Có lỗi khi add vật phẩm");

                        return -1;
                    }
                    else
                    {
                        return 1;
                    }    
                }    
                else
                {
                    PlayerManager.ShowNotification(client, "Ghép thất bại, chúc bạn may mắn lần sau!");
                }
            }    
            return 0;
        }

        #endregion

        #region TCP_Network
        /// <summary>
        /// Nhận từ client thông tin quá đang muốn bắt
        /// </summary>
        public static TCPProcessCmdResults ProcessStartTakePetCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 3)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                int RoleIDMonster = Convert.ToInt32(fields[1]); 
                
                int IDItem = Convert.ToInt32(fields[2]); 

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                strcmd = $"{client.RoleID}";
                //sử dụng item bắt
                PetManager.StartTakePet(client, RoleIDMonster, IDItem);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// Phản hồi thăng cấp cho pet từ client
        /// </summary>
        public static TCPProcessCmdResults ProcessSpriteLevelUpPetCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                int itemID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                // tăng cấp
                int dt = LevelUpPet(client, itemID);

                strcmd = $"{client.RoleID}:{dt}:{itemID}";

                GameManager.ClientMgr.SendToClient(client, strcmd, nID);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// Phản hồi gói tin giải tán pet
        /// </summary>
        public static TCPProcessCmdResults ProcessSpriteRemovePetCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                int itemID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if(RemovePet(client, itemID))
                {
                    strcmd = $"{client.RoleID}:{1}";

                    GameManager.ClientMgr.SendToClient(client, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        /// <summary>
        /// Gói tin tặng quà cho pet, tăng điểm lĩnh ngộ
        /// </summary>
        public static TCPProcessCmdResults ResponseSendGiftPet(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            SendGiftPet sendgift = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                sendgift = DataHelper.BytesToObject<SendGiftPet>(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            try
            {
                /// Nếu dữ liệu trong gói tin không tồn tại
                if (sendgift == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), 0));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Tìm chủ nhân của gói tin tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// pet tương ứng trong của người chơi
                GoodsData itemGD = client.GoodsDataList.Where(x => x.Id == sendgift.EquipDbID).FirstOrDefault();
                /// Nếu pet tương ứng khong tồn tại trong người chơi
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(client, "Bạn Đồng Hành không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                if(itemGD.Using != 20)
                {
                    PlayerManager.ShowNotification(client, "Chỉ có thể tặng quà cho bạn đồng hành đang giúp đỡ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                /// Nếu không có danh sách Huyền Tinh
                if (sendgift.GiftsDbID == null || sendgift.GiftsDbID.Count <= 0)
                {
                    PlayerManager.ShowNotification(client, "Không có quà tặng, không thể tăng điểm lĩnh ngộ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> gifts = client.GoodsDataList.Where(x => sendgift.GiftsDbID.Contains(x.Id)).ToList();
                /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                if (gifts.Count != sendgift.GiftsDbID.Count)
                {
                    PlayerManager.ShowNotification(client, "Danh sách quà tặng không hợp lệ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                // tiến hành tặng quà
                int ret = PetGiftManager.DoSendGift(gifts, itemGD, client);

                string cmdData = $"{client.RoleID}:{ret}";

                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_SENDGIFTPET);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;

        }

        public static TCPProcessCmdResults ResponseMakeEquip(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            MakeEquipPet equipEnhance = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equipEnhance = DataHelper.BytesToObject<MakeEquipPet>(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                /// Nếu dữ liệu trong gói tin không tồn tại
                if (equipEnhance == null)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), 0));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Tìm chủ nhân của gói tin tương ứng
                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Bản đồ hiện tại
                GameMap gameMap = GameManager.MapMgr.GetGameMap(client.CurrentMapCode);
                /// Nếu không phải ở thành thị, tần lăng
                if (gameMap.MapType != "village" && gameMap.MapType != "city" && gameMap.MapType != "qinshihuangling_1")
                {
                    /// Cho toác luôn
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Nếu không thấy NPC dã luyện
                if (client.LastEquipMasterNPC == null || client.LastEquipMasterNPC.MapCode != client.CurrentMapCode)
                {
                    /// Cho toác luôn
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                /// Nếu khoảng cách quá xa so với NPC dã luyện
                if (Global.GetTwoPointDistance(client.CurrentPos, client.LastEquipMasterNPC.CurrentPos) > 100)
                {
                    PlayerManager.ShowNotification(client, "Khoảng cách tới Dã Luyện Đại Sư quá xa!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Nếu không có danh sách mảnh
                if (equipEnhance.CrystalStonesDbID == null || equipEnhance.CrystalStonesDbID.Count <= 0)
                {
                    PlayerManager.ShowNotification(client, "Không có mảnh trang bị");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> crystalStones = client.GoodsDataList.Where(x => equipEnhance.CrystalStonesDbID.Contains(x.Id)).ToList();
                /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                if (crystalStones.Count != equipEnhance.CrystalStonesDbID.Count)
                {
                    PlayerManager.ShowNotification(client, "Danh sách mảnh trang bị không hợp lệ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                int ret = MakeEquipPet(crystalStones, client);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_SENDMAKEEQUIPPET);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        #endregion

        #region GM
        public static void AddExpAllPet(KPlayer client, int expadd, int id = 0)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                if (listPet.Count == 0)
                {
                    return;
                }
                if(id != 0)
                {
                    UpdateExpPet(client, id, expadd);
                    return;
                }
                foreach (var item in listPet)
                {
                    UpdateExpPet(client, item.Id, expadd);
                }
            }
        }
        public static bool AddThanMatAllPet(KPlayer client, int expadd, int id = 0, bool isGM = true)
        {
            if (client.GoodsDataList == null)
            {
                return false;
            }

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();

                if (!isGM)
                {
                    listPet = listPet.Where(x => x.Forge_level >= 30 && CanAddMoreTM(client, x)).ToList();
                }

                if (listPet.Count == 0)
                {
                    PlayerManager.ShowNotification(client, $"Bạn không có bạn đồng hành nào cần tăng thân mật!");
                    return false;
                }


                if (id != 0)
                {
                    UpdateThanMatPet(client, id, expadd);
                    return true;
                }

                foreach (var item in listPet)
                {
                    UpdateThanMatPet(client, item.Id, expadd);
                }
                return true;
            }
        }


        public static void AddLinhNgoAllPet(KPlayer client, int expadd, int id = 0)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                if (listPet.Count == 0)
                {
                    return;
                }
                if (id != 0)
                {
                    UpdateLinhNgoPet(client, id, expadd);
                    return;
                }
                foreach (var item in listPet)
                {
                    UpdateLinhNgoPet(client, item.Id, expadd);
                }
            }
        }

        public static void LevelUpAllPet(KPlayer client, int level, int id = 0)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                if (listPet.Count == 0)
                {
                    return;
                }
                if (id != 0)
                {
                    int Level = level;
                    do
                    {
                        LevelUpPet(client, id, true);
                        Level--;
                    }
                    while (Level > 0);
                    return;
                }
                foreach (var item in listPet)
                {
                    int Level = level;
                    do 
                    { 
                        LevelUpPet(client, item.Id, true);
                        Level--;
                    }
                    while (Level > 0);

                }
            }
        }

        public static bool CreateChanNguyen(KPlayer client, int itemID)
        {
            GoodsData goodsData = null;
            goodsData = Global.GetGoodsByDbID(client, itemID);
            if (goodsData.Using > 0)
            {
                PlayerManager.ShowNotification(client, "Cần thu hồi Bạn đồng hành trước khi giải tán!");
                return false;
            }
            if (goodsData.Forge_level > 1)
            {
                PlayerManager.ShowNotification(client, "Đồng hành cấp quá cao");
                return false;
            }    
            if (goodsData.Creator != "CN")
            {
                PlayerManager.ShowNotification(client, "Đồng hành này không thể tạo chân nguyên");
                return false;
            }
            /// xóa pet
            ItemManager.RemoveItem(client, itemID);
            LogManager.WriteLog(LogTypes.Pet, $"Xóa pet {goodsData.ItemName} để tạo ra chân nguyên");
            /// tạo ra chân nguyên
            // lấy ra ds chân nguyên
            if (ChanNguyenManager._ListCNCreate.Count > 0)
            {
                var IDCNCreate = StaticRandom.AccumulatedProbabilitySelection(ChanNguyenManager._ListCNCreate.Select(x => x.IDCN).ToList(), ChanNguyenManager._ListCNCreate.Select(x => x.RateApear).ToList());
                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, IDCNCreate, 1, 0, "PET", false, 0, false, Global.ConstGoodsEndTime))
                {
                    PlayerManager.ShowNotification(client, "Lỗi không xác định!");
                }
                LogManager.WriteLog(LogTypes.Pet, $"Tạo ra Chân nguyên thành công từ pet {goodsData.ItemName} thành chân nguyên ID={IDCNCreate}");
            }

            return true;
        }

        /// <summary>
        /// Phản hồi gói tin nâng cấp pet thành chân nguyên
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <returns></returns>
        public static TCPProcessCmdResults CreateChanNguyen(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;

            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Decode data faild, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                //Lấy ra thông tin gửi lên
                string[] fields = cmdData.Split(':');
                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                int itemID = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}",
                                                                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                if (CreateChanNguyen(client, itemID))
                {
                    strcmd = $"{client.RoleID}:{1}";

                    GameManager.ClientMgr.SendToClient(client, strcmd, nID);

                    return TCPProcessCmdResults.RESULT_OK;
                }

                return TCPProcessCmdResults.RESULT_OK;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
        #endregion

    }
}
