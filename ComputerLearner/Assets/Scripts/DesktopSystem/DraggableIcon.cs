/**
 * Author: Julia Vera
 * Date: 14/09/2026
 * Description: Draggable desktop icon with one window instance per icon, context menu, and Recycle Bin protection.
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        #region Public Variables
        public Window AppWindow => appWindow;
        public LevelDefinition LevelDef => levelDefinition;
        public bool AllowDoubleClick { get; set; } = true;
        public DesktopItemType ItemType { get; private set; }
        public bool IsProtected { get; private set; } = true;
        public ExplorerItemData LinkedExplorerItem { get; private set; }
        #endregion

        #region Private Variables
        private IconGrid grid;
        [SerializeField] private GameObject windowPrefab;
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private LevelDefinition levelDefinition; // The level to launch (optional)
        [SerializeField] private bool isLocked = false;
        private Window appWindow;
        private WindowManager manager;
        private Vector3 initialPosition;
        private Vector3 originalScale;
        private Color originalColor = Color.white;
        private bool dragging;
        private CanvasGroup canvasGroup;
        #endregion

        #region Unity Methods
        private void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            grid = GetComponentInParent<IconGrid>();
            if (grid != null) grid.Register(this);
            manager = GetComponentInParent<WindowManager>();
            initialPosition = transform.position;
            originalScale = transform.localScale;
            
            UnityEngine.UI.Image iconImg = GetComponent<UnityEngine.UI.Image>();
            if (iconImg != null) originalColor = iconImg.color;
            
            // Check ProgressData to see if this level is unlocked
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
            {
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

            // Update color dynamically (darkened/dimmed when locked)
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
                // Only pulse if this icon corresponds to the active NEXT level to complete
                shouldPulse = !actuallyLocked && levelDefinition.levelId == currentNextLevel && (appWindow == null || appWindow.IsClosed);
            }
            else if (gameObject.name.Equals("Levels", System.StringComparison.OrdinalIgnoreCase) || (contentPrefab != null && contentPrefab.name.Contains("Levels")))
            {
                // The Levels folder on the desktop pulses whenever there is a pending next level to complete!
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
        }

        private void OnDestroy()
        {
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
            this.IsProtected = isProtected;
            this.LinkedExplorerItem = linkedItem;
        }

        public void ReturnToInitialPosition()
        {
            transform.position = initialPosition;
            if (grid != null) grid.Register(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            if (actuallyLocked) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;
            initialPosition = transform.position;
            dragging = false;
            if (grid != null) grid.Unregister(this);
            transform.SetAsLastSibling();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (grid != null)
                grid.TryPlaceIcon(this, eventData.position, initialPosition, eventData.pressEventCamera);
        }

        public void OnBeginDrag(PointerEventData eventData) 
        { 
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            dragging = true;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 point))
                transform.position = point;
        }

        public void OnEndDrag(PointerEventData eventData) 
        { 
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ShowIconContextMenu(eventData.position);
                return;
            }

            if (!AllowDoubleClick) return;
            if (!dragging && eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
                OpenApplication();
        }

        private void ShowIconContextMenu(Vector2 screenPosition)
        {
            if (ContextMenuController.Instance == null) return;

            var items = new List<ContextMenuItem>();
            RecycleBinIcon bin = GetComponent<RecycleBinIcon>();

            if (bin != null || gameObject.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new ContextMenuItem("Abrir Papelera de Reciclaje", () =>
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
                items.Add(new ContextMenuItem($"Vaciar Papelera de Reciclaje ({binCount})", () =>
                {
                    FileExplorerApp.EmptyRecycleBin();
                }, binCount > 0));
            }
            else
            {
                items.Add(new ContextMenuItem("Abrir", OpenApplication, true));
                bool canModify = !IsProtected;
                items.Add(new ContextMenuItem("Cambiar nombre", PromptRenameDesktopIcon, canModify));
                items.Add(new ContextMenuItem("Eliminar", DeleteDesktopIconToRecycleBin, canModify));
            }

            ContextMenuController.Instance.Show(screenPosition, items);
        }

        public void PromptRenameDesktopIcon()
        {
            if (IsProtected || ContextMenuController.Instance == null) return;

            TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            string currentName = label != null ? label.text : gameObject.name;

            ContextMenuController.Instance.ShowInputModal("Cambiar nombre", currentName, "Guardar", (newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;
                newName = newName.Trim();

                if (ItemType == DesktopItemType.TextDocument && !newName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    newName += ".txt";
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

            if (LinkedExplorerItem != null)
            {
                FileExplorerApp.MoveItemToRecycleBin(LinkedExplorerItem);
            }
            else
            {
                TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                string itemName = label != null ? label.text : gameObject.name;
                ExplorerFileType fType = ItemType == DesktopItemType.TextDocument ? ExplorerFileType.TextFile : ExplorerFileType.Folder;
                UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
                ExplorerItemData tempItem = new ExplorerItemData
                {
                    name = itemName,
                    fileType = fType,
                    icon = img != null ? img.sprite : null,
                    isProtected = false
                };
                FileExplorerApp.MoveItemToRecycleBin(tempItem);
            }

            if (appWindow != null && !appWindow.IsClosed)
            {
                Destroy(appWindow.gameObject);
            }

            Destroy(gameObject);
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

            // If this is a user-created text document with a linked ExplorerItemData, open it directly in Notepad
            if (LinkedExplorerItem != null && LinkedExplorerItem.fileType == ExplorerFileType.TextFile)
            {
                if (StartMenuController.Instance != null)
                {
                    StartMenuController.Instance.OpenNotepadWithFile(LinkedExplorerItem);
                    return;
                }
            }
            
            if (manager == null) manager = GetComponentInParent<WindowManager>();
            
            string resolvedTitle = null;
            if (levelDefinition != null)
            {
                resolvedTitle = levelDefinition.displayName;
            }
            else
            {
                TMPro.TextMeshProUGUI label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null && !string.IsNullOrWhiteSpace(label.text))
                {
                    resolvedTitle = label.text;
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
                // Compatibility with older scenes
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

                // Inject the dynamic content if this icon was configured with one
                if (contentPrefab != null)
                {
                    GameObject contentInstance = Instantiate(contentPrefab);
                    appWindow.SetContent(contentInstance);

                    if (LinkedExplorerItem != null)
                    {
                        if (LinkedExplorerItem.fileType == ExplorerFileType.TextFile)
                        {
                            NotepadApp notepad = contentInstance.GetComponentInChildren<NotepadApp>();
                            if (notepad != null) notepad.LoadFile(LinkedExplorerItem, appWindow);
                        }
                        else if (LinkedExplorerItem.fileType == ExplorerFileType.Folder)
                        {
                            FileExplorerApp explorer = contentInstance.GetComponentInChildren<FileExplorerApp>();
                            if (explorer != null) explorer.NavigateToFolder(LinkedExplorerItem, addToHistory: false);
                        }
                    }
                    else if (ItemType == DesktopItemType.Folder && !IsProtected)
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
            }

            // If a new window was just created and we have a level definition, start it!
            if (isNewWindow && appWindow != null && levelDefinition != null)
            {
                appWindow.setTitle(levelDefinition.displayName);
                
                // Maximize the window automatically
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
    }
}
