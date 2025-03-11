using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RevivalMod.Constants;
using RevivalMod.Features;
using EFT.InventoryLogic;
using UnityEngine;

namespace RevivalMod.Patches
{
    // Updated patch to intercept damage and work with the new revival system
    internal class DamageInfoPatch : ModulePatch
    {
        // Track players in critical state to prevent endless loops
        private static Dictionary<string, long> _playersInCriticalState = new Dictionary<string, long>();
        private static readonly TimeSpan CRITICAL_COOLDOWN = TimeSpan.FromSeconds(5);

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.ApplyDamageInfo));
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

                if (!__instance.IsYourPlayer) return true;

                string playerId = __instance.ProfileId;

                // Check if player is invulnerable from revival or in critical state
                if (RevivalFeatures.IsPlayerInvulnerable(playerId))
                {
                    Plugin.LogSource.LogInfo($"Player {playerId} is invulnerable, blocking all damage");
                    damageInfo.Damage = 0f;
                    // Don't even run the original method - completely block all damage
                    return false;
                }

                // Check for critical damage
                bool isVitalPart = bodyPartType == EBodyPart.Head || bodyPartType == EBodyPart.Chest;
                bool isLethalDamage = damageInfo.Damage > 35f || (isVitalPart && damageInfo.Damage > 20f);
                bool isCriticalBleed = damageInfo.DamageType == EDamageType.HeavyBleeding;
                bool wouldBeFatal = IsLikelyFatal(__instance, damageInfo, bodyPartType);

                // If damage would be lethal or is heavy bleeding
                if (wouldBeFatal || isLethalDamage || isCriticalBleed)
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

                        // Set the player in critical state for the revival system
                        RevivalFeatures.SetPlayerCriticalState(__instance, true);

                        Plugin.LogSource.LogInfo($"Critical damage detected to {bodyPartType} from {damageInfo.DamageType}. Player entered critical state.");

                        // For heavy bleeding, we want to let some damage through but prevent death
                        if (isCriticalBleed)
                        {
                            // Allow minimal damage for bleeding effects to apply, but not kill
                            damageInfo.Damage = 5f;
                            Plugin.LogSource.LogInfo("Allowing minimal bleeding damage, player is in critical state");
                            return true;
                        }

                        // For lethal damage, reduce it significantly but allow some through
                        // This will create a "near death" feeling without healing the player
                        if (isLethalDamage || wouldBeFatal)
                        {
                            // Calculate how much damage would leave the player at very low health
                            float currentHealth = GetCurrentHealth(__instance, bodyPartType);
                            float reducedDamage = Math.Max(1f, currentHealth - 5f); // Leave player with 5 HP

                            // Let a small amount through to show damage but prevent death
                            damageInfo.Damage = Math.Min(reducedDamage, damageInfo.Damage);
                            Plugin.LogSource.LogInfo($"Reduced lethal damage to {damageInfo.Damage}, player is in critical state with approx 5 HP remaining");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in UpdatedDamageInfo patch: {ex.Message}");
            }

            return true; // Let original method run
        }

        // Get the current health of a body part
        private static float GetCurrentHealth(Player player, EBodyPart bodyPart)
        {
            try
            {
                return player.HealthController.GetBodyPartHealth(bodyPart, false).Current;
            }
            catch
            {
                return 50f; // Default assumption if we can't get actual health
            }
        }

        // Check if damage would be fatal
        private static bool IsLikelyFatal(Player player, DamageInfoStruct damageInfo, EBodyPart bodyPart)
        {
            try
            {
                float currentHealth = GetCurrentHealth(player, bodyPart);

                // If damage exceeds current health, it would be fatal
                if (damageInfo.Damage >= currentHealth)
                {
                    return true;
                }

                // Check for one-shot kills to vital areas
                bool isHeadshot = bodyPart == EBodyPart.Head && damageInfo.Damage > 35;
                bool isChestshot = bodyPart == EBodyPart.Chest && damageInfo.Damage > 80;

                return isHeadshot || isChestshot;
            }
            catch
            {
                // If we can't determine, assume it's fatal if damage is high
                return damageInfo.Damage > 70;
            }
        }
    }
}