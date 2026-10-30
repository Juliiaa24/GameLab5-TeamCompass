using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class RightClickTask : BaseTask, IPointerClickHandler
    {
        protected override void SetupTask(TaskDefinition definition)
        {
            TaskTargetMovement mover = GetComponent<TaskTargetMovement>();
            if (mover == null) mover = gameObject.AddComponent<TaskTargetMovement>();
            mover.Setup(definition);
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 5.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback("Click the right mouse button on the target circle!", transform as RectTransform, 2.8f);
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);
            VirtualMascot.ShowContextualFeedback("Aim inside the circle and click the right mouse button!", transform as RectTransform, 2.8f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log("[RightClickTask] Right click detected!");
                Complete(true);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                RegisterAttempt(); // Wrong button
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, true);
            }
        }
    }
}
