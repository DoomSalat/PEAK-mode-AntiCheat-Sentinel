using PeakSentinel.Reporting;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class InstantiateAudit
    {
        private const float BackpackVisualHeight = -400f;

        private readonly SuspicionReport _report;
        private readonly DebugTrace _trace;

        public InstantiateAudit(SuspicionReport report, DebugTrace trace)
        {
            _report = report;
            _trace = trace;
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
                _report.Notice(creator, $"took '{item.GetItemName()}' out of the inventory");
                return;
            }

            _report.Flag(creator, $"created '{item.GetItemName()}' that is not in their inventory (spawn or legit breakable — check)");
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
