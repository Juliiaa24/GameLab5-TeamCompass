/**
 * Author: Diego
 * Date: 25/09/26
 * Description: A configured level used as window content, with explicit replay.
*/
using UnityEngine;

namespace ComputerLearning
{
    public class TrainingLevel : Level
    {
        #region Private Variables
        [SerializeField] private bool startOnOpen = true;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (startOnOpen) StartExercise();
        }
        #endregion

        #region Public Methods
        public void StartExercise()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.BeginLevel(this);
        }
        #endregion
    }
}
