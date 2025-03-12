using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevivalMod.Constants
{
    public class Constants
    {
        // Change this ID to a defibrillator if available, or keep it as a bandage for testing
        // Defibrillator ID from Escape from Tarkov: "5c052e6986f7746b207bc3c9"
        // Personal medkit ID: "5e99711486f7744bfc4af328"
        // CMS kit ID: "5d02778e86f774203e7dedbe"
        // Bandage ID for testing: "544fb25a4bdc2dfb738b4567"
        public const string ITEM_ID = "5c052e6986f7746b207bc3c9"; // Using bandage for testing purposes

        // Set to true for testing without requiring an actual defibrillator item
        public const bool TESTING = false;
        public const bool SELF_REVIVAL = true;
    }
}