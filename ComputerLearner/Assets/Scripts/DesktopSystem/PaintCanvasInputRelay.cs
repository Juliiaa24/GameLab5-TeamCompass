/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Forwards pointer down and drag events on the Paint drawing surface to PaintApp.
 */
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Forwards pointer down, drag, move, enter, and exit events on the Paint drawing surface to PaintApp.
    /// </summary>
    public class PaintCanvasInputRelay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public PaintApp owner;
        private Vector2 lastScreenPos;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
            if (owner != null) owner.HandleCanvasPointerEnter(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
            if (owner != null) owner.HandleCanvasPointerExit(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
            if (owner != null) owner.HandleCanvasPointerMove(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastScreenPos = eventData.position;
            if (owner == null) owner = GetComponentInParent<PaintApp>();
            if (owner != null) owner.HandleCanvasPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
            if (owner != null) owner.HandleCanvasDrag(lastScreenPos, eventData);
            lastScreenPos = eventData.position;
        }
    }
}
