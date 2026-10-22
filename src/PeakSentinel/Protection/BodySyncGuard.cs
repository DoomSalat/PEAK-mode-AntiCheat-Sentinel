using System.Collections.Generic;
using PeakSentinel.Reporting;
using Unity.Mathematics;

namespace PeakSentinel.Protection
{
    public sealed class BodySyncGuard
    {
        private const string Tag = "sends broken data (crash attempt)";

        private readonly HashSet<int> _broken = new HashSet<int>();
        private readonly SuspicionReport _report;

        public BodySyncGuard(SuspicionReport report)
        {
            _report = report;
        }

        public bool Repair(Character character, ref CharacterSyncData data)
        {
            if (character == null || character.IsLocal)
                return false;

            int actor = character.photonView.Owner.ActorNumber;

            if (IsFinite(data))
            {
                _broken.Remove(actor);
                return false;
            }

            float3 here = character.GetBodypart(BodypartType.Hip).Rig.position;
            data.hipLocation = Finite(data.hipLocation) ? data.hipLocation : here;
            data.groundPos = Finite(data.groundPos) ? data.groundPos : here;
            data.climbPos = Finite(data.climbPos) ? data.climbPos : here;
            data.averageVelocity = Finite(data.averageVelocity) ? data.averageVelocity : float3.zero;
            data.groundNormal = Finite(data.groundNormal) ? data.groundNormal : new float3(0f, 1f, 0f);
            data.lookValues = math.all(math.isfinite(data.lookValues)) ? data.lookValues : float2.zero;

            if (_broken.Add(actor))
                _report.Block(character.photonView.Owner, FindingCategory.Cheats, Tag, "sent a broken body position (NaN or infinity, it breaks physics on every screen), kept their body where it was");

            return true;
        }

        private bool IsFinite(CharacterSyncData data)
        {
            return Finite(data.hipLocation) && Finite(data.groundPos) && Finite(data.climbPos) && Finite(data.averageVelocity) && Finite(data.groundNormal) && math.all(math.isfinite(data.lookValues));
        }

        private bool Finite(float3 value)
        {
            return math.all(math.isfinite(value));
        }
    }
}
