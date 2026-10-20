using System.Reflection;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class ForeignObjects
    {
        private const string RemoveObjectMethod = "RemoveInstantiatedGO";

        private static readonly MethodInfo RemoveObject = AccessTools.Method(typeof(PhotonNetwork), RemoveObjectMethod, new[] { typeof(GameObject), typeof(bool) }); // Static: PUN's internal local removal, looked up once

        public void RemoveLocal(GameObject target)
        {
            RemoveObject.Invoke(null, new object[] { target, true });
        }

        public void RemoveForEveryone(GameObject target, PhotonPlayer creator)
        {
            int viewId = target.GetComponent<PhotonView>().ViewID;
            RemoveLocal(target);
            PhotonNetwork.NetworkingClient.OpRaiseEvent(PunCodes.RemoveEvent, new Hashtable { [PunCodes.ViewIdKey] = viewId }, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            PhotonNetwork.NetworkingClient.OpRaiseEvent(PunCodes.SpawnEvent, new Hashtable { [PunCodes.InstantiationIdKey] = viewId }, new RaiseEventOptions { CachingOption = EventCaching.RemoveFromRoomCache, TargetActors = new[] { creator.ActorNumber } }, SendOptions.SendReliable);
        }
    }
}
