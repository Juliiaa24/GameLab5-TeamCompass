/**
 * Author: Julia Vera 
 * Date: 25/09/2026
 * Description: Manager that checks if tasks have been completed
*/

using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Metadata;

namespace ComputerLearning
{
    /// <summary>
    /// Checks if tasks have been completed
    /// </summary>
    public class TaskManager : MonoBehaviour
    {

        #region Public Variables
        /**
         * Unique Instance of TaskManager to apply the singleton method
        */
        public static TaskManager Instance { get; private set; }


        #endregion

        #region Private Variables
       
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
        }

        private void Update()
        {
            Managers.Lm().CurrentLevel().CheckTasks();
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class
        //private void RegisterLevel(GameObject level)
        //{
        //    Debug.Log(level);
        //    levelTasks = level.GetComponentsInChildren<Task>();
        //    levelTasksCount = levelTasks.Length;
           
        //}

        #endregion
    }
}
