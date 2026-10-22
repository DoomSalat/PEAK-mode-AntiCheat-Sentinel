using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class LuckText
    {
        private const string OddsFormat = "1 in {0:F0}";
        private const string ThousandFormat = "1 in {0:F1}k";
        private const string MillionFormat = "1 in {0:F1}M";
        private const string BillionFormat = "1 in {0:F1}B";
        private const string HugeOddsFormat = "1 in {0:E1}";
        private const double SuspiciousOdds = 1000d;
        private const double CheatOdds = 1000000d;
        private const double Thousand = 1000d;
        private const double Million = 1000000d;
        private const double Billion = 1000000000d;
        private const double Trillion = 1000000000000d;

        private static readonly Color CleanBright = new Color(0.55f, 0.92f, 0.45f, 1f); // Static: fixed bright green for the HUD badge

        public string OneIn(double chance)
        {
            double odds = chance <= 0d ? double.PositiveInfinity : 1d / chance;

            if (odds >= Trillion)
                return string.Format(HugeOddsFormat, odds);

            if (odds >= Billion)
                return string.Format(BillionFormat, odds / Billion);

            if (odds >= Million)
                return string.Format(MillionFormat, odds / Million);

            if (odds >= Thousand)
                return string.Format(ThousandFormat, odds / Thousand);

            return string.Format(OddsFormat, odds);
        }

        public Color ColorOf(double chance)
        {
            if (chance * CheatOdds <= 1d)
                return UiPalette.Cheat;

            return chance * SuspiciousOdds <= 1d ? UiPalette.Suspicious : UiPalette.Clean;
        }

        public Color BrightColorOf(double chance)
        {
            if (chance * CheatOdds <= 1d)
                return UiPalette.CheatBright;

            return chance * SuspiciousOdds <= 1d ? UiPalette.SuspiciousBright : CleanBright;
        }
    }
}
