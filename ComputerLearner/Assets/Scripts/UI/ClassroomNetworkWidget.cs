/**
 * Author: TeamCompass
 * Description: Scene-serialized Classroom LAN controller for Menu.unity:
 *              - Student Join Class modal (auto-discovered teacher classes + collapsible manual IP fallback)
 *              - Student Connected Class banner on Menu (with friendly "Connected to class" indicator + Leave button)
 *              - Teacher LAN Server controls inside Teacher Mode Panel (opens TeacherDashboardController).
 *              Zero procedural UI generation — uses serialized scene objects and hostRowTemplate.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class ClassroomNetworkWidget : MonoBehaviour
    {
        public static ClassroomNetworkWidget Instance { get; private set; }

        public static bool IsManualInputFocused => false;

        public static bool IsAnyInputFocused => IsManualInputFocused;

        [Header("Student Connected Banner (Menu Main)")]
        [SerializeField] private GameObject connectedBannerRoot;
        [SerializeField] private TMP_Text connectedBannerText;
        [SerializeField] private Button leaveClassButton;
        [SerializeField] private TMP_Text leaveClassButtonLabel;

        [Header("Student Join Classroom Modal")]
        [SerializeField] private GameObject joinModalRoot;
        [SerializeField] private TMP_Text joinModalTitleText;
        [SerializeField] private Button joinModalCloseButton;
        [SerializeField] private Button refreshDiscoveryButton;
        [SerializeField] private TMP_Text refreshDiscoveryLabel;
        [SerializeField] private RectTransform refreshSpinnerIcon;
        [SerializeField] private TMP_Text discoveryStatusText;
        [SerializeField] private Transform discoveredHostsContainer;
        [SerializeField] private GameObject hostRowTemplate;
        [SerializeField] private TMP_Text emptyHostsText;
        [SerializeField] private Button toggleManualIpButton;
        [SerializeField] private TMP_Text toggleManualIpLabel;
        [SerializeField] private GameObject manualIpSectionRoot;
        [SerializeField] private TMP_InputField manualIpInputField;
        [SerializeField] private TMP_Text manualIpPlaceholderText;
        [SerializeField] private Button connectManualButton;
        [SerializeField] private TMP_Text connectManualLabel;
        [SerializeField] private TMP_Text clientErrorText;

        [Header("Teacher Mode Panel Network Section")]
        [SerializeField] private Button openTeacherDashboardButton;
        [SerializeField] private TMP_Text openTeacherDashboardLabel;
        [SerializeField] private TMP_Text teacherHostStatusText;
        [SerializeField] private TeacherDashboardController teacherDashboard;

        private readonly List<GameObject> _spawnedHostRows = new List<GameObject>();
        private float _nextUiRefreshAt;
        private float _manualSearchSpinUntil;
        private string _lastHostsSignature = string.Empty;

        private void Awake()
        {
            Instance = this;
            if (hostRowTemplate != null)
                hostRowTemplate.SetActive(false);
            HideManualIpControls();
            BindEvents();
        }

        private void HideManualIpControls()
        {
            if (toggleManualIpButton != null)
                toggleManualIpButton.gameObject.SetActive(false);
            if (manualIpSectionRoot != null)
                manualIpSectionRoot.SetActive(false);

            ScrollRect hostsScroll = discoveredHostsContainer != null
                ? discoveredHostsContainer.GetComponentInParent<ScrollRect>()
                : null;
            if (hostsScroll != null)
            {
                RectTransform scrollRt = hostsScroll.GetComponent<RectTransform>();
                if (scrollRt != null)
                {
                    scrollRt.offsetMin = new Vector2(scrollRt.offsetMin.x, 54f);
                }
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshUI;
            SubscribeNetworkEvents(true);
            RefreshUI();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            SubscribeNetworkEvents(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (joinModalRoot != null && joinModalRoot.activeSelf)
            {
                bool isSearching = Time.unscaledTime < _manualSearchSpinUntil ||
                                   (LanDiscovery.ExistingInstance != null && LanDiscovery.ExistingInstance.DiscoveredHosts.Count == 0);
                if (refreshSpinnerIcon != null)
                {
                    if (isSearching)
                        refreshSpinnerIcon.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
                    else
                        refreshSpinnerIcon.localRotation = Quaternion.identity;
                }

                if (Time.unscaledTime >= _nextUiRefreshAt)
                {
                    _nextUiRefreshAt = Time.unscaledTime + 0.8f;
                    RebuildDiscoveredHostsList(forceRebuild: false);
                }

                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    CloseStudentJoinModal();
            }

            if (NetworkSessionHost.Instance != null && NetworkSessionHost.Instance.IsHosting && Time.unscaledTime >= _nextUiRefreshAt)
            {
                _nextUiRefreshAt = Time.unscaledTime + 1.0f;
                RefreshTeacherHostSummary();
            }
        }

        private void BindEvents()
        {
            if (joinModalCloseButton != null)
            {
                joinModalCloseButton.onClick.RemoveAllListeners();
                joinModalCloseButton.onClick.AddListener(CloseStudentJoinModal);
            }

            if (refreshDiscoveryButton != null)
            {
                refreshDiscoveryButton.onClick.RemoveAllListeners();
                refreshDiscoveryButton.onClick.AddListener(OnRefreshDiscoveryClicked);
            }

            if (leaveClassButton != null)
            {
                leaveClassButton.onClick.RemoveAllListeners();
                leaveClassButton.onClick.AddListener(() =>
                {
                    if (NetworkSessionClient.ExistingInstance != null &&
                        NetworkSessionClient.ExistingInstance.IsConnected &&
                        NetworkSessionClient.ExistingInstance.IsLeaveClassLocked)
                    {
                        return;
                    }
                    NetworkSessionClient.Instance?.Disconnect(false);
                    if (joinModalRoot == null || !joinModalRoot.activeSelf)
                        LanDiscovery.ExistingInstance?.StopListening();
                    RefreshUI();
                });
            }

            if (openTeacherDashboardButton != null)
            {
                openTeacherDashboardButton.onClick.RemoveAllListeners();
                openTeacherDashboardButton.onClick.AddListener(OpenTeacherDashboardClicked);
            }
        }

        private void SubscribeNetworkEvents(bool subscribe)
        {
            if (subscribe)
            {
                if (NetworkSessionHost.Instance != null)
                {
                    NetworkSessionHost.Instance.OnHostStateChanged -= RefreshUI;
                    NetworkSessionHost.Instance.OnHostStateChanged += RefreshUI;
                    NetworkSessionHost.Instance.OnStudentsChanged -= RefreshUI;
                    NetworkSessionHost.Instance.OnStudentsChanged += RefreshUI;
                }
                if (NetworkSessionClient.Instance != null)
                {
                    NetworkSessionClient.Instance.OnClientStateChanged -= RefreshUI;
                    NetworkSessionClient.Instance.OnClientStateChanged += RefreshUI;
                }
                if (LanDiscovery.Instance != null)
                {
                    LanDiscovery.Instance.OnHostsUpdated -= HandleHostsUpdated;
                    LanDiscovery.Instance.OnHostsUpdated += HandleHostsUpdated;
                }
            }
            else
            {
                if (NetworkSessionHost.Instance != null)
                {
                    NetworkSessionHost.Instance.OnHostStateChanged -= RefreshUI;
                    NetworkSessionHost.Instance.OnStudentsChanged -= RefreshUI;
                }
                if (NetworkSessionClient.Instance != null)
                {
                    NetworkSessionClient.Instance.OnClientStateChanged -= RefreshUI;
                }
                if (LanDiscovery.Instance != null)
                {
                    LanDiscovery.Instance.OnHostsUpdated -= HandleHostsUpdated;
                }
            }
        }

        public void OpenStudentJoinModal()
        {
            if (joinModalRoot == null) return;
            HideManualIpControls();
            joinModalRoot.SetActive(true);
            joinModalRoot.transform.SetAsLastSibling();

            if (clientErrorText != null)
                clientErrorText.text = string.Empty;

            _manualSearchSpinUntil = Time.unscaledTime + 1.5f;
            LanDiscovery.Instance?.StartListening(NetworkProtocol.DiscoveryPort);
            RebuildDiscoveredHostsList(forceRebuild: true);
            RefreshUI();
        }

        private void OnRefreshDiscoveryClicked()
        {
            _manualSearchSpinUntil = Time.unscaledTime + 1.5f;
            if (clientErrorText != null)
                clientErrorText.text = string.Empty;

            LanDiscovery.Instance?.RestartDiscovery(NetworkProtocol.DiscoveryPort);
            RebuildDiscoveredHostsList(forceRebuild: true);
            RefreshUI();
        }

        public void CloseStudentJoinModal()
        {
            if (joinModalRoot != null)
                joinModalRoot.SetActive(false);

            bool keepListening = NetworkSessionClient.Instance != null &&
                                 (NetworkSessionClient.Instance.IsConnecting || NetworkSessionClient.Instance.IsConnected);
            if (!keepListening)
                LanDiscovery.Instance?.StopListening();
        }

        private void OpenTeacherDashboardClicked()
        {
            if (TeacherModeGate.Instance != null && !TeacherModeGate.IsTeacherUnlocked)
                return;
            if (NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected)
                return;

            if (teacherDashboard != null)
                teacherDashboard.Open();
            else
                TeacherDashboardController.OpenOnCanvas(GetComponentInParent<Canvas>()?.transform);
        }

        private void HandleHostsUpdated(IReadOnlyList<DiscoveredHostInfo> _)
        {
            if (joinModalRoot != null && joinModalRoot.activeSelf)
                RebuildDiscoveredHostsList(forceRebuild: false);
        }

        private string ComputeHostsSignature(IReadOnlyList<DiscoveredHostInfo> hosts, UIDialogs ui)
        {
            string lang = LocalizationManager.CurrentLanguage;
            if (hosts == null || hosts.Count == 0)
                return $"{lang}|EMPTY|{ui.lan_join_no_hosts}";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(lang).Append('|').Append(ui.lan_join_btn_connect).Append('|').Append(hosts.Count);
            for (int i = 0; i < hosts.Count; i++)
            {
                DiscoveredHostInfo h = hosts[i];
                sb.Append('|').Append(h.hostName).Append(':').Append(h.EndpointKey);
            }
            return sb.ToString();
        }

        private void RebuildDiscoveredHostsList(bool forceRebuild)
        {
            IReadOnlyList<DiscoveredHostInfo> hosts = LanDiscovery.Instance != null ? LanDiscovery.Instance.DiscoveredHosts : null;
            UIDialogs ui = UIDialogs.Current;
            string signature = ComputeHostsSignature(hosts, ui);
            if (!forceRebuild && signature == _lastHostsSignature)
                return;
            _lastHostsSignature = signature;

            for (int i = 0; i < _spawnedHostRows.Count; i++)
            {
                if (_spawnedHostRows[i] != null)
                    Destroy(_spawnedHostRows[i]);
            }
            _spawnedHostRows.Clear();

            bool hasHosts = hosts != null && hosts.Count > 0;

            if (emptyHostsText != null)
            {
                emptyHostsText.gameObject.SetActive(!hasHosts);
                emptyHostsText.text = ui.lan_join_no_hosts;
            }

            if (!hasHosts || hostRowTemplate == null || discoveredHostsContainer == null)
                return;

            for (int i = 0; i < hosts.Count; i++)
            {
                DiscoveredHostInfo host = hosts[i];
                GameObject row = Instantiate(hostRowTemplate, discoveredHostsContainer);
                row.name = $"HostRow_{i}";
                row.SetActive(true);
                _spawnedHostRows.Add(row);

                TMP_Text hostNameText = row.transform.Find("HostNameText")?.GetComponent<TMP_Text>();
                if (hostNameText != null)
                    hostNameText.text = host.hostName;

                Button joinBtn = row.transform.Find("Btn-JoinHost")?.GetComponent<Button>();
                TMP_Text joinBtnLabel = joinBtn != null ? joinBtn.GetComponentInChildren<TMP_Text>(true) : null;
                if (joinBtnLabel != null)
                    joinBtnLabel.text = ui.lan_join_btn_connect;

                if (joinBtn != null)
                {
                    DiscoveredHostInfo hostCopy = host;
                    joinBtn.onClick.RemoveAllListeners();
                    joinBtn.onClick.AddListener(() =>
                    {
                        if (clientErrorText != null)
                            clientErrorText.text = ui.lan_join_connecting;
                        NetworkSessionClient.Instance?.ConnectToHost(hostCopy);
                    });
                }
            }
        }

        private void RefreshTeacherHostSummary()
        {
            UIDialogs ui = UIDialogs.Current;
            bool hosting = NetworkSessionHost.Instance != null && NetworkSessionHost.Instance.IsHosting;
            bool clientConnected = NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected;

            if (openTeacherDashboardButton != null)
                openTeacherDashboardButton.interactable = !clientConnected;

            if (openTeacherDashboardLabel != null)
                openTeacherDashboardLabel.text = ui.lan_teacher_open_dashboard;

            if (teacherHostStatusText != null)
            {
                if (clientConnected)
                {
                    string host = NetworkSessionClient.Instance.ConnectedHostName;
                    if (string.IsNullOrEmpty(host)) host = "Teacher";
                    teacherHostStatusText.text = string.Format(ui.lan_connected_banner_format, host);
                    teacherHostStatusText.color = RetroUITheme.TitleBlue;
                }
                else if (hosting)
                {
                    teacherHostStatusText.text = string.Format(
                        ui.lan_teacher_host_online_format,
                        NetworkSessionHost.Instance.ActiveConnectedCount,
                        NetworkSessionHost.Instance.LocalIpAddress,
                        NetworkSessionHost.Instance.ListenPort);
                    teacherHostStatusText.color = RetroUITheme.SuccessMint;
                }
                else
                {
                    teacherHostStatusText.text = ui.lan_teacher_host_offline;
                    teacherHostStatusText.color = RetroUITheme.InkMuted;
                }
            }
        }

        public void RefreshUI()
        {
            UIDialogs ui = UIDialogs.Current;
            bool clientConnected = NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnected;
            bool clientConnecting = NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnecting;
            bool leaveLocked = clientConnected && NetworkSessionClient.Instance.IsLeaveClassLocked;

            HideManualIpControls();

            if (clientConnected && joinModalRoot != null && joinModalRoot.activeSelf)
                CloseStudentJoinModal();

            if (connectedBannerRoot != null)
                connectedBannerRoot.SetActive(clientConnected);

            if (connectedBannerText != null && clientConnected)
            {
                string host = NetworkSessionClient.Instance.ConnectedHostName;
                if (string.IsNullOrEmpty(host)) host = "Teacher";
                connectedBannerText.text = string.Format(ui.menu_connected_status_format, host);
            }

            if (leaveClassButton != null)
            {
                leaveClassButton.gameObject.SetActive(clientConnected && !leaveLocked);
                leaveClassButton.interactable = clientConnected && !leaveLocked;
            }
            if (leaveClassButtonLabel != null)
                leaveClassButtonLabel.text = ui.menu_btn_leave_class;

            if (joinModalTitleText != null)
                joinModalTitleText.text = ui.lan_join_modal_title;
            if (refreshDiscoveryLabel != null)
                refreshDiscoveryLabel.text = ui.net_btn_search_again;
            if (discoveryStatusText != null)
                discoveryStatusText.text = clientConnecting ? ui.lan_join_connecting : ui.lan_join_searching;

            if (clientErrorText != null && NetworkSessionClient.Instance != null && !clientConnected && !clientConnecting)
            {
                string statusMsg = NetworkSessionClient.Instance.LastStatusMessage;
                if (!string.IsNullOrEmpty(statusMsg))
                    clientErrorText.text = statusMsg;
            }

            if (joinModalRoot != null && joinModalRoot.activeSelf)
                RebuildDiscoveredHostsList(forceRebuild: false);

            RefreshTeacherHostSummary();
        }
    }
}
