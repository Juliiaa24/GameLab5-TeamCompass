/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Level abstarct class
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public enum LevelID
    {
        LEVEL1 = 0,
        LEVEL2,
        LEVEL3,
        LEVEL4,
        LEVEL5,
        LEVEL6,
        LEVEL7,
        LEVEL8,
        LEVEL9,
        LEVEL10,
        LEVEL11,
        LEVEL12,
        LEVEL13,
        LEVEL14,
        LEVEL15,
        LEVEL16,
        LEVEL17,
        LEVEL18,
        LEVEL19,
        NUM_LEVELS
    }
    /// <summary>
    /// Level with the task that the player needs to complete
    /// </summary>
    [Serializable]
    public abstract class Level : MonoBehaviour
    {
        #region Public Variables

        #endregion

        #region Private Variables
        #endregion

        #region Protected Variables
        [SerializeField]protected Task[] tasks;
        protected int tasksCount;
        protected bool levelCompleted = false;
        [SerializeField] protected LevelID id;
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {

        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            ResetLevel();
            Managers.Lm().CloseLevel();   
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public bool IsCompleted() { return levelCompleted; }
        public void CheckTasks()
        {
            
            if (!levelCompleted)
            {
                foreach (var task in tasks)
                {
                    if (tasksCount <= 0) levelCompleted = true;
                    if (task.Check())
                    {
                        task.Feedback();
                        tasksCount--;
                    }
                }
            }
            else
            {
                Managers.Lm().CompleteLevel(id);
            }
        }
        public void RegisterTasks()
        {
            tasks = GetComponentsInChildren<Task>();
            tasksCount = tasks.Length;
        }
        public void ResetLevel()
        {
            Array.Clear(tasks, 0, tasksCount);
            tasksCount = 0;
            levelCompleted = false;
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
