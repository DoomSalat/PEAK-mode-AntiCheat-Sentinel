using BepInEx.Configuration;

namespace PeakSentinel.Settings
{
    public sealed class SentinelSettings
    {
        public SentinelSettings(ConfigFile config, string traceFile)
        {
            Debug = new DebugSettings(config, traceFile);
            Manager = new ManagerSettings(config);
            Protection = new ProtectionSettings(config);
            Detection = new DetectionSettings(config);
            Other = new OtherSettings(config);
            Flood = new FloodSettings(config);
            Flight = new FlightSettings(config);
            Stamina = new StaminaSettings(config);
        }

        public DebugSettings Debug { get; }
        public ManagerSettings Manager { get; }
        public ProtectionSettings Protection { get; }
        public DetectionSettings Detection { get; }
        public OtherSettings Other { get; }
        public FloodSettings Flood { get; }
        public FlightSettings Flight { get; }
        public StaminaSettings Stamina { get; }
    }
}