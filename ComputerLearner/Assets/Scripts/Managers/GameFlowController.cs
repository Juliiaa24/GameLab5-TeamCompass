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

        private TutorialDialogs dialogs;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();
        }

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
            // Point the mascot to the Level 1 icon so the child knows what to do next
            DraggableIcon level1Icon = DesktopManager.Instance?.GetIconByLevelId("level1");
            if (level1Icon == null) 
            {
                var icons = UnityEngine.Object.FindObjectsByType<DraggableIcon>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var icon in icons)
                {
                    if (icon.LevelDef != null && icon.LevelDef.levelId == "level1")
                    {
                        level1Icon = icon;
                        break;
                    }
                }
                if (level1Icon == null && icons.Length > 0) level1Icon = icons[0];
            }

            if (level1Icon != null)
            {
                VirtualMascot.Show(dialogs.first_welcome, level1Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }

            // We intentionally do NOT automatically start InitialTestManager or LearningSessionManager here.
            // If we do, they try to spawn tasks without a window, instantly fail, and trigger 
            // a chain reaction that calls VirtualMascot.HideMascot() in the exact same frame!
            // The LevelRunner inside the DraggableIcon will take over when the user double-clicks.
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

