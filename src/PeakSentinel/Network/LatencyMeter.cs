using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Network
{
    public sealed class LatencyMeter
    {
        private const float Smoothing = 0.15f;
        private const float StaleSeconds = 5f;
        private const int MaxDelay = 10000;

        private readonly Dictionary<int, (float Delay, float Seen)> _delays = new Dictionary<int, (float Delay, float Seen)>();

        public void Note(int actor, int sentServerTime)
        {
            int delay = unchecked(PhotonNetwork.ServerTimestamp - sentServerTime);

            if (delay < 0 || delay > MaxDelay)
                return;

            float smoothed = _delays.TryGetValue(actor, out (float Delay, float Seen) last) ? Mathf.Lerp(last.Delay, delay, Smoothing) : delay;
            _delays[actor] = (smoothed, Time.unscaledTime);
        }

        public int? PingOf(int actor)
        {
            if (!_delays.TryGetValue(actor, out (float Delay, float Seen) entry) || Time.unscaledTime - entry.Seen > StaleSeconds)
                return null;

            float ownHalf = PhotonNetwork.GetPing() / 2f;
            return Mathf.Max(0, Mathf.RoundToInt((entry.Delay - ownHalf) * 2f));
        }
    }
}