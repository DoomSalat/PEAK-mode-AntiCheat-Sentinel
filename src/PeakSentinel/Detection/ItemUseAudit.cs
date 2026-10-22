using PeakSentinel.Network;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;

namespace PeakSentinel.Detection
{
    public sealed class ItemUseAudit : IRpcAudit
    {
        private const string Tag = "uses items without end";
        private const float RestoreSeconds = 20f;

        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;
        private readonly ItemUseLedger _ledger;
        private readonly StatusRestorer _restorer;

        public ItemUseAudit(VictimGuard guard, DetectionSettings detection, ItemUseLedger ledger, StatusRestorer restorer)
        {
            _restorer = restorer;
            _guard = guard;
            _detection = detection;
            _ledger = ledger;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.DartImpact;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.HarmToOthers.Value || call.Sender == null || call.FromLocal)
                return true;

            Item item = call.Component<Item>();

            if (!_ledger.Overspent(item))
                return true;

            PhotonView hit = call.ViewAt(0);
            Character victim = hit == null ? null : hit.GetComponent<Character>();
            string action = $"fired '{item.GetItemName()}' more than its {item.totalUses} uses (endless uses cheat)";
            Recovery undo = victim == null ? null : new Recovery($"undid its effect on {PlayerNames.Name(victim.photonView.Owner)} for {RestoreSeconds:F0} s", _ => _restorer.Guard(victim, RestoreSeconds), false);
            return _guard.Refuse(call.Sender, victim, FindingCategory.Cheats, Tag, action, _detection.BlockHarmToOthers.Value, undo);
        }
    }
}
