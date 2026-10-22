using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class BalloonAudit : IRpcAudit
    {
        private const string ForeignTag = "ties balloons onto other players";
        private const string SelfTag = "ties balloons without a balloon";
        private const float JoinSyncSeconds = 15f;
        private const float ConsumeSeconds = 3f;
        private const float TieWindowSeconds = 1.5f;
        private const int TiesPerUse = 3;

        private readonly SuspicionReport _report;
        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;
        private readonly ProtectionSettings _protection;
        private readonly SpawnClock _spawns;
        private readonly ForeignObjects _objects;
        private readonly Dictionary<int, (float Since, int Count)> _ties = new Dictionary<int, (float Since, int Count)>();

        public BalloonAudit(SuspicionReport report, VictimGuard guard, DetectionSettings detection, ProtectionSettings protection, SpawnClock spawns, ForeignObjects objects)
        {
            _guard = guard;
            _objects = objects;
            _spawns = spawns;
            _protection = protection;
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.TieBalloon;
        }

        public bool Inspect(RpcCall call)
        {
            TiedBalloon balloon = call.Component<TiedBalloon>();

            if (balloon == null || call.Sender == null || call.FromHost || call.FromLocal)
                return true;

            if (!(call.Argument(2) is int color) || !IsColor(color))
                return _report.Refuse(call.Sender, FindingCategory.Cheats, SelfTag, $"tied a balloon with an invalid color {call.Argument(2)} (breaks it for everyone), always blocked", true);

            PhotonView target = call.ViewAt(0);
            Character tiedTo = target == null ? null : target.GetComponent<Character>();

            if (tiedTo == null)
                return true;

            if (!call.SentBy(tiedTo.photonView.Owner))
                return CheckForeign(call, balloon, tiedTo);

            return CheckSelf(call, balloon, tiedTo);
        }

        private bool CheckForeign(RpcCall call, TiedBalloon balloon, Character victim)
        {
            if (!_detection.HarmToOthers.Value)
                return true;

            Recovery removal = new Recovery("removed the balloon", _ => _objects.RemoveForEveryone(balloon.gameObject, call.Sender), false);
            return _guard.Refuse(call.Sender, victim, FindingCategory.Cheats, ForeignTag, $"tied a balloon onto {PlayerNames.Name(victim.photonView.Owner)}", _detection.BlockHarmToOthers.Value, removal);
        }

        private bool CheckSelf(RpcCall call, TiedBalloon balloon, Character self)
        {
            if (_spawns.JoinedRoomWithin(JoinSyncSeconds))
                return true;

            bool used = HoldsBalloon(self) || Time.time - self.data.lastConsumedItem < ConsumeSeconds;

            if (used && !Overtied(call.Sender))
                return true;

            string action = used ? $"tied more than {TiesPerUse} balloons from one balloon" : "tied a balloon to themselves without holding one";

            if (!PhotonNetwork.IsMasterClient)
            {
                _report.Flag(call.Sender, FindingCategory.Cheats, $"{action} (only the host can remove it)");
                _report.Tag(call.Sender, SelfTag);
                return true;
            }

            return Remove(call, balloon, SelfTag, action, _protection.BlockCheatSpawns.Value);
        }

        private bool Remove(RpcCall call, TiedBalloon balloon, string tag, string action, bool block)
        {
            if (!block)
                return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, action, false);

            _objects.RemoveForEveryone(balloon.gameObject, call.Sender);
            return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, $"{action}, removed for everyone", true);
        }

        private bool Overtied(PhotonPlayer sender)
        {
            int actor = sender.ActorNumber;
            bool fresh = !_ties.TryGetValue(actor, out (float Since, int Count) ties) || Time.time - ties.Since > TieWindowSeconds;
            ties = fresh ? (Time.time, 1) : (ties.Since, ties.Count + 1);
            _ties[actor] = ties;
            return ties.Count > TiesPerUse;
        }

        private bool HoldsBalloon(Character character)
        {
            Item held = character.data.currentItem;
            return held != null && held.TryGetComponent(out Action_Balloon _);
        }

        private bool IsColor(int color)
        {
            Character local = Character.localCharacter;
            return color >= 0 && (local == null || color < local.refs.balloons.balloonColors.Length);
        }
    }
}
