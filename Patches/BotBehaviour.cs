using EFT;
using EFT.Bots;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using HarmonyLib;
using RevivalMod.Features;
using Fika.Core.Coop.BotClasses;

namespace RevivalMod.Patches
{
    /// <summary>
    /// Patch to modify bot behavior toward players in critical state
    /// </summary>
    internal class BotBehaviorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Target the method bots use to decide what to do with an enemy
            // This will vary based on game version - the example is simplified
            return AccessTools.Method(typeof(BotAttackManager), nameof(BotAttackManager.PointAndPathDetecting));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            try
            {
                // Get the bot owner
                var botOwnerField = AccessTools.Field(typeof(BotAttackManager), "owner");
                if (botOwnerField == null) return true;

                var botOwner = botOwnerField.GetValue(null) as BotOwner;
                if (botOwner == null) return true;

                // Get the enemy player
                var enemyPlayer = botOwner.Memory.GoalEnemy?.Person?.AIData?.Player;
                if (enemyPlayer == null) return true;

                string playerId = enemyPlayer.ProfileId;

                // If the enemy is in critical state or invulnerable, make the bot ignore them
                if (RevivalFeatures.IsPlayerInCriticalState(playerId) || RevivalFeatures.IsPlayerInvulnerable(playerId))
                {
                    // Reset bot's enemy memory
                    botOwner.Memory.GoalEnemy = null;

                    // Force bot to stop shooting and move away if close
                    var botBrain = botOwner.Brain;
                    if (botBrain != null)
                    {
                        // Set active game action to move away
                        typeof(StandartBotBrain).GetMethod("SetActiveGameAction",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.Invoke(botBrain, new object[] { "retreat" });
                    }

                    Plugin.LogSource.LogInfo($"Bot {botOwner.Profile.ProfileId} is now ignoring player {playerId} in critical state");

                    // Skip original method
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in BotBehaviorPatch: {ex.Message}");
            }

            return true; // Run original method by default
        }
    }
}