/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Captures right clicks on the desktop canvas surface to open the multi-context Menu
 *              (New Folder, New Text Document, New Paint Image, Empty Recycle Bin, etc.).
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DesktopContextMenuTrigger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public static bool RestrictToFolderCreationOnly { get; set; } = false;

        #region Private Variables
        [Header("Desktop Creation Configuration")]
        [SerializeField] private Sprite folderSprite;
        [SerializeField] private Sprite textDocumentSprite;
        [SerializeField] private Sprite imageDocumentSprite;
        [SerializeField] private GameObject fileExplorerContentPrefab;
        [SerializeField] private GameObject notepadContentPrefab;
        [SerializeField] private GameObject paintContentPrefab;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (folderSprite == null)
            {
                folderSprite = FileExplorerApp.GetFolderSprite();
            }
            if (textDocumentSprite == null)
            {
                textDocumentSprite = FileExplorerApp.GetTextFileSprite();
            }
            if (imageDocumentSprite == null)
            {
                imageDocumentSprite = FileExplorerApp.GetImageFileSprite();
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                DraggableIcon.ClearSelection();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                DraggableIcon.ClearSelection();
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                DraggableIcon.ClearSelection();
                if (ContextMenuController.Instance != null)
                {
                    int binCount = FileExplorerApp.GetRecycleBinCount();
                    var menuItems = new List<ContextMenuItem>();

                    if (RestrictToFolderCreationOnly)
                    {
                        menuItems.Add(new ContextMenuItem("+ New Text Document (.txt)", CreateTextDocumentOnDesktop, true));
                    }
                    else
                    {
                        var newSubMenu = new List<ContextMenuItem>
                        {
                            new ContextMenuItem("Folder", CreateFolderOnDesktop, true),
                            new ContextMenuItem("Text Document (.txt)", CreateTextDocumentOnDesktop, true),
                            new ContextMenuItem("Image (.png)", CreateImageDocumentOnDesktop, true)
                        };
                        menuItems.Add(new ContextMenuItem("New", newSubMenu, true));

                        var openAppSubMenu = new List<ContextMenuItem>
                        {
                            new ContextMenuItem("File Explorer", () =>
                            {
                                if (StartMenuController.Instance != null) StartMenuController.Instance.OpenFileExplorer();
                            }, StartMenuController.Instance != null),
                            new ContextMenuItem("Notepad", () =>
                            {
                                if (StartMenuController.Instance != null) StartMenuController.Instance.OpenNotepad();
                            }, StartMenuController.Instance != null),
                            new ContextMenuItem("Paint", () =>
                            {
                                if (StartMenuController.Instance != null) StartMenuController.Instance.OpenPaint();
                            }, StartMenuController.Instance != null)
                        };
                        menuItems.Add(new ContextMenuItem("Open App", openAppSubMenu, StartMenuController.Instance != null));

                        var wallpaperSubMenu = new List<ContextMenuItem>
                        {
                            new ContextMenuItem("Default", () => DesktopManager.Instance?.SetDesktopWallpaperTheme("default"), true),
                            new ContextMenuItem("Classic Blue", () => DesktopManager.Instance?.SetDesktopWallpaperTheme("blue"), true),
                            new ContextMenuItem("Modern Teal", () => DesktopManager.Instance?.SetDesktopWallpaperTheme("teal"), true),
                            new ContextMenuItem("Retro Purple", () => DesktopManager.Instance?.SetDesktopWallpaperTheme("purple"), true)
                        };
                        menuItems.Add(new ContextMenuItem("Wallpaper", wallpaperSubMenu, true));

                        menuItems.Add(new ContextMenuItem("Paste", () =>
                        {
                            FileExplorerApp.PasteClipboardIntoFolder(FileExplorerApp.GetDesktopFolder());
                        }, FileExplorerApp.SharedClipboardItem != null));

                        menuItems.Add(new ContextMenuItem($"Empty Recycle Bin ({binCount})", () =>
                        {
                            FileExplorerApp.EmptyRecycleBin();
                        }, binCount > 0));
                    }

                    ContextMenuController.Instance.Show(eventData.position, menuItems);
                }
            }
        }

        public void CreateFolderOnDesktop()
        {
            DesktopManager desktopMgr = DesktopManager.Instance;
            if (desktopMgr == null)
            {
                Debug.LogWarning("[DesktopContextMenuTrigger] DesktopManager instance not found.");
                return;
            }

            string folderName = GetUniqueDesktopItemName(desktopMgr, "New Folder", "");

            Sprite iconToUse = folderSprite != null ? folderSprite : FileExplorerApp.GetFolderSprite();
            if (iconToUse == null)
            {
                foreach (var icon in desktopMgr.ActiveIcons)
                {
                    if (icon != null && (icon.name.Contains("Files") || icon.name.Contains("Folder")))
                    {
                        var img = icon.GetComponent<UnityEngine.UI.Image>();
                        if (img != null && img.sprite != null) { iconToUse = img.sprite; break; }
                    }
                }
            }

            GameObject explorerPrefabToUse = fileExplorerContentPrefab;
            if (explorerPrefabToUse == null && StartMenuController.Instance != null)
            {
                explorerPrefabToUse = StartMenuController.Instance.FileExplorerPrefab;
            }
            if (explorerPrefabToUse == null)
            {
                explorerPrefabToUse = Resources.Load<GameObject>("FileExplorerContent");
            }

            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
            ExplorerItemData linkedFolder = new ExplorerItemData
            {
                name = folderName,
                fileType = ExplorerFileType.Folder,
                icon = iconToUse,
                isProtected = false,
                children = new List<ExplorerItemData>()
            };

            if (desktopFolder != null)
            {
                desktopFolder.children.Add(linkedFolder);
            }

            DesktopIconData newIconData = new DesktopIconData
            {
                iconName = folderName,
                itemType = DesktopItemType.Folder,
                iconSprite = iconToUse,
                contentPrefab = explorerPrefabToUse,
                levelDefinition = null,
                isLocked = false,
                isUserCreated = true,
                linkedExplorerItem = linkedFolder
            };

            desktopMgr.SpawnIcon(newIconData);
            FileExplorerApp.NotifyFileSystemChanged();
            if (!VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                DraggableIcon di = desktopMgr.GetIconByName(folderName);
                di?.PromptRenameDesktopIcon();
            }
            Debug.Log($"[DesktopContextMenuTrigger] Created new desktop folder: {folderName}");
        }

        public void CreateTextDocumentOnDesktop()
        {
            DesktopManager desktopMgr = DesktopManager.Instance;
            if (desktopMgr == null)
            {
                Debug.LogWarning("[DesktopContextMenuTrigger] DesktopManager instance not found.");
                return;
            }

            string docName = GetUniqueDesktopItemName(desktopMgr, "New Document", ".txt");

            Sprite iconToUse = FileExplorerApp.GetTextFileSprite();
            if (iconToUse == null) iconToUse = textDocumentSprite;

            GameObject notepadPrefabToUse = notepadContentPrefab;
            if (notepadPrefabToUse == null && StartMenuController.Instance != null)
            {
                notepadPrefabToUse = StartMenuController.Instance.NotepadPrefab;
            }

            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
            ExplorerItemData linkedDoc = FileExplorerApp.SaveOrCreateTextDocument(docName, "", desktopFolder);

            if (desktopMgr.GetIconByName(docName) == null)
            {
                DesktopIconData newIconData = new DesktopIconData
                {
                    iconName = docName,
                    itemType = DesktopItemType.TextDocument,
                    iconSprite = iconToUse,
                    contentPrefab = notepadPrefabToUse,
                    levelDefinition = null,
                    isLocked = false,
                    isUserCreated = true,
                    linkedExplorerItem = linkedDoc
                };

                desktopMgr.SpawnIcon(newIconData);
            }

            FileExplorerApp.NotifyFileSystemChanged();
            if (!VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                DraggableIcon di = desktopMgr.GetIconByName(docName);
                di?.PromptRenameDesktopIcon();
            }
            Debug.Log($"[DesktopContextMenuTrigger] Created new desktop text document: {docName}");
        }

        public void CreateImageDocumentOnDesktop()
        {
            DesktopManager desktopMgr = DesktopManager.Instance;
            if (desktopMgr == null)
            {
                Debug.LogWarning("[DesktopContextMenuTrigger] DesktopManager instance not found.");
                return;
            }

            string imgName = GetUniqueDesktopItemName(desktopMgr, "New Drawing", ".png");

            Sprite iconToUse = FileExplorerApp.GetImageFileSprite();
            if (iconToUse == null) iconToUse = imageDocumentSprite;

            GameObject paintPrefabToUse = paintContentPrefab;
            if (paintPrefabToUse == null && StartMenuController.Instance != null)
            {
                paintPrefabToUse = StartMenuController.Instance.PaintPrefab;
            }

            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();
            ExplorerItemData linkedImg = FileExplorerApp.SaveOrCreateImageDocument(imgName, null, desktopFolder);

            if (desktopMgr.GetIconByName(imgName) == null)
            {
                DesktopIconData newIconData = new DesktopIconData
                {
                    iconName = imgName,
                    itemType = DesktopItemType.ImageDocument,
                    iconSprite = iconToUse,
                    contentPrefab = paintPrefabToUse,
                    levelDefinition = null,
                    isLocked = false,
                    isUserCreated = true,
                    linkedExplorerItem = linkedImg
                };

                desktopMgr.SpawnIcon(newIconData);
            }

            FileExplorerApp.NotifyFileSystemChanged();
            if (!VirtualDesktopGuidedLevels.IsLevelRunning)
            {
                DraggableIcon di = desktopMgr.GetIconByName(imgName);
                di?.PromptRenameDesktopIcon();
            }
            Debug.Log($"[DesktopContextMenuTrigger] Created new desktop image (.png): {imgName}");
        }
        #endregion

        #region Private Methods
        private string GetUniqueDesktopItemName(DesktopManager mgr, string baseName, string extension)
        {
            int count = 0;
            string candidate = baseName + extension;

            ExplorerItemData desktopFolder = FileExplorerApp.GetDesktopFolder();

            bool exists = true;
            while (exists)
            {
                exists = false;
                foreach (var icon in mgr.ActiveIcons)
                {
                    if (icon != null && icon.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists && desktopFolder != null && desktopFolder.children != null)
                {
                    foreach (var child in desktopFolder.children)
                    {
                        if (child != null && child.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (exists)
                {
                    count++;
                    candidate = $"{baseName} ({count}){extension}";
                }
            }

            return candidate;
        }
        #endregion
    }
}
