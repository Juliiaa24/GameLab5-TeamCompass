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
                    if (SceneManager.GetActiveScene().name == "Menu") Application.Quit();
                    else ChangeToMenu();
                }
                else if (SceneManager.GetActiveScene().name == "Menu" && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
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
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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

            // 2. Preserve current language and student counter across the wipe
            string activeLanguage = LocalizationManager.CurrentLanguage;
            int currentStudentCounter = Mathf.Max(1, PlayerPrefs.GetInt("Playtest_StudentCounter", 1));

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

            // Ensure cursor and event system are clean and responsive (especially in WebGL)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

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

                btn.onClick.RemoveAllListeners();
                switch (btn.gameObject.name)
                {
                    case "Btn-Play":
                        btn.onClick.AddListener(ChangeToMain);
                        break;
                    case "Btn-VirtualDesktop":
                        btn.onClick.AddListener(ChangeToVirtualDesktop);
                        break;
                    case "Btn-Stats":
                        btn.onClick.AddListener(ChangeToStats);
                        break;
                    case "Btn-1":
                        btn.onClick.AddListener(ResetProgress);
                        break;
                    case "Btn-2":
                        btn.onClick.AddListener(Exit);
                        break;
                }
            }
        }

        private void EnsurePlaySpaceHint()
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            GameObject playBtnObj = GameObject.Find("Btn-Play");
            if (playBtnObj == null) return;

            Transform parent = playBtnObj.transform.parent;
            if (parent != null)
            {
                Transform oldSiblingHint = parent.Find("PlaySpaceHint");
                if (oldSiblingHint != null)
                {
                    Destroy(oldSiblingHint.gameObject);
                }
            }

            Transform existing = playBtnObj.transform.Find("PlaySpaceHint");
            TextMeshProUGUI hintText = null;

            if (existing == null)
            {
                GameObject hintObj = new GameObject("PlaySpaceHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                hintObj.transform.SetParent(playBtnObj.transform, false);

                LayoutElement le = hintObj.GetComponent<LayoutElement>();
                le.ignoreLayout = true;

                RectTransform hintRt = hintObj.GetComponent<RectTransform>();
                hintRt.anchorMin = new Vector2(0.5f, 1f);
                hintRt.anchorMax = new Vector2(0.5f, 1f);
                hintRt.pivot = new Vector2(0.5f, 0f);
                hintRt.anchoredPosition = new Vector2(0f, 10f);
                hintRt.sizeDelta = new Vector2(480f, 32f);

                hintText = hintObj.GetComponent<TextMeshProUGUI>();
                TMP_Text btnText = playBtnObj.GetComponentInChildren<TMP_Text>(true);
                if (btnText != null && btnText.font != null)
                {
                    hintText.font = btnText.font;
                }
                hintText.fontSize = 18f;
                hintText.fontStyle = FontStyles.Bold;
                hintText.textWrappingMode = TextWrappingModes.NoWrap;
                hintText.alignment = TextAlignmentOptions.Center;
                hintText.color = new Color(0.10f, 0.14f, 0.20f, 1f);
                hintText.raycastTarget = false;
            }
            else
            {
                RectTransform hintRt = existing.GetComponent<RectTransform>();
                if (hintRt != null)
                {
                    hintRt.anchorMin = new Vector2(0.5f, 1f);
                    hintRt.anchorMax = new Vector2(0.5f, 1f);
                    hintRt.pivot = new Vector2(0.5f, 0f);
                    hintRt.anchoredPosition = new Vector2(0f, 10f);
                    hintRt.sizeDelta = new Vector2(480f, 32f);
                }
                hintText = existing.GetComponent<TextMeshProUGUI>();
            }

            if (hintText != null)
            {
                hintText.text = UIDialogs.Current.menu_press_space_to_start;
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
