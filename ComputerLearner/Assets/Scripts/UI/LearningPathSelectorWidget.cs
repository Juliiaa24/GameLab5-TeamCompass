/**
 * Author: TeamCompass
 * Description: Clean Learning Path selector and independent Assessment Level ON/OFF button on the Main Menu.
 *              Supports the 3 progressive student flows (Beginner Foundations, Intermediate, Advanced),
 *              all completing Levels 1-16 with different levels of tutorial guidance.
 */
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ComputerLearning
{
    public class LearningPathSelectorWidget : MonoBehaviour
    {
        public static LearningPathSelectorWidget Instance { get; private set; }

        private static readonly LearningPath[] SelectablePaths =
        {
            LearningPath.BeginnerFoundations,
            LearningPath.Intermediate,
            LearningPath.Advanced
        };

        private Image[] pathButtonBackgrounds;
        private TextMeshProUGUI[] pathButtonTitles;
        private TextMeshProUGUI[] pathButtonSubtitles;
        private TextMeshProUGUI headerTitleLabel;
        private Image assessmentToggleBg;
        private TextMeshProUGUI assessmentToggleLabel;
        private TextMeshProUGUI summaryFooterText;

        private readonly Color activePathBgColor = new Color(0.15f, 0.39f, 0.92f, 1f);
        private readonly Color inactivePathBgColor = Color.white;
        private readonly Color activePathTitleColor = Color.white;
        private readonly Color activePathSubtitleColor = new Color(0.88f, 0.93f, 1f, 1f);
        private readonly Color inactivePathTitleColor = new Color(0.14f, 0.18f, 0.25f, 1f);
        private readonly Color inactivePathSubtitleColor = new Color(0.38f, 0.45f, 0.55f, 1f);

        private readonly Color assessmentOnBgColor = new Color(0.09f, 0.60f, 0.36f, 1f);
        private readonly Color assessmentOffBgColor = new Color(0.39f, 0.45f, 0.55f, 1f);

        public static LearningPathSelectorWidget EnsureOnCanvas(Transform canvasTransform)
        {
            if (canvasTransform == null) return null;
            if (SceneManager.GetActiveScene().name != "Menu") return null;

            Transform existing = canvasTransform.Find("LearningPathSelectorWidget");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject rootObj = new GameObject("LearningPathSelectorWidget", typeof(RectTransform), typeof(LearningPathSelectorWidget));
            rootObj.transform.SetParent(canvasTransform, false);
            rootObj.transform.SetAsLastSibling();

            LearningPathSelectorWidget widget = rootObj.GetComponent<LearningPathSelectorWidget>();
            widget.BuildUI(canvasTransform);
            return widget;
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != "Menu")
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SessionManager.Instance.OnConfigurationChanged += HandleConfigurationChanged;
            LocalizationManager.OnLanguageChanged += RefreshUI;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (SessionManager.ExistingInstance != null)
            {
                SessionManager.ExistingInstance.OnConfigurationChanged -= HandleConfigurationChanged;
            }
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu")
            {
                Destroy(gameObject);
            }
        }

        private void HandleConfigurationChanged(StudentConfiguration cfg)
        {
            RefreshUI();
        }

        private void BuildUI(Transform canvasTransform)
        {
            _ = SessionManager.Instance;

            Sprite menuRoundedSprite = null;
            TMP_FontAsset menuFont = null;

            Transform btnPlayT = canvasTransform.Find("Panel (1)/Btn-Play") ?? canvasTransform.Find("Btn-Play");
            if (btnPlayT == null)
            {
                Button[] allBtns = canvasTransform.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b != null && b.name == "Btn-Play")
                    {
                        btnPlayT = b.transform;
                        break;
                    }
                }
            }

            if (btnPlayT != null)
            {
                Image btnImg = btnPlayT.GetComponent<Image>();
                if (btnImg != null) menuRoundedSprite = btnImg.sprite;
                TMP_Text btnTmp = btnPlayT.GetComponentInChildren<TMP_Text>(true);
                if (btnTmp != null) menuFont = btnTmp.font;
            }

            Color cardBgColor = new Color(0.95f, 0.96f, 0.98f, 0.97f);
            Color headerTextColor = new Color(0.12f, 0.16f, 0.23f, 1f);
            Color subtleTextColor = new Color(0.32f, 0.38f, 0.47f, 1f);

            // Top-left card, clear of center logo/buttons and bottom-left PlaytestStudentWidget
            RectTransform rootRt = GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = new Vector2(40f, -28f);
            rootRt.sizeDelta = new Vector2(464f, 322f);

            Image cardImg = gameObject.AddComponent<Image>();
            cardImg.color = cardBgColor;
            cardImg.raycastTarget = true;
            if (menuRoundedSprite != null)
            {
                cardImg.sprite = menuRoundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            // Header Title
            headerTitleLabel = CreateText("HeaderLabel", transform, menuFont, 18f, FontStyles.Bold, headerTextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform headerRt = headerTitleLabel.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -12f);
            headerRt.sizeDelta = new Vector2(-30f, 28f);

            // Prominent Assessment Level ON/OFF Button (full width row right below header)
            GameObject toggleObj = new GameObject("Btn-ToggleAssessment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            toggleObj.transform.SetParent(transform, false);
            RectTransform toggleRt = toggleObj.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0f, 1f);
            toggleRt.anchorMax = new Vector2(1f, 1f);
            toggleRt.pivot = new Vector2(0.5f, 1f);
            toggleRt.anchoredPosition = new Vector2(0f, -46f);
            toggleRt.sizeDelta = new Vector2(-28f, 40f);

            assessmentToggleBg = toggleObj.GetComponent<Image>();
            assessmentToggleBg.color = assessmentOnBgColor;
            if (menuRoundedSprite != null)
            {
                assessmentToggleBg.sprite = menuRoundedSprite;
                assessmentToggleBg.type = Image.Type.Sliced;
            }

            Button toggleBtn = toggleObj.GetComponent<Button>();
            toggleBtn.targetGraphic = assessmentToggleBg;
            ColorBlock toggleCb = toggleBtn.colors;
            toggleCb.normalColor = Color.white;
            toggleCb.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            toggleCb.pressedColor = new Color(0.84f, 0.88f, 0.94f, 1f);
            toggleCb.selectedColor = Color.white;
            toggleBtn.colors = toggleCb;
            toggleBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            toggleBtn.onClick.AddListener(OnToggleAssessmentClicked);

            assessmentToggleLabel = CreateText("Label", toggleObj.transform, menuFont, 15.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            assessmentToggleLabel.richText = true;
            StretchFull(assessmentToggleLabel.GetComponent<RectTransform>(), 12f, 0f);

            // 3 Learning Path Flow Buttons (Title + Subtitle inside each row)
            pathButtonBackgrounds = new Image[SelectablePaths.Length];
            pathButtonTitles = new TextMeshProUGUI[SelectablePaths.Length];
            pathButtonSubtitles = new TextMeshProUGUI[SelectablePaths.Length];

            float startY = -98f;
            float rowHeight = 54f;
            float rowSpacing = 8f;

            for (int i = 0; i < SelectablePaths.Length; i++)
            {
                LearningPath path = SelectablePaths[i];
                int capturedIndex = i;

                GameObject btnObj = new GameObject($"Btn-Path-{path}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(transform, false);
                RectTransform btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(0.5f, 1f);
                btnRt.anchoredPosition = new Vector2(0f, startY - (i * (rowHeight + rowSpacing)));
                btnRt.sizeDelta = new Vector2(-28f, rowHeight);

                Image bg = btnObj.GetComponent<Image>();
                bg.color = inactivePathBgColor;
                if (menuRoundedSprite != null)
                {
                    bg.sprite = menuRoundedSprite;
                    bg.type = Image.Type.Sliced;
                }

                Button btn = btnObj.GetComponent<Button>();
                btn.targetGraphic = bg;
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(0.94f, 0.96f, 1f, 1f);
                cb.pressedColor = new Color(0.86f, 0.90f, 0.97f, 1f);
                cb.selectedColor = Color.white;
                btn.colors = cb;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.onClick.AddListener(() => OnPathButtonClicked(SelectablePaths[capturedIndex]));

                TextMeshProUGUI titleLbl = CreateText("Title", btnObj.transform, menuFont, 16f, FontStyles.Bold, inactivePathTitleColor, TextAlignmentOptions.MidlineLeft);
                RectTransform titleRt = titleLbl.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0.5f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = new Vector2(14f, -2f);
                titleRt.offsetMax = new Vector2(-14f, -4f);

                TextMeshProUGUI subLbl = CreateText("Subtitle", btnObj.transform, menuFont, 13f, FontStyles.Normal, inactivePathSubtitleColor, TextAlignmentOptions.MidlineLeft);
                RectTransform subRt = subLbl.GetComponent<RectTransform>();
                subRt.anchorMin = new Vector2(0f, 0f);
                subRt.anchorMax = new Vector2(1f, 0.5f);
                subRt.offsetMin = new Vector2(14f, 4f);
                subRt.offsetMax = new Vector2(-14f, 2f);

                pathButtonBackgrounds[i] = bg;
                pathButtonTitles[i] = titleLbl;
                pathButtonSubtitles[i] = subLbl;
            }

            // Footer summary line
            summaryFooterText = CreateText("SummaryFooter", transform, menuFont, 13f, FontStyles.Normal, subtleTextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform footerRt = summaryFooterText.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, 8f);
            footerRt.sizeDelta = new Vector2(-28f, 28f);

            RefreshUI();
        }

        private void OnPathButtonClicked(LearningPath selectedPath)
        {
            FeedbackManager.PlayClick();
            SessionManager.Instance.SetLearningPath(selectedPath);
            RefreshUI();

            if (PlaytestStudentWidget.Instance != null)
            {
                StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
                PlaytestStudentWidget.Instance.ShowToast(
                    $"{StudentConfiguration.GetPathDisplayName(selectedPath)}  ({StudentConfiguration.GetPathShortSummary(cfg)})",
                    3.2f);
            }
        }

        private void OnToggleAssessmentClicked()
        {
            FeedbackManager.PlayClick();
            StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
            bool nextState = cfg == null || !cfg.runAssessment;
            SessionManager.Instance.SetRunAssessmentOverride(nextState);
            RefreshUI();

            if (PlaytestStudentWidget.Instance != null)
            {
                bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);
                string msg = isEs
                    ? (nextState
                        ? "Assessment inicial ACTIVADO antes del flujo"
                        : "Assessment inicial DESACTIVADO (se usa perfil de habilidades del flujo)")
                    : (nextState
                        ? "Assessment Level ENABLED before starting flow"
                        : "Assessment Level DISABLED (using flow skill preset)");
                PlaytestStudentWidget.Instance.ShowToast(msg, 3.0f);
            }
        }

        public void RefreshUI()
        {
            StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
            if (cfg == null) return;

            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);

            if (headerTitleLabel != null)
            {
                headerTitleLabel.text = isEs ? "Flujo de Aprendizaje (Learning Path)" : "Learning Path";
            }

            if (assessmentToggleBg != null)
            {
                assessmentToggleBg.color = cfg.runAssessment ? assessmentOnBgColor : assessmentOffBgColor;
            }

            if (assessmentToggleLabel != null)
            {
                assessmentToggleLabel.text = isEs
                    ? (cfg.runAssessment
                        ? "Assessment Level:  ACTIVO  (Clic para desactivar)"
                        : "Assessment Level:  DESACTIVADO  (Clic para activar)")
                    : (cfg.runAssessment
                        ? "Assessment Level:  ON  (Click to disable)"
                        : "Assessment Level:  OFF  (Click to enable)");
                assessmentToggleLabel.ForceMeshUpdate();
            }

            if (pathButtonBackgrounds != null && pathButtonTitles != null && pathButtonSubtitles != null)
            {
                for (int i = 0; i < SelectablePaths.Length; i++)
                {
                    LearningPath path = SelectablePaths[i];
                    bool isSelected = cfg.learningPath == path;

                    if (pathButtonBackgrounds[i] != null)
                    {
                        pathButtonBackgrounds[i].color = isSelected ? activePathBgColor : inactivePathBgColor;
                    }
                    if (pathButtonTitles[i] != null)
                    {
                        pathButtonTitles[i].text = $"{i + 1}. {StudentConfiguration.GetPathDisplayName(path)}";
                        pathButtonTitles[i].color = isSelected ? activePathTitleColor : inactivePathTitleColor;
                    }
                    if (pathButtonSubtitles[i] != null)
                    {
                        pathButtonSubtitles[i].text = GetRowSubtitle(path, isEs);
                        pathButtonSubtitles[i].color = isSelected ? activePathSubtitleColor : inactivePathSubtitleColor;
                    }
                }
            }

            if (summaryFooterText != null)
            {
                summaryFooterText.text = StudentConfiguration.GetPathShortSummary(cfg);
            }
        }

        private static string GetRowSubtitle(LearningPath path, bool isEs)
        {
            switch (path)
            {
                case LearningPath.BeginnerFoundations:
                    return isEs
                        ? "Guiado completo desde inicio  •  Niveles 1–16 progresivos"
                        : "Full initial guided tutorial  •  Levels 1–16 progressive";

                case LearningPath.Intermediate:
                    return isEs
                        ? "Sin tutorial inicial, con tutorial de ventanas  •  Niveles 1–16"
                        : "Window tutorial only, then full OS  •  Levels 1–16 progressive";

                case LearningPath.Advanced:
                    return isEs
                        ? "Escritorio virtual libre sin tutoriales  •  Niveles 1–16"
                        : "Direct Virtual Desktop, no tutorials  •  Levels 1–16 progressive";

                default:
                    return string.Empty;
            }
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float horizontalPadding, float verticalPadding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rt.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }
    }
}
