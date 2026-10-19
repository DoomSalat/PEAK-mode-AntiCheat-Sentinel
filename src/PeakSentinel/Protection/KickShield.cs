using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class KickShield : IRpcAudit
    {
        private const float KickSeconds = 2f;

        private static readonly HashSet<string> Methods = new HashSet<string> { RpcNames.Kick, RpcNames.Fall, RpcNames.ForceAtPosition }; // Static: a vanilla kick and the RPCs it sends to the victim

        private readonly Dictionary<int, float> _kicks = new Dictionary<int, float>();
        private readonly SuspicionReport _report;
        private readonly OtherSettings _settings;

        public KickShield(SuspicionReport report, OtherSettings settings)
        {
            _report = report;
            _settings = settings;
        }

        public bool Watches(string method)
        {
            return Methods.Contains(method);
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Sender == null || call.FromLocal)
                return true;

            if (call.Method == RpcNames.Kick)
            {
                NoteKick(call.Sender);
                return true;
            }

            Character victim = call.Component<Character>();

            if (victim == null || !_kicks.TryGetValue(call.Sender.ActorNumber, out float kickedAt) || Time.time - kickedAt > KickSeconds)
                return true;

            return victim.IsLocal ? ShieldMe(call) : ShieldOther(call, victim);
        }

        private bool ShieldMe(RpcCall call)
        {
            if (!_settings.IgnoreKicks.Value)
                return true;

            if (call.Method == RpcNames.Fall)
                _report.Notice(call.Sender, FindingCategory.Other, "kicked you, the kick was ignored");

            return false;
        }

        private bool ShieldOther(RpcCall call, Character victim)
        {
            if (call.Method != RpcNames.Fall || !ShieldsEveryone())
                return true;

            victim.photonView.RPC(RpcNames.UnFall, RpcTarget.All);
            _report.Notice(call.Sender, FindingCategory.Other, $"kicked {PlayerNames.Name(victim.photonView.Owner)}, stood them back up");
            return true;
        }

        private void NoteKick(PhotonPlayer sender)
        {
            _kicks[sender.ActorNumber] = Time.time;

            if (!IsKickMode())
                _report.Flag(sender, FindingCategory.Other, "kicked while kicks are off in this run", ShieldsEveryone());
        }

        private bool ShieldsEveryone()
        {
            bool enabled = _settings.IgnoreKicks.Value && _settings.ShieldEveryoneFromKicks.Value && (PhotonNetwork.IsMasterClient || _settings.ShieldEveryoneFromKicksAsClient.Value);
            return enabled && !IsKickMode();
        }

        private bool IsKickMode()
        {
            return RunSettings.GetValue(RunSettings.SETTINGTYPE.HelpingHand) == 1;
        }
    }
}