using System.Collections.Generic;
using PeakSentinel.Network;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Protection
{
    public sealed class StatusRestorer : MonoBehaviour
    {
        private const float CheckSeconds = 1f;
        private const float SettleSeconds = 3f;
        private const float Tolerance = 0.025f;

        private readonly Dictionary<int, (float[] Before, float Until, float CheckAt)> _guards = new Dictionary<int, (float[] Before, float Until, float CheckAt)>();
        private readonly List<int> _viewIds = new List<int>();

        private void Update()
        {
            if (_guards.Count == 0)
                return;

            _viewIds.Clear();
            _viewIds.AddRange(_guards.Keys);

            foreach (int viewId in _viewIds)
            {
                PhotonView view = PhotonView.Find(viewId);
                (float[] Before, float Until, float CheckAt) guard = _guards[viewId];

                if (view == null || !PhotonNetwork.IsMasterClient || Time.unscaledTime > guard.Until)
                {
                    _guards.Remove(viewId);
                    continue;
                }

                if (Time.unscaledTime < guard.CheckAt)
                    continue;

                bool restored = Restore(view.GetComponent<Character>(), guard.Before);
                _guards[viewId] = (guard.Before, guard.Until, Time.unscaledTime + (restored ? SettleSeconds : CheckSeconds));
            }
        }

        public void Guard(Character victim, float seconds)
        {
            int viewId = victim.photonView.ViewID;
            float until = Time.unscaledTime + seconds;

            if (_guards.TryGetValue(viewId, out (float[] Before, float Until, float CheckAt) guard))
            {
                _guards[viewId] = (guard.Before, until, guard.CheckAt);
                return;
            }

            _guards[viewId] = ((float[])victim.refs.afflictions.currentStatuses.Clone(), until, Time.unscaledTime + CheckSeconds);
        }

        private bool Restore(Character victim, float[] before)
        {
            float[] now = victim.refs.afflictions.currentStatuses;
            float[] undo = new float[now.Length];
            bool rose = false;

            for (int i = 0; i < now.Length && i < before.Length; i++)
            {
                float rise = now[i] - before[i];

                if (rise > Tolerance)
                {
                    undo[i] = -rise;
                    rose = true;
                }
            }

            if (rose)
                victim.photonView.RPC(RpcNames.ApplyStatuses, RpcTarget.All, undo);

            return rose;
        }
    }
}
