using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;

namespace PeakSentinel.Detection
{
    public sealed class CarryAudit : IRpcAudit
    {
        private const float CarryReach = 6f;
        private const float PiggybackReach = 3f;
        private const string Tag = "carries players illegally";

        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public CarryAudit(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.StartCarry;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.ForcedActions.Value || call.Sender == null || call.FromHost || call.FromLocal)
                return true;

            Character carrier = call.Component<Character>();
            PhotonView targetView = call.ViewAt(0);

            if (carrier == null || targetView == null || !targetView.TryGetComponent(out Character target))
                return true;

            string reason = ReasonAgainst(call, carrier, target);
            // Block only protects your own screen: the cheater still sees the victim on their back, while on yours the victim stays on the ground
            return reason == null || _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, $"picked up {PlayerNames.Name(targetView.Owner)} {reason}", _detection.BlockForcedActions.Value);
        }

        private string ReasonAgainst(RpcCall call, Character carrier, Character target)
        {
            if (!call.FromOwner)
                return $"in the name of {PlayerNames.Name(call.Owner)}";

            if (target.data.dead)
                return "while they were dead";

            float distance = (carrier.Center - target.Center).sqrMagnitude;

            if (!target.data.fullyPassedOut && distance > PiggybackReach * PiggybackReach)
                return "while they were conscious and not next to them";

            if (target.data.carrier != null && target.data.carrier != carrier)
                return "out of someone else's arms";

            return distance > CarryReach * CarryReach ? "from far away" : null;
        }
    }
}
