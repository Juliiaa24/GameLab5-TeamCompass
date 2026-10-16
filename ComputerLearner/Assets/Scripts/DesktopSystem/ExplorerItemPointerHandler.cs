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
    public class ExplorerItemPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FileExplorerApp explorer;
        [NonSerialized] private ExplorerItemData itemData;
        private bool isUnlocked = true;
        private float lastLeftClickTime;
        private GameObject dragGhost;
        private Image cardBg;

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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (itemData == null || itemData.isProtected) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

            dragGhost = new GameObject($"Drag_{itemData.name}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragGhost.transform.SetParent(rootCanvas.transform, false);
            dragGhost.transform.SetAsLastSibling();

            RectTransform rt = dragGhost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(56f, 56f);

            CanvasGroup cg = dragGhost.GetComponent<CanvasGroup>();
            cg.alpha = 0.80f;
            cg.blocksRaycasts = false;

            Image ghostImg = dragGhost.GetComponent<Image>();
            Image sourceImg = transform.Find("Icon")?.GetComponent<Image>();
            if (sourceImg != null) ghostImg.sprite = sourceImg.sprite;
            ghostImg.preserveAspect = true;
            ghostImg.raycastTarget = false;

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
            if (itemData == null || itemData.isProtected) return;

            RecycleBinIcon desktopBin = RecycleBinIcon.FindBinAtScreenPoint(eventData.position, eventData.pressEventCamera);
            if (desktopBin != null)
            {
                FileExplorerApp.MoveItemToRecycleBin(itemData);
                Debug.Log($"[RecycleBin] Recycled explorer item '{itemData.name}' to Recycle Bin.");
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
                    if (targetItemHandler.ItemData.fileType == ExplorerFileType.Folder)
                    {
                        if (targetItemHandler.ItemData.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                        {
                            FileExplorerApp.MoveItemToRecycleBin(itemData);
                        }
                        else
                        {
                            FileExplorerApp.MoveItemToFolder(itemData, targetItemHandler.ItemData);
                        }
                        return;
                    }
                }

                DraggableIcon desktopIcon = res.gameObject.GetComponentInParent<DraggableIcon>();
                if (desktopIcon != null && desktopIcon.GetComponent<RecycleBinIcon>() == null)
                {
                    if (desktopIcon.ItemType == DesktopItemType.Folder && desktopIcon.LinkedExplorerItem != null)
                    {
                        FileExplorerApp.MoveItemToFolder(itemData, desktopIcon.LinkedExplorerItem);
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
                        FileExplorerApp.MoveItemToRecycleBin(itemData);
                    }
                    else
                    {
                        FileExplorerApp.MoveItemToFolder(itemData, targetExplorer.CurrentFolder);
                    }
                    return;
                }
            }

            if (!hitAnyWindow)
            {
                ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
                if (desktopFolder != null)
                {
                    FileExplorerApp.MoveItemToFolder(itemData, desktopFolder, eventData.position);
                }
            }
        }

        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (dragGhost == null) return;
            Canvas rootCanvas = dragGhost.GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            Camera cam = rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPos))
            {
                (dragGhost.transform as RectTransform).anchoredPosition = localPos;
            }
        }
    }
}
