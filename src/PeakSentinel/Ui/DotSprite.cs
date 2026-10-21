using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class DotSprite
    {
        private const int Size = 32;
        private const int Supersample = 4;
        private const float Center = 16f;
        private const float Radius = 14f;

        public DotSprite()
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int inside = 0;

                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            Vector2 point = new Vector2(x + (sx + 0.5f) / Supersample, y + (sy + 0.5f) / Supersample);

                            if ((point - new Vector2(Center, Center)).sqrMagnitude <= Radius * Radius)
                                inside++;
                        }
                    }

                    pixels[y * Size + x] = new Color(1f, 1f, 1f, inside / (float)(Supersample * Supersample));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            Sprite.hideFlags = HideFlags.HideAndDontSave;
        }

        public Sprite Sprite { get; }
    }
}