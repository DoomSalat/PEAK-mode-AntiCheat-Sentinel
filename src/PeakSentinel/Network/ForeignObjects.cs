using ExitGames.Client.Photon;
using PeakSentinel.Game;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class ForeignObjects
    {
        public void RemoveLocal(GameObject target)
        {
            GameInternals.RemoveLocal(target);
        }

        public void RemoveForEveryone(GameObject target, PhotonPlayer creator)
        {
            int viewId = target.GetComponent<PhotonView>().ViewID;
            RemoveLocal(target);
            RemoveForEveryone(viewId, creator);
        }

        public void RemoveForEveryone(int viewId, PhotonPlayer creator)
        {
            PhotonNetwork.NetworkingClient.OpRaiseEvent(PunCodes.RemoveEvent, new Hashtable { [PunCodes.ViewIdKey] = viewId }, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            PhotonNetwork.NetworkingClient.OpRaiseEvent(PunCodes.SpawnEvent, new Hashtable { [PunCodes.InstantiationIdKey] = viewId }, new RaiseEventOptions { CachingOption = EventCaching.RemoveFromRoomCache, TargetActors = new[] { creator.ActorNumber } }, SendOptions.SendReliable);
        }
    }
}
