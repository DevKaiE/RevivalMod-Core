﻿using Comfort.Common;
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

        public static void SendItemInRaidInventoryPacket(string playerId, bool hasItem)
        {
            Plugin.LogSource.LogInfo($"FikaWrapper: Sending packet for player {playerId}, has item: {hasItem}");

            RevivalItemInPlayerRaidInventoryPacket packet = new RevivalItemInPlayerRaidInventoryPacket
            {
                playerId = playerId,
                hasItem = hasItem
            };

            if (Singleton<FikaServer>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as server");
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
            else if (Singleton<FikaClient>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Sending as client");
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Plugin.LogSource.LogWarning("FikaWrapper: Neither server nor client is instantiated");
            }
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
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableSequenced);
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

        private static void OnRevivalItemInPlayerRaidInventoryPacketReceived(RevivalItemInPlayerRaidInventoryPacket packet, NetPeer peer)
        {
            Plugin.LogSource.LogInfo($"FikaWrapper: Received packet for player {packet.playerId}, has item: {packet.hasItem}");

            try
            {
                RMSession.AddToInRaidPlayersWithItem(packet.playerId, packet.hasItem);
                Plugin.LogSource.LogInfo($"FikaWrapper: Added player {packet.playerId} to session");
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"FikaWrapper: Error processing packet: {ex.Message}");
            }

            // Only forward if we're the server
            if (Singleton<FikaServer>.Instantiated)
            {
                Plugin.LogSource.LogInfo("FikaWrapper: Forwarding packet as server");
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        private static void OnPlayerPositionPacketReceived(PlayerPositionPacket packet, NetPeer peer)
        {
            Plugin.LogSource.LogDebug($"Packet received: playerId: {packet.playerId}, position: X {packet.position.x}, Y {packet.position.y},  Z {packet.position.z}");
        }

        public static void OnFikaNetManagerCreated(FikaNetworkManagerCreatedEvent managerCreatedEvent)
        {
            Plugin.LogSource.LogInfo("FikaWrapper: Registering packet handler");
            managerCreatedEvent.Manager.RegisterPacket<RevivalItemInPlayerRaidInventoryPacket, NetPeer>(OnRevivalItemInPlayerRaidInventoryPacketReceived);
            managerCreatedEvent.Manager.RegisterPacket<PlayerPositionPacket, NetPeer>(OnPlayerPositionPacketReceived);
        }

        public static void InitOnPluginEnabled()
        {
            Plugin.LogSource.LogInfo("FikaWrapper: Subscribing to network manager event");
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetManagerCreated);
        }
    }
}