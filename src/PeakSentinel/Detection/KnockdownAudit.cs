using PeakSentinel.Detection.Flight;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;

namespace PeakSentinel.Detection
{
    public sealed class KnockdownAudit : IRpcAudit
    {
        private const float LongFallSeconds = 3f;
        private const string Tag = "knocks other players down";
        private const string NoCause = "no kick, nothing they threw hit them";
        private const string SpawnedCause = "with an item they spawned out of nowhere";

        private readonly SuspicionReport _report;
        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;
        private readonly LegitForces _forces;
        private readonly EnvironmentMarks _environment;
        private readonly KickClock _kicks;
        private readonly SpawnBlame _blame;

        public KnockdownAudit(SuspicionReport report, VictimGuard guard, DetectionSettings detection, LegitForces forces, EnvironmentMarks environment, KickClock kicks, SpawnBlame blame)
        {
            _guard = guard;
            _blame = blame;
            _kicks = kicks;
            _report = report;
            _detection = detection;
            _forces = forces;
            _environment = environment;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Fall;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || call.FromLocal || _kicks.JustKicked(call.Sender))
                return true;

            Character victim = call.Component<Character>();

            if (victim == null || _forces.Explains(call.Sender, victim) || _environment.CauseOf(victim) != null)
                return true;

            float seconds = call.Argument(0) is float fall ? fall : 0f;
            string name = PlayerNames.Name(call.Owner);

            if (seconds <= LongFallSeconds && !_blame.Blames(call.Sender))
            {
                _report.Suspect(call.Sender, FindingCategory.Cheats, $"knocked {name} down for {seconds:F1} s ({NoCause})");
                return true;
            }

            string action = $"knocked {name} down for {seconds:F1} s ({(_blame.Blames(call.Sender) ? SpawnedCause : NoCause)})";
            Recovery standUp = new Recovery($"stood {name} back up", target => victim.photonView.RPC(RpcNames.UnFall, target), true);
            return _guard.Refuse(call.Sender, victim, FindingCategory.Cheats, Tag, action, _detection.BlockHarmToOthers.Value, standUp);
        }
    }
}
