using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class ShieldSprite
    {
        private const int Size = 64;
        private const int Supersample = 4;
        private const float Center = 32f;
        private const float HalfWidth = 22f;
        private const float Top = 58f;
        private const float Shoulder = 34f;
        private const float Tip = 4f;
        private const float Outline = 3f;
        private const float Stroke = 3.4f;

        private static readonly Color Steel = new Color(0.34f, 0.58f, 0.86f, 1f); // Static: fixed shield fill
        private static readonly Vector2 CheckStart = new Vector2(21f, 33f); // Static: fixed check mark points
        private static readonly Vector2 CheckCorner = new Vector2(29f, 24f); // Static: fixed check mark points
        private static readonly Vector2 CheckEnd = new Vector2(44f, 43f); // Static: fixed check mark points

        public ShieldSprite()
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color sum = Color.clear;

                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                            sum += Sample(new Vector2(x + (sx + 0.5f) / Supersample, y + (sy + 0.5f) / Supersample));
                    }

                    pixels[y * Size + x] = sum / (Supersample * Supersample);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            Sprite.hideFlags = HideFlags.HideAndDontSave;
        }

        public Sprite Sprite { get; }

        private Color Sample(Vector2 point)
        {
            if (!Inside(point, 0f))
                return Color.clear;

            if (!Inside(point, Outline))
                return UiPalette.Ink;

            return OnCheck(point) ? Color.white : Steel;
        }

        private bool Inside(Vector2 point, float inset)
        {
            if (point.y > Top - inset || point.y < Tip + inset)
                return false;

            float halfWidth = point.y >= Shoulder ? HalfWidth : HalfWidth * Mathf.Sqrt((point.y - Tip) / (Shoulder - Tip));
            return Mathf.Abs(point.x - Center) <= halfWidth - inset;
        }

        private bool OnCheck(Vector2 point)
        {
            return SegmentDistance(point, CheckStart, CheckCorner) < Stroke / 2f || SegmentDistance(point, CheckCorner, CheckEnd) < Stroke / 2f;
        }

        private float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 line = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(point - start, line) / line.sqrMagnitude);
            return (point - (start + line * along)).magnitude;
        }
    }
}