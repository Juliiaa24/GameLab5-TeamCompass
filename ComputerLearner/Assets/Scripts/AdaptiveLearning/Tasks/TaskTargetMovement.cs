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
            Rect bounds = parentRect.rect;
            float halfW = rectTransform.rect.width * 0.5f;
            float halfH = rectTransform.rect.height * 0.5f;

            float minX = bounds.xMin + halfW;
            float maxX = bounds.xMax - halfW;
            float minY = bounds.yMin + halfH;
            float maxY = bounds.yMax - halfH;

            if (maxX > minX && maxY > minY)
            {
                float rx = Random.Range(minX, maxX);
                float ry = Random.Range(minY, maxY);
                rectTransform.anchoredPosition = new Vector2(rx, ry);
            }
        }

        private void Update()
        {
            if (!isMoving || parentRect == null) return;

            Vector2 pos = rectTransform.anchoredPosition;
            pos += moveDirection * (moveSpeed * Time.deltaTime);

            Rect bounds = parentRect.rect;
            float halfW = rectTransform.rect.width * 0.5f;
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
    }
}
