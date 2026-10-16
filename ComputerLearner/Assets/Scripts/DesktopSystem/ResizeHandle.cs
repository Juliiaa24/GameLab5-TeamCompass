/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window resizing script
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ResizeHandle : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler
    {
        #region Public Variables
        [Flags]
        public enum ResizeDirection
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }
        #endregion

        #region Private Variables
        [SerializeField] private RectTransform window;
        private RectTransform parentRect;
        private Window controller;
        private bool resizing;

        private float startLeft;
        private float startRight;
        private float startTop;
        private float startBottom;
        private Vector2 startPointerPosition;
        [SerializeField] private ResizeDirection direction;
        [SerializeField] private Vector2 minSize = new Vector2(490, 300);
        public Vector2 MinSize { get => minSize; set => minSize = value; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (window == null) window = transform.parent as RectTransform;
            if (window == null) return;
            parentRect = window.parent as RectTransform;
            controller = window.GetComponent<Window>();
        }
        #endregion

        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            resizing = false;
            if (window == null) window = transform.parent as RectTransform;
            if (window == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (controller == null) controller = window.GetComponent<Window>();
            controller?.BringToFront();
            if (controller != null && controller.IsMaximized) return;

            parentRect = window.parent as RectTransform;
            resizing = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.pressPosition,
                eventData.pressEventCamera,
                out startPointerPosition
            );

            Rect rect = window.rect;
            Vector3 position = window.localPosition;

            startLeft = position.x + rect.xMin;
            startRight = position.x + rect.xMax;
            startBottom = position.y + rect.yMin;
            startTop = position.y + rect.yMax;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!resizing || window == null || (controller != null && controller.IsMaximized)) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 currentPointerPosition
            );

            Vector2 delta = currentPointerPosition - startPointerPosition;

            float left = startLeft;
            float right = startRight;
            float top = startTop;
            float bottom = startBottom;

            if (HasDirection(ResizeDirection.Left))
                left += delta.x;

            if (HasDirection(ResizeDirection.Right))
                right += delta.x;

            if (HasDirection(ResizeDirection.Top))
                top += delta.y;

            if (HasDirection(ResizeDirection.Bottom))
                bottom += delta.y;

            // Clamp dragged edges to parent desktop container bounds
            if (parentRect != null && parentRect.rect.width > 0f && parentRect.rect.height > 0f)
            {
                Rect pRect = parentRect.rect;
                if (HasDirection(ResizeDirection.Left))
                    left = Mathf.Max(left, pRect.xMin);
                if (HasDirection(ResizeDirection.Right))
                    right = Mathf.Min(right, pRect.xMax);
                if (HasDirection(ResizeDirection.Top))
                    top = Mathf.Min(top, pRect.yMax);
                if (HasDirection(ResizeDirection.Bottom))
                    bottom = Mathf.Max(bottom, pRect.yMin);
            }

            // Enforce minimum horizontal size
            if (right - left < minSize.x)
            {
                if (HasDirection(ResizeDirection.Left))
                    left = right - minSize.x;
                else
                    right = left + minSize.x;
            }

            // Enforce minimum vertical size
            if (top - bottom < minSize.y)
            {
                if (HasDirection(ResizeDirection.Bottom))
                    bottom = top - minSize.y;
                else
                    top = bottom + minSize.y;
            }

            float width = right - left;
            float height = top - bottom;

            if (controller != null) controller.IsResizing = true;

            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                width
            );

            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height
            );

            Vector3 newPosition = window.localPosition;
            newPosition.x = left + width * window.pivot.x;
            newPosition.y = bottom + height * window.pivot.y;
            window.localPosition = newPosition;

            if (controller != null)
            {
                controller.IsResizing = false;
                controller.KeepTitleVisible();
            }
        }
        #endregion

        #region Private Methods
        private bool HasDirection(ResizeDirection value)
        {
            return (direction & value) != 0;
        }
        #endregion
    }
}
