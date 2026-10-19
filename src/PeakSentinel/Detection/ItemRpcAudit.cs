using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class ItemRpcAudit : IRpcAudit
    {
        private const string PassportItem = "Passport";
        private const string UnknownItem = "unknown item";

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of item RPC names
        {
            RpcNames.SpawnItemInHand, RpcNames.RequestPickup, RpcNames.PickupAccepted, RpcNames.SyncInventory, RpcNames.SetInventory,
            RpcNames.RemoveFromSlot, RpcNames.InitHoldingItem, RpcNames.EquipSlot, RpcNames.Clone,
        };

        private readonly SuspicionReport _report;
        private readonly ProtectionSettings _protection;

        public ItemRpcAudit(SuspicionReport report, ProtectionSettings protection)
        {
            _report = report;
            _protection = protection;
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
                case RpcNames.SetInventory:
                case RpcNames.RemoveFromSlot:
                    return CheckFromOwner(call, $"rewrote the inventory ({call.Method}) of");

                case RpcNames.InitHoldingItem:
                case RpcNames.EquipSlot:
                    return CheckFromOwner(call, $"forced an item into the hands ({call.Method}) of");

                default:
                    _report.Suspect(call.Sender, FindingCategory.Items, $"cloned an item ({ItemNameOf(call.View)})");
                    return true;
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

        private bool CheckFromOwner(RpcCall call, string action)
        {
            return call.FromOwner || call.FromHost || Refuse(call.Sender, $"{action} {PlayerNames.Name(call.Owner)}", false);
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