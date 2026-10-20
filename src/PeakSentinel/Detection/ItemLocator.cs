using System;
using HarmonyLib;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class ItemLocator
    {
        private const string HookTargetField = "targetPlayer";
        private const string HookPullingField = "isPulling";

        public bool Holds<T>(Character character) where T : Component
        {
            Player player = character == null ? null : character.player;
            return player != null && AnySlot(player, item => item.TryGetComponent(out T _));
        }

        public bool Carries(PhotonPlayer owner, ushort itemId)
        {
            return PlayerHandler.TryGetPlayer(owner.ActorNumber, out Player player) && AnySlot(player, item => item.itemID == itemId);
        }

        public bool LooseItemNear(PhotonPlayer owner, Vector3 point, float reach, GameObject except = null)
        {
            return AnyItemNear(owner, point, reach, item => item.gameObject != except && item.itemState == ItemState.Ground);
        }

        public bool OwnedNear<T>(PhotonPlayer owner, Vector3 point, float reach) where T : Component
        {
            return AnyItemNear(owner, point, reach, item => item.TryGetComponent(out T _));
        }

        public bool PullsWithRescueHook(PhotonPlayer owner, Character victim)
        {
            foreach (Item item in Item.ALL_ITEMS)
            {
                if (item == null || item.photonView == null || item.photonView.OwnerActorNr != owner.ActorNumber || !item.TryGetComponent(out RescueHook hook))
                    continue;

                Traverse state = Traverse.Create(hook);

                if (state.Field<bool>(HookPullingField).Value && state.Field<Character>(HookTargetField).Value == victim)
                    return true;
            }

            return false;
        }

        private bool AnyItemNear(PhotonPlayer owner, Vector3 point, float reach, Func<Item, bool> match)
        {
            foreach (Item item in Item.ALL_ITEMS)
            {
                if (item == null || item.photonView == null || item.photonView.OwnerActorNr != owner.ActorNumber || !match(item))
                    continue;

                if ((item.transform.position - point).sqrMagnitude <= reach * reach)
                    return true;
            }

            return false;
        }

        private bool AnySlot(Player player, Func<Item, bool> match)
        {
            foreach (ItemSlot slot in player.itemSlots)
            {
                if (Matches(slot, match))
                    return true;
            }

            return Matches(player.tempFullSlot, match) || Matches(player.backpackSlot, match);
        }

        private bool Matches(ItemSlot slot, Func<Item, bool> match)
        {
            return slot != null && !slot.IsEmpty() && match(slot.prefab);
        }
    }
}