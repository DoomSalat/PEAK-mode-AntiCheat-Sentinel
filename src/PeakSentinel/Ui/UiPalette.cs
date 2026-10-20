using UnityEngine;

namespace PeakSentinel.Ui
{
    public static class UiPalette // Static: fixed ItemSpawnerPremium hand-drawn palette shared by the Sentinel UI
    {
        public static Color Idle { get; } = new Color(0.82f, 0.75f, 0.63f, 1f); // Static: palette
        public static Color Hover { get; } = new Color(0.9f, 0.84f, 0.74f, 1f); // Static: palette
        public static Color Selected { get; } = new Color(0.96f, 0.91f, 0.81f, 1f); // Static: palette
        public static Color PanelFill { get; } = new Color(0.76f, 0.69f, 0.56f, 1f); // Static: palette
        public static Color Ink { get; } = new Color(0.36f, 0.25f, 0.15f, 1f); // Static: palette
        public static Color InnerHighlight { get; } = new Color(0.99f, 0.94f, 0.85f, 1f); // Static: palette
        public static Color CardFill { get; } = new Color(0.89f, 0.83f, 0.73f, 1f); // Static: palette
        public static Color BarBackground { get; } = new Color(0.58f, 0.48f, 0.36f, 0.6f); // Static: palette
        public static Color BarHandle { get; } = new Color(0.8f, 0.71f, 0.58f, 1f); // Static: palette
        public static Color Text { get; } = new Color(0.28f, 0.21f, 0.14f, 1f); // Static: palette
        public static Color Suspicious { get; } = new Color(0.74f, 0.38f, 0.02f, 1f); // Static: palette
        public static Color Cheat { get; } = new Color(0.62f, 0.1f, 0.07f, 1f); // Static: palette
        public static Color Notice { get; } = new Color(0.45f, 0.37f, 0.27f, 1f); // Static: palette
        public static Color CheatBright { get; } = new Color(0.95f, 0.18f, 0.12f, 1f); // Static: palette
        public static Color SuspiciousBright { get; } = new Color(1f, 0.6f, 0.1f, 1f); // Static: palette
        public static Color Clean { get; } = new Color(0.2f, 0.42f, 0.14f, 1f); // Static: palette
    }
}
