using TMPro;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class PlayerRow
    {
        public PlayerRow(TextMeshProUGUI name, Image face, TextMeshProUGUI problems, TextMeshProUGUI ping)
        {
            Name = name;
            Face = face;
            Problems = problems;
            Ping = ping;
        }

        public TextMeshProUGUI Name { get; }
        public Image Face { get; }
        public TextMeshProUGUI Problems { get; }
        public TextMeshProUGUI Ping { get; }
    }
}
