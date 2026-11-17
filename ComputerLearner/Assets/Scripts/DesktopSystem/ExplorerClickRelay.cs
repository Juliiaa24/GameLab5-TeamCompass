/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Forwards pointer click events from empty explorer container/scroll areas to FileExplorerApp.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Forwards pointer click and marquee drag-selection events from empty explorer container/scroll areas to FileExplorerApp.
    /// </summary>
    public class ExplorerClickRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public FileExplorerApp targetExplorer;

        private bool isMarqueeDragging;
        private bool suppressNextLeftClick;
        private ScrollRect disabledScrollRect;

        private void Awake()
        {
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
        }

        private void OnDisable()
        {
            if (isMarqueeDragging)
            {
                isMarqueeDragging = false;
                targetExplorer?.EndMarqueeSelection(null);
            }
            RestoreScrollRect();
        }

        private void RestoreScrollRect()
        {
            if (disabledScrollRect != null)
            {
                disabledScrollRect.enabled = true;
                disabledScrollRect = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                suppressNextLeftClick = false;
                ScrollRect sr = GetComponent<ScrollRect>() ?? GetComponentInParent<ScrollRect>();
                if (sr != null && sr.enabled)
                {
                    sr.StopMovement();
                    sr.enabled = false;
                    disabledScrollRect = sr;
                }
                targetExplorer?.ClaimFocus();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !isMarqueeDragging)
            {
                RestoreScrollRect();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
            isMarqueeDragging = true;
            suppressNextLeftClick = true;
            targetExplorer?.BeginMarqueeSelection(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isMarqueeDragging || eventData.button != PointerEventData.InputButton.Left) return;
            targetExplorer?.UpdateMarqueeSelection(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isMarqueeDragging || eventData.button != PointerEventData.InputButton.Left) return;
            isMarqueeDragging = false;
            RestoreScrollRect();
            targetExplorer?.EndMarqueeSelection(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (suppressNextLeftClick || eventData.dragging)
                {
                    suppressNextLeftClick = false;
                    return;
                }
            }
            if (targetExplorer != null) targetExplorer.OnPointerClick(eventData);
        }
    }
}
