using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class HostOnlyPrefabs
    {
        private readonly Dictionary<string, bool> _hostOnly = new Dictionary<string, bool>();

        public bool IsHostOnly(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return false;

            if (_hostOnly.TryGetValue(prefabPath, out bool hostOnly))
                return hostOnly;

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            hostOnly = prefab != null && (IsFreeMob(prefab) || prefab.GetComponentInChildren<TumbleWeed>(true) != null || prefab.GetComponentInChildren<Tornado>(true) != null || prefab.GetComponentInChildren<BeeSwarm>(true) != null || prefab.GetComponentInChildren<Peak.GhostBall>(true) != null);
            _hostOnly[prefabPath] = hostOnly;
            return hostOnly;
        }

        private bool IsFreeMob(GameObject prefab)
        {
            return prefab.GetComponentInChildren<Mob>(true) != null && prefab.GetComponentInChildren<Item>(true) == null; // A mob that is not an item (beetle...) only comes from the host's spawners; carried mobs like the frog are items
        }
    }
}
