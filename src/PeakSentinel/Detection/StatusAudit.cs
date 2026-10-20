using System.Collections.Generic;
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
        private const float ExpectedShare = 0.5f;
        private const float MinimumRise = 0.01f;
        private const string Tag = "ignores poison and other statuses";

        private readonly List<Expectation> _pending = new List<Expectation>();
        private SuspicionReport _report;
        private DetectionSettings _detection;
        private ItemLocator _locator;

        public void Initialize(SuspicionReport report, DetectionSettings detection, ItemLocator locator)
        {
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

            if (consumerView == null || item == null || !consumerView.TryGetComponent(out Character consumer) || consumer.IsLocal)
                return true;

            foreach (Action_InflictPoison poison in item.GetComponents<Action_InflictPoison>())
            {
                if (poison.isActiveAndEnabled)
                    Expect(consumer, item, CharacterAfflictions.STATUSTYPE.Poison, poison.poisonPerSecond, poison.delay + SyncSeconds);
            }

            foreach (Action_ModifyStatus modify in item.GetComponents<Action_ModifyStatus>())
            {
                if (modify.isActiveAndEnabled && modify.changeAmount > 0f)
                    Expect(consumer, item, modify.statusType, modify.changeAmount, SyncSeconds);
            }

            return true;
        }

        private void Expect(Character consumer, Item item, CharacterAfflictions.STATUSTYPE status, float amount, float seconds)
        {
            float before = consumer.refs.afflictions.GetCurrentStatus(status);
            _pending.Add(new Expectation(consumer, item.GetItemName(), status, before, Mathf.Max(MinimumRise, amount * ExpectedShare), Time.time + seconds));
        }

        private void Settle(Expectation expectation)
        {
            Character consumer = expectation.Consumer;

            if (consumer == null || consumer.data.dead || consumer.data.isInvincible || _locator.Holds<Peak.HealingAmulet>(consumer) || consumer.refs.afflictions.HasAfflictionType(Peak.Afflictions.Affliction.AfflictionType.HealAll, out _))
                return;

            float now = consumer.refs.afflictions.GetCurrentStatus(expectation.Status);

            if (now - expectation.Before >= expectation.Rise)
                return;

            PhotonView view = consumer.photonView;
            _report.Tag(view.Owner, Tag);
            _report.Suspect(view.Owner, FindingCategory.Cheats, $"ate '{expectation.Item}' but their {expectation.Status} did not rise ({expectation.Before * 100f:F0}% → {now * 100f:F0}%), status lock");
        }

        private sealed class Expectation
        {
            public Expectation(Character consumer, string item, CharacterAfflictions.STATUSTYPE status, float before, float rise, float due)
            {
                Consumer = consumer;
                Item = item;
                Status = status;
                Before = before;
                Rise = rise;
                Due = due;
            }

            public Character Consumer { get; }
            public string Item { get; }
            public CharacterAfflictions.STATUSTYPE Status { get; }
            public float Before { get; }
            public float Rise { get; }
            public float Due { get; }
        }
    }
}
