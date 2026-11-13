using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

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
        public float requiredDistanceToSnap = 35f;
        public bool isDone = false;

        private RectTransform dragRect;
        private CanvasGroup canvasGroup;
        private Vector2 startPosition;
        private bool isDragging;
        private float dragStartTime;
        private bool hasWarnedDwell;
        private bool hasDragged;

        private Image dropTargetImage;
        private Color dropTargetBaseColor = Color.white;
        private Vector3 dropTargetBaseScale = Vector3.one;
        private bool hasCachedDropVisuals;
        private bool isShowingValidHighlight;
        private Coroutine missShakeCoroutine;

        private void Awake()
        {
            dragRect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            ResetDropTargetVisual();
        }

        private void CacheDropTargetVisuals()
        {
            if (hasCachedDropVisuals || dropTarget == null) return;
            dropTargetImage = dropTarget.GetComponent<Image>();
            if (dropTargetImage != null) dropTargetBaseColor = dropTargetImage.color;
            dropTargetBaseScale = dropTarget.localScale;
            hasCachedDropVisuals = true;
        }

        private void SetDropTargetHighlight(bool isValidCenter)
        {
            if (dropTarget == null) return;
            CacheDropTargetVisuals();
            if (isShowingValidHighlight == isValidCenter) return;
            isShowingValidHighlight = isValidCenter;

            if (isValidCenter)
            {
                if (dropTargetImage != null)
                {
                    dropTargetImage.color = Color.Lerp(dropTargetBaseColor, new Color(0.18f, 0.85f, 0.45f, dropTargetBaseColor.a), 0.45f);
                }
                dropTarget.localScale = dropTargetBaseScale * 1.06f;
            }
            else
            {
                ResetDropTargetVisual();
            }
        }

        private void ResetDropTargetVisual()
        {
            isShowingValidHighlight = false;
            if (!hasCachedDropVisuals || dropTarget == null) return;
            if (dropTargetImage != null) dropTargetImage.color = dropTargetBaseColor;
            dropTarget.localScale = dropTargetBaseScale;
        }

        /// <summary>
        /// Computes distance between the piece center and drop zone center in dropTarget's local coordinate space
        /// so Canvas scaling never alters the snap tolerance.
        /// </summary>
        public float GetLocalDistanceToDropTarget()
        {
            if (dragRect == null || dropTarget == null) return float.MaxValue;
            Vector3 pieceCenterWorld = dragRect.TransformPoint(dragRect.rect.center);
            Vector3 localInDrop = dropTarget.InverseTransformPoint(pieceCenterWorld);
            return Vector2.Distance(new Vector2(localInDrop.x, localInDrop.y), dropTarget.rect.center);
        }

        public bool IsWithinValidDropZone()
        {
            return dropTarget != null && GetLocalDistanceToDropTarget() <= requiredDistanceToSnap;
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

            if (missShakeCoroutine != null)
            {
                StopCoroutine(missShakeCoroutine);
                missShakeCoroutine = null;
                dragRect.anchoredPosition = startPosition;
            }

            hasDragged = true;
            isDragging = true;
            dragStartTime = Time.time;
            hasWarnedDwell = false;

            CacheDropTargetVisuals();
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

            SetDropTargetHighlight(IsWithinValidDropZone());
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            isDragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            ResetDropTargetVisual();

            if (IsWithinValidDropZone())
            {
                Vector3 dropWorldCenter = dropTarget.TransformPoint(dropTarget.rect.center);
                dragRect.position = dropWorldCenter;
                isDone = true;
                canvasGroup.interactable = false;
                OnPieceDropped?.Invoke(this);
                return;
            }

            dragRect.anchoredPosition = startPosition;
            if (missShakeCoroutine != null) StopCoroutine(missShakeCoroutine);
            missShakeCoroutine = StartCoroutine(MissShakeCoroutine());
            OnPieceMissed?.Invoke();
        }

        private IEnumerator MissShakeCoroutine()
        {
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration && !isDragging && !isDone)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float offsetX = Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t);
                dragRect.anchoredPosition = startPosition + new Vector2(offsetX, 0f);
                yield return null;
            }
            if (!isDragging && !isDone)
            {
                dragRect.anchoredPosition = startPosition;
            }
            missShakeCoroutine = null;
        }
    }
}
