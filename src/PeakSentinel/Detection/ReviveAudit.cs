using PeakSentinel.Game;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;
using Zorro.Core;

namespace PeakSentinel.Detection
{
    public sealed class ReviveAudit : IRpcAudit
    {
        private const float FreshSpawnSeconds = 20f;
        private const float ReviveReach = 12f;
        private const string BookTag = "BookOfBones";
        private const float SkeletonReach = 3f;
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

            string action = call.FromOwner ? "revived themselves with no source" : $"revived {PlayerNames.Name(call.Owner)} with no source (no statue, effigy, Book of Bones or skeleton)";
            return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, action, _detection.BlockHostOnlyEvents.Value);
        }

        private bool HasSource(RpcCall call, Character target)
        {
            if (InVoid())
                return true;

            if (call.FromOwner)
                return _spawns.SpawnedWithin(target, FreshSpawnSeconds);

            Character reviver = PlayerHandler.GetPlayerCharacter(call.Sender);
            Item item = reviver == null ? null : reviver.data.currentItem;

            if (reviver == null || !(call.Argument(0) is Vector3 place) || (reviver.Center - place).sqrMagnitude > ReviveReach * ReviveReach)
                return false;

            if (SkeletonOf(target, place))
                return true;

            if (item == null)
                return false;

            return item.TryGetComponent(out ScoutEffigy _) || item.gameObject.CompareTag(BookTag);
        }

        private bool InVoid()
        {
            MapHandler map = Singleton<MapHandler>.Instance;
            return map != null && map.GetCurrentSegment() == Segment.Void;
        }

        private bool SkeletonOf(Character target, Vector3 place)
        {
            foreach (Collider collider in Physics.OverlapSphere(place, SkeletonReach))
            {
                Skelleton skeleton = collider.GetComponentInParent<Skelleton>();

                if (skeleton != null && GameInternals.OwnerOf(skeleton) == target)
                    return true;
            }

            return false;
        }
    }
}