/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Level Manager
*/

using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private Level currentLevel;
        private List<bool> completedLevels = Enumerable.Repeat(false, (int)LevelID.NUM_LEVELS).ToList();

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
            //currentLevel = levels[0];
            //currentLevel.RegisterTasks();
            //Debug.Log("FirstLevelRegistered");
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public bool CompleteLevel(LevelID level)
        {
            return completedLevels[(int)level] = true;
        }

        public Level StartLevel(LevelID id)
        {
            if (currentLevel != null) currentLevel.ResetLevel();
            int level = (int)id;
            if (level < levels.Count && !completedLevels[(int)id])
            {
                Debug.Log($"nivel: {id}, empezado");
                currentLevel = Instantiate(levels[level]);
                currentLevel.RegisterTasks();
                currentLevel.ShowTutorial();
                return currentLevel;
            }
            else { 
                Debug.Log("LevelID no valido"); 
                return null;
            }
        }
        public Level CurrentLevel()
        {
            return currentLevel;
        }

        public void ResetProgress()
        {
            currentLevel = null;
            for (int i = 0; i < completedLevels.Count; i++) completedLevels[i] = false;
        }

        public void CloseLevel()
        {
            currentLevel = null;
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
