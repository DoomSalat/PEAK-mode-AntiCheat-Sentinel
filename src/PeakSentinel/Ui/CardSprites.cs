using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class CardSprites
    {
        private const int Size = 64;
        private const int Supersample = 4;
        private const float PixelsPerUnit = 100f;
        private const float PanelRadius = 16f;
        private const float CardRadius = 9f;
        private const float ButtonRadius = 10f;
        private const float BoxRadius = 8f;
        private const float MarkRadius = 6f;
        private const float BarRadius = 6f;
        private const float PanelOutline = 2.5f;
        private const float CardOutline = 1.5f;
        private const float InnerShare = 0.6f;
        private const float MaxBorder = 31f;

        public CardSprites()
        {
            Panel = Create(PanelRadius, PanelOutline, UiPalette.PanelFill);
            Card = Create(CardRadius, CardOutline, UiPalette.CardFill);
            CardHover = Create(CardRadius, CardOutline, UiPalette.Selected);
            ButtonIdle = Create(ButtonRadius, CardOutline, UiPalette.Idle);
            ButtonHover = Create(ButtonRadius, CardOutline, UiPalette.Hover);
            ButtonSelected = Create(ButtonRadius, CardOutline, UiPalette.Selected);
            Box = Create(BoxRadius, CardOutline, UiPalette.CardFill);
            Mark = Create(MarkRadius, 0f, UiPalette.Ink);
            BarBackground = Create(BarRadius, 0f, UiPalette.BarBackground);
            BarHandle = Create(BarRadius, 0f, UiPalette.BarHandle);
        }

        public Sprite Panel { get; }
        public Sprite Card { get; }
        public Sprite CardHover { get; }
        public Sprite ButtonIdle { get; }
        public Sprite ButtonHover { get; }
        public Sprite ButtonSelected { get; }
        public Sprite Box { get; }
        public Sprite Mark { get; }
        public Sprite BarBackground { get; }
        public Sprite BarHandle { get; }

        private Sprite Create(float radius, float outline, Color fill)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[Size * Size];
            float half = Size / 2f;
            float inner = outline * InnerShare;
            float edge = half - (outline + 1f) - radius;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = Pixel(x, y, half, edge, radius, outline, inner, fill);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            float border = Mathf.Min(outline + 1f + radius + outline + inner + 1f, MaxBorder);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Color Pixel(int x, int y, float half, float edge, float radius, float outline, float inner, Color fill)
        {
            Color sum = Color.clear;

            for (int sy = 0; sy < Supersample; sy++)
            {
                for (int sx = 0; sx < Supersample; sx++)
                {
                    Vector2 offset = new Vector2(Mathf.Abs(x + (sx + 0.5f) / Supersample - half) - edge, Mathf.Abs(y + (sy + 0.5f) / Supersample - half) - edge);
                    float distance = new Vector2(Mathf.Max(offset.x, 0f), Mathf.Max(offset.y, 0f)).magnitude + Mathf.Min(Mathf.Max(offset.x, offset.y), 0f) - radius;
                    sum += Sample(distance, outline, inner, fill);
                }
            }

            return sum / (Supersample * Supersample);
        }

        private Color Sample(float distance, float outline, float inner, Color fill)
        {
            if (distance < -inner)
                return fill;

            if (distance < 0f)
                return outline > 0f ? UiPalette.InnerHighlight : fill;

            if (distance < outline)
                return UiPalette.Ink;

            Color edge = outline > 0f ? UiPalette.Ink : fill;
            return new Color(edge.r, edge.g, edge.b, 0f);
        }
    }
}
