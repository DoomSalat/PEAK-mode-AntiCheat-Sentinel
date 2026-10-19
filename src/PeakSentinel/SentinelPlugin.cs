using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PeakSentinel.Detection;
using PeakSentinel.Patches;
using PeakSentinel.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakSentinel
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class SentinelPlugin : BaseUnityPlugin
    {
        public const string Guid = "peaksentinel";
        public const string Name = "Peak Sentinel";
        public const string Version = "0.1.0";

        private const string DebugSection = "Debug";
        private const string ManagerSection = "Manager";
        private const string TraceFile = "PeakSentinel.log";

        private Harmony _harmony;
        private DebugTrace _trace;

        private void Awake()
        {
            ConfigEntry<bool> traceEnabled = Config.Bind(DebugSection, "TraceFile", true, $"Write a debug trace to BepInEx/{TraceFile}: every item RPC (sender, view owner, arguments), every item created over the network and every finding.");
            ConfigEntry<bool> traceAllRpcs = Config.Bind(DebugSection, "TraceAllRpcs", false, "Also write every other RPC to the trace (very noisy, for finding new cheat paths).");
            _trace = new DebugTrace(Path.Combine(Paths.BepInExRootPath, TraceFile), traceEnabled, traceAllRpcs);
            ProtectionSettings protection = new ProtectionSettings(Config);
            SuspicionReport report = new SuspicionReport(Logger, _trace);
            SentinelHooks.Report = report;
            SentinelHooks.Rpc = new RpcAudit(report, _trace, protection);
            SentinelHooks.Instantiate = new InstantiateAudit(report, _trace);
            GameObject host = new GameObject(Name);
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<VersionBadge>().Initialize($"{Name} {Version}");
            SentinelWindow window = host.AddComponent<SentinelWindow>();
            window.Initialize($"{Name} {Version}", Config.Bind(ManagerSection, "ToggleKey", Key.F6, "Key that opens or closes the Sentinel manager window: findings and protection switches (None = no key)."), protection);
            report.Window = window;
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
    }
}
