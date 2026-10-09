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
            }
        }
    }
}
