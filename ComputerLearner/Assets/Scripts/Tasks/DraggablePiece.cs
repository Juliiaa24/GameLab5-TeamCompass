using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ComputerLearning
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class DraggablePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        public Action<DraggablePiece> OnPieceDropped;
        public Action OnPieceMissed;
        public Action<DraggablePiece> OnPieceRightClicked;
        public Action<DraggablePiece> OnPieceTapWithoutDrag;
        public Action<DraggablePiece> OnPieceDragDwell;

        public RectTransform dropTarget;
        public float requiredDistanceToSnap = 50f;
        public bool isDone = false;

        private RectTransform dragRect;
        private CanvasGroup canvasGroup;
        private Vector2 startPosition;
        private bool isDragging;
        private float dragStartTime;
        private bool hasWarnedDwell;
        private bool hasDragged;

        private void Awake()
        {
            dragRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (isDragging && !isDone)
            {
                if ((Time.time - dragStartTime) >= 5.0f && !hasWarnedDwell)
                {
                    hasWarnedDwell = true;
                    OnPieceDragDwell?.Invoke(this);
                }
            }
        }

        public void SetStartPosition(Vector2 pos)
        {
            startPosition = pos;
            if (dragRect != null) dragRect.anchoredPosition = pos;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            hasDragged = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDone) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnPieceRightClicked?.Invoke(this);
                return;
            }

            if (!hasDragged && eventData.button == PointerEventData.InputButton.Left)
            {
                OnPieceTapWithoutDrag?.Invoke(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            hasDragged = true;
            isDragging = true;
            dragStartTime = Time.time;
            hasWarnedDwell = false;

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

            isDragging = false;
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
