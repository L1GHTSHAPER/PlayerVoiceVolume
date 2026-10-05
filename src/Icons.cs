using UnityEngine;

namespace PlayerVoiceVolume
{
    /// <summary>Icons the game has no sprite for, drawn once at runtime (white, tinted by the Image colour).</summary>
    internal static class Icons
    {
        static Sprite _reset;

        /// <summary>A clockwise circular arrow.</summary>
        public static Sprite Reset
        {
            get
            {
                if (_reset == null)
                    _reset = CreateResetIcon(64);
                return _reset;
            }
        }

        static Sprite CreateResetIcon(int size)
        {
            // Geometry in 0..1 units, y up. The arc runs counter-clockwise from 40° to 320°, leaving a gap on the
            // right; the arrow head sits on the 40° end and points clockwise into the gap.
            const float radius = 0.31f;
            const float stroke = 0.11f;
            const float startDeg = 40f;
            const float endDeg = 320f;
            var center = new Vector2(0.5f, 0.5f);

            float a = startDeg * Mathf.Deg2Rad;
            var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var clockwise = new Vector2(radial.y, -radial.x);
            Vector2 headBase = center + radial * radius;
            Vector2 tip = headBase + clockwise * 0.17f;
            Vector2 left = headBase + radial * 0.16f - clockwise * 0.03f;
            Vector2 right = headBase - radial * 0.16f - clockwise * 0.03f;

            float b = endDeg * Mathf.Deg2Rad;
            Vector2 tailEnd = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius;

            const int samples = 4;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int covered = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            var p = new Vector2((x + (sx + 0.5f) / samples) / size, (y + (sy + 0.5f) / samples) / size);
                            if (OnArc(p, center, radius, stroke, startDeg, endDeg)
                                || (p - tailEnd).sqrMagnitude <= stroke * stroke * 0.25f
                                || InTriangle(p, tip, left, right))
                                covered++;
                        }
                    }
                    byte alpha = (byte)(255 * covered / (samples * samples));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlayerVoiceVolume_Reset",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static bool OnArc(Vector2 p, Vector2 center, float radius, float stroke, float startDeg, float endDeg)
        {
            Vector2 d = p - center;
            if (Mathf.Abs(d.magnitude - radius) > stroke * 0.5f)
                return false;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;
            return angle >= startDeg && angle <= endDeg;
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b);
            float d2 = Cross(p, b, c);
            float d3 = Cross(p, c, a);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }
    }
}
