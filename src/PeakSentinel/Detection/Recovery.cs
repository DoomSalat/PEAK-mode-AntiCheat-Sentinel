using System;
using PeakSentinel.Reporting;
using Photon.Pun;

namespace PeakSentinel.Detection
{
    public sealed class Recovery
    {
        private readonly Action<RpcTarget> _apply;

        public Recovery(string action, Action<RpcTarget> apply, bool mirrorsOnMe)
        {
            Action = action;
            MirrorsOnMe = mirrorsOnMe;
            _apply = apply;
        }

        public string Action { get; }
        public bool MirrorsOnMe { get; }

        public void Apply(RpcTarget target)
        {
            _apply(target);
        }

        public Repair AsRepair()
        {
            return new Repair(Action, () => _apply(RpcTarget.All));
        }
    }
}
