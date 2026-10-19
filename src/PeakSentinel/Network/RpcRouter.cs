using System.Collections.Generic;
using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class RpcRouter
    {
        private const byte ViewIdKey = 0;
        private const byte MethodNameKey = 3;
        private const byte ArgumentsKey = 4;
        private const byte MethodIndexKey = 5;

        private readonly List<IRpcAudit> _audits;
        private readonly DebugTrace _trace;

        public RpcRouter(DebugTrace trace, params IRpcAudit[] audits)
        {
            _trace = trace;
            _audits = new List<IRpcAudit>(audits);
        }

        public bool Inspect(Hashtable rpc, PhotonPlayer sender)
        {
            if (rpc == null || !(rpc[ViewIdKey] is int viewId))
                return true;

            string method = MethodOf(rpc);

            if (method == null)
                return true;

            RpcCall call = new RpcCall(method, sender, PhotonView.Find(viewId), rpc[ArgumentsKey] as object[]);

            if (_trace.TracesAllRpcs || _audits.Exists(audit => audit.Watches(method)))
                _trace.Rpc(method, sender, viewId, call.Owner, call.Arguments);

            bool allowed = true;

            foreach (IRpcAudit audit in _audits)
            {
                if (audit.Watches(method))
                    allowed &= audit.Inspect(call);
            }

            return allowed;
        }

        private string MethodOf(Hashtable rpc)
        {
            if (rpc[MethodIndexKey] is byte index)
                return index < PhotonNetwork.PhotonServerSettings.RpcList.Count ? PhotonNetwork.PhotonServerSettings.RpcList[index] : null;

            return rpc[MethodNameKey] as string;
        }
    }
}