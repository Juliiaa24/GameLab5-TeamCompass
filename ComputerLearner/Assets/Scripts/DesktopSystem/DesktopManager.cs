using UnityEngine;
using System.Collections.Generic;

namespace ComputerLearning
{
    /// <summary>
    /// Type of items that can be spawned on the desktop.
    /// Used for future scalability (folders, minigames, docs, etc.).
    /// </summary>
    public enum DesktopItemType
    {
        Application,
        Folder,
        TextDocument,
        Minigame
    }

    /// <summary>
    /// Configuration data for creating a desktop icon.
    /// Can be expanded or turned into a ScriptableObject later.
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
    }

    /// <summary>
    /// Central manager for the virtual desktop environment.
    /// Handles spawning dynamic elements like the Virtual Mascot and Desktop Icons.
    /// </summary>
    public class DesktopManager : MonoBehaviour
    {
        #region Public Variables
        public static DesktopManager Instance { get; private set; }
        #endregion

        #region Private Component References
        [Header("Window Configuration")]
        [SerializeField] private GameObject baseWindowPrefab; // Used for all icons

        [Header("Mascot Configuration")]
        [SerializeField] private GameObject mascotPrefab;
        [SerializeField] private RectTransform mascotContainer; // Usually the main Desktop Canvas

        [Header("Icon Configuration")]
        [SerializeField] private RectTransform desktopIconContainer; // E.g., a GridLayoutGroup for icons
        [SerializeField] private GameObject defaultIconPrefab; // Fallback icon prefab
        
        [Header("Initial Desktop Icons")]
        [Tooltip("List of icons to spawn automatically on start (or via test method)")]
        [SerializeField] private List<DesktopIconData> initialDesktopIcons = new List<DesktopIconData>();
        #endregion

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
        #endregion

        #region Private Methods
        /// <summary>
        /// Bootstraps the desktop environment elements.
        /// </summary>
        private void InitializeDesktop()
        {
            SpawnVirtualMascot();
        }

        /// <summary>
        /// Instantiates the virtual mascot and keeps it hidden until needed.
        /// </summary>
        private void SpawnVirtualMascot()
        {
            if (mascotPrefab == null)
            {
                Debug.LogWarning("[DesktopManager] Mascot Prefab is not assigned in the Inspector.");
                return;
            }

            Transform container = mascotContainer != null ? mascotContainer : transform;
            
            // Instantiate and disable
            GameObject mascotInstance = Instantiate(mascotPrefab, container);
            mascotInstance.name = "VirtualMascot";
            mascotInstance.SetActive(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Spawns a new icon on the desktop grid.
        /// Scalable design for future item types.
        /// </summary>
        /// <param name="iconData">Data containing type, name, and prefab for the icon.</param>
        public GameObject SpawnIcon(DesktopIconData iconData)
        {
            if (desktopIconContainer == null)
            {
                Debug.LogError("[DesktopManager] Desktop Icon Container is missing!");
                return null;
            }

            if (defaultIconPrefab == null)
            {
                Debug.LogError($"[DesktopManager] Default Icon Prefab is missing in DesktopManager!");
                return null;
            }

            // Instantiate into the grid/layout container
            GameObject iconInstance = Instantiate(defaultIconPrefab, desktopIconContainer);
            iconInstance.name = iconData.iconName;

            // Update the icon image if a sprite was provided
            if (iconData.iconSprite != null)
            {
                UnityEngine.UI.Image img = iconInstance.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = iconData.iconSprite;
                }
            }

            // Update the text label if the prefab has one
            TMPro.TextMeshProUGUI textComponent = iconInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = iconData.iconName;
            }

            // Inject the window data into the DraggableIcon component
            DraggableIcon draggable = iconInstance.GetComponent<DraggableIcon>();
            if (draggable != null)
            {
                if (baseWindowPrefab == null) Debug.LogWarning("[DesktopManager] Base Window Prefab is not assigned! Icon might not open a window.");
                draggable.SetupDynamicIcon(baseWindowPrefab, iconData.contentPrefab, iconData.levelDefinition, iconData.isLocked);
            }

            return iconInstance;
        }

        [ContextMenu("Spawn Initial Icons (Test)")]
        private void SpawnInitialIcons()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please enter Play Mode to test spawning icons.");
                return;
            }

            if (initialDesktopIcons == null || initialDesktopIcons.Count == 0)
            {
                Debug.LogWarning("No initial icons configured in DesktopManager. Please add them in the Inspector.");
                return;
            }

            foreach (var iconData in initialDesktopIcons)
            {
                SpawnIcon(iconData);
            }
            
            Debug.Log($"Spawned {initialDesktopIcons.Count} icons on the desktop!");
        }
        #endregion
    }
}
