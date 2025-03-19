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
        public Dictionary<string, Vector3> CriticalPlayers = new Dictionary<string, Vector3>();

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

        public static void AddToCriticalPlayers(string playerId, Vector3 position)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Plugin.LogSource.LogError("Tried to add player with null or empty ID");
                return;
            }

            // Allow overwrites for updating item status
            Instance.CriticalPlayers[playerId] = position;
            Plugin.LogSource.LogInfo($"Player {playerId} added to critical players.");
        }

        public static void RemovePlayerFromCriticalPlayers(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            Instance.CriticalPlayers.Remove(playerId);
            Plugin.LogSource.LogInfo($"Player {playerId} removed from critical players.");
        }

        public static Vector3 GetPosition(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) { return Vector3.zero; }
            return Instance.CriticalPlayers[playerId];
        }

        public static Dictionary<string, Vector3> GetCriticalPlayers()
        {
            return Instance.CriticalPlayers;
        }
    }
}