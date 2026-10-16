using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class RecycleBinIcon : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public static event Action<DraggableIcon> OnIconRecycled;
        private RectTransform myRect;

        private void Start()
        {
            myRect = GetComponent<RectTransform>();

            bool isTutorialScene = FindAnyObjectByType<AdvancedDesktopTour>() != null;
            DraggableIcon di = GetComponent<DraggableIcon>();
            if (di != null && isTutorialScene)
            {
                // In Tutorial2 (AdvancedDesktopTour), keep DraggableIcon for grid slot without opening a window
                di.SetupDynamicIcon(null, null, null, false);
            }

            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = Color.white;

            UnityEngine.UI.Text txt = GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null) txt.text = "Recycle Bin";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // If DraggableIcon is present on the same GameObject, it already handles right-click;
                // otherwise show the Recycle Bin context menu directly.
                if (GetComponent<DraggableIcon>() == null && ContextMenuController.Instance != null)
                {
                    int binCount = FileExplorerApp.GetRecycleBinCount();
                    var items = new List<ContextMenuItem>
                    {
                        new ContextMenuItem("Abrir Papelera de Reciclaje", () =>
                        {
                            if (StartMenuController.Instance != null)
                            {
                                StartMenuController.Instance.OpenFileExplorerAtFolder("Recycle Bin");
                            }
                        }, StartMenuController.Instance != null),
                        new ContextMenuItem($"Vaciar Papelera de Reciclaje ({binCount})", () =>
                        {
                            FileExplorerApp.EmptyRecycleBin();
                        }, binCount > 0)
                    };
                    ContextMenuController.Instance.Show(eventData.position, items);
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            // 1. Check if an item was dragged from inside FileExplorerApp
            ExplorerItemPointerHandler explorerItem = eventData.pointerDrag.GetComponent<ExplorerItemPointerHandler>();
            if (explorerItem != null && explorerItem.ItemData != null)
            {
                if (!explorerItem.ItemData.isProtected)
                {
                    FileExplorerApp.MoveItemToRecycleBin(explorerItem.ItemData);
                    Debug.Log($"[RecycleBin] Moved explorer item '{explorerItem.ItemData.name}' to Recycle Bin.");
                }
                else
                {
                    Debug.Log($"[RecycleBin] '{explorerItem.ItemData.name}' is protected and cannot be deleted.");
                }
                return;
            }

            // 2. Check if a DraggableIcon from the desktop was dropped
            DraggableIcon icon = eventData.pointerDrag.GetComponent<DraggableIcon>();
            if (icon != null && icon.gameObject != this.gameObject)
            {
                bool isTutorialTour = FindAnyObjectByType<AdvancedDesktopTour>() != null;

                if (!isTutorialTour && icon.IsProtected)
                {
                    Debug.Log($"[RecycleBin] Protected system icon '{icon.gameObject.name}' cannot be deleted.");
                    icon.ReturnToInitialPosition();
                    return;
                }

                Debug.Log($"[RecycleBin] Recycled {icon.gameObject.name}");
                OnIconRecycled?.Invoke(icon);

                if (isTutorialTour)
                {
                    Destroy(icon.gameObject);
                }
                else
                {
                    icon.DeleteDesktopIconToRecycleBin();
                }
            }
        }
    }
}
