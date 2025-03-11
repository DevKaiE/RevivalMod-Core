using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RevivalMod.Constants;
using EFT.InventoryLogic;
using UnityEngine;
using EFT.Communications;

namespace RevivalMod.Features
{
    /// <summary>
    /// Enhanced revival feature with manual activation and temporary invulnerability
    /// </summary>
    internal class RevivalFeatureExtension : ModulePatch
    {
        // Constants for configuration
        private const float INVULNERABILITY_DURATION = 10f; // Duration of invulnerability after revival in seconds
        private const KeyCode MANUAL_REVIVAL_KEY = KeyCode.F5; // Key to trigger manual revival
        private const float REVIVAL_COOLDOWN = 180f; // Cooldown between revivals (3 minutes)

        // States
        private static Dictionary<string, long> _lastRevivalTimesByPlayer = new Dictionary<string, long>();
        private static Dictionary<string, bool> _playerInCriticalState = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _playerIsInvulnerable = new Dictionary<string, bool>();
        private static Dictionary<string, float> _playerInvulnerabilityTimers = new Dictionary<string, float>();
        private static Dictionary<string, float> _criticalModeTags = new Dictionary<string, float>(); // Keep track of players with stealth tag applied
        private static Player PlayerClient { get; set; } = null;
        // Visual effects
        private static GameObject _screenFX;

        protected override MethodBase GetTargetMethod()
        {
            // We're patching the Update method of Player to constantly check for revival key press
            return AccessTools.Method(typeof(Player), nameof(Player.UpdateTick));
        }

        [PatchPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                string playerId = __instance.ProfileId;
                PlayerClient = __instance;
                // Only proceed for the local player
                if (!__instance.IsYourPlayer)
                    return;

                // Update invulnerability timer if active
                if (_playerIsInvulnerable.TryGetValue(playerId, out bool isInvulnerable) && isInvulnerable)
                {
                    if (_playerInvulnerabilityTimers.TryGetValue(playerId, out float timer))
                    {
                        timer -= Time.deltaTime;
                        _playerInvulnerabilityTimers[playerId] = timer;

                        // End invulnerability if timer is up
                        if (timer <= 0)
                        {
                            EndInvulnerability(__instance);
                        }
                    }
                }

                // Check for manual revival key press when in critical state
                if (_playerInCriticalState.TryGetValue(playerId, out bool inCritical) && inCritical)
                {
                    if (Input.GetKeyDown(MANUAL_REVIVAL_KEY))
                    {
                        TryPerformManualRevival(__instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in RevivalFeatureExtension patch: {ex.Message}");
            }
        }

        public static void SetPlayerCriticalState(Player player, bool criticalState)
        {
            if (player == null)
                return;

            string playerId = player.ProfileId;

            // Update critical state
            _playerInCriticalState[playerId] = criticalState;

            if (criticalState)
            {
                // Make player invulnerable while in critical state too
                _playerIsInvulnerable[playerId] = true;

                // Make player invisible to AI
                ApplyStealthToPlayer(player);

                if (player.IsYourPlayer)
                {
                    try
                    {
                        // Apply visual effects for critical state
                        ApplyCriticalStateVisuals(player);

                        // Show revival message
                        NotificationManagerClass.DisplayMessageNotification(
                            "CRITICAL CONDITION! Press F5 to use your defibrillator!",
                            ENotificationDurationType.Long,
                            ENotificationIconType.Default,
                            Color.red);
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogSource.LogError($"Error displaying critical state UI: {ex.Message}");
                    }
                }
            }
            else
            {
                if (player.IsYourPlayer)
                {
                    // Remove critical state visuals
                    RemoveCriticalStateVisuals();
                }

                // If player is leaving critical state without revival (e.g., revival failed),
                // make sure to remove stealth from player and disable invulnerability
                if (!_playerInvulnerabilityTimers.ContainsKey(playerId))
                {
                    RemoveStealthFromPlayer(player);
                    _playerIsInvulnerable.Remove(playerId);
                }
            }
        }

        // Method to make player invisible to AI
        private static void ApplyStealthToPlayer(Player player)
        {
            try
            {
                string playerId = player.ProfileId;

                // Skip if already applied
                if (_criticalModeTags.ContainsKey(playerId))
                    return;

                // Set awareness to 0 to make bots not detect the player
                _criticalModeTags[playerId] = player.Awareness;
                player.Awareness = 0f;

                Plugin.LogSource.LogInfo($"Applied stealth mode to player {playerId}");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error applying stealth mode: {ex.Message}");
            }
        }

        // Method to remove invisibility from player
        private static void RemoveStealthFromPlayer(Player player)
        {
            try
            {
                string playerId = player.ProfileId;

                // Restore player's original awareness level
                if (_criticalModeTags.TryGetValue(playerId, out float awareness))
                {
                    player.Awareness = awareness;
                    _criticalModeTags.Remove(playerId);
                    Plugin.LogSource.LogInfo($"Removed stealth mode from player {playerId}");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error removing stealth mode: {ex.Message}");
            }
        }

        public static KeyValuePair<string, bool> CheckRevivalItemInRaidInventory()
        {
            if (PlayerClient == null)
                return new KeyValuePair<string, bool>(string.Empty, false);

            var inRaidItems = PlayerClient.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            bool hasItem = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

            return new KeyValuePair<string, bool>(PlayerClient.ProfileId, hasItem);
        }


        public static bool TryPerformManualRevival(Player player)
        {
            if (player == null)
                return false;

            string playerId = player.ProfileId;

            // Check if the player has the revival item
            bool hasDefib = CheckRevivalItemInRaidInventory().Value;

            // Check if the revival is on cooldown
            bool isOnCooldown = false;
            if (_lastRevivalTimesByPlayer.TryGetValue(playerId, out long lastRevivalTime))
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                isOnCooldown = (currentTime - lastRevivalTime) < REVIVAL_COOLDOWN;
            }

            if (isOnCooldown)
            {
                // Calculate remaining cooldown
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int remainingCooldown = (int)(REVIVAL_COOLDOWN - (currentTime - lastRevivalTime));

                NotificationManagerClass.DisplayMessageNotification(
                    $"Revival on cooldown! Available in {remainingCooldown} seconds",
                    ENotificationDurationType.Long,
                    ENotificationIconType.Alert,
                    Color.yellow);

                return false;
            }

            if (hasDefib || Constants.Constants.TESTING)
            {
                // Consume the item
                if (hasDefib && !Constants.Constants.TESTING)
                {
                    ConsumeDefibItem(player);
                }

                // Apply emergency treatment
                ApplyRevivalEffects(player);

                // Apply invulnerability
                StartInvulnerability(player);

                // Reset critical state
                _playerInCriticalState[playerId] = false;

                // Set last revival time
                _lastRevivalTimesByPlayer[playerId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Remove critical state visuals
                RemoveCriticalStateVisuals();

                // Show successful revival notification
                NotificationManagerClass.DisplayMessageNotification(
                    "Defibrillator used successfully! You are temporarily invulnerable.",
                    ENotificationDurationType.Long,
                    ENotificationIconType.Default,
                    Color.green);

                Plugin.LogSource.LogInfo($"Manual revival performed for player {playerId}");
                return true;
            }
            else
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "No defibrillator found! Unable to revive!",
                    ENotificationDurationType.Long,
                    ENotificationIconType.Alert,
                    Color.red);

                return false;
            }
        }

        private static void ConsumeDefibItem(Player player)
        {
            try
            {
                var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
                Item defibItem = inRaidItems.FirstOrDefault(item => item.TemplateId == Constants.Constants.ITEM_ID);

                if (defibItem != null)
                {
                    // Use reflection to access the necessary methods to destroy the item
                    MethodInfo moveMethod = AccessTools.Method(typeof(InventoryController), "ThrowItem");
                    if (moveMethod != null)
                    {
                        // This will effectively discard the item
                        moveMethod.Invoke(player.InventoryController, new object[] { defibItem, false, null });
                        Plugin.LogSource.LogInfo($"Consumed defibrillator item {defibItem.Id}");
                    }
                    else
                    {
                        Plugin.LogSource.LogError("Could not find ThrowItem method");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error consuming defib item: {ex.Message}");
            }
        }

        private static void ApplyRevivalEffects(Player player)
        {
            try
            {
                // Use similar logic from DamageInfoPatch's EmergencyTreatment method
                ActiveHealthController healthController = player.ActiveHealthController;
                if (healthController == null)
                {
                    Plugin.LogSource.LogError("Could not get ActiveHealthController");
                    return;
                }

                // Remove negative effects
                RemoveAllNegativeEffects(healthController);

                // Apply direct healing - more generous healing than the regular emergency treatment
                healthController.ChangeHealth(EBodyPart.Head, 100f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.Chest, 100f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.LeftArm, 80f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.RightArm, 80f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.LeftLeg, 80f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.RightLeg, 80f, new DamageInfoStruct());
                healthController.ChangeHealth(EBodyPart.Stomach, 80f, new DamageInfoStruct());

                // Restore energy and hydration
                healthController.ChangeEnergy(100f);
                healthController.ChangeHydration(100f);

                // Apply painkillers effect
                healthController.DoPainKiller();

                Plugin.LogSource.LogInfo("Applied revival effects to player");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error applying revival effects: {ex.Message}");
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

        private static void StartInvulnerability(Player player)
        {
            if (player == null)
                return;

            string playerId = player.ProfileId;
            _playerIsInvulnerable[playerId] = true;
            _playerInvulnerabilityTimers[playerId] = INVULNERABILITY_DURATION;

            // Apply visual effects for invulnerability
            ApplyInvulnerabilityVisuals(player);

            // Start coroutine for visual flashing effect
            player.StartCoroutine(FlashInvulnerabilityEffect(player));

            Plugin.LogSource.LogInfo($"Started invulnerability for player {playerId} for {INVULNERABILITY_DURATION} seconds");
        }

        private static void EndInvulnerability(Player player)
        {
            if (player == null)
                return;

            string playerId = player.ProfileId;
            _playerIsInvulnerable[playerId] = false;
            _playerInvulnerabilityTimers.Remove(playerId);

            // Remove visual effects
            RemoveInvulnerabilityVisuals(player);

            // Remove stealth from player
            RemoveStealthFromPlayer(player);

            // Show notification that invulnerability has ended
            if (player.IsYourPlayer)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Temporary invulnerability has ended.",
                    ENotificationDurationType.Long,
                    ENotificationIconType.Default,
                    Color.white);
            }

            Plugin.LogSource.LogInfo($"Ended invulnerability for player {playerId}");
        }

        private static IEnumerator FlashInvulnerabilityEffect(Player player)
        {
            string playerId = player.ProfileId;
            float flashInterval = 0.5f; // Flash every half second
            bool isVisible = true; // Track visibility state

            // Store original visibility states of all renderers
            Dictionary<Renderer, bool> originalStates = new Dictionary<Renderer, bool>();

            // First ensure player is visible to start
            if (player.PlayerBody != null && player.PlayerBody.BodySkins != null)
            {
                foreach (var kvp in player.PlayerBody.BodySkins)
                {
                    if (kvp.Value != null)
                    {
                        var renderers = kvp.Value.GetComponentsInChildren<Renderer>(true);
                        foreach (var renderer in renderers)
                        {
                            if (renderer != null)
                            {
                                originalStates[renderer] = renderer.enabled;
                                renderer.enabled = true;
                            }
                        }
                    }
                }
            }

            // Now flash the player model
            while (_playerIsInvulnerable.TryGetValue(playerId, out bool isInvulnerable) && isInvulnerable)
            {
                try
                {
                    isVisible = !isVisible; // Toggle visibility

                    // Apply visibility to all renderers in the player model
                    if (player.PlayerBody != null && player.PlayerBody.BodySkins != null)
                    {
                        foreach (var kvp in player.PlayerBody.BodySkins)
                        {
                            if (kvp.Value != null)
                            {
                                var renderers = kvp.Value.GetComponentsInChildren<Renderer>(true);
                                foreach (var renderer in renderers)
                                {
                                    if (renderer != null)
                                    {
                                        renderer.enabled = isVisible;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError($"Error in flash effect: {ex.Message}");
                }

                yield return new WaitForSeconds(flashInterval);
            }

            // Always ensure player is visible when effect ends by restoring original states
            try
            {
                foreach (var kvp in originalStates)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.enabled = true; // Force visibility on exit
                    }
                }
            }
            catch
            {
                // Last resort fallback if the dictionary approach fails
                if (player.PlayerBody != null && player.PlayerBody.BodySkins != null)
                {
                    foreach (var kvp in player.PlayerBody.BodySkins)
                    {
                        if (kvp.Value != null)
                        {
                            kvp.Value.EnableRenderers(true);
                        }
                    }
                }
            }
        }

        private static void ApplyCriticalStateVisuals(Player player)
        {
            //try
            //{
            //    // Create a red vignette effect on the screen
            //    if (_screenFX == null && player.IsYourPlayer)
            //    {
            //        // First ensure any existing _screenFX is properly destroyed
            //        RemoveCriticalStateVisuals();

            //        // Create a UI canvas for our effect using GameObject.Find to get the existing canvas
            //        _screenFX = new GameObject("RevivalCriticalFX");
            //        Canvas canvas = _screenFX.AddComponent<Canvas>();
            //        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            //        canvas.sortingOrder = 999; // Make sure it's on top

            //        // Create the vignette image
            //        GameObject imageObj = new GameObject("Vignette");
            //        imageObj.transform.SetParent(_screenFX.transform, false);
            //        UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();

            //        // Create a pulsing red vignette effect
            //        image.color = new Color(1f, 0f, 0f, 0.3f);

            //        // Set active to ensure it's visible
            //        _screenFX.SetActive(true);

            //        // Start pulsing effect with a safer approach
            //        if (player != null)
            //        {
            //            try
            //            {
            //                player.StartCoroutine(PulseEffect(image));
            //            }
            //            catch
            //            {
            //                // If coroutine fails, at least keep the static red overlay
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Plugin.LogSource.LogError($"Error applying critical state visuals: {ex.Message}");
            //}
        }

        private static IEnumerator PulseEffect(UnityEngine.UI.Image image)
        {
            float minAlpha = 0.2f;
            float maxAlpha = 0.5f;
            float pulseSpeed = 2.0f;

            while (image != null)
            {
                // Pulse the alpha value
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

                image.color = new Color(1f, 0f, 0f, alpha);

                yield return null;
            }
        }

        private static void RemoveCriticalStateVisuals()
        {
            try
            {
                if (_screenFX != null)
                {
                    GameObject.Destroy(_screenFX);
                    _screenFX = null;
                }

                // Also try to find by name in case our reference is stale
                var existingEffect = GameObject.Find("RevivalCriticalFX");
                if (existingEffect != null)
                {
                    GameObject.Destroy(existingEffect);
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error removing critical state visuals: {ex.Message}");
                // Still set reference to null to allow recreation
                _screenFX = null;
            }
        }

        private static void ApplyInvulnerabilityVisuals(Player player)
        {
            // This is handled by the FlashInvulnerabilityEffect coroutine
        }

        private static void RemoveInvulnerabilityVisuals(Player player)
        {
            // Just ensure player is visible
            if (player.PlayerBody != null && player.PlayerBody.BodySkins != null)
            {
                foreach (var renderer in player.PlayerBody.BodySkins)
                {
                   

                    // With this code block:
                    if (renderer.Value != null)
                    {
                        renderer.Value.enabled = true;
                    }
                }
            }
        }

        public static bool IsPlayerInvulnerable(string playerId)
        {
            return _playerIsInvulnerable.TryGetValue(playerId, out bool invulnerable) && invulnerable;
        }
    }
}