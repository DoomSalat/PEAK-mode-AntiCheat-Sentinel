using System.Collections.Generic;
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
        private const string SelfTeleportTag = "teleports themselves";

        private static readonly HashSet<string> Methods = new HashSet<string> // Static: fixed set of RPC names that can harm another player
        {
            RpcNames.Die, RpcNames.SetDead, RpcNames.PassOut, RpcNames.Zombify, RpcNames.Petrify, RpcNames.SyncPetrify, RpcNames.Warp, RpcNames.ForceAtPosition, RpcNames.ForceToBodyPart, RpcNames.RocketExplode,
        };

        private readonly ItemLocator _locator;
        private readonly LegitForces _forces;
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public HarmAudit(SuspicionReport report, DetectionSettings detection, ItemLocator locator, LegitForces forces)
        {
            _forces = forces;
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
                return _report.Refuse(call.Sender, FindingCategory.Cheats, InfinityTag, $"warped {PlayerNames.Name(call.Owner)} to an invalid position {target} (black screen), always blocked", true);

            if (call.Method == RpcNames.Warp && _detection.HarmToOthers.Value && call.FromOwner && !call.FromHost && !call.FromLocal && call.Argument(0) is Vector3 destination)
                return CheckSelfWarp(call, destination);

            if (!_detection.HarmToOthers.Value || call.Sender == null || call.Owner == null || call.FromOwner || call.FromHost || call.FromLocal)
                return true;

            string victim = PlayerNames.Name(call.Owner);
            Character character = call.Component<Character>();

            switch (call.Method)
            {
                case RpcNames.Die:
                    if (IsCannibalism(call, character))
                    {
                        _report.Notice(call.Sender, FindingCategory.Cheats, $"ate {victim} (cannibalism is on for them)");
                        return true;
                    }

                    return Refuse(call, KillsTag, $"killed {victim}", Revive(character, victim));

                case RpcNames.SetDead:
                    return Refuse(call, KillsTag, $"killed {victim}", Revive(character, victim));

                case RpcNames.PassOut:
                    return Refuse(call, KnocksOutTag, $"knocked out {victim}", WakeUp(character, victim));

                case RpcNames.Zombify:
                    return Refuse(call, CursesTag, $"zombified {victim}", null);

                case RpcNames.Petrify:
                    return Refuse(call, CursesTag, $"petrified {victim}", null);

                case RpcNames.SyncPetrify:
                    return Refuse(call, CursesTag, $"set the petrification of {victim} to {call.Argument(0)}%", Unpetrify(character, victim));

                case RpcNames.Warp:
                    return Refuse(call, TeleportsTag, $"teleported {victim} to {call.Argument(0)}", Return(character, victim));

                case RpcNames.RocketExplode:
                    return Refuse(call, FlingsTag, $"set off an explosion on {victim}", Return(character, victim));

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

            bool block = _detection.BlockHarmToOthers.Value;
            Vector3 place = self.Center;
            string action = $"teleported themselves {WhereTo(self, destination)} with no warp item";

            if (block && PhotonNetwork.IsMasterClient)
            {
                self.photonView.RPC(RpcNames.Warp, RpcTarget.All, place, false);
                action += ", sent back for everyone";
            }

            return _report.Refuse(call.Sender, FindingCategory.Cheats, SelfTeleportTag, action, block, Return(self, PlayerNames.Name(call.Sender)));
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
            return force < FlingForce || (character != null && _forces.Explains(call.Sender, character)) || Refuse(call, FlingsTag, $"flung {victim} with force {force:F0}", Return(character, victim));
        }

        private bool Refuse(RpcCall call, string tag, string action, Repair repair)
        {
            return _report.Refuse(call.Sender, FindingCategory.Cheats, tag, action, _detection.BlockHarmToOthers.Value, repair);
        }

        private Repair Revive(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Repair($"revived {victim} where they died", () => character.photonView.RPC(RpcNames.ReviveAtPosition, RpcTarget.All, place, false, -1));
        }

        private Repair Unpetrify(Character character, string victim)
        {
            if (character == null)
                return null;

            int amount = character.data.petrifyAmount;
            return new Repair($"returned the petrification of {victim} to {amount}%", () => character.photonView.RPC(RpcNames.SyncPetrify, RpcTarget.All, amount));
        }

        private Repair WakeUp(Character character, string victim)
        {
            return character == null ? null : new Repair($"woke {victim} up", () => character.photonView.RPC(RpcNames.UnPassOut, RpcTarget.All));
        }

        private Repair Return(Character character, string victim)
        {
            if (character == null)
                return null;

            Vector3 place = character.Center;
            return new Repair($"returned {victim} to where they stood", () => character.photonView.RPC(RpcNames.Warp, RpcTarget.All, place, true));
        }
    }
}