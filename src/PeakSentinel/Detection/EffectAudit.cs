using System.Collections.Generic;
using Peak.Afflictions;
using PeakSentinel.Compat;
using PeakSentinel.Game;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
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
        private const string CampfireSource = "lit campfire nearby";
        private const float CampfireReachScale = 1.5f;
        private const float GivenItemSeconds = 60f;
        private const float ItemReach = 4f;
        private const float LateJoinSeconds = 60f;

        private static readonly HashSet<Affliction.AfflictionType> Watched = new HashSet<Affliction.AfflictionType> // Static: fixed effects that cheats fake for godmode or endless stamina
        {
            Affliction.AfflictionType.Invincibility, Affliction.AfflictionType.BingBongShield, Affliction.AfflictionType.InfiniteStamina, Affliction.AfflictionType.NoHunger,
        };

        private readonly Dictionary<int, (float Time, string Source)> _consumed = new Dictionary<int, (float Time, string Source)>();
        private readonly List<(PhotonPlayer Player, Affliction.AfflictionType Effect, float Time)> _pending = new List<(PhotonPlayer Player, Affliction.AfflictionType Effect, float Time)>();
        private SuspicionReport _report;
        private DebugTrace _trace;
        private DetectionSettings _detection;
        private ItemRpcAudit _items;
        private AreaEffects _areas;
        private ItemLocator _locator;
        private TrueAscentState _ascent;
        private SpawnClock _spawns;

        public void Initialize(SuspicionReport report, DebugTrace trace, DetectionSettings detection, ItemRpcAudit items, AreaEffects areas, ItemLocator locator, TrueAscentState ascent, SpawnClock spawns)
        {
            _ascent = ascent;
            _spawns = spawns;
            _locator = locator;
            _areas = areas;
            _items = items;
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

                if (_items.RefusedGiveWithin(player, GivenItemSeconds))
                    SettleGiven(player, effect);
                else
                    Settle(player, effect, ConsumedSource(player, time) ?? ItemSource(player, effect) ?? _areas.SourceNear(PlayerHandler.GetPlayerCharacter(player)) ?? AscentSource(player, effect));
            }
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Consume || method == RpcNames.StartFeed || method == RpcNames.SyncAfflictions;
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Method == RpcNames.Consume)
                NoteConsume(call);
            else if (call.Method == RpcNames.StartFeed)
                NoteFeed(call);
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
            _consumed[consumer.Owner.ActorNumber] = (Time.time, $"ate '{(item == null ? UnknownItem : item.GetItemName())}'");
        }

        private void NoteFeed(RpcCall call)
        {
            if (!(call.Argument(1) is int receiverId) || !(call.Argument(2) is int itemId))
                return;

            PhotonView receiver = PhotonView.Find(receiverId);

            if (receiver == null || receiver.Owner == null)
                return;

            float feedTime = call.Argument(3) is float usingTime ? usingTime : 0f;
            string item = SingletonAsset<ItemDatabase>.Instance.itemLookup.TryGetValue((ushort)itemId, out Item fed) ? fed.GetItemName() : UnknownItem;
            _consumed[receiver.Owner.ActorNumber] = (Time.time + feedTime, $"was fed '{item}' by {PlayerNames.Name(call.Sender)}");
        }

        private void Check(RpcCall call, Affliction.AfflictionType effect)
        {
            if (effect == Affliction.AfflictionType.BingBongShield)
            {
                Character character = call.Component<Character>();
                Settle(call.Sender, effect, character != null && HoldsShieldItem(character) ? ShieldSource : null);
                return;
            }

            if (effect == Affliction.AfflictionType.NoHunger)
            {
                Character resting = call.Component<Character>();

                if (resting != null && NearLitCampfire(resting))
                {
                    Settle(call.Sender, effect, CampfireSource);
                    return;
                }
            }

            string item = ItemSource(call.Sender, effect);

            if (item != null)
            {
                Settle(call.Sender, effect, item);
                return;
            }

            _pending.Add((call.Sender, effect, Time.time));
        }

        private string ItemSource(PhotonPlayer player, Affliction.AfflictionType effect)
        {
            Character character = PlayerHandler.GetPlayerCharacter(player);

            if (character == null)
                return null;

            Item held = character.data.currentItem;

            if (held != null && Gives(held, effect))
                return $"used '{held.GetItemName()}' in hand";

            Item nearby = _locator.ItemNear(character.Center, ItemReach, item => Gives(item, effect));
            return nearby == null ? null : $"used '{nearby.GetItemName()}' and put it down";
        }

        private bool Gives(Item item, Affliction.AfflictionType effect)
        {
            if (effect == Affliction.AfflictionType.Invincibility && item.TryGetComponent(out Peak.Action_HealingGem _))
                return true;

            if (effect == Affliction.AfflictionType.InfiniteStamina && item.TryGetComponent(out Action_ApplyInfiniteStamina _))
                return true;

            foreach (Action_ApplyAffliction action in item.GetComponents<Action_ApplyAffliction>())
            {
                if (action.affliction != null && action.affliction.GetAfflictionType() == effect)
                    return true;

                if (action.extraAfflictions != null && System.Array.Exists(action.extraAfflictions, extra => extra != null && extra.GetAfflictionType() == effect))
                    return true;
            }

            return false;
        }

        private string AscentSource(PhotonPlayer player, Affliction.AfflictionType effect)
        {
            Character character = PlayerHandler.GetPlayerCharacter(player);
            return character == null ? null : _ascent.EffectCause(character, effect, _spawns.SpawnedWithin(character, LateJoinSeconds));
        }

        private void Settle(PhotonPlayer player, Affliction.AfflictionType effect, string source)
        {
            _trace.Effect(player, $"got {effect}, source: {source ?? "none"}");

            if (source != null)
                return;

            _report.Tag(player, Tag);
            _report.Suspect(player, FindingCategory.Cheats, $"got {effect} with no source (nothing eaten, no campfire, amulet or area item nearby, no golden Bing Bong in hand)");
        }

        private void SettleGiven(PhotonPlayer player, Affliction.AfflictionType effect)
        {
            _trace.Effect(player, $"got {effect} right after giving themselves items");
            _report.Tag(player, Tag);
            _report.Flag(player, FindingCategory.Cheats, $"got {effect} from an item they gave themselves (the host refused it, but it still works on their own side)");
        }

        private string ConsumedSource(PhotonPlayer player, float effectTime)
        {
            if (!_consumed.TryGetValue(player.ActorNumber, out (float Time, string Source) consumed) || Mathf.Abs(effectTime - consumed.Time) > ConsumeSeconds)
                return null;

            return $"{consumed.Source} {Mathf.Abs(effectTime - consumed.Time):F1} s before";
        }

        private bool NearLitCampfire(Character character)
        {
            foreach (Campfire campfire in GameInternals.Campfires())
            {
                if (campfire == null || !campfire.Lit)
                    continue;

                float reach = campfire.moraleBoostRadius * CampfireReachScale;

                if ((character.Center - campfire.transform.position).sqrMagnitude <= reach * reach)
                    return true;
            }

            return false;
        }

        private bool HoldsShieldItem(Character character)
        {
            Item item = character.data.currentItem;
            return item != null && item.TryGetComponent(out BingBongShieldWhileHolding _);
        }
    }
}
