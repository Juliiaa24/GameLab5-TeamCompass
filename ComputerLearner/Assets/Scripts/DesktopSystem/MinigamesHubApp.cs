/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Minigames Hub launcher to play educational mouse exercises inside simulated OS.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            UpdateLocalizedLabels();
        }

        private void OnEnable()
        {
            UpdateButtonStates();
            UpdateLocalizedLabels();
            if (ProgressData.Instance != null)
                ProgressData.Instance.OnProgressChanged += UpdateButtonStates;
            LocalizationManager.OnLanguageChanged += UpdateLocalizedLabels;
        }

        private void OnDisable()
        {
            if (ProgressData.HasInstance)
                ProgressData.Instance.OnProgressChanged -= UpdateButtonStates;
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedLabels;
        }
        #endregion

        #region Public Methods
        public void PlayTargets()
        {
            LaunchLevel(targetsLevel, UIDialogs.Current.minigames_targets);
        }

        public void PlayHover()
        {
            LaunchLevel(hoverLevel, UIDialogs.Current.minigames_hover);
        }

        public void PlayDoubleClick()
        {
            LaunchLevel(doubleClickLevel, UIDialogs.Current.minigames_double_click);
        }

        public void PlayAdaptive()
        {
            LaunchLevel(adaptiveLevel, UIDialogs.Current.minigames_adaptive);
        }
        #endregion

        #region Private Methods
        private void UpdateLocalizedLabels()
        {
            UIDialogs ui = UIDialogs.Current;
            UpdateCardLabels(btnPlayTargets, ui.minigames_targets, ui.minigames_btn_play);
            UpdateCardLabels(btnPlayHover, ui.minigames_hover, ui.minigames_btn_play);
            UpdateCardLabels(btnPlayDoubleClick, ui.minigames_double_click, ui.minigames_btn_play);
            UpdateCardLabels(btnPlayAdaptive, ui.minigames_adaptive, ui.minigames_btn_play);
        }

        private void UpdateCardLabels(Button playBtn, string cardTitle, string btnLabel)
        {
            if (playBtn == null) return;
            TMP_Text btnText = playBtn.GetComponentInChildren<TMP_Text>(true);
            if (btnText != null) btnText.text = btnLabel;

            if (playBtn.transform.parent != null)
            {
                Transform titleT = playBtn.transform.parent.Find("Title");
                if (titleT != null)
                {
                    TMP_Text titleText = titleT.GetComponent<TMP_Text>();
                    if (titleText != null) titleText.text = cardTitle;
                }
            }
        }

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
            string title = def != null ? def.GetLocalizedDisplayName() : fallbackTitle;

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
