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
        private PlayerKicker _kicker;
        private int _master;
        private PhotonPlayer _thiefToKick;

        public void Initialize(SuspicionReport report, OtherSettings settings, PlayerKicker kicker)
        {
            _report = report;
            _settings = settings;
            _kicker = kicker;
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
            if (_thiefToKick != null && PhotonNetwork.IsMasterClient)
            {
                _report.Kick(_thiefToKick, FindingCategory.Cheats, Tag, "took the host from you");
                _kicker.Kick(_thiefToKick);
                _thiefToKick = null;
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
            bool reclaim = mine && _settings.ReclaimHost.Value && !_report.Spares(thief);
            string victim = mine ? "you" : PlayerNames.Name(PhotonNetwork.CurrentRoom.GetPlayer(previous));

            if (!reclaim)
            {
                _report.Refuse(thief, FindingCategory.Cheats, Tag, $"took the host from {victim}", false);
                return;
            }

            _report.Recover(thief, FindingCategory.Cheats, Tag, $"took the host from {victim}, taken back");
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);

            if (_settings.KickHostThieves.Value)
                _thiefToKick = thief;
        }
    }
}
