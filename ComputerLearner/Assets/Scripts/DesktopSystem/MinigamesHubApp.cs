/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Minigames Hub launcher to play educational mouse exercises inside simulated OS.
*/
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    public class MinigamesHubApp : MonoBehaviour
    {
        #region Public Variables
        #endregion

        #region Private Variables
        [Header("Launch Buttons")]
        [SerializeField] private Button btnPlayTargets;
        [SerializeField] private Button btnPlayHover;
        [SerializeField] private Button btnPlayDoubleClick;
        [SerializeField] private Button btnPlayAdaptive;

        [Header("Level Definitions")]
        [SerializeField] private LevelDefinition targetsLevel;
        [SerializeField] private LevelDefinition hoverLevel;
        [SerializeField] private LevelDefinition doubleClickLevel;
        [SerializeField] private LevelDefinition adaptiveLevel;

        [Header("Base Window Prefab")]
        [SerializeField] private GameObject baseWindowPrefab;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (btnPlayTargets != null) btnPlayTargets.onClick.AddListener(PlayTargets);
            if (btnPlayHover != null) btnPlayHover.onClick.AddListener(PlayHover);
            if (btnPlayDoubleClick != null) btnPlayDoubleClick.onClick.AddListener(PlayDoubleClick);
            if (btnPlayAdaptive != null) btnPlayAdaptive.onClick.AddListener(PlayAdaptive);

            UpdateButtonStates();
        }

        private void OnEnable()
        {
            UpdateButtonStates();
            if (ProgressData.Instance != null)
                ProgressData.Instance.OnProgressChanged += UpdateButtonStates;
        }

        private void OnDisable()
        {
            if (ProgressData.Instance != null)
                ProgressData.Instance.OnProgressChanged -= UpdateButtonStates;
        }
        #endregion

        #region Public Methods
        public void PlayTargets()
        {
            LaunchLevel(targetsLevel, "Targets & Clicks");
        }

        public void PlayHover()
        {
            LaunchLevel(hoverLevel, "Tracking & Hover");
        }

        public void PlayDoubleClick()
        {
            LaunchLevel(doubleClickLevel, "Fast Double Click");
        }

        public void PlayAdaptive()
        {
            LaunchLevel(adaptiveLevel, "Adaptive Practice");
        }
        #endregion

        #region Private Methods
        private void UpdateButtonStates()
        {
            UpdateButton(btnPlayTargets, targetsLevel);
            UpdateButton(btnPlayHover, hoverLevel);
            UpdateButton(btnPlayDoubleClick, doubleClickLevel);
            UpdateButton(btnPlayAdaptive, adaptiveLevel);
        }

        private void UpdateButton(Button btn, LevelDefinition level)
        {
            if (btn == null) return;
            bool unlocked = level == null || ProgressData.Instance == null || ProgressData.Instance.IsLevelUnlocked(level.levelId);
            btn.interactable = unlocked;

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = unlocked ? 1f : 0.5f;

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }
        }

        private void LaunchLevel(LevelDefinition def, string fallbackTitle)
        {
            if (def != null && ProgressData.Instance != null && !ProgressData.Instance.IsLevelUnlocked(def.levelId))
            {
                Debug.LogWarning($"[MinigamesHubApp] Level {def.levelId} is locked.");
                return;
            }

            if (WindowManager.Instance == null)
            {
                Debug.LogWarning("[MinigamesHubApp] WindowManager no disponible.");
                return;
            }

            GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
            string title = def != null ? def.displayName : fallbackTitle;

            Window win = WindowManager.Instance.OpenWindow(winPrefab, null, title);
            if (win != null && def != null)
            {
                LevelRunner runner = win.GetComponent<LevelRunner>();
                if (runner == null) runner = win.gameObject.AddComponent<LevelRunner>();
                if (!win.IsMaximized && !DesktopTour.IsTourRunning) win.toggleMaximize();
                if (runner.taskContentArea == null) runner.taskContentArea = win.ContentArea != null ? win.ContentArea : win.GetComponent<RectTransform>();
                runner.StartLevel(def);
            }
        }
        #endregion
    }
}
