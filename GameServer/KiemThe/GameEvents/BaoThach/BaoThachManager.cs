using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// using System.Text.Json;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.BaoThach;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Utilities;
using GameServer.Logic;
using GameServer.Server;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using UnityEngine;
using Random = System.Random;

namespace GameServer.KiemThe.GameEvents.BaoThach
{
    public class BaoThachManager
    {
        public const string ConfigPath = "Config/KT_Interactive/BaoThach.xml";
        public const string ConfigEquipPath = "Config/KT_Interactive/BaoThachEquip.xml";

        private static BaoThachConfig config;
        private static EquipBaoThachConfig equipConfig;

        public static void Init()
        {
            config = (BaoThachConfig)LoadConfig<BaoThachConfig>(ConfigPath);
            equipConfig = (EquipBaoThachConfig)LoadConfig<EquipBaoThachConfig>(ConfigEquipPath);
        }

        public static int GetPoint(int itemvalue, int level)
        {
            return equipConfig.GetPoint(itemvalue, level);
        }

        public static T LoadConfig<T>(string path)
        {
            var configFilePath = Global.GameResPath(path);
            if (!File.Exists(configFilePath))
            {
                return default(T);
            }

            var xmlcontent = File.ReadAllText(configFilePath);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(xmlcontent))
            {
                return (T)serializer.Deserialize(reader);
            }

            return default(T);
        }

        public static bool CheckMaterialsRequire(KPlayer player, string materials)
        {
            var materialarray = materials.Split(',');
            string[] cache = null;
            List<(int, int)> requires = new List<(int, int)>();
            foreach (var material in materialarray)
            {
                cache = material.Split(':');
                (int, int) cc;
                cc.Item1 = int.Parse(cache[0]);
                cc.Item2 = int.Parse(cache[1]);
                if (ItemManager.GetItemCountInBag(player, cc.Item1) < cc.Item2)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ConsumeMaterialRequire(KPlayer player, string materials, string from = "")
        {
            var materialarray = materials.Split(',');
            string[] cache = null;
            List<(int, int)> requires = new List<(int, int)>();
            foreach (var material in materialarray)
            {
                cache = material.Split(':');
                (int, int) cc;
                cc.Item1 = int.Parse(cache[0]);
                cc.Item2 = int.Parse(cache[1]);
                ItemManager.RemoveItemFromBag(player, cc.Item1, cc.Item2, -1, from);
            }
        }

        public static void ConsumeMaterialBinding(KPlayer player, string materials, string from, ref bool isLock)
        {
            var materialarray = materials.Split(',');
            string[] cache = null;
            foreach (var material in materialarray)
            {
                cache = material.Split(':');
                int itemID = int.Parse(cache[0]);
                int count = int.Parse(cache[1]);
                LogManager.WriteLog(LogTypes.Item, $"[{player.strUserID}][{player.RoleID}][{player.RoleName}][{from}] Bắt đầu xoá vật phẩm: {itemID}, số lượng: {count}" );
                for (int i = 0; i < count; i++)
                {
                    var item = ItemManager.GetFirstItemInBag(player, itemID);
                    if (item.Binding == 1)
                        isLock = true;
                    ItemManager.RemoveItem(player, item.Id);
                }
            }
        }

        public static bool CheckMoneyRequire(KPlayer player, string moneyString)
        {
            var moneyArray = moneyString.Split(',');
            foreach (var VARIABLE in moneyArray)
            {
                var moneystring = VARIABLE.Split(':');
                var moneyType = (MoneyType)int.Parse(moneystring[0]);
                var moneyNumber = int.Parse(moneystring[1]);
                if (!KTGlobal.IsHaveMoney(player, moneyNumber, moneyType))
                {
                    return false;
                }
            }

            return true;
        }

        public static void ConsumeMoneyRequire(KPlayer player, string moneyString, string from = "")
        {
            var moneyArray = moneyString.Split(',');
            foreach (var VARIABLE in moneyArray)
            {
                var moneystring = VARIABLE.Split(':');
                var moneyType = (MoneyType)int.Parse(moneystring[0]);
                var moneyNumber = int.Parse(moneystring[1]);
                KTGlobal.SubMoney(player, moneyNumber, moneyType, from);
            }
        }

        public static int CreateItemHole(KPlayer player, int gid, string from = "Create_Hole_Baothach")
        {
            /// Nếu vật phẩm không tồn tại
            var goods_item = Global.GetGoodsByDbID(player, gid);

            // nếu goods id ko tồn tại
            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Thất bại", "Vật phẩm không tồn tại!");
                return 1; // không tồn tại item trong kho đồ
            }

            if (!ItemManager._TotalGameItem.TryGetValue(goods_item.GoodsID, out ItemData itemData))
            {
                PlayerManager.ShowMessageBox(player, "Thất bại", "Vật phẩm không tồn tại!");
                return 1;
            }


            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị đang được sử dụng, không thể đục lỗ.");
                    return 1;
                }
            }

            /// Nếu không phải là trang bị
            if (!ItemManager.KD_ISEQUIP(itemData.Genre) || itemData.DetailType > 11)
            {
                PlayerManager.ShowMessageBox(player, "Thất bại", "Vật phẩm không hỗ trợ đục lỗ!");
                return 1; // không phai trang bị
            }

            int holeIndex = 0;
            if (!ItemManager.CreateHoleData(player, goods_item, out BaoThachUserData data))
            {
                return 1;
            }

            holeIndex = data.FindNonHole();

            // check nguyên liệu và tiền
            // ...
            if (from != "GMCOMMAND")
                if (!CheckMaterialsRequire(player, config.HoleConfig[holeIndex].materials))
                {
                    PlayerManager.ShowMessageBox(player, "Thất bại", "Không đủ nguyên liệu cho việc đục lỗ!");
                    return 1; // không đủ nguyên liệu
                }

            if (from != "GMCOMMAND")
                if (!CheckMoneyRequire(player, config.HoleConfig[holeIndex].money))
                {
                    PlayerManager.ShowMessageBox(player, "Thất bại", "Không đủ tài nguyên cho việc khoan lỗ!");
                    return 1; // không đủ tiền
                }

            // trừ nguyên liệu và tiền
            // ...

            if (from != "GMCOMMAND") ConsumeMaterialRequire(player, config.HoleConfig[holeIndex].materials, from);
            if (from != "GMCOMMAND") ConsumeMoneyRequire(player, config.HoleConfig[holeIndex].money, from);

            // Tính % xắc suất
            var random = new Random();
            var next = random.Next(0, 100);
            if (next <= config.HoleConfig.Find(x => x.id == holeIndex).ratio)
            {
                ItemManager.CreateHole(player, gid, from);
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Item[{itemData.Name}:{itemData.ItemID}] Đục lỗ [{holeIndex}] thành công");
                return 0;
            }

            PlayerManager.ShowMessageBox(player, "Thất bại", "Đục lỗ thất bại!");
            LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Item[{itemData.Name}:{itemData.ItemID}] Đục lỗ [{holeIndex}] thất bại do tỷ lệ %");
            return 1; ///dđục lỗ thất bai
        }

        public static int Equip(KPlayer player, int item_dbid, int goodsid_baothach, int indexHole, int amount,
            string from)
        {
            var goods_item = Global.GetGoodsByDbID(player, item_dbid);
            var goods_bt = Global.GetGoodsByDbID(player, goodsid_baothach);
            var holeData = config.HoleConfig.Find(x => x.id == indexHole);
            bool isGM = from == "GMCOMMND";

            #region kiểm tra tương thích vật phẩm

            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm trang bị không tồn tại");
                return 1;
            }

            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi",
                        "Trang bị đang được sử dụng, không thể khảm bảo thạch.");
                    return 1;
                }
            }

            if (goods_bt == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm bảo thạch không tồn tại");
                return 1;
            }

            var itemData = ItemManager.GetItemTemplate(goods_item.GoodsID);
            if (itemData == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm trang bị không tồn tại");
                return 1;
            }

            var baothach = ItemManager.GetItemTemplate(goods_bt.GoodsID);
            if (baothach == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm bảo thạch không tồn tại");
                return 1;
            }

            if (goods_item.OtherParams == null || goods_item.OtherParams.Count == 0 ||
                goods_item.OtherParams[ItemPramenter.Hole] == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị không có lỗ khảm phù hợp");
                return 1;
            }

            var userdata = BaoThachUserData.GetFromStringData(goods_item.OtherParams[ItemPramenter.Hole]);

            if (userdata.GetHoleStatus(indexHole) == -1)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa được đục lỗ!");
                return 1;
            }

            if (userdata.GetHoleStatus(indexHole) == 1)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vui lòng tháo bảo thạch trước khi khảm!");
                return 1;
            }

            if (!holeData.IsValid(goods_bt.GoodsID))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Bảo thạch không phù hợp với lỗ khảm!");
                return 1;
            }

            #endregion


            var props = equipConfig.GetProps(baothach.ItemValue, indexHole + 1, baothach.Level, itemData.DetailType);
            Random r = new Random();
            int index = r.Next(0, props.Length);
            var selected = props[index];

            // xoá item bảo thạch khỏi túi
            if (!isGM) ConsumeMaterialRequire(player, $"{goods_bt.GoodsID}:1", from);
            userdata.Equip(indexHole, goods_bt.GoodsID, selected.alias, selected.score, selected.point, selected.type);

            //update data
            if (!ItemManager.UpdateOtherParameter(player, ItemPramenter.Hole, userdata.ToStringData(), goods_item,
                    from))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Khảm bảo thạch thất bại");
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Item[{itemData.Name}:{itemData.ItemID}] Khảm lỗ [{indexHole}] thất  bại do update Other Parameter thất bại");
                return 1;
            }

            BaoThachUserData.Hole hole =
                new BaoThachUserData.Hole(goods_bt.GoodsID, selected.alias, selected.score, selected.type);
            var serializer = new XmlSerializer(typeof(BaoThachUserData.Hole));
            string result;
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, hole);
                result = stringWriter.ToString();
            }

            var bytes = DataHelper.ObjectToBytes(result);
            GameManager.ClientMgr.SendToClient(player, bytes, (int)TCPGameServerCmds.CMD_BT_EQUIP);
            PlayerManager.ShowMessageBox(player, "Thành công", "Khảm bảo thạch thành công");
            LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Item[{itemData.Name}:{itemData.ItemID}] Khảm lỗ [{indexHole}] thành công. Bảo thạch: [{goods_bt.ItemName}:{goods_bt.GoodsID}]");
            return 0;
        }

        public static int Upgrade(KPlayer player, int goodsid_bt, int amount, string from)
        {
            #region checking

            if (ItemManager.GetItemCountInBag(player, goodsid_bt) < amount)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Số lượng bảo thạch không đủ để hợp thành");
                return 1;
            }

            var itemdata = ItemManager.GetItemTemplate(goodsid_bt);
            if (itemdata.Genre != 26)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm không hỗ trợ hợp thành");
                return 1;
            }

            var item = config.upgrades.Find(x => x.id == goodsid_bt);
            if (item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm không hỗ trợ hợp thành");
                return 1;
            }

            var pack = item.packs.Find(x => x.amount == amount);
            if (pack == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không tìm thấy thông tin hợp thành!");
                return 1;
            }

            if (!CheckMaterialsRequire(player, $"{goodsid_bt}:{amount}"))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ bảo thạch để hợp thành!");
                return 1;
            }

            if (from != "GMCOMMAND")
                if (!CheckMaterialsRequire(player, pack.materials))
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ nguyên liệu để hợp thành!");
                    return 1;
                }

            if (from != "GMCOMMAND")
                if (!CheckMoneyRequire(player, pack.money))
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ chi phí cho việc hợp thành!");
                    return 1;
                }

            #endregion


            bool islock = false;
            // tru item trong bag
            if (from != "GMCOMMAND") ConsumeMaterialBinding(player, pack.materials, from, ref islock);
            if (from != "GMCOMMAND") ConsumeMoneyRequire(player, pack.money, from);
            //ItemManager.RemoveItemFromBag(player, goodsid_bt, amount, -1, from);
            ConsumeMaterialBinding(player, $"{goodsid_bt}:{amount}", from, ref islock);

            // tien hanh hop thanh

            Random random = new Random();
            var input = random.Next(1, 100);
            string[] output = null;
            
            if (float.Parse(pack.rate) < input)
            {
                output = pack.fail.Split(':');
                if (itemdata.Level >= 3)
                {
                    KTGlobal.SendSystemChat( $"{player.RoleName} dốc hết vốn liếng mang {amount} viên {itemdata.Name} đi nâng cấp, nhưng vận rủi đeo bám, thất bại thê thảm, hai mắt ngấn lệ quay về chờ ngày Đông Sơn tái khởi.");
                }
                PlayerManager.ShowMessageBox(player, "Thất bại", "Hợp thành thất bại");
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Hợp thành thất bại do tỷ lệ % ko đạt {amount} viên [{itemdata.Name}:{itemdata.ItemID}]");
            }
            else
            {
                output = pack.success.Split(':');
                PlayerManager.ShowMessageBox(player, "Thành công", "Hợp thành thành công");
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Hợp thành thành công bằng {amount} viên [{itemdata.Name}:{itemdata.ItemID}]");
                if (itemdata.Level >= 3)
                {
                    int id = int.Parse(output[0]);
                    var outItem = ItemManager.GetItemTemplate(id);
                    KTGlobal.SendSystemChat( $"{player.RoleName} dốc hết vốn liếng mang {amount} viên {itemdata.Name} đến Bảo Thạch Đại Sư, thành công nâng cấp lên {outItem.Name}. Thật đáng khâm phục!");
                }
            }

            if (output.Length == 2)
            {
                int id = int.Parse(output[0]);
                int count = int.Parse(output[1]);
                if (id == 14241) // là mảnh vỡ thì auto lock
                {
                    islock = true;
                }
                if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, id, count, 0, from,
                        true, islock?1:0,
                        false, Global.ConstGoodsEndTime))
                {
                    LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Hợp thành thất bại do khi thêm vật phẩm vào túi đồ! {amount} viên [{itemdata.Name}:{itemdata.ItemID}]");
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Có lỗi khi thêm vật phẩm");
                    return 1;
                }
            }

            return 0;
        }

        public static int UnEquip(KPlayer player, int item_dbid, int indexHole, string from)
        {
            #region checking

            var goods_item = Global.GetGoodsByDbID(player, item_dbid);
            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị không tồn tại");
                return 1;
            }

            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị đang được sử dụng, không thể tháo bảo thạch");
                    return 1;
                }
            }

            if (goods_item.OtherParams == null || !goods_item.OtherParams.ContainsKey(ItemPramenter.Hole))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa được khảm!");
                return 1;
            }

            var holestring = goods_item.OtherParams[ItemPramenter.Hole];
            if (string.IsNullOrEmpty(holestring))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa được đục lỗ bảo thạch!");
                return 1;
            }

            BaoThachUserData holedata = BaoThachUserData.GetFromStringData(holestring);

            if (holedata.holes.Count <= indexHole)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Có lỗi phát sinh, vui lòng liên hệ Admin.\n Mã lối: [1]");
                return 1;
            }

            if (holedata.holes[indexHole].itemGoodsId == 0)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa được khảm bảo thạch");
                return 1;
            }

            GoodsData goods_bt = null;
            BaoThachConfig.UnequipBaoThach.ItemRequire require = null;

            if (from != "GMCOMMAND")
            {
                var data = config.unequips.Find(x => x.id == indexHole);
                require = data.requires[0];
                goods_bt =
                    Global.GetGoodsByID(player, require.ItemID); // lay bao thach trich tu phu cho viec thao go
                if (goods_bt == null)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi",
                        $"Không đủ {ItemManager.GetItemTemplate(require.ItemID).Name} để tháo bảo thạch!");
                    return 1;
                }
            }

            #endregion

            if (from != "GMCOMMAND")
                if (!CheckMoneyRequire(player, config.unequips[indexHole].money))
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ bạc khoá cho việc tháo bảo thạch");
                    return 1;
                }

            if (from != "GMCOMMAND") ConsumeMoneyRequire(player, config.unequips[indexHole].money);

            // tru bao thach khoi kho do
            if (from != "GMCOMMAND") ItemManager.RemoveItemFromBag(player, goods_bt.GoodsID, 1);

            //tinh % thanh cong
            if (from != "GMCOMMAND")
            {
                var random = new Random();
                int percentage = random.Next(0, 100);
                if (percentage < require.rate)
                {
                    PlayerManager.ShowMessageBox(player, "Thông báo", "Tháo bảo thạch khỏi trang bị thất bại!");
                    return 1;
                }
            }

            /// tra lai bao thach
            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player,
                    holedata.holes[indexHole].itemGoodsId,
                    1, 0, from, false, 1, false, Global.ConstGoodsEndTime, "", -1, "", 0, 1, false))
            {
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Gỡ bảo thạch thành công, nhưng không nhận lại được bảo thạch! Bảo thạch [{holedata.holes[indexHole].itemGoodsId}]");
                return 1;
            }
            // Tháo bảo thạch khỏi trang bị

            LogManager.WriteLog(LogTypes.BaoThach,
                $"[{player.ServerId}][{player.RoleID}][{from}] Gỡ bảo thạch thành công");
            return ItemManager.UnequipBaoThach(player, item_dbid, indexHole, from);
        }

        public static int ResetAndConfirm(KPlayer player, int item_dbid, int indexHole, string from)
        {
            var goods_item = Global.GetGoodsByDbID(player, item_dbid);
            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị không tồn tại");
                return 1;
            }

            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị đang được sử dụng, không thể tẩy luyện");
                    return 1;
                }
            }

            if (goods_item.OtherParams == null || goods_item.OtherParams.Count == 0 ||
                !goods_item.OtherParams.ContainsKey(ItemPramenter.Hole))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa đựợc khảm!");
                return 1;
            }

            var itemData = ItemManager.GetItemTemplate(goods_item.GoodsID);
            var strindata = goods_item.OtherParams[ItemPramenter.Hole];
            var userdata = BaoThachUserData.GetFromStringData(strindata);
            if (userdata.holes.Count < indexHole)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không tôn tại bảo thạch có thể tẩy luyện");
                return 1;
            }


            if (userdata.GetHoleStatus(indexHole) == -1)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa đục lỗ này!");
                return 1;
            }

            if (userdata.GetHoleStatus(indexHole) == 0)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa khảm tại lỗ này!");
                return 1;
            }

            var configc = config.resets.Find(x => x.ItemIds.Contains(userdata.holes[indexHole].itemGoodsId.ToString()));
            if (configc == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Bảo thạch đang khảm không thể tẩy luyện.");
                return 1;
            }

            if (from != "GMCOMMAND")
                if (!CheckMaterialsRequire(player, configc.materials))
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ nguyên liệu để tẩy luyện!");
                    return 1;
                }

            var baothach = ItemManager.GetItemTemplate(userdata.holes[indexHole].itemGoodsId);
            if (baothach == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", " Vật phẩm bảo thạch không còn tồn tại");
                return 1;
            }

            if (from != "GMCOMMAND") ConsumeMaterialRequire(player, configc.materials);

            // trừ chỉ số vào property
            //KTAttributesModifier.AttachProperty(CreateMagicAttribute(userdata.holes[indexHole].magicName, userdata.holes[indexHole].magicValue), player, true);
            //

            Random random = new Random();

            var props = equipConfig.GetProps(baothach.ItemValue, indexHole + 1, baothach.Level, itemData.DetailType);
            EquipProp selected = null;
            Random r = new Random();
            do
            {
                int index = r.Next(0, props.Length);
                selected = props[index];
            } while (selected.alias == userdata.holes[indexHole].magicName);

            userdata.holes[indexHole].magicName = selected.alias;
            userdata.holes[indexHole].type = selected.type;
            userdata.holes[indexHole].magicValue = selected.score;
            if (!ItemManager.UpdateOtherParameter(player, ItemPramenter.Hole, userdata.ToStringData(), goods_item,
                    from))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Tẩy luyện thất bại [1]");
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Tẩy luyện thất bại do UpdateOtherParameter thất bại! [{goods_item.GoodsID}:{indexHole}]");
                return 1;
            }

            PlayerManager.ShowMessageBox(player, "Kết quả", "Tẩy luyện thành công");
            LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Tẩy luyện thành công! [{goods_item.GoodsID}:{indexHole}]");
            return 0;
        }


        public static int Reset(KPlayer player, int item_dbid, int indexHole, string from)
        {
            var goods_item = Global.GetGoodsByDbID(player, item_dbid);
            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị không tồn tại");
                return 1;
            }

            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị đang được sử dụng, không thể tẩy luyện");
                    return 1;
                }
            }

            if (goods_item.OtherParams == null || goods_item.OtherParams.Count == 0 ||
                !goods_item.OtherParams.ContainsKey(ItemPramenter.Hole))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa đựợc khảm!");
                return 1;
            }

            var itemData = ItemManager.GetItemTemplate(goods_item.GoodsID);
            var strindata = goods_item.OtherParams[ItemPramenter.Hole];
            var userdata = BaoThachUserData.GetFromStringData(strindata);
            if (userdata.holes.Count < indexHole)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không tôn tại bảo thạch có thể tẩy luyện");
                return 1;
            }


            if (userdata.GetHoleStatus(indexHole) == -1)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa đục lỗ này!");

                return 1;
            }

            if (userdata.GetHoleStatus(indexHole) == 0)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa khảm tại lỗ này!");

                return 1;
            }

            var configc = config.resets.Find(x => x.ItemIds.Contains(userdata.holes[indexHole].itemGoodsId.ToString()));
            if (configc == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Bảo thạch đang khảm không thể tẩy luyện.");

                return 1;
            }

            if (from != "GMCOMMAND")
                if (!CheckMaterialsRequire(player, configc.materials))
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Không đủ nguyên liệu để tẩy luyện!");

                    return 1;
                }

            var baothach = ItemManager.GetItemTemplate(userdata.holes[indexHole].itemGoodsId);
            if (baothach == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", " Vật phẩm bảo thạch không còn tồn tại");

                return 1;
            }

            if (from != "GMCOMMAND") ConsumeMaterialRequire(player, configc.materials);

            // trừ chỉ số vào property
            //KTAttributesModifier.AttachProperty(CreateMagicAttribute(userdata.holes[indexHole].magicName, userdata.holes[indexHole].magicValue), player, true);
            //

            Random random = new Random();

            var props = equipConfig.GetProps(baothach.ItemValue, indexHole + 1, baothach.Level, itemData.DetailType);
            EquipProp selected = null;
            Random r = new Random();
            do
            {
                int index = r.Next(0, props.Length);
                selected = props[index];
            } while (selected.alias == userdata.holes[indexHole].magicName);

            // tim dược rồi thì thêm vao cache
            BaoThachUserData.Hole hole = new BaoThachUserData.Hole()
            {
                itemGoodsId = baothach.ItemID,
                magicName = selected.alias,
                magicValue = selected.score,
                type = selected.type
            };
            player.AddResetQueue(item_dbid, indexHole, hole);
            var serializer = new XmlSerializer(typeof(BaoThachUserData.Hole));
            string result;
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, hole);
                result = stringWriter.ToString();
            }

            var bytes = DataHelper.ObjectToBytes(result);
            GameManager.ClientMgr.SendToClient(player, bytes, (int)TCPGameServerCmds.CMD_BT_RESET);
            LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Gửi yêu cầu tẩy luyện: [{goods_item.GoodsID}:{indexHole}:{result}]");
            return 0;
        }

        public static int ConfirmReset(KPlayer player, int item_dbid, int indexHole, string from)
        {
            var goods_item = Global.GetGoodsByDbID(player, item_dbid);
            if (goods_item == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị không tồn tại");
                return 1;
            }

            foreach (var e in player.GetPlayEquipBody().EquipBody.Values)
            {
                if (e._GoodDatas.Id == goods_item.Id)
                {
                    PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị đang được sử dụng, không thể tẩy luyện");
                    return 1;
                }
            }

            if (goods_item.OtherParams == null || goods_item.OtherParams.Count == 0 ||
                !goods_item.OtherParams.ContainsKey(ItemPramenter.Hole))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Trang bị chưa đựợc khảm!");
                return 1;
            }

            var stringdata = goods_item.OtherParams[ItemPramenter.Hole];
            var userdata = BaoThachUserData.GetFromStringData(stringdata);
            if (userdata.holes.Count < indexHole)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không tôn tại bảo thạch có thể tẩy luyện");
                return 1;
            }


            if (userdata.GetHoleStatus(indexHole) == -1)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa đục lỗ này!");
                return 1;
            }

            if (userdata.GetHoleStatus(indexHole) == 0)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Vật phẩm chưa khảm tại lỗ này!");
                return 1;
            }

            var configc = config.resets.Find(x => x.ItemIds.Contains(userdata.holes[indexHole].itemGoodsId.ToString()));
            if (configc == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Bảo thạch đang khảm không thể tẩy luyện.");
                return 1;
            }

            var selected = player.GetResetQueue(item_dbid, indexHole);
            if (selected == null)
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Không có thông tin tẩy luyện!");
                return 1;
            }

            userdata.holes[indexHole] = selected;
            if (!ItemManager.UpdateOtherParameter(player, ItemPramenter.Hole, userdata.ToStringData(), goods_item,
                    from))
            {
                PlayerManager.ShowMessageBox(player, "Lỗi", "Tẩy luyện thất bại [1]");
                LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Tẩy luyện thất bại do UpdateOtherParameter lỗi! [{goods_item.GoodsID}:{indexHole}:{selected}]");
                return 1;
            }
            
            LogManager.WriteLog(LogTypes.BaoThach, $"[{player.ServerId}][{player.RoleID}][{from}] Tẩy luyện thành công! [{goods_item.GoodsID}:{indexHole}:{selected}]");
            return 0;
        }

        public static KMagicAttrib CreateMagicAttribute(string magicName, int magicValue)
        {
            KMagicAttrib magicAttrib = new KMagicAttrib();
            if (magicName.StartsWith("magic_"))
            {
                magicName = magicName.Remove(0, "magic_".Length);
            }

            if (magicName == "addpoisondamage_v" || magicName == "addpoisonmagic_v")
            {
                magicAttrib.Init(magicName, magicValue, 5*18, 0); // 18 frame/s
                magicAttrib.nType = new[] { 1, 1, 1 };
            }
            else if (magicName == "life_mana_replenish_p" || magicName == "life_mana_max_p" || magicName == "melee_range_damagereturn_p")
            {
                magicAttrib.Init(magicName, magicValue, magicValue, 0);
                magicAttrib.nType = new[] { 1, 1, 1 };
            }
            else
            {
                magicAttrib.Init(magicName, magicValue, 0, 0);
                magicAttrib.nType = new[] { 1, 1, 1 };
            }

            return magicAttrib;
        }
    }

    [XmlRoot("BaoThach")]
    public class BaoThachConfig
    {
        [XmlArray("Holes")] [XmlArrayItem("Hole")]
        public List<Holes> HoleConfig;

        [XmlArray("Upgrade")] [XmlArrayItem("Item")]
        public List<Item> upgrades;

        [XmlArray("UnEquip")] [XmlArrayItem("Hole")]
        public List<UnequipBaoThach> unequips;

        [XmlArray("Reset")] [XmlArrayItem("Item")]
        public List<ResetBaoThach> resets;

        public class Holes
        {
            [XmlAttribute("ID")] public int id;
            [XmlAttribute("materials")] public string materials;
            [XmlAttribute("money")] public string money;
            [XmlAttribute("rate")] public int ratio;
            [XmlAttribute("ItemIDs")] public string idItem;

            public bool IsValid(int idItem)
            {
                return this.idItem.Split(',').Contains(idItem.ToString());
            }
        }

        public class Item
        {
            [XmlAttribute("id")] public int id;
            [XmlElement("Pack")] public List<Pack> packs;

            public class Pack
            {
                [XmlAttribute("amount")] public int amount;
                [XmlAttribute("materials")] public string materials;
                [XmlAttribute("money")] public string money;
                [XmlAttribute("rate")] public string rate;
                [XmlAttribute("fail")] public string fail;
                [XmlAttribute("success")] public string success;
            }
        }

        public class UnequipBaoThach
        {
            [XmlAttribute("ID")] public int id;
            [XmlAttribute("money")] public string money;
            [XmlElement("Item")] public List<ItemRequire> requires;

            public class ItemRequire
            {
                [XmlAttribute("ItemID")] public int ItemID;
                [XmlAttribute("rate")] public int rate;
            }
        }

        public class ResetBaoThach
        {
            [XmlAttribute("ItemIds")] public string ItemIds;
            [XmlAttribute("materials")] public string materials;
        }
    }

    [System.Serializable]
    [XmlRoot("BaoThachUserData")]
    public class BaoThachUserData
    {
        [XmlAttribute("Point")] public int point; // điểm tài phú bảo thạch
        [XmlElement("Hole")] public List<Hole> holes = new List<Hole>();

        public void CreateHole()
        {
            holes.Add(new Hole());
        }

        public int FindNonHole()
        {
            return holes.Count;
        }

        public void Equip(int index, int baothach_goodsid, string magicName, int value, int taiphu, int type)
        {
            holes[index] = new Hole(baothach_goodsid, magicName, value, type);
            this.point += taiphu;
        }

        /// <summary>
        /// Trả về trạng thái của lỗ khảm
        /// </summary>
        /// <param name="???"></param>
        /// <returns>
        /// -1:chưa đục; 0 đang trống; 1 đã khảm
        /// </returns>
        public int GetHoleStatus(int indexHole)
        {
            var data = holes[indexHole];
            if (data == null) return -1;
            if (data.itemGoodsId == 0) return 0;
            return 1;
        }

        public static BaoThachUserData GetFromStringData(string stringdata)
        {
            var detectType = FormatChecker.DetectFormat(stringdata);
            switch (detectType)
            {
                case FormatChecker.FormatType.XML:
                    XmlSerializer serializer = new XmlSerializer(typeof(BaoThachUserData));
                    BaoThachUserData result = null;
                    using (TextReader reader = new StringReader(stringdata))
                    {
                        result = (BaoThachUserData)serializer.Deserialize(reader);
                    }

                    return result;
                case FormatChecker.FormatType.JSON:
                    return JsonConvert.DeserializeObject<BaoThachUserData>(stringdata);
                default:
                    return null;
            }
        }

        public string ToStringData()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(BaoThachUserData));
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, this);
                    xml = sww.ToString();
                }
            }

            return xml;
            //return JsonConvert.SerializeObject(this);
        }

        [System.Serializable]
        public class Hole
        {
            [XmlAttribute("BT_GoodsID")] public int itemGoodsId;
            [XmlAttribute("Type")] public int type;
            [XmlAttribute("Key")] public string magicName;
            [XmlAttribute("Value")] public int magicValue;

            public Hole()
            {
            }

            public Hole(int itemGoodsId, string magicName, int magicValue, int type)
            {
                this.itemGoodsId = itemGoodsId;
                this.magicName = magicName;
                this.magicValue = magicValue;
                this.type = type;
            }
        }
    }


    [System.Serializable]
    [XmlRoot("Props")]
    public class EquipBaoThachConfig
    {
        [XmlElement("Prop")] public List<EquipProp> props;

        public EquipProp[] GetProps(int itemvalue, int hole, int level, int detailType)
        {
            var u= props.FindAll(x =>
                x.hole == hole && x.itemValue == itemvalue && x.level == level && CheckCondition(x.condition,detailType)).ToArray();
            return u;
        }

        public int GetPoint(int itemvalue, int level)
        {
            return props.Find(x=> x.itemValue == itemvalue && x.level == level).point;
        }

        private bool CheckCondition(string condition, int detailType)
        {
            var cacheSplit = condition.Split(',');
            return cacheSplit.Contains(detailType.ToString());
        }
    }

    public class EquipProp
    {
        [XmlAttribute("ItemValue")] public int itemValue;
        [XmlAttribute("Hole")] public int hole;
        [XmlAttribute("Level")] public int level;
        [XmlAttribute("Alias")] public string alias;
        [XmlAttribute("Type")] public int type;
        [XmlAttribute("Point")] public int point;
        [XmlAttribute("Score")] public int score;
        [XmlAttribute("Condition")] public string condition;
    }

    public enum BaoThachType
    {
        Magic = 0,
        Level = 1,
        MTLevel = 2,
        EnhArtifact = 3
    }
}

public class FormatChecker
{
    public enum FormatType
    {
        JSON,
        XML,
        Unknown
    }

    public static FormatType DetectFormat(string input)
    {
        input = input.Trim();

        // Try JSON
        if ((input.StartsWith("{") && input.EndsWith("}")) || (input.StartsWith("[") && input.EndsWith("]")))
        {
            try
            {
                JToken.Parse(input);
                // JsonDocument.Parse(input);
                return FormatType.JSON;
            }
            catch
            {
            }
        }

        // Try XML
        if (input.StartsWith("<") && input.EndsWith(">"))
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(input);
                return FormatType.XML;
            }
            catch
            {
            }
        }

        return FormatType.Unknown;
    }
}