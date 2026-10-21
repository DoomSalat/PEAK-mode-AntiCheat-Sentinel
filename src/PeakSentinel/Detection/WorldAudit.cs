using System;
using System.Collections.Generic;
using PeakSentinel.Game;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace PeakSentinel.Detection
{
    public sealed class WorldAudit : IRpcAudit
    {
        private const string WorldTag = "breaks host-only game events";
        private const int FlareLitKey = 3;
        private const float PeakCheckOffset = 10f;
        private const float StandingReach = 15f;

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

        public bool Inspect(RpcCall call)
        {
            if (!_detection.HostOnlyEvents.Value || call.Sender == null || call.View == null || call.FromLocal)
                return true;

            foreach (Rule rule in Rules[call.Method])
            {
                if (call.View.GetComponent(rule.Component) == null)
                    continue;

                return IsAllowed(rule, call) || _report.Refuse(call.Sender, FindingCategory.Cheats, WorldTag, Describe(rule, call), _detection.BlockHostOnlyEvents.Value, RepairFor(call));
            }

            return true;
        }

        private bool IsAllowed(Rule rule, RpcCall call)
        {
            switch (rule.Allowed)
            {
                case Sender.Host:
                    return call.FromHost || (call.Method == RpcNames.TriggerHelicopter && IsHelicopterLegit(call.View));

                case Sender.Owner:
                    return call.FromOwner || call.FromHost;

                case Sender.Standing:
                    return call.FromHost || StandsNear(call);

                default:
                    PhotonView victim = call.ViewAt(0);
                    return victim == null || call.SentBy(victim.Owner);
            }
        }

        private string Describe(Rule rule, RpcCall call)
        {
            if (call.Method == RpcNames.TriggerBanana)
                return $"{rule.Action} {PlayerNames.Name(call.ViewAt(0)?.Owner)}";

            return rule.NamesTarget ? $"{rule.Action} {PlayerNames.Name(call.Owner)}" : rule.Action;
        }

        private Repair RepairFor(RpcCall call)
        {
            switch (call.Method)
            {
                case RpcNames.ExtinguishCampfire:
                    return Relight(call.View, call.Component<Campfire>());

                case RpcNames.SyncDayTime:
                    return RestoreDayTime(call.View, call.Component<DayNightManager>());

                case RpcNames.SyncRunTime:
                    return RestoreRunTime(call.View, call.Component<RunManager>());

                case RpcNames.SyncAscent:
                    return RestoreAscent(call.View);

                case RpcNames.InitFog:
                case RpcNames.StartFog:
                case RpcNames.SyncFog:
                    return RestoreFog(call.View, call.Component<OrbFogHandler>());

                default:
                    return null;
            }
        }

        private Repair Relight(PhotonView view, Campfire campfire)
        {
            if (campfire == null || campfire.state != Campfire.FireState.Lit || campfire.IsMiniRunEndingCampfire())
                return null;

            float burning = campfire.beenBurningFor;
            return new Repair("relit the campfire", () => view.RPC(RpcNames.LightCampfire, RpcTarget.All, false, burning));
        }

        private Repair RestoreFog(PhotonView view, OrbFogHandler fog)
        {
            if (fog == null)
                return null;

            float size = fog.currentSize;
            bool moving = fog.isMoving;
            return new Repair("put the fog back", () => view.RPC(RpcNames.SyncFog, RpcTarget.All, size, moving));
        }

        private Repair RestoreDayTime(PhotonView view, DayNightManager manager)
        {
            if (manager == null)
                return null;

            int day = manager.dayCount;
            float time = manager.timeOfDay;
            return new Repair($"put the time of day back to day {day}, {time:F1} h", () => view.RPC(RpcNames.SyncDayTime, RpcTarget.All, day, time));
        }

        private Repair RestoreRunTime(PhotonView view, RunManager manager)
        {
            if (manager == null)
                return null;

            float time = manager.TimeSinceRunStarted;
            bool active = GameInternals.IsTimerActive(manager);
            return new Repair($"put the run timer back to {time:F0} s", () => view.RPC(RpcNames.SyncRunTime, RpcTarget.All, time, active));
        }

        private Repair RestoreAscent(PhotonView view)
        {
            int ascent = Ascents.currentAscent;
            return new Repair($"put the ascent back to {ascent}", () => view.RPC(RpcNames.SyncAscent, RpcTarget.All, ascent));
        }

        private bool StandsNear(RpcCall call)
        {
            Character standing = PlayerHandler.GetPlayerCharacter(call.Sender);
            return standing != null && (standing.Center - call.View.transform.position).sqrMagnitude <= StandingReach * StandingReach;
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
            Add(rules, RpcNames.TriggerBanana, typeof(BananaPeel), Sender.Victim, "made slip on a banana:");
            Add(rules, RpcNames.DisperseBees, typeof(BeeSwarm), Sender.Host, "dispersed a bee swarm");
            Add(rules, RpcNames.AngerBees, typeof(BeeSwarm), Sender.Host, "changed a bee swarm's anger");
            Add(rules, RpcNames.TriggerHelicopter, typeof(Flare), Sender.Host, "triggered the rescue helicopter");
            Add(rules, RpcNames.LightFlare, typeof(Flare), Sender.Owner, "lit the flare of", true);
            Add(rules, RpcNames.ExtinguishCampfire, typeof(Campfire), Sender.Host, "tried to extinguish a campfire");
            Add(rules, RpcNames.EndGame, typeof(Character), Sender.Host, "forced the game to end");
            Add(rules, RpcNames.ForceWin, typeof(Character), Sender.Host, "forced a win");
            Add(rules, RpcNames.InitGhost, typeof(PlayerGhost), Sender.Owner, "took over the ghost of", true);
            Add(rules, RpcNames.GhostTarget, typeof(PlayerGhost), Sender.Owner, "controlled the ghost of", true);
            Add(rules, RpcNames.SpawnEruption, typeof(EruptionSpawner), Sender.Host, "spawned a volcano eruption");
            Add(rules, RpcNames.GrowVine, typeof(MagicBeanVine), Sender.Host, "grew a magic bean vine");
            Add(rules, RpcNames.StartFog, typeof(OrbFogHandler), Sender.Host, "started the fog");
            Add(rules, RpcNames.SyncFog, typeof(OrbFogHandler), Sender.Host, "moved the fog");
            Add(rules, RpcNames.StartIsland, typeof(AirportCheckInKiosk), Sender.Host, "started the run from the airport");
            Add(rules, RpcNames.BackToAirport, typeof(GameOverHandler), Sender.Host, "sent everyone back to the airport");
            Add(rules, RpcNames.ShakeBridge, typeof(BreakableBridge), Sender.Host, "shook a bridge");
            Add(rules, RpcNames.BreakBridge, typeof(BreakableBridge), Sender.Host, "broke a bridge");
            Add(rules, RpcNames.DetachRope, typeof(Rope), Sender.Owner, "detached a rope");
            Add(rules, RpcNames.ShakeRock, typeof(ShakyIcicleIce), Sender.Standing, "shook an icicle from afar");
            Add(rules, RpcNames.ShakeRock, typeof(ShakyRock), Sender.Standing, "shook a rock from afar");
            Add(rules, RpcNames.StartBreakingPiton, typeof(ShittyPiton), Sender.Owner, "started breaking a piton of", true);
            Add(rules, RpcNames.BreakPiton, typeof(ShittyPiton), Sender.Owner, "broke a piton of", true);
            Add(rules, RpcNames.SyncDayTime, typeof(DayNightManager), Sender.Host, "changed the time of day");
            Add(rules, RpcNames.SyncAscent, typeof(GameUtils), Sender.Host, "changed the ascent (difficulty)");
            Add(rules, RpcNames.SyncRunSettings, typeof(GameUtils), Sender.Host, "changed the run settings");
            Add(rules, RpcNames.SpawnResource, typeof(GameUtils), Sender.Host, "spawned a resource");
            Add(rules, RpcNames.SyncLava, typeof(LavaRising), Sender.Host, "moved the lava");
            Add(rules, RpcNames.InitFog, typeof(OrbFogHandler), Sender.Host, "reset the fog");
            Add(rules, RpcNames.SyncRunTime, typeof(RunManager), Sender.Host, "changed the run timer");
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
            Standing,
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
