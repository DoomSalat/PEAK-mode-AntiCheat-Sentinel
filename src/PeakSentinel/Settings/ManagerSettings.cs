using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace PeakSentinel.Settings
{
    public sealed class ManagerSettings
    {
        private const string Section = "Manager";
        private const int DefaultLogEntries = 500;

        public const int MinLogEntries = 50;
        public const int MaxLogEntries = 5000;

        public ManagerSettings(ConfigFile config)
        {
            ToggleKey = config.Bind(Section, "ToggleKey", Key.F6, "Key that opens or closes the Sentinel manager window (None = no key). Can also be picked inside the window.");
            LogEntries = config.Bind(Section, "MaxLogEntries", DefaultLogEntries, new ConfigDescription("How many findings the manager window keeps (oldest are dropped first). Also editable in the Log tab.", new AcceptableValueRange<int>(MinLogEntries, MaxLogEntries)));
        }

        public ConfigEntry<Key> ToggleKey { get; }
        public ConfigEntry<int> LogEntries { get; }
    }
}