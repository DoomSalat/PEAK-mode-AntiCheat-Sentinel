using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;

namespace PeakSentinel.Protection
{
    public sealed class CarryShield : IRpcAudit
    {
        private readonly SuspicionReport _report;
        private readonly OtherSettings _settings;

        public CarryShield(SuspicionReport report, OtherSettings settings)
        {
            _report = report;
            _settings = settings;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.StartCarry;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_settings.RefuseCarryWhileAwake.Value || call.Sender == null || call.FromLocal || _report.Spares(call.Sender))
                return true;

            Character carrier = call.Component<Character>();
            PhotonView targetView = call.ViewAt(0);

            if (carrier == null || targetView == null || !targetView.TryGetComponent(out Character me) || !me.IsLocal || me.data.fullyPassedOut)
                return true;

            carrier.photonView.RPC(RpcNames.DropCarried, RpcTarget.Others, targetView);
            _report.Announce(call.Sender, FindingCategory.Other, "tried to pick you up while you were awake, refused", FeedTone.Good);
            return false;
        }
    }
}
