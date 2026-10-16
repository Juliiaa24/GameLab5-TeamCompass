/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Captures right clicks on the desktop canvas surface to open the multi-context Menu
 *              (New Folder, New Text Document, Empty Recycle Bin, etc.).
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DesktopContextMenuTrigger : MonoBehaviour, IPointerClickHandler
    {
        #region Private Variables
        [Header("Desktop Creation Configuration")]
        [SerializeField] private Sprite folderSprite;
        [SerializeField] private Sprite textDocumentSprite;
        [SerializeField] private GameObject fileExplorerContentPrefab;
        [SerializeField] private GameObject notepadContentPrefab;
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
        }
        #endregion

        #region Public Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (ContextMenuController.Instance != null)
                {
                    int binCount = FileExplorerApp.GetRecycleBinCount();
                    var menuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem("+ Nueva Carpeta", CreateFolderOnDesktop, true),
                        new ContextMenuItem("+ Nuevo Documento de Texto", CreateTextDocumentOnDesktop, true),
                        new ContextMenuItem("Abrir Explorador de Archivos", () =>
                        {
                            if (StartMenuController.Instance != null) StartMenuController.Instance.OpenFileExplorer();
                        }, StartMenuController.Instance != null),
                        new ContextMenuItem("Abrir Bloc de Notas", () =>
                        {
                            if (StartMenuController.Instance != null) StartMenuController.Instance.OpenNotepad();
                        }, StartMenuController.Instance != null),
                        new ContextMenuItem($"Vaciar Papelera de Reciclaje ({binCount})", () =>
                        {
                            FileExplorerApp.EmptyRecycleBin();
                        }, binCount > 0)
                    };

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
                    if (icon != null && (icon.name.Contains("Files") || icon.name.Contains("Folder") || icon.name.Contains("Archivos")))
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

            // Also register the folder in the persistent FileExplorerApp hierarchy so it persists and can hold files
            ExplorerItemData linkedFolder = FileExplorerApp.FindOrCreateSystemFolder(folderName, isProtectedFolder: false);

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
#if UNITY_EDITOR
            if (notepadPrefabToUse == null)
            {
                notepadPrefabToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/NotepadContent.prefab");
            }
#endif

            ExplorerItemData docsFolder = FileExplorerApp.FindOrCreateSystemFolder("Documents", true);
            ExplorerItemData linkedDoc = FileExplorerApp.SaveOrCreateTextDocument(docName, "", docsFolder);

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
            Debug.Log($"[DesktopContextMenuTrigger] Created new desktop text document: {docName}");
        }
        #endregion

        #region Private Methods
        private string GetUniqueDesktopItemName(DesktopManager mgr, string baseName, string extension)
        {
            int count = 0;
            string candidate = baseName + extension;

            bool exists = true;
            while (exists)
            {
                exists = false;
                foreach (var icon in mgr.ActiveIcons)
                {
                    if (icon != null && icon.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        candidate = $"{baseName} ({count}){extension}";
                        exists = true;
                        break;
                    }
                }
            }

            return candidate;
        }
        #endregion
    }
}
