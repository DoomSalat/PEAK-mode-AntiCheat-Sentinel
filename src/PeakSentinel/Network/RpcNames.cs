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
        public const string ReviveAtPosition = "RPCA_ReviveAtPosition";
        public const string UnPassOut = "RPCA_UnPassOut";
        public const string LightCampfire = "Light_Rpc";
        public const string ExtinguishCampfire = "Extinguish_Rpc";
        public const string StartFog = "StartMovingRPC";
        public const string SyncFog = "RPCA_SyncFog";
        public const string Jump = "JumpRpc";
        public const string Crouch = "RPCA_SetCrouch";
        public const string StopClimbing = "StopClimbingRpc";
        public const string StopRopeClimbing = "StopRopeClimbingRpc";
        public const string DropFromSlot = "DropItemFromSlotRPC";
        public const string Emote = "RPCA_PlayRemove";
        public const string ShownDead = "CharacterDied";
        public const string ShownPassedOut = "CharacterPassedOut";
        public const string SyncStatuses = "SyncStatusesRPC";
        public const string ApplyStatuses = "RPC_ApplyStatusesFromFloatArray";
        public const string LightFlare = "SetFlareLitRPC";
        public const string Build = "CreatePrefabRPC";
        public const string SyncAfflictions = "SyncAfflictionsRPC";
    }
}