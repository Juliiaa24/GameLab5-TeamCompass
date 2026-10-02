/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Top-level coordinator that wires the full learning flow together.
 */
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Wires the full learning flow in order:
    ///
    ///   Tutorial → Initial Test → Learning Sessions (loop)
    ///
    /// This class only connects events — all actual logic lives in the individual managers.
    /// Add this component to the same persistent GameObject as the other managers.
    ///
    /// Inspector flags let you skip the tutorial or initial test during development.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        #region Private Variables
        [Header("Development Shortcuts")]
        [Tooltip("Skip the tutorial and go straight to the initial test (useful during development).")]
        [SerializeField] private bool skipTutorial = false;

        [Tooltip("Skip the initial test and go straight to learning sessions with all scores at 0.")]
        [SerializeField] private bool skipInitialTest = false;

        [Header("Content Area")]
        [Tooltip("The RectTransform used as the parent for all spawned task prefabs. " +
                 "Typically a window's content RectTransform.")]
        [SerializeField] private RectTransform taskContentArea;
        #endregion

        #region Unity Methods
        private void Start()
        {
            // Subscribe to events before starting anything
            SubscribeToEvents();
            // Hand the content area to managers that need it
            PropagateContentArea();
            // Kick off the flow
            StartFlow();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Private Methods
        private void SubscribeToEvents()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnTutorialCompleted += OnTutorialCompleted;

            if (InitialTestManager.Instance != null)
                InitialTestManager.Instance.OnTestCompleted += OnInitialTestCompleted;

            if (LearningSessionManager.Instance != null)
                LearningSessionManager.Instance.OnSessionEnded += OnSessionEnded;
        }

        private void UnsubscribeFromEvents()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnTutorialCompleted -= OnTutorialCompleted;

            if (InitialTestManager.Instance != null)
                InitialTestManager.Instance.OnTestCompleted -= OnInitialTestCompleted;

            if (LearningSessionManager.Instance != null)
                LearningSessionManager.Instance.OnSessionEnded -= OnSessionEnded;
        }

        private void PropagateContentArea()
        {
            if (taskContentArea == null) return;
            InitialTestManager.Instance?.SetContentArea(taskContentArea);
            LearningSessionManager.Instance?.SetContentArea(taskContentArea);
        }

        private void StartFlow()
        {
            DesktopTour tour = UnityEngine.Object.FindFirstObjectByType<DesktopTour>();
            if (tour != null && (tour.forcePlayTutorial || PlayerPrefs.GetInt("DesktopTourCompleted", 0) == 0))
            {
                tour.OnIntroductionCompleted += StartActualFlow;
            }
            else
            {
                StartActualFlow();
            }
        }

        private void StartActualFlow()
        {
            if (skipTutorial)
            {
                Debug.Log("[GameFlowController] Tutorial skipped.");
                OnTutorialCompleted();
            }
            else
            {
                TutorialManager.Instance?.StartTutorial();
            }
        }

        private void OnTutorialCompleted()
        {
            if (skipInitialTest)
            {
                Debug.Log("[GameFlowController] Initial test skipped.");
                OnInitialTestCompleted();
            }
            else
            {
                InitialTestManager.Instance?.StartTest();
            }
        }

        private void OnInitialTestCompleted()
        {
            LearningSessionManager.Instance?.StartSession();
        }

        private void OnSessionEnded()
        {
            Debug.Log("[GameFlowController] Session ended — starting next session.");
            // For now, loop immediately. Later: show a results screen, award badges, etc.
            LearningSessionManager.Instance?.StartSession();
        }
        #endregion
    }
}
