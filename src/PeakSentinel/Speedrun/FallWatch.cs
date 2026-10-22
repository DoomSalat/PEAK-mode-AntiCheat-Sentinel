using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class FallWatch
    {
        private const float MinFactor = 0.05f;
        private const float MinExpected = 0.1f;
        private const float ExpectedShare = 0.5f;
        private const float SettleSeconds = 0.6f;
        private const float BounceSeconds = 2.5f;
        private const string NoDamageFormat = "landed at {0:F0} m/s after {1:F1} s of falling without fall damage (the game should add about {2:P0} injury)";

        private bool _airborne;
        private float _expected;
        private float _speed;
        private float _fallTime;
        private float _injuryBefore;
        private float _checkAt = -1f;

        public string Check(Character local)
        {
            if (local == null)
                return null;

            if (_checkAt >= 0f && Time.time >= _checkAt)
                return Settle(local);

            if (!local.data.isGrounded)
            {
                Track(local);
                return null;
            }

            if (_airborne)
                Land(local);

            return null;
        }

        private void Track(Character local)
        {
            CharacterMovement movement = local.refs.movement;
            _airborne = true;
            _fallTime = movement.FallTime();
            _speed = -local.data.avarageLastFrameVelocity.y;
            _expected = _fallTime > movement.fallDamageTime ? Mathf.Min(Mathf.Max(movement.FallFactor(), MinFactor), movement.MaxVelDmg()) * Ascents.fallDamageMultiplier / Ascents.etcDamageMultiplier : 0f;
            _injuryBefore = local.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury);
        }

        private void Land(Character local)
        {
            _airborne = false;

            if (_expected >= MinExpected && _injuryBefore + _expected * ExpectedShare < 1f && !IsShielded(local))
                _checkAt = Time.time + SettleSeconds;
        }

        private string Settle(Character local)
        {
            _checkAt = -1f;
            float rise = local.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) - _injuryBefore;
            return rise < _expected * ExpectedShare && !IsShielded(local) ? string.Format(NoDamageFormat, _speed, _fallTime, _expected) : null;
        }

        private bool IsShielded(Character local)
        {
            CharacterData data = local.data;
            return data.dead || data.isInvincible || local.statusesLocked || local.inAirport || data.lastBouncedTime + BounceSeconds > Time.time || Time.time < local.refs.movement.fallDamageCapEndTime;
        }
    }
}
