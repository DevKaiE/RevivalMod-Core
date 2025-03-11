using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevivalMod.Components
{
    internal class RMSession : MonoBehaviour
    {
        private RMSession() { }
        private static RMSession _instance = null;

        public Player Player { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public GamePlayerOwner GamePlayerOwner { get; private set; }

        // Dictionary to track players with revival items
        public Dictionary<string, bool> InRaidPlayersWithItem = new Dictionary<string, bool>();

        public static RMSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Singleton<GameWorld>.Instantiated)
                    {
                        Plugin.LogSource.LogError("Can't get ModSession Instance when GameWorld is not instantiated!");
                        // Create a temporary instance for error resistance
                        GameObject go = new GameObject("RMSessionTemp");
                        _instance = go.AddComponent<RMSession>();
                        return _instance;
                    }

                    try
                    {
                        _instance = Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetOrAddComponent<RMSession>();
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogSource.LogError($"Error creating RMSession: {ex.Message}");
                        GameObject go = new GameObject("RMSessionError");
                        _instance = go.AddComponent<RMSession>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            try
            {
                if (Singleton<GameWorld>.Instantiated)
                {
                    GameWorld = Singleton<GameWorld>.Instance;
                    Player = GameWorld.MainPlayer;
                    if (Player != null)
                    {
                        GamePlayerOwner = Player.gameObject.GetComponent<GamePlayerOwner>();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error in RMSession.Awake: {ex.Message}");
            }
        }

        public static void AddToInRaidPlayersWithItem(string playerId, bool hasItem)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Plugin.LogSource.LogError("Tried to add player with null or empty ID");
                return;
            }

            // Allow overwrites for updating item status
            Instance.InRaidPlayersWithItem[playerId] = hasItem;
            Plugin.LogSource.LogInfo($"Player {playerId} item status set to {hasItem}");
        }

        public static bool GetHasPlayerRevivalItem(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Plugin.LogSource.LogError("Tried to check null or empty player ID");
                return false;
            }

            if (!Instance.InRaidPlayersWithItem.ContainsKey(playerId))
            {
                Plugin.LogSource.LogWarning($"No record for player {playerId}, defaulting to false");
                return false;
            }

            return Instance.InRaidPlayersWithItem[playerId];
        }
    }
}