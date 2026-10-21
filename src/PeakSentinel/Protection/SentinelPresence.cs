using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class SentinelPresence : MonoBehaviour
    {
        private const string MarkerKey = "peaksentinel.version";
        private const float SettleSeconds = 3f;
        private const float FeedWaitSeconds = 20f;

        private SuspicionReport _report;
        private string _version;
        private string _room;
        private float _joinedAt;
        private int _checkedHost;

        public void Initialize(SuspicionReport report, string version)
        {
            _report = report;
            _version = version;
        }

        private void Update()
        {
            if (!PhotonNetwork.InRoom)
            {
                _room = null;
                return;
            }

            if (PhotonNetwork.CurrentRoom.Name != _room)
            {
                _room = PhotonNetwork.CurrentRoom.Name;
                _joinedAt = Time.unscaledTime;
                _checkedHost = 0;
                PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [MarkerKey] = _version });
            }

            PhotonPlayer host = PhotonNetwork.MasterClient;
            float waited = Time.unscaledTime - _joinedAt;

            if (host == null || host.ActorNumber == _checkedHost || waited < SettleSeconds || (_report.Feed == null && waited < FeedWaitSeconds))
                return;

            _checkedHost = host.ActorNumber;
            _report.Announce(host, FindingCategory.Other, Describe(host), Runs(host) ? FeedTone.Good : FeedTone.Suspect);
        }

        public bool Runs(PhotonPlayer player)
        {
            return player != null && (player.IsLocal || player.CustomProperties.ContainsKey(MarkerKey));
        }

        private string Describe(PhotonPlayer host)
        {
            if (host.IsLocal)
                return "you are the host: the lobby is protected by your Peak Sentinel";

            return host.CustomProperties.TryGetValue(MarkerKey, out object version) ? $"the host runs Peak Sentinel {version}: the lobby is protected" : "the host has no Peak Sentinel: the lobby is not protected";
        }
    }
}