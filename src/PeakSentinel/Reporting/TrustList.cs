using System.Collections.Generic;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public sealed class TrustList
    {
        private readonly HashSet<string> _trusted = new HashSet<string>();

        public int Version { get; private set; }

        public bool Trusts(PhotonPlayer player)
        {
            return player != null && !string.IsNullOrEmpty(player.UserId) && _trusted.Contains(player.UserId);
        }

        public void Trust(PhotonPlayer player)
        {
            if (player != null && !string.IsNullOrEmpty(player.UserId) && _trusted.Add(player.UserId))
                Version++;
        }

        public void Distrust(PhotonPlayer player)
        {
            if (player != null && !string.IsNullOrEmpty(player.UserId) && _trusted.Remove(player.UserId))
                Version++;
        }
    }
}
