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
        private const string FlagLevel = "SUSPICIOUS";
        private const string BlockedLevel = "BLOCKED";
        private const string NoticeLevel = "notice";
        private const string BlockedPrefix = "blocked: ";

        private readonly ManualLogSource _log;
        private readonly DebugTrace _trace;
        private readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>();

        public SuspicionReport(ManualLogSource log, DebugTrace trace)
        {
            _log = log;
            _trace = trace;
        }

        public PlayerConnectionLog Feed { get; set; } // Set: remembered by the connection log patch once the HUD wakes up
        public SentinelWindow Window { get; set; } // Set: assigned once by SentinelPlugin after the window object exists

        public void Flag(PhotonPlayer culprit, string action, bool blocked = false)
        {
            string name = NameOf(culprit);
            string level = blocked ? BlockedLevel : FlagLevel;
            string text = blocked ? BlockedPrefix + action : action;
            _log.LogWarning($"{level} {name} (actor {ActorOf(culprit)}): {action}");
            _trace.Finding(level, culprit, action);
            Window?.Add($"{name}: {text}", true, blocked);
            Show(name, text);
        }

        public void Notice(PhotonPlayer culprit, string action)
        {
            _log.LogInfo($"{NoticeLevel} {NameOf(culprit)} (actor {ActorOf(culprit)}): {action}");
            _trace.Finding(NoticeLevel, culprit, action);
            Window?.Add($"{NameOf(culprit)}: {action}", false, false);
        }

        public static string NameOf(PhotonPlayer player) // Static: pure formatting shared by the audits
        {
            if (player == null)
                return "unknown";

            return string.IsNullOrEmpty(player.NickName) ? $"actor {player.ActorNumber}" : player.NickName;
        }

        private static int ActorOf(PhotonPlayer player) // Static: pure formatting
        {
            return player == null ? 0 : player.ActorNumber;
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
