using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class CampfirePatches // Static: Harmony patch class
    {
        private const string CantLightKey = "CANTLIGHT";
        private const string MissingFormat = "\n{0} {1}m";

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Campfire), nameof(Campfire.EveryoneInRange), new[] { typeof(float) })]
        public static void IgnoreMutedPlayers(Campfire __instance, float range, ref bool __result)
        {
            if (!__result && SentinelHooks.Mutes != null)
                __result = Missing(__instance, range).Count == 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Campfire), nameof(Campfire.EveryoneInRange), new[] { typeof(string), typeof(float) }, new[] { ArgumentType.Out, ArgumentType.Normal })]
        public static void IgnoreMutedPlayersInPrompt(Campfire __instance, ref string printout, float range, ref bool __result)
        {
            if (__result || SentinelHooks.Mutes == null)
                return;

            List<Character> missing = Missing(__instance, range);
            __result = missing.Count == 0;
            printout = __result ? string.Empty : Prompt(__instance, missing);
        }

        private static List<Character> Missing(Campfire campfire, float range)
        {
            List<Character> near = campfire.PlayerCharactersInRadius(range);
            List<Character> missing = new List<Character>();

            foreach (Character character in PlayerHandler.GetAllPlayerCharacters())
            {
                if (!character.data.dead && !near.Contains(character) && !SentinelHooks.Mutes.IsMuted(character.photonView.Owner))
                    missing.Add(character);
            }

            return missing;
        }

        private static string Prompt(Campfire campfire, List<Character> missing)
        {
            StringBuilder text = new StringBuilder(LocalizedText.GetText(CantLightKey)).Append('\n');

            foreach (Character character in missing)
            {
                float distance = (campfire.transform.position - character.Center).magnitude;
                text.AppendFormat(MissingFormat, character.photonView.Owner.NickName, Mathf.RoundToInt(distance * CharacterStats.unitsToMeters));
            }

            return text.ToString();
        }
    }
}