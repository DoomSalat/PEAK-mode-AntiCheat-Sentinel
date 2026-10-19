using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class EnvironmentMarks
    {
        private const float MarkSeconds = 1.5f;

        private readonly Dictionary<Character, (float Time, string Cause)> _marks = new Dictionary<Character, (float Time, string Cause)>();

        public void Mark(Character character, string cause)
        {
            if (character != null)
                _marks[character] = (Time.time, cause);
        }

        public string CauseOf(Character character)
        {
            return _marks.TryGetValue(character, out (float Time, string Cause) mark) && Time.time - mark.Time < MarkSeconds ? mark.Cause : null;
        }
    }
}
