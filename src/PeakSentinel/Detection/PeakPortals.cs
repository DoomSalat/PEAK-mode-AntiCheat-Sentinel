using System.Collections.Generic;
using Peak;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class PeakPortals
    {
        private readonly HashSet<PeakGatePortal> _portals = new HashSet<PeakGatePortal>();

        public void Note(PeakGatePortal portal)
        {
            _portals.Add(portal);
        }

        public bool Near(Vector3 position, float reach)
        {
            _portals.RemoveWhere(portal => portal == null);

            foreach (PeakGatePortal portal in _portals)
            {
                if ((portal.transform.position - position).sqrMagnitude <= reach * reach)
                    return true;
            }

            return false;
        }
    }
}
