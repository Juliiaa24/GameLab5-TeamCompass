/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Captures right clicks on the desktop canvas surface to open the Context Menu.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DesktopContextMenuTrigger : MonoBehaviour, IPointerClickHandler
    {
        #region Public Variables
        #endregion

        #region Private Variables
        [Header("Folder Configuration")]
        [SerializeField] private Sprite folderSprite;
        [SerializeField] private GameObject fileExplorerContentPrefab;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (folderSprite == null)
            {
                folderSprite = Resources.Load<Sprite>("Folder_Icon01");
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
                    ContextMenuController.Instance.Show(eventData.position, CreateFolderOnDesktop);
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

            // Generate unique folder name
            string folderName = GetUniqueDesktopFolderName(desktopMgr);

            Sprite iconToUse = folderSprite;
            if (iconToUse == null)
            {
                // Fallback to active icons if available
                foreach (var icon in desktopMgr.ActiveIcons)
                {
                    if (icon.name.Contains("Files") || icon.name.Contains("Folder") || icon.name.Contains("Archivos"))
                    {
                        var img = icon.GetComponent<UnityEngine.UI.Image>();
                        if (img != null && img.sprite != null) { iconToUse = img.sprite; break; }
                    }
                }
            }

            GameObject explorerPrefabToUse = fileExplorerContentPrefab;
            if (explorerPrefabToUse == null)
            {
                // Look for FileExplorerContent prefab in Resources or find by type
                explorerPrefabToUse = Resources.Load<GameObject>("FileExplorerContent");
            }

            DesktopIconData newIconData = new DesktopIconData
            {
                iconName = folderName,
                itemType = DesktopItemType.Folder,
                iconSprite = iconToUse,
                contentPrefab = explorerPrefabToUse,
                levelDefinition = null,
                isLocked = false
            };

            desktopMgr.SpawnIcon(newIconData);
            Debug.Log($"[DesktopContextMenuTrigger] Created new desktop folder: {folderName}");
        }
        #endregion

        #region Private Methods
        private string GetUniqueDesktopFolderName(DesktopManager mgr)
        {
            string baseName = "New Folder";
            int count = 0;
            string candidate = baseName;

            bool exists = true;
            while (exists)
            {
                exists = false;
                foreach (var icon in mgr.ActiveIcons)
                {
                    if (icon != null && icon.name == candidate)
                    {
                        count++;
                        candidate = $"{baseName} ({count})";
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
