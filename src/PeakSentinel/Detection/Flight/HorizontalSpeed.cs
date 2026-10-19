using System.Collections.Generic;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class HorizontalSpeed
    {
        private const float TeleportStep = 20f;
        private const float Slack = 0.3f;

        private readonly Dictionary<int, List<(float Time, Vector3 Position)>> _paths = new Dictionary<int, List<(float Time, Vector3 Position)>>();

        public float Measure(int actor, float now, Vector3 position, float window)
        {
            if (!_paths.TryGetValue(actor, out List<(float Time, Vector3 Position)> path))
            {
                path = new List<(float Time, Vector3 Position)>();
                _paths[actor] = path;
            }

            if (path.Count > 0 && Flat(position - path[path.Count - 1].Position) > TeleportStep)
                path.Clear();

            path.Add((now, position));

            while (path.Count > 0 && now - path[0].Time > window + Slack)
                path.RemoveAt(0);

            (float Time, Vector3 Position) start = path[0];

            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (now - path[i].Time >= window)
                {
                    start = path[i];
                    break;
                }
            }

            float elapsed = now - start.Time;
            return elapsed < window * 0.5f ? 0f : Flat(position - start.Position) / elapsed;
        }

        public void Forget(int actor)
        {
            _paths.Remove(actor);
        }

        private float Flat(Vector3 offset)
        {
            return new Vector2(offset.x, offset.z).magnitude;
        }
    }
}
