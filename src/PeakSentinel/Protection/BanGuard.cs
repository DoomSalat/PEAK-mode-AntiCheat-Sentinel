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
        private const string MuteReason = "banned";
        private const string BannedByYou = "was banned by you";

        private SuspicionReport _report;
        private BanList _bans;
        private MuteList _mutes;
        private PlayerKicker _kicker;
        private float _nextScan;

        public void Initialize(SuspicionReport report, BanList bans, MuteList mutes, PlayerKicker kicker)
        {
            _report = report;
            _bans = bans;
            _mutes = mutes;
            _kicker = kicker;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan || !PhotonNetwork.InRoom)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal || !_bans.Contains(player.UserId))
                    continue;

                if (!_mutes.IsMuted(player))
                    _mutes.Mute(player, MuteReason);

                if (PhotonNetwork.IsMasterClient && !_kicker.IsKicking(player))
                    Remove(player);
            }
        }

        public void Ban(PhotonPlayer player)
        {
            _bans.Ban(player.UserId, PlayerNames.Name(player));
            _mutes.Mute(player, MuteReason);

            if (PhotonNetwork.IsMasterClient)
            {
                _report.Kick(player, FindingCategory.Cheats, BannedTag, BannedByYou);
                _kicker.Kick(player);
            }
            else
            {
                _report.Tag(player, BannedTag);
                _report.Flag(player, FindingCategory.Cheats, BannedByYou, true);
            }
        }

        private void Remove(PhotonPlayer player)
        {
            _report.Kick(player, FindingCategory.Cheats, BannedTag, "is on your ban list");
            _kicker.Kick(player);
        }
    }
}
