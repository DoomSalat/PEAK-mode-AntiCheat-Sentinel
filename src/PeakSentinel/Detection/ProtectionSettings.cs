using BepInEx.Configuration;

namespace PeakSentinel.Detection
{
    public sealed class ProtectionSettings
    {
        private const string Section = "Protection";

        public ProtectionSettings(ConfigFile config)
        {
            BlockCheatSpawns = config.Bind(Section, "BlockCheatSpawns", true, "As host, refuse item spawns and forged pickups/inventory changes that the vanilla game never sends (cheats and spawner mods). Only works while you are the host.");
            AllowMySpawns = config.Bind(Section, "AllowMySpawns", true, "Do not block item spawns that you (the host) request yourself, e.g. with your own spawner mod.");
        }

        public ConfigEntry<bool> BlockCheatSpawns { get; }
        public ConfigEntry<bool> AllowMySpawns { get; }
    }
}
