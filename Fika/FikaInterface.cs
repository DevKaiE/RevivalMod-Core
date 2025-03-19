using Comfort.Common;
using EFT;
using RevivalMod;
using System;
using UnityEngine;

namespace RevivalMod.Fika
{
    internal class FikaInterface
    {
        public static bool IAmHost()
        {
            if (!Plugin.FikaInstalled) return true;
            return FikaWrapper.IAmHost();
        }

        public static string GetRaidId()
        {
            if (!Plugin.FikaInstalled) return Singleton<GameWorld>.Instance.MainPlayer.ProfileId;
            return FikaWrapper.GetRaidId();
        }

        public static void InitOnPluginEnabled()
        {
            if (!Plugin.FikaInstalled) return;
            FikaWrapper.InitOnPluginEnabled();
        }

        public static void SendPlayerPositionPacket(string playerId, DateTime timeOfDeath, Vector3 position)
        {
            if(!Plugin.FikaInstalled) return;
            FikaWrapper.SendPlayerPositionPacket(playerId, timeOfDeath, position);
        }

        public static void SendRemovePlayerFromCriticalPlayersListPacket(string playerId)
        {
            if (!Plugin.FikaInstalled) return;
            FikaWrapper.SendRemovePlayerFromCriticalPlayersListPacket(playerId);
        }


    }
}
