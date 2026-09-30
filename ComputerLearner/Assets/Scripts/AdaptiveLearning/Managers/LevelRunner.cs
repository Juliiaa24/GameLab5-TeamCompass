using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Executes a specific LevelDefinition (sequence of tasks) inside a Window.
    /// Replaces the old Level1/Level2 hardcoded classes.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The content area inside the window where tasks will be spawned.")]
        public RectTransform taskContentArea;
        
        [Header("State (Debug)")]
        [SerializeField] private LevelDefinition currentLevel;
        [SerializeField] private int currentTaskIndex = 0;
        
        private BaseTask activeTask;
        private Window appWindow;

        private void Awake()
        {
            appWindow = GetComponentInParent<Window>();
        }

        public void StartLevel(LevelDefinition level)
        {
            if (level == null || level.tasks == null || level.tasks.Count == 0)
            {
                Debug.LogError("[LevelRunner] Invalid LevelDefinition provided.");
                return;
            }

            currentLevel = level;
            currentTaskIndex = 0;
            
            if (appWindow != null && !string.IsNullOrEmpty(level.displayName))
            {
                appWindow.gameObject.name = "Window_" + level.displayName;
                // If there's a title text component, we could update it here.
            }

            SpawnNextTask();
        }

        private void SpawnNextTask()
        {
            if (currentLevel == null) return;

            if (currentTaskIndex >= currentLevel.tasks.Count)
            {
                Debug.Log($"[LevelRunner] Level '{currentLevel.displayName}' completed!");
                if (appWindow != null) appWindow.CloseWindow();
                return;
            }

            TaskDefinition def = currentLevel.tasks[currentTaskIndex];
            
            if (TaskManager.Instance == null)
            {
                Debug.LogError("[LevelRunner] TaskManager.Instance is null. Is it in the scene?");
                return;
            }

            activeTask = TaskManager.Instance.SpawnTask(def, taskContentArea);
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted += HandleTaskCompleted;
            }
            else
            {
                Debug.LogError($"[LevelRunner] Failed to spawn task '{def.displayName}'.");
                // Skip to next task if failed to spawn
                currentTaskIndex++;
                SpawnNextTask();
            }
        }

        private void HandleTaskCompleted(TaskResult result)
        {
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
                Destroy(activeTask.gameObject);
                activeTask = null;
            }

            // Apply scores
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ApplyResult(result);
            }
            
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.RecordResult(result);
            }

            // Move to next
            currentTaskIndex++;
            SpawnNextTask();
        }
        
        private void OnDestroy()
        {
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
            }
        }
    }
}
