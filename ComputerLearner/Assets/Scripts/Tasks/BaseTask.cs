/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Abstract base class for all task prefab components.
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
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            startTime = Time.time;
            attempts  = 0;
        }
        #endregion

        #region Public Methods
        public void Initialise(TaskDefinition definition)
        {
            Definition = definition;
            IsFinished = false;
            SetupTask(definition);
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

            // Fetch level info dynamically
            LevelRunner runner = GetComponentInParent<LevelRunner>();
            if (runner != null && runner.currentLevel != null)
            {
                result.levelId = runner.currentLevel.levelId;
                result.levelName = runner.currentLevel.displayName;
            }

            if (SkillManager.Instance != null) SkillManager.Instance.ApplyResult(result);
            if (ProgressData.Instance != null) ProgressData.Instance.RecordResult(result);
            
            // Reset timer and attempts for the next sub-task
            startTime = Time.time;
            attempts = 0;
        }

        protected void Complete(bool success)
        {
            if (IsFinished) return;
            IsFinished = true;

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
            OnTaskCompleted?.Invoke(null);
        }

        protected void RegisterAttempt()
        {
            attempts++;
        }
        #endregion
    }
}
