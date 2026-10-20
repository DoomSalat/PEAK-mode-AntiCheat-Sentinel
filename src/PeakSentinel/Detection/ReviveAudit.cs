using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class ReviveAudit : MonoBehaviour, IRpcAudit
    {
        private const float FreshSpawnSeconds = 20f;
        private const float ReviveReach = 12f;
        private const string BookTag = "BookOfBones";
        private const string Tag = "revives without a source";

        private readonly Dictionary<Character, float> _firstSeen = new Dictionary<Character, float>();
        private SuspicionReport _report;
        private DetectionSettings _detection;

        public void Initialize(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        private void Update()
        {
            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && !_firstSeen.ContainsKey(character))
                    _firstSeen[character] = Time.time;
            }
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
                return _firstSeen.TryGetValue(target, out float seen) && Time.time - seen < FreshSpawnSeconds;

            Character reviver = PlayerHandler.GetPlayerCharacter(call.Sender);
            Item item = reviver == null ? null : reviver.data.currentItem;

            if (item == null || !(call.Argument(0) is Vector3 place) || (reviver.Center - place).sqrMagnitude > ReviveReach * ReviveReach)
                return false;

            return item.TryGetComponent(out ScoutEffigy _) || item.gameObject.CompareTag(BookTag);
        }
    }
}