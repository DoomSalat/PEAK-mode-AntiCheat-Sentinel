namespace PeakSentinel.Network
{
    public static class PunCodes // Static: fixed Photon PUN event codes and payload keys shared by the network code
    {
        public const byte RpcEvent = 200;
        public const byte SerializeEvent = 201;
        public const byte SpawnEvent = 202;
        public const byte RemoveEvent = 204;
        public const byte SerializeReliableEvent = 206;
        public const byte OwnershipRequestEvent = 209;
        public const byte OwnershipTransferEvent = 210;
        public const byte OwnershipUpdateEvent = 212;
        public const byte FirstServerEvent = 220;
        public const byte ViewIdKey = 0;
        public const byte PrefabNameKey = 0;
        public const byte RpcTimestampKey = 2;
        public const byte MethodNameKey = 3;
        public const byte ArgumentsKey = 4;
        public const byte MethodIndexKey = 5;
        public const byte InstantiationIdKey = 7;
        public const byte SpawnDataKey = 5;
        public const byte SpawnTimestampKey = 6;
        public const int CachedAgeMilliseconds = 3000;
    }
}