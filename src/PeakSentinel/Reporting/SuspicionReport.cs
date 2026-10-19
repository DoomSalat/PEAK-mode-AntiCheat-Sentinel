using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public sealed class SuspicionReport
    {
        private const float RepeatSeconds = 2f;
        private const string FeedFormat = "<color=#FF6060>[Sentinel]</color> <color=#FFD060>{0}</color> {1}";
        private const string CheatLevel = "CHEAT";
        private const string SuspectLevel = "SUSPICIOUS";
        private const string BlockedLevel = "BLOCKED";
        private const string NoticeLevel = "notice";
        private const string BlockedPrefix = "blocked: ";

        private readonly ManualLogSource _log;
        private readonly DebugTrace _trace;
        private readonly FindingLog _findings;
        private readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>();

        public SuspicionReport(ManualLogSource log, DebugTrace trace, FindingLog findings)
        {
            _log = log;
            _trace = trace;
            _findings = findings;
        }

        public PlayerConnectionLog Feed { get; set; } // Set: remembered by the connection log patch once the HUD wakes up

        public void Flag(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false)
        {
            Report(culprit, category, action, blocked ? FindingKind.Blocked : FindingKind.Cheat);
        }

        public void Suspect(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false)
        {
            Report(culprit, category, action, blocked ? FindingKind.Blocked : FindingKind.Suspicious);
        }

        public void Notice(PhotonPlayer culprit, FindingCategory category, string action)
        {
            _log.LogInfo($"{NoticeLevel} {NameOf(culprit)} (actor {ActorOf(culprit)}): {action}");
            _trace.Finding(NoticeLevel, culprit, action);
            _findings.Add(ActorOf(culprit), NameOf(culprit), action, FindingKind.Notice, category);
        }

        public void Tag(PhotonPlayer culprit, string tag)
        {
            _findings.Tag(ActorOf(culprit), tag);
        }

        public static string NameOf(PhotonPlayer player) // Static: pure formatting shared by the audits
        {
            if (player == null)
                return "unknown";

            return string.IsNullOrEmpty(player.NickName) ? $"actor {player.ActorNumber}" : player.NickName;
        }

        public static int ActorOf(PhotonPlayer player) // Static: pure formatting shared by the audits
        {
            return player == null ? 0 : player.ActorNumber;
        }

        private void Report(PhotonPlayer culprit, FindingCategory category, string action, FindingKind kind)
        {
            string name = NameOf(culprit);
            string level = LevelOf(kind);
            string text = kind == FindingKind.Blocked ? BlockedPrefix + action : action;
            _log.LogWarning($"{level} {name} (actor {ActorOf(culprit)}): {action}");
            _trace.Finding(level, culprit, action);
            _findings.Add(ActorOf(culprit), name, text, kind, category);
            Show(name, text);
        }

        private string LevelOf(FindingKind kind)
        {
            switch (kind)
            {
                case FindingKind.Blocked:
                    return BlockedLevel;

                case FindingKind.Cheat:
                    return CheatLevel;

                default:
                    return SuspectLevel;
            }
        }

        private void Show(string name, string action)
        {
            string key = name + action;
            float now = Time.unscaledTime;

            if (_lastShown.TryGetValue(key, out float last) && now - last < RepeatSeconds)
                return;

            _lastShown[key] = now;

            if (Feed != null)
                Feed.AddMessage(string.Format(FeedFormat, name, action));
        }
    }
}
