using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class PushAudit : IRpcAudit
    {
        private const float KickSeconds = 2f;
        private const float BurstSeconds = 0.15f;
        private const int BurstPushes = 2;
        private const float PulseSeconds = 3f;
        private const int PulsePushes = 4;
        private const float BlockSeconds = 3f;
        private const string PulseTag = "pushes other players again and again";
        private const string ExplosionTag = "blasts players with no explosive";
        private const string PushTag = "pushes other players";
        private const float ThrownReach = 5f;

        private readonly Dictionary<int, float> _kickedAt = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _blockedUntil = new Dictionary<int, float>();
        private readonly Dictionary<int, Queue<float>> _bursts = new Dictionary<int, Queue<float>>();
        private readonly Dictionary<(int Sender, int Victim), Queue<float>> _pulses = new Dictionary<(int Sender, int Victim), Queue<float>>();
        private readonly Dictionary<(int Sender, int Victim), Vector3> _standing = new Dictionary<(int Sender, int Victim), Vector3>();
        private readonly ItemLocator _locator;
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public PushAudit(SuspicionReport report, DetectionSettings detection, ItemLocator locator)
        {
            _locator = locator;
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return method == RpcNames.ForceAtPosition || method == RpcNames.ForceToBodyPart || method == RpcNames.Kick;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Sender == null)
                return true;

            float now = Time.time;

            if (call.Method == RpcNames.Kick)
            {
                _kickedAt[call.Sender.ActorNumber] = now;
                return true;
            }

            if (!_detection.HarmToOthers.Value || call.Owner == null || call.FromHost || call.FromLocal || Kicked(call.Sender, now))
                return true;

            if (call.FromOwner)
            {
                Record(_bursts, call.Sender.ActorNumber, now, BurstSeconds);
                return true;
            }

            Character victim = call.Component<Character>();

            if (victim == null)
                return true;

            int sender = call.Sender.ActorNumber;
            (int, int) pair = (sender, call.Owner.ActorNumber);

            int burst = Record(_bursts, sender, now, BurstSeconds);
            int pulse = Record(_pulses, pair, now, PulseSeconds);

            if (pulse == 1)
                _standing[pair] = victim.Center;

            if (_blockedUntil.TryGetValue(sender, out float until) && now < until)
            {
                _blockedUntil[sender] = now + BlockSeconds;
                return false;
            }

            if (burst >= BurstPushes)
                return Refuse(call, ExplosionTag, $"blasted {PlayerNames.Name(call.Owner)} away with no explosive", victim, pair);

            if (pulse >= PulsePushes)
                return Refuse(call, PulseTag, $"pushed {PlayerNames.Name(call.Owner)} again and again (not a kick)", victim, pair);

            if (_locator.LooseItemNear(call.Sender, victim.Center, ThrownReach))
                return true;

            return Refuse(call, PushTag, $"pushed {PlayerNames.Name(call.Owner)} (no kick, nothing they threw hit them)", victim, pair);
        }

        private bool Kicked(PhotonPlayer sender, float now)
        {
            return _kickedAt.TryGetValue(sender.ActorNumber, out float kicked) && now - kicked < KickSeconds;
        }

        private int Record<TKey>(Dictionary<TKey, Queue<float>> windows, TKey key, float now, float seconds)
        {
            if (!windows.TryGetValue(key, out Queue<float> times))
            {
                times = new Queue<float>();
                windows[key] = times;
            }

            times.Enqueue(now);

            while (now - times.Peek() > seconds)
                times.Dequeue();

            return times.Count;
        }

        private bool Refuse(RpcCall call, string tag, string action, Character victim, (int, int) pair)
        {
            bool block = _detection.BlockHarmToOthers.Value;

            if (block)
                _blockedUntil[call.Sender.ActorNumber] = Time.time + BlockSeconds;

            Vector3 place = _standing[pair];
            string name = PlayerNames.Name(call.Owner);
            Repair repair = new Repair($"returned {name} to where they stood", () => victim.photonView.RPC(RpcNames.Warp, RpcTarget.All, place, true));
            return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, action, block, repair);
        }
    }
}
