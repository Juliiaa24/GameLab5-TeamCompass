/**
 * Author: TeamCompass
 * Description: Teacher LAN Host server (DontDestroyOnLoad). Accepts multiple Student TCP clients,
 *              broadcasts presence via LanDiscovery, pushes StudentConfiguration JSON (per-student or classroom-wide),
 *              and aggregates live StudentTelemetrySnapshot updates for the Teacher Dashboard UI.
 *              Strictly decoupled from gameplay scripts.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
    }

    public class NetworkSessionHost : MonoBehaviour
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

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

            public void SendEnvelope(string type, string payloadJson)
            {
                if (closed || writer == null) return;
                NetworkEnvelope env = new NetworkEnvelope(type, payloadJson);
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
        private float nextPingTime;

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
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            LocalIpAddress = LanDiscovery.GetPrimaryLocalIPv4();
            HostName = $"{LanDiscovery.GetDefaultDeviceName()} (Teacher)";
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

            if (IsHosting && Time.unscaledTime >= nextPingTime)
            {
                nextPingTime = Time.unscaledTime + 5.0f;
                BroadcastRawEnvelope(NetworkProtocol.MsgPing, string.Empty);
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

                LanDiscovery.Instance.StartBroadcasting(HostName, ListenPort, () => ActiveConnectedCount);

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

            if (LanDiscovery.ExistingInstance != null)
            {
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

            if (wasHosting)
            {
                Debug.Log("[NetworkSessionHost] Classroom Host stopped.");
                OnHostStateChanged?.Invoke();
                OnStudentsChanged?.Invoke();
            }
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
                resetProgress = resetProgress
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
                    StartStudentSession(orderedStudents[i].clientUid);
                }
            }
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
                    tcp.NoDelay = true;

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
                            handle.SendEnvelope(NetworkProtocol.MsgPong, string.Empty);
                            continue;
                        }
                        if (env.type == NetworkProtocol.MsgPong)
                        {
                            continue;
                        }

                        string msgType = env.type;
                        string payload = env.payloadJson;
                        mainThreadQueue.Enqueue(() => HandleEnvelopeOnMainThread(handle, msgType, payload));
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

        private void HandleEnvelopeOnMainThread(ClientConnectionHandle handle, string msgType, string payloadJson)
        {
            switch (msgType)
            {
                case NetworkProtocol.MsgHello:
                    HandleClientHello(handle, payloadJson);
                    break;

                case NetworkProtocol.MsgTelemetry:
                    HandleClientTelemetry(handle, payloadJson);
                    break;
            }
        }

        private void HandleClientHello(ClientConnectionHandle handle, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            ClientHelloPayload hello = JsonUtility.FromJson<ClientHelloPayload>(payloadJson);
            if (hello == null) return;

            string uid = !string.IsNullOrWhiteSpace(hello.clientUid) ? hello.clientUid : handle.connectionKey;
            handle.clientUid = uid;

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
                    remoteIp = handle.remoteIp,
                    studentId = uniqueStudentId,
                    deviceName = !string.IsNullOrWhiteSpace(hello.deviceName) ? hello.deviceName : handle.remoteIp,
                    isConnected = true,
                    connectedSince = DateTime.Now.ToString("HH:mm:ss"),
                    assignedConfig = defaultCfg
                };

                studentSessions[uid] = session;
                orderedStudents.Add(session);
            }
            else
            {
                session.remoteIp = handle.remoteIp;
                session.deviceName = !string.IsNullOrWhiteSpace(hello.deviceName) ? hello.deviceName : session.deviceName;
                session.isConnected = true;
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
                assignedConfiguration = session.assignedConfig
            };

            handle.SendEnvelope(NetworkProtocol.MsgWelcome, JsonUtility.ToJson(welcome));
            Debug.Log($"[NetworkSessionHost] Student connected: {session.studentId} ({session.deviceName} @ {session.remoteIp}).");

            OnStudentsChanged?.Invoke();
        }

        private void HandleClientTelemetry(ClientConnectionHandle handle, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            StudentTelemetrySnapshot snap = JsonUtility.FromJson<StudentTelemetrySnapshot>(payloadJson);
            if (snap == null) return;

            string uid = !string.IsNullOrWhiteSpace(snap.clientUid) ? snap.clientUid : handle.clientUid;
            if (string.IsNullOrWhiteSpace(uid)) return;

            if (!studentSessions.TryGetValue(uid, out ConnectedStudentSession session))
            {
                return;
            }

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
            // Check whether the student already reconnected on a newer socket handle
            foreach (var kvp in activeHandles)
            {
                if (kvp.Value.clientUid == clientUid && kvp.Key != closedConnectionKey && !kvp.Value.closed)
                {
                    return;
                }
            }

            if (studentSessions.TryGetValue(clientUid, out ConnectedStudentSession session))
            {
                session.isConnected = false;
                Debug.Log($"[NetworkSessionHost] Student disconnected: {session.studentId} ({session.remoteIp}).");
                OnStudentsChanged?.Invoke();
            }
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

        private void SendToClientUid(string clientUid, string type, string payloadJson)
        {
            foreach (var kvp in activeHandles)
            {
                if (kvp.Value.clientUid == clientUid && !kvp.Value.closed)
                {
                    kvp.Value.SendEnvelope(type, payloadJson);
                }
            }
        }

        private void BroadcastRawEnvelope(string type, string payloadJson)
        {
            foreach (var kvp in activeHandles)
            {
                if (!kvp.Value.closed)
                {
                    kvp.Value.SendEnvelope(type, payloadJson);
                }
            }
        }
    }
}
