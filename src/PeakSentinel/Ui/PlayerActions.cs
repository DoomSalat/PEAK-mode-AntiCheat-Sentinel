using System;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class PlayerActions
    {
        private const string KickAction = "kick";
        private const string KickLabel = "Kick";
        private const string ConfirmKickLabel = "Confirm kick?";
        private const string BanAction = "ban";
        private const string BanLabel = "Ban";
        private const string ConfirmBanLabel = "Confirm ban?";
        private const string BringAction = "bring";
        private const string BringLabel = "Bring to me";
        private const string ConfirmBringLabel = "Confirm bring?";
        private const float BringLift = 1f;
        private const string MuteLabel = "Mute";
        private const string UnmuteLabel = "Unmute";
        private const string MutedBy = "by you";

        private readonly ConfirmClicks _confirm = new ConfirmClicks();
        private readonly SuspicionReport _report;
        private readonly MuteList _mutes;
        private readonly BanGuard _banGuard;
        private readonly PlayerKicker _kicker;
        private readonly TrustList _trust;
        private readonly PlayerTeleporter _teleporter;
        private readonly Action _changed;

        public PlayerActions(SuspicionReport report, MuteList mutes, BanGuard banGuard, PlayerKicker kicker, TrustList trust, PlayerTeleporter teleporter, Action changed)
        {
            _report = report;
            _mutes = mutes;
            _banGuard = banGuard;
            _kicker = kicker;
            _trust = trust;
            _teleporter = teleporter;
            _changed = changed;
        }

        public string State => $"{_confirm.Version}|{_mutes.Version}|{_trust.Version}";

        public bool IsMuted(PhotonPlayer player)
        {
            return _mutes.IsMuted(player);
        }

        public int DroppedFrom(PhotonPlayer player)
        {
            return _mutes.DroppedFrom(player);
        }

        public bool IsTrusted(PhotonPlayer player)
        {
            return _trust.Trusts(player);
        }

        public string MuteLabelOf(PhotonPlayer player)
        {
            return _mutes.IsMuted(player) ? UnmuteLabel : MuteLabel;
        }

        public string KickLabelOf(int actor)
        {
            return _confirm.LabelOf(KickAction, actor, KickLabel, ConfirmKickLabel);
        }

        public string BanLabelOf(int actor)
        {
            return _confirm.LabelOf(BanAction, actor, BanLabel, ConfirmBanLabel);
        }

        public string BringLabelOf(int actor)
        {
            return _confirm.LabelOf(BringAction, actor, BringLabel, ConfirmBringLabel);
        }

        public void SwitchMute(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_mutes.IsMuted(player))
            {
                _mutes.Unmute(player);
                _report.LogAction(player, "unmuted");
            }
            else
            {
                _mutes.Mute(player, MutedBy);
                _report.LogAction(player, "muted: everything they send is ignored on your side");
            }

            _changed();
        }

        public void Trust(PhotonPlayer player)
        {
            _trust.Trust(player);
            _report.LogAction(player, "trusted: Sentinel does not block, undo or kick anything they do");
            _changed();
        }

        public void Distrust(PhotonPlayer player)
        {
            _trust.Distrust(player);
            _report.LogAction(player, "no longer trusted");
            _changed();
        }

        public void Kick(PhotonPlayer player)
        {
            if (player != null && _confirm.Confirm(KickAction, player.ActorNumber))
            {
                _kicker.Kick(player);
                _report.LogAction(player, "kicked with the Kick button");
            }

            _changed();
        }

        public void Ban(PhotonPlayer player)
        {
            if (player != null && _confirm.Confirm(BanAction, player.ActorNumber))
                _banGuard.Ban(player);

            _changed();
        }

        public void Bring(PhotonPlayer player)
        {
            if (player != null && _confirm.Confirm(BringAction, player.ActorNumber))
                _teleporter.BringToMe(player, BringLift);

            _changed();
        }

        public PhotonPlayer Find(int actor)
        {
            return PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(actor);
        }
    }
}
