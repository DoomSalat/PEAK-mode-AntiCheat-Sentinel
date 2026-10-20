using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class ReviveAudit : IRpcAudit
    {
        private const float FreshSpawnSeconds = 20f;
        private const float ReviveReach = 12f;
        private const string BookTag = "BookOfBones";
        private const string Tag = "revives without a source";

        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;
        private readonly SpawnClock _spawns;

        public ReviveAudit(SuspicionReport report, DetectionSettings detection, SpawnClock spawns)
        {
            _spawns = spawns;
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.ReviveAtPosition;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.HostOnlyEvents.Value || call.Sender == null || call.FromHost || call.FromLocal)
                return true;

            Character target = call.Component<Character>();

            if (target == null || HasSource(call, target))
                return true;

            string action = call.FromOwner ? "revived themselves with no source" : $"revived {PlayerNames.Name(call.Owner)} with no source (no statue, effigy or Book of Bones)";
            return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, action, _detection.BlockHostOnlyEvents.Value);
        }

        private bool HasSource(RpcCall call, Character target)
        {
            if (call.FromOwner)
                return _spawns.SpawnedWithin(target, FreshSpawnSeconds);

            Character reviver = PlayerHandler.GetPlayerCharacter(call.Sender);
            Item item = reviver == null ? null : reviver.data.currentItem;

            if (item == null || !(call.Argument(0) is Vector3 place) || (reviver.Center - place).sqrMagnitude > ReviveReach * ReviveReach)
                return false;

            return item.TryGetComponent(out ScoutEffigy _) || item.gameObject.CompareTag(BookTag);
        }
    }
}