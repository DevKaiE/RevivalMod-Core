using EFT;
using UnityEngine;
using EFT.Interactive;
using RevivalMod.Features;
using RevivalMod.Helpers;
using RevivalMod.Fika;
using Comfort.Common;
using EFT.InventoryLogic;
using System.Linq;
using RevivalMod.Constants;
using EFT.Communications;
using System;

namespace RevivalMod.Components
{
    /// <summary>
    /// Component that makes a downed player interactable for revival
    /// </summary>
    public class RevivablePlayerComponent : MonoBehaviour, IPhysicsTrigger
    {
        // Reference to the downed player
        private Player _downedPlayer;

        // Visual marker for revival
        private GameObject _reviveMarker;

        // Interaction properties
        public string Id { get; } = "RevivablePlayer";

        public bool IsInteractable => true;

        public float InteractionDistance => 2f; // Distance at which a player can interact

        // Start delay for player-to-player revival
        private const float PLAYER_REVIVAL_DELAY = 3f; // 3 seconds to revive

        // Timer for revival progress
        private float _revivalProgress = 0f;
        private bool _isBeingRevived = false;
        private Player _reviverPlayer = null;

        // Initialize component
        public void Initialize(Player downedPlayer)
        {
            _downedPlayer = downedPlayer;
            CreateReviveMarker();
            Plugin.LogSource.LogInfo($"RevivablePlayerComponent initialized for player {_downedPlayer.ProfileId}");
        }

        // Create a visual marker indicating this player can be revived
        private void CreateReviveMarker()
        {
            if (_reviveMarker != null)
                return;

            try
            {
                // Create a simple marker (primitive cube)
                _reviveMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _reviveMarker.name = "ReviveMarker";
                _reviveMarker.transform.SetParent(transform);
                _reviveMarker.transform.localPosition = new Vector3(0, 1.5f, 0); // Above player
                _reviveMarker.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // Small marker

                // Make it semi-transparent red
                Renderer renderer = _reviveMarker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = new Material(Shader.Find("Standard"));
                    material.color = new Color(1f, 0, 0, 0.7f); // Semi-transparent red
                    renderer.material = material;
                }

                // Disable collider
                Collider collider = _reviveMarker.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                // Add flashing effect
                StartCoroutine(FlashMarker());
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error creating revive marker: {ex.Message}");
            }
        }

        // Flashing effect for the marker
        private System.Collections.IEnumerator FlashMarker()
        {
            float alpha = 0.7f;
            bool increasing = false;

            while (_reviveMarker != null)
            {
                if (increasing)
                {
                    alpha += Time.deltaTime * 2f;
                    if (alpha >= 0.7f)
                    {
                        alpha = 0.7f;
                        increasing = false;
                    }
                }
                else
                {
                    alpha -= Time.deltaTime * 2f;
                    if (alpha <= 0.2f)
                    {
                        alpha = 0.2f;
                        increasing = true;
                    }
                }

                Renderer renderer = _reviveMarker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    renderer.material.color = new Color(color.r, color.g, color.b, alpha);
                }

                yield return null;
            }
        }

        // IPhysicsTrigger implementation for interaction
        public void Interact(Player player)
        {
            if (_downedPlayer == null || !RevivalFeatures.IsPlayerInCriticalState(_downedPlayer.ProfileId))
            {
                // Player is no longer in critical state
                DestroyMarker();
                Destroy(this);
                return;
            }

            // Check if the revival is already in progress
            if (_isBeingRevived)
            {
                if (_reviverPlayer != player)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "This player is already being revived by someone else!",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow);
                }
                return;
            }

            // Check if the player has the revival item
            var inRaidItems = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            bool hasDefib = inRaidItems.Any(item => item.TemplateId == Constants.Constants.ITEM_ID);

            if (!hasDefib && !Settings.TESTING.Value)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "You need a defibrillator to revive this player!",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red);
                return;
            }

            // Start revival process
            StartRevival(player);
        }

        // Start the revival process
        private void StartRevival(Player reviver)
        {
            _isBeingRevived = true;
            _reviverPlayer = reviver;
            _revivalProgress = 0f;

            // Show progress notification
            NotificationManagerClass.DisplayMessageNotification(
                "Starting revival process...",
                ENotificationDurationType.Default,
                ENotificationIconType.Default,
                Color.yellow);

            // Start reviving
            StartCoroutine(ReviveProcess());
        }

        // Revival coroutine
        private System.Collections.IEnumerator ReviveProcess()
        {
            float totalReviveTime = PLAYER_REVIVAL_DELAY;

            while (_revivalProgress < totalReviveTime)
            {
                // Check if reviver is still close enough
                if (_reviverPlayer == null || Vector3.Distance(_reviverPlayer.Position, _downedPlayer.Position) > InteractionDistance * 1.5f)
                {
                    // Revival interrupted - reviver moved away
                    _isBeingRevived = false;
                    _reviverPlayer = null;

                    if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance.MainPlayer != null)
                    {
                        NotificationManagerClass.DisplayMessageNotification(
                            "Revival interrupted! Stay close to complete the process.",
                            ENotificationDurationType.Default,
                            ENotificationIconType.Alert,
                            Color.red);
                    }

                    yield break;
                }

                // Increment progress
                _revivalProgress += Time.deltaTime;

                // Show progress notification every second
                if (Mathf.Floor(_revivalProgress) > Mathf.Floor(_revivalProgress - Time.deltaTime) && _reviverPlayer.IsYourPlayer)
                {
                    int remainingSeconds = Mathf.CeilToInt(totalReviveTime - _revivalProgress);
                    NotificationManagerClass.DisplayMessageNotification(
                        $"Reviving player... {remainingSeconds}s remaining",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Default,
                        Color.yellow);
                }

                yield return null;
            }

            // Revival complete
            CompleteRevival();
        }

        // Complete the revival process
        private void CompleteRevival()
        {
            if (_downedPlayer == null || _reviverPlayer == null)
                return;

            try
            {
                // Consume the reviver's defibrillator item
                if (!Settings.TESTING.Value)
                {
                    ConsumeRevivalItem(_reviverPlayer);
                }

                // Apply revival to the downed player
                RevivalFeatures.RevivePlayer(_downedPlayer, _reviverPlayer);

                // Show notifications
                if (_reviverPlayer.IsYourPlayer)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "Player successfully revived!",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Default,
                        Color.green);
                }

                // Clean up
                DestroyMarker();
                Destroy(this);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error completing revival: {ex.Message}");
            }
        }

        // Consume the revival item from the reviver
        private void ConsumeRevivalItem(Player reviver)
        {
            var inRaidItems = reviver.Inventory.GetPlayerItems(EPlayerItems.Equipment);
            Item defibItem = inRaidItems.FirstOrDefault(item => item.TemplateId == Constants.Constants.ITEM_ID);

            if (defibItem != null)
            {
                try
                {
                    // Use reflection to access the necessary methods to destroy the item
                    System.Reflection.MethodInfo moveMethod = System.Reflection.MethodBase.GetMethodFromHandle(
                        typeof(InventoryController).GetMethod("ThrowItem").MethodHandle) as System.Reflection.MethodInfo;

                    if (moveMethod != null)
                    {
                        // This will effectively discard the item
                        moveMethod.Invoke(reviver.InventoryController, new object[] { defibItem, false, null });
                        Plugin.LogSource.LogInfo($"Consumed defibrillator item {defibItem.Id} from reviver");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError($"Error consuming reviver's defib item: {ex.Message}");
                }
            }
        }

        // Destroy the marker
        private void DestroyMarker()
        {
            if (_reviveMarker != null)
            {
                Destroy(_reviveMarker);
                _reviveMarker = null;
            }
        }

        private void OnDestroy()
        {
            DestroyMarker();
        }

        // Implementing IPhysicsTrigger interface members
        public void OnTriggerEnter(Collider collider)
        {
            // Implementation for OnTriggerEnter
        }

        public void OnTriggerExit(Collider collider)
        {
            // Implementation for OnTriggerExit
        }

        public string Description => "RevivablePlayerComponent";
    }
}