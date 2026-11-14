using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// In-game Scores & Achievements window content inside the virtual desktop.
    /// Renders the exact same Colorful Retro Desktop layout as Menu -> Stats (via RetroStatsSharedUI),
    /// paired with live Refresh & Reset Records controls.
    /// </summary>
    public class ScoresApp : MonoBehaviour
    {
        private Window _parentWindow;
        private RetroStatsSharedUI.StatsUIBinding _sharedUI;
        private RetroStatsSharedUI.LevelFilter _activeFilter = RetroStatsSharedUI.LevelFilter.All;

        private TMP_Text _subtitleText;
        private Button _refreshBtn;
        private TMP_Text _refreshBtnLabel;
        private Button _resetBtn;
        private TMP_Text _resetBtnLabel;

        private GameObject _resetModalRoot;
        private TMP_InputField _resetConfirmInput;
        private TMP_Text _resetModalTitle;
        private TMP_Text _resetModalPrompt;
        private TMP_Text _resetModalConfirmLabel;

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
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
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

            // Soft sky background inside content panel
            Image bgImg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bgImg.color = RetroUITheme.WindowSkySoft;
            bgImg.raycastTarget = false;

            // Configure parent retro window title, trophy/medal icon, and generous size
            if (_parentWindow != null)
            {
                _parentWindow.setTitle(dlg.scores_window_title);
                if (assets != null && assets.goldMedal != null)
                    _parentWindow.setIcon(assets.goldMedal);

                ResizeParentWindowComfortably();
            }

            // Clear any legacy children (e.g. if prefab contained old placeholder UI)
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // Top Subtitle + Action Bar (identical in style to StatsScene header controls)
            GameObject actionStrip = new GameObject("TopActionStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            actionStrip.transform.SetParent(transform, false);
            RetroUITheme.ApplySlicedSprite(actionStrip.GetComponent<Image>(), assets != null ? assets.cardRounded : null, RetroUITheme.CardPastelBlue, false);
            RectTransform asRt = actionStrip.GetComponent<RectTransform>();
            asRt.anchorMin = new Vector2(0f, 1f);
            asRt.anchorMax = new Vector2(1f, 1f);
            asRt.pivot = new Vector2(0.5f, 1f);
            asRt.offsetMin = new Vector2(14f, -62f);
            asRt.offsetMax = new Vector2(-14f, -12f);

            _subtitleText = CreateTmp("SubtitleText", actionStrip.transform, font, dlg.scores_subtitle, 15, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform subRt = _subtitleText.GetComponent<RectTransform>();
            subRt.anchorMin = Vector2.zero;
            subRt.anchorMax = Vector2.one;
            subRt.offsetMin = new Vector2(16f, 4f);
            subRt.offsetMax = new Vector2(-340f, -4f);

            _refreshBtn = CreateRetroButton("Btn-RefreshRecords", actionStrip.transform, assets, font, dlg.scores_refresh_button,
                RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 14, out _refreshBtnLabel);
            RectTransform rfRt = _refreshBtn.GetComponent<RectTransform>();
            rfRt.anchorMin = new Vector2(1f, 0.5f);
            rfRt.anchorMax = new Vector2(1f, 0.5f);
            rfRt.pivot = new Vector2(1f, 0.5f);
            rfRt.anchoredPosition = new Vector2(-168f, 0f);
            rfRt.sizeDelta = new Vector2(150f, 36f);
            _refreshBtn.onClick.AddListener(RefreshRecords);

            _resetBtn = CreateRetroButton("Btn-ResetRecords", actionStrip.transform, assets, font, dlg.scores_reset_button,
                RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 14, out _resetBtnLabel);
            RectTransform rsRt = _resetBtn.GetComponent<RectTransform>();
            rsRt.anchorMin = new Vector2(1f, 0.5f);
            rsRt.anchorMax = new Vector2(1f, 0.5f);
            rsRt.pivot = new Vector2(1f, 0.5f);
            rsRt.anchoredPosition = new Vector2(-10f, 0f);
            rsRt.sizeDelta = new Vector2(148f, 36f);
            _resetBtn.onClick.AddListener(OpenResetConfirmModal);

            // Shared Retro Body (exact match with StatsScene)
            GameObject bodyRoot = new GameObject("SharedStatsBody", typeof(RectTransform));
            bodyRoot.transform.SetParent(transform, false);
            RectTransform brRt = bodyRoot.GetComponent<RectTransform>();
            brRt.anchorMin = Vector2.zero;
            brRt.anchorMax = Vector2.one;
            brRt.offsetMin = new Vector2(14f, 12f);
            brRt.offsetMax = new Vector2(-14f, -72f);

            _sharedUI = RetroStatsSharedUI.BuildStatsBody(bodyRoot.transform, assets, font);

            // Bind category filter tabs & page buttons
            if (_sharedUI.tabButtons != null)
            {
                for (int i = 0; i < _sharedUI.tabButtons.Length; i++)
                {
                    int idx = i;
                    Button btn = _sharedUI.tabButtons[i];
                    if (btn == null) continue;
                    btn.onClick.AddListener(() =>
                    {
                        _activeFilter = (RetroStatsSharedUI.LevelFilter)idx;
                        RefreshRecords();
                        if (_sharedUI.levelsScrollRect != null)
                            _sharedUI.levelsScrollRect.verticalNormalizedPosition = 1f;
                    });
                }
            }

            if (_sharedUI.pageUpButton != null)
                _sharedUI.pageUpButton.onClick.AddListener(() => ScrollLevelsBy(0.38f));
            if (_sharedUI.pageDownButton != null)
                _sharedUI.pageDownButton.onClick.AddListener(() => ScrollLevelsBy(-0.38f));
        }

        public void RefreshRecords()
        {
            if (!_built) return;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            if (_parentWindow != null) _parentWindow.setTitle(dlg.scores_window_title);
            if (_subtitleText != null) _subtitleText.text = dlg.scores_subtitle;
            if (_refreshBtnLabel != null) _refreshBtnLabel.text = dlg.scores_refresh_button;
            if (_resetBtnLabel != null) _resetBtnLabel.text = dlg.scores_reset_button;

            if (_resetModalTitle != null) _resetModalTitle.text = dlg.scores_reset_modal_title;
            if (_resetModalPrompt != null) _resetModalPrompt.text = dlg.scores_reset_modal_prompt;
            if (_resetModalConfirmLabel != null) _resetModalConfirmLabel.text = dlg.scores_reset_modal_confirm;

            RetroStatsSharedUI.RenderAll(_sharedUI, _activeFilter, RetroThemeAssets.Instance, TMP_Settings.defaultFontAsset);
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

        private void OpenResetConfirmModal()
        {
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();
            if (_resetModalRoot == null)
                BuildResetConfirmModal();

            if (_resetModalTitle != null) _resetModalTitle.text = dlg.scores_reset_modal_title;
            if (_resetModalPrompt != null) _resetModalPrompt.text = dlg.scores_reset_modal_prompt;
            if (_resetModalConfirmLabel != null) _resetModalConfirmLabel.text = dlg.scores_reset_modal_confirm;
            if (_resetConfirmInput != null) _resetConfirmInput.text = string.Empty;

            _resetModalRoot.SetActive(true);
            _resetModalRoot.transform.SetAsLastSibling();
            if (_resetConfirmInput != null)
            {
                _resetConfirmInput.Select();
                _resetConfirmInput.ActivateInputField();
            }
        }

        private void ConfirmResetRecords()
        {
            string typed = _resetConfirmInput != null ? _resetConfirmInput.text.Trim() : string.Empty;
            if (!string.Equals(typed, "RESET", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (ProgressData.ExistingInstance != null)
                ProgressData.ExistingInstance.ClearHistory();
            if (SkillManager.Instance != null)
                SkillManager.Instance.ResetAllScores();
            PlaytestDataManager.ClearCurrentStudentSessionData();

            if (_resetModalRoot != null)
                _resetModalRoot.SetActive(false);

            RefreshRecords();
        }

        private void BuildResetConfirmModal()
        {
            RetroThemeAssets assets = RetroThemeAssets.Instance;
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            _resetModalRoot = new GameObject("ResetConfirmOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _resetModalRoot.transform.SetParent(transform, false);
            Image scrim = _resetModalRoot.GetComponent<Image>();
            scrim.color = RetroUITheme.OverlayScrim;
            scrim.raycastTarget = true;
            RectTransform ovRt = _resetModalRoot.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;

            GameObject win = new GameObject("ResetWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            win.transform.SetParent(_resetModalRoot.transform, false);
            RetroUITheme.ApplySlicedSprite(win.GetComponent<Image>(), assets != null ? assets.windowBody : null, RetroUITheme.WindowCream, true);
            RectTransform wRt = win.GetComponent<RectTransform>();
            wRt.anchorMin = new Vector2(0.5f, 0.5f);
            wRt.anchorMax = new Vector2(0.5f, 0.5f);
            wRt.sizeDelta = new Vector2(520f, 290f);

            GameObject bar = new GameObject("TitleBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(win.transform, false);
            RetroUITheme.ApplySlicedSprite(bar.GetComponent<Image>(), assets != null ? assets.windowTitleBar : null, RetroUITheme.BtnCoralRed, false);
            RectTransform bRt = bar.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.offsetMin = new Vector2(4f, -48f);
            bRt.offsetMax = new Vector2(-4f, -4f);

            _resetModalTitle = CreateTmp("ModalTitle", bar.transform, font, dlg.scores_reset_modal_title, 18, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform mtRt = _resetModalTitle.GetComponent<RectTransform>();
            mtRt.anchorMin = Vector2.zero;
            mtRt.anchorMax = Vector2.one;
            mtRt.offsetMin = new Vector2(16f, 2f);
            mtRt.offsetMax = new Vector2(-16f, -2f);

            _resetModalPrompt = CreateTmp("ModalPrompt", win.transform, font, dlg.scores_reset_modal_prompt, 16, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform mpRt = _resetModalPrompt.GetComponent<RectTransform>();
            mpRt.anchorMin = new Vector2(0f, 1f);
            mpRt.anchorMax = new Vector2(1f, 1f);
            mpRt.offsetMin = new Vector2(24f, -118f);
            mpRt.offsetMax = new Vector2(-24f, -64f);

            // Retro input field
            GameObject inputGo = new GameObject("Input-ConfirmReset", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(win.transform, false);
            RetroUITheme.ApplySlicedSprite(inputGo.GetComponent<Image>(), assets != null ? assets.inputBox : null, RetroUITheme.InputSurface, true);
            RectTransform inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0f, 1f);
            inRt.anchorMax = new Vector2(1f, 1f);
            inRt.offsetMin = new Vector2(48f, -182f);
            inRt.offsetMax = new Vector2(-48f, -130f);

            GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputGo.transform, false);
            RectTransform taRt = textArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(14f, 6f);
            taRt.offsetMax = new Vector2(-14f, -6f);

            TMP_Text txt = CreateTmp("Text", textArea.transform, font, "", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;

            TMP_Text ph = CreateTmp("Placeholder", textArea.transform, font, "RESET", 18, FontStyles.Italic, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform phRt = ph.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;

            _resetConfirmInput = inputGo.GetComponent<TMP_InputField>();
            _resetConfirmInput.textViewport = taRt;
            _resetConfirmInput.textComponent = txt;
            _resetConfirmInput.placeholder = ph;

            string cancelLabel = LocalizationManager.CurrentLanguage == LocalizationManager.LANG_ES ? "Cancelar"
                : (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_NO ? "Avbryt" : "Cancel");
            Button cancelBtn = CreateRetroButton("Btn-Cancel", win.transform, assets, font, cancelLabel,
                RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 16, out _);
            RectTransform cbRt = cancelBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0f);
            cbRt.anchorMax = new Vector2(0.47f, 0f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.offsetMin = new Vector2(28f, 22f);
            cbRt.offsetMax = new Vector2(-6f, 70f);
            cancelBtn.onClick.AddListener(() => _resetModalRoot.SetActive(false));

            Button confirmBtn = CreateRetroButton("Btn-Confirm", win.transform, assets, font, dlg.scores_reset_modal_confirm,
                RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 16, out _resetModalConfirmLabel);
            RectTransform cfRt = confirmBtn.GetComponent<RectTransform>();
            cfRt.anchorMin = new Vector2(0.53f, 0f);
            cfRt.anchorMax = new Vector2(1f, 0f);
            cfRt.pivot = new Vector2(0.5f, 0f);
            cfRt.offsetMin = new Vector2(6f, 22f);
            cfRt.offsetMax = new Vector2(-28f, 70f);
            confirmBtn.onClick.AddListener(ConfirmResetRecords);
        }

        private static Button CreateRetroButton(string name, Transform parent, RetroThemeAssets assets, TMP_FontAsset font, string labelText, Color bg, Color ink, float fontSize, out TMP_Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RetroUITheme.ApplySlicedSprite(go.GetComponent<Image>(), assets != null ? assets.button3D : null, bg, true);
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            RetroUITheme.ConfigureButtonColors(btn, bg);

            label = CreateTmp("Label", go.transform, font, labelText, fontSize, FontStyles.Bold, ink, TextAlignmentOptions.Center);
            RectTransform lRt = label.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(8f, 2f);
            lRt.offsetMax = new Vector2(-8f, -2f);
            return btn;
        }

        private static TMP_Text CreateTmp(string name, Transform parent, TMP_FontAsset font, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
