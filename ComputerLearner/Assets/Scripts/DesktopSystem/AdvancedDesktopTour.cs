/**
 * Author: Julia Vera & TeamCompass
 * Date: 05/10/26
 * Description: Advanced desktop tutorial teaching icon movement, window movement,
 *              resizing, minimize/maximize/close, and recycling items directly on VirtualDesktop.
 */
using UnityEngine;
using System.Collections;
using System;

namespace ComputerLearning
{
    [Serializable]
    public class AdvancedDesktopTourDialogs
    {
        public string step1_welcome = "Welcome back! Now that you've mastered tasks, let's learn how to organize and manipulate windows like a pro!";
        public string step2_moveIcon = "You can drag desktop icons anywhere to organize your space. Try moving this Practice Note to another spot!";
        public string step2_reminder = "Click and hold the left mouse button on the icon to drag it.";
        public string step2_perfect = "Perfect!";
        public string step3_openWindow = "Now, open the Practice Note by double-clicking on it.";
        public string step3_reminder = "Remember: double-click quickly to open it.";
        public string step4_moveWindow = "Windows can be moved anywhere on your screen! Drag the top title bar to move the window.";
        public string step4_reminder = "Click on the top title bar and drag the window.";
        public string step4_veryGood = "Very good!";
        public string step5_resizeWindow = "If you move your mouse to the window edges or corners, you can drag to make it bigger or smaller. Try resizing it!";
        public string step5_reminder = "Drag from the corners or edges to resize the window.";
        public string step5_awesome = "Awesome! You can resize any window to fit your needs.";
        public string step6_maximize = "You can also make the window fill the whole screen! Click the Maximize button.";
        public string step6_maximizeReminder = "Click the square Maximize button in the top-right.";
        public string step6_restore = "Now click the button again to restore it to normal size.";
        public string step6_restoreReminder = "Click the restore button in the top-right.";
        public string step6_greatJob = "Great job!";
        public string step7_minimize = "To temporarily hide a window, click the Minimize button.";
        public string step7_minimizeReminder = "Click the minus Minimize button.";
        public string step7_taskbar = "It hides right here on your taskbar! Click the icon to bring it back.";
        public string step7_taskbarReminder = "Click the taskbar button to restore your window.";
        public string step7_thereItIs = "There it is!";
        public string step8_close = "When you are finished with a window, click the red X to close it.";
        public string step8_closeReminder = "Click the red X to close the window.";
        public string step9_recycle = "Finally, if you don't need a file anymore, you can throw it in the Recycle Bin. Drag the Practice Note there!";
        public string step9_recycleReminder = "Drag the Practice Note icon over the Recycle Bin and drop it.";
        public string step9_congrats = "Congratulations! You've mastered all desktop and window controls. Have fun exploring!";
    }

    public class AdvancedDesktopTour : MonoBehaviour
    {
        public static bool IsTourRunning { get; private set; }

        private AdvancedDesktopTourDialogs dialogs;
        private TutorialBlocker tutorialBlocker;
        private RecycleBinIcon cachedRecycleBin;
        private Action<DraggableIcon> activeRecycledListener;

        private void LoadDialogs()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("AdvancedDesktopTourDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<AdvancedDesktopTourDialogs>(json.text);
            if (dialogs == null) dialogs = new AdvancedDesktopTourDialogs();
        }

        private void SetupTutorialBlocker()
        {
            if (TutorialBlocker.Instance != null)
            {
                tutorialBlocker = TutorialBlocker.Instance;
                return;
            }

            GameObject blockerObj = new GameObject("TutorialBlocker");
            tutorialBlocker = blockerObj.AddComponent<TutorialBlocker>();
        }

        private void OnDestroy()
        {
            IsTourRunning = false;
            if (activeRecycledListener != null)
            {
                RecycleBinIcon.OnIconRecycled -= activeRecycledListener;
                activeRecycledListener = null;
            }
            if (cachedRecycleBin != null)
            {
                cachedRecycleBin.enabled = true;
            }
            if (tutorialBlocker != null)
            {
                Destroy(tutorialBlocker.gameObject);
                tutorialBlocker = null;
            }
        }

        private IEnumerator Start()
        {
            IsTourRunning = true;
            LoadDialogs();
            Debug.Log("[AdvancedDesktopTour] Starting AdvancedDesktopTour sequence...");
            SetupTutorialBlocker();
            tutorialBlocker?.SetAllowedTarget(null);
            yield return new WaitForSeconds(0.5f);

            // Find or spawn a Practice Note for the tutorial
            DraggableIcon appIcon = null;
            DraggableIcon[] allIcons = UnityEngine.Object.FindObjectsByType<DraggableIcon>();
            foreach (var i in allIcons)
            {
                if (i != null && i.name.Equals("Practice Note", StringComparison.OrdinalIgnoreCase))
                {
                    appIcon = i;
                    break;
                }
            }

            if (appIcon == null && DesktopManager.Instance != null)
            {
                DesktopIconData practiceData = new DesktopIconData
                {
                    iconName = "Practice Note",
                    itemType = DesktopItemType.TextDocument,
                    contentPrefab = DesktopManager.Instance.NotepadContentPrefab,
                    isUserCreated = true
                };
                GameObject spawned = DesktopManager.Instance.SpawnIcon(practiceData);
                if (spawned != null)
                {
                    appIcon = spawned.GetComponent<DraggableIcon>();
                }
            }

            if (appIcon == null)
            {
                // Fallback to any non-protected, non-recycle-bin icon
                foreach (var i in allIcons)
                {
                    if (i != null && i.GetComponent<RecycleBinIcon>() == null && !DesktopManager.IsSystemProtectedName(i.name))
                    {
                        appIcon = i;
                        break;
                    }
                }
            }

            RecycleBinIcon recycleBin = UnityEngine.Object.FindAnyObjectByType<RecycleBinIcon>(FindObjectsInactive.Include);
            if (recycleBin == null)
            {
                foreach (var i in allIcons)
                {
                    if (i != null && i.name.IndexOf("Recycle", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        recycleBin = i.GetComponent<RecycleBinIcon>() ?? i.gameObject.AddComponent<RecycleBinIcon>();
                        break;
                    }
                }
            }

            if (appIcon == null || recycleBin == null)
            {
                Debug.LogWarning("[AdvancedDesktopTour] Missing icons for tutorial! Completing tutorial.");
                CompleteTutorial();
                yield break;
            }

            // Disable the recycle bin initially so the user doesn't accidentally use it early
            cachedRecycleBin = recycleBin;
            recycleBin.enabled = false;

            RectTransform iconRect = appIcon.GetComponent<RectTransform>();
            RectTransform binRect = recycleBin.GetComponent<RectTransform>();

            // 1. Welcome
            tutorialBlocker?.SetAllowedTarget(null);
            VirtualMascot.Show(dialogs.step1_welcome, iconRect, new Vector2(250, 0));
            yield return new WaitForSeconds(4.0f);

            // 2. Move Icon
            appIcon.AllowDoubleClick = false; // Block opening it by accident
            VirtualMascot.Show(dialogs.step2_moveIcon, iconRect, new Vector2(250, 0));
            tutorialBlocker?.SetAllowedTarget(iconRect);

            Vector3 startIconPos = iconRect.position;
            yield return StartCoroutine(WaitWithReminder(() => iconRect == null || Vector3.Distance(startIconPos, iconRect.position) > 50f,
                dialogs.step2_reminder, iconRect, new Vector2(250, 0)));

            tutorialBlocker?.SetAllowedTarget(null);
            VirtualMascot.Show(dialogs.step2_perfect, iconRect, new Vector2(250, 0));
            yield return new WaitForSeconds(2.0f);

            // 3. Open Window
            appIcon.AllowDoubleClick = true;
            VirtualMascot.Show(dialogs.step3_openWindow, iconRect, new Vector2(250, 0));
            tutorialBlocker?.SetAllowedTarget(iconRect);
            yield return StartCoroutine(WaitWithReminder(() => appIcon == null || (appIcon.AppWindow != null && !appIcon.AppWindow.IsClosed),
                dialogs.step3_reminder, iconRect, new Vector2(250, 0)));

            Window targetWindow = appIcon != null ? appIcon.AppWindow : null;
            if (targetWindow != null)
            {
                RectTransform windowRect = targetWindow.GetComponent<RectTransform>();
                RectTransform dragBarRect = targetWindow.TitleBar != null
                    ? targetWindow.TitleBar
                    : (targetWindow.transform.Find("WindowTop") as RectTransform ?? targetWindow.transform.Find("TopBar") as RectTransform ?? windowRect);

                // Prepare button references
                Transform btnClose = targetWindow.CloseButton != null
                    ? targetWindow.CloseButton.transform
                    : (windowRect.Find("WindowTop/Buttons/Close") ?? windowRect.Find("TopBar/Buttons/CloseButton") ?? windowRect.Find("Close"));
                Transform btnMin = targetWindow.MinimizeButton != null
                    ? targetWindow.MinimizeButton.transform
                    : (windowRect.Find("WindowTop/Buttons/Minimize") ?? windowRect.Find("TopBar/Buttons/MinimizeButton"));
                Transform btnMax = targetWindow.MaximizeButton != null
                    ? targetWindow.MaximizeButton.transform
                    : (windowRect.Find("WindowTop/Buttons/Maximize") ?? windowRect.Find("TopBar/Buttons/MaximizeButton"));

                // 4. Move Window
                VirtualMascot.Show(dialogs.step4_moveWindow, windowRect, new Vector2(0, 200));
                tutorialBlocker?.SetAllowedTarget(dragBarRect);

                tutorialBlocker?.SetExcludedTarget(null);
                if (btnClose != null) tutorialBlocker?.AddExcludedTarget(btnClose as RectTransform);
                if (btnMin != null) tutorialBlocker?.AddExcludedTarget(btnMin as RectTransform);
                if (btnMax != null) tutorialBlocker?.AddExcludedTarget(btnMax as RectTransform);

                Vector3 startWinPos = windowRect.position;
                yield return StartCoroutine(WaitWithReminder(() => {
                    return windowRect == null || targetWindow.IsClosed || Vector3.Distance(startWinPos, windowRect.position) > 50f;
                }, dialogs.step4_reminder, dragBarRect, new Vector2(0, 200)));

                tutorialBlocker?.SetAllowedTarget(null);
                VirtualMascot.Show(dialogs.step4_veryGood, windowRect, new Vector2(0, 200));
                yield return new WaitForSeconds(2.0f);

                // 5. Resize Window
                VirtualMascot.Show(dialogs.step5_resizeWindow, windowRect, new Vector2(250, 0));

                tutorialBlocker?.SetAllowedTarget(windowRect);
                // Exclusions for Close, Min, Max are still active

                Vector2 startSize = windowRect.sizeDelta;
                yield return StartCoroutine(WaitWithReminder(() => {
                    return windowRect == null || targetWindow.IsClosed || Vector2.Distance(startSize, windowRect.sizeDelta) > 20f;
                }, dialogs.step5_reminder, windowRect, new Vector2(250, 0)));

                // Clear exclusions
                tutorialBlocker?.SetExcludedTarget(null);
                tutorialBlocker?.SetAllowedTarget(null);

                VirtualMascot.Show(dialogs.step5_awesome, windowRect, new Vector2(250, 0));
                yield return new WaitForSeconds(2.5f);

                // 6. Maximize and Restore
                if (btnMax != null && targetWindow != null && !targetWindow.IsClosed)
                {
                    RectTransform maxRect = btnMax as RectTransform;
                    VirtualMascot.Show(dialogs.step6_maximize, maxRect, new Vector2(-250, 50));
                    tutorialBlocker?.SetAllowedTarget(maxRect);

                    yield return StartCoroutine(WaitWithReminder(() => {
                        return targetWindow == null || targetWindow.IsClosed || targetWindow.IsMaximized;
                    }, dialogs.step6_maximizeReminder, maxRect, new Vector2(-250, 50)));

                    tutorialBlocker?.SetAllowedTarget(null);
                    VirtualMascot.Show(dialogs.step6_restore, maxRect, new Vector2(-250, 50));
                    tutorialBlocker?.SetAllowedTarget(maxRect);

                    yield return StartCoroutine(WaitWithReminder(() => {
                        return targetWindow == null || targetWindow.IsClosed || !targetWindow.IsMaximized;
                    }, dialogs.step6_restoreReminder, maxRect, new Vector2(-250, 50)));

                    tutorialBlocker?.SetAllowedTarget(null);
                    VirtualMascot.Show(dialogs.step6_greatJob, windowRect, new Vector2(250, 0));
                    yield return new WaitForSeconds(2.0f);
                }

                // 7. Minimize and Restore from Taskbar
                if (btnMin != null && targetWindow != null && !targetWindow.IsClosed)
                {
                    RectTransform minRect = btnMin as RectTransform;
                    VirtualMascot.Show(dialogs.step7_minimize, minRect, new Vector2(-250, 50));
                    tutorialBlocker?.SetAllowedTarget(minRect);

                    yield return StartCoroutine(WaitWithReminder(() => {
                        return targetWindow == null || targetWindow.IsClosed || targetWindow.IsMinimized;
                    }, dialogs.step7_minimizeReminder, minRect, new Vector2(-250, 50)));

                    tutorialBlocker?.SetAllowedTarget(null);
                    yield return new WaitForSeconds(0.5f);

                    TaskbarWindowButton taskBtn = FindTaskbarButton(targetWindow);
                    if (taskBtn != null)
                    {
                        RectTransform taskRect = taskBtn.GetComponent<RectTransform>();
                        VirtualMascot.Show(dialogs.step7_taskbar, taskRect, new Vector2(100, 200));
                        tutorialBlocker?.SetAllowedTarget(taskRect);

                        yield return StartCoroutine(WaitWithReminder(() => {
                            return targetWindow == null || targetWindow.IsClosed || (!targetWindow.IsMinimized && targetWindow.IsFocused);
                        }, dialogs.step7_taskbarReminder, taskRect, new Vector2(100, 200)));

                        tutorialBlocker?.SetAllowedTarget(null);
                        VirtualMascot.Show(dialogs.step7_thereItIs, windowRect, new Vector2(250, 0));
                        yield return new WaitForSeconds(2.0f);
                    }
                }

                // 8. Close Window
                RectTransform closeBtnRect = btnClose as RectTransform ?? windowRect;
                VirtualMascot.Show(dialogs.step8_close, closeBtnRect, new Vector2(0, 200));
                tutorialBlocker?.SetAllowedTarget(closeBtnRect);

                yield return StartCoroutine(WaitWithReminder(() => {
                    return targetWindow == null || targetWindow.IsClosed;
                }, dialogs.step8_closeReminder, closeBtnRect, new Vector2(0, 200)));

                tutorialBlocker?.SetAllowedTarget(null);
            }

            // 9. Recycle Bin
            VirtualMascot.Show(dialogs.step9_recycle, binRect, new Vector2(250, 0));

            recycleBin.enabled = true; // Enable the recycle bin only now!

            bool recycled = false;
            activeRecycledListener = (icon) => { if (icon != null) recycled = true; };
            RecycleBinIcon.OnIconRecycled += activeRecycledListener;

            // Completely clear the blocker so both the icon and bin can be interacted with
            tutorialBlocker?.ClearTarget();

            yield return StartCoroutine(WaitWithReminder(() => recycled || appIcon == null,
                dialogs.step9_recycleReminder, binRect, new Vector2(250, 0)));

            if (activeRecycledListener != null)
            {
                RecycleBinIcon.OnIconRecycled -= activeRecycledListener;
                activeRecycledListener = null;
            }

            // Block input during final congratulations
            if (tutorialBlocker != null)
            {
                tutorialBlocker.gameObject.SetActive(true);
                tutorialBlocker.SetAllowedTarget(null);
            }

            VirtualMascot.Show(dialogs.step9_congrats, binRect, new Vector2(250, 0));
            yield return new WaitForSeconds(4.0f);

            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            IsTourRunning = false;
            if (tutorialBlocker != null)
            {
                tutorialBlocker.ClearTarget();
                Destroy(tutorialBlocker.gameObject);
                tutorialBlocker = null;
            }

            VirtualMascot.HideMascot();

            PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", 1);
            PlayerPrefs.SetInt("VirtualDesktopIntroCompleted", 1);
            PlayerPrefs.Save();

            Debug.Log("[AdvancedDesktopTour] Advanced Desktop Tour completed in place on VirtualDesktop!");
            Destroy(this);
        }

        private TaskbarWindowButton FindTaskbarButton(Window targetWindow)
        {
            TaskbarWindowButton[] taskbarButtons = UnityEngine.Object.FindObjectsByType<TaskbarWindowButton>();
            foreach (var btn in taskbarButtons)
            {
                if (btn != null && btn.TargetWindow == targetWindow) return btn;
            }
            return null;
        }

        private IEnumerator WaitWithReminder(Func<bool> condition, string reminderText, RectTransform targetObj, Vector2 offset)
        {
            float timer = 0f;
            while (!condition())
            {
                timer += Time.deltaTime;
                if (timer > 8.0f)
                {
                    VirtualMascot.Show(reminderText, targetObj, offset);
                    timer = 0f;
                }
                yield return null;
            }
        }
    }
}
