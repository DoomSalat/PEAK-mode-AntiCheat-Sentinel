using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class CookingEchoes
    {
        private const float EchoSeconds = 3f;

        private readonly Dictionary<Character, (float Time, ItemAction[] Actions)> _echoes = new Dictionary<Character, (float Time, ItemAction[] Actions)>();

        public void Note(Character holder, ItemAction[] actions)
        {
            if (holder != null && actions != null)
                _echoes[holder] = (Time.time, actions);
        }

        public bool Explains<T>(Character victim) where T : ItemAction
        {
            if (victim == null || !_echoes.TryGetValue(victim, out (float Time, ItemAction[] Actions) echo) || Time.time - echo.Time > EchoSeconds)
                return false;

            foreach (ItemAction action in echo.Actions)
            {
                if (action is T)
                    return true;
            }

            return false;
        }
    }
}
