/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Controller for the Simulated OS Start Menu, app launching (File Explorer, Notepad, Paint, Minigames), and system navigation.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ComputerLearning
{
    public class StartMenuController : MonoBehaviour
    {
        #region Public Variables
        private static StartMenuController instance;
        public static StartMenuController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }
        public bool IsMenuOpen => startMenuPanel != null && startMenuPanel.activeSelf;
        #endregion

        #region Private Variables
        [Header("Start Menu Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject startMenuPanel;

        [Header("Application Launch Buttons")]
        [SerializeField] private Button btnFileExplorer;
        [SerializeField] private Button btnNotepad;
        [SerializeField] private Button btnPaint;
        [SerializeField] private Button btnMinigamesHub;
        [SerializeField] private Button btnStats;
        [SerializeField] private Button btnExitMenu;

        [Header("Window Prefab & App Content Prefabs")]
        [SerializeField] private GameObject baseWindowPrefab;
        [SerializeField] private GameObject fileExplorerPrefab;
        [SerializeField] private GameObject notepadPrefab;
        [SerializeField] private GameObject paintPrefab;
        [SerializeField] private GameObject minigamesHubPrefab;

        private Window fileExplorerWindow;
        private Window notepadWindow;
        private Window paintWindow;
        private Window minigamesHubWindow;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            instance = this;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(ToggleMenu);
                startButton.onClick.AddListener(ToggleMenu);
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(ToggleMenu);
                startButton.onClick.AddListener(ToggleMenu);
            }

            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(false);
            }

            if (btnFileExplorer != null)
            {
                btnFileExplorer.onClick.AddListener(OpenFileExplorer);
            }
            if (btnNotepad != null)
            {
                btnNotepad.onClick.AddListener(OpenNotepad);
            }
            if (btnPaint != null)
            {
                btnPaint.onClick.AddListener(OpenPaint);
            }
            if (btnMinigamesHub != null)
            {
                btnMinigamesHub.onClick.AddListener(OpenMinigamesHub);
            }
            if (btnStats != null)
            {
                btnStats.onClick.AddListener(OpenStats);
            }
            if (btnExitMenu != null)
            {
                btnExitMenu.onClick.AddListener(ExitToMainMenu);
            }
        }

        private void Update()
        {
            if (IsMenuOpen && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
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
                DraggableIcon.ClearSelection();
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

        public GameObject BaseWindowPrefab => baseWindowPrefab;
        public GameObject FileExplorerPrefab => fileExplorerPrefab;
        public GameObject NotepadPrefab => notepadPrefab;
        public GameObject PaintPrefab => paintPrefab;

        public void OpenFileExplorer()
        {
            CloseMenu();
            OpenAppWindow("File Explorer", fileExplorerPrefab, ref fileExplorerWindow);
        }

        public FileExplorerApp OpenFileExplorerAtFolder(string folderName)
        {
            CloseMenu();
            string winTitle = string.IsNullOrEmpty(folderName) ? "File Explorer" : $"File Explorer - {folderName}";
            OpenAppWindow(winTitle, fileExplorerPrefab, ref fileExplorerWindow);
            if (fileExplorerWindow != null && fileExplorerWindow.Content != null)
            {
                FileExplorerApp explorer = fileExplorerWindow.Content.GetComponentInChildren<FileExplorerApp>();
                if (explorer != null && !string.IsNullOrEmpty(folderName))
                {
                    explorer.OpenFolderByName(folderName);
                }
                return explorer;
            }
            return null;
        }

        public void OpenNotepad()
        {
            CloseMenu();
            OpenAppWindow("Notepad", notepadPrefab, ref notepadWindow);
        }

        public NotepadApp OpenNotepadWithFile(ExplorerItemData fileItem, ExplorerItemData folder = null)
        {
            CloseMenu();
            string winTitle = fileItem != null ? $"Notepad - {fileItem.name}" : "Notepad";
            OpenAppWindow(winTitle, notepadPrefab, ref notepadWindow);
            if (notepadWindow != null)
            {
                notepadWindow.setTitle(winTitle);
                if (notepadWindow.Content != null)
                {
                    NotepadApp notepad = notepadWindow.Content.GetComponentInChildren<NotepadApp>();
                    if (notepad != null && fileItem != null)
                    {
                        notepad.LoadFile(fileItem, notepadWindow, folder);
                    }
                    return notepad;
                }
            }
            return null;
        }

        public void OpenPaint()
        {
            CloseMenu();
            OpenPaintWithFile(null);
        }

        public PaintApp OpenPaintWithFile(ExplorerItemData fileItem, ExplorerItemData folder = null)
        {
            CloseMenu();
            string winTitle = fileItem != null ? $"Paint - {fileItem.name}" : "Paint - Untitled.png";

            OpenAppWindow(winTitle, paintPrefab, ref paintWindow);
            if (paintWindow != null)
            {
                paintWindow.setTitle(winTitle);
                PaintApp app = paintWindow.Content != null ? paintWindow.Content.GetComponentInChildren<PaintApp>() : null;
                if (app != null)
                {
                    if (fileItem != null)
                    {
                        app.LoadImageFile(fileItem, paintWindow, folder);
                    }
                    else if (folder != null)
                    {
                        app.SetTargetSaveFolder(folder);
                    }
                }
                paintWindow.BringToFront();
                return app;
            }

            return null;
        }

        public ImageViewerApp OpenImageViewer(ExplorerItemData fileItem)
        {
            CloseMenu();
            if (fileItem == null) return null;

            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : FindAnyObjectByType<WindowManager>();
            if (winManager == null) return null;

            Window existingWin = winManager.FindOpenWindowByTitle($"Image Viewer - {fileItem.name}");
            if (existingWin != null && !existingWin.IsClosed)
            {
                existingWin.Restore();
                existingWin.BringToFront();
                return existingWin.Content != null ? existingWin.Content.GetComponentInChildren<ImageViewerApp>() : null;
            }

            GameObject winPrefab = baseWindowPrefab != null ? baseWindowPrefab : Resources.Load<GameObject>("BaseWindow");
            GameObject viewerPrefab = Resources.Load<GameObject>("ImageViewerContent");
#if UNITY_EDITOR
            if (viewerPrefab == null) viewerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/ImageViewerContent.prefab");
#endif

            if (winPrefab == null || viewerPrefab == null) return null;

            string winTitle = $"Image Viewer - {fileItem.name}";
            Window win = winManager.OpenWindow(winPrefab, null, winTitle);
            if (win != null)
            {
                win.setTitle(winTitle);
                GameObject viewerContent = Instantiate(viewerPrefab);
                win.SetContent(viewerContent);
                ImageViewerApp viewer = viewerContent.GetComponentInChildren<ImageViewerApp>();
                if (viewer != null)
                {
                    Sprite spriteToDisplay = fileItem.imageContent != null
                        ? fileItem.imageContent
                        : (fileItem.icon != null ? fileItem.icon : FileExplorerApp.GetImageFileSprite());

                    viewer.Initialize(fileItem, spriteToDisplay, () =>
                    {
                        OpenPaintWithFile(fileItem);
                    });
                }
                win.BringToFront();
                return viewer;
            }
            return null;
        }

        public void OpenMinigamesHub()
        {
            CloseMenu();
            OpenAppWindow("Minigames Hub", minigamesHubPrefab, ref minigamesHubWindow);
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
                winManager = FindAnyObjectByType<WindowManager>();
            }
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
            if (winPrefabToUse == null && DesktopManager.Instance != null)
            {
                winPrefabToUse = DesktopManager.Instance.BaseWindowPrefab;
            }
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
