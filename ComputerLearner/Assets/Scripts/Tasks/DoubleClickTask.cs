using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DoubleClickTask : BaseTask, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private float lastClickTime = -10f;
        private const float DOUBLE_CLICK_THRESHOLD = 0.65f;
        private Coroutine pendingSingleClickErrorCoroutine;
        private bool isHovering;
        private float hoverDwellTime;
        private bool hasWarnedHoverStagnant;

        protected override void SetupTask(TaskDefinition definition)
        {
            lastClickTime = -10f;
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
            TaskTargetMovement mover = GetComponent<TaskTargetMovement>();
            if (mover == null) mover = gameObject.AddComponent<TaskTargetMovement>();
            mover.Setup(definition);
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHovering && lastClickTime < 0f)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickHoverStagnant, transform as RectTransform, 3.0f);
                }
            }

            // Inactivity reminder if player takes > 6s
            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 6.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickInactivityReminder, transform as RectTransform, 3.0f);
            }
        }

        private void OnDisable()
        {
            if (pendingSingleClickErrorCoroutine != null)
            {
                StopCoroutine(pendingSingleClickErrorCoroutine);
                pendingSingleClickErrorCoroutine = null;
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
            if (IsFinished) return;

            // User clicked once inside, but left the target before second click
            if (lastClickTime > 0f)
            {
                if (pendingSingleClickErrorCoroutine != null)
                {
                    StopCoroutine(pendingSingleClickErrorCoroutine);
                    pendingSingleClickErrorCoroutine = null;
                }
                lastClickTime = -10f;
                RegisterAttempt(false);
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickLeftBeforeSecond, transform as RectTransform, 2.8f);
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (pendingSingleClickErrorCoroutine != null)
            {
                StopCoroutine(pendingSingleClickErrorCoroutine);
                pendingSingleClickErrorCoroutine = null;
            }
            lastClickTime = -10f;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickMissedBackground, transform as RectTransform, 2.8f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            NotifyUserInteraction();
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = true;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (pendingSingleClickErrorCoroutine != null)
                {
                    StopCoroutine(pendingSingleClickErrorCoroutine);
                    pendingSingleClickErrorCoroutine = null;
                }
                lastClickTime = -10f;
                RegisterAttempt();
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float now = Time.unscaledTime;
                float timeSinceLastClick = now - lastClickTime;

                if (eventData.clickCount >= 2 || (lastClickTime > 0f && timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD))
                {
                    if (pendingSingleClickErrorCoroutine != null)
                    {
                        StopCoroutine(pendingSingleClickErrorCoroutine);
                        pendingSingleClickErrorCoroutine = null;
                    }
                    lastClickTime = -10f;
                    Debug.Log("[DoubleClickTask] Double click detected!");
                    Complete(true);
                }
                else
                {
                    lastClickTime = now;
                    FeedbackManager.PlayClick();

                    if (pendingSingleClickErrorCoroutine != null)
                    {
                        StopCoroutine(pendingSingleClickErrorCoroutine);
                    }
                    pendingSingleClickErrorCoroutine = StartCoroutine(WaitAndReportSingleClickError());
                }
            }
        }

        private IEnumerator WaitAndReportSingleClickError()
        {
            yield return new WaitForSecondsRealtime(DOUBLE_CLICK_THRESHOLD);
            pendingSingleClickErrorCoroutine = null;

            if (IsFinished) yield break;

            lastClickTime = -10f;
            RegisterAttempt();
            var fb = VirtualMascot.FeedbackDialogs;
            string msg = MascotFeedbackDialogs.PickRandom(fb.doubleClickSingleClickMessages, fb.doubleClickFast);
            VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
        }
    }
}
