using TMPro;

namespace PeakSentinel.Ui
{
    public static class GameFonts // Static: one lookup of the game's TextMeshPro font shared by every Sentinel label
    {
        public static TMP_FontAsset Main()
        {
            FontFallbackSwapper swapper = FontFallbackSwapper.instance;
            return swapper != null && swapper.mainBaseFont != null ? swapper.mainBaseFont : TMP_Settings.defaultFontAsset;
        }
    }
}