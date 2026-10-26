using PeakSentinel.Network;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class PlayerTeleporter
    {
        private readonly SuspicionReport _report;

        public PlayerTeleporter(SuspicionReport report)
        {
            _report = report;
        }

        public void BringToMe(PhotonPlayer player, float lift)
        {
            Character character = PlayerHandler.GetPlayerCharacter(player);
            Character me = Character.localCharacter;

            if (character == null || me == null || !PhotonNetwork.IsMasterClient)
                return;

            character.photonView.RPC(RpcNames.Warp, RpcTarget.All, me.Center + Vector3.up * lift, true);
            _report.LogAction(player, "brought to you with Bring to me (rescue from lag or from falling through the map)");
        }
    }
}
