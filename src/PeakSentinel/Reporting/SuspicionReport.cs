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
        private const string FeedFormat = "<color=#5AA9FF>[Sentinel]</color> <color=#FFD060>{1}</color> {3}<color={0}>{2}</color>";
        private const string LeadFormat = FeedIcons.Shield + "<color=#5AA9FF>{0}</color>";
        private const string CheatColor = "#FF6060";
        private const string SuspectColor = "#FFA040";
        private const string GoodColor = "#70DC70";
        private const string CheatLevel = "CHEAT";
        private const string SuspectLevel = "SUSPICIOUS";
        private const string BlockedLevel = "BLOCKED";
        private const string NoticeLevel = "notice";
        private const string BlockedPrefix = "blocked: ";
        private const string LocalBlockPrefix = "blocked in your game only (you are not the host): ";
        private const string RecoveredLevel = "RECOVERED";
        private const string RecoveredPrefix = "recovered: ";
        private const string KickedLevel = "KICKED";
        private const string KickedPrefix = "kicked: ";
        private const string FixLevel = "FIX";
        private const string FixPrefix = "fix: ";
        private const string ActionLevel = "ACTION";
        private const string ActionPrefix = "by you: ";
        private const string SelfCheckLevel = "SELF-CHECK";
        private const string SelfCheckPrefix = "self-check: ";
        private const string SelfCheckNote = " (your own action, only a warning: nothing was blocked)";
        private const string TrustedNote = " (trusted by you, allowed)";
        private const string UnsureNote = " (heuristic, may be wrong)";

        private readonly ManualLogSource _log;
        private readonly DebugTrace _trace;
        private readonly FindingLog _findings;
        private readonly TrustList _trust;
        private readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>();

        public SuspicionReport(ManualLogSource log, DebugTrace trace, FindingLog findings, TrustList trust)
        {
            _log = log;
            _trace = trace;
            _findings = findings;
            _trust = trust;
        }

        public TrustList Trust => _trust;
        public PlayerConnectionLog Feed { get; set; } // Set: remembered by the connection log patch once the HUD wakes up

        public void Flag(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false, Repair repair = null)
        {
            Report(culprit, category, action, blocked ? FindingKind.Blocked : FindingKind.Cheat, repair, blocked ? BlockPrefix() : string.Empty);
        }

        public void Suspect(PhotonPlayer culprit, FindingCategory category, string action, bool blocked = false)
        {
            Report(culprit, category, action + UnsureNote, blocked ? FindingKind.Blocked : FindingKind.Suspicious, null, blocked ? BlockPrefix() : string.Empty);
        }

        public bool Block(PhotonPlayer culprit, FindingCategory category, string tag, string action)
        {
            Tag(culprit, tag);
            Report(culprit, category, action, FindingKind.Blocked, null, BlockedPrefix);
            return Spares(culprit);
        }

        public bool Recover(PhotonPlayer culprit, FindingCategory category, string tag, string action)
        {
            Tag(culprit, tag);
            return Recover(culprit, category, action);
        }

        public bool Recover(PhotonPlayer culprit, FindingCategory category, string action, bool unsure = false)
        {
            Report(culprit, category, unsure ? action + UnsureNote : action, FindingKind.Recovered, null, RecoveredPrefix);
            return Spares(culprit);
        }

        public void Kick(PhotonPlayer culprit, FindingCategory category, string tag, string action)
        {
            Tag(culprit, tag);
            Report(culprit, category, action, FindingKind.Kicked, null, KickedPrefix);
        }

        public bool Refuse(PhotonPlayer culprit, FindingCategory category, string tag, string action, bool block, Repair repair = null)
        {
            Tag(culprit, tag);
            Flag(culprit, category, action, block, repair);
            return !block || Spares(culprit);
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

        public void LogAction(PhotonPlayer target, string action)
        {
            LogAction(PlayerNames.Actor(target), PlayerNames.Name(target), action);
        }

        public void LogAction(string name, string action)
        {
            LogAction(0, name, action);
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

        public bool Spares(PhotonPlayer player)
        {
            return player != null && (player.IsLocal || _trust.Trusts(player));
        }

        public void Tag(PhotonPlayer culprit, string tag)
        {
            if (culprit != null && culprit.IsLocal)
                return;

            _findings.Tag(PlayerNames.Actor(culprit), tag);
        }

        private void Report(PhotonPlayer culprit, FindingCategory category, string action, FindingKind kind, Repair repair, string lead)
        {
            if (culprit != null && culprit.IsLocal)
            {
                WarnSelf(category, action);
                return;
            }

            if (Spares(culprit))
            {
                Notice(culprit, category, action + TrustedNote);
                return;
            }

            string name = PlayerNames.Name(culprit);
            string level = LevelOf(kind);
            _log.LogWarning($"{level} {name} (actor {PlayerNames.Actor(culprit)}): {action}");
            _trace.Finding(level, culprit, action);
            _findings.Add(PlayerNames.Actor(culprit), name, lead + action, kind, category, repair);
            Show(name, action, ColorOf(kind == FindingKind.Suspicious ? FeedTone.Suspect : FeedTone.Cheat), lead);
        }

        private void LogAction(int actor, string name, string action)
        {
            _log.LogInfo($"{ActionLevel} {name}: {action}");
            _trace.Finding(ActionLevel, PhotonNetwork.LocalPlayer, $"{name}: {action}");
            _findings.Add(actor, name, ActionPrefix + action, FindingKind.Fix, FindingCategory.Other);
            Show(name, action, ColorOf(FeedTone.Good), ActionPrefix);
        }

        private void WarnSelf(FindingCategory category, string action)
        {
            string name = PlayerNames.Name(PhotonNetwork.LocalPlayer);
            string text = action + SelfCheckNote;
            _log.LogWarning($"{SelfCheckLevel} {name}: {text}");
            _trace.Finding(SelfCheckLevel, PhotonNetwork.LocalPlayer, text);
            _findings.Add(PlayerNames.Actor(PhotonNetwork.LocalPlayer), name, SelfCheckPrefix + text, FindingKind.Suspicious, category);
            Show(name, text, ColorOf(FeedTone.Suspect), SelfCheckPrefix);
        }

        private string BlockPrefix()
        {
            return PhotonNetwork.IsMasterClient ? BlockedPrefix : LocalBlockPrefix;
        }

        private string LevelOf(FindingKind kind)
        {
            switch (kind)
            {
                case FindingKind.Blocked:
                    return BlockedLevel;

                case FindingKind.Recovered:
                    return RecoveredLevel;

                case FindingKind.Kicked:
                    return KickedLevel;

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

        private void Show(string name, string action, string color, string lead = "")
        {
            string key = name + lead + action;
            float now = Time.unscaledTime;

            if (_lastShown.TryGetValue(key, out float last) && now - last < RepeatSeconds)
                return;

            _lastShown[key] = now;

            if (Feed != null)
                Feed.AddMessage(string.Format(FeedFormat, color, name, action, lead.Length > 0 ? string.Format(LeadFormat, lead) : string.Empty));
        }
    }
}