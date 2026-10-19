using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class OtherSettings
    {
        private const string Section = "Other";

        public OtherSettings(ConfigFile config)
        {
            IgnoreKicks = config.Bind(Section, "IgnoreKicks", false, "Vanilla kicks of other players do nothing to you: no ragdoll and no push (refused on your client, which owns your character).");
            AllowHonestKicks = config.Bind(Section, "AllowHonestKicks", false, "Needs IgnoreKicks. Turn your own kick protection off by itself while the run has kicks on, so only cheat kicks are ignored.");
            ShieldEveryoneFromKicks = config.Bind(Section, "ShieldEveryoneFromKicks", false, "Needs IgnoreKicks. As host, while the run has kicks off (kicks then come only from cheats): a kicked player is stood back up at once (RPCA_UnFall to everyone). The push itself still lands, only the ragdoll is cancelled.");
            ShieldEveryoneInKickMode = config.Bind(Section, "ShieldEveryoneInKickMode", false, "Needs ShieldEveryoneFromKicks. Also stand kicked players back up when the run has kicks on and the kick is honest.");
            ShieldEveryoneFromKicksAsClient = config.Bind(Section, "ShieldEveryoneFromKicksAsClient", false, "Needs ShieldEveryoneFromKicks. Also apply it while you are not the host. Other anticheats may see your RPCs on other players as suspicious.");
        }

        public ConfigEntry<bool> IgnoreKicks { get; }
        public ConfigEntry<bool> AllowHonestKicks { get; }
        public ConfigEntry<bool> ShieldEveryoneFromKicks { get; }
        public ConfigEntry<bool> ShieldEveryoneFromKicksAsClient { get; }
        public ConfigEntry<bool> ShieldEveryoneInKickMode { get; }
    }
}