using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
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
            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || call.FromLocal)
                return true;

            string victim = PlayerNames.Name(call.Owner);
            Character character = call.Component<Character>();

            switch (call.Method)
            {
                case RpcNames.Die:
                case RpcNames.SetDead:
                    return Refuse(call, KillsTag, $"killed {victim}", Revive(character, victim));

                case RpcNames.PassOut:
                    return Refuse(call, KnocksOutTag, $"knocked out {victim}", WakeUp(character, victim));

                case RpcNames.Zombify:
                    return Refuse(call, CursesTag, $"zombified {victim}", null);

                case RpcNames.Petrify:
                    return Refuse(call, CursesTag, $"petrified {victim}", null);

                case RpcNames.Warp:
                    return Refuse(call, TeleportsTag, $"teleported {victim} to {call.Argument(0)}", Return(character, victim));

                case RpcNames.ForceAtPosition:
                    return CheckFling(call, victim, call.MagnitudeAt(0), character);

                default:
                    return CheckFling(call, victim, Mathf.Max(call.MagnitudeAt(1), call.MagnitudeAt(2)), character);
            }
        }

        private bool CheckFling(RpcCall call, string victim, float force, Character character)
        {
            return force < FlingForce || Refuse(call, FlingsTag, $"flung {victim} with force {force:F0}", Return(character, victim));
        }

        private bool Refuse(RpcCall call, string tag, string action, Repair repair)
        {
            return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, action, _detection.BlockHarmToOthers.Value, repair);
        }

        private Repair Revive(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Repair($"revived {victim} where they died", () => character.photonView.RPC(RpcNames.ReviveAtPosition, RpcTarget.All, place, false, -1));
        }

        private Repair WakeUp(Character character, string victim)
        {
            return character == null ? null : new Repair($"woke {victim} up", () => character.photonView.RPC(RpcNames.UnPassOut, RpcTarget.All));
        }

        private Repair Return(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Repair($"returned {victim} to where they stood", () => character.photonView.RPC(RpcNames.Warp, RpcTarget.All, place, true));
        }
    }
}