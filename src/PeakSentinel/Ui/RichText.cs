using UnityEngine;

namespace PeakSentinel.Ui
{
    public static class RichText // Static: pure string formatting for TextMeshPro labels
    {
        public static string Warning(string text)
        {
            return Paint(text, UiPalette.Suspicious);
        }

        public static string Paint(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
    }
}