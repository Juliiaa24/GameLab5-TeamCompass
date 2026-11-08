/**
 * Author: TeamCompass
 * Description: Persistent session coordinator (DontDestroyOnLoad) that manages the active
 *              StudentConfiguration and LearningPath, seeds SkillManager and ProgressData,
 *              and routes scene transitions across AssessmentScene, Desktop1, Desktop2, and VirtualDesktop.
 *              Built to operate both offline (local Menu path selector) and with future LAN Host->Client delivery.
 */
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ComputerLearning
{
    [DefaultExecutionOrder(-120)]
    public class SessionManager : MonoBehaviour
    {
        private const string SelectedPathPrefKey = "Session_SelectedLearningPath";
        private const string ConfigJsonPrefKey = "Session_CurrentConfigJson";
        private const string AppliedSignaturePrefKey = "Session_AppliedSignature";
        private const string PendingSwitchPrefKey = "Session_PendingPathSwitch";

        private static SessionManager instance;

        public static SessionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SessionManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SessionManager");
                        instance = go.AddComponent<SessionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public static SessionManager ExistingInstance => instance;

        public StudentConfiguration CurrentConfiguration { get; private set; }

        public LearningPath ActiveLearningPath =>
            CurrentConfiguration != null ? CurrentConfiguration.learningPath : LearningPath.DiagnosticFull;

        public event Action<StudentConfiguration> OnConfigurationChanged;

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

            PlaytestDataManager.EnsureInitialized();
            LoadOrCreateConfiguration();
        }

        private void OnEnable()
        {
            PlaytestDataManager.OnStudentIdChanged += HandleStudentIdChanged;
        }

        private void OnDisable()
        {
            PlaytestDataManager.OnStudentIdChanged -= HandleStudentIdChanged;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Selects one of the 5 predefined Learning Paths for the current student (offline Menu / local teacher).
        /// Actual state seeding occurs cleanly when starting the session (or immediately on demand).
        /// </summary>
        public void SetLearningPath(LearningPath path)
        {
            PlaytestDataManager.EnsureInitialized();
            StudentConfiguration nextConfig = StudentConfiguration.FromLearningPath(path, PlaytestDataManager.CurrentStudentId);
            CurrentConfiguration = nextConfig;

            SaveConfigurationToPrefs();
            MarkPendingSwitch(true);

            Debug.Log($"[SessionManager] Selected LearningPath '{path}' for student '{CurrentConfiguration.studentId}'.");
            OnConfigurationChanged?.Invoke(CurrentConfiguration);
        }

        /// <summary>
        /// Allows overriding whether the Initial Assessment runs before entering the selected path's starting desktop.
        /// </summary>
        public void SetRunAssessmentOverride(bool runAssessment)
        {
            EnsureConfigurationLoaded();
            if (CurrentConfiguration.runAssessment == runAssessment) return;

            CurrentConfiguration.SetRunAssessment(runAssessment);
            SaveConfigurationToPrefs();
            MarkPendingSwitch(true);

            Debug.Log($"[SessionManager] Assessment override set to {runAssessment} for path '{CurrentConfiguration.learningPath}'.");
            OnConfigurationChanged?.Invoke(CurrentConfiguration);
        }

        /// <summary>
        /// Entry point for applying an explicit StudentConfiguration (used locally or from future LAN Host->Client payload).
        /// Enforces order-of-operations: StudentId -> Clear/Seed Skills -> Seed ProgressData & Tour Flags.
        /// </summary>
        public void ApplyConfiguration(StudentConfiguration config, bool resetExistingProgress = true)
        {
            if (config == null) return;

            PlaytestDataManager.EnsureInitialized();

            // 1. Set Student ID FIRST so all PlaytestDataManager keys/folders target the right student
            if (!string.IsNullOrWhiteSpace(config.studentId) &&
                !string.Equals(config.studentId.Trim(), PlaytestDataManager.CurrentStudentId, StringComparison.OrdinalIgnoreCase))
            {
                PlaytestDataManager.SetCurrentStudentId(config.studentId.Trim());
            }
            config.studentId = PlaytestDataManager.CurrentStudentId;

            CurrentConfiguration = config.Clone();
            SaveConfigurationToPrefs();

            // 2. Optionally clear previous session records before seeding the new path
            if (resetExistingProgress)
            {
                PlaytestDataManager.ClearCurrentStudentSessionData();
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.ResetAllScores();
                }
                else
                {
                    foreach (string skillId in SkillProfilePreset.AllKnownSkillIds)
                    {
                        PlayerPrefs.DeleteKey($"SkillScore_{skillId}");
                        PlayerPrefs.DeleteKey($"SeededSkillScore_{skillId}");
                        PlayerPrefs.DeleteKey($"SkillTasks_{skillId}");
                        PlayerPrefs.DeleteKey($"SkillSuccess_{skillId}");
                        PlayerPrefs.DeleteKey($"SkillFail_{skillId}");
                    }
                }

                if (ProgressData.ExistingInstance != null)
                {
                    ProgressData.ExistingInstance.ClearHistory();
                }
                else
                {
                    PlayerPrefs.DeleteKey("ComputerLearner_ProgressData_v1");
                }
            }

            // 3. Seed SkillManager if bypassing the Initial Assessment
            if (!CurrentConfiguration.runAssessment &&
                CurrentConfiguration.initialSkillScores != null &&
                CurrentConfiguration.initialSkillScores.Count > 0)
            {
                SkillManager.ApplySkillProfileToPrefs(CurrentConfiguration.initialSkillScores, resetTaskCounts: resetExistingProgress);
            }

            // 4. Seed ProgressData unlocked/completed levels and tutorial flags
            ProgressData.Instance.ApplyStudentConfiguration(
                CurrentConfiguration,
                preserveExistingResults: !resetExistingProgress,
                assessmentJustCompleted: false);

            // 5. Mark configuration as active for this student session
            PlayerPrefs.SetString(AppliedSignaturePrefKey, BuildSessionSignature(CurrentConfiguration));
            MarkPendingSwitch(false);
            PlayerPrefs.Save();

            OnConfigurationChanged?.Invoke(CurrentConfiguration);
        }

        /// <summary>
        /// Starts a newly configured LearningPath session or resumes an already-active student session.
        /// Called when clicking Play or pressing SPACE on the Main Menu.
        /// </summary>
        public void StartOrContinueSession()
        {
            EnsureConfigurationLoaded();
            CurrentConfiguration.studentId = PlaytestDataManager.CurrentStudentId;

            bool pendingSwitch = PlayerPrefs.GetInt(PendingSwitchPrefKey, 0) == 1;
            bool isAppliedForStudent = IsConfigurationAppliedForCurrentStudent();

            // Backward compatibility: if launching into existing DiagnosticFull progress with no pending switch, resume it
            if (!pendingSwitch &&
                !isAppliedForStudent &&
                CurrentConfiguration.learningPath == LearningPath.DiagnosticFull &&
                CurrentConfiguration.runAssessment &&
                PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlayerPrefs.SetString(AppliedSignaturePrefKey, BuildSessionSignature(CurrentConfiguration));
                PlayerPrefs.Save();
                isAppliedForStudent = true;
            }

            if (pendingSwitch || !isAppliedForStudent)
            {
                // If auto-download is enabled and the current student already played something before switching paths, save snapshot first
                if (pendingSwitch && PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
                {
                    PlaytestDataManager.ExportCurrentStudentData("auto_before_path_switch");
                }

                ApplyConfiguration(CurrentConfiguration, resetExistingProgress: true);
                string startScene = !string.IsNullOrWhiteSpace(CurrentConfiguration.startingScene)
                    ? CurrentConfiguration.startingScene
                    : ResolveResumeScene();

                Debug.Log($"[SessionManager] Starting fresh '{CurrentConfiguration.learningPath}' session for '{CurrentConfiguration.studentId}' -> {startScene}");
                SceneManager.LoadScene(startScene);
                return;
            }

            string resumeScene = ResolveResumeScene();
            Debug.Log($"[SessionManager] Resuming '{CurrentConfiguration.learningPath}' session for '{CurrentConfiguration.studentId}' -> {resumeScene}");
            SceneManager.LoadScene(resumeScene);
        }

        /// <summary>
        /// Ensures pending VirtualDesktop-based configurations (AdvancedApplications / FreePractice)
        /// are also applied if the user clicks the 'Virtual Desktop' shortcut button on the Main Menu.
        /// </summary>
        public void PrepareForDirectVirtualDesktopEntry()
        {
            EnsureConfigurationLoaded();
            bool pendingSwitch = PlayerPrefs.GetInt(PendingSwitchPrefKey, 0) == 1;
            bool isApplied = IsConfigurationAppliedForCurrentStudent();

            if ((pendingSwitch || !isApplied) &&
                string.Equals(CurrentConfiguration.startingScene, "VirtualDesktop", StringComparison.OrdinalIgnoreCase))
            {
                ApplyConfiguration(CurrentConfiguration, resetExistingProgress: true);
            }
        }

        /// <summary>
        /// Called by InitialTestManager when AssessmentScene finishes or is skipped.
        /// Routes to the configured post-assessment scene without wiping path flags or prior progress on retakes.
        /// </summary>
        public void HandleAssessmentCompleted()
        {
            EnsureConfigurationLoaded();

            bool wasInitialPendingAssessment = PlayerPrefs.GetInt("Session_PendingAssessment", 0) == 1;
            PlayerPrefs.SetInt("Session_PendingAssessment", 0);
            PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);

            if (wasInitialPendingAssessment)
            {
                if (ProgressData.Instance != null)
                {
                    ProgressData.Instance.ApplyStudentConfiguration(
                        CurrentConfiguration,
                        preserveExistingResults: true,
                        assessmentJustCompleted: true);
                }

                string target = !string.IsNullOrWhiteSpace(CurrentConfiguration.postAssessmentScene)
                    ? CurrentConfiguration.postAssessmentScene
                    : "Desktop1";

                PlayerPrefs.Save();
                Debug.Log($"[SessionManager] Initial Assessment completed for '{CurrentConfiguration.learningPath}'. Routing to {target}.");
                SceneManager.LoadScene(target);
                return;
            }

            // Assessment was retaken later (e.g. from StatsScene): preserve completed tours/levels and resume
            if (ProgressData.ExistingInstance == null && !PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlayerPrefs.SetInt("AutoSequenceCompleted", 0);
            }
            PlayerPrefs.Save();

            string nextScene = ResolveResumeScene();
            Debug.Log($"[SessionManager] Retake Assessment completed. Routing to {nextScene}.");
            SceneManager.LoadScene(nextScene);
        }

        /// <summary>
        /// Determines the scene to load after FirstDesktopTutorial completes Level 5 in Desktop1.
        /// Routes BeginnerFoundations (routeDesktop1ToDesktop2 == true) to Desktop2, and DiagnosticFull to VirtualDesktop.
        /// </summary>
        public string GetPostDesktop1Scene()
        {
            EnsureConfigurationLoaded();
            if (CurrentConfiguration != null &&
                CurrentConfiguration.routeDesktop1ToDesktop2 &&
                PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 0 &&
                !(ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(9)))
            {
                return "Desktop2";
            }

            return "VirtualDesktop";
        }

        /// <summary>
        /// Called during SceneSystem.ResetProgress() after PlayerPrefs are wiped and Student ID advances.
        /// Preserves the teacher's selected LearningPath ready for the next student.
        /// </summary>
        public void ResetForNextStudent(string nextStudentId, LearningPath pathToKeep)
        {
            CurrentConfiguration = StudentConfiguration.FromLearningPath(pathToKeep, nextStudentId);
            SaveConfigurationToPrefs();
            PlayerPrefs.DeleteKey(AppliedSignaturePrefKey);
            MarkPendingSwitch(true);
            PlayerPrefs.Save();

            OnConfigurationChanged?.Invoke(CurrentConfiguration);
        }

        /// <summary>
        /// Resolves which scene an in-progress session should resume into based on current completion flags.
        /// </summary>
        public string ResolveResumeScene()
        {
            EnsureConfigurationLoaded();

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.SyncProgressionFlagsToPlayerPrefs();
            }

            if (CurrentConfiguration.runAssessment && PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0)
            {
                return "AssessmentScene";
            }

            string firstGameplay = CurrentConfiguration.runAssessment
                ? CurrentConfiguration.postAssessmentScene
                : CurrentConfiguration.startingScene;

            bool usesDesktop1 = string.Equals(firstGameplay, "Desktop1", StringComparison.OrdinalIgnoreCase);
            bool autoDone = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1 ||
                            (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(5));

            if (usesDesktop1 && !autoDone)
            {
                return "Desktop1";
            }

            bool usesDesktop2 = string.Equals(firstGameplay, "Desktop2", StringComparison.OrdinalIgnoreCase) ||
                                CurrentConfiguration.routeDesktop1ToDesktop2;
            bool semiDone = PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1 ||
                            (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(9));

            if (usesDesktop2 && !semiDone)
            {
                return "Desktop2";
            }

            return "VirtualDesktop";
        }

        public static string GetCurrentLearningPathLabel()
        {
            if (instance != null && instance.CurrentConfiguration != null)
            {
                return StudentConfiguration.GetPathDisplayName(instance.CurrentConfiguration.learningPath);
            }

            int savedPath = PlayerPrefs.GetInt(SelectedPathPrefKey, (int)LearningPath.DiagnosticFull);
            if (Enum.IsDefined(typeof(LearningPath), savedPath))
            {
                return StudentConfiguration.GetPathDisplayName((LearningPath)savedPath);
            }

            return StudentConfiguration.GetPathDisplayName(LearningPath.DiagnosticFull);
        }

        private void EnsureConfigurationLoaded()
        {
            if (CurrentConfiguration == null)
            {
                LoadOrCreateConfiguration();
            }
        }

        private void LoadOrCreateConfiguration()
        {
            string json = PlayerPrefs.GetString(ConfigJsonPrefKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    StudentConfiguration parsed = JsonUtility.FromJson<StudentConfiguration>(json);
                    if (parsed != null)
                    {
                        parsed.studentId = PlaytestDataManager.CurrentStudentId;
                        if (parsed.initialSkillScores == null || parsed.initialSkillScores.Count == 0)
                        {
                            parsed.initialSkillScores = SkillProfilePreset.GetDefaultEntriesForPath(parsed.learningPath);
                        }
                        CurrentConfiguration = parsed;
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SessionManager] Failed to parse saved StudentConfiguration JSON: {e.Message}");
                }
            }

            int savedPathInt = PlayerPrefs.GetInt(SelectedPathPrefKey, (int)LearningPath.DiagnosticFull);
            LearningPath path = Enum.IsDefined(typeof(LearningPath), savedPathInt)
                ? (LearningPath)savedPathInt
                : LearningPath.DiagnosticFull;

            CurrentConfiguration = StudentConfiguration.FromLearningPath(path, PlaytestDataManager.CurrentStudentId);
            SaveConfigurationToPrefs();
        }

        private void SaveConfigurationToPrefs()
        {
            if (CurrentConfiguration == null) return;
            PlayerPrefs.SetInt(SelectedPathPrefKey, (int)CurrentConfiguration.learningPath);
            PlayerPrefs.SetString(ConfigJsonPrefKey, JsonUtility.ToJson(CurrentConfiguration));
            PlayerPrefs.Save();
        }

        private void HandleStudentIdChanged(string newStudentId)
        {
            if (CurrentConfiguration == null) return;
            CurrentConfiguration.studentId = newStudentId;
            SaveConfigurationToPrefs();
            OnConfigurationChanged?.Invoke(CurrentConfiguration);
        }

        private bool IsConfigurationAppliedForCurrentStudent()
        {
            if (CurrentConfiguration == null) return false;
            string savedSig = PlayerPrefs.GetString(AppliedSignaturePrefKey, string.Empty);
            return string.Equals(savedSig, BuildSessionSignature(CurrentConfiguration), StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSessionSignature(StudentConfiguration cfg)
        {
            if (cfg == null) return string.Empty;
            return $"{cfg.studentId}|{(int)cfg.learningPath}|{cfg.runAssessment}";
        }

        private static void MarkPendingSwitch(bool pending)
        {
            PlayerPrefs.SetInt(PendingSwitchPrefKey, pending ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
