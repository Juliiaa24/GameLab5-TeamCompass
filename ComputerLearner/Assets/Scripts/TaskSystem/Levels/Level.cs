/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Level abstarct class
*/

using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    enum LevelID
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
    /// 
    /// </summary>
    public abstract class Level : MonoBehaviour
    {
        #region Public Variables

        #endregion

        #region Private Variables
        
        #endregion

        #region Protected Variables
        protected List<Task> tasks;
        protected bool levelCompleted = false;
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {

        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public bool IsCompleted() { return levelCompleted; }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class


        #endregion
    }
}
