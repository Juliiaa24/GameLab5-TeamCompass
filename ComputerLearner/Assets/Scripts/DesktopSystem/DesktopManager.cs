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

        // Tracks all currently spawned icons
        private List<DraggableIcon> activeIcons = new List<DraggableIcon>();
        #endregion

        public IReadOnlyList<DraggableIcon> ActiveIcons => activeIcons;

        /// <summary>
        /// Gets an icon by its LevelDef ID (e.g., "level1", "level2")
        /// </summary>
        public DraggableIcon GetIconByLevelId(string levelId)
        {
            foreach (var icon in activeIcons)
            {
                if (icon.LevelDef != null && icon.LevelDef.levelId == levelId) return icon;
            }
            return null;
        }

        /// <summary>
        /// Gets an icon by its visible item name (e.g., "Level 1", "Paint")
        /// </summary>
        public DraggableIcon GetIconByName(string itemName)
        {
            foreach (var icon in activeIcons)
            {
                if (icon.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text == itemName) return icon;
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

            DraggableIcon draggable = iconInstance.GetComponent<DraggableIcon>();
            if (draggable != null)
            {
                if (!activeIcons.Contains(draggable)) activeIcons.Add(draggable);
            }

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
            if (draggable != null)
            {
                if (baseWindowPrefab == null) Debug.LogWarning("[DesktopManager] Base Window Prefab is not assigned! Icon might not open a window.");
                draggable.SetupDynamicIcon(baseWindowPrefab, iconData.contentPrefab, iconData.levelDefinition, iconData.isLocked);
            }

            return iconInstance;
        }

        private void Start()
        {
            if (PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 0)
            {
                StartCoroutine(AutoOnboardingSequence());
            }
            else
            {
                SpawnInitialIcons();
            }
        }

        private System.Collections.IEnumerator AutoOnboardingSequence()
        {
            Debug.Log("[DesktopManager] Starting AutoOnboardingSequence.");
            yield return new WaitForSeconds(1f); // wait for UI to settle

            // Ensure the initial icons are spawned
            List<GameObject> spawnedIcons = SpawnInitialIcons();
            
            // Create a tutorial blocker to prevent early clicks during onboarding
            GameObject blockerObj = new GameObject("OnboardingBlocker");
            TutorialBlocker blocker = blockerObj.AddComponent<TutorialBlocker>();
            blocker.SetAllowedTarget(null); // Block EVERYTHING initially

            // Expected sequence of LevelDefinitions
            string[] levelSequence = { "level1", "level2", "level3", "level4" };
            int foundIconsCount = 0;

            foreach (string targetLevelId in levelSequence)
            {
                DraggableIcon targetIcon = null;
                foreach (var iconObj in spawnedIcons)
                {
                    DraggableIcon draggable = iconObj.GetComponent<DraggableIcon>();
                    if (draggable != null && draggable.LevelDef != null && draggable.LevelDef.levelId == targetLevelId)
                    {
                        targetIcon = draggable;
                        break;
                    }
                }

                if (targetIcon != null)
                {
                    foundIconsCount++;
                    Debug.Log($"[DesktopManager] Icon {targetLevelId} found. Forcing unlock.");
                    
                    // Force unlock so OpenApplication doesn't block it
                    if (ProgressData.Instance != null)
                    {
                        ProgressData.Instance.UnlockLevel(targetLevelId);
                    }
                    
                    // Force the DraggableIcon itself to visually update and unlock!
                    targetIcon.GetType().GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(targetIcon, false);

                    // Make the mascot lively when introducing the next app
                    string introMessage = $"Let's open {targetIcon.LevelDef.displayName}!";
                    if (targetLevelId.Contains("1")) introMessage = "Let's start by opening our very first application! Watch this!";
                    else if (targetLevelId.Contains("2")) introMessage = "Great! Now let's try the next one. It's a bit faster!";
                    else if (targetLevelId.Contains("3")) introMessage = "Fantastic! Time for a new challenge. Let's open it!";
                    else if (targetLevelId.Contains("4")) introMessage = "You're unstoppable! One last practice app. Here we go!";

                    VirtualMascot.Show(introMessage, targetIcon.GetComponent<RectTransform>(), new Vector2(160, -80));
                    yield return new WaitForSeconds(5.0f);

                    VirtualMascot.HideMascot();
                    Debug.Log($"[DesktopManager] Opening application for {targetLevelId}...");
                    targetIcon.OpenApplication();

                    if (targetIcon.AppWindow == null)
                    {
                        Debug.LogError($"[DesktopManager] AppWindow is NULL after OpenApplication for {targetLevelId}! The level is skipping!");
                    }

                    // Wait for window to actually open
                    while (targetIcon.AppWindow == null || targetIcon.AppWindow.IsClosed)
                    {
                        yield return null;
                    }

                    // Allow interactions ONLY inside the application window content area
                    blocker.SetAllowedTarget(targetIcon.AppWindow.ContentArea);

                    bool movedToNext = false;
                    int currentIndex = System.Array.IndexOf(levelSequence, targetLevelId);
                    
                    while (!movedToNext)
                    {
                        // Wait for the window to be closed (which is now automated by LevelRunner)
                        while (targetIcon.AppWindow != null && !targetIcon.AppWindow.IsClosed)
                        {
                            yield return null;
                        }

                        // Block everything again while transitioning
                        blocker.SetAllowedTarget(null);

                        // Give one frame for any OnDestroy / ProgressData updates to settle
                        yield return null;

                        // Check again
                        if (currentIndex < levelSequence.Length - 1)
                        {
                            string nextLevelId = levelSequence[currentIndex + 1];
                            if (ProgressData.Instance != null && ProgressData.Instance.IsLevelUnlocked(nextLevelId))
                            {
                                movedToNext = true;
                            }
                        }
                        else
                        {
                            movedToNext = true;
                        }

                        if (!movedToNext)
                        {
                            Debug.LogWarning($"[DesktopManager] User somehow closed {targetLevelId} early. Re-opening.");
                            targetIcon.OpenApplication();
                            while (targetIcon.AppWindow == null || targetIcon.AppWindow.IsClosed) yield return null;
                            blocker.SetAllowedTarget(targetIcon.AppWindow.ContentArea);
                        }
                    }

                    Debug.Log($"[DesktopManager] Finished waiting for {targetLevelId}. Moving to next.");
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    Debug.LogError($"[DesktopManager] ERRORES: No se pudo encontrar el icono para {targetLevelId}. Asegurate de que esta configurado en el DesktopManager. La secuencia se va a abortar.");
                }
            }

            if (foundIconsCount < 4)
            {
                Debug.LogError("[DesktopManager] SECUENCIA ABORTADA. No se encontraron los 4 iconos. Arregla los iconos antes de continuar.");
                if (blockerObj != null) Destroy(blockerObj);
                yield break;
            }

            Debug.Log("[DesktopManager] Sequence finished! Transitioning to IntroTutorialScene.");
            
            // Clean up the blocker
            if (blockerObj != null) Destroy(blockerObj);
            
            // Sequence finished!
            PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
            PlayerPrefs.SetInt("DesktopTourCompleted", 0);

            // Transition to Desktop Tour
            UnityEngine.SceneManagement.SceneManager.LoadScene("IntroTutorialScene");
        }

        [ContextMenu("Spawn Initial Icons (Test)")]
        private List<GameObject> SpawnInitialIcons()
        {
            List<GameObject> spawned = new List<GameObject>();

            if (initialDesktopIcons == null || initialDesktopIcons.Count == 0)
            {
                Debug.LogWarning("No initial icons configured in DesktopManager. Please add them in the Inspector.");
                return spawned;
            }

            foreach (var iconData in initialDesktopIcons)
            {
                GameObject icon = SpawnIcon(iconData);
                if (icon != null) spawned.Add(icon);
            }
            
            Debug.Log($"Spawned {spawned.Count} icons on the desktop!");
            return spawned;
        }
        #endregion
    }
}
