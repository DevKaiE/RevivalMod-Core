using EFT;
using EFT.Communications;
using Comfort.Common;
using RevivalMod;
using RevivalMod.Components;
using RevivalMod.Features;
using RevivalMod.Fika;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;
using EFT.InventoryLogic;
using System.Linq;

namespace RevivalMod.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        static void PatchPostfix()
        {
            try
            {
                Plugin.LogSource.LogInfo("Game started, checking revival item");

                // Make sure GameWorld is instantiated
                if (!Singleton<GameWorld>.Instantiated)
                {
                    Plugin.LogSource.LogError("GameWorld not instantiated yet");
                    return;
                }

                // Initialize player client directly
                Player playerClient = Singleton<GameWorld>.Instance.MainPlayer;
                if (playerClient == null)
                {
                    Plugin.LogSource.LogError("MainPlayer is null");
                    return;
                }

                // Check if player has revival item
                string playerId = playerClient.ProfileId;
                var inRaidItems = playerClient.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                bool hasItem = false;

                try
                {
                    hasItem = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError($"Error checking player items: {ex.Message}");
                }

                Plugin.LogSource.LogInfo($"Player {playerId} has revival item: {hasItem}");

                // Initialize RMSession with this player
                try
                {
                    RMSession.AddToInRaidPlayersWithItem(playerId, hasItem);
                    Plugin.LogSource.LogInfo($"Added player {playerId} to RMSession");
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError($"Error adding player to session: {ex.Message}");
                }

                // Send packet if Fika is installed
                if (Plugin.FikaInstalled)
                {
                    Plugin.LogSource.LogInfo("Fika is installed, sending packet");
                    FikaInterface.SendItemInRaidInventoryPacket(playerId, hasItem);
                }

                // Display notification about revival item status
                NotificationManagerClass.DisplayMessageNotification(
                    $"Revival System: {(hasItem ? "Revival item found" : "No revival item found")}",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Default,
                    hasItem ? Color.green : Color.yellow);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in GameStartedPatch: {ex.Message}");
            }
        }
    }
}