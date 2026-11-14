using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// Controls the Menu StatsScene screen using the shared Colorful Retro Desktop presentation
    /// (identical to in-game ScoresApp / SkillsReportWindow).
    /// All scene hierarchy elements are materialized at edit-time by RetroSceneMaterializer.BuildStatsScene().
    /// </summary>
    public class StatsSceneController : MonoBehaviour
    {
        [Header("Header Controls")]
        [SerializeField] private TMP_Text headerTitleText;
        [SerializeField] private TMP_Text headerSubtitleText;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TMP_Text backButtonLabel;
        [SerializeField] private Button retakeAssessmentButton;
        [SerializeField] private TMP_Text retakeButtonLabel;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMP_Text refreshButtonLabel;

        [Header("Shared Retro Stats Body Binding")]
        [SerializeField] private RetroStatsSharedUI.StatsUIBinding sharedUI = new RetroStatsSharedUI.StatsUIBinding();

        private RetroStatsSharedUI.LevelFilter _activeFilter = RetroStatsSharedUI.LevelFilter.All;

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshAll;
        }

        private void BindButtons()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(HandleBackToMenu);
            }
            if (retakeAssessmentButton != null)
            {
                retakeAssessmentButton.onClick.RemoveAllListeners();
                retakeAssessmentButton.onClick.AddListener(HandleRetakeAssessment);
            }
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(RefreshAll);
            }

            if (sharedUI != null && sharedUI.tabButtons != null)
            {
                for (int i = 0; i < sharedUI.tabButtons.Length; i++)
                {
                    int tabIndex = i;
                    Button btn = sharedUI.tabButtons[i];
                    if (btn == null) continue;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        _activeFilter = (RetroStatsSharedUI.LevelFilter)tabIndex;
                        RefreshAll();
                        if (sharedUI.levelsScrollRect != null)
                            sharedUI.levelsScrollRect.verticalNormalizedPosition = 1f;
                    });
                }
            }

            if (sharedUI != null && sharedUI.pageUpButton != null)
            {
                sharedUI.pageUpButton.onClick.RemoveAllListeners();
                sharedUI.pageUpButton.onClick.AddListener(() => ScrollLevelsBy(0.38f));
            }
            if (sharedUI != null && sharedUI.pageDownButton != null)
            {
                sharedUI.pageDownButton.onClick.RemoveAllListeners();
                sharedUI.pageDownButton.onClick.AddListener(() => ScrollLevelsBy(-0.38f));
            }
        }

        public void RefreshAll()
        {
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            if (headerTitleText != null) headerTitleText.text = dlg.stats_page_title;
            if (headerSubtitleText != null) headerSubtitleText.text = dlg.scores_subtitle;
            if (backButtonLabel != null) backButtonLabel.text = dlg.stats_btn_back;
            if (retakeButtonLabel != null) retakeButtonLabel.text = dlg.stats_btn_retake;
            if (refreshButtonLabel != null) refreshButtonLabel.text = dlg.scores_refresh_button;

            if (sharedUI != null)
            {
                TMP_FontAsset font = headerTitleText != null ? headerTitleText.font : TMP_Settings.defaultFontAsset;
                RetroStatsSharedUI.RenderAll(sharedUI, _activeFilter, RetroThemeAssets.Instance, font);
            }
        }

        private void ScrollLevelsBy(float delta)
        {
            if (sharedUI == null || sharedUI.levelsScrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            sharedUI.levelsScrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(sharedUI.levelsScrollRect.verticalNormalizedPosition + delta);
        }

        private void HandleBackToMenu()
        {
            if (SceneSystem.Instance != null)
                SceneSystem.Instance.ChangeToMenu();
            else
                SceneManager.LoadScene("Menu");
        }

        private void HandleRetakeAssessment()
        {
            if (SceneSystem.Instance != null)
                SceneSystem.Instance.ChangeToAssessment();
            else
                SceneManager.LoadScene("AssessmentScene");
        }
    }
}
