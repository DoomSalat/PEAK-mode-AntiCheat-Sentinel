using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace PeakSentinel.Protection
{
    public sealed class HandGuard : IRpcAudit
    {
        private const float RequestSeconds = 5f;
        private const string UnknownItem = "unknown item";

        private static readonly HashSet<string> OwnRequests = new HashSet<string> { RpcNames.RequestPickup, RpcNames.RequestFakeItemPickup, RpcNames.SpawnItemInHand }; // Static: fixed vanilla requests that end in OnPickupAccepted

        private readonly List<float> _requests = new List<float>();
        private readonly SuspicionReport _report;
        private readonly ProtectionSettings _protection;

        public HandGuard(SuspicionReport report, ProtectionSettings protection)
        {
            _report = report;
            _protection = protection;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.PickupAccepted;
        }

        public void NoteOutgoing(string method)
        {
            if (OwnRequests.Contains(method))
                _requests.Add(Time.time);
        }

        public bool Inspect(RpcCall call)
        {
            if (PhotonNetwork.IsMasterClient || call.View == null || !call.View.IsMine || !(call.Argument(0) is byte slotId) || ConsumeRequest())
                return true;

            Character character = call.Component<Character>();
            ItemSlot slot = character.player.GetItemSlot(slotId);
            string item = slot == null || slot.IsEmpty() ? UnknownItem : slot.GetPrefabName();
            bool block = _protection.RefuseForcedPickups.Value;

            if (block)
                character.player.EmptySlot(Optionable<byte>.Some(slotId));

            _report.Flag(null, FindingCategory.Items, block ? $"forced '{item}' into your hands through the host, removed it" : $"forced '{item}' into your hands through the host", block);
            return !block;
        }

        private bool ConsumeRequest()
        {
            while (_requests.Count > 0 && Time.time - _requests[0] > RequestSeconds)
                _requests.RemoveAt(0);

            if (_requests.Count == 0)
                return false;

            _requests.RemoveAt(0);
            return true;
        }
    }
}