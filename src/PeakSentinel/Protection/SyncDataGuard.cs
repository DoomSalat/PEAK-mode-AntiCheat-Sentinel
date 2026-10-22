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
        private const string RunSettings = "run settings";
        private const string FakeItems = "map item";
        private const int IslandSettingsIndex = 2;
        private const int AskedSettingsIndex = 1;

        private readonly SuspicionReport _report;

        public SyncDataGuard(SuspicionReport report)
        {
            _report = report;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.SyncThorns || method == RpcNames.SyncStatuses || method == RpcNames.SyncAfflictions || method == RpcNames.SyncInventory
                || method == RpcNames.SyncRunSettings || method == RpcNames.StartIsland || method == RpcNames.AskIslandStart || method == RpcNames.SyncFakeItems;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Sender == null || call.FromLocal)
                return true;

            string broken = BrokenPart(call);

            if (broken == null)
                return true;

            string whose = broken == RunSettings || broken == FakeItems ? string.Empty : $" for {PlayerNames.Name(call.Owner)}";
            return _report.Block(call.Sender, FindingCategory.Cheats, Tag, $"sent broken {broken} data{whose} (it would throw errors or crash the game), always blocked");
        }

        private string BrokenPart(RpcCall call)
        {
            string part = PartOf(call.Method);

            if (!(call.Argument(DataIndexOf(call.Method)) is byte[] data))
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

                case RpcNames.SyncRunSettings:
                case RpcNames.StartIsland:
                case RpcNames.AskIslandStart:
                    return IBinarySerializable.GetFromManagedArray<RunSettingSyncData>(data).settingValues != null;

                case RpcNames.SyncFakeItems:
                    return IBinarySerializable.GetFromManagedArray<FakeItemManager.FakeItemData>(data).hiddenItems != null;

                default:
                    Player player = call.Component<Player>();
                    return player == null || IBinarySerializable.GetFromManagedArray<InventorySyncData>(data).slots.Length >= player.itemSlots.Length;
            }
        }

        private int DataIndexOf(string method)
        {
            switch (method)
            {
                case RpcNames.StartIsland:
                    return IslandSettingsIndex;

                case RpcNames.AskIslandStart:
                    return AskedSettingsIndex;

                default:
                    return 0;
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

                case RpcNames.SyncRunSettings:
                case RpcNames.StartIsland:
                case RpcNames.AskIslandStart:
                    return RunSettings;

                case RpcNames.SyncFakeItems:
                    return FakeItems;

                default:
                    return Inventory;
            }
        }
    }
}
