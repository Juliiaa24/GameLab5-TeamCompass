using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class RightClickTask : BaseTask, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool isHovering;
        private float hoverDwellTime;
        private bool hasWarnedHoverStagnant;
        private TaskTargetMovement targetMover;

        protected override void SetupTask(TaskDefinition definition)
        {
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
            targetMover = GetComponent<TaskTargetMovement>();
            if (targetMover == null) targetMover = gameObject.AddComponent<TaskTargetMovement>();
            targetMover.Setup(definition);
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        protected override void ShowTaskInactivityReminder()
        {
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.rightClickReminder, transform as RectTransform, 3.5f);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHovering)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.rightClickHoverStagnant, transform as RectTransform, 3.2f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsFinished) return;
            isHovering = true;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            hoverDwellTime = 0f;
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.rightClickBackgroundLeftClick, transform as RectTransform, 3.0f);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.rightClickBackgroundRightClick, transform as RectTransform, 3.0f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RectTransform rectTransform = transform as RectTransform;
                TargetMovementType moveType = targetMover != null ? targetMover.MovementType : BaseTask.ResolveTargetMovementType(Definition);
                int scorePoints = BaseTask.CalculatePrecisionAndSizeScore(rectTransform, eventData.position, eventData.pressEventCamera, out bool isOuterRing, moveType);
                int maxScorePoints = BaseTask.CalculateMaxPossiblePrecisionScore(rectTransform, moveType);
                if (isOuterRing && rectTransform != null)
                {
                    VirtualMascot.ShowAlmostOutFeedback(rectTransform);
                }

                Debug.Log("[RightClickTask] Right click detected!");
                Complete(true, transform, scorePoints, maxScorePoints);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                RegisterAttempt(); // Wrong button
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, true);
            }
        }
    }
}
