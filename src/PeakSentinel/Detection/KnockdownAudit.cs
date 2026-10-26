using System.Collections.Generic;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class KnockdownAudit : IRpcAudit
    {
        private const float LongFallSeconds = 3f;
        private const float EchoSeconds = 1f;
        private const float JoinSyncSeconds = 15f;
        private const string Tag = "knocks other players down";
        private const string NoCause = "no kick, nothing they threw hit them";
        private const string SpawnedCause = "with an item they spawned out of nowhere";

        private readonly SuspicionReport _report;
        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;
        private readonly LegitForces _forces;
        private readonly EnvironmentMarks _environment;
        private readonly KickClock _kicks;
        private readonly SpawnBlame _blame;
        private readonly SpawnClock _spawns;
        private readonly Dictionary<Character, (float At, float Seconds)> _ownFalls = new Dictionary<Character, (float At, float Seconds)>();

        public KnockdownAudit(SuspicionReport report, VictimGuard guard, DetectionSettings detection, LegitForces forces, EnvironmentMarks environment, KickClock kicks, SpawnBlame blame, SpawnClock spawns)
        {
            _spawns = spawns;
            _guard = guard;
            _blame = blame;
            _kicks = kicks;
            _report = report;
            _detection = detection;
            _forces = forces;
            _environment = environment;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Fall;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.FromLocal)
            {
                NoteOwnFall(call);
                return true;
            }

            if (call.SelfCheck)
                return true; // Our own game sends the vanilla echo falls (frog tongue, rescue hook) too: checking them would only warn about the game itself

            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || _kicks.JustKicked(call.Sender) || JustLoaded())
                return true;

            Character victim = call.Component<Character>();
            float seconds = SecondsOf(call);

            if (victim == null || _forces.Explains(call.Sender, victim) || _environment.CauseOf(victim) != null || EchoesOwnFall(victim, seconds))
                return true;

            string name = PlayerNames.Name(call.Owner);

            if (seconds <= LongFallSeconds && !_blame.Blames(call.Sender))
            {
                _report.Suspect(call.Sender, FindingCategory.Cheats, $"knocked {name} down for {seconds:F1} s ({NoCause})");
                return true;
            }

            string action = $"knocked {name} down for {seconds:F1} s ({(_blame.Blames(call.Sender) ? SpawnedCause : NoCause)})";
            Recovery standUp = new Recovery($"stood {name} back up", target => victim.photonView.RPC(RpcNames.UnFall, target), true);
            return _guard.Refuse(call.Sender, victim, FindingCategory.Cheats, Tag, action, _detection.BlockHarmToOthers.Value, standUp);
        }

        private void NoteOwnFall(RpcCall call)
        {
            Character victim = call.Component<Character>();

            if (victim != null)
                _ownFalls[victim] = (Time.time, SecondsOf(call));
        }

        private bool EchoesOwnFall(Character victim, float seconds)
        {
            return _ownFalls.TryGetValue(victim, out (float At, float Seconds) own) && Time.time - own.At <= EchoSeconds && Mathf.Approximately(own.Seconds, seconds);
        }

        private bool JustLoaded()
        {
            Character me = Character.localCharacter;
            return me == null || _spawns.SpawnedWithin(me, JoinSyncSeconds); // Right after loading, the falls queued while we loaded play at once and our own game sent none of them
        }

        private float SecondsOf(RpcCall call)
        {
            return call.Argument(0) is float fall ? fall : 0f;
        }
    }
}
