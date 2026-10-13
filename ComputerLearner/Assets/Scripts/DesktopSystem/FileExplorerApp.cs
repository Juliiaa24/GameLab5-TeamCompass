/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Deep interactive File Explorer system with hierarchical navigation, context menu, and folder creation.
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ComputerLearning
{
    public enum ExplorerFileType
    {
        Folder,
        TextFile,
        ImageFile,
        Minigame
    }

    public class ExplorerItemData
    {
        public string name;
        public ExplorerFileType fileType;
        public Sprite icon;
        [TextArea(2, 5)] public string textContent;
        public Sprite imageContent;
        public LevelDefinition levelToLaunch;
        public List<ExplorerItemData> children = new List<ExplorerItemData>();
    }

    public class FileExplorerApp : MonoBehaviour, IPointerClickHandler
    {
        #region Public Variables
        public ExplorerItemData CurrentFolder => currentFolder;
        #endregion

        #region Private Variables
        [Header("Top Navigation & Toolbar")]
        [SerializeField] private TextMeshProUGUI pathText;
        [SerializeField] private Button btnBack;
        [SerializeField] private Button btnNewFolderToolbar;
        [SerializeField] private TextMeshProUGUI statusFooterText;

        [Header("Items Grid & Container")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemPrefab;

        [Header("Default Assets")]
        [SerializeField] private Sprite defaultFolderIcon;
        [SerializeField] private Sprite defaultTextIcon;
        [SerializeField] private Sprite defaultImageIcon;
        [SerializeField] private Sprite defaultGameIcon;

        [Header("Folder Configuration")]
        [Tooltip("If specified, navigates directly to this folder on startup (e.g. 'Minijuegos')")]
        [SerializeField] private string initialFolderName = "";

        [Header("Minigames Configuration")]
        [SerializeField] private LevelDefinition targetsLevel;
        [SerializeField] private LevelDefinition hoverLevel;
        [SerializeField] private LevelDefinition doubleClickLevel;
        [SerializeField] private LevelDefinition adaptiveLevel;
        [SerializeField] private Sprite targetGameIcon;
        [SerializeField] private Sprite mazeGameIcon;
        [SerializeField] private GameObject baseWindowPrefab;

        [Header("Levels Configuration")]
        [SerializeField] private LevelDefinition[] mouseLevels;
        [SerializeField] private LevelDefinition level10CopyPaste;

        [System.NonSerialized] private ExplorerItemData rootFolder;
        [System.NonSerialized] private ExplorerItemData currentFolder;
        private readonly Stack<ExplorerItemData> navigationHistory = new Stack<ExplorerItemData>();
        private GameObject selectedItemVisual;
        private ExplorerItemData selectedItemData;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (btnBack != null) btnBack.onClick.AddListener(NavigateBack);
            if (btnNewFolderToolbar != null) btnNewFolderToolbar.onClick.AddListener(() => CreateNewFolder());
            BuildInitialFileSystem();
        }

        private void OnEnable()
        {
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged += RefreshItemsView;
            }
        }

        private void OnDisable()
        {
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged -= RefreshItemsView;
            }
        }

        private void Start()
        {
            SetupClickRelays();

            if (!string.IsNullOrEmpty(initialFolderName))
            {
                OpenFolderByName(initialFolderName);
            }
            else
            {
                NavigateToFolder(rootFolder, addToHistory: false);
            }
        }

        private void SetupClickRelays()
        {
            if (itemsContainer != null)
            {
                EnsureClickRelay(itemsContainer.gameObject);
                if (itemsContainer.parent != null)
                {
                    EnsureClickRelay(itemsContainer.parent.gameObject);
                }
            }
        }

        private void EnsureClickRelay(GameObject target)
        {
            if (target == null) return;
            UnityEngine.UI.Graphic g = target.GetComponent<UnityEngine.UI.Graphic>();
            if (g == null)
            {
                UnityEngine.UI.Image img = target.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.clear;
                img.raycastTarget = true;
            }
            else
            {
                g.raycastTarget = true;
            }

            ExplorerClickRelay relay = target.GetComponent<ExplorerClickRelay>();
            if (relay == null)
            {
                relay = target.AddComponent<ExplorerClickRelay>();
                relay.targetExplorer = this;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Captures right clicks on the empty area of the File Explorer window.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (ContextMenuController.Instance != null)
                {
                    ContextMenuController.Instance.Show(eventData.position, () => CreateNewFolder());
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Clicking on empty window space clears selection
                ClearSelection();
            }
        }

        /// <summary>
        /// Creates a new folder inside the current directory.
        /// </summary>
        public void CreateNewFolder()
        {
            if (currentFolder == null) return;

            string uniqueName = GetUniqueFolderName("New Folder");
            ExplorerItemData newFolder = new ExplorerItemData
            {
                name = uniqueName,
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon,
                children = new List<ExplorerItemData>()
            };

            currentFolder.children.Add(newFolder);
            RefreshItemsView();
            Debug.Log($"[FileExplorerApp] Created folder '{uniqueName}' in '{currentFolder.name}'");
        }

        public void NavigateToFolder(ExplorerItemData folder, bool addToHistory = true)
        {
            if (folder == null) return;

            if (addToHistory && currentFolder != null)
            {
                navigationHistory.Push(currentFolder);
            }

            currentFolder = folder;
            ClearSelection();
            UpdatePathDisplay();
            RefreshItemsView();
        }

        public void NavigateBack()
        {
            if (navigationHistory.Count > 0)
            {
                ExplorerItemData previousFolder = navigationHistory.Pop();
                NavigateToFolder(previousFolder, addToHistory: false);
            }
        }

        public void OnItemDoubleClicked(ExplorerItemData item)
        {
            if (item == null) return;

            switch (item.fileType)
            {
                case ExplorerFileType.Folder:
                    NavigateToFolder(item);
                    break;

                case ExplorerFileType.TextFile:
                    OpenTextFile(item);
                    break;

                case ExplorerFileType.ImageFile:
                    OpenImageFile(item);
                    break;

                case ExplorerFileType.Minigame:
                    OpenMinigame(item);
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void UpdatePathDisplay()
        {
            if (pathText != null)
            {
                pathText.text = currentFolder != null ? $"This PC > {currentFolder.name}" : "This PC";
            }

            if (btnBack != null)
            {
                btnBack.interactable = navigationHistory.Count > 0;
            }
        }

        private void RefreshItemsView()
        {
            if (itemsContainer == null) return;

            // Clear previous UI items
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            int count = currentFolder != null && currentFolder.children != null ? currentFolder.children.Count : 0;
            if (statusFooterText != null)
            {
                statusFooterText.text = $"{count} item{(count == 1 ? "" : "s")}";
            }

            if (count == 0) return;

            foreach (var item in currentFolder.children)
            {
                CreateItemUI(item);
            }
        }

        private void CreateItemUI(ExplorerItemData item)
        {
            GameObject go = null;
            Image iconImg = null;
            TextMeshProUGUI label = null;

            if (itemPrefab != null)
            {
                go = Instantiate(itemPrefab, itemsContainer);
                iconImg = go.transform.Find("Icon")?.GetComponent<Image>();
                label = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                go = new GameObject(item.name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(itemsContainer, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110, 110);

                Image cardBg = go.GetComponent<Image>();
                cardBg.color = Color.clear;

                // Dedicated child for the icon with 100% full opacity
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 1f);
                iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.anchoredPosition = new Vector2(0, -8);
                iconRt.sizeDelta = new Vector2(64, 64);

                iconImg = iconGo.GetComponent<Image>();
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Dedicated child for the label
                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                RectTransform labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0, 0);
                labelRt.anchorMax = new Vector2(1, 0);
                labelRt.pivot = new Vector2(0.5f, 0);
                labelRt.anchoredPosition = new Vector2(0, 6);
                labelRt.sizeDelta = new Vector2(-6, 32);

                label = labelGo.GetComponent<TextMeshProUGUI>();
                label.fontSize = 12;
                label.alignment = TextAlignmentOptions.Top;
                label.color = new Color(0.09f, 0.13f, 0.24f, 1f);
                label.raycastTarget = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }

            // Check if this item represents a level and verify its unlock state
            bool isLevelItem = item.fileType == ExplorerFileType.Minigame && item.levelToLaunch != null;
            bool isUnlocked = true;
            if (isLevelItem && ProgressData.Instance != null)
            {
                isUnlocked = ProgressData.Instance.IsLevelUnlocked(item.levelToLaunch.levelId);
            }

            // Assign Icon
            if (iconImg != null)
            {
                Sprite s = item.icon != null ? item.icon : GetDefaultIcon(item.fileType);
                if (s != null) iconImg.sprite = s;
                // Dim/darken icon if locked (subtle dark gray tint with lowered alpha)
                iconImg.color = isUnlocked ? Color.white : new Color(0.28f, 0.28f, 0.28f, 0.7f);
                iconImg.preserveAspect = true;
            }

            // CanvasGroup for general darkening / translucency
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = isUnlocked ? 1f : 0.6f;

            // Assign Label
            if (label != null)
            {
                label.text = isUnlocked ? item.name : $"{item.name} <color=#777777>[Locked]</color>";
                label.color = isUnlocked ? new Color(0.09f, 0.13f, 0.24f, 1f) : new Color(0.45f, 0.45f, 0.45f, 0.7f);
            }

            // Selection & Double-click interaction
            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                float lastClickTime = 0f;
                btn.onClick.AddListener(() =>
                {
                    float timeSinceLast = Time.unscaledTime - lastClickTime;
                    if (timeSinceLast < 0.45f)
                    {
                        if (!isUnlocked)
                        {
                            if (statusFooterText != null)
                            {
                                statusFooterText.text = $"<color=#D9534F>Locked: Complete previous levels to unlock {item.name}</color>";
                            }
                            return;
                        }
                        OnItemDoubleClicked(item);
                    }
                    else
                    {
                        SelectItem(go, item);
                        if (!isUnlocked && statusFooterText != null)
                        {
                            statusFooterText.text = $"{item.name} (Locked - Complete previous levels first)";
                        }
                    }
                    lastClickTime = Time.unscaledTime;
                });
            }

            // Pulsing highlight effect for the next actionable level or containing folder
            PulsingIcon pulsing = go.GetComponent<PulsingIcon>();
            if (pulsing == null) pulsing = go.AddComponent<PulsingIcon>();

            string nextLevelId = ProgressData.Instance != null ? ProgressData.Instance.GetCurrentNextLevelId() : null;
            bool shouldPulse = false;

            if (item.fileType == ExplorerFileType.Minigame)
            {
                shouldPulse = isUnlocked && item.levelToLaunch != null && item.levelToLaunch.levelId == nextLevelId;
            }
            else if (item.fileType == ExplorerFileType.Folder)
            {
                shouldPulse = FolderContainsLevel(item, nextLevelId);
            }

            pulsing.SetPulsing(shouldPulse);
        }

        private bool FolderContainsLevel(ExplorerItemData folder, string targetLevelId)
        {
            if (folder == null || folder.children == null || string.IsNullOrEmpty(targetLevelId)) return false;
            foreach (var child in folder.children)
            {
                if (child.fileType == ExplorerFileType.Minigame && child.levelToLaunch != null && child.levelToLaunch.levelId == targetLevelId)
                {
                    return true;
                }
                if (child.fileType == ExplorerFileType.Folder && FolderContainsLevel(child, targetLevelId))
                {
                    return true;
                }
            }
            return false;
        }

        private void SelectItem(GameObject itemGo, ExplorerItemData item)
        {
            ClearSelection();
            selectedItemVisual = itemGo;
            selectedItemData = item;

            Image img = itemGo.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.2f, 0.6f, 1f, 0.25f); // Subtle blue selection highlight
            }
        }

        private void ClearSelection()
        {
            if (selectedItemVisual != null)
            {
                Image img = selectedItemVisual.GetComponent<Image>();
                if (img != null) img.color = Color.clear;
            }
            selectedItemVisual = null;
            selectedItemData = null;
        }

        private string GetUniqueFolderName(string baseName)
        {
            if (currentFolder == null || currentFolder.children == null) return baseName;

            string candidate = baseName;
            int counter = 0;
            bool exists = true;

            while (exists)
            {
                exists = false;
                foreach (var child in currentFolder.children)
                {
                    if (child.name == candidate)
                    {
                        counter++;
                        candidate = $"{baseName} ({counter})";
                        exists = true;
                        break;
                    }
                }
            }

            return candidate;
        }

        private Sprite GetDefaultIcon(ExplorerFileType type)
        {
            switch (type)
            {
                case ExplorerFileType.Folder: return defaultFolderIcon;
                case ExplorerFileType.TextFile: return defaultTextIcon;
                case ExplorerFileType.ImageFile: return defaultImageIcon;
                case ExplorerFileType.Minigame: return defaultGameIcon;
                default: return null;
            }
        }

        public void OpenFolderByName(string folderName)
        {
            if (rootFolder == null) BuildInitialFileSystem();

            ExplorerItemData target = FindFolderRecursive(rootFolder, folderName);
            if (target != null)
            {
                navigationHistory.Clear();
                navigationHistory.Push(rootFolder);
                NavigateToFolder(target, addToHistory: false);
            }
            else
            {
                NavigateToFolder(rootFolder, addToHistory: false);
            }
        }

        private ExplorerItemData FindFolderRecursive(ExplorerItemData parent, string name)
        {
            if (parent == null || parent.children == null) return null;
            foreach (var child in parent.children)
            {
                if (child.fileType == ExplorerFileType.Folder && child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                ExplorerItemData found = FindFolderRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void OpenTextFile(ExplorerItemData item)
        {
            if (StartMenuController.Instance != null)
            {
                StartMenuController.Instance.OpenNotepad();
            }
        }

        private void OpenImageFile(ExplorerItemData item)
        {
            Debug.Log($"[FileExplorer] Visualizando imagen: {item.name}");
        }

        private readonly Dictionary<string, Window> openedWindows = new Dictionary<string, Window>();

        private void OpenMinigame(ExplorerItemData item)
        {
            if (item != null && item.levelToLaunch != null)
            {
                if (ProgressData.Instance != null && !ProgressData.Instance.IsLevelUnlocked(item.levelToLaunch.levelId))
                {
                    if (statusFooterText != null)
                        statusFooterText.text = $"<color=#D9534F>Locked: Complete previous levels to unlock {item.name}</color>";
                    return;
                }

                string key = item.levelToLaunch.levelId;
                if (openedWindows.TryGetValue(key, out Window existingWin) && existingWin != null && !existingWin.IsClosed)
                {
                    existingWin.Restore();
                    existingWin.BringToFront();
                    return;
                }

                if (WindowManager.Instance != null)
                {
                    GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
                    Window win = WindowManager.Instance.OpenWindow(winPrefab, null, item.name);
                    if (win != null)
                    {
                        openedWindows[key] = win;
                        LevelRunner runner = win.GetComponent<LevelRunner>();
                        if (runner == null)
                        {
                            runner = win.gameObject.AddComponent<LevelRunner>();
                            runner.taskContentArea = win.ContentArea != null ? win.ContentArea : win.GetComponent<RectTransform>();
                            runner.StartLevel(item.levelToLaunch);
                        }
                        else
                        {
                            win.Restore();
                            win.BringToFront();
                        }
                    }
                }
            }
            else if (StartMenuController.Instance != null)
            {
                StartMenuController.Instance.OpenMinigamesHub();
            }
        }

        private void BuildInitialFileSystem()
        {
            rootFolder = new ExplorerItemData
            {
                name = "Files",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };

            // 1. Folder Documents
            ExplorerItemData docsFolder = new ExplorerItemData
            {
                name = "Documents",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Welcome.txt",
                fileType = ExplorerFileType.TextFile,
                icon = defaultTextIcon,
                textContent = "Welcome to your virtual computer!\nHere you will learn how to use all the tools."
            });
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Mouse_Guide.txt",
                fileType = ExplorerFileType.TextFile,
                icon = defaultTextIcon,
                textContent = "Tip: Use left click to select and double click quickly to open programs."
            });

            // 2. Folder Pictures
            ExplorerItemData imagesFolder = new ExplorerItemData
            {
                name = "Pictures",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };
            imagesFolder.children.Add(new ExplorerItemData
            {
                name = "Mascot.png",
                fileType = ExplorerFileType.ImageFile,
                icon = defaultImageIcon
            });

            // 3. Folder Minigames with all educational minigames
            ExplorerItemData gamesFolder = new ExplorerItemData
            {
                name = "Minigames",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Targets & Clicks",
                fileType = ExplorerFileType.Minigame,
                icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                levelToLaunch = targetsLevel
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Tracking & Hover",
                fileType = ExplorerFileType.Minigame,
                icon = mazeGameIcon != null ? mazeGameIcon : defaultGameIcon,
                levelToLaunch = hoverLevel
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Fast Double Click",
                fileType = ExplorerFileType.Minigame,
                icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                levelToLaunch = doubleClickLevel
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Adaptive Practice",
                fileType = ExplorerFileType.Minigame,
                icon = mazeGameIcon != null ? mazeGameIcon : defaultGameIcon,
                levelToLaunch = adaptiveLevel
            });

            rootFolder.children.Add(docsFolder);
            rootFolder.children.Add(imagesFolder);
            rootFolder.children.Add(gamesFolder);

            // 4. Folder Levels (Contains Mouse Levels subfolder and Level 10 Copy & Paste)
            ExplorerItemData levelsFolder = new ExplorerItemData
            {
                name = "Levels",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };

            ExplorerItemData mouseFolder = new ExplorerItemData
            {
                name = "Mouse Levels",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };

            if (mouseLevels != null && mouseLevels.Length > 0)
            {
                foreach (var lvl in mouseLevels)
                {
                    if (lvl != null)
                    {
                        mouseFolder.children.Add(new ExplorerItemData
                        {
                            name = lvl.displayName,
                            fileType = ExplorerFileType.Minigame,
                            icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                            levelToLaunch = lvl
                        });
                    }
                }
            }

            levelsFolder.children.Add(mouseFolder);

            if (level10CopyPaste != null)
            {
                levelsFolder.children.Add(new ExplorerItemData
                {
                    name = level10CopyPaste.displayName,
                    fileType = ExplorerFileType.Minigame,
                    icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                    levelToLaunch = level10CopyPaste
                });
            }

            // 5. Folder Recycle Bin (Deleted files)
            ExplorerItemData recycleFolder = new ExplorerItemData
            {
                name = "Recycle Bin",
                fileType = ExplorerFileType.Folder,
                icon = defaultFolderIcon
            };
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Old_Drawing.png",
                fileType = ExplorerFileType.ImageFile,
                icon = defaultImageIcon
            });
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Draft_Notes.txt",
                fileType = ExplorerFileType.TextFile,
                icon = defaultTextIcon,
                textContent = "This is a discarded draft document inside the Recycle Bin."
            });
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Deleted_Shortcut.txt",
                fileType = ExplorerFileType.TextFile,
                icon = defaultTextIcon,
                textContent = "Deleted shortcut file."
            });

            rootFolder.children.Add(levelsFolder);
            rootFolder.children.Add(recycleFolder);
        }
        #endregion
    }

    /// <summary>
    /// Forwards pointer click events from empty explorer container/scroll areas to FileExplorerApp.
    /// </summary>
    public class ExplorerClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public FileExplorerApp targetExplorer;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetExplorer != null) targetExplorer.OnPointerClick(eventData);
        }
    }
}
