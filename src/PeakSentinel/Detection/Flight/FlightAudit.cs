using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Detection.Flight
{
    public sealed class FlightAudit : MonoBehaviour
    {
        private const float SampleSeconds = 0.1f;
        private const float SpeedSpan = 0.3f;
        private const float TraceSeconds = 0.5f;
        private const float MaxProbe = 80f;
        private const float HistorySlack = 0.5f;
        private const float TraceFloor = 1.3f;
        private const float GroundLift = 0.3f;
        private const float SnapFallSeconds = 1.5f;
        private const float ContactReach = 2.5f;
        private const float ContactProbe = 0.15f;
        private const string FliesTag = "flies or speeds";

        private readonly Dictionary<int, FlightTrack> _tracks = new Dictionary<int, FlightTrack>();
        private readonly HorizontalSpeed _horizontal = new HorizontalSpeed();
        private SuspicionReport _report;
        private DebugTrace _trace;
        private DetectionSettings _detection;
        private FlightSettings _settings;
        private FlightExemptions _exemptions;
        private int _groundMask;
        private float _nextSample;

        public bool CheckLocal { get; set; } // Set: SpeedrunMonitor turns the check of the local player on for speedruns

        public void Initialize(SuspicionReport report, DebugTrace trace, DetectionSettings detection, FlightSettings settings, FlightExemptions exemptions)
        {
            _report = report;
            _trace = trace;
            _detection = detection;
            _settings = settings;
            _exemptions = exemptions;
            _groundMask = HelperFunctions.GetMask(HelperFunctions.LayerType.AllPhysicalExceptCharacter);
        }

        private void Update()
        {
            if (!_detection.Flight.Value || Time.time < _nextSample)
                return;

            _nextSample = Time.time + SampleSeconds;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && (!character.IsLocal || CheckLocal || _detection.CheckMyself.Value) && !character.isBot && character.photonView.Owner != null)
                    Sample(character, Time.time);
            }
        }

        private void Sample(Character character, float now)
        {
            int actor = character.photonView.Owner.ActorNumber;
            FlightTrack track = TrackOf(actor);
            float ground = TouchesGround(character) ? 0f : GroundDistance(character);
            string exemption = _exemptions.ReasonFor(character, actor);
            track.FlatSpeed = _horizontal.Measure(actor, now, character.Center, _settings.SpeedWindowSeconds.Value);
            CheckSpeed(character, track, exemption);

            if (character.data.dead || exemption != null || ground <= _settings.MinGroundDistance.Value)
            {
                Land(character, track, now, ground, exemption);
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

        private void Land(Character character, FlightTrack track, float now, float ground, string exemption)
        {
            bool low = ground <= _settings.MinGroundDistance.Value;

            if (exemption != null && !low)
                TraceSkip(character, track, now, ground, $"not checked: {exemption}");
            else if (ground > TraceFloor)
                TraceSkip(character, track, now, ground, $"below the airborne height {_settings.MinGroundDistance.Value:F1} m");

            if (exemption == null && !character.data.dead && low)
                track.LastGround = character.Center + Vector3.up * GroundLift;

            track.Reset();
        }

        private void CheckSpeed(Character character, FlightTrack track, string exemption)
        {
            bool tooFast = track.FlatSpeed > _settings.MaxRunSpeed.Value && exemption == null && !character.data.dead && character.data.fallSeconds <= 0f;
            track.FastFor = tooFast ? track.FastFor + SampleSeconds : Mathf.Max(track.FastFor - SampleSeconds, 0f);

            if (track.FastFor <= 0f)
                track.SpeedFlagged = false;

            if (track.SpeedFlagged || track.FastFor < _settings.SpeedFlagSeconds.Value)
                return;

            track.SpeedFlagged = true;
            Punish(character, track, $"moves too fast: {track.FlatSpeed:F1} m/s horizontally for {track.FastFor:F1} s (limit {_settings.MaxRunSpeed.Value:F0} m/s)");
        }

        private void Punish(Character character, FlightTrack track, string action)
        {
            bool snapBack = _detection.BlockFlight.Value && PhotonNetwork.IsMasterClient && track.LastGround.HasValue && !character.IsLocal && !_report.Spares(character.photonView.Owner);
            _report.Tag(character.photonView.Owner, FliesTag);

            if (snapBack)
            {
                _report.Recover(character.photonView.Owner, FindingCategory.Cheats, action + ", returned to the ground", true);
                SnapBack(character, track);
            }
            else
            {
                _report.Suspect(character.photonView.Owner, FindingCategory.Cheats, action);
            }
        }

        private void SnapBack(Character character, FlightTrack track)
        {
            character.photonView.RPC(RpcNames.Warp, RpcTarget.All, track.LastGround.Value, true);
            character.Fall(SnapFallSeconds);
            _horizontal.Forget(character.photonView.Owner.ActorNumber);
            track.FastFor = 0f;
            track.Reset();
        }

        private FlightTrack TrackOf(int actor)
        {
            if (!_tracks.TryGetValue(actor, out FlightTrack track))
            {
                track = new FlightTrack();
                _tracks[actor] = track;
            }

            return track;
        }

        private void TraceSample(Character character, FlightTrack track, float now, float ground, float speedNow, float drop)
        {
            if (!_settings.TraceSamples.Value || now < track.NextTrace)
                return;

            track.NextTrace = now + TraceSeconds;
            _trace.Flight(character.photonView.Owner, $"ground {ground:F1} m, h-speed {track.FlatSpeed:F1}, vy {speedNow:F1}, vy change over {_settings.WindowSeconds.Value:F1} s {drop:F1}, gravity missing {track.MissingFor:F1} s");
        }

        private void TraceSkip(Character character, FlightTrack track, float now, float ground, string reason)
        {
            if (!_settings.TraceSamples.Value || now < track.NextTrace)
                return;

            track.NextTrace = now + TraceSeconds;
            _trace.Flight(character.photonView.Owner, $"ground {ground:F1} m, h-speed {track.FlatSpeed:F1} (too fast {track.FastFor:F1} s), {reason}");
        }

        private bool TouchesGround(Character character)
        {
            CharacterData data = character.data;
            return data.isGrounded && (data.groundPos - character.Center).sqrMagnitude < ContactReach * ContactReach && Physics.CheckSphere(data.groundPos, ContactProbe, _groundMask);
        }

        private float GroundDistance(Character character)
        {
            float radius = _settings.GroundProbeRadius.Value;
            return Physics.SphereCast(character.Center, radius, Vector3.down, out RaycastHit hit, MaxProbe, _groundMask) ? hit.distance + radius : MaxProbe;
        }
    }
}
