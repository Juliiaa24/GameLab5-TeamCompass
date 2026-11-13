/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Task where the player must hover the cursor over a target for a required duration.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Spawns one HoverTarget and succeeds when the player keeps the cursor over it
    /// for the required hover duration.
    ///
    /// Configurable via TaskConfig:
    ///   targetSize            – diameter of the target
    ///   requiredHoverDuration – seconds of continuous hover needed
    ///   isMoving              – whether the target moves
    ///   moveSpeed             – speed of movement
    ///   timeLimit             – total task time limit; 0 = unlimited
    /// </summary>
    public class HoverTargetTask : BaseTask, IPointerClickHandler
    {
        #region Private Variables
        [Tooltip("The HoverTarget prefab to instantiate.")]
        [SerializeField] private HoverTarget targetPrefab;

        [Tooltip("The RectTransform the target is placed inside.")]
        [SerializeField] private RectTransform spawnArea;

        private HoverTarget spawnedTarget;
        private bool isTargetMoving;
        private float timeLimit;
        private float elapsed;
        private bool  timeLimitActive;
        private bool hasShownStartReminder;
        #endregion

        #region Unity Methods
        protected override void Start()
        {
            base.Start();
            hasShownStartReminder = false;
            // Ensure background receives clicks so clicking outside the hover target triggers mascot feedback
            Image img = GetComponent<Image>();
            if (img == null) img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            // Delayed start reminder: took > 5 seconds to place mouse inside target
            if (!hasShownStartReminder && spawnedTarget != null && !spawnedTarget.HasEverHovered && (Time.time - startTime) >= 5.0f)
            {
                hasShownStartReminder = true;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.hoverStartReminder, GetActiveFeedbackTarget(), 3.0f);
            }

            if (!timeLimitActive) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeLimit) Complete(false, GetActiveFeedbackTarget()); // Time ran out → failure
        }
        #endregion

        #region Protected Methods
        protected override RectTransform GetActiveFeedbackTarget()
        {
            if (spawnedTarget != null && spawnedTarget.gameObject.activeInHierarchy)
            {
                return spawnedTarget.GetComponent<RectTransform>();
            }
            return spawnArea != null ? spawnArea : GetComponent<RectTransform>();
        }

        protected override void SetupTask(TaskDefinition definition)
        {
            TaskConfig cfg = definition.config;

            timeLimit       = cfg.timeLimit;
            timeLimitActive = timeLimit > 0f;
            elapsed         = 0f;
            isTargetMoving  = cfg.isMoving;
            hasShownStartReminder = false;

            if (targetPrefab == null)
            {
                Debug.LogError("[HoverTargetTask] targetPrefab is not assigned!");
                return;
            }
            if (spawnArea == null)
            {
                Debug.LogError("[HoverTargetTask] spawnArea is not assigned!");
                return;
            }

            HoverTarget target = Instantiate(targetPrefab, spawnArea, false);
            spawnedTarget = target;
            target.SetParentRect(spawnArea);
            target.Configure(cfg.targetSize, cfg.requiredHoverDuration, cfg.isMoving, cfg.moveSpeed);

            // Random position within spawn area with safe bounds padding
            if (spawnArea.rect.width <= 0f || spawnArea.rect.height <= 0f)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(spawnArea);
            }
            Rect area = spawnArea.rect;
            float padding = (cfg.targetSize * 0.5f) + 24f;

            float minX = area.xMin + padding;
            float maxX = area.xMax - padding;
            float minY = area.yMin + padding;
            float maxY = area.yMax - padding - 22f;

            float x = (maxX > minX) ? Random.Range(minX, maxX) : area.center.x;
            float y = (maxY > minY) ? Random.Range(minY, maxY) : area.center.y;

            RectTransform targetRect = target.GetComponent<RectTransform>();
            targetRect.anchoredPosition = new Vector2(x, y);

            target.OnHoverActive += NotifyUserInteraction;
            target.OnHoverCompleteWithScore += (score) => Complete(true, targetRect, score, BaseTask.CalculateMaxPossiblePrecisionScore(targetRect));
            target.OnFailedAttempt += () =>
            {
                if (IsFinished) return;
                RegisterAttempt(false, targetRect);
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.hoverKeepSteady, targetRect, 3.0f);
            };
            target.OnTargetClicked += () =>
            {
                if (IsFinished) return;
                RegisterAttempt(false, targetRect);
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.hoverNoClick, targetRect, 3.0f);
            };
        }
        #endregion

        #region Public Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;
            RectTransform feedbackAnchor = GetActiveFeedbackTarget();
            RegisterAttempt(true, feedbackAnchor);
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.hoverDontClickOutside, feedbackAnchor, 3.0f);
        }
        #endregion
    }
}
