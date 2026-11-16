using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// In-game Scores & Achievements window content inside the virtual desktop.
    /// Renders the exact same Colorful Retro Desktop layout as Menu -> Stats (via RetroStatsSharedUI)
    /// using pre-serialized prefab references (zero runtime UI generation).
    /// </summary>
    public class ScoresApp : MonoBehaviour
    {
        [Header("Pre-Built Prefab UI References")]
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Button _refreshBtn;
        [SerializeField] private TMP_Text _refreshBtnLabel;
        [SerializeField] private RetroStatsSharedUI.StatsUIBinding _sharedUI = new RetroStatsSharedUI.StatsUIBinding();

        private Window _parentWindow;
        private RetroStatsSharedUI.LevelFilter _activeFilter = RetroStatsSharedUI.LevelFilter.All;
        private bool _built;

        private void Awake()
        {
            _parentWindow = GetComponentInParent<Window>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshRecords;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshRecords;
        }

        private void Start()
        {
            EnsureBuilt();
            RefreshRecords();
        }

        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            if (_parentWindow == null)
                _parentWindow = GetComponentInParent<Window>();

            RetroThemeAssets assets = RetroThemeAssets.Instance;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            // Stretch cleanly to fill WindowContents
            RectTransform selfRt = GetComponent<RectTransform>();
            if (selfRt != null)
            {
                selfRt.anchorMin = Vector2.zero;
                selfRt.anchorMax = Vector2.one;
                selfRt.offsetMin = Vector2.zero;
                selfRt.offsetMax = Vector2.zero;
            }

            Image bgImg = GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = RetroUITheme.WindowSkySoft;
                bgImg.raycastTarget = false;
            }

            // Configure parent retro window title, trophy/medal icon, and generous size
            if (_parentWindow != null)
            {
                _parentWindow.setTitle(dlg.scores_window_title);
                Sprite iconSpr = assets != null ? (assets.iconTrophy != null ? assets.iconTrophy : assets.goldMedal) : null;
                if (iconSpr != null)
                    _parentWindow.setIcon(iconSpr);

                ResizeParentWindowComfortably();
            }

            if (_refreshBtn != null)
            {
                _refreshBtn.onClick.RemoveAllListeners();
                _refreshBtn.onClick.AddListener(RefreshRecords);
            }

            // Bind category filter tabs & page buttons
            if (_sharedUI != null && _sharedUI.tabButtons != null)
            {
                for (int i = 0; i < _sharedUI.tabButtons.Length; i++)
                {
                    int idx = i;
                    Button btn = _sharedUI.tabButtons[i];
                    if (btn == null) continue;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        _activeFilter = (RetroStatsSharedUI.LevelFilter)idx;
                        RefreshRecords();
                        if (_sharedUI.levelsScrollRect != null)
                            _sharedUI.levelsScrollRect.verticalNormalizedPosition = 1f;
                    });
                }
            }

            if (_sharedUI != null && _sharedUI.pageUpButton != null)
            {
                _sharedUI.pageUpButton.onClick.RemoveAllListeners();
                _sharedUI.pageUpButton.onClick.AddListener(() => ScrollLevelsBy(0.38f));
            }
            if (_sharedUI != null && _sharedUI.pageDownButton != null)
            {
                _sharedUI.pageDownButton.onClick.RemoveAllListeners();
                _sharedUI.pageDownButton.onClick.AddListener(() => ScrollLevelsBy(-0.38f));
            }
        }

        public void RefreshRecords()
        {
            if (!_built) return;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            if (_parentWindow != null) _parentWindow.setTitle(dlg.scores_window_title);
            if (_subtitleText != null) _subtitleText.text = dlg.scores_subtitle;
            if (_refreshBtnLabel != null) _refreshBtnLabel.text = dlg.scores_refresh_button;

            if (_sharedUI != null)
            {
                RetroStatsSharedUI.RenderAll(_sharedUI, _activeFilter, RetroThemeAssets.Instance, TMP_Settings.defaultFontAsset);
            }
        }

        private void ResizeParentWindowComfortably()
        {
            if (_parentWindow == null) return;
            RectTransform winRt = _parentWindow.GetComponent<RectTransform>();
            if (winRt == null) return;

            RectTransform parentRt = winRt.parent as RectTransform;
            float maxW = parentRt != null && parentRt.rect.width > 400f ? parentRt.rect.width - 40f : 1560f;
            float maxH = parentRt != null && parentRt.rect.height > 300f ? parentRt.rect.height - 32f : 860f;

            float targetW = Mathf.Min(1520f, maxW);
            float targetH = Mathf.Min(840f, maxH);

            if (winRt.sizeDelta.x < targetW - 20f || winRt.sizeDelta.y < targetH - 20f)
            {
                winRt.anchorMin = new Vector2(0.5f, 0.5f);
                winRt.anchorMax = new Vector2(0.5f, 0.5f);
                winRt.pivot = new Vector2(0.5f, 0.5f);
                winRt.sizeDelta = new Vector2(targetW, targetH);
                winRt.anchoredPosition = Vector2.zero;
            }
        }

        private void ScrollLevelsBy(float delta)
        {
            if (_sharedUI == null || _sharedUI.levelsScrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            _sharedUI.levelsScrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(_sharedUI.levelsScrollRect.verticalNormalizedPosition + delta);
        }
    }
}
