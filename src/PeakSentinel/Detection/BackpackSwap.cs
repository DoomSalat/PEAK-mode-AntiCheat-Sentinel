using System.Collections.Generic;
using PeakSentinel.Network;
using UnityEngine;
using Zorro.Core.Serizalization;

namespace PeakSentinel.Detection
{
    public sealed class BackpackSwap
    {
        private const float SwapReach = 4f;
        private const ushort EmptySlot = ushort.MaxValue;
        private const float ReleaseSeconds = 3f;

        private readonly List<(ushort Backpack, float Time)> _released = new List<(ushort Backpack, float Time)>();

        public bool IsSwap(RpcCall call)
        {
            Player owner = call.Component<Player>();
            Character ownerCharacter = owner == null ? null : owner.character;
            Character senderCharacter = PlayerHandler.GetPlayerCharacter(call.Sender);

            if (ownerCharacter == null || senderCharacter == null || !Near(ownerCharacter, senderCharacter) || !(call.Argument(0) is byte[] data))
                return false;

            InventorySyncData inventory = IBinarySerializable.GetFromManagedArray<InventorySyncData>(data);

            if (!SameSlots(owner, inventory))
                return false;

            ushort backpack = inventory.backpackSlot.ItemID;
            ushort current = BackpackOf(owner);

            if (backpack == current)
                return true;

            if (backpack == EmptySlot)
            {
                _released.Add((current, Time.time));
                return true;
            }

            return current == EmptySlot && TakeReleased(backpack);
        }

        private bool SameSlots(Player owner, InventorySyncData inventory)
        {
            if (inventory.slots == null || inventory.slots.Length != owner.itemSlots.Length || inventory.tempSlot.ItemID != IdOf(owner.tempFullSlot))
                return false;

            for (int i = 0; i < inventory.slots.Length; i++)
            {
                if (inventory.slots[i].ItemID != IdOf(owner.itemSlots[i]))
                    return false;
            }

            return true;
        }

        private bool TakeReleased(ushort backpack)
        {
            _released.RemoveAll(released => Time.time - released.Time > ReleaseSeconds);
            int index = _released.FindIndex(released => released.Backpack == backpack);

            if (index < 0)
                return false;

            _released.RemoveAt(index);
            return true;
        }

        private bool Near(Character first, Character second)
        {
            return (first.Center - second.Center).sqrMagnitude <= SwapReach * SwapReach;
        }

        private ushort BackpackOf(Player player)
        {
            return IdOf(player.backpackSlot);
        }

        private ushort IdOf(ItemSlot slot)
        {
            return slot == null || slot.IsEmpty() ? EmptySlot : slot.prefab.itemID;
        }
    }
}
