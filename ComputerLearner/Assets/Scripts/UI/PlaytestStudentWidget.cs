/**
 * Author: Julia & AI (TeamCompass)
 * Description: Scene-serialized controller for:
 *              1) The cheerful Student Welcome Name card on Menu.unity ("Hi, [Name]!")
 *              2) Playtest Student ID display, Data Export & AutoDownload inside Teacher Mode Panel
 *              3) Shared toast banner feedback.
 *              Zero procedural UI generation — all UI GameObjects are serialized in Menu.unity.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class PlaytestStudentWidget : MonoBehaviour
    {
        public static PlaytestStudentWidget Instance { get; private set; }
        public static bool IsEditingStudentId =>
            Instance != null && Instance.studentNameInputField != null && Instance.studentNameInputField.isFocused;

        [Header("Student Welcome Card (Main Menu)")]
        [SerializeField] private TMP_Text welcomeGreetingText;
        [SerializeField] private TMP_Text studentNamePromptText;
        [SerializeField] private TMP_InputField studentNameInputField;
        [SerializeField] private TMP_Text studentNamePlaceholderText;

        [Header("Teacher Mode Playtest Controls")]
        [SerializeField] private TMP_Text teacherStudentIdLabelText;
        [SerializeField] private TMP_Text teacherStudentIdValueText;
        [SerializeField] private Button exportButton;
        [SerializeField] private TMP_Text exportButtonLabel;
        [SerializeField] private Button autoDownloadButton;
        [SerializeField] private TMP_Text autoDownloadButtonLabel;

        [Header("Shared Toast Notification")]
        [SerializeField] private GameObject toastContainer;
        [SerializeField] private TMP_Text toastText;

        private Coroutine _toastCoroutine;
        private bool _suppressInputEvent;

        private void Awake()
        {
            Instance = this;
            BindEvents();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshUI;
            SessionManager.Instance.OnConfigurationChanged += HandleConfigurationChanged;
            RefreshUI();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            if (SessionManager.Instance != null)
                SessionManager.Instance.OnConfigurationChanged -= HandleConfigurationChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BindEvents()
        {
            if (studentNameInputField != null)
            {
                studentNameInputField.onEndEdit.RemoveListener(HandleStudentNameEndEdit);
                studentNameInputField.onEndEdit.AddListener(HandleStudentNameEndEdit);
                studentNameInputField.onValueChanged.RemoveListener(HandleStudentNameValueChanged);
                studentNameInputField.onValueChanged.AddListener(HandleStudentNameValueChanged);
            }

            if (exportButton != null)
            {
                exportButton.onClick.RemoveAllListeners();
                exportButton.onClick.AddListener(OnExportClicked);
            }

            if (autoDownloadButton != null)
            {
                autoDownloadButton.onClick.RemoveAllListeners();
                autoDownloadButton.onClick.AddListener(OnAutoDownloadClicked);
            }
        }

        private void HandleConfigurationChanged(StudentConfiguration _)
        {
            RefreshUI();
        }

        private void HandleStudentNameValueChanged(string newText)
        {
            if (_suppressInputEvent) return;
            UpdateGreetingPreview(newText);
        }

        private void HandleStudentNameEndEdit(string submitted)
        {
            if (_suppressInputEvent) return;
            string cleaned = string.IsNullOrWhiteSpace(submitted)
                ? PlaytestDataManager.CurrentStudentId
                : submitted.Trim();

            PlaytestDataManager.SetCurrentStudentId(cleaned);
            RefreshUI();
        }

        private void OnExportClicked()
        {
            if (TeacherModeGate.Instance != null && !TeacherModeGate.IsTeacherUnlocked) return;
            string path = PlaytestDataManager.ExportCurrentStudentData("manual_export");
            UIDialogs ui = UIDialogs.Current;
            string studentId = PlaytestDataManager.CurrentStudentId;
            if (!string.IsNullOrEmpty(path))
                ShowToast(string.Format(ui.playtest_export_success, studentId), 3.5f);
        }

        private void OnAutoDownloadClicked()
        {
            if (TeacherModeGate.Instance != null && !TeacherModeGate.IsTeacherUnlocked) return;
            bool nowEnabled = PlaytestDataManager.ToggleAutoDownload();
            RefreshUI();
            UIDialogs ui = UIDialogs.Current;
            ShowToast(nowEnabled ? ui.playtest_auto_download_enabled_toast : ui.playtest_auto_download_disabled_toast, 3.0f);
        }

        private void UpdateGreetingPreview(string rawName)
        {
            if (welcomeGreetingText == null) return;
            string displayName = string.IsNullOrWhiteSpace(rawName)
                ? PlaytestDataManager.CurrentStudentId
                : rawName.Trim();
            welcomeGreetingText.text = string.Format(UIDialogs.Current.menu_greeting_format, displayName);
        }

        public void RefreshUI()
        {
            UIDialogs ui = UIDialogs.Current;
            string studentId = PlaytestDataManager.CurrentStudentId;

            if (studentNameInputField != null && !studentNameInputField.isFocused)
            {
                _suppressInputEvent = true;
                studentNameInputField.text = studentId;
                _suppressInputEvent = false;
            }

            UpdateGreetingPreview(studentId);

            if (studentNamePromptText != null)
                studentNamePromptText.text = ui.menu_name_label;
            if (studentNamePlaceholderText != null)
                studentNamePlaceholderText.text = ui.menu_name_placeholder;

            if (teacherStudentIdLabelText != null)
                teacherStudentIdLabelText.text = ui.playtest_student_id_label;
            if (teacherStudentIdValueText != null)
                teacherStudentIdValueText.text = studentId;

            if (exportButtonLabel != null)
                exportButtonLabel.text = ui.playtest_btn_export;

            bool autoOn = PlaytestDataManager.AutoDownloadEnabled;
            if (autoDownloadButtonLabel != null)
                autoDownloadButtonLabel.text = autoOn ? ui.playtest_btn_auto_download_on : ui.playtest_btn_auto_download_off;

            if (autoDownloadButton != null)
                RetroUITheme.ConfigureButtonColors(autoDownloadButton, autoOn ? RetroUITheme.BtnMintGreen : RetroUITheme.BtnSoftSlate);
            if (autoDownloadButtonLabel != null)
                autoDownloadButtonLabel.color = autoOn ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (toastContainer == null || toastText == null || string.IsNullOrEmpty(message))
                return;

            toastText.text = message;
            toastContainer.SetActive(true);
            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(HideToastAfterDelay(duration));
        }

        private IEnumerator HideToastAfterDelay(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (toastContainer != null)
                toastContainer.SetActive(false);
            _toastCoroutine = null;
        }
    }
}
