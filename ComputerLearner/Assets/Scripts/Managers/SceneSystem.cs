/**
 * Author: Julia Vera & TeamCompass
 * Date: 11/09
 * Description: Controls changes between scenes with DontDestroyOnLoad persistence,
 *              preserves Inspector button bindings when reloading scenes, and handles
 *              Playtest Student ID progression and full reset (Assessment + Skills + Levels).
 *              Enhanced with Space to Start and localized visual space hint.
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// The class that controls changes in between scenes
    /// </summary>
    public class SceneSystem : MonoBehaviour
    {
        #region Public Variables
        /** Instance of the SceneManager to apply the singleton method*/
        public static SceneSystem Instance { get; private set; }

        public enum SceneNames
        {
            Menu,
            Main,
            AssessmentScene,
            StatsScene,
            Desktop1,
            VirtualDesktop
        }
        #endregion

        #region Unity Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeSessionFlags()
        {
            PlaytestDataManager.EnsureInitialized();

            // Preserve saved progression flags across sessions; do not wipe intermediate progress.
            // Progression flags are preserved across sessions and only cleared via ResetProgress().
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            // When reloading a scene (like Menu) that has its own SceneSystem serialized
            // in the buttons' Inspector OnClick events, destroy the OLD persistent instance
            // and keep THIS new scene instance so the scene buttons never lose their target reference!
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            PlaytestDataManager.EnsureInitialized();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            LocalizationManager.OnLanguageChanged += HandleLanguageChangedForMenu;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChangedForMenu;
        }

        private void Start()
        {
            EnsureMenuButtonsBound();
            EnsurePlaySpaceHint();
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChangedForMenu;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Menu")
            {
                EnsureMenuButtonsBound();
                EnsurePlaySpaceHint();
            }
        }

        private void HandleLanguageChangedForMenu()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                EnsurePlaySpaceHint();
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (TeacherDashboardController.IsDashboardOpen)
                    {
                        TeacherDashboardController.CloseDashboard();
                        return;
                    }
                    MenuSceneReferences menuRefs = FindAnyObjectByType<MenuSceneReferences>();
                    if (menuRefs != null && menuRefs.IsAnyInputOrModalActive())
                    {
                        return;
                    }
                    if (SceneManager.GetActiveScene().name == "Menu") Application.Quit();
                    else ChangeToMenu();
                }
                else if (SceneManager.GetActiveScene().name == "Menu" && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    MenuSceneReferences menuRefs = FindAnyObjectByType<MenuSceneReferences>();
                    if ((menuRefs != null && menuRefs.IsAnyInputOrModalActive()) ||
                        TeacherDashboardController.IsDashboardOpen ||
                        ClassroomNetworkWidget.IsManualInputFocused ||
                        PlaytestStudentWidget.IsEditingStudentId ||
                        (TeacherModeGate.Instance != null && TeacherModeGate.Instance.IsAnyBlockingModalOpen))
                    {
                        return;
                    }

                    // Ensure the player is not currently typing into a student ID / text input field
                    bool isInputFocused = EventSystem.current != null &&
                                          EventSystem.current.currentSelectedGameObject != null &&
                                          (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null ||
                                           EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null);
                    if (!isInputFocused)
                    {
                        ChangeToMain();
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void ChangeToMain() 
        { 
            SessionManager.Instance.StartOrContinueSession();
        }

        public void ChangeToMenu() 
        { 
            ProgressData.Instance?.Save();
            PlayerPrefs.Save();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ChangeScene(SceneNames.Menu); 
        }

        public void ChangeToVirtualDesktop() 
        { 
            SessionManager.Instance.PrepareForDirectVirtualDesktopEntry();
            SceneManager.LoadScene("VirtualDesktop"); 
        }

        public void ChangeToAssessment() 
        { 
            ChangeScene(SceneNames.AssessmentScene); 
        }

        public void ChangeToStats() 
        { 
            ChangeScene(SceneNames.StatsScene); 
        }

        public void Exit() 
        { 
            if (PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlaytestDataManager.ExportCurrentStudentData("exit_button");
            }
            Application.Quit(); 
        }

        public void ResetProgress()
        {
            // 1. Auto-export the finishing student's data to Desktop before wiping if they did anything
            if (PlaytestDataManager.HasAnyRecordedActivityForCurrentStudent())
            {
                PlaytestDataManager.ExportCurrentStudentData("auto_before_reset");
            }

            // 2. Preserve current language, student counter, auto-download setting, selected LearningPath, and Assessment toggle
            string activeLanguage = LocalizationManager.CurrentLanguage;
            int currentStudentCounter = Mathf.Max(1, PlayerPrefs.GetInt("Playtest_StudentCounter", 1));
            int autoDownloadSetting = PlayerPrefs.GetInt("Playtest_AutoDownloadEnabled", 0);
            LearningPath selectedPath = SessionManager.Instance.ActiveLearningPath;
            bool assessmentEnabled = SessionManager.Instance.IsAssessmentEnabled;

            // 3. Clear all runtime managers
            if (ProgressData.ExistingInstance != null)
            {
                ProgressData.ExistingInstance.ClearHistory();
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ResetAllScores();
            }

            PlaytestDataManager.ClearCurrentStudentSessionData();

            if (InitialTestManager.Instance != null)
            {
                Destroy(InitialTestManager.Instance.gameObject);
            }

            // 4. Wipe all PlayerPrefs and restore only global settings + clean initial flags
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString("SelectedLanguage", activeLanguage);
            PlayerPrefs.SetInt("Playtest_StudentCounter", currentStudentCounter);
            PlayerPrefs.SetInt("Playtest_AutoDownloadEnabled", autoDownloadSetting);
            PlayerPrefs.SetInt("Playtest_AutoDownloadDefaultOff_v2", 1);
            PlayerPrefs.SetInt("InitialAssessmentCompleted", 0);
            PlayerPrefs.SetInt("AutoSequenceCompleted", 0);
            PlayerPrefs.SetInt("DesktopTourCompleted", 0);
            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 0);
            PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", 0);
            PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", 0);
            PlayerPrefs.Save();

            // 5. Advance to the next sequential Student ID (e.g. S01 -> S02) and re-arm selected LearningPath + Assessment toggle
            string nextStudentId = PlaytestDataManager.AdvanceToNextStudent();
            SessionManager.Instance.ResetForNextStudent(nextStudentId, selectedPath, assessmentEnabled);

            LocalizationManager.SetLanguage(activeLanguage);

            if (LearningPathSelectorWidget.Instance != null)
            {
                LearningPathSelectorWidget.Instance.RefreshUI();
            }

            if (ClassroomNetworkWidget.Instance != null)
            {
                ClassroomNetworkWidget.Instance.RefreshUI();
            }

            if (PlaytestStudentWidget.Instance != null)
            {
                PlaytestStudentWidget.Instance.RefreshUI();
                string msg = string.Format(UIDialogs.Current.playtest_reset_success, nextStudentId);
                PlaytestStudentWidget.Instance.ShowToast(msg, 4f);
            }

            Debug.Log($"[SceneSystem] Full game & assessment progress reset. Ready for next student: {nextStudentId}");
        }
        #endregion

        #region Private Methods
        private static void BindMenuButton(Button btn, UnityAction action)
        {
            if (btn == null) return;
            int count = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                btn.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void EnsureMenuButtonsBound()
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            // Ensure cursor and event system are clean and responsive (especially in WebGL)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            MenuSceneReferences menuRefs = FindAnyObjectByType<MenuSceneReferences>();
            if (menuRefs != null)
            {
                BindMenuButton(menuRefs.btnPlay, ChangeToMain);
                BindMenuButton(menuRefs.btnStats, ChangeToStats);
                BindMenuButton(menuRefs.btnExit, Exit);

                if (menuRefs.btnJoinClass != null && menuRefs.classroomNetworkWidget != null)
                {
                    BindMenuButton(menuRefs.btnJoinClass, menuRefs.classroomNetworkWidget.OpenStudentJoinModal);
                }

                menuRefs.teacherModeGate?.RefreshLocalization();
                menuRefs.teacherModeGate?.RefreshVisualState();
                menuRefs.playtestStudentWidget?.RefreshUI();
                menuRefs.learningPathSelectorWidget?.RefreshUI();
                menuRefs.classroomNetworkWidget?.RefreshUI();
                return;
            }

            // Fallback lookup only if MenuSceneReferences is absent
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;
                switch (btn.gameObject.name)
                {
                    case "Btn-Play":
                        BindMenuButton(btn, ChangeToMain);
                        break;
                    case "Btn-Stats":
                        BindMenuButton(btn, ChangeToStats);
                        break;
                    case "Btn-2":
                        BindMenuButton(btn, Exit);
                        break;
                }
            }
        }

        private void EnsurePlaySpaceHint()
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            UIDialogs ui = UIDialogs.Current;
            MenuSceneReferences menuRefs = FindAnyObjectByType<MenuSceneReferences>();
            if (menuRefs != null)
            {
                if (menuRefs.playSpaceHintText != null)
                    menuRefs.playSpaceHintText.text = ui.menu_press_space_to_start;
                if (menuRefs.desktopTitleText != null)
                    menuRefs.desktopTitleText.text = ui.menu_welcome_title;
                if (menuRefs.languageLabelText != null)
                    menuRefs.languageLabelText.text = ui.menu_language_label;
                return;
            }

            GameObject playBtnObj = GameObject.Find("Btn-Play");
            if (playBtnObj == null) return;
            Transform existing = playBtnObj.transform.Find("PlaySpaceHint");
            if (existing != null)
            {
                TMP_Text hint = existing.GetComponent<TMP_Text>();
                if (hint != null) hint.text = ui.menu_press_space_to_start;
            }
        }

        private void ChangeScene(SceneNames scene)
        {
            switch (scene)
            {
                case SceneNames.Menu:
                    SceneManager.LoadScene("Menu");
                    break;
                case SceneNames.Main:
                case SceneNames.Desktop1:
                    SceneManager.LoadScene("Desktop1");
                    break;
                case SceneNames.VirtualDesktop:
                    SceneManager.LoadScene("VirtualDesktop");
                    break;
                case SceneNames.AssessmentScene:
                    SceneManager.LoadScene("AssessmentScene");
                    break;
                case SceneNames.StatsScene:
                    SceneManager.LoadScene("StatsScene");
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
