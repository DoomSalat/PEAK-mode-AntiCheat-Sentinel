using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class FlightSettings
    {
        private const string Section = "Flight";

        public FlightSettings(ConfigFile config)
        {
            MinGroundDistance = config.Bind(Section, "MinGroundDistance", 1.8f, "A player counts as airborne only higher than this above the terrain under them (meters, own raycast; the player's grounded flag counts only when the ground point it reports is real terrain right next to them, so a foot on a ledge edge is not flying).");
            GroundProbeRadius = config.Bind(Section, "GroundProbeRadius", 0.5f, "Width of the probe that looks for ground under the player (meters, sphere radius). A thin ray missed ledges when a player stood with one foot on an edge.");
            WindowSeconds = config.Bind(Section, "WindowSeconds", 1.5f, "Length of the window over which the vertical speed change is measured (seconds).");
            MaxGravityDrop = config.Bind(Section, "MaxGravityDrop", -2f, "If over the window the vertical speed dropped less than this (m/s, gravity pulls it down a lot more), gravity is not acting: possible flight.");
            FastFallSpeed = config.Bind(Section, "FastFallSpeed", -8f, "Falling faster than this (m/s) is never flight: terminal speed keeps the vertical speed constant.");
            FlagSeconds = config.Bind(Section, "FlagSeconds", 1.5f, "How long gravity must stay missing before the player is reported (seconds).");
            MaxRunSpeed = config.Bind(Section, "MaxRunSpeed", 12f, "Horizontal speed above which a player without a reason (rope, carry, wind, ragdoll and so on) counts as speeding (m/s, a sprint is well below this). Checked at any height, so it catches fast flight close to the ground.");
            SpeedWindowSeconds = config.Bind(Section, "SpeedWindowSeconds", 1f, "Window over which the horizontal speed is averaged (seconds). Jumps over 20 m in one sample are treated as teleports and restart the window.");
            SpeedFlagSeconds = config.Bind(Section, "SpeedFlagSeconds", 1f, "How long the player must stay above MaxRunSpeed before being reported (seconds).");
            TraceSamples = config.Bind(Section, "TraceSamples", true, "Write the measured numbers of every airborne player to the debug trace twice a second, for tuning these thresholds.");
        }

        public ConfigEntry<float> MinGroundDistance { get; }
        public ConfigEntry<float> GroundProbeRadius { get; }
        public ConfigEntry<float> WindowSeconds { get; }
        public ConfigEntry<float> MaxGravityDrop { get; }
        public ConfigEntry<float> FastFallSpeed { get; }
        public ConfigEntry<float> FlagSeconds { get; }
        public ConfigEntry<float> MaxRunSpeed { get; }
        public ConfigEntry<float> SpeedWindowSeconds { get; }
        public ConfigEntry<float> SpeedFlagSeconds { get; }
        public ConfigEntry<bool> TraceSamples { get; }
    }
}
