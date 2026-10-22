using System.Collections.Generic;
using PeakSentinel.Settings;
using PeakSentinel.Speedrun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class SpeedrunOverlay : MonoBehaviour
    {
        private const string CanvasName = "PeakSentinelSpeedrun";
        private const int SortingOrder = 250;
        private const float RefreshSeconds = 0.5f;
        private const float IconSize = 30f;
        private const float TextSize = 17f;
        private const float Margin = 18f;
        private const float OutlineWidth = 0.25f;
        private const string RunFormat = "<b><color=#{0}>{1}</color></b>";
        private const string TodayFormat = "   <size=75%><color=#{0}>today {1}</color></size>";
        private const string NoChests = "no chests yet";
        private const string InfoFormat = "<size=70%><color=#{0}>{1}</color></size>";
        private const string LineBreak = "\n";
        private const string VersionFormat = "Sentinel {0}";
        private const string AttemptFormat = "attempt {0}";
        private const string RunIdFormat = "run {0}";
        private const string IntegrityClean = "integrity OK";
        private const string IntegrityAlertsFormat = "{0} alerts";
        private const string InfoSeparator = " · ";

        private static readonly Color Quiet = new Color(0.85f, 0.85f, 0.85f, 1f); // Static: fixed grey for quiet overlay text
        private static readonly Color AlertColor = new Color(1f, 0.45f, 0.4f, 1f); // Static: fixed bright red for overlay alerts

        private readonly LuckMath _math = new LuckMath();
        private readonly LuckText _text = new LuckText();
        private readonly List<string> _info = new List<string>();
        private readonly RunRules _rules = new RunRules();
        private LootLedger _ledger;
        private SpeedrunMonitor _monitor;
        private SpeedrunSettings _settings;
        private string _version;
        private GameObject _canvas;
        private TMP_FontAsset _font;
        private Image _clover;
        private TextMeshProUGUI _label;
        private (int Loot, int Alerts, int Journal, RunCategory Category, bool Luck, bool Integrity, bool Record) _shown = (-1, -1, -1, RunCategory.NoMajorGlitches, false, false, false);
        private float _nextRefresh;

        public void Initialize(LootLedger ledger, SpeedrunMonitor monitor, SpeedrunSettings settings, string version)
        {
            _ledger = ledger;
            _monitor = monitor;
            _settings = settings;
            _version = version;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
                return;

            _nextRefresh = Time.unscaledTime + RefreshSeconds;

            if (!_settings.ShowOverlay.Value)
            {
                Hide();
                return;
            }

            TMP_FontAsset font = GameFonts.Main();

            if (font == null)
                return;

            if (_canvas == null || _font != font)
                Build(font);

            _canvas.SetActive(true);
            (int Loot, int Alerts, int Journal, RunCategory Category, bool Luck, bool Integrity, bool Record) wanted = (_ledger.Version, _monitor.Version, _monitor.Journal.Version, _settings.Category.Value, _settings.TrackLuck.Value, _settings.CheckIntegrity.Value, _settings.RecordRun.Value);

            if (wanted != _shown)
                Show(wanted.Luck, wanted.Integrity, wanted.Record);

            _shown = wanted;
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas);
        }

        private void Build(TMP_FontAsset font)
        {
            if (_canvas != null)
                Destroy(_canvas);

            _font = font;
            UiFactory ui = new UiFactory(font);
            _canvas = ui.Canvas(CanvasName, SortingOrder);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            DontDestroyOnLoad(_canvas);

            RectTransform badge = ui.Row(_canvas.transform, 6f);
            badge.anchorMin = Vector2.one;
            badge.anchorMax = Vector2.one;
            badge.pivot = Vector2.one;
            badge.anchoredPosition = new Vector2(-Margin, -Margin);
            ContentSizeFitter fitter = badge.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _clover = ui.Icon(badge, ui.Clover, IconSize);
            _label = ui.Label(badge, string.Empty, TextSize, Color.white, true);
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.outlineWidth = OutlineWidth;
            _label.outlineColor = Color.black;
            _shown = (-1, -1, -1, RunCategory.NoMajorGlitches, false, false, false);
        }

        private void Show(bool luck, bool integrity, bool record)
        {
            string text = luck ? Part(RunFormat, _ledger.Run) + Part(TodayFormat, _ledger.Today) : string.Empty;

            if (luck)
                _clover.gameObject.SetActive(true);
            else
                _clover.gameObject.SetActive(false);

            _info.Clear();
            _info.Add(string.Format(VersionFormat, _version));
            _info.Add(_rules.LabelOf(_settings.Category.Value));

            if (record && _monitor.Journal.RunId.Length > 0)
            {
                _info.Add(string.Format(AttemptFormat, _monitor.Journal.Attempt));
                _info.Add(string.Format(RunIdFormat, _monitor.Journal.RunId));
            }

            bool alerts = _monitor.Alerts.Count > 0;

            if (integrity || alerts)
                _info.Add(alerts ? string.Format(IntegrityAlertsFormat, _monitor.Alerts.Count) : IntegrityClean);

            string color = ColorUtility.ToHtmlStringRGB(alerts ? AlertColor : Quiet);
            string info = string.Format(InfoFormat, color, string.Join(InfoSeparator, _info));
            _label.text = text.Length == 0 ? info : text + LineBreak + info;
        }

        private string Part(string format, LootTally tally)
        {
            if (tally.Draws.Count == 0)
                return string.Format(format, ColorUtility.ToHtmlStringRGB(Quiet), NoChests);

            double chance = _math.Evaluate(tally.Draws).Chance;
            return string.Format(format, ColorUtility.ToHtmlStringRGB(_text.BrightColorOf(chance)), _text.OneIn(chance));
        }

        private void Hide()
        {
            if (_canvas != null)
                _canvas.SetActive(false);
        }
    }
}
