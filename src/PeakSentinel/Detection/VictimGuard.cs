using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class VictimGuard
    {
        private const string OnlyHostNote = " (their own game still applies it, only the host can undo it)";

        private readonly SuspicionReport _report;
        private readonly SelfResync _selfResync;

        public VictimGuard(SuspicionReport report, SelfResync selfResync)
        {
            _report = report;
            _selfResync = selfResync;
        }

        public bool Refuse(PhotonPlayer culprit, Character victim, FindingCategory category, string tag, string action, bool block, Recovery recovery = null, Repair repair = null)
        {
            Repair fallback = repair ?? recovery?.AsRepair();

            if (_report.Spares(culprit))
                return _report.Refuse(culprit, category, tag, action, false, fallback);

            if (victim == null || victim.IsLocal)
            {
                if (!block)
                    return _report.Refuse(culprit, category, tag, action, false, fallback);

                if (victim != null && recovery != null && recovery.MirrorsOnMe)
                    recovery.Apply(RpcTarget.Others);

                if (victim != null)
                    _selfResync.Schedule(); // The attack still reached everyone else's copy of us: push our real state to them

                return _report.Block(culprit, category, tag, action);
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                _report.Tag(culprit, tag);
                _report.Flag(culprit, category, action + OnlyHostNote, false, fallback);
                return true;
            }

            if (recovery == null)
                return _report.Refuse(culprit, category, tag, action, block, fallback);

            if (!block)
                return _report.Refuse(culprit, category, tag, action, false, fallback);

            recovery.Apply(RpcTarget.All);
            return _report.Recover(culprit, category, tag, $"{action}, {recovery.Action} for everyone");
        }
    }
}
