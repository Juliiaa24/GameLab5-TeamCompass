using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// Holds serialized references to the Colorful Retro Desktop sprites and existing art assets.
    /// Stored at Assets/Resources/RetroThemeAssets.asset for uniform access across scenes and runtime apps.
    /// </summary>
    [CreateAssetMenu(fileName = "RetroThemeAssets", menuName = "ComputerLearner/Retro Theme Assets")]
    public class RetroThemeAssets : ScriptableObject
    {
        private static RetroThemeAssets _cached;
        public static RetroThemeAssets Instance
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<RetroThemeAssets>("RetroThemeAssets");
                return _cached;
            }
        }

        [Header("9-Slice Retro UI Frames")]
        public Sprite windowBody;
        public Sprite windowTitleBar;
        public Sprite button3D;
        public Sprite cardRounded;
        public Sprite inputBox;
        public Sprite badgePill;

        [Header("Retro UI Icons")]
        public Sprite iconLock;
        public Sprite iconUnlock;
        public Sprite iconGear;
        public Sprite iconPlay;
        public Sprite iconGlobe;
        public Sprite iconClassroom;
        public Sprite iconCheck;
        public Sprite iconExit;

        [Header("Existing Project Art")]
        public Sprite desktopWallpaper;
        public Sprite topBarClassic;
        public Sprite topBarCloseBtn;
        public Sprite topBarMinBtn;
        public Sprite topBarMaxBtn;
        public Sprite mascotLogoNoBg;
        public Sprite mascotLogoSquared;
        public Sprite computerIcon;
        public Sprite targetIcon;
        public Sprite mouseIcon;
        public Sprite mouseMoveIcon;
        public Sprite mouseLeftClickIcon;
        public Sprite mouseRightClickIcon;
        public Sprite mouseHoldClickIcon;
        public Sprite goldMedal;
        public Sprite silverMedal;
        public Sprite bronzeMedal;
        public Sprite folderYellow;
        public Sprite folderBlue;
        public Sprite folderGreen;
        public Sprite folderCyan;
        public Sprite folderPink;
        public Sprite folderPurple;
        public Sprite barOutline;
        public Sprite barFilling;
    }

    /// <summary>
    /// Shared Colorful Retro Desktop palette, typography helpers, and UI styling utilities.
    /// Designed for kids ages 5-12: sunny yellows, sky blues, mint greens, soft cream cards, high-contrast ink text.
    /// </summary>
    public static class RetroUITheme
    {
        // Primary Desktop / Window Palette
        public static readonly Color SkyBackdropTint   = new Color(0.86f, 0.95f, 1.00f, 1.00f);
        public static readonly Color OverlayScrim      = new Color(0.07f, 0.16f, 0.34f, 0.62f);
        public static readonly Color WindowCream       = new Color(0.99f, 0.98f, 0.94f, 1.00f);
        public static readonly Color WindowSkySoft     = new Color(0.92f, 0.97f, 1.00f, 1.00f);
        public static readonly Color CardWhite         = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color CardPastelBlue    = new Color(0.89f, 0.95f, 1.00f, 1.00f);
        public static readonly Color CardPastelYellow  = new Color(1.00f, 0.96f, 0.80f, 1.00f);
        public static readonly Color CardPastelMint    = new Color(0.87f, 0.98f, 0.91f, 1.00f);
        public static readonly Color CardPastelLavend  = new Color(0.94f, 0.91f, 1.00f, 1.00f);
        public static readonly Color CardPastelPeach   = new Color(1.00f, 0.92f, 0.87f, 1.00f);
        public static readonly Color InputSurface      = new Color(0.97f, 0.99f, 1.00f, 1.00f);

        // Retro Titlebar Colors
        public static readonly Color TitleBlue         = new Color(0.14f, 0.50f, 0.90f, 1.00f);
        public static readonly Color TitleTeal         = new Color(0.08f, 0.64f, 0.68f, 1.00f);
        public static readonly Color TitleAmber        = new Color(0.92f, 0.58f, 0.10f, 1.00f);
        public static readonly Color TitleMint         = new Color(0.14f, 0.68f, 0.45f, 1.00f);
        public static readonly Color TitlePlum         = new Color(0.45f, 0.34f, 0.82f, 1.00f);

        // Tactile Button Colors
        public static readonly Color BtnSunnyGold      = new Color(1.00f, 0.80f, 0.16f, 1.00f);
        public static readonly Color BtnMintGreen      = new Color(0.24f, 0.78f, 0.48f, 1.00f);
        public static readonly Color BtnSkyBlue        = new Color(0.22f, 0.62f, 0.96f, 1.00f);
        public static readonly Color BtnPlayfulPurple  = new Color(0.55f, 0.42f, 0.92f, 1.00f);
        public static readonly Color BtnCoralRed       = new Color(0.92f, 0.33f, 0.33f, 1.00f);
        public static readonly Color BtnWarmOrange     = new Color(0.96f, 0.55f, 0.18f, 1.00f);
        public static readonly Color BtnNeutralCream   = new Color(0.95f, 0.94f, 0.89f, 1.00f);
        public static readonly Color BtnSoftSlate      = new Color(0.84f, 0.89f, 0.95f, 1.00f);

        // Text Ink & Borders
        public static readonly Color InkDark           = new Color(0.11f, 0.20f, 0.36f, 1.00f);
        public static readonly Color InkMuted          = new Color(0.32f, 0.43f, 0.58f, 1.00f);
        public static readonly Color InkWhite          = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color BorderNavy        = new Color(0.16f, 0.30f, 0.52f, 1.00f);
        public static readonly Color ProgressTrack     = new Color(0.84f, 0.91f, 0.98f, 1.00f);

        // Medal / Mastery Accents
        public static readonly Color GoldAccent        = new Color(0.98f, 0.74f, 0.10f, 1.00f);
        public static readonly Color SilverAccent      = new Color(0.64f, 0.73f, 0.84f, 1.00f);
        public static readonly Color BronzeAccent      = new Color(0.84f, 0.50f, 0.22f, 1.00f);
        public static readonly Color SuccessMint       = new Color(0.15f, 0.70f, 0.42f, 1.00f);
        public static readonly Color WarningAmber      = new Color(0.92f, 0.56f, 0.10f, 1.00f);
        public static readonly Color DangerCoral       = new Color(0.88f, 0.26f, 0.28f, 1.00f);

        public static void ApplySlicedSprite(Image image, Sprite sprite, Color tint, bool raycastTarget = false)
        {
            if (image == null) return;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            image.color = tint;
            image.raycastTarget = raycastTarget;
        }

        public static void ConfigureButtonColors(Button btn, Color baseColor)
        {
            if (btn == null) return;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.65f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            Image img = btn.targetGraphic as Image ?? btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = baseColor;
                img.raycastTarget = true;
            }
        }

        public static void SetNonBlockingGraphic(Graphic g)
        {
            if (g != null) g.raycastTarget = false;
        }

        public static string ColorToHex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
