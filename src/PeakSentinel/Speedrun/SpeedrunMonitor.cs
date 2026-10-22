using System.Collections.Generic;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Detection.Stamina;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class SpeedrunMonitor : MonoBehaviour
    {
        private const float SlowSeconds = 1f;
        private const string AlertPrefix = "speedrun check: ";
        private const string PatchFormat = "another mod patches randomness: {0}";
        private const string AssemblyFormat = "code loaded from outside the game and BepInEx: {0}";
        private const string TablesChanged = "loot tables changed while the game was running";
        private const string ChestFormat = "{0}: {1} ({2:P1})";
        private const string PassportFormat = "category {0}, game {1}, scene {2}, biomes {3}, mushrooms {4}, loot tables {5}, randomiser {6}";
        private const string NoRandomiser = "none";
        private const string CategoryChangedFormat = "category changed from {0} to {1} during the attempt";

        private readonly List<string> _alerts = new List<string>();
        private readonly HashSet<string> _raised = new HashSet<string>();
        private readonly TimeWatch _time = new TimeWatch();
        private readonly FallWatch _fall = new FallWatch();
        private readonly RunRules _rules = new RunRules();
        private SpeedrunSettings _settings;
        private SuspicionReport _report;
        private LootLedger _ledger;
        private FlightAudit _flight;
        private StaminaAudit _stamina;
        private RunManager _run;
        private int _journaledDraws;
        private bool _attemptPending;
        private RunCategory? _attemptCategory;
        private float _nextSlow;

        public IntegrityScan Integrity { get; private set; }
        public MushroomTable Mushrooms { get; } = new MushroomTable();
        public MapPassport Passport { get; } = new MapPassport();
        public ConstantsWatch Constants { get; } = new ConstantsWatch();
        public RunJournal Journal { get; private set; }
        public IReadOnlyList<string> Alerts => _alerts;
        public int Version { get; private set; }

        public void Initialize(SpeedrunSettings settings, SuspicionReport report, LootLedger ledger, RunJournal journal, FlightAudit flight, StaminaAudit stamina)
        {
            _settings = settings;
            _report = report;
            _ledger = ledger;
            Journal = journal;
            _flight = flight;
            _stamina = stamina;
            Integrity = new IntegrityScan(SentinelPlugin.Guid);
        }

        private void Update()
        {
            RunCategory category = _settings.Category.Value;

            if (_attemptCategory.HasValue && _attemptCategory.Value != category)
            {
                Raise(string.Format(CategoryChangedFormat, _rules.LabelOf(_attemptCategory.Value), _rules.LabelOf(category)));
                _attemptCategory = category;
            }

            _flight.CheckLocal = _settings.CheckMyself.Value && _rules.ChecksFlight(category);
            _stamina.CheckLocal = _settings.CheckMyself.Value;

            if (RunManager.Instance != _run)
            {
                _run = RunManager.Instance;
                _attemptPending = _run != null;
            }

            if (_settings.CheckMyself.Value && _rules.ChecksFallDamage(category))
                Raise(_fall.Check(Character.localCharacter));

            if (Time.unscaledTime < _nextSlow)
                return;

            _nextSlow = Time.unscaledTime + SlowSeconds;
            Mushrooms.Refresh();
            Passport.Refresh();

            if (_settings.CheckIntegrity.Value)
                CheckIntegrity();

            if (_attemptPending && Character.localCharacter != null)
                StartAttempt();

            JournalChests();
        }

        private void StartAttempt()
        {
            _attemptPending = false;
            _alerts.Clear();
            _raised.Clear();
            _journaledDraws = 0;
            _attemptCategory = _settings.Category.Value;
            Journal.StartAttempt(PassportLine());
            Version++;
        }

        private string PassportLine()
        {
            return string.Format(PassportFormat, _rules.LabelOf(_settings.Category.Value), Integrity.GameVersion, Passport.Scene, Passport.Biomes, Mushrooms.Code, Integrity.TablesCode, Passport.Randomiser ?? NoRandomiser);
        }

        private void CheckIntegrity()
        {
            Integrity.Refresh();
            Raise(_time.Check());
            Raise(Constants.Check());

            foreach (string patch in Integrity.Patches)
                Raise(string.Format(PatchFormat, patch));

            foreach (string assembly in Integrity.Assemblies)
                Raise(string.Format(AssemblyFormat, assembly));

            if (Integrity.TablesChanged)
                Raise(TablesChanged);
        }

        private void JournalChests()
        {
            IReadOnlyList<LootDraw> draws = _ledger.Run.Draws;

            if (draws.Count < _journaledDraws)
                _journaledDraws = 0;

            for (; _journaledDraws < draws.Count; _journaledDraws++)
            {
                LootDraw draw = draws[_journaledDraws];
                Journal.Chest(string.Format(ChestFormat, draw.Source, _ledger.NameOf(draw.Item), draw.Chance));
            }
        }

        private void Raise(string alert)
        {
            if (alert == null || !_raised.Add(alert))
                return;

            _alerts.Add(alert);
            Journal.Alert(alert);
            _report.Flag(PhotonNetwork.LocalPlayer, FindingCategory.Cheats, AlertPrefix + alert);
            Version++;
        }
    }
}
