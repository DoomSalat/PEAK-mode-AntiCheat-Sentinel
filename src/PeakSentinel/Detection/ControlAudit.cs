using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;

namespace PeakSentinel.Detection
{
    public sealed class ControlAudit : IRpcAudit
    {
        private const string Tag = "controls other players";

        private static readonly Dictionary<string, string> Actions = new Dictionary<string, string> // Static: fixed RPCs only the character's owner sends, with what they force
        {
            { RpcNames.Jump, "jump" },
            { RpcNames.Crouch, "crouch" },
            { RpcNames.StopClimbing, "let go of the wall" },
            { RpcNames.StopRopeClimbing, "let go of the rope" },
            { RpcNames.DropFromSlot, "drop an item" },
            { RpcNames.Emote, "emote" },
            { RpcNames.ShownDead, "look dead" },
            { RpcNames.ShownPassedOut, "look passed out" },
            { RpcNames.SyncStatuses, "show fake statuses" },
            { RpcNames.ApplyStatuses, "get statuses" },
            { RpcNames.UnPassOut, "wake up" },
        };

        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public ControlAudit(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return Actions.ContainsKey(method);
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.ForcedActions.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || call.FromLocal)
                return true;

            return _report.Refuse(call.Sender, FindingCategory.Cheats, Tag, $"made {PlayerNames.Name(call.Owner)} {Actions[call.Method]}", _detection.BlockForcedActions.Value);
        }
    }
}