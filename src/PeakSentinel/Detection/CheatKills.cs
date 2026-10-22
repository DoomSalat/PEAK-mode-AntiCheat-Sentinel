using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class CheatKills
    {
        private const float UndoSeconds = 10f;

        private readonly Dictionary<Character, float> _killedAt = new Dictionary<Character, float>();

        public void Note(Character victim)
        {
            if (victim != null)
                _killedAt[victim] = Time.time;
        }

        public bool Undoes(Character victim)
        {
            return victim != null && _killedAt.TryGetValue(victim, out float killed) && Time.time - killed < UndoSeconds;
        }
    }
}
