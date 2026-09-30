/**
 * Author: Diego
 * Date: 30/09/26
 * Description: A hoverable UI target that tracks cursor dwell time.
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// A UI target that fires OnHoverComplete after the player keeps the cursor
    /// over it for the required duration. Optionally moves around the content area.
    ///
    /// This is a child element — it does NOT inherit BaseTask.
    /// The parent HoverTargetTask listens to OnHoverComplete and calls Complete().
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Public Variables
        /// <summary>Fired when the player hovers for the full required duration.</summary>
        public event Action OnHoverComplete;
        #endregion

        #region Private Variables
        [Tooltip("Optional radial fill image showing hover progress. Set fillMethod to Radial360.")]
        [SerializeField] private Image fillProgressImage;

        private float requiredDuration;
        private float hoverTime;
        private bool  isHovering;
        private bool  completed;

        private bool isMoving;
        private float moveSpeed;
        private RectTransform rectTransform;
        private RectTransform parentRect;
        private Vector2 moveDirection;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (parentRect == null) parentRect = transform.parent as RectTransform;
            moveDirection = UnityEngine.Random.insideUnitCircle.normalized;
            if (moveDirection == Vector2.zero) moveDirection = Vector2.right;

            if (fillProgressImage != null) fillProgressImage.fillAmount = 0f;
        }

        private void Update()
        {
            if (completed) return;

            // ── Movement ──────────────────────────────────────────────────
            if (isMoving && parentRect != null)
            {
                Vector2 pos = rectTransform.anchoredPosition;
                pos += moveDirection * (moveSpeed * Time.deltaTime);

                Rect bounds = parentRect.rect;
                float halfW = rectTransform.rect.width  * 0.5f;
                float halfH = rectTransform.rect.height * 0.5f;

                if (pos.x - halfW < bounds.xMin || pos.x + halfW > bounds.xMax)
                {
                    moveDirection.x = -moveDirection.x;
                    pos.x = Mathf.Clamp(pos.x, bounds.xMin + halfW, bounds.xMax - halfW);
                }
                if (pos.y - halfH < bounds.yMin || pos.y + halfH > bounds.yMax)
                {
                    moveDirection.y = -moveDirection.y;
                    pos.y = Mathf.Clamp(pos.y, bounds.yMin + halfH, bounds.yMax - halfH);
                }

                rectTransform.anchoredPosition = pos;
            }

            // ── Hover tracking ────────────────────────────────────────────
            if (isHovering)
            {
                hoverTime += Time.deltaTime;
            }
            else
            {
                // Hover time decays at 2× rate when not hovering
                hoverTime = Mathf.Max(0f, hoverTime - Time.deltaTime * 2f);
            }

            if (fillProgressImage != null)
                fillProgressImage.fillAmount = Mathf.Clamp01(hoverTime / requiredDuration);

            if (hoverTime >= requiredDuration)
            {
                completed = true;
                OnHoverComplete?.Invoke();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>Configure size, required duration, and movement. Call right after spawning.</summary>
        public void Configure(float size, float duration, bool moving, float speed)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(size, size);
            requiredDuration = Mathf.Max(0.1f, duration);
            isMoving  = moving;
            moveSpeed = speed;
        }

        /// <summary>Sets the parent rect used as movement boundary.</summary>
        public void SetParentRect(RectTransform parent)
        {
            parentRect = parent;
        }

        public void OnPointerEnter(PointerEventData eventData) { isHovering = true; }
        public void OnPointerExit(PointerEventData eventData)  { isHovering = false; }
        #endregion
    }
}
