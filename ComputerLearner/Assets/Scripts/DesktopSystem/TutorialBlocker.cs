using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// A transparent overlay that blocks all clicks EXCEPT those targeting the allowed RectTransform.
    /// Used during the Desktop Tour to prevent the user from clicking random things.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TutorialBlocker : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public static TutorialBlocker Instance { get; private set; }

        private RectTransform allowedTarget;
        private System.Collections.Generic.List<RectTransform> excludedTargets = new System.Collections.Generic.List<RectTransform>();
        private Camera eventCamera;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Give the blocker its own Canvas sorting layer to guarantee it's always above all windows
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            
            gameObject.AddComponent<GraphicRaycaster>();

            // Ensure the image is transparent but solid enough for raycasting
            Image img = GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Completely transparent
            img.raycastTarget = true;

            RectTransform rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void Start()
        {
            // Try to find the original parent Canvas to inherit its camera
            Canvas[] canvases = GetComponentsInParent<Canvas>();
            foreach (Canvas c in canvases)
            {
                // Find the first canvas that isn't the one we just created
                if (c != GetComponent<Canvas>() && c.isRootCanvas)
                {
                    eventCamera = c.worldCamera;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Sets the currently allowed UI element.
        /// </summary>
        public void SetAllowedTarget(RectTransform target)
        {
            allowedTarget = target;
        }

        /// <summary>
        /// Sets a single excluded target (clearing previous ones).
        /// </summary>
        public void SetExcludedTarget(RectTransform target)
        {
            excludedTargets.Clear();
            if (target != null) excludedTargets.Add(target);
        }

        /// <summary>
        /// Adds a UI element that should always be blocked, even if it's inside the allowed target.
        /// </summary>
        public void AddExcludedTarget(RectTransform target)
        {
            if (target != null && !excludedTargets.Contains(target))
                excludedTargets.Add(target);
        }

        /// <summary>
        /// Clears the allowed target and disables the blocker.
        /// </summary>
        public void ClearTarget()
        {
            allowedTarget = null;
            excludedTargets.Clear();
            gameObject.SetActive(false); // Hide entirely if no target
        }

        /// <summary>
        /// ICanvasRaycastFilter implementation.
        /// Returns TRUE if we WANT to block the raycast (it hits the blocker).
        /// Returns FALSE if we want the raycast to pass through (it hits the target).
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            Camera cam = this.eventCamera ?? eventCamera;
            
            // Check exclusion first
            foreach (var excluded in excludedTargets)
            {
                if (excluded != null && RectTransformUtility.RectangleContainsScreenPoint(excluded, sp, cam))
                {
                    return true; // Block!
                }
            }

            if (allowedTarget == null) return true; // Block everything if active but no target

            // Check if the screen point is inside the allowed target
            bool isInsideTarget = RectTransformUtility.RectangleContainsScreenPoint(allowedTarget, sp, cam);

            // If it's inside the target, we do NOT want the blocker to catch the raycast (return false).
            // If it's outside, the blocker catches it (return true).
            return !isInsideTarget;
        }

        // Empty interface implementations to physically CONSUME the events so they don't bubble down
        public void OnPointerClick(PointerEventData eventData) { }
        public void OnPointerDown(PointerEventData eventData) { }
        public void OnPointerUp(PointerEventData eventData) { }
        public void OnDrag(PointerEventData eventData) { }
        public void OnBeginDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }
    }
}

