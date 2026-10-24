using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using Zorro.Core.Serizalization;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class ItemRpcAudit : IRpcAudit
    {
        private const string PassportItem = "Passport";
        private const string UnknownItem = "unknown item";
        private const string MissingViewNote = "Photon drops it, nothing changes";
        private const float JoinSyncSeconds = 15f;
        private const float SpawnSyncSeconds = 15f;
        private const float CloneReachMargin = 2f;

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of item RPC names
        {
            RpcNames.SpawnItemInHand, RpcNames.RequestPickup, RpcNames.PickupAccepted, RpcNames.SyncInventory, RpcNames.SetInventory,
            RpcNames.RemoveFromSlot, RpcNames.InitHoldingItem, RpcNames.EquipSlot, RpcNames.Clone,
            RpcNames.StickFakeItem, RpcNames.SpawnAndGrab, RpcNames.AddToBackpack,
        };

        private readonly SuspicionReport _report;
        private readonly ProtectionSettings _protection;
        private readonly SpawnClock _spawns;
        private readonly BackpackSwap _backpacks;
        private readonly Dictionary<int, float> _refusedGives = new Dictionary<int, float>();

        public ItemRpcAudit(SuspicionReport report, ProtectionSettings protection, SpawnClock spawns, BackpackSwap backpacks)
        {
            _backpacks = backpacks;
            _spawns = spawns;
            _report = report;
            _protection = protection;
        }

        public bool RefusedGiveWithin(PhotonPlayer player, float seconds)
        {
            return player != null && _refusedGives.TryGetValue(player.ActorNumber, out float refused) && UnityEngine.Time.time - refused < seconds;
        }

        public bool Watches(string method)
        {
            return Methods.Contains(method);
        }

        public bool Inspect(RpcCall call)
        {
            switch (call.Method)
            {
                case RpcNames.SpawnItemInHand:
                    return CheckSpawnInHand(call);

                case RpcNames.RequestPickup:
                    return CheckPickupRequest(call);

                case RpcNames.PickupAccepted:
                    return call.FromHost || Refuse(call.Sender, $"forced an item pickup on {PlayerNames.Name(call.Owner)}", false);

                case RpcNames.SyncInventory:
                    return CheckInventorySync(call);

                case RpcNames.SetInventory:
                case RpcNames.RemoveFromSlot:
                    return CheckFromOwner(call, $"rewrote the inventory ({call.Method}) of");

                case RpcNames.InitHoldingItem:
                case RpcNames.EquipSlot:
                    return CheckFromOwner(call, $"forced an item into the hands ({call.Method}) of");

                case RpcNames.StickFakeItem:
                    return CheckActsAs(call, PhotonView.Find(call.Argument(0) is int stuckOn ? stuckOn : 0), "stuck a fake item onto");

                case RpcNames.SpawnAndGrab:
                    return CheckActsAs(call, call.ViewAt(1), $"spawned '{call.StringAt(0)}' into the hands of");

                case RpcNames.AddToBackpack:
                    return CheckActsAs(call, call.ViewAt(0), "moved items into a backpack in the name of");

                default:
                    return CheckClone(call);
            }
        }

        private bool CheckSpawnInHand(RpcCall call)
        {
            string item = call.StringAt(0);
            bool mine = call.FromLocal;

            if (!call.FromOwner)
                return Refuse(call.Sender, $"spawned '{item}' into the hands of {PlayerNames.Name(call.Owner)}", mine);

            if (IsGrappleStartItem(item))
            {
                _report.Notice(call.Sender, FindingCategory.Items, $"got the grapple mode start item '{item}'");
                return true;
            }

            if (IsAirportPassport(call, item))
            {
                _report.Notice(call.Sender, FindingCategory.Items, $"got the airport '{item}'");
                return true;
            }

            return Refuse(call.Sender, $"spawned '{item}' into own hands", mine);
        }

        private bool CheckPickupRequest(RpcCall call)
        {
            PhotonView picker = call.ViewAt(0);
            PhotonPlayer pickerOwner = picker == null ? null : picker.Owner;

            if (call.SentBy(pickerOwner) || call.FromHost)
                return true;

            return Refuse(call.Sender, $"made {PlayerNames.Name(pickerOwner)} pick up '{ItemNameOf(call.View)}'", false);
        }

        private bool CheckInventorySync(RpcCall call)
        {
            if (call.FromHost)
                return true;

            if (call.Argument(1) is bool forced && forced)
            {
                if ((call.FromOwner && _spawns.SpawnedWithin(call.Sender, SpawnSyncSeconds)) || _backpacks.IsSwap(call))
                    return true;

                return RefuseGive(call);
            }

            return _spawns.JoinedRoomWithin(JoinSyncSeconds) || Refuse(call.Sender, $"sent a fake inventory of {PlayerNames.Name(call.Owner)} (only the host keeps inventories)", false);
        }

        private bool RefuseGive(RpcCall call)
        {
            _refusedGives[call.Sender.ActorNumber] = UnityEngine.Time.time;
            bool allowed = Refuse(call.Sender, $"forced a new inventory onto {PlayerNames.Name(call.Owner)} (item give cheat)", call.FromLocal);

            if (!allowed)
                PushRealInventory(call);

            return allowed;
        }

        private void PushRealInventory(RpcCall call)
        {
            Player player = call.Component<Player>();

            if (player == null)
                return;

            byte[] inventory = IBinarySerializable.ToManagedArray(new InventorySyncData(player.itemSlots, player.backpackSlot, player.tempFullSlot));
            call.View.RPC(RpcNames.SyncInventory, RpcTarget.Others, inventory, false);
        }

        private bool CheckClone(RpcCall call)
        {
            Item target = call.Component<Item>();
            string name = ItemNameOf(call.View);

            if (CloneAllowed(call.Sender, target))
            {
                _report.Notice(call.Sender, FindingCategory.Items, $"cloned '{name}' with the clone amulet");
                return true;
            }

            return Refuse(call.Sender, $"cloned '{name}' without the clone amulet", call.FromLocal);
        }

        private bool CloneAllowed(PhotonPlayer sender, Item target)
        {
            Character cloner = PlayerHandler.GetPlayerCharacter(sender);
            Item held = cloner == null ? null : cloner.data.currentItem;

            if (target == null || held == null || !held.TryGetComponent(out Peak.Action_CloneSelectedItem amulet))
                return false;

            if (target.itemTags.HasFlag(Item.ItemTags.ScoutAmulet) || target.itemTags.HasFlag(Item.ItemTags.NonCloneable))
                return false;

            float reach = amulet.range + CloneReachMargin;
            return (target.transform.position - cloner.Center).sqrMagnitude <= reach * reach;
        }

        private bool CheckActsAs(RpcCall call, PhotonView character, string action)
        {
            PhotonPlayer target = character == null ? null : character.Owner;
            return target == null || call.SentBy(target) || call.FromHost || Refuse(call.Sender, $"{action} {PlayerNames.Name(target)}", false);
        }

        private bool CheckFromOwner(RpcCall call, string action)
        {
            if (call.FromOwner || call.FromHost || OwnsView(call))
                return true;

            if (call.View == null)
            {
                _report.Notice(call.Sender, FindingCategory.Items, $"{action} an object this host does not have ({MissingViewNote})");
                return true;
            }

            return Refuse(call.Sender, $"{action} {PlayerNames.Name(call.Owner)}", false);
        }

        private bool OwnsView(RpcCall call)
        {
            return call.View != null && call.Sender != null && call.View.OwnerActorNr == call.Sender.ActorNumber;
        }

        private bool Refuse(PhotonPlayer sender, string action, bool mine)
        {
            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient && !(mine && _protection.AllowMySpawns.Value);
            _report.Flag(sender, FindingCategory.Items, action, block);
            return !block;
        }

        private string ItemNameOf(PhotonView view)
        {
            return view != null && view.TryGetComponent(out Item item) ? item.GetItemName() : UnknownItem;
        }

        private bool IsAirportPassport(RpcCall call, string item)
        {
            Character character = call.Component<Character>();
            return item == PassportItem && character != null && character.inAirport;
        }

        private bool IsGrappleStartItem(string item)
        {
            return item == RunSettings.GRAPPLE_MODE_ITEM && RunSettings.GetValue(RunSettings.SETTINGTYPE.GrappleMode) == 1;
        }
    }
}