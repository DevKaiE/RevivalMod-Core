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
    internal class DamageInterceptorPatch : ModulePatch
    {
        // Dictionary to track player damage cooldowns
        private static Dictionary<string, long> _playersWithRecentDamage = new Dictionary<string, long>();
        private static readonly TimeSpan DAMAGE_COOLDOWN = TimeSpan.FromSeconds(0.5);

        protected override MethodBase GetTargetMethod()
        {
            // Target the ApplyDamage method which is called before Kill
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));
        }

        [PatchPostfix]
        static void Postfix(ActiveHealthController __instance, EBodyPart bodyPart, float damage, DamageInfoStruct damageInfo)
        {
            try
            {
                // Get the Player field using reflection
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                if (playerField == null) return;

                Player player = playerField.GetValue(__instance) as Player;
                if (player == null) return;

                string playerId = player.ProfileId;

                // Check if this player has had damage recently to avoid processing multiple times
                if (_playersWithRecentDamage.TryGetValue(playerId, out long lastDamageTime))
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime - lastDamageTime < DAMAGE_COOLDOWN.TotalMilliseconds)
                    {
                        // Within cooldown period, don't process again
                        return;
                    }
                }

                // Record current damage time
                _playersWithRecentDamage[playerId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Simplified approach: For any significant damage to vital parts or bleeding damage
                bool isDangerousDamage =
                    (bodyPart == EBodyPart.Head && damage > 5f) ||
                    (bodyPart == EBodyPart.Chest && damage > 5f) ||
                    damageInfo.DamageType == EDamageType.HeavyBleeding ||
                    damageInfo.DamageType == EDamageType.LightBleeding;

                if (isDangerousDamage)
                {
                    // Check if player has defibrillator
                    var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                    bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

                    if (hasDefib || Constants.Constants.TESTING)
                    {
                        Plugin.LogSource.LogInfo($"Intercepting potentially lethal/critical damage to {bodyPart}");

                        // Apply emergency treatment
                        EmergencyTreatment(__instance, bodyPart);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in DamageInterceptor: {ex.Message}");
            }
        }

        private static void EmergencyTreatment(ActiveHealthController healthController, EBodyPart bodyPart)
        {
            try
            {
                // 1. Remove all negative effects
                RemoveAllNegativeEffects(healthController);

                // 2. Heal the damaged part
                HealBodyPart(healthController, bodyPart, 15);

                // 3. Always heal vital parts as well
                if (bodyPart != EBodyPart.Head)
                    HealBodyPart(healthController, EBodyPart.Head, 15);

                if (bodyPart != EBodyPart.Chest)
                    HealBodyPart(healthController, EBodyPart.Chest, 15);

                // 4. Apply painkillers
                healthController.DoPainKiller();
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
                // Try to use the direct method to remove all negative effects
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

        private static void HealBodyPart(ActiveHealthController healthController, EBodyPart bodyPart, float amount)
        {
            try
            {
                // Try to use the Heal method if available
                MethodInfo healMethod = AccessTools.Method(typeof(ActiveHealthController), "Heal");
                if (healMethod != null)
                {
                    healMethod.Invoke(healthController, new object[] { bodyPart, amount });
                    Plugin.LogSource.LogInfo($"Healed {bodyPart} by {amount} points");
                    return;
                }

                // Fallback to ChangeHealth if needed
                healthController.ChangeHealth(bodyPart, amount, new DamageInfoStruct());
                Plugin.LogSource.LogInfo($"Applied health change to {bodyPart}");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error healing body part: {ex.Message}");
            }
        }
    }
}