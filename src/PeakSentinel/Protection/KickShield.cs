using System.Collections.Generic;
using PeakSentinel.Compat;
using PeakSentinel.Detection;
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
        private static readonly HashSet<string> Methods = new HashSet<string> { RpcNames.Kick, RpcNames.Fall, RpcNames.ForceAtPosition }; // Static: a vanilla kick and the RPCs it sends to the victim

        private readonly KickClock _kicks;
        private readonly SuspicionReport _report;
        private readonly OtherSettings _settings;
        private readonly TrueAscentState _ascent;

        public KickShield(SuspicionReport report, OtherSettings settings, TrueAscentState ascent, KickClock kicks)
        {
            _kicks = kicks;
            _ascent = ascent;
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

            if (victim == null || !_kicks.JustKicked(call.Sender))
                return true;

            return victim.IsLocal ? ShieldMe(call, victim) : ShieldOther(call, victim);
        }

        private bool ShieldMe(RpcCall call, Character me)
        {
            if (!_settings.IgnoreKicks.Value || (_settings.AllowHonestKicks.Value && IsKickMode()))
                return true;

            if (call.Method == RpcNames.Fall)
            {
                me.photonView.RPC(RpcNames.UnFall, RpcTarget.Others); // The kick reached the others too: stand our character back up on their screens
                _report.Announce(call.Sender, FindingCategory.Other, "kicked you, the kick was ignored", KickTone());
            }

            return false;
        }

        private bool ShieldOther(RpcCall call, Character victim)
        {
            if (call.Method != RpcNames.Fall || !ShieldsEveryone())
                return true;

            victim.photonView.RPC(RpcNames.UnFall, RpcTarget.All);
            _report.Announce(call.Sender, FindingCategory.Other, $"kicked {PlayerNames.Name(victim.photonView.Owner)}, stood them back up", KickTone());
            return true;
        }

        private FeedTone KickTone()
        {
            return IsKickMode() ? FeedTone.Suspect : FeedTone.Cheat;
        }

        private void NoteKick(PhotonPlayer sender)
        {
            if (!IsKickMode())
                _report.Flag(sender, FindingCategory.Other, "kicked while kicks are off in this run", ShieldsEveryone());
        }

        private bool ShieldsEveryone()
        {
            bool enabled = _settings.IgnoreKicks.Value && _settings.ShieldEveryoneFromKicks.Value && (PhotonNetwork.IsMasterClient || _settings.ShieldEveryoneFromKicksAsClient.Value);
            return enabled && (!IsKickMode() || _settings.ShieldEveryoneInKickMode.Value);
        }

        private bool IsKickMode()
        {
            return RunSettings.GetValue(RunSettings.SETTINGTYPE.HelpingHand) == 1 || _ascent.KicksAllowed;
        }
    }
}