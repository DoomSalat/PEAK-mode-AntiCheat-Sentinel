using PeakSentinel.Compat;
using PeakSentinel.Network;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Speedrun;

namespace PeakSentinel.Ui
{
    public sealed class WindowServices
    {
        public WindowServices(FindingLog findings, SuspicionReport report, BanList bans, BanGuard banGuard, MuteList mutes, ReportExport export, LatencyMeter latency, DesyncFixer desync, SentinelPresence presence, LootLedger loot, TrueAscentState ascent, SpeedrunMonitor speedrun)
        {
            Speedrun = speedrun;
            Ascent = ascent;
            Loot = loot;
            Findings = findings;
            Report = report;
            Bans = bans;
            BanGuard = banGuard;
            Mutes = mutes;
            Export = export;
            Latency = latency;
            Desync = desync;
            Presence = presence;
        }

        public FindingLog Findings { get; }
        public SuspicionReport Report { get; }
        public BanList Bans { get; }
        public BanGuard BanGuard { get; }
        public MuteList Mutes { get; }
        public ReportExport Export { get; }
        public LatencyMeter Latency { get; }
        public DesyncFixer Desync { get; }
        public SentinelPresence Presence { get; }
        public LootLedger Loot { get; }
        public TrueAscentState Ascent { get; }
        public SpeedrunMonitor Speedrun { get; }
    }
}