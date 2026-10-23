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
        #endregion

        #region Unity Methods
        protected override void Start()
        {
            base.Start();
            // Ensure background receives clicks so clicking outside the hover target triggers mascot feedback
            Image img = GetComponent<Image>();
            if (img == null) img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished || !timeLimitActive) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeLimit) Complete(false); // Time ran out → failure
        }
        #endregion

        #region Protected Methods
        protected override void SetupTask(TaskDefinition definition)
        {
            TaskConfig cfg = definition.config;

            timeLimit       = cfg.timeLimit;
            timeLimitActive = timeLimit > 0f;
            elapsed         = 0f;
            isTargetMoving  = cfg.isMoving;

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

            // Random position within spawn area
            Rect  area     = spawnArea.rect;
            float halfSize = cfg.targetSize * 0.5f;

            float maxX = area.xMax - halfSize;
            float minX = area.xMin + halfSize;
            float maxY = area.yMax - halfSize;
            float minY = area.yMin + halfSize;

            float x = (maxX > minX) ? Random.Range(minX, maxX) : 0f;
            float y = (maxY > minY) ? Random.Range(minY, maxY) : 0f;

            RectTransform targetRect = target.GetComponent<RectTransform>();
            targetRect.anchoredPosition = new Vector2(x, y);

            RectTransform feedbackAnchor = cfg.isMoving ? spawnArea : targetRect;

            target.OnHoverActive += NotifyUserInteraction;
            target.OnHoverComplete += () => Complete(true);
            target.OnFailedAttempt += () =>
            {
                if (IsFinished) return;
                NotifyUserInteraction();
                FeedbackManager.PlayError(false);
                VirtualMascot.ShowContextualFeedback("Oops! Keep the cursor steady inside the target until the bar fills!", feedbackAnchor, 3.0f);
            };
            target.OnTargetClicked += () =>
            {
                if (IsFinished) return;
                NotifyUserInteraction();
                FeedbackManager.PlayError(false);
                VirtualMascot.ShowContextualFeedback("No need to click! Just hold the mouse cursor over the circle!", feedbackAnchor, 3.0f);
            };
        }
        #endregion

        #region Public Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;
            NotifyUserInteraction();
            FeedbackManager.PlayError(true);

            RectTransform feedbackAnchor = (!isTargetMoving && spawnedTarget != null)
                ? spawnedTarget.GetComponent<RectTransform>()
                : (spawnArea != null ? spawnArea : GetComponent<RectTransform>());

            VirtualMascot.ShowContextualFeedback("Don't click outside! Move your mouse inside the circle and hold it steady!", feedbackAnchor, 3.0f);
        }
        #endregion
    }
}
