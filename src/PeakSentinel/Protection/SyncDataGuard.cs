using System;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using Zorro.Core.Serizalization;

namespace PeakSentinel.Protection
{
    public sealed class SyncDataGuard : IRpcAudit
    {
        private const string Tag = "sends broken data (crash attempt)";
        private const string Thorns = "thorn";
        private const string Statuses = "status";
        private const string Afflictions = "effect";
        private const string Inventory = "inventory";

        private readonly SuspicionReport _report;

        public SyncDataGuard(SuspicionReport report)
        {
            _report = report;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.SyncThorns || method == RpcNames.SyncStatuses || method == RpcNames.SyncAfflictions || method == RpcNames.SyncInventory;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Sender == null || call.FromLocal)
                return true;

            string broken = BrokenPart(call);

            if (broken == null)
                return true;

            return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, $"sent broken {broken} data for {PlayerNames.Name(call.Owner)} (it would throw errors or crash the game), always blocked", true);
        }

        private string BrokenPart(RpcCall call)
        {
            string part = PartOf(call.Method);

            if (!(call.Argument(0) is byte[] data))
                return part;

            try
            {
                return Fits(call, data) ? null : part;
            }
            catch (Exception)
            {
                return part;
            }
        }

        private bool Fits(RpcCall call, byte[] data)
        {
            switch (call.Method)
            {
                case RpcNames.SyncThorns:
                    return IBinarySerializable.GetFromManagedArray<ThornSyncData>(data).stuckThornIndices != null;

                case RpcNames.SyncStatuses:
                    Character character = call.Component<Character>();
                    return character == null || IBinarySerializable.GetFromManagedArray<StatusSyncData>(data).statusList.Count == character.refs.afflictions.currentStatuses.Length;

                case RpcNames.SyncAfflictions:
                    return IBinarySerializable.GetFromManagedArray<AfflictionSyncData>(data).afflictions != null;

                default:
                    Player player = call.Component<Player>();
                    return player == null || IBinarySerializable.GetFromManagedArray<InventorySyncData>(data).slots.Length >= player.itemSlots.Length;
            }
        }

        private string PartOf(string method)
        {
            switch (method)
            {
                case RpcNames.SyncThorns:
                    return Thorns;

                case RpcNames.SyncStatuses:
                    return Statuses;

                case RpcNames.SyncAfflictions:
                    return Afflictions;

                default:
                    return Inventory;
            }
        }
    }
}
