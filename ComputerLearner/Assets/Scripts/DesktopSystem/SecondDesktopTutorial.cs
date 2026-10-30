using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [DefaultExecutionOrder(-100)]
    public class SecondDesktopTutorial : MonoBehaviour
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

            bool isAlreadyCompleted = PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1 ||
                                      (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(9));

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
                blockerObj = new GameObject("SemiGuidedBlocker");
                blocker = blockerObj.AddComponent<TutorialBlocker>();
            }

            blocker.gameObject.SetActive(true);
            blocker.SetAllowedTarget(null);
        }

        private void Start()
        {
            desktopManager = GetComponent<DesktopManager>();

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.SyncProgressionFlagsToPlayerPrefs();
            }

            bool isAlreadyCompleted = PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1 ||
                                      (ProgressData.Instance != null && ProgressData.Instance.AreAllLevelsCompletedUpTo(9));

            if (isAlreadyCompleted)
            {
                PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 1);
                PlayerPrefs.Save();
                if (blockerObj != null) Destroy(blockerObj);
                return;
            }

            EnsureBlockerActive();
            StartCoroutine(SemiGuidedOnboardingSequence());
        }

        private IEnumerator SemiGuidedOnboardingSequence()
        {
            Debug.Log("[SecondDesktopTutorial] Starting SemiGuidedOnboardingSequence.");
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

            string[] levelSequence = { "level6", "level7", "level8", "level9" };

            foreach (string targetLevelId in levelSequence)
            {
                if (ProgressData.Instance != null && ProgressData.Instance.IsLevelCompleted(targetLevelId))
                {
                    Debug.Log($"[SecondDesktopTutorial] Skipping already completed level: {targetLevelId}");
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
                    Debug.Log($"[SecondDesktopTutorial] Guiding user to open {targetLevelId}.");

                    if (ProgressData.Instance != null) ProgressData.Instance.UnlockLevel(targetLevelId);
                    targetIcon.GetType().GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(targetIcon, false);

                    string introMessage = "Double click here to start the next practice!";
                    if (targetLevelId == "level6")
                        introMessage = !string.IsNullOrEmpty(dialogs?.second_right_click) ? dialogs.second_right_click : "Let's learn something new: Right Click! Double click here to start.";
                    else if (targetLevelId == "level7")
                        introMessage = !string.IsNullOrEmpty(dialogs?.second_click_hold) ? dialogs.second_click_hold : "Great! Now let's try Click and Hold. Open this app.";
                    else if (targetLevelId == "level8")
                        introMessage = !string.IsNullOrEmpty(dialogs?.second_drag_drop) ? dialogs.second_drag_drop : "Awesome! Time for Drag and Drop. Double click!";
                    else if (targetLevelId == "level9")
                        introMessage = !string.IsNullOrEmpty(dialogs?.second_mixed) ? dialogs.second_mixed : "You're a master! Let's mix everything together in a final challenge!";

                    while (true)
                    {
                        // 1. Allow clicking ONLY the icon
                        blockerObj.SetActive(true);
                        blocker.SetAllowedTarget(targetIcon.GetComponent<RectTransform>());

                        VirtualMascot.Show(introMessage, targetIcon.GetComponent<RectTransform>(), new Vector2(160, -80));

                        // Wait until the window actually opens (user double clicks)
                        yield return new WaitUntil(() => targetIcon.AppWindow != null && targetIcon.AppWindow.gameObject.activeInHierarchy);

                        // 2. Window is open! Let the child play the level and close it normally.
                        VirtualMascot.HideMascot();
                        blocker.SetAllowedTarget(null);
                        blockerObj.SetActive(false);

                        // Grab the runner to track completion
                        LevelRunner runner = targetIcon.AppWindow.GetComponentInChildren<LevelRunner>();

                        // Wait until the window starts closing or is closed
                        yield return new WaitUntil(() => targetIcon.AppWindow == null || !targetIcon.AppWindow.gameObject.activeInHierarchy || targetIcon.AppWindow.IsClosed);

                        // Did they finish?
                        if (runner != null && runner.IsCompleted)
                        {
                            break;
                        }

                        // Otherwise, they closed it early. Remind them.
                        introMessage = "Oops! You closed it without finishing! Double click to try again.";
                        yield return new WaitForSeconds(1f);
                    }

                    // Re-enable blocker for the next guided step
                    blockerObj.SetActive(true);
                    blocker.SetAllowedTarget(null);

                    if (ProgressData.Instance != null)
                    {
                        ProgressData.Instance.MarkLevelCompleted(targetLevelId);
                        ProgressData.Instance.Save();
                    }
                    PlayerPrefs.Save();

                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("[SecondDesktopTutorial] Semi-Guided Sequence finished!");
            if (blockerObj != null) Destroy(blockerObj);

            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 1);
            if (ProgressData.Instance != null) ProgressData.Instance.Save();
            PlayerPrefs.Save();

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
