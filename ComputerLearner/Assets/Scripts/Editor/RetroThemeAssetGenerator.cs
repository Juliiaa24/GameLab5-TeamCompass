#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using ComputerLearning;

namespace RetroUIBuilder
{
    /// <summary>
    /// Generates colorful retro desktop 9-slice sprites & crisp UI icons under Assets/Art/UI/RetroTheme/
    /// and populates Assets/Resources/RetroThemeAssets.asset referencing both generated and existing project art.
    /// </summary>
    public static class RetroThemeAssetGenerator
    {
        private const string ThemeArtFolder = "Assets/Art/UI/RetroTheme";
        private const string ResourcesAssetPath = "Assets/Resources/RetroThemeAssets.asset";

        [MenuItem("Tools/Retro UI/1. Generate Retro Theme Assets")]
        private static void GenerateAllMenu()
        {
            GenerateAll();
        }

        public static RetroThemeAssets GenerateAll()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/UI");
            EnsureFolder(ThemeArtFolder);
            EnsureFolder("Assets/Resources");

            // 1. Generate 9-slice sprites
            WriteRoundedWindowBody($"{ThemeArtFolder}/RetroWindowBody.png", 64, 64, 12f);
            WriteTitleBar($"{ThemeArtFolder}/RetroWindowTitleBar.png", 64, 48, 10f);
            WriteButton3D($"{ThemeArtFolder}/RetroButton3D.png", 64, 64, 12f);
            WriteCardRounded($"{ThemeArtFolder}/RetroCardRounded.png", 48, 48, 10f);
            WriteInputBox($"{ThemeArtFolder}/RetroInputBox.png", 48, 48, 10f);
            WriteBadgePill($"{ThemeArtFolder}/RetroBadgePill.png", 48, 32, 15f);

            // 2. Generate crisp retro icons (64x64)
            WriteLockIcon($"{ThemeArtFolder}/Icon_Lock.png", unlocked: false);
            WriteLockIcon($"{ThemeArtFolder}/Icon_Unlock.png", unlocked: true);
            WriteGearIcon($"{ThemeArtFolder}/Icon_Gear.png");
            WritePlayIcon($"{ThemeArtFolder}/Icon_PlayTriangle.png");
            WriteGlobeIcon($"{ThemeArtFolder}/Icon_Globe.png");
            WriteClassroomIcon($"{ThemeArtFolder}/Icon_Classroom.png");
            WriteCheckIcon($"{ThemeArtFolder}/Icon_Check.png");
            WriteExitIcon($"{ThemeArtFolder}/Icon_Exit.png");

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // 3. Configure TextureImporters with 9-slice borders
            ConfigureSprite($"{ThemeArtFolder}/RetroWindowBody.png", new Vector4(16, 16, 16, 16));
            ConfigureSprite($"{ThemeArtFolder}/RetroWindowTitleBar.png", new Vector4(14, 10, 14, 10));
            ConfigureSprite($"{ThemeArtFolder}/RetroButton3D.png", new Vector4(16, 18, 16, 16));
            ConfigureSprite($"{ThemeArtFolder}/RetroCardRounded.png", new Vector4(14, 14, 14, 14));
            ConfigureSprite($"{ThemeArtFolder}/RetroInputBox.png", new Vector4(12, 12, 12, 12));
            ConfigureSprite($"{ThemeArtFolder}/RetroBadgePill.png", new Vector4(15, 10, 15, 10));

            ConfigureSprite($"{ThemeArtFolder}/Icon_Lock.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Unlock.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Gear.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_PlayTriangle.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Globe.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Classroom.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Check.png", Vector4.zero);
            ConfigureSprite($"{ThemeArtFolder}/Icon_Exit.png", Vector4.zero);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // 4. Populate RetroThemeAssets ScriptableObject
            RetroThemeAssets assets = AssetDatabase.LoadAssetAtPath<RetroThemeAssets>(ResourcesAssetPath);
            if (assets == null)
            {
                if (File.Exists(ResourcesAssetPath))
                {
                    AssetDatabase.DeleteAsset(ResourcesAssetPath);
                    AssetDatabase.Refresh();
                }
                assets = ScriptableObject.CreateInstance<RetroThemeAssets>();
                AssetDatabase.CreateAsset(assets, ResourcesAssetPath);
            }

            assets.windowBody = LoadSprite($"{ThemeArtFolder}/RetroWindowBody.png");
            assets.windowTitleBar = LoadSprite($"{ThemeArtFolder}/RetroWindowTitleBar.png");
            assets.button3D = LoadSprite($"{ThemeArtFolder}/RetroButton3D.png");
            assets.cardRounded = LoadSprite($"{ThemeArtFolder}/RetroCardRounded.png");
            assets.inputBox = LoadSprite($"{ThemeArtFolder}/RetroInputBox.png");
            assets.badgePill = LoadSprite($"{ThemeArtFolder}/RetroBadgePill.png");

            assets.iconLock = LoadSprite($"{ThemeArtFolder}/Icon_Lock.png");
            assets.iconUnlock = LoadSprite($"{ThemeArtFolder}/Icon_Unlock.png");
            assets.iconGear = LoadSprite($"{ThemeArtFolder}/Icon_Gear.png");
            assets.iconPlay = LoadSprite($"{ThemeArtFolder}/Icon_PlayTriangle.png");
            assets.iconGlobe = LoadSprite($"{ThemeArtFolder}/Icon_Globe.png");
            assets.iconClassroom = LoadSprite($"{ThemeArtFolder}/Icon_Classroom.png");
            assets.iconCheck = LoadSprite($"{ThemeArtFolder}/Icon_Check.png");
            assets.iconExit = LoadSprite($"{ThemeArtFolder}/Icon_Exit.png");

            // Bind existing art assets
            assets.desktopWallpaper = LoadSprite("Assets/Art/UI/Desktop Background.png");
            assets.topBarClassic = LoadSprite("Assets/Art/UI/TopBarIcons/TopBar_Background_Full_Size01.png");
            assets.topBarCloseBtn = LoadSprite("Assets/Art/UI/TopBarIcons/Close_Button_Full_Size.png");
            assets.topBarMinBtn = LoadSprite("Assets/Art/UI/TopBarIcons/Min_Button_Full_Size_Blue.png");
            assets.topBarMaxBtn = LoadSprite("Assets/Art/UI/TopBarIcons/Max_Button_Full_Size_Blue.png");

            assets.mascotLogoNoBg = LoadSprite("Assets/Art/Logos/Computer Presentation Logo Squared No Background.png");
            assets.mascotLogoSquared = LoadSprite("Assets/Art/Logos/Computer Presentation Logo Squared.png");
            assets.computerIcon = LoadSprite("Assets/Art/UI/App icons/Computer Presentation.png");
            assets.targetIcon = LoadSprite("Assets/Art/UI/Target Icons/Target_Icon_128x128.png")
                ?? LoadSprite("Assets/Art/UI/Target Icons/Target_Icon_01.png");

            assets.mouseIcon = LoadSprite("Assets/Art/UI/Mouse Icons/PC_Mouse_Icon_08b.png");
            assets.mouseMoveIcon = LoadSprite("Assets/Art/UI/Mouse Icons/PC_Mouse_Move_Icon_01.png");
            assets.mouseLeftClickIcon = LoadSprite("Assets/Art/UI/Mouse Icons/PC_Mouse_LClick_Icon_01.png");
            assets.mouseRightClickIcon = LoadSprite("Assets/Art/UI/Mouse Icons/PC_Mouse_RClick_Icon_01.png");
            assets.mouseHoldClickIcon = LoadSprite("Assets/Art/UI/Mouse Icons/PC_Mouse_HoldClick_Icon_01.png");

            assets.goldMedal = LoadSprite("Assets/Art/UI/Medals/GoldMedalNoRibbon.png");
            assets.silverMedal = LoadSprite("Assets/Art/UI/Medals/SilverMedalNoRibbon.png");
            assets.bronzeMedal = LoadSprite("Assets/Art/UI/Medals/BronzeMedalNoRibbon.png");

            assets.folderYellow = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01.png");
            assets.folderBlue = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Blue.png");
            assets.folderGreen = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Green.png");
            assets.folderCyan = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Cyan.png");
            assets.folderPink = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Pink.png");
            assets.folderPurple = LoadSprite("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Purple.png");

            assets.barOutline = LoadSprite("Assets/Art/UI/BarOutline.png");
            assets.barFilling = LoadSprite("Assets/Art/UI/BarFilling.png");

            EditorUtility.SetDirty(assets);

            EnsureTmpFallbackAndGlyphs();

            AssetDatabase.SaveAssets();
            Debug.Log("[RetroThemeAssetGenerator] Generated retro theme sprites & updated RetroThemeAssets.asset.");
            return assets;
        }

        private static void EnsureTmpFallbackAndGlyphs()
        {
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset");

            if (mainFont != null && fallbackFont != null)
            {
                if (mainFont.fallbackFontAssetTable == null)
                    mainFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (!mainFont.fallbackFontAssetTable.Contains(fallbackFont))
                {
                    mainFont.fallbackFontAssetTable.Add(fallbackFont);
                    EditorUtility.SetDirty(mainFont);
                }

                // Pre-populate EN / ES / NO special characters on dynamic fallback asset if supported
                if (fallbackFont.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                {
                    fallbackFont.TryAddCharacters("áéíóúüñÁÉÍÓÚÜÑ¡¿æøåÆØÅ–—•…°");
                    EditorUtility.SetDirty(fallbackFont);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ConfigureSprite(string assetPath, Vector4 border)
        {
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.spriteBorder = border;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) return spr;

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
                spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return spr;
        }

        // =====================================================================
        // Procedural SDF texture builders (White/neutral base so UI Color tints cleanly)
        // =====================================================================

        private static float RoundedRectSdf(float px, float py, float cx, float cy, float halfW, float halfH, float r)
        {
            float dx = Mathf.Abs(px - cx) - (halfW - r);
            float dy = Mathf.Abs(py - cy) - (halfH - r);
            float ox = Mathf.Max(dx, 0f);
            float oy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        private static void SaveTexture(string assetPath, Texture2D tex)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void WriteRoundedWindowBody(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.14f, 0.27f, 0.49f, 1f);
            Color shadowLip = new Color(0.84f, 0.90f, 0.96f, 1f);
            Color highlightBevel = Color.white;
            Color bodyFill = new Color(0.98f, 0.98f, 0.99f, 1f);

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float hw = w * 0.5f - 1.5f;
            float hh = h * 0.5f - 1.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);
                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c;
                    if (d > -2.6f)
                        c = borderColor;
                    else if (y < 6)
                        c = shadowLip;
                    else if (y > h - 6 || x < 5)
                        c = highlightBevel;
                    else
                        c = bodyFill;

                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteTitleBar(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.12f, 0.24f, 0.45f, 1f);

            float cx = w * 0.5f;
            float cy = h * 0.5f - 3f; // flat bottom edge
            float hw = w * 0.5f - 1.5f;
            float hh = h * 0.5f + 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = y < 8
                        ? Mathf.Max(Mathf.Abs((x + 0.5f) - cx) - hw, -y + 0.5f)
                        : RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);

                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c;
                    if (d > -2.4f || y < 2)
                        c = borderColor;
                    else
                    {
                        float t = (float)y / h;
                        float gloss = t > 0.68f ? 1.0f : Mathf.Lerp(0.86f, 0.97f, t);
                        c = new Color(gloss, gloss, gloss, 1f);
                    }
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteButton3D(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.13f, 0.25f, 0.46f, 1f);
            Color bottomLip = new Color(0.74f, 0.78f, 0.85f, 1f);
            Color topGloss = Color.white;
            Color middle = new Color(0.95f, 0.96f, 0.98f, 1f);

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float hw = w * 0.5f - 1.5f;
            float hh = h * 0.5f - 1.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);
                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c;
                    if (d > -2.5f)
                        c = borderColor;
                    else if (y < 8)
                        c = bottomLip;
                    else if (y > h - 9)
                        c = topGloss;
                    else
                        c = middle;

                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteCardRounded(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.22f, 0.37f, 0.58f, 0.85f);
            Color bottomShade = new Color(0.90f, 0.93f, 0.97f, 1f);
            Color fill = Color.white;

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float hw = w * 0.5f - 1.5f;
            float hh = h * 0.5f - 1.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);
                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c = d > -2.0f ? borderColor : (y < 5 ? bottomShade : fill);
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteInputBox(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.22f, 0.38f, 0.62f, 1f);
            Color innerShadow = new Color(0.85f, 0.90f, 0.96f, 1f);
            Color fill = Color.white;

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float hw = w * 0.5f - 1.5f;
            float hh = h * 0.5f - 1.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);
                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c = d > -2.0f ? borderColor : (y > h - 6 ? innerShadow : fill);
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteBadgePill(string path, int w, int h, float radius)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color borderColor = new Color(0.16f, 0.30f, 0.52f, 0.9f);
            Color fill = Color.white;

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float hw = w * 0.5f - 1f;
            float hh = h * 0.5f - 1f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, radius);
                    if (d > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    float alpha = Mathf.Clamp01(0.5f - d);
                    Color c = d > -1.8f ? borderColor : fill;
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        // =====================================================================
        // Simple crisp 64x64 icons
        // =====================================================================

        private static Texture2D BlankTex(int size = 64)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clear = new Color[size * size];
            t.SetPixels(clear);
            return t;
        }

        private static void PlotCircle(Texture2D t, float cx, float cy, float r, Color col)
        {
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - r;
                if (d < 0.8f)
                {
                    float a = Mathf.Clamp01(0.5f - d) * col.a;
                    BlendPixel(t, x, y, new Color(col.r, col.g, col.b, a));
                }
            }
        }

        private static void PlotRoundedBox(Texture2D t, float cx, float cy, float hw, float hh, float r, Color col)
        {
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
            {
                float d = RoundedRectSdf(x + 0.5f, y + 0.5f, cx, cy, hw, hh, r);
                if (d < 0.8f)
                {
                    float a = Mathf.Clamp01(0.5f - d) * col.a;
                    BlendPixel(t, x, y, new Color(col.r, col.g, col.b, a));
                }
            }
        }

        private static void BlendPixel(Texture2D t, int x, int y, Color src)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            Color dst = t.GetPixel(x, y);
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f) return;
            Color outC = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            outC.a = outA;
            t.SetPixel(x, y, outC);
        }

        private static void WriteLockIcon(string path, bool unlocked)
        {
            Texture2D tex = BlankTex();
            Color ink = new Color(0.11f, 0.20f, 0.36f, 1f);
            Color gold = new Color(1.00f, 0.78f, 0.14f, 1f);
            Color goldDark = new Color(0.88f, 0.56f, 0.08f, 1f);
            Color shackle = new Color(0.84f, 0.90f, 0.97f, 1f);

            // Shackle ring
            float sx = unlocked ? 39f : 32f;
            float sy = unlocked ? 45f : 42f;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                if (y < (unlocked ? 32 : 30)) continue;
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(sx, sy));
                if (dist >= 8.5f && dist <= 16.5f)
                {
                    bool outline = dist < 10.6f || dist > 14.4f;
                    BlendPixel(tex, x, y, outline ? ink : shackle);
                }
            }

            // Body
            PlotRoundedBox(tex, 32f, 22f, 18f, 15f, 5f, ink);
            PlotRoundedBox(tex, 32f, 22f, 15.5f, 12.5f, 3.5f, goldDark);
            PlotRoundedBox(tex, 32f, 23.5f, 15.5f, 11f, 3.5f, gold);
            // Keyhole
            PlotCircle(tex, 32f, 25f, 3.3f, ink);
            PlotRoundedBox(tex, 32f, 19.5f, 1.8f, 4f, 1f, ink);

            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteGearIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color ink = new Color(0.11f, 0.20f, 0.36f, 1f);
            Color fill = new Color(0.88f, 0.94f, 1.0f, 1f);

            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 0.25f;
                float tx = 32f + Mathf.Cos(ang) * 20f;
                float ty = 32f + Mathf.Sin(ang) * 20f;
                PlotCircle(tex, tx, ty, 6.2f, ink);
            }
            PlotCircle(tex, 32f, 32f, 20f, ink);

            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 0.25f;
                float tx = 32f + Mathf.Cos(ang) * 20f;
                float ty = 32f + Mathf.Sin(ang) * 20f;
                PlotCircle(tex, tx, ty, 3.8f, fill);
            }
            PlotCircle(tex, 32f, 32f, 17.5f, fill);
            PlotCircle(tex, 32f, 32f, 9f, ink);
            PlotCircle(tex, 32f, 32f, 6.5f, Color.clear);
            // Punch center hole
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) < 6.4f)
                    tex.SetPixel(x, y, Color.clear);
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WritePlayIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color ink = new Color(0.11f, 0.20f, 0.36f, 1f);
            Color fill = Color.white;

            Vector2 a = new Vector2(18f, 14f);
            Vector2 b = new Vector2(18f, 50f);
            Vector2 c = new Vector2(52f, 32f);

            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = DistToTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c);
                if (d < 3.5f)
                {
                    float alpha = Mathf.Clamp01(3.5f - d);
                    Color col = d > 0.5f ? ink : fill;
                    col.a = alpha;
                    BlendPixel(tex, x, y, col);
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteGlobeIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color ink = new Color(0.11f, 0.20f, 0.36f, 1f);
            Color sea = new Color(0.30f, 0.72f, 0.98f, 1f);
            Color land = new Color(0.36f, 0.82f, 0.45f, 1f);

            PlotCircle(tex, 32f, 32f, 24f, ink);
            PlotCircle(tex, 32f, 32f, 21f, sea);

            // Friendly continents
            PlotRoundedBox(tex, 25f, 36f, 7f, 6f, 4f, land);
            PlotRoundedBox(tex, 40f, 28f, 6f, 8f, 4f, land);
            PlotRoundedBox(tex, 27f, 21f, 4f, 5f, 3f, land);

            // Latitude/longitude lines
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
                if (r <= 21f && (Mathf.Abs(y - 31.5f) < 1.1f || Mathf.Abs(x - 31.5f) < 1.1f))
                    BlendPixel(tex, x, y, new Color(ink.r, ink.g, ink.b, 0.45f));
            }

            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteClassroomIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color ink = new Color(0.11f, 0.20f, 0.36f, 1f);
            Color monitor = new Color(0.95f, 0.97f, 1.0f, 1f);
            Color screen = new Color(0.24f, 0.66f, 0.96f, 1f);

            // Stand
            PlotRoundedBox(tex, 32f, 13f, 12f, 3f, 2f, ink);
            PlotRoundedBox(tex, 32f, 18f, 4f, 5f, 1f, ink);
            // Monitor
            PlotRoundedBox(tex, 32f, 37f, 24f, 17f, 5f, ink);
            PlotRoundedBox(tex, 32f, 37f, 21.5f, 14.5f, 3.5f, monitor);
            PlotRoundedBox(tex, 32f, 37f, 18f, 11f, 2f, screen);

            // Wifi arcs inside screen
            PlotCircle(tex, 32f, 32f, 2.5f, Color.white);
            for (int y = 32; y < 48; y++)
            for (int x = 20; x < 44; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
                if ((d >= 5.5f && d <= 7.8f) || (d >= 10f && d <= 12.3f))
                    BlendPixel(tex, x, y, Color.white);
            }

            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteCheckIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color white = Color.white;
            Vector2 p1 = new Vector2(14f, 32f);
            Vector2 p2 = new Vector2(27f, 18f);
            Vector2 p3 = new Vector2(50f, 44f);

            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Mathf.Min(DistToSegment(p, p1, p2), DistToSegment(p, p2, p3));
                if (d < 5.5f)
                {
                    float a = Mathf.Clamp01(5.5f - d);
                    BlendPixel(tex, x, y, new Color(white.r, white.g, white.b, a));
                }
            }
            tex.Apply();
            SaveTexture(path, tex);
        }

        private static void WriteExitIcon(string path)
        {
            Texture2D tex = BlankTex();
            Color white = Color.white;
            // Door frame + arrow pointing right
            PlotRoundedBox(tex, 24f, 32f, 12f, 20f, 3f, white);
            for (int y = 16; y < 48; y++)
            for (int x = 16; x < 34; x++)
                tex.SetPixel(x, y, Color.clear);

            // Arrow shaft
            PlotRoundedBox(tex, 38f, 32f, 12f, 3.5f, 1.5f, white);
            // Arrow head
            Vector2 a = new Vector2(44f, 22f);
            Vector2 b = new Vector2(44f, 42f);
            Vector2 c = new Vector2(56f, 32f);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = DistToTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c);
                if (d < 1.2f)
                    BlendPixel(tex, x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.2f - d)));
            }

            tex.Apply();
            SaveTexture(path, tex);
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float DistToTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d = Mathf.Min(
                Mathf.Min(DistToSegment(p, a, b), DistToSegment(p, b, c)),
                DistToSegment(p, c, a));
            float s1 = Sign(p, a, b);
            float s2 = Sign(p, b, c);
            float s3 = Sign(p, c, a);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            return inside ? -d : d;
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
#endif
