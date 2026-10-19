using PeakSentinel.Reporting;
using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class FaceSprites
    {
        private const int Size = 64;
        private const int Supersample = 4;
        private const float Center = 32f;
        private const float Radius = 28f;
        private const float Outline = 2.5f;
        private const float Stroke = 2.6f;
        private const float EyeRadius = 4f;
        private const float EyeOffsetX = 10f;
        private const float EyeHeight = 38f;
        private const float MouthRadius = 13f;
        private const float MouthHalfWidth = 10f;
        private const float SmileCenter = 30f;
        private const float SmileCut = 26f;
        private const float FrownCenter = 8f;
        private const float FrownCut = 14f;
        private const float FlatMouthHeight = 20f;
        private const float BrowOuter = 17f;
        private const float BrowInner = 5f;
        private const float BrowOuterHeight = 49f;
        private const float BrowInnerHeight = 44f;

        private static readonly Color Happy = new Color(0.56f, 0.8f, 0.45f, 1f); // Static: fixed face colors
        private static readonly Color Worried = new Color(0.96f, 0.72f, 0.32f, 1f); // Static: fixed face colors
        private static readonly Color Angry = new Color(0.9f, 0.36f, 0.3f, 1f); // Static: fixed face colors

        public FaceSprites()
        {
            Clean = Create(PlayerStanding.Clean, Happy);
            Suspect = Create(PlayerStanding.Suspect, Worried);
            Cheater = Create(PlayerStanding.Cheater, Angry);
        }

        public Sprite Clean { get; }
        public Sprite Suspect { get; }
        public Sprite Cheater { get; }

        public Sprite Of(PlayerStanding standing)
        {
            switch (standing)
            {
                case PlayerStanding.Cheater:
                    return Cheater;

                case PlayerStanding.Suspect:
                    return Suspect;

                default:
                    return Clean;
            }
        }

        private Sprite Create(PlayerStanding standing, Color fill)
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
                            sum += Sample(new Vector2(x + (sx + 0.5f) / Supersample, y + (sy + 0.5f) / Supersample), standing, fill);
                    }

                    pixels[y * Size + x] = sum / (Supersample * Supersample);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Color Sample(Vector2 point, PlayerStanding standing, Color fill)
        {
            float distance = (point - new Vector2(Center, Center)).magnitude;

            if (distance > Radius)
                return Color.clear;

            if (distance > Radius - Outline || IsFeature(point, standing))
                return UiPalette.Ink;

            return fill;
        }

        private bool IsFeature(Vector2 point, PlayerStanding standing)
        {
            bool eye = (point - new Vector2(Center - EyeOffsetX, EyeHeight)).sqrMagnitude < EyeRadius * EyeRadius || (point - new Vector2(Center + EyeOffsetX, EyeHeight)).sqrMagnitude < EyeRadius * EyeRadius;

            if (eye || (standing == PlayerStanding.Cheater && OnBrow(point)))
                return true;

            if (Mathf.Abs(point.x - Center) > MouthHalfWidth + Stroke)
                return false;

            switch (standing)
            {
                case PlayerStanding.Cheater:
                    return OnArc(point, FrownCenter, point.y > FrownCut);

                case PlayerStanding.Suspect:
                    return Mathf.Abs(point.y - FlatMouthHeight) < Stroke / 2f && Mathf.Abs(point.x - Center) < MouthHalfWidth;

                default:
                    return OnArc(point, SmileCenter, point.y < SmileCut);
            }
        }

        private bool OnArc(Vector2 point, float centerHeight, bool inside)
        {
            float distance = (point - new Vector2(Center, centerHeight)).magnitude;
            return inside && Mathf.Abs(distance - MouthRadius) < Stroke / 2f;
        }

        private bool OnBrow(Vector2 point)
        {
            return SegmentDistance(point, new Vector2(Center - BrowOuter, BrowOuterHeight), new Vector2(Center - BrowInner, BrowInnerHeight)) < Stroke / 2f || SegmentDistance(point, new Vector2(Center + BrowOuter, BrowOuterHeight), new Vector2(Center + BrowInner, BrowInnerHeight)) < Stroke / 2f;
        }

        private float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 line = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(point - start, line) / line.sqrMagnitude);
            return (point - (start + line * along)).magnitude;
        }
    }
}
