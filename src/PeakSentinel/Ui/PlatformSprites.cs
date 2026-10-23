using System;
using Portningsbolaget.Platforms;
using UnityEngine;

namespace PeakSentinel.Ui
{
    public sealed class PlatformSprites
    {
        private const int Size = 32;
        private const int Supersample = 4;

        private readonly Sprite _monitor;
        private readonly Sprite _gamepad;
        private readonly Sprite _phone;

        public PlatformSprites()
        {
            _monitor = Draw(IsMonitor);
            _gamepad = Draw(IsGamepad);
            _phone = Draw(IsPhone);
        }

        public Sprite Of(PlatformUtility.PlatformFamily family)
        {
            switch (family)
            {
                case PlatformUtility.PlatformFamily.Playstation:
                case PlatformUtility.PlatformFamily.Xbox:
                case PlatformUtility.PlatformFamily.Switch:
                    return _gamepad;

                case PlatformUtility.PlatformFamily.Android:
                case PlatformUtility.PlatformFamily.IOS:
                    return _phone;

                default:
                    return _monitor;
            }
        }

        public Color TintOf(PlatformUtility.PlatformFamily family)
        {
            switch (family)
            {
                case PlatformUtility.PlatformFamily.Playstation:
                    return UiPalette.PlayStation;

                case PlatformUtility.PlatformFamily.Xbox:
                    return UiPalette.Xbox;

                case PlatformUtility.PlatformFamily.Switch:
                    return UiPalette.Switch;

                default:
                    return UiPalette.Ink;
            }
        }

        private bool IsMonitor(Vector2 point)
        {
            bool screen = InRoundedBox(point, 2f, 10f, 30f, 29f, 3f) && !InRoundedBox(point, 5f, 13f, 27f, 26f, 1f);
            bool stand = InBox(point, 13.5f, 5f, 18.5f, 10f);
            bool foot = InRoundedBox(point, 8f, 2f, 24f, 5.5f, 1.5f);
            return screen || stand || foot;
        }

        private bool IsGamepad(Vector2 point)
        {
            bool body = InRoundedBox(point, 4f, 13f, 28f, 25f, 6f) || NearSegment(point, new Vector2(9.5f, 17f), new Vector2(7.5f, 11f), 4.25f) || NearSegment(point, new Vector2(22.5f, 17f), new Vector2(24.5f, 11f), 4.25f);
            bool cross = InBox(point, 6.75f, 17.75f, 13.25f, 20.25f) || InBox(point, 8.75f, 15.75f, 11.25f, 22.25f);
            bool buttons = Within(point, 22f, 21.3f, 1.1f) || Within(point, 24.3f, 19f, 1.1f) || Within(point, 22f, 16.7f, 1.1f) || Within(point, 19.7f, 19f, 1.1f);
            return body && !cross && !buttons;
        }

        private bool IsPhone(Vector2 point)
        {
            bool body = InRoundedBox(point, 9f, 2f, 23f, 30f, 3f);
            bool screen = InBox(point, 11.5f, 6.5f, 20.5f, 26f);
            bool button = Within(point, 16f, 4.25f, 1.1f);
            return body && !screen && !button;
        }

        private bool InBox(Vector2 point, float left, float bottom, float right, float top)
        {
            return point.x >= left && point.x <= right && point.y >= bottom && point.y <= top;
        }

        private bool InRoundedBox(Vector2 point, float left, float bottom, float right, float top, float radius)
        {
            float x = Mathf.Clamp(point.x, left + radius, right - radius);
            float y = Mathf.Clamp(point.y, bottom + radius, top - radius);
            return InBox(point, left, bottom, right, top) && (point - new Vector2(x, y)).sqrMagnitude <= radius * radius;
        }

        private bool Within(Vector2 point, float x, float y, float radius)
        {
            return (point - new Vector2(x, y)).sqrMagnitude <= radius * radius;
        }

        private bool NearSegment(Vector2 point, Vector2 from, Vector2 to, float radius)
        {
            Vector2 line = to - from;
            float along = Mathf.Clamp01(Vector2.Dot(point - from, line) / line.sqrMagnitude);
            return (point - (from + line * along)).sqrMagnitude <= radius * radius;
        }

        private Sprite Draw(Func<Vector2, bool> inside)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int covered = 0;

                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            if (inside(new Vector2(x + (sx + 0.5f) / Supersample, y + (sy + 0.5f) / Supersample)))
                                covered++;
                        }
                    }

                    pixels[y * Size + x] = new Color(1f, 1f, 1f, covered / (float)(Supersample * Supersample));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
