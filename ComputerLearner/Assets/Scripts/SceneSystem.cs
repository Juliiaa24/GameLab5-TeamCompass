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
        // Public Variables [Public Constant Variables, Public Component References, Public Variables]

        // Public Constant Variables
        /** Instance of the SceneManager to apply the singleton method*/
        public static SceneSystem Instance { get; private set; }


        // Public Component References


        // Public Variables
        public enum SceneNames
        {
            Menu,
            Main
        }


        #endregion

        #region Private Variables
        // Private Variables [Private Constant Variables, Private Component References, Private Variables]

        // Private Constant Variables


        // Private Component References


        // Private Variables


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
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {

        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public void ChangeToMain() { ChangeScene(SceneNames.Main); }
        public void ChangeToMenu() { ChangeScene(SceneNames.Menu); }

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
                    SceneManager.LoadScene("MainScene");
                    break;
                default:
                    break;
            }
        }


        #endregion
    }
}
