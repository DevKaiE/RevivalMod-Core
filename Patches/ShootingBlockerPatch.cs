using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using RevivalMod.Features;
using RevivalMod.Helpers;

namespace RevivalMod.Patches
{
    /// <summary>
    /// Patch to block shooting while in invulnerable state
    /// </summary>
    internal class ShootingBlockerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Target the method responsible for firing weapons
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.RegisterShot));
        }

        [PatchPrefix]
        static bool Prefix(Player.FirearmController __instance)
        {
            try
            {
                // Get the player instance from the FirearmController
                Player player = Helpers.Utils.GetYourPlayer();
                if (player == null)
                {
                    return true; // Let original method run if we can't get player
                }

                string playerId = player.ProfileId;

                // Check if player is invulnerable
                if (RevivalFeatures.IsPlayerInvulnerable(playerId))
                {
                    // Block shooting completely
                    if (player.IsYourPlayer)
                    {
                        // Only show message for local player to avoid spam
                        NotificationManagerClass.DisplayMessageNotification(
                            "Cannot shoot while in critical state!",
                            EFT.Communications.ENotificationDurationType.Default,
                            EFT.Communications.ENotificationIconType.Alert,
                            UnityEngine.Color.red);
                    }

                    Plugin.LogSource.LogInfo($"Player {playerId} attempted to shoot while invulnerable - blocking");
                    return false; // Block the shot completely
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in ShootingBlockerPatch: {ex.Message}");
            }

            return true; // Allow shooting
        }
    }
}