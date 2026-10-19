using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class ProtectionSettings
    {
        private const string Section = "Protection";

        public ProtectionSettings(ConfigFile config)
        {
            BlockCheatSpawns = config.Bind(Section, "BlockCheatSpawns", true, "As host, refuse item spawns and forged pickups/inventory changes that the vanilla game never sends (cheats and spawner mods). Only works while you are the host.");
            RefuseForcedPickups = config.Bind(Section, "RefuseForcedPickups", true, "When you are not the host: if the host tells you to take an item you never asked for (someone spawned it into your hands through the host), do not take it and remove it from your inventory.");
            AllowMySpawns = config.Bind(Section, "AllowMySpawns", true, "Do not block item spawns that you (the host) request yourself, e.g. with your own spawner mod.");
        }

        public ConfigEntry<bool> BlockCheatSpawns { get; }
        public ConfigEntry<bool> RefuseForcedPickups { get; }
        public ConfigEntry<bool> AllowMySpawns { get; }
    }
}