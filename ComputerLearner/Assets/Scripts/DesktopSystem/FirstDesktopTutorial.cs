using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [DefaultExecutionOrder(-100)]
    public class FirstDesktopTutorial : MonoBehaviour
    {
        private TutorialDialogs dialogs;

        [Tooltip("If true, forces tutorial only when no saved progress exists.")]
        public bool forcePlayTutorial = false;

        private DesktopManager desktopManager;
        private GameObject blockerObj;
        private TutorialBlocker blocker;

        private void Awake()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.SyncProgressionFlagsToPlayerPrefs();
            }

            bool isAlreadyCompleted = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1 ||
                                      (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(5));

            if (!isAlreadyCompleted)
            {
                EnsureBlockerActive();
            }
        }

        private void EnsureBlockerActive()
        {
            if (blocker != null)
            {
                blocker.gameObject.SetActive(true);
                blocker.SetAllowedTarget(null);
                return;
            }

            if (TutorialBlocker.Instance != null)
            {
                blocker = TutorialBlocker.Instance;
                blockerObj = blocker.gameObject;
            }
            else
            {
                blockerObj = new GameObject("TutorialBlocker");
                blocker = blockerObj.AddComponent<TutorialBlocker>();
            }

            blocker.gameObject.SetActive(true);
            blocker.SetAllowedTarget(null); // Block 100% of input from frame 0
        }

        private void Start()
        {
            desktopManager = GetComponent<DesktopManager>();

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.SyncProgressionFlagsToPlayerPrefs();
            }

            bool isAlreadyCompleted = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 1 ||
                                      (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(5));

            if (isAlreadyCompleted)
            {
                PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
                PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
                PlayerPrefs.Save();

                if (blockerObj != null) Destroy(blockerObj);

                if (SceneSystem.Instance != null)
                {
                    SceneSystem.Instance.ChangeToVirtualDesktop();
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("VirtualDesktop");
                }
                return;
            }

            EnsureBlockerActive();
            StartCoroutine(AutoOnboardingSequence());
        }

        private IEnumerator AutoOnboardingSequence()
        {
            Debug.Log("[FirstDesktopTutorial] Starting AutoOnboardingSequence.");
            EnsureBlockerActive();

            yield return new WaitForSeconds(1f); // wait for UI to settle while input is 100% blocked

            List<GameObject> spawnedIcons = new List<GameObject>();
            if (desktopManager != null && desktopManager.ActiveIcons != null)
            {
                foreach (var i in desktopManager.ActiveIcons)
                {
                    if (i != null) spawnedIcons.Add(i.gameObject);
                }
            }

            EnsureBlockerActive();
            blocker.SetAllowedTarget(null);

            bool anyPriorProgress = ProgressData.Instance != null &&
                                    (ProgressData.Instance.IsLevelCompleted("level1") ||
                                     ProgressData.Instance.IsLevelCompleted("level2") ||
                                     ProgressData.Instance.IsLevelCompleted("level3") ||
                                     ProgressData.Instance.IsLevelCompleted("level4") ||
                                     ProgressData.Instance.IsLevelCompleted("level5"));

            if (!anyPriorProgress && desktopManager != null)
            {
                VirtualMascot.Show(dialogs.first_welcome, desktopManager.DesktopIconContainer, new Vector2(0, 0));
                yield return new WaitForSeconds(4f);
            }

            // Expected sequence of LevelDefinitions
            string[] levelSequence = { "assessment", "level1", "level2", "level3", "level4", "level5" };

            foreach (string targetLevelId in levelSequence)
            {
                // Skip levels that were already completed in a previous session
                if (targetLevelId == "assessment" && PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 1)
                {
                    continue;
                }
                if (ProgressData.Instance != null && ProgressData.Instance.IsLevelCompleted(targetLevelId))
                {
                    Debug.Log($"[FirstDesktopTutorial] Skipping already completed level: {targetLevelId}");
                    continue;
                }

                DraggableIcon targetIcon = null;
                foreach (var iconObj in spawnedIcons)
                {
                    if (iconObj == null) continue;
                    DraggableIcon draggable = iconObj.GetComponent<DraggableIcon>();
                    if (draggable != null && draggable.LevelDef != null && draggable.LevelDef.levelId == targetLevelId)
                    {
                        targetIcon = draggable;
                        break;
                    }
                }

                if (targetIcon != null)
                {
                    Debug.Log($"[FirstDesktopTutorial] Processing {targetLevelId} auto sequence.");

                    EnsureBlockerActive();
                    blocker.SetAllowedTarget(null); // Keep desktop icons blocked while mascot introduces the level

                    if (ProgressData.Instance != null) ProgressData.Instance.UnlockLevel(targetLevelId);
                    targetIcon.GetType().GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(targetIcon, false);

                    // 1. Highlight the icon and explain
                    string mascotMsg = dialogs.first_default;
                    if (targetLevelId == "assessment") mascotMsg = "Let's start with a quick assessment! Double click the Assessment icon!";
                    else if (targetLevelId == "level1") mascotMsg = dialogs.first_move;
                    else if (targetLevelId == "level2") mascotMsg = dialogs.first_hover;
                    else if (targetLevelId == "level3") mascotMsg = dialogs.first_click;
                    else if (targetLevelId == "level4") mascotMsg = dialogs.first_adaptive;
                    else if (targetLevelId == "level5") mascotMsg = dialogs.first_double_click;

                    VirtualMascot.Show(mascotMsg, targetIcon.GetComponent<RectTransform>(), new Vector2(150, -50));
                    yield return new WaitForSeconds(3f);

                    // 2. Auto-open the application
                    targetIcon.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
                    targetIcon.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, clickCount = 2 });

                    yield return new WaitForSeconds(0.5f); // Wait for window to spawn

                    // 3. Constrain clicks ONLY to the app window content
                    if (targetIcon.AppWindow != null)
                    {
                        blocker.SetAllowedTarget(targetIcon.AppWindow.ContentArea);

                        // Wait until the window closes (level completed)
                        yield return new WaitUntil(() => targetIcon.AppWindow == null || !targetIcon.AppWindow.gameObject.activeInHierarchy);
                    }

                    // Immediately re-block everything between levels
                    if (blocker != null) blocker.SetAllowedTarget(null);

                    if (ProgressData.Instance != null)
                    {
                        ProgressData.Instance.MarkLevelCompleted(targetLevelId);
                        ProgressData.Instance.Save();
                    }
                    PlayerPrefs.Save();

                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("[FirstDesktopTutorial] Auto Sequence finished!");
            if (blockerObj != null) Destroy(blockerObj);

            PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
            PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.Save();
            }
            PlayerPrefs.Save();

            // Transition directly to VirtualDesktop
            if (SceneSystem.Instance != null)
            {
                SceneSystem.Instance.ChangeToVirtualDesktop();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("VirtualDesktop");
            }
        }

        private void OnDestroy()
        {
            if (blockerObj != null)
            {
                Destroy(blockerObj);
            }
        }
    }
}
