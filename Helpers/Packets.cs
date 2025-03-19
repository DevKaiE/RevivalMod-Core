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

    public struct RemovePlayerFromCriticalPlayersListPacket : INetSerializable
    {
        public string playerId;
        public void Deserialize(NetDataReader reader)
        {
            playerId = reader.GetString();
        }
        public void Serialize(NetDataWriter writer) {
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
            // Make sure we read in the same order as we write in Serialize
            playerId = reader.GetString();

            // Properly handle DateTime serialization
            try
            {
                timeOfDeath = DateTime.FromBinary(reader.GetLong());
            }
            catch (Exception)
            {
                // Fallback if DateTime deserialization fails
                timeOfDeath = DateTime.UtcNow;
            }

            // Deserialize Vector3 with proper error handling
            float x = reader.GetFloat();
            float y = reader.GetFloat();
            float z = reader.GetFloat();
            position = new Vector3(x, y, z);
        }

        public void Serialize(NetDataWriter writer)
        {
            // Make sure we write in the same order as we read in Deserialize
            writer.Put(playerId ?? string.Empty); // Avoid null reference

            // Properly handle DateTime serialization
            try
            {
                writer.Put(timeOfDeath.ToBinary());
            }
            catch (Exception)
            {
                // Fallback if DateTime serialization fails
                writer.Put(DateTime.UtcNow.ToBinary());
            }

            // Serialize Vector3
            writer.Put(position.x);
            writer.Put(position.y);
            writer.Put(position.z);
        }
    }

}
