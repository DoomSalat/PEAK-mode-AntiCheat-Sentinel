using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class HarmAudit : IRpcAudit
    {
        private const float FlingForce = 1500f;
        private const string KillsTag = "kills other players";
        private const string KnocksOutTag = "knocks out other players";
        private const string CursesTag = "zombifies or petrifies other players";
        private const string TeleportsTag = "teleports other players";
        private const string FlingsTag = "flings other players";

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of RPC names that can harm another player
        {
            RpcNames.Die, RpcNames.SetDead, RpcNames.PassOut, RpcNames.Zombify, RpcNames.Petrify, RpcNames.Warp, RpcNames.ForceAtPosition, RpcNames.ForceToBodyPart,
        };

        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public HarmAudit(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return Methods.Contains(method);
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost)
                return true;

            string victim = PlayerNames.Name(call.Owner);

            switch (call.Method)
            {
                case RpcNames.Die:
                case RpcNames.SetDead:
                    return Refuse(call, KillsTag, $"killed {victim}");

                case RpcNames.PassOut:
                    return Refuse(call, KnocksOutTag, $"knocked out {victim}");

                case RpcNames.Zombify:
                    return Refuse(call, CursesTag, $"zombified {victim}");

                case RpcNames.Petrify:
                    return Refuse(call, CursesTag, $"petrified {victim}");

                case RpcNames.Warp:
                    return Refuse(call, TeleportsTag, $"teleported {victim} to {call.Argument(0)}");

                case RpcNames.ForceAtPosition:
                    return CheckFling(call, victim, call.MagnitudeAt(0));

                default:
                    return CheckFling(call, victim, Mathf.Max(call.MagnitudeAt(1), call.MagnitudeAt(2)));
            }
        }

        private bool CheckFling(RpcCall call, string victim, float force)
        {
            return force < FlingForce || Refuse(call, FlingsTag, $"flung {victim} with force {force:F0}");
        }

        private bool Refuse(RpcCall call, string tag, string action)
        {
            return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, action, _detection.BlockHarmToOthers.Value);
        }
    }
}