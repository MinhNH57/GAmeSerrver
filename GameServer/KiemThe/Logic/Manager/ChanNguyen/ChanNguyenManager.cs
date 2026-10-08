using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.FlowerTower;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using static GameServer.KiemThe.Logic.Manager.ChanNguyen.ChanNguyen;

namespace GameServer.KiemThe.Logic.Manager.ChanNguyen
{
    public class ChanNguyenManager
    {
        #region Properties
        private static string KTCNPROPUP = "Config/KT_Item/ChanNguyen/ChanNguyenUpProp.xml";

        private static string KTEXPCN = "Config/KT_Item/ChanNguyen/ChanNguyenExp.xml";

        private static string KTCNINFO = "Config/KT_Item/ChanNguyen/ChanNguyenInfo.xml";

        private static string KTCNCreate = "Config/KT_Item/ChanNguyen/ChanNguyenCreate.xml";

        private static string KTCNAHINFO = "Config/KT_Item/ChanNguyen/ChanNguyenAnhHung.xml";

        private static string KTCNTLINFO = "Config/KT_Item/ChanNguyen/ChanNguyenTayLuyen.xml";

        public static List<EXPCN> _ListEXPCN = new List<EXPCN>();

        public static List<ChanNguyenPropUp> _ListCNPropUp = new List<ChanNguyenPropUp>();

        public static List<PropInfo> _ListPropInfo = new List<PropInfo>();

        public static List<RandomCN> _ListCNCreate = new List<RandomCN>();

        public static List<AnhHungInfo> _ListCNAH = new List<AnhHungInfo>();

        public static List<AnhHungInfo> _ListCNTL= new List<AnhHungInfo>();

        #endregion

        #region public methods

        public static void SetUpExpCN()
        {
            string Files = Global.GameResPath(KTEXPCN);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<EXPCN>));

                _ListEXPCN = serializer.Deserialize(stream) as List<EXPCN>;
            }
        }

        public static void SetUpPropUp()
        {
            string Files = Global.GameResPath(KTCNPROPUP);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<ChanNguyenPropUp>));

                _ListCNPropUp = serializer.Deserialize(stream) as List<ChanNguyenPropUp>;
            }
        }
        public static void SetUpPropInfo()
        {
            string Files = Global.GameResPath(KTCNINFO);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<PropInfo>));

                _ListPropInfo = serializer.Deserialize(stream) as List<PropInfo>;
            }
        }

        public static void SetUpCNCreate()
        {
            string Files = Global.GameResPath(KTCNCreate);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<RandomCN>));

                _ListCNCreate = serializer.Deserialize(stream) as List<RandomCN>;
            }
        }

        public static void SetUpCNAH()
        {
            string Files = Global.GameResPath(KTCNAHINFO);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<AnhHungInfo>));

                _ListCNAH = serializer.Deserialize(stream) as List<AnhHungInfo>;
            }
        }

        public static void SetUpCNTL()
        {
            string Files = Global.GameResPath(KTCNTLINFO);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<AnhHungInfo>));

                _ListCNTL = serializer.Deserialize(stream) as List<AnhHungInfo>;
            }
        }

        /// <summary>
        /// Cường hóa chân nguyên
        /// </summary>
        /// <param name="listEXPItem"></param>
        /// <param name="itemCN"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int UpdateExpCN(List<GoodsData> listEXPItem, GoodsData itemCN, KPlayer client)
        {
            if (itemCN.Forge_level >= 120)
            {
                return -1;
            }    
            var expBonus = 0;
            var expTotal = 0;
            foreach (var itemExp in listEXPItem)
            {
                expBonus += 1 * itemExp.GCount;
            }
            expTotal = expBonus;
            ItemData itemData = ItemManager.TotalItem.Where(x => x.ItemID == itemCN.GoodsID).FirstOrDefault();
            
            // xem kinh nghiệm cũ
            var expOld = 0;
            var levelOld = "";
            if (itemCN.OtherParams != null)
            {
                if (itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1) && itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                {
                    expOld = int.Parse(param1);
                    levelOld = levelProps;
                }
            }
            var listExpNeed = ChanNguyenManager._ListEXPCN.Where(x => x.CNLevel == itemData.Level && x.Level > itemCN.Forge_level).ToList();
            var total = 0;
            foreach (var expNeed in listExpNeed)
            {
                total += expNeed.Exp;
            }
            total = total - expOld;

            var expThua = 0;
            if (total < expBonus)
            {
                expThua = expBonus - total;
                expBonus = total;                
            }
            if (expThua > 0)
            {
                var expMinus = expTotal - expThua;
                while (expMinus > 0)
                {
                    var expItem = listEXPItem.OrderBy(x => x.GCount).FirstOrDefault();
                    if (expMinus > expItem.GCount)
                    {
                        expMinus -= expItem.GCount;
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} cường hóa chân nguyên {itemCN.GoodsID}, dùng {expItem.GCount} chân nguyên kinh nghiệm");
                        GlobalEventSource.getInstance().fireEvent(new ChanNguyenEnhanceEventObject(client, expItem.GCount));
                        ItemManager.RemoveItemByCount(client, expItem, expItem.GCount, "CN");
                        listEXPItem.Remove(expItem);
                    }
                    else
                    {
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} cường hóa chân nguyên {itemCN.GoodsID}, dùng {expMinus} chân nguyên kinh nghiệm");
                        GlobalEventSource.getInstance().fireEvent(new ChanNguyenEnhanceEventObject(client, expMinus));
                        ItemManager.RemoveItemByCount(client, expItem, expMinus, "CN");
                        expMinus = 0;
                    }
                }
            }   
            else
            {
                foreach (var expItem in listEXPItem)
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} cường hóa chân nguyên {itemCN.GoodsID}, dùng {expItem.GCount} chân nguyên kinh nghiệm");
                    GlobalEventSource.getInstance().fireEvent(new ChanNguyenEnhanceEventObject(client, expItem.GCount));
                    ItemManager.RemoveItemByCount(client, expItem, expItem.GCount, "CN");       
                }    
            }

            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} bắt đầu cường hóa itemID={itemCN.GoodsID} cấp độ hiện tại {itemCN.Forge_level} tổng kinh nghiệm ={expBonus}");

            while (expBonus > 0 && itemCN.Forge_level < 120)
            {                
                var CNEXPInfo = ChanNguyenManager._ListEXPCN.Where(x => x.CNLevel == itemData.Level && x.Level == itemCN.Forge_level + 1).FirstOrDefault();
                var expNeed = CNEXPInfo.Exp - expOld;
                if (expBonus >= expNeed && itemCN.Forge_level < 120)
                {
                    itemCN.Forge_level += 1;
                    expBonus -= expNeed;
                    expOld = 0;
                    // nếu level chia hết cho 5 thì cho tỷ lệ tăng sao cho tư chất                    
                    if (itemCN.Forge_level % 5 == 0)
                    {
                        // Chuyển chuỗi mã hóa thuộc tính về dạng Object
                        var base64DecodeItem = Convert.FromBase64String(itemCN.Props);
                        var itemProps = DataHelper.BytesToObject<ItemGenByteData>(base64DecodeItem, 0, base64DecodeItem.Length);
                        Dictionary<int, int> lsprop = new Dictionary<int, int>();

                        var listProp = levelOld.Split(';');
                        for (var i = 0; i < listProp.Count(); i++)
                        {
                            if (float.Parse(listProp[i], CultureInfo.InvariantCulture) < 10f)
                            {
                                lsprop.Add(i, i);
                            }
                        }

                        if (lsprop.Count > 0)
                        {
                            var random = KTGlobal.GetRandomNumber(0, lsprop.Count - 1);
                            var fProp = float.Parse(listProp[lsprop[random]], CultureInfo.InvariantCulture);
                            fProp = (float)(fProp + 0.5);
                            listProp[random] = fProp.ToString();
                            levelOld = $"{Convert.ToString(listProp[0], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[1], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[2], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[3], CultureInfo.InvariantCulture)}";
                            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} has itemID={itemCN.GoodsID} sao tư chất là: {Convert.ToString(listProp[0], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[1], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[2], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[3], CultureInfo.InvariantCulture)}");
                        }
                    }
                }
                else
                {    
                    expOld += expBonus;
                    expBonus = 0;
                }
            }
            // lưu vào  DB
            // Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            OtherParams.Add(ItemPramenter.Pram_1, expOld.ToString());
            OtherParams.Add(ItemPramenter.Pram_2, levelOld);
            if (itemData.Level == 2 && itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
            {
                OtherParams.Add(ItemPramenter.Pram_3, param3);
            }

            var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                    {
                        { UPDATEITEM.ROLEID, client.RoleID },
                        { UPDATEITEM.ITEMDBID, itemCN.Id },
                        { UPDATEITEM.FORGE_LEVEL, itemCN.Forge_level },
                        { UPDATEITEM.USING, itemCN.Using},
                        { UPDATEITEM.BAGINDEX, itemCN.BagIndex },
                        { UPDATEITEM.BINDING, itemCN.Binding },
                        { UPDATEITEM.OTHER_PRAM, OtherParams },
                        { UPDATEITEM.PROPS, itemCN.Props}
                    };

            if (!ItemManager.UpdateItemPrammenter(goodUpdateDict, itemCN, client, "ChanNguyen", true))
            {
                string cmd = $"Người chơi: {client.RoleName} lưu tăng exp CN {itemCN.GoodsID} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
                return -1;
            }
            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {client.RoleName} ID={client.RoleID} cường hóa thành công itemID={itemCN.GoodsID} cấp độ hiện tại {itemCN.Forge_level}");
            return 1;
        }

        public static int UpdateExpCNByCreate(int exp, GoodsData itemCN, KPlayer player)
        {
            if (itemCN.Forge_level > 0)
            {
                itemCN.Forge_level = 0;
            }

            var expBonus = exp;
            var expTotal = 0;
            expTotal = expBonus;
            ItemData itemData = ItemManager.TotalItem.Where(x => x.ItemID == itemCN.GoodsID).FirstOrDefault();

            // xem kinh nghiệm cũ
            var expOld = 0;
            var levelOld = "";
            if (itemCN.OtherParams != null)
            {
                if (itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1) && itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                {
                    expOld = int.Parse(param1);
                    levelOld = levelProps;
                }
            }
            var listExpNeed = ChanNguyenManager._ListEXPCN.Where(x => x.CNLevel == itemData.Level && x.Level > itemCN.Forge_level).ToList();
            var total = 0;
            foreach (var expNeed in listExpNeed)
            {
                total += expNeed.Exp;
            }
            total = total - expOld;

            while (expBonus > 0 && itemCN.Forge_level < 120)
            {
                var CNEXPInfo = ChanNguyenManager._ListEXPCN.Where(x => x.CNLevel == itemData.Level && x.Level == itemCN.Forge_level + 1).FirstOrDefault();
                var expNeed = CNEXPInfo.Exp - expOld;
                if (expBonus >= expNeed && itemCN.Forge_level < 120)
                {
                    itemCN.Forge_level += 1;
                    expBonus -= expNeed;
                    expOld = 0;
                    // nếu level chia hết cho 5 thì cho tỷ lệ tăng sao cho tư chất                    
                    if (itemCN.Forge_level % 5 == 0)
                    {
                        // Chuyển chuỗi mã hóa thuộc tính về dạng Object
                        var base64DecodeItem = Convert.FromBase64String(itemCN.Props);
                        var itemProps = DataHelper.BytesToObject<ItemGenByteData>(base64DecodeItem, 0, base64DecodeItem.Length);
                        Dictionary<int, int> lsprop = new Dictionary<int, int>();

                        var listProp = levelOld.Split(';');
                        for (var i = 0; i < listProp.Count(); i++)
                        {
                            if (float.Parse(listProp[i], CultureInfo.InvariantCulture) < 10f)
                            {
                                lsprop.Add(i, i);
                            }
                        }

                        if (lsprop.Count > 0)
                        {
                            var random = KTGlobal.GetRandomNumber(0, lsprop.Count - 1);
                            var fProp = float.Parse(listProp[lsprop[random]], CultureInfo.InvariantCulture);
                            fProp = (float)(fProp + 0.5);
                            listProp[random] = fProp.ToString();
                            levelOld = $"{Convert.ToString(listProp[0], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[1], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[2], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[3], CultureInfo.InvariantCulture)}";
                            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} has itemID={itemCN.GoodsID} sao tư chất là: {Convert.ToString(listProp[0], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[1], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[2], CultureInfo.InvariantCulture)};{Convert.ToString(listProp[3], CultureInfo.InvariantCulture)}");
                        }
                    }
                }
                else
                {
                    expOld += expBonus;
                    expBonus = 0;
                }
            }
            // lưu vào  DB
            // Dictionary<UPDATEITEM, object> TotalUpdate = new Dictionary<UPDATEITEM, object>();

            Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
            OtherParams.Add(ItemPramenter.Pram_1, expOld.ToString());
            OtherParams.Add(ItemPramenter.Pram_2, levelOld);
            if (itemData.Level == 2 && itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
            {
                OtherParams.Add(ItemPramenter.Pram_3, param3);
            }

            var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                    {
                        { UPDATEITEM.ROLEID, player.RoleID },
                        { UPDATEITEM.ITEMDBID, itemCN.Id },
                        { UPDATEITEM.FORGE_LEVEL, itemCN.Forge_level },
                        { UPDATEITEM.USING, itemCN.Using},
                        { UPDATEITEM.BAGINDEX, itemCN.BagIndex },
                        { UPDATEITEM.BINDING, itemCN.Binding },
                        { UPDATEITEM.OTHER_PRAM, OtherParams },
                        { UPDATEITEM.PROPS, itemCN.Props}
                    };

            if (!ItemManager.UpdateItemPrammenter(goodUpdateDict, itemCN, player, "ChanNguyen", true))
            {
                string cmd = $"Người chơi: {player.RoleName} lưu tăng exp CN {itemCN.GoodsID} thất bại";
                LogManager.WriteLog(LogTypes.Error, cmd);
                return -1;
            }
            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} cường hóa thành công itemID={itemCN.GoodsID} cấp độ hiện tại {itemCN.Forge_level}");
            return 1;
        }

        /// <summary>
        /// luyện hóa chân nguyên
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemGD"></param>
        /// <param name="recipeGD"></param>
        /// <param name="listStones"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static int UpdatePropCN(KPlayer player, GoodsData itemGD, GoodsData recipeGD, List<GoodsData> listStones, string propName)
        {
            // check như client
            if (itemGD.Forge_level < 120)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {itemGD.GoodsID} chưa dủ cấp độ");
                return -1;
            }    
            if (recipeGD.Forge_level < 120)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {recipeGD.GoodsID} chưa dủ cấp độ");
                return -1;
            }    
            if (recipeGD.Binding == 1)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {recipeGD.GoodsID} chưa mở khóa");
                return -1;
            }    
            var emptyProps = new List<PropUp>();
            var itemProps = new List<float>();
            if (itemGD.OtherParams != null)
            {
                if (itemGD.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                {
                    var listProps = levelProps.Split(';');
                    for (int i = 0; i < listProps.Length; i++)
                    {
                        itemProps.Add(float.Parse(listProps[i], CultureInfo.InvariantCulture));
                    }
                }
            }
            var recipeProps = new List<float>();
            if (recipeGD.OtherParams != null)
            {
                if (recipeGD.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProps))
                {
                    var listProps = levelProps.Split(';');
                    for (int i = 0; i < listProps.Length; i++)
                    {
                        recipeProps.Add(float.Parse(listProps[i], CultureInfo.InvariantCulture));
                    }
                }
            }

            // lấy các thuộc tính của item
            // Nếu thông tin trang bị không tồn tại
            var item = ItemManager.TotalItem.Where(x => x.ItemID == itemGD.GoodsID).FirstOrDefault();
            if (item == null)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {itemGD.GoodsID} không thấy ở file cấu hình");
                return -1;
            }
            var recipe = ItemManager.TotalItem.Where(x => x.ItemID == recipeGD.GoodsID).FirstOrDefault();
            if (recipe == null)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {recipeGD.GoodsID} không thấy ở file cấu hình");
                return -1;
            }   
            
            if (itemGD.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var paramItem3))
            {
                item.ListBasicProp.Clear();
                // thực hiện tạo danh sách thuộc tính
                var listType = paramItem3.Split(';');
                for (var i = 0; i < listType.Count(); i++)
                {
                    item.ListBasicProp.Add(new BasicProp
                    {
                        BasicPropType = listType[i],
                        Index = (sbyte)(i + 1)
                    });
                }
            }
            var emptyPropsItem = item.ListBasicProp?.OrderBy(x => x.Index)?.ToList();
            
            if (recipe.Level == 2 && recipeGD.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var paramRecipe3))
            {
                recipe.ListBasicProp.Clear();
                // thực hiện tạo danh sách thuộc tính
                var listType = paramRecipe3.Split(';');
                for (var i = 0; i < listType.Count(); i++)
                {
                    recipe.ListBasicProp.Add(new BasicProp
                    {
                        BasicPropType = listType[i],
                        Index = (sbyte)(i + 1)
                    });
                }
            }
            var emptyPropsRecipe = recipe.ListBasicProp?.OrderBy(x => x.Index)?.ToList();
            // lấy ra các tư chất cùng nhau
            foreach (var prop in emptyPropsItem)
            {
                var propRecipe = emptyPropsRecipe.Where(x => x.BasicPropType == prop.BasicPropType).FirstOrDefault();
                if (propRecipe != null)
                {
                    var propIndex = (int)(itemProps[prop.Index - 1] * 2f);
                    var recipeIndex = (int)(recipeProps[propRecipe.Index - 1] * 2);
                    if (propIndex == recipeIndex && propIndex < 20)
                    {
                        var propInfo = _ListPropInfo.Where(x => x.Name == prop.BasicPropType).FirstOrDefault();
                        PropUp something = new PropUp();
                        something.Title = propInfo.Title;
                        something.NameProp = prop.BasicPropType;
                        something.StarOld = itemProps[prop.Index - 1];
                        something.StarUp = 1;
                        emptyProps.Add(something);
                    }
                    else if (propIndex < recipeIndex && propIndex < 20)
                    {
                        var propInfo = _ListPropInfo.Where(x => x.Name == prop.BasicPropType).FirstOrDefault();
                        PropUp something = new PropUp();
                        something.Title = propInfo.Title;
                        something.NameProp = prop.BasicPropType;
                        something.StarOld = itemProps[prop.Index - 1];
                        something.StarUp = 2;
                        emptyProps.Add(something);
                    }    
                }
            }
            if (emptyProps.Count > 0)
            {
                var propInfo = emptyProps.Where(x => x.Title == propName).FirstOrDefault();
                if (propInfo != null)
                {
                    // tính toán xem đủ huyền tinh và cần bn bạc
                    long value = 0;
                    int binding = 0;
                    if (listStones != null && listStones.Count > 0)
                    {
                        foreach (var stone in listStones)
                        {
                            var stoneInfo = ItemManager.TotalItem.Where(x => x.ItemID == stone.GoodsID).FirstOrDefault();
                            if (stoneInfo != null)
                            {
                                value += stoneInfo.ItemValue;
                            }    

                            if (stone.Binding == 1)
                            {
                                binding = 1;
                            }    
                        }
                    }
                    var level = (int)((propInfo.StarOld + propInfo.StarUp) * 2);
                    if (value > 0)
                    {
                        level++;
                    }
                    level = Math.Min(20, level);
                    var propUpInfoUp = _ListCNPropUp.Where(x => x.Name == propName && x.Level == level).FirstOrDefault();
                    // tỷ lệ thêm sao là
                    double valueNeed = propUpInfoUp.Value;
                    var rate = (int)Math.Floor(value / valueNeed * 100);
                    var random = KTGlobal.GetRandomNumber(0, 100);
                    if (rate > random)
                    {
                        propInfo.StarUp += 0.5f;
                    }
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} có tỷ lệ huyền tinh là {rate} để thêm 0.5 sao, random = {random}");
                   
                    // đủ tiền ko
                    var moneyNeed = propUpInfoUp.Money;
                    if (KTGlobal.GetMoneyhave(player, MoneyType.Bac) < moneyNeed)
                    {
                        PlayerManager.ShowNotification(player, "Bạn không có đủ bạc trong người");
                        return -1;
                    }
                    // xóa recipe, listStones
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng item {recipeGD.GoodsID} để luyện hóa {itemGD.GoodsID} tư chất {propName} từ {propInfo.StarOld} sao lên {propInfo.StarUp} sao");
                    ItemManager.RemoveItem(player, recipeGD.Id);
                    
                    foreach (var stone in listStones)
                    {
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} dùng huyền tinh {stone.GoodsID} để luyện hóa {itemGD.GoodsID} tư chất {propName} từ {propInfo.StarOld} sao lên {propInfo.StarUp} sao");
                        ItemManager.RemoveItem(player, stone.Id);                        
                    }
                    // trừ tiền
                    KTGlobal.SubMoney(player, moneyNeed, MoneyType.Bac, "ChanNguyen");
                    // them log
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player {player.RoleName} ID={player.RoleID} tiêu {moneyNeed} bạc để luyện hóa {itemGD.GoodsID} tư chất {propName} từ {propInfo.StarOld} sao lên {propInfo.StarUp} sao");
                    // nâng sao cho item
                    var propRecipe = emptyPropsItem.Where(x => x.BasicPropType == propInfo.NameProp).FirstOrDefault();
                    itemProps[propRecipe.Index -1] = propInfo.StarOld + propInfo.StarUp;
                    if (itemProps[propRecipe.Index-1] > 10)
                    {
                        itemProps[propRecipe.Index-1] = 10;
                    }
                    if (itemGD.Binding == 0)
                    {
                        itemGD.Binding = binding;
                    }                        
                    // luu vao db
                    Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                    if (itemGD.OtherParams != null)
                    {
                        if (itemGD.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var levelProp) && itemGD.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var expOld))
                        {
                            levelProp = $"{Convert.ToString(itemProps[0], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[1], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[2], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[3], CultureInfo.InvariantCulture)}";
                            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} has itemID={itemGD.GoodsID} sao tư chất là: {Convert.ToString(itemProps[0], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[1], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[2], CultureInfo.InvariantCulture)};{Convert.ToString(itemProps[3], CultureInfo.InvariantCulture)}");
                            OtherParams.Add(ItemPramenter.Pram_2, levelProp);
                            OtherParams.Add(ItemPramenter.Pram_1, expOld);
                            if (itemGD.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                            {
                                OtherParams.Add(ItemPramenter.Pram_3, param3);
                            }
                        }
                    }

                    var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                    {
                        { UPDATEITEM.ROLEID, player.RoleID },
                        { UPDATEITEM.ITEMDBID, itemGD.Id },
                        { UPDATEITEM.FORGE_LEVEL, itemGD.Forge_level },
                        { UPDATEITEM.USING, itemGD.Using},
                        { UPDATEITEM.BAGINDEX, itemGD.BagIndex },
                        { UPDATEITEM.BINDING, itemGD.Binding },
                        { UPDATEITEM.OTHER_PRAM, OtherParams },
                        { UPDATEITEM.PROPS, itemGD.Props }
                    };

                    ItemManager.UpdateItemPrammenter(goodUpdateDict, itemGD, player, "ChanNguyen", true);
                }
            } 
            else
            {
                return -1;
            }    
            return 1;
        }

        /// <summary>
        /// Tạo ra chân nguyên anh hùng
        /// </summary>
        /// <param name="player"></param>
        /// <param name="listCN"></param>
        /// <param name="itemBDQ"></param>
        /// <returns></returns>
        public static int CreateChanNguyenAnhHung(KPlayer player, List<GoodsData> listCN, GoodsData itemBDQ)
        {
            // duyệt danh sách cn xem đủ đk ko
            foreach (var itemCN in listCN)
            {
                if (itemCN.Forge_level < 120)
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng itemID={itemCN.GoodsID} chưa đủ cấp độ");
                    return -1;
                }
                // nếu là chân nguyên anh hùng r thì thôi
                ItemData itemData = ItemManager.GetItemTemplate(itemCN.GoodsID);
                if (itemData.Level == 2)
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng itemID={itemCN.GoodsID} đã là chân nguyên anh hùng");
                    return -1;
                }    
            }

            // đủ tiền ko
            if (KTGlobal.GetMoneyhave(player, MoneyType.Bac) < 1000000)
            {
                PlayerManager.ShowNotification(player, "Bạn không đủ bạc (cần tối thiểu 100 vạn bạc)");
                return -1;
            }

            // thực hiện xóa vật phẩm và trừ tiền
            foreach (var itemCN in listCN)
            {
                LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng itemID={itemCN.GoodsID} để tạo chân nguyên anh hùng");
                ItemManager.RemoveItem(player, itemCN.Id);                
            }

            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng bồ đề quả itemID={itemBDQ.GoodsID} để tạo chân nguyên anh hùng");
            ItemManager.RemoveItem(player, itemBDQ.Id);

            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng 1000000 bạc để tạo chân nguyên anh hùng");
            KTGlobal.SubMoney(player, 1000000, MoneyType.Bac, "ChanNguyen");

            // thêm chân nguyên anh hùng 25254
            ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, 25254, 1, 0, "ChanNguyen", true, 1, false, Global.ConstGoodsEndTime);
            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} tạo chân nguyên anh hùng thành công");
            return 1;
        }

        public static int UnBindingChanNguyen(KPlayer player, List<GoodsData> listCN)
        {
            foreach (var itemCN in listCN)
            {
                if (itemCN.Binding == 0)
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} mở khóa thất bại do chân nguyên không khóa");
                    return -1;
                } 
            }
            int NAT = 928;
            int numberNAT = 5 * listCN.Count;
            // nếu ko đủ NHHT
            var listNAT = player.GoodsDataList.Where(x => x.GoodsID == NAT).ToList().Clone();
            int totalNAT = 0;
            foreach (var itemNAT in listNAT)
            {
                totalNAT += itemNAT.GCount;
            }
            if (totalNAT < 5 * listCN.Count)
            {
                PlayerManager.ShowNotification(player, $"Bạn không đủ NAT(cần {numberNAT} NAT)");
                return -1;
            }
            foreach ( var itemNAT in listNAT)
            {
                if (numberNAT > itemNAT.GCount)
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng {itemNAT.GCount} Nguyệt Anh Thạch để mở khóa chân nguyên");
                    ItemManager.RemoveItemByCount(player, itemNAT, itemNAT.GCount, "ChanNguyen");
                    numberNAT -= itemNAT.GCount; 
                }
                else
                {
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng {numberNAT} Nguyệt Anh Thạch để mở khóa chân nguyên");
                    ItemManager.RemoveItemByCount(player, itemNAT, numberNAT, "ChanNguyen");
                    numberNAT = 0;
                    break;
                }
            }
            foreach (var itemCN in listCN)
            {
                itemCN.Binding = 0;
                // lưu lại vào db
                var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                    {
                        { UPDATEITEM.ROLEID, player.RoleID },
                        { UPDATEITEM.ITEMDBID, itemCN.Id },
                        { UPDATEITEM.FORGE_LEVEL, itemCN.Forge_level },
                        { UPDATEITEM.USING, itemCN.Using},
                        { UPDATEITEM.BAGINDEX, itemCN.BagIndex },
                        { UPDATEITEM.BINDING, itemCN.Binding },
                        { UPDATEITEM.OTHER_PRAM, itemCN.OtherParams },
                        { UPDATEITEM.PROPS, itemCN.Props }
                    };

                ItemManager.UpdateItemPrammenter(goodUpdateDict, itemCN, player, "ChanNguyen", true);
            }
            LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID}  mở khóa chân nguyên thành công");
            return 1;
        }

        public static string[] ResetChanNguyen(KPlayer player, GoodsData itemCN, List<GoodsData> listItem)
        {
            string[] listBasicProp = new string[4];

            ItemData itemData = ItemManager.TotalItem.Where(x => x.ItemID == itemCN.GoodsID).FirstOrDefault();

            if (itemData.Category != 2)
            {
                PlayerManager.ShowNotification(player, "Chỉ tẩy luyện chân nguyên anh hùng");
                return listBasicProp;
            }    
            // đủ tiền ko
            if (KTGlobal.GetMoneyhave(player, MoneyType.Bac) < 1000000)
            {
                PlayerManager.ShowNotification(player, "Bạn không đủ bạc (cần tối thiểu 100 vạn bạc)");
                return listBasicProp;
            }

            // lay ra ds cac item xem du nguyen lieu chua
            int totalKNCN = 0;
            int totalBDQ = 0;
            foreach(var item in listItem)
            {
                // neu la bo de qua
                if(item.GoodsID == 1101)
                {
                    totalBDQ++;
                }
                else if(item.GoodsID == 25270)
                {
                    totalKNCN += item.GCount;
                }
            }

            int needKNCN = 2000;
            int needBDQ = 1;
            if (totalBDQ < needBDQ)
            {
                PlayerManager.ShowNotification(player, "Chưa đủ nguyên liệu");
                return listBasicProp;
            }

            if (totalKNCN < needKNCN)
            {
                PlayerManager.ShowNotification(player, "Chưa đủ nguyên liệu");
                return listBasicProp;
            }

            // trừ tiền
            KTGlobal.SubMoney(player, 1000000, MoneyType.Bac, "ChanNguyen");

            // trừ nguyên liệu
            foreach (var item in listItem)
            {
                if (needBDQ > 0 && item.GoodsID == 1101)
                {
                    ItemManager.RemoveItem(player, item.Id);
                    LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng {1} bồ đề quả chân nguyên để tẩy luyện chân nguyên");
                    needBDQ--;
                }
                else if (needKNCN > 0 && item.GoodsID == 25270)
                {
                    if (needKNCN >= item.GCount)
                    {
                        ItemManager.RemoveItemByCount(player, item, item.GCount, "ChanNguyen");
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng {item.GCount} kinh nghiệm chân nguyên để tẩy luyện chân nguyên");
                        needKNCN -= item.GCount;
                    }
                    else
                    {
                        ItemManager.RemoveItemByCount(player, item, needKNCN, "ChanNguyen");
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} ID={player.RoleID} dùng {needKNCN} kinh nghiệm chân nguyên để tẩy luyện chân nguyên");
                        needKNCN = 0;
                    }
                }
                else if (needBDQ <= 0 && needKNCN <= 0)
                {
                    break;
                }
            }

            player.ListBasicProp = new string[4];
            player.IDCNReset = itemCN.Id;
            // thực hiện tạo các dòng mới cho chân nguyên
            ///nếu là chân nguyên anh hùng
            if (itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
            {
                var listType = param3.Split(';');
                for (var i = 0; i < listType.Count(); i++)
                {
                    // lay thong tin dong
                    var ListPropInfo = _ListCNTL.Where(x => x.BasicPropType != listType[i] && !listBasicProp.Contains(x.BasicPropType)).ToList().Clone();
                    var propRan = StaticRandom.AccumulatedProbabilitySelection(ListPropInfo, ListPropInfo.Select(x => x.Rate).ToList());
                    listBasicProp[i] = propRan.BasicPropType;
                    player.ListBasicProp[i] = propRan.BasicPropType;

                }
            }    
            else /// nếu là chân nguyên thường
            {
                for (var i = 0; i < itemData.ListBasicProp.Count; i++)
                {
                    var ListPropInfo = _ListCNTL.Where(x => x.BasicPropType != itemData.ListBasicProp[i].BasicPropType && !listBasicProp.Contains(x.BasicPropType)).ToList().Clone();
                    var propRan = StaticRandom.AccumulatedProbabilitySelection(ListPropInfo, ListPropInfo.Select(x => x.Rate).ToList());
                    listBasicProp[i] = propRan.BasicPropType;
                    player.ListBasicProp[i] = propRan.BasicPropType;
                        
                }    
            }    
            
            return listBasicProp;
        }

        public static int CompleteResetCN(KPlayer player, GoodsData itemCN, bool isComplete)
        {
            // xem đúng id ko
            if (itemCN.Id != player.IDCNReset)
            {
                //PlayerManager.ShowNotification(player, "Chân nguyên sai ID trước khi tẩy luyện");
                return -1;
            }
            
            if (player.ListBasicProp == null || player.ListBasicProp.Length <= 0)
            {
                PlayerManager.ShowNotification(player, "Chưa có danh sách sách tư chất sau khi tẩy luyện");
                return -1;
            }
            // nếu là đồng ý
            if (isComplete)
            {
                string param3 = $"{player.ListBasicProp[0]};{player.ListBasicProp[1]};{player.ListBasicProp[2]};{player.ListBasicProp[3]}";
                Dictionary<ItemPramenter, string> OtherParams = new Dictionary<ItemPramenter, string>();
                OtherParams.Add(ItemPramenter.Pram_3, param3);
                if (itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_1, out var param1)
                    && itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_2, out var param2))
                {
                    OtherParams.Add(ItemPramenter.Pram_1, param1);
                    OtherParams.Add(ItemPramenter.Pram_2, param2);
                }
                ItemData itemData = ItemManager.TotalItem.Where(x => x.ItemID == itemCN.GoodsID).FirstOrDefault();
                var oldListData = itemData.ListBasicProp.Clone();
                itemData.ListBasicProp.Clear();
                for (var i = 0; i < player.ListBasicProp.Length; i++)
                {
                    var infoProp = _ListCNTL.Where(x => x.BasicPropType == player.ListBasicProp[i]).FirstOrDefault();
                    var prop = new BasicProp
                    {
                        BasicPropType = infoProp.BasicPropType,
                        BasicPropPA1Min = infoProp.BasicPropPA1Min,
                        BasicPropPA2Min = infoProp.BasicPropPA2Min,
                        BasicPropPA3Min = infoProp.BasicPropPA3Min,
                        BasicPropPA1Max = infoProp.BasicPropPA1Max,
                        BasicPropPA2Max = infoProp.BasicPropPA2Max,
                        BasicPropPA3Max = infoProp.BasicPropPA3Max,
                    };
                    itemData.ListBasicProp.Add(prop);
                }
                var oldLevel = itemData.Level;
                itemData.Level = 2;
                var Probs = ItemManager.GenerateProbs(itemData, 0);
                itemData.Level = oldLevel;
                itemData.ListBasicProp.Clear();
                itemData.ListBasicProp.AddRange(oldListData);
                var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                    {
                        { UPDATEITEM.ROLEID, player.RoleID },
                        { UPDATEITEM.ITEMDBID, itemCN.Id },
                        { UPDATEITEM.FORGE_LEVEL, itemCN.Forge_level },
                        { UPDATEITEM.USING, itemCN.Using},
                        { UPDATEITEM.BAGINDEX, itemCN.BagIndex },
                        { UPDATEITEM.BINDING, itemCN.Binding },
                        { UPDATEITEM.OTHER_PRAM, OtherParams },
                        {UPDATEITEM.PROPS, Probs }
                    };

                ItemManager.UpdateItemPrammenter(goodUpdateDict, itemCN, player, "ChanNguyen", true);
            }
            player.IDCNReset = 0;
            player.ListBasicProp = new string[4];
            return 1;
        }

        /// <summary>
        /// lấy ra ngẫu nhiên các thuộc tính cho chân nguyên anh hùng
        /// </summary>
        /// <returns></returns>
        public static List<BasicProp> GetBasicProps(List<BasicProp> basicProps = null)
        {
            List<BasicProp> result = new List<BasicProp>();
            var ListPropInfo = _ListCNAH.Clone();
            int count = 0;
            if (basicProps != null)
            {
                foreach (var basicProp in basicProps)
                {
                    result.Add(basicProp);
                    var prop = ListPropInfo.Where(x => x.BasicPropType == basicProp.BasicPropType).FirstOrDefault();
                    if (prop != null)
                    {
                        ListPropInfo.Remove(prop);
                    }    
                }
                count = basicProps.Count;
            }    
            
            for (int i = 0; i < 4 - count; i++)
            {
                BasicProp prop = new BasicProp();
                prop.Index = i + 1 + count;
                var propRan = StaticRandom.AccumulatedProbabilitySelection(ListPropInfo, ListPropInfo.Select(x => x.Rate).ToList());
                prop.BasicPropType = propRan.BasicPropType;
                prop.BasicPropPA1Min = propRan.BasicPropPA1Min;
                prop.BasicPropPA1Max = propRan.BasicPropPA1Max;
                prop.BasicPropPA2Min = propRan.BasicPropPA2Min;
                prop.BasicPropPA2Max = propRan.BasicPropPA2Max;
                prop.BasicPropPA3Min = propRan.BasicPropPA3Min;
                prop.BasicPropPA3Max = propRan.BasicPropPA3Max;
                result.Add(prop);
                ListPropInfo.Remove(propRan);
            }
            return result;
        }

        public static void ResetBaiscProps(KPlayer player)
        {
            // lay ra danh sach chan nguyen cuar nguoi choi
            var listCN = player.GoodsDataList.Where(x => x.IsCN()).ToList();
            foreach (var itemCN in listCN)
            {
                var base64Decode = Convert.FromBase64String(itemCN.Props);
                var equipProp = DataHelper.BytesToObject<ItemGenByteData>(base64Decode, 0, base64Decode.Length);
                if (itemCN.OtherParams.TryGetValue(ItemPramenter.Pram_3, out var param3))
                {
                    bool reset = false;
                    var listProp = param3.Split(';');
                    for (var i = 0; i < listProp.Length; i++)
                    {
                        if (listProp[i] == "life_mana_max_p" || listProp[i] == "life_mana_replenish_p" || listProp[i] == "melee_range_damagereturn_p")
                        {
                            if (equipProp.BasicPropValue[i * 3 +1] <=0)
                            {
                                reset = true;
                            }    
                           
                        }    
                    }    
                    if (equipProp.BasicPropValue[0] <= 0 || equipProp.BasicPropValue[3] <=0 || equipProp.BasicPropValue[6] <=0 || equipProp.BasicPropValue[9] <=0 || reset)
                    {
                        
                        ItemData itemData = ItemManager.TotalItem.Where(x => x.ItemID == itemCN.GoodsID).FirstOrDefault();
                        var oldListData = itemData.ListBasicProp.Clone();
                        itemData.ListBasicProp.Clear();
                        for (var i = 0; i < listProp.Length; i++)
                        {

                            {
                                var infoProp = _ListCNTL.Where(x => x.BasicPropType == listProp[i]).FirstOrDefault();
                                var prop = new BasicProp
                                {
                                    BasicPropType = infoProp.BasicPropType,
                                    BasicPropPA1Min = infoProp.BasicPropPA1Min,
                                    BasicPropPA2Min = infoProp.BasicPropPA2Min,
                                    BasicPropPA3Min = infoProp.BasicPropPA3Min,
                                    BasicPropPA1Max = infoProp.BasicPropPA1Max,
                                    BasicPropPA2Max = infoProp.BasicPropPA2Max,
                                    BasicPropPA3Max = infoProp.BasicPropPA3Max,
                                };
                                itemData.ListBasicProp.Add(prop);
                            }

                        }
                        var oldLevel = itemData.Level;
                        itemData.Level = 2;
                        var Probs = ItemManager.GenerateProbs(itemData, 0);
                        itemData.Level = oldLevel;
                        itemData.ListBasicProp.Clear();
                        itemData.ListBasicProp.AddRange(oldListData);
                        var goodUpdateDict = new Dictionary<UPDATEITEM, object>
                        {
                            { UPDATEITEM.ROLEID, player.RoleID },
                            { UPDATEITEM.ITEMDBID, itemCN.Id },
                            { UPDATEITEM.FORGE_LEVEL, itemCN.Forge_level },
                            { UPDATEITEM.USING, itemCN.Using},
                            { UPDATEITEM.BAGINDEX, itemCN.BagIndex },
                            { UPDATEITEM.BINDING, itemCN.Binding },
                            { UPDATEITEM.OTHER_PRAM, itemCN.OtherParams },
                            {UPDATEITEM.PROPS, Probs }
                        };
                        ItemManager.UpdateItemPrammenter(goodUpdateDict, itemCN, player, "ChanNguyen", true);
                        LogManager.WriteLog(LogTypes.ChanNguyen, $"Player: {player.RoleName} đã reset chân nguyên itemID={itemCN.GoodsID}; idDB={itemCN.Id} thành công");
                    }                        
                }
            }
            PlayerManager.ShowNotification(player, "Đã hoàn thành");
        }
        #endregion

        #region
        /// <summary>
        /// Nhận gói tin cường hóa Chân nguyên
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
        public static TCPProcessCmdResults ResponseEquipEnhance(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            EquipEnhance equipEnhance = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equipEnhance = DataHelper.BytesToObject<EquipEnhance>(data, 0, count);
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

                /// Vật phẩm tương ứng trong túi người chơi
                GoodsData itemGD = client.GoodsDataList.Where(x => x.Id == equipEnhance.EquipDbID).FirstOrDefault();
                /// Nếu vật phẩm tương ứng khong tồn tại trong túi người chơi
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(client, "Vị trí trang bị không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Nếu không có danh sách Huyền Tinh
                if (equipEnhance.CrystalStonesDbID == null || equipEnhance.CrystalStonesDbID.Count <= 0)
                {
                    PlayerManager.ShowNotification(client, "Không có kinh nghiệm chân nguyên, không thể cường hóa trang bị!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> crystalStones = client.GoodsDataList.Where(x => equipEnhance.CrystalStonesDbID.Contains(x.Id)).ToList();
                /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                if (crystalStones.Count != equipEnhance.CrystalStonesDbID.Count)
                {
                    PlayerManager.ShowNotification(client, "Danh sách kinh nghiệm chân nguyên không hợp lệ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                int ret = ChanNguyenManager.UpdateExpCN(crystalStones, itemGD, client);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_ENHANCE_CHANNGUYEN);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Nhận gói tin luyện hóa chân nguyên
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
        public static TCPProcessCmdResults ResponseCNCrafting(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            G2C_CNRefine equipEnhance = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equipEnhance = DataHelper.BytesToObject<G2C_CNRefine>(data, 0, count);
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

                /// chân nguyên chính
                GoodsData itemGD = client.GoodsDataList.Where(x => x.Id == equipEnhance.EquipDbID).FirstOrDefault();
                /// chan nguyên phụ 
                GoodsData recipeGD = client.GoodsDataList.Where(x => x.Id == equipEnhance.RecipeDbID).FirstOrDefault();
                /// Nếu vật phẩm tương ứng khong tồn tại trong túi người chơi
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(client, "Vị trí trang bị không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                ///// Nếu không có danh sách Huyền Tinh
                //if (equipEnhance.CrystalStoneDbIDs == null || equipEnhance.CrystalStoneDbIDs.Count <= 0)
                //{
                //    PlayerManager.ShowNotification(client, "Không có kinh nghiệm chân nguyên, không thể cường hóa trang bị!");
                //    return TCPProcessCmdResults.RESULT_OK;
                //}

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> crystalStones = new List<GoodsData>();
                if (equipEnhance.CrystalStoneDbIDs != null && equipEnhance.CrystalStoneDbIDs.Count > 0)
                {
                    crystalStones = client.GoodsDataList.Where(x => equipEnhance.CrystalStoneDbIDs.Contains(x.Id)).ToList();
                    /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                    if (crystalStones.Count != equipEnhance.CrystalStoneDbIDs.Count)
                    {
                        PlayerManager.ShowNotification(client, "Danh sách kinh nghiệm chân nguyên không hợp lệ!");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                }
                
                int ret = ChanNguyenManager.UpdatePropCN(client, itemGD, recipeGD, crystalStones, equipEnhance.PropName);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_CRAFTING_CHANNGUYEN);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Nhận gói tin chân nguyên anh hùng
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
        public static TCPProcessCmdResults ResponseCNHero(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            G2C_CNAHRefine equip = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equip = DataHelper.BytesToObject<G2C_CNAHRefine>(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                /// Nếu dữ liệu trong gói tin không tồn tại
                if (equip == null)
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

                /// chân nguyên 1
                GoodsData CN1 = client.GoodsDataList.Where(x => x.Id == equip.CN1ID).FirstOrDefault();
                /// chân nguyên 1
                GoodsData CN2 = client.GoodsDataList.Where(x => x.Id == equip.CN2ID).FirstOrDefault();
                /// chân nguyên 1
                GoodsData CN3 = client.GoodsDataList.Where(x => x.Id == equip.CN3ID).FirstOrDefault();
                /// Bồ đề quả
                GoodsData BDQ = client.GoodsDataList.Where(x => x.Id == equip.BDQID).FirstOrDefault();
                /// Nếu vật phẩm tương ứng khong tồn tại trong túi người chơi
                if (CN1 == null || CN2 == null || CN3 == null || BDQ == null)
                {
                    PlayerManager.ShowNotification(client, "Vị trí trang bị không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                var crystalStones = new List<GoodsData>();
                crystalStones.Add(CN1);
                crystalStones.Add(CN2);
                crystalStones.Add(CN3);

                int ret = ChanNguyenManager.CreateChanNguyenAnhHung(client, crystalStones, BDQ);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_HERO_CHANNGUYEN);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// nhan goi tin mo khoa CN
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
        public static TCPProcessCmdResults ResponseCNUnBinding(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            G2C_CNUnBinding equip = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equip = DataHelper.BytesToObject<G2C_CNUnBinding>(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Error while getting DATA, CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                /// Nếu dữ liệu trong gói tin không tồn tại
                if (equip == null)
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

                /// Nếu không có danh sách Huyền Tinh
                if (equip.ChanNguyenDbIDs == null || equip.ChanNguyenDbIDs.Count <= 0)
                {
                    PlayerManager.ShowNotification(client, "Không có kinh nghiệm chân nguyên, không thể cường hóa trang bị!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> cnList = client.GoodsDataList.Where(x => equip.ChanNguyenDbIDs.Contains(x.Id)).ToList();
                /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                if (cnList.Count != equip.ChanNguyenDbIDs.Count)
                {
                    PlayerManager.ShowNotification(client, "Danh sách kinh nghiệm chân nguyên không hợp lệ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                int ret = ChanNguyenManager.UnBindingChanNguyen(client, cnList);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_UNBINDING_CHANNGUYEN);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Nhan goi tin tay luyen chan nguyen
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
        public static TCPProcessCmdResults ResponseCNReset(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            EquipEnhance equipEnhance = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equipEnhance = DataHelper.BytesToObject<EquipEnhance>(data, 0, count);
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

                /// Vật phẩm tương ứng trong túi người chơi
                GoodsData itemGD = client.GoodsDataList.Where(x => x.Id == equipEnhance.EquipDbID).FirstOrDefault();
                /// Nếu vật phẩm tương ứng khong tồn tại trong túi người chơi
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(client, "Vị trí trang bị không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Nếu không có danh sách Huyền Tinh
                if (equipEnhance.CrystalStonesDbID == null || equipEnhance.CrystalStonesDbID.Count <= 0)
                {
                    PlayerManager.ShowNotification(client, "Không có kinh nghiệm chân nguyên, không thể cường hóa trang bị!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                /// Danh sách Huyền Tinh trong túi
                List<GoodsData> crystalStones = client.GoodsDataList.Where(x => equipEnhance.CrystalStonesDbID.Contains(x.Id)).ToList();
                /// Nếu độ dài truyền lên và danh sách tìm được không khớp
                if (crystalStones.Count != equipEnhance.CrystalStonesDbID.Count)
                {
                    PlayerManager.ShowNotification(client, "Danh sách kinh nghiệm chân nguyên không hợp lệ!");
                    return TCPProcessCmdResults.RESULT_OK;
                }
                string cmdData = "";
                var ret = ChanNguyenManager.ResetChanNguyen(client,  itemGD, crystalStones);
                if (ret != null)
                {
                    cmdData = "1";
                    for (var i = 0; i< ret.Length; i++)
                    {
                        cmdData += $"|{ret[i]}";
                    }    
                }    
                else
                {
                    cmdData = "0";
                }    
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_RESET_CHANNGUYEN);

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }

        /// <summary>
        /// Nhan goi tin xac nhan tay luyen chan nguyen
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
        public static TCPProcessCmdResults ResponseCNResetComplete(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            EquipEnhance equipEnhance = null;
            try
            {
                /// Giải mã gói tin đẩy về dạng ProtoBytes
                equipEnhance = DataHelper.BytesToObject<EquipEnhance>(data, 0, count);
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

                /// Vật phẩm tương ứng trong túi người chơi
                GoodsData itemGD = client.GoodsDataList.Where(x => x.Id == equipEnhance.EquipDbID).FirstOrDefault();
                /// Nếu vật phẩm tương ứng khong tồn tại trong túi người chơi
                if (itemGD == null)
                {
                    PlayerManager.ShowNotification(client, "Vị trí trang bị không tồn tại!");
                    return TCPProcessCmdResults.RESULT_OK;
                }

                bool isComplete = equipEnhance.MoneyType == 1;

                var ret = ChanNguyenManager.CompleteResetCN(client, itemGD, isComplete);
                string cmdData = string.Format("{0}", ret);
                GameManager.ClientMgr.SendToClient(client, cmdData, (int)TCPGameServerCmds.CMD_SPR_COMPLETE_RESET_CHANNGUYEN);

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
