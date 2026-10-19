using System;
using System.Collections.Generic;
using PeakSentinel.Reporting;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class WorldAudit
    {
        private const string WorldTag = "breaks host-only game events";
        private const string HelicopterRpc = "TriggerHelicopter";
        private const string BananaRpc = "RPCA_TriggerBanana";
        private const int FlareLitKey = 3;
        private const float PeakCheckOffset = 10f;

        private static readonly Dictionary<string, Rule[]> Rules = BuildRules(); // Static: fixed table of host-only RPCs (same as PEAKER)

        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public WorldAudit(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return Rules.ContainsKey(method);
        }

        public bool Inspect(string method, PhotonPlayer sender, PhotonView view, object[] arguments)
        {
            if (!_detection.WorldEvents.Value || sender == null || view == null || !Rules.TryGetValue(method, out Rule[] rules))
                return true;

            foreach (Rule rule in rules)
            {
                if (view.GetComponent(rule.Component) == null)
                    continue;

                if (IsAllowed(rule, method, sender, view, arguments))
                    return true;

                bool block = _detection.BlockWorldEvents.Value;
                _report.Tag(sender, WorldTag);
                _report.Flag(sender, FindingCategory.Cheats, Describe(rule, method, view, arguments), block);
                return !block;
            }

            return true;
        }

        private bool IsAllowed(Rule rule, string method, PhotonPlayer sender, PhotonView view, object[] arguments)
        {
            bool isOwner = view.Owner != null && view.Owner.ActorNumber == sender.ActorNumber;

            switch (rule.Allowed)
            {
                case Sender.Host:
                    return sender.IsMasterClient || (method == HelicopterRpc && IsHelicopterLegit(view));

                case Sender.Owner:
                    return isOwner;

                case Sender.Victim:
                    PhotonView victim = VictimOf(arguments);
                    return victim == null || (victim.Owner != null && victim.Owner.ActorNumber == sender.ActorNumber);

                default:
                    return true;
            }
        }

        private string Describe(Rule rule, string method, PhotonView view, object[] arguments)
        {
            if (method == BananaRpc)
                return $"{rule.Action} {SuspicionReport.NameOf(VictimOf(arguments)?.Owner)}";

            return rule.NamesTarget ? $"{rule.Action} {SuspicionReport.NameOf(view.Owner)}" : rule.Action;
        }

        private PhotonView VictimOf(object[] arguments)
        {
            return arguments != null && arguments.Length > 0 && arguments[0] is int viewId ? PhotonView.Find(viewId) : null;
        }

        private bool IsHelicopterLegit(PhotonView view)
        {
            if (Singleton<PeakHandler>.Instance.summonedHelicopter)
                return true;

            Flare flare = view.GetComponent<Flare>();
            return flare != null && flare.GetData<BoolItemData>((DataEntryKey)FlareLitKey).Value && Singleton<MountainProgressHandler>.Instance.IsAtPeak(flare.transform.position + Vector3.forward * PeakCheckOffset);
        }

        private static Dictionary<string, Rule[]> BuildRules() // Static: builds the fixed rule table once
        {
            Dictionary<string, Rule[]> rules = new Dictionary<string, Rule[]>();
            Add(rules, "RPCA_TriggerBanana", typeof(BananaPeel), Sender.Victim, "made slip on a banana:");
            Add(rules, "DisperseRPC", typeof(BeeSwarm), Sender.Host, "dispersed a bee swarm");
            Add(rules, "SetBeesAngryRPC", typeof(BeeSwarm), Sender.Host, "changed a bee swarm's anger");
            Add(rules, HelicopterRpc, typeof(Flare), Sender.Host, "triggered the rescue helicopter");
            Add(rules, "Extinguish_Rpc", typeof(Campfire), Sender.Host, "tried to extinguish a campfire");
            Add(rules, "RPCEndGame", typeof(Character), Sender.Host, "forced the game to end");
            Add(rules, "RPCEndGame_ForceWin", typeof(Character), Sender.Host, "forced a win");
            Add(rules, "ReviveCharacter", typeof(Character), Sender.Host, "revived", true);
            Add(rules, "RPCA_ReviveAtPosition", typeof(Character), Sender.Host, "revived", true);
            Add(rules, "RPCA_InitGhost", typeof(PlayerGhost), Sender.Owner, "took over the ghost of", true);
            Add(rules, "RPCA_SetTarget", typeof(PlayerGhost), Sender.Owner, "controlled the ghost of", true);
            Add(rules, "RPCA_SpawnEruption", typeof(EruptionSpawner), Sender.Host, "spawned a volcano eruption");
            Add(rules, "RPC_GrowVine", typeof(MagicBeanVine), Sender.Host, "grew a magic bean vine");
            Add(rules, "StartMovingRPC", typeof(OrbFogHandler), Sender.Host, "started the fog");
            Add(rules, "RPCA_SyncFog", typeof(OrbFogHandler), Sender.Host, "moved the fog");
            Add(rules, "BeginIslandLoadRPC", typeof(AirportCheckInKiosk), Sender.Host, "started the run from the airport");
            Add(rules, "BeginAirportLoadRPC", typeof(GameOverHandler), Sender.Host, "sent everyone back to the airport");
            Add(rules, "ShakeBridge_Rpc", typeof(BreakableBridge), Sender.Host, "shook a bridge");
            Add(rules, "Fall_Rpc", typeof(BreakableBridge), Sender.Host, "broke a bridge");
            Add(rules, "Detach_Rpc", typeof(Rope), Sender.Host, "detached a rope");
            Add(rules, "ShakeRock", typeof(ShakyIcicleIce), Sender.Host, "shook an icicle");
            Add(rules, "RPCA_StartBreaking", typeof(ShittyPiton), Sender.Host, "started breaking a piton");
            Add(rules, "RPCA_Break", typeof(ShittyPiton), Sender.Host, "broke a piton");
            return rules;
        }

        private static void Add(Dictionary<string, Rule[]> rules, string method, Type component, Sender allowed, string action, bool namesTarget = false) // Static: part of the one-time table build
        {
            Rule rule = new Rule(component, allowed, action, namesTarget);
            rules[method] = rules.TryGetValue(method, out Rule[] existing) ? Append(existing, rule) : new[] { rule };
        }

        private static Rule[] Append(Rule[] rules, Rule rule) // Static: part of the one-time table build
        {
            Rule[] result = new Rule[rules.Length + 1];
            rules.CopyTo(result, 0);
            result[rules.Length] = rule;
            return result;
        }

        private enum Sender
        {
            Host,
            Owner,
            Victim,
        }

        private readonly struct Rule
        {
            public Rule(Type component, Sender allowed, string action, bool namesTarget)
            {
                Component = component;
                Allowed = allowed;
                Action = action;
                NamesTarget = namesTarget;
            }

            public Type Component { get; }
            public Sender Allowed { get; }
            public string Action { get; }
            public bool NamesTarget { get; }
        }
    }
}
