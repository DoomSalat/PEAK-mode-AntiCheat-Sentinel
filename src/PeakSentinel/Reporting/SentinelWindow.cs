using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using PeakSentinel.Detection;
using PeakSentinel.Moderation;
using PeakSentinel.Reporting.Ui;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PeakSentinel.Reporting
{
    public sealed class SentinelWindow : MonoBehaviour
    {
        private const string CanvasName = "PeakSentinelCanvas";
        private const int SortingOrder = 260;
        private const float Width = 1100f;
        private const float Height = 760f;
        private const float TopMargin = 60f;
        private const float RefreshSeconds = 0.25f;
        private const float TitleSize = 28f;
        private const float TabSize = 21f;
        private const float HeaderSize = 24f;
        private const float TextSize = 20f;
        private const float BlockToggleWidth = 210f;
        private const float OptionToggleWidth = 200f;
        private const float ClearWidth = 140f;
        private const string HostOnly = "Blocking and kick work only while you are the host";
        private const string ItemsHeader = "Item spawn blocker";
        private const string CheatsHeader = "General cheat detection";
        private const string OtherHeader = "Other protection";
        private const string IgnoreKicksLabel = "Protect me: ignore vanilla kicks from other players (no ragdoll, no push)";
        private const string ShieldEveryoneLabel = "Protect everyone when I am host: stand up anyone kicked while kicks are off in this run";
        private const string ShieldAsClientLabel = "Protect everyone also when I am a client";
        private const string ShieldAsClientWarning = "(other anticheats may see this as suspicious)";
        private const string BlockToggleLabel = "Block";
        private const string KickToggleLabel = "Kick (host)";
        private const string SnapBackToggleLabel = "Snap back (host)";
        private const string HarmLabel = "Kills, knock-outs, curses, teleports and flings of other players";
        private const string WorldLabel = "Host-only events sent by a non-host (end game, revive, fog, bridges...)";
        private const string DangerousLabel = "Bee swarms, tornados and Bing Bong spawned by a non-host";
        private const string CheatModsLabel = "Cherry and Atlas cheat mod markers";
        private const string FlightLabel = "Flying: staying in the air without gravity (numbers in PeakSentinel.log)";
        private const string BlockLabel = "Block cheat item spawns (spawners, forged pickups and inventories)";
        private const string AllowMineLabel = "Allow my own spawns (host)";
        private const string GuardHandsLabel = "Refuse items forced into my hands (when not host)";
        private const string NoticesLabel = "Show notices";
        private const string FollowLabel = "Follow newest";
        private const string ClearLabel = "Clear";
        private const string SearchHint = "Search nick or text...";
        private const string CapacityLabel = "Keep at most";
        private const string StoredFormat = "Stored findings: {0} / {1}";
        private const float CapacityWidth = 120f;
        private const float DisabledAlpha = 0.45f;
        private const float DependentIndent = 38f;

        private static readonly string[] Tabs = { "Log", "Items", "Cheats", "Other", "Players", "Bans" }; // Static: fixed tab captions

        private readonly FeedFilter _filter = new FeedFilter();
        private readonly AvatarCache _avatars = new AvatarCache();
        private readonly List<(Toggle Toggle, ConfigEntry<bool> Entry)> _bindings = new List<(Toggle Toggle, ConfigEntry<bool> Entry)>();
        private readonly List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)> _dependents = new List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)>();
        private readonly List<(Toggle Notices, Toggle Follow, TMP_InputField Search)> _optionRows = new List<(Toggle Notices, Toggle Follow, TMP_InputField Search)>();
        private readonly List<GameObject> _hostNotes = new List<GameObject>();
        private readonly List<GameObject> _pages = new List<GameObject>();
        private readonly List<Image> _tabImages = new List<Image>();
        private ConfigEntry<Key> _toggleKey;
        private ProtectionSettings _protection;
        private DetectionSettings _detection;
        private OtherSettings _other;
        private FindingLog _findings;
        private BanList _bans;
        private BanGuard _banGuard;
        private string _title;
        private UiFactory _ui;
        private GameObject _canvas;
        private TextMeshProUGUI _storedLabel;
        private TMP_InputField _capacityInput;
        private FindingFeed _logFeed;
        private FindingFeed _itemsFeed;
        private FindingFeed _cheatsFeed;
        private FindingFeed _otherFeed;
        private PlayerListView _players;
        private BanListView _banList;
        private int _tab;
        private float _nextRefresh;

        public bool IsOpen { get; private set; }

        public void Initialize(string title, ConfigEntry<Key> toggleKey, ProtectionSettings protection, DetectionSettings detection, OtherSettings other, FindingLog findings, BanList bans, BanGuard banGuard)
        {
            _title = title;
            _toggleKey = toggleKey;
            _protection = protection;
            _detection = detection;
            _other = other;
            _findings = findings;
            _bans = bans;
            _banGuard = banGuard;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && _toggleKey.Value != Key.None && keyboard[_toggleKey.Value].wasPressedThisFrame)
            {
                if (IsOpen)
                    Close();
                else
                    Open();
            }

            if (IsOpen && Time.unscaledTime >= _nextRefresh)
                Refresh();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas);
        }

        private void Open()
        {
            TMP_FontAsset font = GameFont();

            if (_canvas == null || _ui.Font != font)
                Build(font);

            EnsureEventSystem();
            _canvas.SetActive(true);
            IsOpen = true;
            Refresh();
        }

        private void Close()
        {
            _canvas.SetActive(false);
            IsOpen = false;
        }

        private void Build(TMP_FontAsset font)
        {
            if (_canvas != null)
                Destroy(_canvas);

            _bindings.Clear();
            _dependents.Clear();
            _optionRows.Clear();
            _hostNotes.Clear();
            _pages.Clear();
            _tabImages.Clear();

            _ui = new UiFactory(font);
            _canvas = _ui.Canvas(CanvasName, SortingOrder);
            DontDestroyOnLoad(_canvas);
            RectTransform panel = _ui.Panel(_canvas.transform, new Vector2(Width, Height), TopMargin);
            RectTransform layout = _ui.Column(panel, 10f, new RectOffset(20, 20, 12, 20));
            _ui.Stretch(layout);

            TextMeshProUGUI title = _ui.Label(layout, $"{_title} — {_toggleKey.Value}", TitleSize, UiPalette.Text, false);
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = true;
            title.gameObject.AddComponent<DragHandle>().Initialize(panel, _canvas.GetComponent<Canvas>());

            RectTransform tabs = _ui.Row(layout, 8f);

            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                _tabImages.Add(_ui.Button(tabs, Tabs[i], 0f, TabSize, () => Select(index)).Button.image);
            }

            RectTransform body = _ui.Column(layout, 0f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(body, 1f, 1f);
            BuildLog(Page(body));
            BuildItems(Page(body));
            BuildCheats(Page(body));
            BuildOther(Page(body));
            BuildPlayers(Page(body));
            _banList = new BanListView(_ui, Page(body), _bans);
            Select(_tab);
        }

        private void BuildLog(RectTransform page)
        {
            RectTransform row = _ui.Row(page, 10f);
            _storedLabel = _ui.Label(row, string.Empty, TextSize, UiPalette.Text, false);
            _ui.Flexible(_storedLabel, 1f, 0f);
            _ui.Label(row, CapacityLabel, TextSize, UiPalette.Text, false);
            _capacityInput = _ui.Input(row, string.Empty, TextSize, CapacityWidth);
            _capacityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _capacityInput.onEndEdit.AddListener(SetCapacity);
            OptionsRow(page);
            _logFeed = new FindingFeed(_ui, page, null, true);
        }

        private void BuildItems(RectTransform page)
        {
            _ui.Label(page, ItemsHeader, HeaderSize, UiPalette.Text, false);
            Bind(page, BlockLabel, _protection.BlockCheatSpawns);
            Bind(page, AllowMineLabel, _protection.AllowMySpawns);
            Bind(page, GuardHandsLabel, _protection.GuardMyHands);
            HostNote(page);
            OptionsRow(page);
            _itemsFeed = new FindingFeed(_ui, page, FindingCategory.Items, true);
        }

        private void BuildCheats(RectTransform page)
        {
            _ui.Label(page, CheatsHeader, HeaderSize, UiPalette.Text, false);
            DetectionRow(page, _detection.Harm, HarmLabel, _detection.BlockHarm, BlockToggleLabel);
            DetectionRow(page, _detection.WorldEvents, WorldLabel, _detection.BlockWorldEvents, BlockToggleLabel);
            DetectionRow(page, _detection.DangerousSpawns, DangerousLabel, _detection.BlockDangerousSpawns, BlockToggleLabel);
            DetectionRow(page, _detection.Flight, FlightLabel, _detection.BlockFlight, SnapBackToggleLabel);
            DetectionRow(page, _detection.CheatMods, CheatModsLabel, _detection.KickCheatMods, KickToggleLabel);
            OptionsRow(page);
            _cheatsFeed = new FindingFeed(_ui, page, FindingCategory.Cheats, true);
        }

        private void BuildOther(RectTransform page)
        {
            _ui.Label(page, OtherHeader, HeaderSize, UiPalette.Text, false);
            Bind(page, IgnoreKicksLabel, _other.IgnoreKicks);
            Toggle everyone = Indented(page, 1, ShieldEveryoneLabel, _other.ShieldEveryone);
            Depend(everyone, () => _other.IgnoreKicks.Value);
            Toggle asClient = Indented(page, 2, $"{ShieldAsClientLabel} <color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Suspicious)}>{ShieldAsClientWarning}</color>", _other.ShieldEveryoneAsClient);
            Depend(asClient, () => _other.IgnoreKicks.Value && _other.ShieldEveryone.Value);
            OptionsRow(page);
            _otherFeed = new FindingFeed(_ui, page, FindingCategory.Other, true);
        }

        private void BuildPlayers(RectTransform page)
        {
            HostNote(page);
            _players = new PlayerListView(_ui, page, _findings, _filter, _banGuard, _avatars);
        }

        private RectTransform Page(RectTransform body)
        {
            RectTransform page = _ui.Column(body, 8f, new RectOffset(0, 0, 0, 0));
            _ui.Flexible(page, 1f, 1f);
            _pages.Add(page.gameObject);
            return page;
        }

        private Toggle Bind(Transform parent, string label, ConfigEntry<bool> entry)
        {
            Toggle toggle = _ui.Toggle(parent, label, TextSize, entry.Value, value => entry.Value = value);
            _bindings.Add((toggle, entry));
            return toggle;
        }

        private void DetectionRow(Transform parent, ConfigEntry<bool> detect, string label, ConfigEntry<bool> block, string blockLabel)
        {
            RectTransform row = _ui.Row(parent, 10f);
            _ui.Flexible(Bind(row, label, detect), 1f, 0f);
            Toggle blockToggle = Bind(row, blockLabel, block);
            _ui.Fixed(blockToggle, BlockToggleWidth, 0f);
            Depend(blockToggle, () => detect.Value);
        }

        private Toggle Indented(Transform parent, int level, string label, ConfigEntry<bool> entry)
        {
            RectTransform row = _ui.Row(parent, 0f);
            _ui.Fixed(_ui.Row(row, 0f), DependentIndent * level, 0f);
            Toggle toggle = Bind(row, label, entry);
            _ui.Flexible(toggle, 1f, 0f);
            return toggle;
        }

        private void Depend(Toggle toggle, Func<bool> enabled)
        {
            _dependents.Add((toggle, toggle.GetComponent<CanvasGroup>(), enabled));
        }

        private void OptionsRow(Transform parent)
        {
            RectTransform row = _ui.Row(parent, 10f);
            Toggle notices = _ui.Toggle(row, NoticesLabel, TextSize, _filter.ShowNotices, value => SetNotices(value));
            Toggle follow = _ui.Toggle(row, FollowLabel, TextSize, _filter.FollowNewest, value => _filter.FollowNewest = value);
            _ui.Fixed(notices, OptionToggleWidth, 0f);
            _ui.Fixed(follow, OptionToggleWidth, 0f);
            TMP_InputField search = _ui.Input(row, SearchHint, TextSize, 0f);
            search.onValueChanged.AddListener(SetSearch);
            _ui.Button(row, ClearLabel, ClearWidth, TextSize, _findings.Clear);
            _optionRows.Add((notices, follow, search));
        }

        private void HostNote(Transform parent)
        {
            _hostNotes.Add(_ui.Label(parent, HostOnly, TextSize, UiPalette.Suspicious, false).gameObject);
        }

        private void SetCapacity(string value)
        {
            if (int.TryParse(value, out int capacity))
                _findings.Resize(capacity);

            _capacityInput.SetTextWithoutNotify(_findings.Capacity.ToString());
            Refresh();
        }

        private void SetSearch(string value)
        {
            _filter.Search = value;
            Refresh();
        }

        private void SetNotices(bool value)
        {
            _filter.ShowNotices = value;
            Refresh();
        }

        private void Select(int index)
        {
            _tab = index;

            for (int i = 0; i < _pages.Count; i++)
            {
                if (i == index)
                    _pages[i].SetActive(true);
                else
                    _pages[i].SetActive(false);

                _tabImages[i].sprite = i == index ? _ui.Sprites.ButtonSelected : _ui.Sprites.ButtonIdle;
            }

            Refresh();
        }

        private void Refresh()
        {
            _nextRefresh = Time.unscaledTime + RefreshSeconds;

            foreach ((Toggle toggle, ConfigEntry<bool> entry) in _bindings)
                toggle.SetIsOnWithoutNotify(entry.Value);

            foreach ((Toggle toggle, CanvasGroup group, Func<bool> enabled) in _dependents)
            {
                bool active = enabled();
                toggle.interactable = active;
                group.alpha = active ? 1f : DisabledAlpha;
            }

            foreach ((Toggle notices, Toggle follow, TMP_InputField search) in _optionRows)
            {
                notices.SetIsOnWithoutNotify(_filter.ShowNotices);
                follow.SetIsOnWithoutNotify(_filter.FollowNewest);

                if (!search.isFocused && search.text != _filter.Search)
                    search.SetTextWithoutNotify(_filter.Search);
            }

            foreach (GameObject note in _hostNotes)
            {
                if (PhotonNetwork.IsMasterClient)
                    note.SetActive(false);
                else
                    note.SetActive(true);
            }

            RefreshPage();
        }

        private void RefreshPage()
        {
            switch (_tab)
            {
                case 0:
                    RefreshStored();
                    _logFeed.Show(_findings, _filter);
                    break;

                case 1:
                    _itemsFeed.Show(_findings, _filter);
                    break;

                case 2:
                    _cheatsFeed.Show(_findings, _filter);
                    break;

                case 3:
                    _otherFeed.Show(_findings, _filter);
                    break;

                case 4:
                    _players.Refresh();
                    break;

                default:
                    _banList.Refresh();
                    break;
            }
        }

        private void RefreshStored()
        {
            _storedLabel.text = string.Format(StoredFormat, _findings.Count, _findings.Capacity);

            if (!_capacityInput.isFocused)
                _capacityInput.SetTextWithoutNotify(_findings.Capacity.ToString());
        }

        private TMP_FontAsset GameFont()
        {
            FontFallbackSwapper swapper = FontFallbackSwapper.instance;
            return swapper != null && swapper.mainBaseFont != null ? swapper.mainBaseFont : TMP_Settings.defaultFontAsset;
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
