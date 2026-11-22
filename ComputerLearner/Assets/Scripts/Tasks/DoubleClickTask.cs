using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DoubleClickTask : BaseTask, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private float lastClickTime = -10f;
        private const float IMMEDIATE_DOUBLE_CLICK_WINDOW = 0.30f;
        private const float DOUBLE_CLICK_THRESHOLD = 0.65f;
        private bool hasValidFirstClick;
        private int firstClickPrecisionScore;
        private bool firstClickWasOuterRing;
        private TaskTargetMovement targetMover;
        private Coroutine pendingSingleClickErrorCoroutine;
        private bool isHovering;
        private float hoverDwellTime;
        private bool hasWarnedHoverStagnant;

        protected override void SetupTask(TaskDefinition definition)
        {
            lastClickTime = -10f;
            hasValidFirstClick = false;
            firstClickPrecisionScore = 100;
            firstClickWasOuterRing = false;
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
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickInactivityReminder, transform as RectTransform, 3.5f);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHovering && !hasValidFirstClick)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickHoverStagnant, transform as RectTransform, 3.2f);
                }
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
            if (hasValidFirstClick)
            {
                if (pendingSingleClickErrorCoroutine != null)
                {
                    StopCoroutine(pendingSingleClickErrorCoroutine);
                    pendingSingleClickErrorCoroutine = null;
                }
                hasValidFirstClick = false;
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
            hasValidFirstClick = false;
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
                hasValidFirstClick = false;
                lastClickTime = -10f;
                RegisterAttempt();
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float now = Time.unscaledTime;
                float timeSinceLastClick = now - lastClickTime;
                RectTransform rt = transform as RectTransform;
                TargetMovementType moveType = targetMover != null ? targetMover.MovementType : BaseTask.ResolveTargetMovementType(Definition);

                if (hasValidFirstClick && timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD)
                {
                    if (pendingSingleClickErrorCoroutine != null)
                    {
                        StopCoroutine(pendingSingleClickErrorCoroutine);
                        pendingSingleClickErrorCoroutine = null;
                    }
                    hasValidFirstClick = false;
                    lastClickTime = -10f;

                    int secondClickPrecisionScore = BaseTask.CalculatePrecisionAndSizeScore(rt, eventData.position, eventData.pressEventCamera, out bool secondClickWasOuterRing, moveType);
                    int maxScorePoints = BaseTask.CalculateMaxPossiblePrecisionScore(rt, moveType);

                    // Combine accuracy from both clicks (center+center = max, mixed = intermediate, outer+outer = lowest)
                    int combinedScore = Mathf.RoundToInt((firstClickPrecisionScore + secondClickPrecisionScore) * 0.5f);

                    // Slight minus if the second click was slightly late (between 0.30s and 0.65s); immediate clicks (<= 0.30s) get full points
                    if (timeSinceLastClick > IMMEDIATE_DOUBLE_CLICK_WINDOW)
                    {
                        float lateRatio = Mathf.Clamp01((timeSinceLastClick - IMMEDIATE_DOUBLE_CLICK_WINDOW) / (DOUBLE_CLICK_THRESHOLD - IMMEDIATE_DOUBLE_CLICK_WINDOW));
                        int latePenalty = Mathf.Clamp(Mathf.RoundToInt((lateRatio * 25f) / 5f) * 5, 5, 25);
                        combinedScore = Mathf.Max(20, combinedScore - latePenalty);
                    }

                    if ((firstClickWasOuterRing || secondClickWasOuterRing) && rt != null)
                    {
                        VirtualMascot.ShowAlmostOutFeedback(rt);
                    }
                    Debug.Log("[DoubleClickTask] Double click detected!");
                    Complete(true, transform, combinedScore, maxScorePoints);
                }
                else
                {
                    hasValidFirstClick = true;
                    lastClickTime = now;
                    firstClickPrecisionScore = BaseTask.CalculatePrecisionAndSizeScore(rt, eventData.position, eventData.pressEventCamera, out firstClickWasOuterRing, moveType);
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

            hasValidFirstClick = false;
            lastClickTime = -10f;
            RegisterAttempt();
            var fb = VirtualMascot.FeedbackDialogs;
            string msg = MascotFeedbackDialogs.PickRandom(fb.doubleClickSingleClickMessages, fb.doubleClickFast);
            VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
        }
    }
}
