using System.Collections.Generic;
using PeakSentinel.Reporting;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class HarmAudit
    {
        private const string Die = "RPCA_Die";
        private const string SetDead = "RPCA_SetDead";
        private const string PassOut = "RPCA_PassOut";
        private const string Zombify = "RPCA_Zombify";
        private const string Petrify = "RPC_PetrifyInstantly";
        private const string Warp = "WarpPlayerRPC";
        private const string ForceAtPosition = "RPCA_AddForceAtPosition";
        private const string ForceToBodyPart = "RPCA_AddForceToBodyPart";
        private const float FlingForce = 1500f;
        private const string KillsTag = "kills other players";
        private const string KnocksOutTag = "knocks out other players";
        private const string CursesTag = "zombifies or petrifies other players";
        private const string TeleportsTag = "teleports other players";
        private const string FlingsTag = "flings other players";

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of RPC names that can harm another player
        {
            Die, SetDead, PassOut, Zombify, Petrify, Warp, ForceAtPosition, ForceToBodyPart,
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

        public bool Inspect(string method, PhotonPlayer sender, PhotonPlayer victim, object[] arguments)
        {
            if (!_detection.Harm.Value || sender == null || victim == null || sender.ActorNumber == victim.ActorNumber || sender.IsMasterClient)
                return true;

            string name = SuspicionReport.NameOf(victim);

            switch (method)
            {
                case Die:
                case SetDead:
                    return Refuse(sender, KillsTag, $"killed {name}");

                case PassOut:
                    return Refuse(sender, KnocksOutTag, $"knocked out {name}");

                case Zombify:
                case Petrify:
                    return Refuse(sender, CursesTag, $"{(method == Zombify ? "zombified" : "petrified")} {name}");

                case Warp:
                    return Refuse(sender, TeleportsTag, $"teleported {name} to {Argument(arguments, 0)}");

                case ForceAtPosition:
                    return CheckFling(sender, name, Magnitude(arguments, 0));

                case ForceToBodyPart:
                    return CheckFling(sender, name, Mathf.Max(Magnitude(arguments, 1), Magnitude(arguments, 2)));

                default:
                    return true;
            }
        }

        private bool CheckFling(PhotonPlayer sender, string victim, float force)
        {
            return force < FlingForce || Refuse(sender, FlingsTag, $"flung {victim} with force {force:F0}");
        }

        private bool Refuse(PhotonPlayer sender, string tag, string action)
        {
            bool block = _detection.BlockHarm.Value;
            _report.Tag(sender, tag);
            _report.Flag(sender, FindingCategory.Cheats, action, block);
            return !block;
        }

        private float Magnitude(object[] arguments, int index)
        {
            return arguments != null && arguments.Length > index && arguments[index] is Vector3 force ? force.magnitude : 0f;
        }

        private object Argument(object[] arguments, int index)
        {
            return arguments != null && arguments.Length > index ? arguments[index] : null;
        }
    }
}
