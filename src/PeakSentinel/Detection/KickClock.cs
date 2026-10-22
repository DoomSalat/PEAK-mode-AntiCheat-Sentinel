using System.Collections.Generic;
using PeakSentinel.Network;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class KickClock : IRpcAudit
    {
        private const float KickSeconds = 2f;

        private readonly Dictionary<int, float> _kickedAt = new Dictionary<int, float>();

        public bool Watches(string method)
        {
            return method == RpcNames.Kick;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Sender != null)
                _kickedAt[call.Sender.ActorNumber] = Time.time;

            return true;
        }

        public bool JustKicked(PhotonPlayer sender)
        {
            return sender != null && _kickedAt.TryGetValue(sender.ActorNumber, out float kicked) && Time.time - kicked < KickSeconds;
        }
    }
}
