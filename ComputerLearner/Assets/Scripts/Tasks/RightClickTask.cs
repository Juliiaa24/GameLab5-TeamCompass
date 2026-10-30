using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class RightClickTask : BaseTask, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool isHovering;
        private float hoverDwellTime;
        private bool hasWarnedHoverStagnant;

        protected override void SetupTask(TaskDefinition definition)
        {
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

            if (isHovering)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback("You're right on target! Now press the RIGHT mouse button (with your middle finger)!", transform as RectTransform, 3.0f);
                }
            }

            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 5.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback("Move inside the circle and click the RIGHT mouse button!", transform as RectTransform, 3.0f);
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
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                VirtualMascot.ShowContextualFeedback("Careful! Aim inside the target circle and use the RIGHT mouse button!", transform as RectTransform, 3.0f);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback("Good button! Now aim inside the target circle and right-click!", transform as RectTransform, 3.0f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RectTransform rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    Camera eventCamera = eventData.pressEventCamera;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventCamera, out Vector2 localPoint))
                    {
                        float radius = Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
                        if (radius > 1f && (localPoint.magnitude / radius) >= 0.75f)
                        {
                            VirtualMascot.ShowAlmostOutFeedback(rectTransform);
                        }
                    }
                }

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
