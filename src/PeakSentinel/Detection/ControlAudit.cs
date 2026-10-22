using System.Collections.Generic;
using System.Linq;
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
            { RpcNames.SyncAfflictions, "show fake effects" },
            { RpcNames.SyncThorns, "show fake thorns" },
            { RpcNames.ApplyStatuses, "get statuses" },
            { RpcNames.UnPassOut, "wake up" },
            { RpcNames.StopVineClimbing, "let go of the vine" },
            { RpcNames.ClimbJump, "jump off the wall" },
            { RpcNames.Stick, "get stuck" },
            { RpcNames.ClearStickData, "come unstuck" },
            { RpcNames.ClearJoint, "come unstuck" },
            { RpcNames.DestroyHeldItem, "lose the item in hand" },
            { RpcNames.ThrowItem, "throw an item" },
        };

        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;

        public ControlAudit(VictimGuard guard, DetectionSettings detection)
        {
            _guard = guard;
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

            return _guard.Refuse(call.Sender, call.Component<Character>(), FindingCategory.Cheats, Tag, $"made {PlayerNames.Name(call.Owner)} {Actions[call.Method]}{StatusesOf(call)}", _detection.BlockForcedActions.Value);
        }

        private string StatusesOf(RpcCall call)
        {
            if (call.Method != RpcNames.ApplyStatuses || !(call.Argument(0) is float[] amounts))
                return string.Empty;

            IEnumerable<string> statuses = amounts.Select((amount, index) => (amount, index)).Where(status => status.amount != 0f).Select(status => $"{(CharacterAfflictions.STATUSTYPE)status.index} {status.amount * 100f:+0;-0}%");
            return $": {string.Join(", ", statuses)} (a host-only command, the game ignores it from anyone else)";
        }
    }
}