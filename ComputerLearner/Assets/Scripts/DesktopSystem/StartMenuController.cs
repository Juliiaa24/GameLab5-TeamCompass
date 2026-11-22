/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Controller for the Simulated OS Start Menu, app launching (File Explorer, Notepad, Paint, Minigames), and system navigation.
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
        public RectTransform StartButtonRect => startButton != null ? startButton.GetComponent<RectTransform>() : null;
        public RectTransform StartMenuPanelRect => startMenuPanel != null ? startMenuPanel.GetComponent<RectTransform>() : null;
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
        [SerializeField] private GameObject scoresContentPrefab;

        private Window fileExplorerWindow;
        private Window notepadWindow;
        private Window paintWindow;
        private Window minigamesHubWindow;
        private Window scoresWindow;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            instance = this;
            ResolveFallbackReferences();
            ApplyAppButtonIcons();
            BindButtons();

            if (btnMinigamesHub != null)
            {
                btnMinigamesHub.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            UpdateLocalizedMenuLabels();
            LocalizationManager.OnLanguageChanged += UpdateLocalizedMenuLabels;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedMenuLabels;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Start()
        {
            ResolveFallbackReferences();
            ApplyAppButtonIcons();
            BindButtons();

            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(false);
            }

            if (btnMinigamesHub != null)
            {
                btnMinigamesHub.gameObject.SetActive(false);
            }

            UpdateLocalizedMenuLabels();
        }

        private void ResolveFallbackReferences()
        {
            if (startMenuPanel == null) startMenuPanel = gameObject;
            if (startButton == null && transform.parent != null)
            {
                startButton = transform.parent.Find("Taskbar/StartButton")?.GetComponent<Button>();
            }
            if (startMenuPanel == null) return;
            Transform container = startMenuPanel.transform.Find("ButtonsContainer");
            if (container == null) return;

            if (btnFileExplorer == null) btnFileExplorer = container.Find("btnExplorer")?.GetComponent<Button>();
            if (btnNotepad == null) btnNotepad = container.Find("btnNotepad")?.GetComponent<Button>();
            if (btnPaint == null) btnPaint = container.Find("BtnPaint")?.GetComponent<Button>();
            if (btnMinigamesHub == null) btnMinigamesHub = container.Find("btnMinigames")?.GetComponent<Button>();
            if (btnStats == null) btnStats = container.Find("btnStats")?.GetComponent<Button>();
            if (btnExitMenu == null) btnExitMenu = container.Find("btnExit")?.GetComponent<Button>();
        }

        private void BindButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(ToggleMenu);
                startButton.onClick.AddListener(ToggleMenu);
            }
            if (btnFileExplorer != null)
            {
                btnFileExplorer.onClick.RemoveListener(OpenFileExplorer);
                btnFileExplorer.onClick.AddListener(OpenFileExplorer);
            }
            if (btnNotepad != null)
            {
                btnNotepad.onClick.RemoveListener(OpenNotepad);
                btnNotepad.onClick.AddListener(OpenNotepad);
            }
            if (btnPaint != null)
            {
                btnPaint.onClick.RemoveListener(OpenPaint);
                btnPaint.onClick.AddListener(OpenPaint);
            }
            if (btnMinigamesHub != null)
            {
                btnMinigamesHub.onClick.RemoveListener(OpenMinigamesHub);
                btnMinigamesHub.onClick.AddListener(OpenMinigamesHub);
            }
            if (btnStats != null)
            {
                btnStats.onClick.RemoveListener(OpenStats);
                btnStats.onClick.AddListener(OpenStats);
            }
            if (btnExitMenu != null)
            {
                btnExitMenu.onClick.RemoveListener(ExitToMainMenu);
                btnExitMenu.onClick.AddListener(ExitToMainMenu);
            }
        }

        private void Update()
        {
            if (IsMenuOpen && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                if (startMenuPanel != null)
                {
                    Canvas rootCanvas = startMenuPanel.GetComponentInParent<Canvas>();
                    Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? rootCanvas.worldCamera
                        : null;

                    RectTransform panelRect = startMenuPanel.GetComponent<RectTransform>();
                    bool insidePanel = panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos, uiCam);
                    bool insideStartBtn = startButton != null &&
                                          RectTransformUtility.RectangleContainsScreenPoint(startButton.GetComponent<RectTransform>(), mousePos, uiCam);
                    bool insideAnyMenuBtn = IsPointerInsideButton(btnFileExplorer, mousePos, uiCam) ||
                                            IsPointerInsideButton(btnNotepad, mousePos, uiCam) ||
                                            IsPointerInsideButton(btnPaint, mousePos, uiCam) ||
                                            IsPointerInsideButton(btnMinigamesHub, mousePos, uiCam) ||
                                            IsPointerInsideButton(btnStats, mousePos, uiCam) ||
                                            IsPointerInsideButton(btnExitMenu, mousePos, uiCam);

                    if (!insidePanel && !insideStartBtn && !insideAnyMenuBtn)
                    {
                        CloseMenu();
                    }
                }
            }
        }

        private static bool IsPointerInsideButton(Button btn, Vector2 mousePos, Camera uiCam)
        {
            if (btn == null || !btn.gameObject.activeInHierarchy) return false;
            RectTransform rt = btn.GetComponent<RectTransform>();
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, uiCam);
        }
        #endregion

        #region Public Methods
        public void UpdateLocalizedMenuLabels()
        {
            UIDialogs ui = UIDialogs.Current;
            SetButtonText(startButton, ui.start_menu_btn_start);
            SetButtonText(btnFileExplorer, ui.start_menu_file_explorer);
            SetButtonText(btnNotepad, ui.start_menu_notepad);
            SetButtonText(btnPaint, ui.start_menu_paint);
            SetButtonText(btnMinigamesHub, ui.start_menu_minigames);
            SetButtonText(btnStats, ui.start_menu_stats);
            SetButtonText(btnExitMenu, ui.start_menu_exit);

            if (startMenuPanel != null)
            {
                Transform header = startMenuPanel.transform.Find("Header/Title") ?? startMenuPanel.transform.Find("HeaderTitle");
                if (header != null)
                {
                    TMP_Text headerTmp = header.GetComponent<TMP_Text>();
                    if (headerTmp != null) headerTmp.text = ui.start_menu_header;
                }
            }
        }

        private void SetButtonText(Button btn, string textValue)
        {
            if (btn == null || string.IsNullOrEmpty(textValue)) return;
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) tmp.text = textValue;
        }

        public void ToggleMenu()
        {
            if (startMenuPanel == null) return;
            bool newState = !startMenuPanel.activeSelf;
            startMenuPanel.SetActive(newState);
            if (newState)
            {
                UpdateLocalizedMenuLabels();
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
            OpenAppWindow("File Explorer", fileExplorerPrefab, ref fileExplorerWindow, GetButtonIcon(btnFileExplorer));
        }

        public FileExplorerApp OpenFileExplorerAtFolder(string folderName)
        {
            CloseMenu();
            string winTitle = string.IsNullOrEmpty(folderName) ? "File Explorer" : $"File Explorer - {folderName}";
            OpenAppWindow(winTitle, fileExplorerPrefab, ref fileExplorerWindow, GetButtonIcon(btnFileExplorer));
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

        private void ApplyAppButtonIcons()
        {
            SetButtonIcon(btnNotepad, FileExplorerApp.GetNotepadAppSprite());
            SetButtonIcon(btnPaint, FileExplorerApp.GetPaintAppSprite());
        }

        private static void SetButtonIcon(Button btn, Sprite iconSprite)
        {
            if (btn == null || iconSprite == null) return;
            Transform iconTr = btn.transform.Find("Icon");
            if (iconTr == null) return;
            Image img = iconTr.GetComponent<Image>();
            if (img != null) img.sprite = iconSprite;
        }

        public void OpenNotepad()
        {
            CloseMenu();
            GameObject prefab = notepadPrefab != null ? notepadPrefab : (DesktopManager.Instance != null ? DesktopManager.Instance.NotepadContentPrefab : null);
            Sprite notepadIcon = FileExplorerApp.GetNotepadAppSprite() ?? GetButtonIcon(btnNotepad);
            OpenAppWindow("Notepad", prefab, ref notepadWindow, notepadIcon);
        }

        public NotepadApp OpenNotepadWithFile(ExplorerItemData fileItem, ExplorerItemData folder = null)
        {
            CloseMenu();
            string winTitle = fileItem != null ? $"Notepad - {fileItem.name}" : "Notepad";
            GameObject prefab = notepadPrefab != null ? notepadPrefab : (DesktopManager.Instance != null ? DesktopManager.Instance.NotepadContentPrefab : null);
            Sprite notepadIcon = FileExplorerApp.GetNotepadAppSprite() ?? GetButtonIcon(btnNotepad);
            OpenAppWindow(winTitle, prefab, ref notepadWindow, notepadIcon);
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

            GameObject prefab = paintPrefab != null ? paintPrefab : (DesktopManager.Instance != null ? DesktopManager.Instance.PaintContentPrefab : null);
            Sprite paintIcon = FileExplorerApp.GetPaintAppSprite() ?? GetButtonIcon(btnPaint);
            OpenAppWindow(winTitle, prefab, ref paintWindow, paintIcon);
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
            OpenAppWindow(UIDialogs.Current.start_menu_minigames, minigamesHubPrefab, ref minigamesHubWindow);
        }

        public void OpenStats()
        {
            CloseMenu();
            WindowManager winManager = WindowManager.Instance != null ? WindowManager.Instance : FindAnyObjectByType<WindowManager>();
            GameObject contentPrefab = scoresContentPrefab != null
                ? scoresContentPrefab
                : (DesktopManager.Instance != null ? DesktopManager.Instance.ScoresContentPrefab : null);

            if (winManager != null && contentPrefab != null)
            {
                RetroThemeAssets assets = RetroThemeAssets.Instance;
                Sprite trophyIcon = assets != null && assets.iconTrophy != null ? assets.iconTrophy : GetButtonIcon(btnStats);
                string title = UIDialogs.Current != null && !string.IsNullOrEmpty(UIDialogs.Current.scores_window_title)
                    ? UIDialogs.Current.scores_window_title
                    : "Score";
                OpenAppWindow(title, contentPrefab, ref scoresWindow, trophyIcon);
                return;
            }

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
        private static Sprite GetButtonIcon(Button btn)
        {
            if (btn == null) return null;
            Transform iconTr = btn.transform.Find("Icon");
            if (iconTr == null) return null;
            Image img = iconTr.GetComponent<Image>();
            return img != null ? img.sprite : null;
        }

        private void OpenAppWindow(string title, GameObject contentPrefab, ref Window windowRef, Sprite appIcon = null)
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
                if (appIcon != null)
                {
                    windowRef.setIcon(appIcon);
                }
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
