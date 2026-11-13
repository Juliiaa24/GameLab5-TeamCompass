using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    public class DragDropTask : BaseTask
    {
        public RectTransform dropTarget;
        private int piecesRemaining = 1;
        private int totalPiecesInitial = 1;
        private readonly List<DraggablePiece> activePieces = new List<DraggablePiece>();

        protected override RectTransform GetActiveFeedbackTarget()
        {
            for (int i = 0; i < activePieces.Count; i++)
            {
                if (activePieces[i] != null && activePieces[i].gameObject.activeInHierarchy)
                {
                    return activePieces[i].transform as RectTransform;
                }
            }
            return dropTarget != null ? dropTarget : (transform as RectTransform);
        }

        protected override void SetupTask(TaskDefinition definition)
        {
            if (definition == null || definition.config == null) return;

            // Strict snap tolerance: require the piece to be cleanly centered inside the drop zone (not just touching the edge)
            float dragSize = definition.config.targetSize > 0 ? definition.config.targetSize : 150f;
            float dropSize = dragSize + 20f;
            float requiredDistance = Mathf.Max(14f, dragSize * 0.25f);

            if (dropTarget != null)
            {
                dropTarget.sizeDelta = new Vector2(dropSize, dropSize);
            }

            int pieceCount = Mathf.Max(1, definition.config.targetCount);
            piecesRemaining = pieceCount;
            totalPiecesInitial = pieceCount;
            activePieces.Clear();

            RectTransform parentRect = transform.parent as RectTransform;
            if (parentRect == null) return;
            if (parentRect.rect.width <= 0f || parentRect.rect.height <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
            Rect bounds = parentRect.rect;

            // Position Drop Target randomly within safe interior margins
            if (dropTarget != null)
            {
                float dropPad = (dropSize * 0.5f) + 28f;
                float minX = bounds.xMin + dropPad;
                float maxX = bounds.xMax - dropPad;
                float minY = bounds.yMin + dropPad;
                float maxY = bounds.yMax - dropPad - 24f;
                dropTarget.anchoredPosition = new Vector2(
                    maxX > minX ? Random.Range(minX, maxX) : bounds.center.x,
                    maxY > minY ? Random.Range(minY, maxY) : bounds.center.y
                );
            }

            // Setup original piece
            SetupPiece(gameObject, dragSize, requiredDistance, bounds);

            // Clone extra pieces
            for (int i = 1; i < pieceCount; i++)
            {
                GameObject clone = Instantiate(gameObject, transform.parent, false);
                // Remove the main task script from clone so it doesn't trigger Complete() itself
                Destroy(clone.GetComponent<DragDropTask>());
                SetupPiece(clone, dragSize, requiredDistance, bounds);
            }

            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        private void SetupPiece(GameObject go, float size, float snapDist, Rect bounds)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // Safe interior bounds away from dropTarget
            float piecePad = (size * 0.5f) + 24f;
            float minX = bounds.xMin + piecePad;
            float maxX = bounds.xMax - piecePad;
            float minY = bounds.yMin + piecePad;
            float maxY = bounds.yMax - piecePad - 24f;

            Vector2 chosenPos = new Vector2(bounds.center.x, bounds.center.y);
            Vector2 dropPos = dropTarget != null ? dropTarget.anchoredPosition : Vector2.zero;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float rx = maxX > minX ? Random.Range(minX, maxX) : bounds.center.x;
                float ry = maxY > minY ? Random.Range(minY, maxY) : bounds.center.y;
                chosenPos = new Vector2(rx, ry);
                if (Vector2.Distance(chosenPos, dropPos) >= size * 1.1f) break;
            }
            rt.anchoredPosition = chosenPos;

            // Ensure it has CanvasGroup
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();

            // Setup Drag script
            DraggablePiece piece = go.GetComponent<DraggablePiece>();
            if (piece == null) piece = go.AddComponent<DraggablePiece>();
            if (go.GetComponent<UnityEngine.UI.Image>() != null && go.GetComponent<CircleHitbox>() == null) go.AddComponent<CircleHitbox>();

            piece.dropTarget = dropTarget;
            piece.requiredDistanceToSnap = snapDist;
            piece.SetStartPosition(rt.anchoredPosition);
            activePieces.Add(piece);

            piece.OnPieceDropped += HandlePieceDropped;
            piece.OnPieceMissed += HandlePieceMissed;
            piece.OnPieceRightClicked += (p) =>
            {
                if (IsFinished) return;
                RegisterAttempt(true, p.transform);
                VirtualMascot.ShowWrongClickFeedback(p.transform as RectTransform, false);
            };
            piece.OnPieceTapWithoutDrag += (p) =>
            {
                if (IsFinished) return;
                RegisterAttempt(false, p.transform);
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.dragTapWithoutDrag, p.transform as RectTransform, 3.0f);
            };
            piece.OnPieceDragDwell += (p) =>
            {
                if (IsFinished) return;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.dragDwellReminder, dropTarget, 3.0f);
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 6.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.dragInactivityReminder, GetActiveFeedbackTarget(), 3.0f);
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RectTransform targetAnchor = GetActiveFeedbackTarget();
            RegisterAttempt(true, targetAnchor);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(targetAnchor, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.dragMissedBackground, targetAnchor, 3.0f);
            }
        }

        private void HandlePieceMissed()
        {
            RegisterAttempt(true, dropTarget);
            TriggerMistakeScoreFeedback(dropTarget, 50);
            var fb = VirtualMascot.FeedbackDialogs;
            string msg = MascotFeedbackDialogs.PickRandom(fb.dragMissedDropMessages, fb.dragAlmost);
            VirtualMascot.ShowContextualFeedback(msg, dropTarget, 3.0f);
        }

        private void HandlePieceDropped(DraggablePiece piece)
        {
            if (IsFinished) return;

            activePieces.Remove(piece);
            piecesRemaining--;
            NotifySubTaskProgress((float)(totalPiecesInitial - piecesRemaining) / Mathf.Max(1, totalPiecesInitial));

            if (dropTarget != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(PulseDropTargetCoroutine());
            }

            Transform scoreAnchor = dropTarget != null ? dropTarget : (piece != null ? piece.transform : transform);
            if (piecesRemaining <= 0)
            {
                Complete(true, scoreAnchor);
            }
            else
            {
                RecordIntermediateResult(true, scoreAnchor);
            }
        }

        private System.Collections.IEnumerator PulseDropTargetCoroutine()
        {
            if (dropTarget == null) yield break;
            Vector3 baseScale = Vector3.one;
            Vector3 peakScale = new Vector3(1.12f, 1.12f, 1f);
            float elapsed = 0f;
            const float halfDur = 0.08f;
            while (elapsed < halfDur && dropTarget != null)
            {
                elapsed += Time.unscaledDeltaTime;
                dropTarget.localScale = Vector3.Lerp(baseScale, peakScale, elapsed / halfDur);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < halfDur && dropTarget != null)
            {
                elapsed += Time.unscaledDeltaTime;
                dropTarget.localScale = Vector3.Lerp(peakScale, baseScale, elapsed / halfDur);
                yield return null;
            }
            if (dropTarget != null) dropTarget.localScale = baseScale;
        }
    }
}
