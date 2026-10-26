using System;
using System.Collections.Generic;
using System.Text;
using PeakSentinel.Reporting;
using Photon.Pun;
using Portningsbolaget.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class PlayerListView
    {
        private const float NameSize = 22f;
        private const float LineSize = 19f;
        private const float ButtonSize = 19f;
        private const float ButtonWidth = 160f;
        private const float AvatarSize = 56f;
        private const float FaceSize = 24f;
        private const float PingSize = 16f;
        private const int GoodPing = 150;
        private const int BadPing = 300;
        private const string NoIssues = "no issues";
        private const string YouLabel = " (you)";
        private const string HostLabel = " [host]";
        private const string NotInRoom = "not in a room";
        private const string PingFormat = "ping {0} ms";
        private const string NoPing = "ping —";
        private const string ResyncLabel = "Resync";
        private const float ResyncWidth = 120f;
        private const string MutedNote = "   · muted";
        private const string ResyncAllLabel = "Resync all";
        private const float ResyncAllWidth = 160f;
        private const string TrustedNote = "   · trusted";

        private readonly StringBuilder _signature = new StringBuilder();
        private readonly Dictionary<int, PlayerRow> _rowViews = new Dictionary<int, PlayerRow>();
        private readonly UiFactory _ui;
        private readonly WindowServices _services;
        private readonly PlayerActions _actions;
        private readonly PlayerLooks _looks;
        private readonly PlayerProfileView _profile;
        private readonly GameObject _list;
        private readonly RectTransform _rows;
        private string _shownSignature;

        public PlayerListView(UiFactory ui, Transform parent, FeedFilter filter, WindowServices services, AvatarCache avatars, Action<FindingLog.Finding> fix)
        {
            _ui = ui;
            _services = services;
            _actions = new PlayerActions(services.Report, services.Mutes, services.BanGuard, services.Kicker, services.Report.Trust, services.Teleporter, Refresh);
            _looks = new PlayerLooks(avatars);
            RectTransform header = ui.Row(parent, 8f);
            ui.Spacer(header);
            ui.Button(header, ResyncAllLabel, ResyncAllWidth, ButtonSize, () => services.Desync.ResyncAll());

            (ScrollRect scroll, RectTransform rows) = ui.Scroll(parent, 8f);
            _list = scroll.gameObject;
            _rows = rows;
            _profile = new PlayerProfileView(ui, parent, services.Findings, filter, _actions, _looks, new VoiceLevels(), fix, CloseProfile);
        }

        public void Refresh()
        {
            if (_profile.Actor != 0)
                _profile.Refresh();
            else
                RefreshList();
        }

        private void RefreshList()
        {
            string signature = ListSignature();

            if (signature == _shownSignature)
            {
                ShowRows();
                return;
            }

            _shownSignature = signature;
            _rowViews.Clear();

            for (int i = _rows.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_rows.GetChild(i).gameObject);

            if (!PhotonNetwork.InRoom)
            {
                _ui.Label(_rows, NotInRoom, LineSize, UiPalette.Text, false);
                return;
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                AddRow(player);

            ShowRows();
        }

        private void AddRow(PhotonPlayer player)
        {
            int actor = player.ActorNumber;
            Button row = _ui.RowButton(_rows, () => OpenProfile(actor));
            RectTransform card = _ui.Column(row.transform, 6f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(card, 1f, 0f);
            RectTransform top = _ui.Row(card, 8f);
            _looks.ShowAvatar(_ui.Icon(top, _ui.Sprites.Box, AvatarSize), player);
            RectTransform text = _ui.Column(top, 2f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(text, 1f, 0f);
            TextMeshProUGUI name = _ui.Label(text, string.Empty, NameSize, UiPalette.Text, false);
            RectTransform status = _ui.Row(text, 8f);
            Image face = _ui.Icon(status, _ui.Faces.Clean, FaceSize);
            TextMeshProUGUI life = _ui.Label(status, string.Empty, LineSize, UiPalette.Notice, false);
            life.textWrappingMode = TextWrappingModes.NoWrap;

            if (_services.Presence.Runs(player))
                _ui.Icon(status, _ui.Shield, FaceSize);

            AddPlatform(status, PlayerPlatforms.FamilyOf(player));

            TextMeshProUGUI ping = _ui.Label(top, string.Empty, PingSize, UiPalette.Notice, true);
            ping.alignment = TextAlignmentOptions.TopRight;
            ping.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform buttons = _ui.Row(card, 8f);
            _ui.Spacer(buttons);
            AddButtons(buttons, player);

            TextMeshProUGUI problems = _ui.Label(card, string.Empty, LineSize, UiPalette.Clean, true);
            _rowViews[actor] = new PlayerRow(name, face, life, problems, ping);
        }

        private void ShowRows()
        {
            FindingLog findings = _services.Findings;

            foreach (KeyValuePair<int, PlayerRow> entry in _rowViews)
            {
                PhotonPlayer player = _actions.Find(entry.Key);

                if (player == null)
                    continue;

                PlayerRow row = entry.Value;
                PlayerStanding standing = findings.StandingOf(entry.Key);
                row.Name.text = $"{PlayerNames.Name(player)}{(player.IsLocal ? YouLabel : string.Empty)}{(player.IsMasterClient ? HostLabel : string.Empty)}   #{entry.Key}";
                row.Face.sprite = _ui.Faces.Of(standing);
                (string lifeText, Color lifeColor) = _looks.LifeOf(player);
                row.Life.text = lifeText;
                row.Life.color = lifeColor;
                row.Problems.text = (findings.ProblemsOf(entry.Key) ?? NoIssues) + (_actions.IsMuted(player) ? RichText.Warning(MutedNote) : string.Empty) + (_actions.IsTrusted(player) ? RichText.Warning(TrustedNote) : string.Empty);
                row.Problems.color = _looks.ColorOf(standing);
                int? ping = player.IsLocal ? PhotonNetwork.GetPing() : _services.Latency.PingOf(entry.Key);
                row.Ping.text = ping.HasValue ? RichText.Paint(string.Format(PingFormat, ping.Value), PingColor(ping.Value)) : NoPing;
            }
        }

        private void AddPlatform(Transform status, PlatformUtility.PlatformFamily family)
        {
            if (family == PlatformUtility.PlatformFamily.None)
                return;

            Color tint = _ui.Platforms.TintOf(family);
            _ui.Icon(status, _ui.Platforms.Of(family), FaceSize).color = tint;
            _ui.Label(status, PlayerPlatforms.NameOf(family), LineSize, tint, false).textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void AddButtons(Transform buttons, PhotonPlayer player)
        {
            int actor = player.ActorNumber;

            if (player.IsLocal)
            {
                _ui.Button(buttons, ResyncLabel, ResyncWidth, ButtonSize, () => _services.Desync.ResyncMe());
                return;
            }

            _ui.Button(buttons, ResyncLabel, ResyncWidth, ButtonSize, () => _services.Desync.Resync(player));

            if (player.IsMasterClient)
                return;

            _ui.Button(buttons, _actions.MuteLabelOf(player), ButtonWidth, ButtonSize, () => _actions.SwitchMute(player));

            if (!PhotonNetwork.IsMasterClient)
                return;

            _ui.Button(buttons, _actions.BringLabelOf(actor), ButtonWidth, ButtonSize, () => _actions.Bring(player));
            _ui.Button(buttons, _actions.KickLabelOf(actor), ButtonWidth, ButtonSize, () => _actions.Kick(player));
            _ui.Button(buttons, _actions.BanLabelOf(actor), ButtonWidth, ButtonSize, () => _actions.Ban(player));
        }

        private Color PingColor(int ping)
        {
            if (ping <= GoodPing)
                return UiPalette.Clean;

            return ping <= BadPing ? UiPalette.Suspicious : UiPalette.Cheat;
        }

        private string ListSignature()
        {
            _signature.Clear();
            _signature.Append(PhotonNetwork.InRoom).Append(PhotonNetwork.IsMasterClient).Append('|').Append(_actions.State).Append('|').Append(_looks.Version);

            if (!PhotonNetwork.InRoom)
                return _signature.ToString();

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                _signature.Append('|').Append(player.ActorNumber).Append(player.IsMasterClient).Append(_services.Presence.Runs(player)).Append(PlayerPlatforms.FamilyOf(player));

            return _signature.ToString();
        }

        private void OpenProfile(int actor)
        {
            _list.SetActive(false);
            _profile.Open(actor, PlayerNames.Name(_actions.Find(actor)));
        }

        private void CloseProfile()
        {
            _profile.Close();
            _shownSignature = null;
            _list.SetActive(true);
            RefreshList();
        }
    }
}
