using System.Collections.Generic;
using Peak.Afflictions;
using Photon.Pun;
using PeakSentinel.Reporting;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class FlightAudit : MonoBehaviour
    {
        private const float SampleSeconds = 0.1f;
        private const float SpeedSpan = 0.3f;
        private const float TraceSeconds = 0.5f;
        private const float MaxProbe = 80f;
        private const float StandOnPlayerHeight = 2.5f;
        private const float StandOnPlayerRadius = 1.2f;
        private const float HistorySlack = 0.5f;
        private const float TraceFloor = 1.3f;
        private const float GroundLift = 0.3f;
        private const float SnapFallSeconds = 1.5f;
        private const string WarpRpc = "WarpPlayerRPC";
        private const string FliesTag = "flies or speeds";

        private readonly Dictionary<int, Track> _tracks = new Dictionary<int, Track>();
        private readonly HorizontalSpeed _horizontal = new HorizontalSpeed();
        private SuspicionReport _report;
        private DebugTrace _trace;
        private DetectionSettings _detection;
        private FlightSettings _settings;
        private RopeLinks _ropes;
        private EnvironmentMarks _environment;
        private int _terrainMask;
        private float _nextSample;

        public void Initialize(SuspicionReport report, DebugTrace trace, DetectionSettings detection, FlightSettings settings, RopeLinks ropes, EnvironmentMarks environment)
        {
            _environment = environment;
            _report = report;
            _trace = trace;
            _detection = detection;
            _settings = settings;
            _ropes = ropes;
            _terrainMask = HelperFunctions.GetMask(HelperFunctions.LayerType.TerrainMap);
        }

        private void Update()
        {
            if (!_detection.Flight.Value || Time.time < _nextSample)
                return;

            _nextSample = Time.time + SampleSeconds;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && !character.IsLocal && !character.isBot && character.photonView.Owner != null)
                    Sample(character, Time.time);
            }
        }

        private void Sample(Character character, float now)
        {
            int actor = character.photonView.Owner.ActorNumber;

            if (!_tracks.TryGetValue(actor, out Track track))
            {
                track = new Track();
                _tracks[actor] = track;
            }

            float ground = GroundDistance(character);
            string support = SupportOf(character, actor);
            float flatSpeed = _horizontal.Measure(actor, now, character.Center, _settings.SpeedWindowSeconds.Value);
            track.FlatSpeed = flatSpeed;
            CheckSpeed(character, track, support, flatSpeed);

            if (character.data.dead || support != null || ground <= _settings.MinGroundDistance.Value)
            {
                if (support != null && ground > _settings.MinGroundDistance.Value)
                    TraceSkip(character, track, now, ground, $"not checked: {support}");
                else if (ground > TraceFloor)
                    TraceSkip(character, track, now, ground, $"below the airborne height {_settings.MinGroundDistance.Value:F1} m");

                if (support == null && !character.data.dead && ground <= _settings.MinGroundDistance.Value)
                    track.LastGround = character.Center + Vector3.up * GroundLift;

                track.Reset();
                return;
            }

            track.Add(now, character.Center.y, _settings.WindowSeconds.Value + SpeedSpan + HistorySlack);

            if (!track.TrySpeeds(now, _settings.WindowSeconds.Value, SpeedSpan, out float speedNow, out float speedThen))
            {
                TraceSkip(character, track, now, ground, "airborne, collecting the window");
                return;
            }

            float drop = speedNow - speedThen;
            bool gravityMissing = speedNow > _settings.FastFallSpeed.Value && drop > _settings.MaxGravityDrop.Value;
            track.MissingFor = gravityMissing ? track.MissingFor + SampleSeconds : Mathf.Max(track.MissingFor - SampleSeconds, 0f);
            TraceSample(character, track, now, ground, speedNow, drop);

            if (track.Flagged || track.MissingFor < _settings.FlagSeconds.Value)
                return;

            track.Flagged = true;
            Punish(character, track, $"is flying: {track.MissingFor:F1} s without gravity, {ground:F1} m above the ground, vertical speed {speedNow:F1} m/s");
        }

        private void CheckSpeed(Character character, Track track, string support, float flatSpeed)
        {
            bool tooFast = flatSpeed > _settings.MaxRunSpeed.Value && support == null && !character.data.dead && character.data.fallSeconds <= 0f;
            track.FastFor = tooFast ? track.FastFor + SampleSeconds : Mathf.Max(track.FastFor - SampleSeconds, 0f);

            if (track.FastFor <= 0f)
                track.SpeedFlagged = false;

            if (track.SpeedFlagged || track.FastFor < _settings.SpeedFlagSeconds.Value)
                return;

            track.SpeedFlagged = true;
            Punish(character, track, $"moves too fast: {flatSpeed:F1} m/s horizontally for {track.FastFor:F1} s (limit {_settings.MaxRunSpeed.Value:F0} m/s)");
        }

        private void Punish(Character character, Track track, string action)
        {
            bool snapBack = _detection.BlockFlight.Value && PhotonNetwork.IsMasterClient && track.LastGround.HasValue;
            _report.Tag(character.photonView.Owner, FliesTag);
            _report.Suspect(character.photonView.Owner, FindingCategory.Cheats, snapBack ? action + ", returned to the ground" : action, snapBack);

            if (snapBack)
                SnapBack(character, track);
        }

        private void SnapBack(Character character, Track track)
        {
            character.photonView.RPC(WarpRpc, RpcTarget.All, track.LastGround.Value, true);
            character.Fall(SnapFallSeconds);
            _horizontal.Forget(character.photonView.Owner.ActorNumber);
            track.FastFor = 0f;
            track.Reset();
        }

        private void TraceSample(Character character, Track track, float now, float ground, float speedNow, float drop)
        {
            if (!_settings.TraceSamples.Value || now < track.NextTrace)
                return;

            track.NextTrace = now + TraceSeconds;
            _trace.Flight(character.photonView.Owner, $"ground {ground:F1} m, h-speed {track.FlatSpeed:F1}, vy {speedNow:F1}, vy change over {_settings.WindowSeconds.Value:F1} s {drop:F1}, gravity missing {track.MissingFor:F1} s");
        }

        private void TraceSkip(Character character, Track track, float now, float ground, string reason)
        {
            if (!_settings.TraceSamples.Value || now < track.NextTrace)
                return;

            track.NextTrace = now + TraceSeconds;
            _trace.Flight(character.photonView.Owner, $"ground {ground:F1} m, h-speed {track.FlatSpeed:F1} (too fast {track.FastFor:F1} s), {reason}");
        }

        private float GroundDistance(Character character)
        {
            Vector3 origin = character.Center;
            return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, MaxProbe, _terrainMask) ? hit.distance : MaxProbe;
        }

        private string SupportOf(Character character, int actor)
        {
            CharacterData data = character.data;

            if (data.isClimbing || data.isRopeClimbing || data.isVineClimbing || data.currentClimbHandle != null)
                return "climbing";

            if (data.isCarried || data.carrier != null || data.carrierCondor != null || data.grabbingPlayer != null)
                return "carried";

            if (data.passedOut || data.fullyPassedOut)
                return "passed out";

            if (character.refs.balloons != null && (character.refs.balloons.heldBalloonCount > 0 || character.refs.balloons.currentBalloonCount > 0))
                return "balloons";

            if (data.currentItem != null && (data.currentItem.gliderHold || IsOpenParasol(data.currentItem)))
                return "glider or parasol";

            if (character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.LowGravity, out _))
                return "low gravity effect";

            string environment = _environment.CauseOf(character);

            if (environment != null)
                return environment;

            if (_ropes.IsTied(actor))
                return "rope carabiner";

            return IsOnAnotherPlayer(character) ? "on a player" : null;
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

        private sealed class Track
        {
            private readonly List<(float Time, float Height)> _samples = new List<(float Time, float Height)>();

            public float MissingFor { get; set; } // Set: accumulated by FlightAudit while gravity is missing
            public bool Flagged { get; set; } // Set: FlightAudit reports each airborne episode once
            public float NextTrace { get; set; } // Set: FlightAudit throttles the trace
            public Vector3? LastGround { get; set; } // Set: FlightAudit remembers where the player last stood, kept across resets
            public float FlatSpeed { get; set; } // Set: latest horizontal speed for the trace
            public float FastFor { get; set; } // Set: accumulated by FlightAudit while the player is faster than a run
            public bool SpeedFlagged { get; set; } // Set: FlightAudit reports each too-fast stretch once

            public void Add(float time, float height, float keep)
            {
                _samples.Add((time, height));

                while (_samples.Count > 0 && time - _samples[0].Time > keep)
                    _samples.RemoveAt(0);
            }

            public void Reset()
            {
                _samples.Clear();
                MissingFor = 0f;
                Flagged = false;
            }

            public bool TrySpeeds(float now, float window, float span, out float speedNow, out float speedThen)
            {
                speedNow = 0f;
                speedThen = 0f;

                if (_samples.Count < 2 || now - _samples[0].Time < window + span)
                    return false;

                speedNow = SpeedAt(now, span);
                speedThen = SpeedAt(now - window, span);
                return true;
            }

            private float SpeedAt(float time, float span)
            {
                return (HeightAt(time) - HeightAt(time - span)) / span;
            }

            private float HeightAt(float time)
            {
                for (int i = _samples.Count - 1; i >= 0; i--)
                {
                    if (_samples[i].Time <= time)
                        return _samples[i].Height;
                }

                return _samples[0].Height;
            }
        }
    }
}
