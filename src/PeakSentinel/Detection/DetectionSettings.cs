using BepInEx.Configuration;

namespace PeakSentinel.Detection
{
    public sealed class DetectionSettings
    {
        private const string Section = "Cheat detection";

        public DetectionSettings(ConfigFile config)
        {
            Harm = config.Bind(Section, "HarmToOthers", true, "Report a player who kills, knocks out, zombifies, petrifies, teleports or hard-flings another player with a command.");
            BlockHarm = config.Bind(Section, "BlockHarmToOthers", true, "Refuse such commands on this client (the victim stays unharmed here; it protects you fully when you are the victim).");
            WorldEvents = config.Bind(Section, "HostOnlyEvents", true, "Report a non-host who triggers events only the host may send: end of game, revive, helicopter, fog, campfire, bridges, eruption, beans, ropes, pitons, bees, foreign banana or ghost.");
            BlockWorldEvents = config.Bind(Section, "BlockHostOnlyEvents", true, "Refuse such events on this client.");
            DangerousSpawns = config.Bind(Section, "DangerousSpawns", true, "Report a non-host who spawns bee swarms, tornados or Bing Bong effects.");
            BlockDangerousSpawns = config.Bind(Section, "BlockDangerousSpawns", true, "Do not create such objects on this client.");
            Flight = config.Bind(Section, "Flight", true, "Report a player who stays in the air without gravity acting on them (fly cheats). Thresholds are in the [Flight] section.");
            BlockFlight = config.Bind(Section, "BlockFlight", false, "As host, teleport a detected flyer back to where they last stood and knock them down for a moment. Off by default until the thresholds are tuned: a false detection would snap an honest player back too.");
            CheatMods = config.Bind(Section, "CheatModMarkers", true, "Report players marked by the Cherry or Atlas cheat mods (same check as PEAKER).");
            KickCheatMods = config.Bind(Section, "KickCheatModUsers", true, "As host, kick players marked by the Cherry or Atlas cheat mods.");
        }

        public ConfigEntry<bool> Harm { get; }
        public ConfigEntry<bool> BlockHarm { get; }
        public ConfigEntry<bool> WorldEvents { get; }
        public ConfigEntry<bool> BlockWorldEvents { get; }
        public ConfigEntry<bool> DangerousSpawns { get; }
        public ConfigEntry<bool> BlockDangerousSpawns { get; }
        public ConfigEntry<bool> Flight { get; }
        public ConfigEntry<bool> BlockFlight { get; }
        public ConfigEntry<bool> CheatMods { get; }
        public ConfigEntry<bool> KickCheatMods { get; }
    }
}
