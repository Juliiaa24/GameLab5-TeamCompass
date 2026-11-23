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
        [Tooltip("Normalized radius of the circular hitbox relative to the RectTransform width/height (0.5 = full rect, 0.25405407 = 376px circle inside 740px canvas).")]
        [SerializeField] private float radiusRatio = 0.5f;

        /// <summary>
        /// Effective normalized radius (0..0.5) of the visible target circle inside this RectTransform.
        /// </summary>
        public float RadiusRatio => (radiusRatio > 0.01f && radiusRatio <= 0.5f) ? radiusRatio : 0.5f;

        public void SetRadiusRatio(float ratio)
        {
            radiusRatio = Mathf.Clamp(ratio, 0.05f, 0.5f);
        }

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

                float r = RadiusRatio;
                return normalizedLocal.sqrMagnitude <= (r * r);
            }
            return false;
        }
    }
}
