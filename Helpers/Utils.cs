using Newtonsoft.Json;
using Comfort.Common;
using SPT.Common.Http;
using System.Collections.Generic;
using EFT;

namespace RevivalMod.Helpers
{
    internal class Utils
    {
        public static T ServerRoute<T>(string url, object data = default(object))
        {
            string json = JsonConvert.SerializeObject(data);
            var req = RequestHandler.PostJson(url, json);
            return JsonConvert.DeserializeObject<T>(req);
        }
        public static string ServerRoute(string url, object data = default(object))
        {
            string json;
            if (data is string)
            {
                Dictionary<string, string> dataDict = new Dictionary<string, string>();
                dataDict.Add("data", (string)data);
                json = JsonConvert.SerializeObject(dataDict);
            }
            else
            {
                json = JsonConvert.SerializeObject(data);
            }

            return RequestHandler.PutJson(url, json);
        }

        public static Player GetYourPlayer() {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player == null) return null;          
            if (!player.IsYourPlayer) return null;
            return player;
        }

    }
}
