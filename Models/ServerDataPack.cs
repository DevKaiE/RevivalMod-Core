using RevivalMod.Components;
using RevivalMod.Fika;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevivalMod.Models
{
    internal class ServerDataPack
    {
        public string ProfileId;
        public List<string> playerIdsInRaid = new List<string>();

        public ServerDataPack(string profileId, string mapId, List<string> playerIds = null)
        {
            ProfileId = profileId;
            if (playerIds != null)
            {
                playerIdsInRaid = playerIds;
            }
        }

        public static ServerDataPack GetRequestPack()
        {
            return new ServerDataPack(FikaInterface.GetRaidId(), RMSession.Instance.GameWorld.LocationId.ToLower());
        }
    }
}
