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
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), "Kill");
        }

        [PatchPrefix]
        static bool Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            try
            {
                // Get the Player field
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                if (playerField == null) return true;

                Player player = playerField.GetValue(__instance) as Player;
                if (player == null) return true;

                Plugin.LogSource.LogInfo($"DEATH PREVENTION: Player {player.ProfileId} about to die from {damageType}");

                // Check if the player has the revival item
                var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

                Plugin.LogSource.LogInfo($"DEATH PREVENTION: Player has defibrillator: {hasDefib || Constants.Constants.TESTING}");

                if (hasDefib || Constants.Constants.TESTING)
                {
                    Plugin.LogSource.LogInfo("DEATH PREVENTION: Blocking death completely");

                    // Final emergency treatment as last resort
                    FinalEmergencyTreatment(__instance, player);

                    return false; // Block the kill completely
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in Death prevention patch: {ex.Message}");
            }

            return true;
        }

        private static void FinalEmergencyTreatment(ActiveHealthController healthController, Player player)
        {
            try
            {
                // Try all possible methods to revive the player

                // 1. Remove all negative effects first
                MethodInfo removeNegEffectsMethod = AccessTools.Method(typeof(ActiveHealthController), "RemoveNegativeEffects");
                if (removeNegEffectsMethod != null)
                {
                    foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                    {
                        try
                        {
                            removeNegEffectsMethod.Invoke(healthController, new object[] { bodyPart });
                        }
                        catch { }
                    }
                }

                // 2. Try to restore full health if the method exists
                MethodInfo restoreFullHealthMethod = AccessTools.Method(typeof(ActiveHealthController), "RestoreFullHealth");
                if (restoreFullHealthMethod != null)
                {
                    try
                    {
                        restoreFullHealthMethod.Invoke(healthController, null);
                    }
                    catch { }
                }

                // 3. Direct health change as backup
                foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                {
                    if (bodyPart != EBodyPart.Common)
                    {
                        try
                        {
                            healthController.ChangeHealth(bodyPart, 100f, new DamageInfoStruct());
                        }
                        catch { }
                    }
                }

                // 4. Energy and hydration
                try
                {
                    healthController.ChangeEnergy(100f);
                    healthController.ChangeHydration(100f);
                }
                catch { }

                // 5. Apply painkillers
                try
                {
                    healthController.DoPainKiller();
                }
                catch { }

                Plugin.LogSource.LogInfo("DEATH PREVENTION: Applied final emergency treatment");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in final emergency treatment: {ex.Message}");
            }
        }
    }
}