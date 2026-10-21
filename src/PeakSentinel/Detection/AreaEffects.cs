using System.Collections.Generic;
using Peak;
using Peak.Afflictions;
using PeakSentinel.Network;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class AreaEffects : IRpcAudit
    {
        private const string MassAffliction = "AddMassAffliction";
        private const string ItemAreaAffliction = "TryAddAfflictionToLocalCharacter";
        private const string DaggerBuff = "RPC_RitualDaggerBuff";
        private const string DaggerSource = "a ritual dagger sacrifice (buffs everyone)";
        private const float Everywhere = float.PositiveInfinity;
        private const float MemorySeconds = 4f;
        private const float DefaultRadius = 20f;
        private const float ReachMargin = 5f;
        private const float AuraReach = 25f;

        private readonly List<(float Time, Vector3 Origin, float Radius, string Source)> _recent = new List<(float Time, Vector3 Origin, float Radius, string Source)>();
        private readonly ItemLocator _locator;

        public AreaEffects(ItemLocator locator)
        {
            _locator = locator;
        }

        public bool Watches(string method)
        {
            return method == MassAffliction || method == ItemAreaAffliction || method == DaggerBuff;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Method == MassAffliction && call.Argument(0) is Vector3 origin && call.Argument(1) is float radius)
                Remember(origin, radius, "a stamina amulet nearby");
            else if (call.Method == DaggerBuff && call.Component<RitualDaggerFeedBehavior>() != null)
                Remember(call.View.transform.position, Everywhere, DaggerSource);
            else if (call.Method == ItemAreaAffliction && call.View != null)
                Remember(call.View.transform.position, RadiusOf(call), "an area item nearby");

            return true;
        }

        public string SourceNear(Character character)
        {
            if (character == null)
                return null;

            float now = Time.time;
            _recent.RemoveAll(effect => now - effect.Time > MemorySeconds);

            foreach ((float _, Vector3 origin, float radius, string source) in _recent)
            {
                float reach = radius + ReachMargin;

                if ((character.Center - origin).sqrMagnitude <= reach * reach)
                    return source;
            }

            return AuraNear(character);
        }

        private string AuraNear(Character character)
        {
            foreach (Character other in Character.AllCharacters)
            {
                if (other == null || (other.Center - character.Center).sqrMagnitude > AuraReach * AuraReach)
                    continue;

                if (other.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.RadiateInfiniteStam, out _))
                    return "a player radiating stamina nearby";

                if (_locator.Holds<InfiniteStamAmulet>(other))
                    return "a stamina amulet nearby";
            }

            return null;
        }

        private float RadiusOf(RpcCall call)
        {
            Action_ApplyMassAffliction area = call.Component<Action_ApplyMassAffliction>();
            return area == null ? DefaultRadius : Mathf.Max(area.radius, DefaultRadius);
        }

        private void Remember(Vector3 origin, float radius, string source)
        {
            _recent.Add((Time.time, origin, radius, source));
        }
    }
}
