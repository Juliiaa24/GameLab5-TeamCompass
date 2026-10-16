using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class RecycleBinIcon : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public static event Action<DraggableIcon> OnIconRecycled;
        private RectTransform myRect;
        private Image binImage;

        private void Awake()
        {
            myRect = GetComponent<RectTransform>();
            binImage = GetComponent<Image>();
        }

        private void Start()
        {
            if (myRect == null) myRect = GetComponent<RectTransform>();
            if (binImage == null) binImage = GetComponent<Image>();

            bool isTutorialScene = FindAnyObjectByType<AdvancedDesktopTour>() != null;
            DraggableIcon di = GetComponent<DraggableIcon>();
            if (di != null && isTutorialScene)
            {
                di.SetupDynamicIcon(null, null, null, false);
            }

            if (binImage != null) binImage.color = Color.white;

            Text txt = GetComponentInChildren<Text>();
            if (txt != null) txt.text = "Recycle Bin";

            TMPro.TextMeshProUGUI tmp = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Recycle Bin";
        }

        public static void NotifyIconRecycled(DraggableIcon icon)
        {
            OnIconRecycled?.Invoke(icon);
        }

        /// <summary>
        /// Checks if a screen position overlaps any active and enabled RecycleBinIcon on the canvas.
        /// Also supports proximity detection (within 100px) and dragged RectTransform overlap.
        /// </summary>
        public static RecycleBinIcon FindBinAtScreenPoint(Vector2 screenPosition, Camera eventCamera, RectTransform draggedRect = null)
        {
            // 1. Check via EventSystem RaycastAll first
            if (EventSystem.current != null)
            {
                PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPosition };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(ped, results);
                foreach (var res in results)
                {
                    if (res.gameObject == null) continue;
                    RecycleBinIcon hitBin = res.gameObject.GetComponentInParent<RecycleBinIcon>();
                    if (hitBin != null && hitBin.isActiveAndEnabled)
                    {
                        return hitBin;
                    }
                }
            }

            // 2. Discover all active RecycleBinIcon instances
            RecycleBinIcon[] bins = FindObjectsByType<RecycleBinIcon>(FindObjectsSortMode.None);
            if (bins == null || bins.Length == 0)
            {
                // Self-healing fallback: find any Desktop icon named "Recycle Bin" and attach RecycleBinIcon
                if (DesktopManager.Instance != null && DesktopManager.Instance.DesktopIconContainer != null)
                {
                    foreach (Transform child in DesktopManager.Instance.DesktopIconContainer)
                    {
                        if (child != null && child.name.IndexOf("Recycle", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            RecycleBinIcon autoAdded = child.GetComponent<RecycleBinIcon>();
                            if (autoAdded == null) autoAdded = child.gameObject.AddComponent<RecycleBinIcon>();
                            bins = new RecycleBinIcon[] { autoAdded };
                            break;
                        }
                    }
                }
            }

            if (bins != null)
            {
                foreach (var bin in bins)
                {
                    if (bin == null || !bin.isActiveAndEnabled) continue;
                    RectTransform rt = bin.GetComponent<RectTransform>();
                    if (rt == null) continue;

                    Canvas canvas = bin.GetComponentInParent<Canvas>();
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? (eventCamera != null ? eventCamera : canvas.worldCamera)
                        : null;

                    // Exact bounds check with correct camera
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition, cam))
                    {
                        return bin;
                    }

                    // Distance-based proximity check (within 100 screen pixels of bin center)
                    Vector2 binScreenCenter = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                    if (Vector2.Distance(binScreenCenter, screenPosition) <= 100f)
                    {
                        return bin;
                    }

                    // If draggedRect provided, check its center and bounds against bin
                    if (draggedRect != null)
                    {
                        Vector2 draggedScreenCenter = RectTransformUtility.WorldToScreenPoint(cam, draggedRect.position);
                        if (Vector2.Distance(binScreenCenter, draggedScreenCenter) <= 100f)
                        {
                            return bin;
                        }

                        if (RectTransformUtility.RectangleContainsScreenPoint(rt, draggedScreenCenter, cam))
                        {
                            return bin;
                        }
                    }
                }
            }

            return null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging && binImage != null)
            {
                binImage.color = new Color(0.80f, 0.92f, 1.0f, 1f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (binImage != null)
            {
                binImage.color = Color.white;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (GetComponent<DraggableIcon>() == null && ContextMenuController.Instance != null)
                {
                    int binCount = FileExplorerApp.GetRecycleBinCount();
                    var items = new List<ContextMenuItem>
                    {
                        new ContextMenuItem("Open Recycle Bin", () =>
                        {
                            if (StartMenuController.Instance != null)
                            {
                                StartMenuController.Instance.OpenFileExplorerAtFolder("Recycle Bin");
                            }
                        }, StartMenuController.Instance != null),
                        new ContextMenuItem($"Empty Recycle Bin ({binCount})", () =>
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
            if (binImage != null) binImage.color = Color.white;
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
                HandleDesktopIconDropped(icon);
            }
        }

        public bool HandleDesktopIconDropped(DraggableIcon icon)
        {
            if (icon == null || icon.gameObject == this.gameObject) return false;
            if (binImage != null) binImage.color = Color.white;
            icon.MarkDropHandled();

            bool isTutorialTour = FindAnyObjectByType<AdvancedDesktopTour>() != null;

            if (!isTutorialTour && icon.IsProtected)
            {
                Debug.Log($"[RecycleBin] Protected system icon '{icon.gameObject.name}' cannot be deleted.");
                icon.ReturnToInitialPosition();
                return true;
            }

            Debug.Log($"[RecycleBin] Recycled {icon.gameObject.name}");
            OnIconRecycled?.Invoke(icon);

            if (isTutorialTour)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(icon.gameObject);
                else Destroy(icon.gameObject);
#else
                Destroy(icon.gameObject);
#endif
            }
            else
            {
                icon.DeleteDesktopIconToRecycleBin();
            }
            return true;
        }
    }
}
