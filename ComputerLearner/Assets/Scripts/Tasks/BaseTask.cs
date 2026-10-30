/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Abstract base class for all task prefab components.
 *              Enhanced with audio/haptic feedback integration and inactivity reminders.
 */
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public abstract class BaseTask : MonoBehaviour
    {
        #region Public Variables
        public TaskDefinition Definition { get; private set; }
        public bool IsFinished { get; private set; }
        public int Attempts => attempts;
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

        public void RegisterAttempt(bool withShake = true)
        {
            attempts++;
            NotifyUserInteraction();
            FeedbackManager.PlayError(withShake);
        }

        /// <summary>
        /// Ensures an invisible raycast Graphic is present on the root task window container
        /// to intercept clicks/misses occurring outside active target hitboxes.
        /// </summary>
        public void SetupBackgroundClickInterceptor(Action<PointerEventData> onBackgroundClick)
        {
            if (onBackgroundClick == null) return;

            // Target the root container of this task (parent of target if on child, or self)
            Transform container = (transform.parent != null && transform.parent.GetComponent<LevelRunner>() == null)
                ? transform.parent
                : transform;

            Image img = container.GetComponent<Image>();
            if (img == null)
            {
                img = container.gameObject.AddComponent<Image>();
            }
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            TaskBackgroundClickCatcher catcher = container.GetComponent<TaskBackgroundClickCatcher>();
            if (catcher == null)
            {
                catcher = container.gameObject.AddComponent<TaskBackgroundClickCatcher>();
            }
            catcher.OnClickAction = onBackgroundClick;
        }
        #endregion
    }

    /// <summary>
    /// Forwards clicks on the background canvas to the active task.
    /// </summary>
    public class TaskBackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action<PointerEventData> OnClickAction;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickAction?.Invoke(eventData);
        }
    }
}
