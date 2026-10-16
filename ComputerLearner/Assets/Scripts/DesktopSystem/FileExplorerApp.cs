/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Deep interactive File Explorer system with persistent hierarchical file system,
 *              multi-context right-click menus, text & image file viewers, and Recycle Bin integration.
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

        [NonSerialized] private ExplorerItemData rootFolder;
        [NonSerialized] private ExplorerItemData currentFolder;
        private readonly Stack<ExplorerItemData> navigationHistory = new Stack<ExplorerItemData>();
        private GameObject selectedItemVisual;
        private ExplorerItemData selectedItemData;
        private readonly Dictionary<string, Window> openedWindows = new Dictionary<string, Window>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            ResolveDefaultIcons();

            if (btnBack != null) btnBack.onClick.AddListener(NavigateBack);
            if (btnNewFolderToolbar != null) btnNewFolderToolbar.onClick.AddListener(() => CreateNewFolder());

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

            if (currentFolder != null)
            {
                NavigateToFolder(currentFolder, addToHistory: false);
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
            Graphic g = target.GetComponent<Graphic>();
            if (g == null)
            {
                Image img = target.AddComponent<Image>();
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
                    BuildFallbackStaticFileSystem();
                }
            }
            return sharedRootFolder;
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
                requestedName = "Nuevo Documento.txt";

            requestedName = requestedName.Trim();
            if (!requestedName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                requestedName += ".txt";
            }

            if (targetFolder == null)
            {
                // Prefer current active non-protected/Documents folder, otherwise Documents
                if (ActiveExplorerFolder != null &&
                    !ActiveExplorerFolder.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) &&
                    !ActiveExplorerFolder.name.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) &&
                    !ActiveExplorerFolder.name.Equals("Minigames", StringComparison.OrdinalIgnoreCase) &&
                    !ActiveExplorerFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                {
                    targetFolder = ActiveExplorerFolder;
                }
                else
                {
                    targetFolder = FindOrCreateSystemFolder("Documents", true);
                }
            }

            if (targetFolder == null) return null;

            // Check if a text file with that exact name already exists in targetFolder
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

            // If already inside Recycle Bin, don't re-add
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
        /// <summary>
        /// Captures right clicks on the empty area of the File Explorer window.
        /// </summary>
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

        /// <summary>
        /// Creates a new user-modifiable folder inside the current directory.
        /// </summary>
        public ExplorerItemData CreateNewFolder(string customBaseName = "New Folder")
        {
            if (currentFolder == null) return null;

            string uniqueName = GetUniqueItemName(currentFolder, customBaseName, "");
            ExplorerItemData newFolder = new ExplorerItemData
            {
                name = uniqueName,
                fileType = ExplorerFileType.Folder,
                icon = GetDefaultIcon(ExplorerFileType.Folder),
                isProtected = false,
                children = new List<ExplorerItemData>()
            };

            currentFolder.children.Add(newFolder);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created folder '{uniqueName}'");
            return newFolder;
        }

        /// <summary>
        /// Creates a new user-modifiable text document inside the current directory.
        /// </summary>
        public ExplorerItemData CreateNewTextDocument(string customBaseName = "New Document")
        {
            if (currentFolder == null) return null;

            string uniqueName = GetUniqueItemName(currentFolder, customBaseName, ".txt");
            ExplorerItemData newDoc = new ExplorerItemData
            {
                name = uniqueName,
                fileType = ExplorerFileType.TextFile,
                icon = GetTextFileSprite() != null ? GetTextFileSprite() : defaultTextIcon,
                textContent = "",
                isProtected = false
            };

            currentFolder.children.Add(newDoc);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created text file '{uniqueName}'");
            return newDoc;
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

        public void OnItemRightClicked(GameObject itemGo, ExplorerItemData item, Vector2 screenPos)
        {
            if (item == null) return;
            SelectItem(itemGo, item);

            if (ContextMenuController.Instance == null) return;

            var menuItems = new List<ContextMenuItem>();
            bool inRecycleBin = currentFolder != null && currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);

            menuItems.Add(new ContextMenuItem("Abrir", () => OnItemDoubleClicked(item), true));

            if (inRecycleBin)
            {
                menuItems.Add(new ContextMenuItem("Restaurar", () => RestoreItemFromRecycleBin(item), true));
                menuItems.Add(new ContextMenuItem("Eliminar permanentemente", () => DeleteItemPermanently(item), true));
            }
            else
            {
                bool canModify = !item.isProtected;
                menuItems.Add(new ContextMenuItem("Cambiar nombre", () => PromptRenameItem(item), canModify));
                menuItems.Add(new ContextMenuItem("Eliminar", () => DeleteItemToRecycleBin(item), canModify));

                if (item.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
                {
                    int binCount = item.children != null ? item.children.Count : 0;
                    menuItems.Add(new ContextMenuItem($"Vaciar Papelera ({binCount})", () =>
                    {
                        EmptyRecycleBin();
                        SetStatusMessage("Recycle Bin emptied.");
                    }, binCount > 0));
                }
            }

            ContextMenuController.Instance.Show(screenPos, menuItems);
        }

        public void OpenFolderByName(string folderName)
        {
            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;

            if (string.IsNullOrEmpty(folderName) || folderName.Equals("Files", StringComparison.OrdinalIgnoreCase) || folderName.Equals("File Explorer", StringComparison.OrdinalIgnoreCase))
            {
                navigationHistory.Clear();
                NavigateToFolder(rootFolder, addToHistory: false);
                return;
            }

            ExplorerItemData target = FindFolderRecursive(rootFolder, folderName);
            if (target == null)
            {
                // If this was a user-created desktop folder, create it in root so it persists!
                target = new ExplorerItemData
                {
                    name = folderName,
                    fileType = ExplorerFileType.Folder,
                    icon = GetDefaultIcon(ExplorerFileType.Folder),
                    isProtected = false,
                    children = new List<ExplorerItemData>()
                };
                rootFolder.children.Add(target);
            }

            navigationHistory.Clear();
            navigationHistory.Push(rootFolder);
            NavigateToFolder(target, addToHistory: false);
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
                menuItems.Add(new ContextMenuItem($"Vaciar Papelera de Reciclaje ({count})", () =>
                {
                    EmptyRecycleBin();
                    SetStatusMessage("Recycle Bin emptied.");
                }, count > 0));
                menuItems.Add(new ContextMenuItem("Actualizar", RefreshItemsView, true));
            }
            else if (isSystemReadOnlyFolder)
            {
                menuItems.Add(new ContextMenuItem("Actualizar", RefreshItemsView, true));
            }
            else
            {
                menuItems.Add(new ContextMenuItem("+ Nueva Carpeta", () => CreateNewFolder("New Folder"), true));
                menuItems.Add(new ContextMenuItem("+ Nuevo Documento de Texto", () => CreateNewTextDocument("New Document"), true));
                menuItems.Add(new ContextMenuItem("Actualizar", RefreshItemsView, true));
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

            string title = item.fileType == ExplorerFileType.Folder ? "Cambiar nombre de carpeta" : "Cambiar nombre de archivo";
            ContextMenuController.Instance.ShowInputModal(title, item.name, "Guardar", (newName) =>
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

                // Prevent duplicate sibling names
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

            ExplorerItemData targetDest = rootFolder;
            if (item.fileType == ExplorerFileType.TextFile)
            {
                targetDest = FindFolderRecursive(rootFolder, "Documents") ?? rootFolder;
            }
            else if (item.fileType == ExplorerFileType.ImageFile)
            {
                targetDest = FindFolderRecursive(rootFolder, "Pictures") ?? rootFolder;
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
                // Ensure icons and levels are up-to-date on the shared hierarchy
                RefreshIconsRecursive(sharedRootFolder);
            }
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
                if (node.icon == null) node.icon = GetDefaultIcon(ExplorerFileType.ImageFile);
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
                pathText.text = currentFolder != null ? $"This PC > {currentFolder.name}" : "This PC";
            }

            if (btnBack != null)
            {
                btnBack.interactable = navigationHistory.Count > 0;
            }
        }

        public void RefreshItemsView()
        {
            if (itemsContainer == null) return;

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

            go.name = item.name;

            bool isLevelItem = item.fileType == ExplorerFileType.Minigame && item.levelToLaunch != null;
            bool isUnlocked = true;
            if (isLevelItem && ProgressData.Instance != null)
            {
                isUnlocked = ProgressData.Instance.IsLevelUnlocked(item.levelToLaunch.levelId);
            }

            if (iconImg != null)
            {
                Sprite s = item.fileType == ExplorerFileType.TextFile
                    ? (GetTextFileSprite() ?? item.icon ?? GetDefaultIcon(item.fileType))
                    : (item.icon != null ? item.icon : GetDefaultIcon(item.fileType));

                if (s != null) iconImg.sprite = s;
                iconImg.color = isUnlocked ? Color.white : new Color(0.28f, 0.28f, 0.28f, 0.7f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = isUnlocked ? 1f : 0.6f;

            if (label != null)
            {
                label.text = isUnlocked ? item.name : $"{item.name} <color=#777777>[Locked]</color>";
                label.color = isUnlocked ? new Color(0.09f, 0.13f, 0.24f, 1f) : new Color(0.45f, 0.45f, 0.45f, 0.7f);
                label.raycastTarget = false;
            }

            // Remove old Button onClick listener and use ExplorerItemPointerHandler for unified left/double/right-click & drag
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
                    img.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                }
            }
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
                NotepadApp app = StartMenuController.Instance.OpenNotepadWithFile(item);
                if (app != null) return;
            }

            // Direct fallback via WindowManager if StartMenuController isn't present or missing prefab
            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : GetComponentInParent<WindowManager>();
            if (winManager != null)
            {
                GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
                string winTitle = $"Bloc de Notas - {item.name}";
                Window win = winManager.OpenWindow(winPrefab, null, winTitle);
                if (win != null)
                {
                    win.setTitle(winTitle);
                    GameObject notepadPrefab = StartMenuController.Instance != null ? StartMenuController.Instance.NotepadPrefab : null;
#if UNITY_EDITOR
                    if (notepadPrefab == null)
                    {
                        notepadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/NotepadContent.prefab");
                    }
#endif
                    if (notepadPrefab != null)
                    {
                        GameObject contentGo = Instantiate(notepadPrefab);
                        win.SetContent(contentGo);
                        NotepadApp notepad = contentGo.GetComponentInChildren<NotepadApp>();
                        if (notepad != null)
                        {
                            notepad.LoadFile(item, win);
                        }
                    }
                    win.BringToFront();
                }
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

#if UNITY_EDITOR
            if (winPrefab == null)
            {
                winPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/BaseWindow.prefab");
            }
#endif
            if (winPrefab == null) return;

            string winTitle = $"Visor de Imágenes - {item.name}";
            Window win = winManager.OpenWindow(winPrefab, null, winTitle);
            if (win != null)
            {
                openedWindows[key] = win;
                win.setTitle(winTitle);

                Sprite spriteToDisplay = item.imageContent != null
                    ? item.imageContent
                    : (item.icon != null ? item.icon : GetDefaultIcon(ExplorerFileType.ImageFile));

                GameObject viewerContent = BuildImageViewerContent(item.name, spriteToDisplay);
                win.SetContent(viewerContent);
                win.BringToFront();
            }
        }

        private GameObject BuildImageViewerContent(string imageName, Sprite sprite)
        {
            TMP_FontAsset fontAsset = statusFooterText != null ? statusFooterText.font : (pathText != null ? pathText.font : null);

            GameObject root = new GameObject("ImageViewerContent", typeof(RectTransform), typeof(Image));
            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.11f, 0.14f, 0.20f, 1f);

            // Top Toolbar
            GameObject topBar = new GameObject("Toolbar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(root.transform, false);
            RectTransform topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0f, 40f);
            topBar.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.28f, 1f);

            GameObject titleGo = new GameObject("ImageInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(topBar.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0.6f, 1f);
            titleRt.offsetMin = new Vector2(14f, 0f);
            titleRt.offsetMax = Vector2.zero;

            TextMeshProUGUI infoTmp = titleGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) infoTmp.font = fontAsset;
            string dimensions = sprite != null && sprite.texture != null ? $" ({sprite.texture.width}x{sprite.texture.height}px)" : "";
            infoTmp.text = $"{imageName}{dimensions}";
            infoTmp.fontSize = 13f;
            infoTmp.color = new Color(0.85f, 0.9f, 1f, 1f);
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Image viewport area
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(12f, 12f);
            vpRt.offsetMax = new Vector2(-12f, -48f);
            viewport.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 1f);

            // Displayed Image
            GameObject imgHolder = new GameObject("DisplayedImage", typeof(RectTransform), typeof(Image));
            imgHolder.transform.SetParent(viewport.transform, false);
            RectTransform imgRt = imgHolder.GetComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0.1f, 0.1f);
            imgRt.anchorMax = new Vector2(0.9f, 0.9f);
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;
            imgRt.pivot = new Vector2(0.5f, 0.5f);

            Image displayImg = imgHolder.GetComponent<Image>();
            displayImg.sprite = sprite;
            displayImg.preserveAspect = true;
            displayImg.color = Color.white;

            // Zoom buttons
            CreateZoomButton(topBar.transform, "- Zoom", new Vector2(-135f, 0f), fontAsset, () =>
            {
                float nextScale = Mathf.Clamp(imgRt.localScale.x - 0.25f, 0.5f, 3f);
                imgRt.localScale = new Vector3(nextScale, nextScale, 1f);
            });

            CreateZoomButton(topBar.transform, "100%", new Vector2(-75f, 0f), fontAsset, () =>
            {
                imgRt.localScale = Vector3.one;
            });

            CreateZoomButton(topBar.transform, "+ Zoom", new Vector2(-15f, 0f), fontAsset, () =>
            {
                float nextScale = Mathf.Clamp(imgRt.localScale.x + 0.25f, 0.5f, 3f);
                imgRt.localScale = new Vector3(nextScale, nextScale, 1f);
            });

            return root;
        }

        private void CreateZoomButton(Transform parent, string text, Vector2 anchoredPos, TMP_FontAsset fontAsset, Action onClick)
        {
            GameObject btnGo = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(54f, 26f);

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.25f, 0.34f, 0.48f, 1f);

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = text;
            tmp.fontSize = 11f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            btnGo.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
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
                name = "Files",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            // 1. Folder Documents (System container protected, files inside are modifiable & deletable)
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

            // 3. Folder Minigames with all educational minigames (Protected)
            ExplorerItemData gamesFolder = new ExplorerItemData
            {
                name = "Minigames",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Targets & Clicks",
                fileType = ExplorerFileType.Minigame,
                icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                levelToLaunch = targetsLevel,
                isProtected = true
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Tracking & Hover",
                fileType = ExplorerFileType.Minigame,
                icon = mazeGameIcon != null ? mazeGameIcon : defaultGameIcon,
                levelToLaunch = hoverLevel,
                isProtected = true
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Fast Double Click",
                fileType = ExplorerFileType.Minigame,
                icon = targetGameIcon != null ? targetGameIcon : defaultGameIcon,
                levelToLaunch = doubleClickLevel,
                isProtected = true
            });
            gamesFolder.children.Add(new ExplorerItemData
            {
                name = "Adaptive Practice",
                fileType = ExplorerFileType.Minigame,
                icon = mazeGameIcon != null ? mazeGameIcon : defaultGameIcon,
                levelToLaunch = adaptiveLevel,
                isProtected = true
            });

            rootFolder.children.Add(docsFolder);
            rootFolder.children.Add(imagesFolder);
            rootFolder.children.Add(gamesFolder);

            // 4. Folder Levels (Protected)
            ExplorerItemData levelsFolder = new ExplorerItemData
            {
                name = "Levels",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            ExplorerItemData mouseFolder = new ExplorerItemData
            {
                name = "Mouse Levels",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
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
                            levelToLaunch = lvl,
                            isProtected = true
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
                    levelToLaunch = level10CopyPaste,
                    isProtected = true
                });
            }

            // 5. Folder Recycle Bin (Container protected, items inside are user-manageable)
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

            rootFolder.children.Add(levelsFolder);
            rootFolder.children.Add(recycleFolder);
        }

        private static void BuildFallbackStaticFileSystem()
        {
            Sprite txtSprite = GetTextFileSprite();
            Sprite imgSprite = GetImageFileSprite();
            Sprite folderSprite = GetFolderSprite();

            sharedRootFolder = new ExplorerItemData
            {
                name = "Files",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            };

            sharedRootFolder.children.Add(new ExplorerItemData
            {
                name = "Documents",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            });

            sharedRootFolder.children.Add(new ExplorerItemData
            {
                name = "Pictures",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            });

            sharedRootFolder.children.Add(new ExplorerItemData
            {
                name = "Recycle Bin",
                fileType = ExplorerFileType.Folder,
                icon = folderSprite,
                isProtected = true
            });
        }
        #endregion
    }

    /// <summary>
    /// Handles left click (selection), double click (open), right click (context menu), and drag-to-recycle on Explorer items.
    /// </summary>
    public class ExplorerItemPointerHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FileExplorerApp explorer;
        private ExplorerItemData itemData;
        private bool isUnlocked = true;
        private float lastLeftClickTime;
        private GameObject dragGhost;

        public ExplorerItemData ItemData => itemData;
        public FileExplorerApp Explorer => explorer;

        public void Initialize(FileExplorerApp owner, ExplorerItemData data, bool unlocked)
        {
            explorer = owner;
            itemData = data;
            isUnlocked = unlocked;
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
            cg.alpha = 0.75f;
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
