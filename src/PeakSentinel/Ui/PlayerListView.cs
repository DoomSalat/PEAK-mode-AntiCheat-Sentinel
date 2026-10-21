using System;
using System.Collections.Generic;
using System.Text;
using PeakSentinel.Reporting;
using Photon.Pun;
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

        private readonly StringBuilder _signature = new StringBuilder();
        private readonly Dictionary<int, TextMeshProUGUI> _pings = new Dictionary<int, TextMeshProUGUI>();
        private readonly UiFactory _ui;
        private readonly WindowServices _services;
        private readonly PlayerActions _actions;
        private readonly PlayerProfileView _profile;
        private readonly GameObject _list;
        private readonly RectTransform _rows;
        private string _shownRows;

        public PlayerListView(UiFactory ui, Transform parent, FeedFilter filter, WindowServices services, AvatarCache avatars, Action<FindingLog.Finding> fix)
        {
            _ui = ui;
            _services = services;
            _actions = new PlayerActions(services.Mutes, services.BanGuard, avatars, Refresh);

            (ScrollRect scroll, RectTransform rows) = ui.Scroll(parent, 8f);
            _list = scroll.gameObject;
            _rows = rows;
            _profile = new PlayerProfileView(ui, parent, services.Findings, filter, _actions, fix, CloseProfile);
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

            if (signature == _shownRows)
            {
                ShowPings();
                return;
            }

            _shownRows = signature;
            _pings.Clear();

            for (int i = _rows.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_rows.GetChild(i).gameObject);

            if (!PhotonNetwork.InRoom)
            {
                _ui.Label(_rows, NotInRoom, LineSize, UiPalette.Text, false);
                return;
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                AddRow(player);

            ShowPings();
        }

        private void AddRow(PhotonPlayer player)
        {
            FindingLog findings = _services.Findings;
            string name = PlayerNames.Name(player);
            int actor = player.ActorNumber;
            Button row = _ui.RowButton(_rows, () => OpenProfile(actor, name));
            _actions.ShowAvatar(_ui.Icon(row.transform, _ui.Sprites.Box, AvatarSize), player);
            RectTransform text = _ui.Column(row.transform, 2f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(text, 1f, 0f);
            _ui.Label(text, $"{name}{(player.IsLocal ? YouLabel : string.Empty)}{(player.IsMasterClient ? HostLabel : string.Empty)}   #{actor}", NameSize, UiPalette.Text, false);
            PlayerStanding standing = findings.StandingOf(actor);
            RectTransform status = _ui.Row(text, 8f);
            _ui.Icon(status, _ui.Faces.Of(standing), FaceSize);

            if (_services.Presence.Runs(player))
                _ui.Icon(status, _ui.Shield, FaceSize);

            bool muted = _actions.IsMuted(player);
            _ui.Flexible(_ui.Label(status, (findings.ProblemsOf(actor) ?? NoIssues) + (muted ? RichText.Warning(MutedNote) : string.Empty), LineSize, _actions.ColorOf(standing), true), 1f, 0f);

            RectTransform side = _ui.Column(row.transform, 4f, new RectOffset(0, 0, 0, 0));
            AddButtons(_ui.Row(side, 8f), player);
            TextMeshProUGUI ping = _ui.Label(side, string.Empty, PingSize, UiPalette.Notice, true);
            ping.alignment = TextAlignmentOptions.BottomRight;
            ping.textWrappingMode = TextWrappingModes.NoWrap;
            _pings[actor] = ping;
        }

        private void AddButtons(Transform buttons, PhotonPlayer player)
        {
            int actor = player.ActorNumber;

            if (player.IsLocal)
                return;

            _ui.Button(buttons, ResyncLabel, ResyncWidth, ButtonSize, () => _services.Desync.Resync(player));

            if (player.IsMasterClient)
                return;

            _ui.Button(buttons, _actions.MuteLabelOf(player), ButtonWidth, ButtonSize, () => _actions.SwitchMute(player));

            if (!PhotonNetwork.IsMasterClient)
                return;

            _ui.Button(buttons, _actions.KickLabelOf(actor), ButtonWidth, ButtonSize, () => _actions.Kick(player));
            _ui.Button(buttons, _actions.BanLabelOf(actor), ButtonWidth, ButtonSize, () => _actions.Ban(player));
        }

        private void ShowPings()
        {
            foreach (KeyValuePair<int, TextMeshProUGUI> entry in _pings)
            {
                PhotonPlayer player = _actions.Find(entry.Key);
                int? ping = player != null && player.IsLocal ? PhotonNetwork.GetPing() : _services.Latency.PingOf(entry.Key);
                entry.Value.text = ping.HasValue ? RichText.Paint(string.Format(PingFormat, ping.Value), PingColor(ping.Value)) : NoPing;
            }
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
            _signature.Append(PhotonNetwork.InRoom).Append(PhotonNetwork.IsMasterClient).Append(_services.Findings.Version).Append('|').Append(_actions.State);

            if (!PhotonNetwork.InRoom)
                return _signature.ToString();

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                _signature.Append('|').Append(player.ActorNumber).Append(player.NickName).Append(player.IsMasterClient).Append(_services.Presence.Runs(player));

            return _signature.ToString();
        }

        private void OpenProfile(int actor, string name)
        {
            _list.SetActive(false);
            _profile.Open(actor, name);
        }

        private void CloseProfile()
        {
            _profile.Close();
            _shownRows = null;
            _list.SetActive(true);
            RefreshList();
        }
    }
}
