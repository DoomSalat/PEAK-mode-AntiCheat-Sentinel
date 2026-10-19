using System.Collections.Generic;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class KickShield
    {
        private const string KickRpc = "RPCA_Kick";
        private const string FallRpc = "RPCA_Fall";
        private const string UnFallRpc = "RPCA_UnFall";
        private const float KickSeconds = 2f;

        private static readonly HashSet<string> KickEffects = new HashSet<string> { FallRpc, "RPCA_AddForceAtPosition" }; // Static: fixed RPCs a vanilla kick sends to the victim

        private readonly Dictionary<int, float> _kicks = new Dictionary<int, float>();
        private readonly SuspicionReport _report;
        private readonly OtherSettings _settings;

        public KickShield(SuspicionReport report, OtherSettings settings)
        {
            _report = report;
            _settings = settings;
        }

        public bool Allow(string method, PhotonPlayer sender, PhotonView view)
        {
            if (sender == null || sender.IsLocal)
                return true;

            if (method == KickRpc)
            {
                NoteKick(sender);
                return true;
            }

            if (!KickEffects.Contains(method) || view == null || !_kicks.TryGetValue(sender.ActorNumber, out float kickedAt) || Time.time - kickedAt > KickSeconds || !view.TryGetComponent(out Character victim))
                return true;

            if (victim.IsLocal && _settings.IgnoreKicks.Value)
            {
                if (method == FallRpc)
                    _report.Notice(sender, FindingCategory.Other, "kicked you, the kick was ignored");

                return false;
            }

            if (method == FallRpc && !victim.IsLocal && ShieldsEveryone())
            {
                victim.photonView.RPC(UnFallRpc, RpcTarget.All);
                _report.Notice(sender, FindingCategory.Other, $"kicked {SuspicionReport.NameOf(victim.photonView.Owner)}, stood them back up");
            }

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
            bool enabled = _settings.IgnoreKicks.Value && _settings.ShieldEveryone.Value && (PhotonNetwork.IsMasterClient || _settings.ShieldEveryoneAsClient.Value);
            return enabled && !IsKickMode();
        }

        private bool IsKickMode()
        {
            return RunSettings.GetValue(RunSettings.SETTINGTYPE.HelpingHand) == 1;
        }
    }
}
