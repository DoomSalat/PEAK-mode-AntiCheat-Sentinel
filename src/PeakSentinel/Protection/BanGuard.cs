using System.Collections.Generic;
using Peak.Network;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class BanGuard : MonoBehaviour
    {
        private const float ScanSeconds = 1f;
        private const string BannedTag = "banned";

        private readonly HashSet<int> _kicked = new HashSet<int>();
        private SuspicionReport _report;
        private BanList _bans;
        private float _nextScan;

        public void Initialize(SuspicionReport report, BanList bans)
        {
            _report = report;
            _bans = bans;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            {
                _kicked.Clear();
                return;
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal && _bans.Contains(player.UserId) && _kicked.Add(player.ActorNumber))
                    Remove(player);
            }
        }

        public void Ban(PhotonPlayer player)
        {
            _bans.Ban(player.UserId, PlayerNames.Name(player));
            _report.Tag(player, BannedTag);
            _report.Flag(player, FindingCategory.Cheats, "was banned by you", true);
            _kicked.Add(player.ActorNumber);
            Kick(player);
        }

        private void Remove(PhotonPlayer player)
        {
            _report.Tag(player, BannedTag);
            _report.Flag(player, FindingCategory.Cheats, "is on your ban list, kicked", true);
            Kick(player);
        }

        private void Kick(PhotonPlayer player)
        {
            if (PlayerHandler.TryGetPlayer(player.ActorNumber, out _))
                PlayerHandler.Kick(player.ActorNumber);
            else
                NetCode.Session.Kick(player.UserId);
        }
    }
}
