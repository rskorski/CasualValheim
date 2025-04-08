using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ItemType = ItemDrop.ItemData.ItemType;

namespace CasualValheim
{
    public partial class Mod : BaseUnityPlugin
    {
        /////////////////////////////////
        //  Started taking from DeathTweaks 
        // https://github.com/aedenthorn/ValheimMods/blob/master/DeathTweaks/BepInExPlugin.cs
        //
        // then realized I mostly want to adjust the settings Valheim already has tied to the presets
        //
        // Death and item drop can be altered with these GlobalKeys, but none of them prevent items from being left on the tombstone
        //
        //  SkillReductionRate
        //  DeathKeepEquip
        //  DeathDeleteUnequipped
        //  DeathDeleteItems
        //  
        // 


        private static ConfigEntry<float> DeathPenaltyPercentage = null;
        private static ConfigEntry<string> ItemsDroppedOnDeathStrings = null;

        private static List<ItemType> ItemsDroppedOnDeathEnums; // a null list will represent "Default".

        readonly static String DropOnDeathDefault = "CVDefault";

        partial void DeathClemencyConfig()
        {
            

            CVUtil.Log("DeathClemencyConfig called");
            DeathPenaltyPercentage = Config.Bind("DeathClemency", "DeathPenaltyPercentage", 0.0f, "Percentage of skill loss on death");
            ItemsDroppedOnDeathStrings = Config.Bind("DeathClemency", "ItemsDroppedOnDeath", "", $"Comma separated list matching the ItemDrop.ItemData.ItemType enum.  Put \"{DropOnDeathDefault}\" to apply game default item drop behavior.");


            List<string> splitString = ItemsDroppedOnDeathStrings.Value.Split(',').ToList();
            ItemsDroppedOnDeathEnums = new List<ItemType>(splitString.Count);
            foreach(var i in splitString)
            {
                string str = i.Trim();

                if(str.Equals( "cvdefault", StringComparison.OrdinalIgnoreCase) )
                {
                    // we use a null list to represent use of default game behavior
                    ItemsDroppedOnDeathEnums = null;
                    break;
                }

                bool ignoreCase = true;
                ItemType e;
                if ( Enum.TryParse(str, ignoreCase, out e) )
                {
                    ItemsDroppedOnDeathEnums.Add(e);
                }
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
        [HarmonyPriority(Priority.First)]
        static class Skills_LowerAllSkills_Patch
        {
            static void Prefix(ref float factor)
            {
                CVUtil.Log($"LowerAllSkills_Prefix |   __result in: [{factor}],  DeathPenaltyPercentage.Value [{DeathPenaltyPercentage.Value}]");
                factor *= DeathPenaltyPercentage.Value;
                CVUtil.Log($"LowerAllSkills_Prefix |   __result out: [{factor}]");
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
        [HarmonyPriority(Priority.First)]
        class Inventory_MoveInventoryToGrave_Patch
        {
            public class Payload
            {
                public List<ItemDrop.ItemData> limbo;
            }

            static void Prefix(Inventory original, out Payload __state)
            {
                __state = new Payload();
                if (null == ItemsDroppedOnDeathEnums)
                {
                    CVUtil.Log("MoveInventoryToGrave_Prefix| Default drop behavior indicated, passing through to original function.");
                    return;
                }

                var originalItems = original.GetAllItems();
                __state.limbo = new List<ItemDrop.ItemData>(originalItems.Count);

                for ( int iItem = originalItems.Count-1; iItem >=0; --iItem)
                {
                    var item = originalItems[iItem];
                    if (!ItemsDroppedOnDeathEnums.Contains(item.m_shared.m_itemType) )
                    {
                        CVUtil.Log($"MoveInventoryToGrave_Prefix| Keeping item [{item.m_shared.m_name}] due to type [{item.m_shared.m_itemType}].");
                        __state.limbo.Add(item);
                        originalItems.RemoveAt(iItem);
                    }
                    else
                    {
                        CVUtil.Log($"MoveInventoryToGrave_Prefix| Passing item [{item.m_shared.m_name}] due to type [{item.m_shared.m_itemType}].");
                    }
                }
            }

            static void Postfix(Inventory original, Payload __state)
            {
                if (null != __state &&
                    null != __state.limbo)
                {
                    foreach (ItemDrop.ItemData item in __state.limbo)
                    {
                        original.AddItem(item, item.m_gridPos);
                    }
                    __state.limbo.Clear();
                }
            }
        }
    }
}