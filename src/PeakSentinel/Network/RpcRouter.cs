using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class RpcRouter
    {
        private const string AuditErrorLevel = "AUDIT ERROR";

        private readonly List<IRpcAudit> _audits;
        private readonly DebugTrace _trace;
        private readonly StateLedger _ledger;
        private readonly TrustList _trust;
        private readonly Func<bool> _checksMe;

        public RpcRouter(DebugTrace trace, StateLedger ledger, TrustList trust, Func<bool> checksMe, params IRpcAudit[] audits)
        {
            _checksMe = checksMe;
            _trust = trust;
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

            RpcCall call = new RpcCall(method, sender, PhotonView.Find(viewId), rpc[PunCodes.ArgumentsKey] as object[]);

            if (_trace.TracesAllRpcs || _audits.Exists(audit => audit.Watches(method)))
                _trace.Rpc(method, sender, viewId, call.Owner, call.Arguments);

            bool allowed = true;

            foreach (IRpcAudit audit in _audits)
            {
                if (audit.Watches(method))
                    allowed &= InspectSafely(audit, call);
            }

            allowed |= _trust.Trusts(sender);

            if (allowed)
                _ledger.Note(sender, rpc);

            return allowed;
        }

        public void InspectOwn(PhotonView view, string method, object[] arguments)
        {
            if (view == null || method == null || !PhotonNetwork.InRoom || !_checksMe())
                return;

            RpcCall call = new RpcCall(method, PhotonNetwork.LocalPlayer, view, arguments, true);

            foreach (IRpcAudit audit in _audits)
            {
                if (audit.Watches(method))
                    InspectSafely(audit, call);
            }
        }

        private bool InspectSafely(IRpcAudit audit, RpcCall call)
        {
            try
            {
                return audit.Inspect(call);
            }
            catch (Exception exception)
            {
                _trace.Finding(AuditErrorLevel, call.Sender, $"{audit.GetType().Name} failed on {call.Method} ({exception.GetType().Name}), the call was dropped");
                return false;
            }
        }
    }
}