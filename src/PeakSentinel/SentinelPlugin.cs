using System.IO;
using BepInEx;
using HarmonyLib;
using PeakSentinel.Detection;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Detection.Stamina;
using PeakSentinel.Network;
using PeakSentinel.Patches;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using PeakSentinel.Ui;
using UnityEngine;

namespace PeakSentinel
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class SentinelPlugin : BaseUnityPlugin
    {
        public const string Guid = "peaksentinel";
        public const string Name = "Peak Sentinel";
        public const string Version = "0.2.0";

        private const string TraceFile = "PeakSentinel.log";
        private const string BansFile = "PeakSentinel.bans.txt";

        private Harmony _harmony;
        private DebugTrace _trace;

        private void Awake()
        {
            DebugSettings debug = new DebugSettings(Config, TraceFile);
            ManagerSettings manager = new ManagerSettings(Config);
            ProtectionSettings protection = new ProtectionSettings(Config);
            DetectionSettings detection = new DetectionSettings(Config);
            OtherSettings other = new OtherSettings(Config);

            _trace = new DebugTrace(Path.Combine(Paths.BepInExRootPath, TraceFile), debug.TraceFile, debug.TraceAllRpcs);
            FindingLog findings = new FindingLog(manager.LogEntries);
            SuspicionReport report = new SuspicionReport(Logger, _trace, findings);
            SentinelHooks.Findings = findings;
            BanList bans = new BanList(Path.Combine(Paths.ConfigPath, BansFile));
            GameObject host = new GameObject(Name) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            EffectAudit effects = host.AddComponent<EffectAudit>();
            effects.Initialize(report, _trace, detection);
            WireNetwork(report, protection, detection, other, effects);
            host.AddComponent<HostGuard>().Initialize(report, other);
            BanGuard banGuard = host.AddComponent<BanGuard>();
            banGuard.Initialize(report, bans);
            host.AddComponent<CheatModScan>().Initialize(report, detection);
            host.AddComponent<FlightAudit>().Initialize(report, _trace, detection, new FlightSettings(Config), new FlightExemptions(new RopeLinks(), SentinelHooks.Environment));
            host.AddComponent<StaminaAudit>().Initialize(report, _trace, detection, new StaminaSettings(Config));
            SentinelWindow window = host.AddComponent<SentinelWindow>();
            window.Initialize($"{Name} {Version}", manager, protection, detection, other, findings, report, bans, banGuard);
            SentinelHooks.Window = window;

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(SentinelPlugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded: block cheat spawns {protection.BlockCheatSpawns.Value}, trace in {_trace.Path}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _trace?.Dispose();
        }

        private void WireNetwork(SuspicionReport report, ProtectionSettings protection, DetectionSettings detection, OtherSettings other, EffectAudit effects)
        {
            HandGuard hands = new HandGuard(report, protection);
            SentinelHooks.Report = report;
            SentinelHooks.Environment = new EnvironmentMarks();
            SentinelHooks.Hands = hands;
            SentinelHooks.Instantiate = new InstantiateAudit(report, _trace, detection);
            SentinelHooks.Router = new RpcRouter(_trace,
                new ItemRpcAudit(report, protection),
                hands,
                new KickShield(report, other),
                new HarmAudit(report, detection),
                new WorldAudit(report, detection),
                effects);
        }
    }
}
