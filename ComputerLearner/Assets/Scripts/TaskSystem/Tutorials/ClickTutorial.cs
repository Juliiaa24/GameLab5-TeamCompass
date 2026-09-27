/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;
using UnityEngine.Video;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class ClickTutorial : Tutorial
    {

        #region Public Variables

        #endregion

        #region Private Variables
        private bool onPlay = false;
        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            player = GetComponent<VideoPlayer>();
            if (player != null) Debug.LogWarning("No VideoPlayer Attached to the tutorial");
        }

        private void Update()
        {
            Debug.Log(player.isPlaying);
            if (onPlay && !player.isPlaying)
            {
               OnEnd();
            }
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public override void Play()
        {
            player.Play();
            Debug.Log("Now Playing" + player.isPlaying);
            onPlay = true;
        }
        public override void OnEnd()
        {
            Destroy(gameObject);
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
