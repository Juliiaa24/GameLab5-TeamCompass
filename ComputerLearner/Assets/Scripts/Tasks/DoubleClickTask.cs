using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DoubleClickTask : BaseTask, IPointerClickHandler
    {
        private float lastClickTime = -10f;
        private const float DOUBLE_CLICK_THRESHOLD = 0.65f;
        private Coroutine pendingSingleClickErrorCoroutine;

        protected override void SetupTask(TaskDefinition definition)
        {
            lastClickTime = -10f;
            TaskTargetMovement mover = GetComponent<TaskTargetMovement>();
            if (mover == null) mover = gameObject.AddComponent<TaskTargetMovement>();
            mover.Setup(definition);
        }

        private void OnDisable()
        {
            if (pendingSingleClickErrorCoroutine != null)
            {
                StopCoroutine(pendingSingleClickErrorCoroutine);
                pendingSingleClickErrorCoroutine = null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            NotifyUserInteraction();

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
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.doubleClickFast, transform as RectTransform, 2.5f);
        }
    }
}
