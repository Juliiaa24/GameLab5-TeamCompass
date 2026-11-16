/**
 * Author: TeamCompass
 * Description: Student LAN Client (DontDestroyOnLoad). Connects to a Teacher Host via
 *              concurrent Direct-TCP (47778), Reverse-TCP callback listener (47779), and
 *              bidirectional UDP session envelopes over discovery port (47777), receives
 *              StudentConfiguration JSON payloads, applies them strictly through SessionManager,
 *              and streams live ProgressData + SkillManager telemetry back to the Teacher Dashboard.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ComputerLearning
{
    public class NetworkSessionClient : MonoBehaviour
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private const string LastHostIpPrefKey = "CL_LastTeacherHostIp";

        private static NetworkSessionClient instance;

        public static NetworkSessionClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<NetworkSessionClient>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("NetworkSessionClient");
                        instance = go.AddComponent<NetworkSessionClient>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public static NetworkSessionClient ExistingInstance => instance;

        public string ClientUid { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        public string ConnectedHostIp { get; private set; } = string.Empty;
        public int ConnectedHostPort { get; private set; } = NetworkProtocol.DefaultSessionPort;
        public string ConnectedHostName { get; private set; } = string.Empty;
        public string LastStatusMessage { get; private set; } = string.Empty;
        public bool IsStudentNameLocked { get; private set; }
        public bool IsLeaveClassLocked { get; private set; }

        public event Action OnClientStateChanged;

        private TcpClient tcpClient;
        private StreamWriter streamWriter;
        private TcpListener reverseTcpListener;
        private int activeCallbackPort = NetworkProtocol.DefaultClientCallbackPort;
        private readonly object writeLock = new object();
        private Thread readThread;
        private volatile bool stopRequested;
        private int currentAttemptId;

        private readonly List<string> activeCandidateIps = new List<string>();
        private int activeHostUdpPort = NetworkProtocol.DiscoveryPort;
        private string pendingHelloJson = string.Empty;
        private float nextUdpHelloRetryTime;
        private float connectDeadlineRealtime;
        private float lastHostActivityRealtime;

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly HashSet<string> processedPacketIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> processedPacketOrder = new Queue<string>();

        private ProgressData subscribedProgressData;
        private float nextTelemetryHeartbeatTime;
        private string lastCompletedLevelIdTracker = string.Empty;

        public string SavedLastHostIp => PlayerPrefs.GetString(LastHostIpPrefKey, "127.0.0.1");

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.runInBackground = true;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            ClientUid = Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PlaytestDataManager.OnStudentIdChanged += HandleStudentIdChanged;
            SessionManager.Instance.OnConfigurationChanged += HandleConfigurationChanged;
            if (LanDiscovery.Instance != null)
            {
                LanDiscovery.Instance.OnUdpEnvelopeReceived -= HandleUdpEnvelopeReceived;
                LanDiscovery.Instance.OnUdpEnvelopeReceived += HandleUdpEnvelopeReceived;
            }
            RefreshProgressDataSubscription();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            PlaytestDataManager.OnStudentIdChanged -= HandleStudentIdChanged;
            if (SessionManager.ExistingInstance != null)
            {
                SessionManager.ExistingInstance.OnConfigurationChanged -= HandleConfigurationChanged;
            }
            if (LanDiscovery.ExistingInstance != null)
            {
                LanDiscovery.ExistingInstance.OnUdpEnvelopeReceived -= HandleUdpEnvelopeReceived;
            }
            UnsubscribeProgressData();
        }

        private void Update()
        {
            while (mainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NetworkSessionClient] Main-thread handler warning: {e.Message}");
                }
            }

            if (IsConnecting)
            {
                if (Time.realtimeSinceStartup >= connectDeadlineRealtime)
                {
                    Disconnect(silent: true);
                    LastStatusMessage = "Could not connect to the classroom. Please try again.";
                    OnClientStateChanged?.Invoke();
                }
                else if (Time.unscaledTime >= nextUdpHelloRetryTime)
                {
                    nextUdpHelloRetryTime = Time.unscaledTime + 0.45f;
                    SendUdpHelloPulse();
                }
            }

            if (IsConnected)
            {
                if (subscribedProgressData != ProgressData.ExistingInstance)
                {
                    RefreshProgressDataSubscription();
                }

                bool hasTcp;
                lock (writeLock)
                {
                    hasTcp = tcpClient != null && tcpClient.Connected;
                }

                if (!hasTcp && Time.realtimeSinceStartup - lastHostActivityRealtime > 12.0f)
                {
                    Disconnect(silent: true);
                    LastStatusMessage = "Connection closed by Teacher Host.";
                    OnClientStateChanged?.Invoke();
                    return;
                }

                if (Time.unscaledTime >= nextTelemetryHeartbeatTime)
                {
                    nextTelemetryHeartbeatTime = Time.unscaledTime + 2.0f;
                    SendTelemetryNow();
                }
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        private void OnDestroy()
        {
            Disconnect();
            if (instance == this)
            {
                instance = null;
            }
        }

        public void ConnectToHost(DiscoveredHostInfo host)
        {
            if (host == null) return;
            ConnectToHost(host.hostIp, host.tcpPort, host.hostName, host.candidateIps, host.lastSenderUdpPort);
        }

        public void ConnectToHost(
            string hostIp,
            int port = NetworkProtocol.DefaultSessionPort,
            string hostDisplayName = null,
            IReadOnlyList<string> candidateIps = null,
            int hostUdpPort = 0)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                LastStatusMessage = "LAN Client requires Standalone (Windows/Mac/Linux) or Unity Editor.";
                OnClientStateChanged?.Invoke();
                return;
            }

            if (IsConnecting) return;

            string trimmedIp = string.IsNullOrWhiteSpace(hostIp) ? "127.0.0.1" : hostIp.Trim();
            if (NetworkSessionHost.ExistingInstance != null && NetworkSessionHost.ExistingInstance.IsHosting)
            {
                LastStatusMessage = "Stop local Teacher Host before joining as a Student Client on the same instance.";
                OnClientStateChanged?.Invoke();
                return;
            }

            Disconnect(silent: true);

            PlayerPrefs.SetString(LastHostIpPrefKey, trimmedIp);
            PlayerPrefs.Save();

            int attemptId = ++currentAttemptId;
            IsConnecting = true;
            ConnectedHostIp = trimmedIp;
            ConnectedHostPort = port;
            ConnectedHostName = !string.IsNullOrWhiteSpace(hostDisplayName) ? hostDisplayName.Trim() : string.Empty;
            LastStatusMessage = UIDialogs.Current.lan_join_connecting;
            lastHostActivityRealtime = Time.realtimeSinceStartup;
            connectDeadlineRealtime = Time.realtimeSinceStartup + 5.5f;
            nextUdpHelloRetryTime = 0f;

            lock (writeLock)
            {
                activeCandidateIps.Clear();
                if (candidateIps != null)
                {
                    for (int i = 0; i < candidateIps.Count; i++)
                    {
                        string ip = candidateIps[i];
                        if (!string.IsNullOrWhiteSpace(ip) && !activeCandidateIps.Contains(ip.Trim()))
                        {
                            activeCandidateIps.Add(ip.Trim());
                        }
                    }
                }
                if (!activeCandidateIps.Contains(trimmedIp))
                {
                    activeCandidateIps.Add(trimmedIp);
                }
                activeHostUdpPort = hostUdpPort > 0 ? hostUdpPort : NetworkProtocol.DiscoveryPort;
            }

            // Capture handshake fields on the Unity main thread before opening sockets
            PlaytestDataManager.EnsureInitialized();
            StudentConfiguration currentCfg = SessionManager.Instance.CurrentConfiguration;
            ClientHelloPayload hello = new ClientHelloPayload
            {
                protocol = NetworkProtocol.ProtocolId,
                clientUid = ClientUid,
                studentId = PlaytestDataManager.CurrentStudentId,
                deviceName = LanDiscovery.GetDefaultDeviceName(),
                activeScene = SceneManager.GetActiveScene().name,
                currentPath = currentCfg != null ? currentCfg.learningPath : LearningPath.BeginnerFoundations,
                runAssessment = currentCfg == null || currentCfg.runAssessment
            };
            pendingHelloJson = JsonUtility.ToJson(hello);

            stopRequested = false;

            StartReverseTcpListener(attemptId);

            LanDiscovery discovery = LanDiscovery.Instance;
            if (discovery != null)
            {
                discovery.LocalClientUid = ClientUid;
                discovery.LocalSenderUid = ClientUid;
                discovery.LocalCallbackTcpPort = activeCallbackPort;
                discovery.OnUdpEnvelopeReceived -= HandleUdpEnvelopeReceived;
                discovery.OnUdpEnvelopeReceived += HandleUdpEnvelopeReceived;
                discovery.StartListening(NetworkProtocol.DiscoveryPort);
            }

            OnClientStateChanged?.Invoke();

            // Immediately fire first UDP Hello and start parallel Direct-TCP attempts
            SendUdpHelloPulse();
            nextUdpHelloRetryTime = Time.unscaledTime + 0.45f;

            List<string> ipsSnapshot;
            lock (writeLock)
            {
                ipsSnapshot = new List<string>(activeCandidateIps);
            }

            Task.Run(() => TryDirectTcpCandidatesAsync(attemptId, ipsSnapshot, port, pendingHelloJson));
        }

        private void StartReverseTcpListener(int attemptId)
        {
            StopReverseTcpListener();

            int[] candidatePorts =
            {
                NetworkProtocol.DefaultClientCallbackPort,
                NetworkProtocol.DefaultClientCallbackPort + 1
            };

            for (int i = 0; i < candidatePorts.Length; i++)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, candidatePorts[i]);
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    listener.Start();
                    reverseTcpListener = listener;
                    activeCallbackPort = candidatePorts[i];
                    break;
                }
                catch
                {
                    // Try next port
                }
            }

            if (reverseTcpListener == null) return;

            TcpListener activeListener = reverseTcpListener;
            Task.Run(() =>
            {
                try
                {
                    while (!stopRequested && attemptId == currentAttemptId && activeListener != null)
                    {
                        if (!activeListener.Pending())
                        {
                            Thread.Sleep(25);
                            continue;
                        }

                        TcpClient accepted = activeListener.AcceptTcpClient();
                        if (stopRequested || attemptId != currentAttemptId)
                        {
                            try { accepted.Close(); } catch { }
                            break;
                        }

                        AttachTcpConnection(attemptId, accepted, pendingHelloJson);
                        break;
                    }
                }
                catch
                {
                    // Listener closed
                }
            });
        }

        private void StopReverseTcpListener()
        {
            if (reverseTcpListener != null)
            {
                try { reverseTcpListener.Stop(); } catch { }
                reverseTcpListener = null;
            }
        }

        private void TryDirectTcpCandidatesAsync(int attemptId, List<string> candidateIps, int port, string helloJson)
        {
            if (candidateIps == null || candidateIps.Count == 0) return;

            List<Task<TcpClient>> connectTasks = new List<Task<TcpClient>>(candidateIps.Count);
            for (int i = 0; i < candidateIps.Count; i++)
            {
                string ip = candidateIps[i];
                connectTasks.Add(Task.Run(() =>
                {
                    TcpClient client = new TcpClient();
                    try
                    {
                        IAsyncResult ar = client.BeginConnect(ip, port, null, null);
                        bool signaled = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2.8));
                        if (!signaled || !client.Connected || stopRequested || attemptId != currentAttemptId)
                        {
                            try { client.Close(); } catch { }
                            return null;
                        }
                        client.EndConnect(ar);
                        return client;
                    }
                    catch
                    {
                        try { client.Close(); } catch { }
                        return null;
                    }
                }));
            }

            try
            {
                while (connectTasks.Count > 0 && !stopRequested && attemptId == currentAttemptId)
                {
                    int idx = Task.WaitAny(connectTasks.ToArray());
                    if (idx < 0 || idx >= connectTasks.Count) break;

                    Task<TcpClient> finished = connectTasks[idx];
                    connectTasks.RemoveAt(idx);

                    TcpClient connectedClient = finished.Status == TaskStatus.RanToCompletion ? finished.Result : null;
                    if (connectedClient != null)
                    {
                        if (!AttachTcpConnection(attemptId, connectedClient, helloJson))
                        {
                            try { connectedClient.Close(); } catch { }
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        private bool AttachTcpConnection(int attemptId, TcpClient client, string helloJson)
        {
            if (client == null || stopRequested || attemptId != currentAttemptId) return false;

            try
            {
                client.NoDelay = true;
                NetworkStream netStream = client.GetStream();
                StreamWriter writer = new StreamWriter(netStream, Utf8NoBom) { AutoFlush = true };

                lock (writeLock)
                {
                    if (stopRequested || attemptId != currentAttemptId || tcpClient != null)
                    {
                        return false;
                    }
                    tcpClient = client;
                    streamWriter = writer;
                }

                StopReverseTcpListener();

                string helloPacketId = Guid.NewGuid().ToString("N").Substring(0, 12);
                SendTcpEnvelopeOnly(NetworkProtocol.MsgHello, helloJson, helloPacketId);

                mainThreadQueue.Enqueue(() =>
                {
                    if (stopRequested || attemptId != currentAttemptId) return;
                    lastHostActivityRealtime = Time.realtimeSinceStartup;
                    bool stateChanged = !IsConnected || IsConnecting;
                    IsConnecting = false;
                    IsConnected = true;
                    if (!string.IsNullOrEmpty(ConnectedHostName))
                    {
                        LastStatusMessage = string.Format(UIDialogs.Current.lan_connected_banner_format, ConnectedHostName);
                    }
                    RefreshProgressDataSubscription();
                    SendTelemetryNow();
                    if (stateChanged)
                    {
                        OnClientStateChanged?.Invoke();
                    }
                });

                readThread = new Thread(() => ReadLoop(attemptId, netStream))
                {
                    IsBackground = true,
                    Name = "CL_StudentClientReader"
                };
                readThread.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SendUdpHelloPulse()
        {
            if (string.IsNullOrEmpty(pendingHelloJson) || LanDiscovery.ExistingInstance == null) return;

            List<string> ipsCopy;
            int udpPort;
            lock (writeLock)
            {
                ipsCopy = new List<string>(activeCandidateIps);
                udpPort = activeHostUdpPort;
            }

            LanDiscovery.ExistingInstance.SendUdpEnvelopeToHost(ipsCopy, udpPort, new UdpEnvelopePacket
            {
                packetId = Guid.NewGuid().ToString("N").Substring(0, 12),
                senderUid = ClientUid,
                targetUid = "HOST",
                callbackTcpPort = activeCallbackPort,
                type = NetworkProtocol.MsgHello,
                payloadJson = pendingHelloJson
            });
        }

        public void Disconnect(bool silent = false)
        {
            bool wasActive = IsConnected || IsConnecting;
            currentAttemptId++;
            stopRequested = true;
            IsConnecting = false;
            IsConnected = false;
            IsStudentNameLocked = false;
            IsLeaveClassLocked = false;
            pendingHelloJson = string.Empty;

            StopReverseTcpListener();

            lock (writeLock)
            {
                try { streamWriter?.Close(); } catch { }
                try { tcpClient?.Close(); } catch { }
                streamWriter = null;
                tcpClient = null;
            }

            processedPacketIds.Clear();
            processedPacketOrder.Clear();

            if (!silent && wasActive)
            {
                LastStatusMessage = "Disconnected from Teacher Host.";
                OnClientStateChanged?.Invoke();
            }
        }

        public void SendTelemetryNow()
        {
            if (!IsConnected) return;

            StudentTelemetrySnapshot snap = BuildTelemetrySnapshotOnMainThread();
            string json = JsonUtility.ToJson(snap);
            nextTelemetryHeartbeatTime = Time.unscaledTime + 2.0f;
            SendEnvelopeOverTransports(NetworkProtocol.MsgTelemetry, json);
        }

        private void ReadLoop(int attemptId, NetworkStream netStream)
        {
            try
            {
                using (StreamReader reader = new StreamReader(netStream, Utf8NoBom))
                {
                    while (!stopRequested && attemptId == currentAttemptId)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        line = line.Trim('\uFEFF', ' ', '\r', '\n', '\t');
                        if (string.IsNullOrEmpty(line)) continue;

                        NetworkEnvelope env = JsonUtility.FromJson<NetworkEnvelope>(line);
                        if (env == null || string.IsNullOrEmpty(env.type)) continue;

                        if (env.type == NetworkProtocol.MsgPing)
                        {
                            SendTcpEnvelopeOnly(NetworkProtocol.MsgPong, string.Empty, Guid.NewGuid().ToString("N").Substring(0, 12));
                            mainThreadQueue.Enqueue(() => { lastHostActivityRealtime = Time.realtimeSinceStartup; });
                            continue;
                        }
                        if (env.type == NetworkProtocol.MsgPong)
                        {
                            mainThreadQueue.Enqueue(() => { lastHostActivityRealtime = Time.realtimeSinceStartup; });
                            continue;
                        }

                        string msgType = env.type;
                        string payload = env.payloadJson;
                        string packetId = env.packetId;
                        mainThreadQueue.Enqueue(() => HandleEnvelopeOnMainThread(msgType, payload, packetId));
                    }
                }
            }
            catch
            {
                // Socket closed
            }
            finally
            {
                if (!stopRequested && attemptId == currentAttemptId)
                {
                    mainThreadQueue.Enqueue(() =>
                    {
                        if (stopRequested || attemptId != currentAttemptId) return;
                        Disconnect(silent: true);
                        LastStatusMessage = "Connection closed by Teacher Host.";
                        OnClientStateChanged?.Invoke();
                    });
                }
            }
        }

        private void HandleUdpEnvelopeReceived(IPEndPoint remoteEp, UdpEnvelopePacket pkt)
        {
            if (stopRequested || pkt == null) return;
            if (!IsConnecting && !IsConnected) return;
            if (!string.IsNullOrEmpty(pkt.targetUid) && pkt.targetUid != ClientUid) return;

            lastHostActivityRealtime = Time.realtimeSinceStartup;
            if (remoteEp != null && remoteEp.Port > 0)
            {
                lock (writeLock)
                {
                    if (remoteEp.Address != null)
                    {
                        string addr = remoteEp.Address.ToString();
                        if (!string.IsNullOrEmpty(addr) && addr != "0.0.0.0" && !activeCandidateIps.Contains(addr))
                        {
                            activeCandidateIps.Insert(0, addr);
                        }
                    }
                    if (remoteEp.Port != NetworkProtocol.DiscoveryPort)
                    {
                        activeHostUdpPort = remoteEp.Port;
                    }
                }
            }

            if (pkt.type == NetworkProtocol.MsgPing)
            {
                SendEnvelopeOverTransports(NetworkProtocol.MsgPong, string.Empty);
                return;
            }
            if (pkt.type == NetworkProtocol.MsgPong)
            {
                return;
            }

            HandleEnvelopeOnMainThread(pkt.type, pkt.payloadJson, pkt.packetId);
        }

        private void HandleEnvelopeOnMainThread(string msgType, string payloadJson, string packetId)
        {
            if (!MarkPacketProcessed(packetId)) return;
            lastHostActivityRealtime = Time.realtimeSinceStartup;

            switch (msgType)
            {
                case NetworkProtocol.MsgWelcome:
                    HandleHostWelcome(payloadJson);
                    break;

                case NetworkProtocol.MsgAssignConfig:
                    HandleAssignConfig(payloadJson);
                    break;

                case NetworkProtocol.MsgClassroomPermissions:
                    HandleClassroomPermissions(payloadJson);
                    break;

                case NetworkProtocol.MsgStartSession:
                    HandleStartSession();
                    break;

                case NetworkProtocol.MsgResetStudent:
                    HandleResetStudent(payloadJson);
                    break;
            }
        }

        private void HandleHostWelcome(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;
            HostWelcomePayload welcome = JsonUtility.FromJson<HostWelcomePayload>(payloadJson);
            if (welcome == null) return;

            IsConnecting = false;
            IsConnected = true;

            if (!string.IsNullOrWhiteSpace(welcome.hostName))
            {
                ConnectedHostName = welcome.hostName.Trim();
            }
            IsStudentNameLocked = welcome.lockStudentNames;
            IsLeaveClassLocked = welcome.lockLeaveClass;

            if (welcome.assignedConfiguration != null)
            {
                // Only reset progress automatically if student is still sitting idle on Menu with no recorded activity
                bool isIdleOnMenu = SceneManager.GetActiveScene().name == "Menu" &&
                                    !PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent();

                SessionManager.Instance.ApplyConfiguration(welcome.assignedConfiguration, resetExistingProgress: isIdleOnMenu);
            }

            string displayHost = !string.IsNullOrWhiteSpace(ConnectedHostName) ? ConnectedHostName : "Teacher";
            LastStatusMessage = string.Format(UIDialogs.Current.lan_connected_banner_format, displayHost);
            RefreshProgressDataSubscription();
            OnClientStateChanged?.Invoke();
            SendTelemetryNow();

            if (PlaytestStudentWidget.Instance != null)
            {
                PlaytestStudentWidget.Instance.RefreshUI();
                PlaytestStudentWidget.Instance.ShowToast(LastStatusMessage, 3.0f);
            }
        }

        private void HandleAssignConfig(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;
            AssignConfigPayload assign = JsonUtility.FromJson<AssignConfigPayload>(payloadJson);
            if (assign == null || assign.configuration == null) return;

            IsStudentNameLocked = assign.lockStudentNames;
            IsLeaveClassLocked = assign.lockLeaveClass;

            SessionManager.Instance.ApplyConfiguration(assign.configuration, resetExistingProgress: assign.resetProgress);
            SendTelemetryNow();
            OnClientStateChanged?.Invoke();

            if (PlaytestStudentWidget.Instance != null)
            {
                PlaytestStudentWidget.Instance.RefreshUI();
                PlaytestStudentWidget.Instance.ShowToast(
                    $"Teacher set: {StudentConfiguration.GetPathDisplayName(assign.configuration.learningPath)} ({(assign.configuration.runAssessment ? "Assessment ON" : "Assessment OFF")})",
                    3.2f);
            }

            if (assign.startImmediately)
            {
                SessionManager.Instance.StartOrContinueSession();
            }
        }

        private void HandleClassroomPermissions(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;
            ClassroomPermissionsPayload perms = JsonUtility.FromJson<ClassroomPermissionsPayload>(payloadJson);
            if (perms == null) return;

            IsStudentNameLocked = perms.lockStudentNames;
            IsLeaveClassLocked = perms.lockLeaveClass;
            if (!string.IsNullOrWhiteSpace(perms.className))
            {
                ConnectedHostName = perms.className.Trim();
            }

            OnClientStateChanged?.Invoke();
            if (PlaytestStudentWidget.Instance != null)
            {
                PlaytestStudentWidget.Instance.RefreshUI();
            }
        }

        private void HandleStartSession()
        {
            SessionManager.Instance.StartOrContinueSession();
        }

        private void HandleResetStudent(string payloadJson)
        {
            ResetStudentPayload reset = !string.IsNullOrWhiteSpace(payloadJson)
                ? JsonUtility.FromJson<ResetStudentPayload>(payloadJson)
                : null;

            if (PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlaytestDataManager.ExportCurrentStudentData("teacher_remote_reset");
            }

            if (reset != null && reset.configuration != null)
            {
                SessionManager.Instance.ApplyConfiguration(reset.configuration, resetExistingProgress: true);
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                SceneManager.LoadScene("Menu");
            }
            else
            {
                PlaytestStudentWidget.Instance?.RefreshUI();
                LearningPathSelectorWidget.Instance?.RefreshUI();
            }

            SendTelemetryNow();
        }

        private StudentTelemetrySnapshot BuildTelemetrySnapshotOnMainThread()
        {
            PlaytestDataManager.EnsureInitialized();
            StudentConfiguration cfg = SessionManager.Instance.CurrentConfiguration;

            ProgressData.GetSavedDataSnapshot(
                out IReadOnlyList<TaskResult> tasks,
                out List<string> completedLevels,
                out List<LevelScoreRecord> levelRecords,
                out _);

            int gold = 0, silver = 0, bronze = 0;
            string lastMedal = "None";

            if (levelRecords != null)
            {
                for (int i = 0; i < levelRecords.Count; i++)
                {
                    LevelScoreRecord rec = levelRecords[i];
                    if (rec == null) continue;
                    switch (rec.bestMedal)
                    {
                        case LevelMedal.Gold: gold++; break;
                        case LevelMedal.Silver: silver++; break;
                        case LevelMedal.Bronze: bronze++; break;
                    }
                }
            }

            string lastCompletedId = lastCompletedLevelIdTracker;
            if (string.IsNullOrEmpty(lastCompletedId) && completedLevels != null && completedLevels.Count > 0)
            {
                lastCompletedId = completedLevels[completedLevels.Count - 1];
            }

            if (!string.IsNullOrEmpty(lastCompletedId) && ProgressData.ExistingInstance != null)
            {
                lastMedal = ProgressData.ExistingInstance.GetBestMedal(lastCompletedId).ToString();
            }

            string nextLevelId = ProgressData.ExistingInstance != null
                ? (ProgressData.ExistingInstance.GetCurrentNextLevelId() ?? "Completed")
                : ComputeNextLevelFromList(completedLevels);

            int tasksAttempted = tasks != null ? tasks.Count : 0;
            int tasksSucceeded = 0;
            if (tasks != null)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i] != null && tasks[i].success) tasksSucceeded++;
                }
            }

            List<SkillScoreEntry> skillScores = new List<SkillScoreEntry>(SkillProfilePreset.AllKnownSkillIds.Length);
            float skillSum = 0f;
            for (int i = 0; i < SkillProfilePreset.AllKnownSkillIds.Length; i++)
            {
                string skillId = SkillProfilePreset.AllKnownSkillIds[i];
                float score = SkillManager.Instance != null && SkillManager.Instance.HasSkill(skillId)
                    ? SkillManager.Instance.GetScore(skillId)
                    : PlayerPrefs.GetFloat($"SkillScore_{skillId}", 0f);

                skillSum += score;
                skillScores.Add(new SkillScoreEntry(skillId, (float)Math.Round(score, 1)));
            }

            float avgSkills = SkillProfilePreset.AllKnownSkillIds.Length > 0
                ? (float)Math.Round(skillSum / SkillProfilePreset.AllKnownSkillIds.Length, 1)
                : 0f;

            bool assessDone = PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 1 &&
                              PlayerPrefs.GetInt("Session_PendingAssessment", 0) == 0;
            float assessAvg = (float)Math.Round(PlayerPrefs.GetFloat("InitialAssessmentAverageScore", 0f), 1);

            return new StudentTelemetrySnapshot
            {
                clientUid = ClientUid,
                studentId = PlaytestDataManager.CurrentStudentId,
                deviceName = LanDiscovery.GetDefaultDeviceName(),
                learningPath = cfg != null ? cfg.learningPath : LearningPath.BeginnerFoundations,
                runAssessment = cfg == null || cfg.runAssessment,
                currentScene = SceneManager.GetActiveScene().name,
                assessmentCompleted = assessDone,
                assessmentAverageScore = assessAvg,
                currentSkillsAverageScore = avgSkills,
                completedLevelsCount = completedLevels != null ? completedLevels.Count : 0,
                currentNextLevelId = nextLevelId,
                lastCompletedLevelId = lastCompletedId,
                lastMedal = lastMedal,
                goldMedalsCount = gold,
                silverMedalsCount = silver,
                bronzeMedalsCount = bronze,
                totalTasksAttempted = tasksAttempted,
                totalTasksSucceeded = tasksSucceeded,
                lastUpdateTimestamp = DateTime.Now.ToString("HH:mm:ss"),
                currentSkillScores = skillScores
            };
        }

        private static string ComputeNextLevelFromList(List<string> completed)
        {
            for (int i = 0; i < ProgressData.LevelOrder.Length; i++)
            {
                string lvl = ProgressData.LevelOrder[i];
                if (completed == null || !completed.Contains(lvl))
                {
                    return lvl;
                }
            }
            return "Completed";
        }

        private void SendEnvelopeOverTransports(string type, string payloadJson)
        {
            string packetId = Guid.NewGuid().ToString("N").Substring(0, 12);
            List<string> ipsCopy;
            int udpPort;
            lock (writeLock)
            {
                ipsCopy = new List<string>(activeCandidateIps);
                udpPort = activeHostUdpPort;
            }

            UdpEnvelopePacket udpPacket = new UdpEnvelopePacket
            {
                packetId = packetId,
                senderUid = ClientUid,
                targetUid = "HOST",
                callbackTcpPort = activeCallbackPort,
                type = type,
                payloadJson = payloadJson
            };

            LanDiscovery discovery = LanDiscovery.ExistingInstance;
            Task.Run(() =>
            {
                SendTcpEnvelopeOnly(type, payloadJson, packetId);
                discovery?.SendUdpEnvelopeToHost(ipsCopy, udpPort, udpPacket);
            });
        }

        private void SendTcpEnvelopeOnly(string type, string payloadJson, string packetId)
        {
            NetworkEnvelope env = new NetworkEnvelope(type, payloadJson, packetId);
            string line = JsonUtility.ToJson(env);

            lock (writeLock)
            {
                if (streamWriter == null || stopRequested) return;
                try
                {
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                }
                catch
                {
                    // Reader loop or UDP transport continues
                }
            }
        }

        private bool MarkPacketProcessed(string packetId)
        {
            if (string.IsNullOrEmpty(packetId)) return true;
            if (!processedPacketIds.Add(packetId)) return false;

            processedPacketOrder.Enqueue(packetId);
            while (processedPacketOrder.Count > 512)
            {
                string oldest = processedPacketOrder.Dequeue();
                processedPacketIds.Remove(oldest);
            }
            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshProgressDataSubscription();
            if (IsConnected)
            {
                SendTelemetryNow();
            }
        }

        private void HandleStudentIdChanged(string newId)
        {
            if (IsConnected)
            {
                SendTelemetryNow();
            }
        }

        private void HandleConfigurationChanged(StudentConfiguration cfg)
        {
            if (IsConnected)
            {
                SendTelemetryNow();
            }
        }

        private void RefreshProgressDataSubscription()
        {
            UnsubscribeProgressData();
            subscribedProgressData = ProgressData.ExistingInstance;
            if (subscribedProgressData != null)
            {
                subscribedProgressData.OnProgressChanged += HandleProgressChanged;
                subscribedProgressData.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        private void UnsubscribeProgressData()
        {
            if (subscribedProgressData != null)
            {
                subscribedProgressData.OnProgressChanged -= HandleProgressChanged;
                subscribedProgressData.OnLevelCompleted -= HandleLevelCompleted;
                subscribedProgressData = null;
            }
        }

        private void HandleProgressChanged()
        {
            if (IsConnected)
            {
                SendTelemetryNow();
            }
        }

        private void HandleLevelCompleted(string levelId)
        {
            lastCompletedLevelIdTracker = levelId;
            if (IsConnected)
            {
                SendTelemetryNow();
            }
        }
    }
}
