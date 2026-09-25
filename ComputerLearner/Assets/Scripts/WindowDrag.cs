/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Drag windows in their parent's Canvas coordinates.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        #region Private Variables
        [SerializeField] private RectTransform window;
        private Window controller;
        private RectTransform parentRect;
        private Vector2 pointerOffset;
        private bool dragging;
        #endregion

        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = false;
            if (window == null || eventData.button != PointerEventData.InputButton.Left) return;
            controller = window.GetComponent<Window>();
            controller?.BringToFront();
            if (controller != null && controller.IsMaximized) return;
            parentRect = window.parent as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                eventData.pressPosition, eventData.pressEventCamera, out Vector2 pointer)) return;
            pointerOffset = (Vector2)window.localPosition - pointer;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || (controller != null && controller.IsMaximized)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                eventData.position, eventData.pressEventCamera, out Vector2 pointer)) return;
            Vector3 position = window.localPosition;
            position.x = pointer.x + pointerOffset.x;
            position.y = pointer.y + pointerOffset.y;
            window.localPosition = position;
            controller?.KeepTitleVisible();
        }
        #endregion
    }
}
