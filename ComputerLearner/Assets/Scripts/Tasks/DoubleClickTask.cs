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

        private static readonly string[] SingleClickFeedback = new string[]
        {
            "That was just one click! Tap the left mouse button twice fast: click-click!",
            "Too slow between clicks! Try tapping the left button two times quickly without moving the mouse!",
            "Almost! Give the left mouse button two quick taps in a row: tap-tap!"
        };

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
                    VirtualMascot.ShowContextualFeedback("You're on the circle! Now click the left button twice super fast: click-click!", transform as RectTransform, 3.0f);
                }
            }

            // Inactivity reminder if player takes > 6s
            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 6.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback("Move inside the circle and double-click quickly with the left button!", transform as RectTransform, 3.0f);
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
                VirtualMascot.ShowContextualFeedback("Oops, keep the mouse steady inside the circle while clicking twice!", transform as RectTransform, 2.8f);
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
                VirtualMascot.ShowContextualFeedback("Missed the circle! Move your cursor inside the target and double-click quickly!", transform as RectTransform, 2.8f);
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
            string msg = SingleClickFeedback[Random.Range(0, SingleClickFeedback.Length)];
            VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
        }
    }
}
