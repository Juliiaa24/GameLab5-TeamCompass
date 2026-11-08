/**
 * Author: TeamCompass
 * Description: Clean, minimalist Learning Path selector panel on the Main Menu (offline teacher / playtest control).
 *              Matches Menu.unity's rounded light aesthetic and connects directly to SessionManager
 *              so all 5 Learning Paths (plus optional Assessment override) can be tested locally.
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
            LearningPath.DiagnosticFull,
            LearningPath.BeginnerFoundations,
            LearningPath.StandardIntermediate,
            LearningPath.AdvancedApplications,
            LearningPath.FreePractice
        };

        private Image[] pathButtonBackgrounds;
        private TextMeshProUGUI[] pathButtonLabels;
        private Image assessmentToggleBg;
        private TextMeshProUGUI assessmentToggleLabel;
        private TextMeshProUGUI summaryFooterText;

        private readonly Color activePathBgColor = new Color(0.15f, 0.39f, 0.92f, 1f);
        private readonly Color inactivePathBgColor = Color.white;
        private readonly Color activePathTextColor = Color.white;
        private readonly Color inactivePathTextColor = new Color(0.16f, 0.20f, 0.27f, 1f);

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
            // Ensure SessionManager is initialized
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
            rootRt.anchoredPosition = new Vector2(40f, -30f);
            rootRt.sizeDelta = new Vector2(456f, 326f);

            Image cardImg = gameObject.AddComponent<Image>();
            cardImg.color = cardBgColor;
            cardImg.raycastTarget = true;
            if (menuRoundedSprite != null)
            {
                cardImg.sprite = menuRoundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            // Header Title ("Learning Path")
            TextMeshProUGUI headerLabel = CreateText("HeaderLabel", transform, menuFont, 18f, FontStyles.Bold, headerTextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform headerRt = headerLabel.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(0f, 1f);
            headerRt.pivot = new Vector2(0f, 1f);
            headerRt.anchoredPosition = new Vector2(16f, -12f);
            headerRt.sizeDelta = new Vector2(235f, 32f);
            headerLabel.text = "Learning Path";

            // Assessment Override Toggle Pill in top-right of card
            GameObject toggleObj = new GameObject("Btn-ToggleAssessment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            toggleObj.transform.SetParent(transform, false);
            RectTransform toggleRt = toggleObj.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(1f, 1f);
            toggleRt.anchorMax = new Vector2(1f, 1f);
            toggleRt.pivot = new Vector2(1f, 1f);
            toggleRt.anchoredPosition = new Vector2(-14f, -12f);
            toggleRt.sizeDelta = new Vector2(182f, 32f);

            assessmentToggleBg = toggleObj.GetComponent<Image>();
            assessmentToggleBg.color = Color.white;
            if (menuRoundedSprite != null)
            {
                assessmentToggleBg.sprite = menuRoundedSprite;
                assessmentToggleBg.type = Image.Type.Sliced;
            }

            Button toggleBtn = toggleObj.GetComponent<Button>();
            toggleBtn.targetGraphic = assessmentToggleBg;
            toggleBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            toggleBtn.onClick.AddListener(OnToggleAssessmentClicked);

            assessmentToggleLabel = CreateText("Label", toggleObj.transform, menuFont, 14f, FontStyles.Bold, headerTextColor, TextAlignmentOptions.Center);
            assessmentToggleLabel.richText = true;
            StretchFull(assessmentToggleLabel.GetComponent<RectTransform>(), 6f, 0f);

            // 5 Path Buttons stacked cleanly
            pathButtonBackgrounds = new Image[SelectablePaths.Length];
            pathButtonLabels = new TextMeshProUGUI[SelectablePaths.Length];

            float startY = -54f;
            float rowHeight = 40f;
            float rowSpacing = 6f;

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

                TextMeshProUGUI lbl = CreateText("Text", btnObj.transform, menuFont, 16f, FontStyles.Normal, inactivePathTextColor, TextAlignmentOptions.MidlineLeft);
                StretchFull(lbl.GetComponent<RectTransform>(), 14f, 0f);
                lbl.text = GetDetailedRowLabel(path, i + 1);

                pathButtonBackgrounds[i] = bg;
                pathButtonLabels[i] = lbl;
            }

            // Footer summary line
            summaryFooterText = CreateText("SummaryFooter", transform, menuFont, 13.5f, FontStyles.Normal, subtleTextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform footerRt = summaryFooterText.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, 10f);
            footerRt.sizeDelta = new Vector2(-28f, 30f);

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
                    $"Path set: {StudentConfiguration.GetPathDisplayName(selectedPath)} ({StudentConfiguration.GetPathShortSummary(cfg)})",
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
                PlaytestStudentWidget.Instance.ShowToast(
                    nextState
                        ? "Initial Assessment enabled before starting path"
                        : "Initial Assessment skipped (using path skill preset)",
                    3.0f);
            }
        }

        public void RefreshUI()
        {
            StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
            if (cfg == null) return;

            if (pathButtonBackgrounds != null && pathButtonLabels != null)
            {
                for (int i = 0; i < SelectablePaths.Length; i++)
                {
                    bool isSelected = cfg.learningPath == SelectablePaths[i];
                    if (pathButtonBackgrounds[i] != null)
                    {
                        pathButtonBackgrounds[i].color = isSelected ? activePathBgColor : inactivePathBgColor;
                    }
                    if (pathButtonLabels[i] != null)
                    {
                        pathButtonLabels[i].color = isSelected ? activePathTextColor : inactivePathTextColor;
                        pathButtonLabels[i].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                    }
                }
            }

            if (assessmentToggleLabel != null)
            {
                assessmentToggleLabel.text = cfg.runAssessment
                    ? "Assessment: <color=#16a34a><b>ON</b></color>"
                    : "Assessment: <color=#64748b><b>SKIP</b></color>";
                assessmentToggleLabel.ForceMeshUpdate();
            }

            if (summaryFooterText != null)
            {
                summaryFooterText.text = StudentConfiguration.GetPathShortSummary(cfg);
            }
        }

        private static string GetDetailedRowLabel(LearningPath path, int number)
        {
            switch (path)
            {
                case LearningPath.DiagnosticFull:
                    return $"{number}. Diagnostic  -  Test + Full Curriculum (L1-L16)";
                case LearningPath.BeginnerFoundations:
                    return $"{number}. Beginner  -  Guided Basics (L1-L9, No Test)";
                case LearningPath.StandardIntermediate:
                    return $"{number}. Standard  -  Core Mouse & OS (Starts L6)";
                case LearningPath.AdvancedApplications:
                    return $"{number}. Advanced  -  OS, Shortcuts & Apps (Starts L10)";
                case LearningPath.FreePractice:
                    return $"{number}. Free Practice  -  Open Sandbox (All 16 Unlocked)";
                default:
                    return $"{number}. {path}";
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
