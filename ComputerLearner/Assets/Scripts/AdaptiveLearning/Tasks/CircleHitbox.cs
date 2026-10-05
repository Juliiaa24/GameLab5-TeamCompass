using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// Restricts raycasting for UI elements to a circular shape instead of the default rectangular RectTransform.
    /// Extremely useful for round targets, so clicking the transparent corners doesn't trigger a hit.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CircleHitbox : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransform rectTransform = (RectTransform)transform;
            Vector2 localPoint;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out localPoint))
            {
                // Convert local coordinates to a normalized range from -0.5 to 0.5
                Vector2 pivot = rectTransform.pivot;
                Vector2 normalizedLocal = new Vector2(
                    localPoint.x / rectTransform.rect.width + pivot.x,
                    localPoint.y / rectTransform.rect.height + pivot.y
                ) - new Vector2(0.5f, 0.5f);

                // If distance from center is <= 0.5 (meaning sqrMagnitude <= 0.25), it's inside the circle
                return normalizedLocal.sqrMagnitude <= 0.25f;
            }
            return false;
        }
    }
}
