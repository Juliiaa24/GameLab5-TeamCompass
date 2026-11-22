using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ClickHoldTask : BaseTask, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public float requiredHoldTime = 1.5f;
        public UnityEngine.UI.Image fillImage;
        private bool isHolding = false;
        private float currentHoldTime = 0f;
        private bool isHovering = false;
        private float hoverDwellTime = 0f;
        private bool hasWarnedHoverStagnant = false;
        private Vector2 lastPointerScreenPos;
        private Camera lastEventCamera;
        private Color fillImageBaseColor = new Color(0.8f, 0.2f, 0.8f, 1f);
        private bool hasCachedFillColor = false;
        private TaskTargetMovement targetMover;

        protected override void SetupTask(TaskDefinition definition)
        {
            targetMover = GetComponent<TaskTargetMovement>();
            if (targetMover == null) targetMover = gameObject.AddComponent<TaskTargetMovement>();

            TargetMovementType holdMoveType = TargetMovementType.Stationary;
            if (definition != null)
            {
                if (definition.difficulty <= 1)
                    holdMoveType = BaseTask.ResolveTargetMovementType(definition);
                else if (definition.difficulty <= 3)
                    holdMoveType = TargetMovementType.Circular;
                else
                    holdMoveType = TargetMovementType.DvdBounce;
            }
            targetMover.SetCircularRadius(18f, 12f);
            targetMover.Setup(definition, holdMoveType, defaultDvdSpeed: 42f);

            if (definition != null)
            {
                requiredHoldTime = 0.5f + (definition.difficulty * 0.2f); // Max 1.5s on difficulty 5
            }
            isHolding = false;
            currentHoldTime = 0f;
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
            if (fillImage != null)
            {
                if (!hasCachedFillColor)
                {
                    fillImageBaseColor = fillImage.color;
                    hasCachedFillColor = true;
                }
                fillImage.fillAmount = 0f;
                fillImage.color = fillImageBaseColor;
            }
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        protected override bool IsPlayerActivelyProgressing()
        {
            return isHolding;
        }

        protected override void ShowTaskInactivityReminder()
        {
            if (!isHolding)
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdInactivityReminder, transform as RectTransform, 3.5f);
            }
        }

        protected override float GetSubTaskParGraceSeconds()
        {
            return 7.5f;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHovering && !isHolding)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdHoverStagnant, transform as RectTransform, 3.2f);
                }
            }

            if (isHolding)
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    lastPointerScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }

                currentHoldTime += Time.deltaTime;
                NotifyTaskProgress();

                float progress = Mathf.Clamp01(currentHoldTime / Mathf.Max(0.1f, requiredHoldTime));
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                    fillImage.color = Color.Lerp(fillImageBaseColor, new Color(0.16f, 0.80f, 0.38f, 1f), progress);
                }
                NotifySubTaskProgress(progress);

                if (currentHoldTime >= requiredHoldTime)
                {
                    isHolding = false;
                    RectTransform rt = transform as RectTransform;
                    TargetMovementType moveType = targetMover != null ? targetMover.MovementType : BaseTask.ResolveTargetMovementType(Definition);
                    int scorePoints = BaseTask.CalculatePrecisionAndSizeScore(rt, lastPointerScreenPos, lastEventCamera, out bool isOuterRing, moveType);
                    int maxScorePoints = BaseTask.CalculateMaxPossiblePrecisionScore(rt, moveType);
                    if (isOuterRing && rt != null)
                    {
                        VirtualMascot.ShowAlmostOutFeedback(rt);
                    }
                    Debug.Log("[ClickHoldTask] Hold complete!");
                    Complete(true, transform, scorePoints, maxScorePoints);
                }
            }
            else
            {
                if (fillImage != null && fillImage.fillAmount > 0f)
                {
                    fillImage.fillAmount = 0f;
                    fillImage.color = fillImageBaseColor;
                }
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdMissedBackground, transform as RectTransform, 3.0f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsFinished) return;
            isHovering = true;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RegisterAttempt(true);
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;

            isHolding = true;
            lastPointerScreenPos = eventData.position;
            lastEventCamera = eventData.pressEventCamera;
            currentHoldTime = 0f;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = true;
            NotifyUserInteraction();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsFinished || eventData.button != PointerEventData.InputButton.Left) return;

            if (isHolding)
            {
                isHolding = false;
                float progress = currentHoldTime / Mathf.Max(0.1f, requiredHoldTime);
                if (currentHoldTime < requiredHoldTime)
                {
                    RegisterAttempt();
                    var fb = VirtualMascot.FeedbackDialogs;
                    string msg;
                    if (currentHoldTime < 0.25f)
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdQuickTapMessages, fb.holdKeepPressing);
                    }
                    else if (progress >= 0.70f)
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdAlmostDoneMessages, fb.holdKeepPressing);
                    }
                    else
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdMidReleaseMessages, fb.holdKeepPressing);
                    }
                    VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
                }
                currentHoldTime = 0f;
                NotifySubTaskProgress(0f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            hoverDwellTime = 0f;

            // If mouse leaves the target while holding, cancel it
            if (isHolding)
            {
                isHolding = false;
                currentHoldTime = 0f;
                NotifySubTaskProgress(0f);
                RegisterAttempt();
                var fb = VirtualMascot.FeedbackDialogs;
                string msg = MascotFeedbackDialogs.PickRandom(fb.holdSlippedOutsideMessages, fb.holdStayInside);
                VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
            }
        }
    }
}
