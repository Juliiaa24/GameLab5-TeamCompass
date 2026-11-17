/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Handles left click (selection), double click (open), right click (context menu), hover highlight,
 *              and bidirectional drag & drop (to Recycle Bin, to other folders, or out onto the Desktop).
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Handles left click (selection), double click (open), right click (context menu), hover highlight,
    /// and bidirectional drag & drop (to Recycle Bin, to other folders, or out onto the Desktop).
    /// </summary>
    public class ExplorerItemPointerHandler : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FileExplorerApp explorer;
        [NonSerialized] private ExplorerItemData itemData;
        private bool isUnlocked = true;
        private float lastLeftClickTime;
        private bool preserveGroupOnClick;
        private GameObject dragGhost;
        private Canvas dragCanvas;
        private Image cardBg;

        private void OnDisable()
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }
            dragCanvas = null;
        }

        private void OnDestroy()
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }
            dragCanvas = null;
        }

        public ExplorerItemData ItemData => itemData;
        public FileExplorerApp Explorer => explorer;

        public void Initialize(FileExplorerApp owner, ExplorerItemData data, bool unlocked)
        {
            explorer = owner;
            itemData = data;
            isUnlocked = unlocked;
            cardBg = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardBg != null && explorer != null && !explorer.IsItemSelected(gameObject))
            {
                cardBg.color = explorer.HoverItemColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardBg != null && explorer != null && !explorer.IsItemSelected(gameObject))
            {
                cardBg.color = Color.clear;
            }
        }

        private static bool IsCtrlHeld()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.ctrlKey.isPressed || kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed))
                return true;
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#else
            return false;
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (explorer == null || itemData == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            bool isCtrl = IsCtrlHeld();
            preserveGroupOnClick = !isCtrl && explorer.SelectedItems.Count > 1 && explorer.IsItemDataSelected(itemData);
            if (!isCtrl && !preserveGroupOnClick)
            {
                explorer.SelectItem(gameObject, itemData);
            }
            else
            {
                explorer.ClaimFocus();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (explorer == null || itemData == null) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                explorer.OnItemRightClicked(gameObject, itemData, eventData.position);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (eventData.dragging) return;

                if (IsCtrlHeld())
                {
                    explorer.ToggleItemSelection(gameObject, itemData);
                    return;
                }

                float now = Time.unscaledTime;
                bool isDoubleClick = eventData.clickCount >= 2 || (now - lastLeftClickTime) < 0.45f;
                lastLeftClickTime = now;

                if (isDoubleClick)
                {
                    lastLeftClickTime = 0f;
                    if (!isUnlocked)
                    {
                        explorer.NotifyLockedItemClicked(itemData, true);
                        return;
                    }
                    explorer.OnItemDoubleClicked(itemData);
                }
                else
                {
                    explorer.SelectItem(gameObject, itemData);
                    if (!isUnlocked)
                    {
                        explorer.NotifyLockedItemClicked(itemData, false);
                    }
                }
            }
        }

        private List<ExplorerItemData> GetActiveDragItems()
        {
            List<ExplorerItemData> items = new List<ExplorerItemData>();
            if (explorer != null && itemData != null && explorer.IsItemDataSelected(itemData) && explorer.SelectedItems.Count > 1)
            {
                foreach (var sel in explorer.SelectedItems)
                {
                    if (sel != null) items.Add(sel);
                }
            }
            else if (itemData != null)
            {
                items.Add(itemData);
            }
            return items;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (itemData == null) return;

            List<ExplorerItemData> dragItems = GetActiveDragItems();
            int movableCount = 0;
            foreach (var it in dragItems)
            {
                if (it != null && !FileExplorerApp.IsItemProtectedStatic(it)) movableCount++;
            }

            if (movableCount == 0)
            {
                explorer?.ShowStatusNotice("System items are protected and cannot be moved.", true);
                return;
            }

            Canvas foundCanvas = GetComponentInParent<Canvas>();
            if (foundCanvas == null) return;
            dragCanvas = foundCanvas.rootCanvas != null ? foundCanvas.rootCanvas : foundCanvas;

            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            dragGhost = new GameObject($"Drag_{itemData.name}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragGhost.transform.SetParent(dragCanvas.transform, false);
            dragGhost.transform.SetAsLastSibling();

            RectTransform rt = dragGhost.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(56f, 56f);

            CanvasGroup cg = dragGhost.GetComponent<CanvasGroup>();
            cg.alpha = 0.80f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            Image ghostImg = dragGhost.GetComponent<Image>();
            Image sourceImg = transform.Find("Icon")?.GetComponent<Image>();
            Sprite spriteToUse = (sourceImg != null && sourceImg.sprite != null) ? sourceImg.sprite : itemData.icon;
            if (spriteToUse == null)
            {
                if (itemData.fileType == ExplorerFileType.Folder) spriteToUse = FileExplorerApp.GetFolderSprite();
                else if (itemData.fileType == ExplorerFileType.TextFile) spriteToUse = FileExplorerApp.GetTextFileSprite();
                else if (itemData.fileType == ExplorerFileType.ImageFile) spriteToUse = FileExplorerApp.GetImageFileSprite();
            }
            ghostImg.sprite = spriteToUse;
            ghostImg.preserveAspect = true;
            ghostImg.raycastTarget = false;

            if (movableCount > 1)
            {
                GameObject badgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObj.transform.SetParent(dragGhost.transform, false);
                RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(1f, 0f);
                badgeRt.anchorMax = new Vector2(1f, 0f);
                badgeRt.pivot = new Vector2(0.5f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(-4f, 6f);
                badgeRt.sizeDelta = new Vector2(22f, 22f);

                Image badgeBg = badgeObj.GetComponent<Image>();
                badgeBg.color = new Color(0.15f, 0.45f, 0.90f, 0.95f);
                badgeBg.raycastTarget = false;

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                txtObj.transform.SetParent(badgeObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                TMPro.TextMeshProUGUI badgeTmp = txtObj.GetComponent<TMPro.TextMeshProUGUI>();
                TMPro.TextMeshProUGUI sourceLabel = transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (sourceLabel != null && sourceLabel.font != null) badgeTmp.font = sourceLabel.font;
                badgeTmp.text = movableCount.ToString();
                badgeTmp.fontSize = 13f;
                badgeTmp.fontStyle = TMPro.FontStyles.Bold;
                badgeTmp.color = Color.white;
                badgeTmp.alignment = TMPro.TextAlignmentOptions.Center;
                badgeTmp.raycastTarget = false;
            }

            UpdateGhostPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                UpdateGhostPosition(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (itemData == null) return;

            List<ExplorerItemData> dragItems = GetActiveDragItems();
            bool hasAnyMovable = false;
            foreach (var it in dragItems)
            {
                if (it != null && !FileExplorerApp.IsItemProtectedStatic(it))
                {
                    hasAnyMovable = true;
                    break;
                }
            }
            if (!hasAnyMovable) return;

            RecycleBinIcon desktopBin = RecycleBinIcon.FindBinAtScreenPoint(eventData.position, eventData.pressEventCamera);
            if (desktopBin != null)
            {
                MoveBatchToRecycleBin(dragItems);
                return;
            }

            if (EventSystem.current == null) return;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool hitAnyWindow = false;

            foreach (var res in results)
            {
                if (res.gameObject == null) continue;

                if (res.gameObject.GetComponentInParent<Window>() != null)
                {
                    hitAnyWindow = true;
                }

                ExplorerItemPointerHandler targetItemHandler = res.gameObject.GetComponentInParent<ExplorerItemPointerHandler>();
                if (targetItemHandler != null && targetItemHandler != this && targetItemHandler.ItemData != null)
                {
                    if (targetItemHandler.ItemData.fileType == ExplorerFileType.Folder && !dragItems.Contains(targetItemHandler.ItemData))
                    {
                        if (targetItemHandler.ItemData.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                        {
                            MoveBatchToRecycleBin(dragItems);
                        }
                        else
                        {
                            MoveBatchToFolder(dragItems, targetItemHandler.ItemData);
                        }
                        return;
                    }
                }

                DraggableIcon desktopIcon = res.gameObject.GetComponentInParent<DraggableIcon>();
                if (desktopIcon != null && desktopIcon.GetComponent<RecycleBinIcon>() == null)
                {
                    if (desktopIcon.ItemType == DesktopItemType.Folder && desktopIcon.LinkedExplorerItem != null &&
                        !dragItems.Contains(desktopIcon.LinkedExplorerItem))
                    {
                        MoveBatchToFolder(dragItems, desktopIcon.LinkedExplorerItem);
                        return;
                    }
                }
            }

            foreach (var res in results)
            {
                if (res.gameObject == null) continue;
                FileExplorerApp targetExplorer = res.gameObject.GetComponentInParent<FileExplorerApp>();
                if (targetExplorer != null && targetExplorer != explorer && targetExplorer.CurrentFolder != null)
                {
                    if (targetExplorer.CurrentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                    {
                        MoveBatchToRecycleBin(dragItems);
                    }
                    else
                    {
                        MoveBatchToFolder(dragItems, targetExplorer.CurrentFolder);
                    }
                    return;
                }
            }

            if (!hitAnyWindow)
            {
                if (explorer != null && explorer.CurrentFolder != null &&
                    explorer.CurrentFolder.name.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
                if (desktopFolder != null)
                {
                    MoveBatchToFolder(dragItems, desktopFolder, eventData.position);
                }
            }
        }

        private void MoveBatchToRecycleBin(List<ExplorerItemData> dragItems)
        {
            if (explorer != null && explorer.CurrentFolder != null &&
                explorer.CurrentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int movedCount = FileExplorerApp.MoveItemsToRecycleBin(dragItems, out int skippedProtected);
            if (movedCount > 0)
            {
                RecycleBinIcon.UpdateAllBinVisuals();
                RecycleBinIcon.NotifyIconRecycled(null);
                string msg = $"Moved {movedCount} item{(movedCount == 1 ? "" : "s")} to Recycle Bin.";
                if (skippedProtected > 0) msg += $" ({skippedProtected} protected skipped)";
                explorer?.ShowStatusNotice(msg);
                FeedbackManager.PlayClick();
            }
            else if (skippedProtected > 0)
            {
                explorer?.ShowStatusNotice("System items are protected and cannot be deleted.", true);
            }
        }

        private void MoveBatchToFolder(List<ExplorerItemData> dragItems, ExplorerItemData targetFolder, Vector2? desktopDropScreenPos = null)
        {
            int movedCount = FileExplorerApp.MoveItemsToFolder(dragItems, targetFolder, out int skippedProtected, out string err, desktopDropScreenPos);
            if (movedCount > 0)
            {
                string msg = $"Moved {movedCount} item{(movedCount == 1 ? "" : "s")} to '{targetFolder.name}'.";
                if (skippedProtected > 0) msg += $" ({skippedProtected} protected skipped)";
                explorer?.ShowStatusNotice(msg);
                FeedbackManager.PlaySuccess();
            }
            else if (!string.IsNullOrEmpty(err))
            {
                explorer?.ShowStatusNotice(err, true);
            }
            else if (skippedProtected > 0)
            {
                explorer?.ShowStatusNotice("System items are protected and cannot be moved.", true);
            }
        }

        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (dragGhost == null || dragCanvas == null) return;

            RectTransform canvasRect = dragCanvas.transform as RectTransform;
            if (canvasRect == null) return;

            Camera cam = eventData.pressEventCamera;
            if (cam == null && dragCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = dragCanvas.worldCamera != null ? dragCanvas.worldCamera : Camera.main;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPos))
            {
                RectTransform rt = dragGhost.transform as RectTransform;
                rt.anchorMin = canvasRect.pivot;
                rt.anchorMax = canvasRect.pivot;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = localPos;
                dragGhost.transform.SetAsLastSibling();
            }
        }
    }
}
