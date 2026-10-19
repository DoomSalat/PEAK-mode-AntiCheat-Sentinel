using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class FlightTrack
    {
        private readonly List<(float Time, float Height)> _samples = new List<(float Time, float Height)>();

        public float MissingFor { get; set; } // Set: accumulated by FlightAudit while gravity is missing
        public bool Flagged { get; set; } // Set: FlightAudit reports each airborne episode once
        public float NextTrace { get; set; } // Set: FlightAudit throttles the trace
        public Vector3? LastGround { get; set; } // Set: FlightAudit remembers where the player last stood, kept across resets
        public float FlatSpeed { get; set; } // Set: latest horizontal speed for the trace
        public float FastFor { get; set; } // Set: accumulated by FlightAudit while the player is faster than a run
        public bool SpeedFlagged { get; set; } // Set: FlightAudit reports each too-fast stretch once

        public void Add(float time, float height, float keep)
        {
            _samples.Add((time, height));

            while (_samples.Count > 0 && time - _samples[0].Time > keep)
                _samples.RemoveAt(0);
        }

        public void Reset()
        {
            _samples.Clear();
            MissingFor = 0f;
            Flagged = false;
        }

        public bool TrySpeeds(float now, float window, float span, out float speedNow, out float speedThen)
        {
            speedNow = 0f;
            speedThen = 0f;

            if (_samples.Count < 2 || now - _samples[0].Time < window + span)
                return false;

            speedNow = SpeedAt(now, span);
            speedThen = SpeedAt(now - window, span);
            return true;
        }

        private float SpeedAt(float time, float span)
        {
            return (HeightAt(time) - HeightAt(time - span)) / span;
        }

        private float HeightAt(float time)
        {
            for (int i = _samples.Count - 1; i >= 0; i--)
            {
                if (_samples[i].Time <= time)
                    return _samples[i].Height;
            }

            return _samples[0].Height;
        }
    }
}