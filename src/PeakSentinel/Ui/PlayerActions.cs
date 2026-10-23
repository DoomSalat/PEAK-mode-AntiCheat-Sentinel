using System;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class PlayerActions
    {
        private const string KickLabel = "Kick";
        private const string ConfirmKickLabel = "Confirm kick?";
        private const string BanLabel = "Ban";
        private const string ConfirmBanLabel = "Confirm ban?";
        private const string MuteLabel = "Mute";
        private const string UnmuteLabel = "Unmute";
        private const string MutedBy = "by you";
        private const string PercentFormat = "{0}%";
        private const float PercentScale = 200f;

        private readonly MuteList _mutes;
        private readonly BanGuard _banGuard;
        private readonly PlayerKicker _kicker;
        private readonly AvatarCache _avatars;
        private readonly Action _changed;
        private int _kickArmedActor;
        private int _banArmedActor;

        public PlayerActions(MuteList mutes, BanGuard banGuard, PlayerKicker kicker, AvatarCache avatars, Action changed)
        {
            _mutes = mutes;
            _banGuard = banGuard;
            _kicker = kicker;
            _avatars = avatars;
            _changed = changed;
        }

        public string State => $"{_kickArmedActor}|{_banArmedActor}|{_mutes.Version}|{_avatars.Version}";

        public bool IsMuted(PhotonPlayer player)
        {
            return _mutes.IsMuted(player);
        }

        public int DroppedFrom(PhotonPlayer player)
        {
            return _mutes.DroppedFrom(player);
        }

        public string MuteLabelOf(PhotonPlayer player)
        {
            return _mutes.IsMuted(player) ? UnmuteLabel : MuteLabel;
        }

        public string KickLabelOf(int actor)
        {
            return _kickArmedActor == actor ? ConfirmKickLabel : KickLabel;
        }

        public string BanLabelOf(int actor)
        {
            return _banArmedActor == actor ? ConfirmBanLabel : BanLabel;
        }

        public float VoiceLevelOf(PhotonPlayer player)
        {
            return AudioLevels.GetPlayerLevel(player.UserId);
        }

        public string VoicePercentOf(float level)
        {
            return string.Format(PercentFormat, Mathf.RoundToInt(level * PercentScale));
        }

        // SAVE NOTE: the level lives in vanilla AudioLevels for the session only; LOAD: file by Steam ID → SetPlayerLevel on join; SAVE: here
        public void SetVoiceLevel(PhotonPlayer player, float level)
        {
            if (player == null)
                return;

            AudioLevels.SetPlayerLevel(player.UserId, level);
        }

        public void SwitchMute(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_mutes.IsMuted(player))
                _mutes.Unmute(player);
            else
                _mutes.Mute(player, MutedBy);

            _changed();
        }

        public void Kick(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_kickArmedActor != player.ActorNumber)
            {
                _kickArmedActor = player.ActorNumber;
                _changed();
                return;
            }

            _kickArmedActor = 0;
            _kicker.Kick(player);
            _changed();
        }

        public void Ban(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_banArmedActor != player.ActorNumber)
            {
                _banArmedActor = player.ActorNumber;
                _changed();
                return;
            }

            _banArmedActor = 0;
            _banGuard.Ban(player);
            _changed();
        }

        public void ShowAvatar(Image image, PhotonPlayer player)
        {
            Sprite avatar = _avatars.Of(player);

            if (avatar == null || image.sprite == avatar)
                return;

            image.sprite = avatar;
            image.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        }

        public Color ColorOf(PlayerStanding standing)
        {
            switch (standing)
            {
                case PlayerStanding.Cheater:
                    return UiPalette.Cheat;

                case PlayerStanding.Suspect:
                    return UiPalette.Suspicious;

                default:
                    return UiPalette.Clean;
            }
        }

        public PhotonPlayer Find(int actor)
        {
            return PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(actor);
        }
    }
}
