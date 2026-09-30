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
        public static ProgressData Instance { get; private set; }

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
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Always save when the application closes
        private void OnApplicationQuit() { Save(); }
        #endregion

        #region Public Methods
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
                var wrapper = new ProgressDataWrapper { results = taskHistory };
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
        }
        #endregion
    }
}
