/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Selects task definitions and spawns task prefabs.
 */
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Selects an appropriate TaskDefinition for a given skill and difficulty,
    /// avoids recent repetitions using a fixed-size history window,
    /// and spawns + initialises the corresponding prefab.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class TaskManager : MonoBehaviour
    {
        #region Public Variables
        public static TaskManager Instance { get; private set; }
        #endregion

        #region Private Variables
        [Tooltip("All TaskDefinition assets the adaptive system can choose from. " +
                 "Drag every task asset into this list in the Inspector.")]
        [SerializeField] private List<TaskDefinition> allTaskDefinitions;

        // Circular buffer of recently used task IDs (anti-repetition)
        private readonly Queue<string> recentTaskIds = new Queue<string>();
        private int antiRepetitionWindowSize = 5;
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
        /// <summary>Sets how many recent task IDs are remembered to avoid repetition.</summary>
        public void SetAntiRepetitionWindow(int size)
        {
            antiRepetitionWindowSize = Mathf.Max(1, size);
        }

        /// <summary>
        /// Selects a TaskDefinition for the given skill and difficulty.
        ///
        /// Selection priority:
        ///   1. Tasks matching skill + difficulty, not in recent history
        ///   2. Tasks matching skill + difficulty (ignores recent history)
        ///   3. Tasks matching skill only (any difficulty), not in recent history
        ///   4. Tasks matching skill only (any difficulty, any history)
        ///
        /// Returns null if no task is found for this skill at all.
        /// </summary>
        public TaskDefinition SelectTask(string skillId, int difficulty)
        {
            var exactMatch   = new List<TaskDefinition>();
            var skillMatch   = new List<TaskDefinition>();

            foreach (TaskDefinition def in allTaskDefinitions)
            {
                if (def == null || def.primarySkill == null || string.IsNullOrEmpty(def.taskId)) continue;
                if (def.primarySkill.skillId != skillId) continue;

                skillMatch.Add(def);
                if (def.difficulty == difficulty) exactMatch.Add(def);
            }

            // Attempt each priority tier
            TaskDefinition selected =
                PickRandom(FilterRecent(exactMatch)) ??
                PickRandom(exactMatch)               ??
                PickRandom(FilterRecent(skillMatch)) ??
                PickRandom(skillMatch);

            if (selected == null)
            {
                Debug.LogWarning($"[TaskManager] No task found for skill='{skillId}' difficulty={difficulty}.");
                return null;
            }

            TrackRecentTask(selected.taskId);
            Debug.Log($"[TaskManager] Selected '{selected.displayName}' " +
                      $"(skill='{skillId}' difficulty={difficulty})");
            return selected;
        }

        /// <summary>
        /// Instantiates the task prefab from a definition, parents it to contentParent,
        /// stretches it to fill the parent, and calls Initialise().
        /// Returns the BaseTask component, or null on error.
        /// </summary>
        public BaseTask SpawnTask(TaskDefinition definition, RectTransform contentParent)
        {
            if (definition == null)
            {
                Debug.LogError("[TaskManager] SpawnTask called with null TaskDefinition.");
                return null;
            }
            if (definition.taskPrefab == null)
            {
                Debug.LogError($"[TaskManager] TaskDefinition '{definition.displayName}' has no taskPrefab assigned.");
                return null;
            }
            if (contentParent == null)
            {
                Debug.LogError("[TaskManager] SpawnTask called with null contentParent.");
                return null;
            }

            GameObject go = Instantiate(definition.taskPrefab, contentParent, false);

            // Stretch to fill parent
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            BaseTask task = go.GetComponent<BaseTask>();
            if (task == null)
            {
                Debug.LogError($"[TaskManager] Prefab '{definition.taskPrefab.name}' has no BaseTask component.");
                Destroy(go);
                return null;
            }

            task.Initialise(definition);
            return task;
        }

        /// <summary>Clears the recent task history. Call at the start of each session.</summary>
        public void ClearHistory()
        {
            recentTaskIds.Clear();
        }
        #endregion

        #region Private Methods
        private List<TaskDefinition> FilterRecent(List<TaskDefinition> source)
        {
            var result = new List<TaskDefinition>(source.Count);
            foreach (TaskDefinition def in source)
                if (!recentTaskIds.Contains(def.taskId)) result.Add(def);
            return result;
        }

        private TaskDefinition PickRandom(List<TaskDefinition> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        private void TrackRecentTask(string taskId)
        {
            if (recentTaskIds.Count >= antiRepetitionWindowSize) recentTaskIds.Dequeue();
            recentTaskIds.Enqueue(taskId);
        }
        #endregion
    }
}
