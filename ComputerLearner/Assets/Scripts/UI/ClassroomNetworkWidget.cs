/**
 * Author: TeamCompass
 * Description: Top-right LAN Classroom card on Menu.unity allowing the user to either:
 *              1) Open the Teacher Classroom Dashboard (Host LAN server), or
 *              2) Join a Classroom as a Student Client (automatic UDP LAN discovery + manual IP fallback).
 *              Matches Menu.unity's clean rounded light UI style.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace ComputerLearning
{
    public class ClassroomNetworkWidget : MonoBehaviour
    {
        public static ClassroomNetworkWidget Instance { get; private set; }

        private Transform canvasRoot;
        private Sprite roundedSprite;
        private TMP_FontAsset menuFont;
        private RectTransform cardRect;

        private TextMeshProUGUI headerLabel;
        private TextMeshProUGUI statusBadgeLabel;
        private Image statusBadgeBg;

        private GameObject idleButtonsPanel;
        private TextMeshProUGUI btnHostLabel;
        private TextMeshProUGUI btnJoinToggleLabel;

        private GameObject studentJoinPanel;
        private TextMeshProUGUI discoveryStatusLabel;
        private Transform discoveredHostsContainer;
        private TMP_InputField ipInputField;
        private TextMeshProUGUI fallbackIpText;
        private TextMeshProUGUI btnConnectManualLabel;
        private TextMeshProUGUI clientErrorLabel;

        private GameObject connectedStudentPanel;
        private TextMeshProUGUI connectedInfoLabel;
        private TextMeshProUGUI btnDisconnectLabel;

        private GameObject activeHostPanel;
        private TextMeshProUGUI activeHostInfoLabel;
        private TextMeshProUGUI btnOpenDashboardLabel;
        private TextMeshProUGUI btnStopHostLabel;

        private bool isJoinExpanded = false;
        private bool fallbackIpFocused = false;
        private string manualIpBuffer = "127.0.0.1";

        private readonly Color primaryBlue = new Color(0.15f, 0.39f, 0.92f, 1f);
        private readonly Color emeraldGreen = new Color(0.09f, 0.60f, 0.36f, 1f);
        private readonly Color dangerRed = new Color(0.82f, 0.24f, 0.24f, 1f);
        private readonly Color darkSlate = new Color(0.14f, 0.18f, 0.25f, 1f);
        private readonly Color mutedSlate = new Color(0.38f, 0.45f, 0.55f, 1f);

        public static bool IsManualInputFocused =>
            Instance != null &&
            ((Instance.ipInputField != null && Instance.ipInputField.isFocused) || Instance.fallbackIpFocused);

        public static ClassroomNetworkWidget EnsureOnCanvas(Transform canvasTransform)
        {
            if (canvasTransform == null) return null;
            if (SceneManager.GetActiveScene().name != "Menu") return null;

            Transform existing = canvasTransform.Find("ClassroomNetworkWidget");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject rootObj = new GameObject("ClassroomNetworkWidget", typeof(RectTransform), typeof(ClassroomNetworkWidget));
            rootObj.transform.SetParent(canvasTransform, false);
            rootObj.transform.SetAsLastSibling();

            ClassroomNetworkWidget widget = rootObj.GetComponent<ClassroomNetworkWidget>();
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
            LocalizationManager.OnLanguageChanged += RefreshUI;
            SessionManager.Instance.OnConfigurationChanged += HandleConfigChanged;
            NetworkSessionHost.Instance.OnHostStateChanged += RefreshUI;
            NetworkSessionHost.Instance.OnStudentsChanged += RefreshUI;
            NetworkSessionClient.Instance.OnClientStateChanged += RefreshUI;
            LanDiscovery.Instance.OnHostsUpdated += HandleDiscoveredHostsUpdated;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            if (SessionManager.ExistingInstance != null)
                SessionManager.ExistingInstance.OnConfigurationChanged -= HandleConfigChanged;
            if (NetworkSessionHost.ExistingInstance != null)
            {
                NetworkSessionHost.ExistingInstance.OnHostStateChanged -= RefreshUI;
                NetworkSessionHost.ExistingInstance.OnStudentsChanged -= RefreshUI;
            }
            if (NetworkSessionClient.ExistingInstance != null)
                NetworkSessionClient.ExistingInstance.OnClientStateChanged -= RefreshUI;
            if (LanDiscovery.ExistingInstance != null)
                LanDiscovery.ExistingInstance.OnHostsUpdated -= HandleDiscoveredHostsUpdated;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (ipInputField == null && fallbackIpFocused && Keyboard.current != null)
            {
                PollFallbackIpKeyboard();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu")
            {
                Destroy(gameObject);
            }
        }

        private void HandleConfigChanged(StudentConfiguration cfg)
        {
            RefreshUI();
        }

        private void HandleDiscoveredHostsUpdated(IReadOnlyList<DiscoveredHostInfo> hosts)
        {
            RebuildDiscoveredHostsList(hosts);
        }

        private void BuildUI(Transform canvasTransform)
        {
            canvasRoot = canvasTransform;
            manualIpBuffer = NetworkSessionClient.Instance.SavedLastHostIp;

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
                if (tmp != null) menuFont = tmp.font;
            }

            cardRect = GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(1f, 1f);
            cardRect.anchorMax = new Vector2(1f, 1f);
            cardRect.pivot = new Vector2(1f, 1f);
            cardRect.anchoredPosition = new Vector2(-40f, -28f);
            cardRect.sizeDelta = new Vector2(440f, 162f);

            Image cardBg = gameObject.AddComponent<Image>();
            cardBg.color = new Color(0.95f, 0.96f, 0.98f, 0.97f);
            ApplyRounded(cardBg);

            // Header Row
            headerLabel = CreateText("HeaderLabel", transform, 17.5f, FontStyles.Bold, darkSlate, TextAlignmentOptions.MidlineLeft);
            RectTransform headerRt = headerLabel.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0f, 1f);
            headerRt.anchoredPosition = new Vector2(16f, -12f);
            headerRt.sizeDelta = new Vector2(-150f, 28f);

            GameObject badgeObj = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObj.transform.SetParent(transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-14f, -13f);
            badgeRt.sizeDelta = new Vector2(124f, 26f);
            statusBadgeBg = badgeObj.GetComponent<Image>();
            ApplyRounded(statusBadgeBg);

            statusBadgeLabel = CreateText("BadgeText", badgeObj.transform, 12.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(statusBadgeLabel.GetComponent<RectTransform>(), 6f, 0f);

            BuildIdlePanel();
            BuildStudentJoinPanel();
            BuildConnectedStudentPanel();
            BuildActiveHostPanel();

            RefreshUI();
        }

        private void BuildIdlePanel()
        {
            idleButtonsPanel = CreateContainer("IdleButtonsPanel", transform);

            Button btnTeacher = CreateStyledButton("Btn-OpenTeacherDashboard", idleButtonsPanel.transform, primaryBlue, Color.white, out btnHostLabel);
            PositionTopRow(btnTeacher.GetComponent<RectTransform>(), -50f, 44f);
            btnTeacher.onClick.AddListener(OnOpenTeacherDashboardClicked);

            Button btnJoin = CreateStyledButton("Btn-ToggleJoinStudent", idleButtonsPanel.transform, Color.white, darkSlate, out btnJoinToggleLabel);
            PositionTopRow(btnJoin.GetComponent<RectTransform>(), -102f, 44f);
            btnJoin.onClick.AddListener(OnToggleJoinPanelClicked);
        }

        private void BuildStudentJoinPanel()
        {
            studentJoinPanel = CreateContainer("StudentJoinPanel", transform);

            discoveryStatusLabel = CreateText("DiscoveryStatus", studentJoinPanel.transform, 13f, FontStyles.Bold, mutedSlate, TextAlignmentOptions.MidlineLeft);
            PositionTopRow(discoveryStatusLabel.GetComponent<RectTransform>(), -154f, 24f);

            GameObject hostsListObj = new GameObject("DiscoveredHostsContainer", typeof(RectTransform));
            hostsListObj.transform.SetParent(studentJoinPanel.transform, false);
            discoveredHostsContainer = hostsListObj.transform;
            PositionTopRow(hostsListObj.GetComponent<RectTransform>(), -180f, 84f);

            // Manual IP Row
            GameObject manualRow = new GameObject("ManualIpRow", typeof(RectTransform));
            manualRow.transform.SetParent(studentJoinPanel.transform, false);
            RectTransform manualRt = manualRow.GetComponent<RectTransform>();
            PositionTopRow(manualRt, -270f, 40f);

            GameObject modalPrefab = Resources.Load<GameObject>("InputModalDialog");
#if UNITY_EDITOR
            if (modalPrefab == null)
            {
                modalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/InputModalDialog.prefab");
            }
#endif
            TMP_InputField prefabInput = modalPrefab != null ? modalPrefab.GetComponentInChildren<TMP_InputField>(true) : null;

            if (prefabInput != null)
            {
                ipInputField = Instantiate(prefabInput, manualRow.transform);
                ipInputField.name = "HostIpInputField";
                RectTransform inputRt = ipInputField.GetComponent<RectTransform>();
                inputRt.anchorMin = new Vector2(0f, 0f);
                inputRt.anchorMax = new Vector2(1f, 1f);
                inputRt.offsetMin = Vector2.zero;
                inputRt.offsetMax = new Vector2(-132f, 0f);

                Image bg = ipInputField.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = Color.white;
                    ApplyRounded(bg);
                }

                ipInputField.characterLimit = 32;
                ipInputField.contentType = TMP_InputField.ContentType.Standard;
                ipInputField.SetTextWithoutNotify(manualIpBuffer);
                if (ipInputField.textComponent != null)
                {
                    if (menuFont != null) ipInputField.textComponent.font = menuFont;
                    ipInputField.textComponent.fontSize = 15f;
                    ipInputField.textComponent.color = darkSlate;
                    ipInputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                }
                if (ipInputField.placeholder is TMP_Text ph)
                {
                    ph.text = "Host IP (e.g. 192.168.1.20)";
                    ph.fontSize = 13.5f;
                }
                ipInputField.onValueChanged.RemoveAllListeners();
                ipInputField.onValueChanged.AddListener(val => manualIpBuffer = val);
                ipInputField.onSubmit.RemoveAllListeners();
                ipInputField.onSubmit.AddListener(_ => OnConnectManualIpClicked());
            }
            else
            {
                Button fallbackBtn = CreateStyledButton("FallbackIpBox", manualRow.transform, Color.white, darkSlate, out fallbackIpText);
                RectTransform fbRt = fallbackBtn.GetComponent<RectTransform>();
                fbRt.anchorMin = Vector2.zero;
                fbRt.anchorMax = Vector2.one;
                fbRt.offsetMin = Vector2.zero;
                fbRt.offsetMax = new Vector2(-132f, 0f);
                fallbackIpText.text = manualIpBuffer;
                fallbackBtn.onClick.AddListener(() => fallbackIpFocused = true);
            }

            Button btnConnect = CreateStyledButton("Btn-ConnectIp", manualRow.transform, emeraldGreen, Color.white, out btnConnectManualLabel);
            RectTransform connRt = btnConnect.GetComponent<RectTransform>();
            connRt.anchorMin = new Vector2(1f, 0f);
            connRt.anchorMax = new Vector2(1f, 1f);
            connRt.pivot = new Vector2(1f, 0.5f);
            connRt.anchoredPosition = Vector2.zero;
            connRt.sizeDelta = new Vector2(124f, 0f);
            btnConnect.onClick.AddListener(OnConnectManualIpClicked);

            clientErrorLabel = CreateText("ClientStatusText", studentJoinPanel.transform, 12.5f, FontStyles.Normal, mutedSlate, TextAlignmentOptions.MidlineLeft);
            PositionTopRow(clientErrorLabel.GetComponent<RectTransform>(), -316f, 24f);
        }

        private void BuildConnectedStudentPanel()
        {
            connectedStudentPanel = CreateContainer("ConnectedStudentPanel", transform);

            connectedInfoLabel = CreateText("ConnectedInfo", connectedStudentPanel.transform, 14f, FontStyles.Normal, darkSlate, TextAlignmentOptions.TopLeft);
            connectedInfoLabel.textWrappingMode = TextWrappingModes.Normal;
            PositionTopRow(connectedInfoLabel.GetComponent<RectTransform>(), -48f, 54f);

            Button btnDisc = CreateStyledButton("Btn-DisconnectHost", connectedStudentPanel.transform, dangerRed, Color.white, out btnDisconnectLabel);
            PositionTopRow(btnDisc.GetComponent<RectTransform>(), -108f, 40f);
            btnDisc.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionClient.Instance.Disconnect();
                RefreshUI();
            });
        }

        private void BuildActiveHostPanel()
        {
            activeHostPanel = CreateContainer("ActiveHostPanel", transform);

            activeHostInfoLabel = CreateText("ActiveHostInfo", activeHostPanel.transform, 14f, FontStyles.Normal, darkSlate, TextAlignmentOptions.MidlineLeft);
            PositionTopRow(activeHostInfoLabel.GetComponent<RectTransform>(), -48f, 32f);

            Button btnDash = CreateStyledButton("Btn-ReopenDashboard", activeHostPanel.transform, primaryBlue, Color.white, out btnOpenDashboardLabel);
            RectTransform dashRt = btnDash.GetComponent<RectTransform>();
            dashRt.anchorMin = new Vector2(0f, 1f);
            dashRt.anchorMax = new Vector2(1f, 1f);
            dashRt.pivot = new Vector2(0f, 1f);
            dashRt.anchoredPosition = new Vector2(14f, -86f);
            dashRt.sizeDelta = new Vector2(-156f, 42f);
            btnDash.onClick.AddListener(OnOpenTeacherDashboardClicked);

            Button btnStop = CreateStyledButton("Btn-StopHost", activeHostPanel.transform, dangerRed, Color.white, out btnStopHostLabel);
            RectTransform stopRt = btnStop.GetComponent<RectTransform>();
            stopRt.anchorMin = new Vector2(1f, 1f);
            stopRt.anchorMax = new Vector2(1f, 1f);
            stopRt.pivot = new Vector2(1f, 1f);
            stopRt.anchoredPosition = new Vector2(-14f, -86f);
            stopRt.sizeDelta = new Vector2(120f, 42f);
            btnStop.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                NetworkSessionHost.Instance.StopHost();
                RefreshUI();
            });
        }

        private void OnOpenTeacherDashboardClicked()
        {
            FeedbackManager.PlayClick();
            isJoinExpanded = false;
            LanDiscovery.Instance.StopListening();

            if (!NetworkSessionHost.Instance.IsHosting)
            {
                NetworkSessionHost.Instance.StartHost();
            }

            TeacherDashboardController.OpenOnCanvas(canvasRoot);
            RefreshUI();
        }

        private void OnToggleJoinPanelClicked()
        {
            FeedbackManager.PlayClick();
            isJoinExpanded = !isJoinExpanded;

            if (isJoinExpanded)
            {
                LanDiscovery.Instance.StartListening();
            }
            else
            {
                LanDiscovery.Instance.StopListening();
            }

            RefreshUI();
        }

        private void OnConnectManualIpClicked()
        {
            FeedbackManager.PlayClick();
            string ip = ipInputField != null ? ipInputField.text : manualIpBuffer;
            NetworkSessionClient.Instance.ConnectToHost(ip, NetworkProtocol.DefaultSessionPort);
            RefreshUI();
        }

        public void RefreshUI()
        {
            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);
            bool isHosting = NetworkSessionHost.ExistingInstance != null && NetworkSessionHost.ExistingInstance.IsHosting;
            bool isClientConnected = NetworkSessionClient.ExistingInstance != null && NetworkSessionClient.ExistingInstance.IsConnected;
            bool isClientConnecting = NetworkSessionClient.ExistingInstance != null && NetworkSessionClient.ExistingInstance.IsConnecting;

            if (headerLabel != null)
            {
                headerLabel.text = isEs ? "Aula LAN (Profesor / Alumno)" : "Classroom LAN";
            }

            if (isHosting)
            {
                int count = NetworkSessionHost.Instance.ActiveConnectedCount;
                statusBadgeBg.color = emeraldGreen;
                statusBadgeLabel.text = isEs ? $"HOST ({count})" : $"HOSTING ({count})";

                idleButtonsPanel.SetActive(false);
                studentJoinPanel.SetActive(false);
                connectedStudentPanel.SetActive(false);
                activeHostPanel.SetActive(true);

                cardRect.sizeDelta = new Vector2(440f, 142f);

                activeHostInfoLabel.text = isEs
                    ? $"IP Profesor: <b>{NetworkSessionHost.Instance.LocalIpAddress}:{NetworkSessionHost.Instance.ListenPort}</b>  •  Alumnos: <b>{count}</b>"
                    : $"Host IP: <b>{NetworkSessionHost.Instance.LocalIpAddress}:{NetworkSessionHost.Instance.ListenPort}</b>  •  Students: <b>{count}</b>";

                btnOpenDashboardLabel.text = isEs ? "Abrir Panel del Profesor" : "Open Teacher Dashboard";
                btnStopHostLabel.text = isEs ? "Detener" : "Stop Host";
                return;
            }

            if (isClientConnected)
            {
                statusBadgeBg.color = emeraldGreen;
                statusBadgeLabel.text = isEs ? "CONECTADO" : "CONNECTED";

                idleButtonsPanel.SetActive(false);
                studentJoinPanel.SetActive(false);
                activeHostPanel.SetActive(false);
                connectedStudentPanel.SetActive(true);

                cardRect.sizeDelta = new Vector2(440f, 162f);

                StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;
                string pathName = StudentConfiguration.GetPathDisplayName(cfg.learningPath);
                string assessTxt = cfg.runAssessment ? (isEs ? "Assessment: SÍ" : "Assessment: ON") : (isEs ? "Assessment: NO" : "Assessment: OFF");

                connectedInfoLabel.text = isEs
                    ? $"Profesor: <b>{NetworkSessionClient.Instance.ConnectedHostName}</b> ({NetworkSessionClient.Instance.ConnectedHostIp})\nAlumno <b>{cfg.studentId}</b>  •  {pathName}  •  {assessTxt}"
                    : $"Teacher: <b>{NetworkSessionClient.Instance.ConnectedHostName}</b> ({NetworkSessionClient.Instance.ConnectedHostIp})\nStudent <b>{cfg.studentId}</b>  •  {pathName}  •  {assessTxt}";

                btnDisconnectLabel.text = isEs ? "Desconectar del Aula" : "Disconnect from Classroom";
                return;
            }

            // Offline / Join state
            statusBadgeBg.color = isClientConnecting ? primaryBlue : mutedSlate;
            statusBadgeLabel.text = isClientConnecting
                ? (isEs ? "CONECTANDO" : "CONNECTING")
                : (isEs ? "MODO LOCAL" : "LOCAL MODE");

            activeHostPanel.SetActive(false);
            connectedStudentPanel.SetActive(false);
            idleButtonsPanel.SetActive(true);
            studentJoinPanel.SetActive(isJoinExpanded);

            btnHostLabel.text = isEs ? "Panel del Profesor (Crear Aula LAN)" : "Teacher Dashboard (Host LAN)";
            btnJoinToggleLabel.text = isJoinExpanded
                ? (isEs ? "Ocultar búsqueda de Aula LAN ▲" : "Hide Student LAN Join ▲")
                : (isEs ? "Unirse a Aula como Alumno (LAN) ▼" : "Join Classroom as Student (LAN) ▼");

            if (isJoinExpanded)
            {
                cardRect.sizeDelta = new Vector2(440f, 350f);
                btnConnectManualLabel.text = isEs ? "Conectar IP" : "Connect IP";
                clientErrorLabel.text = NetworkSessionClient.Instance.LastStatusMessage;
                RebuildDiscoveredHostsList(LanDiscovery.Instance.DiscoveredHosts);
            }
            else
            {
                cardRect.sizeDelta = new Vector2(440f, 158f);
            }
        }

        private void RebuildDiscoveredHostsList(IReadOnlyList<DiscoveredHostInfo> hosts)
        {
            if (discoveredHostsContainer == null || !isJoinExpanded) return;

            for (int i = discoveredHostsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(discoveredHostsContainer.GetChild(i).gameObject);
            }

            bool isEs = string.Equals(LocalizationManager.CurrentLanguage, "ES", StringComparison.OrdinalIgnoreCase);

            if (hosts == null || hosts.Count == 0)
            {
                discoveryStatusLabel.text = isEs
                    ? "Buscando profesores en la red local (UDP)..."
                    : "Scanning LAN for Teacher Hosts (UDP)...";

                TextMeshProUGUI emptyHint = CreateText("EmptyHint", discoveredHostsContainer, 12.5f, FontStyles.Italic, mutedSlate, TextAlignmentOptions.MidlineLeft);
                StretchFull(emptyHint.GetComponent<RectTransform>(), 4f, 0f);
                emptyHint.text = isEs
                    ? "Ningún host detectado aún. También puedes escribir la IP abajo:"
                    : "No host broadcast seen yet. Or enter Teacher IP manually below:";
                return;
            }

            discoveryStatusLabel.text = isEs
                ? $"Profesores encontrados en LAN ({hosts.Count}):"
                : $"Discovered Teacher Hosts ({hosts.Count}):";

            int maxShown = Mathf.Min(2, hosts.Count);
            for (int i = 0; i < maxShown; i++)
            {
                DiscoveredHostInfo info = hosts[i];
                Button hostBtn = CreateStyledButton($"Btn-Host-{i}", discoveredHostsContainer, primaryBlue, Color.white, out TextMeshProUGUI lbl);
                RectTransform rt = hostBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 42f);
                rt.sizeDelta = new Vector2(0f, 38f);

                lbl.fontSize = 13.5f;
                lbl.text = isEs
                    ? $"Conectar: {info.hostName}  ({info.hostIp})"
                    : $"Join: {info.hostName}  ({info.hostIp})";

                string capturedIp = info.hostIp;
                int capturedPort = info.tcpPort;
                hostBtn.onClick.AddListener(() =>
                {
                    FeedbackManager.PlayClick();
                    if (ipInputField != null) ipInputField.SetTextWithoutNotify(capturedIp);
                    manualIpBuffer = capturedIp;
                    NetworkSessionClient.Instance.ConnectToHost(capturedIp, capturedPort);
                    RefreshUI();
                });
            }
        }

        private void PollFallbackIpKeyboard()
        {
            if (Keyboard.current.backspaceKey.wasPressedThisFrame && manualIpBuffer.Length > 0)
            {
                manualIpBuffer = manualIpBuffer.Substring(0, manualIpBuffer.Length - 1);
                if (fallbackIpText != null) fallbackIpText.text = manualIpBuffer;
                return;
            }
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                fallbackIpFocused = false;
                OnConnectManualIpClicked();
                return;
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                fallbackIpFocused = false;
                return;
            }
            foreach (var key in Keyboard.current.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame) continue;
                string k = key.name;
                if (k == "period" || k == "numpadPeriod")
                {
                    manualIpBuffer += ".";
                    if (fallbackIpText != null) fallbackIpText.text = manualIpBuffer;
                }
                else if (k.Length == 1 && char.IsDigit(k[0]) && manualIpBuffer.Length < 21)
                {
                    manualIpBuffer += k[0];
                    if (fallbackIpText != null) fallbackIpText.text = manualIpBuffer;
                }
            }
        }

        private GameObject CreateContainer(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchFull(go.GetComponent<RectTransform>(), 0f, 0f);
            return go;
        }

        private Button CreateStyledButton(string name, Transform parent, Color bgColor, Color textColor, out TextMeshProUGUI label)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image bg = btnObj.GetComponent<Image>();
            bg.color = bgColor;
            ApplyRounded(bg);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.93f, 0.96f, 1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.89f, 0.96f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            label = CreateText("Label", btnObj.transform, 14.5f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            StretchFull(label.GetComponent<RectTransform>(), 10f, 0f);
            return btn;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (menuFont != null) tmp.font = menuFont;
            tmp.fontSize = fontSize;
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

        private static void PositionTopRow(RectTransform rt, float yOffset, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            rt.sizeDelta = new Vector2(-28f, height);
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
