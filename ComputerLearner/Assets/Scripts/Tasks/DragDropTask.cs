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

        private static readonly string[] MissedDropFeedback = new string[]
        {
            "Almost! Keep holding the left button down and drag the piece all the way into the target zone!",
            "So close! Drag the item right inside the target zone before letting go of the button!",
            "Don't let go too early! Move the piece inside the target circle and then release!"
        };

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
            totalPiecesInitial = pieceCount;

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

            SetupBackgroundClickInterceptor(OnBackgroundClicked);

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
            piece.OnPieceMissed += HandlePieceMissed;
            piece.OnPieceRightClicked += (p) =>
            {
                if (IsFinished) return;
                RegisterAttempt(true);
                VirtualMascot.ShowWrongClickFeedback(p.transform as RectTransform, false);
            };
            piece.OnPieceTapWithoutDrag += (p) =>
            {
                if (IsFinished) return;
                RegisterAttempt(false);
                VirtualMascot.ShowContextualFeedback("That was a quick click! Hold down the left mouse button and move the mouse to drag it!", p.transform as RectTransform, 3.0f);
            };
            piece.OnPieceDragDwell += (p) =>
            {
                if (IsFinished) return;
                VirtualMascot.ShowContextualFeedback("Great! Now move the piece into the target zone and release the button!", dropTarget, 3.0f);
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (!hasShownInactivityReminder && (Time.time - lastInteractionTime) >= 6.0f)
            {
                hasShownInactivityReminder = true;
                VirtualMascot.ShowContextualFeedback("Press and hold the left button on a piece, then drag it into the target zone!", dropTarget, 3.0f);
            }
        }

        public void OnBackgroundClicked(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(dropTarget, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback("Aim at the draggable piece, hold the left button down, and drag it to the target area!", dropTarget, 3.0f);
            }
        }

        private void HandlePieceMissed()
        {
            RegisterAttempt();
            string msg = MissedDropFeedback[Random.Range(0, MissedDropFeedback.Length)];
            VirtualMascot.ShowContextualFeedback(msg, dropTarget, 3.0f);
        }

        private void HandlePieceDropped(DraggablePiece piece)
        {
            if (IsFinished) return;

            RecordIntermediateResult(true);
            piecesRemaining--;
            NotifySubTaskProgress((float)(totalPiecesInitial - piecesRemaining) / Mathf.Max(1, totalPiecesInitial));

            if (piecesRemaining <= 0)
            {
                Complete(true);
            }
        }
    }
}
