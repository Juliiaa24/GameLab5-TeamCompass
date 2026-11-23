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
        private Vector2 circularRadius = new Vector2(32f, 22f);
        private CircleHitbox circleHitbox;
        private bool hasBottomHoldBar;

        public TargetMovementType MovementType { get; private set; } = TargetMovementType.Stationary;

        public void Setup(TaskDefinition definition, TargetMovementType? overrideMovementType = null, float defaultDvdSpeed = 95f)
        {
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;
            circleHitbox = GetComponent<CircleHitbox>();
            hasBottomHoldBar = GetComponent<ClickHoldTask>() != null;

            if (definition == null || parentRect == null || definition.config == null) return;

            float radiusRatio = circleHitbox != null ? circleHitbox.RadiusRatio : 0.5f;
            float scaleMultiplier = (radiusRatio > 0.01f && radiusRatio < 0.5f) ? (0.5f / radiusRatio) : 1f;

            // Use data from TaskConfig scaled so the visible circle matches targetSize
            float logicalSize = definition.config.targetSize > 0 ? definition.config.targetSize : 100f;
            float rectSize = logicalSize * scaleMultiplier;
            rectTransform.sizeDelta = new Vector2(rectSize, rectSize);

            TargetMovementType mode = overrideMovementType ?? BaseTask.ResolveTargetMovementType(definition);
            SetMovementType(mode, definition.config.moveSpeed, defaultDvdSpeed);

            // Start at a random position inside the parent
            RandomizePosition();
        }

        private void GetEffectiveHalfExtents(out float halfW, out float halfH, out float extraBottomMargin)
        {
            float radiusRatio = circleHitbox != null ? circleHitbox.RadiusRatio : 0.5f;
            halfW = rectTransform.rect.width * radiusRatio;
            halfH = rectTransform.rect.height * radiusRatio;
            extraBottomMargin = (hasBottomHoldBar && radiusRatio < 0.5f) ? (rectTransform.rect.height * 0.09f) : 0f;
        }

        public void SetCircularRadius(float radiusX, float radiusY)
        {
            circularRadius = new Vector2(Mathf.Max(4f, radiusX), Mathf.Max(4f, radiusY));
        }

        public void SetMovementType(TargetMovementType mode, float configSpeed = 0f, float defaultDvdSpeed = 95f)
        {
            MovementType = mode;
            switch (mode)
            {
                case TargetMovementType.DvdBounce:
                    isMoving = true;
                    isSwayingOnly = false;
                    float baseSpeed = configSpeed > 0f ? configSpeed : defaultDvdSpeed;
                    moveSpeed = baseSpeed * 1.15f;
                    moveDirection = BaseTask.GetRandomDvdDirection();
                    break;

                case TargetMovementType.Circular:
                    isMoving = false;
                    isSwayingOnly = true;
                    moveSpeed = 35f;
                    swayTimer = Random.Range(0f, 6.28f);
                    break;

                case TargetMovementType.Stationary:
                default:
                    isMoving = false;
                    isSwayingOnly = false;
                    moveSpeed = 0f;
                    break;
            }
        }

        private void RandomizePosition()
        {
            if (parentRect.rect.width <= 0f || parentRect.rect.height <= 0f)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            Rect bounds = parentRect.rect;
            GetEffectiveHalfExtents(out float halfW, out float halfH, out float extraBottom);
            float padding = Mathf.Max(halfW, halfH) + 24f;

            float minX = bounds.xMin + padding;
            float maxX = bounds.xMax - padding;
            float minY = bounds.yMin + padding + extraBottom;
            float maxY = bounds.yMax - padding - 22f;

            float rx = (maxX > minX) ? Random.Range(minX, maxX) : bounds.center.x;
            float ry = (maxY > minY) ? Random.Range(minY, maxY) : bounds.center.y;
            rectTransform.anchoredPosition = new Vector2(rx, ry);
            baseAnchoredPosition = rectTransform.anchoredPosition;
            hasRecordedBasePos = true;
        }

        public void SetMoving(bool moving)
        {
            SetMovementType(moving ? TargetMovementType.DvdBounce : TargetMovementType.Stationary, moveSpeed);
        }

        private void Update()
        {
            if (parentRect == null || parentRect.rect.width <= 0f || parentRect.rect.height <= 0f) return;

            GetEffectiveHalfExtents(out float halfW, out float halfH, out float extraBottom);

            if (isSwayingOnly)
            {
                if (!hasRecordedBasePos && rectTransform.anchoredPosition != Vector2.zero)
                {
                    baseAnchoredPosition = rectTransform.anchoredPosition;
                    hasRecordedBasePos = true;
                }

                swayTimer += Time.deltaTime * 1.5f;
                Vector2 swayOffset = new Vector2(Mathf.Sin(swayTimer) * circularRadius.x, Mathf.Cos(swayTimer * 0.85f) * circularRadius.y);
                Vector2 targetPos = (hasRecordedBasePos ? baseAnchoredPosition : rectTransform.anchoredPosition) + swayOffset;

                Rect swayBounds = parentRect.rect;
                float sMarginW = halfW + 18f;
                float sMarginH = halfH + 18f;
                float sMinX = swayBounds.xMin + sMarginW;
                float sMaxX = swayBounds.xMax - sMarginW;
                float sMinY = swayBounds.yMin + sMarginH + extraBottom;
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
            float marginW = halfW + 18f;
            float marginH = halfH + 18f;

            float minX = bounds.xMin + marginW;
            float maxX = bounds.xMax - marginW;
            float minY = bounds.yMin + marginH + extraBottom;
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
