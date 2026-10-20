using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class FloodSettings
    {
        private const string Section = "Flood";

        public FloodSettings(ConfigFile config)
        {
            MaxRpcPerSecond = config.Bind(Section, "MaxRpcPerSecond", 60, "More commands (RPC) than this from one player in one second count as a flood (lag or crash cheats).");
            MaxSpawnsPerSecond = config.Bind(Section, "MaxSpawnsPerSecond", 20, "More network object spawns than this from one player in one second count as a flood.");
            MaxObjectEventsPerSecond = config.Bind(Section, "MaxObjectEventsPerSecond", 15, "More object destroys and ownership changes than this from one player in one second count as a flood.");
            MaxBuildsPerMinute = config.Bind(Section, "MaxBuildsPerMinute", 6, "More placed buildables (flags and similar) than this from one player in one minute count as build spam.");
        }

        public ConfigEntry<int> MaxRpcPerSecond { get; }
        public ConfigEntry<int> MaxSpawnsPerSecond { get; }
        public ConfigEntry<int> MaxObjectEventsPerSecond { get; }
        public ConfigEntry<int> MaxBuildsPerMinute { get; }
    }
}