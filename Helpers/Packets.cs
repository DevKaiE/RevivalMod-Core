using Fika.Core.Networking;
using LiteNetLib.Utils;
using System;
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

    public struct PlayerPositionPacket : INetSerializable
    {
        public string playerId;
        public DateTime timeOfDeath;
        public Vector3 position;

        public void Deserialize(NetDataReader reader)
        {
            playerId = reader.GetString();
            timeOfDeath = DateTime.FromBinary(reader.GetLong());
            position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(playerId);
            writer.Put(timeOfDeath.ToBinary());
            writer.Put(position.x);
            writer.Put(position.y);
            writer.Put(position.z);
        }
    }

}
