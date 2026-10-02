using UnityEngine;

namespace ComputerLearning
{
    public class MoveMouseTask : BaseTask
    {
        [Tooltip("The total distance in pixels the mouse must travel to complete the task.")]
        public float requiredDistance = 2000f;
        
        private float currentDistance = 0f;
        private Vector3 lastMousePosition;
        private bool isTracking = false;

        protected override void SetupTask(TaskDefinition definition)
        {
            if (UnityEngine.InputSystem.Mouse.current != null)
                lastMousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            isTracking = true;
            Debug.Log("[MoveMouseTask] SetupTask called. Required distance: " + requiredDistance);
        }

        protected override void Start()
        {
            base.Start();
            if (UnityEngine.InputSystem.Mouse.current != null)
                lastMousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }

        private void Update()
        {
            if (!isTracking || IsFinished) return;
            if (UnityEngine.InputSystem.Mouse.current == null) return;

            Vector3 currentMousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            float distanceThisFrame = Vector3.Distance(currentMousePosition, lastMousePosition);
            
            if (distanceThisFrame > 0)
            {
                currentDistance += distanceThisFrame;
                lastMousePosition = currentMousePosition;

                if (currentDistance >= requiredDistance)
                {
                    isTracking = false;
                    Debug.Log("[MoveMouseTask] Distance reached! Task complete.");
                    Complete(true);
                }
            }
        }
    }
}
