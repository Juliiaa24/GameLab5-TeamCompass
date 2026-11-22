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
        protected float subTaskStartTime;
        protected float lastProgressTime;
        protected int attempts;
        protected float lastInteractionTime;
        protected float nextInactivityReminderTime;
        protected float nextInactivityPenaltyTime;
        protected bool hasShownInactivityReminder;
        protected bool hasSpawnedCompletionScorePopup;

        protected const float INITIAL_INACTIVITY_REMINDER_DELAY = 5.5f;
        protected const float REPEAT_INACTIVITY_REMINDER_INTERVAL = 7.0f;
        protected const float INITIAL_INACTIVITY_PENALTY_DELAY = 10.0f;
        protected const float REPEAT_INACTIVITY_PENALTY_INTERVAL = 7.0f;
        protected const int INACTIVITY_TIME_PENALTY_POINTS = 15;
        #endregion

        #region Unity Methods
        protected virtual void Start()
        {
            float now = Time.time;
            startTime = now;
            subTaskStartTime = now;
            lastProgressTime = now;
            lastInteractionTime = now;
            nextInactivityReminderTime = now + INITIAL_INACTIVITY_REMINDER_DELAY;
            nextInactivityPenaltyTime = now + INITIAL_INACTIVITY_PENALTY_DELAY;
            hasShownInactivityReminder = false;
            hasSpawnedCompletionScorePopup = false;
            attempts = 0;
        }

        protected virtual void Update()
        {
            if (IsFinished) return;

            if (IsPlayerActivelyProgressing())
            {
                NotifyTaskProgress();
                return;
            }

            float now = Time.time;

            // Periodic inactivity reminder: pops up at ~5.5s and repeats every 7.0s while player has not progressed
            if (now >= nextInactivityReminderTime && (now - lastInteractionTime) >= INITIAL_INACTIVITY_REMINDER_DELAY)
            {
                hasShownInactivityReminder = true;
                nextInactivityReminderTime = now + REPEAT_INACTIVITY_REMINDER_INTERVAL;
                ShowTaskInactivityReminder();
            }

            // Periodic inactivity / time-taken penalty: after 10s without task progress, deducts points every 7s
            if (now >= nextInactivityPenaltyTime && (now - lastProgressTime) >= INITIAL_INACTIVITY_PENALTY_DELAY)
            {
                nextInactivityPenaltyTime = now + REPEAT_INACTIVITY_PENALTY_INTERVAL;
                TriggerInactivityTimePenalty(GetActiveFeedbackTarget(), INACTIVITY_TIME_PENALTY_POINTS);
            }
        }
        #endregion

        #region Public Methods
        public void Initialise(TaskDefinition definition)
        {
            Definition = definition;
            IsFinished = false;
            float now = Time.time;
            startTime = now;
            subTaskStartTime = now;
            lastProgressTime = now;
            lastInteractionTime = now;
            nextInactivityReminderTime = now + INITIAL_INACTIVITY_REMINDER_DELAY;
            nextInactivityPenaltyTime = now + INITIAL_INACTIVITY_PENALTY_DELAY;
            hasShownInactivityReminder = false;
            hasSpawnedCompletionScorePopup = false;
            attempts = 0;
            subTaskProgress = 0f;
            SetupTask(definition);
        }

        /// <summary>
        /// Returns bonus points awarded based on the target's movement mode:
        /// Stationary = +0, Circular = +25, DvdBounce = +50.
        /// </summary>
        public static int GetMovementBonusPoints(TargetMovementType movementType)
        {
            switch (movementType)
            {
                case TargetMovementType.Circular:
                    return 25;
                case TargetMovementType.DvdBounce:
                    return 50;
                case TargetMovementType.Stationary:
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Generates a diagonal direction vector (between 28 and 62 degrees in any quadrant)
        /// so DVD-bouncing targets always travel diagonally like a classic DVD screensaver logo.
        /// </summary>
        public static Vector2 GetRandomDvdDirection()
        {
            float angleRad = UnityEngine.Random.Range(28f, 62f) * Mathf.Deg2Rad;
            float signX = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float signY = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            return new Vector2(Mathf.Cos(angleRad) * signX, Mathf.Sin(angleRad) * signY).normalized;
        }

        /// <summary>
        /// Resolves the movement mode (Stationary, Circular, or DvdBounce) for a given task definition.
        /// </summary>
        public static TargetMovementType ResolveTargetMovementType(TaskDefinition definition, int targetIndex = 0, int totalTargets = 1)
        {
            if (definition == null) return TargetMovementType.Stationary;
            if (definition.config != null && definition.config.isMoving)
            {
                return TargetMovementType.DvdBounce;
            }

            if (definition.difficulty <= 1)
            {
                LevelRunner runner = LevelRunner.ActiveRunner;
                if (runner != null && runner.currentLevel != null &&
                    string.Equals(runner.currentLevel.levelId, "level9", StringComparison.OrdinalIgnoreCase))
                {
                    int cycle = (runner.CurrentTaskIndex + targetIndex) % 3;
                    if (cycle == 1) return TargetMovementType.Circular;
                    if (cycle == 2) return TargetMovementType.DvdBounce;
                }
                return TargetMovementType.Stationary;
            }

            if (definition.difficulty == 2)
            {
                return TargetMovementType.Circular;
            }

            return TargetMovementType.DvdBounce;
        }

        /// <summary>
        /// Calculates adaptive points based on concentric aim precision (distance from target center),
        /// smaller target size bonus, and target movement mode bonus.
        /// </summary>
        public static int CalculatePrecisionAndSizeScore(
            RectTransform targetRect,
            Vector2 screenPos,
            Camera eventCamera,
            out bool isOuterRing,
            TargetMovementType movementType = TargetMovementType.Stationary)
        {
            isOuterRing = false;
            if (targetRect == null) return 100 + GetMovementBonusPoints(movementType);

            float ratio = 0f;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, eventCamera, out Vector2 localPoint))
            {
                float radius = Mathf.Max(targetRect.rect.width, targetRect.rect.height) * 0.5f;
                float distFromCenter = (localPoint - targetRect.rect.center).magnitude;
                ratio = radius > 1f ? (distFromCenter / radius) : 0f;
            }

            return CalculateRatioAndSizeScore(targetRect, ratio, out isOuterRing, movementType);
        }

        public static int CalculateRatioAndSizeScore(
            RectTransform targetRect,
            float ratio,
            out bool isOuterRing,
            TargetMovementType movementType = TargetMovementType.Stationary)
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

            return baseScore + sizeBonus + GetMovementBonusPoints(movementType);
        }

        /// <summary>
        /// Returns the maximum possible precision score (bullseye center + target size bonus + movement bonus) for a given target RectTransform.
        /// </summary>
        public static int CalculateMaxPossiblePrecisionScore(
            RectTransform targetRect,
            TargetMovementType movementType = TargetMovementType.Stationary)
        {
            return CalculateRatioAndSizeScore(targetRect, 0f, out _, movementType);
        }

        /// <summary>
        /// Resets the inactivity reminder timer when the user interacts.
        /// </summary>
        public void NotifyUserInteraction()
        {
            float now = Time.time;
            lastInteractionTime = now;
            nextInactivityReminderTime = now + INITIAL_INACTIVITY_REMINDER_DELAY;
            hasShownInactivityReminder = false;
        }

        /// <summary>
        /// Resets both the interaction timer and the task progress/penalty timer when forward progress occurs.
        /// </summary>
        public void NotifyTaskProgress()
        {
            NotifyUserInteraction();
            float now = Time.time;
            lastProgressTime = now;
            nextInactivityPenaltyTime = now + INITIAL_INACTIVITY_PENALTY_DELAY;
        }
        #endregion

        #region Protected Methods
        protected abstract void SetupTask(TaskDefinition definition);

        /// <summary>
        /// Override in tasks where the player continuously holds or hovers to suppress inactivity reminders during active progress.
        /// </summary>
        protected virtual bool IsPlayerActivelyProgressing()
        {
            return false;
        }

        /// <summary>
        /// Displays the task-specific inactivity reminder via VirtualMascot.
        /// </summary>
        protected virtual void ShowTaskInactivityReminder()
        {
            VirtualMascot.ShowInactivityReminder(GetActiveFeedbackTarget());
        }

        /// <summary>
        /// Grace window in seconds before per-target overtime point decay begins.
        /// </summary>
        protected virtual float GetSubTaskParGraceSeconds()
        {
            return 6.5f;
        }

        /// <summary>
        /// Returns the active target's RectTransform so mascot feedback and floating score popups
        /// appear next to or on the active target rather than the root container.
        /// </summary>
        protected virtual RectTransform GetActiveFeedbackTarget()
        {
            return transform as RectTransform;
        }

        /// <summary>
        /// Returns standardized points inversely proportional to target size plus movement mode bonus.
        /// </summary>
        protected virtual int GetStandardizedTargetPoints()
        {
            if (Definition == null || Definition.config == null) return 100;

            float size = Definition.config.targetSize;
            TargetMovementType moveType = ResolveTargetMovementType(Definition);

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

            pts += GetMovementBonusPoints(moveType);
            return pts;
        }

        protected int ApplySubTaskOvertimeDecay(int earnedPoints, int maxPoints)
        {
            // Skip per-hit overtime decay on micro-spot tasks (MoveMouseTask spots = 15 pts) where periodic inactivity penalty handles delays
            if (maxPoints <= 25) return earnedPoints;

            float elapsed = Mathf.Max(0f, Time.time - subTaskStartTime);
            float grace = Mathf.Max(2.5f, GetSubTaskParGraceSeconds());
            if (elapsed <= grace) return earnedPoints;

            float overtime = elapsed - grace;
            int rawPenalty = Mathf.FloorToInt(overtime / 2.5f) * 5;
            if (rawPenalty <= 0) return earnedPoints;

            int maxAllowedDecay = Mathf.Max(10, Mathf.RoundToInt(earnedPoints * 0.35f / 5f) * 5);
            int clampedDecay = Mathf.Min(rawPenalty, maxAllowedDecay);
            return Mathf.Max(20, earnedPoints - clampedDecay);
        }

        protected void TriggerHitScoreFeedback(Transform targetTransform = null, int overrideFixedPoints = -1, int overrideMaxPoints = -1)
        {
            int rawFixedPts = overrideFixedPoints > 0 ? overrideFixedPoints : GetStandardizedTargetPoints();
            Transform anchor = targetTransform != null ? targetTransform : GetActiveFeedbackTarget();
            int maxPts = overrideMaxPoints > 0
                ? Mathf.Max(rawFixedPts, overrideMaxPoints)
                : rawFixedPts;

            int adjustedPts = ApplySubTaskOvertimeDecay(rawFixedPts, maxPts);
            subTaskStartTime = Time.time;
            NotifyTaskProgress();

            LevelRunner runner = GetComponentInParent<LevelRunner>() ?? LevelRunner.ActiveRunner;
            int pts = runner != null ? runner.AwardHitPoints(adjustedPts, maxPts) : adjustedPts;

            Vector3 worldPos = anchor != null ? anchor.position : transform.position;
            FloatingScoreText.Spawn(transform, worldPos, pts);
        }

        protected void TriggerInactivityTimePenalty(Transform targetTransform = null, int penalty = INACTIVITY_TIME_PENALTY_POINTS)
        {
            LevelRunner runner = GetComponentInParent<LevelRunner>() ?? LevelRunner.ActiveRunner;
            int delta = runner != null ? runner.ApplyTimePenalty(penalty) : -Mathf.Abs(penalty);

            Transform anchor = targetTransform != null ? targetTransform : GetActiveFeedbackTarget();
            Vector3 worldPos = anchor != null ? anchor.position : transform.position;
            FloatingScoreText.Spawn(transform, worldPos, delta);
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
        protected void RecordIntermediateResult(bool success, Transform feedbackTarget = null, int overrideFixedPoints = -1, int overrideMaxPoints = -1)
        {
            if (IsFinished) return;

            NotifyUserInteraction();

            if (success)
            {
                FeedbackManager.PlaySuccess();
                TriggerHitScoreFeedback(feedbackTarget, overrideFixedPoints, overrideMaxPoints);
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

        protected void Complete(bool success, Transform feedbackTarget = null, int overrideFixedPoints = -1, int overrideMaxPoints = -1)
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
                    TriggerHitScoreFeedback(feedbackTarget, overrideFixedPoints, overrideMaxPoints);
                }
            }
            else
            {
                FeedbackManager.PlayError(true);
                TriggerMistakeScoreFeedback(feedbackTarget);
                LevelRunner failRunner = GetComponentInParent<LevelRunner>() ?? LevelRunner.ActiveRunner;
                if (failRunner != null)
                {
                    int missedMax = overrideMaxPoints > 0 ? overrideMaxPoints : GetStandardizedTargetPoints();
                    failRunner.RegisterMissedMaxPoints(missedMax);
                }
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
