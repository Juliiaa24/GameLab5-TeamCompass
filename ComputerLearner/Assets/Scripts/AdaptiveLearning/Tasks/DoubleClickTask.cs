using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DoubleClickTask : BaseTask, IPointerClickHandler
    {
        protected override void SetupTask(TaskDefinition definition)
        {
            TaskTargetMovement mover = GetComponent<TaskTargetMovement>();
            if (mover == null) mover = gameObject.AddComponent<TaskTargetMovement>();
            mover.Setup(definition);
        }

        private float lastClickTime = 0f;
        private const float DOUBLE_CLICK_THRESHOLD = 1.0f; // 1 second for kids

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                
                if (timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD)
                {
                    Debug.Log("[DoubleClickTask] Double click detected!");
                    Complete(true);
                }
                else
                {
                    lastClickTime = Time.time;
                    RegisterAttempt();
                }
            }
        }
    }
}
