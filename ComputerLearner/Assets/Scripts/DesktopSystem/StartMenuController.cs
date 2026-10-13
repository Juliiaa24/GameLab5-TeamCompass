/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Controller for the Simulated OS Start Menu, app launching, and system navigation.
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ComputerLearning
{
    public class StartMenuController : MonoBehaviour
    {
        #region Public Variables
        public static StartMenuController Instance { get; private set; }
        public bool IsMenuOpen => startMenuPanel != null && startMenuPanel.activeSelf;
        #endregion

        #region Private Variables
        [Header("Start Menu Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject startMenuPanel;

        [Header("Application Launch Buttons")]
        [SerializeField] private Button btnFileExplorer;
        [SerializeField] private Button btnNotepad;
        [SerializeField] private Button btnMinigamesHub;
        [SerializeField] private Button btnStats;
        [SerializeField] private Button btnExitMenu;

        [Header("Window Prefab & App Content Prefabs")]
        [SerializeField] private GameObject baseWindowPrefab;
        [SerializeField] private GameObject fileExplorerPrefab;
        [SerializeField] private GameObject notepadPrefab;
        [SerializeField] private GameObject minigamesHubPrefab;

        private Window fileExplorerWindow;
        private Window notepadWindow;
        private Window minigamesHubWindow;
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
        }

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(ToggleMenu);
            }

            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(false);
            }

            // Wire up menu button clicks
            if (btnFileExplorer != null) btnFileExplorer.onClick.AddListener(OpenFileExplorer);
            if (btnNotepad != null) btnNotepad.onClick.AddListener(OpenNotepad);
            if (btnMinigamesHub != null) btnMinigamesHub.onClick.AddListener(OpenMinigamesHub);
            if (btnStats != null) btnStats.onClick.AddListener(OpenStats);
            if (btnExitMenu != null) btnExitMenu.onClick.AddListener(ExitToMainMenu);
        }

        private void Update()
        {
            // Close Start Menu if clicking outside
            if (IsMenuOpen && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                if (startMenuPanel != null && startButton != null)
                {
                    RectTransform panelRect = startMenuPanel.GetComponent<RectTransform>();
                    RectTransform btnRect = startButton.GetComponent<RectTransform>();
                    
                    bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos);
                    bool insideButton = RectTransformUtility.RectangleContainsScreenPoint(btnRect, mousePos);

                    if (!insidePanel && !insideButton)
                    {
                        CloseMenu();
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void ToggleMenu()
        {
            if (startMenuPanel == null) return;
            bool newState = !startMenuPanel.activeSelf;
            startMenuPanel.SetActive(newState);
            if (newState)
            {
                startMenuPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseMenu()
        {
            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(false);
            }
        }

        public void OpenFileExplorer()
        {
            CloseMenu();
            OpenAppWindow("Explorador de Archivos", fileExplorerPrefab, ref fileExplorerWindow);
        }

        public void OpenNotepad()
        {
            CloseMenu();
            OpenAppWindow("Bloc de Notas", notepadPrefab, ref notepadWindow);
        }

        public void OpenMinigamesHub()
        {
            CloseMenu();
            OpenAppWindow("Centro de Juegos", minigamesHubPrefab, ref minigamesHubWindow);
        }

        public void OpenStats()
        {
            CloseMenu();
            SceneSystem sceneSys = SceneSystem.Instance != null ? SceneSystem.Instance : UnityEngine.Object.FindFirstObjectByType<SceneSystem>();
            if (sceneSys != null)
            {
                sceneSys.ChangeToStats();
            }
            else
            {
                SceneManager.LoadScene("StatsScene");
            }
        }

        public void ExitToMainMenu()
        {
            CloseMenu();
            SceneSystem sceneSys = SceneSystem.Instance != null ? SceneSystem.Instance : UnityEngine.Object.FindFirstObjectByType<SceneSystem>();
            if (sceneSys != null)
            {
                sceneSys.ChangeToMenu();
            }
            else
            {
                SceneManager.LoadScene("Menu");
            }
        }
        #endregion

        #region Private Methods
        private void OpenAppWindow(string title, GameObject contentPrefab, ref Window windowRef)
        {
            WindowManager winManager = WindowManager.Instance;
            if (winManager == null)
            {
                Debug.LogWarning("[StartMenuController] WindowManager instance not found in scene.");
                return;
            }

            if (windowRef != null && !windowRef.IsClosed)
            {
                windowRef.Restore();
                windowRef.BringToFront();
                return;
            }

            GameObject winPrefabToUse = baseWindowPrefab;
            if (winPrefabToUse == null)
            {
                winPrefabToUse = Resources.Load<GameObject>("BaseWindow");
            }

            if (winPrefabToUse == null)
            {
                Debug.LogError("[StartMenuController] BaseWindow prefab reference missing.");
                return;
            }

            windowRef = winManager.OpenWindow(winPrefabToUse, null, title);
            if (windowRef != null)
            {
                windowRef.setTitle(title);
                if (contentPrefab != null)
                {
                    GameObject contentInstance = Instantiate(contentPrefab);
                    windowRef.SetContent(contentInstance);
                }
                windowRef.BringToFront();
            }
        }
        #endregion
    }
}
