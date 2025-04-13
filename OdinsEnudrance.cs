using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Text;
using System.Linq;


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
        private static ConfigEntry<string> PunyBeings;

        partial void OdinsEnduranceInit()
        {
            CVUtil.Log("OdinsEndurance Initializing");

            MaxWeightMult = Config.Bind("OdinsEndurance", "MaxWeightMult", 10.0f, "multiplier for MaxCarryWeight");
            InfStamina = Config.Bind("OdinsEndurance", "InfStamina", true, "Prevent stamina drain, considering the state specific flags.");
            InfStaminaTargeted = Config.Bind("OdinsEndurance", "InfStaminaTargeted", false, "Prevent stamina drain while targeted.");
            PunyBeings = Config.Bind("OdinsEndurance", "PunyBeings", "|neck|greyling|boar|deer|",
                                       string.Join(System.Environment.NewLine,
                                                   "These puny beings are insignificant compared to the might of Odin, and their presence will be ignored even if InfStaminaTargets == true.",
                                                   "Case insensitive, pipe separated and surrounded (start and end line with a pipe) without spaces.  Ex: |beiNG1|being2|BEing3|")
                                       );
            InfStaminaSwimming = Config.Bind("OdinsEndurance", "InfStaminaSwimming", false, "Prevent stamina drain while swimming.");
            InfStaminaBow = Config.Bind("OdinsEndurance", "InfStaminaBow", false, "Prevent stamina drain while drawing a bow, the distance can keep you out of detection range.");
            InfStaminaFishing = Config.Bind("OdinsEndurance", "InfStaminaFishing", false, "Prevent stamina drain while casting and reeling a rod.");
            InfStaminaSneaking = Config.Bind("OdinsEndurance", "InfStaminaSneaking", false, "Prevent stamina drain while sneaking.");

            playerTargetingMap.PunyBeings = PunyBeings.Value.ToLower();
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

                // using both Player.IsTargeted and the targeting map might be redundant, but I'm not certain about the timing in which the target data is cleared from the AI vs the player
                if( !InfStaminaTargeted.Value &&
                    __instance.IsTargeted() && playerTargetingMap.CheckForMightyTargeters(__instance) )
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
                    if( !InfStaminaFishing.Value &&
                         weapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.FishingRod &&
                         (isCasting || isReeling) )
                    {
                        return true;
                    }
                }

                return false;
            }
        }



        ////////////////////////////////////////
        //
        //  Puny Beings
        //
        // NOTE: LogDebug should be used most often in this system to avoid revealing game information a player may not know otherwise.
        //   Spawned NPCs are not immediately visible so the player would "see" them before they should.

        class TargetingMap
        {
            class TargetingPair
            {
                public int playerIID { get; }
                public int npcIID { get; }

                public TargetingPair(int playerIID, int npcIID)
                {
                    this.playerIID = playerIID;
                    this.npcIID = npcIID;
                }
            }

            // we hold two dicts to get quick lookups from either the player perspective or the NPC's perspective.
            // the IID should come from a Character object.  The API works with Characters and Players (which inherits from Character) to enforce that.
            // The functions we work with also uses BaseAI, which comes from a different class tree and has its own separate IID that is not present in these tables.
            // 
            // Character IID => target pairing set {targeted player's character IID,  targetting npc character IID}
            //
            // I'm not certain we get multiple players on the same client.  If not, we got some noteable overengineering with player2np
            // NPCs cannot currently target multiple players, but npc2player holds a set jic.
            class DictIID2TP : Dictionary<int, HashSet<TargetingPair>> { }
            private DictIID2TP player2npc = new DictIID2TP();
            private DictIID2TP npc2player = new DictIID2TP();

            public string PunyBeings { get; set; } = "";

            // return true if the NPC was targeting something, false if not.
            //
            // NOTE: even though we store a set of targets for an NPC, this function relies on the fact that npcs can only have one target.
            // I don't expect this function would be hard to update to accomodate multiple targets, however knowing which player they are
            // untargeting could be tough depending on the hooks Valheim gives us; we'd need to track which player an AI stopped targeting.
            public bool ClearNPCTarget(Character npc)
            {
                int iid = npc.GetInstanceID();
                var oldTargets = npc2player[iid];
                bool wasTargeting = oldTargets.Count > 0;

                npc2player[iid] = new HashSet<TargetingPair>();

                foreach (var tp in oldTargets)
                {
                    player2npc[tp.playerIID].Remove(tp);
                }

                return wasTargeting;
            }


            public enum Consideration
            {
                Existing,  // the targeting was previously established
                Puny,      // the targeter is puny, ignore hid
                Worthy,    // the targeter is a worthy opponent, heed its challenge
            }

            public Consideration ConsiderPlayerAsTarget(Character targetingNPC, Player player)
            {
                int playerIID = player.GetInstanceID();
                int npcIID  = targetingNPC.GetInstanceID();
                string processedName = GetProcessedName(targetingNPC);

                CVUtil.LogVerbose
                    ( () => {

                        StringBuilder sb = new StringBuilder();
                        sb.Append("ConsiderPlayerAsTarget | tables ----");
                        sb.AppendLine("player2npc = {");
                        foreach( var kvp in player2npc )
                        {
                            sb.AppendLine($"  [{kvp.Key.ToString()}] = {kvp.Value.ToString()}");
                        }
                        sb.AppendLine("}");
                        sb.AppendLine("");
                        sb.AppendLine("npc2player = {");
                        foreach( var kvp in npc2player )
                        {
                            sb.AppendLine($"  [{kvp.Key.ToString()}] = {kvp.Value.ToString()}");
                        }
                        sb.AppendLine("}");
                        return sb.ToString();
                    });

                if( PunyBeings.Contains(processedName) )
                {
                    return Consideration.Puny;
                }
                TargetingPair tp = new TargetingPair(playerIID, npcIID);
                
                // the dicts should mirror each other, so we only check one of them to determine the return
                player2npc[playerIID].Add(tp);
                bool wasPreviouslyTargeted = npc2player[npcIID].Add(tp);

                return wasPreviouslyTargeted ? Consideration.Existing : Consideration.Worthy;
            }
            public bool CheckForMightyTargeters(Player player)
            {
                return player2npc[player.GetInstanceID()].Count > 0;
            }

            public void PrepNPC(Character npc)
            {
                npc2player[npc.GetInstanceID()] = new HashSet<TargetingPair>();
            }
            public void PrepPlayer(Player player)
            {
                player2npc[player.GetInstanceID()] = new HashSet<TargetingPair>();
            }

            public void CleanupNPC(Character npc)
            {
                int iid = npc.GetInstanceID();
                var set = npc2player[iid];
                npc2player.Remove(iid);
                foreach( var tp in set )
                {
                    player2npc[tp.playerIID].Remove(tp);
                }
            }
            public void CleanupPlayer(Player player)
            {
                int iid = player.GetInstanceID();
                var set = player2npc[iid];
                player2npc.Remove(iid);
                foreach( var tp in set )
                {
                    npc2player[tp.npcIID].Remove(tp);
                }
            }
        }

        static FieldInfo MonsterAI_targetCreature = typeof(MonsterAI).GetField("m_targetCreature", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo AnimalAI_target = typeof(AnimalAI).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo BaseAI_character = typeof(BaseAI).GetField("m_character", BindingFlags.NonPublic | BindingFlags.Instance);

        static TargetingMap playerTargetingMap = new TargetingMap();

        // Get the name with pipes around it.  The config explains it needs the pipes, too.
        // That allows us to keep the puny being list in a string while allowing complete string match on the name.
        // eg: we can differentiate, say, "neck" from "superneck"
        static string GetProcessedName(MonoBehaviour b)
        {
            return $"|{b.name.Replace("(Clone)", "").ToLower()}|";
        }

        static void UpdateAIForPunyBeings(Character npc, Character target)
        {
            if( null == npc )
            {
                CVUtil.LogError($"UpdateAIForPunyBeings | ERROR: processing an npc that has no Character");
            }

            if( null == npc || null == target || !target.IsPlayer() )
            {
                return;
            }

            if( null != target && target.IsPlayer() )
            {
                Player player = (Player)target;
                TargetingMap.Consideration con = playerTargetingMap.ConsiderPlayerAsTarget(npc, player);
                switch( con )
                {
                    case TargetingMap.Consideration.Worthy:
                        CVUtil.LogDebug($"UpdateAIForPunyBeings | NPC [iid:{npc.GetInstanceID()}, name:{npc.name}] has started targeting Player [iid:{player.GetInstanceID()}, name:{player.m_name}].");
                        break;

                    case TargetingMap.Consideration.Puny:
                        CVUtil.LogDebug($"UpdateAIForPunyBeings | Player [{player.m_name}] cares not about NPC [iid:{npc.GetInstanceID()}, name:{npc.name}], for they are puny.");
                        break;

                    default:
                        break;
                }
            }
            else
            {
                if( playerTargetingMap.ClearNPCTarget(npc) )
                {
                    CVUtil.LogDebug($"UpdateAIForPunyBeings | NPC [iid:{npc.GetInstanceID()}, name:{npc.name}] no longer has a target.");
                }
            }
        }


        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        static class MonsterAI_UpdateAI_Patch
        {
            static void Postfix(MonsterAI __instance)
            {                
                Character monster = (Character)BaseAI_character.GetValue(__instance);
                Character target = (Character)MonsterAI_targetCreature.GetValue(__instance);
                CVUtil.LogVerbose($"MonsterAI.UpdateAI.POSTFIX | Update Monster [iid:{monster.GetInstanceID()}, name:{monster.name}].");
                UpdateAIForPunyBeings(monster, target);
            }
        }

        [HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
        static class AnimalAI_UpdateAI_Patch
        {
            static void Postfix(AnimalAI __instance)
            {
                Character animal = (Character)BaseAI_character.GetValue(__instance);
                Character target = (Character)AnimalAI_target.GetValue(__instance);
                CVUtil.LogVerbose($"AnimalAI.UpdateAI.POSTFIX  | Update Animal [iid:{animal.GetInstanceID()}, name:{animal.name}].");
                UpdateAIForPunyBeings(animal, target);
            }
        }


        /////////////////
        // hooks into Awake/Destroy functions to pre-emptively create and remove entries from playerTargetingMap
        // Lets other functions use the map without having to worry about empty dict entries and null lists.
        [HarmonyPatch(typeof(BaseAI), "Awake")]
        static class BaseAI_Awake_Patch
        {
            static void Postfix(BaseAI __instance)
            {
                Character character = (Character)BaseAI_character.GetValue(__instance);
                CVUtil.LogDebug($"BaseAI.Awake.POSTFIX     | Awaken, NPC [iid:{character.GetInstanceID()},  name:{character.name}].");
                playerTargetingMap.PrepNPC(character);
            }
        }
        [HarmonyPatch(typeof(BaseAI), "OnDestroy")]
        static class BaseAI_OnDestroy_Patch
        {
            static void Postfix(BaseAI __instance)
            {
                Character character = (Character)BaseAI_character.GetValue(__instance);
                CVUtil.LogDebug($"BaseAI.OnDestroy.POSTFIX | Be gone, NPC [iid:{character.GetInstanceID()},  name:{character.name}]");
                playerTargetingMap.CleanupNPC(character);
            }
        }
        [HarmonyPatch(typeof(Player), "Awake")]
        static class Player_Awake_Patch
        {
            static void Postfix(Player __instance)
            {
                CVUtil.LogDebug($"Player.Awake.POSTFIX | Awaken, Player [iid:{__instance.GetInstanceID()}, name:{__instance.name}].");
                playerTargetingMap.PrepPlayer(__instance);
            }
        }
        [HarmonyPatch(typeof(Player), "OnDestroy")]
        static class Player_OnDestroy_Patch
        {
            static void Postfix(Player __instance)
            {
                CVUtil.LogDebug($"Player.OnDestroy.POSTFIX     | Be gone, Player [iid:{__instance.GetInstanceID()},  name:{__instance.name}].");
                playerTargetingMap.CleanupPlayer(__instance);
            }
        }
    }
}
