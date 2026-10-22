using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using PeakSentinel.Speedrun;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeakSentinel.Ui.Pages
{
    public sealed class SpeedrunPage : ITabPage
    {
        private const string Header = "Speedrun";
        private const string Explanation = "Tools that help speedrun moderators check a run. Everything is off until you tick it. Chests and map items are rolled by the host, so luck works only while you are the host.";
        private const string TrackLabel = "Loot luck: log chests and map items with the game's odds, show the mushroom table";
        private const string OverlayLabel = "Overlay in the top right corner (clover luck, version, attempt, run ID, integrity)";
        private const string IntegrityLabel = "Check game integrity: patches on randomness, foreign code, game, loot table and movement codes, game speed";
        private const string MyselfLabel = "Check myself: flight, speed, stamina and fall damage checks on my own character";
        private const string RecordLabel = "Speedrun journal: attempts, chests and alerts in a daily file chained by hashes";
        private const string ExportLabel = "Export run report";
        private const string ExportedFormat = "Saved {0}";
        private const string ExportFailedFormat = "Could not save the report: {0}";
        private const string NotHost = "You are not the host: the host rolls the chests, only the host's Sentinel can log them.";
        private const string NoChests = "No chests opened in this run yet.";
        private const string RunLuckFormat = "This run: {0}";
        private const string TodayLuckFormat = "Today: {0}";
        private const string NoLuck = "no chests yet";
        private const string RunSummaryFormat = "{0} chests, {1} items";
        private const string TodaySummaryFormat = "{0} runs, {1} chests, {2} items (all runs since midnight while the game is open)";
        private const string ResetLabel = "Reset run";
        private const string ResetTodayLabel = "Reset today";
        private const string AttemptFormat = "Attempt {0} of the day, run ID {1}  ({2})";
        private const string NoAlerts = "<color=#{0}>No alerts in this attempt</color>";
        private const string AlertsHeader = "<color=#{0}>Alerts in this attempt:</color>";
        private const string MapFormat = "Map: scene {0}, biomes {1}";
        private const string RandomiserFormat = "TerrainRandomiser {0}, file {1}:";
        private const string IntegrityHeader = "Game integrity:";
        private const string GameFormat = "Game {0}, game code {1}, loot tables {2}, movement {3}";
        private const string TablesChanged = "<color=#{0}>Loot tables changed while the game was running</color>";
        private const string NoPatches = "<color=#{0}>No other mod patches loot, map items or mushrooms</color>";
        private const string PatchesHeader = "<color=#{0}>Other mods patch loot or randomness:</color>";
        private const string NoAssemblies = "<color=#{0}>No code loaded from outside the game and BepInEx</color>";
        private const string AssembliesHeader = "<color=#{0}>Code loaded from outside the game and BepInEx:</color>";
        private const string MushroomsHeader = "Mushrooms on this map, code {0} (the same map always gives the same code; a different code on the same seed means the game was changed):";
        private const string MushroomFormat = "<color=#{0}>{1}: {2}</color>, +{3}% bonus stamina";
        private const string NoMushrooms = "Mushrooms: no table on this map yet (airport or the map is still loading).";
        private const string MapItemsFormat = "Map items this run (berries and spawned items): {0} over {1} items";
        private const string ItemsHeader = "Luckiest chest items (got / expected, chance of at least that many):";
        private const string ItemFormat = "{0}: {1} / {2:F1}, {3}";
        private const string DrawsHeader = "Chest rolls, newest first (item, chance in that roll):";
        private const string DrawFormat = "{0}: {1} ({2:P1})";
        private const string ListItemFormat = "  {0}";
        private const float LineSize = 19f;
        private const float LuckSize = 28f;
        private const float ResetWidth = 180f;
        private const float ExportWidth = 260f;
        private const float CategoryWidth = 150f;
        private const string CategoryLabel = "Category:";
        private const int ShownItems = 8;
        private const int ShownDraws = 60;

        private readonly UiFactory _ui;
        private readonly LootLedger _ledger;
        private readonly SpeedrunMonitor _monitor;
        private readonly FindingLog _findings;
        private readonly SpeedrunSettings _settings;
        private readonly LuckMath _math = new LuckMath();
        private readonly LuckText _text = new LuckText();
        private readonly RunReport _report = new RunReport();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly TextMeshProUGUI _exported;
        private readonly TextMeshProUGUI _runLuck;
        private readonly TextMeshProUGUI _runSummary;
        private readonly TextMeshProUGUI _todayLuck;
        private readonly TextMeshProUGUI _todaySummary;
        private readonly RectTransform _rows;
        private readonly RunRules _rules = new RunRules();
        private readonly List<(RunCategory Category, Image Image)> _categoryButtons = new List<(RunCategory Category, Image Image)>();
        private readonly TextMeshProUGUI _categoryHint;
        private RunCategory? _shownCategory;
        private (int Loot, int Monitor, int Mushrooms, int Map, int Integrity, int Journal, bool Host, bool Track, bool Check, bool Record) _shown;

        public SpeedrunPage(PageContext context, Transform page, WindowServices services, SpeedrunSettings settings)
        {
            UiFactory ui = context.Ui;
            SettingsBinder binder = context.Binder;
            _ui = ui;
            _ledger = services.Loot;
            _monitor = services.Speedrun;
            _findings = services.Findings;
            _settings = settings;
            ui.Header(page, Header);
            ui.Label(page, Explanation, LineSize, UiPalette.Notice, false);
            RectTransform categories = ui.Row(page, 8f);
            ui.Label(categories, CategoryLabel, LineSize, UiPalette.Text, false);

            foreach (RunCategory category in (RunCategory[])System.Enum.GetValues(typeof(RunCategory)))
            {
                RunCategory chosen = category;
                _categoryButtons.Add((category, ui.Button(categories, _rules.LabelOf(category), CategoryWidth, LineSize, () => Choose(chosen)).Button.image));
            }

            _categoryHint = ui.Label(page, string.Empty, LineSize, UiPalette.Notice, false);
            binder.Bind(page, TrackLabel, settings.TrackLuck);
            binder.Bind(page, OverlayLabel, settings.ShowOverlay);
            binder.Bind(page, IntegrityLabel, settings.CheckIntegrity);
            binder.Bind(page, MyselfLabel, settings.CheckMyself);
            binder.Bind(page, RecordLabel, settings.RecordRun);
            RectTransform export = ui.Row(page, 10f);
            ui.Button(export, ExportLabel, ExportWidth, LineSize, Export);
            _exported = ui.Label(export, string.Empty, LineSize, UiPalette.Clean, false);
            ui.Flexible(_exported, 1f, 0f);
            (_runLuck, _runSummary) = LuckLine(page, ResetLabel, ResetRun);
            (_todayLuck, _todaySummary) = LuckLine(page, ResetTodayLabel, ResetToday);
            (ScrollRect _, RectTransform rows) = ui.Scroll(page, 6f);
            _rows = rows;
            _shown.Loot = -1;
        }

        public void Refresh()
        {
            ShowCategory();
            (int Loot, int Monitor, int Mushrooms, int Map, int Integrity, int Journal, bool Host, bool Track, bool Check, bool Record) wanted = (_ledger.Version, _monitor.Version, _monitor.Mushrooms.Version, _monitor.Passport.Version, _monitor.Integrity.Version, _monitor.Journal.Version, PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom, _settings.TrackLuck.Value, _settings.CheckIntegrity.Value, _settings.RecordRun.Value);

            if (wanted == _shown)
                return;

            _shown = wanted;
            LootTally run = _ledger.Run;
            LootTally today = _ledger.Today;
            LuckReport report = ShowLuck(_runLuck, RunLuckFormat, run.Draws);
            ShowLuck(_todayLuck, TodayLuckFormat, today.Draws);
            _runSummary.text = string.Format(RunSummaryFormat, run.Chests, run.Draws.Count);
            _todaySummary.text = string.Format(TodaySummaryFormat, today.Runs, today.Chests, today.Draws.Count);

            for (int i = _rows.childCount - 1; i >= 0; i--)
                Object.Destroy(_rows.GetChild(i).gameObject);

            if (wanted.Record && _monitor.Journal.RunId.Length > 0)
                _ui.Label(_rows, string.Format(AttemptFormat, _monitor.Journal.Attempt, _monitor.Journal.RunId, _monitor.Journal.Path), LineSize, UiPalette.Text, false);

            _ui.Label(_rows, AlertList(), LineSize, UiPalette.Text, true);
            _ui.Label(_rows, MapList(), LineSize, UiPalette.Text, false);

            if (wanted.Check)
                _ui.Label(_rows, IntegrityList(), LineSize, UiPalette.Text, true);

            if (!wanted.Track)
                return;

            _ui.Label(_rows, MushroomList(), LineSize, UiPalette.Text, true);
            _ui.Label(_rows, MapItemsLine(), LineSize, UiPalette.Text, false);

            if (report == null)
            {
                _ui.Label(_rows, wanted.Host ? NoChests : NotHost, LineSize, UiPalette.Notice, false);
                return;
            }

            _ui.Label(_rows, ItemsList(report), LineSize, UiPalette.Text, false);
            _ui.Label(_rows, DrawsList(run.Draws), LineSize, UiPalette.Notice, false);
        }

        private (TextMeshProUGUI Luck, TextMeshProUGUI Summary) LuckLine(Transform page, string resetLabel, UnityAction reset)
        {
            RectTransform row = _ui.Row(page, 10f);
            TextMeshProUGUI luck = _ui.Label(row, string.Empty, LuckSize, UiPalette.Text, false);
            _ui.Flexible(luck, 1f, 0f);
            _ui.Button(row, resetLabel, ResetWidth, LineSize, reset);
            return (luck, _ui.Label(page, string.Empty, LineSize, UiPalette.Notice, false));
        }

        private LuckReport ShowLuck(TextMeshProUGUI label, string format, IReadOnlyList<LootDraw> draws)
        {
            if (draws.Count == 0)
            {
                label.text = string.Format(format, NoLuck);
                label.color = UiPalette.Notice;
                return null;
            }

            LuckReport report = _math.Evaluate(draws);
            label.text = string.Format(format, _text.OneIn(report.Chance));
            label.color = _text.ColorOf(report.Chance);
            return report;
        }

        private string AlertList()
        {
            _builder.Clear();
            AppendList(_monitor.Alerts, NoAlerts, AlertsHeader);
            return _builder.ToString().TrimStart('\n');
        }

        private string MapList()
        {
            MapPassport passport = _monitor.Passport;
            _builder.Clear();
            _builder.AppendFormat(MapFormat, passport.Scene, passport.Biomes);

            if (passport.Randomiser == null)
                return _builder.ToString();

            _builder.Append('\n').AppendFormat(RandomiserFormat, passport.Randomiser, passport.RandomiserHash);

            foreach (string setting in passport.RandomiserSettings)
                _builder.Append('\n').AppendFormat(ListItemFormat, setting);

            return _builder.ToString();
        }

        private string IntegrityList()
        {
            IntegrityScan integrity = _monitor.Integrity;
            _builder.Clear();
            _builder.Append(IntegrityHeader);
            _builder.Append('\n').AppendFormat(GameFormat, integrity.GameVersion, integrity.GameHash, integrity.TablesCode, _monitor.Constants.Code);

            if (integrity.TablesChanged)
                _builder.Append('\n').AppendFormat(TablesChanged, ColorUtility.ToHtmlStringRGB(UiPalette.Cheat));

            AppendList(integrity.Patches, NoPatches, PatchesHeader);
            AppendList(integrity.Assemblies, NoAssemblies, AssembliesHeader);
            return _builder.ToString();
        }

        private void AppendList(IReadOnlyList<string> lines, string none, string header)
        {
            string color = ColorUtility.ToHtmlStringRGB(lines.Count == 0 ? UiPalette.Clean : UiPalette.Cheat);
            _builder.Append('\n').AppendFormat(lines.Count == 0 ? none : header, color);

            foreach (string line in lines)
                _builder.Append('\n').AppendFormat(ListItemFormat, line);
        }

        private string MushroomList()
        {
            MushroomTable mushrooms = _monitor.Mushrooms;

            if (mushrooms.Entries.Count == 0)
                return NoMushrooms;

            _builder.Clear();
            _builder.AppendFormat(MushroomsHeader, mushrooms.Code);

            foreach (MushroomEntry entry in mushrooms.Entries)
            {
                string color = ColorUtility.ToHtmlStringRGB(entry.Good ? UiPalette.Clean : UiPalette.Cheat);
                _builder.Append('\n').AppendFormat(MushroomFormat, color, entry.Mushrooms, entry.Effect, entry.Stamina);
            }

            return _builder.ToString();
        }

        private string MapItemsLine()
        {
            IReadOnlyList<LootDraw> draws = _ledger.Map.Draws;
            string luck = draws.Count == 0 ? NoLuck : _text.OneIn(_math.Evaluate(draws).Chance);
            return string.Format(MapItemsFormat, luck, draws.Count);
        }

        private string ItemsList(LuckReport report)
        {
            _builder.Clear();
            _builder.Append(ItemsHeader);

            for (int i = 0; i < report.Items.Count && i < ShownItems; i++)
            {
                ItemLuck item = report.Items[i];
                _builder.Append('\n').AppendFormat(ItemFormat, _ledger.NameOf(item.Item), item.Count, item.Expected, _text.OneIn(item.Chance));
            }

            return _builder.ToString();
        }

        private string DrawsList(IReadOnlyList<LootDraw> draws)
        {
            _builder.Clear();
            _builder.Append(DrawsHeader);

            for (int i = draws.Count - 1; i >= 0 && i >= draws.Count - ShownDraws; i--)
                _builder.Append('\n').AppendFormat(DrawFormat, draws[i].Source, _ledger.NameOf(draws[i].Item), draws[i].Chance);

            return _builder.ToString();
        }

        private void Export()
        {
            try
            {
                string path = _report.Write(Paths.BepInExRootPath, SentinelPlugin.Version, _monitor, _ledger, _findings, _settings.CheckIntegrity.Value, _settings.Category.Value);
                _exported.text = string.Format(ExportedFormat, path);
                _exported.color = UiPalette.Clean;
            }
            catch (IOException exception)
            {
                _exported.text = string.Format(ExportFailedFormat, exception.Message);
                _exported.color = UiPalette.Cheat;
            }
        }

        private void ShowCategory()
        {
            RunCategory category = _settings.Category.Value;

            if (_shownCategory == category)
                return;

            _shownCategory = category;
            _categoryHint.text = _rules.HintOf(category);

            foreach ((RunCategory option, Image image) in _categoryButtons)
                image.sprite = option == category ? _ui.Sprites.ButtonSelected : _ui.Sprites.ButtonIdle;
        }

        private void Choose(RunCategory category)
        {
            _settings.Category.Value = category;
            ShowCategory();
        }

        private void ResetRun()
        {
            _ledger.ResetRun();
            Refresh();
        }

        private void ResetToday()
        {
            _ledger.ResetToday();
            Refresh();
        }
    }
}
