/**
 * Author: Julia Vera
 * Date: 11/09
 * Description:
*/

using System;
using UnityEngine;
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
            Tutorial1,
            Desktop2,
            Tutorial2,
            VirtualDesktop
        }


        #endregion

        #region Private Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        private void Start()
        {

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
        // Public Methods accessible from other classes
        public void ChangeToMain() 
        { 
            // If the initial assessment has not been completed, it serves as the first screen!
            if (PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0)
            {
                SceneManager.LoadScene("AssessmentScene");
                return;
            }

            if (PlayerPrefs.GetInt("AdvancedDesktopTourCompleted", 0) == 1)
            {
                SceneManager.LoadScene("VirtualDesktop");
            }
            else if (PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1)
            {
                SceneManager.LoadScene("Tutorial2");
            }
            else if (PlayerPrefs.GetInt("DesktopTourCompleted", 0) == 1)
            {
                SceneManager.LoadScene("Desktop2");
            }
            else if (PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1)
            {
                SceneManager.LoadScene("Tutorial1");
            }
            else
            {
                SceneManager.LoadScene("Desktop1");
            }
        }
        public void ChangeToMenu() { ChangeScene(SceneNames.Menu); }
        public void ChangeToVirtualDesktop() { SceneManager.LoadScene("VirtualDesktop"); }
        public void ChangeToAssessment() { ChangeScene(SceneNames.AssessmentScene); }
        public void ChangeToStats() { ChangeScene(SceneNames.StatsScene); }
        public void Exit() { Application.Quit(); }

        public void ResetProgress()
        {
            string lang = PlayerPrefs.GetString("SelectedLanguage", "EN");

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.ClearHistory();
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ResetAllScores();
            }

            if (InitialTestManager.Instance != null)
            {
                Destroy(InitialTestManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString("SelectedLanguage", lang);
            PlayerPrefs.SetInt("InitialAssessmentCompleted", 0);
            PlayerPrefs.SetInt("AutoSequenceCompleted", 0);
            PlayerPrefs.SetInt("DesktopTourCompleted", 0);
            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 0);
            PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", 0);
            PlayerPrefs.Save();

            Debug.Log("[SceneSystem] Game progress has been successfully reset.");
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class

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
                case SceneNames.Tutorial1:
                    SceneManager.LoadScene("Tutorial1");
                    break;
                case SceneNames.Desktop2:
                    SceneManager.LoadScene("Desktop2");
                    break;
                case SceneNames.Tutorial2:
                    SceneManager.LoadScene("Tutorial2");
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
