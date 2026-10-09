/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Runs a fixed diagnostic task sequence to estimate the child's starting skill scores.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Runs a short, hand-designed sequence of tasks before adaptive learning begins.
    /// Its purpose is diagnostic: "What can the child already do?"
    ///
    /// It is NOT a traditional level or exam.
    /// Results feed directly into SkillManager to set reasonable starting scores.
    ///
    /// Replace or extend this class without touching any other system —
    /// GameFlowController only cares about the OnTestCompleted event.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class InitialTestManager : MonoBehaviour
    {
        #region Public Variables
        public static InitialTestManager Instance { get; private set; }

        /// <summary>True while the initial test is running.</summary>
        public bool IsTestActive { get; private set; }

        /// <summary>Fired when all test tasks have been completed (or the test is skipped).</summary>
        public event Action OnTestCompleted;
        #endregion

        #region Private Variables
        [Tooltip("Ordered list of TaskDefinitions for the initial diagnostic. " +
                 "Include tasks of various difficulties to get a meaningful first estimate.")]
        [SerializeField] private List<TaskDefinition> testSequence;

        [Tooltip("The RectTransform that task prefabs are spawned into.")]
        [SerializeField] private RectTransform taskContentArea;

        private int currentIndex = 0;
        private BaseTask currentTask;
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
        /// <summary>Sets the content area for spawned tasks at runtime.</summary>
        public void SetContentArea(RectTransform area) { taskContentArea = area; }

        /// <summary>Starts the initial diagnostic test sequence.</summary>
        public void StartTest()
        {
            if (IsTestActive)
            {
                Debug.LogWarning("[InitialTestManager] Test already active.");
                return;
            }
            if (testSequence == null || testSequence.Count == 0)
            {
                Debug.LogWarning("[InitialTestManager] No test sequence defined — skipping initial test.");
                FinishTest();
                return;
            }

            IsTestActive  = true;
            currentIndex  = 0;
            Debug.Log($"[InitialTestManager] Starting initial test ({testSequence.Count} tasks).");
            SpawnNextTask();
        }

        /// <summary>
        /// Skips the initial test entirely (e.g. for a returning player who already has scores).
        /// All skills keep their current scores (usually 0 on first run).
        /// </summary>
        public void SkipTest()
        {
            Debug.Log("[InitialTestManager] Initial test skipped.");
            FinishTest();
        }
        #endregion

        #region Private Methods
        private void SpawnNextTask()
        {
            // Advance past any null entries in the list
            while (currentIndex < testSequence.Count && testSequence[currentIndex] == null)
                currentIndex++;

            if (currentIndex >= testSequence.Count) { FinishTest(); return; }

            TaskManager tm = TaskManager.Instance;
            if (tm == null) { Debug.LogError("[InitialTestManager] TaskManager not found!"); FinishTest(); return; }

            TaskDefinition def = testSequence[currentIndex];
            currentTask = tm.SpawnTask(def, taskContentArea);

            if (currentTask == null)
            {
                Debug.LogWarning($"[InitialTestManager] Could not spawn task '{def.displayName}' — skipping.");
                currentIndex++;
                SpawnNextTask();
                return;
            }

            currentTask.OnTaskCompleted += HandleTaskCompleted;
            Debug.Log($"[InitialTestManager] Test task {currentIndex + 1}/{testSequence.Count}: '{def.displayName}'");
        }

        private void HandleTaskCompleted(TaskResult result)
        {
            if (currentTask != null)
            {
                currentTask.OnTaskCompleted -= HandleTaskCompleted;
                Destroy(currentTask.gameObject);
                currentTask = null;
            }

            // Apply result to set initial skill scores
            SkillManager.Instance?.ApplyResult(result);
            ProgressData.Instance?.RecordResult(result);

            currentIndex++;
            SpawnNextTask();
        }

        private void FinishTest()
        {
            IsTestActive = false;
            Debug.Log("[InitialTestManager] Initial test complete.");
            OnTestCompleted?.Invoke();
        }
        #endregion
    }
}
