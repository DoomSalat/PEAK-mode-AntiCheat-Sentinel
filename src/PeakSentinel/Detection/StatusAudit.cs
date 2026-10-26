using System.Collections.Generic;
using PeakSentinel.Compat;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class StatusAudit : MonoBehaviour, IRpcAudit
    {
        private const float SyncSeconds = 2f;
        private const float FrozenChange = 0.005f;
        private const float FullBar = 2f;
        private const string Tag = "ignores poison and other statuses";

        private readonly List<Expectation> _pending = new List<Expectation>();
        private SuspicionReport _report;
        private DetectionSettings _detection;
        private ItemLocator _locator;
        private TrueAscentState _ascent;

        public void Initialize(SuspicionReport report, DetectionSettings detection, ItemLocator locator, TrueAscentState ascent)
        {
            _ascent = ascent;
            _locator = locator;
            _report = report;
            _detection = detection;
        }

        private void Update()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Expectation expectation = _pending[i];

                if (Time.time < expectation.Due)
                    continue;

                _pending.RemoveAt(i);
                Settle(expectation);
            }
        }

        public bool Watches(string method)
        {
            return method == RpcNames.Consume;
        }

        public bool Inspect(RpcCall call)
        {
            if (!_detection.SourcelessEffects.Value || call.FromLocal)
                return true;

            PhotonView consumerView = call.ViewAt(0);
            Item item = call.Component<Item>();

            if (consumerView == null || item == null || !consumerView.TryGetComponent(out Character consumer) || (consumer.IsLocal && !call.SelfCheck) || IsCooked(item) || item.TryGetComponent(out Action_RandomMushroomEffect _))
                return true;

            foreach (Action_InflictPoison poison in item.GetComponents<Action_InflictPoison>())
            {
                if (poison.isActiveAndEnabled)
                    Expect(consumer, item, CharacterAfflictions.STATUSTYPE.Poison, poison.delay + SyncSeconds);
            }

            foreach (Action_ModifyStatus modify in item.GetComponents<Action_ModifyStatus>())
            {
                if (modify.isActiveAndEnabled && modify.changeAmount > 0f)
                    Expect(consumer, item, modify.statusType, SyncSeconds);
            }

            return true;
        }

        private void Expect(Character consumer, Item item, CharacterAfflictions.STATUSTYPE status, float seconds)
        {
            if (status == CharacterAfflictions.STATUSTYPE.Hot || status == CharacterAfflictions.STATUSTYPE.Cold)
                return; // Heat and cold cancel each other and fade on their own, so a berry can leave them unchanged

            float before = consumer.refs.afflictions.GetCurrentStatus(status);
            _pending.Add(new Expectation(consumer, item.GetItemName(), status, before, Time.time + seconds));
        }

        private void Settle(Expectation expectation)
        {
            Character consumer = expectation.Consumer;

            if (consumer == null || IsProtected(consumer, expectation.Status))
                return;

            float now = consumer.refs.afflictions.GetCurrentStatus(expectation.Status);

            if (Mathf.Abs(now - expectation.Before) >= FrozenChange)
                return;

            PhotonView view = consumer.photonView;
            _report.Tag(view.Owner, Tag);
            _report.Suspect(view.Owner, FindingCategory.Cheats, $"ate '{expectation.Item}' but their {expectation.Status} stayed frozen at {now * 100f:F0}%, status lock");
        }

        private bool IsCooked(Item item)
        {
            return item.TryGetComponent(out ItemCooking cooking) && cooking.timesCookedLocal > 0;
        }

        private bool IsProtected(Character consumer, CharacterAfflictions.STATUSTYPE status)
        {
            if (consumer.data.dead || consumer.data.isInvincible || consumer.data.isSkeleton || consumer.isZombie || consumer.statusesLocked || consumer.inAirport)
                return true;

            if (status == CharacterAfflictions.STATUSTYPE.Hunger && !consumer.refs.afflictions.canGetHungry)
                return true;

            if (consumer.refs.afflictions.statusSum >= FullBar - FrozenChange)
                return true;

            if (_ascent.StatusCause(consumer, status) != null)
                return true;

            return _locator.Holds<Peak.HealingAmulet>(consumer) || consumer.refs.afflictions.HasAfflictionType(Peak.Afflictions.Affliction.AfflictionType.HealAll, out _);
        }

        private sealed class Expectation
        {
            public Expectation(Character consumer, string item, CharacterAfflictions.STATUSTYPE status, float before, float due)
            {
                Consumer = consumer;
                Item = item;
                Status = status;
                Before = before;
                Due = due;
            }

            public Character Consumer { get; }
            public string Item { get; }
            public CharacterAfflictions.STATUSTYPE Status { get; }
            public float Before { get; }
            public float Due { get; }
        }
    }
}
