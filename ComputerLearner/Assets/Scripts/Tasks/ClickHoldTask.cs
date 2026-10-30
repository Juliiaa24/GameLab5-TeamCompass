using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ClickHoldTask : BaseTask, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float requiredHoldTime = 1.5f;
        public UnityEngine.UI.Image fillImage;
        private bool isHolding = false;
        private float currentHoldTime = 0f;

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
            if (fillImage != null) fillImage.fillAmount = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHolding)
            {
                currentHoldTime += Time.deltaTime;
                NotifyUserInteraction();
                
                if (fillImage != null)
                {
                    fillImage.fillAmount = Mathf.Clamp01(currentHoldTime / requiredHoldTime);
                }

                if (currentHoldTime >= requiredHoldTime)
                {
                    isHolding = false;
                    Debug.Log("[ClickHoldTask] Hold complete!");
                    Complete(true);
                }
            }
            else
            {
                // Slowly decay the fill if not holding? Or instantly reset. Let's instantly reset.
                if (fillImage != null && fillImage.fillAmount > 0f)
                {
                    fillImage.fillAmount = 0f;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsFinished) return;
            
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            isHolding = true;
            currentHoldTime = 0f;
            NotifyUserInteraction();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsFinished || eventData.button != PointerEventData.InputButton.Left) return;
            
            if (isHolding)
            {
                isHolding = false;
                if (currentHoldTime < requiredHoldTime)
                {
                    RegisterAttempt();
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdKeepPressing, transform as RectTransform, 2.5f);
                }
                currentHoldTime = 0f;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // If mouse leaves the target while holding, cancel it
            if (isHolding)
            {
                isHolding = false;
                currentHoldTime = 0f;
                RegisterAttempt();
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdStayInside, transform as RectTransform, 2.5f);
            }
        }
    }
}
