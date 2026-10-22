using System.IO;
using BepInEx;
using HarmonyLib;
using PeakSentinel.Compat;
using PeakSentinel.Detection;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Detection.Stamina;
using PeakSentinel.Network;
using PeakSentinel.Patches;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using PeakSentinel.Speedrun;
using PeakSentinel.Ui;
using UnityEngine;

namespace PeakSentinel
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class SentinelPlugin : BaseUnityPlugin
    {
        public const string Guid = "peaksentinel";
        public const string Name = "Peak Sentinel";
        public const string Version = "1.0.6";

        private const string TraceFile = "PeakSentinel.log";
        private const string BansFile = "PeakSentinel.bans.txt";

        private Harmony _harmony;
        private DebugTrace _trace;

        private void Awake()
        {
            SentinelSettings settings = new SentinelSettings(Config, TraceFile);
            _trace = new DebugTrace(Path.Combine(Paths.BepInExRootPath, TraceFile), settings.Debug.TraceFile, settings.Debug.TraceAllRpcs);
            FindingLog findings = new FindingLog(settings.Manager.LogEntries);
            SuspicionReport report = new SuspicionReport(Logger, _trace, findings);
            SentinelHooks.Findings = findings;
            SentinelHooks.Report = report;
            SentinelHooks.Environment = new EnvironmentMarks();

            GameObject host = new GameObject(Name) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            ForeignObjects objects = new ForeignObjects();
            StateLedger ledger = new StateLedger(objects);
            MuteList mutes = new MuteList(report, ledger);
            LatencyMeter latency = new LatencyMeter();
            SpawnClock spawns = host.AddComponent<SpawnClock>();
            TrueAscentState ascent = new TrueAscentState();

            GuardPackets(settings, report, mutes, latency);
            AuditRpcs(host, settings, report, spawns, ledger, objects, ascent);
            WindowServices services = Watch(host, settings, report, findings, mutes, latency, ledger, spawns, ascent);
            OpenWindow(host, settings, services);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(SentinelPlugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded: block cheat spawns {settings.Protection.BlockCheatSpawns.Value}, trace in {_trace.Path}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _trace?.Dispose();
        }

        private void GuardPackets(SentinelSettings settings, SuspicionReport report, MuteList mutes, LatencyMeter latency)
        {
            SentinelHooks.Mutes = mutes;
            SentinelHooks.Gate = new PacketGate(report, settings.Detection, settings.Flood, mutes, latency);
        }

        private void AuditRpcs(GameObject host, SentinelSettings settings, SuspicionReport report, SpawnClock spawns, StateLedger ledger, ForeignObjects objects, TrueAscentState ascent)
        {
            DetectionSettings detection = settings.Detection;
            ItemLocator locator = new ItemLocator();
            CookingEchoes cooking = new CookingEchoes();
            LegitForces forces = new LegitForces(locator, cooking);
            AreaEffects areas = new AreaEffects(locator);
            ItemRpcAudit items = new ItemRpcAudit(report, settings.Protection, spawns, new BackpackSwap());
            HandGuard hands = new HandGuard(report, settings.Protection);
            EffectAudit effects = host.AddComponent<EffectAudit>();
            effects.Initialize(report, _trace, detection, items, areas, locator, ascent, spawns);
            StatusAudit statuses = host.AddComponent<StatusAudit>();
            statuses.Initialize(report, detection, locator, ascent);

            SentinelHooks.Hands = hands;
            SentinelHooks.Cooking = cooking;
            SentinelHooks.Instantiate = new InstantiateAudit(report, _trace, detection, settings.Protection, items, objects, locator, ascent);
            SentinelHooks.Router = new RpcRouter(_trace, ledger,
                items,
                hands,
                new KickShield(report, settings.Other, ascent),
                new HarmAudit(report, detection, locator, forces, cooking, ascent),
                new WorldAudit(report, detection),
                new ControlAudit(report, detection),
                new CarryAudit(report, detection),
                new BuildAudit(report, detection, settings.Flood),
                new ReviveAudit(report, detection, spawns, ascent),
                new PushAudit(report, detection, forces),
                areas,
                effects,
                statuses);
        }

        private WindowServices Watch(GameObject host, SentinelSettings settings, SuspicionReport report, FindingLog findings, MuteList mutes, LatencyMeter latency, StateLedger ledger, SpawnClock spawns, TrueAscentState ascent)
        {
            BanList bans = new BanList(Path.Combine(Paths.ConfigPath, BansFile));
            host.AddComponent<HostGuard>().Initialize(report, settings.Other);
            SentinelPresence presence = host.AddComponent<SentinelPresence>();
            presence.Initialize(report, Version);
            DesyncFixer desync = host.AddComponent<DesyncFixer>();
            desync.Initialize(report, settings.Other, ledger);
            BanGuard banGuard = host.AddComponent<BanGuard>();
            banGuard.Initialize(report, bans);
            host.AddComponent<CheatModScan>().Initialize(report, settings.Detection);
            host.AddComponent<IdentityAudit>().Initialize(report, settings.Detection);
            FlightAudit flight = host.AddComponent<FlightAudit>();
            flight.Initialize(report, _trace, settings.Detection, settings.Flight, new FlightExemptions(new RopeLinks(), SentinelHooks.Environment, spawns, ascent));
            StaminaAudit stamina = host.AddComponent<StaminaAudit>();
            stamina.Initialize(report, _trace, settings.Detection, settings.Stamina, ascent);
            LootLedger loot = new LootLedger(settings.Speedrun.TrackLuck);
            SentinelHooks.Loot = loot;
            SpeedrunMonitor speedrun = host.AddComponent<SpeedrunMonitor>();
            speedrun.Initialize(settings.Speedrun, report, loot, new RunJournal(Paths.BepInExRootPath, settings.Speedrun.RecordRun), flight, stamina);
            return new WindowServices(findings, report, bans, banGuard, mutes, new ReportExport(Paths.BepInExRootPath, findings), latency, desync, presence, loot, ascent, speedrun);
        }

        private void OpenWindow(GameObject host, SentinelSettings settings, WindowServices services)
        {
            new ShieldGlyph().Register();
            SentinelWindow window = host.AddComponent<SentinelWindow>();
            window.Initialize($"{Name} {Version}", settings, services);
            SentinelHooks.Window = window;
            host.AddComponent<SpeedrunOverlay>().Initialize(services.Loot, services.Speedrun, settings.Speedrun, Version);
        }
    }
}
