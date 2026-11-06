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
    public class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region Public Variables
        /// <summary>Fired when the player hovers for the full required duration.</summary>
        public event Action OnHoverComplete;
        
        /// <summary>Fired when the player exits the target before completion.</summary>
        public event Action OnFailedAttempt;

        /// <summary>Fired when the player clicks on the hover target instead of just hovering.</summary>
        public event Action OnTargetClicked;

        /// <summary>Fired while the player is actively hovering.</summary>
        public event Action OnHoverActive;
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

            // ── Movement & Bounds Clamping ────────────────────────────────
            if (parentRect != null && parentRect.rect.width > 0f && parentRect.rect.height > 0f)
            {
                Vector2 pos = rectTransform.anchoredPosition;
                if (isMoving)
                {
                    pos += moveDirection * (moveSpeed * Time.deltaTime);
                }

                Rect bounds = parentRect.rect;
                float marginW = (rectTransform.rect.width  * 0.5f) + 18f;
                float marginH = (rectTransform.rect.height * 0.5f) + 18f;

                float minX = bounds.xMin + marginW;
                float maxX = bounds.xMax - marginW;
                float minY = bounds.yMin + marginH;
                float maxY = bounds.yMax - marginH - 22f;

                if (maxX > minX)
                {
                    if (pos.x < minX || pos.x > maxX)
                    {
                        if (isMoving) moveDirection.x = -moveDirection.x;
                        pos.x = Mathf.Clamp(pos.x, minX, maxX);
                    }
                }
                else
                {
                    pos.x = bounds.center.x;
                }

                if (maxY > minY)
                {
                    if (pos.y < minY || pos.y > maxY)
                    {
                        if (isMoving) moveDirection.y = -moveDirection.y;
                        pos.y = Mathf.Clamp(pos.y, minY, maxY);
                    }
                }
                else
                {
                    pos.y = bounds.center.y;
                }

                rectTransform.anchoredPosition = pos;
            }

            // ── Hover tracking ────────────────────────────────────────────
            if (isHovering)
            {
                hoverTime += Time.deltaTime;
                OnHoverActive?.Invoke();
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
            moveSpeed = moving ? ((speed > 0f ? speed : 100f) * 1.2f) : speed;
        }

        /// <summary>Sets the parent rect used as movement boundary.</summary>
        public void SetParentRect(RectTransform parent)
        {
            parentRect = parent;
        }

        public bool HasEverHovered { get; private set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            HasEverHovered = true;
            OnHoverActive?.Invoke();
        }
        
        public void OnPointerExit(PointerEventData eventData)  
        { 
            // If they started hovering and exited before completion, notify the task for error feedback
            if (!completed && hoverTime > 0.15f)
            {
                OnFailedAttempt?.Invoke();
            }
            isHovering = false; 
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (completed) return;
            OnTargetClicked?.Invoke();
        }
        #endregion
    }
}
