using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic.Manager.Pet;
using GameServer.KiemThe.LuaSystem.Entities;
using MoonSharp.Interpreter;
using Server.Data;
using Server.Tools;

namespace GameServer.KiemThe.LuaSystem.Logic
{
    
    [MoonSharpUserData]
    public static class KTLuaLib_Pet
    {
        public static int GetNumberOfPets(Lua_Player player) 
        {
            lock (player.RefObject.GoodsDataList)
            {
                var listPets = player.RefObject.GoodsDataList.Where(x => x.IsPet()).ToList();
                return listPets.Count();
            }
        }

        public static List<Lua_Item> GetPets(Lua_Player player, int minNumberSkillRequired, int maxNumberSkillRequired, bool includeYenTieuLau = false)
        {
            lock (player.RefObject.GoodsDataList)
            {
                var listPets = player.RefObject.GoodsDataList.Where(x => x.IsPet()).ToList();
                var listValidPets = new List<GoodsData>();
                if (minNumberSkillRequired != -1)
                {
                    foreach (var pet in listPets) 
                    {
                        var numberSkills = PetManager.GetNumberSkill(pet);
                        if ((numberSkills >= minNumberSkillRequired && numberSkills <= maxNumberSkillRequired) || (includeYenTieuLau && PetManager.IsYenTieuLau(pet.GoodsID)))
                        {
                            listValidPets.Add(pet);
                        }
                    }
                }
                else
                {
                    listValidPets = listPets;
                }
                
                if (listValidPets.Count == 0)
                    return null;

                return listValidPets.Select(x => new Lua_Item
                {
                    RefObject = x
                }).ToList();
            }
        }

        public static bool CanLearnNewSkill(Lua_Item pet, int skillID)
        {
            var slotAvailable = PetItemManager.GetNumberOfLearnedSkills(pet.RefObject) < 10;
            if (skillID == -1)
                return slotAvailable;

            return slotAvailable && PetItemManager.CanLearnSkill(pet.RefObject, skillID);
        }

        public static int AddNewSkillToPet(Lua_Player player, Lua_Item pet, int skillID)
        {
            return PetItemManager.AddPetSkill(player.RefObject, pet.RefObject, skillID);
        }

        public static IDictionary<int, int> GetBinhTinhHoaReturn(Lua_Player player, Lua_Item pet) 
        {
            return PetItemManager.CaculationPet(player.RefObject, pet.RefObject.Id);
        }

        public static int GetNumberOfLearnedSkills(Lua_Item pet) 
        {
            return PetItemManager.GetNumberOfLearnedSkills(pet.RefObject);
        }

        public static int GetStarsRank(Lua_Item pet)
        {
            return PetManager.GetNumberSkill(pet.RefObject);
        }
    }
}