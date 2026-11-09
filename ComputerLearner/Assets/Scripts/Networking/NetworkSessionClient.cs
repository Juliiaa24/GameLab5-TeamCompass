/**
 * Author: TeamCompass
 * Description: Student LAN Client (DontDestroyOnLoad). Connects to a Teacher Host (via LanDiscovery
 *              or manual IP), receives StudentConfiguration JSON payloads, applies them strictly
 *              through SessionManager, and streams live ProgressData + SkillManager telemetry back
 *              to the Teacher Dashboard without placing any networking code inside gameplay classes.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        public event Action OnClientStateChanged;

        private TcpClient tcpClient;
        private StreamWriter streamWriter;
        private readonly object writeLock = new object();
        private Thread readThread;
        private volatile bool stopRequested;

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
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

            if (IsConnected)
            {
                if (subscribedProgressData != ProgressData.ExistingInstance)
                {
                    RefreshProgressDataSubscription();
                }

                if (Time.unscaledTime >= nextTelemetryHeartbeatTime)
                {
                    nextTelemetryHeartbeatTime = Time.unscaledTime + 2.5f;
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

        public void ConnectToHost(string hostIp, int port = NetworkProtocol.DefaultSessionPort)
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

            IsConnecting = true;
            ConnectedHostIp = trimmedIp;
            ConnectedHostPort = port;
            LastStatusMessage = $"Connecting to {trimmedIp}:{port}...";
            OnClientStateChanged?.Invoke();

            // Capture handshake fields on the Unity main thread before opening background socket
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
            string helloJson = JsonUtility.ToJson(hello);

            stopRequested = false;

            Task.Run(() =>
            {
                try
                {
                    TcpClient client = new TcpClient();
                    IAsyncResult ar = client.BeginConnect(trimmedIp, port, null, null);
                    bool signaled = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3.5));
                    if (!signaled || !client.Connected)
                    {
                        try { client.Close(); } catch { }
                        throw new SocketException((int)SocketError.TimedOut);
                    }
                    client.EndConnect(ar);
                    client.NoDelay = true;

                    NetworkStream netStream = client.GetStream();
                    StreamWriter writer = new StreamWriter(netStream, Utf8NoBom) { AutoFlush = true };

                    lock (writeLock)
                    {
                        tcpClient = client;
                        streamWriter = writer;
                    }

                    SendEnvelopeThreadSafe(NetworkProtocol.MsgHello, helloJson);

                    mainThreadQueue.Enqueue(() =>
                    {
                        IsConnecting = false;
                        IsConnected = true;
                        ConnectedHostName = trimmedIp;
                        LastStatusMessage = $"Connected to {trimmedIp}:{port}";
                        LanDiscovery.Instance.StopListening();
                        RefreshProgressDataSubscription();
                        SendTelemetryNow();
                        OnClientStateChanged?.Invoke();
                    });

                    readThread = new Thread(() => ReadLoop(netStream))
                    {
                        IsBackground = true,
                        Name = "CL_StudentClientReader"
                    };
                    readThread.Start();
                }
                catch (Exception e)
                {
                    mainThreadQueue.Enqueue(() =>
                    {
                        IsConnecting = false;
                        IsConnected = false;
                        LastStatusMessage = $"Connection failed: {e.Message}";
                        OnClientStateChanged?.Invoke();
                    });
                }
            });
        }

        public void Disconnect(bool silent = false)
        {
            bool wasActive = IsConnected || IsConnecting;
            stopRequested = true;
            IsConnecting = false;
            IsConnected = false;

            lock (writeLock)
            {
                try { streamWriter?.Close(); } catch { }
                try { tcpClient?.Close(); } catch { }
                streamWriter = null;
                tcpClient = null;
            }

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
            nextTelemetryHeartbeatTime = Time.unscaledTime + 2.5f;

            Task.Run(() => SendEnvelopeThreadSafe(NetworkProtocol.MsgTelemetry, json));
        }

        private void ReadLoop(NetworkStream netStream)
        {
            try
            {
                using (StreamReader reader = new StreamReader(netStream, Utf8NoBom))
                {
                    while (!stopRequested)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        line = line.Trim('\uFEFF', ' ', '\r', '\n', '\t');
                        if (string.IsNullOrEmpty(line)) continue;

                        NetworkEnvelope env = JsonUtility.FromJson<NetworkEnvelope>(line);
                        if (env == null || string.IsNullOrEmpty(env.type)) continue;

                        if (env.type == NetworkProtocol.MsgPing)
                        {
                            SendEnvelopeThreadSafe(NetworkProtocol.MsgPong, string.Empty);
                            continue;
                        }
                        if (env.type == NetworkProtocol.MsgPong)
                        {
                            continue;
                        }

                        string msgType = env.type;
                        string payload = env.payloadJson;
                        mainThreadQueue.Enqueue(() => HandleEnvelopeOnMainThread(msgType, payload));
                    }
                }
            }
            catch
            {
                // Socket closed
            }
            finally
            {
                if (!stopRequested)
                {
                    mainThreadQueue.Enqueue(() =>
                    {
                        Disconnect(silent: true);
                        LastStatusMessage = "Connection closed by Teacher Host.";
                        OnClientStateChanged?.Invoke();
                    });
                }
            }
        }

        private void HandleEnvelopeOnMainThread(string msgType, string payloadJson)
        {
            switch (msgType)
            {
                case NetworkProtocol.MsgWelcome:
                    HandleHostWelcome(payloadJson);
                    break;

                case NetworkProtocol.MsgAssignConfig:
                    HandleAssignConfig(payloadJson);
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

            if (!string.IsNullOrWhiteSpace(welcome.hostName))
            {
                ConnectedHostName = welcome.hostName;
            }

            if (welcome.assignedConfiguration != null)
            {
                // Only reset progress automatically if student is still sitting idle on Menu with no recorded activity
                bool isIdleOnMenu = SceneManager.GetActiveScene().name == "Menu" &&
                                    !PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent();

                SessionManager.Instance.ApplyConfiguration(welcome.assignedConfiguration, resetExistingProgress: isIdleOnMenu);
            }

            LastStatusMessage = $"Connected to {ConnectedHostName}";
            OnClientStateChanged?.Invoke();
            SendTelemetryNow();

            if (PlaytestStudentWidget.Instance != null)
            {
                PlaytestStudentWidget.Instance.RefreshUI();
                PlaytestStudentWidget.Instance.ShowToast($"Connected to {ConnectedHostName}", 3.0f);
            }
        }

        private void HandleAssignConfig(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;
            AssignConfigPayload assign = JsonUtility.FromJson<AssignConfigPayload>(payloadJson);
            if (assign == null || assign.configuration == null) return;

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

        private void SendEnvelopeThreadSafe(string type, string payloadJson)
        {
            NetworkEnvelope env = new NetworkEnvelope(type, payloadJson);
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
                    // Reader loop will detect closed stream
                }
            }
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
