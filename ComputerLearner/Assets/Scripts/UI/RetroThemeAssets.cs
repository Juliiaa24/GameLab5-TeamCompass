using UnityEngine;

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
}
