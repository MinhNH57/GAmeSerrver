using GameServer.KiemThe.GameEvents.SlotMachine;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Text;
using System.Collections.Generic;
using GameServer.KiemThe.LuaSystem.Logic;
using Server.Data;
using System.Linq;
using GameServer.KiemThe.Core.Item;

namespace GameServer.KiemThe.Logic
{
    public static partial class KT_TCPHandler
    {
        public static TCPProcessCmdResults ResponseCrystalBag(TCPManager tcpMgr, TMSKSocket socket,
            TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count,
            out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = "";
            try
            {
                cmdData = new ASCIIEncoding().GetString(data);
                var client = GameManager.ClientMgr.FindClient(socket);
                if (null == client)
                {
                    LogManager.WriteLog(LogTypes.Error,
                        string.Format("Không tìm thấy thông tin người chơi, CMD={0}, Client={1}",
                            (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                var fields = cmdData.Split(':');
                int param = int.Parse(fields[0]);

                string result = "";
                switch (param)
                {
                    case 1:
                        result = GetCrystal(client);
                        break;
                        
                    case 2:
                        if (fields.Length < 2)
                        {
                            result = "-1";
                            break;
                        }
                        result = WithdrawCrystals(client, fields.Skip(1).ToArray());
                        break;

                    case 3:
                        if (!AddNewCrystalToBag(client, fields.Skip(1).ToArray()))
                        {
                            result = "-1";
                            break;
                        }
                        result = "1";
                        break;
                }

                var bytesData = DataHelper.ObjectToBytes(result);
                GameManager.ClientMgr.SendToClient(client, bytesData, nID);
                return TCPProcessCmdResults.RESULT_OK;
            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
                return TCPProcessCmdResults.RESULT_FAILED;
            }
        }

        public static bool AddNewCrystalToBag(KPlayer player, string[] data)
        {
            var crystalBagDataString = GetCrystal(player);
            var crystalBagDataList = new List<GoodsData>();

            if (!string.IsNullOrEmpty(crystalBagDataString))
            {
                var items = crystalBagDataString.Split(':');
                foreach (var item in items)
                {
                    var itemData = item.Split(';');
                    if (itemData.Length == 3)
                    {
                        int id = int.Parse(itemData[0]);
                        int count = int.Parse(itemData[1]);
                        int binding = int.Parse(itemData[2]);

                        var existingCrystal = crystalBagDataList.FirstOrDefault(x => x.GoodsID == id && x.Binding == binding);
                        if (existingCrystal != null)
                        {
                            existingCrystal.GCount += count;
                        }
                        else
                        {
                            crystalBagDataList.Add(new GoodsData
                            {
                                GoodsID = id,
                                GCount = count,
                                Binding = binding
                            });
                        }
                    }
                }
            }

            var dataIds = data.Select(id => int.Parse(id)).ToList();

            List<GoodsData> crystalStones = player.GoodsDataList.Where(x => dataIds.Contains(x.Id)).ToList();

            foreach (var crystal in crystalStones)
            {
                crystalBagDataList.Add(new GoodsData
                {
                    GoodsID = crystal.GoodsID,
                    GCount = crystal.GCount,
                    Binding = crystal.Binding
                });

                ItemManager.AbadonItem(crystal.Id, player, false, "CrystalBag");
            }

            var groupedCrystals = new List<GoodsData>();
            foreach (var crystal in crystalBagDataList)
            {
                if (crystal.Binding != 0 && crystal.Binding != 1)
                {
                    continue;
                }

                var existingCrystal = groupedCrystals.FirstOrDefault(x => x.GoodsID == crystal.GoodsID && x.Binding == crystal.Binding);

                if (existingCrystal != null)
                {
                    existingCrystal.GCount += crystal.GCount;
                }
                else
                {
                    groupedCrystals.Add(new GoodsData
                    {
                        GoodsID = crystal.GoodsID,
                        Binding = crystal.Binding,
                        GCount = crystal.GCount
                    });
                }
            }

            groupedCrystals.Sort((a, b) => 
            {
                var idCompare = a.GoodsID.CompareTo(b.GoodsID);
                if (idCompare != 0) 
                    return idCompare;
                return a.Binding.CompareTo(b.Binding);
            });

            var updatedData = string.Join(":", groupedCrystals.Select(x => $"{x.GoodsID};{x.GCount};{x.Binding}"));

            StoreCrystal(player, updatedData);

            return true;
        }

        private static string WithdrawCrystals(KPlayer player, string[] withdrawRequests)
        {
            var currentCrystals = GetCrystalDataList(player);
            if (currentCrystals == null) 
                return "-1";

            int totalWithdrawAmount = 0;
            foreach (var request in withdrawRequests)
            {
                var parts = request.Split(';');
                if (parts.Length != 3) 
                    continue;

                int withdrawAmount = int.Parse(parts[1]);
                totalWithdrawAmount += withdrawAmount;
            }

            if (!KTGlobal.IsHaveSpace(totalWithdrawAmount, player))
            {
                PlayerManager.ShowNotification(player, "Túi đồ không đủ chỗ trống");
                return "-1";
            }

            foreach (var request in withdrawRequests)
            {
                var parts = request.Split(';');
                if (parts.Length != 3) 
                    continue;

                int crystalId = int.Parse(parts[0]);
                int withdrawAmount = int.Parse(parts[1]);
                int binding = int.Parse(parts[2]);

                if (binding != 0 && binding != 1)
                {
                    continue;
                }

                var crystal = currentCrystals.FirstOrDefault(x => x.GoodsID == crystalId && x.Binding == binding);

                if (crystal == null || crystal.GCount < withdrawAmount)
                {
                    continue;
                }

                if (!ItemManager.CreateSimpleItem(player, crystalId, withdrawAmount, 0, binding))
                {
                    continue;
                }

                crystal.GCount -= withdrawAmount;
            }

            var sortedCrystals = new List<GoodsData>();
            foreach (var crystal in currentCrystals)
            {
                if (crystal.Binding != 0 && crystal.Binding != 1)
                {
                    continue;
                }

                if (crystal.GCount > 0)
                {
                    sortedCrystals.Add(crystal);
                }
            }

            sortedCrystals.Sort((a, b) => 
            {
                var idCompare = a.GoodsID.CompareTo(b.GoodsID);
                if (idCompare != 0) 
                    return idCompare;
                return a.Binding.CompareTo(b.Binding);
            });
            

            var updatedData = string.Join(":", sortedCrystals.Select(x => $"{x.GoodsID};{x.GCount};{x.Binding}"));

            LogManager.WriteLog(LogTypes.CrystalBag,
                $"[CrystalBag] Player {player.RoleName}({player.RoleID}) withdrawing crystals:"
                + $"{string.Join(":", withdrawRequests.Select(r => $"[{r}]"))} crystals left in bag: {updatedData}");

            StoreCrystal(player, updatedData);

            return updatedData;
        }

        private static List<GoodsData> GetCrystalDataList(KPlayer player)
        {
            var crystalData = GetCrystal(player);
            if (string.IsNullOrEmpty(crystalData)) 
                return new List<GoodsData>();

            var crystalList = new List<GoodsData>();
            foreach (var item in crystalData.Split(':'))
            {
                var parts = item.Split(';');
                if (parts.Length != 3) continue;

                crystalList.Add(new GoodsData
                {
                    GoodsID = int.Parse(parts[0]),
                    GCount = int.Parse(parts[1]),
                    Binding = int.Parse(parts[2])
                });
            }

            return crystalList;
        }

        private static void StoreCrystal(KPlayer client, string data)
        {
            LogManager.WriteLog(LogTypes.CrystalBag, 
                $"[CrystalBag] Player {client.RoleName}({client.RoleID}) storing crystals: {data}");
            var encode64 = DataHelper.EncodeBase64(data);
            Global.SaveRoleParamsStringToDB(client, RoleParamName.CrystalBag_Store, encode64, true);
        }

        private static string GetCrystal(KPlayer client)
        {
            var encodedString = Global.GetRoleParamsStringWithDB(client, RoleParamName.CrystalBag_Store);
            var decode64 = DataHelper.DecodeBase64(encodedString);

            LogManager.WriteLog(LogTypes.CrystalBag, 
                $"[CrystalBag] Player {client.RoleName}({client.RoleID}) retrieving crystals: {decode64}");
            return decode64;
        }
    }
}