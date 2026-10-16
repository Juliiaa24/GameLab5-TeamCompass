/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Forwards pointer click events from empty explorer container/scroll areas to FileExplorerApp.
 */
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Forwards pointer click events from empty explorer container/scroll areas to FileExplorerApp.
    /// </summary>
    public class ExplorerClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public FileExplorerApp targetExplorer;

        private void Awake()
        {
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetExplorer == null) targetExplorer = GetComponentInParent<FileExplorerApp>();
            if (targetExplorer != null) targetExplorer.OnPointerClick(eventData);
        }
    }
}
