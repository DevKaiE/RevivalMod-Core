using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RevivalMod.Constants;
using EFT.InventoryLogic;
using UnityEngine;

namespace RevivalMod.ExamplePatches
{
    // First patch to intercept damage and reduce it
    internal class DamageInfoPatch : ModulePatch
    {
        // Track players in critical state to prevent endless loops
        private static Dictionary<string, long> _playersInCriticalState = new Dictionary<string, long>();
        private static readonly TimeSpan CRITICAL_COOLDOWN = TimeSpan.FromSeconds(5);

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "ApplyDamageInfo");
        }

        [PatchPrefix]
        static bool Prefix(Player __instance, ref DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            try
            {
                // Skip processing if player is already dead
                if (!__instance.HealthController.IsAlive)
                {
                    return true; // Let original method run
                }

                string playerId = __instance.ProfileId;

                // Check for critical damage
                bool isVitalPart = bodyPartType == EBodyPart.Head || bodyPartType == EBodyPart.Chest;
                bool isLethalDamage = damageInfo.Damage > 35f || (isVitalPart && damageInfo.Damage > 20f);
                bool isCriticalBleed = damageInfo.DamageType == EDamageType.HeavyBleeding;

                // If damage would be lethal or is heavy bleeding
                if (isLethalDamage || isCriticalBleed)
                {
                    // Check if the player has the revival item
                    var inRaidItems = __instance.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                    bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

                    if (hasDefib || Constants.Constants.TESTING)
                    {
                        // Check if player is already in critical state
                        if (_playersInCriticalState.TryGetValue(playerId, out long lastCriticalTime))
                        {
                            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            if (currentTime - lastCriticalTime < CRITICAL_COOLDOWN.TotalMilliseconds)
                            {
                                // Already in critical state, reduce damage but let some through
                                damageInfo.Damage = Math.Min(5f, damageInfo.Damage);
                                return true;
                            }
                        }

                        // Record that player is now in critical state
                        _playersInCriticalState[playerId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        Plugin.LogSource.LogInfo($"Critical damage detected to {bodyPartType} from {damageInfo.DamageType}");

                        // For heavy bleeding, we want to let some damage through but prevent death
                        if (isCriticalBleed)
                        {
                            // Allow minimal damage for bleeding effects to apply, but not kill
                            damageInfo.Damage = 5f;

                            // Apply emergency treatment after allowing minimal damage
                            Plugin.LogSource.LogInfo("Allowing minimal bleeding damage, then treating player");

                            // Schedule treatment to occur after damage applies
                            __instance.StartCoroutine(EmergencyTreatmentCoroutine(__instance, bodyPartType));

                            return true;
                        }

                        // For lethal damage, try to prevent most of it
                        if (isLethalDamage)
                        {
                            // Let a small amount through to trigger effects but prevent death
                            damageInfo.Damage = Math.Min(10f, damageInfo.Damage);

                            // Apply emergency treatment
                            EmergencyTreatment(__instance, bodyPartType);

                            Plugin.LogSource.LogInfo($"Reduced lethal damage to {damageInfo.Damage}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in DamageInfo patch: {ex.Message}");
            }

            return true; // Let original method run
        }

        private static System.Collections.IEnumerator EmergencyTreatmentCoroutine(Player player, EBodyPart bodyPart)
        {
            // Wait a short time to allow the damage to be applied
            yield return new WaitForSeconds(0.2f);

            // Then apply emergency treatment
            EmergencyTreatment(player, bodyPart);
        }

        private static void EmergencyTreatment(Player player, EBodyPart bodyPart)
        {
            try
            {
                Plugin.LogSource.LogInfo($"Applying emergency treatment for {bodyPart}");

                // Get the ActiveHealthController
                ActiveHealthController healthController = player.ActiveHealthController;
                if (healthController == null)
                {
                    Plugin.LogSource.LogError("Could not get ActiveHealthController");
                    return;
                }

                // First remove negative effects
                RemoveAllNegativeEffects(healthController);

                // Apply direct healing
                ApplyDirectHealing(player, healthController);

                // Apply painkillers
                healthController.DoPainKiller();

                Plugin.LogSource.LogInfo("Emergency treatment applied");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in emergency treatment: {ex.Message}");
            }
        }

        private static void RemoveAllNegativeEffects(ActiveHealthController healthController)
        {
            try
            {
                MethodInfo removeNegativeEffectsMethod = AccessTools.Method(typeof(ActiveHealthController), "RemoveNegativeEffects");
                if (removeNegativeEffectsMethod != null)
                {
                    foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                    {
                        try
                        {
                            removeNegativeEffectsMethod.Invoke(healthController, new object[] { bodyPart });
                        }
                        catch { }
                    }
                    Plugin.LogSource.LogInfo("Removed all negative effects from player");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error removing effects: {ex.Message}");
            }
        }

        private static void ApplyDirectHealing(Player player, ActiveHealthController healthController)
        {
            try
            {
                // Use ChangeHealth for all body parts
                healthController.ChangeHealth(EBodyPart.Head, 50f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.Chest, 50f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.LeftArm, 30f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.RightArm, 30f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.LeftLeg, 30f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.RightLeg, 30f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.Stomach, 30f, new DamageInfoStruct());

                // Restore energy and hydration
                healthController.ChangeEnergy(50f);
                healthController.ChangeHydration(50f);

                Plugin.LogSource.LogInfo("Applied direct healing to all body parts");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error applying direct healing: {ex.Message}");
            }
        }
    }

    // Second patch as a last line of defense to prevent death
    
}