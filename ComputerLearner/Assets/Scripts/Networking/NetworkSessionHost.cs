/**
 * Author: TeamCompass
 * Description: Teacher LAN Host server (DontDestroyOnLoad). Accepts multiple Student TCP clients
 *              (both inbound on 47778 and outbound Reverse-TCP to Student 47779), broadcasts presence
 *              via LanDiscovery, relays/receives UDP session envelopes over 47777 as firewall-resilient
 *              transport, pushes StudentConfiguration JSON, and aggregates live StudentTelemetrySnapshot
 *              updates for the Teacher Dashboard UI. Strictly decoupled from gameplay scripts.
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

namespace ComputerLearning
{
    public class ConnectedStudentSession
    {
        public string clientUid;
        public string remoteIp;
        public string studentId;
        public string deviceName;
        public bool isConnected;
        public string connectedSince;
        public StudentConfiguration assignedConfig;
        public StudentTelemetrySnapshot latestTelemetry;
        [NonSerialized] public int remoteUdpPort;
        [NonSerialized] public float lastActivityRealtime;
    }

    public class NetworkSessionHost : MonoBehaviour
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private const string PrefClassNameKey = "TeacherCustomClassName";
        private const string PrefLockStudentNamesKey = "TeacherLockStudentNames";
        private const string PrefLockLeaveClassKey = "TeacherLockLeaveClass";

        private static NetworkSessionHost instance;

        public static NetworkSessionHost Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<NetworkSessionHost>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("NetworkSessionHost");
                        instance = go.AddComponent<NetworkSessionHost>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public static NetworkSessionHost ExistingInstance => instance;

        public bool IsHosting { get; private set; }
        public int ListenPort { get; private set; } = NetworkProtocol.DefaultSessionPort;
        public string HostName { get; private set; } = "Teacher PC";
        public string LocalIpAddress { get; private set; } = "127.0.0.1";
        public string LastError { get; private set; } = string.Empty;

        public LearningPath DefaultClassroomPath { get; private set; } = LearningPath.BeginnerFoundations;
        public bool DefaultClassroomAssessment { get; private set; } = true;
        public bool LockStudentNames { get; private set; }
        public bool LockLeaveClass { get; private set; }

        public event Action OnHostStateChanged;
        public event Action OnStudentsChanged;
        public event Action<ConnectedStudentSession> OnStudentTelemetryUpdated;

        private class ClientConnectionHandle
        {
            public string connectionKey;
            public string clientUid;
            public string remoteIp;
            public TcpClient tcpClient;
            public StreamWriter writer;
            public readonly object writeLock = new object();
            public volatile bool closed;

            public void SendEnvelope(string type, string payloadJson, string packetId = null)
            {
                if (closed || writer == null) return;
                NetworkEnvelope env = new NetworkEnvelope(type, payloadJson, packetId);
                string line = JsonUtility.ToJson(env);
                lock (writeLock)
                {
                    if (closed || writer == null) return;
                    try
                    {
                        writer.WriteLine(line);
                        writer.Flush();
                    }
                    catch
                    {
                        closed = true;
                    }
                }
            }

            public void Close()
            {
                closed = true;
                try { writer?.Close(); } catch { }
                try { tcpClient?.Close(); } catch { }
                writer = null;
                tcpClient = null;
            }
        }

        private string hostUid;
        private TcpListener tcpListener;
        private Thread acceptThread;
        private volatile bool stopRequested;

        private readonly ConcurrentDictionary<string, ClientConnectionHandle> activeHandles =
            new ConcurrentDictionary<string, ClientConnectionHandle>();

        private readonly Dictionary<string, ConnectedStudentSession> studentSessions =
            new Dictionary<string, ConnectedStudentSession>();
        private readonly List<ConnectedStudentSession> orderedStudents =
            new List<ConnectedStudentSession>();

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly HashSet<string> processedPacketIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> processedPacketOrder = new Queue<string>();

        private readonly ConcurrentDictionary<string, byte> inFlightReverseConnects =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> lastReverseAttemptTime =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private float nextPingTime;
        private float nextArpProbeTime;
        private float nextUdpTimeoutCheckTime;

        public IReadOnlyList<ConnectedStudentSession> ConnectedStudents => orderedStudents;

        public int ActiveConnectedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < orderedStudents.Count; i++)
                {
                    if (orderedStudents[i].isConnected) count++;
                }
                return count;
            }
        }

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

            hostUid = "HOST_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            LocalIpAddress = LanDiscovery.GetPrimaryLocalIPv4();
            string savedClassName = PlayerPrefs.GetString(PrefClassNameKey, string.Empty);
            HostName = !string.IsNullOrWhiteSpace(savedClassName)
                ? savedClassName.Trim()
                : $"{LanDiscovery.GetDefaultDeviceName()} (Teacher)";
            LockStudentNames = PlayerPrefs.GetInt(PrefLockStudentNamesKey, 0) == 1;
            LockLeaveClass = PlayerPrefs.GetInt(PrefLockLeaveClassKey, 0) == 1;
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
                    Debug.LogWarning($"[NetworkSessionHost] Main-thread handler warning: {e.Message}");
                }
            }

            if (IsHosting)
            {
                float now = Time.unscaledTime;
                if (now >= nextPingTime)
                {
                    nextPingTime = now + 3.0f;
                    BroadcastRawEnvelope(NetworkProtocol.MsgPing, string.Empty);
                }

                if (now >= nextArpProbeTime)
                {
                    nextArpProbeTime = now + 1.0f;
                    ProbeArpNeighborsForReverseTcp();
                }

                if (now >= nextUdpTimeoutCheckTime)
                {
                    nextUdpTimeoutCheckTime = now + 2.0f;
                    PruneTimedOutUdpOnlyStudents();
                }
            }
        }

        private void OnApplicationQuit()
        {
            StopHost();
        }

        private void OnDestroy()
        {
            StopHost();
            if (instance == this)
            {
                instance = null;
            }
        }

        public bool StartHost(int port = NetworkProtocol.DefaultSessionPort, string customHostName = null)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                LastError = "LAN Host server requires Standalone (Windows/Mac/Linux) or Unity Editor.";
                OnHostStateChanged?.Invoke();
                return false;
            }

            if (IsHosting) return true;

            // Ensure this instance is not simultaneously connected as a client
            if (NetworkSessionClient.ExistingInstance != null && NetworkSessionClient.ExistingInstance.IsConnected)
            {
                NetworkSessionClient.ExistingInstance.Disconnect();
            }

            try
            {
                LastError = string.Empty;
                ListenPort = port;
                LocalIpAddress = LanDiscovery.GetPrimaryLocalIPv4();
                if (!string.IsNullOrWhiteSpace(customHostName))
                {
                    HostName = customHostName.Trim();
                }

                // Sync classroom defaults from local Menu selector so Teacher starts with intuitive defaults
                if (SessionManager.ExistingInstance != null && SessionManager.ExistingInstance.CurrentConfiguration != null)
                {
                    DefaultClassroomPath = SessionManager.ExistingInstance.CurrentConfiguration.learningPath;
                    DefaultClassroomAssessment = SessionManager.ExistingInstance.CurrentConfiguration.runAssessment;
                }

                stopRequested = false;
                tcpListener = new TcpListener(IPAddress.Any, ListenPort);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start();

                IsHosting = true;
                acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "CL_HostAcceptThread"
                };
                acceptThread.Start();

                LanDiscovery discovery = LanDiscovery.Instance;
                discovery.LocalSenderUid = hostUid;
                SubscribeDiscoveryEvents(true);
                discovery.StartBroadcasting(HostName, ListenPort, () => ActiveConnectedCount);

                nextArpProbeTime = 0f;
                Debug.Log($"[NetworkSessionHost] Classroom Host started on {LocalIpAddress}:{ListenPort} ('{HostName}').");
                OnHostStateChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError($"[NetworkSessionHost] Failed to start server on port {port}: {e.Message}");
                StopHost();
                return false;
            }
        }

        public void StopHost()
        {
            bool wasHosting = IsHosting;
            stopRequested = true;
            IsHosting = false;

            SubscribeDiscoveryEvents(false);
            if (LanDiscovery.ExistingInstance != null)
            {
                LanDiscovery.ExistingInstance.LocalSenderUid = string.Empty;
                LanDiscovery.ExistingInstance.StopBroadcasting();
            }

            if (tcpListener != null)
            {
                try { tcpListener.Stop(); } catch { }
                tcpListener = null;
            }

            foreach (var kvp in activeHandles)
            {
                kvp.Value.Close();
            }
            activeHandles.Clear();

            for (int i = 0; i < orderedStudents.Count; i++)
            {
                orderedStudents[i].isConnected = false;
            }
            studentSessions.Clear();
            orderedStudents.Clear();
            processedPacketIds.Clear();
            processedPacketOrder.Clear();
            lastReverseAttemptTime.Clear();

            if (wasHosting)
            {
                Debug.Log("[NetworkSessionHost] Classroom Host stopped.");
                OnHostStateChanged?.Invoke();
                OnStudentsChanged?.Invoke();
            }
        }

        private void SubscribeDiscoveryEvents(bool subscribe)
        {
            LanDiscovery discovery = LanDiscovery.ExistingInstance ?? (subscribe ? LanDiscovery.Instance : null);
            if (discovery == null) return;

            discovery.OnUdpEnvelopeReceived -= HandleUdpEnvelopeReceived;
            discovery.OnStudentDiscoveryAckReceived -= HandleStudentDiscoveryAck;
            if (subscribe)
            {
                discovery.OnUdpEnvelopeReceived += HandleUdpEnvelopeReceived;
                discovery.OnStudentDiscoveryAckReceived += HandleStudentDiscoveryAck;
            }
        }

        private void HandleStudentDiscoveryAck(string studentIp, int callbackPort)
        {
            if (!IsHosting || string.IsNullOrWhiteSpace(studentIp)) return;
            if (!HasActiveTcpConnectionForIp(studentIp))
            {
                TryReverseTcpConnectAsync(studentIp, callbackPort);
            }
        }

        private void HandleUdpEnvelopeReceived(IPEndPoint remoteEp, UdpEnvelopePacket pkt)
        {
            if (!IsHosting || pkt == null) return;
            if (!string.IsNullOrEmpty(pkt.targetUid) && pkt.targetUid != "HOST" && pkt.targetUid != hostUid) return;

            string remoteIp = remoteEp?.Address != null ? remoteEp.Address.ToString() : "Unknown";
            int remoteUdpPort = remoteEp != null ? remoteEp.Port : NetworkProtocol.DiscoveryPort;

            if (!string.IsNullOrWhiteSpace(pkt.senderUid) &&
                studentSessions.TryGetValue(pkt.senderUid, out ConnectedStudentSession existingSession))
            {
                existingSession.lastActivityRealtime = Time.realtimeSinceStartup;
                if (remoteEp?.Address != null) existingSession.remoteIp = remoteIp;
                if (remoteUdpPort > 0) existingSession.remoteUdpPort = remoteUdpPort;
            }

            if (pkt.type == NetworkProtocol.MsgPing)
            {
                if (!string.IsNullOrEmpty(pkt.senderUid) && LanDiscovery.ExistingInstance != null)
                {
                    LanDiscovery.ExistingInstance.SendUdpEnvelopeToStudent(remoteIp, remoteUdpPort, new UdpEnvelopePacket
                    {
                        packetId = Guid.NewGuid().ToString("N").Substring(0, 12),
                        senderUid = hostUid,
                        targetUid = pkt.senderUid,
                        type = NetworkProtocol.MsgPong,
                        payloadJson = string.Empty
                    });
                }
                return;
            }

            if (pkt.type == NetworkProtocol.MsgPong)
            {
                return;
            }

            if (!MarkPacketProcessed(pkt.packetId))
            {
                return;
            }

            if (pkt.type == NetworkProtocol.MsgHello)
            {
                HandleClientHelloData(null, remoteIp, remoteUdpPort, pkt.payloadJson);
                if (!string.IsNullOrEmpty(pkt.senderUid) &&
                    !HasActiveTcpConnectionForUid(pkt.senderUid) &&
                    pkt.callbackTcpPort > 0)
                {
                    TryReverseTcpConnectAsync(remoteIp, pkt.callbackTcpPort);
                }
            }
            else if (pkt.type == NetworkProtocol.MsgTelemetry)
            {
                HandleClientTelemetryData(null, remoteIp, remoteUdpPort, pkt.payloadJson);
            }
        }

        private void ProbeArpNeighborsForReverseTcp()
        {
            List<IPAddress> neighbors = LanDiscovery.GetArpNeighborIPv4Addresses();
            for (int i = 0; i < neighbors.Count; i++)
            {
                string ip = neighbors[i].ToString();
                if (HasActiveTcpConnectionForIp(ip)) continue;
                TryReverseTcpConnectAsync(ip, NetworkProtocol.DefaultClientCallbackPort);
            }
        }

        private void TryReverseTcpConnectAsync(string studentIp, int callbackPort)
        {
            if (!IsHosting || stopRequested || string.IsNullOrWhiteSpace(studentIp)) return;
            if (callbackPort <= 0) callbackPort = NetworkProtocol.DefaultClientCallbackPort;

            string key = $"{studentIp}:{callbackPort}";
            float now = Time.unscaledTime;
            if (lastReverseAttemptTime.TryGetValue(key, out float lastAttempt) && now - lastAttempt < 2.5f)
            {
                return;
            }
            if (!inFlightReverseConnects.TryAdd(key, 1))
            {
                return;
            }
            lastReverseAttemptTime[key] = now;

            Task.Run(() =>
            {
                try
                {
                    TcpClient tcp = new TcpClient();
                    IAsyncResult ar = tcp.BeginConnect(studentIp, callbackPort, null, null);
                    bool signaled = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(650));
                    if (!signaled || !tcp.Connected || stopRequested || !IsHosting)
                    {
                        try { tcp.Close(); } catch { }
                        return;
                    }
                    tcp.EndConnect(ar);
                    RegisterAcceptedTcpClient(tcp);
                    Debug.Log($"[NetworkSessionHost] Established Reverse-TCP connection to Student at {studentIp}:{callbackPort}.");
                }
                catch
                {
                    // Neighbor is not a Student listening on callback port
                }
                finally
                {
                    inFlightReverseConnects.TryRemove(key, out _);
                }
            });
        }

        public void SetCustomClassName(string newClassName)
        {
            string trimmed = string.IsNullOrWhiteSpace(newClassName)
                ? $"{LanDiscovery.GetDefaultDeviceName()} (Teacher)"
                : newClassName.Trim();

            HostName = trimmed;
            PlayerPrefs.SetString(PrefClassNameKey, trimmed);
            PlayerPrefs.Save();

            if (LanDiscovery.ExistingInstance != null)
            {
                LanDiscovery.ExistingInstance.UpdateBroadcastHostName(HostName);
            }

            BroadcastClassroomPermissions();
            OnHostStateChanged?.Invoke();
        }

        public void SetLockStudentNames(bool locked)
        {
            LockStudentNames = locked;
            PlayerPrefs.SetInt(PrefLockStudentNamesKey, locked ? 1 : 0);
            PlayerPrefs.Save();
            BroadcastClassroomPermissions();
            OnHostStateChanged?.Invoke();
        }

        public void SetLockLeaveClass(bool locked)
        {
            LockLeaveClass = locked;
            PlayerPrefs.SetInt(PrefLockLeaveClassKey, locked ? 1 : 0);
            PlayerPrefs.Save();
            BroadcastClassroomPermissions();
            OnHostStateChanged?.Invoke();
        }

        private void BroadcastClassroomPermissions()
        {
            if (!IsHosting) return;
            ClassroomPermissionsPayload payload = new ClassroomPermissionsPayload
            {
                className = HostName,
                lockStudentNames = LockStudentNames,
                lockLeaveClass = LockLeaveClass
            };
            BroadcastRawEnvelope(NetworkProtocol.MsgClassroomPermissions, JsonUtility.ToJson(payload));
        }

        public void SetClassroomDefaults(LearningPath path, bool runAssessment)
        {
            DefaultClassroomPath = path;
            DefaultClassroomAssessment = runAssessment;
            OnHostStateChanged?.Invoke();
        }

        public ConnectedStudentSession GetStudent(string clientUid)
        {
            if (string.IsNullOrWhiteSpace(clientUid)) return null;
            return studentSessions.TryGetValue(clientUid, out ConnectedStudentSession s) ? s : null;
        }

        public void SetStudentDraftPath(string clientUid, LearningPath path, bool pushImmediately = true)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null) return;

            bool keepAssessment = session.assignedConfig != null
                ? session.assignedConfig.runAssessment
                : DefaultClassroomAssessment;

            session.assignedConfig = StudentConfiguration.FromLearningPath(path, session.studentId, keepAssessment);
            OnStudentsChanged?.Invoke();

            if (pushImmediately && session.isConnected)
            {
                PushStudentConfiguration(clientUid, startImmediately: false, resetProgress: true);
            }
        }

        public void SetStudentDraftAssessment(string clientUid, bool runAssessment, bool pushImmediately = true)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null) return;

            if (session.assignedConfig == null)
            {
                session.assignedConfig = StudentConfiguration.FromLearningPath(DefaultClassroomPath, session.studentId, runAssessment);
            }
            else
            {
                session.assignedConfig.SetRunAssessment(runAssessment);
            }

            OnStudentsChanged?.Invoke();

            if (pushImmediately && session.isConnected)
            {
                PushStudentConfiguration(clientUid, startImmediately: false, resetProgress: true);
            }
        }

        public void CycleStudentId(string clientUid, int delta)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null) return;

            int currentNum = ParseStudentNumber(session.studentId);
            int nextNum = Mathf.Clamp(currentNum + delta, 1, 99);
            string nextId = $"S{nextNum:00}";

            session.studentId = nextId;
            if (session.assignedConfig != null)
            {
                session.assignedConfig.studentId = nextId;
            }
            if (session.latestTelemetry != null)
            {
                session.latestTelemetry.studentId = nextId;
            }

            OnStudentsChanged?.Invoke();
            if (session.isConnected)
            {
                PushStudentConfiguration(clientUid, startImmediately: false, resetProgress: false);
            }
        }

        public void PushStudentConfiguration(string clientUid, bool startImmediately, bool resetProgress = true)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null || !session.isConnected) return;

            if (session.assignedConfig == null)
            {
                session.assignedConfig = StudentConfiguration.FromLearningPath(
                    DefaultClassroomPath,
                    session.studentId,
                    DefaultClassroomAssessment);
            }
            session.assignedConfig.studentId = session.studentId;

            AssignConfigPayload payload = new AssignConfigPayload
            {
                configuration = session.assignedConfig,
                startImmediately = startImmediately,
                resetProgress = resetProgress,
                lockStudentNames = LockStudentNames,
                lockLeaveClass = LockLeaveClass
            };

            SendToClientUid(clientUid, NetworkProtocol.MsgAssignConfig, JsonUtility.ToJson(payload));
            Debug.Log($"[NetworkSessionHost] Sent config '{session.assignedConfig.learningPath}' (Assessment={session.assignedConfig.runAssessment}, start={startImmediately}) -> {session.studentId} ({session.remoteIp}).");
        }

        public void ApplyClassroomDefaultsToAll(bool startImmediately)
        {
            for (int i = 0; i < orderedStudents.Count; i++)
            {
                ConnectedStudentSession s = orderedStudents[i];
                if (!s.isConnected) continue;

                s.assignedConfig = StudentConfiguration.FromLearningPath(
                    DefaultClassroomPath,
                    s.studentId,
                    DefaultClassroomAssessment);

                PushStudentConfiguration(s.clientUid, startImmediately: startImmediately, resetProgress: true);
            }
            OnStudentsChanged?.Invoke();
        }

        public void StartStudentSession(string clientUid)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null || !session.isConnected) return;

            SendToClientUid(clientUid, NetworkProtocol.MsgStartSession, string.Empty);
        }

        public void StartAllConnectedStudents()
        {
            for (int i = 0; i < orderedStudents.Count; i++)
            {
                if (orderedStudents[i].isConnected)
                {
                    PushStudentConfiguration(orderedStudents[i].clientUid, startImmediately: true, resetProgress: true);
                }
            }
            OnStudentsChanged?.Invoke();
        }

        public void ResetStudentSession(string clientUid)
        {
            ConnectedStudentSession session = GetStudent(clientUid);
            if (session == null || !session.isConnected) return;

            if (session.assignedConfig == null)
            {
                session.assignedConfig = StudentConfiguration.FromLearningPath(
                    DefaultClassroomPath,
                    session.studentId,
                    DefaultClassroomAssessment);
            }

            ResetStudentPayload payload = new ResetStudentPayload
            {
                nextStudentId = session.studentId,
                configuration = session.assignedConfig
            };

            SendToClientUid(clientUid, NetworkProtocol.MsgResetStudent, JsonUtility.ToJson(payload));
        }

        public void RemoveDisconnectedStudents()
        {
            for (int i = orderedStudents.Count - 1; i >= 0; i--)
            {
                if (!orderedStudents[i].isConnected)
                {
                    studentSessions.Remove(orderedStudents[i].clientUid);
                    orderedStudents.RemoveAt(i);
                }
            }
            OnStudentsChanged?.Invoke();
        }

        private void AcceptLoop()
        {
            while (!stopRequested && tcpListener != null)
            {
                try
                {
                    TcpClient tcp = tcpListener.AcceptTcpClient();
                    RegisterAcceptedTcpClient(tcp);
                }
                catch (SocketException)
                {
                    if (stopRequested) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (!stopRequested)
                    {
                        Debug.LogWarning($"[NetworkSessionHost] Accept warning: {e.Message}");
                    }
                }
            }
        }

        private void RegisterAcceptedTcpClient(TcpClient tcp)
        {
            if (tcp == null) return;
            tcp.NoDelay = true;
            tcp.ReceiveTimeout = 30000;

            string remoteIp = (tcp.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "Unknown";
            string connKey = Guid.NewGuid().ToString("N");

            NetworkStream netStream = tcp.GetStream();
            StreamWriter writer = new StreamWriter(netStream, Utf8NoBom) { AutoFlush = true };

            ClientConnectionHandle handle = new ClientConnectionHandle
            {
                connectionKey = connKey,
                remoteIp = remoteIp,
                tcpClient = tcp,
                writer = writer
            };
            activeHandles[connKey] = handle;

            Thread readerThread = new Thread(() => ClientReadLoop(handle, netStream))
            {
                IsBackground = true,
                Name = $"CL_HostClient_{remoteIp}"
            };
            readerThread.Start();
        }

        private void ClientReadLoop(ClientConnectionHandle handle, NetworkStream netStream)
        {
            try
            {
                using (StreamReader reader = new StreamReader(netStream, Utf8NoBom))
                {
                    while (!stopRequested && !handle.closed)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        line = line.Trim('\uFEFF', ' ', '\r', '\n', '\t');
                        if (string.IsNullOrEmpty(line)) continue;

                        NetworkEnvelope env = JsonUtility.FromJson<NetworkEnvelope>(line);
                        if (env == null || string.IsNullOrEmpty(env.type)) continue;

                        if (env.type == NetworkProtocol.MsgPing)
                        {
                            handle.SendEnvelope(NetworkProtocol.MsgPong, string.Empty, Guid.NewGuid().ToString("N").Substring(0, 12));
                            continue;
                        }
                        if (env.type == NetworkProtocol.MsgPong)
                        {
                            continue;
                        }

                        string msgType = env.type;
                        string payload = env.payloadJson;
                        string packetId = env.packetId;
                        mainThreadQueue.Enqueue(() => HandleEnvelopeOnMainThread(handle, msgType, payload, packetId));
                    }
                }
            }
            catch
            {
                // Connection closed or reset
            }
            finally
            {
                activeHandles.TryRemove(handle.connectionKey, out _);
                handle.Close();

                if (!string.IsNullOrEmpty(handle.clientUid))
                {
                    string disconnectedUid = handle.clientUid;
                    mainThreadQueue.Enqueue(() => HandleClientDisconnectedOnMainThread(disconnectedUid, handle.connectionKey));
                }
            }
        }

        private void HandleEnvelopeOnMainThread(ClientConnectionHandle handle, string msgType, string payloadJson, string packetId)
        {
            if (!string.IsNullOrEmpty(handle.clientUid) &&
                studentSessions.TryGetValue(handle.clientUid, out ConnectedStudentSession activeSession))
            {
                activeSession.lastActivityRealtime = Time.realtimeSinceStartup;
            }

            if (msgType == NetworkProtocol.MsgHello)
            {
                // Always bind clientUid to the TCP handle even if the same packetId arrived over UDP first
                HandleClientHelloData(handle, handle.remoteIp, 0, payloadJson, packetId);
                return;
            }

            if (!MarkPacketProcessed(packetId))
            {
                return;
            }

            if (msgType == NetworkProtocol.MsgTelemetry)
            {
                HandleClientTelemetryData(handle, handle.remoteIp, 0, payloadJson);
            }
        }

        private void HandleClientHelloData(ClientConnectionHandle handle, string remoteIp, int remoteUdpPort, string payloadJson, string packetId = null)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            ClientHelloPayload hello = JsonUtility.FromJson<ClientHelloPayload>(payloadJson);
            if (hello == null) return;

            string uid = !string.IsNullOrWhiteSpace(hello.clientUid)
                ? hello.clientUid
                : (handle != null ? handle.connectionKey : remoteIp);

            if (handle != null)
            {
                handle.clientUid = uid;
            }

            // If this exact hello packet already came through another transport, just ensure handle is bound and welcome sent
            bool duplicateHello = !string.IsNullOrEmpty(packetId) && !MarkPacketProcessed(packetId);
            bool isNewOrUpdated = false;

            if (!studentSessions.TryGetValue(uid, out ConnectedStudentSession session))
            {
                string uniqueStudentId = EnsureUniqueStudentIdForNewClient(hello.studentId, uid);
                StudentConfiguration defaultCfg = StudentConfiguration.FromLearningPath(
                    DefaultClassroomPath,
                    uniqueStudentId,
                    DefaultClassroomAssessment);

                session = new ConnectedStudentSession
                {
                    clientUid = uid,
                    remoteIp = remoteIp,
                    remoteUdpPort = remoteUdpPort > 0 ? remoteUdpPort : NetworkProtocol.DiscoveryPort,
                    lastActivityRealtime = Time.realtimeSinceStartup,
                    studentId = uniqueStudentId,
                    deviceName = !string.IsNullOrWhiteSpace(hello.deviceName) ? hello.deviceName : remoteIp,
                    isConnected = true,
                    connectedSince = DateTime.Now.ToString("HH:mm:ss"),
                    assignedConfig = defaultCfg
                };

                studentSessions[uid] = session;
                orderedStudents.Add(session);
                isNewOrUpdated = true;
            }
            else
            {
                if (!duplicateHello)
                {
                    session.remoteIp = remoteIp;
                    if (remoteUdpPort > 0) session.remoteUdpPort = remoteUdpPort;
                    session.deviceName = !string.IsNullOrWhiteSpace(hello.deviceName) ? hello.deviceName : session.deviceName;
                    session.isConnected = true;
                    isNewOrUpdated = true;
                }
                session.lastActivityRealtime = Time.realtimeSinceStartup;
                if (session.assignedConfig == null)
                {
                    session.assignedConfig = StudentConfiguration.FromLearningPath(
                        DefaultClassroomPath,
                        session.studentId,
                        DefaultClassroomAssessment);
                }
            }

            HostWelcomePayload welcome = new HostWelcomePayload
            {
                hostName = HostName,
                assignedStudentId = session.studentId,
                assignedConfiguration = session.assignedConfig,
                lockStudentNames = LockStudentNames,
                lockLeaveClass = LockLeaveClass
            };

            string welcomeJson = JsonUtility.ToJson(welcome);
            string welcomePacketId = Guid.NewGuid().ToString("N").Substring(0, 12);

            if (handle != null && !handle.closed)
            {
                handle.SendEnvelope(NetworkProtocol.MsgWelcome, welcomeJson, welcomePacketId);
            }
            else
            {
                SendTcpToClientUid(uid, NetworkProtocol.MsgWelcome, welcomeJson, welcomePacketId);
            }

            if (LanDiscovery.ExistingInstance != null)
            {
                LanDiscovery.ExistingInstance.SendUdpEnvelopeToStudent(session.remoteIp, session.remoteUdpPort, new UdpEnvelopePacket
                {
                    packetId = welcomePacketId,
                    senderUid = hostUid,
                    targetUid = uid,
                    type = NetworkProtocol.MsgWelcome,
                    payloadJson = welcomeJson
                });
            }

            if (isNewOrUpdated)
            {
                Debug.Log($"[NetworkSessionHost] Student connected: {session.studentId} ({session.deviceName} @ {session.remoteIp}).");
                OnStudentsChanged?.Invoke();
            }
        }

        private void HandleClientTelemetryData(ClientConnectionHandle handle, string remoteIp, int remoteUdpPort, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            StudentTelemetrySnapshot snap = JsonUtility.FromJson<StudentTelemetrySnapshot>(payloadJson);
            if (snap == null) return;

            string uid = !string.IsNullOrWhiteSpace(snap.clientUid) ? snap.clientUid : handle?.clientUid;
            if (string.IsNullOrWhiteSpace(uid)) return;

            if (!studentSessions.TryGetValue(uid, out ConnectedStudentSession session))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(remoteIp) && remoteIp != "Unknown")
            {
                session.remoteIp = remoteIp;
            }
            if (remoteUdpPort > 0)
            {
                session.remoteUdpPort = remoteUdpPort;
            }
            session.lastActivityRealtime = Time.realtimeSinceStartup;
            session.isConnected = true;
            session.latestTelemetry = snap;

            if (!string.IsNullOrWhiteSpace(snap.studentId))
            {
                session.studentId = snap.studentId;
                if (session.assignedConfig != null)
                {
                    session.assignedConfig.studentId = snap.studentId;
                }
            }

            OnStudentTelemetryUpdated?.Invoke(session);
        }

        private void HandleClientDisconnectedOnMainThread(string clientUid, string closedConnectionKey)
        {
            foreach (var kvp in activeHandles)
            {
                if (kvp.Value.clientUid == clientUid && kvp.Key != closedConnectionKey && !kvp.Value.closed)
                {
                    return;
                }
            }

            if (studentSessions.TryGetValue(clientUid, out ConnectedStudentSession session))
            {
                studentSessions.Remove(clientUid);
                orderedStudents.Remove(session);
                Debug.Log($"[NetworkSessionHost] Student disconnected and removed: {session.studentId} ({session.remoteIp}).");
                OnStudentsChanged?.Invoke();
            }
        }

        private void PruneTimedOutUdpOnlyStudents()
        {
            float now = Time.realtimeSinceStartup;
            bool removedAny = false;
            for (int i = orderedStudents.Count - 1; i >= 0; i--)
            {
                ConnectedStudentSession session = orderedStudents[i];
                if (HasActiveTcpConnectionForUid(session.clientUid)) continue;

                if (now - session.lastActivityRealtime > 10.0f)
                {
                    studentSessions.Remove(session.clientUid);
                    orderedStudents.RemoveAt(i);
                    removedAny = true;
                    Debug.Log($"[NetworkSessionHost] UDP-only student timed out: {session.studentId} ({session.remoteIp}).");
                }
            }

            if (removedAny)
            {
                OnStudentsChanged?.Invoke();
            }
        }

        private bool HasActiveTcpConnectionForUid(string clientUid)
        {
            if (string.IsNullOrEmpty(clientUid)) return false;
            foreach (var kvp in activeHandles)
            {
                if (!kvp.Value.closed && kvp.Value.clientUid == clientUid)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasActiveTcpConnectionForIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            foreach (var kvp in activeHandles)
            {
                if (!kvp.Value.closed && string.Equals(kvp.Value.remoteIp, ip, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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

        private string EnsureUniqueStudentIdForNewClient(string requestedId, string clientUid)
        {
            string candidate = string.IsNullOrWhiteSpace(requestedId) ? "S01" : requestedId.Trim();
            if (!IsStudentIdUsedByAnotherClient(candidate, clientUid))
            {
                return candidate;
            }

            for (int n = 1; n <= 99; n++)
            {
                string next = $"S{n:00}";
                if (!IsStudentIdUsedByAnotherClient(next, clientUid))
                {
                    return next;
                }
            }

            return $"S{orderedStudents.Count + 1:00}";
        }

        private bool IsStudentIdUsedByAnotherClient(string studentId, string currentUid)
        {
            for (int i = 0; i < orderedStudents.Count; i++)
            {
                ConnectedStudentSession s = orderedStudents[i];
                if (s.clientUid != currentUid &&
                    string.Equals(s.studentId, studentId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static int ParseStudentNumber(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId)) return 1;
            string digits = string.Empty;
            for (int i = 0; i < studentId.Length; i++)
            {
                if (char.IsDigit(studentId[i])) digits += studentId[i];
            }
            return int.TryParse(digits, out int val) && val > 0 ? val : 1;
        }

        private void SendTcpToClientUid(string clientUid, string type, string payloadJson, string packetId)
        {
            foreach (var kvp in activeHandles)
            {
                if (kvp.Value.clientUid == clientUid && !kvp.Value.closed)
                {
                    kvp.Value.SendEnvelope(type, payloadJson, packetId);
                }
            }
        }

        private void SendToClientUid(string clientUid, string type, string payloadJson)
        {
            string packetId = Guid.NewGuid().ToString("N").Substring(0, 12);
            SendTcpToClientUid(clientUid, type, payloadJson, packetId);

            if (studentSessions.TryGetValue(clientUid, out ConnectedStudentSession session) &&
                LanDiscovery.ExistingInstance != null)
            {
                LanDiscovery.ExistingInstance.SendUdpEnvelopeToStudent(session.remoteIp, session.remoteUdpPort, new UdpEnvelopePacket
                {
                    packetId = packetId,
                    senderUid = hostUid,
                    targetUid = clientUid,
                    type = type,
                    payloadJson = payloadJson
                });
            }
        }

        private void BroadcastRawEnvelope(string type, string payloadJson)
        {
            for (int i = 0; i < orderedStudents.Count; i++)
            {
                ConnectedStudentSession s = orderedStudents[i];
                if (s != null && s.isConnected)
                {
                    SendToClientUid(s.clientUid, type, payloadJson);
                }
            }

            // Also reach any unassigned TCP handle still mid-handshake
            foreach (var kvp in activeHandles)
            {
                if (string.IsNullOrEmpty(kvp.Value.clientUid) && !kvp.Value.closed)
                {
                    kvp.Value.SendEnvelope(type, payloadJson, Guid.NewGuid().ToString("N").Substring(0, 12));
                }
            }
        }
    }
}
