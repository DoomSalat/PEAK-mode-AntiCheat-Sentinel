using System.Collections.Generic;
using PeakSentinel.Compat;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Detection
{
    public sealed class HarmAudit : IRpcAudit
    {
        private const float FlingForce = 1500f;
        private const string KillsTag = "kills other players";
        private const string KnocksOutTag = "knocks out other players";
        private const string CursesTag = "zombifies or petrifies other players";
        private const string TeleportsTag = "teleports other players";
        private const string FlingsTag = "flings other players";
        private const string InfinityTag = "warps players to infinity";
        private const float MaxWorldCoordinate = 100000f;
        private const float NextToPlayer = 3f;
        private const float WarpItemReach = 6f;
        private const float EatReach = 6f;
        private const float HungerMargin = 0.05f;
        private const float ZombieSpores = 0.4f;
        private const string SelfTeleportTag = "teleports themselves";

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of RPC names that can harm another player
        {
            RpcNames.Die, RpcNames.SetDead, RpcNames.PassOut, RpcNames.Zombify, RpcNames.Petrify, RpcNames.SyncPetrify, RpcNames.Warp, RpcNames.ForceAtPosition, RpcNames.ForceToBodyPart, RpcNames.RocketExplode,
        };

        private readonly ItemLocator _locator;
        private readonly LegitForces _forces;
        private readonly CookingEchoes _cooking;
        private readonly TrueAscentState _ascent;
        private readonly CheatKills _kills;
        private readonly SuspicionReport _report;
        private readonly VictimGuard _guard;
        private readonly DetectionSettings _detection;

        public HarmAudit(SuspicionReport report, VictimGuard guard, DetectionSettings detection, ItemLocator locator, LegitForces forces, CookingEchoes cooking, TrueAscentState ascent, CheatKills kills)
        {
            _guard = guard;
            _kills = kills;
            _ascent = ascent;
            _forces = forces;
            _cooking = cooking;
            _locator = locator;
            _report = report;
            _detection = detection;
        }

        public bool Watches(string method)
        {
            return Methods.Contains(method);
        }

        public bool Inspect(RpcCall call)
        {
            if (call.Method == RpcNames.Warp && call.Sender != null && !call.FromLocal && call.Argument(0) is Vector3 target && !IsSane(target))
                return _guard.Refuse(call.Sender, call.Component<Character>(), FindingCategory.Cheats, InfinityTag, $"warped {PlayerNames.Name(call.Owner)} to an invalid position {target} (black screen), always blocked", true, PhotonNetwork.IsMasterClient ? Return(call.Component<Character>(), PlayerNames.Name(call.Owner)) : null); // A client still drops it: an invalid position breaks physics on every screen

            if (call.Method == RpcNames.Warp && _detection.HarmToOthers.Value && call.FromOwner && !call.FromHost && !call.FromLocal && call.Argument(0) is Vector3 destination)
                return CheckSelfWarp(call, destination);

            if (call.Method == RpcNames.Zombify && _detection.HarmToOthers.Value && call.FromOwner && !call.FromHost && !call.FromLocal)
                return CheckSelfZombify(call);

            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || call.FromLocal)
                return true;

            string victim = PlayerNames.Name(call.Owner);
            Character character = call.Component<Character>();

            switch (call.Method)
            {
                case RpcNames.Die:
                    if (IsDeathEcho(character))
                        return true;

                    if (IsCannibalism(call, character))
                    {
                        _report.Notice(call.Sender, FindingCategory.Cheats, $"ate {victim} (cannibalism is on for them)");
                        return true;
                    }

                    return RefuseKill(call, character, victim);

                case RpcNames.SetDead:
                    return RefuseKill(call, character, victim);

                case RpcNames.PassOut:
                    return Refuse(call, character, KnocksOutTag, $"knocked out {victim}", WakeUp(character, victim));

                case RpcNames.Zombify:
                    return Refuse(call, character, CursesTag, $"zombified {victim}", null);

                case RpcNames.Petrify:
                    return Refuse(call, character, CursesTag, $"petrified {victim}", null);

                case RpcNames.SyncPetrify:
                    return Refuse(call, character, CursesTag, $"set the petrification of {victim} to {call.Argument(0)}%", Unpetrify(character, victim));

                case RpcNames.Warp:
                    if (_cooking.Explains<Action_WarpToRandomPlayer>(character))
                        return true;

                    return Refuse(call, character, TeleportsTag, $"teleported {victim} to {call.Argument(0)}", Return(character, victim));

                case RpcNames.RocketExplode:
                    return Refuse(call, character, FlingsTag, $"set off an explosion on {victim}", Return(character, victim));

                case RpcNames.ForceAtPosition:
                    return CheckFling(call, victim, call.MagnitudeAt(0), character);

                default:
                    return CheckFling(call, victim, Mathf.Max(call.MagnitudeAt(1), call.MagnitudeAt(2)), character);
            }
        }

        private bool CheckSelfWarp(RpcCall call, Vector3 destination)
        {
            Character self = call.Component<Character>();

            if (self == null || (self.data.currentItem != null && self.data.currentItem.TryGetComponent(out Action_WarpToRandomPlayer _)) || _locator.OwnedNear<Peak.WarpOnThrow>(call.Sender, destination, WarpItemReach))
                return true;

            string landing = _ascent.LandingCause(self);

            if (landing != null)
            {
                _report.Notice(call.Sender, FindingCategory.Cheats, $"teleported themselves {WhereTo(self, destination)} ({landing})");
                return true;
            }

            bool block = _detection.BlockHarmToOthers.Value;
            Vector3 place = self.Center;
            string action = $"teleported themselves {WhereTo(self, destination)} with no warp item";

            if (block && PhotonNetwork.IsMasterClient)
            {
                self.photonView.RPC(RpcNames.Warp, RpcTarget.All, place, false);
                action += ", sent back for everyone";
            }

            return _report.Refuse(call.Sender, FindingCategory.Cheats, SelfTeleportTag, action, block, Return(self, PlayerNames.Name(call.Sender))?.AsRepair());
        }

        private bool IsDeathEcho(Character victim)
        {
            if (victim == null)
                return false;

            Item held = victim.data.currentItem;
            return victim.data.dead || (held != null && held.TryGetComponent(out Action_Die _)) || _cooking.Explains<Action_Die>(victim);
        }

        private bool CheckSelfZombify(RpcCall call)
        {
            Character self = call.Component<Character>();

            if (self == null || self.data.zombified || (Ascents.shouldSpawnZombie && self.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Spores) >= ZombieSpores))
                return true;

            return _report.Refuse(call.Sender, FindingCategory.Cheats, CursesTag, "turned themselves into a zombie with no spores (console command or cheat)", _detection.BlockHarmToOthers.Value);
        }

        private bool IsCannibalism(RpcCall call, Character victim)
        {
            Character eater = PlayerHandler.GetPlayerCharacter(call.Sender);

            if (victim == null || eater == null || !victim.data.cannibalismPermitted || !eater.data.fullyConscious)
                return false;

            bool hungry = eater.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger) >= UIPlayerNames.CANNIBAL_HUNGER_THRESHOLD - HungerMargin;
            return hungry && (eater.Center - victim.Center).sqrMagnitude <= EatReach * EatReach;
        }

        private string WhereTo(Character self, Vector3 destination)
        {
            foreach (Character other in Character.AllCharacters)
            {
                if (other != null && other != self && !other.isBot && (other.Center - destination).sqrMagnitude <= NextToPlayer * NextToPlayer)
                    return $"next to {PlayerNames.Name(other.photonView.Owner)}";
            }

            return $"to {destination}";
        }

        private bool IsSane(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) && Mathf.Abs(position.x) < MaxWorldCoordinate && Mathf.Abs(position.y) < MaxWorldCoordinate && Mathf.Abs(position.z) < MaxWorldCoordinate;
        }

        private bool CheckFling(RpcCall call, string victim, float force, Character character)
        {
            return force < FlingForce || (character != null && _forces.Explains(call.Sender, character)) || _guard.Refuse(call.Sender, character, FindingCategory.Cheats, FlingsTag, $"flung {victim} with force {force:F0}", _detection.BlockHarmToOthers.Value, null, Return(character, victim)?.AsRepair());
        }

        private bool Refuse(RpcCall call, Character character, string tag, string action, Recovery recovery)
        {
            return _guard.Refuse(call.Sender, character, FindingCategory.Cheats, tag, action, _detection.BlockHarmToOthers.Value, recovery);
        }

        private bool RefuseKill(RpcCall call, Character character, string victim)
        {
            _kills.Note(character);
            return Refuse(call, character, KillsTag, $"killed {victim}", Revive(character, victim));
        }

        private Recovery Revive(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Recovery($"revived {victim} where they stood", target => character.photonView.RPC(RpcNames.ReviveAtPosition, target, place, false, -1), true);
        }

        private Recovery Unpetrify(Character character, string victim)
        {
            if (character == null)
                return null;

            int amount = character.data.petrifyAmount;
            return new Recovery($"returned the petrification of {victim} to {amount}%", target => character.photonView.RPC(RpcNames.SyncPetrify, target, amount), true);
        }

        private Recovery WakeUp(Character character, string victim)
        {
            return character == null ? null : new Recovery($"woke {victim} up", target => character.photonView.RPC(RpcNames.UnPassOut, target), true);
        }

        private Recovery Return(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Recovery($"returned {victim} to where they stood", target => character.photonView.RPC(RpcNames.Warp, target, place, true), false);
        }
    }
}