/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Track level progress after task results have reached the skills.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public class LevelManager : MonoBehaviour
    {
        #region Public Variables
        public static LevelManager Instance { get; private set; }
        public Level ActiveLevel { get; private set; }
        public event Action<Level> LevelProgressChanged;
        public event Action<Level> LevelCompleted;
        #endregion

        #region Private Variables
        private readonly Dictionary<LevelID, Level> levels = new Dictionary<LevelID, Level>();
        private readonly HashSet<LevelID> completedLevels = new HashSet<LevelID>();
        private TaskManager taskManager;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (Instance != this) return;
            taskManager = TaskManager.Instance;
            if (taskManager == null) return;
            taskManager.TaskProgressChanged += OnTaskProgress;
            taskManager.TaskCompleted += OnTaskCompleted;
        }

        private void OnDisable()
        {
            if (taskManager == null) return;
            taskManager.TaskProgressChanged -= OnTaskProgress;
            taskManager.TaskCompleted -= OnTaskCompleted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        public void BeginLevel(Level level)
        {
            if (level == null || TaskManager.Instance == null) return;
            if (ActiveLevel != null && ActiveLevel != level) ActiveLevel.CancelLevel();
            levels[level.ID] = level;
            ActiveLevel = level;
            level.BeginLevel();
            LevelProgressChanged?.Invoke(level);
        }

        public bool HasCompletedLevel(LevelID id) { return completedLevels.Contains(id); }

        public void UnregisterLevel(Level level)
        {
            if (levels.TryGetValue(level.ID, out Level registered) && registered == level) levels.Remove(level.ID);
            if (ActiveLevel == level) ActiveLevel = null;
        }
        #endregion

        #region Private Methods
        private bool BelongsToActiveLevel(Task task)
        {
            if (ActiveLevel == null) return false;
            foreach (Task candidate in ActiveLevel.Tasks)
                if (candidate == task) return true;
            return false;
        }

        private void OnTaskProgress(Task task)
        {
            if (BelongsToActiveLevel(task)) LevelProgressChanged?.Invoke(ActiveLevel);
        }

        private void OnTaskCompleted(Task task)
        {
            if (!BelongsToActiveLevel(task) || !ActiveLevel.IsRunning) return;
            ActiveLevel.Evaluate();
            LevelProgressChanged?.Invoke(ActiveLevel);
            if (!ActiveLevel.IsCompleted()) return;
            completedLevels.Add(ActiveLevel.ID);
            LevelCompleted?.Invoke(ActiveLevel);
        }
        #endregion
    }
}
