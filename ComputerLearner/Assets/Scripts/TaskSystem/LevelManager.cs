/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Level Manager
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class LevelManager : MonoBehaviour
    {

        #region Public Variables
        public static LevelManager Instance { get; private set; }
        #endregion

        #region Private Variables

        [SerializeField] private List<Level> levels;
        private Level currentLevel;

        #endregion

        #region Protected Variables

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
            currentLevel = levels[0];
            currentLevel.RegisterTasks();
            Debug.Log("FirstLevelRegistered");
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public void ChangeLevel()
        {
            levels.Remove(currentLevel);
            if (levels.Count > 0)currentLevel = levels[0];
            currentLevel.RegisterTasks();
            Debug.Log("LevelRegistered");
        }

        public Level CurrentLevel()
        {
            return currentLevel;
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class


        #endregion
    }
}
