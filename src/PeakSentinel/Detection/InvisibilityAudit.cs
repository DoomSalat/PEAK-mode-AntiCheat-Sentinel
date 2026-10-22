using System.Collections.Generic;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Unity.Mathematics;

namespace PeakSentinel.Detection
{
    public sealed class InvisibilityAudit
    {
        private const float HiddenHeight = -5000f;
        private const float HipHeight = 1f;
        private const string Tag = "turns invisible";

        private readonly HashSet<int> _hidden = new HashSet<int>();
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;

        public InvisibilityAudit(SuspicionReport report, DetectionSettings detection)
        {
            _report = report;
            _detection = detection;
        }

        public void Reveal(Character character, ref CharacterSyncData data)
        {
            if (character == null || character.IsLocal || character.isBot)
                return;

            int actor = character.photonView.Owner.ActorNumber;

            if (data.hipLocation.y >= HiddenHeight)
            {
                _hidden.Remove(actor);
                return;
            }

            if (!_detection.Flight.Value)
                return;

            float sentHeight = data.hipLocation.y;
            data.hipLocation = data.isClimbing ? data.climbPos : data.groundPos + new float3(0f, HipHeight, 0f);

            if (!_hidden.Add(actor))
                return;

            _report.Tag(character.photonView.Owner, Tag);
            _report.Flag(character.photonView.Owner, FindingCategory.Cheats, $"turned invisible: sent their position as {sentHeight:F0} m below the map (Admin_menukz invisibility), shown where they stand instead", true);
        }
    }
}
