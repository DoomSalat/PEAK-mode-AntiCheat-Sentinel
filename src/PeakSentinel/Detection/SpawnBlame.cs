using System.Collections.Generic;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class SpawnBlame
    {
        private const float BlameSeconds = 20f;

        private readonly Dictionary<int, float> _flaggedAt = new Dictionary<int, float>();

        public void Note(PhotonPlayer player)
        {
            if (player != null)
                _flaggedAt[player.ActorNumber] = Time.time;
        }

        public bool Blames(PhotonPlayer player)
        {
            return player != null && _flaggedAt.TryGetValue(player.ActorNumber, out float flagged) && Time.time - flagged < BlameSeconds;
        }
    }
}
