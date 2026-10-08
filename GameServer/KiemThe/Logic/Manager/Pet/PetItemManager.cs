using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.KiemThe.Logic.Manager.Pet
{
    /// <summary>
    /// Hàm quản lý các item liên quan đến pet
    /// </summary>
    public static class PetItemManager
    {
        /// <summary>
        /// Danh sách các item chứa pet bn kỹ năng trả về cho ng chơi khi sử dụng thư giới thiệu
        /// </summary>
        private static List<int> PetIcon = new List<int>() { 1107, 1108, 1109, 1110, 1111, 1112, 1113, 1114, 1115, 1116 };

        #region Pet Item
        /// <summary>
        /// kiểm tra xem có thể tăng thêm EXP cho pet nữa không
        /// </summary>
        private static bool CanAddMoreExpPet(KPlayer client, int id, int expPlus)
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

                if (goodsData.Forge_level >= itemData.PetProperty.MaxLevel)
                {
                    return false;
                }
                KItem petItem = new KItem(goodsData);
                if (goodsData.Forge_level < 120)
                {
                    for (int i = goodsData.Forge_level; i <= 120; i++)
                    {
                        ExpMax += ItemManager.GetMaxPetEXP(goodsData.Forge_level, petItem.SkillsFromPet.Count);
                    }
                }
                else
                {
                    ExpMax = ExpNeed;
                }

                if ((Exp + expPlus) >= ExpMax)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                return false;
            }
            return true;
        }
        /// <summary>
        /// Thêm thân mật pet bằng item tinh phách tán
        /// </summary>
        public static bool AddThanMatAllPetItem(KPlayer client, int levelItem)
        {
            if (client.GoodsDataList == null)
            {
                return false;
            }

            lock (client.GoodsDataList)
            {
                var listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();

                listPet = listPet.Where(x => PetManager.CanAddMoreTM(client, x)).ToList();

                if (listPet.Count == 0)
                {
                    PlayerManager.ShowNotification(client, $"Bạn không có bạn đồng hành nào cần tăng thân mật!");
                    return false;
                }


                foreach (var item in listPet)
                {
                    int starpet = PetManager.GetNumberSkill(item);
                    int tmAdd = PetManager.ItemTMValue[levelItem].FirstOrDefault(x => x.Level == starpet).Value;
                    PetManager.UpdateThanMatPet(client, item.Id, tmAdd);
                }

                return true;
            }
        }


        public static bool CanLearnSkill(GoodsData pet, int skillID)
        {
            var propsBytes = Convert.FromBase64String(pet.Props);
            var props = DataHelper.BytesToObject<ItemGenByteData>(propsBytes, 0, propsBytes.Length);
            var listSkills = props.PetPropertyValue.Skip(4).ToList();

            return !listSkills.Contains(skillID);
        }

        public static int GetNumberOfLearnedSkills(GoodsData pet)
        {
            var propsBytes = Convert.FromBase64String(pet.Props);
            var props = DataHelper.BytesToObject<ItemGenByteData>(propsBytes, 0, propsBytes.Length);
            var listSkills = props.PetPropertyValue.Skip(4).ToList();

            return listSkills.Count;
        }


        /// <summary>
        /// thêm kỹ năng mới cho đồng hành
        /// </summary>
        /// <param name="player">người chơi</param>
        /// <param name="pet">item đồng hành</param>
        /// <param name="skillID">skill id muốn thêm mới</param>
        /// <returns>skillID: add thành công, -1: skill đã tồn tại</returns>
        public static int AddPetSkill(KPlayer player, GoodsData pet, int skillID = -1)
        {
            var propsBytes = Convert.FromBase64String(pet.Props);
            var props = DataHelper.BytesToObject<ItemGenByteData>(propsBytes, 0, propsBytes.Length);
            var listSkills = props.PetPropertyValue.Skip(4).ToList();
            var isReturnTinhoa = skillID == -1; // nếu không chỉ định skill (học random) thì sẽ trả lại bình tinh hoa
            if (skillID == -1)
                skillID = PetManager.GetNewSkillAddToPet(listSkills);

            if (listSkills.Contains(skillID))
                return -1;

            if (skillID != -1)
            {
                // add thêm skill
                props.PetPropertyValue.Insert(4, skillID);
                // skill mới có level 1
                if (!pet.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelSkill))
                    return -99;

            
                var itemToReturn = isReturnTinhoa ? PetItemManager.CaculationPet(player, pet.Id) : new Dictionary<int, int>();

                levelSkill = $"{levelSkill}|1";
                pet.OtherParams[ItemPramenter.Pram_2] = levelSkill;

                propsBytes = DataHelper.ObjectToBytes(props);
                var propsStr = Convert.ToBase64String(propsBytes);

                var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                {
                    { UPDATEITEM.ROLEID, player.RoleID },
                    { UPDATEITEM.ITEMDBID, pet.Id },
                    { UPDATEITEM.PROPS, propsStr },
                    { UPDATEITEM.OTHER_PRAM, pet.OtherParams }
                };

                ItemManager.UpdateItemPrammenter(goodUpdateDict, pet, player, "AddPetSKill", true);

                // sau khi thêm skill xong thì mới reset pet
                // TODO: tối ưu. hiện tại đang update item parameters 2 lần
                ResetPet(player, pet.Id, false);

                foreach (var info in itemToReturn.Keys)
                {
                    ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, info, itemToReturn[info], 0, "PETSKILLBOOK", true, 1, false, Global.ConstGoodsEndTime, "", -1, "", 1, 1, false);
                }

                return skillID;
            }

            return -99;
        }

        /// <summary>
        /// sách kinh nghiệm
        /// </summary>
        private static void UseBookExpPet(KPlayer player, GoodsData item, int petID)
        {
            // check xem người chơi đã dùng quá số lượng sách hôm nay chưa
            int numberbNormal = player.GetValueOfDailyRecore(item.GoodsID);

            if (numberbNormal < 0)
                numberbNormal = 0;

            if (numberbNormal >= 40)
            {
                PlayerManager.ShowNotification(player, $"Bạn đã sử dụng tối đa lượng sách trong ngày (40 quyển)");
                return;
            }
            else
            {
                ItemManager.RemoveItem(player, item.Id);
                ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
                PetManager.UpdateExpPet(player, petID, itemData.ItemValue);
                PlayerManager.ShowNotification(player, $"Tăng thành công cho {item.ItemName} {itemData.ItemValue} điểm kinh nghiệm!");
                player.SetValueOfDailyRecore(item.GoodsID, numberbNormal + 1);

            }

        }

        /// <summary>
        /// Sử dụng Sách kinh nghiệm đồng hành
        /// </summary>
        public static void UseBookItemExp(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                var listPets = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                foreach (var pet in listPets)
                {
                    if (CanAddMoreExpPet(client, pet.Id, itemData.ItemValue))
                    {
                        listPet.Add(pet);
                    }
                }
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành để tăng {itemData.ItemValue} kinh nghiệm cho họ!";
                if (itemData.ItemValue > 100000)
                {
                    dialog.Text += $"\n Bạn đồng hành cấp 30 trở lên mới có thể sử dụng vật phẩm này!";

                }
                dialog.Selections = new Dictionary<int, string>();
                //Thêm vào các sự lựa chọn pet có thể dùng
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

                        UseBookExpPet(client, item, data.SelectID);
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào hãy tìm bạn đồng hành để có thể sử dụng vật phẩm.";
            }

            dialog.ShowDialog(client);
        }
        /// <summary>
        /// Sử dụng Bình tinh hoa
        /// </summary>
        public static void UseBinhTinhHoa(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet() && PetManager.GetLN(x) < 100).ToList();
                if (itemData.Level < 3)
                {
                    listPet.RemoveAll(x => PetManager.IsYenTieuLau(x.GoodsID));
                }
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành sử dụng {itemData.Name} cho họ. Điểm lĩnh ngộ sẽ được tăng dựa theo điểm lĩnh ngộ hiện có của bạn đồng hành.\n\nĐồng hành Yến Tiểu Lâu chỉ có thể sử dụng bình cấp 3 trở lên.";
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
                        string value = GetValueBinhTinhHoa(client, id, data.SelectID);
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                        KNPCDialog Dialog = new KNPCDialog();
                        Dialog.Text = $"Bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} sẽ tăng {value} điểm lĩnh ngộ. Bạn có đồng ý sử dụng không?";

                        Dialog.Selections = new Dictionary<int, string>();
                        Dialog.Selections.Add(1, "Chắc chắn");
                        Dialog.Selections.Add(-1, "Để ta suy nghĩ thêm!");

                        Dialog.OnSelect = (Data) =>
                        {
                            if (Data.SelectID == 1)
                            {
                                PetManager.UpdateLinhNgoPetStr(client, data.SelectID, value);
                                ItemManager.RemoveItem(client, id);
                                PlayerManager.ShowNotification(client, $"Tăng thành công cho {Global.GetGoodsByDbID(client, data.SelectID).ItemName} {value} điểm lĩnh ngộ!");
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(Dialog);
                            }
                            if (Data.SelectID == -1)
                            {
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(Dialog);
                            }
                        };
                        Dialog.ShowDialog(client);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào đủ điều kiện để sử dụng hãy thử lại sau.";
            }

            dialog.ShowDialog(client);
        }


        /// <summary>
        /// kiểm tra xem có thể sử dụng Mật tịch đồng hành được không
        /// </summary>
        private static int CanUseMT(KPlayer client, GoodsData MT, int petID)
        {
            int result = -1;
            ItemData itemData = ItemManager.GetItemTemplate(MT.GoodsID);
            // lấy số skill của pet

            GoodsData petData = Global.GetGoodsByDbID(client, petID);
            KItem petItem = new KItem(petData);
            
            if (itemData.Level == 3 && petData.Forge_level < 100)
            {
                return -6;
            } else if (itemData.Level == 2 && petData.Forge_level < 110)
            {
                return -7;
            } 
            else if (itemData.Level == 1 && petData.Forge_level < 120)
            {
                return -5;
            }

            int numberskill = petItem.SkillsFromPet.Count;
            int numberStar = PetManager.GetNumberSkill(petData);
            int totalSkillPet = PetManager.GetTotalLevelSkill(petData, numberskill);
            if (numberskill * 6 <= totalSkillPet)
            {
                return -1;// ko có skill để nâng cấp
            }
            int mattichthuong = 0;
            int mattichvip = 0;

            try
            {
                if (petData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd))
                {
                    var data = strcmd.Split('|');
                    // Kiểm tra data có đủ 6 phần tử (Exp|thanMat|linhNgo|linhNgoLe|mattichthuong|mattichvip)
                    if (data.Length < 6)
                    {
                        LogManager.WriteLog(LogTypes.Error, $"Pet {petID} có dữ liệu Pram_1 không hợp lệ: {strcmd}");
                        return -10;
                    }
                    mattichthuong = Int32.Parse(data[4]);
                    mattichvip = Int32.Parse(data[5]);
                }
                else
                {
                    return -10;
                }    
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
                return -10;
            }

            //nếu ko phải loại đặc biệt
            if (itemData.Level < 3)
            {
                if (mattichthuong >= 3)
                {
                    return -2; // dùng quá số lượng MT thường
                }

                // sửa yêu cầu mật tịch: cấp đồng hành phải từ 4 trở lên
                if (numberStar >= 4)
                    return 1;

                return -3;


                // if (numberskill <= 3)
                // {
                //     if (itemData.Level == 1)
                //     {
                //         result = 1;
                //     }
                // }
                // else if (numberskill <= 6)
                // {
                //     if (itemData.Level == 2)
                //     {
                //         result = 1;
                //     }
                // }
                // else if (numberskill >= 7)
                // {
                //     if (itemData.Level != 3)
                //     {
                //         result = 1;
                //     }
                // }
            }
            else
            {
                if (mattichvip >= 3)
                {
                    return -2; // dùng quá số lượng MT cao
                }

                if (numberStar >= 4)
                    return 2;

                result = -3;
            }

            return result;
            //ItemData itemData = ItemManager.GetItemTemplate(MT.GoodsID);
            //GoodsData goodsData = Global.GetGoodsByDbID(client, petID);
            //if (goodsData.Forge_level < 120)
            //    return -5;
            //
            //if (!PetManager.CanLevelUpSkill(goodsData))
            //    return -1;
            //
            //
            //if (itemData.Level <= 3)
            //{
            //    int indexValue = 0;
            //    // số sao của pet cũng là số skill của pet
            //    float starvalue = PetManager.GetNumberSkill(goodsData);
            //    if (starvalue <= 4)
            //    {
            //        indexValue = 1;
            //    }
            //    if (starvalue >= 5 && starvalue <= 7)
            //    {
            //        indexValue = 2;
            //    }
            //    if (starvalue >= 8)
            //    {
            //        indexValue = 3;
            //    }
            //    if (indexValue != itemData.Level)
            //        return -2;
            //}
            //
            //
            //int mattichthuong = 0;
            //int mattichvip = 0;
            //
            //
            //try
            //{
            //    if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd))
            //    {
            //        var data = strcmd.Split('|');
            //        mattichthuong = Int32.Parse(data[4]);
            //        mattichvip = Int32.Parse(data[5]);
            //    }
            //    if (itemData.Level <= 3)
            //    {
            //        if (KTGMCommandManager.IsGM(client))
            //            return 1;
            //        if (mattichthuong >= 2)
            //        {
            //            return -1;
            //        }
            //        else
            //        {
            //            return 1;
            //        }
            //    }
            //    else
            //    {
            //        if (KTGMCommandManager.IsGM(client))
            //            return 2;
            //        if (mattichvip >= 2)
            //        {
            //            return -1;
            //        }
            //        else
            //        {
            //            return 2;
            //        }
            //    }
            //}
            //catch
            //{
            //    return -10;
            //}
        }

        /// <summary>
        /// Tiến hành nâng cấp skill cho pet và lưu lại dữ liệu là dùng được mấy quyển rồi
        /// </summary>
        private static void UseMTPet(KPlayer player, GoodsData item, int petID)
        {
            int ret = CanUseMT(player, item, petID);
            if (ret == -1)
            {
                PlayerManager.ShowNotification(player, $"Bạn đồng hành không còn kỹ năng có thể tăng cấp nữa!");
                return;
            }
            if (ret == -2)
            {
                PlayerManager.ShowNotification(player, $"Bạn đồng hành đã sử dụng tối đa {item.ItemName}");
                return;
            }
            if (ret == -3)
            {
                PlayerManager.ShowNotification(player, $"Bạn đồng hành chưa đủ cấp đồng hành để sử dụng {item.ItemName}");
                return;
            }
            if (ret == -5)
            {
                PlayerManager.ShowNotification(player, $"Mật tịch đồng hành (sơ) yêu cầu bạn đồng hành đạt cấp 120 trở lên để sử dụng!");
                return;
            }
            if (ret == -6)
            {
                PlayerManager.ShowNotification(player, $"Mật tịch đồng hành (cao) yêu cầu bạn đồng hành đạt cấp 100 trở lên để sử dụng!");
                return;
            }
            if (ret == -7)
            {
                PlayerManager.ShowNotification(player, $"Mật tịch đồng hành (trung) yêu cầu bạn đồng hành đạt cấp 110 trở lên để sử dụng!");
                return;
            }
            if (ret == -10)
            {
                PlayerManager.ShowNotification(player, "Có lỗi xảy ra, hãy thử lại sau!");
                return;
            }

            if (ret > 0)
            {
                // Gọi LevelUpSkillPet với tham số loại mật tịch (ret = 1: mật tịch thường, ret = 2: mật tịch VIP)
                // Hàm này sẽ tự động lưu số lượng mật tịch đã dùng vào database
                int levelUpResult = PetManager.LevelUpSkillPet(player, petID, ret);
                if (levelUpResult != 1)
                {
                    // Nếu thăng cấp skill thất bại, không xóa item
                    if (levelUpResult == -1)
                    {
                        PlayerManager.ShowNotification(player, $"Bạn đồng hành không còn kỹ năng có thể tăng cấp nữa!");
                    }
                    else if (levelUpResult == -100)
                    {
                        PlayerManager.ShowNotification(player, "Có lỗi xảy ra khi thăng cấp kỹ năng, hãy thử lại sau!");
                        LogManager.WriteLog(LogTypes.Error, $"Player: {player.RoleName} thăng cấp skill pet {petID} thất bại khi sử dụng mật tịch");
                    }
                    return;
                }

                // Chỉ xóa item mật tịch khi thăng cấp skill và lưu số lượng mật tịch thành công
                ItemManager.RemoveItem(player, item.Id);
                PlayerManager.ShowNotification(player, $"Sử dụng {item.ItemName} thành công!");
            }
        }

        /// <summary>
        /// Sử dụng item mật tịch đồng hành
        /// </summary>
        public static void UseMTItem(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                var listPets = client.GoodsDataList.Where(x => x.IsPet()).ToList();
                foreach (var pet in listPets)
                {
                    if (CanUseMT(client, item, pet.Id) > 0)
                        listPet.Add(pet);
                }
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành sử dụng {itemData.Name} cho họ!";
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
                        UseMTPet(client, item, data.SelectID);
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào đủ điều kiện để sử dụng hãy thử lại sau.";
            }

            dialog.ShowDialog(client);
        }

        /// <summary>
        /// Tẩy lại chỉ số cho bạn đồng hành
        /// </summary>
        public static void ResetProbPet(KPlayer client, int id, PetAttrData data = null)
        {
            GoodsData item = Global.GetGoodsByDbID(client, id);
            Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();
            TotalUpdate.Add(UPDATEITEM.ROLEID, client.RoleID);
            TotalUpdate.Add(UPDATEITEM.ITEMDBID, item.Id);
            byte[] Base64Decode = Convert.FromBase64String(item.Props);
            ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);
            if (data == null)
            {
                data = PetManager.GetPetAttrData();

            }

            if (data.Str > 0)
            {
                _ItemBuild.PetPropertyValue[0] = data.Str;
            }
            else
            {
                _ItemBuild.PetPropertyValue[0] = -1;
            }

            if (data.Dex > 0)
            {
                _ItemBuild.PetPropertyValue[1] = data.Dex;
            }
            else
            {
                _ItemBuild.PetPropertyValue[1] = -1;
            }

            if (data.Vit > 0)
            {
                _ItemBuild.PetPropertyValue[2] = data.Vit;
            }
            else
            {
                _ItemBuild.PetPropertyValue[2] = -1;
            }

            if (data.Eng > 0)
            {
                _ItemBuild.PetPropertyValue[3] = data.Eng;
            }
            else
            {
                _ItemBuild.PetPropertyValue[3] = -1;
            }

            byte[] ItemDataByteArray = DataHelper.ObjectToBytes(_ItemBuild);
            var strcmd = Convert.ToBase64String(ItemDataByteArray);
            TotalUpdate.Add(UPDATEITEM.PROPS, strcmd);
            if (!ItemManager.UpdateItemPrammenter(TotalUpdate, item, client, "PET", true))
            {
                string cmd = $"Người chơi: {client.RoleName} lưu sử dụng tẩy tủy kinh pet {id} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
            }

        }
        /// <summary>
        /// Sử dụng Tẩy tủy kinh đồng hành
        /// </summary>
        public static void UseTTKItem(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet()).ToList();
            }
            KNPCDialog dialog = new KNPCDialog();

            if (listPet.Count > 0)
            {

                if (itemData.Level == 1)
                {
                    dialog.Text = $"Hãy chọn 1 bạn đồng hành để tẩy điểm tiềm năng và phân phối lại ngẫu nhiên cho họ!";
                }
                else if (itemData.Level == 2)
                {
                    dialog.Text = $"Hãy chọn 1 bạn đồng hành để tẩy điểm tiềm năng!";

                }

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

                    if (itemData.Level == 1)
                    {
                        if (data.SelectID > 0)
                        {
                            ResetProbPet(client, data.SelectID);
                            PlayerManager.ShowNotification(client, $"Tẩy điểm tiềm năng cho bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} thành công!");
                            ItemManager.RemoveItem(client, id);
                            KT_TCPHandler.CloseDialog(client);
                            KTNPCDialogManager.RemoveNPCDialog(dialog);
                        }
                    }

                    if (itemData.Level == 2)
                    {
                        if (data.SelectID > 0)
                        {
                            KT_TCPHandler.CloseDialog(client);
                            KTNPCDialogManager.RemoveNPCDialog(dialog);
                            KNPCDialog Dialog = new KNPCDialog();
                            Dialog.Text = "Hãy chọn 1 nhánh chỉ số mà bạn mong muốn để phân phối lại điểm tiềm năng!";
                            Dialog.Selections = new Dictionary<int, string>();
                            int index = 1;
                            foreach (var info in PetManager.InfoPets.PetAttrData)
                            {
                                var str = $"Sức mạnh {info.Str}%, Thân pháp {info.Dex}%, Ngoại công {info.Vit}%, Nội công {info.Eng}%";
                                Dialog.Selections.Add(index, str);
                                index++;
                            }
                            Dialog.Selections.Add(-1, "Để ta suy nghĩ thêm!");
                            Dialog.OnSelect = (Data) =>
                            {
                                if (Data.SelectID == -1)
                                {
                                    KT_TCPHandler.CloseDialog(client);
                                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                                }
                                if (Data.SelectID > 0)
                                {
                                    ResetProbPet(client, data.SelectID, PetManager.InfoPets.PetAttrData[Data.SelectID - 1]);
                                    PlayerManager.ShowNotification(client, $"Tẩy điểm tiềm năng cho bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} thành công!");
                                    ItemManager.RemoveItem(client, id);
                                    KT_TCPHandler.CloseDialog(client);
                                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                                }
                            };
                            Dialog.ShowDialog(client);
                        }
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào hãy tìm bạn đồng hành để có thể sử dụng vật phẩm.";
            }

            dialog.ShowDialog(client);
        }

        /// <summary>
        /// Sử dụng thư giới thiệu đồng hành để tiến cử (Khi dùng lần đầu tiên) 
        /// </summary>
        public static void UseThuDongHanhItem(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            var level = itemData.Level + 3;
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet() && x.Forge_level == 1 && PetManager.GetNumberSkill(x) > 1 && PetManager.GetNumberSkill(x) <= level && x.GoodsID != 25311).ToList();
            }

            KNPCDialog dialog = new KNPCDialog();

            if (listPet.Count > 0)
            {
                //if (itemData.Level == 1)
                {
                    dialog.Text = $"Hãy chọn 1 bạn đồng hành để đưa vào thư tiến cử! Chỉ bạn đồng hành có cấp độ {level} sao trở xuống mới có thể tiến cử.";
                }

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

                    //if (itemData.Level == 1)
                    {
                        if (data.SelectID > 0)
                        {
                            KT_TCPHandler.CloseDialog(client);
                            KTNPCDialogManager.RemoveNPCDialog(dialog);
                            KNPCDialog Dialog = new KNPCDialog();
                            GoodsData petGoodsData = Global.GetGoodsByDbID(client, data.SelectID);
                            KItem petItem = new KItem(petGoodsData);
                            Dialog.Text = $"Ngươi có chắc chắn muốn đưa bạn đồng hành {petGoodsData.ItemName} vào thư tiến cử không?";
                            Dialog.Text += $"\n Lưu ý: Sau khi chọn bạn đồng hành sẽ không thể giúp đỡ nữa!";
                            Dialog.Selections = new Dictionary<int, string>();

                            Dialog.Selections.Add(1, "Chắc chắn");
                            Dialog.Selections.Add(-1, "Để ta suy nghĩ đã");

                            Dialog.OnSelect = (Data) =>
                            {
                                if (Data.SelectID == -1)
                                {
                                    KT_TCPHandler.CloseDialog(client);
                                    KTNPCDialogManager.RemoveNPCDialog(dialog);
                                }
                                if (Data.SelectID == 1)
                                {


                                    // Lấy ra id của pet icon cầm tạo ra

                                    if (petItem.SkillsFromPet.Count < 2)
                                    {
                                        PlayerManager.ShowNotification(client, "Đồng hành phải có 2 skill trở lên");
                                        return;
                                    }
                                    else
                                    {
                                        int ItemIconID = PetIcon[petItem.SkillsFromPet.Count - 2];

                                        // Lưu lại dữ liệu của pet cho vào thư
                                        var dataPet = HidePetInLetter(petGoodsData);
                                        Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                                        OtherParams.Add(ItemPramenter.Pram_1, dataPet);
                                        string otherParamString = Convert.ToBase64String(DataHelper.ObjectToBytes<Dictionary<ItemPramenter, string>>(OtherParams));

                                        if (ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, ItemIconID, 1, 0, "DONGHANHTRE", true, 0, false, Global.ConstGoodsEndTime, "", -1, "", 1, 1, false, otherParamString))
                                        {
                                            ItemManager.RemoveItem(client, data.SelectID);
                                            ItemManager.RemoveItem(client, id);
                                        }
                                        KT_TCPHandler.CloseDialog(client);
                                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                                        PlayerManager.ShowNotification(client, $"Đưa bạn đồng hành {petGoodsData.ItemName} vào thư tiến cử thành công!");
                                    }
                                }
                            };
                            Dialog.ShowDialog(client);
                        }
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào đủ điều kiện hãy tìm bạn đồng hành để có thể sử dụng vật phẩm.  Chỉ bạn đồng hành có cấp độ {level} sao trở xuống mới có thể tiến cử.";
            }

            dialog.ShowDialog(client);
        }
        /// <summary>
        /// Sử dụng Bồ đề quả
        /// </summary>
        public static void UseBoDeQua(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            ItemData itemData = ItemManager.GetItemTemplate(item.GoodsID);
            KItem petItem = new KItem(item);
            var listPet = new List<GoodsData>();
            lock (client.GoodsDataList)
            {
                listPet = client.GoodsDataList.Where(x => x.IsPet() && (
                    (x.Forge_level >= 100 && PetManager.GetNumberSkill(x) >= 7 && PetManager.GetNumberSkill(x) < 10 && PetManager.IsYenTieuLau(x.GoodsID)) || 
                    (x.Forge_level >= 100 && PetManager.GetNumberSkill(x) >= 3 && PetManager.GetNumberSkill(x) < 7) || 
                    (x.Forge_level >= 80 && PetManager.GetNumberSkill(x) == 2) || 
                    (x.Forge_level >= 60 && PetManager.GetNumberSkill(x) == 1))
                ).ToList();
            }

            KNPCDialog dialog = new KNPCDialog();
            if (listPet.Count > 0)
            {
                dialog.Text = $"Hãy chọn 1 bạn đồng hành sử dụng {itemData.Name} để trùng sinh bạn đồng hành và nhận được các bình tinh hoa.\n\nYêu cầu bạn đồng hành:\nYến Tiểu Lâu cấp độ 100 trở lên.\n-Đồng hành 3-7 sao cấp độ 100 trở lên.\n-Đồng hành 2 sao cấp độ 80 trở lên.\n-Đồng hành 1 sao cấp độ 60 trở lên.";
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
                        KT_TCPHandler.CloseDialog(client);
                        KTNPCDialogManager.RemoveNPCDialog(dialog);
                        var listItem = CaculationPet(client, data.SelectID);
                        KNPCDialog Dialog = new KNPCDialog();
                        Dialog.Text = $"Bạn có chắc chắn muốn trùng sinh bạn đồng hành {Global.GetGoodsByDbID(client, data.SelectID).ItemName} không?";
                        string infoStr = "";

                        int totalSpaceNeeded = 1;
                        if (listItem.Count > 0)
                        {
                            foreach (var info in listItem.Keys)
                            {
                                totalSpaceNeeded += listItem[info];
                                if (infoStr == "")
                                    infoStr += $"{listItem[info]} {ItemManager.GetItemTemplate(info).Name}";
                                else
                                    infoStr += $", {listItem[info]} {ItemManager.GetItemTemplate(info).Name}";
                            }
                            Dialog.Text += "\nBạn sẽ nhận được " + infoStr;
                        }
                        Dialog.Selections = new Dictionary<int, string>();

                        Dialog.Selections.Add(1, "Chắc chắn");
                        Dialog.Selections.Add(-1, "Để ta suy nghĩ");


                        Dialog.OnSelect = (Data) =>
                        {

                            if (Data.SelectID == -1)
                            {
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(Dialog);
                            }
                            if (Data.SelectID == 1)
                            {
                                if (KTGlobal.IsHaveSpace(10, client))
                                {
                                    ResetPet(client, data.SelectID);

                                    ItemManager.RemoveItem(client, id);
                                    foreach (var info in listItem.Keys)
                                    {
                                        ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, info, listItem[info], 0, "BODEQUA", true, 1, false, Global.ConstGoodsEndTime, "", -1, "", 1, 1, false);
                                    }
                                }
                                else
                                {
                                    PlayerManager.ShowNotification(client, $"Cần sắp xếp {10} ô trống trong túi đồ để nhận bình tinh hoa!");
                                }
                                KT_TCPHandler.CloseDialog(client);
                                KTNPCDialogManager.RemoveNPCDialog(Dialog);
                            }
                        };
                        Dialog.ShowDialog(client);
                    }
                };
            }
            else
            {
                dialog.Text = $"Bạn chưa có bạn đồng hành nào đủ điều kiện để sử dụng hãy thử lại sau.";
            }

            dialog.ShowDialog(client);
        }
        /// <summary>
        /// Lấy ra giá trị của bình tinh hoa
        /// </summary>
        private static string GetValueBinhTinhHoa(KPlayer client, int itemid, int petid)
        {
            string value = "";
            if (client.GoodsDataList == null)
            {
                return value;
            }

            GoodsData pet = Global.GetGoodsByDbID(client, petid);
            GoodsData item = Global.GetGoodsByDbID(client, itemid);
            KItem petItem = new KItem(pet);
            int kindPet = petItem.SkillsFromPet.Count >= 4 ? 2 : 1;

            var tinhHoaConfig = PetManager.TinhHoaValue;
            if (PetManager.IsYenTieuLau(pet.Id))
            {
                tinhHoaConfig = PetManager.YenTieuLauOverride.TinhHoaValue;
            }

            var info = tinhHoaConfig.Where(x => x.ID == item.GoodsID).FirstOrDefault();
            if (info != null)
            {
                if (kindPet > info.Kind)
                {
                    value = (info.Value / 3) + "|" + (info.Value % 3);
                }
                else if (kindPet == info.Kind)
                {
                    value = info.Value + "|0";
                }
                else
                {
                    value = info.Value * 3 + "|0";
                }
            }

            return value;
        }
        /// <summary>
        /// Ẩn pet vào trong thư đồng hành để giao dịch
        /// </summary>
        private static string HidePetInLetter(GoodsData itemGD)
        {
            if (itemGD == null)
            {
                return "";
            }
            string otherParamString = itemGD.OtherParams == null ? "" : Convert.ToBase64String(DataHelper.ObjectToBytes<Dictionary<ItemPramenter, string>>(itemGD.OtherParams));
            return string.Format("{0}_{1}_{2}", itemGD.GoodsID, itemGD.Props, otherParamString);
        }

        /// <summary>
        /// Tạo ra pet khi sử dụng icon pet
        /// </summary>
        public static bool InitPetFromData(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return false;
            }

            GoodsData item = Global.GetGoodsByDbID(client, id);
            if (item == null || item.OtherParams == null)
            {
                return false;
            }
            try
            {
                if (item.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string data))
                {
                    // fields[2] là otherparam của pet
                    string[] fields = data.Split('_');
                    var pet = new GoodsData()
                    {
                        GoodsID = int.Parse(fields[0]),
                        Props = fields[1],
                    };
                    if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, pet.GoodsID, 1, 0, "DONGHANHTRE", true, 1, false, Global.ConstGoodsEndTime, pet.Props, -1, "", 1, 1, false, fields[2]))
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
        /// <summary>
        /// lấy ra thông tin của pet để show ra dialog cho ng chơi biết khi sử dụng icon pet
        /// </summary>
        public static string GetInfoPetInItem(KPlayer client, int id)
        {
            if (client.GoodsDataList == null)
            {
                return "";
            }
            string info = "";
            GoodsData item = Global.GetGoodsByDbID(client, id);
            if (item == null || item.OtherParams == null)
            {
                return "";
            }
            try
            {
                if (item.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string data))
                {
                    string[] fields = data.Split('_');
                    var pet = new GoodsData()
                    {
                        GoodsID = int.Parse(fields[0]),
                        Props = fields[1],
                    };
                    byte[] Base64Decode = Convert.FromBase64String(fields[2]);
                    pet.OtherParams = DataHelper.BytesToObject<Dictionary<ItemPramenter, string>>(Base64Decode, 0, Base64Decode.Length);

                    var strdt = pet.OtherParams[ItemPramenter.Pram_1].Split('|');
                    var DataSkill = pet.OtherParams[ItemPramenter.Pram_2].Split('|');
                    int Exp = Int32.Parse(strdt[0]);
                    int thanMat = Int32.Parse(strdt[1]);
                    int linhNgo = Int32.Parse(strdt[2]);
                    int mattichthuong = Int32.Parse(strdt[4]);
                    int mattichvip = Int32.Parse(strdt[5]);

                    info = $"Thông tin bạn đồng hành:";
                    info += $"\n Tên: {pet.ItemName}, Cấp độ: {pet.Forge_level}";
                    info += $"\n Kinh nghiệm: {Exp} \n Độ lĩnh ngộ: {linhNgo} \n Điểm thân mật: {thanMat}";
                    info += $"\n Cấp đồng hành: {DataSkill.Length}";

                }
            }
            catch
            {
                return "";
            }

            return info;
        }
        /// <summary>
        /// Tính toán số bình tinh hoa nhận được theo kỹ năng
        /// </summary>
        public static IDictionary<int, int> CaculationPet(KPlayer client, int id)
        {
            Dictionary<int, int> listItem = new Dictionary<int, int>();
            GoodsData goodsData = Global.GetGoodsByDbID(client, id);
            KItem petItem = new KItem(goodsData);
            int kindPet = petItem.SkillsFromPet.Count >= 4 ? 2 : 1;

            int TotalValue = 0;
            if (goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_1, out string strcmd) &&
               goodsData.OtherParams.TryGetValue(ItemPramenter.Pram_2, out string dataSkill))
            {
                var data = strcmd.Split('|');
                int linhNgo = Int32.Parse(data[2]);
                var skilllv = dataSkill.Split('|');

                //if (skilllv.Length <= 2)
                //    return listItem;
                //
                foreach (var lv in skilllv)
                {
                    var lvInt = int.Parse(lv);
                    // // skill level 1 không được tính vào lĩnh ngộ
                    if (lvInt == 1)
                        continue;
                    TotalValue += int.Parse(lv) * 10;
                }

                TotalValue += linhNgo - 40;
                TotalValue *= 2;
                TotalValue /= 3;
                LogManager.WriteLog(LogTypes.Pet, $"Player : {client.RoleName} có petID={id} lĩnh ngộ đổi tinh hoa = {TotalValue}");
                int numberbinh4 = TotalValue / 10;
                if (numberbinh4 > 0)
                {
                    var info = PetManager.TinhHoaValue.Where(x => x.Kind == kindPet && x.Value == 10).FirstOrDefault();
                    if (info != null)
                    {
                        listItem[info.ID] = numberbinh4;
                    }

                    TotalValue %= 10;
                }

                int numberbinh3 = TotalValue / 5;
                if (numberbinh3 > 0)
                {
                    var info = PetManager.TinhHoaValue.Where(x => x.Kind == kindPet && x.Value == 5).FirstOrDefault();
                    if (info != null)
                    {
                        listItem[info.ID] = numberbinh3;
                    }

                    TotalValue %= 5;
                }

                int numberbinh2 = TotalValue / 2;
                if (numberbinh2 > 0)
                {
                    var info = PetManager.TinhHoaValue.Where(x => x.Kind == kindPet && x.Value == 2).FirstOrDefault();
                    if (info != null)
                    {
                        listItem[info.ID] = numberbinh2;
                    }

                    TotalValue %= 2;
                }

                var info1 = PetManager.TinhHoaValue.Where(x => x.Kind == kindPet && x.Value == 1).FirstOrDefault();
                if (info1 != null && TotalValue > 0)
                {
                    listItem[info1.ID] = TotalValue;
                }
            }

            if (listItem.Any())
            {
                if (petItem.SkillsFromPet.Count >= 5)
                {
                    // nếu đồng hành 
                    var listID = listItem.Keys.ToList();
                    foreach (var itemid in listID)
                    {
                        listItem[itemid] = listItem[itemid] / 2;
                        if (listItem[itemid] == 0)
                        {
                            listItem[itemid] = 1;
                        }
                    }
                }
                return listItem;
            }

            return null;
        }
        /// <summary>
        /// reset lại mọi chỉ số lại từ đầu cho pet
        /// </summary>
        private static void ResetPet(KPlayer client, int id, bool increaseLevel = true)
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
            ItemData itemData = ItemManager.GetItemTemplate(goodsData.GoodsID);


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
                Exp = 0;
                thanMat = 60;
                linhNgo = 40;
                linhNgoLe = 0;
                mattichthuong = 0;
                mattichvip = 0;
                ItemManager.SetEquipForgeLevel(goodsData, client, 1, 1);
                KItem petItem = new KItem(goodsData);
                ExpNeed = ItemManager.GetMaxPetEXP(1, petItem.SkillsFromPet.Count + 1);

                var skillrs = DataSkill.Split('|');

                var newLVSkillPet = "1";
                var totalSkill = skillrs.Length - 1;
                if (increaseLevel)
                    totalSkill = skillrs.Length;

                for (int i = 0; i < totalSkill; i++)
                {
                    newLVSkillPet += "|1";
                }
                DataSkill = newLVSkillPet;

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

                // bỏ tăng skill pet
                //byte[] Base64Decode = Convert.FromBase64String(goodsData.Props);
                //ItemGenByteData _ItemBuild = DataHelper.BytesToObject<ItemGenByteData>(Base64Decode, 0, Base64Decode.Length);
                //_ItemBuild.PetPropertyValue.Add(PetManager.GetSkillAddToPet(petItem.SkillsFromPet));
                //
                //byte[] ItemDataByteArray = DataHelper.ObjectToBytes(_ItemBuild);
                //var strCmd = Convert.ToBase64String(ItemDataByteArray);
                //TotalUpdate.Add(UPDATEITEM.PROPS, strCmd);

                if (!ItemManager.UpdateItemPrammenter(TotalUpdate, goodsData, client, "PET", true))
                {
                    string cmd = $"Người chơi: {client.RoleName} đặt lại các chỉ số pet {id} thất bại";
                    LogManager.WriteLog(LogTypes.Error, cmd);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
        }
        #endregion

    }
}
