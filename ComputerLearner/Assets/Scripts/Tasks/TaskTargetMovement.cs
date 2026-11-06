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
        private float moveSpeed;

        public void Setup(TaskDefinition definition)
        {
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;

            if (definition == null || parentRect == null || definition.config == null) return;

            // Use data from TaskConfig
            float size = definition.config.targetSize > 0 ? definition.config.targetSize : 100f;
            rectTransform.sizeDelta = new Vector2(size, size);

            isMoving = definition.config.isMoving;
            moveSpeed = definition.config.moveSpeed > 0 ? definition.config.moveSpeed : 100f;

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
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        private void Update()
        {
            if (parentRect == null || parentRect.rect.width <= 0f || parentRect.rect.height <= 0f) return;

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
