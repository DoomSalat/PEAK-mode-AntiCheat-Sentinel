using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Game
{
    public static class GameInternals // Static: the one place that reaches into private game and PUN members, fix here after a PEAK update
    {
        private static readonly FieldInfo CampfireList = AccessTools.Field(typeof(Campfire), "ALL_CAMPFIRES"); // Static: cached reflection handle
        private static readonly AccessTools.FieldRef<RescueHook, bool> HookPulling = AccessTools.FieldRefAccess<RescueHook, bool>("isPulling"); // Static: cached field accessor
        private static readonly AccessTools.FieldRef<RescueHook, Character> HookTarget = AccessTools.FieldRefAccess<RescueHook, Character>("targetPlayer"); // Static: cached field accessor
        private static readonly AccessTools.FieldRef<FrogTongue, Character> TongueTarget = AccessTools.FieldRefAccess<FrogTongue, Character>("_targetCharacter"); // Static: cached field accessor
        private static readonly AccessTools.FieldRef<Skelleton, Character> SkeletonOwner = AccessTools.FieldRefAccess<Skelleton, Character>("spawnedFromCharacter"); // Static: cached field accessor
        private static readonly AccessTools.FieldRef<RunManager, bool> TimerActive = AccessTools.FieldRefAccess<RunManager, bool>("timerActive"); // Static: cached field accessor
        private static readonly MethodInfo RemoveInstantiated = AccessTools.Method(typeof(PhotonNetwork), "RemoveInstantiatedGO", new[] { typeof(GameObject), typeof(bool) }); // Static: cached reflection handle

        public static IEnumerable<Campfire> Campfires()
        {
            return (List<Campfire>)CampfireList.GetValue(null);
        }

        public static bool IsPulling(RescueHook hook, Character victim)
        {
            return HookPulling(hook) && HookTarget(hook) == victim;
        }

        public static Character TargetOf(FrogTongue tongue)
        {
            return TongueTarget(tongue);
        }

        public static Character OwnerOf(Skelleton skeleton)
        {
            return SkeletonOwner(skeleton);
        }

        public static bool IsTimerActive(RunManager manager)
        {
            return TimerActive(manager);
        }

        public static void RemoveLocal(GameObject target)
        {
            RemoveInstantiated.Invoke(null, new object[] { target, true });
        }
    }
}