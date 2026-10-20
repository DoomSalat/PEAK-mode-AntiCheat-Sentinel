using System;

namespace PeakSentinel.Reporting
{
    public sealed class Repair
    {
        private readonly Action _apply;

        public Repair(string action, Action apply)
        {
            Action = action;
            _apply = apply;
        }

        public string Action { get; }

        public void Apply()
        {
            _apply();
        }
    }
}