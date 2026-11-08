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
        public virtual float SubTaskProgress => subTaskProgress;
        public event Action<float> OnSubTaskProgressChanged;
        private float subTaskProgress;

        protected void NotifySubTaskProgress(float progress)
        {
            subTaskProgress = Mathf.Clamp01(progress);
            OnSubTaskProgressChanged?.Invoke(subTaskProgress);
        }
        #endregion

        #region Protected Variables
        protected float startTime;
        protected int attempts;
        protected float lastInteractionTime;
        protected bool hasShownInactivityReminder;
        protected bool hasSpawnedCompletionScorePopup;
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            startTime = Time.time;
            lastInteractionTime = Time.time;
            hasShownInactivityReminder = false;
            hasSpawnedCompletionScorePopup = false;
            attempts = 0;
        }

        protected virtual void Update()
        {
            if (IsFinished) return;

            // Check for 7 seconds of player inactivity during the task
            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 7f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowInactivityReminder(GetActiveFeedbackTarget());
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
            hasSpawnedCompletionScorePopup = false;
            attempts = 0;
            subTaskProgress = 0f;
            SetupTask(definition);
        }

        /// <summary>
        /// Calculates adaptive points based on concentric aim precision (distance from target center)
        /// plus a bonus for smaller target sizes.
        /// </summary>
        public static int CalculatePrecisionAndSizeScore(RectTransform targetRect, Vector2 screenPos, Camera eventCamera, out bool isOuterRing)
        {
            isOuterRing = false;
            if (targetRect == null) return 100;

            float ratio = 0f;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, eventCamera, out Vector2 localPoint))
            {
                float radius = Mathf.Max(targetRect.rect.width, targetRect.rect.height) * 0.5f;
                float distFromCenter = (localPoint - targetRect.rect.center).magnitude;
                ratio = radius > 1f ? (distFromCenter / radius) : 0f;
            }

            return CalculateRatioAndSizeScore(targetRect, ratio, out isOuterRing);
        }

        public static int CalculateRatioAndSizeScore(RectTransform targetRect, float ratio, out bool isOuterRing)
        {
            isOuterRing = false;
            int baseScore;
            if (ratio <= 0.35f)
            {
                baseScore = 100; // Center (Bullseye)
            }
            else if (ratio <= 0.70f)
            {
                baseScore = 75; // Mid ring
            }
            else
            {
                baseScore = 45; // Outer ring
                isOuterRing = true;
            }

            int sizeBonus = 0;
            if (targetRect != null)
            {
                float diameter = Mathf.Max(targetRect.rect.width, targetRect.rect.height);
                if (diameter > 0f && diameter < 130f)
                {
                    int rawBonus = Mathf.RoundToInt(Mathf.Clamp01((130f - diameter) / 80f) * 50f);
                    sizeBonus = Mathf.RoundToInt(rawBonus / 5f) * 5;
                }
            }

            return baseScore + sizeBonus;
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
        /// Returns the active target's RectTransform so mascot feedback and floating score popups
        /// appear next to or on the active target rather than the root container.
        /// </summary>
        protected virtual RectTransform GetActiveFeedbackTarget()
        {
            return transform as RectTransform;
        }

        /// <summary>
        /// Returns standardized points inversely proportional to target size:
        /// smaller targets award more points, larger targets award fewer points,
        /// and targets of the same size/config always award the exact same round amount.
        /// </summary>
        protected virtual int GetStandardizedTargetPoints()
        {
            if (Definition == null || Definition.config == null) return 100;

            float size = Definition.config.targetSize;
            bool moving = Definition.config.isMoving;

            int pts;
            if (size <= 0f || size >= 220f)
            {
                pts = 100; // Large targets (e.g. 230, 250, 300)
            }
            else if (size >= 160f)
            {
                pts = 125; // Medium-large targets (e.g. 170, 180, 190, 210)
            }
            else if (size >= 120f)
            {
                pts = 150; // Medium targets (e.g. 120, 130, 150)
            }
            else if (size >= 95f)
            {
                pts = 175; // Small targets (e.g. 100, 110)
            }
            else if (size >= 75f)
            {
                pts = 200; // Very small targets (e.g. 80, 90)
            }
            else
            {
                pts = 225; // Extra-small targets (e.g. 70)
            }

            if (moving)
            {
                pts += 25;
            }

            return pts;
        }

        protected void TriggerHitScoreFeedback(Transform targetTransform = null, int overrideFixedPoints = -1)
        {
            int fixedPts = overrideFixedPoints > 0 ? overrideFixedPoints : GetStandardizedTargetPoints();
            LevelRunner runner = GetComponentInParent<LevelRunner>() ?? LevelRunner.ActiveRunner;
            int pts = runner != null ? runner.AwardHitPoints(fixedPts) : fixedPts;

            Transform anchor = targetTransform != null ? targetTransform : GetActiveFeedbackTarget();
            Vector3 worldPos = anchor != null ? anchor.position : transform.position;
            FloatingScoreText.Spawn(transform, worldPos, pts);
        }

        protected void TriggerMistakeScoreFeedback(Transform targetTransform = null, int penalty = 50)
        {
            LevelRunner runner = GetComponentInParent<LevelRunner>() ?? LevelRunner.ActiveRunner;
            int delta = runner != null ? runner.ApplyMistakePenalty(penalty) : -Mathf.Abs(penalty);

            // Spawn mistake penalty directly above the player's mouse cursor so the target never covers it
            FloatingScoreText.SpawnAtMouseCursor(transform, delta);
        }

        /// <summary>
        /// Generates and records an intermediate result without ending the task.
        /// Useful for tasks with multiple distinct sub-goals (e.g. clicking multiple targets).
        /// </summary>
        protected void RecordIntermediateResult(bool success, Transform feedbackTarget = null, int overrideFixedPoints = -1)
        {
            if (IsFinished) return;

            NotifyUserInteraction();

            if (success)
            {
                FeedbackManager.PlaySuccess();
                TriggerHitScoreFeedback(feedbackTarget, overrideFixedPoints);
            }
            else
            {
                attempts++;
                FeedbackManager.PlayError(true);
                TriggerMistakeScoreFeedback(feedbackTarget);
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

        protected void Complete(bool success, Transform feedbackTarget = null, int overrideFixedPoints = -1)
        {
            if (IsFinished) return;
            IsFinished = true;

            NotifyUserInteraction();

            if (success)
            {
                FeedbackManager.PlaySuccess();
                if (!hasSpawnedCompletionScorePopup)
                {
                    hasSpawnedCompletionScorePopup = true;
                    TriggerHitScoreFeedback(feedbackTarget, overrideFixedPoints);
                }
            }
            else
            {
                FeedbackManager.PlayError(true);
                TriggerMistakeScoreFeedback(feedbackTarget);
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

        public void RegisterAttempt(bool withShake = true, Transform feedbackTarget = null)
        {
            attempts++;
            NotifyUserInteraction();
            FeedbackManager.PlayError(withShake);
            TriggerMistakeScoreFeedback(feedbackTarget);
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
