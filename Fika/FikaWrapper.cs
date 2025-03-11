using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using RevivalMod.Components;
using RevivalMod.Packets;
using RevivalMod;

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
            RevivalItemInPlayerRaidInventoryPacket packet = new RevivalItemInPlayerRaidInventoryPacket
            {
               playerId = playerId,
               hasItem = hasItem
            };
            if (Singleton<FikaServer>.Instantiated)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
            if (Singleton<FikaClient>.Instantiated)
            {
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        private static void OnRevivalItemInPlayerRaidInventoryPacketReceived(RevivalItemInPlayerRaidInventoryPacket packet, NetPeer peer)
        {
            RMSession.AddToInRaidPlayersWithItem(packet.playerId, packet.hasItem);

           

            if (Singleton<FikaServer>.Instantiated)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public static void OnFikaNetManagerCreated(FikaNetworkManagerCreatedEvent managerCreatedEvent)
        {
            managerCreatedEvent.Manager.RegisterPacket<RevivalItemInPlayerRaidInventoryPacket, NetPeer>(OnRevivalItemInPlayerRaidInventoryPacketReceived);
        }

        public static void InitOnPluginEnabled()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetManagerCreated);
        }
    }
}
