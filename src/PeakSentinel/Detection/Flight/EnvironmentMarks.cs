using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class EnvironmentMarks
    {
        private const float MarkSeconds = 1.5f;

        private readonly Dictionary<Character, (float Until, string Cause)> _marks = new Dictionary<Character, (float Until, string Cause)>();

        public void Mark(Character character, string cause)
        {
            Mark(character, cause, MarkSeconds);
        }

        public void Mark(Character character, string cause, float seconds)
        {
            if (character != null)
                _marks[character] = (Time.time + seconds, cause);
        }

        public string CauseOf(Character character)
        {
            return _marks.TryGetValue(character, out (float Until, string Cause) mark) && Time.time < mark.Until ? mark.Cause : null;
        }
    }
}
