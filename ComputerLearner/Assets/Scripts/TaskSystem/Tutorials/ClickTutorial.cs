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
            Debug.Log(played + "<- On Play | Playing ->" + player.isPlaying);
            if (played && !player.isPlaying)
            {
               OnEnd();
            }
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public override void Play()
        {
            played = true;
            Debug.Log("Play Tutorial");
        }
        public override void OnEnd()
        {
            Debug.Log("Finished");
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
