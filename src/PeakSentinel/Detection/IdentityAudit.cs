using System;
using System.Collections.Generic;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using Steamworks;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class IdentityAudit : MonoBehaviour
    {
        private const float ScanSeconds = 5f;
        private const string UserIdProperty = "UserID";
        private const string UnknownPersona = "[unknown]";
        private const string SpoofTag = "spoofs a name or Steam ID";

        private readonly Dictionary<int, string> _firstNames = new Dictionary<int, string>();
        private readonly HashSet<(int Actor, string Kind)> _reported = new HashSet<(int Actor, string Kind)>();
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
            if (!_detection.NameSpoofing.Value || !PhotonNetwork.InRoom || Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            if (_room != PhotonNetwork.CurrentRoom.Name)
            {
                _room = PhotonNetwork.CurrentRoom.Name;
                _firstNames.Clear();
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
            CheckRename(player);
            CheckSteamName(player);
            CheckSteamId(player);
        }

        private void CheckRename(PhotonPlayer player)
        {
            if (!_firstNames.TryGetValue(player.ActorNumber, out string first))
            {
                _firstNames[player.ActorNumber] = player.NickName;
                return;
            }

            if (first != player.NickName && _reported.Add((player.ActorNumber, player.NickName)))
                _report.Refuse(player, FindingCategory.Cheats, SpoofTag, $"changed their name mid-game from '{first}' to '{player.NickName}'", false);
        }

        private void CheckSteamName(PhotonPlayer player)
        {
            if (!TrySteamId(player, out ulong id))
                return;

            CSteamID steamId = new CSteamID(id);

            if (SteamFriends.RequestUserInformation(steamId, true))
                return;

            string persona = SteamFriends.GetFriendPersonaName(steamId);

            if (string.IsNullOrEmpty(persona) || persona == UnknownPersona || string.Equals(persona, player.NickName, StringComparison.OrdinalIgnoreCase) || !_reported.Add((player.ActorNumber, persona)))
                return;

            _report.Tag(player, SpoofTag);
            _report.Suspect(player, FindingCategory.Cheats, $"the in-game name differs from the Steam name '{persona}' of their Steam ID (name spoofing or a Steam rename)");
        }

        private void CheckSteamId(PhotonPlayer player)
        {
            string id = player.CustomProperties[UserIdProperty] as string;

            if (string.IsNullOrEmpty(id))
                return;

            string local = SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID.ToString() : null;

            if (id == local && _reported.Add((player.ActorNumber, id)))
            {
                _report.Refuse(player, FindingCategory.Cheats, SpoofTag, "uses your Steam ID (impersonation)", false);
                return;
            }

            foreach (PhotonPlayer other in PhotonNetwork.PlayerList)
            {
                if (other.ActorNumber != player.ActorNumber && !other.IsLocal && other.CustomProperties[UserIdProperty] as string == id && _reported.Add((player.ActorNumber, id)))
                    _report.Refuse(player, FindingCategory.Cheats, SpoofTag, $"uses the same Steam ID as {PlayerNames.Name(other)} (impersonation)", false);
            }
        }

        private bool TrySteamId(PhotonPlayer player, out ulong id)
        {
            id = 0;
            return SteamAPI.IsSteamRunning() && player.CustomProperties[UserIdProperty] is string text && ulong.TryParse(text, out id);
        }
    }
}