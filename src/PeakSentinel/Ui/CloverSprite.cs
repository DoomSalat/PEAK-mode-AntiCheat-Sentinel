using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class CloverSprite
    {
        private const int Size = 64;
        private const int Supersample = 4;
        private const float LobeReach = 17f;
        private const float LobeSpread = 5.2f;
        private const float LobeRadius = 7.3f;
        private const float SideWidth = 1.15f;
        private const float SideCurve = 0.7f;
        private const float HeartRadius = 3.2f;
        private const float StemWidth = 6f;
        private const float Outline = 2.5f;
        private const int OutlineChecks = 12;
        private const int StemSteps = 24;

        private static readonly Vector2 Middle = new Vector2(32f, 37f); // Static: fixed clover centre
        private static readonly Vector2 StemBend = new Vector2(31f, 22f); // Static: fixed stem curve control point
        private static readonly Vector2 StemEnd = new Vector2(21f, 6.5f); // Static: fixed stem tip
        private static readonly Color Leaf = new Color(0.31f, 0.69f, 0.32f, 1f); // Static: fixed leaf green
        private static readonly Color Stem = new Color(0.26f, 0.6f, 0.27f, 1f); // Static: fixed stem green
        private static readonly Vector2[] Leaves = // Static: fixed directions of the four leaves
        {
            new Vector2(1f, 1f).normalized, new Vector2(-1f, 1f).normalized, new Vector2(-1f, -1f).normalized, new Vector2(1f, -1f).normalized,
        };

        private readonly Vector2[] _stem = new Vector2[StemSteps + 1];
        private readonly Vector2[] _ring = new Vector2[OutlineChecks];

        public CloverSprite()
        {
            for (int i = 0; i <= StemSteps; i++)
                _stem[i] = StemPoint((float)i / StemSteps);

            for (int i = 0; i < OutlineChecks; i++)
            {
                float angle = 2f * Mathf.PI * i / OutlineChecks;
                _ring[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Outline;
            }

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
            if (InLeaf(point))
                return NearEdge(point, InLeaf) ? UiPalette.Ink : Leaf;

            if (OnStem(point))
                return NearEdge(point, OnStem) ? UiPalette.Ink : Stem;

            return Color.clear;
        }

        private bool NearEdge(Vector2 point, System.Func<Vector2, bool> inside)
        {
            foreach (Vector2 step in _ring)
            {
                if (!inside(point + step))
                    return true;
            }

            return false;
        }

        private bool InLeaf(Vector2 point)
        {
            Vector2 offset = point - Middle;

            if (offset.magnitude <= HeartRadius)
                return true;

            foreach (Vector2 direction in Leaves)
            {
                float along = Vector2.Dot(offset, direction);
                float across = Vector2.Dot(offset, new Vector2(-direction.y, direction.x));

                if (new Vector2(along - LobeReach, across - LobeSpread).magnitude <= LobeRadius || new Vector2(along - LobeReach, across + LobeSpread).magnitude <= LobeRadius)
                    return true;

                if (along >= 0f && along <= LobeReach && Mathf.Abs(across) <= (LobeSpread + LobeRadius * SideWidth) * Mathf.Pow(along / LobeReach, SideCurve))
                    return true;
            }

            return false;
        }

        private bool OnStem(Vector2 point)
        {
            for (int i = 0; i < StemSteps; i++)
            {
                if (SegmentDistance(point, _stem[i], _stem[i + 1]) <= StemWidth / 2f)
                    return true;
            }

            return false;
        }

        private Vector2 StemPoint(float progress)
        {
            float rest = 1f - progress;
            return rest * rest * Middle + 2f * rest * progress * StemBend + progress * progress * StemEnd;
        }

        private float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 line = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(point - start, line) / line.sqrMagnitude);
            return (point - (start + line * along)).magnitude;
        }
    }
}
