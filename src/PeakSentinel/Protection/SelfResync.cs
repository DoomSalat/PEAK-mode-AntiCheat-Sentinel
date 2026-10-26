using PeakSentinel.Network;
using Photon.Pun;

namespace PeakSentinel.Protection
{
    public sealed class SelfResync
    {
        private const int CallKey = -1;
        private const float CoalesceSeconds = 0.5f;

        private readonly DelayedCalls _later;

        public SelfResync(DelayedCalls later)
        {
            _later = later;
        }

        public void Schedule()
        {
            _later.After(CallKey, CoalesceSeconds, Push); // A forced-action spam keeps postponing it, so others get one push after the spam instead of one per RPC
        }

        public void Push()
        {
            Character me = Character.localCharacter;

            if (me == null || !PhotonNetwork.InRoom)
                return;

            if (me.data.dead)
            {
                me.photonView.RPC(RpcNames.SetDead, RpcTarget.Others);
            }
            else
            {
                if (me.data.fallSeconds <= 0f)
                    me.photonView.RPC(RpcNames.UnFall, RpcTarget.Others);

                if (!me.data.passedOut)
                    me.photonView.RPC(RpcNames.UnPassOut, RpcTarget.Others);
            }

            me.photonView.RPC(RpcNames.Crouch, RpcTarget.Others, me.data.isCrouching);
            me.photonView.RPC(RpcNames.SyncPetrify, RpcTarget.Others, me.data.petrifyAmount);
            me.refs.afflictions.PushStatuses();
            me.refs.afflictions.PushAfflictions();
        }
    }
}
