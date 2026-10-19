using System.Collections.Generic;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class CheatModScan : MonoBehaviour
    {
        private const float ScanSeconds = 5f;

        private static readonly (string Key, string Tag)[] Markers = // Static: fixed property markers the cheat mods put on their users (same as PEAKER)
        {
            ("CherryUser", "Cherry cheat user"),
            ("CherryOwner", "Cherry cheat owner"),
            ("AtlUser", "Atlas cheat user"),
            ("AtlOwner", "Atlas cheat owner"),
        };

        private readonly HashSet<(int Actor, string Key)> _reported = new HashSet<(int Actor, string Key)>();
        private SuspicionReport _report;
        private DetectionSettings _detection;
        private string _room;
        private float _nextScan;

        public void Initialize(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        private void Update()
        {
            if (!_detection.CheatModMarkers.Value || Time.unscaledTime < _nextScan || !PhotonNetwork.InRoom)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;
            Scan();
        }

        private void Scan()
        {
            if (_room != PhotonNetwork.CurrentRoom.Name)
            {
                _room = PhotonNetwork.CurrentRoom.Name;
                _reported.Clear();
            }

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal)
                    Check(player);
            }
        }

        private void Check(PhotonPlayer player)
        {
            foreach ((string key, string tag) in Markers)
            {
                if (!player.CustomProperties.ContainsKey(key) || !_reported.Add((player.ActorNumber, key)))
                    continue;

                bool kick = _detection.KickCheatModUsers.Value && PhotonNetwork.IsMasterClient;
                _report.Refuse(player, FindingCategory.Cheats, tag, kick ? $"is a {tag}, kicked" : $"is a {tag}", kick);

                if (kick)
                {
                    PlayerHandler.Kick(player.ActorNumber);
                    return;
                }
            }
        }
    }
}
