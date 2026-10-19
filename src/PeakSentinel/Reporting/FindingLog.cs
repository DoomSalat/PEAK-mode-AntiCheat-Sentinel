using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Reporting
{
    public sealed class FindingLog
    {
        private const string Separator = ", ";

        private readonly List<Finding> _findings = new List<Finding>();
        private readonly Dictionary<int, PlayerRecord> _players = new Dictionary<int, PlayerRecord>();
        private readonly ConfigEntry<int> _capacity;

        public FindingLog(ConfigEntry<int> capacity)
        {
            _capacity = capacity;
        }

        public IReadOnlyList<Finding> Findings => _findings;
        public int Version { get; private set; }
        public int Count => _findings.Count;
        public int Capacity => _capacity.Value;

        public void Add(int actor, string name, string text, FindingKind kind, FindingCategory category)
        {
            _findings.Add(new Finding(DateTime.Now, actor, name, text, kind, category));
            Version++;

            Trim();

            PlayerRecord record = RecordOf(actor);

            if (kind == FindingKind.Suspicious)
                record.Suspicious++;
            else if (kind == FindingKind.Cheat)
                record.Cheats++;
            else if (kind == FindingKind.Blocked)
                record.Blocked++;
        }

        public void Resize(int capacity)
        {
            _capacity.Value = Mathf.Clamp(capacity, ManagerSettings.MinLogEntries, ManagerSettings.MaxLogEntries);
            Trim();
            Version++;
        }

        public void Tag(int actor, string tag)
        {
            RecordOf(actor).Tags.Add(tag);
            Version++;
        }

        public void Clear()
        {
            _findings.Clear();
            _players.Clear();
            Version++;
        }

        public string ProblemsOf(int actor)
        {
            if (!_players.TryGetValue(actor, out PlayerRecord record))
                return null;

            StringBuilder text = new StringBuilder();

            foreach (string tag in record.Tags)
                Append(text, tag);

            if (record.Blocked > 0)
                Append(text, $"{record.Blocked} blocked");

            if (record.Cheats > 0)
                Append(text, $"{record.Cheats} cheats");

            if (record.Suspicious > 0)
                Append(text, $"{record.Suspicious} suspicious");

            return text.Length == 0 ? null : text.ToString();
        }

        public PlayerStanding StandingOf(int actor)
        {
            if (!_players.TryGetValue(actor, out PlayerRecord record))
                return PlayerStanding.Clean;

            if (record.Blocked > 0 || record.Cheats > 0)
                return PlayerStanding.Cheater;

            return record.Suspicious > 0 || record.Tags.Count > 0 ? PlayerStanding.Suspect : PlayerStanding.Clean;
        }

        private void Trim()
        {
            int excess = _findings.Count - _capacity.Value;

            if (excess > 0)
                _findings.RemoveRange(0, excess);
        }

        private PlayerRecord RecordOf(int actor)
        {
            if (!_players.TryGetValue(actor, out PlayerRecord record))
            {
                record = new PlayerRecord();
                _players[actor] = record;
            }

            return record;
        }

        private void Append(StringBuilder text, string part)
        {
            if (text.Length > 0)
                text.Append(Separator);

            text.Append(part);
        }

        public readonly struct Finding
        {
            public Finding(DateTime time, int actor, string name, string text, FindingKind kind, FindingCategory category)
            {
                Time = time;
                Actor = actor;
                Name = name;
                Text = text;
                Kind = kind;
                Category = category;
            }

            public DateTime Time { get; }
            public int Actor { get; }
            public string Name { get; }
            public string Text { get; }
            public FindingKind Kind { get; }
            public FindingCategory Category { get; }
        }

        private sealed class PlayerRecord
        {
            public HashSet<string> Tags { get; } = new HashSet<string>();
            public int Suspicious { get; set; } // Set: counted by FindingLog.Add
            public int Blocked { get; set; } // Set: counted by FindingLog.Add
            public int Cheats { get; set; } // Set: counted by FindingLog.Add
        }
    }
}
