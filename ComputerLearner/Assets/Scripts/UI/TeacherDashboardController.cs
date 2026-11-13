/**
 * Author: TeamCompass
 * Description: Scene-serialized Full Teacher Host Dashboard UI modal in Menu.unity.
 *              Displays LAN server IP/status, classroom-wide Learning Path + Assessment batch controls,
 *              per-student rows via serialized StudentRowTemplate, remote Start/Reset controls,
 *              and a live student telemetry inspector with 9 serialized skill progress bars.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

        [Serializable]
        public class SkillBarSlot
        {
            public string skillId;
            public GameObject rowRoot;
            public TMP_Text skillNameText;
            public TMP_Text skillScoreText;
            public Image barFillImage;
        }

        [Header("Top Bar Controls")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text serverStatusText;
        [SerializeField] private Button clearOfflineButton;
        [SerializeField] private TMP_Text clearOfflineLabel;
        [SerializeField] private Button toggleServerButton;
        [SerializeField] private TMP_Text toggleServerLabel;
        [SerializeField] private Button closeButton;

        [Header("Classroom Batch Bar")]
        [SerializeField] private TMP_Text batchLabelText;
        [SerializeField] private Button[] batchPathButtons = new Button[3];
        [SerializeField] private Image[] batchPathBgs = new Image[3];
        [SerializeField] private TMP_Text[] batchPathLabels = new TMP_Text[3];
        [SerializeField] private Button batchAssessButton;
        [SerializeField] private Image batchAssessBg;
        [SerializeField] private TMP_Text batchAssessLabel;
        [SerializeField] private Button applyAllButton;
        [SerializeField] private TMP_Text applyAllLabel;
        [SerializeField] private Button launchAllButton;
        [SerializeField] private TMP_Text launchAllLabel;

        [Header("Students List (Serialized Row Template)")]
        [SerializeField] private TMP_Text studentsHeaderText;
        [SerializeField] private Transform studentsListContainer;
        [SerializeField] private GameObject studentRowTemplate;
        [SerializeField] private TMP_Text emptyStudentsText;

        [Header("Right Student Telemetry Inspector")]
        [SerializeField] private TMP_Text inspectorPlaceholderText;
        [SerializeField] private GameObject inspectorContentRoot;
        [SerializeField] private TMP_Text inspectorAvatarText;
        [SerializeField] private TMP_Text inspectorTitleText;
        [SerializeField] private TMP_Text inspectorSubtitleText;
        [SerializeField] private TMP_Text inspectorKpiProgressValue;
        [SerializeField] private TMP_Text inspectorKpiProgressLabel;
        [SerializeField] private TMP_Text inspectorKpiMedalsValue;
        [SerializeField] private TMP_Text inspectorKpiMedalsLabel;
        [SerializeField] private TMP_Text inspectorKpiScoreValue;
        [SerializeField] private TMP_Text inspectorKpiScoreLabel;
        [SerializeField] private SkillBarSlot[] skillBarSlots = new SkillBarSlot[9];

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private string _selectedClientUid;
        private float _nextPeriodicRefresh;

        public bool IsOpen => gameObject.activeSelf;

        public static TeacherDashboardController OpenOnCanvas(Transform canvasRoot)
        {
            TeacherDashboardController ctrl = Instance ?? FindAnyObjectByType<TeacherDashboardController>(FindObjectsInactive.Include);
            if (ctrl == null && canvasRoot != null)
                ctrl = canvasRoot.GetComponentInChildren<TeacherDashboardController>(true);
            ctrl?.Open();
            return ctrl;
        }

        public static void CloseDashboard()
        {
            if (Instance != null)
                Instance.Close();
        }

        private void Awake()
        {
            Instance = this;
            if (studentRowTemplate != null)
                studentRowTemplate.SetActive(false);
            BindStaticButtons();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshAll;
            SubscribeHostEvents(true);
            RefreshAll();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshAll;
            SubscribeHostEvents(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (NetworkSessionHost.Instance != null && NetworkSessionHost.Instance.IsHosting && Time.unscaledTime >= _nextPeriodicRefresh)
            {
                _nextPeriodicRefresh = Time.unscaledTime + 1.5f;
                RefreshServerBanner();
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            if (TeacherModeGate.Instance != null && !TeacherModeGate.IsTeacherUnlocked)
                return;
            if (NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected)
                return;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (!NetworkSessionHost.Instance.IsHosting)
                NetworkSessionHost.Instance.StartHost();
            RefreshAll();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void BindStaticButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (toggleServerButton != null)
            {
                toggleServerButton.onClick.RemoveAllListeners();
                toggleServerButton.onClick.AddListener(() =>
                {
                    if (NetworkSessionHost.Instance.IsHosting)
                        NetworkSessionHost.Instance.StopHost();
                    else
                        NetworkSessionHost.Instance.StartHost();
                    RefreshAll();
                });
            }

            if (clearOfflineButton != null)
            {
                clearOfflineButton.onClick.RemoveAllListeners();
                clearOfflineButton.onClick.AddListener(() =>
                {
                    NetworkSessionHost.Instance.RemoveDisconnectedStudents();
                    RefreshStudentsList();
                });
            }

            for (int i = 0; i < batchPathButtons.Length && i < DashboardPaths.Length; i++)
            {
                if (batchPathButtons[i] == null) continue;
                LearningPath path = DashboardPaths[i];
                batchPathButtons[i].onClick.RemoveAllListeners();
                batchPathButtons[i].onClick.AddListener(() =>
                {
                    NetworkSessionHost.Instance.SetClassroomDefaults(path, NetworkSessionHost.Instance.DefaultClassroomAssessment);
                    RefreshBatchBar();
                });
            }

            if (batchAssessButton != null)
            {
                batchAssessButton.onClick.RemoveAllListeners();
                batchAssessButton.onClick.AddListener(() =>
                {
                    bool next = !NetworkSessionHost.Instance.DefaultClassroomAssessment;
                    NetworkSessionHost.Instance.SetClassroomDefaults(NetworkSessionHost.Instance.DefaultClassroomPath, next);
                    RefreshBatchBar();
                });
            }

            if (applyAllButton != null)
            {
                applyAllButton.onClick.RemoveAllListeners();
                applyAllButton.onClick.AddListener(() =>
                {
                    NetworkSessionHost.Instance.ApplyClassroomDefaultsToAll(false);
                    RefreshStudentsList();
                });
            }

            if (launchAllButton != null)
            {
                launchAllButton.onClick.RemoveAllListeners();
                launchAllButton.onClick.AddListener(() =>
                {
                    NetworkSessionHost.Instance.ApplyClassroomDefaultsToAll(true);
                    NetworkSessionHost.Instance.StartAllConnectedStudents();
                    RefreshStudentsList();
                });
            }
        }

        private void SubscribeHostEvents(bool subscribe)
        {
            NetworkSessionHost host = NetworkSessionHost.Instance;
            if (host == null) return;

            if (subscribe)
            {
                host.OnHostStateChanged -= HandleHostOrStudentsChanged;
                host.OnHostStateChanged += HandleHostOrStudentsChanged;
                host.OnStudentsChanged -= HandleHostOrStudentsChanged;
                host.OnStudentsChanged += HandleHostOrStudentsChanged;
                host.OnStudentTelemetryUpdated -= HandleStudentTelemetryUpdated;
                host.OnStudentTelemetryUpdated += HandleStudentTelemetryUpdated;
            }
            else
            {
                host.OnHostStateChanged -= HandleHostOrStudentsChanged;
                host.OnStudentsChanged -= HandleHostOrStudentsChanged;
                host.OnStudentTelemetryUpdated -= HandleStudentTelemetryUpdated;
            }
        }

        private void HandleHostOrStudentsChanged()
        {
            if (IsOpen) RefreshAll();
        }

        private void HandleStudentTelemetryUpdated(ConnectedStudentSession updated)
        {
            if (!IsOpen) return;
            RefreshStudentsList();
            if (updated != null && updated.clientUid == _selectedClientUid)
                RefreshInspector();
        }

        public void RefreshAll()
        {
            UIDialogs ui = UIDialogs.Current;
            if (titleText != null) titleText.text = ui.dash_title;
            if (clearOfflineLabel != null) clearOfflineLabel.text = ui.dash_btn_clear_offline;
            if (studentsHeaderText != null) studentsHeaderText.text = ui.dash_students_header;
            if (batchLabelText != null) batchLabelText.text = ui.dash_batch_label;
            if (applyAllLabel != null) applyAllLabel.text = ui.dash_btn_apply_all;
            if (launchAllLabel != null) launchAllLabel.text = ui.dash_btn_launch_all;
            if (inspectorKpiProgressLabel != null) inspectorKpiProgressLabel.text = ui.dash_kpi_progress;
            if (inspectorKpiMedalsLabel != null) inspectorKpiMedalsLabel.text = ui.dash_kpi_medals;
            if (inspectorKpiScoreLabel != null) inspectorKpiScoreLabel.text = ui.dash_kpi_score;

            RefreshServerBanner();
            RefreshBatchBar();
            RefreshStudentsList();
            RefreshInspector();
        }

        private void RefreshServerBanner()
        {
            UIDialogs ui = UIDialogs.Current;
            NetworkSessionHost host = NetworkSessionHost.Instance;
            bool isHosting = host != null && host.IsHosting;

            if (serverStatusText != null)
            {
                serverStatusText.text = isHosting
                    ? string.Format(ui.dash_server_on_format, host.LocalIpAddress, host.ListenPort, host.ActiveConnectedCount)
                    : ui.dash_server_off;
            }

            if (toggleServerLabel != null)
                toggleServerLabel.text = isHosting ? ui.dash_btn_stop_server : ui.dash_btn_start_server;

            if (toggleServerButton != null)
                RetroUITheme.ConfigureButtonColors(toggleServerButton, isHosting ? RetroUITheme.BtnCoralRed : RetroUITheme.BtnMintGreen);
        }

        private void RefreshBatchBar()
        {
            UIDialogs ui = UIDialogs.Current;
            NetworkSessionHost host = NetworkSessionHost.Instance;
            LearningPath defaultPath = host != null ? host.DefaultClassroomPath : LearningPath.BeginnerFoundations;
            bool assessOn = host == null || host.DefaultClassroomAssessment;

            string[] pathTitles = { ui.path_beginner_title, ui.path_intermediate_title, ui.path_advanced_title };
            for (int i = 0; i < DashboardPaths.Length; i++)
            {
                bool active = DashboardPaths[i] == defaultPath;
                if (i < batchPathBgs.Length && batchPathBgs[i] != null)
                    batchPathBgs[i].color = active ? RetroUITheme.BtnSkyBlue : RetroUITheme.BtnNeutralCream;
                if (i < batchPathLabels.Length && batchPathLabels[i] != null)
                {
                    batchPathLabels[i].text = pathTitles[i];
                    batchPathLabels[i].color = active ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
                }
            }

            if (batchAssessBg != null)
                batchAssessBg.color = assessOn ? RetroUITheme.BtnMintGreen : RetroUITheme.BtnWarmOrange;
            if (batchAssessLabel != null)
            {
                batchAssessLabel.text = assessOn ? ui.dash_assess_on : ui.dash_assess_off;
                batchAssessLabel.color = RetroUITheme.InkWhite;
            }
        }

        private void RefreshStudentsList()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                    Destroy(_spawnedRows[i]);
            }
            _spawnedRows.Clear();

            IReadOnlyList<ConnectedStudentSession> students = NetworkSessionHost.Instance != null
                ? NetworkSessionHost.Instance.ConnectedStudents
                : null;

            bool hasStudents = students != null && students.Count > 0;
            if (emptyStudentsText != null)
            {
                emptyStudentsText.gameObject.SetActive(!hasStudents);
                emptyStudentsText.text = UIDialogs.Current.dash_empty_students;
            }

            if (!hasStudents || studentRowTemplate == null || studentsListContainer == null)
            {
                _selectedClientUid = null;
                return;
            }

            bool foundSelected = false;
            for (int i = 0; i < students.Count; i++)
            {
                if (students[i].clientUid == _selectedClientUid)
                {
                    foundSelected = true;
                    break;
                }
            }
            if (!foundSelected)
                _selectedClientUid = students[0].clientUid;

            UIDialogs ui = UIDialogs.Current;
            string[] shortPaths = { ui.path_beginner_title, ui.path_intermediate_title, ui.path_advanced_title };

            for (int i = 0; i < students.Count; i++)
            {
                ConnectedStudentSession student = students[i];
                GameObject row = Instantiate(studentRowTemplate, studentsListContainer);
                row.name = $"StudentRow_{student.studentId}";
                row.SetActive(true);
                _spawnedRows.Add(row);

                bool isSelected = student.clientUid == _selectedClientUid;
                Image rowBg = row.GetComponent<Image>();
                if (rowBg != null)
                    rowBg.color = isSelected ? RetroUITheme.CardPastelBlue : RetroUITheme.CardWhite;

                Button selectBtn = row.transform.Find("SelectArea")?.GetComponent<Button>();
                if (selectBtn != null)
                {
                    string uidCopy = student.clientUid;
                    selectBtn.onClick.RemoveAllListeners();
                    selectBtn.onClick.AddListener(() =>
                    {
                        _selectedClientUid = uidCopy;
                        RefreshStudentsList();
                        RefreshInspector();
                    });
                }

                TMP_Text idText = row.transform.Find("StudentIdText")?.GetComponent<TMP_Text>();
                if (idText != null)
                    idText.text = student.studentId;

                TMP_Text metaText = row.transform.Find("MetaText")?.GetComponent<TMP_Text>();
                if (metaText != null)
                {
                    string sceneLabel = student.latestTelemetry != null && !string.IsNullOrEmpty(student.latestTelemetry.currentScene)
                        ? student.latestTelemetry.currentScene
                        : "Menu";
                    string statusWord = student.isConnected ? ui.dash_status_online : ui.dash_status_offline;
                    int levelsDone = student.latestTelemetry != null ? student.latestTelemetry.completedLevelsCount : 0;
                    metaText.text = $"{statusWord} • {student.deviceName} • {sceneLabel} ({levelsDone}/16)";
                }

                Image statusDot = row.transform.Find("StatusDot")?.GetComponent<Image>();
                if (statusDot != null)
                    statusDot.color = student.isConnected ? RetroUITheme.SuccessMint : RetroUITheme.SilverAccent;

                Button minusBtn = row.transform.Find("Btn-IdMinus")?.GetComponent<Button>();
                if (minusBtn != null)
                {
                    string uidCopy = student.clientUid;
                    minusBtn.onClick.RemoveAllListeners();
                    minusBtn.onClick.AddListener(() => NetworkSessionHost.Instance.CycleStudentId(uidCopy, -1));
                }

                Button plusBtn = row.transform.Find("Btn-IdPlus")?.GetComponent<Button>();
                if (plusBtn != null)
                {
                    string uidCopy = student.clientUid;
                    plusBtn.onClick.RemoveAllListeners();
                    plusBtn.onClick.AddListener(() => NetworkSessionHost.Instance.CycleStudentId(uidCopy, +1));
                }

                LearningPath studentPath = student.assignedConfig != null ? student.assignedConfig.learningPath : LearningPath.BeginnerFoundations;
                for (int p = 0; p < DashboardPaths.Length; p++)
                {
                    Button pathBtn = row.transform.Find($"Btn-Path{p}")?.GetComponent<Button>();
                    if (pathBtn == null) continue;
                    bool activePath = DashboardPaths[p] == studentPath;
                    RetroUITheme.ConfigureButtonColors(pathBtn, activePath ? RetroUITheme.BtnSkyBlue : RetroUITheme.BtnNeutralCream);
                    TMP_Text lbl = pathBtn.GetComponentInChildren<TMP_Text>(true);
                    if (lbl != null)
                    {
                        lbl.text = shortPaths[p];
                        lbl.color = activePath ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
                    }
                    string uidCopy = student.clientUid;
                    LearningPath targetPath = DashboardPaths[p];
                    pathBtn.onClick.RemoveAllListeners();
                    pathBtn.onClick.AddListener(() => NetworkSessionHost.Instance.SetStudentDraftPath(uidCopy, targetPath, true));
                }

                bool assessOn = student.assignedConfig == null || student.assignedConfig.runAssessment;
                Button assessBtn = row.transform.Find("Btn-ToggleAssess")?.GetComponent<Button>();
                if (assessBtn != null)
                {
                    RetroUITheme.ConfigureButtonColors(assessBtn, assessOn ? RetroUITheme.BtnMintGreen : RetroUITheme.BtnWarmOrange);
                    TMP_Text lbl = assessBtn.GetComponentInChildren<TMP_Text>(true);
                    if (lbl != null)
                    {
                        lbl.text = assessOn ? ui.dash_assess_on : ui.dash_assess_off;
                        lbl.color = RetroUITheme.InkWhite;
                    }
                    string uidCopy = student.clientUid;
                    assessBtn.onClick.RemoveAllListeners();
                    assessBtn.onClick.AddListener(() => NetworkSessionHost.Instance.SetStudentDraftAssessment(uidCopy, !assessOn, true));
                }

                Button startBtn = row.transform.Find("Btn-StartStudent")?.GetComponent<Button>();
                if (startBtn != null)
                {
                    startBtn.interactable = student.isConnected;
                    TMP_Text lbl = startBtn.GetComponentInChildren<TMP_Text>(true);
                    if (lbl != null) lbl.text = ui.dash_btn_start_student;
                    string uidCopy = student.clientUid;
                    startBtn.onClick.RemoveAllListeners();
                    startBtn.onClick.AddListener(() => NetworkSessionHost.Instance.PushStudentConfiguration(uidCopy, true, true));
                }

                Button resetBtn = row.transform.Find("Btn-ResetStudent")?.GetComponent<Button>();
                if (resetBtn != null)
                {
                    resetBtn.interactable = student.isConnected;
                    TMP_Text lbl = resetBtn.GetComponentInChildren<TMP_Text>(true);
                    if (lbl != null) lbl.text = ui.dash_btn_reset_student;
                    string uidCopy = student.clientUid;
                    resetBtn.onClick.RemoveAllListeners();
                    resetBtn.onClick.AddListener(() => NetworkSessionHost.Instance.ResetStudentSession(uidCopy));
                }
            }
        }

        private void RefreshInspector()
        {
            ConnectedStudentSession selected = FindSelectedStudent();
            bool hasSelection = selected != null;

            if (inspectorPlaceholderText != null)
            {
                inspectorPlaceholderText.gameObject.SetActive(!hasSelection);
                inspectorPlaceholderText.text = UIDialogs.Current.dash_inspector_placeholder;
            }
            if (inspectorContentRoot != null)
                inspectorContentRoot.SetActive(hasSelection);

            if (!hasSelection) return;

            UIDialogs ui = UIDialogs.Current;
            StudentTelemetrySnapshot tel = selected.latestTelemetry;

            if (inspectorAvatarText != null)
                inspectorAvatarText.text = string.IsNullOrEmpty(selected.studentId) ? "?" : selected.studentId.Substring(0, Mathf.Min(3, selected.studentId.Length));
            if (inspectorTitleText != null)
                inspectorTitleText.text = $"{selected.studentId}  ({selected.deviceName})";
            if (inspectorSubtitleText != null)
            {
                string pathName = selected.assignedConfig != null
                    ? StudentConfiguration.GetPathDisplayName(selected.assignedConfig.learningPath)
                    : string.Empty;
                string sceneName = tel != null && !string.IsNullOrEmpty(tel.currentScene) ? tel.currentScene : "Menu";
                inspectorSubtitleText.text = $"{selected.remoteIp}  •  {sceneName}  •  {pathName}";
            }

            int completedLevels = tel != null ? tel.completedLevelsCount : 0;
            int totalMedals = tel != null ? (tel.goldMedalsCount + tel.silverMedalsCount + tel.bronzeMedalsCount) : 0;
            float avgSkills = tel != null ? tel.currentSkillsAverageScore : 0f;

            if (inspectorKpiProgressValue != null)
                inspectorKpiProgressValue.text = $"{completedLevels}/16";
            if (inspectorKpiMedalsValue != null)
                inspectorKpiMedalsValue.text = $"{totalMedals}";
            if (inspectorKpiScoreValue != null)
                inspectorKpiScoreValue.text = $"{avgSkills:0}%";

            for (int i = 0; i < skillBarSlots.Length; i++)
            {
                SkillBarSlot slot = skillBarSlots[i];
                if (slot == null) continue;

                float accuracy = FindSkillAccuracy(tel, slot.skillId);
                if (slot.skillNameText != null)
                    slot.skillNameText.text = GetLocalizedSkillTitle(slot.skillId, ui);
                if (slot.skillScoreText != null)
                    slot.skillScoreText.text = $"{accuracy:0}%";
                if (slot.barFillImage != null)
                {
                    slot.barFillImage.fillAmount = Mathf.Clamp01(accuracy / 100f);
                    slot.barFillImage.color = accuracy >= 80f
                        ? RetroUITheme.SuccessMint
                        : (accuracy >= 50f ? RetroUITheme.GoldAccent : RetroUITheme.BtnSkyBlue);
                }
            }
        }

        private ConnectedStudentSession FindSelectedStudent()
        {
            if (string.IsNullOrEmpty(_selectedClientUid) || NetworkSessionHost.Instance == null)
                return null;
            IReadOnlyList<ConnectedStudentSession> students = NetworkSessionHost.Instance.ConnectedStudents;
            for (int i = 0; i < students.Count; i++)
            {
                if (students[i].clientUid == _selectedClientUid)
                    return students[i];
            }
            return null;
        }

        private static float FindSkillAccuracy(StudentTelemetrySnapshot tel, string skillId)
        {
            if (tel == null || tel.currentSkillScores == null || string.IsNullOrEmpty(skillId))
                return 0f;
            for (int i = 0; i < tel.currentSkillScores.Count; i++)
            {
                SkillScoreEntry entry = tel.currentSkillScores[i];
                if (entry != null && string.Equals(entry.skillId, skillId, StringComparison.OrdinalIgnoreCase))
                    return entry.score;
            }
            return 0f;
        }

        private static string GetLocalizedSkillTitle(string skillId, UIDialogs ui)
        {
            switch (skillId)
            {
                case "mouse_move": return ui.skill_mouse_move;
                case "hover": return ui.skill_hover;
                case "click": return ui.skill_click;
                case "double_click": return ui.skill_double_click;
                case "right_click": return ui.skill_right_click;
                case "click_hold": return ui.skill_click_hold;
                case "drag_drop": return ui.skill_drag_drop;
                case "typing": return ui.level_display_11;
                case "shortcuts": return ui.level_display_12;
                default: return skillId;
            }
        }
    }
}
