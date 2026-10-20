using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class HostGuard : MonoBehaviour
    {
        private const string Tag = "steals the host";

        private SuspicionReport _report;
        private OtherSettings _settings;
        private int _master;
        private int _thiefToKick;

        public void Initialize(SuspicionReport report, OtherSettings settings)
        {
            _report = report;
            _settings = settings;
        }

        private void OnEnable()
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null)
                PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }

        private void Update()
        {
            if (_thiefToKick != 0 && PhotonNetwork.IsMasterClient)
            {
                PlayerHandler.Kick(_thiefToKick);
                _thiefToKick = 0;
            }

            _master = PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.ActorNumber : 0;
        }

        private void OnEvent(EventData data)
        {
            if (data.Code != EventCode.PropertiesChanged || !(data.Parameters[ParameterCode.Properties] is Hashtable properties) || !(properties[GamePropertyKey.MasterClientId] is int newMaster))
                return;

            int previous = _master;
            _master = newMaster;
            PhotonPlayer thief = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(data.Sender);

            if (thief == null || thief.IsLocal || data.Sender == previous || previous == 0 || PhotonNetwork.CurrentRoom.GetPlayer(previous) == null)
                return;

            bool mine = previous == PhotonNetwork.LocalPlayer.ActorNumber;
            bool reclaim = mine && _settings.ReclaimHost.Value;
            string victim = mine ? "you" : PlayerNames.Name(PhotonNetwork.CurrentRoom.GetPlayer(previous));
            _report.Refuse(thief, FindingCategory.Cheats, Tag, reclaim ? $"took the host from {victim}, taken back" : $"took the host from {victim}", reclaim);

            if (!reclaim)
                return;

            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);

            if (_settings.KickHostThieves.Value)
                _thiefToKick = thief.ActorNumber;
        }
    }
}
