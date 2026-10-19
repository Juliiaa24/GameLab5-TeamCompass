/**
 * Author: Diego
 * Date: 19/10/26
 * Description: Runs an introductory mascot tour on the full Virtual Desktop while blocking player input,
 *              then releases full control and transitions the mascot into desktop companion mode.
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class VirtualDesktopIntro : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, plays the Virtual Desktop introduction even if VirtualDesktopIntroCompleted is already set.")]
        public bool forcePlayIntro = false;

        private TutorialBlocker blocker;
        private TutorialDialogs dialogs;

        private void LoadDialogs()
        {
            TextAsset jsonFile = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (jsonFile != null)
            {
                dialogs = JsonUtility.FromJson<TutorialDialogs>(jsonFile.text);
            }
            else
            {
                dialogs = new TutorialDialogs();
            }
        }

        private void Start()
        {
            if (!forcePlayIntro && PlayerPrefs.GetInt("VirtualDesktopIntroCompleted", 0) == 1)
            {
                return;
            }

            LoadDialogs();
            SetupBlocker();
            StartCoroutine(IntroSequence());
        }

        private void SetupBlocker()
        {
            if (TutorialBlocker.Instance != null)
            {
                blocker = TutorialBlocker.Instance;
            }
            else
            {
                GameObject blockerObj = new GameObject("TutorialBlocker");
                blocker = blockerObj.AddComponent<TutorialBlocker>();
            }

            blocker.gameObject.SetActive(true);
            blocker.SetExcludedTarget(null);
            blocker.SetAllowedTarget(null); // Blocks 100% of clicks during the intro
        }

        private IEnumerator IntroSequence()
        {
            // Wait briefly for DesktopManager and layout groups to finish positioning icons
            yield return new WaitForSeconds(0.5f);

            if (blocker != null)
            {
                blocker.gameObject.SetActive(true);
                blocker.SetAllowedTarget(null);
            }

            RectTransform centerTarget = FindCenterTarget();
            RectTransform levelsTarget = FindDesktopIconByLabel("Levels") ?? centerTarget;
            RectTransform appsTarget = FindDesktopIconByLabel("Paint")
                                       ?? FindDesktopIconByLabel("Minigames")
                                       ?? FindDesktopIconByLabel("Notepad")
                                       ?? levelsTarget;
            RectTransform startButtonTarget = FindStartButtonTarget() ?? levelsTarget;

            // 1. Welcome to the full Virtual Desktop
            string welcomeMsg = (dialogs != null && !string.IsNullOrEmpty(dialogs.virtual_welcome))
                ? dialogs.virtual_welcome
                : "Welcome to your full Virtual Desktop! Here you can explore and practice freely.";
            VirtualMascot.Show(welcomeMsg, centerTarget != null ? centerTarget : levelsTarget, Vector2.zero);
            yield return new WaitForSeconds(4.5f);

            // 2. Highlight the Levels folder
            string levelsMsg = (dialogs != null && !string.IsNullOrEmpty(dialogs.virtual_levels))
                ? dialogs.virtual_levels
                : "Double-click the Levels folder anytime to challenge yourself with more tasks.";
            VirtualMascot.Show(levelsMsg, levelsTarget, Vector2.zero);
            yield return new WaitForSeconds(4.5f);

            // 3. Highlight Creative Apps & Minigames
            if (appsTarget != null && appsTarget != levelsTarget)
            {
                string appsMsg = (dialogs != null && !string.IsNullOrEmpty(dialogs.virtual_apps))
                    ? dialogs.virtual_apps
                    : "You can also draw in Paint, type in Notepad, or play fun Minigames!";
                VirtualMascot.Show(appsMsg, appsTarget, Vector2.zero);
                yield return new WaitForSeconds(4.5f);
            }

            // 4. Highlight Start Button & Companion Pet
            string startAndPetMsg = (dialogs != null && !string.IsNullOrEmpty(dialogs.virtual_start_and_pet))
                ? dialogs.virtual_start_and_pet
                : "Click the Start button on the bottom-left for more options. I'll be in the top-right corner whenever you need tips. Have fun!";
            VirtualMascot.Show(startAndPetMsg, startButtonTarget, Vector2.zero);
            yield return new WaitForSeconds(5.0f);

            CompleteIntro();
        }

        private void CompleteIntro()
        {
            PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", 1);
            PlayerPrefs.Save();

            if (blocker != null)
            {
                blocker.ClearTarget();
                Destroy(blocker.gameObject);
                blocker = null;
            }

            // HideMascot transitions the mascot back to DesktopCompanion mode in the top-right corner
            VirtualMascot.HideMascot();
        }

        private void OnDestroy()
        {
            if (blocker != null)
            {
                Destroy(blocker.gameObject);
                blocker = null;
            }
        }

        private RectTransform FindCenterTarget()
        {
            if (DesktopManager.Instance != null && DesktopManager.Instance.DesktopIconContainer != null)
            {
                return DesktopManager.Instance.DesktopIconContainer;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.GetComponent<RectTransform>() : null;
        }

        private RectTransform FindDesktopIconByLabel(string labelText)
        {
            DraggableIcon[] icons = Object.FindObjectsByType<DraggableIcon>(FindObjectsSortMode.None);
            foreach (DraggableIcon icon in icons)
            {
                if (icon == null || !icon.gameObject.activeInHierarchy) continue;

                if (icon.gameObject.name.IndexOf(labelText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return icon.GetComponent<RectTransform>();
                }

                TMP_Text tmp = icon.GetComponentInChildren<TMP_Text>();
                if (tmp != null && tmp.text != null && tmp.text.IndexOf(labelText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return icon.GetComponent<RectTransform>();
                }

                Text legacyText = icon.GetComponentInChildren<Text>();
                if (legacyText != null && legacyText.text != null && legacyText.text.IndexOf(labelText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return icon.GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private RectTransform FindStartButtonTarget()
        {
            GameObject startBtnObj = GameObject.Find("StartButton");
            if (startBtnObj != null)
            {
                return startBtnObj.GetComponent<RectTransform>();
            }

            Taskbar taskbar = Object.FindFirstObjectByType<Taskbar>();
            if (taskbar != null)
            {
                return taskbar.GetComponent<RectTransform>();
            }

            return null;
        }
    }
}
