using System.Collections.Generic;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace PeakSentinel.Detection
{
    public sealed class HandGuard
    {
        private const float RequestSeconds = 5f;
        private const string UnknownItem = "unknown item";

        private static readonly HashSet<string> OwnRequests = new HashSet<string> { "RequestPickup", "RPC_RequestFakeItemPickup", "RPC_SpawnItemInHandMaster" }; // Static: fixed vanilla requests that end in OnPickupAccepted

        private readonly List<float> _requests = new List<float>();
        private readonly SuspicionReport _report;
        private readonly ProtectionSettings _protection;

        public HandGuard(SuspicionReport report, ProtectionSettings protection)
        {
            _report = report;
            _protection = protection;
        }

        public void NoteOutgoing(string method)
        {
            if (OwnRequests.Contains(method))
                _requests.Add(Time.time);
        }

        public bool Allow(PhotonView characterView, object[] arguments)
        {
            if (PhotonNetwork.IsMasterClient || characterView == null || !characterView.IsMine || !(arguments != null && arguments.Length > 0 && arguments[0] is byte slotId))
                return true;

            while (_requests.Count > 0 && Time.time - _requests[0] > RequestSeconds)
                _requests.RemoveAt(0);

            if (_requests.Count > 0)
            {
                _requests.RemoveAt(0);
                return true;
            }

            Character character = characterView.GetComponent<Character>();
            ItemSlot slot = character.player.GetItemSlot(slotId);
            string item = slot == null || slot.IsEmpty() ? UnknownItem : slot.GetPrefabName();
            bool block = _protection.GuardMyHands.Value;

            if (block)
                character.player.EmptySlot(Optionable<byte>.Some(slotId));

            _report.Flag(null, FindingCategory.Items, block ? $"forced '{item}' into your hands through the host, removed it" : $"forced '{item}' into your hands through the host", block);
            return !block;
        }
    }
}
