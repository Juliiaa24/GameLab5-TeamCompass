using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Type of items that can be spawned on the desktop.
    /// </summary>
    public enum DesktopItemType
    {
        Application,
        Folder,
        TextDocument,
        ImageDocument,
        Minigame
    }

    /// <summary>
    /// Configuration data for creating a desktop icon.
    /// </summary>
    [System.Serializable]
    public class DesktopIconData
    {
        public string iconName;
        public DesktopItemType itemType;
        [Tooltip("The visual image for the desktop icon")]
        public Sprite iconSprite;

        [Header("Content Settings")]
        [Tooltip("The content prefab to inject into the window (e.g. minigame, text doc)")]
        public GameObject contentPrefab;
        [Tooltip("Optional: The level definition to load")]
        public LevelDefinition levelDefinition;
        [Tooltip("Should this icon start locked?")]
        public bool isLocked;
        [System.NonSerialized] public bool isUserCreated;
        [System.NonSerialized] public ExplorerItemData linkedExplorerItem;
    }

    /// <summary>
    /// Central manager for the virtual desktop environment.
    /// Handles spawning dynamic elements like the Virtual Mascot, Desktop Icons,
    /// and two-way synchronization with the 'This PC > Desktop' folder.
    /// </summary>
    public class DesktopManager : MonoBehaviour
    {
        #region Public Variables
        public static DesktopManager Instance { get; private set; }
        #endregion

        #region Private Component References
        [Header("Window Configuration")]
        [SerializeField] private GameObject baseWindowPrefab;

        [Header("Mascot Configuration")]
        [SerializeField] private GameObject mascotPrefab;
        [SerializeField] private RectTransform mascotContainer;
        [Tooltip("If true, the virtual mascot spawns as an active, interactive desktop companion in the top-right corner")]
        [SerializeField] private bool enableMascotCompanion = false;
        public bool EnableMascotCompanion { get => enableMascotCompanion; set => enableMascotCompanion = value; }

        [Header("Icon Configuration")]
        [SerializeField] private RectTransform desktopIconContainer;
        [SerializeField] private GameObject defaultIconPrefab;
        [SerializeField] private GameObject paintContentPrefab;
        [SerializeField] private GameObject notepadContentPrefab;

        [Header("Initial Desktop Icons")]
        [Tooltip("List of icons to spawn automatically on start")]
        [SerializeField] private List<DesktopIconData> initialDesktopIcons = new List<DesktopIconData>();

        private readonly List<DraggableIcon> activeIcons = new List<DraggableIcon>();
        private bool isSyncingDesktop;
        #endregion

        public IReadOnlyList<DraggableIcon> ActiveIcons => activeIcons;
        public RectTransform DesktopIconContainer => desktopIconContainer;
        public GameObject BaseWindowPrefab => baseWindowPrefab;
        public GameObject PaintContentPrefab => paintContentPrefab;
        public GameObject NotepadContentPrefab => notepadContentPrefab;

        public DraggableIcon GetIconByLevelId(string levelId)
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null && icon.LevelDef != null && icon.LevelDef.levelId == levelId) return icon;
            }
            return null;
        }

        public DraggableIcon GetIconByName(string itemName)
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null && icon.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text == itemName) return icon;
            }
            return null;
        }

        #region Unity Methods
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeDesktop();
        }

        private void OnEnable()
        {
            FileExplorerApp.OnFileSystemChanged += SyncDesktopWithExplorerFolder;
        }

        private void OnDisable()
        {
            FileExplorerApp.OnFileSystemChanged -= SyncDesktopWithExplorerFolder;
        }

        private void Start()
        {
            SpawnInitialIcons();
            RegisterSceneIconsIntoDesktopFolder();
            SyncDesktopWithExplorerFolder();
        }
        #endregion

        #region Private Methods
        private void InitializeDesktop()
        {
            SpawnVirtualMascot();
        }

        private void SpawnVirtualMascot()
        {
            if (mascotPrefab == null)
            {
                return;
            }

            Transform container = mascotContainer;
            if (container == null)
            {
                Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                if (canvas != null) container = canvas.transform;
                else container = transform;
            }

            GameObject mascotInstance = Instantiate(mascotPrefab, container);
            mascotInstance.name = "VirtualMascot";

            VirtualMascot mascot = mascotInstance.GetComponent<VirtualMascot>();
            if (enableMascotCompanion && mascot != null)
            {
                Transform windowsArea = container.Find("WindowsArea");
                if (windowsArea != null)
                {
                    mascotInstance.transform.SetSiblingIndex(windowsArea.GetSiblingIndex());
                }

                mascotInstance.SetActive(true);
                mascot.InitAsDesktopCompanion();
            }
            else
            {
                mascotInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Identifies core system applications/folders that cannot be deleted to the Recycle Bin.
        /// </summary>
        public static bool IsSystemProtectedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string clean = name.Trim();
            return clean.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Minigames", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Paint", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Notepad", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applications and shortcuts that reside purely on the desktop/taskbar
        /// and should never be created as dummy folders inside 'This PC > Desktop'.
        /// </summary>
        public static bool IsDesktopOnlyApplication(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string clean = name.Trim();
            return clean.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("File Explorer", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Paint", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Notepad", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ensures pre-existing desktop icons (and Paint shortcut) are registered in the active list and mirrored in This PC > Desktop.
        /// </summary>
        private void RegisterSceneIconsIntoDesktopFolder()
        {
            if (desktopIconContainer != null)
            {
                DraggableIcon[] existing = desktopIconContainer.GetComponentsInChildren<DraggableIcon>(true);
                foreach (var ic in existing)
                {
                    if (ic != null)
                    {
                        if (!activeIcons.Contains(ic))
                        {
                            activeIcons.Add(ic);
                        }

                        // Ensure CanvasGroup is attached and blocks raycasts
                        CanvasGroup cg = ic.GetComponent<CanvasGroup>() ?? ic.gameObject.AddComponent<CanvasGroup>();
                        cg.blocksRaycasts = true;

                        // Ensure RecycleBinIcon component is attached to the Recycle Bin desktop icon
                        if (ic.gameObject.name.IndexOf("Recycle", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            ic.GetComponent<RecycleBinIcon>() == null)
                        {
                            ic.gameObject.AddComponent<RecycleBinIcon>();
                        }
                    }
                }
            }

            // Mirror desktop icons into This PC > Desktop folder
            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
            if (desktopFolder == null) return;

            foreach (var icon in activeIcons)
            {
                if (icon == null) continue;
                if (icon.GetComponent<RecycleBinIcon>() != null) continue;

                TMPro.TextMeshProUGUI label = icon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                string iconTitle = label != null && !string.IsNullOrWhiteSpace(label.text) ? label.text.Trim() : icon.gameObject.name;

                if (IsDesktopOnlyApplication(iconTitle))
                {
                    icon.ConfigureMetadata(icon.ItemType, isProtected: true, linkedItem: null);
                    continue;
                }

                bool isSystemProtected = (icon.LevelDef != null) || IsSystemProtectedName(iconTitle);

                if (icon.LinkedExplorerItem != null)
                {
                    if (!desktopFolder.children.Contains(icon.LinkedExplorerItem))
                    {
                        desktopFolder.children.Add(icon.LinkedExplorerItem);
                    }
                    continue;
                }

                // Check if already in desktopFolder
                ExplorerItemData match = null;
                foreach (var child in desktopFolder.children)
                {
                    if (child.name.Equals(iconTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        match = child;
                        break;
                    }
                }

                if (match == null)
                {
                    UnityEngine.UI.Image img = icon.GetComponent<UnityEngine.UI.Image>();
                    ExplorerFileType fType = ExplorerFileType.Folder;
                    if (icon.LevelDef != null || icon.ItemType == DesktopItemType.Minigame)
                        fType = ExplorerFileType.Minigame;
                    else if (iconTitle.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || icon.ItemType == DesktopItemType.TextDocument)
                        fType = ExplorerFileType.TextFile;
                    else if (iconTitle.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || icon.ItemType == DesktopItemType.ImageDocument)
                        fType = ExplorerFileType.ImageFile;

                    Sprite iconSpr = fType == ExplorerFileType.ImageFile
                        ? FileExplorerApp.GetImageFileSprite()
                        : (img != null && img.sprite != null ? img.sprite : FileExplorerApp.GetFolderSprite());

                    match = new ExplorerItemData
                    {
                        name = iconTitle,
                        fileType = fType,
                        icon = iconSpr,
                        levelToLaunch = icon.LevelDef,
                        isProtected = isSystemProtected
                    };
                    desktopFolder.children.Add(match);
                }

                icon.ConfigureMetadata(icon.ItemType, isSystemProtected, match);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Synchronizes the Desktop icons with the contents of 'This PC > Desktop'.
        /// Any user file/folder added to 'This PC > Desktop' spawns on the Desktop;
        /// any user file/folder removed or moved out of 'This PC > Desktop' is removed from the Desktop.
        /// </summary>
        public void SyncDesktopWithExplorerFolder()
        {
            if (isSyncingDesktop) return;
            isSyncingDesktop = true;

            try
            {
                ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
                if (desktopFolder == null || desktopFolder.children == null) return;

                // 1. Remove or update non-protected desktop icons whose LinkedExplorerItem was deleted/moved out of Desktop folder
                for (int i = activeIcons.Count - 1; i >= 0; i--)
                {
                    DraggableIcon icon = activeIcons[i];
                    if (icon == null)
                    {
                        activeIcons.RemoveAt(i);
                        continue;
                    }

                    if (icon.GetComponent<RecycleBinIcon>() != null || icon.IsProtected) continue;

                    if (icon.LinkedExplorerItem != null)
                    {
                        if (!desktopFolder.children.Contains(icon.LinkedExplorerItem))
                        {
                            // Moved to another folder or Recycle Bin! Remove from Desktop surface
                            activeIcons.RemoveAt(i);
                            Destroy(icon.gameObject);
                        }
                        else
                        {
                            // Sync name & sprite in case it was renamed or edited in Paint/Notepad
                            icon.gameObject.name = icon.LinkedExplorerItem.name;
                            var tmp = icon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                            if (tmp != null) tmp.text = icon.LinkedExplorerItem.name;

                            var img = icon.GetComponent<UnityEngine.UI.Image>();
                            if (img != null)
                            {
                                if (icon.LinkedExplorerItem.fileType == ExplorerFileType.ImageFile)
                                {
                                    img.sprite = FileExplorerApp.GetImageFileSprite();
                                }
                                else if (icon.LinkedExplorerItem.icon != null)
                                {
                                    img.sprite = icon.LinkedExplorerItem.icon;
                                }
                            }
                        }
                    }
                }

                // 2. Spawn desktop icons for any items inside 'This PC > Desktop' that don't yet have a Desktop icon
                foreach (var item in desktopFolder.children)
                {
                    if (item == null) continue;

                    bool alreadyOnDesktop = false;
                    foreach (var icon in activeIcons)
                    {
                        if (icon == null) continue;
                        if (icon.LinkedExplorerItem == item ||
                            icon.gameObject.name.Equals(item.name, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyOnDesktop = true;
                            break;
                        }
                    }

                    if (!alreadyOnDesktop)
                    {
                        SpawnIconForExplorerItem(item);
                    }
                }
            }
            finally
            {
                isSyncingDesktop = false;
            }
        }

        /// <summary>
        /// Spawns a Desktop icon linked to a specific ExplorerItemData at an optional screen position.
        /// </summary>
        public GameObject SpawnIconForExplorerItem(ExplorerItemData item, Vector2? dropScreenPosition = null)
        {
            if (item == null) return null;

            DesktopItemType dType = DesktopItemType.Folder;
            GameObject contentPrefab = null;
            Sprite iconSprite = item.icon;

            switch (item.fileType)
            {
                case ExplorerFileType.Folder:
                    dType = DesktopItemType.Folder;
                    contentPrefab = StartMenuController.Instance != null ? StartMenuController.Instance.FileExplorerPrefab : Resources.Load<GameObject>("FileExplorerContent");
                    if (iconSprite == null) iconSprite = FileExplorerApp.GetFolderSprite();
                    break;

                case ExplorerFileType.TextFile:
                    dType = DesktopItemType.TextDocument;
                    contentPrefab = StartMenuController.Instance != null ? StartMenuController.Instance.NotepadPrefab : null;
                    if (iconSprite == null) iconSprite = FileExplorerApp.GetTextFileSprite();
                    break;

                case ExplorerFileType.ImageFile:
                    dType = DesktopItemType.ImageDocument;
                    contentPrefab = paintContentPrefab != null ? paintContentPrefab : (StartMenuController.Instance != null ? StartMenuController.Instance.PaintPrefab : null);
                    iconSprite = FileExplorerApp.GetImageFileSprite();
                    break;

                case ExplorerFileType.Minigame:
                    dType = DesktopItemType.Minigame;
                    break;
            }

            DesktopIconData data = new DesktopIconData
            {
                iconName = item.name,
                itemType = dType,
                iconSprite = iconSprite,
                contentPrefab = contentPrefab,
                levelDefinition = item.levelToLaunch,
                isLocked = false,
                isUserCreated = !item.isProtected,
                linkedExplorerItem = item
            };

            GameObject go = SpawnIcon(data);
            if (go != null && dropScreenPosition.HasValue)
            {
                DraggableIcon di = go.GetComponent<DraggableIcon>();
                IconGrid grid = desktopIconContainer != null ? desktopIconContainer.GetComponent<IconGrid>() : null;
                if (di != null && grid != null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    grid.TryPlaceIcon(di, dropScreenPosition.Value, go.transform.position, cam);
                }
            }
            return go;
        }

        /// <summary>
        /// Spawns a new icon on the desktop grid.
        /// </summary>
        public GameObject SpawnIcon(DesktopIconData iconData)
        {
            if (desktopIconContainer == null)
            {
                Debug.LogWarning("[DesktopManager] Desktop Icon Container is missing! Skipping icon spawn.");
                return null;
            }

            if (defaultIconPrefab == null)
            {
                Debug.LogError("[DesktopManager] Default Icon Prefab is missing in DesktopManager!");
                return null;
            }

            GameObject iconInstance = Instantiate(defaultIconPrefab, desktopIconContainer);
            iconInstance.name = iconData.iconName;

            CanvasGroup canvasGroup = iconInstance.GetComponent<CanvasGroup>() ?? iconInstance.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            DraggableIcon draggable = iconInstance.GetComponent<DraggableIcon>();
            if (draggable != null)
            {
                if (!activeIcons.Contains(draggable)) activeIcons.Add(draggable);
            }

            // Ensure RecycleBinIcon component exists if this is the Recycle Bin
            if (iconData.iconName.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase) ||
                iconInstance.name.IndexOf("Recycle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (iconInstance.GetComponent<RecycleBinIcon>() == null)
                {
                    iconInstance.AddComponent<RecycleBinIcon>();
                }
            }

            Sprite spriteToUse = iconData.iconSprite;
            if (iconData.itemType == DesktopItemType.TextDocument &&
                !string.Equals(iconData.iconName, "Notepad", System.StringComparison.OrdinalIgnoreCase))
            {
                Sprite txtSprite = FileExplorerApp.GetTextFileSprite();
                if (txtSprite != null) spriteToUse = txtSprite;
            }
            else if (iconData.itemType == DesktopItemType.ImageDocument &&
                !string.Equals(iconData.iconName, "Paint", System.StringComparison.OrdinalIgnoreCase))
            {
                spriteToUse = FileExplorerApp.GetImageFileSprite();
            }

            if (spriteToUse != null)
            {
                UnityEngine.UI.Image img = iconInstance.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = spriteToUse;
                }
            }

            TMPro.TextMeshProUGUI textComponent = iconInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = iconData.iconName;
            }

            if (draggable != null)
            {
                draggable.SetupDynamicIcon(baseWindowPrefab, iconData.contentPrefab, iconData.levelDefinition, iconData.isLocked);
                bool isProtected = !iconData.isUserCreated && IsSystemProtectedName(iconData.iconName);
                draggable.ConfigureMetadata(iconData.itemType, isProtected, iconData.linkedExplorerItem);

                IconGrid grid = desktopIconContainer != null ? desktopIconContainer.GetComponent<IconGrid>() : null;
                if (grid != null)
                {
                    grid.PlaceInNextAvailableSlot(draggable);
                }
            }

            return iconInstance;
        }

        public void UnregisterIcon(DraggableIcon icon)
        {
            if (icon != null)
            {
                activeIcons.Remove(icon);
            }
        }

        [ContextMenu("Spawn Initial Icons (Test)")]
        private List<GameObject> SpawnInitialIcons()
        {
            List<GameObject> spawned = new List<GameObject>();

            if (initialDesktopIcons == null || initialDesktopIcons.Count == 0)
            {
                return spawned;
            }

            foreach (var iconData in initialDesktopIcons)
            {
                if (iconData == null || string.IsNullOrWhiteSpace(iconData.iconName)) continue;

                // Check if already present on desktop container
                bool alreadyExists = false;
                if (desktopIconContainer != null)
                {
                    foreach (Transform child in desktopIconContainer)
                    {
                        if (child == null) continue;
                        if (child.name.Equals(iconData.iconName, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                        var tmp = child.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        if (tmp != null && tmp.text.Trim().Equals(iconData.iconName, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                }

                if (alreadyExists) continue;

                GameObject icon = SpawnIcon(iconData);
                if (icon != null) spawned.Add(icon);
            }

            return spawned;
        }
        #endregion
    }
}
