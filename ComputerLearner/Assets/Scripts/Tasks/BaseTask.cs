/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Abstract base class for all task prefab components.
 *              Enhanced with audio/haptic feedback integration and inactivity reminders.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    public abstract class BaseTask : MonoBehaviour
    {
        #region Public Variables
        public TaskDefinition Definition { get; private set; }
        public bool IsFinished { get; private set; }
        public event Action<TaskResult> OnTaskCompleted;
        #endregion

        #region Protected Variables
        protected float startTime;
        protected int attempts;
        protected float lastInteractionTime;
        protected bool hasShownInactivityReminder;
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            startTime = Time.time;
            lastInteractionTime = Time.time;
            hasShownInactivityReminder = false;
            attempts = 0;
        }

        protected virtual void Update()
        {
            if (IsFinished) return;

            // Check for 7 seconds of player inactivity during the task
            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 7f)
            {
                hasShownInactivityReminder = true;
                RectTransform myRect = GetComponent<RectTransform>();
                VirtualMascot.ShowInactivityReminder(myRect);
            }
        }
        #endregion

        #region Public Methods
        public void Initialise(TaskDefinition definition)
        {
            Definition = definition;
            IsFinished = false;
            lastInteractionTime = Time.time;
            hasShownInactivityReminder = false;
            attempts = 0;
            SetupTask(definition);
        }

        /// <summary>
        /// Resets the 7-second inactivity timer when the user hovers, moves, or interacts.
        /// </summary>
        public void NotifyUserInteraction()
        {
            lastInteractionTime = Time.time;
            hasShownInactivityReminder = false;
        }
        #endregion

        #region Protected Methods
        protected abstract void SetupTask(TaskDefinition definition);

        /// <summary>
        /// Generates and records an intermediate result without ending the task.
        /// Useful for tasks with multiple distinct sub-goals (e.g. clicking multiple targets).
        /// </summary>
        protected void RecordIntermediateResult(bool success)
        {
            if (IsFinished) return;

            NotifyUserInteraction();

            if (success)
            {
                FeedbackManager.PlaySuccess();
            }
            else
            {
                attempts++;
                FeedbackManager.PlayError(true);
            }

            float elapsed  = Time.time - startTime;
            string skillId = Definition?.primarySkill?.skillId ?? "unknown";
            int difficulty = Definition?.difficulty ?? 1;

            float scoreChange = (SkillManager.Instance != null)
                ? SkillManager.Instance.CalculateScoreChange(difficulty, success)
                : (success ? 2f : -1f);

            TaskResult result = new TaskResult(
                Definition?.taskId  ?? "unknown",
                skillId,
                difficulty,
                success,
                success ? 0 : 1,
                elapsed,
                scoreChange
            );

            // Fetch level info dynamically
            LevelRunner runner = GetComponentInParent<LevelRunner>();
            if (runner != null && runner.currentLevel != null)
            {
                result.levelId = runner.currentLevel.levelId;
                result.levelName = runner.currentLevel.displayName;
            }

            if (SkillManager.Instance != null) SkillManager.Instance.ApplyResult(result);
            if (ProgressData.Instance != null) ProgressData.Instance.RecordResult(result);
            
            // Reset sub-task timer (preserve cumulative attempts for final Complete call)
            startTime = Time.time;
        }

        protected void Complete(bool success)
        {
            if (IsFinished) return;
            IsFinished = true;

            NotifyUserInteraction();

            if (success)
            {
                FeedbackManager.PlaySuccess();
            }
            else
            {
                FeedbackManager.PlayError(true);
            }

            float elapsed  = Time.time - startTime;
            string skillId = Definition?.primarySkill?.skillId ?? "unknown";
            int difficulty = Definition?.difficulty ?? 1;

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

            LevelRunner runner = GetComponentInParent<LevelRunner>();
            if (runner != null && runner.currentLevel != null)
            {
                result.levelId = runner.currentLevel.levelId;
                result.levelName = runner.currentLevel.displayName;
            }

            OnTaskCompleted?.Invoke(result);
        }

        protected void FinishWithoutResult()
        {
            if (IsFinished) return;
            IsFinished = true;
            NotifyUserInteraction();
            OnTaskCompleted?.Invoke(null);
        }

        protected void RegisterAttempt()
        {
            attempts++;
            NotifyUserInteraction();
            FeedbackManager.PlayError(true);
        }
        #endregion
    }
}
