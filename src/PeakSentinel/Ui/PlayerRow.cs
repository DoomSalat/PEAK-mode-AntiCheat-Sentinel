using TMPro;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class PlayerRow
    {
        public PlayerRow(TextMeshProUGUI name, Image face, TextMeshProUGUI life, TextMeshProUGUI problems, TextMeshProUGUI ping)
        {
            Life = life;
            Name = name;
            Face = face;
            Problems = problems;
            Ping = ping;
        }

        public TextMeshProUGUI Name { get; }
        public Image Face { get; }
        public TextMeshProUGUI Life { get; }
        public TextMeshProUGUI Problems { get; }
        public TextMeshProUGUI Ping { get; }
    }
}
