using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication.ExtendedProtection.Configuration;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Data;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.GameEvents.MonthlyEvents;
using GameServer.Logic;
using GameServer.Server;
using Newtonsoft.Json;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using Recipe = GameServer.Data.Recipe;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static Dictionary<int, Recipe> ComposeDictionary;

        public static void SetupCompose()
        {
            ComposeDictionary = new Dictionary<int, Recipe>();

            var listJSON = Directory.GetFiles("Config/Config/KT_Item/Compose", "compose*.json", SearchOption.AllDirectories);
            foreach (var path in listJSON)
            {
                var jsonString = File.ReadAllText(path);
                var composeData = JsonConvert.DeserializeObject<ComposeData>(jsonString);
                foreach (var recipe in composeData.recipes)
                {
                    ComposeDictionary[recipe.compose_id] = recipe;
                }
            }   
        }

        private static void FireComposeEvent(KPlayer client, int itemId, int multiple)
        {
            GlobalEventSource.getInstance().fireEvent(new EventComposeEventObject(client, itemId, multiple));
            // client.SendPacket((int)TCPGameServerCmds.CMD_KT_COMPOSE_ITEMS, 1);
        }

        private static TCPProcessCmdResults MakeResponseCompose(KPlayer player, int result, string message)
        {
            if (result == 0)
                PlayerManager.ShowMessageBox(player, "Ghép vật phẩm", message);
            player.SendPacket((int)TCPGameServerCmds.CMD_KT_COMPOSE_ITEMS, result);
            return TCPProcessCmdResults.RESULT_DATA;
        }

        public static TCPProcessCmdResults ResponseComposeItems(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;

            C2G_ComposeDialog composeInfo = null;
            try
            {
                composeInfo = DataHelper.BytesToObject<C2G_ComposeDialog>(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error,
                    $"Error while getting DATA, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                return TCPProcessCmdResults.RESULT_FAILED;
            }
            
            // nếu công thức không tồn tại
            if (composeInfo == null || !ComposeDictionary.TryGetValue(composeInfo.composeId, out _))
            {
                try
                {
                    var fields = DataHelper.BytesToObject<List<int>>(data, 0, count);
                    composeInfo = new C2G_ComposeDialog
                    {
                        composeId = fields[0],
                        multiple = fields[1],
                        sourceItemDBId = fields[2],
                        rateUpItemDBId = fields[3],
                        insureItemDBId = fields[4]
                    };
                }
                catch (Exception)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Error while getting DATA, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
            }
            
            


            try
            {
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        $"Không tìm thấy thông tin người chơi, CMD={(TCPGameServerCmds)nID}, Client={Global.GetSocketRemoteEndPoint(socket)}");
                    return TCPProcessCmdResults.RESULT_FAILED;
                }
                
                LogManager.WriteLog(LogTypes.Analysis, $"Getting compose request: client {client.RoleID}: id {composeInfo.composeId}: rate up {composeInfo.rateUpItemDBId}: insure {composeInfo.insureItemDBId}: source {composeInfo.sourceItemDBId}");


                // nếu công thức không tồn tại
                if (!ComposeDictionary.TryGetValue(composeInfo.composeId, out var currentRecipe))
                {
                    return MakeResponseCompose(client, 0, "Công thức không tồn tại.");
                }

                
                if (currentRecipe.limit != null)
                {
                    int amount = client.GetComposeRecordCount(currentRecipe.compose_id, currentRecipe.limit.type);
                    SysConOut.WriteLine("compose record Count: " + amount);
                    if (amount != -1)
                    {
                        if (currentRecipe.limit.number < amount + composeInfo.multiple)
                        {
                            return MakeResponseCompose(client, 0,
                                $"Vật phẩm đã đến giới hạn ghép trang bị {currentRecipe.limit.ToString()}");
                        }
                    }
                }
                
                var isComposeEventRecipe = ComposeEventManager.Instance.IsRecipeInEvent(currentRecipe.compose_id);
                if (isComposeEventRecipe)
                {
                    if (!ComposeEventManager.Instance.CanPlayerParticipate(client, currentRecipe.compose_id, out var message)) 
                    {
                        if (string.IsNullOrEmpty(message))
                        {
                            message = "Bạn không thể tham gia sự kiện này.";
                        }
                        return MakeResponseCompose(client, 0, message);
                    }
                    // check giới hạn số lượt ghép
                    if (ComposeEventManager.Instance.IsRecipeLimitReached(client, currentRecipe.compose_id))
                    {
                        return MakeResponseCompose(client, 0, "Đã đạt giới hạn số lần ghép vật phẩm trong sự kiện.");
                    }
                }

                GoodsData sourceItem = null;
                GoodsData rateUpItem = null;
                GoodsData insureItem = null;

                // kiểm tra vật phẩm gốc
                if (currentRecipe.enable_source)
                {
                    if (composeInfo.sourceItemDBId == -1)
                    { return MakeResponseCompose(client, 0, "Vui lòng chọn vật phẩm theo yêu cầu.");
                    }

                    sourceItem = Global.GetGoodsByDbID(client, composeInfo.sourceItemDBId);
                    if (sourceItem == null)
                    {
                        return MakeResponseCompose(client, 0, "Vật phẩm không tồn tại.");
                    }
                    if (!currentRecipe.source.ids.Contains(sourceItem.GoodsID))
                    {
                        return MakeResponseCompose(client, 0,  "Vật phẩm không hợp lệ.");
                    }

                    if (currentRecipe.source.forge_level != -1 &&
                        sourceItem.Forge_level != currentRecipe.source.forge_level)
                    {
                        return MakeResponseCompose(client, 0,
                            $"Vật phẩm yêu cầu cấp cường hóa +{currentRecipe.source.forge_level}");
                    }

                    if (currentRecipe.source.total_value > 0)
                    {
                        ItemManager.ItemValueCalculation(sourceItem, out var source_value);
                        if (source_value < 10000L * currentRecipe.source.total_value)
                        {
                            return MakeResponseCompose(client, 0,
                                $"Vật phẩm yêu cầu tài phú {currentRecipe.source.total_value}");
                        }
                    }
                }

                // kiểm tra vật phẩm tăng rate
                if (currentRecipe.enable_rate_up && composeInfo.rateUpItemDBId != -1)
                {
                    rateUpItem = Global.GetGoodsByDbID(client, composeInfo.rateUpItemDBId);
                    if (rateUpItem == null)
                    {
                        return MakeResponseCompose(client, 0, "Vật phẩm không tồn tại.");
                    }

                    if (!currentRecipe.rate_up_items.Contains(rateUpItem.GoodsID))
                    {
                        return MakeResponseCompose(client, 0, "Vật phẩm không hợp lệ.");
                    }
                    
                }

                if (currentRecipe.insure_items != null && currentRecipe.insure_items.Any() &&
                    composeInfo.insureItemDBId != -1)
                {
                    insureItem = Global.GetGoodsByDbID(client, composeInfo.insureItemDBId);
                    if (insureItem == null)
                    {
                        return MakeResponseCompose(client, 0, "Vật phẩm không tồn tại.");
                    }

                    if (!currentRecipe.insure_items.Contains(insureItem.GoodsID))
                    {
                        return MakeResponseCompose(client, 0, "Vật phẩm không hợp lệ.");
                    }
                }

                // kiểm tra nguyên liệu
                foreach (var material in currentRecipe.materials)
                {
                    var material_needed = currentRecipe.materials.Sum(x => x.id == material.id ? material.number : 0);
                    var countInBag = ItemManager.GetItemCountInBag(client, material.id);
                    if (countInBag < composeInfo.multiple * material_needed)
                    {
                        var materialGoodTemplate = ItemManager.GetItemTemplate(material.id);
                        return MakeResponseCompose(client, 0, 
                            $"Nguyên liệu {materialGoodTemplate.Name} không đủ.");
                    }
                }

                // kiểm tra phí
                if (currentRecipe.fees != null)
                    foreach (var fee in currentRecipe.fees)
                    {
                        switch (fee.type)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            {
                                var currentMoney = KTGlobal.GetMoneyhave(client, (MoneyType)fee.type);
                                if (currentMoney < composeInfo.multiple * fee.number)
                                {
                                    return MakeResponseCompose(client, 0, 
                                        $"Không đủ tiền");
                                }

                                break;
                            }
                            case 4:
                            {
                                // tinh lực
                                var currentGatherPoint = client.GetGatherPoint();
                                if (currentGatherPoint < composeInfo.multiple * fee.number)
                                {
                                    return MakeResponseCompose(client, 0, 
                                        $"Không đủ tinh lực");
                                }

                                break;
                            }
                            case 5:
                            {
                                // tinh lực
                                var currentMakePoint = client.GetMakePoint();
                                if (currentMakePoint < composeInfo.multiple * fee.number)
                                {
                                    return MakeResponseCompose(client, 0, 
                                        $"Không đủ hoạt lực");
                                }

                                break;
                            }
                        }
                    }
                
                // kiểm tra ô trống
                if (!currentRecipe.output_based_on_input)
                {
                    var spaceNeeded = KTGlobal.GetTotalSpacesNeedToTakeItem(currentRecipe.output.id,
                        currentRecipe.output.number * composeInfo.multiple);

                    if (!KTGlobal.IsHaveSpace(spaceNeeded, client))
                    {
                        return MakeResponseCompose(client, 0, $"Cần có {spaceNeeded} ô trống trong túi đồ để nhận vật phẩm!");
                    }
                }

                if (currentRecipe.fees != null)
                    // tiến hành trừ phí
                    foreach (var fee in currentRecipe.fees)
                    {
                        switch (fee.type)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            {
                                KTGlobal.SubMoney(client, composeInfo.multiple * fee.number, (MoneyType)fee.type,
                                    "ITEM_COMPOSING");
                                break;
                            }
                            case 4:
                            {
                                client.ChangeCurGatherPoint(-composeInfo.multiple * fee.number);
                                break;
                            }
                            case 5:
                            {
                                client.ChangeCurMakePoint(-composeInfo.multiple * fee.number);
                                break;
                            }
                        }
                    }

                var currentRate = currentRecipe.default_rate;
                var displayRate = currentRecipe.default_rate;
                if (currentRecipe.actual_rate > 0)
                {
                    currentRate = currentRecipe.actual_rate;
                }
                if (currentRecipe.enable_rate_up && rateUpItem != null)
                {
                    // TODO: sửa rate
                    var rateUpItemData = ItemManager.GetItemTemplate(rateUpItem.GoodsID);
                    currentRate += rateUpItemData.ItemValue;
                    displayRate += rateUpItemData.ItemValue;

                    // xóa vật phẩm tăng rate
                    ItemManager.RemoveItem(client, rateUpItem.Id);
                }
                
                if (KTGlobal.IsRateReduce(client.RoleID))
                {
                    var listComposePet = new List<int> {
                        100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140
                    };
                    if (listComposePet.Contains(composeInfo.composeId) && currentRate < 90)
                    {
                        currentRate = 0;
                    } 
                }

                var isSucceeded = StaticRandom.PositiveOutcome(currentRate);
                
                var saved_material_by_insure = false;
                // tiến hành trừ nguyên liệu
                foreach (var material in currentRecipe.materials)
                {
                    // nếu là vật phẩm không tiêu hao thì không xóa
                    if (material.non_consumable)
                        continue;
                    
                    if (!isSucceeded)
                        if (insureItem != null && !saved_material_by_insure)
                        {
                            // nếu có vật phẩm bảo hiểm thì nếu ko thành công giữ lại nguyên liệu đầu tiên không xóa
                            saved_material_by_insure = true;
                            continue;
                        }
                    
                    if (!ItemManager.RemoveItemFromBag(client, material.id, composeInfo.multiple * material.number, -1,
                            "ITEM_COMPOSING"))
                    {
                        return MakeResponseCompose(client, 0, $"Đã có lỗi khi tiêu hao nguyên liệu. Vui lòng liên hệ với quản trị viên!");
                    };
                }

                if (currentRecipe.insure_items != null && currentRecipe.insure_items.Any() && insureItem != null)
                {
                    // xóa vật phẩm bảo hiểm
                    ItemManager.RemoveItem(client, insureItem.Id);
                }
                
                if (!isSucceeded)
                {
                    return MakeResponseCompose(client, 0, 
                        $"Thất bại với tỉ lệ {(int)displayRate}%");
                }

                if (!currentRecipe.output_based_on_input)
                {
                    var binding = 1;
                    if (currentRecipe.output.force_binding != -1) 
                        binding = currentRecipe.output.force_binding;

                    var endTime = Global.ConstGoodsEndTime;

                    if (currentRecipe.output.expiry > 0)
                    {
                        var dt = DateTime.Now.AddMinutes(currentRecipe.output.expiry);
                        endTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    // xóa vật phẩm source
                    if (sourceItem != null)
                        ItemManager.RemoveItem(client, sourceItem.Id);
                    
                    // nhận được vật phẩm hoàn toàn mới
                    ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, currentRecipe.output.id,
                        currentRecipe.output.number * composeInfo.multiple, 0, "ITEM_COMPOSING", true, binding, false,
                        endTime, "", -1, "", currentRecipe.output.forge_level);

                    FireComposeEvent(client, currentRecipe.output.id, currentRecipe.output.number * composeInfo.multiple);
                    client.SendPacket((int)TCPGameServerCmds.CMD_KT_COMPOSE_ITEMS, 1);
                }
                else if (sourceItem != null)
                {
                    lock (client.GoodsDataList)
                    {
                        // chỉnh sửa từ sourceItem
                        var updateProperties = new Dictionary<UPDATEITEM, object>
                        {
                            { UPDATEITEM.ROLEID, client.RoleID },
                            { UPDATEITEM.ITEMDBID, sourceItem.Id },
                            { UPDATEITEM.GOODSID, currentRecipe.output.id == 0 ? sourceItem.GoodsID : currentRecipe.output.id },
                            { UPDATEITEM.FORGE_LEVEL, sourceItem.Forge_level + currentRecipe.output.forge_level },
                            { UPDATEITEM.SERIES, (currentRecipe.output.series == -1) ? sourceItem.Series : currentRecipe.output.series }
                        };

                        if (currentRecipe.output.id != 0)
                        {
                            // thay đổi goodsid -> tạo lại props
                            var outputItemData = ItemManager.GetItemTemplate(currentRecipe.output.id);
                            var newProps = ItemManager.GenerateProbs(outputItemData);
                            updateProperties[UPDATEITEM.PROPS] = newProps;
                        }

                        if (ItemManager.UpdateItemPrammenter(updateProperties, sourceItem, client, "ITEM_COMPOSING",
                                true))
                        {
                            var newItemId = currentRecipe.output.id == 0 ? sourceItem.GoodsID : currentRecipe.output.id;
                            FireComposeEvent(client, newItemId, 1);
                            client.SendPacket((int)TCPGameServerCmds.CMD_KT_COMPOSE_ITEMS, 1);
                        }
                    }
                }
                else
                {
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                client.AddComposeRecord(composeInfo.composeId, composeInfo.multiple);
                if (isComposeEventRecipe)
                {
                    // cập nhật record ghép vật phẩm
                    ComposeEventManager.Instance.UpdateComposeRecord(client, currentRecipe.compose_id, composeInfo.multiple);
                }

                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }

            return TCPProcessCmdResults.RESULT_FAILED;
        }
    }
}