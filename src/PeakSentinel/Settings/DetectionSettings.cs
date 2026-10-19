using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class DetectionSettings
    {
        private const string Section = "Cheat detection";

        public DetectionSettings(ConfigFile config)
        {
            HarmToOthers = config.Bind(Section, "HarmToOthers", true, "Report a player who kills, knocks out, zombifies, petrifies, teleports or hard-flings another player with a command.");
            BlockHarmToOthers = config.Bind(Section, "BlockHarmToOthers", true, "Refuse such commands on this client (the victim stays unharmed here; it protects you fully when you are the victim).");
            HostOnlyEvents = config.Bind(Section, "HostOnlyEvents", true, "Report a non-host who triggers events only the host may send: end of game, revive, helicopter, fog, campfire, bridges, eruption, beans, ropes, pitons, bees, foreign banana or ghost.");
            BlockHostOnlyEvents = config.Bind(Section, "BlockHostOnlyEvents", true, "Refuse such events on this client.");
            DangerousSpawns = config.Bind(Section, "DangerousSpawns", true, "Report a non-host who spawns bee swarms, tornados or Bing Bong effects.");
            BlockDangerousSpawns = config.Bind(Section, "BlockDangerousSpawns", true, "Do not create such objects on this client.");
            Flight = config.Bind(Section, "Flight", true, "Report a player who stays in the air without gravity acting on them (fly cheats). Thresholds are in the [Flight] section.");
            BlockFlight = config.Bind(Section, "BlockFlight", false, "As host, teleport a detected flyer back to where they last stood and knock them down for a moment. Off by default until the thresholds are tuned: a false detection would snap an honest player back too.");
            Stamina = config.Bind(Section, "Stamina", true, "Report a player whose stamina, as their client sends it, does not go down while they sprint or climb (infinite stamina cheats). Thresholds are in the [Stamina] section. Cannot be blocked: stamina lives on their client.");
            SourcelessEffects = config.Bind(Section, "SourcelessEffects", true, "Report a player who gets protection or infinite stamina (Invincibility, golden Bing Bong shield, InfiniteStamina) without a source: nothing eaten just before and no golden Bing Bong in hand. Cannot be blocked: effects live on their client.");
            CheatModMarkers = config.Bind(Section, "CheatModMarkers", true, "Report players marked by the Cherry or Atlas cheat mods (same check as PEAKER).");
            KickCheatModUsers = config.Bind(Section, "KickCheatModUsers", true, "As host, kick players marked by the Cherry or Atlas cheat mods.");
        }

        public ConfigEntry<bool> HarmToOthers { get; }
        public ConfigEntry<bool> BlockHarmToOthers { get; }
        public ConfigEntry<bool> HostOnlyEvents { get; }
        public ConfigEntry<bool> BlockHostOnlyEvents { get; }
        public ConfigEntry<bool> DangerousSpawns { get; }
        public ConfigEntry<bool> BlockDangerousSpawns { get; }
        public ConfigEntry<bool> Flight { get; }
        public ConfigEntry<bool> BlockFlight { get; }
        public ConfigEntry<bool> Stamina { get; }
        public ConfigEntry<bool> SourcelessEffects { get; }
        public ConfigEntry<bool> CheatModMarkers { get; }
        public ConfigEntry<bool> KickCheatModUsers { get; }
    }
}
