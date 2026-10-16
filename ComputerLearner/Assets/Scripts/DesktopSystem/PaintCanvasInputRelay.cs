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
    /// Forwards pointer down and drag events on the Paint drawing surface to PaintApp.
    /// </summary>
    public class PaintCanvasInputRelay : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public PaintApp owner;
        private Vector2 lastScreenPos;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<PaintApp>();
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
