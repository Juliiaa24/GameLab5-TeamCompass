/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Orchestrates a dynamic adaptive learning session.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Orchestrates a learning session:
    ///   1. Selects skills adaptively using weighted random selection
    ///   2. Gets the appropriate difficulty from SkillManager
    ///   3. Asks TaskManager to select and spawn a task
    ///   4. Listens for the task result
    ///   5. Updates SkillManager and ProgressData
    ///   6. Decides whether to spawn another task or end the session
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class LearningSessionManager : MonoBehaviour
    {
        #region Public Variables
        public static LearningSessionManager Instance { get; private set; }

        /// <summary>True while a session is running.</summary>
        public bool IsSessionActive { get; private set; }

        /// <summary>How many tasks have been completed in the current session.</summary>
        public int TasksCompleted { get; private set; }

        /// <summary>Fired when a new session starts.</summary>
        public event Action OnSessionStarted;

        /// <summary>Fired when the session ends (either by reaching a limit or manually).</summary>
        public event Action OnSessionEnded;

        /// <summary>Fired after each task result is processed. Useful for UI updates.</summary>
        public event Action<TaskResult> OnResultProcessed;
        #endregion

        #region Private Variables
        [Header("Session Configuration")]
        [SerializeField] private SessionConfig sessionConfig = new SessionConfig();

        [Header("Content Area")]
        [Tooltip("The RectTransform that task prefabs are spawned into. " +
                 "Can also be set at runtime via SetContentArea().")]
        [SerializeField] private RectTransform taskContentArea;

        private BaseTask currentTask;
        private float    sessionStartTime;

        // Skills practiced in this session (for minDistinctSkills check)
        private readonly HashSet<string> practicedSkillsThisSession = new HashSet<string>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the RectTransform where task prefabs are spawned.
        /// Call this before StartSession() if you cannot assign it in the Inspector.
        /// </summary>
        public void SetContentArea(RectTransform area) { taskContentArea = area; }

        /// <summary>Starts a new adaptive learning session.</summary>
        public void StartSession()
        {
            if (IsSessionActive)
            {
                Debug.LogWarning("[LearningSessionManager] Session already active — ignoring StartSession().");
                return;
            }

            IsSessionActive  = true;
            TasksCompleted   = 0;
            sessionStartTime = Time.time;
            practicedSkillsThisSession.Clear();

            // Configure TaskManager for this session
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.ClearHistory();
                TaskManager.Instance.SetAntiRepetitionWindow(sessionConfig.antiRepetitionWindowSize);
            }

            Debug.Log("[LearningSessionManager] ──── Session Started ────");
            OnSessionStarted?.Invoke();
            SelectNextTask();
        }

        /// <summary>Ends the current session early (e.g. player exits the app).</summary>
        public void EndSession()
        {
            if (!IsSessionActive) return;
            IsSessionActive = false;

            // Clean up any running task
            if (currentTask != null)
            {
                currentTask.OnTaskCompleted -= HandleTaskCompleted;
                Destroy(currentTask.gameObject);
                currentTask = null;
            }

            Debug.Log($"[LearningSessionManager] ──── Session Ended ──── " +
                      $"tasks={TasksCompleted} skills={practicedSkillsThisSession.Count}");
            OnSessionEnded?.Invoke();
        }
        #endregion

        #region Private Methods
        private void SelectNextTask()
        {
            if (!IsSessionActive) return;

            // Check end conditions before selecting another task
            if (ShouldEndSession()) { EndSession(); return; }

            SkillManager sm = SkillManager.Instance;
            TaskManager  tm = TaskManager.Instance;

            if (sm == null) { Debug.LogError("[LearningSessionManager] SkillManager not found!"); return; }
            if (tm == null) { Debug.LogError("[LearningSessionManager] TaskManager not found!"); return; }

            // ── 1. Adaptive skill selection ────────────────────────────────
            string skillId = SelectSkillAdaptively(sm);
            if (skillId == null)
            {
                Debug.LogError("[LearningSessionManager] No skills registered in SkillManager.");
                EndSession();
                return;
            }

            // ── 2. Difficulty from current skill score ─────────────────────
            int difficulty = sm.GetDifficulty(skillId);

            // ── 3. Task selection ──────────────────────────────────────────
            TaskDefinition def = tm.SelectTask(skillId, difficulty);

            // Fallback: try any skill if the selected one has no available tasks
            if (def == null)
            {
                Debug.LogWarning($"[LearningSessionManager] No task for '{skillId}' D{difficulty}, trying fallback skill.");
                string fallbackSkill = SelectAnySkill(sm);
                if (fallbackSkill != null) def = tm.SelectTask(fallbackSkill, sm.GetDifficulty(fallbackSkill));
            }

            if (def == null)
            {
                Debug.LogError("[LearningSessionManager] No tasks available at all — ending session.");
                EndSession();
                return;
            }

            // ── 4. Spawn task ──────────────────────────────────────────────
            currentTask = tm.SpawnTask(def, taskContentArea);
            if (currentTask == null) return;

            currentTask.OnTaskCompleted += HandleTaskCompleted;
            practicedSkillsThisSession.Add(def.primarySkill?.skillId ?? skillId);

            Debug.Log($"[LearningSessionManager] Task #{TasksCompleted + 1}: " +
                      $"'{def.displayName}' | skill='{skillId}' | D{difficulty}");
        }

        private void HandleTaskCompleted(TaskResult result)
        {
            // Detach listener and destroy the task GameObject
            if (currentTask != null)
            {
                currentTask.OnTaskCompleted -= HandleTaskCompleted;
                Destroy(currentTask.gameObject);
                currentTask = null;
            }

            TasksCompleted++;

            // Update skill score
            SkillManager.Instance?.ApplyResult(result);

            // Persist to history
            ProgressData.Instance?.RecordResult(result);

            OnResultProcessed?.Invoke(result);

            // Queue the next task (or end the session)
            SelectNextTask();
        }

        /// <summary>Returns true when at least one session-end condition is met.</summary>
        private bool ShouldEndSession()
        {
            // Max tasks reached
            if (sessionConfig.maxTasks > 0 && TasksCompleted >= sessionConfig.maxTasks)
            {
                Debug.Log("[LearningSessionManager] Max tasks reached.");
                return true;
            }

            // Time limit reached — only allowed if minimum distinct skills have been practiced
            if (sessionConfig.maxDurationSeconds > 0)
            {
                float elapsed     = Time.time - sessionStartTime;
                bool  minSkillsMet = practicedSkillsThisSession.Count >= sessionConfig.minDistinctSkills;
                if (elapsed >= sessionConfig.maxDurationSeconds && minSkillsMet)
                {
                    Debug.Log("[LearningSessionManager] Time limit reached (min skills met).");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Weighted roulette-wheel selection.
        /// Formula: weight = (101 - score) ^ exponent  (see DifficultyConfig)
        /// Weak skills get higher weight → selected more often.
        /// Even a score-100 skill gets weight 1 for occasional review.
        /// </summary>
        private string SelectSkillAdaptively(SkillManager sm)
        {
            Dictionary<string, float> weights = sm.GetSkillWeights();
            if (weights.Count == 0) return null;

            float total = 0f;
            foreach (float w in weights.Values) total += w;

            float r = UnityEngine.Random.value * total;
            foreach (var pair in weights)
            {
                r -= pair.Value;
                if (r <= 0f) return pair.Key;
            }

            // Floating-point safety: return the last entry
            string last = null;
            foreach (string key in weights.Keys) last = key;
            return last;
        }

        /// <summary>Returns any registered skill id (used as a final fallback).</summary>
        private string SelectAnySkill(SkillManager sm)
        {
            foreach (string key in sm.AllStates.Keys) return key;
            return null;
        }
        #endregion
    }
}
