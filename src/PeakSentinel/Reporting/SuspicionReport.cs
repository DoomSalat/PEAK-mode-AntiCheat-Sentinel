using System.Collections.Generic;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public sealed class SuspicionReport
    {
        private const float RepeatSeconds = 2f;
        private const string FeedFormat = "<color=#5AA9FF>[Sentinel]</color> <color=#FFD060>{1}</color> <color={0}>{2}</color>";
        private const string CheatColor = "#FF6060";
        private const string SuspectColor = "#FFA040";
        private const string GoodColor = "#70DC70";
        private const string CheatLevel = "CHEAT";
        private const string SuspectLevel = "SUSPICIOUS";
        private const string BlockedLevel = "BLOCKED";
        private const string NoticeLevel = "notice";
        private const string BlockedPrefix = "blocked: ";
        private const string FixLevel = "FIX";
        private const string FixPrefix = "fix: ";
        private const string UnsureNote = " (heuristic, may be wrong)";

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

        public void Flag(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false, Repair repair = null)
        {
            Report(culprit, category, action, blocked ? FindingKind.Blocked : FindingKind.Cheat, repair);
        }

        public void Suspect(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false)
        {
            Report(culprit, category, action + UnsureNote, blocked ? FindingKind.Blocked : FindingKind.Suspicious, null);
        }

        public bool Refuse(PhotonPlayer culprit, FindingCategory category, string tag, string action, bool block, Repair repair = null)
        {
            Tag(culprit, tag);
            Flag(culprit, category, action, block, repair);
            return !block;
        }

        public void Fix(FindingLog.Finding finding)
        {
            if (finding.Repair == null || finding.Fixed)
                return;

            finding.Repair.Apply();
            _findings.MarkFixed(finding);
            string text = FixPrefix + finding.Repair.Action;
            _log.LogInfo($"{FixLevel} {text} (after: {finding.Name} {finding.Text})");
            _trace.Finding(FixLevel, PhotonNetwork.LocalPlayer, text);
            _findings.Add(PlayerNames.Actor(PhotonNetwork.LocalPlayer), PlayerNames.Name(PhotonNetwork.LocalPlayer), text, FindingKind.Fix, finding.Category);
            Show(PlayerNames.Name(PhotonNetwork.LocalPlayer), text, ColorOf(FeedTone.Good));
        }

        public void Notice(PhotonPlayer culprit, FindingCategory category, string action)
        {
            _log.LogInfo($"{NoticeLevel} {PlayerNames.Name(culprit)} (actor {PlayerNames.Actor(culprit)}): {action}");
            _trace.Finding(NoticeLevel, culprit, action);
            _findings.Add(PlayerNames.Actor(culprit), PlayerNames.Name(culprit), action, FindingKind.Notice, category);
        }

        public void Announce(PhotonPlayer subject, FindingCategory category, string action, FeedTone tone)
        {
            Notice(subject, category, action);
            Show(PlayerNames.Name(subject), action, ColorOf(tone));
        }

        public void Tag(PhotonPlayer culprit, string tag)
        {
            _findings.Tag(PlayerNames.Actor(culprit), tag);
        }

        private void Report(PhotonPlayer culprit, FindingCategory category, string action, FindingKind kind, Repair repair)
        {
            string name = PlayerNames.Name(culprit);
            string level = LevelOf(kind);
            string text = kind == FindingKind.Blocked ? BlockedPrefix + action : action;
            _log.LogWarning($"{level} {name} (actor {PlayerNames.Actor(culprit)}): {action}");
            _trace.Finding(level, culprit, action);
            _findings.Add(PlayerNames.Actor(culprit), name, text, kind, category, repair);
            Show(name, text, ColorOf(kind == FindingKind.Suspicious ? FeedTone.Suspect : FeedTone.Cheat));
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

        private string ColorOf(FeedTone tone)
        {
            switch (tone)
            {
                case FeedTone.Good:
                    return GoodColor;

                case FeedTone.Suspect:
                    return SuspectColor;

                default:
                    return CheatColor;
            }
        }

        private void Show(string name, string action, string color)
        {
            string key = name + action;
            float now = Time.unscaledTime;

            if (_lastShown.TryGetValue(key, out float last) && now - last < RepeatSeconds)
                return;

            _lastShown[key] = now;

            if (Feed != null)
                Feed.AddMessage(string.Format(FeedFormat, color, name, action));
        }
    }
}