using HarmonyLib;
using PeakSentinel.Game;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class CookingPatches // Static: Harmony patch class
    {
        private const string TriggerMethod = "TriggerBehaviour";

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CookingBehavior_RunActions), TriggerMethod)]
        public static void NoteEchoes(CookingBehavior_RunActions __instance)
        {
            if (SentinelHooks.Cooking != null)
                SentinelHooks.Cooking.Note(GameInternals.HolderOf(__instance), __instance.actionsToRun);
        }
    }
}
