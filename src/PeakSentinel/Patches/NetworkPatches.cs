using ExitGames.Client.Photon;
using HarmonyLib;
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
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.ExecuteRpc))]
        public static bool InspectRpc(Hashtable rpcData, PhotonPlayer sender)
        {
            return SentinelHooks.Rpc == null || SentinelHooks.Rpc.Inspect(rpcData, sender);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.NetworkInstantiate), new[] { typeof(PunInstantiateParameters), typeof(bool), typeof(bool) })]
        public static void InspectInstantiate(PunInstantiateParameters parameters, bool instantiateEvent, GameObject __result)
        {
            SentinelHooks.Instantiate?.Inspect(parameters.creator, __result, instantiateEvent);
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
