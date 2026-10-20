using PeakSentinel.Detection;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Network;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Ui;

namespace PeakSentinel.Patches
{
    public static class SentinelHooks // Static: Harmony patches are static and reach the mod only through statics
    {
        public static SuspicionReport Report { get; set; } // Set: assigned once by SentinelPlugin
        public static FindingLog Findings { get; set; } // Set: assigned once by SentinelPlugin, read by the pause menu badges
        public static RpcRouter Router { get; set; } // Set: assigned once by SentinelPlugin
        public static InstantiateAudit Instantiate { get; set; } // Set: assigned once by SentinelPlugin
        public static HandGuard Hands { get; set; } // Set: assigned once by SentinelPlugin, told about own outgoing pickup requests
        public static EnvironmentMarks Environment { get; set; } // Set: assigned once by SentinelPlugin, filled by the environment patches
        public static SentinelWindow Window { get; set; } // Set: assigned once by SentinelPlugin, frees the cursor while open
    }
}