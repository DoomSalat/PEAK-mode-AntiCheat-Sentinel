using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using PeakSentinel.Network;
using Photon.Pun;

namespace PeakSentinel.Protection
{
    public sealed class CarryReset
    {
        private const string PiggybackGuid = "nakazora.peak.piggyback";
        private const string TogglePhysicsMethod = "ToggleCarryPhysics";

        private readonly MethodInfo _togglePhysics = AccessTools.Method(typeof(CharacterCarrying), TogglePhysicsMethod);

        public bool Applies => Chainloader.PluginInfos.ContainsKey(PiggybackGuid);

        public int DropMine(Character me)
        {
            int dropped = 0;

            foreach (Character other in Character.AllCharacters)
            {
                if (other == null || other == me || (me.data.carriedPlayer != other && other.data.carrier != me))
                    continue;

                me.photonView.RPC(RpcNames.DropCarried, RpcTarget.All, other.photonView);
                dropped++;
            }

            if (me.data.carrier != null)
            {
                me.data.carrier.photonView.RPC(RpcNames.DropCarried, RpcTarget.All, me.photonView);
                dropped++;
            }

            return dropped;
        }

        public int ClearStale(Character character)
        {
            int cleared = 0;
            Character carrier = character.data.carrier;

            if (carrier != null && carrier.data.carriedPlayer != character)
            {
                character.data.carrier = null;
                character.data.isCarried = false;
                _togglePhysics.Invoke(character.refs.carriying, new object[] { false });
                cleared++;
            }

            Character carried = character.data.carriedPlayer;

            if (carried != null && carried.data.carrier != character)
            {
                character.data.carriedPlayer = null;
                cleared++;
            }

            return cleared;
        }
    }
}
