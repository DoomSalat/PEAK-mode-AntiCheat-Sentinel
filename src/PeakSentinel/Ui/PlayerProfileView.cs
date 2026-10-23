using System;
using PeakSentinel.Reporting;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class PlayerProfileView
    {
        private const float NameSize = 22f;
        private const float LineSize = 19f;
        private const float ButtonSize = 19f;
        private const float ButtonWidth = 160f;
        private const float AvatarSize = 44f;
        private const float FaceSize = 24f;
        private const string BackLabel = "< Back";
        private const string LeftRoom = "left the room";
        private const string NoIssues = "no issues";
        private const string DroppedFormat = "   · muted, {0} packets ignored";
        private const string VoiceLabel = "Voice volume";
        private const float VolumeWidth = 360f;

        private readonly UiFactory _ui;
        private readonly FindingLog _findings;
        private readonly FeedFilter _filter;
        private readonly PlayerActions _actions;
        private readonly GameObject _root;
        private readonly Image _avatar;
        private readonly TextMeshProUGUI _name;
        private readonly Image _face;
        private readonly TextMeshProUGUI _problems;
        private readonly (Button Button, TextMeshProUGUI Label) _mute;
        private readonly (Button Button, TextMeshProUGUI Label) _kick;
        private readonly (Button Button, TextMeshProUGUI Label) _ban;
        private readonly RectTransform _voice;
        private readonly Slider _volume;
        private readonly TextMeshProUGUI _percent;
        private readonly FindingFeed _feed;
        private string _title;

        public PlayerProfileView(UiFactory ui, Transform parent, FindingLog findings, FeedFilter filter, PlayerActions actions, Action<FindingLog.Finding> fix, Action back)
        {
            _ui = ui;
            _findings = findings;
            _filter = filter;
            _actions = actions;

            RectTransform root = ui.Column(parent, 8f, new RectOffset(0, 0, 0, 0));
            ui.Flexible(root, 1f, 1f);
            _root = root.gameObject;
            RectTransform header = ui.Row(root, 10f);
            ui.Button(header, BackLabel, ButtonWidth, ButtonSize, () => back());
            _avatar = ui.Icon(header, ui.Sprites.Box, AvatarSize);
            _name = ui.Label(header, string.Empty, NameSize, UiPalette.Text, false);
            ui.Flexible(_name, 1f, 0f);
            _mute = ui.Button(header, string.Empty, ButtonWidth, ButtonSize, () => actions.SwitchMute(actions.Find(Actor)));
            _kick = ui.Button(header, string.Empty, ButtonWidth, ButtonSize, () => actions.Kick(actions.Find(Actor)));
            _ban = ui.Button(header, string.Empty, ButtonWidth, ButtonSize, () => actions.Ban(actions.Find(Actor)));
            _voice = ui.Row(root, 10f);
            ui.Label(_voice, VoiceLabel, LineSize, UiPalette.Text, false);
            _volume = ui.Slider(_voice, VolumeWidth, OnVolumeChanged);
            _percent = ui.Label(_voice, string.Empty, LineSize, UiPalette.Notice, false);
            RectTransform status = ui.Row(root, 8f);
            _face = ui.Icon(status, ui.Faces.Clean, FaceSize);
            _problems = ui.Label(status, string.Empty, LineSize, UiPalette.Clean, false);
            _problems.richText = true;
            ui.Flexible(_problems, 1f, 0f);
            _feed = new FindingFeed(ui, root, null, false, fix);
            _root.SetActive(false);
        }

        public int Actor { get; private set; }

        public void Open(int actor, string name)
        {
            Actor = actor;
            _title = name;
            _feed.Actor = actor;
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            Actor = 0;
            _root.SetActive(false);
        }

        public void Refresh()
        {
            PhotonPlayer player = _actions.Find(Actor);
            bool muted = _actions.IsMuted(player);
            PlayerStanding standing = _findings.StandingOf(Actor);
            _name.text = $"{_title}   #{Actor}" + (player == null ? $" — {LeftRoom}" : string.Empty);
            _problems.text = (_findings.ProblemsOf(Actor) ?? NoIssues) + (muted ? RichText.Warning(string.Format(DroppedFormat, _actions.DroppedFrom(player))) : string.Empty);
            _problems.color = _actions.ColorOf(standing);
            _face.sprite = _ui.Faces.Of(standing);
            _mute.Label.text = _actions.MuteLabelOf(player);
            _kick.Label.text = _actions.KickLabelOf(Actor);
            _ban.Label.text = _actions.BanLabelOf(Actor);

            if (player != null && !player.IsLocal && !player.IsMasterClient)
                _mute.Button.gameObject.SetActive(true);
            else
                _mute.Button.gameObject.SetActive(false);

            if (player != null && PhotonNetwork.IsMasterClient && !player.IsLocal)
            {
                _kick.Button.gameObject.SetActive(true);
                _ban.Button.gameObject.SetActive(true);
            }
            else
            {
                _kick.Button.gameObject.SetActive(false);
                _ban.Button.gameObject.SetActive(false);
            }

            if (player != null && !player.IsLocal)
            {
                _voice.gameObject.SetActive(true);
                _volume.SetValueWithoutNotify(_actions.VoiceLevelOf(player));
                _percent.text = _actions.VoicePercentOf(_volume.value);
            }
            else
            {
                _voice.gameObject.SetActive(false);
            }

            if (player != null)
                _actions.ShowAvatar(_avatar, player);

            _feed.Show(_findings, _filter);
        }

        private void OnVolumeChanged(float level)
        {
            _actions.SetVoiceLevel(_actions.Find(Actor), level);
            _percent.text = _actions.VoicePercentOf(level);
        }
    }
}
