using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PeakSentinel.Reporting;
using PeakSentinel.Ui;
using Photon.Pun;

namespace PeakSentinel.Speedrun
{
    public sealed class RunReport
    {
        private const string FileFormat = "PeakSentinel-run-{0:yyyyMMdd-HHmmss}{1}.txt";
        private const string RunIdFormat = "-{0}";
        private const string TitleFormat = "Peak Sentinel {0} speedrun report, {1:yyyy-MM-dd HH:mm:ss}";
        private const string AttemptFormat = "Attempt {0} of the day, run ID {1}, journal {2}";
        private const string CategoryFormat = "Category preset: {0}. {1}";
        private const string NoJournal = "Journal is off (RecordRun): no attempt number and run ID";
        private const string MapHeader = "\n== Map ==";
        private const string MapFormat = "Game {0}, scene {1}\nBiomes: {2}";
        private const string RandomiserFormat = "TerrainRandomiser: {0}, file {1}";
        private const string NoRandomiser = "TerrainRandomiser: not loaded";
        private const string IntegrityHeader = "\n== Integrity ==";
        private const string CodesFormat = "Game code {0}, loot tables {1}{2}, movement constants {3}";
        private const string TablesChanged = " (CHANGED while playing)";
        private const string NoPatches = "Other mods patching randomness: none";
        private const string PatchesHeader = "Other mods patching randomness:";
        private const string NoAssemblies = "Code from outside the game and BepInEx: none";
        private const string AssembliesHeader = "Code from outside the game and BepInEx:";
        private const string IntegrityOff = "Integrity check is off (CheckIntegrity)";
        private const string AlertsHeader = "\n== Alerts in this attempt ==";
        private const string NoAlerts = "none";
        private const string FindingsHeader = "\n== Sentinel findings about you since the attempt started ==";
        private const string FindingFormat = "[{0:HH:mm:ss}] {1}";
        private const string MushroomsHeader = "\n== Mushrooms, code {0} ==";
        private const string MushroomFormat = "{0}: {1} ({2}), +{3}% bonus stamina";
        private const string Good = "good";
        private const string Bad = "bad";
        private const string LuckHeader = "\n== Loot luck ==";
        private const string LuckFormat = "{0}: {1} ({2} items)";
        private const string RunLabel = "Chests this run";
        private const string TodayLabel = "Chests today";
        private const string MapLabel = "Map items this run";
        private const string NoRolls = "no rolls";
        private const string ItemFormat = "  {0}: got {1}, expected {2:F1}, at least that many {3}";
        private const string DrawsHeader = "\n== Chest rolls this run (item, chance in that roll) ==";
        private const string DrawFormat = "{0}: {1} ({2:P1})";
        private const string VerifyHeader = "\n== How to check the journal ==";
        private const string VerifyText = "Each journal line ends with a hash: the first 16 hex digits of SHA-256(previous line hash + this line without its hash); the first previous hash is GENESIS. An edited, removed or inserted line breaks every hash after it. The run ID is the first 8 digits of the START line hash and is shown on the overlay.";
        private const string ListItemFormat = "  {0}";

        private readonly LuckMath _math = new LuckMath();
        private readonly RunRules _rules = new RunRules();
        private readonly LuckText _text = new LuckText();
        private readonly StringBuilder _builder = new StringBuilder();

        public string Write(string folder, string version, SpeedrunMonitor monitor, LootLedger ledger, FindingLog findings, bool integrityOn, RunCategory category)
        {
            RunJournal journal = monitor.Journal;
            DateTime now = DateTime.Now;
            _builder.Clear();
            _builder.AppendFormat(TitleFormat, version, now).AppendLine();
            _builder.AppendFormat(CategoryFormat, _rules.LabelOf(category), _rules.HintOf(category)).AppendLine();
            _builder.AppendLine(journal.RunId.Length > 0 ? string.Format(AttemptFormat, journal.Attempt, journal.RunId, journal.Path) : NoJournal);
            AppendMap(monitor);
            AppendIntegrity(monitor, integrityOn);
            AppendList(AlertsHeader, monitor.Alerts, NoAlerts);
            AppendFindings(findings, journal.AttemptStart);
            AppendMushrooms(monitor.Mushrooms);
            AppendLuck(ledger);
            _builder.AppendLine(VerifyHeader).AppendLine(VerifyText);

            string runId = journal.RunId.Length > 0 ? string.Format(RunIdFormat, journal.RunId) : string.Empty;
            string path = Path.Combine(folder, string.Format(FileFormat, now, runId));
            File.WriteAllText(path, _builder.ToString());
            return path;
        }

        private void AppendMap(SpeedrunMonitor monitor)
        {
            MapPassport passport = monitor.Passport;
            _builder.AppendLine(MapHeader).AppendFormat(MapFormat, monitor.Integrity.GameVersion, passport.Scene, passport.Biomes).AppendLine();
            _builder.AppendLine(passport.Randomiser == null ? NoRandomiser : string.Format(RandomiserFormat, passport.Randomiser, passport.RandomiserHash));

            foreach (string setting in passport.RandomiserSettings)
                _builder.AppendFormat(ListItemFormat, setting).AppendLine();
        }

        private void AppendIntegrity(SpeedrunMonitor monitor, bool integrityOn)
        {
            _builder.AppendLine(IntegrityHeader);

            if (!integrityOn)
            {
                _builder.AppendLine(IntegrityOff);
                return;
            }

            IntegrityScan integrity = monitor.Integrity;
            _builder.AppendFormat(CodesFormat, integrity.GameHash, integrity.TablesCode, integrity.TablesChanged ? TablesChanged : string.Empty, monitor.Constants.Code).AppendLine();
            AppendList(integrity.Patches.Count == 0 ? NoPatches : PatchesHeader, integrity.Patches, null);
            AppendList(integrity.Assemblies.Count == 0 ? NoAssemblies : AssembliesHeader, integrity.Assemblies, null);
        }

        private void AppendFindings(FindingLog findings, DateTime since)
        {
            int actor = PlayerNames.Actor(PhotonNetwork.LocalPlayer);
            List<string> lines = findings.Findings
                .Where(finding => finding.Actor == actor && finding.Kind != FindingKind.Notice && finding.Time >= since)
                .Select(finding => string.Format(FindingFormat, finding.Time, finding.Text))
                .ToList();
            AppendList(FindingsHeader, lines, NoAlerts);
        }

        private void AppendMushrooms(MushroomTable mushrooms)
        {
            _builder.AppendFormat(MushroomsHeader, mushrooms.Code).AppendLine();

            foreach (MushroomEntry entry in mushrooms.Entries)
                _builder.AppendFormat(MushroomFormat, entry.Mushrooms, entry.Effect, entry.Good ? Good : Bad, entry.Stamina).AppendLine();
        }

        private void AppendLuck(LootLedger ledger)
        {
            _builder.AppendLine(LuckHeader);
            AppendTally(RunLabel, ledger.Run.Draws, ledger);
            AppendTally(TodayLabel, ledger.Today.Draws, ledger);
            AppendTally(MapLabel, ledger.Map.Draws, ledger);
            _builder.AppendLine(DrawsHeader);

            foreach (LootDraw draw in ledger.Run.Draws)
                _builder.AppendFormat(DrawFormat, draw.Source, ledger.NameOf(draw.Item), draw.Chance).AppendLine();
        }

        private void AppendTally(string label, IReadOnlyList<LootDraw> draws, LootLedger ledger)
        {
            if (draws.Count == 0)
            {
                _builder.AppendFormat(LuckFormat, label, NoRolls, 0).AppendLine();
                return;
            }

            LuckReport report = _math.Evaluate(draws);
            _builder.AppendFormat(LuckFormat, label, _text.OneIn(report.Chance), draws.Count).AppendLine();

            foreach (ItemLuck item in report.Items)
                _builder.AppendFormat(ItemFormat, ledger.NameOf(item.Item), item.Count, item.Expected, _text.OneIn(item.Chance)).AppendLine();
        }

        private void AppendList(string header, IReadOnlyList<string> lines, string none)
        {
            _builder.AppendLine(header);

            if (lines.Count == 0 && none != null)
                _builder.AppendLine(none);

            foreach (string line in lines)
                _builder.AppendFormat(ListItemFormat, line).AppendLine();
        }
    }
}
