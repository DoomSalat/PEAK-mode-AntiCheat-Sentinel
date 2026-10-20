using HarmonyLib;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class LegitForces
    {
        private const float ThrownReach = 6f;
        private const string HookTargetField = "targetPlayer";
        private const string HookPullingField = "isPulling";
        private const string TongueTargetField = "_targetCharacter";

        private readonly ItemLocator _locator;

        public LegitForces(ItemLocator locator)
        {
            _locator = locator;
        }

        public bool Explains(PhotonPlayer sender, Character victim)
        {
            return victim.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.FlyTrap) > 0f || _locator.LooseItemNear(sender, victim.Center, ThrownReach) || PullsVictim(sender, victim);
        }

        private bool PullsVictim(PhotonPlayer sender, Character victim)
        {
            foreach (Item item in Item.ALL_ITEMS)
            {
                if (item == null || item.photonView == null || item.photonView.OwnerActorNr != sender.ActorNumber)
                    continue;

                if (item.TryGetComponent(out RescueHook hook) && Traverse.Create(hook).Field<bool>(HookPullingField).Value && Traverse.Create(hook).Field<Character>(HookTargetField).Value == victim)
                    return true;

                FrogTongue tongue = item.GetComponentInChildren<FrogTongue>();

                if (tongue != null && Traverse.Create(tongue).Field<Character>(TongueTargetField).Value == victim)
                    return true;
            }

            return false;
        }
    }
}