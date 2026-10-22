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
        private const float RetrySeconds = 5f;
        private const string MuteReason = "banned";

        private readonly Dictionary<int, float> _kicked = new Dictionary<int, float>();
        private SuspicionReport _report;
        private BanList _bans;
        private MuteList _mutes;
        private float _nextScan;

        public void Initialize(SuspicionReport report, BanList bans, MuteList mutes)
        {
            _report = report;
            _bans = bans;
            _mutes = mutes;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            if (!PhotonNetwork.InRoom)
            {
                _kicked.Clear();
                return;
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal || !_bans.Contains(player.UserId))
                    continue;

                if (!_mutes.IsMuted(player))
                    _mutes.Mute(player, MuteReason);

                if (!PhotonNetwork.IsMasterClient)
                    continue;

                if (!_kicked.TryGetValue(player.ActorNumber, out float kickedAt))
                    Remove(player);
                else if (Time.unscaledTime - kickedAt >= RetrySeconds)
                    Kick(player);
            }
        }

        public void Ban(PhotonPlayer player)
        {
            _bans.Ban(player.UserId, PlayerNames.Name(player));
            _report.Tag(player, BannedTag);
            _report.Flag(player, FindingCategory.Cheats, "was banned by you", true);
            _mutes.Mute(player, MuteReason);

            if (PhotonNetwork.IsMasterClient)
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
            _kicked[player.ActorNumber] = Time.unscaledTime;

            if (PlayerHandler.TryGetPlayer(player.ActorNumber, out _))
                PlayerHandler.Kick(player.ActorNumber);
            else
                NetCode.Session.Kick(player.UserId);
        }
    }
}
