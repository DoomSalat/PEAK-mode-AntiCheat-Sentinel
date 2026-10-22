using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Network
{
    public sealed class DelayedCalls : MonoBehaviour
    {
        private readonly Dictionary<int, (float At, Action Call)> _calls = new Dictionary<int, (float At, Action Call)>();
        private readonly List<int> _due = new List<int>();

        private void Update()
        {
            if (_calls.Count == 0)
                return;

            foreach (KeyValuePair<int, (float At, Action Call)> call in _calls)
            {
                if (Time.unscaledTime >= call.Value.At)
                    _due.Add(call.Key);
            }

            foreach (int key in _due)
            {
                Action call = _calls[key].Call;
                _calls.Remove(key);
                call();
            }

            _due.Clear();
        }

        public void After(int key, float seconds, Action call)
        {
            _calls[key] = (Time.unscaledTime + seconds, call);
        }

        public bool Pending(int key)
        {
            return _calls.ContainsKey(key);
        }
    }
}
