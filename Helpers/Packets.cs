using Fika.Core.Networking;
using LiteNetLib.Utils;
using UnityEngine;

namespace RevivalMod.Packets
{
    public struct RevivalItemInPlayerRaidInventoryPacket : INetSerializable
    {
        public bool hasItem;
        public string playerId;

        public void Deserialize(NetDataReader reader)
        {
            hasItem = reader.GetBool();
            playerId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(hasItem);
            writer.Put(playerId);
        }
    }

}
