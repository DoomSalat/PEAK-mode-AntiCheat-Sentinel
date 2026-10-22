using System.Collections.Generic;
using Peak.Afflictions;
using PeakSentinel.Compat;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Detection.Stamina
{
    public sealed class StaminaAudit : MonoBehaviour
    {
        private const float SampleSeconds = 0.25f;
        private const float TraceSeconds = 0.5f;
        private const float HangingDrain = 0.001f;
        private const float SurfaceReach = 1f;
        private const int SurfaceBuffer = 8;
        private const float LightSurface = 0.34f;
        private const float NearlyEmpty = 0.1f;
        private const string Tag = "infinite stamina";
        private const string Sprint = "sprinting";
        private const string Climb = "climbing";
        private const string Dead = "dead or passed out";
        private const string Airport = "airport";
        private const string StaminaItem = "stamina item effect";
        private const string Empty = "stamina nearly empty";
        private const string Ladder = "ladder or light climbing surface";

        private readonly Dictionary<int, StaminaTrack> _tracks = new Dictionary<int, StaminaTrack>();
        private readonly Collider[] _surfaces = new Collider[SurfaceBuffer];
        private SuspicionReport _report;
        private DebugTrace _trace;
        private DetectionSettings _detection;
        private StaminaSettings _settings;
        private TrueAscentState _ascent;
        private int _surfaceMask;
        private float _nextSample;

        public bool CheckLocal { get; set; } // Set: SpeedrunMonitor turns the check of the local player on for speedruns

        public void Initialize(SuspicionReport report, DebugTrace trace, DetectionSettings detection, StaminaSettings settings, TrueAscentState ascent)
        {
            _surfaceMask = HelperFunctions.GetMask(HelperFunctions.LayerType.AllPhysicalExceptCharacter);
            _ascent = ascent;
            _report = report;
            _trace = trace;
            _detection = detection;
            _settings = settings;
        }

        private void Update()
        {
            if (!_detection.Stamina.Value || Time.time < _nextSample)
                return;

            _nextSample = Time.time + SampleSeconds;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && (!character.IsLocal || CheckLocal) && !character.isBot && character.photonView.Owner != null)
                    Sample(character, Time.time);
            }
        }

        private void Sample(Character character, float now)
        {
            StaminaTrack track = TrackOf(character.photonView.Owner.ActorNumber);
            string effort = EffortOf(character);
            float stamina = character.data.currentStamina + character.data.extraStamina;
            string exemption = effort == null ? null : ExemptionOf(character, stamina);

            if (effort == null || exemption != null)
            {
                if (exemption != null)
                    Trace(character, track, now, $"{effort}, stamina {stamina:F2}, not checked: {exemption}");

                track.Reset();
                return;
            }

            bool ate = character.data.extraStamina > track.Bonus + _settings.MinDrop.Value;
            track.Bonus = character.data.extraStamina;

            if (!track.Lowest.HasValue || ate || stamina < track.Lowest.Value - HangingDrain)
            {
                track.Lowest = stamina;
                track.UnspentFor = 0f;
            }
            else
            {
                track.UnspentFor += SampleSeconds;
            }

            Trace(character, track, now, $"{effort}, stamina {stamina:F2} (regular {character.data.currentStamina:F2}, bonus {character.data.extraStamina:F2}), lowest {track.Lowest.Value:F2}, not spent for {track.UnspentFor:F2} s");

            if (track.Flagged || track.UnspentFor < _settings.FlagSeconds.Value)
                return;

            track.Flagged = true;
            _report.Tag(character.photonView.Owner, Tag);
            _report.Suspect(character.photonView.Owner, FindingCategory.Cheats, $"stamina does not go down: {effort} for {track.UnspentFor:F1} s at stamina {stamina:F2}");
        }

        private string EffortOf(Character character)
        {
            CharacterData data = character.data;

            if (data.isClimbing || data.isRopeClimbing || data.isVineClimbing)
                return Climb;

            Vector3 velocity = data.avarageVelocity;
            bool moving = new Vector2(velocity.x, velocity.z).sqrMagnitude > _settings.MinSprintSpeed.Value * _settings.MinSprintSpeed.Value;
            return data.isSprinting && data.isGrounded && moving ? Sprint : null;
        }

        private string ExemptionOf(Character character, float stamina)
        {
            if (character.data.dead || character.data.passedOut || character.data.fullyPassedOut)
                return Dead;

            if (character.inAirport)
                return Airport;

            CharacterAfflictions afflictions = character.refs.afflictions;

            if (afflictions.HasAfflictionType(Affliction.AfflictionType.InfiniteStamina, out _) || afflictions.HasAfflictionType(Affliction.AfflictionType.FasterBoi, out _))
                return StaminaItem;

            string ascent = _ascent.StaminaCause(character);

            if (ascent != null)
                return ascent;

            if (character.data.isClimbing && OnLightSurface(character.data.climbPos))
                return Ladder;

            return stamina <= NearlyEmpty ? Empty : null;
        }

        private bool OnLightSurface(Vector3 climbPos)
        {
            int count = Physics.OverlapSphereNonAlloc(climbPos, SurfaceReach, _surfaces, _surfaceMask);

            for (int i = 0; i < count; i++)
            {
                if (_surfaces[i].TryGetComponent(out ClimbModifierSurface surface) && surface.staminaUsageMultiplier <= LightSurface)
                    return true;
            }

            return false;
        }

        private StaminaTrack TrackOf(int actor)
        {
            if (!_tracks.TryGetValue(actor, out StaminaTrack track))
            {
                track = new StaminaTrack();
                _tracks[actor] = track;
            }

            return track;
        }

        private void Trace(Character character, StaminaTrack track, float now, string numbers)
        {
            if (!_settings.TraceSamples.Value || now < track.NextTrace)
                return;

            track.NextTrace = now + TraceSeconds;
            _trace.Stamina(character.photonView.Owner, numbers);
        }
    }
}