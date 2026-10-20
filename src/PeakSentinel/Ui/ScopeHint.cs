using System;
using Photon.Pun;

namespace PeakSentinel.Ui
{
    public static class ScopeHint // Static: pure text for the "If on" lines under the settings
    {
        private const string Format = "If on: {0}   {1}{2}";
        private const string EveryoneChip = "→ protects everyone";
        private const string YouChip = "→ protects only you";
        private const string LogOnlyChip = "→ only writes to the log";
        private const string NeedsHostChip = "→ does nothing now: you are not the host";
        private const string NotNeededChip = "→ not needed while you are the host";
        private const string AsHostEveryone = "   (as host: protects everyone)";

        public static string Text(string effect, Func<bool, HintScope> scope)
        {
            bool host = PhotonNetwork.IsMasterClient;
            HintScope now = scope(host);
            bool betterAsHost = !host && now != HintScope.Everyone && scope(true) == HintScope.Everyone;
            return string.Format(Format, effect, Chip(now), betterAsHost ? RichText.Paint(AsHostEveryone, UiPalette.Clean) : string.Empty);
        }

        private static string Chip(HintScope scope)
        {
            switch (scope)
            {
                case HintScope.Everyone:
                    return RichText.Paint(EveryoneChip, UiPalette.Clean);

                case HintScope.You:
                    return RichText.Paint(YouChip, UiPalette.Text);

                case HintScope.NeedsHost:
                    return RichText.Warning(NeedsHostChip);

                case HintScope.NotNeeded:
                    return NotNeededChip;

                default:
                    return LogOnlyChip;
            }
        }
    }
}