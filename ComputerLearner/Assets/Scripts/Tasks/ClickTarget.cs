/**
 * Author: Diego
 * Date: 30/09/26
 * Description: A single clickable target UI element used inside ClickTargetTask.
 *              Enhanced with edge-accuracy detection and contextual mascot feedback.
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
    public class ClickTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Public Variables
        /// <summary>Fired when the player left-clicks this target.</summary>
        public event Action OnClicked;
        /// <summary>Fired when the player left-clicks this target, reporting precision points (100 center, 70 mid, 40 outer).</summary>
        public event Action<int> OnClickedWithScore;
        /// <summary>Fired when the player clicks with the wrong button (right click).</summary>
        public event Action OnWrongButtonClicked;
        /// <summary>Fired when the player hovers over the target for > 2.5s without clicking.</summary>
        public event Action OnHoverStagnant;
        #endregion

        #region Private Variables
        [SerializeField] private Image backgroundImage;

        private bool isMoving;
        private bool isSwayingOnly;
        private float swayTimer;
        private Vector2 baseAnchoredPosition;
        private bool hasRecordedBasePos;
        private float moveSpeed;
        private RectTransform rectTransform;
        private RectTransform parentRect;
        private Vector2 moveDirection;
        private bool hasBeenClicked;
        private bool isHovering;
        private float hoverDwellTime;
        private bool hasWarnedStagnant;
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
            if (isHovering && !hasBeenClicked)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedStagnant)
                {
                    hasWarnedStagnant = true;
                    OnHoverStagnant?.Invoke();
                }
            }

            if (parentRect == null || parentRect.rect.width <= 0f || parentRect.rect.height <= 0f) return;

            if (isSwayingOnly)
            {
                if (!hasRecordedBasePos && rectTransform.anchoredPosition != Vector2.zero)
                {
                    baseAnchoredPosition = rectTransform.anchoredPosition;
                    hasRecordedBasePos = true;
                }

                swayTimer += Time.deltaTime * 1.5f;
                Vector2 swayOffset = new Vector2(Mathf.Sin(swayTimer) * 32f, Mathf.Cos(swayTimer * 0.85f) * 22f);
                Vector2 targetPos = (hasRecordedBasePos ? baseAnchoredPosition : rectTransform.anchoredPosition) + swayOffset;

                Rect swayBounds = parentRect.rect;
                float sMarginW = (rectTransform.rect.width * 0.5f) + 18f;
                float sMarginH = (rectTransform.rect.height * 0.5f) + 18f;
                float sMinX = swayBounds.xMin + sMarginW;
                float sMaxX = swayBounds.xMax - sMarginW;
                float sMinY = swayBounds.yMin + sMarginH;
                float sMaxY = swayBounds.yMax - sMarginH - 22f;

                targetPos.x = (sMaxX > sMinX) ? Mathf.Clamp(targetPos.x, sMinX, sMaxX) : swayBounds.center.x;
                targetPos.y = (sMaxY > sMinY) ? Mathf.Clamp(targetPos.y, sMinY, sMaxY) : swayBounds.center.y;
                rectTransform.anchoredPosition = targetPos;
                return;
            }

            Vector2 pos = rectTransform.anchoredPosition;
            if (isMoving)
            {
                pos += moveDirection * (moveSpeed * Time.deltaTime);
            }

            // Clamp/bounce off the edges of the parent rect with safe interior margins
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Configures size and movement. Call this immediately after spawning.
        /// </summary>
        public void Configure(float size, bool moving, float speed)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(size, size);
            if (moving)
            {
                isMoving = true;
                isSwayingOnly = false;
                moveSpeed = (speed > 0f ? speed : 100f) * 1.2f; // +20% speed
            }
            else
            {
                isMoving = false;
                isSwayingOnly = true;
                moveSpeed = 35f;
                swayTimer = UnityEngine.Random.Range(0f, 6.28f);
            }
            hasBeenClicked = false;
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedStagnant = false;
        }

        /// <summary>Sets the parent rect used as movement boundary.</summary>
        public void SetParentRect(RectTransform parent)
        {
            parentRect = parent;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            hoverDwellTime = 0f;
            hasWarnedStagnant = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedStagnant = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hasBeenClicked) return;

            // Contextual Feedback 1: Wrong click button (Right click instead of Left click)
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnWrongButtonClicked?.Invoke();
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;

            hasBeenClicked = true;

            // Concentric precision scoring:
            // Center (Bullseye, <= 35% radius): +100
            // Mid ring (35% - 70% radius): +70
            // Outer ring (> 70% radius): +40
            int scorePoints = 100;
            if (rectTransform != null)
            {
                Camera eventCamera = eventData.pressEventCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventCamera, out Vector2 localPoint))
                {
                    float radius = Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
                    float distFromCenter = localPoint.magnitude;
                    float ratio = radius > 1f ? (distFromCenter / radius) : 0f;

                    if (ratio <= 0.35f)
                    {
                        scorePoints = 100; // Center (Bullseye)
                    }
                    else if (ratio <= 0.70f)
                    {
                        scorePoints = 70; // Mid ring
                    }
                    else
                    {
                        scorePoints = 40; // Outer ring
                        VirtualMascot.ShowAlmostOutFeedback(rectTransform);
                    }
                }
            }

            OnClickedWithScore?.Invoke(scorePoints);
            OnClicked?.Invoke();
        }
        #endregion
    }
}
