/**
 * Author: DEVELOPERNAME
 * Date: 25/09
 * Description: Tutorial to demonstrate how to perform a task (video)
*/

using System;
using UnityEngine;
using UnityEngine.Video;

namespace ComputerLearning
{
    /// <summary>
    /// Tutorial to demonstrate how to perform a task
    /// </summary>
    [Serializable]
    public abstract class Tutorial : MonoBehaviour
    {

        #region Public Variables
        
        #endregion

        #region Private Variables


        #endregion

        #region Protected Variables
        [SerializeField] protected VideoPlayer player;

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            //player = GetComponent<VideoPlayer>();
            //if (player != null) Debug.LogWarning("No VideoPlayer Attached to the tutorial");
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public abstract void Play();
        public abstract void OnEnd();

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
