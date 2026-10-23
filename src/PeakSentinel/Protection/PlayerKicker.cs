using System.Collections.Generic;
using Peak.Network;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class PlayerKicker : MonoBehaviour
    {
        private const float ScanSeconds = 1f;
        private const float RetrySeconds = 5f;

        private readonly Dictionary<int, float> _kickedAt = new Dictionary<int, float>();
        private readonly List<int> _gone = new List<int>();
        private readonly List<int> _due = new List<int>();
        private float _nextScan;

        private void Update()
        {
            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            if (!PhotonNetwork.InRoom)
            {
                _kickedAt.Clear();
                return;
            }

            _gone.Clear();
            _due.Clear();

            foreach (KeyValuePair<int, float> kicked in _kickedAt)
            {
                if (PhotonNetwork.CurrentRoom.GetPlayer(kicked.Key) == null)
                    _gone.Add(kicked.Key);
                else if (Time.unscaledTime - kicked.Value >= RetrySeconds)
                    _due.Add(kicked.Key);
            }

            foreach (int actor in _gone)
                _kickedAt.Remove(actor);

            if (!PhotonNetwork.IsMasterClient)
                return;

            foreach (int actor in _due)
                Send(PhotonNetwork.CurrentRoom.GetPlayer(actor));
        }

        public bool IsKicking(PhotonPlayer player)
        {
            return _kickedAt.ContainsKey(player.ActorNumber);
        }

        public void Kick(PhotonPlayer player)
        {
            if (player == null || player.IsLocal || !PhotonNetwork.IsMasterClient)
                return;

            Send(player);
        }

        private void Send(PhotonPlayer player)
        {
            _kickedAt[player.ActorNumber] = Time.unscaledTime;

            if (PlayerHandler.TryGetPlayer(player.ActorNumber, out _))
                PlayerHandler.Kick(player.ActorNumber);
            else
                NetCode.Session.Kick(player.UserId); // The vanilla kick reads the Player object, which a player who just joined does not have yet
        }
    }
}
