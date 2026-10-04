using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComputerLearning;

namespace ComputerLearning
{
    public class SecondDesktopTutorial : MonoBehaviour
    {
        private DesktopManager desktopManager;

        private void Start()
        {
            desktopManager = GetComponent<DesktopManager>();
            if (PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 0)
            {
                StartCoroutine(SemiGuidedOnboardingSequence());
            }
        }

        private IEnumerator SemiGuidedOnboardingSequence()
        {
            Debug.Log("[SecondDesktopTutorial] Starting SemiGuidedOnboardingSequence.");
            yield return new WaitForSeconds(1f); // wait for UI to settle

            // Ensure the initial icons are spawned
            List<GameObject> spawnedIcons = new List<GameObject>(); foreach(var i in desktopManager.ActiveIcons) spawnedIcons.Add(i.gameObject);
            
            // Create a tutorial blocker to guide clicks
            GameObject blockerObj = new GameObject("SemiGuidedBlocker");
            TutorialBlocker blocker = blockerObj.AddComponent<TutorialBlocker>();
            blocker.SetAllowedTarget(null); // Block EVERYTHING initially

            string[] levelSequence = { "level6", "level7", "level8", "level9" };

            foreach (string targetLevelId in levelSequence)
            {
                DraggableIcon targetIcon = null;
                foreach (var iconObj in spawnedIcons)
                {
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
                    
                    // 1. Allow clicking ONLY the icon
                    blocker.SetAllowedTarget(targetIcon.GetComponent<RectTransform>());
                    
                    string introMessage = "Double click here to start the next practice!";
                    if (targetLevelId == "level6") introMessage = "Let's learn something new: Right Click! Double click here to start.";
                    else if (targetLevelId == "level7") introMessage = "Great! Now let's try Click and Hold. Open this app.";
                    else if (targetLevelId == "level8") introMessage = "Awesome! Time for Drag and Drop. Double click!";
                    else if (targetLevelId == "level9") introMessage = "You're a master! Let's mix everything together in a final challenge!";

                    VirtualMascot.Show(introMessage, targetIcon.GetComponent<RectTransform>(), new Vector2(160, -80));
                    
                    // Wait until the window actually opens (user double clicks)
                    yield return new WaitUntil(() => targetIcon.AppWindow != null && targetIcon.AppWindow.gameObject.activeInHierarchy);
                    
                    // 2. Window is open! Let the child play the level and close it normally.
                    VirtualMascot.HideMascot();
                    blocker.SetAllowedTarget(null); // Block nothing
                    blockerObj.SetActive(false); // Let them interact freely with the desktop and window

                    // Wait until the window closes (level completed)
                    yield return new WaitUntil(() => targetIcon.AppWindow == null || !targetIcon.AppWindow.gameObject.activeInHierarchy);
                    
                    // Re-enable blocker for the next guided step
                    blockerObj.SetActive(true);
                    
                    // Wait to make sure progress data is saved
                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("[SecondDesktopTutorial] Semi-Guided Sequence finished!");
            if (blockerObj != null) Destroy(blockerObj);
            
            PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 1);
            VirtualMascot.Show("You've completed all the computer basics! You're ready to use the desktop freely.", desktopManager.DesktopIconContainer, new Vector2(0, 0));
        }
    }
}

