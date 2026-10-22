using Peak.Afflictions;
using PeakSentinel.Compat;
using PeakSentinel.Network;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class FlightExemptions
    {
        private const float StandOnPlayerHeight = 2.5f;
        private const float StandOnPlayerRadius = 1.2f;
        private const float SpawnGraceSeconds = 15f;
        private const string Spawning = "just spawned";
        private const string Airport = "in the airport";
        private const string Frozen = "held in place by the game";
        private const string Climbing = "climbing";
        private const string Carried = "carried";
        private const string PassedOut = "passed out";
        private const string Fallen = "knocked down (ragdoll)";
        private const string Balloons = "balloons";
        private const string Glider = "glider or parasol";
        private const string LowGravity = "low gravity effect";
        private const string Boosted = "speed or jump boost (energy drink, jump amulet)";
        private const string RopeCarabiner = "rope carabiner";
        private const string OnPlayer = "on a player";

        private readonly RopeLinks _ropes;
        private readonly EnvironmentMarks _environment;
        private readonly SpawnClock _spawns;
        private readonly TrueAscentState _ascent;

        public FlightExemptions(RopeLinks ropes, EnvironmentMarks environment, SpawnClock spawns, TrueAscentState ascent)
        {
            _ascent = ascent;
            _spawns = spawns;
            _ropes = ropes;
            _environment = environment;
        }

        public string ReasonFor(Character character, int actor)
        {
            CharacterData data = character.data;

            if (_spawns.SpawnedWithin(character, SpawnGraceSeconds))
                return Spawning;

            if (character.inAirport)
                return Airport;

            if (character.refs.hip.Rig.isKinematic)
                return Frozen;

            if (data.isClimbing || data.isRopeClimbing || data.isVineClimbing || data.currentClimbHandle != null)
                return Climbing;

            if (data.isCarried || data.carrier != null || data.carrierCondor != null || data.grabbingPlayer != null)
                return Carried;

            if (data.passedOut || data.fullyPassedOut)
                return PassedOut;

            if (data.fallSeconds > 0f)
                return Fallen;

            if (character.refs.balloons != null && (character.refs.balloons.heldBalloonCount > 0 || character.refs.balloons.currentBalloonCount > 0))
                return Balloons;

            if (data.currentItem != null && (data.currentItem.gliderHold || IsOpenParasol(data.currentItem)))
                return Glider;

            if (character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.LowGravity, out _))
                return LowGravity;

            if (HasAny(character, Affliction.AfflictionType.FasterBoi, Affliction.AfflictionType.DoubleJumpAmulet, Affliction.AfflictionType.MassSuperJump))
                return Boosted;

            string environment = _environment.CauseOf(character);

            if (environment != null)
                return environment;

            string ascent = _ascent.MovementCause(character);

            if (ascent != null)
                return ascent;

            if (_ropes.IsTied(actor))
                return RopeCarabiner;

            return IsOnAnotherPlayer(character) ? OnPlayer : null;
        }

        private bool HasAny(Character character, params Affliction.AfflictionType[] types)
        {
            foreach (Affliction.AfflictionType type in types)
            {
                if (character.refs.afflictions.HasAfflictionType(type, out _))
                    return true;
            }

            return false;
        }

        private bool IsOpenParasol(Item item)
        {
            return item.TryGetComponent(out Parasol parasol) && parasol.isOpen;
        }

        private bool IsOnAnotherPlayer(Character character)
        {
            Vector3 center = character.Center;

            foreach (Character other in Character.AllCharacters)
            {
                if (other == null || other == character)
                    continue;

                Vector3 offset = center - other.Center;
                Vector2 flat = new Vector2(offset.x, offset.z);

                if (offset.y > 0f && offset.y < StandOnPlayerHeight && flat.sqrMagnitude < StandOnPlayerRadius * StandOnPlayerRadius)
                    return true;
            }

            return false;
        }
    }
}