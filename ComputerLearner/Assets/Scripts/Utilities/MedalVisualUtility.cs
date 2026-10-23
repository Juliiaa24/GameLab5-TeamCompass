/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Procedural and cached visual assets (Sprites, Colors, Badges) for Medals & Trophy UI.
 */
using UnityEngine;

namespace ComputerLearning
{
    public static class MedalVisualUtility
    {
        private static Sprite goldMedalSprite;
        private static Sprite silverMedalSprite;
        private static Sprite bronzeMedalSprite;
        private static Sprite trophyAppSprite;

        public static Sprite GetMedalSprite(LevelMedal medal)
        {
            switch (medal)
            {
                case LevelMedal.Gold:
                    if (goldMedalSprite == null) goldMedalSprite = CreateCircularMedalSprite(new Color(1f, 0.84f, 0.0f), new Color(0.9f, 0.65f, 0.0f), "1");
                    return goldMedalSprite;
                case LevelMedal.Silver:
                    if (silverMedalSprite == null) silverMedalSprite = CreateCircularMedalSprite(new Color(0.85f, 0.88f, 0.92f), new Color(0.65f, 0.70f, 0.75f), "2");
                    return silverMedalSprite;
                case LevelMedal.Bronze:
                    if (bronzeMedalSprite == null) bronzeMedalSprite = CreateCircularMedalSprite(new Color(0.82f, 0.52f, 0.25f), new Color(0.60f, 0.35f, 0.15f), "3");
                    return bronzeMedalSprite;
                default:
                    return null;
            }
        }

        public static Sprite GetTrophyAppSprite()
        {
            if (trophyAppSprite != null) return trophyAppSprite;

            // Generate a 48x48 retro trophy sprite
            int w = 48;
            int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color goldMain = new Color(1.0f, 0.84f, 0.0f);
            Color goldHighlight = new Color(1.0f, 0.95f, 0.5f);
            Color goldShadow = new Color(0.75f, 0.55f, 0.05f);
            Color baseColor = new Color(0.25f, 0.25f, 0.35f);
            Color baseHighlight = new Color(0.45f, 0.45f, 0.55f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Draw Cup Body & Handles
            for (int y = 4; y < 44; y++)
            {
                for (int x = 4; x < 44; x++)
                {
                    float dx = x - 23.5f;
                    
                    // Base pedestal (y: 6 to 11)
                    if (y >= 6 && y <= 9 && Mathf.Abs(dx) <= 12)
                    {
                        tex.SetPixel(x, y, dx < -4 ? baseHighlight : baseColor);
                    }
                    else if (y >= 10 && y <= 13 && Mathf.Abs(dx) <= 6)
                    {
                        tex.SetPixel(x, y, baseColor);
                    }
                    // Stem (y: 14 to 18)
                    else if (y >= 14 && y <= 18 && Mathf.Abs(dx) <= 3)
                    {
                        tex.SetPixel(x, y, dx < 0 ? goldHighlight : goldMain);
                    }
                    // Cup bottom (y: 19 to 24)
                    else if (y >= 19 && y <= 24)
                    {
                        float span = 4f + (y - 19) * 1.5f;
                        if (Mathf.Abs(dx) <= span)
                        {
                            tex.SetPixel(x, y, dx < -span * 0.3f ? goldHighlight : (dx > span * 0.4f ? goldShadow : goldMain));
                        }
                    }
                    // Cup upper bowl (y: 25 to 37)
                    else if (y >= 25 && y <= 37)
                    {
                        float span = 11.5f + (y - 25) * 0.2f;
                        if (Mathf.Abs(dx) <= span)
                        {
                            tex.SetPixel(x, y, dx < -span * 0.4f ? goldHighlight : (dx > span * 0.4f ? goldShadow : goldMain));
                        }
                    }
                    // Cup rim (y: 38 to 40)
                    else if (y >= 38 && y <= 40 && Mathf.Abs(dx) <= 14.5f)
                    {
                        tex.SetPixel(x, y, dx < 0 ? goldHighlight : goldMain);
                    }
                    // Handles (left and right arches between y 25 and 36)
                    if (y >= 24 && y <= 35)
                    {
                        float absDx = Mathf.Abs(dx);
                        if (absDx >= 12f && absDx <= 16f)
                        {
                            tex.SetPixel(x, y, dx < 0 ? goldHighlight : goldShadow);
                        }
                    }
                }
            }

            tex.filterMode = FilterMode.Point;
            tex.Apply();

            trophyAppSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            trophyAppSprite.name = "Trophy_Icon_Procedural";
            return trophyAppSprite;
        }

        private static Sprite CreateCircularMedalSprite(Color mainCol, Color shadowCol, string label)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color rimCol = new Color(1f, 1f, 1f, 0.85f);
            float radius = (size / 2f) - 1.5f;
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else if (dist >= radius - 2f)
                    {
                        // Outer rim
                        tex.SetPixel(x, y, shadowCol);
                    }
                    else if (dist >= radius - 3.5f)
                    {
                        // Embossed inner highlight
                        tex.SetPixel(x, y, (x <= center.x && y >= center.y) ? rimCol : shadowCol);
                    }
                    else
                    {
                        // Core face
                        float grad = ((x - center.x) + (center.y - y)) / (radius * 2f);
                        Color c = Color.Lerp(mainCol, shadowCol, Mathf.Clamp01(grad * 0.6f + 0.2f));
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Draw center star / number highlight
            int cx = size / 2;
            int cy = size / 2;
            tex.SetPixel(cx, cy, Color.white);
            tex.SetPixel(cx + 1, cy, Color.white);
            tex.SetPixel(cx - 1, cy, Color.white);
            tex.SetPixel(cx, cy + 1, Color.white);
            tex.SetPixel(cx, cy - 1, Color.white);

            tex.filterMode = FilterMode.Point;
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.name = $"Medal_{label}_Procedural";
            return s;
        }
    }
}
