using EFT;
using EFT.Communications;
using EFT.Interactive;
using RevivalMod;
using RevivalMod.Components;
using RevivalMod.Features;
using RevivalMod.Fika;
using RevivalMod.Helpers;
using RevivalMod.Models;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LockableDoors.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        static void PatchPrefix()
        {
            KeyValuePair<string,bool> playerHasRevivalItem = RevivalFeatureExtension.CheckRevivalItemInRaidInventory();
            FikaInterface.SendItemInRaidInventoryPacket(playerHasRevivalItem.Key, playerHasRevivalItem.Value);
            ServerDataPack pack = Utils.ServerRoute<ServerDataPack>(Plugin.DataToClientURL, ServerDataPack.GetRequestPack());
            foreach (string id in pack.playerIdsInRaid)
            {
                bool hasItem = RMSession.GetHasPlayerRevivalItem(id);
                NotificationManagerClass.DisplayMessageNotification(
                           $"Player with id {id} {(hasItem ? "has revival item" : "doesn't have revival item")}",
                           ENotificationDurationType.Default,
                           ENotificationIconType.Default,
                           Color.yellow);
            }

            
        }
    }
}
