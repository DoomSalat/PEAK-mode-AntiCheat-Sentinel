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
        private const string RadiatingSource = "a player radiating stamina nearby";
        private const string AmuletSource = "a stamina amulet nearby";

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
                Remember(origin, radius, AmuletSource);
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
            string aura = AuraNear(character);

            if (aura != null)
                return aura;

            foreach ((float _, Vector3 origin, float radius, string source) in _recent)
            {
                float reach = radius + ReachMargin;

                if ((character.Center - origin).sqrMagnitude <= reach * reach)
                    return source;
            }

            return null;
        }

        private string AuraNear(Character character)
        {
            foreach (Character other in Character.AllCharacters)
            {
                if (other == null || (other.Center - character.Center).sqrMagnitude > AuraReach * AuraReach)
                    continue;

                if (other.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.RadiateInfiniteStam, out _))
                    return RememberAura(other, RadiatingSource);

                if (_locator.Holds<InfiniteStamAmulet>(other))
                    return RememberAura(other, AmuletSource);
            }

            return null;
        }

        private string RememberAura(Character source, string description)
        {
            Remember(source.Center, AuraReach, description); // The aura's last stamina ticks outlive it by a second or two, so it stays a source for MemorySeconds
            return description;
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
