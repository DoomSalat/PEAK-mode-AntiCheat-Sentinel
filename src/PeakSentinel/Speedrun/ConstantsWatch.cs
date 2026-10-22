using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class ConstantsWatch
    {
        private const string CodeFormat = "X8";
        private const string Changed = "movement constants changed while playing (gravity, run, jump or air control)";
        private const uint HashSeed = 2166136261;
        private const uint HashPrime = 16777619;

        private Character _character;
        private string _first;

        public string Code { get; private set; } = string.Empty;

        public string Check()
        {
            Character local = Character.localCharacter;

            if (local == null)
                return null;

            CharacterMovement movement = local.refs.movement;
            Vector3 gravity = Physics.gravity;
            uint hash = HashSeed;

            foreach (float value in new[] { movement.movementForce, movement.sprintMultiplier, movement.jumpImpulse, movement.jumpGravity, movement.maxGravity, movement.standForce, movement.airMovementTurnSpeed, gravity.x, gravity.y, gravity.z })
                hash = (hash ^ (uint)value.GetHashCode()) * HashPrime;

            Code = hash.ToString(CodeFormat);

            if (local != _character)
            {
                _character = local;
                _first = Code;
            }

            return Code != _first ? Changed : null;
        }
    }
}
