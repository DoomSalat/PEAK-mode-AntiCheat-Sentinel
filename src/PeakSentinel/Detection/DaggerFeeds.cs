using System.Collections.Generic;
using Peak;
using PeakSentinel.Network;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class DaggerFeeds : IRpcAudit
    {
        private const float FeedSeconds = 5f;

        private readonly Dictionary<int, float> _stabbedAt = new Dictionary<int, float>();

        public bool Watches(string method)
        {
            return method == RpcNames.StartFeed;
        }

        public bool Inspect(RpcCall call)
        {
            PhotonView receiver = call.ViewAt(1);

            if (receiver != null && call.View != null && call.View.TryGetComponent(out Action_SacrificeFriend _))
                _stabbedAt[receiver.ViewID] = Time.time;

            return true;
        }

        public bool StabbedRecently(Character character)
        {
            return character != null && _stabbedAt.TryGetValue(character.photonView.ViewID, out float stabbed) && Time.time - stabbed < FeedSeconds;
        }
    }
}
