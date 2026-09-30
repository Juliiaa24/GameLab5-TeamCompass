/**
 * Author: Diego
 * Date: 30/09/26
 * Description: A single clickable target UI element used inside ClickTargetTask.
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// A single clickable target circle placed inside a ClickTargetTask.
    /// Optionally bounces around the content area for higher difficulties.
    /// Reports left-clicks via the OnClicked event.
    ///
    /// This is a child element — it does NOT inherit BaseTask.
    /// The parent ClickTargetTask listens to OnClicked and calls Complete().
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ClickTarget : MonoBehaviour, IPointerClickHandler
    {
        #region Public Variables
        /// <summary>Fired when the player left-clicks this target.</summary>
        public event Action OnClicked;
        #endregion

        #region Private Variables
        [SerializeField] private Image backgroundImage;

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
            // Random starting direction
            moveDirection = UnityEngine.Random.insideUnitCircle.normalized;
            if (moveDirection == Vector2.zero) moveDirection = Vector2.right;
        }

        private void Update()
        {
            if (!isMoving || parentRect == null) return;

            Vector2 pos = rectTransform.anchoredPosition;
            pos += moveDirection * (moveSpeed * Time.deltaTime);

            // Bounce off the edges of the parent rect
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Configures size and movement. Call this immediately after spawning.
        /// </summary>
        public void Configure(float size, bool moving, float speed)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(size, size);
            isMoving  = moving;
            moveSpeed = speed;
        }

        /// <summary>Sets the parent rect used as movement boundary.</summary>
        public void SetParentRect(RectTransform parent)
        {
            parentRect = parent;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnClicked?.Invoke();
        }
        #endregion
    }
}
