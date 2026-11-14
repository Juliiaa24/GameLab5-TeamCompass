using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// Controls access to Teacher Mode behind a discreet corner button + 4-digit PIN modal.
    /// Enforces TeacherModeConfig.allowTeacherMode and automatic LAN client lockouts when connected to a class.
    /// </summary>
    public class TeacherModeGate : MonoBehaviour
    {
        private const string PinPrefsKey = "TeacherMode_CustomPin_v1";

        public static TeacherModeGate Instance { get; private set; }
        public static bool IsTeacherUnlocked { get; private set; }
        public static event Action<bool> OnTeacherLockChanged;

        [Header("Configuration")]
        [SerializeField] private TeacherModeConfig configOverride;

        [Header("Discreet Corner Access Button")]
        [SerializeField] private GameObject teacherAccessRoot;
        [SerializeField] private Button teacherAccessButton;
        [SerializeField] private Image teacherAccessIcon;
        [SerializeField] private TMP_Text teacherAccessLabel;
        [SerializeField] private Image holdProgressFill;

        [Header("PIN Modal")]
        [SerializeField] private GameObject pinModalRoot;
        [SerializeField] private TMP_Text pinTitleText;
        [SerializeField] private TMP_Text pinPromptText;
        [SerializeField] private TMP_InputField pinInputField;
        [SerializeField] private Image[] pinDotImages = new Image[4];
        [SerializeField] private TMP_Text pinErrorText;
        [SerializeField] private Button[] digitButtons = new Button[10]; // 0..9
        [SerializeField] private Button pinBackspaceButton;
        [SerializeField] private TMP_Text pinBackspaceLabel;
        [SerializeField] private Button pinClearButton;
        [SerializeField] private TMP_Text pinClearLabel;
        [SerializeField] private Button pinCloseButton;

        [Header("Teacher Mode Control Window")]
        [SerializeField] private GameObject teacherPanelRoot;
        [SerializeField] private TMP_Text teacherPanelTitleText;
        [SerializeField] private TMP_Text teacherPanelSubtitleText;
        [SerializeField] private Button teacherPanelCloseButton;
        [SerializeField] private Button lockTeacherModeButton;
        [SerializeField] private TMP_Text lockTeacherModeLabel;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private TMP_Text resetProgressLabel;
        [SerializeField] private GameObject lanLockNoticeBanner;
        [SerializeField] private TMP_Text lanLockNoticeText;
        [SerializeField] private TMP_Text sectionStudentControlsLabel;
        [SerializeField] private TMP_Text sectionClassNetworkLabel;
        [SerializeField] private TMP_Text sectionPinConfigLabel;

        [Header("Teacher PIN Change Controls")]
        [SerializeField] private TMP_InputField newPinInputField;
        [SerializeField] private TMP_Text newPinPlaceholderText;
        [SerializeField] private Button savePinButton;
        [SerializeField] private TMP_Text savePinLabel;
        [SerializeField] private TMP_Text pinChangeFeedbackText;

        [Header("Linked Menu Windows (for lock/close & modal checks)")]
        [SerializeField] private TeacherDashboardController teacherDashboard;
        [SerializeField] private GameObject studentJoinModalRoot;

        private string _enteredPin = string.Empty;
        private bool _isHolding;
        private float _holdTimer;
        private bool _holdTriggered;
        private float _feedbackClearAt = -1f;

        public TeacherModeConfig ActiveConfig => configOverride != null ? configOverride : TeacherModeConfig.Instance;

        public bool IsPinModalOpen => pinModalRoot != null && pinModalRoot.activeSelf;
        public bool IsTeacherPanelOpen => teacherPanelRoot != null && teacherPanelRoot.activeSelf;
        public bool IsAnyBlockingModalOpen
        {
            get
            {
                if (IsPinModalOpen || IsTeacherPanelOpen) return true;
                if (studentJoinModalRoot != null && studentJoinModalRoot.activeSelf) return true;
                if (teacherDashboard != null && teacherDashboard.IsOpen) return true;
                if (newPinInputField != null && newPinInputField.isFocused) return true;
                return false;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            BindEvents();
            ApplyBuildAvailability();
            RefreshLocalization();
            RefreshVisualState();
            TrySubscribeClient();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (NetworkSessionClient.Instance != null)
                NetworkSessionClient.Instance.OnClientStateChanged -= HandleClientStateChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_isHolding && !IsTeacherUnlocked && ActiveConfig.allowTeacherMode && ActiveConfig.requireLongPress)
            {
                _holdTimer += Time.unscaledDeltaTime;
                float dur = Mathf.Max(0.25f, ActiveConfig.longPressDuration);
                if (holdProgressFill != null)
                    holdProgressFill.fillAmount = Mathf.Clamp01(_holdTimer / dur);

                if (_holdTimer >= dur && !_holdTriggered)
                {
                    _holdTriggered = true;
                    _isHolding = false;
                    ResetHoldIndicator();
                    OpenPinOrTeacherPanel();
                }
            }

            if (IsPinModalOpen)
            {
                HandlePinKeyboardInput();
            }
            else if (IsTeacherPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (teacherDashboard == null || !teacherDashboard.IsOpen)
                    CloseTeacherPanel();
            }

            if (_feedbackClearAt > 0f && Time.unscaledTime >= _feedbackClearAt)
            {
                _feedbackClearAt = -1f;
                if (pinChangeFeedbackText != null)
                    pinChangeFeedbackText.text = string.Empty;
            }
        }

        private void BindEvents()
        {
            if (teacherAccessButton != null)
            {
                teacherAccessButton.onClick.RemoveListener(HandleAccessButtonClicked);
                teacherAccessButton.onClick.AddListener(HandleAccessButtonClicked);

                TeacherHoldTrigger trigger = teacherAccessButton.GetComponent<TeacherHoldTrigger>();
                if (trigger == null)
                    trigger = teacherAccessButton.gameObject.AddComponent<TeacherHoldTrigger>();
                trigger.Bind(BeginHold, CancelHold);
            }

            for (int i = 0; i < digitButtons.Length; i++)
            {
                if (digitButtons[i] == null) continue;
                int digit = i;
                digitButtons[i].onClick.RemoveAllListeners();
                digitButtons[i].onClick.AddListener(() => AppendPinDigit((char)('0' + digit)));
            }

            if (pinBackspaceButton != null)
            {
                pinBackspaceButton.onClick.RemoveAllListeners();
                pinBackspaceButton.onClick.AddListener(BackspacePin);
            }
            if (pinClearButton != null)
            {
                pinClearButton.onClick.RemoveAllListeners();
                pinClearButton.onClick.AddListener(ClearPin);
            }
            if (pinCloseButton != null)
            {
                pinCloseButton.onClick.RemoveAllListeners();
                pinCloseButton.onClick.AddListener(ClosePinModal);
            }
            if (pinInputField != null)
            {
                // Keep Keyboard.current as single source of truth so digits are never double-counted
                pinInputField.readOnly = true;
                pinInputField.characterLimit = 4;
            }
            if (teacherPanelCloseButton != null)
            {
                teacherPanelCloseButton.onClick.RemoveAllListeners();
                teacherPanelCloseButton.onClick.AddListener(CloseTeacherPanel);
            }
            if (lockTeacherModeButton != null)
            {
                lockTeacherModeButton.onClick.RemoveAllListeners();
                lockTeacherModeButton.onClick.AddListener(LockTeacherMode);
            }
            if (savePinButton != null)
            {
                savePinButton.onClick.RemoveAllListeners();
                savePinButton.onClick.AddListener(HandleChangePinClicked);
            }
            if (resetProgressButton != null)
            {
                resetProgressButton.onClick.RemoveAllListeners();
                resetProgressButton.onClick.AddListener(() =>
                {
                    if (!IsTeacherUnlocked) return;
                    if (SceneSystem.Instance != null)
                        SceneSystem.Instance.ResetProgress();
                });
            }
        }

        private void TrySubscribeClient()
        {
            if (NetworkSessionClient.Instance != null)
            {
                NetworkSessionClient.Instance.OnClientStateChanged -= HandleClientStateChanged;
                NetworkSessionClient.Instance.OnClientStateChanged += HandleClientStateChanged;
            }
        }

        private void HandleLanguageChanged()
        {
            RefreshLocalization();
            RefreshVisualState();
        }

        private void HandleClientStateChanged()
        {
            RefreshVisualState();
        }

        public void ApplyBuildAvailability()
        {
            bool allowed = ActiveConfig.allowTeacherMode;
            if (!allowed)
            {
                IsTeacherUnlocked = false;
                if (pinModalRoot != null) pinModalRoot.SetActive(false);
                if (teacherPanelRoot != null) teacherPanelRoot.SetActive(false);
                if (teacherDashboard != null && teacherDashboard.IsOpen) teacherDashboard.Close();
            }
            if (teacherAccessRoot != null)
                teacherAccessRoot.SetActive(allowed);
        }

        private void BeginHold()
        {
            if (!ActiveConfig.allowTeacherMode || IsTeacherUnlocked || !ActiveConfig.requireLongPress) return;
            _isHolding = true;
            _holdTriggered = false;
            _holdTimer = 0f;
            if (holdProgressFill != null)
                holdProgressFill.fillAmount = 0f;
        }

        private void CancelHold()
        {
            _isHolding = false;
            _holdTimer = 0f;
            ResetHoldIndicator();
        }

        private void ResetHoldIndicator()
        {
            if (holdProgressFill != null)
                holdProgressFill.fillAmount = 0f;
        }

        public void HandleAccessButtonClicked()
        {
            if (!ActiveConfig.allowTeacherMode) return;

            if (IsTeacherUnlocked)
            {
                if (IsTeacherPanelOpen)
                    CloseTeacherPanel();
                else
                    OpenTeacherPanel();
                return;
            }

            if (ActiveConfig.requireLongPress)
            {
                if (_holdTriggered)
                    _holdTriggered = false;
                return;
            }

            OpenPinOrTeacherPanel();
        }

        public void OpenPinOrTeacherPanel()
        {
            if (!ActiveConfig.allowTeacherMode) return;
            if (IsTeacherUnlocked)
            {
                OpenTeacherPanel();
                return;
            }

            _enteredPin = string.Empty;
            if (pinErrorText != null)
                pinErrorText.text = string.Empty;
            if (pinModalRoot != null)
            {
                pinModalRoot.SetActive(true);
                pinModalRoot.transform.SetAsLastSibling();
            }
            UpdatePinDots();
            FocusPinInputField();
        }

        public void ClosePinModal()
        {
            _enteredPin = string.Empty;
            if (pinErrorText != null)
                pinErrorText.text = string.Empty;
            if (pinInputField != null)
            {
                pinInputField.SetTextWithoutNotify(string.Empty);
                pinInputField.DeactivateInputField();
            }
            if (pinModalRoot != null)
                pinModalRoot.SetActive(false);
        }

        private void FocusPinInputField()
        {
            if (pinInputField == null) return;
            pinInputField.SetTextWithoutNotify(new string('*', _enteredPin.Length));
            EventSystem.current?.SetSelectedGameObject(pinInputField.gameObject);
            pinInputField.Select();
            pinInputField.ActivateInputField();
            if (isActiveAndEnabled)
                StartCoroutine(FocusPinInputDeferred());
        }

        private IEnumerator FocusPinInputDeferred()
        {
            yield return null;
            if (!IsPinModalOpen || pinInputField == null) yield break;
            EventSystem.current?.SetSelectedGameObject(pinInputField.gameObject);
            pinInputField.Select();
            pinInputField.ActivateInputField();
        }

        public void OpenTeacherPanel()
        {
            if (!ActiveConfig.allowTeacherMode || !IsTeacherUnlocked) return;
            if (pinModalRoot != null) pinModalRoot.SetActive(false);
            if (studentJoinModalRoot != null) studentJoinModalRoot.SetActive(false);
            if (teacherPanelRoot != null) teacherPanelRoot.SetActive(true);
            RefreshVisualState();
        }

        public void CloseTeacherPanel()
        {
            if (teacherPanelRoot != null) teacherPanelRoot.SetActive(false);
        }

        public void LockTeacherMode()
        {
            IsTeacherUnlocked = false;
            if (teacherPanelRoot != null) teacherPanelRoot.SetActive(false);
            if (pinModalRoot != null) pinModalRoot.SetActive(false);
            if (teacherDashboard != null && teacherDashboard.IsOpen) teacherDashboard.Close();
            RefreshVisualState();
            OnTeacherLockChanged?.Invoke(false);
        }

        public void AppendPinDigit(char digit)
        {
            if (!char.IsDigit(digit) || _enteredPin.Length >= 4) return;
            _enteredPin += digit;
            if (pinErrorText != null) pinErrorText.text = string.Empty;
            UpdatePinDots();

            if (_enteredPin.Length == 4)
            {
                ValidatePin();
            }
        }

        public void BackspacePin()
        {
            if (_enteredPin.Length == 0) return;
            _enteredPin = _enteredPin.Substring(0, _enteredPin.Length - 1);
            if (pinErrorText != null) pinErrorText.text = string.Empty;
            UpdatePinDots();
        }

        public void ClearPin()
        {
            _enteredPin = string.Empty;
            if (pinErrorText != null) pinErrorText.text = string.Empty;
            UpdatePinDots();
        }

        public string GetActivePin()
        {
            string stored = PlayerPrefs.GetString(PinPrefsKey, string.Empty);
            if (IsFourDigits(stored)) return stored;
            return ActiveConfig.GetNormalizedDefaultPin();
        }

        public bool TryUnlockWithPin(string candidate)
        {
            if (!ActiveConfig.allowTeacherMode) return false;
            if (string.Equals(candidate, GetActivePin(), StringComparison.Ordinal))
            {
                IsTeacherUnlocked = true;
                ClosePinModal();
                OpenTeacherPanel();
                OnTeacherLockChanged?.Invoke(true);
                return true;
            }
            return false;
        }

        private void ValidatePin()
        {
            if (TryUnlockWithPin(_enteredPin))
                return;

            UIDialogs ui = UIDialogs.Current;
            if (pinErrorText != null)
                pinErrorText.text = ui.teacher_pin_wrong;
            _enteredPin = string.Empty;
            UpdatePinDots();
        }

        private void HandleChangePinClicked()
        {
            if (!IsTeacherUnlocked || newPinInputField == null) return;
            string candidate = (newPinInputField.text ?? string.Empty).Trim();
            UIDialogs ui = UIDialogs.Current;

            if (!IsFourDigits(candidate))
            {
                ShowPinChangeFeedback(ui.teacher_pin_invalid_length, RetroUITheme.DangerCoral);
                return;
            }

            PlayerPrefs.SetString(PinPrefsKey, candidate);
            PlayerPrefs.Save();
            newPinInputField.text = string.Empty;
            ShowPinChangeFeedback(string.Format(ui.teacher_pin_updated_format, candidate), RetroUITheme.SuccessMint);
        }

        private void ShowPinChangeFeedback(string message, Color color)
        {
            if (pinChangeFeedbackText == null) return;
            pinChangeFeedbackText.text = message;
            pinChangeFeedbackText.color = color;
            _feedbackClearAt = Time.unscaledTime + 3.5f;
        }

        private void UpdatePinDots()
        {
            for (int i = 0; i < pinDotImages.Length; i++)
            {
                if (pinDotImages[i] == null) continue;
                bool filled = i < _enteredPin.Length;
                pinDotImages[i].color = filled ? RetroUITheme.BtnSunnyGold : RetroUITheme.ProgressTrack;
            }
            if (pinInputField != null)
            {
                pinInputField.SetTextWithoutNotify(new string('*', _enteredPin.Length));
            }
        }

        private void HandlePinKeyboardInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                ClosePinModal();
                return;
            }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                ValidatePin();
                return;
            }
            if (kb.backspaceKey.wasPressedThisFrame)
            {
                BackspacePin();
                return;
            }

            KeyControl[] mainDigits =
            {
                kb.digit0Key, kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key,
                kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key
            };
            KeyControl[] numpadDigits =
            {
                kb.numpad0Key, kb.numpad1Key, kb.numpad2Key, kb.numpad3Key, kb.numpad4Key,
                kb.numpad5Key, kb.numpad6Key, kb.numpad7Key, kb.numpad8Key, kb.numpad9Key
            };

            for (int d = 0; d <= 9; d++)
            {
                if (mainDigits[d].wasPressedThisFrame || numpadDigits[d].wasPressedThisFrame)
                {
                    AppendPinDigit((char)('0' + d));
                    break;
                }
            }
        }

        public void RefreshLocalization()
        {
            UIDialogs ui = UIDialogs.Current;

            if (teacherAccessLabel != null)
                teacherAccessLabel.text = IsTeacherUnlocked ? ui.teacher_btn_unlocked : ui.teacher_btn_locked;

            if (pinTitleText != null)
                pinTitleText.text = ui.teacher_pin_title;
            if (pinPromptText != null)
                pinPromptText.text = ui.teacher_pin_prompt;
            if (pinBackspaceLabel != null)
                pinBackspaceLabel.text = ui.teacher_pin_backspace;
            if (pinClearLabel != null)
                pinClearLabel.text = ui.teacher_pin_clear;

            if (teacherPanelTitleText != null)
                teacherPanelTitleText.text = ui.teacher_panel_title;
            if (teacherPanelSubtitleText != null)
                teacherPanelSubtitleText.text = ui.teacher_panel_subtitle;
            if (lockTeacherModeLabel != null)
                lockTeacherModeLabel.text = ui.teacher_btn_lock;
            if (resetProgressLabel != null)
                resetProgressLabel.text = ui.menu_btn_reset_progress;
            if (sectionStudentControlsLabel != null)
                sectionStudentControlsLabel.text = ui.teacher_section_student_controls;
            if (sectionClassNetworkLabel != null)
                sectionClassNetworkLabel.text = ui.teacher_section_class_network;
            if (sectionPinConfigLabel != null)
                sectionPinConfigLabel.text = ui.teacher_section_pin_config;
            if (newPinPlaceholderText != null)
                newPinPlaceholderText.text = ui.teacher_pin_new_placeholder;
            if (savePinLabel != null)
                savePinLabel.text = ui.teacher_pin_save_btn;
        }

        public void RefreshVisualState()
        {
            ApplyBuildAvailability();
            RetroThemeAssets assets = RetroThemeAssets.Instance;

            if (teacherAccessIcon != null && assets != null)
                teacherAccessIcon.sprite = IsTeacherUnlocked ? assets.iconUnlock : assets.iconLock;

            if (teacherAccessButton != null)
            {
                Color cornerTint = IsTeacherUnlocked ? RetroUITheme.BtnMintGreen : RetroUITheme.CardPastelYellow;
                RetroUITheme.ConfigureButtonColors(teacherAccessButton, cornerTint);
            }

            if (teacherAccessLabel != null)
            {
                UIDialogs ui = UIDialogs.Current;
                teacherAccessLabel.text = IsTeacherUnlocked ? ui.teacher_btn_unlocked : ui.teacher_btn_locked;
                teacherAccessLabel.color = IsTeacherUnlocked ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
            }

            bool isClientConnected = NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected;
            if (lanLockNoticeBanner != null)
                lanLockNoticeBanner.SetActive(isClientConnected);
            if (lanLockNoticeText != null && isClientConnected)
            {
                string host = NetworkSessionClient.Instance.ConnectedHostName;
                if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
                lanLockNoticeText.text = string.Format(UIDialogs.Current.lan_connected_banner_format, host);
            }
        }

        private static bool IsFourDigits(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 4) return false;
            for (int i = 0; i < 4; i++)
            {
                if (!char.IsDigit(value[i])) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Pointer hold listener attached to the corner Teacher Mode button so long-press mode works seamlessly.
    /// </summary>
    public class TeacherHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Action _onDown;
        private Action _onCancel;

        public void Bind(Action onDown, Action onCancel)
        {
            _onDown = onDown;
            _onCancel = onCancel;
        }

        public void OnPointerDown(PointerEventData eventData) => _onDown?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => _onCancel?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => _onCancel?.Invoke();
    }
}
