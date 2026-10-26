using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public readonly struct RpcCall
    {
        public RpcCall(string method, PhotonPlayer sender, PhotonView view, object[] arguments, bool selfCheck = false)
        {
            SelfCheck = selfCheck;
            Method = method;
            Sender = sender;
            View = view;
            Arguments = arguments;
        }

        public string Method { get; }
        public PhotonPlayer Sender { get; }
        public PhotonView View { get; }
        public object[] Arguments { get; }
        public bool SelfCheck { get; }
        public PhotonPlayer Owner => View == null ? null : View.Owner;
        public bool FromHost => Sender != null && Sender.IsMasterClient;
        public bool FromLocal => !SelfCheck && Sender != null && Sender.IsLocal; // A self-check runs our own outgoing RPC through the audits as if a remote player sent it
        public bool FromOwner => IsSame(Sender, Owner);

        public object Argument(int index)
        {
            return Arguments != null && Arguments.Length > index ? Arguments[index] : null;
        }

        public string StringAt(int index)
        {
            return Argument(index) as string;
        }

        public float MagnitudeAt(int index)
        {
            return Argument(index) is Vector3 vector ? vector.magnitude : 0f;
        }

        public PhotonView ViewAt(int index)
        {
            object argument = Argument(index);

            if (argument is PhotonView view)
                return view;

            return argument is int id ? PhotonView.Find(id) : null;
        }

        public bool SentBy(PhotonPlayer player)
        {
            return IsSame(Sender, player);
        }

        public T Component<T>() where T : Component
        {
            return View != null && View.TryGetComponent(out T component) ? component : null;
        }

        private static bool IsSame(PhotonPlayer first, PhotonPlayer second) // Static: pure comparison used by the instance helpers
        {
            return first != null && second != null && first.ActorNumber == second.ActorNumber;
        }
    }
}