using System.Collections;
using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Core;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class DesyncFixer : MonoBehaviour
    {
        private const float ScanSeconds = 0.25f;
        private const float MaxSpeed = 35f;
        private const float MaxOffset = 8f;
        private const float NearOffset = 3f;
        private const float CursedSeconds = 1.5f;
        private const float CooldownSeconds = 10f;
        private const float ChillSeconds = 3f;
        private const float ChillDamping = 20f;

        private readonly Dictionary<Character, (CharacterSyncer Syncer, float CursedFor, float FixedAt)> _tracks = new Dictionary<Character, (CharacterSyncer Syncer, float CursedFor, float FixedAt)>();
        private SuspicionReport _report;
        private OtherSettings _settings;
        private StateLedger _ledger;
        private CarryReset _carry;
        private SelfResync _selfResync;
        private float _nextScan;

        public void Initialize(SuspicionReport report, OtherSettings settings, StateLedger ledger, CarryReset carry, SelfResync selfResync)
        {
            _selfResync = selfResync;
            _ledger = ledger;
            _carry = carry;
            _report = report;
            _settings = settings;
        }

        private void Update()
        {
            if (!_settings.FixDesyncedPlayers.Value || Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && !character.IsLocal && !character.isBot)
                    Watch(character);
            }
        }

        public void Resync(PhotonPlayer player)
        {
            int replayed = _ledger.Reapply(player);
            Character character = PlayerHandler.GetPlayerCharacter(player);
            _report.LogAction(player, $"resynced: {replayed} of their latest state updates were applied again{ClearCarry(character)}");
            Fix(character, "by you");
        }

        public void ResyncMe()
        {
            Character me = Character.localCharacter;

            if (me == null || !PhotonNetwork.InRoom)
                return;

            _selfResync.Push();
            string carry = _carry.Applies ? $", {_carry.DropMine(me)} piggyback carries dropped for everyone" : string.Empty;
            _report.LogAction(PhotonNetwork.LocalPlayer, $"resynced yourself for everyone: alive or dead, standing, awake, crouching, statuses, effects and petrification sent again{carry}");
        }

        public void ResyncAll()
        {
            if (!PhotonNetwork.InRoom)
                return;

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal)
                    Resync(player);
            }

            ResyncMe();
            _report.LogAction(PhotonNetwork.LocalPlayer, "Resync all: every player resynced on your side, you for everyone");
        }

        public void Fix(Character character, string reason)
        {
            if (character == null || character.IsLocal || !TrackOf(character, out (CharacterSyncer Syncer, float CursedFor, float FixedAt) track) || track.Syncer.RemoteValue.IsNone)
                return;

            _tracks[character] = (track.Syncer, 0f, Time.unscaledTime);

            foreach (FixedJoint joint in character.refs.ragdoll.GetComponentsInChildren<FixedJoint>())
                Destroy(joint);

            Vector3 real = track.Syncer.RemoteValue.Value.hipLocation;
            character.WarpPlayer(real, false);
            StartCoroutine(Chill(character, track.Syncer));
            _report.Notice(character.photonView.Owner, FindingCategory.Other, $"their body was out of sync on your side ({reason}), snapped back locally");
        }

        private void Watch(Character character)
        {
            if (!TrackOf(character, out (CharacterSyncer Syncer, float CursedFor, float FixedAt) track) || track.Syncer.RemoteValue.IsNone || character.warping || character.data.carrier != null)
                return;

            Rigidbody hip = character.GetBodypart(BodypartType.Hip).Rig;
            Vector3 real = track.Syncer.RemoteValue.Value.hipLocation;
            float speed = hip.linearVelocity.magnitude;
            float offset = (real - hip.position).magnitude;
            bool cursed = offset > MaxOffset || (speed > MaxSpeed && offset > NearOffset);
            float cursedFor = cursed ? track.CursedFor + ScanSeconds : 0f;
            _tracks[character] = (track.Syncer, cursedFor, track.FixedAt);

            if (cursedFor >= CursedSeconds && Time.unscaledTime - track.FixedAt >= CooldownSeconds)
                Fix(character, $"{offset:F0} m off, {speed:F0} m/s");
        }

        private bool TrackOf(Character character, out (CharacterSyncer Syncer, float CursedFor, float FixedAt) track)
        {
            if (_tracks.TryGetValue(character, out track))
                return true;

            if (!character.TryGetComponent(out CharacterSyncer syncer))
                return false;

            track = (syncer, 0f, -CooldownSeconds);
            _tracks[character] = track;
            return true;
        }

        private string ClearCarry(Character character)
        {
            if (character == null || Character.localCharacter == null || !_carry.Applies)
                return string.Empty;

            int cleared = _carry.ClearStale(character) + _carry.ClearStale(Character.localCharacter);
            return $", {cleared} stuck piggyback states cleared on your side";
        }

        private IEnumerator Chill(Character character, CharacterSyncer syncer)
        {
            while (character != null && character.warping)
                yield return new WaitForFixedUpdate();

            float started = Time.realtimeSinceStartup;

            while (character != null && Time.realtimeSinceStartup - started < ChillSeconds)
            {
                character.refs.ragdoll.SnapToAnimation();
                character.refs.ragdoll.SetRigDamping(ChillDamping);
                yield return new WaitForFixedUpdate();
            }

            if (character == null)
                yield break;

            character.refs.ragdoll.SetRigDamping(0f);

            if (syncer.RemoteValue.IsSome)
                syncer.lastPosition = Optionable<float3>.Some(syncer.RemoteValue.Value.hipLocation);

            character.refs.ragdoll.SnapToAnimation();
        }
    }
}