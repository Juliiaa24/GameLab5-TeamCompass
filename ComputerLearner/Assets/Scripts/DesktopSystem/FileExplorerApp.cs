/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Deep interactive File Explorer system with persistent hierarchical file system ('This PC' + 'Desktop'),
 *              Inspector-configured UI references and prefabs, bidirectional Drag & Drop, MS Paint (.png) & Notepad (.txt) integration,
 *              and 100% English localization.
 */
using System;
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

    [Serializable]
    public class ExplorerItemData
    {
        public string name;
        public ExplorerFileType fileType;
        public Sprite icon;
        [TextArea(2, 5)] public string textContent;
        public Sprite imageContent;
        public LevelDefinition levelToLaunch;
        public bool isProtected;
        public List<ExplorerItemData> children = new List<ExplorerItemData>();
    }

    public class FileExplorerApp : MonoBehaviour, IPointerClickHandler
    {
        #region Static Shared File System
        private static ExplorerItemData sharedRootFolder;
        public static event Action OnFileSystemChanged;

        public static Sprite SharedFolderIcon { get; private set; }
        public static Sprite SharedTextIcon { get; private set; }
        public static Sprite SharedImageIcon { get; private set; }
        public static Sprite SharedGameIcon { get; private set; }

        public static ExplorerItemData ActiveExplorerFolder { get; private set; }

        public static void NotifyFileSystemChanged()
        {
            OnFileSystemChanged?.Invoke();
        }

        public static Sprite GetTextFileSprite()
        {
            if (SharedTextIcon != null) return SharedTextIcon;
#if UNITY_EDITOR
            SharedTextIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Text_File_Icon_T01_Scaled.png");
            if (SharedTextIcon != null) return SharedTextIcon;
#endif
            SharedTextIcon = Resources.Load<Sprite>("Text_File_Icon_T01_Scaled");
            return SharedTextIcon;
        }

        public static Sprite GetImageFileSprite()
        {
            if (SharedImageIcon != null) return SharedImageIcon;
#if UNITY_EDITOR
            SharedImageIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Image_File_Icon_01_Scaled.png");
            if (SharedImageIcon != null) return SharedImageIcon;
#endif
            SharedImageIcon = Resources.Load<Sprite>("Image_File_Icon_01_Scaled");
            return SharedImageIcon;
        }

        public static Sprite GetFolderSprite()
        {
            if (SharedFolderIcon != null) return SharedFolderIcon;
#if UNITY_EDITOR
            SharedFolderIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01.png");
            if (SharedFolderIcon != null) return SharedFolderIcon;
#endif
            SharedFolderIcon = Resources.Load<Sprite>("Folder_Icon01");
            return SharedFolderIcon;
        }
        #endregion

        #region Public Variables
        public ExplorerItemData CurrentFolder => currentFolder;
        public Color SelectedItemColor => selectedItemColor;
        public Color HoverItemColor => hoverItemColor;
        #endregion

        #region Private Variables
        [Header("Top Navigation & Toolbar")]
        [SerializeField] private TextMeshProUGUI pathText;
        [SerializeField] private Button btnBack;
        [SerializeField] private Button btnNewFolderToolbar;
        [SerializeField] private Button btnNewImageToolbar;
        [SerializeField] private TextMeshProUGUI statusFooterText;

        [Header("Items Grid & Container")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Color selectedItemColor = new Color(0.80f, 0.90f, 1.0f, 0.85f);
        [SerializeField] private Color hoverItemColor = new Color(0.90f, 0.95f, 1.0f, 0.75f);

        [Header("Default Assets & Content Prefabs")]
        [SerializeField] private Sprite defaultFolderIcon;
        [SerializeField] private Sprite defaultTextIcon;
        [SerializeField] private Sprite defaultImageIcon;
        [SerializeField] private Sprite defaultGameIcon;
        [SerializeField] private GameObject baseWindowPrefab;
        [SerializeField] private GameObject notepadPrefab;
        [SerializeField] private GameObject paintPrefab;
        [SerializeField] private GameObject imageViewerPrefab;

        [Header("Folder Configuration")]
        [Tooltip("If specified, navigates directly to this folder on startup (e.g. 'Minigames')")]
        [SerializeField] private string initialFolderName = "";

        [Header("Minigames Configuration")]
        [SerializeField] private LevelDefinition targetsLevel;
        [SerializeField] private LevelDefinition hoverLevel;
        [SerializeField] private LevelDefinition doubleClickLevel;
        [SerializeField] private LevelDefinition adaptiveLevel;
        [SerializeField] private Sprite targetGameIcon;
        [SerializeField] private Sprite mazeGameIcon;

        [Header("Levels Configuration")]
        [SerializeField] private LevelDefinition[] mouseLevels;
        [SerializeField] private LevelDefinition level10CopyPaste;

        [NonSerialized] private ExplorerItemData rootFolder;
        [NonSerialized] private ExplorerItemData currentFolder;
        private readonly Stack<ExplorerItemData> navigationHistory = new Stack<ExplorerItemData>();
        private GameObject selectedItemVisual;
        [NonSerialized] private ExplorerItemData selectedItemData;
        private readonly Dictionary<string, Window> openedWindows = new Dictionary<string, Window>();

        private bool isSaveDialogMode = false;
        public static bool IsSaveDialogOpen { get; private set; }
        private string saveDialogExtension = "";
        private TMP_InputField saveDialogInputField;
        private Action<string, ExplorerItemData> onSaveDialogConfirm;
        private Window parentWindow;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            ResolveDefaultIcons();

            if (btnBack != null)
            {
                btnBack.onClick.RemoveListener(NavigateBack);
                btnBack.onClick.AddListener(NavigateBack);
            }
            if (btnNewFolderToolbar != null)
            {
                btnNewFolderToolbar.onClick.RemoveAllListeners();
                btnNewFolderToolbar.onClick.AddListener(() => CreateNewFolder());
            }
            if (btnNewImageToolbar != null)
            {
                btnNewImageToolbar.onClick.RemoveAllListeners();
                btnNewImageToolbar.onClick.AddListener(() => CreateNewImageDocument("New Drawing"));
            }

            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;
        }

        private void OnEnable()
        {
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged += RefreshItemsView;
            }
            OnFileSystemChanged += RefreshItemsView;
        }

        private void OnDisable()
        {
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged -= RefreshItemsView;
            }
            OnFileSystemChanged -= RefreshItemsView;
        }

        private void Start()
        {
            SetupClickRelays();

            if (isSaveDialogMode)
            {
                return;
            }

            if (currentFolder != null && currentFolder != rootFolder)
            {
                OpenFolder(currentFolder);
            }
            else if (!string.IsNullOrEmpty(initialFolderName))
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
            ExplorerClickRelay relay = target.GetComponent<ExplorerClickRelay>();
            if (relay == null)
            {
                relay = target.AddComponent<ExplorerClickRelay>();
            }
            relay.targetExplorer = this;
        }
        #endregion

        #region Public Static File System Operations
        public static ExplorerItemData GetRootFolder()
        {
            if (sharedRootFolder == null)
            {
                FileExplorerApp existing = FindAnyObjectByType<FileExplorerApp>(FindObjectsInactive.Include);
                if (existing != null)
                {
                    existing.ResolveDefaultIcons();
                    existing.EnsureSharedFileSystem();
                }
                else
                {
                    GameObject prefab = null;
                    if (StartMenuController.Instance != null && StartMenuController.Instance.FileExplorerPrefab != null)
                    {
                        prefab = StartMenuController.Instance.FileExplorerPrefab;
                    }
                    if (prefab == null)
                    {
                        prefab = Resources.Load<GameObject>("FileExplorerContent");
                    }
#if UNITY_EDITOR
                    if (prefab == null)
                    {
                        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/FileExplorerContent.prefab");
                    }
#endif
                    if (prefab != null)
                    {
                        FileExplorerApp prefabApp = prefab.GetComponent<FileExplorerApp>() ?? prefab.GetComponentInChildren<FileExplorerApp>();
                        if (prefabApp != null)
                        {
                            prefabApp.ResolveDefaultIcons();
                            prefabApp.EnsureSharedFileSystem();
                        }
                    }

                    if (sharedRootFolder == null)
                    {
                        BuildFallbackStaticFileSystem();
                    }
                }
            }
            return sharedRootFolder;
        }

        public static ExplorerItemData GetDesktopFolder()
        {
            return FindOrCreateSystemFolder("Desktop", isProtectedFolder: true);
        }

        public static ExplorerItemData FindOrCreateSystemFolder(string folderName, bool isProtectedFolder = true)
        {
            ExplorerItemData root = GetRootFolder();
            if (root == null) return null;

            if (root.name.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                return root;

            ExplorerItemData found = FindFolderRecursiveStatic(root, folderName);
            if (found != null) return found;

            ExplorerItemData newFolder = new ExplorerItemData
            {
                name = folderName,
                fileType = ExplorerFileType.Folder,
                icon = GetFolderSprite(),
                isProtected = isProtectedFolder,
                children = new List<ExplorerItemData>()
            };
            root.children.Add(newFolder);
            NotifyFileSystemChanged();
            return newFolder;
        }

        public static ExplorerItemData SaveOrCreateTextDocument(string requestedName, string content, ExplorerItemData targetFolder = null)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                requestedName = "New Document.txt";

            requestedName = requestedName.Trim();
            if (!requestedName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                requestedName += ".txt";
            }

            if (targetFolder == null)
            {
                if (ActiveExplorerFolder != null && !IsReadOnlySystemFolderStatic(ActiveExplorerFolder))
                {
                    targetFolder = ActiveExplorerFolder;
                }
                else
                {
                    targetFolder = FindOrCreateSystemFolder("Documents", true);
                }
            }

            if (targetFolder == null) return null;

            foreach (var child in targetFolder.children)
            {
                if (child.fileType == ExplorerFileType.TextFile && child.name.Equals(requestedName, StringComparison.OrdinalIgnoreCase))
                {
                    child.textContent = content;
                    child.icon = GetTextFileSprite();
                    NotifyFileSystemChanged();
                    return child;
                }
            }

            ExplorerItemData newDoc = new ExplorerItemData
            {
                name = requestedName,
                fileType = ExplorerFileType.TextFile,
                icon = GetTextFileSprite(),
                textContent = content ?? string.Empty,
                isProtected = false
            };

            targetFolder.children.Add(newDoc);
            NotifyFileSystemChanged();
            return newDoc;
        }

        public static ExplorerItemData SaveOrCreateImageDocument(string requestedName, Sprite imageSprite, ExplorerItemData targetFolder = null)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                requestedName = "New Drawing.png";

            requestedName = requestedName.Trim();
            if (!requestedName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                requestedName += ".png";
            }

            if (targetFolder == null)
            {
                if (ActiveExplorerFolder != null && !IsReadOnlySystemFolderStatic(ActiveExplorerFolder))
                {
                    targetFolder = ActiveExplorerFolder;
                }
                else
                {
                    targetFolder = FindOrCreateSystemFolder("Pictures", true);
                }
            }

            if (targetFolder == null) return null;

            Sprite defaultIcon = GetImageFileSprite();

            foreach (var child in targetFolder.children)
            {
                if (child.fileType == ExplorerFileType.ImageFile && child.name.Equals(requestedName, StringComparison.OrdinalIgnoreCase))
                {
                    child.imageContent = imageSprite;
                    child.icon = defaultIcon;
                    NotifyFileSystemChanged();
                    return child;
                }
            }

            ExplorerItemData newImg = new ExplorerItemData
            {
                name = requestedName,
                fileType = ExplorerFileType.ImageFile,
                icon = defaultIcon,
                imageContent = imageSprite,
                isProtected = false
            };

            targetFolder.children.Add(newImg);
            NotifyFileSystemChanged();
            return newImg;
        }

        /// <summary>
        /// Moves an item from its current folder in the virtual file system into destinationFolder,
        /// and syncs with the Desktop if moved to or from 'Desktop'.
        /// </summary>
        public static bool MoveItemToFolder(ExplorerItemData item, ExplorerItemData destinationFolder, Vector2? desktopDropScreenPos = null)
        {
            if (item == null || destinationFolder == null || item.isProtected) return false;
            if (item == destinationFolder) return false;
            if (IsReadOnlySystemFolderStatic(destinationFolder)) return false;

            if (item.fileType == ExplorerFileType.Folder && FindFolderRecursiveStatic(item, destinationFolder.name) != null)
            {
                return false;
            }

            if (destinationFolder.children.Contains(item)) return false;

            ExplorerItemData root = GetRootFolder();
            if (root != null)
            {
                RemoveItemRecursive(root, item);
            }

            string baseName = item.name;
            string ext = "";
            if (item.fileType == ExplorerFileType.TextFile && baseName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                ext = ".txt";
                baseName = baseName.Substring(0, baseName.Length - 4);
            }
            else if (item.fileType == ExplorerFileType.ImageFile && baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                ext = ".png";
                baseName = baseName.Substring(0, baseName.Length - 4);
            }

            string candidate = item.name;
            int counter = 0;
            bool exists = true;
            while (exists)
            {
                exists = false;
                foreach (var sibling in destinationFolder.children)
                {
                    if (sibling != item && sibling.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        counter++;
                        candidate = $"{baseName} ({counter}){ext}";
                        exists = true;
                        break;
                    }
                }
            }
            item.name = candidate;

            destinationFolder.children.Add(item);

            if (destinationFolder.name.Equals("Desktop", StringComparison.OrdinalIgnoreCase) &&
                desktopDropScreenPos.HasValue && DesktopManager.Instance != null)
            {
                DesktopManager.Instance.SpawnIconForExplorerItem(item, desktopDropScreenPos.Value);
            }

            NotifyFileSystemChanged();
            return true;
        }

        private static bool IsReadOnlySystemFolderStatic(ExplorerItemData folder)
        {
            if (folder == null) return true;
            return folder.name.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                   folder.name.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                   folder.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                   folder.name.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) ||
                   folder.name.Equals("Minigames", StringComparison.OrdinalIgnoreCase) ||
                   folder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MoveItemToRecycleBin(ExplorerItemData item)
        {
            if (item == null || item.isProtected) return false;

            ExplorerItemData root = GetRootFolder();
            if (root == null) return false;

            ExplorerItemData recycleBin = FindFolderRecursiveStatic(root, "Recycle Bin");
            if (recycleBin == null)
            {
                recycleBin = FindOrCreateSystemFolder("Recycle Bin", true);
            }

            if (recycleBin.children.Contains(item)) return false;

            RemoveItemRecursive(root, item);
            recycleBin.children.Add(item);
            NotifyFileSystemChanged();
            return true;
        }

        public static void EmptyRecycleBin()
        {
            ExplorerItemData root = GetRootFolder();
            if (root == null) return;

            ExplorerItemData recycleBin = FindFolderRecursiveStatic(root, "Recycle Bin");
            if (recycleBin != null && recycleBin.children != null)
            {
                recycleBin.children.Clear();
                NotifyFileSystemChanged();
            }
        }

        public static int GetRecycleBinCount()
        {
            ExplorerItemData root = GetRootFolder();
            if (root == null) return 0;

            ExplorerItemData recycleBin = FindFolderRecursiveStatic(root, "Recycle Bin");
            return recycleBin != null && recycleBin.children != null ? recycleBin.children.Count : 0;
        }

        private static bool RemoveItemRecursive(ExplorerItemData parent, ExplorerItemData itemToRemove)
        {
            if (parent == null || parent.children == null) return false;
            if (parent.children.Remove(itemToRemove)) return true;

            foreach (var child in parent.children)
            {
                if (child.fileType == ExplorerFileType.Folder && RemoveItemRecursive(child, itemToRemove))
                {
                    return true;
                }
            }
            return false;
        }

        private static ExplorerItemData FindFolderRecursiveStatic(ExplorerItemData parent, string name)
        {
            if (parent == null || parent.children == null) return null;
            foreach (var child in parent.children)
            {
                if (child.fileType == ExplorerFileType.Folder && child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;

                ExplorerItemData found = FindFolderRecursiveStatic(child, name);
                if (found != null) return found;
            }
            return null;
        }
        #endregion

        #region Public Instance Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ShowEmptySpaceContextMenu(eventData.position);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                ClearSelection();
            }
        }

        public ExplorerItemData CreateNewFolder(string customBaseName = "New Folder")
        {
            if (currentFolder == null) return null;

            ExplorerItemData target = currentFolder;

            string uniqueName = GetUniqueItemName(target, customBaseName, "");
            ExplorerItemData newFolder = new ExplorerItemData
            {
                name = uniqueName,
                fileType = ExplorerFileType.Folder,
                icon = GetDefaultIcon(ExplorerFileType.Folder),
                isProtected = false,
                children = new List<ExplorerItemData>()
            };

            target.children.Add(newFolder);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created folder '{uniqueName}'");
            return newFolder;
        }

        public ExplorerItemData CreateNewTextDocument(string customBaseName = "New Document")
        {
            if (currentFolder == null) return null;

            ExplorerItemData target = IsReadOnlySystemFolderStatic(currentFolder)
                ? FindOrCreateSystemFolder("Documents", true)
                : currentFolder;

            string uniqueName = GetUniqueItemName(target, customBaseName, ".txt");
            ExplorerItemData newDoc = new ExplorerItemData
            {
                name = uniqueName,
                fileType = ExplorerFileType.TextFile,
                icon = GetTextFileSprite() != null ? GetTextFileSprite() : defaultTextIcon,
                textContent = "",
                isProtected = false
            };

            target.children.Add(newDoc);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created text file '{uniqueName}'");
            return newDoc;
        }

        public ExplorerItemData CreateNewImageDocument(string customBaseName = "New Drawing")
        {
            if (currentFolder == null) return null;

            ExplorerItemData target = IsReadOnlySystemFolderStatic(currentFolder)
                ? FindOrCreateSystemFolder("Pictures", true)
                : currentFolder;

            string uniqueName = GetUniqueItemName(target, customBaseName, ".png");
            ExplorerItemData newImg = SaveOrCreateImageDocument(uniqueName, null, target);
            SetStatusMessage($"Created image file '{uniqueName}'");
            return newImg;
        }

        public void NavigateToFolder(ExplorerItemData folder, bool addToHistory = true)
        {
            if (folder == null) return;

            if (addToHistory && currentFolder != null && currentFolder != folder)
            {
                navigationHistory.Push(currentFolder);
            }

            currentFolder = folder;
            ActiveExplorerFolder = currentFolder;
            ClearSelection();
            UpdatePathDisplay();
            RefreshItemsView();
        }

        public ExplorerItemData GetParentFolder(ExplorerItemData target)
        {
            if (target == null || target == rootFolder || target.name.Equals("This PC", StringComparison.OrdinalIgnoreCase))
                return null;

            List<ExplorerItemData> chain = new List<ExplorerItemData>();
            if (FindFolderChainRecursive(rootFolder, target, chain) && chain.Count > 1)
            {
                return chain[chain.Count - 2];
            }
            return rootFolder;
        }

        public void NavigateBack()
        {
            if (navigationHistory.Count > 0)
            {
                ExplorerItemData previousFolder = navigationHistory.Pop();
                NavigateToFolder(previousFolder, addToHistory: false);
            }
            else if (currentFolder != null && currentFolder != rootFolder && !currentFolder.name.Equals("This PC", StringComparison.OrdinalIgnoreCase))
            {
                ExplorerItemData parent = GetParentFolder(currentFolder);
                if (parent != null)
                {
                    NavigateToFolder(parent, addToHistory: false);
                }
                else
                {
                    NavigateToFolder(rootFolder, addToHistory: false);
                }
            }
        }

        public void OnItemDoubleClicked(ExplorerItemData item)
        {
            if (item == null) return;

            if (isSaveDialogMode)
            {
                if (item.fileType == ExplorerFileType.Folder)
                {
                    NavigateToFolder(item);
                }
                else
                {
                    if (saveDialogInputField != null)
                    {
                        saveDialogInputField.text = item.name;
                    }
                    ConfirmSaveDialog();
                }
                return;
            }

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

        public void OnItemRightClicked(GameObject itemGo, ExplorerItemData item, Vector2 screenPos)
        {
            if (item == null) return;
            SelectItem(itemGo, item);

            if (ContextMenuController.Instance == null) return;

            var menuItems = new List<ContextMenuItem>();
            bool inRecycleBin = currentFolder != null && currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);

            if (item.fileType == ExplorerFileType.ImageFile)
            {
                menuItems.Add(new ContextMenuItem("Open", () => OpenImageFile(item), true));
                menuItems.Add(new ContextMenuItem("Open in Paint", () => OpenImageInPaint(item), true));
            }
            else
            {
                menuItems.Add(new ContextMenuItem("Open", () => OnItemDoubleClicked(item), true));
            }

            if (inRecycleBin)
            {
                menuItems.Add(new ContextMenuItem("Restore", () => RestoreItemFromRecycleBin(item), true));
                menuItems.Add(new ContextMenuItem("Delete Permanently", () => DeleteItemPermanently(item), true));
            }
            else
            {
                if (item.fileType == ExplorerFileType.Folder && !item.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                {
                    menuItems.Add(new ContextMenuItem("Change Folder Color", () =>
                    {
                        ContextMenuController.Instance.ShowFolderColorPicker(screenPos, (selectedSprite) =>
                        {
                            if (selectedSprite == null) return;
                            item.icon = selectedSprite;
                            NotifyFileSystemChanged();
                            FeedbackManager.PlaySuccess();
                        });
                    }, true));
                }

                bool canModify = !item.isProtected;
                menuItems.Add(new ContextMenuItem("Rename", () => PromptRenameItem(item), canModify));
                menuItems.Add(new ContextMenuItem("Delete", () => DeleteItemToRecycleBin(item), canModify));

                if (item.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                {
                    int binCount = item.children != null ? item.children.Count : 0;
                    menuItems.Add(new ContextMenuItem($"Empty Recycle Bin ({binCount})", () =>
                    {
                        EmptyRecycleBin();
                        SetStatusMessage("Recycle Bin emptied.");
                    }, binCount > 0));
                }
            }

            ContextMenuController.Instance.Show(screenPos, menuItems);
        }

        public void OpenFolder(ExplorerItemData target)
        {
            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;

            if (target == null || target == rootFolder || target.name.Equals("This PC", StringComparison.OrdinalIgnoreCase))
            {
                navigationHistory.Clear();
                NavigateToFolder(rootFolder, addToHistory: false);
                return;
            }

            navigationHistory.Clear();
            List<ExplorerItemData> chain = new List<ExplorerItemData>();
            if (FindFolderChainRecursive(rootFolder, target, chain))
            {
                for (int i = 0; i < chain.Count - 1; i++)
                {
                    navigationHistory.Push(chain[i]);
                }
            }
            else
            {
                navigationHistory.Push(rootFolder);
            }

            NavigateToFolder(target, addToHistory: false);
        }

        public void OpenFolderByName(string folderName)
        {
            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;

            if (string.IsNullOrEmpty(folderName) ||
                folderName.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("File Explorer", StringComparison.OrdinalIgnoreCase))
            {
                OpenFolder(rootFolder);
                return;
            }

            ExplorerItemData target = FindFolderRecursive(rootFolder, folderName);
            if (target == null)
            {
                target = new ExplorerItemData
                {
                    name = folderName,
                    fileType = ExplorerFileType.Folder,
                    icon = GetDefaultIcon(ExplorerFileType.Folder),
                    isProtected = false,
                    children = new List<ExplorerItemData>()
                };
                ExplorerItemData desktop = GetDesktopFolder();
                if (desktop != null)
                {
                    desktop.children.Add(target);
                }
                else
                {
                    rootFolder.children.Add(target);
                }
            }

            OpenFolder(target);
        }
        #endregion

        #region Context Menu & Item Manipulation
        private void ShowEmptySpaceContextMenu(Vector2 screenPosition)
        {
            if (ContextMenuController.Instance == null || currentFolder == null) return;

            var menuItems = new List<ContextMenuItem>();
            bool isRecycleBin = currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);
            bool isSystemReadOnlyFolder =
                currentFolder.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                currentFolder.name.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) ||
                currentFolder.name.Equals("Minigames", StringComparison.OrdinalIgnoreCase);

            if (isRecycleBin)
            {
                int count = currentFolder.children != null ? currentFolder.children.Count : 0;
                menuItems.Add(new ContextMenuItem($"Empty Recycle Bin ({count})", () =>
                {
                    EmptyRecycleBin();
                    SetStatusMessage("Recycle Bin emptied.");
                }, count > 0));
                menuItems.Add(new ContextMenuItem("Refresh", RefreshItemsView, true));
            }
            else if (isSystemReadOnlyFolder)
            {
                menuItems.Add(new ContextMenuItem("Refresh", RefreshItemsView, true));
            }
            else
            {
                menuItems.Add(new ContextMenuItem("+ New Folder", () => CreateNewFolder("New Folder"), true));
                menuItems.Add(new ContextMenuItem("+ New Text Document (.txt)", () => CreateNewTextDocument("New Document"), true));
                menuItems.Add(new ContextMenuItem("+ New Image (.png)", () => CreateNewImageDocument("New Drawing"), true));
                menuItems.Add(new ContextMenuItem("Refresh", RefreshItemsView, true));
            }

            ContextMenuController.Instance.Show(screenPosition, menuItems);
        }

        public void PromptRenameItem(ExplorerItemData item)
        {
            if (item == null || item.isProtected)
            {
                SetStatusMessage("<color=#D9534F>System item is protected and cannot be renamed.</color>");
                return;
            }

            if (ContextMenuController.Instance == null) return;

            string title = item.fileType == ExplorerFileType.Folder ? "Rename Folder" : "Rename File";
            ContextMenuController.Instance.ShowInputModal(title, item.name, "Save", (newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;
                newName = newName.Trim();

                if (item.fileType == ExplorerFileType.TextFile && !newName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    newName += ".txt";
                }
                else if (item.fileType == ExplorerFileType.ImageFile && !newName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !newName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    newName += ".png";
                }

                if (currentFolder != null && currentFolder.children != null)
                {
                    foreach (var sibling in currentFolder.children)
                    {
                        if (sibling != item && sibling.name.Equals(newName, StringComparison.OrdinalIgnoreCase))
                        {
                            SetStatusMessage($"<color=#D9534F>An item named '{newName}' already exists here.</color>");
                            return;
                        }
                    }
                }

                string oldName = item.name;
                item.name = newName;
                NotifyFileSystemChanged();
                SetStatusMessage($"Renamed '{oldName}' to '{newName}'");
            });
        }

        public void DeleteItemToRecycleBin(ExplorerItemData item)
        {
            if (item == null) return;
            if (item.isProtected)
            {
                SetStatusMessage($"<color=#D9534F>'{item.name}' is a system item and cannot be deleted.</color>");
                return;
            }

            if (MoveItemToRecycleBin(item))
            {
                SetStatusMessage($"Moved '{item.name}' to Recycle Bin.");
            }
        }

        private void RestoreItemFromRecycleBin(ExplorerItemData item)
        {
            if (item == null || currentFolder == null) return;
            currentFolder.children.Remove(item);

            ExplorerItemData targetDest = GetDesktopFolder() ?? rootFolder;
            if (item.fileType == ExplorerFileType.TextFile)
            {
                targetDest = FindFolderRecursive(rootFolder, "Documents") ?? targetDest;
            }
            else if (item.fileType == ExplorerFileType.ImageFile)
            {
                targetDest = FindFolderRecursive(rootFolder, "Pictures") ?? targetDest;
            }

            if (!targetDest.children.Contains(item))
            {
                targetDest.children.Add(item);
            }

            NotifyFileSystemChanged();
            SetStatusMessage($"Restored '{item.name}' to {targetDest.name}.");
        }

        private void DeleteItemPermanently(ExplorerItemData item)
        {
            if (item == null || currentFolder == null) return;
            currentFolder.children.Remove(item);
            NotifyFileSystemChanged();
            SetStatusMessage($"Permanently deleted '{item.name}'.");
        }
        #endregion

        #region Private Methods
        private void ResolveDefaultIcons()
        {
            Sprite t01 = GetTextFileSprite();
            if (t01 != null)
            {
                defaultTextIcon = t01;
                SharedTextIcon = t01;
            }
            else if (defaultTextIcon != null)
            {
                SharedTextIcon = defaultTextIcon;
            }

            if (defaultImageIcon == null)
            {
                defaultImageIcon = GetImageFileSprite();
            }
            if (defaultImageIcon != null) SharedImageIcon = defaultImageIcon;

            if (defaultFolderIcon == null)
            {
                defaultFolderIcon = GetFolderSprite();
            }
            if (defaultFolderIcon != null) SharedFolderIcon = defaultFolderIcon;

            if (defaultGameIcon != null) SharedGameIcon = defaultGameIcon;
        }

        private void EnsureSharedFileSystem()
        {
            if (sharedRootFolder == null)
            {
                BuildInitialFileSystem();
                sharedRootFolder = rootFolder;
            }
            else
            {
                ExplorerItemData desktop = FindFolderRecursiveStatic(sharedRootFolder, "Desktop");
                if (desktop == null)
                {
                    desktop = new ExplorerItemData
                    {
                        name = "Desktop",
                        fileType = ExplorerFileType.Folder,
                        icon = GetFolderSprite(),
                        isProtected = true,
                        children = new List<ExplorerItemData>()
                    };
                    sharedRootFolder.children.Insert(0, desktop);
                }

                // Ensure Levels is inside Desktop
                ExplorerItemData levels = FindFolderRecursiveStatic(sharedRootFolder, "Levels");
                if (levels == null)
                {
                    levels = new ExplorerItemData
                    {
                        name = "Levels",
                        fileType = ExplorerFileType.Folder,
                        icon = GetFolderSprite(),
                        isProtected = true,
                        children = new List<ExplorerItemData>()
                    };
                    desktop.children.Add(levels);
                }
                else if (!desktop.children.Contains(levels))
                {
                    sharedRootFolder.children.Remove(levels);
                    desktop.children.Add(levels);
                }

                // Ensure Minigames is inside Desktop
                ExplorerItemData games = FindFolderRecursiveStatic(sharedRootFolder, "Minigames");
                if (games == null)
                {
                    games = new ExplorerItemData
                    {
                        name = "Minigames",
                        fileType = ExplorerFileType.Folder,
                        icon = GetFolderSprite(),
                        isProtected = true,
                        children = new List<ExplorerItemData>()
                    };
                    desktop.children.Add(games);
                }
                else if (!desktop.children.Contains(games))
                {
                    sharedRootFolder.children.Remove(games);
                    desktop.children.Add(games);
                }

                if (levels.children == null || levels.children.Count == 0)
                {
                    PopulateLevelsFolder(levels);
                }
                if (games.children == null || games.children.Count == 0)
                {
                    PopulateGamesFolder(games);
                }

                sharedRootFolder.name = "This PC";
                RefreshIconsRecursive(sharedRootFolder);
            }
        }

        private void PopulateLevelsFolder(ExplorerItemData folder)
        {
            if (folder == null) return;
            if (folder.children == null) folder.children = new List<ExplorerItemData>();

            Sprite defGameIcon = defaultGameIcon != null ? defaultGameIcon : SharedGameIcon;
            Sprite icon = targetGameIcon != null ? targetGameIcon : defGameIcon;

            if (folder.children.Count == 0 && mouseLevels != null && mouseLevels.Length > 0)
            {
                foreach (var lvl in mouseLevels)
                {
                    if (lvl != null)
                    {
                        folder.children.Add(new ExplorerItemData
                        {
                            name = lvl.displayName,
                            fileType = ExplorerFileType.Minigame,
                            icon = icon,
                            levelToLaunch = lvl,
                            isProtected = true
                        });
                    }
                }
            }

            EnsureGuidedLevelsInFolder(folder, icon);
        }

        private static void EnsureGuidedLevelsInFolder(ExplorerItemData folder, Sprite icon)
        {
            if (folder == null || folder.children == null) return;

            string[] guidedLevelIds = { "level10", "level11", "level12", "level13", "level14", "level15", "level16" };
            foreach (string lvlId in guidedLevelIds)
            {
                bool alreadyExists = false;
                foreach (var child in folder.children)
                {
                    if (child.levelToLaunch != null && string.Equals(child.levelToLaunch.levelId, lvlId, StringComparison.OrdinalIgnoreCase))
                    {
                        child.levelToLaunch = VirtualDesktopGuidedLevels.GetOrCreateLevelDefinition(lvlId);
                        child.name = child.levelToLaunch.displayName;
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    LevelDefinition def = VirtualDesktopGuidedLevels.GetOrCreateLevelDefinition(lvlId);
                    if (def != null)
                    {
                        folder.children.Add(new ExplorerItemData
                        {
                            name = def.displayName,
                            fileType = ExplorerFileType.Minigame,
                            icon = icon,
                            levelToLaunch = def,
                            isProtected = true
                        });
                    }
                }
            }

            folder.children.Sort((a, b) =>
            {
                int idxA = GetLevelSortOrder(a?.levelToLaunch?.levelId);
                int idxB = GetLevelSortOrder(b?.levelToLaunch?.levelId);
                if (idxA != idxB) return idxA.CompareTo(idxB);
                return string.Compare(a?.name, b?.name, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static int GetLevelSortOrder(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return 999;
            for (int i = 0; i < ProgressData.LevelOrder.Length; i++)
            {
                if (string.Equals(ProgressData.LevelOrder[i], levelId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 999;
        }

        private void PopulateGamesFolder(ExplorerItemData folder)
        {
            if (folder == null) return;
            if (folder.children == null) folder.children = new List<ExplorerItemData>();
            if (folder.children.Count > 0) return;

            Sprite defGameIcon = defaultGameIcon != null ? defaultGameIcon : SharedGameIcon;

            if (targetsLevel != null)
            {
                folder.children.Add(new ExplorerItemData
                {
                    name = "Targets & Clicks",
                    fileType = ExplorerFileType.Minigame,
                    icon = targetGameIcon != null ? targetGameIcon : defGameIcon,
                    levelToLaunch = targetsLevel,
                    isProtected = true
                });
            }
            if (hoverLevel != null)
            {
                folder.children.Add(new ExplorerItemData
                {
                    name = "Tracking & Hover",
                    fileType = ExplorerFileType.Minigame,
                    icon = mazeGameIcon != null ? mazeGameIcon : defGameIcon,
                    levelToLaunch = hoverLevel,
                    isProtected = true
                });
            }
            if (doubleClickLevel != null)
            {
                folder.children.Add(new ExplorerItemData
                {
                    name = "Fast Double Click",
                    fileType = ExplorerFileType.Minigame,
                    icon = targetGameIcon != null ? targetGameIcon : defGameIcon,
                    levelToLaunch = doubleClickLevel,
                    isProtected = true
                });
            }
            if (adaptiveLevel != null)
            {
                folder.children.Add(new ExplorerItemData
                {
                    name = "Adaptive Practice",
                    fileType = ExplorerFileType.Minigame,
                    icon = mazeGameIcon != null ? mazeGameIcon : defGameIcon,
                    levelToLaunch = adaptiveLevel,
                    isProtected = true
                });
            }
        }

        private string GetBreadcrumbPath(ExplorerItemData root, ExplorerItemData target)
        {
            if (target == null || target == root || target.name.Equals("This PC", StringComparison.OrdinalIgnoreCase))
                return "This PC";

            List<string> path = new List<string>();
            if (FindPathRecursive(root, target, path))
            {
                return string.Join("  >  ", path);
            }
            return $"This PC  >  {target.name}";
        }

        private static bool FindPathRecursive(ExplorerItemData current, ExplorerItemData target, List<string> path)
        {
            if (current == null) return false;
            path.Add(current.name);
            if (current == target) return true;

            if (current.children != null)
            {
                foreach (var child in current.children)
                {
                    if (child != null && FindPathRecursive(child, target, path))
                        return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        private static bool FindFolderChainRecursive(ExplorerItemData current, ExplorerItemData target, List<ExplorerItemData> chain)
        {
            if (current == null || target == null) return false;
            chain.Add(current);
            if (current == target || (current.fileType == ExplorerFileType.Folder && current.name.Equals(target.name, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (current.children != null)
            {
                foreach (var child in current.children)
                {
                    if (child != null && child.fileType == ExplorerFileType.Folder && FindFolderChainRecursive(child, target, chain))
                        return true;
                }
            }

            chain.RemoveAt(chain.Count - 1);
            return false;
        }

        private void RefreshIconsRecursive(ExplorerItemData node)
        {
            if (node == null) return;

            if (node.fileType == ExplorerFileType.TextFile)
            {
                Sprite txtSprite = GetTextFileSprite() != null ? GetTextFileSprite() : defaultTextIcon;
                if (txtSprite != null) node.icon = txtSprite;
            }
            else if (node.fileType == ExplorerFileType.ImageFile)
            {
                node.icon = GetImageFileSprite() ?? GetDefaultIcon(ExplorerFileType.ImageFile);
                if (node.imageContent == null) node.imageContent = node.icon;
            }
            else if (node.fileType == ExplorerFileType.Folder && node.icon == null)
            {
                node.icon = GetDefaultIcon(ExplorerFileType.Folder);
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    RefreshIconsRecursive(child);
                }
            }
        }

        private void SetStatusMessage(string msg)
        {
            if (statusFooterText != null)
            {
                statusFooterText.text = msg;
            }
        }

        private void UpdatePathDisplay()
        {
            if (pathText != null)
            {
                if (currentFolder == null || currentFolder == rootFolder || currentFolder.name.Equals("This PC", StringComparison.OrdinalIgnoreCase))
                {
                    pathText.text = "This PC";
                }
                else
                {
                    pathText.text = GetBreadcrumbPath(rootFolder, currentFolder);
                }
            }

            if (btnBack != null)
            {
                bool canGoBack = navigationHistory.Count > 0 ||
                    (currentFolder != null && currentFolder != rootFolder && !currentFolder.name.Equals("This PC", StringComparison.OrdinalIgnoreCase));
                btnBack.interactable = canGoBack;
            }
        }

        public void RefreshItemsView()
        {
            if (itemsContainer == null) return;

            foreach (Transform child in itemsContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
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
            if (itemPrefab == null || itemsContainer == null) return;

            GameObject go = Instantiate(itemPrefab, itemsContainer);
            go.name = item.name;

            Image iconImg = go.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI label = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

            bool isLevelItem = item.fileType == ExplorerFileType.Minigame && item.levelToLaunch != null;
            bool isUnlocked = true;
            LevelMedal levelMedal = LevelMedal.None;
            if (isLevelItem && ProgressData.Instance != null)
            {
                isUnlocked = ProgressData.Instance.IsLevelUnlocked(item.levelToLaunch.levelId);
                levelMedal = ProgressData.Instance.GetBestMedal(item.levelToLaunch.levelId);
            }

            if (iconImg != null)
            {
                Sprite s;
                if (item.fileType == ExplorerFileType.TextFile)
                    s = GetTextFileSprite() ?? item.icon ?? GetDefaultIcon(item.fileType);
                else if (item.fileType == ExplorerFileType.ImageFile)
                    s = GetImageFileSprite() ?? GetDefaultIcon(item.fileType);
                else
                    s = item.icon != null ? item.icon : GetDefaultIcon(item.fileType);

                if (s != null) iconImg.sprite = s;
                iconImg.color = isUnlocked ? Color.white : new Color(0.28f, 0.28f, 0.28f, 0.7f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Display medal badge under/on the icon if level has an achieved medal
                if (levelMedal != LevelMedal.None)
                {
                    Transform existingBadge = go.transform.Find("MedalBadge");
                    if (existingBadge == null)
                    {
                        GameObject badgeObj = new GameObject("MedalBadge", typeof(RectTransform), typeof(Image));
                        badgeObj.transform.SetParent(go.transform, false);
                        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
                        badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
                        badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                        badgeRect.pivot = new Vector2(0.5f, 0.5f);
                        badgeRect.sizeDelta = new Vector2(22, 22);
                        badgeRect.anchoredPosition = new Vector2(16, -6);

                        Image badgeImg = badgeObj.GetComponent<Image>();
                        badgeImg.sprite = MedalVisualUtility.GetMedalSprite(levelMedal);
                        badgeImg.preserveAspect = true;
                        badgeImg.raycastTarget = false;
                    }
                }
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = isUnlocked ? 1f : 0.6f;
            }

            if (label != null)
            {
                if (!isUnlocked)
                {
                    label.text = $"{item.name} <color=#777777>[Locked]</color>";
                }
                else if (levelMedal != LevelMedal.None)
                {
                    string emoji = LevelScoreRecord.GetMedalEmoji(levelMedal);
                    string hex = LevelScoreRecord.GetMedalColorHex(levelMedal);
                    string medalName = LevelScoreRecord.GetMedalName(levelMedal);
                    label.text = $"{item.name}\n<size=11><color={hex}><b>{emoji} {medalName}</b></color></size>";
                }
                else
                {
                    label.text = item.name;
                }
                label.raycastTarget = false;
            }

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }

            ExplorerItemPointerHandler pointerHandler = go.GetComponent<ExplorerItemPointerHandler>();
            if (pointerHandler == null)
            {
                pointerHandler = go.AddComponent<ExplorerItemPointerHandler>();
            }
            pointerHandler.Initialize(this, item, isUnlocked);

            PulsingIcon pulsing = go.GetComponent<PulsingIcon>();
            if (pulsing != null)
            {
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

        public void SelectItem(GameObject itemGo, ExplorerItemData item)
        {
            ClearSelection();
            selectedItemVisual = itemGo;
            selectedItemData = item;

            if (itemGo != null)
            {
                Image img = itemGo.GetComponent<Image>();
                if (img != null)
                {
                    img.color = selectedItemColor;
                }
            }

            if (isSaveDialogMode && item != null && item.fileType != ExplorerFileType.Folder)
            {
                if (saveDialogInputField != null)
                {
                    saveDialogInputField.text = item.name;
                }
            }
        }

        public bool IsItemSelected(GameObject itemGo)
        {
            return selectedItemVisual == itemGo && itemGo != null;
        }

        public void ClearSelection()
        {
            if (selectedItemVisual != null)
            {
                Image img = selectedItemVisual.GetComponent<Image>();
                if (img != null) img.color = Color.clear;
            }
            selectedItemVisual = null;
            selectedItemData = null;
        }

        public void NotifyLockedItemClicked(ExplorerItemData item, bool doubleClick)
        {
            if (statusFooterText != null && item != null)
            {
                statusFooterText.text = doubleClick
                    ? $"<color=#D9534F>Locked: Complete previous levels to unlock {item.name}</color>"
                    : $"{item.name} (Locked - Complete previous levels first)";
            }
        }

        private string GetUniqueItemName(ExplorerItemData folder, string baseName, string extension)
        {
            if (folder == null || folder.children == null) return baseName + extension;

            string candidate = baseName + extension;
            int counter = 0;
            bool exists = true;

            while (exists)
            {
                exists = false;
                foreach (var child in folder.children)
                {
                    if (child.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        counter++;
                        candidate = $"{baseName} ({counter}){extension}";
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
                case ExplorerFileType.Folder: return defaultFolderIcon != null ? defaultFolderIcon : GetFolderSprite();
                case ExplorerFileType.TextFile: return GetTextFileSprite() != null ? GetTextFileSprite() : defaultTextIcon;
                case ExplorerFileType.ImageFile: return defaultImageIcon != null ? defaultImageIcon : GetImageFileSprite();
                case ExplorerFileType.Minigame: return defaultGameIcon;
                default: return null;
            }
        }

        private ExplorerItemData FindFolderRecursive(ExplorerItemData parent, string name)
        {
            return FindFolderRecursiveStatic(parent, name);
        }

        public void OpenTextFile(ExplorerItemData item)
        {
            if (item == null) return;

            if (StartMenuController.Instance != null)
            {
                NotepadApp app = StartMenuController.Instance.OpenNotepadWithFile(item, currentFolder);
                if (app != null) return;
            }

            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : GetComponentInParent<WindowManager>();
            if (winManager != null)
            {
                GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
                string winTitle = $"Notepad - {item.name}";
                Window win = winManager.OpenWindow(winPrefab, null, winTitle);
                if (win != null)
                {
                    win.setTitle(winTitle);
                    GameObject notepadPrefabToUse = notepadPrefab != null
                        ? notepadPrefab
                        : (StartMenuController.Instance != null ? StartMenuController.Instance.NotepadPrefab : null);

                    if (notepadPrefabToUse != null)
                    {
                        GameObject contentGo = Instantiate(notepadPrefabToUse);
                        win.SetContent(contentGo);
                        NotepadApp notepad = contentGo.GetComponentInChildren<NotepadApp>();
                        if (notepad != null)
                        {
                            notepad.LoadFile(item, win, currentFolder);
                        }
                    }
                    win.BringToFront();
                }
            }
        }

        public void OpenImageInPaint(ExplorerItemData item)
        {
            if (item == null) return;

            if (StartMenuController.Instance != null)
            {
                PaintApp paint = StartMenuController.Instance.OpenPaintWithFile(item, currentFolder);
                if (paint != null) return;
            }

            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : GetComponentInParent<WindowManager>();
            if (winManager == null) winManager = FindAnyObjectByType<WindowManager>();
            if (winManager == null) return;

            GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
            if (winPrefab == null) return;

            GameObject paintPrefabToUse = paintPrefab != null
                ? paintPrefab
                : (StartMenuController.Instance != null ? StartMenuController.Instance.PaintPrefab : null);
            if (paintPrefabToUse == null) return;

            string winTitle = $"Paint - {item.name}";
            Window win = winManager.OpenWindow(winPrefab, null, winTitle);
            if (win != null)
            {
                win.setTitle(winTitle);
                GameObject paintContent = Instantiate(paintPrefabToUse);
                win.SetContent(paintContent);
                PaintApp app = paintContent.GetComponentInChildren<PaintApp>();
                if (app != null)
                {
                    app.LoadImageFile(item, win, currentFolder);
                }
                win.BringToFront();
            }
        }

        public void OpenImageFile(ExplorerItemData item)
        {
            if (item == null) return;

            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : GetComponentInParent<WindowManager>();
            if (winManager == null)
            {
                winManager = FindAnyObjectByType<WindowManager>();
            }
            if (winManager == null) return;

            string key = $"img_{item.name}";
            if (openedWindows.TryGetValue(key, out Window existingWin) && existingWin != null && !existingWin.IsClosed)
            {
                existingWin.Restore();
                existingWin.BringToFront();
                return;
            }

            GameObject winPrefab = baseWindowPrefab != null
                ? baseWindowPrefab
                : (StartMenuController.Instance != null && StartMenuController.Instance.BaseWindowPrefab != null
                    ? StartMenuController.Instance.BaseWindowPrefab
                    : Resources.Load<GameObject>("BaseWindow"));

            if (winPrefab == null || imageViewerPrefab == null) return;

            string winTitle = $"Image Viewer - {item.name}";
            Window win = winManager.OpenWindow(winPrefab, null, winTitle);
            if (win != null)
            {
                openedWindows[key] = win;
                win.setTitle(winTitle);

                Sprite spriteToDisplay = item.imageContent != null
                    ? item.imageContent
                    : (item.icon != null ? item.icon : GetDefaultIcon(ExplorerFileType.ImageFile));

                GameObject viewerContent = Instantiate(imageViewerPrefab);
                win.SetContent(viewerContent);

                ImageViewerApp viewer = viewerContent.GetComponentInChildren<ImageViewerApp>();
                if (viewer != null)
                {
                    viewer.Initialize(item, spriteToDisplay, () => OpenImageInPaint(item));
                }
                win.BringToFront();
            }
        }

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

                if (VirtualDesktopGuidedLevels.IsGuidedDesktopLevel(item.levelToLaunch.levelId))
                {
                    VirtualDesktopGuidedLevels.StartGuidedLevel(item.levelToLaunch);
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
            Sprite txtSprite = GetTextFileSprite() != null ? GetTextFileSprite() : defaultTextIcon;
            Sprite imgSprite = defaultImageIcon != null ? defaultImageIcon : GetImageFileSprite();
            Sprite folderSprite = defaultFolderIcon != null ? defaultFolderIcon : GetFolderSprite();

            Sprite mascotImgContent = imgSprite;
#if UNITY_EDITOR
            Sprite presentationSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Computer Presentation.png");
            if (presentationSprite != null) mascotImgContent = presentationSprite;
#endif

            rootFolder = new ExplorerItemData
            {
                name = "This PC",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            // 0. Folder Desktop (Synced with Desktop surface icons!)
            ExplorerItemData desktopFolder = new ExplorerItemData
            {
                name = "Desktop",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            // 1. Folder Documents
            ExplorerItemData docsFolder = new ExplorerItemData
            {
                name = "Documents",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Welcome.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "Welcome to your virtual computer!\nHere you will learn how to use all the tools.\n\nYou can edit this text and click Save in Notepad!",
                isProtected = false
            });
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Mouse_Guide.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "Tip: Use left click to select, double click quickly to open programs, and right click to open the Context Menu!",
                isProtected = false
            });

            // 2. Folder Pictures
            ExplorerItemData imagesFolder = new ExplorerItemData
            {
                name = "Pictures",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            imagesFolder.children.Add(new ExplorerItemData
            {
                name = "Mascot.png",
                fileType = ExplorerFileType.ImageFile,
                icon = imgSprite,
                imageContent = mascotImgContent,
                isProtected = false
            });
            imagesFolder.children.Add(new ExplorerItemData
            {
                name = "Target_Badge.png",
                fileType = ExplorerFileType.ImageFile,
                icon = imgSprite,
                imageContent = targetGameIcon != null ? targetGameIcon : imgSprite,
                isProtected = false
            });

            // 3. Folder Minigames inside Desktop!
            ExplorerItemData gamesFolder = new ExplorerItemData
            {
                name = "Minigames",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            PopulateGamesFolder(gamesFolder);

            // 4. Folder Levels inside Desktop!
            ExplorerItemData levelsFolder = new ExplorerItemData
            {
                name = "Levels",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            PopulateLevelsFolder(levelsFolder);

            EnsureGuidedLevelsInFolder(levelsFolder, SharedGameIcon);

            desktopFolder.children.Add(levelsFolder);
            desktopFolder.children.Add(gamesFolder);

            // 5. Folder Recycle Bin
            ExplorerItemData recycleFolder = new ExplorerItemData
            {
                name = "Recycle Bin",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Old_Drawing.png",
                fileType = ExplorerFileType.ImageFile,
                icon = imgSprite,
                imageContent = mazeGameIcon != null ? mazeGameIcon : imgSprite,
                isProtected = false
            });
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Draft_Notes.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "This is a discarded draft document inside the Recycle Bin.",
                isProtected = false
            });
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Deleted_Shortcut.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "Deleted shortcut file.",
                isProtected = false
            });

            rootFolder.children.Add(desktopFolder);
            rootFolder.children.Add(docsFolder);
            rootFolder.children.Add(imagesFolder);
            rootFolder.children.Add(recycleFolder);
        }

        private static void BuildFallbackStaticFileSystem()
        {
            Sprite folderSprite = GetFolderSprite();
            Sprite txtSprite = GetTextFileSprite();
            Sprite imgSprite = GetImageFileSprite();

            sharedRootFolder = new ExplorerItemData
            {
                name = "This PC",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            ExplorerItemData desktopFolder = new ExplorerItemData
            {
                name = "Desktop",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            ExplorerItemData docsFolder = new ExplorerItemData
            {
                name = "Documents",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Welcome.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "Welcome to your virtual computer!\nHere you will learn how to use all the tools.\n\nYou can edit this text and click Save in Notepad!",
                isProtected = false
            });
            docsFolder.children.Add(new ExplorerItemData
            {
                name = "Mouse_Guide.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "Tip: Use left click to select, double click quickly to open programs, and right click to open the Context Menu!",
                isProtected = false
            });

            ExplorerItemData imagesFolder = new ExplorerItemData
            {
                name = "Pictures",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            imagesFolder.children.Add(new ExplorerItemData
            {
                name = "Mascot.png",
                fileType = ExplorerFileType.ImageFile,
                icon = imgSprite,
                imageContent = imgSprite,
                isProtected = false
            });

            ExplorerItemData recycleFolder = new ExplorerItemData
            {
                name = "Recycle Bin",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Old_Drawing.png",
                fileType = ExplorerFileType.ImageFile,
                icon = imgSprite,
                imageContent = imgSprite,
                isProtected = false
            });
            recycleFolder.children.Add(new ExplorerItemData
            {
                name = "Draft_Notes.txt",
                fileType = ExplorerFileType.TextFile,
                icon = txtSprite,
                textContent = "This is a discarded draft document inside the Recycle Bin.",
                isProtected = false
            });

            ExplorerItemData levelsFolder = new ExplorerItemData
            {
                name = "Levels",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            ExplorerItemData gamesFolder = new ExplorerItemData
            {
                name = "Minigames",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            LevelDefinition[] levels = Resources.FindObjectsOfTypeAll<LevelDefinition>();
            if (levels != null && levels.Length > 0)
            {
                Array.Sort(levels, (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                foreach (var lvl in levels)
                {
                    if (lvl == null) continue;
                    if (lvl.name.StartsWith("L_Level", StringComparison.OrdinalIgnoreCase))
                    {
                        levelsFolder.children.Add(new ExplorerItemData
                        {
                            name = lvl.displayName,
                            fileType = ExplorerFileType.Minigame,
                            icon = SharedGameIcon,
                            levelToLaunch = lvl,
                            isProtected = true
                        });
                    }
                    else if (lvl.name.IndexOf("Assessment", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        gamesFolder.children.Add(new ExplorerItemData
                        {
                            name = lvl.displayName,
                            fileType = ExplorerFileType.Minigame,
                            icon = SharedGameIcon,
                            levelToLaunch = lvl,
                            isProtected = true
                        });
                    }
                }
            }

            EnsureGuidedLevelsInFolder(levelsFolder, SharedGameIcon);

            desktopFolder.children.Add(levelsFolder);
            desktopFolder.children.Add(gamesFolder);

            sharedRootFolder.children.Add(desktopFolder);
            sharedRootFolder.children.Add(docsFolder);
            sharedRootFolder.children.Add(imagesFolder);
            sharedRootFolder.children.Add(recycleFolder);
        }
        #endregion

        #region Save As Dialog Mode
        public static void OpenSaveDialog(string dialogTitle, string defaultFileName, string extension, ExplorerItemData initialFolder, Action<string, ExplorerItemData> onSaveCallback)
        {
            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : UnityEngine.Object.FindAnyObjectByType<WindowManager>();
            if (winManager == null)
            {
                Debug.LogWarning("[FileExplorerApp] WindowManager not found for OpenSaveDialog.");
                onSaveCallback?.Invoke(defaultFileName, initialFolder);
                return;
            }

            GameObject winPrefab = null;
            if (StartMenuController.Instance != null && StartMenuController.Instance.BaseWindowPrefab != null)
            {
                winPrefab = StartMenuController.Instance.BaseWindowPrefab;
            }
            if (winPrefab == null)
            {
                winPrefab = Resources.Load<GameObject>("BaseWindow");
#if UNITY_EDITOR
                if (winPrefab == null) winPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/BaseWindow.prefab");
#endif
            }

            GameObject explorerPrefab = null;
            if (StartMenuController.Instance != null && StartMenuController.Instance.FileExplorerPrefab != null)
            {
                explorerPrefab = StartMenuController.Instance.FileExplorerPrefab;
            }
            if (explorerPrefab == null)
            {
                explorerPrefab = Resources.Load<GameObject>("FileExplorerContent");
#if UNITY_EDITOR
                if (explorerPrefab == null) explorerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/FileExplorerContent.prefab");
#endif
            }

            if (winPrefab == null || explorerPrefab == null)
            {
                Debug.LogError("[FileExplorerApp] BaseWindow or FileExplorerContent prefab not found.");
                onSaveCallback?.Invoke(defaultFileName, initialFolder);
                return;
            }

            Window win = winManager.OpenWindow(winPrefab, null, dialogTitle);
            if (win == null) return;

            win.setTitle(dialogTitle);
            RectTransform winRt = win.GetComponent<RectTransform>();
            if (winRt != null)
            {
                winRt.sizeDelta = new Vector2(620, 480);
            }

            GameObject contentInstance = UnityEngine.Object.Instantiate(explorerPrefab);
            win.SetContent(contentInstance);

            FileExplorerApp explorer = contentInstance.GetComponentInChildren<FileExplorerApp>();
            if (explorer != null)
            {
                explorer.SetupSaveDialogMode(defaultFileName, extension, onSaveCallback, initialFolder, win);
            }
        }

        public void SetupSaveDialogMode(string defaultFileName, string extension, Action<string, ExplorerItemData> onConfirm, ExplorerItemData initialFolder, Window window)
        {
            isSaveDialogMode = true;
            IsSaveDialogOpen = true;
            saveDialogExtension = extension;
            onSaveDialogConfirm = onConfirm;
            parentWindow = window;

            if (btnNewImageToolbar != null)
            {
                btnNewImageToolbar.gameObject.SetActive(false);
            }

            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;

            ExplorerItemData folderToOpen = initialFolder ?? GetDesktopFolder() ?? rootFolder;
            OpenFolder(folderToOpen);

            CreateSaveDialogBottomBar(defaultFileName);
        }

        private void CreateSaveDialogBottomBar(string initialFileName)
        {
            Transform footerT = transform.Find("Footer");
            if (footerT != null)
            {
                footerT.gameObject.SetActive(false);
            }

            Transform scrollT = transform.Find("ScrollView");
            if (scrollT is RectTransform scrollRt)
            {
                scrollRt.offsetMin = new Vector2(scrollRt.offsetMin.x, 56);
            }

            GameObject modalPrefab = null;
            if (ContextMenuController.Instance != null && ContextMenuController.Instance.InputModalPrefab != null)
            {
                modalPrefab = ContextMenuController.Instance.InputModalPrefab;
            }
            if (modalPrefab == null)
            {
                modalPrefab = Resources.Load<GameObject>("InputModalDialog");
#if UNITY_EDITOR
                if (modalPrefab == null) modalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/InputModalDialog.prefab");
#endif
            }

            GameObject saveBar = new GameObject("SaveDialogBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            saveBar.transform.SetParent(transform, false);
            RectTransform rt = saveBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 50);

            UnityEngine.UI.Image bg = saveBar.GetComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.14f, 0.14f, 0.17f, 0.98f);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(saveBar.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0, 1);
            labelRt.offsetMin = new Vector2(12, 0);
            labelRt.offsetMax = new Vector2(90, 0);
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "File name:";
            label.fontSize = 13;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;

            if (modalPrefab != null)
            {
                TMP_InputField modalInput = modalPrefab.GetComponentInChildren<TMP_InputField>(true);
                if (modalInput != null)
                {
                    saveDialogInputField = UnityEngine.Object.Instantiate(modalInput, saveBar.transform);
                    saveDialogInputField.name = "SaveFileNameInput";
                    RectTransform inputRt = saveDialogInputField.GetComponent<RectTransform>();
                    inputRt.anchorMin = new Vector2(0, 0.15f);
                    inputRt.anchorMax = new Vector2(1, 0.85f);
                    inputRt.offsetMin = new Vector2(95, 0);
                    inputRt.offsetMax = new Vector2(-155, 0);
                    saveDialogInputField.text = initialFileName ?? string.Empty;
                    saveDialogInputField.onSubmit.RemoveAllListeners();
                    saveDialogInputField.onSubmit.AddListener((_) => ConfirmSaveDialog());
                }

                UnityEngine.UI.Button modalConfirm = modalPrefab.transform.Find("DialogPanel/Btn_Confirm")?.GetComponent<UnityEngine.UI.Button>();
                if (modalConfirm != null)
                {
                    UnityEngine.UI.Button btnSave = UnityEngine.Object.Instantiate(modalConfirm, saveBar.transform);
                    btnSave.name = "Btn_Save";
                    RectTransform saveRt = btnSave.GetComponent<RectTransform>();
                    saveRt.anchorMin = new Vector2(1, 0.15f);
                    saveRt.anchorMax = new Vector2(1, 0.85f);
                    saveRt.offsetMin = new Vector2(-148, 0);
                    saveRt.offsetMax = new Vector2(-80, 0);
                    TextMeshProUGUI saveText = btnSave.GetComponentInChildren<TextMeshProUGUI>();
                    if (saveText != null) saveText.text = "Save";
                    btnSave.onClick.RemoveAllListeners();
                    btnSave.onClick.AddListener(ConfirmSaveDialog);
                }

                UnityEngine.UI.Button modalCancel = modalPrefab.transform.Find("DialogPanel/Btn_Cancel")?.GetComponent<UnityEngine.UI.Button>();
                if (modalCancel != null)
                {
                    UnityEngine.UI.Button btnCancel = UnityEngine.Object.Instantiate(modalCancel, saveBar.transform);
                    btnCancel.name = "Btn_Cancel";
                    RectTransform cancelRt = btnCancel.GetComponent<RectTransform>();
                    cancelRt.anchorMin = new Vector2(1, 0.15f);
                    cancelRt.anchorMax = new Vector2(1, 0.85f);
                    cancelRt.offsetMin = new Vector2(-74, 0);
                    cancelRt.offsetMax = new Vector2(-8, 0);
                    TextMeshProUGUI cancelText = btnCancel.GetComponentInChildren<TextMeshProUGUI>();
                    if (cancelText != null) cancelText.text = "Cancel";
                    btnCancel.onClick.RemoveAllListeners();
                    btnCancel.onClick.AddListener(CancelSaveDialog);
                }
            }
        }

        public void ConfirmSaveDialog()
        {
            if (!isSaveDialogMode) return;

            string enteredName = saveDialogInputField != null ? saveDialogInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(enteredName))
            {
                SetStatusMessage("<color=#D9534F>Please enter a file name.</color>");
                return;
            }

            if (!string.IsNullOrEmpty(saveDialogExtension) && !enteredName.EndsWith(saveDialogExtension, StringComparison.OrdinalIgnoreCase))
            {
                enteredName += saveDialogExtension;
            }

            if (currentFolder == null)
            {
                SetStatusMessage("<color=#D9534F>No folder selected.</color>");
                return;
            }

            if (IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage($"<color=#D9534F>Cannot save inside '{currentFolder.name}'. Choose Desktop, Documents, Pictures or a user folder.</color>");
                return;
            }

            IsSaveDialogOpen = false;
            Action<string, ExplorerItemData> callback = onSaveDialogConfirm;
            Window winToClose = parentWindow;

            if (winToClose != null)
            {
                winToClose.CloseWindow();
            }

            callback?.Invoke(enteredName, currentFolder);
        }

        private void OnDestroy()
        {
            if (isSaveDialogMode)
            {
                IsSaveDialogOpen = false;
            }
        }

        public void CancelSaveDialog()
        {
            IsSaveDialogOpen = false;
            if (parentWindow != null)
            {
                parentWindow.CloseWindow();
            }
        }
        #endregion
    }
}
