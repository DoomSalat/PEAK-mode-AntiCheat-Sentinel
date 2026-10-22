using System;
using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class TimeWatch
    {
        private const double WindowSeconds = 5d;
        private const double MaxDrift = 0.1d;
        private const float ScaleSlack = 0.001f;
        private const string ClockFormat = "game clock runs at {0:P0} of real time (speed hack)";
        private const string ScaleFormat = "time scale is {0:F2} while the game sets {1:F2} (slow motion or fast forward)";
        private const string StepFormat = "physics step changed from {0:F4} s to {1:F4} s";

        private float _physicsStep = -1f;
        private DateTime _windowStart;
        private float _windowGameStart = -1f;

        public string Check()
        {
            if (_physicsStep < 0f)
                _physicsStep = Time.fixedDeltaTime;

            if (!Mathf.Approximately(Time.fixedDeltaTime, _physicsStep))
                return string.Format(StepFormat, _physicsStep, Time.fixedDeltaTime);

            float expected = GameUtils.GetTimeScale();

            if (Time.timeScale != 0f && Mathf.Abs(Time.timeScale - expected) > ScaleSlack)
                return string.Format(ScaleFormat, Time.timeScale, expected);

            return CheckClock();
        }

        private string CheckClock()
        {
            DateTime now = DateTime.UtcNow;
            float game = Time.realtimeSinceStartup;

            if (_windowGameStart < 0f)
            {
                _windowStart = now;
                _windowGameStart = game;
                return null;
            }

            double real = (now - _windowStart).TotalSeconds;

            if (real < WindowSeconds)
                return null;

            double ratio = (game - _windowGameStart) / real;
            _windowStart = now;
            _windowGameStart = game;
            return Math.Abs(ratio - 1d) > MaxDrift ? string.Format(ClockFormat, ratio) : null;
        }
    }
}
