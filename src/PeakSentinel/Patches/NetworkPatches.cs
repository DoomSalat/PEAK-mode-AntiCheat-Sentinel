using ExitGames.Client.Photon;
using HarmonyLib;
using PeakSentinel.Network;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;
using PunInstantiateParameters = Photon.Pun.InstantiateParameters;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class NetworkPatches // Static: Harmony patch class
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.OnEvent))]
        public static bool GateEvent(EventData photonEvent)
        {
            return SentinelHooks.Gate == null || SentinelHooks.Gate.Allow(photonEvent);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.ExecuteRpc))]
        public static bool InspectRpc(Hashtable rpcData, PhotonPlayer sender)
        {
            return SentinelHooks.Router == null || SentinelHooks.Router.Inspect(rpcData, sender);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonView), nameof(PhotonView.RPC), new[] { typeof(string), typeof(RpcTarget), typeof(object[]) })]
        public static void NoteOwnRpc(string methodName)
        {
            SentinelHooks.Hands?.NoteOutgoing(methodName);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.NetworkInstantiate), new[] { typeof(Hashtable), typeof(PhotonPlayer) })]
        public static bool FilterIncomingInstantiate(Hashtable networkEvent, PhotonPlayer creator, ref GameObject __result)
        {
            if (SentinelHooks.Instantiate == null || networkEvent == null || SentinelHooks.Instantiate.AllowCreate(creator, networkEvent[PunCodes.PrefabNameKey] as string, networkEvent[PunCodes.SpawnTimestampKey] is int sentAt ? sentAt : PhotonNetwork.ServerTimestamp, networkEvent[PunCodes.InstantiationIdKey] is int viewId ? viewId : 0, networkEvent[PunCodes.SpawnDataKey] as object[]))
                return true;

            __result = null;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.NetworkInstantiate), new[] { typeof(PunInstantiateParameters), typeof(bool), typeof(bool) })]
        public static void InspectInstantiate(PunInstantiateParameters parameters, bool instantiateEvent, GameObject __result)
        {
            SentinelHooks.Instantiate?.Inspect(parameters.creator, __result, instantiateEvent, parameters.timestamp);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterSyncer), nameof(CharacterSyncer.OnDataReceived))]
        public static void RevealHidden(CharacterSyncer __instance, ref CharacterSyncData data)
        {
            SentinelHooks.Invisibility?.Reveal(__instance.m_character, ref data);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerConnectionLog), "Awake")]
        public static void RememberFeed(PlayerConnectionLog __instance)
        {
            if (SentinelHooks.Report != null)
                SentinelHooks.Report.Feed = __instance;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GUIManager), "UpdateWindowStatus")]
        public static void FreeCursorForWindow(GUIManager __instance)
        {
            if (SentinelHooks.Window == null || !SentinelHooks.Window.IsOpen)
                return;

            __instance.windowShowingCursor = true;
            __instance.windowBlockingInput = true;
        }
    }
}
