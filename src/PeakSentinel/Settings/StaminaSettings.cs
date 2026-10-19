using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class StaminaSettings
    {
        private const string Section = "Stamina";

        public StaminaSettings(ConfigFile config)
        {
            FlagSeconds = config.Bind(Section, "FlagSeconds", 4f, "How long a player must sprint or climb without their stamina going down before they are reported (seconds).");
            MinDrop = config.Bind(Section, "MinDrop", 0.02f, "Stamina (0..1, regular plus bonus) must fall at least this much below the lowest value seen in the current effort to count as spent.");
            MinSprintSpeed = config.Bind(Section, "MinSprintSpeed", 3f, "Sprinting counts only while the player actually moves faster than this (m/s, horizontal).");
            TraceSamples = config.Bind(Section, "TraceSamples", true, "Write STAMINA lines with the numbers of every sprinting or climbing player to the debug trace twice a second, for tuning.");
        }

        public ConfigEntry<float> FlagSeconds { get; }
        public ConfigEntry<float> MinDrop { get; }
        public ConfigEntry<float> MinSprintSpeed { get; }
        public ConfigEntry<bool> TraceSamples { get; }
    }
}