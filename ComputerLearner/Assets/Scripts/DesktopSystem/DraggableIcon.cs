/**
 * Author: Julia Vera / Diego
 * Date: 16/10/2026
 * Description: Draggable desktop icon with one window instance per icon, English context menu,
 *              Paint/Notepad/Explorer integration, and bidirectional Drag & Drop (to Recycle Bin & File Explorer).
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Public Variables
        public Window AppWindow => appWindow;
        public LevelDefinition LevelDef => levelDefinition;
        public bool AllowDoubleClick { get; set; } = true;
        public DesktopItemType ItemType { get; private set; }
        public bool IsProtected { get; private set; } = true;
        [field: NonSerialized] public ExplorerItemData LinkedExplorerItem { get; private set; }
        public bool IsSelected => isSelected;
        #endregion

        #region Static Selection
        public static DraggableIcon SelectedIcon { get; private set; }
        private static DraggableIcon hoveredIcon;

        private static GameObject selectionCard;
        private static UnityEngine.UI.Image selectionCardImg;
        private static RectTransform selectionCardRect;

        private static GameObject hoverCard;
        private static UnityEngine.UI.Image hoverCardImg;
        private static RectTransform hoverCardRect;

        public static void SelectIcon(DraggableIcon icon)
        {
            if (SelectedIcon == icon)
            {
                if (icon != null) icon.UpdateSelectionCardTransform();
                return;
            }

            if (SelectedIcon != null)
            {
                SelectedIcon.isSelected = false;
            }

            SelectedIcon = icon;

            if (SelectedIcon != null)
            {
                SelectedIcon.isSelected = true;
                if (hoveredIcon == SelectedIcon && hoverCard != null)
                {
                    hoverCard.SetActive(false);
                }

                EnsureSelectionCard(SelectedIcon.transform.parent);
                if (selectionCardImg != null)
                {
                    selectionCardImg.color = SelectedIcon.selectedColor;
                }
                if (selectionCard != null)
                {
                    selectionCard.SetActive(true);
                }
                SelectedIcon.UpdateSelectionCardTransform();
            }
            else
            {
                if (selectionCard != null) selectionCard.SetActive(false);
            }
        }

        public static void ClearSelection()
        {
            if (SelectedIcon != null)
            {
                SelectedIcon.isSelected = false;
                SelectedIcon = null;
            }
            if (selectionCard != null)
            {
                selectionCard.SetActive(false);
            }
        }

        private static void EnsureSelectionCard(Transform parent)
        {
            if (selectionCard == null || selectionCardImg == null || selectionCardRect == null)
            {
                if (parent != null)
                {
                    Transform existing = parent.Find("DesktopSelectionHighlightCard");
                    if (existing != null)
                    {
                        selectionCard = existing.gameObject;
                        selectionCardImg = selectionCard.GetComponent<UnityEngine.UI.Image>();
                        selectionCardRect = selectionCard.GetComponent<RectTransform>();
                    }
                }

                if (selectionCard == null)
                {
                    selectionCard = new GameObject("DesktopSelectionHighlightCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    selectionCardImg = selectionCard.GetComponent<UnityEngine.UI.Image>();
                    selectionCardRect = selectionCard.GetComponent<RectTransform>();
                    selectionCardImg.raycastTarget = false;
                }
            }

            if (parent != null && selectionCard.transform.parent != parent)
            {
                selectionCard.transform.SetParent(parent, false);
            }
        }

        private static void EnsureHoverCard(Transform parent)
        {
            if (hoverCard == null || hoverCardImg == null || hoverCardRect == null)
            {
                if (parent != null)
                {
                    Transform existing = parent.Find("DesktopHoverHighlightCard");
                    if (existing != null)
                    {
                        hoverCard = existing.gameObject;
                        hoverCardImg = hoverCard.GetComponent<UnityEngine.UI.Image>();
                        hoverCardRect = hoverCard.GetComponent<RectTransform>();
                    }
                }

                if (hoverCard == null)
                {
                    hoverCard = new GameObject("DesktopHoverHighlightCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    hoverCardImg = hoverCard.GetComponent<UnityEngine.UI.Image>();
                    hoverCardRect = hoverCard.GetComponent<RectTransform>();
                    hoverCardImg.raycastTarget = false;
                }
            }

            if (parent != null && hoverCard.transform.parent != parent)
            {
                hoverCard.transform.SetParent(parent, false);
            }
        }

        private static void PlaceBehind(Transform card, Transform target)
        {
            if (card == null || target == null || card.parent != target.parent) return;

            int targetIndex = target.GetSiblingIndex();
            int cardIndex = card.GetSiblingIndex();

            if (cardIndex == targetIndex - 1) return;

            if (cardIndex < targetIndex)
            {
                card.SetSiblingIndex(targetIndex - 1);
            }
            else
            {
                card.SetSiblingIndex(targetIndex);
            }
        }
        #endregion

        #region Private Variables
        private IconGrid grid;
        [SerializeField] private GameObject windowPrefab;
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private bool isLocked = false;

        [Header("Selection & Hover Visuals (Translucent Desktop Styling)")]
        [SerializeField] private Color selectedColor = new Color(144f / 255f, 198f / 255f, 244f / 255f, 0.45f); // Translucent sky-blue (Windows desktop parity)
        [SerializeField] private Color hoverColor = new Color(184f / 255f, 220f / 255f, 248f / 255f, 0.25f);    // Soft translucent hover

        private bool isHovered;
        private bool isSelected;
        private float lastLeftClickTime;

        private Window appWindow;
        private WindowManager manager;
        private Vector3 initialPosition;
        private Vector3 originalScale;
        private Color originalColor = Color.white;
        private bool dragging;
        private bool dropHandled;
        private CanvasGroup canvasGroup;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
                return canvasGroup;
            }
        }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            CleanupOldChildHighlight();

            // Translucent desktop selection and hover styling (Windows desktop parity)
            selectedColor = new Color(144f / 255f, 198f / 255f, 244f / 255f, 0.45f);
            hoverColor = new Color(184f / 255f, 220f / 255f, 248f / 255f, 0.25f);

            // Standardize icon sizing and text clearance
            RectTransform iconRect = transform as RectTransform;
            if (iconRect != null && (Mathf.Approximately(iconRect.sizeDelta.x, 120f) || iconRect.sizeDelta.x > 105f))
            {
                iconRect.sizeDelta = new Vector2(105f, 105f);
            }

            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                if (label.rectTransform.anchoredPosition.y < -65f || Mathf.Approximately(label.rectTransform.anchoredPosition.y, -70f))
                {
                    label.rectTransform.anchoredPosition = new Vector2(0f, -65f);
                }
                if (label.rectTransform.sizeDelta.x < 280f)
                {
                    label.rectTransform.sizeDelta = new Vector2(290f, 60f);
                }
            }
        }

        private void Start()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            CleanupOldChildHighlight();

            grid = GetComponentInParent<IconGrid>();
            if (grid != null) grid.Register(this);
            manager = GetComponentInParent<WindowManager>();
            initialPosition = transform.position;
            originalScale = transform.localScale;

            UnityEngine.UI.Image iconImg = GetComponent<UnityEngine.UI.Image>();
            if (iconImg != null) originalColor = iconImg.color;

            bool actuallyLocked = isLocked;
            if (levelDefinition != null)
            {
                TMPro.TextMeshProUGUI lvlLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (lvlLabel != null) lvlLabel.text = levelDefinition.GetLocalizedDisplayName();
                if (ProgressData.Instance != null)
                    actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            }

            if (actuallyLocked)
            {
                if (iconImg != null) iconImg.color = new Color(0.28f, 0.28f, 0.28f, 0.7f);
                if (canvasGroup != null) canvasGroup.alpha = 0.6f;
                TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }

        private void Update()
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);

            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = actuallyLocked ? new Color(0.28f, 0.28f, 0.28f, 0.7f) : originalColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = actuallyLocked ? 0.6f : 1f;
            }

            TMPro.TextMeshProUGUI textLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textLabel != null)
            {
                textLabel.color = actuallyLocked ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : Color.white;
            }

            bool shouldPulse = false;
            string currentNextLevel = ProgressData.Instance != null ? ProgressData.Instance.GetCurrentNextLevelId() : null;

            if (levelDefinition != null)
            {
                shouldPulse = !actuallyLocked && levelDefinition.levelId == currentNextLevel && (appWindow == null || appWindow.IsClosed);
            }
            else if (gameObject.name.Equals("Levels", System.StringComparison.OrdinalIgnoreCase) || (contentPrefab != null && contentPrefab.name.Contains("Levels")))
            {
                shouldPulse = !string.IsNullOrEmpty(currentNextLevel) && (appWindow == null || appWindow.IsClosed);
            }

            if (shouldPulse)
            {
                float scale = 1f + Mathf.Sin(Time.time * 3f) * 0.06f;
                transform.localScale = originalScale * scale;
            }
            else
            {
                transform.localScale = originalScale;
            }

            if (SelectedIcon == this && selectionCard != null && selectionCard.activeSelf)
            {
                UpdateSelectionCardTransform();
            }
            else if (hoveredIcon == this && hoverCard != null && hoverCard.activeSelf)
            {
                UpdateHoverCardTransform();
            }
        }

        private void OnDestroy()
        {
            if (SelectedIcon == this)
            {
                ClearSelection();
            }
            if (hoveredIcon == this)
            {
                hoveredIcon = null;
                if (hoverCard != null) hoverCard.SetActive(false);
            }
            if (grid != null) grid.Unregister(this);
            if (DesktopManager.Instance != null) DesktopManager.Instance.UnregisterIcon(this);
        }
        #endregion

        #region Public Methods
        public void SetupDynamicIcon(GameObject winPrefab, GameObject contentPref, LevelDefinition levelDef, bool lockedState)
        {
            this.windowPrefab = winPrefab;
            this.contentPrefab = contentPref;
            this.levelDefinition = levelDef;
            this.isLocked = lockedState;
            if (this.levelDefinition != null)
            {
                TMPro.TextMeshProUGUI lvlLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (lvlLabel != null) lvlLabel.text = this.levelDefinition.GetLocalizedDisplayName();
            }

            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                if (!lockedState) img.color = Color.white;
                originalColor = img.color;
            }
        }

        public void ConfigureMetadata(DesktopItemType type, bool isProtected, ExplorerItemData linkedItem)
        {
            this.ItemType = type;
            string currentName = gameObject.name;
            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && !string.IsNullOrWhiteSpace(label.text))
            {
                currentName = label.text.Trim();
            }

            if (DesktopManager.IsSystemProtectedName(currentName))
            {
                this.IsProtected = true;
            }
            else if (type == DesktopItemType.TextDocument || type == DesktopItemType.ImageDocument)
            {
                this.IsProtected = false;
            }
            else if (linkedItem != null)
            {
                this.IsProtected = linkedItem.isProtected;
            }
            else
            {
                this.IsProtected = isProtected;
            }
            this.LinkedExplorerItem = linkedItem;
            if (this.LinkedExplorerItem != null)
            {
                this.LinkedExplorerItem.isProtected = this.IsProtected;
            }
        }

        public void MarkDropHandled()
        {
            dropHandled = true;
        }

        public void ReturnToInitialPosition()
        {
            transform.position = initialPosition;
            CanvasGroup.blocksRaycasts = true;
            dragging = false;
            UpdateSelectionCardTransform();
            if (grid != null)
            {
                grid.Unregister(this);
                grid.Register(this);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SelectIcon(this);
            }

            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            if (actuallyLocked) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;
            initialPosition = transform.position;
            dragging = false;
            dropHandled = false;
            CanvasGroup.blocksRaycasts = true;
            transform.SetAsLastSibling();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            CanvasGroup.blocksRaycasts = true;
            dragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            SelectIcon(this);
            dragging = true;
            dropHandled = false;
            CanvasGroup.blocksRaycasts = false;
            if (grid != null) grid.Unregister(this);
            transform.SetAsLastSibling();
            UpdateSelectionCardTransform();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            dragging = true;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 point))
            {
                transform.position = point;
                UpdateSelectionCardTransform();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            CanvasGroup.blocksRaycasts = true;
            UpdateSelectionCardTransform();

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (dropHandled)
            {
                return;
            }

            // 1. Check if dropped on the Recycle Bin icon on the Desktop
            if (GetComponent<RecycleBinIcon>() == null)
            {
                RecycleBinIcon targetBin = RecycleBinIcon.FindBinAtScreenPoint(eventData.position, eventData.pressEventCamera, transform as RectTransform);
                if (targetBin != null && targetBin.gameObject != this.gameObject)
                {
                    dropHandled = true;
                    if (targetBin.HandleDesktopIconDropped(this))
                    {
                        CanvasGroup.blocksRaycasts = true;
                        dragging = false;
                        return;
                    }
                }
            }

            CanvasGroup.blocksRaycasts = true;

            // 2. Check if dropped into an open FileExplorerApp window or another folder icon via RaycastAll
            if (EventSystem.current != null && !IsProtected)
            {
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                // First check if dropped directly onto a folder item inside FileExplorerApp
                foreach (var res in results)
                {
                    if (res.gameObject == null || res.gameObject == this.gameObject) continue;

                    ExplorerItemPointerHandler itemHandler = res.gameObject.GetComponentInParent<ExplorerItemPointerHandler>();
                    if (itemHandler != null && itemHandler.ItemData != null && itemHandler.ItemData.fileType == ExplorerFileType.Folder)
                    {
                        ExplorerItemData targetFolder = itemHandler.ItemData;
                        if (targetFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                        {
                            DeleteDesktopIconToRecycleBin();
                            return;
                        }
                        if (MoveDesktopIconIntoFolder(targetFolder))
                        {
                            return;
                        }
                    }
                }

                // Next check if dropped inside a FileExplorerApp window's general content area
                foreach (var res in results)
                {
                    if (res.gameObject == null || res.gameObject == this.gameObject) continue;

                    FileExplorerApp explorer = res.gameObject.GetComponentInParent<FileExplorerApp>();
                    if (explorer != null && explorer.CurrentFolder != null)
                    {
                        ExplorerItemData targetFolder = explorer.CurrentFolder;
                        if (targetFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                        {
                            DeleteDesktopIconToRecycleBin();
                            return;
                        }
                        if (MoveDesktopIconIntoFolder(targetFolder))
                        {
                            return;
                        }
                    }
                }

                // Next check if dropped onto another Folder DraggableIcon on the Desktop
                foreach (var res in results)
                {
                    if (res.gameObject == null || res.gameObject == this.gameObject) continue;

                    DraggableIcon otherIcon = res.gameObject.GetComponentInParent<DraggableIcon>();
                    if (otherIcon != null && otherIcon != this && otherIcon.GetComponent<RecycleBinIcon>() == null)
                    {
                        if (otherIcon.ItemType == DesktopItemType.Folder && otherIcon.LinkedExplorerItem != null &&
                            otherIcon.LinkedExplorerItem != LinkedExplorerItem)
                        {
                            if (MoveDesktopIconIntoFolder(otherIcon.LinkedExplorerItem))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            // 3. Otherwise place on the Desktop IconGrid at the new drop position
            if (grid != null)
            {
                grid.TryPlaceIcon(this, eventData.position, initialPosition, eventData.pressEventCamera);
            }
        }

        private bool MoveDesktopIconIntoFolder(ExplorerItemData targetFolder)
        {
            if (targetFolder == null || IsProtected) return false;

            // Prevent dropping a folder into itself
            if (LinkedExplorerItem != null && LinkedExplorerItem == targetFolder) return false;

            // If dropped into "Desktop" folder, keep it on Desktop
            if (targetFolder.name.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Prevent dropping into read-only system folders like Levels/Minigames/This PC root
            if (targetFolder.name.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                targetFolder.name.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                targetFolder.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                targetFolder.name.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) ||
                targetFolder.name.Equals("Minigames", StringComparison.OrdinalIgnoreCase))
            {
                ReturnToInitialPosition();
                return true;
            }

            EnsureLinkedExplorerItem();
            if (LinkedExplorerItem != null)
            {
                if (FileExplorerApp.MoveItemToFolder(LinkedExplorerItem, targetFolder))
                {
                    if (appWindow != null && !appWindow.IsClosed)
                    {
                        Destroy(appWindow.gameObject);
                    }
                    Destroy(gameObject);
                    return true;
                }
            }
            return false;
        }

        private void EnsureLinkedExplorerItem()
        {
            if (LinkedExplorerItem != null) return;

            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            string itemName = label != null && !string.IsNullOrWhiteSpace(label.text) ? label.text.Trim() : gameObject.name;

            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
            if (desktopFolder != null && desktopFolder.children != null)
            {
                foreach (var child in desktopFolder.children)
                {
                    if (child != null && child.name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                    {
                        LinkedExplorerItem = child;
                        return;
                    }
                }
            }

            ExplorerFileType fType = ExplorerFileType.Folder;
            if (ItemType == DesktopItemType.TextDocument || itemName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                fType = ExplorerFileType.TextFile;
            else if (ItemType == DesktopItemType.ImageDocument || itemName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                fType = ExplorerFileType.ImageFile;

            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            Sprite iconSpr = fType == ExplorerFileType.ImageFile
                ? FileExplorerApp.GetImageFileSprite()
                : (img != null ? img.sprite : null);

            LinkedExplorerItem = new ExplorerItemData
            {
                name = itemName,
                fileType = fType,
                icon = iconSpr,
                isProtected = IsProtected
            };
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                SelectIcon(this);
                ShowIconContextMenu(eventData.position);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SelectIcon(this);

                if (!AllowDoubleClick) return;

                float now = Time.unscaledTime;
                bool isDoubleClick = eventData.clickCount >= 2 || (now - lastLeftClickTime < 0.45f);
                lastLeftClickTime = now;

                if (isDoubleClick)
                {
                    lastLeftClickTime = 0f;
                    dragging = false;
                    OpenApplication();
                }
            }
        }

        private void ShowIconContextMenu(Vector2 screenPosition)
        {
            if (ContextMenuController.Instance == null) return;

            var items = new List<ContextMenuItem>();
            RecycleBinIcon bin = GetComponent<RecycleBinIcon>();

            if (bin != null || gameObject.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new ContextMenuItem("Open Recycle Bin", () =>
                {
                    if (windowPrefab != null && contentPrefab != null)
                    {
                        OpenApplication();
                    }
                    else if (StartMenuController.Instance != null)
                    {
                        StartMenuController.Instance.OpenFileExplorerAtFolder("Recycle Bin");
                    }
                }, true));

                int binCount = FileExplorerApp.GetRecycleBinCount();
                items.Add(new ContextMenuItem($"Empty Recycle Bin ({binCount})", () =>
                {
                    FileExplorerApp.EmptyRecycleBin();
                }, binCount > 0));
            }
            else
            {
                items.Add(new ContextMenuItem("Open", OpenApplication, true));

                bool isImage = (LinkedExplorerItem != null && LinkedExplorerItem.fileType == ExplorerFileType.ImageFile) ||
                               ItemType == DesktopItemType.ImageDocument ||
                               gameObject.name.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

                if (isImage)
                {
                    items.Add(new ContextMenuItem("Open in Paint", () =>
                    {
                        EnsureLinkedExplorerItem();
                        StartMenuController c = StartMenuController.Instance ?? FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);
                        if (c != null) c.OpenPaintWithFile(LinkedExplorerItem, FileExplorerApp.GetDesktopFolder());
                    }, true));
                }

                bool isFolder = (ItemType == DesktopItemType.Folder) ||
                                (LinkedExplorerItem != null && LinkedExplorerItem.fileType == ExplorerFileType.Folder) ||
                                gameObject.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                                gameObject.name.Equals("Files", StringComparison.OrdinalIgnoreCase);

                if (isFolder)
                {
                    items.Add(new ContextMenuItem("Change Folder Color", () =>
                    {
                        ContextMenuController.Instance.ShowFolderColorPicker(screenPosition, (selectedSprite) =>
                        {
                            if (selectedSprite == null) return;
                            EnsureLinkedExplorerItem();
                            if (LinkedExplorerItem != null)
                            {
                                LinkedExplorerItem.icon = selectedSprite;
                                FileExplorerApp.NotifyFileSystemChanged();
                            }
                            UnityEngine.UI.Image iconImg = GetComponent<UnityEngine.UI.Image>();
                            if (iconImg != null)
                            {
                                iconImg.sprite = selectedSprite;
                            }
                            FeedbackManager.PlaySuccess();
                        });
                    }, true));
                }

                bool canModify = !IsProtected;
                items.Add(new ContextMenuItem("Rename", PromptRenameDesktopIcon, canModify));
                items.Add(new ContextMenuItem("Delete", DeleteDesktopIconToRecycleBin, canModify));
            }

            ContextMenuController.Instance.Show(screenPosition, items);
        }

        public void PromptRenameDesktopIcon()
        {
            if (IsProtected || ContextMenuController.Instance == null) return;

            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            string currentName = label != null ? label.text : gameObject.name;

            ContextMenuController.Instance.ShowInputModal("Rename", currentName, "Save", (newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;
                newName = newName.Trim();

                if (ItemType == DesktopItemType.TextDocument && !newName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    newName += ".txt";
                }
                else if (ItemType == DesktopItemType.ImageDocument && !newName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    newName += ".png";
                }

                gameObject.name = newName;
                if (label != null) label.text = newName;

                if (LinkedExplorerItem != null)
                {
                    LinkedExplorerItem.name = newName;
                    FileExplorerApp.NotifyFileSystemChanged();
                }

                if (appWindow != null && !appWindow.IsClosed)
                {
                    appWindow.setTitle(newName);
                }
            });
        }

        public void DeleteDesktopIconToRecycleBin()
        {
            if (IsProtected) return;

            EnsureLinkedExplorerItem();
            if (LinkedExplorerItem != null)
            {
                FileExplorerApp.MoveItemToRecycleBin(LinkedExplorerItem);
            }

            if (appWindow != null && !appWindow.IsClosed)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(appWindow.gameObject);
                else Destroy(appWindow.gameObject);
#else
                Destroy(appWindow.gameObject);
#endif
            }

            if (DesktopManager.Instance != null)
            {
                DesktopManager.Instance.UnregisterIcon(this);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(gameObject);
            else Destroy(gameObject);
#else
            Destroy(gameObject);
#endif
        }

        public void OpenApplication()
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            if (actuallyLocked) return;

            if (!DesktopTour.IsTourRunning)
            {
                VirtualMascot.HideMascot();
            }

            StartMenuController smc = StartMenuController.Instance ?? FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);

            // 1. Check if this is the Paint desktop application shortcut
            TMPro.TextMeshProUGUI currentLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            string labelName = currentLabel != null && !string.IsNullOrWhiteSpace(currentLabel.text) ? currentLabel.text.Trim() : gameObject.name;

            if (labelName.Equals("Paint", StringComparison.OrdinalIgnoreCase))
            {
                if (smc != null)
                {
                    smc.OpenPaint();
                    return;
                }
            }

            // 2. Check if this is the Notepad desktop application shortcut
            if (labelName.Equals("Notepad", StringComparison.OrdinalIgnoreCase))
            {
                if (smc != null)
                {
                    smc.OpenNotepad();
                    return;
                }
            }

            // 3. Check if this is a user-created or linked text document -> Open in Notepad
            if (LinkedExplorerItem != null && LinkedExplorerItem.fileType == ExplorerFileType.TextFile)
            {
                if (smc != null)
                {
                    smc.OpenNotepadWithFile(LinkedExplorerItem, FileExplorerApp.GetDesktopFolder());
                    return;
                }
            }

            // 4. Check if this is a user-created or linked .png image file -> Open in Image Viewer
            if ((LinkedExplorerItem != null && LinkedExplorerItem.fileType == ExplorerFileType.ImageFile) ||
                ItemType == DesktopItemType.ImageDocument ||
                labelName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                EnsureLinkedExplorerItem();
                if (smc != null)
                {
                    smc.OpenImageViewer(LinkedExplorerItem);
                    return;
                }
            }

            // Fallback: If contentPrefab is null for core apps, resolve from smc or DesktopManager
            if (contentPrefab == null)
            {
                if (labelName.Equals("Paint", StringComparison.OrdinalIgnoreCase))
                {
                    if (smc != null && smc.PaintPrefab != null) contentPrefab = smc.PaintPrefab;
                    else if (DesktopManager.Instance != null) contentPrefab = DesktopManager.Instance.PaintContentPrefab;
                }
                else if (labelName.Equals("Notepad", StringComparison.OrdinalIgnoreCase))
                {
                    if (smc != null && smc.NotepadPrefab != null) contentPrefab = smc.NotepadPrefab;
                    else if (DesktopManager.Instance != null) contentPrefab = DesktopManager.Instance.NotepadContentPrefab;
                }
                else if (labelName.Equals("Files", StringComparison.OrdinalIgnoreCase) || labelName.Equals("This PC", StringComparison.OrdinalIgnoreCase))
                {
                    if (smc != null && smc.FileExplorerPrefab != null) contentPrefab = smc.FileExplorerPrefab;
                }
                else if (labelName.Equals("Scores", StringComparison.OrdinalIgnoreCase) ||
                         labelName.Equals("Medals", StringComparison.OrdinalIgnoreCase) ||
                         labelName.Equals("High Scores", StringComparison.OrdinalIgnoreCase) ||
                         labelName.Equals("Leaderboard", StringComparison.OrdinalIgnoreCase))
                {
                    if (DesktopManager.Instance != null && DesktopManager.Instance.ScoresContentPrefab != null)
                        contentPrefab = DesktopManager.Instance.ScoresContentPrefab;
                }
            }

            if (windowPrefab == null && DesktopManager.Instance != null)
            {
                windowPrefab = DesktopManager.Instance.BaseWindowPrefab;
            }

            if (manager == null) manager = GetComponentInParent<WindowManager>();

            string resolvedTitle = null;
            if (levelDefinition != null)
            {
                resolvedTitle = levelDefinition.GetLocalizedDisplayName();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(labelName))
                {
                    resolvedTitle = labelName;
                }
                else
                {
                    resolvedTitle = gameObject.name.Replace("Icon", "").Trim();
                }

                if (resolvedTitle.Equals("Files", System.StringComparison.OrdinalIgnoreCase))
                {
                    resolvedTitle = "File Explorer";
                }
            }

            bool isNewWindow = false;

            if (manager != null)
            {
                if (appWindow == null || appWindow.IsClosed) isNewWindow = true;
                appWindow = manager.OpenWindow(windowPrefab, appWindow, resolvedTitle);
            }
            else if (appWindow != null && !appWindow.IsClosed)
            {
                appWindow.Restore();
            }
            else if (windowPrefab != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    appWindow = Instantiate(windowPrefab, canvas.transform).GetComponent<Window>();
                    isNewWindow = true;
                }
            }

            if (isNewWindow && appWindow != null)
            {
                appWindow.setTitle(resolvedTitle);

                if (contentPrefab != null)
                {
                    GameObject contentInstance = Instantiate(contentPrefab);
                    appWindow.SetContent(contentInstance);

                    if (LinkedExplorerItem != null)
                    {
                        if (LinkedExplorerItem.fileType == ExplorerFileType.TextFile)
                        {
                            NotepadApp notepad = contentInstance.GetComponentInChildren<NotepadApp>();
                            if (notepad != null) notepad.LoadFile(LinkedExplorerItem, appWindow, FileExplorerApp.GetDesktopFolder());
                        }
                        else if (LinkedExplorerItem.fileType == ExplorerFileType.Folder)
                        {
                            FileExplorerApp explorer = contentInstance.GetComponentInChildren<FileExplorerApp>();
                            if (explorer != null) explorer.OpenFolder(LinkedExplorerItem);
                        }
                    }
                    else if (ItemType == DesktopItemType.Folder)
                    {
                        FileExplorerApp explorer = contentInstance.GetComponentInChildren<FileExplorerApp>();
                        if (explorer != null) explorer.OpenFolderByName(resolvedTitle);
                    }
                }

                if (windowPrefab != null && windowPrefab.name.Contains("SkillsReport"))
                {
                    if (appWindow.GetComponent<StatsUIBuilder>() == null)
                        appWindow.gameObject.AddComponent<StatsUIBuilder>();
                }

                if (resolvedTitle.Equals("Scores", StringComparison.OrdinalIgnoreCase) ||
                    resolvedTitle.Equals("Medals", StringComparison.OrdinalIgnoreCase) ||
                    resolvedTitle.Equals("High Scores", StringComparison.OrdinalIgnoreCase) ||
                    resolvedTitle.Equals("Leaderboard", StringComparison.OrdinalIgnoreCase))
                {
                    if (appWindow.GetComponentInChildren<ScoresApp>() == null)
                    {
                        GameObject scoresContent = new GameObject("ScoresContent", typeof(RectTransform));
                        scoresContent.AddComponent<ScoresApp>();
                        appWindow.SetContent(scoresContent);
                    }
                }
            }

            if (isNewWindow && appWindow != null && levelDefinition != null)
            {
                appWindow.setTitle(levelDefinition.GetLocalizedDisplayName());

                if (!DesktopTour.IsTourRunning)
                {
                    appWindow.toggleMaximize();
                }

                if (levelDefinition.levelId == "assessment")
                {
                    InitialTestManager runner = appWindow.GetComponentInChildren<InitialTestManager>();
                    if (runner == null && InitialTestManager.Instance != null)
                    {
                        runner = InitialTestManager.Instance;
                    }
                    else if (runner == null)
                    {
                        runner = appWindow.gameObject.AddComponent<InitialTestManager>();
                    }

                    if (!DesktopTour.IsTourRunning)
                    {
                        runner.StartAssessment(levelDefinition, appWindow);
                    }
                }
                else
                {
                    LevelRunner runner = appWindow.GetComponentInChildren<LevelRunner>();
                    if (runner == null)
                    {
                        runner = appWindow.gameObject.AddComponent<LevelRunner>();
                        runner.taskContentArea = appWindow.ContentArea;

                        if (runner.taskContentArea == null)
                            runner.taskContentArea = appWindow.GetComponent<RectTransform>();
                    }

                    if (!DesktopTour.IsTourRunning)
                    {
                        runner.StartLevel(levelDefinition);
                    }
                }
            }
        }
        #endregion

        #region Selection & Highlight Logic
        private void UpdateCardTransform(GameObject cardGo, RectTransform cardRt)
        {
            if (cardGo == null || !cardGo.activeSelf || cardRt == null) return;

            RectTransform iconRect = transform as RectTransform;
            if (iconRect == null) return;

            if (cardGo.transform.parent != transform.parent)
            {
                cardGo.transform.SetParent(transform.parent, false);
            }

            cardRt.anchorMin = iconRect.anchorMin;
            cardRt.anchorMax = iconRect.anchorMax;
            cardRt.pivot = iconRect.pivot;

            float spriteHalfHeight = iconRect.rect.height > 0 ? iconRect.rect.height * (1f - iconRect.pivot.y) : 52.5f;
            float topY = spriteHalfHeight + 7f;
            float textBottom = -spriteHalfHeight - 20f;
            float textWidth = iconRect.rect.width;

            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                Vector2 rendered = label.GetRenderedValues(false);
                float textH = rendered.y * label.transform.localScale.y;
                textWidth = rendered.x * label.transform.localScale.x;
                textBottom = label.rectTransform.anchoredPosition.y - textH * 0.5f;
            }

            float bottomY = textBottom - 8f;
            float cardHeight = Mathf.Max(135f, topY - bottomY);
            float centerY = (topY + bottomY) * 0.5f;
            float cardWidth = Mathf.Clamp(Mathf.Max(iconRect.rect.width + 24f, textWidth + 24f), 126f, 150f);

            cardRt.anchoredPosition = iconRect.anchoredPosition + new Vector2(0f, centerY);
            cardRt.sizeDelta = new Vector2(cardWidth, cardHeight);
            cardGo.transform.localScale = transform.localScale;

            PlaceBehind(cardGo.transform, transform);
        }

        public void UpdateSelectionCardTransform()
        {
            UpdateCardTransform(selectionCard, selectionCardRect);
        }

        public void UpdateHoverCardTransform()
        {
            UpdateCardTransform(hoverCard, hoverCardRect);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (SelectedIcon == this) return;

            hoveredIcon = this;
            EnsureHoverCard(transform.parent);
            if (hoverCardImg != null)
            {
                hoverCardImg.color = hoverColor;
            }
            if (hoverCard != null)
            {
                hoverCard.SetActive(true);
            }
            UpdateHoverCardTransform();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (hoveredIcon == this)
            {
                hoveredIcon = null;
                if (hoverCard != null)
                {
                    hoverCard.SetActive(false);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                SelectIcon(this);
            }
            else if (SelectedIcon == this)
            {
                ClearSelection();
            }
        }

        private void CleanupOldChildHighlight()
        {
            Transform oldChild = transform.Find("SelectionHighlight");
            if (oldChild != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(oldChild.gameObject);
                else Destroy(oldChild.gameObject);
#else
                Destroy(oldChild.gameObject);
#endif
            }
        }
        #endregion
    }
}
