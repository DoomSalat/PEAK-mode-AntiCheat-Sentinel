namespace PeakSentinel.Speedrun
{
    public sealed class RunRules
    {
        private const string NoMajorGlitchesLabel = "NMG";
        private const string GlitchedLabel = "Glitched";
        private const string AnyLabel = "Any%";
        private const string NoMajorGlitchesHint = "NMG: every check on. Cactus Grapple and the Shroomalloon combo with a balloon bunch are allowed here, report a false flight alarm if one shows.";
        private const string GlitchedHint = "Glitched: flight and speed checks on yourself are off (Normal and Fast Flight, Cactus Yeet), the fall damage check is off (Glider Rest cancels fall damage). Luck, integrity, stamina, journal and report stay on.";
        private const string AnyHint = "Any%: like Glitched (Cannon Glitch, out of bounds), flight, speed and fall damage checks on yourself are off. Luck, integrity, stamina, journal and report stay on.";

        public bool ChecksFlight(RunCategory category)
        {
            return category == RunCategory.NoMajorGlitches;
        }

        public bool ChecksFallDamage(RunCategory category)
        {
            return category == RunCategory.NoMajorGlitches;
        }

        public string LabelOf(RunCategory category)
        {
            switch (category)
            {
                case RunCategory.Glitched:
                    return GlitchedLabel;

                case RunCategory.Any:
                    return AnyLabel;

                default:
                    return NoMajorGlitchesLabel;
            }
        }

        public string HintOf(RunCategory category)
        {
            switch (category)
            {
                case RunCategory.Glitched:
                    return GlitchedHint;

                case RunCategory.Any:
                    return AnyHint;

                default:
                    return NoMajorGlitchesHint;
            }
        }
    }
}
