using Comfort.Common;
using EFT;
using RevivalMod;

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

        public static void SendItemInRaidInventoryPacket(string playerId, bool hasItem)
        {
            if (!Plugin.FikaInstalled) return;
            FikaWrapper.SendItemInRaidInventoryPacket(playerId, hasItem);
        }
    }
}
