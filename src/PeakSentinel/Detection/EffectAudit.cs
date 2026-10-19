using System.Collections.Generic;
using Peak.Afflictions;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class EffectAudit : MonoBehaviour, IRpcAudit
    {
        private const float ConsumeSeconds = 5f;
        private const float GraceSeconds = 2f;
        private const string Tag = "fakes protection or stamina effects";
        private const string UnknownItem = "unknown item";
        private const string ShieldSource = "golden Bing Bong in hand";

        private static readonly HashSet<Affliction.AfflictionType> Watched = new HashSet<Affliction.AfflictionType> // Static: fixed effects that cheats fake for godmode or endless stamina
        {
            Affliction.AfflictionType.Invincibility, Affliction.AfflictionType.BingBongShield, Affliction.AfflictionType.InfiniteStamina,
        };

        private readonly Dictionary<int, (float Time, string Item)> _consumed = new Dictionary<int, (float Time, string Item)>();
        private readonly List<(PhotonPlayer Player, Affliction.AfflictionType Effect, float Time)> _pending = new List<(PhotonPlayer Player, Affliction.AfflictionType Effect, float Time)>();
        private SuspicionReport _report;
        private DebugTrace _trace;
        private DetectionSettings _detection;

        public void Initialize(SuspicionReport report, DebugTrace trace, DetectionSettings detection)
        {
            _report = report;
            _trace = trace;
            _detection = detection;
        }

        private void Update()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                (PhotonPlayer player, Affliction.AfflictionType effect, float time) = _pending[i];

                if (Time.time - time < GraceSeconds)
                    continue;

                _pending.RemoveAt(i);
                Settle(player, effect, ConsumedSource(player, time));
            }
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Consume || method == RpcNames.SyncAfflictions;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Method == RpcNames.Consume)
                NoteConsume(call);
            else if (_detection.SourcelessEffects.Value && call.FromOwner && !call.FromLocal && call.Argument(1) is int type && Watched.Contains((Affliction.AfflictionType)type))
                Check(call, (Affliction.AfflictionType)type);

            return true;
        }

        private void NoteConsume(RpcCall call)
        {
            PhotonView consumer = call.ViewAt(0);

            if (consumer == null || consumer.Owner == null)
                return;

            Item item = call.Component<Item>();
            _consumed[consumer.Owner.ActorNumber] = (Time.time, item == null ? UnknownItem : item.GetItemName());
        }

        private void Check(RpcCall call, Affliction.AfflictionType effect)
        {
            if (effect == Affliction.AfflictionType.BingBongShield)
            {
                Character character = call.Component<Character>();
                Settle(call.Sender, effect, character != null && HoldsShieldItem(character) ? ShieldSource : null);
                return;
            }

            _pending.Add((call.Sender, effect, Time.time));
        }

        private void Settle(PhotonPlayer player, Affliction.AfflictionType effect, string source)
        {
            _trace.Effect(player, $"got {effect}, source: {source ?? "none"}");

            if (source != null)
                return;

            _report.Tag(player, Tag);
            _report.Suspect(player, FindingCategory.Cheats, $"got {effect} with no source (nothing eaten around it, no golden Bing Bong in hand)");
        }

        private string ConsumedSource(PhotonPlayer player, float effectTime)
        {
            if (!_consumed.TryGetValue(player.ActorNumber, out (float Time, string Item) consumed) || Mathf.Abs(effectTime - consumed.Time) > ConsumeSeconds)
                return null;

            return $"ate '{consumed.Item}' {effectTime - consumed.Time:F1} s before";
        }

        private bool HoldsShieldItem(Character character)
        {
            Item item = character.data.currentItem;
            return item != null && item.TryGetComponent(out BingBongShieldWhileHolding _);
        }
    }
}
