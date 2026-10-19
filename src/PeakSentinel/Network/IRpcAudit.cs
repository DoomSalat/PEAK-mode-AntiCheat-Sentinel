namespace PeakSentinel.Network
{
    public interface IRpcAudit
    {
        bool Watches(string method);

        bool Inspect(RpcCall call);
    }
}