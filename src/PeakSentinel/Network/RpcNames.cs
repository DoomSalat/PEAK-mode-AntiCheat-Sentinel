namespace PeakSentinel.Network
{
    public static class RpcNames // Static: vanilla RPC method names shared by the audits and protections
    {
        public const string SpawnItemInHand = "RPC_SpawnItemInHandMaster";
        public const string RequestPickup = "RequestPickup";
        public const string RequestFakeItemPickup = "RPC_RequestFakeItemPickup";
        public const string PickupAccepted = "OnPickupAccepted";
        public const string SyncInventory = "SyncInventoryRPC";
        public const string SetInventory = "RPC_SetInventory";
        public const string RemoveFromSlot = "RPCRemoveItemFromSlot";
        public const string InitHoldingItem = "RPC_InitHoldingItem";
        public const string EquipSlot = "EquipSlotRpc";
        public const string Clone = "RPC_Clone";
        public const string Die = "RPCA_Die";
        public const string SetDead = "RPCA_SetDead";
        public const string PassOut = "RPCA_PassOut";
        public const string Zombify = "RPCA_Zombify";
        public const string Petrify = "RPC_PetrifyInstantly";
        public const string Warp = "WarpPlayerRPC";
        public const string ForceAtPosition = "RPCA_AddForceAtPosition";
        public const string ForceToBodyPart = "RPCA_AddForceToBodyPart";
        public const string Fall = "RPCA_Fall";
        public const string UnFall = "RPCA_UnFall";
        public const string Kick = "RPCA_Kick";
        public const string TriggerHelicopter = "TriggerHelicopter";
        public const string TriggerBanana = "RPCA_TriggerBanana";
        public const string Consume = "Consume";
        public const string SyncAfflictions = "SyncAfflictionsRPC";
    }
}