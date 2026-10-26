using Peak;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;

namespace PeakSentinel.Protection
{
    public sealed class DaggerShield : IRpcAudit
    {
        private readonly SuspicionReport _report;
        private readonly OtherSettings _settings;

        public DaggerShield(SuspicionReport report, OtherSettings settings)
        {
            _report = report;
            _settings = settings;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.GetFedItem;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_settings.RefuseDaggerSacrifice.Value || call.Sender == null || call.FromLocal || _report.Spares(call.Sender))
                return true;

            Character me = call.Component<Character>();

            if (me == null || !me.IsLocal || !(call.Argument(0) is int itemId))
                return true;

            PhotonView item = PhotonView.Find(itemId);

            if (item == null || !item.TryGetComponent(out Action_SacrificeFriend _))
                return true;

            _report.Announce(call.Sender, FindingCategory.Other, "tried to sacrifice you with a ritual dagger, refused", FeedTone.Good);
            return false;
        }
    }
}
