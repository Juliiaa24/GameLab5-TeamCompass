using UnityEngine;

namespace ComputerLearning
{
    [RequireComponent(typeof(RectTransform))]
    public class TaskTargetMovement : MonoBehaviour
    {
        private RectTransform rectTransform;
        private RectTransform parentRect;
        private Vector2 moveDirection;
        private bool isMoving;
        private bool isSwayingOnly;
        private float swayTimer;
        private Vector2 baseAnchoredPosition;
        private bool hasRecordedBasePos;
        private float moveSpeed;

        public void Setup(TaskDefinition definition)
        {
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;

            if (definition == null || parentRect == null || definition.config == null) return;

            // Use data from TaskConfig
            float size = definition.config.targetSize > 0 ? definition.config.targetSize : 100f;
            rectTransform.sizeDelta = new Vector2(size, size);

            if (definition.config.isMoving)
            {
                isMoving = true;
                isSwayingOnly = false;
                float baseSpeed = definition.config.moveSpeed > 0 ? definition.config.moveSpeed : 100f;
                moveSpeed = baseSpeed * 1.2f; // +20% speed on existing movers
            }
            else
            {
                // Subtle sinusoidal oscillation for static targets to give life
                isMoving = false;
                isSwayingOnly = true;
                moveSpeed = 35f;
                swayTimer = Random.Range(0f, 6.28f);
            }

            // Pick a random starting direction
            moveDirection = Random.insideUnitCircle.normalized;
            if (moveDirection == Vector2.zero) moveDirection = Vector2.right;

            // Start at a random position inside the parent
            RandomizePosition();
        }

        private void RandomizePosition()
        {
            if (parentRect.rect.width <= 0f || parentRect.rect.height <= 0f)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            Rect bounds = parentRect.rect;
            float padding = (Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 0.5f) + 24f;

            float minX = bounds.xMin + padding;
            float maxX = bounds.xMax - padding;
            float minY = bounds.yMin + padding;
            float maxY = bounds.yMax - padding - 22f;

            float rx = (maxX > minX) ? Random.Range(minX, maxX) : bounds.center.x;
            float ry = (maxY > minY) ? Random.Range(minY, maxY) : bounds.center.y;
            rectTransform.anchoredPosition = new Vector2(rx, ry);
            baseAnchoredPosition = rectTransform.anchoredPosition;
            hasRecordedBasePos = true;
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
            if (moving) isSwayingOnly = false;
        }

        private void Update()
        {
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

            Rect bounds = parentRect.rect;
            float marginW = (rectTransform.rect.width * 0.5f) + 18f;
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
    }
}
