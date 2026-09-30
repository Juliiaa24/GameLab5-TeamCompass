/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Minimal tutorial/introduction manager.
 *              Completely decoupled — replace or extend without touching other systems.
 */
using System;
using System.Collections;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Manages the tutorial/introduction sequence shown before the initial test.
    ///
    /// This is intentionally minimal to allow the tutorial content to be freely changed.
    /// When the tutorial ends, it fires OnTutorialCompleted.
    /// GameFlowController listens to that event to start the initial test.
    ///
    /// To extend: add your own UI, video, or panel sequence logic here.
    /// Other managers do not need to know about the tutorial format.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        #region Public Variables
        public static TutorialManager Instance { get; private set; }

        /// <summary>True while the tutorial is playing.</summary>
        public bool IsTutorialActive { get; private set; }

        /// <summary>Fired when the tutorial sequence completes.</summary>
        public event Action OnTutorialCompleted;
        #endregion

        #region Private Variables
        [Tooltip("GameObjects to show during the tutorial. All are hidden when the tutorial ends.")]
        [SerializeField] private GameObject[] tutorialPanels;

        [Tooltip("If > 0, the tutorial advances automatically after this many seconds. " +
                 "If 0, you must call ConfirmTutorial() from a UI button.")]
        [SerializeField] private float autoAdvanceDelay = 0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>Shows the tutorial panels and begins the sequence.</summary>
        public void StartTutorial()
        {
            if (IsTutorialActive) return;
            IsTutorialActive = true;

            foreach (GameObject panel in tutorialPanels)
                if (panel != null) panel.SetActive(true);

            if (autoAdvanceDelay > 0f)
                StartCoroutine(AutoAdvanceCoroutine());
            else
                Debug.Log("[TutorialManager] Tutorial started — call ConfirmTutorial() to proceed.");
        }

        /// <summary>
        /// Called by a UI confirm/continue button to manually complete the tutorial.
        /// </summary>
        public void ConfirmTutorial()
        {
            FinishTutorial();
        }

        /// <summary>Skips the tutorial immediately (useful for returning players).</summary>
        public void SkipTutorial()
        {
            FinishTutorial();
        }
        #endregion

        #region Private Methods
        private IEnumerator AutoAdvanceCoroutine()
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            FinishTutorial();
        }

        private void FinishTutorial()
        {
            if (!IsTutorialActive) return;
            IsTutorialActive = false;

            foreach (GameObject panel in tutorialPanels)
                if (panel != null) panel.SetActive(false);

            Debug.Log("[TutorialManager] Tutorial complete.");
            OnTutorialCompleted?.Invoke();
        }
        #endregion
    }
}
