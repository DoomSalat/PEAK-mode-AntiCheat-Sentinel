using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Reporting
{
    public sealed class VersionBadge : MonoBehaviour
    {
        private const float Width = 400f;
        private const float Height = 24f;
        private const float TopMargin = 6f;
        private const int FontSize = 16;

        private static readonly Color TextColor = new Color(1f, 0.85f, 0.4f, 0.9f); // Static: fixed badge color

        private string _text;
        private GUIStyle _style;

        public void Initialize(string text)
        {
            _text = text;
        }

        private void OnGUI()
        {
            if (!PhotonNetwork.InRoom)
                return;

            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = FontSize, fontStyle = FontStyle.Bold, normal = { textColor = TextColor } };

            GUI.Label(new Rect((Screen.width - Width) / 2f, TopMargin, Width, Height), _text, _style);
        }
    }
}
