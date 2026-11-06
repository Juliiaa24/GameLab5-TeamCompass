/**
 * Author: Julia Vera & TeamCompass
 * Date: 11/09
 * Description: Controls changes between scenes with DontDestroyOnLoad persistence,
 *              preserves Inspector button bindings when reloading scenes, and handles
 *              Playtest Student ID progression and full reset (Assessment + Skills + Levels).
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

            // If a previous session was aborted mid-onboarding before reaching VirtualDesktop,
            // clear intermediate tutorial flags so pressing Play in Menu starts cleanly at Desktop1.
            // Preserve saved progression flags across sessions; do not wipe intermediate progress.
            // Progression flags are preserved across sessions and only cleared via ResetProgress().
            /*if (PlayerPrefs.GetInt("AdvancedDesktopTourCompleted", 0) == 0)
            {
                PlayerPrefs.SetInt("AutoSequenceCompleted", 0);
                PlayerPrefs.SetInt("DesktopTourCompleted", 0);
                PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 0);
                PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", 0);*/
            {
                PlayerPrefs.Save();
            }
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
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            EnsureMenuButtonsBound();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Menu")
            {
                EnsureMenuButtonsBound();
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (SceneManager.GetActiveScene().name == "Menu") Application.Quit();
                else ChangeToMenu();
            }
        }
        #endregion

        #region Public Methods
        public void ChangeToMain() 
        { 
            // If the initial assessment has not been completed, it serves as the first screen!
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.SyncProgressionFlagsToPlayerPrefs();
            }

            if (PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0)
            {
                SceneManager.LoadScene("AssessmentScene");
                return;
            }

            if (PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1 ||
                (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(5)))
            {
                SceneManager.LoadScene("VirtualDesktop");
            }
            else
            {
                SceneManager.LoadScene("Desktop1");
            }
        }

        public void ChangeToMenu() 
        { 
            ProgressData.Instance?.Save();
            PlayerPrefs.Save();
            ChangeScene(SceneNames.Menu); 
        }

        public void ChangeToVirtualDesktop() 
        { 
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

            // 2. Preserve current language, student counter and auto-download setting across the wipe
            string activeLanguage = LocalizationManager.CurrentLanguage;
            int currentStudentCounter = Mathf.Max(1, PlayerPrefs.GetInt("Playtest_StudentCounter", 1));
            int autoDownloadSetting = PlayerPrefs.GetInt("Playtest_AutoDownloadEnabled", 1);

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
            PlayerPrefs.SetInt("InitialAssessmentCompleted", 0);
            PlayerPrefs.SetInt("AutoSequenceCompleted", 0);
            PlayerPrefs.SetInt("DesktopTourCompleted", 0);
            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 0);
            PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", 0);
            PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", 0);
            PlayerPrefs.Save();

            // 5. Advance to the next sequential Student ID (e.g. S01 -> S02)
            string nextStudentId = PlaytestDataManager.AdvanceToNextStudent();

            LocalizationManager.SetLanguage(activeLanguage);

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
        private void EnsureMenuButtonsBound()
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            GameObject menuRootObj = GameObject.Find("Menu");
            Canvas menuCanvas = menuRootObj != null ? menuRootObj.GetComponent<Canvas>() : null;
            if (menuCanvas != null && menuCanvas.gameObject.scene.name == "Menu")
            {
                PlaytestStudentWidget.EnsureOnCanvas(menuCanvas.transform);
            }

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;

                // Only add runtime listener if the Inspector persistent listener is missing/broken
                bool hasValidPersistentListener = false;
                int count = btn.onClick.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    if (btn.onClick.GetPersistentTarget(i) != null && !string.IsNullOrEmpty(btn.onClick.GetPersistentMethodName(i)))
                    {
                        hasValidPersistentListener = true;
                        break;
                    }
                }

                if (hasValidPersistentListener) continue;

                // Only bind the 5 main menu buttons; do NOT remove listeners from other UI widgets
                switch (btn.gameObject.name)
                {
                    case "Btn-Play":
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ChangeToMain);
                        break;
                    case "Btn-VirtualDesktop":
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ChangeToVirtualDesktop);
                        break;
                    case "Btn-Stats":
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ChangeToStats);
                        break;
                    case "Btn-1":
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ResetProgress);
                        break;
                    case "Btn-2":
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(Exit);
                        break;
                }
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
