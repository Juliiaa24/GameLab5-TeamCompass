using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComputerLearning;

namespace ComputerLearning
{
    public class FirstDesktopTutorial : MonoBehaviour
    {
        private TutorialDialogs dialogs;
        private void Awake() {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();
        }

        private DesktopManager desktopManager;

        private void Start()
        {
            desktopManager = GetComponent<DesktopManager>();
            if (PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 0)
            {
                StartCoroutine(AutoOnboardingSequence());
            }
        }

        private IEnumerator AutoOnboardingSequence()
        {
            Debug.Log("[FirstDesktopTutorial] Starting AutoOnboardingSequence.");
            yield return new WaitForSeconds(1f); // wait for UI to settle

            List<GameObject> spawnedIcons = new List<GameObject>(); foreach(var i in desktopManager.ActiveIcons) spawnedIcons.Add(i.gameObject);
            
            // Create a tutorial blocker to prevent user from clicking anything else
            GameObject blockerObj = new GameObject("TutorialBlocker");
            TutorialBlocker blocker = blockerObj.AddComponent<TutorialBlocker>();
            blocker.SetAllowedTarget(null); // Block EVERYTHING initially

            VirtualMascot.Show(dialogs.first_welcome, desktopManager.DesktopIconContainer, new Vector2(0, 0));
            yield return new WaitForSeconds(4f);

            // Expected sequence of LevelDefinitions
            string[] levelSequence = { "assessment", "level1", "level2", "level3", "level4", "level5" };
            int foundIconsCount = 0;

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
                    foundIconsCount++;
                    Debug.Log($"[FirstDesktopTutorial] Processing {targetLevelId} auto sequence.");
                    
                    if (ProgressData.Instance != null) ProgressData.Instance.UnlockLevel(targetLevelId);
                    // Force unlock via reflection since it's private
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
                    
                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("[FirstDesktopTutorial] Auto Sequence finished!");
            if (blockerObj != null) Destroy(blockerObj);
            
            PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
            
            // Transition to Desktop Tour
            UnityEngine.SceneManagement.SceneManager.LoadScene("IntroTutorialScene");
        }
    }
}




