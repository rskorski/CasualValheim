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
        private static ConfigEntry<bool> InfStaminaBow;
        private static ConfigEntry<bool> InfStaminaFishing;
        private static ConfigEntry<bool> InfStaminaSneaking;

        partial void OdinsEnduranceConfig()
        {
            CVUtil.Log("OdinsEnduranceConfig called");
            MaxWeightMult = Config.Bind("OdinsEndurance", "MaxWeightMult", 10.0f, "multiplier for MaxCarryWeight");
            InfStamina = Config.Bind("OdinsEndurance", "InfStamina", true, "Prevent stamina drain, considering the state specific flags.");
            InfStaminaTargeted = Config.Bind("OdinsEndurance", "InfStaminaTargeted", false, "Prevent stamina drain while targeted.");
            InfStaminaSwimming = Config.Bind("OdinsEndurance", "InfStaminaSwimming", false, "Prevent stamina drain while swimming.");
            InfStaminaBow = Config.Bind("OdinsEndurance", "InfStaminaBow", false, "Prevent stamina drain while drawing a bow, the distance can keep you out of detection range.");
            InfStaminaFishing = Config.Bind("OdinsEndurance", "InfStaminaFishing", false, "Prevent stamina drain while casting and reeling a rod.");
            InfStaminaSneaking = Config.Bind("OdinsEndurance", "InfStaminaSneaking", false, "Prevent stamina drain while sneaking.");
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
                if( !InfStamina.Value )
                {
                    return true;
                }
                if( !InfStaminaSwimming.Value && __instance.IsSwimming() )
                {
                    return true;
                }
                if( !InfStaminaTargeted.Value && __instance.IsTargeted() )
                {
                    return true;
                }
                if( !InfStaminaSneaking.Value && __instance.IsCrouching() )
                {
                    return true;
                }

                // bow draw and fishing look to the weapon in hand
                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if( null != weapon )
                {
                    if( !InfStaminaBow.Value &&
                        weapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.Bow &&
                        __instance.IsDrawingBow() )
                    {
                        return true;
                    }

                    bool isCasting = __instance.IsDrawingBow();
                    bool isReeling = __instance.IsBlocking();
                    if ( !InfStaminaFishing.Value &&
                         weapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.FishingRod &&
                         (isCasting || isReeling) )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
