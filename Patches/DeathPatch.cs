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

namespace RevivalMod.Patches
{
    internal class DeathPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill));
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

                if (!player.IsYourPlayer || player.IsAI) return true;

                string playerId = player.ProfileId;

                // Check if player is invulnerable from recent revival
                if (RevivalFeatures.IsPlayerInvulnerable(playerId))
                {
                    Plugin.LogSource.LogInfo($"Player {playerId} is invulnerable, blocking death completely");
                    return false; // Block the kill completely
                }

                Plugin.LogSource.LogInfo($"DEATH PREVENTION: Player {player.ProfileId} about to die from {damageType}");

                // Check if the player has the revival item
                var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

                Plugin.LogSource.LogInfo($"DEATH PREVENTION: Player has defibrillator: {hasDefib || Constants.Constants.TESTING}");

                if (hasDefib || Constants.Constants.TESTING)
                {
                    Plugin.LogSource.LogInfo("DEATH PREVENTION: Setting player to critical state instead of death");

                    // Set the player in critical state for the revival system
                    RevivalFeatures.SetPlayerCriticalState(player, true);

                    // Block the kill completely
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in Death prevention patch: {ex.Message}");
            }

            return true;
        }
    }
}