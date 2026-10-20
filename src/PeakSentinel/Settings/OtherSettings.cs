using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class OtherSettings
    {
        private const string Section = "Other";

        public OtherSettings(ConfigFile config)
        {
            FixDesyncedPlayers = config.Bind(Section, "FixDesyncedPlayers", true, "When another player's body goes crazy on your side only (flies around at huge speed and knocks you away, a known PEAK desync after joining), snap it back to where they really are, like the game's own RagdollExorcism command, locally.");
            ReclaimHost = config.Bind(Section, "ReclaimHost", true, "When you are the host and another player makes themselves host (only cheats do that, Photon cannot forbid it), take the host back at once. Taking it is logged on every Sentinel client either way.");
            KickHostThieves = config.Bind(Section, "KickHostThieves", false, "Needs ReclaimHost. After taking the host back, kick the player who took it.");
            IgnoreKicks = config.Bind(Section, "IgnoreKicks", false, "Vanilla kicks of other players do nothing to you: no ragdoll and no push (refused on your client, which owns your character).");
            AllowHonestKicks = config.Bind(Section, "AllowHonestKicks", false, "Needs IgnoreKicks. Turn your own kick protection off by itself while the run has kicks on, so only cheat kicks are ignored.");
            ShieldEveryoneFromKicks = config.Bind(Section, "ShieldEveryoneFromKicks", false, "Needs IgnoreKicks. As host, while the run has kicks off (kicks then come only from cheats): a kicked player is stood back up at once (RPCA_UnFall to everyone). The push itself still lands, only the ragdoll is cancelled.");
            ShieldEveryoneInKickMode = config.Bind(Section, "ShieldEveryoneInKickMode", false, "Needs ShieldEveryoneFromKicks. Also stand kicked players back up when the run has kicks on and the kick is honest.");
            ShieldEveryoneFromKicksAsClient = config.Bind(Section, "ShieldEveryoneFromKicksAsClient", false, "Needs ShieldEveryoneFromKicks. Also apply it while you are not the host. Other anticheats may see your RPCs on other players as suspicious.");
        }

        public ConfigEntry<bool> FixDesyncedPlayers { get; }
        public ConfigEntry<bool> ReclaimHost { get; }
        public ConfigEntry<bool> KickHostThieves { get; }
        public ConfigEntry<bool> IgnoreKicks { get; }
        public ConfigEntry<bool> AllowHonestKicks { get; }
        public ConfigEntry<bool> ShieldEveryoneFromKicks { get; }
        public ConfigEntry<bool> ShieldEveryoneFromKicksAsClient { get; }
        public ConfigEntry<bool> ShieldEveryoneInKickMode { get; }
    }
}