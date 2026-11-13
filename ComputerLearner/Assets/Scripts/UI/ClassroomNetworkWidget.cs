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

        public static bool IsManualInputFocused =>
            Instance != null && Instance.manualIpInputField != null && Instance.manualIpInputField.isFocused;

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
        private bool _manualIpExpanded;

        private void Awake()
        {
            Instance = this;
            if (hostRowTemplate != null)
                hostRowTemplate.SetActive(false);
            if (manualIpSectionRoot != null)
                manualIpSectionRoot.SetActive(false);
            BindEvents();
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
                if (Time.unscaledTime >= _nextUiRefreshAt)
                {
                    _nextUiRefreshAt = Time.unscaledTime + 1.0f;
                    RebuildDiscoveredHostsList();
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

            if (leaveClassButton != null)
            {
                leaveClassButton.onClick.RemoveAllListeners();
                leaveClassButton.onClick.AddListener(() =>
                {
                    NetworkSessionClient.Instance?.Disconnect(false);
                    RefreshUI();
                });
            }

            if (toggleManualIpButton != null)
            {
                toggleManualIpButton.onClick.RemoveAllListeners();
                toggleManualIpButton.onClick.AddListener(() =>
                {
                    _manualIpExpanded = !_manualIpExpanded;
                    if (manualIpSectionRoot != null)
                        manualIpSectionRoot.SetActive(_manualIpExpanded);
                });
            }

            if (connectManualButton != null)
            {
                connectManualButton.onClick.RemoveAllListeners();
                connectManualButton.onClick.AddListener(OnManualConnectClicked);
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
            joinModalRoot.SetActive(true);

            if (clientErrorText != null)
                clientErrorText.text = string.Empty;

            if (manualIpInputField != null && NetworkSessionClient.Instance != null && string.IsNullOrEmpty(manualIpInputField.text))
                manualIpInputField.text = NetworkSessionClient.Instance.SavedLastHostIp ?? string.Empty;

            LanDiscovery.Instance?.StartListening();
            RebuildDiscoveredHostsList();
            RefreshUI();
        }

        public void CloseStudentJoinModal()
        {
            if (joinModalRoot != null)
                joinModalRoot.SetActive(false);

            bool isConnecting = NetworkSessionClient.Instance != null && NetworkSessionClient.Instance.IsConnecting;
            if (!isConnecting)
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

        private void OnManualConnectClicked()
        {
            if (NetworkSessionClient.Instance == null) return;
            string ip = manualIpInputField != null ? manualIpInputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(ip))
            {
                if (clientErrorText != null)
                    clientErrorText.text = UIDialogs.Current.lan_join_ip_empty;
                return;
            }

            if (clientErrorText != null)
                clientErrorText.text = UIDialogs.Current.lan_join_connecting;
            NetworkSessionClient.Instance.ConnectToHost(ip, NetworkProtocol.DefaultSessionPort);
        }

        private void HandleHostsUpdated(IReadOnlyList<DiscoveredHostInfo> _)
        {
            if (joinModalRoot != null && joinModalRoot.activeSelf)
                RebuildDiscoveredHostsList();
        }

        private void RebuildDiscoveredHostsList()
        {
            for (int i = 0; i < _spawnedHostRows.Count; i++)
            {
                if (_spawnedHostRows[i] != null)
                    Destroy(_spawnedHostRows[i]);
            }
            _spawnedHostRows.Clear();

            IReadOnlyList<DiscoveredHostInfo> hosts = LanDiscovery.Instance != null ? LanDiscovery.Instance.DiscoveredHosts : null;
            bool hasHosts = hosts != null && hosts.Count > 0;

            if (emptyHostsText != null)
            {
                emptyHostsText.gameObject.SetActive(!hasHosts);
                emptyHostsText.text = UIDialogs.Current.lan_join_no_hosts;
            }

            if (!hasHosts || hostRowTemplate == null || discoveredHostsContainer == null)
                return;

            UIDialogs ui = UIDialogs.Current;
            for (int i = 0; i < hosts.Count; i++)
            {
                DiscoveredHostInfo host = hosts[i];
                GameObject row = Instantiate(hostRowTemplate, discoveredHostsContainer);
                row.name = $"HostRow_{i}";
                row.SetActive(true);
                _spawnedHostRows.Add(row);

                TMP_Text hostNameText = row.transform.Find("HostNameText")?.GetComponent<TMP_Text>();
                if (hostNameText != null)
                    hostNameText.text = string.Format(ui.lan_join_host_entry_format, host.hostName, host.hostIp);

                Button joinBtn = row.transform.Find("Btn-JoinHost")?.GetComponent<Button>();
                TMP_Text joinBtnLabel = joinBtn != null ? joinBtn.GetComponentInChildren<TMP_Text>(true) : null;
                if (joinBtnLabel != null)
                    joinBtnLabel.text = ui.lan_join_btn_connect;

                if (joinBtn != null)
                {
                    string ipCopy = host.hostIp;
                    int portCopy = host.tcpPort;
                    joinBtn.onClick.RemoveAllListeners();
                    joinBtn.onClick.AddListener(() =>
                    {
                        if (clientErrorText != null)
                            clientErrorText.text = ui.lan_join_connecting;
                        NetworkSessionClient.Instance?.ConnectToHost(ipCopy, portCopy);
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
                    if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
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

            if (clientConnected && joinModalRoot != null && joinModalRoot.activeSelf)
                CloseStudentJoinModal();

            if (connectedBannerRoot != null)
                connectedBannerRoot.SetActive(clientConnected);

            if (connectedBannerText != null && clientConnected)
            {
                string host = NetworkSessionClient.Instance.ConnectedHostName;
                if (string.IsNullOrEmpty(host)) host = NetworkSessionClient.Instance.ConnectedHostIp;
                connectedBannerText.text = string.Format(ui.menu_connected_status_format, host);
            }

            if (leaveClassButtonLabel != null)
                leaveClassButtonLabel.text = ui.menu_btn_leave_class;

            if (joinModalTitleText != null)
                joinModalTitleText.text = ui.lan_join_modal_title;
            if (discoveryStatusText != null)
                discoveryStatusText.text = clientConnecting ? ui.lan_join_connecting : ui.lan_join_searching;
            if (toggleManualIpLabel != null)
                toggleManualIpLabel.text = ui.lan_join_manual_toggle;
            if (manualIpPlaceholderText != null)
                manualIpPlaceholderText.text = ui.lan_join_manual_placeholder;
            if (connectManualLabel != null)
                connectManualLabel.text = ui.lan_join_btn_connect;

            if (clientErrorText != null && NetworkSessionClient.Instance != null && !clientConnected && !clientConnecting)
            {
                string statusMsg = NetworkSessionClient.Instance.LastStatusMessage;
                if (!string.IsNullOrEmpty(statusMsg))
                    clientErrorText.text = statusMsg;
            }

            if (joinModalRoot != null && joinModalRoot.activeSelf)
                RebuildDiscoveredHostsList();

            RefreshTeacherHostSummary();
        }
    }
}
