using System;
using System.Text;
using PeakSentinel.Protection;
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
        private const float RemovalWidth = 160f;
        private const float AvatarSize = 56f;
        private const float ProfileAvatarSize = 44f;
        private const float FaceSize = 24f;
        private const string NoIssues = "no issues";
        private const string KickLabel = "Kick";
        private const string ConfirmKickLabel = "Confirm kick?";
        private const string BanLabel = "Ban";
        private const string ConfirmBanLabel = "Confirm ban?";
        private const string BackLabel = "< Back";
        private const string YouLabel = " (you)";
        private const string HostLabel = " [host]";
        private const string NotInRoom = "not in a room";
        private const string LeftRoom = "left the room";

        private readonly StringBuilder _signature = new StringBuilder();
        private readonly UiFactory _ui;
        private readonly FindingLog _findings;
        private readonly FeedFilter _filter;
        private readonly BanGuard _banGuard;
        private readonly AvatarCache _avatars;
        private readonly GameObject _list;
        private readonly RectTransform _rows;
        private readonly GameObject _profile;
        private readonly Image _profileAvatar;
        private readonly TextMeshProUGUI _profileName;
        private readonly Image _profileFace;
        private readonly TextMeshProUGUI _profileProblems;
        private readonly (Button Button, TextMeshProUGUI Label) _profileKick;
        private readonly (Button Button, TextMeshProUGUI Label) _profileBan;
        private readonly FindingFeed _profileFeed;
        private string _shownRows;
        private int _profileActor;
        private string _profileTitle;
        private int _kickArmedActor;
        private int _banArmedActor;

        public PlayerListView(UiFactory ui, Transform parent, FindingLog findings, FeedFilter filter, BanGuard banGuard, AvatarCache avatars, Action<FindingLog.Finding> fix)
        {
            _avatars = avatars;
            _ui = ui;
            _findings = findings;
            _filter = filter;
            _banGuard = banGuard;

            (ScrollRect scroll, RectTransform rows) = ui.Scroll(parent, 8f);
            _list = scroll.gameObject;
            _rows = rows;

            RectTransform profile = ui.Column(parent, 8f, new RectOffset(0, 0, 0, 0));
            ui.Flexible(profile, 1f, 1f);
            _profile = profile.gameObject;
            RectTransform header = ui.Row(profile, 10f);
            ui.Button(header, BackLabel, RemovalWidth, ButtonSize, CloseProfile);
            _profileAvatar = ui.Icon(header, ui.Sprites.Box, ProfileAvatarSize);
            _profileName = ui.Label(header, string.Empty, NameSize, UiPalette.Text, false);
            ui.Flexible(_profileName, 1f, 0f);
            _profileKick = ui.Button(header, KickLabel, RemovalWidth, ButtonSize, () => Kick(FindPlayer(_profileActor)));
            _profileBan = ui.Button(header, BanLabel, RemovalWidth, ButtonSize, () => Ban(FindPlayer(_profileActor)));
            RectTransform status = ui.Row(profile, 8f);
            _profileFace = ui.Icon(status, ui.Faces.Clean, FaceSize);
            _profileProblems = ui.Label(status, string.Empty, LineSize, UiPalette.Clean, false);
            ui.Flexible(_profileProblems, 1f, 0f);
            _profileFeed = new FindingFeed(ui, profile, null, false, fix);
            _profile.SetActive(false);
        }

        public void Refresh()
        {
            if (_profileActor != 0)
                RefreshProfile();
            else
                RefreshList();
        }

        private void RefreshList()
        {
            string signature = ListSignature();

            if (signature == _shownRows)
                return;

            _shownRows = signature;

            for (int i = _rows.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_rows.GetChild(i).gameObject);

            if (!PhotonNetwork.InRoom)
            {
                _ui.Label(_rows, NotInRoom, LineSize, UiPalette.Text, false);
                return;
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                AddRow(player);
        }

        private void AddRow(PhotonPlayer player)
        {
            string name = PlayerNames.Name(player);
            string problems = _findings.ProblemsOf(player.ActorNumber);
            int actor = player.ActorNumber;
            Button row = _ui.RowButton(_rows, () => OpenProfile(actor, name));
            ShowAvatar(_ui.Icon(row.transform, _ui.Sprites.Box, AvatarSize), player);
            RectTransform text = _ui.Column(row.transform, 2f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(text, 1f, 0f);
            _ui.Label(text, $"{name}{(player.IsLocal ? YouLabel : string.Empty)}{(player.IsMasterClient ? HostLabel : string.Empty)}   #{actor}", NameSize, UiPalette.Text, false);
            PlayerStanding standing = _findings.StandingOf(actor);
            RectTransform status = _ui.Row(text, 8f);
            _ui.Icon(status, _ui.Faces.Of(standing), FaceSize);
            _ui.Flexible(_ui.Label(status, problems ?? NoIssues, LineSize, ColorOf(standing), false), 1f, 0f);

            if (!PhotonNetwork.IsMasterClient || player.IsLocal)
                return;

            _ui.Button(row.transform, _kickArmedActor == actor ? ConfirmKickLabel : KickLabel, RemovalWidth, ButtonSize, () => Kick(player));
            _ui.Button(row.transform, _banArmedActor == actor ? ConfirmBanLabel : BanLabel, RemovalWidth, ButtonSize, () => Ban(player));
        }

        private string ListSignature()
        {
            _signature.Clear();
            _signature.Append(PhotonNetwork.InRoom).Append(PhotonNetwork.IsMasterClient).Append(_findings.Version).Append('|').Append(_avatars.Version).Append('|').Append(_kickArmedActor).Append('|').Append(_banArmedActor);

            if (!PhotonNetwork.InRoom)
                return _signature.ToString();

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                _signature.Append('|').Append(player.ActorNumber).Append(player.NickName).Append(player.IsMasterClient);

            return _signature.ToString();
        }

        private void RefreshProfile()
        {
            PhotonPlayer player = FindPlayer(_profileActor);
            bool removable = player != null && PhotonNetwork.IsMasterClient && !player.IsLocal;
            string problems = _findings.ProblemsOf(_profileActor);
            _profileName.text = $"{_profileTitle}   #{_profileActor}" + (player == null ? $" — {LeftRoom}" : string.Empty);
            _profileProblems.text = problems ?? NoIssues;
            PlayerStanding standing = _findings.StandingOf(_profileActor);
            _profileProblems.color = ColorOf(standing);
            _profileFace.sprite = _ui.Faces.Of(standing);

            if (player != null)
                ShowAvatar(_profileAvatar, player);
            _profileKick.Label.text = _kickArmedActor == _profileActor ? ConfirmKickLabel : KickLabel;
            _profileBan.Label.text = _banArmedActor == _profileActor ? ConfirmBanLabel : BanLabel;

            if (removable)
            {
                _profileKick.Button.gameObject.SetActive(true);
                _profileBan.Button.gameObject.SetActive(true);
            }
            else
            {
                _profileKick.Button.gameObject.SetActive(false);
                _profileBan.Button.gameObject.SetActive(false);
            }

            _profileFeed.Show(_findings, _filter);
        }

        private void OpenProfile(int actor, string name)
        {
            _profileActor = actor;
            _profileTitle = name;
            _profileFeed.Actor = actor;
            _list.SetActive(false);
            _profile.SetActive(true);
            RefreshProfile();
        }

        private void CloseProfile()
        {
            _profileActor = 0;
            _shownRows = null;
            _profile.SetActive(false);
            _list.SetActive(true);
            RefreshList();
        }

        private void Kick(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_kickArmedActor != player.ActorNumber)
            {
                _kickArmedActor = player.ActorNumber;
                Refresh();
                return;
            }

            _kickArmedActor = 0;
            PlayerHandler.Kick(player.ActorNumber);
            Refresh();
        }

        private void Ban(PhotonPlayer player)
        {
            if (player == null)
                return;

            if (_banArmedActor != player.ActorNumber)
            {
                _banArmedActor = player.ActorNumber;
                Refresh();
                return;
            }

            _banArmedActor = 0;
            _banGuard.Ban(player);
            Refresh();
        }

        private void ShowAvatar(Image image, PhotonPlayer player)
        {
            Sprite avatar = _avatars.Of(player);

            if (avatar == null || image.sprite == avatar)
                return;

            image.sprite = avatar;
            image.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        }

        private Color ColorOf(PlayerStanding standing)
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

        private PhotonPlayer FindPlayer(int actor)
        {
            return PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(actor);
        }
    }
}
