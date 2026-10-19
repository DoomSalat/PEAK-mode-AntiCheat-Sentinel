using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using PeakSentinel.Reporting;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class RpcAudit
    {
        private const byte ViewIdKey = 0;
        private const byte MethodNameKey = 3;
        private const byte ArgumentsKey = 4;
        private const byte MethodIndexKey = 5;
        private const string SpawnInHand = "RPC_SpawnItemInHandMaster";
        private const string RequestPickup = "RequestPickup";
        private const string PickupAccepted = "OnPickupAccepted";
        private const string SyncInventory = "SyncInventoryRPC";
        private const string SetInventory = "RPC_SetInventory";
        private const string RemoveFromSlot = "RPCRemoveItemFromSlot";
        private const string InitHoldingItem = "RPC_InitHoldingItem";
        private const string EquipSlot = "EquipSlotRpc";
        private const string Clone = "RPC_Clone";
        private const string PassportItem = "Passport";

        private static readonly HashSet<string> Watched = new HashSet<string> // Static: fixed set of item RPC names
        {
            SpawnInHand, RequestPickup, PickupAccepted, SyncInventory, SetInventory, RemoveFromSlot, InitHoldingItem, EquipSlot, Clone,
        };

        private readonly SuspicionReport _report;
        private readonly DebugTrace _trace;
        private readonly ProtectionSettings _protection;
        private readonly HarmAudit _harm;
        private readonly WorldAudit _world;
        private readonly HandGuard _hands;
        private readonly KickShield _kicks;

        public RpcAudit(SuspicionReport report, DebugTrace trace, ProtectionSettings protection, HarmAudit harm, WorldAudit world, HandGuard hands, KickShield kicks)
        {
            _kicks = kicks;
            _hands = hands;
            _report = report;
            _trace = trace;
            _protection = protection;
            _harm = harm;
            _world = world;
        }

        public bool Inspect(Hashtable rpc, PhotonPlayer sender)
        {
            if (rpc == null || !(rpc[ViewIdKey] is int viewId))
                return true;

            string method = MethodOf(rpc);

            if (method == null)
                return true;

            PhotonView view = PhotonView.Find(viewId);
            PhotonPlayer owner = view == null ? null : view.Owner;
            object[] arguments = rpc[ArgumentsKey] as object[];

            if (_trace.TracesAllRpcs || Watched.Contains(method) || _harm.Watches(method) || _world.Watches(method))
                _trace.Rpc(method, sender, viewId, owner, arguments);

            switch (method)
            {
                case SpawnInHand:
                    return CheckSpawnInHand(sender, owner, view, FirstString(arguments));

                case RequestPickup:
                    return CheckPickupRequest(sender, view, arguments);

                case PickupAccepted:
                    return CheckFromHost(sender, owner, "forced an item pickup on") && _hands.Allow(view, arguments);

                case SyncInventory:
                case SetInventory:
                case RemoveFromSlot:
                    return CheckFromOwner(sender, owner, $"rewrote the inventory ({method}) of");

                case InitHoldingItem:
                case EquipSlot:
                    return CheckFromOwner(sender, owner, $"forced an item into the hands ({method}) of");

                case Clone:
                    _report.Suspect(sender, FindingCategory.Items, $"cloned an item ({ItemNameOf(view)})");
                    return true;

                default:
                    return _kicks.Allow(method, sender, view) && (_harm.Inspect(method, sender, owner, arguments) & _world.Inspect(method, sender, view, arguments));
            }
        }

        private bool CheckSpawnInHand(PhotonPlayer sender, PhotonPlayer owner, PhotonView view, string item)
        {
            if (!IsSame(sender, owner))
                return Refuse(sender, $"spawned '{item}' into the hands of {SuspicionReport.NameOf(owner)}", IsMine(sender));

            if (IsGrappleStartItem(item))
            {
                _report.Notice(sender, FindingCategory.Items, $"got the grapple mode start item '{item}'");
                return true;
            }

            if (IsAirportPassport(view, item))
            {
                _report.Notice(sender, FindingCategory.Items, $"got the airport '{item}'");
                return true;
            }

            return Refuse(sender, $"spawned '{item}' into own hands", IsMine(sender));
        }

        private bool CheckPickupRequest(PhotonPlayer sender, PhotonView itemView, object[] arguments)
        {
            PhotonView picker = arguments != null && arguments.Length > 0 ? ViewOf(arguments[0]) : null;
            PhotonPlayer pickerOwner = picker == null ? null : picker.Owner;

            if (IsSame(sender, pickerOwner) || IsHost(sender))
                return true;

            return Refuse(sender, $"made {SuspicionReport.NameOf(pickerOwner)} pick up '{ItemNameOf(itemView)}'", false);
        }

        private bool CheckFromHost(PhotonPlayer sender, PhotonPlayer owner, string action)
        {
            return IsHost(sender) || Refuse(sender, $"{action} {SuspicionReport.NameOf(owner)}", false);
        }

        private bool CheckFromOwner(PhotonPlayer sender, PhotonPlayer owner, string action)
        {
            return IsSame(sender, owner) || IsHost(sender) || Refuse(sender, $"{action} {SuspicionReport.NameOf(owner)}", false);
        }

        private bool Refuse(PhotonPlayer sender, string action, bool mine)
        {
            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient && !(mine && _protection.AllowMySpawns.Value);
            _report.Flag(sender, FindingCategory.Items, action, block);
            return !block;
        }

        private bool IsMine(PhotonPlayer sender)
        {
            return sender != null && sender.IsLocal;
        }

        private bool IsHost(PhotonPlayer sender)
        {
            return sender != null && sender.IsMasterClient;
        }

        private string MethodOf(Hashtable rpc)
        {
            if (rpc[MethodIndexKey] is byte index)
                return index < PhotonNetwork.PhotonServerSettings.RpcList.Count ? PhotonNetwork.PhotonServerSettings.RpcList[index] : null;

            return rpc[MethodNameKey] as string;
        }

        private PhotonView ViewOf(object argument)
        {
            if (argument is PhotonView view)
                return view;

            return argument is int id ? PhotonView.Find(id) : null;
        }

        private string ItemNameOf(PhotonView view)
        {
            Item item = view == null ? null : view.GetComponent<Item>();
            return item == null ? "unknown item" : item.GetItemName();
        }

        private string FirstString(object[] arguments)
        {
            return arguments != null && arguments.Length > 0 ? arguments[0] as string : null;
        }

        private bool IsSame(PhotonPlayer first, PhotonPlayer second)
        {
            return first != null && second != null && first.ActorNumber == second.ActorNumber;
        }

        private bool IsAirportPassport(PhotonView view, string item)
        {
            Character character = view == null ? null : view.GetComponent<Character>();
            return item == PassportItem && character != null && character.inAirport;
        }

        private bool IsGrappleStartItem(string item)
        {
            return item == RunSettings.GRAPPLE_MODE_ITEM && RunSettings.GetValue(RunSettings.SETTINGTYPE.GrappleMode) == 1;
        }
    }
}
