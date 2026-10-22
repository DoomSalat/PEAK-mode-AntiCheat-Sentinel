using System;
using System.Collections.Generic;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using PeakSentinel.Ui.Pages;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class SentinelWindow : MonoBehaviour
    {
        private const string CanvasName = "PeakSentinelCanvas";
        private const int SortingOrder = 260;
        private const float Width = 1100f;
        private const float Height = 900f;
        private const float TopMargin = 60f;
        private const float RefreshSeconds = 0.25f;
        private const float TitleSize = 28f;
        private const float TabSize = 21f;
        private const float LobbySize = 19f;
        private const string NoLobby = "Not in a lobby";
        private const string HostIsYou = "Lobby protected: you are the host";
        private const string HostRuns = "Lobby protected: host {0} runs Peak Sentinel";
        private const string HostLacks = "Lobby not protected: host {0} has no Peak Sentinel";
        private const string AscentFormat = "True Final Ascent: {0}";

        private static readonly string[] Tabs = { "Log", "Items", "Cheats", "Other", "Players", "Bans", "Speedrun" }; // Static: fixed tab captions, in page order

        private readonly FeedFilter _filter = new FeedFilter();
        private readonly AvatarCache _avatars = new AvatarCache();
        private readonly List<(GameObject Root, ITabPage Page)> _pages = new List<(GameObject Root, ITabPage Page)>();
        private readonly List<Image> _tabImages = new List<Image>();
        private string _title;
        private SentinelSettings _settings;
        private WindowServices _services;
        private UiFactory _ui;
        private SettingsBinder _binder;
        private HotkeyButton _hotkey;
        private TextMeshProUGUI _lobby;
        private TextMeshProUGUI _mode;
        private GameObject _canvas;
        private int _tab;
        private float _nextRefresh;

        public bool IsOpen { get; private set; }

        public void Initialize(string title, SentinelSettings settings, WindowServices services)
        {
            _title = title;
            _settings = settings;
            _services = services;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (_hotkey != null && _hotkey.Listening)
                _hotkey.Capture(keyboard);
            else if (_settings.Manager.ToggleKey.Value != Key.None && keyboard[_settings.Manager.ToggleKey.Value].wasPressedThisFrame)
                SwitchVisibility();

            if (IsOpen && Time.unscaledTime >= _nextRefresh)
                Refresh();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas);
        }

        private void SwitchVisibility()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        private void Open()
        {
            TMP_FontAsset font = GameFonts.Main();

            if (_canvas == null || _ui.Font != font)
                Build(font);

            EnsureEventSystem();
            _canvas.SetActive(true);
            IsOpen = true;
            Refresh();
        }

        private void Close()
        {
            _hotkey.StopListening();
            _canvas.SetActive(false);
            IsOpen = false;
        }

        private void Build(TMP_FontAsset font)
        {
            if (_canvas != null)
                Destroy(_canvas);

            _pages.Clear();
            _tabImages.Clear();
            _ui = new UiFactory(font);
            _binder = new SettingsBinder(_ui);
            _canvas = _ui.Canvas(CanvasName, SortingOrder);
            DontDestroyOnLoad(_canvas);

            RectTransform panel = _ui.Panel(_canvas.transform, new Vector2(Width, Height), TopMargin);
            RectTransform layout = _ui.Column(panel, 10f, new RectOffset(20, 20, 12, 20));
            _ui.Stretch(layout);
            BuildHeader(layout, panel);
            BuildTabs(layout);

            RectTransform body = _ui.Column(layout, 0f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(body, 1f, 1f);
            PageContext context = new PageContext(_ui, _binder, _filter, _services.Findings, Refresh, Fix, ExportReport);
            AddPage(body, page => new LogPage(context, page));
            AddPage(body, page => new ItemsPage(context, page, _settings.Protection));
            AddPage(body, page => new CheatsPage(context, page, _settings.Detection));
            AddPage(body, page => new OtherPage(context, page, _settings.Other));
            AddPage(body, page => new PlayersPage(context, page, _services, _avatars));
            AddPage(body, page => new BansPage(_ui, page, _services.Bans));
            AddPage(body, page => new SpeedrunPage(context, page, _services, _settings.Speedrun));
            Select(_tab);
        }

        private void BuildHeader(Transform layout, RectTransform panel)
        {
            RectTransform header = _ui.Row(layout, 10f);
            TextMeshProUGUI title = _ui.Label(header, _title, TitleSize, UiPalette.Text, false);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.raycastTarget = true;
            title.gameObject.AddComponent<DragHandle>().Initialize(panel, _canvas.GetComponent<Canvas>());
            _ui.Flexible(title, 1f, 0f);
            _mode = _ui.Label(header, string.Empty, LobbySize, UiPalette.Text, false);
            _mode.alignment = TextAlignmentOptions.MidlineRight;
            _lobby = _ui.Label(header, string.Empty, LobbySize, UiPalette.Notice, false);
            _lobby.alignment = TextAlignmentOptions.MidlineRight;
            _hotkey = new HotkeyButton(_ui, header, _settings.Manager.ToggleKey);
        }

        private void BuildTabs(Transform layout)
        {
            RectTransform tabs = _ui.Row(layout, 8f);

            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                _tabImages.Add(_ui.Button(tabs, Tabs[i], 0f, TabSize, () => Select(index)).Button.image);
            }
        }

        private void AddPage(RectTransform body, Func<Transform, ITabPage> create)
        {
            RectTransform root = _ui.Column(body, 8f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(root, 1f, 1f);
            _pages.Add((root.gameObject, create(root)));
        }

        private void Select(int index)
        {
            _tab = index;

            for (int i = 0; i < _pages.Count; i++)
            {
                if (i == index)
                    _pages[i].Root.SetActive(true);
                else
                    _pages[i].Root.SetActive(false);

                _tabImages[i].sprite = i == index ? _ui.Sprites.ButtonSelected : _ui.Sprites.ButtonIdle;
            }

            Refresh();
        }

        private void Refresh()
        {
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            ShowLobby();
            _binder.Refresh();
            _pages[_tab].Page.Refresh();
        }

        private void ShowLobby()
        {
            PhotonPlayer host = PhotonNetwork.InRoom ? PhotonNetwork.MasterClient : null;
            string mode = _services.Ascent.ModeName;
            _mode.text = mode == null ? string.Empty : string.Format(AscentFormat, mode);

            if (host == null)
            {
                _lobby.text = NoLobby;
                _lobby.color = UiPalette.Notice;
                return;
            }

            bool guarded = _services.Presence.Runs(host);
            _lobby.text = host.IsLocal ? HostIsYou : string.Format(guarded ? HostRuns : HostLacks, PlayerNames.Name(host));
            _lobby.color = guarded ? UiPalette.Clean : UiPalette.Suspicious;
        }

        private string ExportReport()
        {
            return _services.Export.Save(PhotonNetwork.CurrentRoom == null ? "none" : PhotonNetwork.CurrentRoom.Name);
        }

        private void Fix(FindingLog.Finding finding)
        {
            _services.Report.Fix(finding);
            Refresh();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject system = new GameObject(nameof(EventSystem), typeof(EventSystem), typeof(InputSystemUIInputModule));
            system.transform.SetParent(_canvas.transform, false);
        }
    }
}
