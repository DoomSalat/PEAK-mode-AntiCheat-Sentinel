using PeakSentinel.Detection;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Network;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Speedrun;
using PeakSentinel.Ui;

namespace PeakSentinel.Patches
{
    public static class SentinelHooks // Static: Harmony patches are static and reach the mod only through statics
    {
        public static SuspicionReport Report { get; set; } // Set: assigned once by SentinelPlugin
        public static FindingLog Findings { get; set; } // Set: assigned once by SentinelPlugin, read by the pause menu badges
        public static MuteList Mutes { get; set; } // Set: assigned once by SentinelPlugin, read by the pause menu badges
        public static RpcRouter Router { get; set; } // Set: assigned once by SentinelPlugin
        public static PacketGate Gate { get; set; } // Set: assigned once by SentinelPlugin, sees every incoming Photon event first
        public static InstantiateAudit Instantiate { get; set; } // Set: assigned once by SentinelPlugin
        public static InvisibilityAudit Invisibility { get; set; } // Set: assigned once by SentinelPlugin, fixes incoming body positions
        public static BodySyncGuard BodySync { get; set; } // Set: assigned once by SentinelPlugin, drops broken incoming body positions
        public static HandGuard Hands { get; set; } // Set: assigned once by SentinelPlugin, told about own outgoing pickup requests
        public static CookingEchoes Cooking { get; set; } // Set: assigned once by SentinelPlugin, filled by the cooking patch
        public static LootLedger Loot { get; set; } // Set: assigned once by SentinelPlugin, filled by the loot roll patch
        public static EnvironmentMarks Environment { get; set; } // Set: assigned once by SentinelPlugin, filled by the environment patches
        public static PeakPortals Portals { get; set; } // Set: assigned once by SentinelPlugin, filled by the peak portal patch
        public static EnvironmentMarks Traps { get; set; } // Set: assigned once by SentinelPlugin, filled by the trap patches
        public static SentinelWindow Window { get; set; } // Set: assigned once by SentinelPlugin, frees the cursor while open
    }
}