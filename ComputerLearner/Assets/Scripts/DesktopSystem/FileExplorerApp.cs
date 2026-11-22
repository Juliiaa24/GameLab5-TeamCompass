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
        public static Sprite SharedPaintAppIcon { get; private set; }
        public static Sprite SharedNotepadAppIcon { get; private set; }

        public static ExplorerItemData ActiveExplorerFolder { get; private set; }
        public static bool IsClipboardCut { get; set; }
        public static bool IsDesktopFocused { get; set; } = true;
        private static readonly List<FileExplorerApp> activeExplorers = new List<FileExplorerApp>();
        private static readonly Stack<List<ExplorerItemData>> createdItemsUndoStack = new Stack<List<ExplorerItemData>>();
        public static bool HasCreatedItemsToUndo => createdItemsUndoStack.Count > 0;
        private static int lastClipboardShortcutFrame = -1;
        private static bool isRestoringSnapshot;
        private const string SNAPSHOT_PREFS_KEY = "VirtualFileSystemSnapshot_v1";
        public const int MAX_FOLDER_DEPTH = 8;
        public const int MAX_ITEMS_PER_FOLDER = 16;
        public const int MAX_BASE_NAME_LENGTH = 16;
        public const int MAX_ITEM_NAME_LENGTH = 20;

        public static bool IsFolderAtCapacity(ExplorerItemData folder)
        {
            return folder != null && folder.children != null && folder.children.Count >= MAX_ITEMS_PER_FOLDER;
        }

        public static string ClampBaseName(string baseName, int maxBaseLen = MAX_BASE_NAME_LENGTH)
        {
            if (string.IsNullOrWhiteSpace(baseName)) return "New Item";
            string trimmed = baseName.Trim();
            int limit = Mathf.Max(3, maxBaseLen);
            if (trimmed.Length > limit)
            {
                trimmed = trimmed.Substring(0, limit).TrimEnd();
            }
            return string.IsNullOrEmpty(trimmed) ? "New Item" : trimmed;
        }

        public static string ClampAndFormatItemName(string rawName, string requiredExtension = "")
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                rawName = "New Item";
            }

            string clean = rawName.Trim();
            string ext = requiredExtension ?? string.Empty;

            if (!string.IsNullOrEmpty(ext))
            {
                if (clean.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    clean = clean.Substring(0, clean.Length - ext.Length).TrimEnd();
                }
            }
            else
            {
                if (clean.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".txt";
                    clean = clean.Substring(0, clean.Length - 4).TrimEnd();
                }
                else if (clean.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".png";
                    clean = clean.Substring(0, clean.Length - 4).TrimEnd();
                }
                else if (clean.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".jpg";
                    clean = clean.Substring(0, clean.Length - 4).TrimEnd();
                }
            }

            string clampedBase = ClampBaseName(clean, MAX_BASE_NAME_LENGTH);
            return clampedBase + ext;
        }

        public static void ResetStaticFileSystem()
        {
            sharedRootFolder = null;
            activeExplorers.Clear();
            createdItemsUndoStack.Clear();
            clipboardItems.Clear();
            clipboardSourceItems.Clear();
            IsClipboardCut = false;
            ActiveExplorerFolder = null;
            IsDesktopFocused = true;
            PlayerPrefs.DeleteKey(SNAPSHOT_PREFS_KEY);
            PlayerPrefs.Save();
        }

        public static void RegisterCreatedItemsForUndo(ExplorerItemData item)
        {
            if (item == null || IsItemProtectedStatic(item)) return;
            createdItemsUndoStack.Push(new List<ExplorerItemData> { item });
        }

        public static void RegisterCreatedItemsForUndo(IEnumerable<ExplorerItemData> items)
        {
            if (items == null) return;
            List<ExplorerItemData> batch = new List<ExplorerItemData>();
            foreach (var item in items)
            {
                if (item != null && !IsItemProtectedStatic(item) && !batch.Contains(item))
                {
                    batch.Add(item);
                }
            }
            if (batch.Count > 0)
            {
                createdItemsUndoStack.Push(batch);
            }
        }

        public static bool UndoLastCreatedItems()
        {
            ExplorerItemData root = GetRootFolder();
            if (root == null) return false;
            ExplorerItemData recycleBin = FindFolderRecursiveStatic(root, "Recycle Bin");

            while (createdItemsUndoStack.Count > 0)
            {
                List<ExplorerItemData> batch = createdItemsUndoStack.Pop();
                if (batch == null || batch.Count == 0) continue;

                List<string> removedNames = new List<string>();
                foreach (var item in batch)
                {
                    if (item == null || IsItemProtectedStatic(item)) continue;
                    if (recycleBin != null && recycleBin.children != null && IsFolderDescendantOf(item, recycleBin))
                    {
                        continue;
                    }

                    if (RemoveItemRecursive(root, item))
                    {
                        removedNames.Add(item.name);
                    }
                }

                if (removedNames.Count > 0)
                {
                    InputModalDialog openModal = UnityEngine.Object.FindAnyObjectByType<InputModalDialog>();
                    if (openModal != null)
                    {
                        if (Application.isPlaying) Destroy(openModal.gameObject);
                        else DestroyImmediate(openModal.gameObject);
                    }

                    foreach (var exp in activeExplorers)
                    {
                        if (exp == null) continue;
                        for (int i = exp.selectedItems.Count - 1; i >= 0; i--)
                        {
                            if (batch.Contains(exp.selectedItems[i]))
                            {
                                exp.selectedItems.RemoveAt(i);
                            }
                        }
                    }

                    DraggableIcon.ClearSelection();
                    NotifyFileSystemChanged();

                    string statusMsg = removedNames.Count == 1
                        ? $"Undid creation of '{removedNames[0]}'"
                        : $"Undid creation of {removedNames.Count} items";
                    foreach (var exp in activeExplorers)
                    {
                        if (exp != null) exp.SetStatusMessage(statusMsg);
                    }

                    FeedbackManager.PlayClick();
                    return true;
                }
            }

            return false;
        }

        public static void ClearAllExplorerSelections(FileExplorerApp except = null)
        {
            for (int i = activeExplorers.Count - 1; i >= 0; i--)
            {
                FileExplorerApp exp = activeExplorers[i];
                if (exp == null)
                {
                    activeExplorers.RemoveAt(i);
                    continue;
                }
                if (exp != except)
                {
                    exp.ClearSelection();
                }
            }
        }

        public static bool HasActiveFocusedExplorer()
        {
            if (WindowManager.Instance != null && WindowManager.Instance.ActiveWindow != null)
            {
                return WindowManager.Instance.ActiveWindow.GetComponentInChildren<FileExplorerApp>() != null;
            }
            for (int i = activeExplorers.Count - 1; i >= 0; i--)
            {
                FileExplorerApp exp = activeExplorers[i];
                if (exp == null)
                {
                    activeExplorers.RemoveAt(i);
                    continue;
                }
                if (exp.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            return false;
        }

        [Serializable]
        private class VirtualFileSystemSnapshot
        {
            public List<SnapshotItemEntry> entries = new List<SnapshotItemEntry>();
        }

        [Serializable]
        private class SnapshotItemEntry
        {
            public string parentPath;
            public string name;
            public int fileType;
            public string textContent;
            public string iconSpriteName;
            public string imagePngBase64;
        }

        public static void NotifyFileSystemChanged()
        {
            if (!isRestoringSnapshot && sharedRootFolder != null)
            {
                SaveFileSystemSnapshot(sharedRootFolder);
            }
            OnFileSystemChanged?.Invoke();
        }

        public static string EncodeSpriteToPngBase64(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return string.Empty;

            Texture2D srcTex = sprite.texture;
            try
            {
                if (srcTex.isReadable &&
                    Mathf.RoundToInt(sprite.rect.width) == srcTex.width &&
                    Mathf.RoundToInt(sprite.rect.height) == srcTex.height)
                {
                    byte[] rawPng = srcTex.EncodeToPNG();
                    if (rawPng != null && rawPng.Length > 0)
                    {
                        return Convert.ToBase64String(rawPng);
                    }
                }
            }
            catch
            {
                // Fallback to RenderTexture blit below
            }

            int w = Mathf.Clamp(Mathf.RoundToInt(sprite.rect.width), 1, 640);
            int h = Mathf.Clamp(Mathf.RoundToInt(sprite.rect.height), 1, 400);
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
                Graphics.Blit(srcTex, rt);
                readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                readable.Apply();
                byte[] png = readable.EncodeToPNG();
                return png != null && png.Length > 0 ? Convert.ToBase64String(png) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                if (Application.isPlaying) Destroy(readable);
                else DestroyImmediate(readable);
            }
        }

        public static Sprite DecodeSpriteFromPngBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                if (bytes == null || bytes.Length == 0) return null;
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
            }
            catch
            {
                // Ignore malformed base64
            }
            return null;
        }

        private static Sprite FindSpriteByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
            if (loaded != null)
            {
                foreach (var sp in loaded)
                {
                    if (sp != null && string.Equals(sp.name, spriteName, StringComparison.OrdinalIgnoreCase))
                    {
                        return sp;
                    }
                }
            }
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{spriteName} t:Sprite");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null && string.Equals(sp.name, spriteName, StringComparison.OrdinalIgnoreCase))
                {
                    return sp;
                }
            }
#endif
            return null;
        }

        public static void SaveFileSystemSnapshot(ExplorerItemData root = null)
        {
            if (isRestoringSnapshot) return;
            if (root == null) root = sharedRootFolder;
            if (root == null || root.children == null) return;

            VirtualFileSystemSnapshot snapshot = new VirtualFileSystemSnapshot();
            string[] trackedRootFolders = { "Desktop", "Documents", "Pictures", "Recycle Bin" };

            foreach (string folderName in trackedRootFolders)
            {
                ExplorerItemData sysFolder = FindFolderRecursiveStatic(root, folderName);
                if (sysFolder != null && sysFolder.children != null)
                {
                    CollectSnapshotEntriesRecursive(sysFolder, folderName, snapshot.entries, 1);
                }
            }

            string json = JsonUtility.ToJson(snapshot);
            PlayerPrefs.SetString(SNAPSHOT_PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        private static void CollectSnapshotEntriesRecursive(ExplorerItemData parentFolder, string parentPath, List<SnapshotItemEntry> list, int currentDepth = 0)
        {
            if (parentFolder == null || parentFolder.children == null || currentDepth > MAX_FOLDER_DEPTH) return;

            foreach (var item in parentFolder.children)
            {
                if (item == null) continue;
                // Skip protected system folders/minigames (such as 'Levels' and its children)
                if (item.isProtected || item.fileType == ExplorerFileType.Minigame || IsReadOnlySystemFolderStatic(item))
                {
                    continue;
                }

                string imgBase64 = string.Empty;
                if (item.fileType == ExplorerFileType.ImageFile && item.imageContent != null && item.imageContent != GetImageFileSprite())
                {
                    imgBase64 = EncodeSpriteToPngBase64(item.imageContent);
                }

                SnapshotItemEntry entry = new SnapshotItemEntry
                {
                    parentPath = parentPath,
                    name = item.name,
                    fileType = (int)item.fileType,
                    textContent = item.textContent ?? string.Empty,
                    iconSpriteName = item.icon != null ? item.icon.name : string.Empty,
                    imagePngBase64 = imgBase64
                };
                list.Add(entry);

                if (item.fileType == ExplorerFileType.Folder && item.children != null && item.children.Count > 0)
                {
                    CollectSnapshotEntriesRecursive(item, $"{parentPath}/{item.name}", list, currentDepth + 1);
                }
            }
        }

        private static void RestoreFileSystemSnapshotIfAvailable(ExplorerItemData root)
        {
            if (root == null || !PlayerPrefs.HasKey(SNAPSHOT_PREFS_KEY)) return;

            string json = PlayerPrefs.GetString(SNAPSHOT_PREFS_KEY, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            VirtualFileSystemSnapshot snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<VirtualFileSystemSnapshot>(json);
            }
            catch
            {
                return;
            }
            if (snapshot == null || snapshot.entries == null) return;

            isRestoringSnapshot = true;
            try
            {
                string[] trackedRootFolders = { "Desktop", "Documents", "Pictures", "Recycle Bin" };
                foreach (string folderName in trackedRootFolders)
                {
                    ExplorerItemData sysFolder = FindFolderRecursiveStatic(root, folderName);
                    if (sysFolder != null && sysFolder.children != null)
                    {
                        sysFolder.children.RemoveAll(c => c != null && !c.isProtected && c.fileType != ExplorerFileType.Minigame && !IsReadOnlySystemFolderStatic(c));
                    }
                }

                foreach (var entry in snapshot.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.name) || string.IsNullOrEmpty(entry.parentPath))
                        continue;

                    ExplorerItemData parentFolder = ResolveFolderByRelativePath(root, entry.parentPath);
                    if (parentFolder == null) continue;
                    if (parentFolder.children == null) parentFolder.children = new List<ExplorerItemData>();

                    ExplorerFileType fType = (ExplorerFileType)entry.fileType;
                    Sprite itemIcon = null;
                    if (!string.IsNullOrEmpty(entry.iconSpriteName))
                    {
                        itemIcon = FindSpriteByName(entry.iconSpriteName);
                    }
                    if (itemIcon == null)
                    {
                        if (fType == ExplorerFileType.Folder) itemIcon = GetFolderSprite();
                        else if (fType == ExplorerFileType.TextFile) itemIcon = GetTextFileSprite();
                        else if (fType == ExplorerFileType.ImageFile) itemIcon = GetImageFileSprite();
                    }

                    Sprite imgContent = null;
                    if (fType == ExplorerFileType.ImageFile)
                    {
                        imgContent = DecodeSpriteFromPngBase64(entry.imagePngBase64);
                        if (imgContent == null) imgContent = itemIcon;
                    }

                    ExplorerItemData restoredItem = new ExplorerItemData
                    {
                        name = entry.name,
                        fileType = fType,
                        icon = itemIcon,
                        textContent = entry.textContent ?? string.Empty,
                        imageContent = imgContent,
                        isProtected = false,
                        children = new List<ExplorerItemData>()
                    };

                    parentFolder.children.Add(restoredItem);
                }
            }
            finally
            {
                isRestoringSnapshot = false;
            }
        }

        private static ExplorerItemData ResolveFolderByRelativePath(ExplorerItemData root, string relativePath)
        {
            if (root == null || string.IsNullOrEmpty(relativePath)) return null;
            string[] parts = relativePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            ExplorerItemData current = FindFolderRecursiveStatic(root, parts[0]);
            if (current == null) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current.children == null) return null;
                ExplorerItemData next = null;
                foreach (var child in current.children)
                {
                    if (child != null && child.fileType == ExplorerFileType.Folder &&
                        string.Equals(child.name, parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        next = child;
                        break;
                    }
                }
                if (next == null) return null;
                current = next;
            }
            return current;
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

        public static Sprite GetPaintAppSprite()
        {
            if (SharedPaintAppIcon != null) return SharedPaintAppIcon;
#if UNITY_EDITOR
            SharedPaintAppIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Paint_App_Icon_01.png");
            if (SharedPaintAppIcon != null) return SharedPaintAppIcon;
#endif
            SharedPaintAppIcon = Resources.Load<Sprite>("Paint_App_Icon_01");
            return SharedPaintAppIcon;
        }

        public static Sprite GetNotepadAppSprite()
        {
            if (SharedNotepadAppIcon != null) return SharedNotepadAppIcon;
#if UNITY_EDITOR
            SharedNotepadAppIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Notepad_App_Icon_01.png");
            if (SharedNotepadAppIcon != null) return SharedNotepadAppIcon;
#endif
            SharedNotepadAppIcon = Resources.Load<Sprite>("Notepad_App_Icon_01");
            return SharedNotepadAppIcon;
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
        [SerializeField] private Button btnNewTextToolbar;
        private static readonly List<ExplorerItemData> clipboardItems = new List<ExplorerItemData>();
        private static readonly List<ExplorerItemData> clipboardSourceItems = new List<ExplorerItemData>();
        public static bool HasClipboardItems => clipboardItems.Count > 0;
        public static ExplorerItemData SharedClipboardItem
        {
            get => clipboardItems.Count > 0 ? clipboardItems[0] : null;
            set
            {
                clipboardItems.Clear();
                clipboardSourceItems.Clear();
                if (value != null)
                {
                    clipboardItems.Add(CloneItemForClipboard(value));
                    clipboardSourceItems.Add(value);
                }
            }
        }
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
        [NonSerialized] private readonly List<ExplorerItemData> selectedItems = new List<ExplorerItemData>();
        public IReadOnlyList<ExplorerItemData> SelectedItems => selectedItems;
        private ExplorerItemData PrimarySelectedItem => selectedItems.Count > 0 ? selectedItems[selectedItems.Count - 1] : null;
        private GameObject marqueeBoxObject;
        private RectTransform marqueeBoxRect;
        private Vector2 marqueeStartScreenPos;
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
            if (btnNewTextToolbar == null && btnNewFolderToolbar != null && btnNewFolderToolbar.transform.parent != null)
            {
                GameObject textBtnObj = Instantiate(btnNewFolderToolbar.gameObject, btnNewFolderToolbar.transform.parent);
                textBtnObj.name = "BtnNewTextToolbar";
                btnNewTextToolbar = textBtnObj.GetComponent<Button>();
                var tmp = textBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "+ New Text";
                textBtnObj.transform.SetSiblingIndex(btnNewImageToolbar != null ? btnNewImageToolbar.transform.GetSiblingIndex() + 1 : btnNewFolderToolbar.transform.GetSiblingIndex() + 1);
            }
            if (btnNewTextToolbar != null)
            {
                btnNewTextToolbar.onClick.RemoveAllListeners();
                btnNewTextToolbar.onClick.AddListener(() => CreateNewTextDocument("New Document"));
            }

            EnsureSharedFileSystem();
            rootFolder = sharedRootFolder;
        }

        private void OnEnable()
        {
            if (!activeExplorers.Contains(this))
            {
                activeExplorers.Add(this);
            }
            DraggableIcon.ClearSelection();
            IsDesktopFocused = false;

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged += RefreshItemsView;
            }
            OnFileSystemChanged += RefreshItemsView;
        }

        private void OnDisable()
        {
            if (marqueeBoxObject != null) marqueeBoxObject.SetActive(false);
            activeExplorers.Remove(this);
            if (ProgressData.HasInstance)
            {
                ProgressData.Instance.OnProgressChanged -= RefreshItemsView;
            }
            OnFileSystemChanged -= RefreshItemsView;
        }

        private void Start()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
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

        private static bool IsTextInputFocused()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                var sel = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                if (sel != null && (sel.GetComponent<TMPro.TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null))
                {
                    return true;
                }
            }
            if (WindowManager.Instance != null && WindowManager.Instance.ActiveWindow != null &&
                WindowManager.Instance.ActiveWindow.GetComponentInChildren<NotepadApp>() != null)
            {
                return true;
            }
            return false;
        }

        private bool IsPrimaryActiveExplorer()
        {
            if (!gameObject.activeInHierarchy) return false;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null && WindowManager.Instance != null)
            {
                return WindowManager.Instance.ActiveWindow == parentWindow;
            }
            for (int i = activeExplorers.Count - 1; i >= 0; i--)
            {
                if (activeExplorers[i] != null && activeExplorers[i].gameObject.activeInHierarchy)
                {
                    return activeExplorers[i] == this;
                }
            }
            return true;
        }

        private void Update()
        {
            if (IsTextInputFocused() || IsDesktopFocused || !IsPrimaryActiveExplorer()) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool deletePressed = kb != null && kb.deleteKey.wasPressedThisFrame;
            bool isCtrl = kb != null && (kb.ctrlKey.isPressed || kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
            bool isC = kb != null && kb.cKey.wasPressedThisFrame;
            bool isX = kb != null && kb.xKey.wasPressedThisFrame;
            bool isV = kb != null && kb.vKey.wasPressedThisFrame;
            bool isD = kb != null && kb.dKey.wasPressedThisFrame;
            bool isZ = kb != null && kb.zKey.wasPressedThisFrame;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!deletePressed) deletePressed = Input.GetKeyDown(KeyCode.Delete);
            if (!isCtrl) isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!isC) isC = Input.GetKeyDown(KeyCode.C);
            if (!isX) isX = Input.GetKeyDown(KeyCode.X);
            if (!isV) isV = Input.GetKeyDown(KeyCode.V);
            if (!isD) isD = Input.GetKeyDown(KeyCode.D);
            if (!isZ) isZ = Input.GetKeyDown(KeyCode.Z);
#endif

            if (deletePressed && selectedItems.Count > 0)
            {
                DeleteSelectedItems();
            }

            if (!isCtrl || Time.frameCount == lastClipboardShortcutFrame) return;

            if (isC && selectedItems.Count > 0)
            {
                lastClipboardShortcutFrame = Time.frameCount;
                CopySelectedItems();
            }
            else if (isX && selectedItems.Count > 0)
            {
                lastClipboardShortcutFrame = Time.frameCount;
                CutSelectedItems();
            }
            else if (isV && currentFolder != null)
            {
                lastClipboardShortcutFrame = Time.frameCount;
                PasteClipboardItem();
            }
            else if (isD && selectedItems.Count > 0 && currentFolder != null)
            {
                lastClipboardShortcutFrame = Time.frameCount;
                DuplicateSelectedItems();
            }
            else if (isZ)
            {
                lastClipboardShortcutFrame = Time.frameCount;
                UndoLastCreatedItems();
            }
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e == null || IsTextInputFocused() || IsDesktopFocused || !IsPrimaryActiveExplorer()) return;

            if (e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand)
            {
                string cmd = e.commandName;
                if (cmd == "Copy" || cmd == "Cut" || cmd == "Paste")
                {
                    if (e.type == EventType.ValidateCommand)
                    {
                        e.Use();
                        return;
                    }

                    if (Time.frameCount == lastClipboardShortcutFrame) return;

                    if (cmd == "Copy" && selectedItems.Count > 0)
                    {
                        lastClipboardShortcutFrame = Time.frameCount;
                        CopySelectedItems();
                        e.Use();
                    }
                    else if (cmd == "Cut" && selectedItems.Count > 0)
                    {
                        lastClipboardShortcutFrame = Time.frameCount;
                        CutSelectedItems();
                        e.Use();
                    }
                    else if (cmd == "Paste" && currentFolder != null)
                    {
                        lastClipboardShortcutFrame = Time.frameCount;
                        PasteClipboardItem();
                        e.Use();
                    }
                }
            }
        }

        public static bool TryConsumeClipboardShortcutFrame()
        {
            if (Time.frameCount == lastClipboardShortcutFrame) return false;
            lastClipboardShortcutFrame = Time.frameCount;
            return true;
        }

        private void OnApplicationQuit()
        {
            if (sharedRootFolder != null)
            {
                SaveFileSystemSnapshot(sharedRootFolder);
            }
        }
        #endregion

        private void SetImageAsDesktopWallpaper(ExplorerItemData item)
        {
            if (item == null) return;
            Sprite s = item.imageContent != null ? item.imageContent : item.icon;
            if (s != null && DesktopManager.Instance != null)
            {
                DesktopManager.Instance.SetDesktopWallpaperSprite(s);
                SetStatusMessage($"Set '{item.name}' as desktop background.");
                FeedbackManager.PlaySuccess();
            }
        }

        #region Clipboard Operations
        public static bool IsItemProtectedStatic(ExplorerItemData item)
        {
            if (item == null) return false;
            return item.isProtected || IsProtectedSystemFolderStatic(item) || IsReadOnlySystemFolderStatic(item);
        }

        public static bool IsItemInCutClipboard(ExplorerItemData item)
        {
            if (!IsClipboardCut || item == null) return false;
            return clipboardSourceItems.Contains(item) || clipboardItems.Contains(item);
        }

        private static ExplorerItemData CloneItemForClipboard(ExplorerItemData source)
        {
            return new ExplorerItemData
            {
                name = source.name,
                fileType = source.fileType,
                icon = source.icon,
                textContent = source.textContent,
                imageContent = source.imageContent,
                levelToLaunch = source.levelToLaunch,
                isProtected = false,
                children = CloneChildrenRecursive(source.children, 0)
            };
        }

        public static bool CopyItemsToClipboard(IEnumerable<ExplorerItemData> items, out int copiedCount, out int skippedProtectedCount)
        {
            copiedCount = 0;
            skippedProtectedCount = 0;
            if (items == null) return false;

            List<ExplorerItemData> cloned = new List<ExplorerItemData>();
            List<ExplorerItemData> sources = new List<ExplorerItemData>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (IsItemProtectedStatic(item))
                {
                    skippedProtectedCount++;
                    continue;
                }
                if (!sources.Contains(item))
                {
                    cloned.Add(CloneItemForClipboard(item));
                    sources.Add(item);
                }
            }

            copiedCount = cloned.Count;
            if (copiedCount == 0) return false;

            clipboardItems.Clear();
            clipboardSourceItems.Clear();
            clipboardItems.AddRange(cloned);
            clipboardSourceItems.AddRange(sources);
            IsClipboardCut = false;
            NotifyFileSystemChanged();
            return true;
        }

        public static bool CutItemsToClipboard(IEnumerable<ExplorerItemData> items, out int cutCount, out int skippedProtectedCount)
        {
            cutCount = 0;
            skippedProtectedCount = 0;
            if (items == null) return false;

            List<ExplorerItemData> cloned = new List<ExplorerItemData>();
            List<ExplorerItemData> sources = new List<ExplorerItemData>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (IsItemProtectedStatic(item))
                {
                    skippedProtectedCount++;
                    continue;
                }
                if (!sources.Contains(item))
                {
                    cloned.Add(CloneItemForClipboard(item));
                    sources.Add(item);
                }
            }

            cutCount = cloned.Count;
            if (cutCount == 0) return false;

            clipboardItems.Clear();
            clipboardSourceItems.Clear();
            clipboardItems.AddRange(cloned);
            clipboardSourceItems.AddRange(sources);
            IsClipboardCut = true;
            NotifyFileSystemChanged();
            return true;
        }

        public void CopySelectedItem() => CopySelectedItems();

        public void CopySelectedItems()
        {
            if (selectedItems.Count == 0) return;

            if (!CopyItemsToClipboard(selectedItems, out int copiedCount, out int skippedProtected))
            {
                SetStatusMessage("<color=#D9534F>System items are protected and cannot be copied.</color>");
                return;
            }

            string summary = copiedCount == 1
                ? $"Copied '{clipboardItems[0].name}' to clipboard."
                : $"Copied {copiedCount} items to clipboard.";
            if (skippedProtected > 0)
            {
                summary += $" ({skippedProtected} protected item{(skippedProtected == 1 ? "" : "s")} skipped)";
            }
            SetStatusMessage(summary);
            FeedbackManager.PlayClick();
        }

        public void CutSelectedItem() => CutSelectedItems();

        public void CutSelectedItems()
        {
            if (selectedItems.Count == 0) return;
            if (currentFolder != null && currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase))
            {
                SetStatusMessage("<color=#D9534F>Restore items from the Recycle Bin before cutting them.</color>");
                return;
            }

            if (!CutItemsToClipboard(selectedItems, out int cutCount, out int skippedProtected))
            {
                SetStatusMessage("<color=#D9534F>System items are protected and cannot be cut.</color>");
                return;
            }

            string summary = cutCount == 1
                ? $"Cut '{clipboardItems[0].name}' to clipboard."
                : $"Cut {cutCount} items to clipboard.";
            if (skippedProtected > 0)
            {
                summary += $" ({skippedProtected} protected item{(skippedProtected == 1 ? "" : "s")} skipped)";
            }
            SetStatusMessage(summary);
            FeedbackManager.PlayClick();
        }

        public static bool CutItemToClipboard(ExplorerItemData itemToCut)
        {
            if (itemToCut == null) return false;
            return CutItemsToClipboard(new[] { itemToCut }, out int cutCount, out _) && cutCount > 0;
        }

        public void PasteClipboardItem()
        {
            if (!HasClipboardItems || currentFolder == null) return;
            if (IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage("<color=#D9534F>Cannot paste items into this system folder.</color>");
                return;
            }

            List<ExplorerItemData> pasted = PasteClipboardBatchIntoFolder(currentFolder, out string errorMessage);
            if (pasted.Count > 0)
            {
                selectedItems.Clear();
                foreach (var p in pasted)
                {
                    if (p != null && currentFolder.children != null && currentFolder.children.Contains(p))
                    {
                        selectedItems.Add(p);
                    }
                }
                RefreshItemSelectionVisuals();

                string msg = pasted.Count == 1
                    ? $"Pasted '{pasted[0].name}'"
                    : $"Pasted {pasted.Count} items";
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    msg += $" ({errorMessage})";
                }
                SetStatusMessage(msg);
                FeedbackManager.PlaySuccess();
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                SetStatusMessage($"<color=#D9534F>{errorMessage}</color>");
            }
        }

        private static List<ExplorerItemData> CloneChildrenRecursive(List<ExplorerItemData> sourceChildren, int currentDepth = 0)
        {
            List<ExplorerItemData> clonedList = new List<ExplorerItemData>();
            if (sourceChildren == null || currentDepth > MAX_FOLDER_DEPTH) return clonedList;

            foreach (var child in sourceChildren)
            {
                if (child == null) continue;
                ExplorerItemData clonedChild = new ExplorerItemData
                {
                    name = child.name,
                    fileType = child.fileType,
                    icon = child.icon,
                    textContent = child.textContent,
                    imageContent = child.imageContent,
                    levelToLaunch = child.levelToLaunch,
                    isProtected = false,
                    children = CloneChildrenRecursive(child.children, currentDepth + 1)
                };
                clonedList.Add(clonedChild);
            }
            return clonedList;
        }

        public static bool IsFolderDescendantOf(ExplorerItemData potentialDescendant, ExplorerItemData ancestor, int depth = 0)
        {
            if (potentialDescendant == null || ancestor == null || depth > MAX_FOLDER_DEPTH + 4) return false;
            if (potentialDescendant == ancestor) return true;
            if (ancestor.children == null) return false;
            foreach (var child in ancestor.children)
            {
                if (child != null && child.fileType == ExplorerFileType.Folder)
                {
                    if (IsFolderDescendantOf(potentialDescendant, child, depth + 1)) return true;
                }
            }
            return false;
        }

        private static bool IsFolderOrDescendant(ExplorerItemData folderNode, ExplorerItemData candidateTarget)
        {
            return IsFolderDescendantOf(candidateTarget, folderNode);
        }

        public static int GetFolderDepth(ExplorerItemData targetFolder)
        {
            if (targetFolder == null || sharedRootFolder == null || targetFolder == sharedRootFolder) return 0;
            List<ExplorerItemData> chain = new List<ExplorerItemData>();
            if (FindFolderChainRecursive(sharedRootFolder, targetFolder, chain))
            {
                return Mathf.Max(0, chain.Count - 1);
            }
            return 0;
        }

        private static int GetSubtreeMaxDepth(ExplorerItemData node, int currentDepth = 0)
        {
            if (node == null || node.fileType != ExplorerFileType.Folder || node.children == null || currentDepth > MAX_FOLDER_DEPTH)
                return 0;
            int maxChild = 0;
            foreach (var child in node.children)
            {
                if (child != null && child.fileType == ExplorerFileType.Folder)
                {
                    int d = 1 + GetSubtreeMaxDepth(child, currentDepth + 1);
                    if (d > maxChild) maxChild = d;
                }
            }
            return maxChild;
        }

        public static ExplorerItemData PasteClipboardIntoFolder(ExplorerItemData targetFolder)
        {
            List<ExplorerItemData> pasted = PasteClipboardBatchIntoFolder(targetFolder, out _);
            return pasted.Count > 0 ? pasted[0] : null;
        }

        public static List<ExplorerItemData> PasteClipboardBatchIntoFolder(ExplorerItemData targetFolder, out string errorMessage)
        {
            errorMessage = null;
            List<ExplorerItemData> results = new List<ExplorerItemData>();
            if (!HasClipboardItems || targetFolder == null) return results;
            if (IsReadOnlySystemFolderStatic(targetFolder))
            {
                errorMessage = "Cannot paste items into this system folder.";
                return results;
            }

            if (targetFolder.children == null) targetFolder.children = new List<ExplorerItemData>();
            ExplorerItemData root = GetRootFolder();
            bool anyChange = false;
            bool wasCut = IsClipboardCut;

            for (int i = 0; i < clipboardItems.Count; i++)
            {
                ExplorerItemData clipItem = clipboardItems[i];
                ExplorerItemData srcItem = i < clipboardSourceItems.Count ? clipboardSourceItems[i] : null;
                if (clipItem == null || IsItemProtectedStatic(clipItem)) continue;

                if (clipItem.fileType == ExplorerFileType.Folder &&
                    (IsFolderDescendantOf(targetFolder, clipItem) || (srcItem != null && IsFolderDescendantOf(targetFolder, srcItem))))
                {
                    errorMessage = UIDialogs.Current.explorer_cycle_error;
                    continue;
                }

                bool alreadyInSameFolderOnCut = wasCut && srcItem != null && targetFolder.children.Contains(srcItem);
                if (!alreadyInSameFolderOnCut && targetFolder.children.Count >= MAX_ITEMS_PER_FOLDER)
                {
                    errorMessage = UIDialogs.Current.explorer_folder_limit_reached;
                    break;
                }

                if (clipItem.fileType == ExplorerFileType.Folder &&
                    GetFolderDepth(targetFolder) + 1 + GetSubtreeMaxDepth(clipItem) > MAX_FOLDER_DEPTH)
                {
                    errorMessage = UIDialogs.Current.explorer_depth_limit_reached;
                    continue;
                }

                string baseName = clipItem.name;
                string ext = System.IO.Path.GetExtension(baseName);
                string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(baseName);

                if (wasCut)
                {
                    if (alreadyInSameFolderOnCut)
                    {
                        results.Add(srcItem);
                        continue;
                    }

                    ExplorerItemData movedItem = srcItem ?? CloneItemForClipboard(clipItem);
                    if (root != null)
                    {
                        RemoveItemRecursive(root, movedItem);
                    }
                    movedItem.name = GetUniqueItemNameStatic(targetFolder, nameWithoutExt, ext);
                    movedItem.isProtected = false;
                    targetFolder.children.Add(movedItem);
                    results.Add(movedItem);
                    anyChange = true;
                }
                else
                {
                    bool copyingIntoSameFolder = srcItem != null && targetFolder.children.Contains(srcItem);
                    string desiredPrefix = copyingIntoSameFolder ? $"Copy of {nameWithoutExt}" : nameWithoutExt;
                    string copyName = GetUniqueItemNameStatic(targetFolder, desiredPrefix, ext);

                    ExplorerItemData duplicate = new ExplorerItemData
                    {
                        name = copyName,
                        fileType = clipItem.fileType,
                        icon = clipItem.icon,
                        textContent = clipItem.textContent,
                        imageContent = clipItem.imageContent,
                        isProtected = false,
                        children = CloneChildrenRecursive(clipItem.children, 0)
                    };

                    targetFolder.children.Add(duplicate);
                    results.Add(duplicate);
                    anyChange = true;
                }
            }

            if (wasCut && results.Count > 0)
            {
                IsClipboardCut = false;
                clipboardItems.Clear();
                clipboardSourceItems.Clear();
                anyChange = true;
            }
            else if (!wasCut && results.Count > 0)
            {
                RegisterCreatedItemsForUndo(results);
            }

            if (anyChange)
            {
                NotifyFileSystemChanged();
            }

            return results;
        }

        public static List<ExplorerItemData> DuplicateItemsInFolder(IEnumerable<ExplorerItemData> sourceItems, ExplorerItemData targetFolder, out int skippedProtectedCount, out string errorMessage)
        {
            skippedProtectedCount = 0;
            errorMessage = null;
            List<ExplorerItemData> results = new List<ExplorerItemData>();
            if (sourceItems == null || targetFolder == null) return results;
            if (IsReadOnlySystemFolderStatic(targetFolder))
            {
                errorMessage = "Cannot duplicate items in this system folder.";
                return results;
            }

            if (targetFolder.children == null) targetFolder.children = new List<ExplorerItemData>();
            List<ExplorerItemData> snapshot = new List<ExplorerItemData>();
            foreach (var s in sourceItems)
            {
                if (s != null && !snapshot.Contains(s)) snapshot.Add(s);
            }

            foreach (var srcItem in snapshot)
            {
                if (IsItemProtectedStatic(srcItem))
                {
                    skippedProtectedCount++;
                    continue;
                }

                if (targetFolder.children.Count >= MAX_ITEMS_PER_FOLDER)
                {
                    errorMessage = UIDialogs.Current.explorer_folder_limit_reached;
                    break;
                }

                if (srcItem.fileType == ExplorerFileType.Folder &&
                    GetFolderDepth(targetFolder) + 1 + GetSubtreeMaxDepth(srcItem) > MAX_FOLDER_DEPTH)
                {
                    errorMessage = UIDialogs.Current.explorer_depth_limit_reached;
                    continue;
                }

                string ext = System.IO.Path.GetExtension(srcItem.name);
                string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(srcItem.name);
                string desiredPrefix = $"Copy of {nameWithoutExt}";
                string copyName = GetUniqueItemNameStatic(targetFolder, desiredPrefix, ext);

                ExplorerItemData duplicate = new ExplorerItemData
                {
                    name = copyName,
                    fileType = srcItem.fileType,
                    icon = srcItem.icon,
                    textContent = srcItem.textContent,
                    imageContent = srcItem.imageContent,
                    isProtected = false,
                    children = CloneChildrenRecursive(srcItem.children, 0)
                };

                targetFolder.children.Add(duplicate);
                results.Add(duplicate);
            }

            if (results.Count > 0)
            {
                RegisterCreatedItemsForUndo(results);
                NotifyFileSystemChanged();
            }

            return results;
        }

        public void DuplicateSelectedItems()
        {
            if (selectedItems.Count == 0 || currentFolder == null) return;
            if (IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage("<color=#D9534F>Cannot duplicate items in this system folder.</color>");
                return;
            }

            List<ExplorerItemData> duplicated = DuplicateItemsInFolder(selectedItems, currentFolder, out int skippedProtected, out string errorMessage);
            if (duplicated.Count > 0)
            {
                selectedItems.Clear();
                foreach (var d in duplicated)
                {
                    if (d != null && currentFolder.children != null && currentFolder.children.Contains(d))
                    {
                        selectedItems.Add(d);
                    }
                }
                RefreshItemSelectionVisuals();

                string msg = duplicated.Count == 1
                    ? $"Duplicated '{duplicated[0].name}'"
                    : $"Duplicated {duplicated.Count} items";
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    msg += $" ({errorMessage})";
                }
                SetStatusMessage(msg);
                FeedbackManager.PlaySuccess();
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                SetStatusMessage($"<color=#D9534F>{errorMessage}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(errorMessage, transform as RectTransform, 3.2f);
            }
            else if (skippedProtected > 0)
            {
                SetStatusMessage("<color=#D9534F>System items are protected and cannot be duplicated.</color>");
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

            requestedName = ClampAndFormatItemName(requestedName, ".txt");

            if (targetFolder == null)
            {
                if (ActiveExplorerFolder != null)
                {
                    targetFolder = ActiveExplorerFolder;
                }
                else
                {
                    targetFolder = FindOrCreateSystemFolder("Documents", true);
                }
            }

            if (targetFolder == null) return null;
            if (targetFolder.children == null) targetFolder.children = new List<ExplorerItemData>();

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

            if (IsFolderAtCapacity(targetFolder))
            {
                BroadcastStatusError(UIDialogs.Current.explorer_folder_limit_reached);
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, null, 3.2f);
                return null;
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
            RegisterCreatedItemsForUndo(newDoc);
            NotifyFileSystemChanged();
            return newDoc;
        }

        public static ExplorerItemData SaveOrCreateImageDocument(string requestedName, Sprite imageSprite, ExplorerItemData targetFolder = null)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                requestedName = "New Drawing.png";

            requestedName = ClampAndFormatItemName(requestedName, ".png");

            if (targetFolder == null)
            {
                if (ActiveExplorerFolder != null)
                {
                    targetFolder = ActiveExplorerFolder;
                }
                else
                {
                    targetFolder = FindOrCreateSystemFolder("Pictures", true);
                }
            }

            if (targetFolder == null) return null;
            if (targetFolder.children == null) targetFolder.children = new List<ExplorerItemData>();

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

            if (IsFolderAtCapacity(targetFolder))
            {
                BroadcastStatusError(UIDialogs.Current.explorer_folder_limit_reached);
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, null, 3.2f);
                return null;
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
            RegisterCreatedItemsForUndo(newImg);
            NotifyFileSystemChanged();
            return newImg;
        }

        /// <summary>
        /// Moves an item from its current folder in the virtual file system into destinationFolder,
        /// and syncs with the Desktop if moved to or from 'Desktop'.
        /// </summary>
        public static bool MoveItemToFolder(ExplorerItemData item, ExplorerItemData destinationFolder, Vector2? desktopDropScreenPos = null)
        {
            if (item == null) return false;
            int movedCount = MoveItemsToFolder(new[] { item }, destinationFolder, out _, out string errorMessage, desktopDropScreenPos);
            if (movedCount == 0 && !string.IsNullOrEmpty(errorMessage))
            {
                BroadcastStatusError(errorMessage);
            }
            return movedCount > 0;
        }

        public static int MoveItemsToFolder(IEnumerable<ExplorerItemData> items, ExplorerItemData destinationFolder, out int skippedProtectedCount, out string errorMessage, Vector2? desktopDropScreenPos = null)
        {
            skippedProtectedCount = 0;
            errorMessage = null;
            if (items == null || destinationFolder == null) return 0;
            if (IsReadOnlySystemFolderStatic(destinationFolder))
            {
                errorMessage = "Cannot move items into this system folder.";
                return 0;
            }

            if (destinationFolder.children == null) destinationFolder.children = new List<ExplorerItemData>();
            ExplorerItemData root = GetRootFolder();
            List<ExplorerItemData> movedList = new List<ExplorerItemData>();

            foreach (var item in items)
            {
                if (item == null) continue;
                if (IsItemProtectedStatic(item))
                {
                    skippedProtectedCount++;
                    continue;
                }
                if (item == destinationFolder) continue;

                if (item.fileType == ExplorerFileType.Folder && IsFolderDescendantOf(destinationFolder, item))
                {
                    errorMessage = UIDialogs.Current.explorer_cycle_error;
                    continue;
                }

                if (destinationFolder.children.Contains(item)) continue;
                if (destinationFolder.children.Count >= MAX_ITEMS_PER_FOLDER)
                {
                    errorMessage = UIDialogs.Current.explorer_folder_limit_reached;
                    break;
                }

                if (item.fileType == ExplorerFileType.Folder &&
                    GetFolderDepth(destinationFolder) + 1 + GetSubtreeMaxDepth(item) > MAX_FOLDER_DEPTH)
                {
                    errorMessage = UIDialogs.Current.explorer_depth_limit_reached;
                    continue;
                }

                if (root != null)
                {
                    RemoveItemRecursive(root, item);
                }

                string ext = System.IO.Path.GetExtension(item.name);
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.name);
                item.name = GetUniqueItemNameStatic(destinationFolder, baseName, ext);

                destinationFolder.children.Add(item);
                movedList.Add(item);
            }

            if (movedList.Count > 0)
            {
                if (destinationFolder.name.Equals("Desktop", StringComparison.OrdinalIgnoreCase) &&
                    desktopDropScreenPos.HasValue && DesktopManager.Instance != null)
                {
                    foreach (var moved in movedList)
                    {
                        DesktopManager.Instance.SpawnIconForExplorerItem(moved, desktopDropScreenPos.Value);
                    }
                }
                NotifyFileSystemChanged();
            }

            return movedList.Count;
        }

        private static void BroadcastStatusError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return;
            foreach (var exp in activeExplorers)
            {
                if (exp != null) exp.SetStatusMessage($"<color=#D9534F>{errorMessage}</color>");
            }
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

        public static bool IsProtectedSystemFolderStatic(ExplorerItemData item)
        {
            if (item == null) return false;
            if (item.isProtected) return true;
            if (item.fileType != ExplorerFileType.Folder) return false;
            return item.name.Equals("This PC", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Desktop", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Documents", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Pictures", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Levels", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Minigames", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Mouse Levels", StringComparison.OrdinalIgnoreCase) ||
                   item.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MoveItemToRecycleBin(ExplorerItemData item)
        {
            if (item == null) return false;
            return MoveItemsToRecycleBin(new[] { item }, out _) > 0;
        }

        public static int MoveItemsToRecycleBin(IEnumerable<ExplorerItemData> items, out int skippedProtectedCount)
        {
            skippedProtectedCount = 0;
            if (items == null) return 0;

            ExplorerItemData root = GetRootFolder();
            if (root == null) return 0;

            ExplorerItemData recycleBin = FindFolderRecursiveStatic(root, "Recycle Bin");
            if (recycleBin == null)
            {
                recycleBin = FindOrCreateSystemFolder("Recycle Bin", true);
            }
            if (recycleBin == null) return 0;
            if (recycleBin.children == null) recycleBin.children = new List<ExplorerItemData>();

            int movedCount = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (IsItemProtectedStatic(item))
                {
                    skippedProtectedCount++;
                    continue;
                }
                if (recycleBin.children.Contains(item)) continue;

                RemoveItemRecursive(root, item);
                recycleBin.children.Add(item);
                movedCount++;
            }

            if (movedCount > 0)
            {
                NotifyFileSystemChanged();
            }

            return movedCount;
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

        private static bool RemoveItemRecursive(ExplorerItemData parent, ExplorerItemData itemToRemove, int depth = 0)
        {
            if (parent == null || parent.children == null || depth > MAX_FOLDER_DEPTH + 4) return false;
            if (parent.children.Remove(itemToRemove)) return true;

            foreach (var child in parent.children)
            {
                if (child != null && child.fileType == ExplorerFileType.Folder && RemoveItemRecursive(child, itemToRemove, depth + 1))
                {
                    return true;
                }
            }
            return false;
        }

        private static ExplorerItemData FindFolderRecursiveStatic(ExplorerItemData parent, string name, int depth = 0)
        {
            if (parent == null || parent.children == null || depth > MAX_FOLDER_DEPTH + 4) return null;
            foreach (var child in parent.children)
            {
                if (child != null && child.fileType == ExplorerFileType.Folder && child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            foreach (var child in parent.children)
            {
                if (child == null || child.fileType != ExplorerFileType.Folder) continue;
                ExplorerItemData found = FindFolderRecursiveStatic(child, name, depth + 1);
                if (found != null) return found;
            }
            return null;
        }
        #endregion

        #region Public Instance Methods
        public void ClaimFocus()
        {
            DraggableIcon.ClearSelection();
            ClearAllExplorerSelections(this);
            IsDesktopFocused = false;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null) parentWindow.BringToFront();
        }

        public void BeginMarqueeSelection(PointerEventData eventData)
        {
            if (eventData == null || itemsContainer == null) return;
            RectTransform viewport = itemsContainer.parent as RectTransform;
            if (viewport == null) return;

            ClaimFocus();
            DesktopManager.EnsureMarqueeSelectionBox("ExplorerMarqueeSelectionBox", viewport, ref marqueeBoxObject, ref marqueeBoxRect);
            if (marqueeBoxObject == null || marqueeBoxRect == null) return;

            ClearSelection();
            marqueeStartScreenPos = eventData.position;
            marqueeBoxRect.sizeDelta = Vector2.zero;
            marqueeBoxObject.SetActive(true);
            marqueeBoxRect.SetAsLastSibling();
            UpdateMarqueeSelection(eventData);
        }

        public void UpdateMarqueeSelection(PointerEventData eventData)
        {
            if (eventData == null || marqueeBoxObject == null || !marqueeBoxObject.activeSelf || marqueeBoxRect == null || itemsContainer == null)
                return;

            RectTransform viewport = marqueeBoxRect.parent as RectTransform;
            if (viewport == null) return;

            Canvas rootCanvas = viewport.GetComponentInParent<Canvas>();
            Camera cam = eventData.pressEventCamera;
            if (cam == null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, marqueeStartScreenPos, cam, out Vector2 startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, cam, out Vector2 currentLocal);

            Rect vpRect = viewport.rect;
            startLocal.x = Mathf.Clamp(startLocal.x, vpRect.xMin, vpRect.xMax);
            startLocal.y = Mathf.Clamp(startLocal.y, vpRect.yMin, vpRect.yMax);
            currentLocal.x = Mathf.Clamp(currentLocal.x, vpRect.xMin, vpRect.xMax);
            currentLocal.y = Mathf.Clamp(currentLocal.y, vpRect.yMin, vpRect.yMax);

            Vector2 min = Vector2.Min(startLocal, currentLocal);
            Vector2 max = Vector2.Max(startLocal, currentLocal);
            Vector2 size = max - min;

            marqueeBoxRect.anchorMin = viewport.pivot;
            marqueeBoxRect.anchorMax = viewport.pivot;
            marqueeBoxRect.pivot = new Vector2(0.5f, 0.5f);
            marqueeBoxRect.anchoredPosition = (min + max) * 0.5f;
            marqueeBoxRect.sizeDelta = size;
            marqueeBoxRect.SetAsLastSibling();

            Rect marqueeLocalRect = new Rect(min, size);
            Vector3[] corners = new Vector3[4];
            List<ExplorerItemData> hitItems = new List<ExplorerItemData>();

            if (size.sqrMagnitude > 16f)
            {
                for (int i = 0; i < itemsContainer.childCount; i++)
                {
                    Transform child = itemsContainer.GetChild(i);
                    if (child == null || !child.gameObject.activeInHierarchy) continue;

                    ExplorerItemPointerHandler handler = child.GetComponent<ExplorerItemPointerHandler>();
                    RectTransform itemRt = child as RectTransform;
                    if (handler == null || handler.ItemData == null || itemRt == null) continue;

                    itemRt.GetWorldCorners(corners);
                    Vector2 itemMin = new Vector2(float.MaxValue, float.MaxValue);
                    Vector2 itemMax = new Vector2(float.MinValue, float.MinValue);

                    for (int c = 0; c < 4; c++)
                    {
                        Vector3 localPt = viewport.InverseTransformPoint(corners[c]);
                        if (localPt.x < itemMin.x) itemMin.x = localPt.x;
                        if (localPt.y < itemMin.y) itemMin.y = localPt.y;
                        if (localPt.x > itemMax.x) itemMax.x = localPt.x;
                        if (localPt.y > itemMax.y) itemMax.y = localPt.y;
                    }

                    Rect itemLocalRect = new Rect(itemMin, itemMax - itemMin);
                    if (marqueeLocalRect.Overlaps(itemLocalRect, true))
                    {
                        hitItems.Add(handler.ItemData);
                    }
                }
            }

            SetMarqueeSelection(hitItems);
        }

        public void EndMarqueeSelection(PointerEventData eventData)
        {
            if (marqueeBoxObject != null)
            {
                marqueeBoxObject.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ClaimFocus();

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ClearSelection();
                ShowEmptySpaceContextMenu(eventData.position);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                ClearSelection();
            }
        }

        public ExplorerItemData CreateNewFolder(string customBaseName = "New Folder")
        {
            if (currentFolder == null || IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage("<color=#D9534F>Cannot create items in this system folder.</color>");
                return null;
            }

            ExplorerItemData target = currentFolder;
            if (target.children == null) target.children = new List<ExplorerItemData>();
            if (target.children.Count >= MAX_ITEMS_PER_FOLDER)
            {
                SetStatusMessage($"<color=#D9534F>{UIDialogs.Current.explorer_folder_limit_reached}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, transform as RectTransform, 3.2f);
                return null;
            }
            if (GetFolderDepth(target) + 1 > MAX_FOLDER_DEPTH)
            {
                SetStatusMessage($"<color=#D9534F>{UIDialogs.Current.explorer_depth_limit_reached}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_depth_limit_reached, transform as RectTransform, 3.2f);
                return null;
            }

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
            RegisterCreatedItemsForUndo(newFolder);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created folder '{uniqueName}'");
            if (!VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                PromptRenameItem(newFolder);
            }
            return newFolder;
        }

        public ExplorerItemData CreateNewTextDocument(string customBaseName = "New Document")
        {
            if (currentFolder == null || IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage("<color=#D9534F>Cannot create items in this system folder.</color>");
                return null;
            }

            ExplorerItemData target = currentFolder;
            if (target.children == null) target.children = new List<ExplorerItemData>();
            if (target.children.Count >= MAX_ITEMS_PER_FOLDER)
            {
                SetStatusMessage($"<color=#D9534F>{UIDialogs.Current.explorer_folder_limit_reached}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, transform as RectTransform, 3.2f);
                return null;
            }

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
            RegisterCreatedItemsForUndo(newDoc);
            NotifyFileSystemChanged();
            SetStatusMessage($"Created text file '{uniqueName}'");
            if (!VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                PromptRenameItem(newDoc);
            }
            return newDoc;
        }

        public ExplorerItemData CreateNewImageDocument(string customBaseName = "New Drawing")
        {
            if (currentFolder == null || IsReadOnlySystemFolderStatic(currentFolder))
            {
                SetStatusMessage("<color=#D9534F>Cannot create items in this system folder.</color>");
                return null;
            }

            ExplorerItemData target = currentFolder;
            if (target.children == null) target.children = new List<ExplorerItemData>();
            if (target.children.Count >= MAX_ITEMS_PER_FOLDER)
            {
                SetStatusMessage($"<color=#D9534F>{UIDialogs.Current.explorer_folder_limit_reached}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, transform as RectTransform, 3.2f);
                return null;
            }

            string uniqueName = GetUniqueItemName(target, customBaseName, ".png");
            ExplorerItemData newImg = SaveOrCreateImageDocument(uniqueName, null, target);
            SetStatusMessage($"Created image file '{uniqueName}'");
            if (newImg != null && !VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                PromptRenameItem(newImg);
            }
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
            DraggableIcon.ClearSelection();
            IsDesktopFocused = false;
            ClearSelection();

            bool isReadOnly = IsReadOnlySystemFolderStatic(currentFolder);
            if (btnNewFolderToolbar != null) btnNewFolderToolbar.gameObject.SetActive(!isReadOnly);
            if (btnNewImageToolbar != null) btnNewImageToolbar.gameObject.SetActive(!isReadOnly && !isSaveDialogMode);
            if (btnNewTextToolbar != null) btnNewTextToolbar.gameObject.SetActive(!isReadOnly);

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
            if (!IsItemDataSelected(item))
            {
                SelectItem(itemGo, item);
            }
            else
            {
                ClaimFocus();
            }

            if (ContextMenuController.Instance == null) return;

            var menuItems = new List<ContextMenuItem>();
            bool inRecycleBin = currentFolder != null && currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);

            if (selectedItems.Count > 1)
            {
                if (inRecycleBin)
                {
                    int n = selectedItems.Count;
                    menuItems.Add(new ContextMenuItem($"Restore ({n})", () => RestoreSelectedItemsFromRecycleBin(), true));
                    menuItems.Add(new ContextMenuItem($"Delete Permanently ({n})", () => DeleteSelectedItemsPermanently(), true));
                }
                else
                {
                    int movableCount = 0;
                    foreach (var sel in selectedItems)
                    {
                        if (sel != null && !IsItemProtectedStatic(sel)) movableCount++;
                    }
                    bool canModifyAny = movableCount > 0;
                    string countSuffix = movableCount == selectedItems.Count
                        ? $"({movableCount})"
                        : $"({movableCount} of {selectedItems.Count})";

                    menuItems.Add(new ContextMenuItem($"Copy {countSuffix}", CopySelectedItems, canModifyAny));
                    menuItems.Add(new ContextMenuItem($"Cut {countSuffix}", CutSelectedItems, canModifyAny));
                    menuItems.Add(new ContextMenuItem($"Delete {countSuffix}", DeleteSelectedItems, canModifyAny));
                }

                ContextMenuController.Instance.Show(screenPos, menuItems);
                return;
            }

            if (item.fileType == ExplorerFileType.ImageFile)
            {
                menuItems.Add(new ContextMenuItem("Open", () => OpenImageFile(item), true));
                menuItems.Add(new ContextMenuItem("Open in Paint", () => OpenImageInPaint(item), true));
                menuItems.Add(new ContextMenuItem("Set as Desktop Background", () => SetImageAsDesktopWallpaper(item), true));
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

                bool canModify = !IsItemProtectedStatic(item);
                menuItems.Add(new ContextMenuItem("Copy", CopySelectedItems, canModify));
                menuItems.Add(new ContextMenuItem("Cut", CutSelectedItems, canModify));
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
            bool isSystemReadOnlyFolder = IsReadOnlySystemFolderStatic(currentFolder);

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
                var newSubMenu = new List<ContextMenuItem>
                {
                    new ContextMenuItem("Folder", () => CreateNewFolder("New Folder"), true),
                    new ContextMenuItem("Text Document (.txt)", () => CreateNewTextDocument("New Document"), true),
                    new ContextMenuItem("Image (.png)", () => CreateNewImageDocument("New Drawing"), true)
                };
                menuItems.Add(new ContextMenuItem("New", newSubMenu, true));
                menuItems.Add(new ContextMenuItem("Paste", PasteClipboardItem, HasClipboardItems));
                menuItems.Add(new ContextMenuItem("Refresh", RefreshItemsView, true));
            }

            ContextMenuController.Instance.Show(screenPosition, menuItems);
        }

        public void PromptRenameItem(ExplorerItemData item)
        {
            if (item == null || item.isProtected || IsProtectedSystemFolderStatic(item))
            {
                SetStatusMessage("<color=#D9534F>System item is protected and cannot be renamed.</color>");
                return;
            }

            if (ContextMenuController.Instance == null) return;

            string title = item.fileType == ExplorerFileType.Folder ? "Rename Folder" : "Rename File";
            ContextMenuController.Instance.ShowInputModal(title, item.name, "Save", (newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;
                string reqExt = "";
                if (item.fileType == ExplorerFileType.TextFile) reqExt = ".txt";
                else if (item.fileType == ExplorerFileType.ImageFile)
                {
                    reqExt = newName.Trim().EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ".png";
                }
                newName = ClampAndFormatItemName(newName, reqExt);

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
            if (IsItemProtectedStatic(item))
            {
                SetStatusMessage($"<color=#D9534F>'{item.name}' is a system item and cannot be deleted.</color>");
                return;
            }

            if (MoveItemToRecycleBin(item))
            {
                selectedItems.Remove(item);
                SetStatusMessage($"Moved '{item.name}' to Recycle Bin.");
            }
        }

        public void DeleteSelectedItems()
        {
            if (selectedItems.Count == 0) return;

            bool inRecycleBin = currentFolder != null && currentFolder.name.Equals("Recycle Bin", StringComparison.OrdinalIgnoreCase);
            if (inRecycleBin)
            {
                SetStatusMessage("Right-click items inside the Recycle Bin to Restore or Delete Permanently.");
                return;
            }

            List<ExplorerItemData> snapshot = new List<ExplorerItemData>(selectedItems);
            int movedCount = MoveItemsToRecycleBin(snapshot, out int skippedProtected);

            foreach (var it in snapshot)
            {
                if (it != null && !IsItemProtectedStatic(it))
                {
                    selectedItems.Remove(it);
                }
            }
            RefreshItemSelectionVisuals();

            if (movedCount == 0 && skippedProtected > 0)
            {
                SetStatusMessage("<color=#D9534F>System items are protected and cannot be deleted.</color>");
            }
            else if (movedCount > 0)
            {
                string msg = movedCount == 1
                    ? $"Moved '{snapshot.Find(x => x != null && !IsItemProtectedStatic(x))?.name}' to Recycle Bin."
                    : $"Moved {movedCount} items to Recycle Bin.";
                if (skippedProtected > 0)
                {
                    msg += $" ({skippedProtected} protected item{(skippedProtected == 1 ? "" : "s")} skipped)";
                }
                SetStatusMessage(msg);
            }
        }

        private void RestoreSelectedItemsFromRecycleBin()
        {
            if (selectedItems.Count == 0 || currentFolder == null) return;
            List<ExplorerItemData> snapshot = new List<ExplorerItemData>(selectedItems);
            int restoredCount = 0;
            foreach (var item in snapshot)
            {
                if (item == null || !currentFolder.children.Contains(item)) continue;
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
                selectedItems.Remove(item);
                restoredCount++;
            }
            if (restoredCount > 0)
            {
                NotifyFileSystemChanged();
                SetStatusMessage($"Restored {restoredCount} item{(restoredCount == 1 ? "" : "s")}.");
            }
        }

        private void DeleteSelectedItemsPermanently()
        {
            if (selectedItems.Count == 0 || currentFolder == null) return;
            List<ExplorerItemData> snapshot = new List<ExplorerItemData>(selectedItems);
            int deletedCount = 0;
            foreach (var item in snapshot)
            {
                if (item != null && currentFolder.children.Remove(item))
                {
                    selectedItems.Remove(item);
                    deletedCount++;
                }
            }
            if (deletedCount > 0)
            {
                NotifyFileSystemChanged();
                SetStatusMessage($"Permanently deleted {deletedCount} item{(deletedCount == 1 ? "" : "s")}.");
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
                RestoreFileSystemSnapshotIfAvailable(sharedRootFolder);
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

                // Remove any legacy Minigames folder if present
                ExplorerItemData games = FindFolderRecursiveStatic(sharedRootFolder, "Minigames");
                if (games != null)
                {
                    RemoveItemRecursive(sharedRootFolder, games);
                }

                if (levels.children == null || levels.children.Count == 0)
                {
                    PopulateLevelsFolder(levels);
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
                            name = lvl.GetLocalizedDisplayName(),
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
                        child.name = child.levelToLaunch.GetLocalizedDisplayName();
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
                            name = def.GetLocalizedDisplayName(),
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

        private static bool FindPathRecursive(ExplorerItemData current, ExplorerItemData target, List<string> path, int depth = 0)
        {
            if (current == null || depth > MAX_FOLDER_DEPTH + 4) return false;
            path.Add(current.name);
            if (current == target) return true;

            if (current.children != null)
            {
                foreach (var child in current.children)
                {
                    if (child != null && FindPathRecursive(child, target, path, depth + 1))
                        return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        private static bool FindFolderChainRecursive(ExplorerItemData current, ExplorerItemData target, List<ExplorerItemData> chain, int depth = 0)
        {
            if (current == null || target == null || depth > MAX_FOLDER_DEPTH + 4) return false;
            chain.Add(current);
            if (current == target)
                return true;

            if (current.children != null)
            {
                foreach (var child in current.children)
                {
                    if (child != null && child.fileType == ExplorerFileType.Folder && FindFolderChainRecursive(child, target, chain, depth + 1))
                        return true;
                }
            }

            chain.RemoveAt(chain.Count - 1);
            return false;
        }

        private void RefreshIconsRecursive(ExplorerItemData node, int depth = 0)
        {
            if (node == null || depth > MAX_FOLDER_DEPTH + 4) return;

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
                    RefreshIconsRecursive(child, depth + 1);
                }
            }
        }

        public void ShowStatusNotice(string msg, bool isError = false)
        {
            if (isError)
            {
                SetStatusMessage($"<color=#D9534F>{msg}</color>");
            }
            else
            {
                SetStatusMessage(msg);
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

            GridLayoutGroup grid = itemsContainer.GetComponent<GridLayoutGroup>();
            if (grid != null && (grid.cellSize.x < 112f || grid.cellSize.y < 136f))
            {
                grid.cellSize = new Vector2(Mathf.Max(grid.cellSize.x, 112f), Mathf.Max(grid.cellSize.y, 136f));
            }

            for (int i = selectedItems.Count - 1; i >= 0; i--)
            {
                if (selectedItems[i] == null || currentFolder == null || currentFolder.children == null || !currentFolder.children.Contains(selectedItems[i]))
                {
                    selectedItems.RemoveAt(i);
                }
            }

            foreach (Transform child in itemsContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            int count = currentFolder != null && currentFolder.children != null ? currentFolder.children.Count : 0;
            UpdateStatusFooterCount();

            if (count == 0) return;

            foreach (var item in currentFolder.children)
            {
                CreateItemUI(item);
            }
        }

        private void UpdateStatusFooterCount()
        {
            if (statusFooterText == null) return;
            int count = currentFolder != null && currentFolder.children != null ? currentFolder.children.Count : 0;
            if (selectedItems.Count > 1)
            {
                statusFooterText.text = $"{count} item{(count == 1 ? "" : "s")} ({selectedItems.Count} selected)";
            }
            else
            {
                statusFooterText.text = $"{count} item{(count == 1 ? "" : "s")}";
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

                // Display medal badge on the bottom-right corner of the icon if level has an achieved medal
                if (levelMedal != LevelMedal.None)
                {
                    Transform existingBadge = go.transform.Find("MedalBadge");
                    if (existingBadge == null)
                    {
                        GameObject badgeObj = new GameObject("MedalBadge", typeof(RectTransform), typeof(Image));
                        badgeObj.transform.SetParent(go.transform, false);
                        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
                        badgeRect.anchorMin = new Vector2(0.5f, 1f);
                        badgeRect.anchorMax = new Vector2(0.5f, 1f);
                        badgeRect.pivot = new Vector2(0.5f, 0.5f);
                        badgeRect.sizeDelta = new Vector2(22, 22);
                        badgeRect.anchoredPosition = new Vector2(20, -54);

                        Image badgeImg = badgeObj.GetComponent<Image>();
                        badgeImg.sprite = MedalVisualUtility.GetMedalSprite(levelMedal);
                        badgeImg.preserveAspect = true;
                        badgeImg.raycastTarget = false;
                    }
                }
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            bool isCut = IsItemInCutClipboard(item);
            cg.alpha = !isUnlocked ? 0.6f : (isCut ? 0.48f : 1f);

            Image cardBg = go.GetComponent<Image>();
            if (cardBg != null)
            {
                cardBg.color = IsItemDataSelected(item) ? selectedItemColor : Color.clear;
            }

            if (label != null)
            {
                RectTransform labelRt = label.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    labelRt.anchorMin = new Vector2(0f, 1f);
                    labelRt.anchorMax = new Vector2(1f, 1f);
                    labelRt.pivot = new Vector2(0.5f, 1f);
                    labelRt.anchoredPosition = new Vector2(0f, -70f);
                    labelRt.sizeDelta = new Vector2(-6f, 36f);
                }

                label.fontSize = 13f;
                label.alignment = TextAlignmentOptions.Top;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.text = item.name;
                label.raycastTarget = false;

                // Dedicated fixed-position medal/status label below the 2-line level title
                Transform existingMedalTr = go.transform.Find("MedalLabel");
                TextMeshProUGUI medalLabel = existingMedalTr != null ? existingMedalTr.GetComponent<TextMeshProUGUI>() : null;
                if (medalLabel == null)
                {
                    GameObject medalObj = new GameObject("MedalLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                    medalObj.transform.SetParent(go.transform, false);
                    medalLabel = medalObj.GetComponent<TextMeshProUGUI>();
                    medalLabel.font = label.font;
                }

                RectTransform medalRt = medalLabel.GetComponent<RectTransform>();
                medalRt.anchorMin = new Vector2(0f, 1f);
                medalRt.anchorMax = new Vector2(1f, 1f);
                medalRt.pivot = new Vector2(0.5f, 1f);
                medalRt.anchoredPosition = new Vector2(0f, -108f);
                medalRt.sizeDelta = new Vector2(-6f, 20f);

                medalLabel.fontSize = 11f;
                medalLabel.fontStyle = FontStyles.Bold;
                medalLabel.alignment = TextAlignmentOptions.Top;
                medalLabel.textWrappingMode = TextWrappingModes.NoWrap;
                medalLabel.overflowMode = TextOverflowModes.Ellipsis;
                medalLabel.raycastTarget = false;

                if (!isUnlocked)
                {
                    medalLabel.gameObject.SetActive(true);
                    medalLabel.text = "<color=#777777>[Locked]</color>";
                }
                else if (levelMedal != LevelMedal.None)
                {
                    string hex = LevelScoreRecord.GetMedalColorHex(levelMedal);
                    string medalName = LevelScoreRecord.GetMedalName(levelMedal);
                    medalLabel.gameObject.SetActive(true);
                    medalLabel.text = $"<color={hex}>[{medalName}]</color>";
                }
                else
                {
                    medalLabel.text = string.Empty;
                    medalLabel.gameObject.SetActive(false);
                }
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

        public void SelectItem(GameObject itemGo, ExplorerItemData item, bool preserveIfAlreadySelected = false)
        {
            ClaimFocus();

            if (preserveIfAlreadySelected && item != null && selectedItems.Contains(item))
            {
                RefreshItemSelectionVisuals();
                return;
            }

            selectedItems.Clear();
            if (item != null)
            {
                selectedItems.Add(item);
            }
            RefreshItemSelectionVisuals();
            UpdateStatusFooterCount();

            if (isSaveDialogMode && item != null && item.fileType != ExplorerFileType.Folder)
            {
                if (saveDialogInputField != null)
                {
                    saveDialogInputField.text = item.name;
                }
            }
        }

        public void ToggleItemSelection(GameObject itemGo, ExplorerItemData item)
        {
            ClaimFocus();
            if (item == null) return;

            if (selectedItems.Contains(item))
            {
                selectedItems.Remove(item);
            }
            else
            {
                selectedItems.Add(item);
            }

            RefreshItemSelectionVisuals();
            UpdateStatusFooterCount();
        }

        public void SetMarqueeSelection(List<ExplorerItemData> items)
        {
            ClaimFocus();
            selectedItems.Clear();
            if (items != null)
            {
                foreach (var it in items)
                {
                    if (it != null && !selectedItems.Contains(it))
                    {
                        selectedItems.Add(it);
                    }
                }
            }
            RefreshItemSelectionVisuals();
            UpdateStatusFooterCount();
        }

        public void RefreshItemSelectionVisuals()
        {
            if (itemsContainer == null) return;
            for (int i = 0; i < itemsContainer.childCount; i++)
            {
                Transform child = itemsContainer.GetChild(i);
                if (child == null) continue;
                ExplorerItemPointerHandler handler = child.GetComponent<ExplorerItemPointerHandler>();
                Image img = child.GetComponent<Image>();
                if (img != null)
                {
                    bool sel = handler != null && IsItemDataSelected(handler.ItemData);
                    img.color = sel ? selectedItemColor : Color.clear;
                }
            }
        }

        public bool IsItemSelected(GameObject itemGo)
        {
            if (itemGo == null) return false;
            ExplorerItemPointerHandler handler = itemGo.GetComponent<ExplorerItemPointerHandler>();
            return handler != null && IsItemDataSelected(handler.ItemData);
        }

        public bool IsItemDataSelected(ExplorerItemData item)
        {
            return item != null && selectedItems.Contains(item);
        }

        public void ClearSelection()
        {
            selectedItems.Clear();
            RefreshItemSelectionVisuals();
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
            return GetUniqueItemNameStatic(folder, baseName, extension);
        }

        private static string GetUniqueItemNameStatic(ExplorerItemData folder, string baseName, string extension)
        {
            string ext = extension ?? string.Empty;
            string cleanBase = ClampBaseName(baseName, MAX_BASE_NAME_LENGTH);
            if (folder == null || folder.children == null) return cleanBase + ext;

            string candidate = cleanBase + ext;
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
                        string suffix = $" ({counter})";
                        string shortBase = ClampBaseName(cleanBase, Mathf.Max(3, MAX_BASE_NAME_LENGTH - suffix.Length));
                        candidate = $"{shortBase}{suffix}{ext}";
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
                    Sprite notepadIcon = GetNotepadAppSprite();
                    if (notepadIcon != null) win.setIcon(notepadIcon);
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
                Sprite paintIcon = GetPaintAppSprite();
                if (paintIcon != null) win.setIcon(paintIcon);
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
                    existingWin.Restore();
                    if (!existingWin.IsMaximized && !DesktopTour.IsTourRunning) existingWin.toggleMaximize();
                    existingWin.BringToFront();
                    return;
                }

                if (WindowManager.Instance != null)
                {
                    GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
                    string levelWinTitle = item.levelToLaunch != null ? item.levelToLaunch.GetLocalizedDisplayName() : item.name;
                    Window win = WindowManager.Instance.OpenWindow(winPrefab, null, levelWinTitle);
                    if (win != null)
                    {
                        openedWindows[key] = win;
                        if (!win.IsMaximized && !DesktopTour.IsTourRunning) win.toggleMaximize();
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

            Sprite mascotImgContent = null;
            if (VirtualMascot.Instance != null)
            {
                var vis = VirtualMascot.Instance.GetComponentInChildren<UnityEngine.UI.Image>();
                if (vis != null && vis.sprite != null) mascotImgContent = vis.sprite;
            }
            if (mascotImgContent == null)
            {
                mascotImgContent = Resources.Load<Sprite>("cursor") ?? Resources.Load<Sprite>("RecyclingBin_Icon_01");
            }
#if UNITY_EDITOR
            if (mascotImgContent == null)
            {
                mascotImgContent = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/cursor.png") ??
                                   UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Computer Presentation.png");
            }
#endif
            if (mascotImgContent == null) mascotImgContent = imgSprite;

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

            // 3. Folder Levels inside Desktop!
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
                            name = lvl.GetLocalizedDisplayName(),
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

            sharedRootFolder.children.Add(desktopFolder);
            sharedRootFolder.children.Add(docsFolder);
            sharedRootFolder.children.Add(imagesFolder);
            sharedRootFolder.children.Add(recycleFolder);
            RestoreFileSystemSnapshotIfAvailable(sharedRootFolder);
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
                    saveDialogInputField.characterLimit = MAX_ITEM_NAME_LENGTH;
                    saveDialogInputField.text = ClampAndFormatItemName(initialFileName ?? string.Empty, saveDialogExtension ?? string.Empty);
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

            enteredName = ClampAndFormatItemName(enteredName, saveDialogExtension ?? "");

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

            bool isOverwritingExisting = false;
            if (currentFolder.children != null)
            {
                foreach (var child in currentFolder.children)
                {
                    if (child != null && child.name.Equals(enteredName, StringComparison.OrdinalIgnoreCase))
                    {
                        isOverwritingExisting = true;
                        break;
                    }
                }
            }

            if (!isOverwritingExisting && IsFolderAtCapacity(currentFolder))
            {
                SetStatusMessage($"<color=#D9534F>{UIDialogs.Current.explorer_folder_limit_reached}</color>");
                FeedbackManager.PlayError(true);
                VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, transform as RectTransform, 3.2f);
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
