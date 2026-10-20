using System.Collections.Generic;
using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class RpcRouter
    {

        private readonly List<IRpcAudit> _audits;
        private readonly DebugTrace _trace;
        private readonly StateLedger _ledger;

        public RpcRouter(DebugTrace trace, StateLedger ledger, params IRpcAudit[] audits)
        {
            _ledger = ledger;
            _trace = trace;
            _audits = new List<IRpcAudit>(audits);
        }

        public static string MethodOf(Hashtable rpc) // Static: pure lookup in the PUN RPC list, shared with MutedState
        {
            if (rpc[PunCodes.MethodIndexKey] is byte index)
                return index < PhotonNetwork.PhotonServerSettings.RpcList.Count ? PhotonNetwork.PhotonServerSettings.RpcList[index] : null;

            return rpc[PunCodes.MethodNameKey] as string;
        }

        public bool Inspect(Hashtable rpc, PhotonPlayer sender)
        {
            if (rpc == null || !(rpc[PunCodes.ViewIdKey] is int viewId))
                return true;

            string method = MethodOf(rpc);

            if (method == null)
                return true;

            _ledger.Note(sender, rpc);
            RpcCall call = new RpcCall(method, sender, PhotonView.Find(viewId), rpc[PunCodes.ArgumentsKey] as object[]);

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
    }
}