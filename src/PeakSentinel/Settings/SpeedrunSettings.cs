using BepInEx.Configuration;
using PeakSentinel.Speedrun;

namespace PeakSentinel.Settings
{
    public sealed class SpeedrunSettings
    {
        private const string Section = "Speedrun";

        public SpeedrunSettings(ConfigFile config)
        {
            Category = config.Bind(Section, "Category", RunCategory.NoMajorGlitches, "Speedrun category preset. NoMajorGlitches: every check. Glitched and Any: flight, speed and fall damage checks on yourself are off, because flight glitches and Glider Rest are allowed there.");
            TrackLuck = config.Bind(Section, "TrackLuck", false, "Log every chest and map item rolled while you are the host with the game's own odds, show the loot luck of this run and of today and the mushroom table of the map.");
            CheckIntegrity = config.Bind(Section, "CheckIntegrity", false, "Check the game while you play: other mods patching loot and randomness, code loaded from outside the game and BepInEx, fingerprints of the game DLL, loot tables and movement constants, game speed and time scale.");
            CheckMyself = config.Bind(Section, "CheckMyself", false, "Run Sentinel's flight, speed and stamina checks on yourself too, and check that a hard landing really hurts you.");
            RecordRun = config.Bind(Section, "RecordRun", false, "Keep a speedrun journal file per day: every attempt with its number, time and run ID, chests and alerts, chained by hashes so lines cannot be edited or removed unnoticed.");
            ShowOverlay = config.Bind(Section, "ShowOverlay", false, "Show the speedrun overlay in the top right corner: loot luck with a clover, Sentinel version, attempt number, run ID and the integrity state.");
        }

        public ConfigEntry<RunCategory> Category { get; }
        public ConfigEntry<bool> TrackLuck { get; }
        public ConfigEntry<bool> CheckIntegrity { get; }
        public ConfigEntry<bool> CheckMyself { get; }
        public ConfigEntry<bool> RecordRun { get; }
        public ConfigEntry<bool> ShowOverlay { get; }
    }
}
