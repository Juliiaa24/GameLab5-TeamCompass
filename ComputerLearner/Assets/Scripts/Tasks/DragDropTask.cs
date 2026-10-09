using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    public class DragDropTask : BaseTask
    {
        public RectTransform dropTarget;
        private int piecesRemaining = 1;

        protected override void SetupTask(TaskDefinition definition)
        {
            if (definition == null || definition.config == null) return;

            // Use config for required distance, drop target size, drag size, and count
            float requiredDistance = Mathf.Max(20f, definition.config.targetSize * 0.5f);
            
            if (dropTarget != null)
            {
                // We'll use targetSize for the drop zone, and a bit smaller for the drag pieces
                float size = definition.config.targetSize > 0 ? definition.config.targetSize : 200f;
                dropTarget.sizeDelta = new Vector2(size + 50f, size + 50f);
            }

            float dragSize = definition.config.targetSize > 0 ? definition.config.targetSize : 150f;
            
            int pieceCount = Mathf.Max(1, definition.config.targetCount);
            piecesRemaining = pieceCount;

            RectTransform parentRect = transform.parent as RectTransform;
            if (parentRect == null) return;
            Rect bounds = parentRect.rect;

            // Position Drop Target randomly
            if (dropTarget != null) 
            {
                dropTarget.anchoredPosition = new Vector2(
                    Random.Range(bounds.xMin + 200, bounds.xMax - 200), 
                    Random.Range(bounds.yMin + 200, bounds.yMax - 200)
                );
            }

            // Setup original piece
            SetupPiece(gameObject, dragSize, requiredDistance, bounds);

            // Clone extra pieces
            for (int i = 1; i < pieceCount; i++)
            {
                GameObject clone = Instantiate(gameObject, transform.parent);
                // Remove the main task script from clone so it doesn't trigger Complete() itself
                Destroy(clone.GetComponent<DragDropTask>());
                SetupPiece(clone, dragSize, requiredDistance, bounds);
            }
        }

        private void SetupPiece(GameObject go, float size, float snapDist, Rect bounds)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // Random position away from center
            rt.anchoredPosition = new Vector2(
                Random.value > 0.5f ? Random.Range(bounds.xMin + 50, -100) : Random.Range(100, bounds.xMax - 50),
                Random.Range(bounds.yMin + 50, bounds.yMax - 50)
            );

            // Ensure it has CanvasGroup
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();

            // Setup Drag script
            DraggablePiece piece = go.GetComponent<DraggablePiece>();
            if (piece == null) piece = go.AddComponent<DraggablePiece>();
            if (go.GetComponent<UnityEngine.UI.Image>() != null && go.GetComponent<CircleHitbox>() == null) go.AddComponent<CircleHitbox>();
            
            piece.dropTarget = dropTarget;
            piece.requiredDistanceToSnap = snapDist;
            piece.SetStartPosition(rt.anchoredPosition);

            piece.OnPieceDropped += HandlePieceDropped;
            piece.OnPieceMissed += RegisterAttempt;
        }

        private void HandlePieceDropped(DraggablePiece piece)
        {
            if (IsFinished) return;

            RecordIntermediateResult(true);
            piecesRemaining--;

            if (piecesRemaining <= 0)
            {
                Complete(true);
            }
        }
    }
}

