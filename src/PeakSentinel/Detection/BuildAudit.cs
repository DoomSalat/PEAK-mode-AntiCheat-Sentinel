using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class BuildAudit : IRpcAudit
    {
        private const float WindowSeconds = 60f;
        private const string Tag = "spams buildables";

        private readonly Dictionary<int, List<float>> _builds = new Dictionary<int, List<float>>();
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;
        private readonly FloodSettings _flood;

        public BuildAudit(SuspicionReport report, DetectionSettings detection, FloodSettings flood)
        {
            _report = report;
            _detection = detection;
            _flood = flood;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Build;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.BuildSpam.Value || call.Sender == null || call.FromLocal || call.FromHost)
                return true;

            string what = call.View == null ? "something" : call.View.name;

            if (call.Owner != null && !call.FromOwner)
                return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, $"built '{what}' with an item of {PlayerNames.Name(call.Owner)}", _detection.BlockBuildSpam.Value);

            int recent = CountRecent(call.Sender.ActorNumber);

            if (recent <= _flood.MaxBuildsPerMinute.Value)
                return true;

            return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, $"built {recent} things in a minute ('{what}'), more than {_flood.MaxBuildsPerMinute.Value}", _detection.BlockBuildSpam.Value);
        }

        private int CountRecent(int actor)
        {
            if (!_builds.TryGetValue(actor, out List<float> times))
            {
                times = new List<float>();
                _builds[actor] = times;
            }

            float now = Time.time;
            times.Add(now);
            times.RemoveAll(time => now - time > WindowSeconds);
            return times.Count;
        }
    }
}