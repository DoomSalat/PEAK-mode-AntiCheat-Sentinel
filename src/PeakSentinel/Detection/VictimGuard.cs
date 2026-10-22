using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class VictimGuard
    {
        private const string OnlyHostNote = " (their own game still applies it, only the host can undo it)";

        private readonly SuspicionReport _report;

        public VictimGuard(SuspicionReport report)
        {
            _report = report;
        }

        public bool Refuse(PhotonPlayer culprit, Character victim, FindingCategory category, string tag, string action, bool block, Recovery recovery = null, Repair repair = null)
        {
            Repair fallback = repair ?? recovery?.AsRepair();

            if (victim == null || victim.IsLocal)
            {
                if (!block)
                    return _report.Refuse(culprit, category, tag, action, false, fallback);

                if (victim != null && recovery != null && recovery.MirrorsOnMe)
                    recovery.Apply(RpcTarget.Others);

                return _report.Block(culprit, category, tag, action);
            }

            if (recovery == null)
                return _report.Refuse(culprit, category, tag, action, block, fallback);

            if (!PhotonNetwork.IsMasterClient)
            {
                _report.Tag(culprit, tag);
                _report.Flag(culprit, category, action + OnlyHostNote, false, fallback);
                return true;
            }

            if (!block)
                return _report.Refuse(culprit, category, tag, action, false, fallback);

            recovery.Apply(RpcTarget.All);
            return _report.Recover(culprit, category, tag, $"{action}, {recovery.Action} for everyone");
        }
    }
}
