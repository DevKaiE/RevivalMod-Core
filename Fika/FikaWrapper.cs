using Comfort.Common;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using RevivalMod.Components;
using RevivalMod.Packets;
using System;
using UnityEngine;

namespace RevivalMod.Fika
{
    internal class FikaWrapper
    {
        public static bool IAmHost()
        {
            return Singleton<FikaServer>.Instantiated;
        }

        public static string GetRaidId()
        {
            return FikaBackendUtils.GroupId;
        }

        public static void SendPlayerPositionPacket(string playerId, DateTime timeOfDeath, Vector3 position)
        {
            PlayerPositionPacket packet = new PlayerPositionPacket
            {
                playerId = playerId,
                timeOfDeath = timeOfDeath,
                position = position
            };

            if (Singleton<FikaServer>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as server");
                try
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError(ex.Message);
                }
            }
            else if (Singleton<FikaClient>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as client");
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableSequenced);
            }
            else
            {
                Plugin.LogSource.LogWarning("FikaWrapper: Neither server nor client is instantiated");
            }
        }
        public static void SendRemovePlayerFromCriticalPlayersListPacket(string playerId)
        {
            RemovePlayerFromCriticalPlayersListPacket packet = new RemovePlayerFromCriticalPlayersListPacket()
            {
                playerId = playerId
            };

            if (Singleton<FikaServer>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as server");
                try
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError(ex.Message);
                }
            }
            else if (Singleton<FikaClient>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as client");
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableSequenced);
            }
            else
            {
                Plugin.LogSource.LogWarning("FikaWrapper: Neither server nor client is instantiated");
            }
        }

        private static void OnPlayerPositionPacketReceived(PlayerPositionPacket packet, NetPeer peer)
        {
            Plugin.LogSource.LogDebug($"Packet received: playerId: {packet.playerId}, position: X {packet.position.x}, Y {packet.position.y},  Z {packet.position.z}");
            RMSession.AddToCriticalPlayers(packet.playerId, packet.position);
        }
        private static void OnRemovePlayerFromCriticalPlayersListPacketReceived(RemovePlayerFromCriticalPlayersListPacket packet,  NetPeer peer)
        {
            Plugin.LogSource.LogDebug($"RemovePlayerFromCriticalPlayersListPacket received: {packet.playerId}");
            RMSession.RemovePlayerFromCriticalPlayers(packet.playerId);
        }
        public static void OnFikaNetManagerCreated(FikaNetworkManagerCreatedEvent managerCreatedEvent)
        {
            Plugin.LogSource.LogInfo("FikaWrapper: Registering packet handler");
            managerCreatedEvent.Manager.RegisterPacket<PlayerPositionPacket, NetPeer>(OnPlayerPositionPacketReceived);
            managerCreatedEvent.Manager.RegisterPacket<RemovePlayerFromCriticalPlayersListPacket, NetPeer>(OnRemovePlayerFromCriticalPlayersListPacketReceived);
        }

        public static void InitOnPluginEnabled()
        {
            Plugin.LogSource.LogInfo("FikaWrapper: Subscribing to network manager event");
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetManagerCreated);
        }
    }
}