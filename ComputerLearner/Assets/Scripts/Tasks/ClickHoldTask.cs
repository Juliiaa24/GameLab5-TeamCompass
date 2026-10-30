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

        protected override void SetupTask(TaskDefinition definition)
        {
            TaskTargetMovement mover = GetComponent<TaskTargetMovement>();
            if (mover == null) mover = gameObject.AddComponent<TaskTargetMovement>();
            mover.Setup(definition);
            mover.enabled = false; // Disable movement entirely because holding while moving is too hard

            if (definition != null)
            {
                requiredHoldTime = 0.5f + (definition.difficulty * 0.2f); // Max 1.5s on difficulty 5
            }
            isHolding = false;
            currentHoldTime = 0f;
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
            if (fillImage != null) fillImage.fillAmount = 0f;
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
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
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdHoverStagnant, transform as RectTransform, 3.0f);
                }
            }

            if (!isHolding && !hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 5.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdInactivityReminder, transform as RectTransform, 3.0f);
            }

            if (isHolding)
            {
                currentHoldTime += Time.deltaTime;
                NotifyUserInteraction();

                float progress = Mathf.Clamp01(currentHoldTime / Mathf.Max(0.1f, requiredHoldTime));
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                }
                NotifySubTaskProgress(progress);

                if (currentHoldTime >= requiredHoldTime)
                {
                    isHolding = false;
                    Debug.Log("[ClickHoldTask] Hold complete!");
                    Complete(true);
                }
            }
            else
            {
                if (fillImage != null && fillImage.fillAmount > 0f)
                {
                    fillImage.fillAmount = 0f;
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
