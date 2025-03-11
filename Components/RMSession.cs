using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static RMSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!Singleton<GameWorld>.Instantiated)
                    {
                        throw new Exception("Can't get ModSession Instance when GameWorld is not instantiated!");
                    }

                    _instance = Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetOrAddComponent<RMSession>();
                }
                return _instance;
            }
        }

        public Dictionary<string, bool> InRaidPlayersWithItem = new();

       

        private void Awake()
        {
            GameWorld = Singleton<GameWorld>.Instance;
            Player = GameWorld.MainPlayer;
            GamePlayerOwner = Player.gameObject.GetComponent<GamePlayerOwner>();
        }

        public static void AddToInRaidPlayersWithItem(string playerId, bool hasItem)
        {
            if (Instance.InRaidPlayersWithItem.ContainsKey(playerId))
            {
                throw new Exception("Tried to add player to PlayerLookUp with a playerId that was already in the dictionary!");
            }

            Instance.InRaidPlayersWithItem[playerId] = hasItem;
        }

        public static bool GetHasPlayerRevivalItem(string playerId)
        {
            return Instance.InRaidPlayersWithItem[playerId];
        }
    }
}
