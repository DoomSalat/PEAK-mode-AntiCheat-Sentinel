using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class DebugSettings
    {
        private const string Section = "Debug";

        public DebugSettings(ConfigFile config, string traceFile)
        {
            TraceFile = config.Bind(Section, "TraceFile", true, $"Write a debug trace to BepInEx/{traceFile}: every item RPC (sender, view owner, arguments), every item created over the network and every finding.");
            TraceAllRpcs = config.Bind(Section, "TraceAllRpcs", false, "Also write every other RPC to the trace (very noisy, for finding new cheat paths).");
        }

        public ConfigEntry<bool> TraceFile { get; }
        public ConfigEntry<bool> TraceAllRpcs { get; }
    }
}