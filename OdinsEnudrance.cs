using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;


namespace CasualValheim
{
    public partial class Mod : BaseUnityPlugin
    {
        /////////////////////////////////
        // Took some points from NeverEncumbered
        // https://github.com/JulianDeclercq/ValheimMods/blob/main/NeverEncumbered/NeverEncumbered.cs
        // 
        // It was quite complicated, pivoted when I realized I just want to up the max weight
        //
        //

        private static ConfigEntry<float> MaxWeightMult;
        private static ConfigEntry<bool> InfStamina;
        private static ConfigEntry<bool> InfStaminaTargeted;
        private static ConfigEntry<bool> InfStaminaSwimming;

        partial void OdinsEnduranceConfig()
        {
            CVUtil.Log("OdinsEnduranceConfig called");
            MaxWeightMult = Config.Bind("OdinsEndurance", "MaxWeightMult", 10.0f, "multiplier for MaxCarryWeight");
            InfStamina = Config.Bind("OdinsEndurance", "InfStamina", true, "Prevent stamina drain, considering the state specific flags.");
            InfStaminaTargeted = Config.Bind("OdinsEndurance", "InfStaminaTargeted", false, "Prevent stamina drain while targeted.");
            InfStaminaSwimming = Config.Bind("OdinsEndurance", "InfStaminaSwimming", false, "Prevent stamina drain while swimming.");
        }

        ////////////////////////////////////////
        //
        //  Carry Weight
        //
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        static class Player_GetMaxCarryWeight_Patch
        {
            static void Postfix(ref float __result)
            {
                __result *= MaxWeightMult.Value;
            }
        }



        ////////////////////////////////////////
        //
        //  Infinite Stamina
        //
        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        static class Player_UseStamina_Patch
        {
            static bool Prefix(Player __instance, float v)
            {
                if ( !InfStamina.Value )
                {
                    return true;
                }
                if( __instance.IsSwimming() && !InfStaminaSwimming.Value )
                {
                    return true;
                }
                if( __instance.IsTargeted() && !InfStaminaTargeted.Value )
                {
                    return true;
                }
                return false;
            }
        }
    }
}
