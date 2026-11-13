/**
 * Author: TeamCompass
 * Description: Scene-serialized Learning Path selector and Assessment ON/OFF toggle inside Teacher Mode.
 *              Supports the 3 progressive student flows (BeginnerFoundations, Intermediate, Advanced)
 *              and locks automatically when connected to a Teacher Host LAN session.
 */
using System;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Scene Serialized UI References")]
        [SerializeField] private TMP_Text sectionTitleText;
        [SerializeField] private Button[] pathButtons = new Button[3];
        [SerializeField] private Image[] pathButtonBackgrounds = new Image[3];
        [SerializeField] private TMP_Text[] pathButtonTitles = new TMP_Text[3];
        [SerializeField] private Button assessmentToggleButton;
        [SerializeField] private Image assessmentToggleBg;
        [SerializeField] private TMP_Text assessmentToggleLabel;
        [SerializeField] private TMP_Text summaryFooterText;
        [SerializeField] private TMP_Text lanLockedNoticeText;

        private readonly Color _activePathBgColor = RetroUITheme.BtnSkyBlue;
        private readonly Color _inactivePathBgColor = RetroUITheme.CardWhite;
        private readonly Color _assessOnColor = RetroUITheme.BtnMintGreen;
        private readonly Color _assessOffColor = RetroUITheme.BtnWarmOrange;

        private void Awake()
        {
            Instance = this;
            BindEvents();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshUI;
            SessionManager.Instance.OnConfigurationChanged += HandleConfigurationChanged;
            if (NetworkSessionClient.Instance != null)
                NetworkSessionClient.Instance.OnClientStateChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            if (SessionManager.Instance != null)
                SessionManager.Instance.OnConfigurationChanged -= HandleConfigurationChanged;
            if (NetworkSessionClient.Instance != null)
                NetworkSessionClient.Instance.OnClientStateChanged -= RefreshUI;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BindEvents()
        {
            for (int i = 0; i < pathButtons.Length && i < SelectablePaths.Length; i++)
            {
                if (pathButtons[i] == null) continue;
                LearningPath targetPath = SelectablePaths[i];
                pathButtons[i].onClick.RemoveAllListeners();
                pathButtons[i].onClick.AddListener(() => SelectPath(targetPath));
            }

            if (assessmentToggleButton != null)
            {
                assessmentToggleButton.onClick.RemoveAllListeners();
                assessmentToggleButton.onClick.AddListener(ToggleAssessment);
            }
        }

        private void HandleConfigurationChanged(StudentConfiguration _)
        {
            RefreshUI();
        }

        private bool IsLanClientLocked()
        {
            return NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected;
        }

        public void SelectPath(LearningPath path)
        {
            if (IsLanClientLocked())
            {
                string host = NetworkSessionClient.Instance.ConnectedHostName;
                if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
                PlaytestStudentWidget.Instance?.ShowToast(string.Format(UIDialogs.Current.path_locked_by_class, host), 3.0f);
                return;
            }

            SessionManager.Instance.SetLearningPath(path);
            RefreshUI();

            if (PlaytestStudentWidget.Instance != null && SessionManager.Instance.CurrentConfiguration != null)
            {
                PlaytestStudentWidget.Instance.ShowToast(
                    StudentConfiguration.GetPathShortSummary(SessionManager.Instance.CurrentConfiguration), 2.5f);
            }
        }

        public void ToggleAssessment()
        {
            if (IsLanClientLocked())
            {
                string host = NetworkSessionClient.Instance.ConnectedHostName;
                if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
                PlaytestStudentWidget.Instance?.ShowToast(string.Format(UIDialogs.Current.path_locked_by_class, host), 3.0f);
                return;
            }

            bool nextState = !SessionManager.Instance.IsAssessmentEnabled;
            SessionManager.Instance.SetRunAssessmentOverride(nextState);
            RefreshUI();

            if (PlaytestStudentWidget.Instance != null)
            {
                UIDialogs ui = UIDialogs.Current;
                PlaytestStudentWidget.Instance.ShowToast(nextState ? ui.path_assess_on : ui.path_assess_off, 2.5f);
            }
        }

        public void RefreshUI()
        {
            UIDialogs ui = UIDialogs.Current;
            StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
            LearningPath activePath = cfg != null ? cfg.learningPath : SessionManager.Instance.ActiveLearningPath;
            bool assessOn = cfg != null ? cfg.runAssessment : SessionManager.Instance.IsAssessmentEnabled;
            bool lanLocked = IsLanClientLocked();

            if (sectionTitleText != null)
                sectionTitleText.text = ui.path_section_title;

            string[] pathNames =
            {
                ui.path_beginner_title,
                ui.path_intermediate_title,
                ui.path_advanced_title
            };

            for (int i = 0; i < SelectablePaths.Length; i++)
            {
                bool isSelected = SelectablePaths[i] == activePath;
                if (pathButtons != null && i < pathButtons.Length && pathButtons[i] != null)
                    pathButtons[i].interactable = !lanLocked;

                if (pathButtonBackgrounds != null && i < pathButtonBackgrounds.Length && pathButtonBackgrounds[i] != null)
                    pathButtonBackgrounds[i].color = isSelected ? _activePathBgColor : _inactivePathBgColor;

                if (pathButtonTitles != null && i < pathButtonTitles.Length && pathButtonTitles[i] != null)
                {
                    pathButtonTitles[i].text = pathNames[i];
                    pathButtonTitles[i].color = isSelected ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
                }
            }

            if (assessmentToggleButton != null)
                assessmentToggleButton.interactable = !lanLocked;

            if (assessmentToggleBg != null)
                assessmentToggleBg.color = assessOn ? _assessOnColor : _assessOffColor;

            if (assessmentToggleLabel != null)
            {
                assessmentToggleLabel.text = assessOn ? ui.path_assess_on : ui.path_assess_off;
                assessmentToggleLabel.color = RetroUITheme.InkWhite;
            }

            if (summaryFooterText != null && cfg != null)
                summaryFooterText.text = StudentConfiguration.GetPathShortSummary(cfg);

            if (lanLockedNoticeText != null)
            {
                lanLockedNoticeText.gameObject.SetActive(lanLocked);
                if (lanLocked)
                {
                    string host = NetworkSessionClient.Instance.ConnectedHostName;
                    if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
                    lanLockedNoticeText.text = string.Format(ui.path_locked_by_class, host);
                }
            }
        }
    }
}
