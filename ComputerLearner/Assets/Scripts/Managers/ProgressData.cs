/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Persists and provides access to the full task history for reporting.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Stores the complete historical record of TaskResults and persists it
    /// between play sessions using PlayerPrefs + JsonUtility.
    ///
    /// This is kept separate from SkillManager so that progress reports and
    /// score-over-time charts can be generated from raw data at any time,
    /// independently of the current SkillState snapshot.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class ProgressData : MonoBehaviour
    {
        #region Public Variables
        private static ProgressData _instance;
        public static ProgressData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ProgressData>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ProgressData");
                        _instance = go.AddComponent<ProgressData>();
                        DontDestroyOnLoad(go);
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
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Always save when the application closes
        private void OnApplicationQuit() { Save(); }
        #endregion

        public static readonly string[] LevelOrder = { "level1", "level2", "level3", "level4", "level5", "level6", "level7", "level8", "level9", "level10" };

        public bool HasSeenDesktopTour { get; private set; }
        private List<string> unlockedLevels = new List<string> { "level1" }; // Level 1 always unlocked
        private List<string> completedLevels = new List<string>();

        #region Public Methods
        public System.Action<string> OnLevelUnlocked;
        public System.Action<string> OnLevelCompleted;
        public System.Action OnProgressChanged;

        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return true;
            if (levelId == "level1") return true;
            if (completedLevels != null && completedLevels.Contains(levelId)) return true;
            if (unlockedLevels != null && unlockedLevels.Contains(levelId)) return true;

            string nextLvl = GetCurrentNextLevelId();
            if (levelId == nextLvl) return true;

            return false;
        }

        public bool IsLevelCompleted(string levelId)
        {
            return completedLevels.Contains(levelId);
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
                OnLevelCompleted?.Invoke(levelId);
            }

            // Unlock next level in sequence
            int idx = Array.IndexOf(LevelOrder, levelId);
            if (idx >= 0 && idx + 1 < LevelOrder.Length)
            {
                UnlockLevel(LevelOrder[idx + 1]);
            }

            OnProgressChanged?.Invoke();
            if (autoSaveAfterEachResult) Save();
        }

        public void SetHasSeenDesktopTour(bool seen)
        {
            HasSeenDesktopTour = seen;
            if (autoSaveAfterEachResult) Save();
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
                var wrapper = new ProgressDataWrapper 
                { 
                    results = taskHistory,
                    unlockedLevels = this.unlockedLevels,
                    completedLevels = this.completedLevels,
                    hasSeenDesktopTour = this.HasSeenDesktopTour
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
            if (!PlayerPrefs.HasKey(SaveKey)) return;

            try
            {
                string json = PlayerPrefs.GetString(SaveKey);
                var wrapper = JsonUtility.FromJson<ProgressDataWrapper>(json);
                if (wrapper?.results != null) taskHistory = wrapper.results;
                if (wrapper?.unlockedLevels != null && wrapper.unlockedLevels.Count > 0) 
                    unlockedLevels = wrapper.unlockedLevels;
                if (wrapper?.completedLevels != null && wrapper.completedLevels.Count > 0)
                    completedLevels = wrapper.completedLevels;
                else
                {
                    // Backward compatibility: infer completed levels from completed tutorial sequences
                    if (PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1)
                    {
                        for (int i = 1; i <= 9; i++)
                        {
                            string id = "level" + i;
                            if (!completedLevels.Contains(id)) completedLevels.Add(id);
                            if (!unlockedLevels.Contains(id)) unlockedLevels.Add(id);
                        }
                        if (!unlockedLevels.Contains("level10")) unlockedLevels.Add("level10");
                    }
                    else if (PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1)
                    {
                        for (int i = 1; i <= 5; i++)
                        {
                            string id = "level" + i;
                            if (!completedLevels.Contains(id)) completedLevels.Add(id);
                            if (!unlockedLevels.Contains(id)) unlockedLevels.Add(id);
                        }
                        if (!unlockedLevels.Contains("level6")) unlockedLevels.Add("level6");
                    }
                }
                if (wrapper != null) HasSeenDesktopTour = wrapper.hasSeenDesktopTour;
                
                Debug.Log($"[ProgressData] Loaded {taskHistory.Count} task results.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressData] Load failed: {e.Message}");
            }
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
            HasSeenDesktopTour = false;
            PlayerPrefs.DeleteKey(SaveKey);
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
        }
        #endregion
    }
}
