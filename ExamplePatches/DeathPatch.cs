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

namespace RevivalMod.ExamplePatches
{
    internal class DeathPatch : ModulePatch
    {
        // Dictionary to track players that have entered critical state to avoid infinite loops
        private static Dictionary<string, long> _playersInCriticalState = new Dictionary<string, long>();
        private static readonly TimeSpan CRITICAL_STATE_COOLDOWN = TimeSpan.FromSeconds(5);

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill));
        }

        [PatchPrefix]
        static bool Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            try
            {
                // Get the Player field using reflection
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                if (playerField == null)
                {
                    Plugin.LogSource.LogError("Could not find Player field in ActiveHealthController");
                    return true; // Let original method run
                }

                Player player = playerField.GetValue(__instance) as Player;
                if (player == null)
                {
                    Plugin.LogSource.LogError("Player field is null");
                    return true; // Let original method run
                }

                string playerId = player.ProfileId;

                // Check if player is already in critical state cooldown period
                if (_playersInCriticalState.TryGetValue(playerId, out long lastCriticalTime))
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime - lastCriticalTime < CRITICAL_STATE_COOLDOWN.TotalMilliseconds)
                    {
                        // Player is already in critical state, don't process again
                        return false; // Prevent death
                    }
                }

                Plugin.LogSource.LogInfo($"Player {playerId} about to die from {damageType}");

                // Check if the player has the revival item
                var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

                Plugin.LogSource.LogInfo($"Player has defibrillator: {hasDefib || Constants.Constants.TESTING}");

                if (hasDefib || Constants.Constants.TESTING)
                {
                    // Record that player is in critical state with current timestamp
                    _playersInCriticalState[playerId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    // Cancel death and put player in critical state
                    Plugin.LogSource.LogInfo("Preventing death and putting player in critical state");

                    // Stop bleeding effects and set minimal health
                    SetPlayerToMinimalHealth(player, __instance);

                    return false; // Prevent the original Kill method from running
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in Kill patch: {ex}");
            }

            return true; // Run the original method (player will die)
        }

        private static void SetPlayerToMinimalHealth(Player player, ActiveHealthController healthController)
        {
            try
            {
                // Remove all bleeding effects first
                RemoveBleedingEffects(healthController);

                // Set minimal health to all body parts
                foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                {
                    if (bodyPart != EBodyPart.Common)
                    {
                        // Set health to a minimal value (e.g., 5) instead of 1
                        healthController.ChangeHealth(bodyPart, 5, new DamageInfoStruct());
                    }
                }

                // Set energy and hydration to reasonable values
                healthController.ChangeEnergy(20);
                healthController.ChangeHydration(20);

                // Apply painkillers to reduce pain effects
                healthController.DoPainKiller();

                // Optional: Add a visual effect or UI notification for critical state

                Plugin.LogSource.LogInfo("Player set to minimal health (critical state)");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in SetPlayerToMinimalHealth: {ex}");
            }
        }

        private static void RemoveBleedingEffects(ActiveHealthController healthController)
        {
            try
            {
                // Use reflection to get all effects
                MethodInfo removeNegativeEffectsMethod = AccessTools.Method(typeof(ActiveHealthController), "RemoveNegativeEffects");
                if (removeNegativeEffectsMethod != null)
                {
                    // Apply to all body parts to make sure we catch all bleeding effects
                    foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                    {
                        removeNegativeEffectsMethod.Invoke(healthController, new object[] { bodyPart });
                    }

                    Plugin.LogSource.LogInfo("Removed all negative effects from player");
                }
                else
                {
                    Plugin.LogSource.LogWarning("Could not find RemoveNegativeEffects method");

                    // Alternative approach using specific effect removal methods
                    // Try to find other methods to remove bleeding specifically
                    var methods = typeof(ActiveHealthController).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    // Look for method to remove bleeding effects
                    var removeBleedingMethod = methods.FirstOrDefault(m => m.Name.Contains("DoBleed") || m.Name.Contains("RemoveBleedingEffect"));
                    if (removeBleedingMethod != null)
                    {
                        // Call with appropriate parameters based on method signature
                        removeBleedingMethod.Invoke(healthController, new object[] { EBodyPart.Common });
                        Plugin.LogSource.LogInfo("Removed bleeding effects from player");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error removing bleeding effects: {ex}");
            }
        }
    }
}