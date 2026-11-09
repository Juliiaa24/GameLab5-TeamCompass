/**
 * Author: TeamCompass
 * Description: Full Teacher Host Dashboard UI overlay on Menu.unity (Step 5).
 *              Displays LAN server IP/status, classroom-wide Learning Path + Assessment batch controls,
 *              per-student path/assessment assignment, remote Start/Reset controls, and a live
 *              real-time telemetry inspector (scene, progressive levels 1-16, medals, and 9 skill bars).
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ComputerLearning
{
    public class TeacherDashboardController : MonoBehaviour
    {
        public static TeacherDashboardController Instance { get; private set; }

        public static bool IsDashboardOpen => Instance != null && Instance.gameObject.activeInHierarchy;

        private static readonly LearningPath[] DashboardPaths =
        {
            LearningPath.BeginnerFoundations,
            LearningPath.Intermediate,
            LearningPath.Advanced
        };

        private Sprite roundedSprite;
        private TMP_FontAsset uiFont;

        // Top Bar
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI serverInfoLabel;
        private Image btnToggleServerBg;
        private TextMeshProUGUI btnToggleServerLabel;
        private TextMeshProUGUI btnClearOfflineLabel;

        // Batch Controls Bar
        private TextMeshProUGUI batchSectionLabel;
        private Image[] batchPathBtnBgs;
        private TextMeshProUGUI[] batchPathBtnLabels;
        private Image batchAssessBtnBg;
        private TextMeshProUGUI batchAssessBtnLabel;
        private TextMeshProUGUI btnApplyAllLabel;
        private TextMeshProUGUI btnStartAllLabel;

        // Students List
        private TextMeshProUGUI listHeaderLabel;
        private RectTransform studentsContentRect;
        private GameObject emptyStatePanel;
        private TextMeshProUGUI emptyStateText;

        // Right Inspector Panel
        private TextMeshProUGUI inspectorTitleLabel;
        private TextMeshProUGUI inspectorMetaLabel;
        private TextMeshProUGUI inspectorKpiText;
        private GameObject inspectorSkillsContainer;
        private readonly Dictionary<string, SkillBarWidgets> skillBars = new Dictionary<string, SkillBarWidgets>();

        private string selectedClientUid = string.Empty;

        private struct SkillBarWidgets
        {
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI valueLabel;
            public RectTransform fillRect;
            public Image fillImage;
        }

        private readonly Color primaryBlue = new Color(0.15f, 0.39f, 0.92f, 1f);
        private readonly Color emeraldGreen = new Color(0.09f, 0.60f, 0.36f, 1f);
        private readonly Color dangerRed = new Color(0.82f, 0.24f, 0.24f, 1f);
        private readonly Color amberColor = new Color(0.85f, 0.52f, 0.08f, 1f);
        private readonly Color darkSlate = new Color(0.12f, 0.16f, 0.23f, 1f);
        private readonly Color mutedSlate = new Color(0.38f, 0.45f, 0.55f, 1f);
        private readonly Color lightPanelBg = new Color(0.96f, 0.97f, 0.99f, 1f);
        private readonly Color cardBorderTint = new Color(0.88f, 0.91f, 0.95f, 1f);

        public static TeacherDashboardController OpenOnCanvas(Transform canvasTransform)
        {
            if (canvasTransform == null) return null;

            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                Instance.transform.SetAsLastSibling();
                Instance.RefreshAll();
                return Instance;
            }

            GameObject modalRoot = new GameObject("TeacherDashboardModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TeacherDashboardController));
            modalRoot.transform.SetParent(canvasTransform, false);
            modalRoot.transform.SetAsLastSibling();

            TeacherDashboardController ctrl = modalRoot.GetComponent<TeacherDashboardController>();
            ctrl.BuildDashboardUI(canvasTransform);
            ctrl.RefreshAll();
            return ctrl;
        }

        public static void CloseDashboard()
        {
            if (Instance != null)
            {
                Instance.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            NetworkSessionHost.Instance.OnHostStateChanged += RefreshAll;
            NetworkSessionHost.Instance.OnStudentsChanged += RefreshAll;
            NetworkSessionHost.Instance.OnStudentTelemetryUpdated += HandleStudentTelemetryUpdated;
            LocalizationManager.OnLanguageChanged += RefreshAll;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (NetworkSessionHost.ExistingInstance != null)
            {
                NetworkSessionHost.ExistingInstance.OnHostStateChanged -= RefreshAll;
                NetworkSessionHost.ExistingInstance.OnStudentsChanged -= RefreshAll;
                NetworkSessionHost.ExistingInstance.OnStudentTelemetryUpdated -= HandleStudentTelemetryUpdated;
            }
            LocalizationManager.OnLanguageChanged -= RefreshAll;
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

        private void HandleStudentTelemetryUpdated(ConnectedStudentSession updatedSession)
        {
            // Refresh full view so both the student's row and inspector stay live
            RefreshStudentsListAndInspector();
        }

        private void BuildDashboardUI(Transform canvasTransform)
        {
            Transform btnPlayT = canvasTransform.Find("Panel (1)/Btn-Play") ?? canvasTransform.Find("Btn-Play");
            if (btnPlayT == null)
            {
                foreach (var b in canvasTransform.GetComponentsInChildren<Button>(true))
                {
                    if (b != null && b.name == "Btn-Play") { btnPlayT = b.transform; break; }
                }
            }
            if (btnPlayT != null)
            {
                Image img = btnPlayT.GetComponent<Image>();
                if (img != null) roundedSprite = img.sprite;
                TMP_Text tmp = btnPlayT.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) uiFont = tmp.font;
            }

            // Full-screen backdrop blocking clicks behind modal
            RectTransform backdropRt = GetComponent<RectTransform>();
            StretchFull(backdropRt, 0f, 0f);
            Image backdropImg = GetComponent<Image>();
            backdropImg.color = new Color(0.06f, 0.09f, 0.16f, 0.76f);
            backdropImg.raycastTarget = true;

            // Main Dashboard Card (1760 x 960 centered on 1920x1080 reference canvas)
            GameObject windowObj = new GameObject("DashboardWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            windowObj.transform.SetParent(transform, false);
            RectTransform winRt = windowObj.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.anchoredPosition = Vector2.zero;
            winRt.sizeDelta = new Vector2(1760f, 956f);

            Image winBg = windowObj.GetComponent<Image>();
            winBg.color = lightPanelBg;
            ApplyRounded(winBg);

            BuildTopHeaderBar(windowObj.transform);
            BuildClassroomBatchBar(windowObj.transform);
            BuildSplitContentArea(windowObj.transform);
        }

        private void BuildTopHeaderBar(Transform parent)
        {
            GameObject topBar = CreatePanel("TopHeaderBar", parent, Color.white);
            RectTransform rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(-36f, 70f);

            titleLabel = CreateText("Title", topBar.transform, 23f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            RectTransform titleRt = titleLabel.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0f, 1f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(22f, 0f);
            titleRt.sizeDelta = new Vector2(470f, 0f);

            serverInfoLabel = CreateText("ServerInfo", topBar.transform, 15.5f, FontStyles.Normal, darkSlate, TextAlignmentOptions.MidlineLeft);
            serverInfoLabel.richText = true;
            RectTransform infoRt = serverInfoLabel.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.offsetMin = new Vector2(500f, 0f);
            infoRt.offsetMax = new Vector2(-430f, 0f);

            // Clear Disconnected Button
            Button btnClear = CreateButton("Btn-ClearOffline", topBar.transform, new Color(0.91f, 0.93f, 0.96f, 1f), darkSlate, out btnClearOfflineLabel);
            RectTransform clearRt = btnClear.GetComponent<RectTransform>();
            clearRt.anchorMin = new Vector2(1f, 0.5f);
            clearRt.anchorMax = new Vector2(1f, 0.5f);
            clearRt.pivot = new Vector2(1f, 0.5f);
            clearRt.anchoredPosition = new Vector2(-248f, 0f);
            clearRt.sizeDelta = new Vector2(166f, 44f);
            btnClear.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionHost.Instance.RemoveDisconnectedStudents();
            });

            // Start / Stop Server Toggle Button
            Button btnServer = CreateButton("Btn-ToggleServer", topBar.transform, emeraldGreen, Color.white, out btnToggleServerLabel);
            btnToggleServerBg = btnServer.GetComponent<Image>();
            RectTransform srvRt = btnServer.GetComponent<RectTransform>();
            srvRt.anchorMin = new Vector2(1f, 0.5f);
            srvRt.anchorMax = new Vector2(1f, 0.5f);
            srvRt.pivot = new Vector2(1f, 0.5f);
            srvRt.anchoredPosition = new Vector2(-74f, 0f);
            srvRt.sizeDelta = new Vector2(164f, 44f);
            btnServer.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                if (NetworkSessionHost.Instance.IsHosting)
                    NetworkSessionHost.Instance.StopHost();
                else
                    NetworkSessionHost.Instance.StartHost();
                RefreshAll();
            });

            // Close Dashboard X Button
            Button btnClose = CreateButton("Btn-CloseDashboard", topBar.transform, new Color(0.91f, 0.93f, 0.96f, 1f), darkSlate, out TextMeshProUGUI closeTxt);
            RectTransform closeRt = btnClose.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-16f, 0f);
            closeRt.sizeDelta = new Vector2(46f, 44f);
            closeTxt.text = "X";
            closeTxt.fontSize = 18f;
            btnClose.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                CloseDashboard();
            });
        }

        private void BuildClassroomBatchBar(Transform parent)
        {
            GameObject batchBar = CreatePanel("ClassroomBatchBar", parent, Color.white);
            RectTransform rt = batchBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -98f);
            rt.sizeDelta = new Vector2(-36f, 68f);

            batchSectionLabel = CreateText("BatchLabel", batchBar.transform, 15.5f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            RectTransform lblRt = batchSectionLabel.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0f, 1f);
            lblRt.pivot = new Vector2(0f, 0.5f);
            lblRt.anchoredPosition = new Vector2(20f, 0f);
            lblRt.sizeDelta = new Vector2(230f, 0f);

            batchPathBtnBgs = new Image[DashboardPaths.Length];
            batchPathBtnLabels = new TextMeshProUGUI[DashboardPaths.Length];

            float xStart = 256f;
            float pillWidth = 224f;
            float pillGap = 10f;

            for (int i = 0; i < DashboardPaths.Length; i++)
            {
                LearningPath capturedPath = DashboardPaths[i];
                Button pathBtn = CreateButton($"Btn-BatchPath-{capturedPath}", batchBar.transform, cardBorderTint, darkSlate, out TextMeshProUGUI pLbl);
                RectTransform pRt = pathBtn.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0f, 0.5f);
                pRt.anchorMax = new Vector2(0f, 0.5f);
                pRt.pivot = new Vector2(0f, 0.5f);
                pRt.anchoredPosition = new Vector2(xStart + i * (pillWidth + pillGap), 0f);
                pRt.sizeDelta = new Vector2(pillWidth, 44f);

                pLbl.fontSize = 14.5f;
                batchPathBtnBgs[i] = pathBtn.GetComponent<Image>();
                batchPathBtnLabels[i] = pLbl;

                pathBtn.onClick.AddListener(() =>
                {
                    FeedbackManager.PlayClick();
                    NetworkSessionHost.Instance.SetClassroomDefaults(
                        capturedPath,
                        NetworkSessionHost.Instance.DefaultClassroomAssessment);
                    RefreshBatchControls();
                });
            }

            // Classroom Assessment Toggle
            Button assessBtn = CreateButton("Btn-BatchAssessment", batchBar.transform, emeraldGreen, Color.white, out batchAssessBtnLabel);
            batchAssessBtnBg = assessBtn.GetComponent<Image>();
            RectTransform aRt = assessBtn.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0f, 0.5f);
            aRt.anchorMax = new Vector2(0f, 0.5f);
            aRt.pivot = new Vector2(0f, 0.5f);
            aRt.anchoredPosition = new Vector2(xStart + 3 * (pillWidth + pillGap) + 8f, 0f);
            aRt.sizeDelta = new Vector2(210f, 44f);
            assessBtn.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionHost.Instance.SetClassroomDefaults(
                    NetworkSessionHost.Instance.DefaultClassroomPath,
                    !NetworkSessionHost.Instance.DefaultClassroomAssessment);
                RefreshBatchControls();
            });

            // Apply to All Button
            Button btnApplyAll = CreateButton("Btn-ApplyAll", batchBar.transform, primaryBlue, Color.white, out btnApplyAllLabel);
            RectTransform applyRt = btnApplyAll.GetComponent<RectTransform>();
            applyRt.anchorMin = new Vector2(1f, 0.5f);
            applyRt.anchorMax = new Vector2(1f, 0.5f);
            applyRt.pivot = new Vector2(1f, 0.5f);
            applyRt.anchoredPosition = new Vector2(-202f, 0f);
            applyRt.sizeDelta = new Vector2(196f, 44f);
            btnApplyAll.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionHost.Instance.ApplyClassroomDefaultsToAll(startImmediately: false);
            });

            // Start All Button
            Button btnStartAll = CreateButton("Btn-StartAll", batchBar.transform, emeraldGreen, Color.white, out btnStartAllLabel);
            RectTransform startRt = btnStartAll.GetComponent<RectTransform>();
            startRt.anchorMin = new Vector2(1f, 0.5f);
            startRt.anchorMax = new Vector2(1f, 0.5f);
            startRt.pivot = new Vector2(1f, 0.5f);
            startRt.anchoredPosition = new Vector2(-16f, 0f);
            startRt.sizeDelta = new Vector2(174f, 44f);
            btnStartAll.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionHost.Instance.ApplyClassroomDefaultsToAll(startImmediately: true);
            });
        }

        private void BuildSplitContentArea(Transform parent)
        {
            // Left Column: Connected Students Scrollable List
            GameObject leftPanel = CreatePanel("StudentsListPanel", parent, Color.white);
            RectTransform leftRt = leftPanel.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(1f, 1f);
            leftRt.offsetMin = new Vector2(18f, 18f);
            leftRt.offsetMax = new Vector2(-536f, -180f);

            listHeaderLabel = CreateText("ListHeader", leftPanel.transform, 14.5f, FontStyles.Bold, mutedSlate, TextAlignmentOptions.MidlineLeft);
            RectTransform hdrRt = listHeaderLabel.GetComponent<RectTransform>();
            hdrRt.anchorMin = new Vector2(0f, 1f);
            hdrRt.anchorMax = new Vector2(1f, 1f);
            hdrRt.pivot = new Vector2(0.5f, 1f);
            hdrRt.anchoredPosition = new Vector2(0f, -10f);
            hdrRt.sizeDelta = new Vector2(-32f, 30f);

            // ScrollView
            GameObject scrollObj = new GameObject("StudentsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(RectMask2D));
            scrollObj.transform.SetParent(leftPanel.transform, false);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(14f, 14f);
            scrollRt.offsetMax = new Vector2(-14f, -46f);

            Image scrollBg = scrollObj.GetComponent<Image>();
            scrollBg.color = new Color(0.97f, 0.98f, 0.99f, 1f);
            ApplyRounded(scrollBg);

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(scrollObj.transform, false);
            studentsContentRect = contentObj.GetComponent<RectTransform>();
            studentsContentRect.anchorMin = new Vector2(0f, 1f);
            studentsContentRect.anchorMax = new Vector2(1f, 1f);
            studentsContentRect.pivot = new Vector2(0.5f, 1f);
            studentsContentRect.anchoredPosition = Vector2.zero;
            studentsContentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = studentsContentRect;
            sr.viewport = scrollRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 28f;

            // Empty State Overlay inside Left Panel
            emptyStatePanel = new GameObject("EmptyState", typeof(RectTransform));
            emptyStatePanel.transform.SetParent(scrollObj.transform, false);
            StretchFull(emptyStatePanel.GetComponent<RectTransform>(), 40f, 40f);
            emptyStateText = CreateText("EmptyText", emptyStatePanel.transform, 16.5f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.Center);
            emptyStateText.textWrappingMode = TextWrappingModes.Normal;
            emptyStateText.richText = true;
            StretchFull(emptyStateText.GetComponent<RectTransform>(), 0f, 0f);

            // Right Column: Live Student Telemetry Inspector
            GameObject rightPanel = CreatePanel("StudentInspectorPanel", parent, Color.white);
            RectTransform rightRt = rightPanel.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.offsetMin = new Vector2(-522f, 18f);
            rightRt.offsetMax = new Vector2(-18f, -180f);

            inspectorTitleLabel = CreateText("InspectorTitle", rightPanel.transform, 19f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            RectTransform inspTitleRt = inspectorTitleLabel.GetComponent<RectTransform>();
            inspTitleRt.anchorMin = new Vector2(0f, 1f);
            inspTitleRt.anchorMax = new Vector2(1f, 1f);
            inspTitleRt.pivot = new Vector2(0.5f, 1f);
            inspTitleRt.anchoredPosition = new Vector2(0f, -14f);
            inspTitleRt.sizeDelta = new Vector2(-32f, 30f);

            inspectorMetaLabel = CreateText("InspectorMeta", rightPanel.transform, 13.5f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.MidlineLeft);
            inspectorMetaLabel.richText = true;
            RectTransform inspMetaRt = inspectorMetaLabel.GetComponent<RectTransform>();
            inspMetaRt.anchorMin = new Vector2(0f, 1f);
            inspMetaRt.anchorMax = new Vector2(1f, 1f);
            inspMetaRt.pivot = new Vector2(0.5f, 1f);
            inspMetaRt.anchoredPosition = new Vector2(0f, -46f);
            inspMetaRt.sizeDelta = new Vector2(-32f, 24f);

            GameObject kpiCard = CreatePanel("KpiCard", rightPanel.transform, new Color(0.96f, 0.97f, 0.99f, 1f));
            RectTransform kpiRt = kpiCard.GetComponent<RectTransform>();
            kpiRt.anchorMin = new Vector2(0f, 1f);
            kpiRt.anchorMax = new Vector2(1f, 1f);
            kpiRt.pivot = new Vector2(0.5f, 1f);
            kpiRt.anchoredPosition = new Vector2(0f, -78f);
            kpiRt.sizeDelta = new Vector2(-32f, 184f);

            inspectorKpiText = CreateText("KpiBody", kpiCard.transform, 14f, FontStyles.Normal, darkSlate, TextAlignmentOptions.TopLeft);
            inspectorKpiText.richText = true;
            inspectorKpiText.textWrappingMode = TextWrappingModes.Normal;
            StretchFull(inspectorKpiText.GetComponent<RectTransform>(), 14f, 12f);

            inspectorSkillsContainer = new GameObject("SkillsContainer", typeof(RectTransform));
            inspectorSkillsContainer.transform.SetParent(rightPanel.transform, false);
            RectTransform skillsRt = inspectorSkillsContainer.GetComponent<RectTransform>();
            skillsRt.anchorMin = new Vector2(0f, 0f);
            skillsRt.anchorMax = new Vector2(1f, 1f);
            skillsRt.offsetMin = new Vector2(16f, 16f);
            skillsRt.offsetMax = new Vector2(-16f, -274f);

            skillBars.Clear();
            for (int i = 0; i < SkillProfilePreset.AllKnownSkillIds.Length; i++)
            {
                string skillId = SkillProfilePreset.AllKnownSkillIds[i];
                GameObject barRow = new GameObject($"SkillBar_{skillId}", typeof(RectTransform));
                barRow.transform.SetParent(inspectorSkillsContainer.transform, false);
                RectTransform rowRt = barRow.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, -i * 50f);
                rowRt.sizeDelta = new Vector2(0f, 44f);

                TextMeshProUGUI sName = CreateText("SkillName", barRow.transform, 13.5f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
                RectTransform sNameRt = sName.GetComponent<RectTransform>();
                sNameRt.anchorMin = new Vector2(0f, 0.5f);
                sNameRt.anchorMax = new Vector2(0.75f, 1f);
                sNameRt.offsetMin = Vector2.zero;
                sNameRt.offsetMax = Vector2.zero;
                sName.text = FormatSkillDisplayName(skillId);

                TextMeshProUGUI sVal = CreateText("SkillVal", barRow.transform, 13.5f, FontStyles.Bold, mutedSlate, TextAlignmentOptions.MidlineRight);
                RectTransform sValRt = sVal.GetComponent<RectTransform>();
                sValRt.anchorMin = new Vector2(0.75f, 0.5f);
                sValRt.anchorMax = new Vector2(1f, 1f);
                sValRt.offsetMin = Vector2.zero;
                sValRt.offsetMax = Vector2.zero;

                GameObject trackObj = CreatePanel("Track", barRow.transform, new Color(0.89f, 0.92f, 0.96f, 1f));
                RectTransform trackRt = trackObj.GetComponent<RectTransform>();
                trackRt.anchorMin = new Vector2(0f, 0f);
                trackRt.anchorMax = new Vector2(1f, 0f);
                trackRt.pivot = new Vector2(0.5f, 0f);
                trackRt.anchoredPosition = new Vector2(0f, 4f);
                trackRt.sizeDelta = new Vector2(0f, 14f);

                GameObject fillObj = CreatePanel("Fill", trackObj.transform, primaryBlue);
                RectTransform fillRt = fillObj.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(0f, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;

                skillBars[skillId] = new SkillBarWidgets
                {
                    nameLabel = sName,
                    valueLabel = sVal,
                    fillRect = fillRt,
                    fillImage = fillObj.GetComponent<Image>()
                };
            }
        }

        public void RefreshAll()
        {
            if (!gameObject.activeInHierarchy) return;

            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);
            NetworkSessionHost host = NetworkSessionHost.Instance;

            if (titleLabel != null)
            {
                titleLabel.text = isEs ? "Panel del Profesor (Aula LAN)" : "Teacher Classroom Dashboard";
            }

            if (serverInfoLabel != null)
            {
                if (host.IsHosting)
                {
                    serverInfoLabel.text = isEs
                        ? $"Estado: <color=#16a34a><b>ACTIVO</b></color>   •   IP Profesor: <b>{host.LocalIpAddress}:{host.ListenPort}</b>   •   Alumnos conectados: <b>{host.ActiveConnectedCount}</b>"
                        : $"Server: <color=#16a34a><b>ONLINE</b></color>   •   Host IP: <b>{host.LocalIpAddress}:{host.ListenPort}</b>   •   Connected Students: <b>{host.ActiveConnectedCount}</b>";
                }
                else
                {
                    string err = !string.IsNullOrEmpty(host.LastError) ? $" ({host.LastError})" : string.Empty;
                    serverInfoLabel.text = isEs
                        ? $"Estado: <color=#dc2626><b>DETENIDO</b></color>{err}"
                        : $"Server: <color=#dc2626><b>OFFLINE</b></color>{err}";
                }
            }

            if (btnToggleServerBg != null && btnToggleServerLabel != null)
            {
                btnToggleServerBg.color = host.IsHosting ? dangerRed : emeraldGreen;
                btnToggleServerLabel.text = host.IsHosting
                    ? (isEs ? "Detener Servidor" : "Stop Server")
                    : (isEs ? "Iniciar Servidor" : "Start Server");
            }

            if (btnClearOfflineLabel != null)
            {
                btnClearOfflineLabel.text = isEs ? "Limpiar Desconectados" : "Clear Offline";
            }

            if (listHeaderLabel != null)
            {
                listHeaderLabel.text = isEs
                    ? "ALUMNO / EQUIPO                  ESCENA Y NIVEL              PROGRESO Y MEDALLAS              FLUJO ASIGNADO Y ASSESSMENT                            ACCIONES"
                    : "STUDENT / DEVICE                 SCENE & LEVEL               PROGRESS & MEDALS                ASSIGNED LEARNING PATH & ASSESSMENT                    ACTIONS";
            }

            RefreshBatchControls();
            RefreshStudentsListAndInspector();
        }

        private void RefreshBatchControls()
        {
            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);
            NetworkSessionHost host = NetworkSessionHost.Instance;

            if (batchSectionLabel != null)
            {
                batchSectionLabel.text = isEs ? "Configuración Global Aula:" : "Classroom Batch Config:";
            }

            for (int i = 0; i < DashboardPaths.Length; i++)
            {
                bool active = host.DefaultClassroomPath == DashboardPaths[i];
                if (batchPathBtnBgs[i] != null)
                    batchPathBtnBgs[i].color = active ? primaryBlue : new Color(0.92f, 0.94f, 0.97f, 1f);
                if (batchPathBtnLabels[i] != null)
                {
                    batchPathBtnLabels[i].color = active ? Color.white : darkSlate;
                    batchPathBtnLabels[i].text = $"{i + 1}. {StudentConfiguration.GetPathDisplayName(DashboardPaths[i])}";
                }
            }

            if (batchAssessBtnBg != null && batchAssessBtnLabel != null)
            {
                batchAssessBtnBg.color = host.DefaultClassroomAssessment ? emeraldGreen : mutedSlate;
                batchAssessBtnLabel.text = isEs
                    ? (host.DefaultClassroomAssessment ? "Assessment: ACTIVO" : "Assessment: NO")
                    : (host.DefaultClassroomAssessment ? "Assessment: ON" : "Assessment: OFF");
            }

            if (btnApplyAllLabel != null)
                btnApplyAllLabel.text = isEs ? "Aplicar a Todos" : "Apply to All";

            if (btnStartAllLabel != null)
                btnStartAllLabel.text = isEs ? "▶ Iniciar Todos" : "▶ Launch All";
        }

        private void RefreshStudentsListAndInspector()
        {
            if (studentsContentRect == null) return;

            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);
            IReadOnlyList<ConnectedStudentSession> students = NetworkSessionHost.Instance.ConnectedStudents;

            for (int i = studentsContentRect.childCount - 1; i >= 0; i--)
            {
                Destroy(studentsContentRect.GetChild(i).gameObject);
            }

            if (students == null || students.Count == 0)
            {
                emptyStatePanel.SetActive(true);
                emptyStateText.text = isEs
                    ? "<b>Esperando alumnos en la red local (LAN)...</b>\n\n" +
                      "1. Abre <b>Computer Learner</b> en los ordenadores de los alumnos (o una build local en este mismo PC).\n" +
                      "2. En el menú superior derecho, haz clic en <b>Unirse a Aula como Alumno (LAN)</b>.\n" +
                      $"3. Selecciona este profesor detectado automáticamente o introduce la IP <b>{NetworkSessionHost.Instance.LocalIpAddress}</b> (o <b>127.0.0.1</b> en el mismo PC)."
                    : "<b>Waiting for students to join over LAN...</b>\n\n" +
                      "1. Launch <b>Computer Learner</b> on student computers (or a second build on this PC).\n" +
                      "2. In the top-right menu card, click <b>Join Classroom as Student (LAN)</b>.\n" +
                      $"3. Click this auto-discovered Teacher Host or enter IP <b>{NetworkSessionHost.Instance.LocalIpAddress}</b> (or <b>127.0.0.1</b> on the same PC).";

                selectedClientUid = string.Empty;
                RefreshInspector(null);
                return;
            }

            emptyStatePanel.SetActive(false);

            ConnectedStudentSession selectedSession = NetworkSessionHost.Instance.GetStudent(selectedClientUid);
            if (selectedSession == null && students.Count > 0)
            {
                selectedSession = students[0];
                selectedClientUid = selectedSession.clientUid;
            }

            for (int i = 0; i < students.Count; i++)
            {
                BuildStudentRow(students[i], students[i].clientUid == selectedClientUid, isEs);
            }

            RefreshInspector(selectedSession);
        }

        private void BuildStudentRow(ConnectedStudentSession s, bool isSelected, bool isEs)
        {
            Color rowBg = isSelected
                ? new Color(0.90f, 0.94f, 1.0f, 1f)
                : Color.white;

            GameObject rowObj = CreatePanel($"StudentRow_{s.studentId}", studentsContentRect, rowBg);
            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0f, 78f);

            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.preferredHeight = 78f;
            le.minHeight = 78f;

            Button selectRowBtn = rowObj.AddComponent<Button>();
            selectRowBtn.targetGraphic = rowObj.GetComponent<Image>();
            selectRowBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            string capturedUid = s.clientUid;
            selectRowBtn.onClick.AddListener(() =>
            {
                selectedClientUid = capturedUid;
                RefreshStudentsListAndInspector();
            });

            // 1. Student ID (- S01 +) & Device
            Button btnMinus = CreateButton("BtnIdMinus", rowObj.transform, cardBorderTint, darkSlate, out TextMeshProUGUI minusTxt);
            PositionFixedLeft(btnMinus.GetComponent<RectTransform>(), 12f, 14f, 26f, 28f);
            minusTxt.text = "-";
            btnMinus.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                selectedClientUid = capturedUid;
                NetworkSessionHost.Instance.CycleStudentId(capturedUid, -1);
            });

            TextMeshProUGUI idLabel = CreateText("StudentId", rowObj.transform, 16f, FontStyles.Bold, darkSlate, TextAlignmentOptions.Center);
            PositionFixedLeft(idLabel.GetComponent<RectTransform>(), 40f, 14f, 54f, 28f);
            idLabel.text = s.studentId;

            Button btnPlus = CreateButton("BtnIdPlus", rowObj.transform, cardBorderTint, darkSlate, out TextMeshProUGUI plusTxt);
            PositionFixedLeft(btnPlus.GetComponent<RectTransform>(), 96f, 14f, 26f, 28f);
            plusTxt.text = "+";
            btnPlus.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                selectedClientUid = capturedUid;
                NetworkSessionHost.Instance.CycleStudentId(capturedUid, 1);
            });

            string statusDot = s.isConnected ? "<color=#16a34a>●</color>" : "<color=#94a3b8>●</color>";
            TextMeshProUGUI devLabel = CreateText("DeviceMeta", rowObj.transform, 12.5f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.MidlineLeft);
            devLabel.richText = true;
            PositionFixedLeft(devLabel.GetComponent<RectTransform>(), 12f, -18f, 200f, 24f);
            devLabel.text = $"{statusDot} {s.deviceName} ({s.remoteIp})";

            // 2. Live Scene & Next Level
            StudentTelemetrySnapshot tel = s.latestTelemetry;
            string sceneStr = tel != null ? FormatSceneFriendly(tel.currentScene, isEs) : (isEs ? "Menú" : "Menu");
            string nextLvlStr = tel != null && !string.IsNullOrEmpty(tel.currentNextLevelId)
                ? FormatLevelShort(tel.currentNextLevelId, isEs)
                : "L1";

            TextMeshProUGUI sceneLabel = CreateText("SceneInfo", rowObj.transform, 14f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            PositionFixedLeft(sceneLabel.GetComponent<RectTransform>(), 220f, 12f, 185f, 26f);
            sceneLabel.text = sceneStr;

            TextMeshProUGUI levelSubLabel = CreateText("NextLevelInfo", rowObj.transform, 12.5f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.MidlineLeft);
            PositionFixedLeft(levelSubLabel.GetComponent<RectTransform>(), 220f, -16f, 185f, 24f);
            levelSubLabel.text = isEs ? $"Siguiente: {nextLvlStr}" : $"Next: {nextLvlStr}";

            // 3. Progress Bar (Levels X/16) & Medals
            int doneLevels = tel != null ? Mathf.Clamp(tel.completedLevelsCount, 0, 16) : 0;
            float avgSkill = tel != null ? tel.currentSkillsAverageScore : 0f;

            TextMeshProUGUI progText = CreateText("ProgText", rowObj.transform, 13.5f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            PositionFixedLeft(progText.GetComponent<RectTransform>(), 414f, 14f, 200f, 24f);
            progText.text = isEs
                ? $"Niveles: {doneLevels}/16  •  Hab: {avgSkill:0}%"
                : $"Levels: {doneLevels}/16  •  Skill: {avgSkill:0}%";

            GameObject barTrack = CreatePanel("ProgTrack", rowObj.transform, new Color(0.86f, 0.89f, 0.94f, 1f));
            PositionFixedLeft(barTrack.GetComponent<RectTransform>(), 414f, -4f, 190f, 10f);

            GameObject barFill = CreatePanel("ProgFill", barTrack.transform, emeraldGreen);
            RectTransform bfRt = barFill.GetComponent<RectTransform>();
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = new Vector2(Mathf.Clamp01(doneLevels / 16f), 1f);
            bfRt.offsetMin = Vector2.zero;
            bfRt.offsetMax = Vector2.zero;

            TextMeshProUGUI medalsText = CreateText("MedalsText", rowObj.transform, 12f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.MidlineLeft);
            medalsText.richText = true;
            PositionFixedLeft(medalsText.GetComponent<RectTransform>(), 414f, -22f, 200f, 20f);
            int g = tel != null ? tel.goldMedalsCount : 0;
            int sv = tel != null ? tel.silverMedalsCount : 0;
            int br = tel != null ? tel.bronzeMedalsCount : 0;
            medalsText.text = $"<color=#d97706>G:{g}</color>  <color=#64748b>S:{sv}</color>  <color=#b45309>B:{br}</color>";

            // 4. Individual Learning Path Selector Pills (Beginner | Intermediate | Advanced) + Assessment Toggle
            LearningPath assignedPath = s.assignedConfig != null
                ? s.assignedConfig.learningPath
                : LearningPath.BeginnerFoundations;
            bool assignedAssess = s.assignedConfig == null || s.assignedConfig.runAssessment;

            string[] shortLabels = { "Beginner", "Interm.", "Advanced" };
            for (int p = 0; p < DashboardPaths.Length; p++)
            {
                LearningPath candidate = DashboardPaths[p];
                bool isActivePath = assignedPath == candidate;
                Button pBtn = CreateButton(
                    $"BtnPath_{candidate}",
                    rowObj.transform,
                    isActivePath ? primaryBlue : new Color(0.92f, 0.94f, 0.97f, 1f),
                    isActivePath ? Color.white : darkSlate,
                    out TextMeshProUGUI pTxt);

                PositionFixedLeft(pBtn.GetComponent<RectTransform>(), 622f + p * 92f, 0f, 88f, 38f);
                pTxt.fontSize = 12.5f;
                pTxt.text = shortLabels[p];

                pBtn.onClick.AddListener(() =>
                {
                    FeedbackManager.PlayClick();
                    selectedClientUid = capturedUid;
                    NetworkSessionHost.Instance.SetStudentDraftPath(capturedUid, candidate, pushImmediately: true);
                });
            }

            Button stuAssessBtn = CreateButton(
                "BtnStudentAssess",
                rowObj.transform,
                assignedAssess ? emeraldGreen : mutedSlate,
                Color.white,
                out TextMeshProUGUI stuAssessTxt);
            PositionFixedLeft(stuAssessBtn.GetComponent<RectTransform>(), 902f, 0f, 96f, 38f);
            stuAssessTxt.fontSize = 12f;
            stuAssessTxt.text = assignedAssess ? "Assess: ON" : "Assess: OFF";
            stuAssessBtn.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                selectedClientUid = capturedUid;
                NetworkSessionHost.Instance.SetStudentDraftAssessment(capturedUid, !assignedAssess, pushImmediately: true);
            });

            // 5. Remote Start & Reset Action Buttons
            Button btnStart = CreateButton("BtnStartStudent", rowObj.transform, emeraldGreen, Color.white, out TextMeshProUGUI startTxt);
            PositionFixedRight(btnStart.GetComponent<RectTransform>(), -82f, 0f, 82f, 40f);
            startTxt.fontSize = 13f;
            startTxt.text = isEs ? "▶ Iniciar" : "▶ Start";
            btnStart.interactable = s.isConnected;
            btnStart.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                selectedClientUid = capturedUid;
                NetworkSessionHost.Instance.PushStudentConfiguration(capturedUid, startImmediately: true, resetProgress: false);
            });

            Button btnReset = CreateButton("BtnResetStudent", rowObj.transform, amberColor, Color.white, out TextMeshProUGUI resetTxt);
            PositionFixedRight(btnReset.GetComponent<RectTransform>(), -10f, 0f, 66f, 40f);
            resetTxt.fontSize = 12.5f;
            resetTxt.text = "Reset";
            btnReset.interactable = s.isConnected;
            btnReset.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                selectedClientUid = capturedUid;
                NetworkSessionHost.Instance.ResetStudentSession(capturedUid);
            });
        }

        private void RefreshInspector(ConnectedStudentSession session)
        {
            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);

            if (session == null)
            {
                inspectorTitleLabel.text = isEs ? "Inspector en Tiempo Real" : "Live Student Inspector";
                inspectorMetaLabel.text = isEs ? "Selecciona un alumno conectado" : "Select a connected student";
                inspectorKpiText.text = isEs
                    ? "Cuando un alumno se conecte al Aula LAN, aquí verás en directo:\n• Escena actual y nivel en curso\n• Resultado del Assessment inicial\n• Medallas y precisión en tareas\n• Evolución en tiempo real de las 9 habilidades"
                    : "Once a student connects to the Classroom LAN, live telemetry appears here:\n• Active scene & current level\n• Initial Assessment score\n• Medals & task accuracy\n• Real-time breakdown of all 9 skills";
                inspectorSkillsContainer.SetActive(false);
                return;
            }

            inspectorSkillsContainer.SetActive(true);
            StudentTelemetrySnapshot tel = session.latestTelemetry;

            inspectorTitleLabel.text = $"{session.studentId}  —  {session.deviceName}";
            string connBadge = session.isConnected
                ? (isEs ? "<color=#16a34a><b>CONECTADO</b></color>" : "<color=#16a34a><b>ONLINE</b></color>")
                : (isEs ? "<color=#dc2626><b>DESCONECTADO</b></color>" : "<color=#dc2626><b>OFFLINE</b></color>");
            string updatedAt = tel != null && !string.IsNullOrEmpty(tel.lastUpdateTimestamp) ? tel.lastUpdateTimestamp : "--:--:--";
            inspectorMetaLabel.text = $"{connBadge}  •  IP: {session.remoteIp}  •  {(isEs ? "Actualizado" : "Updated")}: {updatedAt}";

            LearningPath path = session.assignedConfig != null ? session.assignedConfig.learningPath : LearningPath.BeginnerFoundations;
            bool runAssess = session.assignedConfig == null || session.assignedConfig.runAssessment;

            string assessSummary;
            if (tel != null && tel.assessmentCompleted && tel.assessmentAverageScore > 0f)
                assessSummary = $"{tel.assessmentAverageScore:0.#}%";
            else if (!runAssess)
                assessSummary = isEs ? "Desactivado (Perfil predefinido)" : "Disabled (Skill preset)";
            else
                assessSummary = isEs ? "Pendiente" : "Pending";

            int completedCount = tel != null ? tel.completedLevelsCount : 0;
            string nextLvl = tel != null ? FormatLevelShort(tel.currentNextLevelId, isEs) : "Level 1";
            string lastLvl = tel != null && !string.IsNullOrEmpty(tel.lastCompletedLevelId)
                ? $"{FormatLevelShort(tel.lastCompletedLevelId, isEs)} ({tel.lastMedal})"
                : "-";
            int tasksDone = tel != null ? tel.totalTasksAttempted : 0;
            int tasksOk = tel != null ? tel.totalTasksSucceeded : 0;
            float avgSkills = tel != null ? tel.currentSkillsAverageScore : 0f;

            inspectorKpiText.text = isEs
                ? $"<b>Flujo asignado:</b> {StudentConfiguration.GetPathDisplayName(path)}\n" +
                  $"<b>Assessment inicial:</b> {assessSummary}\n" +
                  $"<b>Escena actual:</b> {(tel != null ? FormatSceneFriendly(tel.currentScene, true) : "Menú")}   |   <b>Siguiente:</b> {nextLvl}\n" +
                  $"<b>Niveles completados:</b> {completedCount} / 16   |   <b>Último:</b> {lastLvl}\n" +
                  $"<b>Tareas superadas:</b> {tasksOk} / {tasksDone}   |   <b>Media habilidades:</b> {avgSkills:0.#}%"
                : $"<b>Assigned Flow:</b> {StudentConfiguration.GetPathDisplayName(path)}\n" +
                  $"<b>Initial Assessment:</b> {assessSummary}\n" +
                  $"<b>Active Scene:</b> {(tel != null ? FormatSceneFriendly(tel.currentScene, false) : "Menu")}   |   <b>Next:</b> {nextLvl}\n" +
                  $"<b>Completed Levels:</b> {completedCount} / 16   |   <b>Last:</b> {lastLvl}\n" +
                  $"<b>Tasks Succeeded:</b> {tasksOk} / {tasksDone}   |   <b>Skills Avg:</b> {avgSkills:0.#}%";

            // Update 9 Skill Bars
            Dictionary<string, float> scoreLookup = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (tel != null && tel.currentSkillScores != null)
            {
                for (int i = 0; i < tel.currentSkillScores.Count; i++)
                {
                    SkillScoreEntry entry = tel.currentSkillScores[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.skillId))
                    {
                        scoreLookup[entry.skillId] = entry.score;
                    }
                }
            }

            foreach (var kvp in skillBars)
            {
                float score = scoreLookup.TryGetValue(kvp.Key, out float val) ? Mathf.Clamp(val, 0f, 100f) : 0f;
                int tier = Mathf.Clamp(Mathf.FloorToInt(score / 20f) + 1, 1, 5);

                kvp.Value.valueLabel.text = $"{score:0}% (D{tier})";
                kvp.Value.fillRect.anchorMax = new Vector2(score / 100f, 1f);
                kvp.Value.fillImage.color = score >= 60f
                    ? emeraldGreen
                    : (score >= 30f ? primaryBlue : amberColor);
            }
        }

        private static string FormatSceneFriendly(string sceneName, bool isEs)
        {
            switch (sceneName)
            {
                case "Menu": return isEs ? "Menú Principal" : "Main Menu";
                case "AssessmentScene": return "Assessment";
                case "Desktop1": return isEs ? "Tutorial Inicial (1-5)" : "Initial Tutorial (1-5)";
                case "VirtualDesktop": return isEs ? "Escritorio Virtual" : "Virtual Desktop";
                case "StatsScene": return isEs ? "Estadísticas" : "Stats Screen";
                default: return string.IsNullOrEmpty(sceneName) ? "Menu" : sceneName;
            }
        }

        private static string FormatLevelShort(string levelId, bool isEs)
        {
            if (string.IsNullOrEmpty(levelId)) return "-";
            if (string.Equals(levelId, "Completed", StringComparison.OrdinalIgnoreCase))
                return isEs ? "16/16 Completado" : "16/16 Completed";

            if (levelId.StartsWith("level", StringComparison.OrdinalIgnoreCase))
            {
                string num = levelId.Substring(5);
                return isEs ? $"Nivel {num}" : $"Level {num}";
            }
            return levelId;
        }

        private static string FormatSkillDisplayName(string skillId)
        {
            switch (skillId)
            {
                case "mouse_move": return "Mouse Movement";
                case "hover": return "Cursor Hover";
                case "click": return "Left Click";
                case "double_click": return "Double Click";
                case "right_click": return "Right Click";
                case "click_hold": return "Click & Hold";
                case "drag_drop": return "Drag & Drop";
                case "scroll": return "Scroll Wheel";
                case "shortcuts": return "Keyboard Shortcuts";
                default: return skillId;
            }
        }

        private GameObject CreatePanel(string name, Transform parent, Color bgColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            ApplyRounded(img);
            return go;
        }

        private Button CreateButton(string name, Transform parent, Color bgColor, Color textColor, out TextMeshProUGUI label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            ApplyRounded(img);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.93f, 0.96f, 1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.89f, 0.96f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.75f, 0.78f, 0.82f, 0.65f);
            btn.colors = cb;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            label = CreateText("Label", go.transform, 14f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            StretchFull(label.GetComponent<RectTransform>(), 6f, 0f);
            return btn;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void ApplyRounded(Image img)
        {
            if (img != null && roundedSprite != null)
            {
                img.sprite = roundedSprite;
                img.type = Image.Type.Sliced;
            }
        }

        private static void PositionFixedLeft(RectTransform rt, float x, float yCenterOffset, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, yCenterOffset);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void PositionFixedRight(RectTransform rt, float xFromRight, float yCenterOffset, float width, float height)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromRight, yCenterOffset);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void StretchFull(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
