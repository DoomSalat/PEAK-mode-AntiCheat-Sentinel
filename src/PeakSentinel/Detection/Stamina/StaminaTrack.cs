namespace PeakSentinel.Detection.Stamina
{
    public sealed class StaminaTrack
    {
        public float? Lowest { get; set; } // Set: StaminaAudit keeps the lowest stamina of the current effort
        public float UnspentFor { get; set; } // Set: accumulated by StaminaAudit while stamina does not go down
        public bool Flagged { get; set; } // Set: StaminaAudit reports each effort once
        public float NextTrace { get; set; } // Set: StaminaAudit throttles the trace
        public float Bonus { get; set; } // Set: StaminaAudit remembers the bonus stamina bar to notice food topping it up

        public void Reset()
        {
            Lowest = null;
            UnspentFor = 0f;
            Flagged = false;
        }
    }
}