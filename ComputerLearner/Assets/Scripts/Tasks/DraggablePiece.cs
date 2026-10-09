using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ComputerLearning
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class DraggablePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<DraggablePiece> OnPieceDropped;
        public Action OnPieceMissed;

        public RectTransform dropTarget;
        public float requiredDistanceToSnap = 50f;
        public bool isDone = false;

        private RectTransform dragRect;
        private CanvasGroup canvasGroup;
        private Vector2 startPosition;

        private void Awake()
        {
            dragRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetStartPosition(Vector2 pos)
        {
            startPosition = pos;
            if (dragRect != null) dragRect.anchoredPosition = pos;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            startPosition = dragRect.anchoredPosition;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
            dragRect.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragRect, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
            {
                dragRect.position = globalMousePos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (dropTarget != null)
            {
                float distance = Vector2.Distance(dragRect.position, dropTarget.position);
                if (distance <= requiredDistanceToSnap)
                {
                    dragRect.position = dropTarget.position;
                    isDone = true;
                    canvasGroup.interactable = false;
                    OnPieceDropped?.Invoke(this);
                    return;
                }
            }

            dragRect.anchoredPosition = startPosition;
            OnPieceMissed?.Invoke();
        }
    }
}
