/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Persists and provides access to the full task history and level attempts for reporting.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Stores the complete historical record of TaskResults and LevelScoreRecords and persists it
    /// between play sessions using PlayerPrefs + JsonUtility.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class ProgressData : MonoBehaviour
    {
        #region Public Variables
        private static ProgressData _instance;
        private static bool _isShuttingDown = false;
        public static bool HasInstance => _instance != null;
        public static ProgressData ExistingInstance => _instance != null ? _instance : FindAnyObjectByType<ProgressData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _isShuttingDown = false;
        }

        public static ProgressData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ProgressData>();
                    if (_instance == null && !_isShuttingDown && Application.isPlaying && UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded)
                    {
                        GameObject go = new GameObject("ProgressData");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<ProgressData>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        /// <summary>Read-only access to the full ordered task history.</summary>
        public IReadOnlyList<TaskResult> TaskHistory => taskHistory;

        /// <summary>Total number of results stored.</summary>
        public int TotalResultCount => taskHistory.Count;
        #endregion

        #region Private Variables
        private const string SaveKey = "ComputerLearner_ProgressData_v1";

        [Tooltip("If true, history is saved to PlayerPrefs after every recorded result. " +
                 "Disable on very low-end hardware and call Save() manually if needed.")]
        [SerializeField] private bool autoSaveAfterEachResult = true;

        private List<TaskResult> taskHistory = new List<TaskResult>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (_instance.GetComponent<DesktopManager>() == null && _instance.gameObject.name == "ProgressData")
                {
                    Destroy(_instance.gameObject);
                }
            }
            _instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Always save and auto-export playtest data when the application closes
        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
            Save();
            if (PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlaytestDataManager.ExportCurrentStudentData("application_quit");
            }
        }
        #endregion

        public static readonly string[] LevelOrder = { "level1", "level2", "level3", "level4", "level5", "level6", "level7", "level8", "level9", "level10", "level11", "level12", "level13", "level14", "level15", "level16" };

        public bool HasSeenDesktopTour { get; private set; }
        public bool UnlockAllLevels => unlockAllLevels;
        private bool unlockAllLevels = false;
        private List<string> unlockedLevels = new List<string> { "level1" }; // Level 1 always unlocked
        private List<string> completedLevels = new List<string>();
        private List<LevelScoreRecord> levelRecords = new List<LevelScoreRecord>();
        private List<LevelAttemptLogEntry> levelAttemptsHistory = new List<LevelAttemptLogEntry>();

        public IReadOnlyList<LevelScoreRecord> LevelRecords => levelRecords;
        public IReadOnlyList<LevelAttemptLogEntry> LevelAttemptsHistory => levelAttemptsHistory;

        #region Public Methods
        public System.Action<string> OnLevelUnlocked;
        public System.Action<string> OnLevelCompleted;
        public System.Action OnProgressChanged;

        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return true;
            if (unlockAllLevels) return true;
            if (levelId == "level1") return true;
            if (completedLevels != null && completedLevels.Contains(levelId)) return true;

            int orderIdx = Array.IndexOf(LevelOrder, levelId);

            // Levels 2-16 (orderIdx >= 1) unlock strictly progressively after completing the previous level
            if (orderIdx >= 1)
            {
                string prevLevelId = LevelOrder[orderIdx - 1];
                return completedLevels != null && completedLevels.Contains(prevLevelId);
            }

            if (unlockedLevels != null && unlockedLevels.Contains(levelId)) return true;

            return false;
        }

        public bool IsLevelCompleted(string levelId)
        {
            return completedLevels != null && completedLevels.Contains(levelId);
        }

        public bool AreAllLevelsCompletedUpTo(int maxLevelNumber)
        {
            if (maxLevelNumber <= 0) return true;
            if (completedLevels == null) return false;
            for (int i = 1; i <= maxLevelNumber; i++)
            {
                if (!completedLevels.Contains("level" + i))
                    return false;
            }
            return true;
        }

        public void SyncProgressionFlagsToPlayerPrefs()
        {
            bool pendingAssessment = PlayerPrefs.GetInt("Session_PendingAssessment", 0) == 1;

            if (!pendingAssessment && completedLevels != null && completedLevels.Count > 0)
            {
                PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
            }
            if (AreAllLevelsCompletedUpTo(5))
            {
                PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
                if (!pendingAssessment)
                {
                    PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
                }
                if (!unlockedLevels.Contains("level6")) unlockedLevels.Add("level6");
            }
            if (HasSeenDesktopTour)
            {
                PlayerPrefs.SetInt("DesktopTourCompleted", 1);
            }
            else if (PlayerPrefs.GetInt("DesktopTourCompleted", 0) == 1)
            {
                HasSeenDesktopTour = true;
            }
        }

        /// <summary>
        /// Seeds unlocked/completed levels and tutorial progression flags from a StudentConfiguration.
        /// Persists immediately to PlayerPrefs so scene-loaded Managers.prefab instances read the exact state.
        /// </summary>
        public void ApplyStudentConfiguration(StudentConfiguration config, bool preserveExistingResults = false, bool assessmentJustCompleted = false)
        {
            if (config == null) return;

            if (!preserveExistingResults)
            {
                taskHistory.Clear();
                levelRecords.Clear();
                levelAttemptsHistory.Clear();
                completedLevels.Clear();
                unlockedLevels = new List<string> { "level1" };
            }

            unlockAllLevels = config.unlockAllLevels;

            int maxPreComplete = Mathf.Clamp(config.preCompleteUpToLevel, 0, LevelOrder.Length);
            for (int i = 1; i <= maxPreComplete; i++)
            {
                string id = "level" + i;
                if (!completedLevels.Contains(id)) completedLevels.Add(id);
                if (!unlockedLevels.Contains(id)) unlockedLevels.Add(id);
            }

            if (maxPreComplete > 0 && maxPreComplete < LevelOrder.Length)
            {
                string nextAfterPreComplete = "level" + (maxPreComplete + 1);
                if (!unlockedLevels.Contains(nextAfterPreComplete))
                {
                    unlockedLevels.Add(nextAfterPreComplete);
                }
            }

            if (!string.IsNullOrWhiteSpace(config.startingLevelId))
            {
                string startLvl = config.startingLevelId.Trim();
                if (!unlockedLevels.Contains(startLvl))
                {
                    unlockedLevels.Add(startLvl);
                }
            }

            if (unlockAllLevels)
            {
                for (int i = 0; i < LevelOrder.Length; i++)
                {
                    string id = LevelOrder[i];
                    if (!unlockedLevels.Contains(id))
                    {
                        unlockedLevels.Add(id);
                    }
                }
            }

            HasSeenDesktopTour = config.skipBasicDesktopTours;

            bool needsAssessment = config.runAssessment && !assessmentJustCompleted;
            bool skipInitialGuidedTutorial = !config.runInitialGuidedTutorial || maxPreComplete >= 5 || unlockAllLevels;

            PlayerPrefs.SetInt("Session_PendingAssessment", needsAssessment ? 1 : 0);
            PlayerPrefs.SetInt("InitialAssessmentCompleted", needsAssessment ? 0 : 1);
            PlayerPrefs.SetInt("AutoSequenceCompleted", skipInitialGuidedTutorial ? 1 : 0);
            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 0);
            PlayerPrefs.SetInt("DesktopTourCompleted", config.skipBasicDesktopTours ? 1 : 0);
            PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", config.skipBasicDesktopTours ? 1 : 0);
            PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", config.skipAdvancedDesktopTour ? 1 : 0);

            Save();
            OnProgressChanged?.Invoke();

            Debug.Log($"[ProgressData] Applied StudentConfiguration '{config.learningPath}' (assessment={config.runAssessment}, initialGuided={config.runInitialGuidedTutorial}, skipBasicTours={config.skipBasicDesktopTours}, skipAdvTour={config.skipAdvancedDesktopTour}).");
        }

        /// <summary>
        /// Returns the first uncompleted level in the canonical progression sequence.
        /// Returns null if all levels are completed.
        /// </summary>
        public string GetCurrentNextLevelId()
        {
            foreach (string lvl in LevelOrder)
            {
                if (!completedLevels.Contains(lvl))
                {
                    return lvl;
                }
            }
            return null;
        }

        public void UnlockLevel(string levelId)
        {
            if (!unlockedLevels.Contains(levelId))
            {
                unlockedLevels.Add(levelId);
                OnLevelUnlocked?.Invoke(levelId);
                OnProgressChanged?.Invoke();
                if (autoSaveAfterEachResult) Save();
            }
        }

        public void MarkLevelCompleted(string levelId)
        {
            if (!completedLevels.Contains(levelId))
            {
                completedLevels.Add(levelId);
            }

            // Unlock next level in sequence
            int idx = Array.IndexOf(LevelOrder, levelId);
            if (idx >= 0 && idx + 1 < LevelOrder.Length)
            {
                UnlockLevel(LevelOrder[idx + 1]);
            }

            SyncProgressionFlagsToPlayerPrefs();
            OnLevelCompleted?.Invoke(levelId);
            OnProgressChanged?.Invoke();
            if (autoSaveAfterEachResult) Save();

            // Automatically update student's playtest export on Desktop after completing a level
            PlaytestDataManager.ExportCurrentStudentData($"level_completed_{levelId}");
        }

        public void SetHasSeenDesktopTour(bool seen)
        {
            HasSeenDesktopTour = seen;
            if (autoSaveAfterEachResult) Save();
        }

        /// <summary>
        /// Returns the best recorded attempt for a level, or null if unplayed.
        /// </summary>
        public LevelScoreRecord GetLevelRecord(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            for (int i = 0; i < levelRecords.Count; i++)
            {
                if (string.Equals(levelRecords[i].levelId, levelId, StringComparison.OrdinalIgnoreCase))
                {
                    return levelRecords[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the best medal achieved for a level.
        /// </summary>
        public LevelMedal GetBestMedal(string levelId)
        {
            LevelScoreRecord record = GetLevelRecord(levelId);
            return record != null ? record.bestMedal : LevelMedal.None;
        }

        /// <summary>
        /// Calculates the medal achieved based on additive points relative to the level's maximum possible points.
        /// Bronze >= 40%, Silver >= 65%, Gold >= 85%.
        /// </summary>
        public static LevelMedal CalculateMedalFromPoints(int currentPoints, int maxPossiblePoints)
        {
            if (maxPossiblePoints <= 0) return LevelMedal.Gold;
            float ratio = (float)Mathf.Max(0, currentPoints) / maxPossiblePoints;
            if (ratio >= 0.85f) return LevelMedal.Gold;
            if (ratio >= 0.65f) return LevelMedal.Silver;
            if (ratio >= 0.40f) return LevelMedal.Bronze;
            return LevelMedal.None;
        }

        /// <summary>
        /// Calculates the medal achieved based on accuracy and mistakes.
        /// </summary>
        public static LevelMedal CalculateMedal(float accuracyPercentage, int mistakesCount)
        {
            if (accuracyPercentage >= 90f && mistakesCount <= 1) return LevelMedal.Gold;
            if (accuracyPercentage >= 75f && mistakesCount <= 4) return LevelMedal.Silver;
            if (accuracyPercentage >= 40f) return LevelMedal.Bronze;
            return LevelMedal.None;
        }

        /// <summary>
        /// Records a level attempt, updating the stored record only if this attempt is superior.
        /// </summary>
        public LevelScoreRecord RecordLevelAttempt(string levelId, float accuracyScore, float timeSeconds, int mistakesCount = 0, int pointsEarned = 0)
        {
            LevelMedal earnedMedal = CalculateMedal(accuracyScore, mistakesCount);
            return RecordLevelAttempt(levelId, earnedMedal, accuracyScore, timeSeconds, mistakesCount, pointsEarned);
        }

        /// <summary>
        /// Records a level attempt with an explicit medal, preserving the best attempt and logging the individual attempt.
        /// </summary>
        public LevelScoreRecord RecordLevelAttempt(string levelId, LevelMedal earnedMedal, float accuracyScore, float timeSeconds, int mistakesCount = 0, int pointsEarned = 0)
        {
            if (string.IsNullOrEmpty(levelId)) return null;

            // Log individual attempt chronologically
            levelAttemptsHistory.Add(new LevelAttemptLogEntry
            {
                attemptNumber = levelAttemptsHistory.Count + 1,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                levelId = levelId,
                levelName = UIDialogs.Current.GetLevelDisplayName(levelId, levelId),
                medal = earnedMedal.ToString(),
                accuracyPercent = (float)Math.Round(accuracyScore, 1),
                durationSeconds = (float)Math.Round(timeSeconds, 2),
                mistakes = mistakesCount
            });

            LevelScoreRecord record = GetLevelRecord(levelId);
            bool isBetter = false;

            if (record == null)
            {
                record = new LevelScoreRecord(levelId, earnedMedal, accuracyScore, timeSeconds, mistakesCount);
                record.bestPoints = Mathf.Max(0, pointsEarned);
                levelRecords.Add(record);
                isBetter = true;
            }
            else
            {
                record.attemptsCount++;
                record.lastCompletedTicks = DateTime.UtcNow.Ticks;

                // Determine if this run was superior to the previous best
                if ((int)earnedMedal > (int)record.bestMedal)
                {
                    isBetter = true;
                }
                else if (earnedMedal == record.bestMedal)
                {
                    if (pointsEarned > record.bestPoints || accuracyScore > record.bestScore + 0.1f)
                    {
                        isBetter = true;
                    }
                    else if (Mathf.Abs(accuracyScore - record.bestScore) <= 0.1f)
                    {
                        if (record.bestTimeSeconds <= 0f || (timeSeconds > 0f && timeSeconds < record.bestTimeSeconds))
                        {
                            isBetter = true;
                        }
                    }
                }

                if (pointsEarned > record.bestPoints)
                {
                    record.bestPoints = pointsEarned;
                }

                if (isBetter)
                {
                    record.bestMedal = earnedMedal;
                    record.bestScore = accuracyScore;
                    record.bestTimeSeconds = timeSeconds;
                    record.bestMistakes = mistakesCount;
                }
            }

            OnProgressChanged?.Invoke();
            if (autoSaveAfterEachResult) Save();

            return record;
        }

        /// <summary>
        /// Adds a TaskResult to the history.
        /// Saves immediately if autoSaveAfterEachResult is enabled.
        /// </summary>
        public void RecordResult(TaskResult result)
        {
            if (result == null) return;
            taskHistory.Add(result);
            if (autoSaveAfterEachResult) Save();
        }

        /// <summary>Returns all results recorded for a specific skill id.</summary>
        public List<TaskResult> GetResultsForSkill(string skillId)
        {
            var results = new List<TaskResult>();
            foreach (TaskResult r in taskHistory)
                if (r.skillId == skillId) results.Add(r);
            return results;
        }

        /// <summary>
        /// Returns the historical success rate (0–1) for a skill across all recorded results.
        /// Returns 0 if no results exist for this skill.
        /// </summary>
        public float GetHistoricalSuccessRate(string skillId)
        {
            int total = 0, successes = 0;
            foreach (TaskResult r in taskHistory)
            {
                if (r.skillId != skillId) continue;
                total++;
                if (r.success) successes++;
            }
            return total > 0 ? (float)successes / total : 0f;
        }

        /// <summary>
        /// Returns an ordered list of scores over time for one skill,
        /// reconstructed from the task history. Useful for progress charts.
        /// </summary>
        public List<float> GetScoreHistory(string skillId, float startingScore = 0f)
        {
            var scores = new List<float>();
            float running = startingScore;
            foreach (TaskResult r in taskHistory)
            {
                if (r.skillId != skillId) continue;
                running = Mathf.Clamp(running + r.scoreChange, 0f, 100f);
                scores.Add(running);
            }
            return scores;
        }

        /// <summary>Saves the full task history to PlayerPrefs as JSON.</summary>
        public void Save()
        {
            try
            {
                SyncProgressionFlagsToPlayerPrefs();
                var wrapper = new ProgressDataWrapper
                {
                    results = taskHistory,
                    unlockedLevels = this.unlockedLevels,
                    completedLevels = this.completedLevels,
                    hasSeenDesktopTour = this.HasSeenDesktopTour,
                    unlockAllLevels = this.unlockAllLevels,
                    levelRecords = this.levelRecords,
                    levelAttemptsHistory = this.levelAttemptsHistory
                };
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressData] Save failed: {e.Message}");
            }
        }

        /// <summary>Loads the task history from PlayerPrefs.</summary>
        public void Load()
        {
            taskHistory.Clear();
            unlockedLevels = new List<string> { "level1" };
            completedLevels.Clear();
            levelRecords.Clear();
            levelAttemptsHistory.Clear();
            HasSeenDesktopTour = false;
            unlockAllLevels = false;

            if (!PlayerPrefs.HasKey(SaveKey)) return;

            try
            {
                string json = PlayerPrefs.GetString(SaveKey);
                var wrapper = JsonUtility.FromJson<ProgressDataWrapper>(json);
                if (wrapper != null) unlockAllLevels = wrapper.unlockAllLevels;
                if (wrapper?.results != null) taskHistory = wrapper.results;
                if (wrapper?.unlockedLevels != null && wrapper.unlockedLevels.Count > 0)
                    unlockedLevels = wrapper.unlockedLevels;
                if (wrapper?.completedLevels != null && wrapper.completedLevels.Count > 0)
                    completedLevels = wrapper.completedLevels;
                if (wrapper != null) HasSeenDesktopTour = wrapper.hasSeenDesktopTour;
                if (wrapper?.levelRecords != null) levelRecords = wrapper.levelRecords;
                if (wrapper?.levelAttemptsHistory != null) levelAttemptsHistory = wrapper.levelAttemptsHistory;

                if (!unlockedLevels.Contains("level1"))
                {
                    unlockedLevels.Add("level1");
                }

                // Ensure the immediate successor of every genuinely completed level is unlocked
                if (completedLevels != null)
                {
                    for (int i = 0; i < completedLevels.Count; i++)
                    {
                        string completedId = completedLevels[i];
                        if (!unlockedLevels.Contains(completedId))
                        {
                            unlockedLevels.Add(completedId);
                        }

                        int idx = Array.IndexOf(LevelOrder, completedId);
                        if (idx >= 0 && idx + 1 < LevelOrder.Length)
                        {
                            string nextId = LevelOrder[idx + 1];
                            if (!unlockedLevels.Contains(nextId))
                            {
                                unlockedLevels.Add(nextId);
                            }
                        }
                    }
                }

                if (unlockAllLevels)
                {
                    for (int i = 0; i < LevelOrder.Length; i++)
                    {
                        string id = LevelOrder[i];
                        if (!unlockedLevels.Contains(id)) unlockedLevels.Add(id);
                    }
                }

                SyncProgressionFlagsToPlayerPrefs();
                PlayerPrefs.Save();

                Debug.Log($"[ProgressData] Loaded {taskHistory.Count} task results and {levelRecords.Count} level records.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressData] Load failed: {e.Message}");
            }
        }

        public static void GetSavedDataSnapshot(
            out IReadOnlyList<TaskResult> outTasks,
            out List<string> outCompletedLevels,
            out List<LevelScoreRecord> outLevelRecords,
            out IReadOnlyList<LevelAttemptLogEntry> outAttemptsHistory)
        {
            ProgressData existing = ExistingInstance;
            if (existing != null)
            {
                outTasks = existing.taskHistory;
                outCompletedLevels = existing.completedLevels;
                outLevelRecords = existing.levelRecords;
                outAttemptsHistory = existing.levelAttemptsHistory;
                return;
            }

            var tasks = new List<TaskResult>();
            var completed = new List<string>();
            var records = new List<LevelScoreRecord>();
            var attempts = new List<LevelAttemptLogEntry>();

            if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SaveKey);
                    var wrapper = JsonUtility.FromJson<ProgressDataWrapper>(json);
                    if (wrapper?.results != null) tasks = wrapper.results;
                    if (wrapper?.completedLevels != null) completed = wrapper.completedLevels;
                    if (wrapper?.levelRecords != null) records = wrapper.levelRecords;
                    if (wrapper?.levelAttemptsHistory != null) attempts = wrapper.levelAttemptsHistory;
                }
                catch (Exception) { }
            }

            outTasks = tasks;
            outCompletedLevels = completed;
            outLevelRecords = records;
            outAttemptsHistory = attempts;
        }

        /// <summary>
        /// Erases all stored history from memory and PlayerPrefs.
        /// Use when starting a new player profile.
        /// </summary>
        public void ClearHistory()
        {
            taskHistory.Clear();
            unlockedLevels = new List<string> { "level1" };
            completedLevels.Clear();
            levelRecords.Clear();
            levelAttemptsHistory.Clear();
            HasSeenDesktopTour = false;
            unlockAllLevels = false;
            PlayerPrefs.DeleteKey("Session_PendingAssessment");
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
            Debug.Log("[ProgressData] History cleared.");
        }
        #endregion

        #region Serialization Helper
        // JsonUtility cannot serialize a root List<T>, so we wrap it.
        [Serializable]
        private class ProgressDataWrapper
        {
            public List<TaskResult> results;
            public List<string> unlockedLevels;
            public List<string> completedLevels;
            public bool hasSeenDesktopTour;
            public bool unlockAllLevels;
            public List<LevelScoreRecord> levelRecords;
            public List<LevelAttemptLogEntry> levelAttemptsHistory;
        }
        #endregion
    }
}
