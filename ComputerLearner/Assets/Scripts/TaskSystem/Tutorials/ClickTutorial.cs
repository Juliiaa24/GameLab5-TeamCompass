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
        private bool played;
        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            //player = gameObject.GetComponent<VideoPlayer>();
            //if (player != null) Debug.LogWarning("No VideoPlayer Attached to the tutorial");
        }
        private void Start()
        {
        }

        private void Update()
        {
            if (player.isPlaying) played = true;
            if (played && !player.isPlaying && player.frameCount > 0
                && player.frame >= (long)player.frameCount - 1)
            {
               OnEnd();
            }
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public override void Play()
        {
            played = false;
            player.Play();
            Debug.Log("Play Tutorial");
        }
        public override void OnEnd()
        {
            Debug.Log("Finished");
            played = false;
            Window window = GetComponentInParent<Window>();
            if (window != null) window.CloseWindow();
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
