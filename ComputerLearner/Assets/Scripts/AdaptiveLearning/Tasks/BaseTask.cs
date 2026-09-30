/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Abstract base class for all task prefab components.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Abstract base class for all task MonoBehaviours.
    ///
    /// How to add a new task type:
    ///   1. Create a class that inherits BaseTask.
    ///   2. Override SetupTask(TaskDefinition) to configure gameplay from the definition's config.
    ///   3. Call Complete(true) when the player succeeds, Complete(false) when they fail.
    ///   4. Call RegisterAttempt() on each incorrect interaction.
    ///   5. Build a prefab with this component and assign it to a TaskDefinition asset.
    ///
    /// BaseTask handles timing, TaskResult creation, and firing the OnTaskCompleted event.
    /// The task should NOT know about SkillManager or LearningSessionManager directly.
    /// </summary>
    public abstract class BaseTask : MonoBehaviour
    {
        #region Public Variables
        /// <summary>The definition that was used to configure this task instance.</summary>
        public TaskDefinition Definition { get; private set; }

        /// <summary>True after Complete() has been called (prevents double-completion).</summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Fired when the task finishes, carrying the full TaskResult.
        /// LearningSessionManager subscribes to this event.
        /// </summary>
        public event Action<TaskResult> OnTaskCompleted;
        #endregion

        #region Private Variables
        private float startTime;
        private int attempts;
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            startTime = Time.time;
            attempts  = 0;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called by TaskManager immediately after spawning this prefab.
        /// Do NOT call this from your subclass — call it from TaskManager only.
        /// </summary>
        public void Initialise(TaskDefinition definition)
        {
            Definition = definition;
            IsFinished = false;
            SetupTask(definition);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Override this in your subclass to configure gameplay from the definition.
        /// Called once, right after Initialise().
        /// </summary>
        protected abstract void SetupTask(TaskDefinition definition);

        /// <summary>
        /// Call this when the task ends (success = true) or times out / fails (success = false).
        /// Creates a TaskResult and fires OnTaskCompleted.
        /// </summary>
        protected void Complete(bool success)
        {
            if (IsFinished) return;
            IsFinished = true;

            float elapsed  = Time.time - startTime;
            string skillId = Definition?.primarySkill?.skillId ?? "unknown";
            int difficulty = Definition?.difficulty ?? 1;

            // Ask SkillManager for the score change without applying it yet.
            // LearningSessionManager will apply it when it receives the event.
            float scoreChange = (SkillManager.Instance != null)
                ? SkillManager.Instance.CalculateScoreChange(difficulty, success)
                : (success ? 2f : -1f);

            TaskResult result = new TaskResult(
                Definition?.taskId  ?? "unknown",
                skillId,
                difficulty,
                success,
                attempts,
                elapsed,
                scoreChange
            );

            Debug.Log($"[Task '{Definition?.displayName}'] " +
                      $"{(success ? "✓ SUCCESS" : "✗ FAILURE")} | " +
                      $"attempts={attempts} | time={elapsed:F2}s | scoreChange={scoreChange:+0;-0}");

            OnTaskCompleted?.Invoke(result);
        }

        /// <summary>
        /// Call this every time the player makes an incorrect interaction.
        /// The count is included in the TaskResult for historical analysis.
        /// </summary>
        protected void RegisterAttempt()
        {
            attempts++;
        }
        #endregion
    }
}
