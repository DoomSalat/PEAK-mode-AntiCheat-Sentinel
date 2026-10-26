using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class DetectionSettings
    {
        private const string Section = "Cheat detection";

        public DetectionSettings(ConfigFile config)
        {
            HarmToOthers = config.Bind(Section, "HarmToOthers", true, "Report a player who kills, knocks out, zombifies, petrifies, teleports, hard-flings, keeps pushing or blasts another player with a command.");
            BlockHarmToOthers = config.Bind(Section, "BlockHarmToOthers", true, "Refuse such commands on this client (the victim stays unharmed here; it protects you fully when you are the victim).");
            HostOnlyEvents = config.Bind(Section, "HostOnlyEvents", true, "Report a non-host who triggers events only the host may send: end of game, revive, helicopter, fog, campfire, bridges, eruption, beans, ropes, pitons, bees, foreign banana or ghost.");
            BlockHostOnlyEvents = config.Bind(Section, "BlockHostOnlyEvents", true, "Refuse such events on this client.");
            CheckMyself = config.Bind(Section, "CheckMyself", true, "Check yourself too: your own commands, spawns, flight, stamina, effects, statuses, cheat-mod markers and name go through the same enabled checks as other players. Only an orange self-check warning, nothing of yours is ever blocked.");
            BlockRevivesOfMe = config.Bind(Section, "BlockRevivesOfMe", true, "Refuse a revive of you with no source (a cheat revive), on its own switch. Off: you are revived and it is only logged.");
            DangerousSpawns = config.Bind(Section, "DangerousSpawns", true, "Report a non-host who spawns bee swarms, tornados, mushroom zombies, the Scoutmaster or Bing Bong effects.");
            BlockDangerousSpawns = config.Bind(Section, "BlockDangerousSpawns", true, "Do not create such objects on this client.");
            Flight = config.Bind(Section, "Flight", true, "Report a player who stays in the air without gravity acting on them (fly cheats). Thresholds are in the [Flight] section.");
            BlockFlight = config.Bind(Section, "BlockFlight", false, "As host, teleport a detected flyer back to where they last stood and knock them down for a moment. Off by default until the thresholds are tuned: a false detection would snap an honest player back too.");
            Stamina = config.Bind(Section, "Stamina", true, "Report a player whose stamina, as their client sends it, does not go down while they sprint or climb (infinite stamina cheats). Thresholds are in the [Stamina] section. Cannot be blocked: stamina lives on their client.");
            SourcelessEffects = config.Bind(Section, "SourcelessEffects", true, "Report a player who gets protection or infinite stamina (Invincibility, golden Bing Bong shield, InfiniteStamina) without a source: nothing eaten just before and no golden Bing Bong in hand. Cannot be blocked: effects live on their client.");
            ForcedActions = config.Bind(Section, "ForcedActions", true, "Report a player who makes another player jump, crouch, let go of a wall or rope, drop items, emote, look dead or change statuses.");
            BlockForcedActions = config.Bind(Section, "BlockForcedActions", true, "Ignore such commands on your side.");
            ObjectDestroy = config.Bind(Section, "ObjectDestroy", true, "Report a player who destroys an object that belongs to another player.");
            BlockObjectDestroy = config.Bind(Section, "BlockObjectDestroy", true, "Ignore such destroys on your side.");
            NameSpoofing = config.Bind(Section, "NameSpoofing", true, "Report a player whose in-game name differs from their Steam name, who renames mid-game, or who uses another player's Steam ID.");
            BuildSpam = config.Bind(Section, "BuildSpam", true, "Report a player who builds with someone else's item or builds more than the [Flood] limit (flag spam).");
            BlockBuildSpam = config.Bind(Section, "BlockBuildSpam", true, "Ignore such builds on your side.");
            Flood = config.Bind(Section, "Flood", true, "Report a player who sends more commands or spawns per second than the [Flood] limits (lag and crash cheats).");
            MuteFlooders = config.Bind(Section, "MuteFlooders", false, "Mute a flooder for this session on your side: everything they send is ignored except voice.");
            CharacterTakeover = config.Bind(Section, "CharacterTakeover", true, "Report a player who tries to take control of another player's character (ownership request or transfer).");
            BlockCharacterTakeover = config.Bind(Section, "BlockCharacterTakeover", true, "Ignore such takeovers on your side.");
            CheatModMarkers = config.Bind(Section, "CheatModMarkers", true, "Report players marked by the Cherry, Atlas or PEAK-MX cheat mods.");
            KickCheatModUsers = config.Bind(Section, "KickCheatModUsers", true, "As host, kick players marked by the Cherry, Atlas or PEAK-MX cheat mods.");
        }

        public ConfigEntry<bool> HarmToOthers { get; }
        public ConfigEntry<bool> BlockHarmToOthers { get; }
        public ConfigEntry<bool> HostOnlyEvents { get; }
        public ConfigEntry<bool> BlockHostOnlyEvents { get; }
        public ConfigEntry<bool> BlockRevivesOfMe { get; }
        public ConfigEntry<bool> CheckMyself { get; }
        public ConfigEntry<bool> DangerousSpawns { get; }
        public ConfigEntry<bool> BlockDangerousSpawns { get; }
        public ConfigEntry<bool> Flight { get; }
        public ConfigEntry<bool> BlockFlight { get; }
        public ConfigEntry<bool> Stamina { get; }
        public ConfigEntry<bool> SourcelessEffects { get; }
        public ConfigEntry<bool> ForcedActions { get; }
        public ConfigEntry<bool> BlockForcedActions { get; }
        public ConfigEntry<bool> ObjectDestroy { get; }
        public ConfigEntry<bool> BlockObjectDestroy { get; }
        public ConfigEntry<bool> NameSpoofing { get; }
        public ConfigEntry<bool> BuildSpam { get; }
        public ConfigEntry<bool> BlockBuildSpam { get; }
        public ConfigEntry<bool> Flood { get; }
        public ConfigEntry<bool> MuteFlooders { get; }
        public ConfigEntry<bool> CharacterTakeover { get; }
        public ConfigEntry<bool> BlockCharacterTakeover { get; }
        public ConfigEntry<bool> CheatModMarkers { get; }
        public ConfigEntry<bool> KickCheatModUsers { get; }
    }
}
