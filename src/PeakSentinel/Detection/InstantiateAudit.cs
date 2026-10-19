using System.Collections.Generic;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class InstantiateAudit
    {
        private const float BackpackVisualHeight = -400f;
        private const char PathSeparator = '/';
        private const string DangerousTag = "spawns dangerous objects";

        private static readonly HashSet<string> Dangerous = new HashSet<string> // Static: fixed host-only prefabs (same list as PEAKER)
        {
            "BeeSwarm", "Tornado", "Bingbong_Blow", "Bingbong_Grab", "Bingbong_Push", "Bingbong_Suck", "Bingbong_Push_Gentle",
        };

        private readonly SuspicionReport _report;
        private readonly DebugTrace _trace;
        private readonly DetectionSettings _detection;

        public InstantiateAudit(SuspicionReport report, DebugTrace trace, DetectionSettings detection)
        {
            _report = report;
            _trace = trace;
            _detection = detection;
        }

        public bool AllowCreate(PhotonPlayer creator, string prefabPath)
        {
            string prefab = PrefabOf(prefabPath);

            if (!_detection.DangerousSpawns.Value || creator == null || creator.IsMasterClient || !Dangerous.Contains(prefab))
                return true;

            return _report.Refuse(creator, FindingCategory.Cheats, DangerousTag, $"spawned '{prefab}' (only the host may spawn it)", _detection.BlockDangerousSpawns.Value);
        }

        public void Inspect(PhotonPlayer creator, GameObject created, bool fromNetwork)
        {
            if (created == null || !created.TryGetComponent(out Item item))
                return;

            _trace.Instantiate(creator, created.name, created.transform.position);

            if (!fromNetwork || creator == null || creator.IsMasterClient)
                return;

            if (created.transform.position.y < BackpackVisualHeight)
                return;

            if (Carries(creator, item.itemID))
            {
                _report.Notice(creator, FindingCategory.Items, $"took '{item.GetItemName()}' out of the inventory");
                return;
            }

            _report.Suspect(creator, FindingCategory.Items, $"created '{item.GetItemName()}' that is not in their inventory (spawn or legit breakable — check)");
        }

        private string PrefabOf(string path)
        {
            if (path == null)
                return string.Empty;

            int slash = path.LastIndexOf(PathSeparator);
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        private bool Carries(PhotonPlayer creator, ushort itemId)
        {
            if (!PlayerHandler.TryGetPlayer(creator.ActorNumber, out Player player))
                return false;

            foreach (ItemSlot slot in player.itemSlots)
            {
                if (Holds(slot, itemId))
                    return true;
            }

            return Holds(player.tempFullSlot, itemId);
        }

        private bool Holds(ItemSlot slot, ushort itemId)
        {
            return slot != null && !slot.IsEmpty() && slot.prefab.itemID == itemId;
        }
    }
}
