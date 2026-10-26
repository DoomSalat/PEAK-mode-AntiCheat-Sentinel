using System.Collections.Generic;

namespace PeakSentinel.Ui
{
    public sealed class ConfirmClicks
    {
        private readonly Dictionary<string, int> _armed = new Dictionary<string, int>();

        public int Version { get; private set; }

        public bool Confirm(string action, int actor)
        {
            Version++;

            if (_armed.TryGetValue(action, out int armed) && armed == actor)
            {
                _armed.Remove(action);
                return true;
            }

            _armed[action] = actor;
            return false;
        }

        public string LabelOf(string action, int actor, string label, string confirmLabel)
        {
            return _armed.TryGetValue(action, out int armed) && armed == actor ? confirmLabel : label;
        }
    }
}
