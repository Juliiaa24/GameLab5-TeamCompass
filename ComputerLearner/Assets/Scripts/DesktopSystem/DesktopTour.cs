using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System;

namespace ComputerLearning
{
    [Serializable]
    public class DesktopTourDialogs
    {
        public string step1_welcome;
        public string step1_lookAround;
        public string step2_desktop;
        public string step2_apps;
        public string step3_openApp;
        public string step3_reminder;
        public string step4_opened;
        public string step5_window;
        public string step5_move;
        public string step6_drag;
        public string step6_reminder;
        public string step6_nice;
        public string step7_maximize;
        public string step7_reminder;
        public string step7_wow;
        public string step8_restore;
        public string step8_reminder;
        public string step8_perfect;
        public string step9_minimize;
        public string step9_reminder;
        public string step10_lookTaskbar;
        public string step10_clickTaskbar;
        public string step10_reminder;
        public string step10_found;
        public string step11_close;
        public string step11_reminder;
        public string step12_great;
        public string step12_ready;
        public string error_minimized;
    }

    public class DesktopTour : MonoBehaviour
    {
        public event Action OnIntroductionCompleted;
        public bool forcePlayTutorial = true; // Added for testing
        
        public static bool IsTourRunning { get; private set; }
        
        private DesktopTourDialogs dialogs;
        private TutorialBlocker tutorialBlocker;

        private void LoadDialogs()
        {
            TextAsset json = Resources.Load<TextAsset>("DesktopTourDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<DesktopTourDialogs>(json.text);
            else dialogs = new DesktopTourDialogs(); // Fallback empty
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

        private IEnumerator Start()
        {
            if (!forcePlayTutorial && PlayerPrefs.GetInt("DesktopTourCompleted", 0) == 1)
            {
                OnIntroductionCompleted?.Invoke();
                yield break; 
            }

            IsTourRunning = true;
            LoadDialogs();
            SetupTutorialBlocker();

            // Give time for UI layout
            yield return new WaitForSeconds(0.5f);

            // Find the Application Icon explicitly first
            DraggableIcon level1Icon = DesktopManager.Instance?.GetIconByLevelId("level1");
            if (level1Icon == null) 
            {
                level1Icon = UnityEngine.Object.FindAnyObjectByType<DraggableIcon>();
            }
            
            if (level1Icon == null) 
            {
                Debug.LogWarning("[DesktopTour] Could not find any DraggableIcon to start the tour!");
                yield break;
            }

            RectTransform iconRect = level1Icon.GetComponent<RectTransform>();

            // STEP 1 — Welcome
            VirtualMascot.Show(dialogs.step1_welcome, iconRect, new Vector2(250, -50));
            yield return new WaitForSeconds(3.0f);
            
            VirtualMascot.Show(dialogs.step1_lookAround, iconRect, new Vector2(250, -50));
            yield return new WaitForSeconds(3.0f);

            // STEP 2 — Desktop
            RectTransform desktopRect = (iconRect.parent as RectTransform) ?? iconRect;
            VirtualMascot.Show(dialogs.step2_desktop, desktopRect, new Vector2(0, 150));
            yield return new WaitForSeconds(3.0f);

            VirtualMascot.Show(dialogs.step2_apps, iconRect, new Vector2(160, -80));
            yield return new WaitForSeconds(3.0f);

            // STEP 3 — Open Application (Double Click)
            VirtualMascot.Show(dialogs.step3_openApp, iconRect, new Vector2(160, -80));
            DraggableIcon dragIcon = level1Icon.GetComponent<DraggableIcon>();
            
            
            tutorialBlocker?.SetAllowedTarget(iconRect);
            yield return StartCoroutine(WaitWithReminder(() => dragIcon.AppWindow != null && !dragIcon.AppWindow.IsClosed, dialogs.step3_reminder, iconRect, new Vector2(160, -80)));
            
            
            tutorialBlocker?.SetAllowedTarget(null);
            Window targetWindow = dragIcon.AppWindow;

            RectTransform windowRect = targetWindow.GetComponent<RectTransform>();
            VirtualMascot.Show(dialogs.step4_opened, windowRect, new Vector2(250, 0));
            yield return new WaitForSeconds(2.0f);

            // STEP 5 — What is a window
            if (IsWindowClosedEarly(targetWindow)) yield break;
            VirtualMascot.Show(dialogs.step5_window, windowRect, new Vector2(250, 0));
            yield return new WaitForSeconds(3.0f);

            RectTransform titleBarRect = targetWindow.TitleBar != null ? targetWindow.TitleBar : windowRect;

            // STEP 7 — Maximize
            RectTransform maxBtnRect = targetWindow.MaximizeButton != null ? targetWindow.MaximizeButton : titleBarRect;
            
            VirtualMascot.Show(dialogs.step7_maximize, maxBtnRect, new Vector2(-250, 50));
            
            
            tutorialBlocker?.SetAllowedTarget(maxBtnRect);
            yield return StartCoroutine(WaitWithReminder(
                () => targetWindow == null || targetWindow.IsClosed || targetWindow.IsMaximized, 
                dialogs.step7_reminder, 
                maxBtnRect, 
                new Vector2(-250, 50),
                targetWindow));

            
            tutorialBlocker?.SetAllowedTarget(null);
            if (IsWindowClosedEarly(targetWindow)) yield break;
            
            VirtualMascot.Show(dialogs.step7_wow, windowRect, new Vector2(0, -200));
            yield return new WaitForSeconds(2.5f);

            // STEP 8 — Restore
            VirtualMascot.Show(dialogs.step8_restore, maxBtnRect, new Vector2(-250, 50));
            
            
            tutorialBlocker?.SetAllowedTarget(maxBtnRect);
            yield return StartCoroutine(WaitWithReminder(
                () => targetWindow == null || targetWindow.IsClosed || !targetWindow.IsMaximized, 
                dialogs.step8_reminder, 
                maxBtnRect, 
                new Vector2(-250, 50),
                targetWindow));

            
            tutorialBlocker?.SetAllowedTarget(null);
            if (IsWindowClosedEarly(targetWindow)) yield break;

            VirtualMascot.Show(dialogs.step8_perfect, windowRect, new Vector2(250, 0));
            yield return new WaitForSeconds(2.0f);

            // STEP 9 — Minimize
            RectTransform minBtnRect = targetWindow.MinimizeButton != null ? targetWindow.MinimizeButton : titleBarRect;

            VirtualMascot.Show(dialogs.step9_minimize, minBtnRect, new Vector2(-250, 50));
            
            
            tutorialBlocker?.SetAllowedTarget(minBtnRect);
            // Do NOT pass associatedWindow here, because getting minimized is the goal of this step!
            yield return StartCoroutine(WaitWithReminder(
                () => targetWindow == null || targetWindow.IsClosed || targetWindow.IsMinimized, 
                dialogs.step9_reminder, 
                minBtnRect, 
                new Vector2(-250, 50)));
                
            
            tutorialBlocker?.SetAllowedTarget(null);
            if (IsWindowClosedEarly(targetWindow)) yield break;
            
            // Wait a brief moment for the minimize animation
            yield return new WaitForSeconds(0.5f);

            // STEP 10 — Taskbar
            TaskbarWindowButton targetBtn = FindTaskbarButton(targetWindow);

            if (targetBtn != null)
            {
                RectTransform targetBtnRect = targetBtn.GetComponent<RectTransform>();
                VirtualMascot.Show(dialogs.step10_lookTaskbar, targetBtnRect, new Vector2(100, 200));
                yield return new WaitForSeconds(3.0f);
                
                VirtualMascot.Show(dialogs.step10_clickTaskbar, targetBtnRect, new Vector2(100, 200));
                
                
                tutorialBlocker?.SetAllowedTarget(targetBtnRect);
                yield return StartCoroutine(WaitWithReminder(
                    () => targetWindow == null || targetWindow.IsClosed || (!targetWindow.IsMinimized && targetWindow.IsFocused), 
                    dialogs.step10_reminder, 
                    targetBtnRect, 
                    new Vector2(100, 200)));

                tutorialBlocker?.SetAllowedTarget(null);
                
                if (IsWindowClosedEarly(targetWindow)) yield break;

                VirtualMascot.Show(dialogs.step10_found, windowRect, new Vector2(250, 0));
                yield return new WaitForSeconds(2.5f);
            }

            // STEP 11 — Close Window
            RectTransform closeBtnRect = targetWindow.CloseButton != null ? targetWindow.CloseButton : titleBarRect;

            VirtualMascot.Show(dialogs.step11_close, closeBtnRect, new Vector2(-250, 50));
            
            
            tutorialBlocker?.SetAllowedTarget(closeBtnRect);
            yield return StartCoroutine(WaitWithReminder(
                () => targetWindow == null || targetWindow.IsClosed, 
                dialogs.step11_reminder, 
                closeBtnRect, 
                new Vector2(-250, 50),
                targetWindow));

            tutorialBlocker?.SetAllowedTarget(null);
            
            VirtualMascot.Show(dialogs.step12_great, iconRect, new Vector2(160, -80));
            yield return new WaitForSeconds(3.0f);

            VirtualMascot.Show(dialogs.step12_ready, iconRect, new Vector2(160, -80));
            yield return new WaitForSeconds(3.0f);
            
            VirtualMascot.HideMascot();

            PlayerPrefs.SetInt("DesktopTourCompleted", 1);
            PlayerPrefs.Save();

            IsTourRunning = false;
            OnIntroductionCompleted?.Invoke();

            // Transition to the second desktop scene for advanced levels
            UnityEngine.SceneManagement.SceneManager.LoadScene("SecondDesktopScene");
        }

        private TaskbarWindowButton FindTaskbarButton(Window targetWindow)
        {
            TaskbarWindowButton[] taskbarButtons = UnityEngine.Object.FindObjectsByType<TaskbarWindowButton>(FindObjectsSortMode.None);
            foreach(var btn in taskbarButtons) 
            {
                if (btn.TargetWindow == targetWindow) return btn;
            }
            return null;
        }

        private IEnumerator WaitWithReminder(Func<bool> condition, string reminderMessage, RectTransform target, Vector2 offset, Window associatedWindow = null)
        {
            float timer = 0f;
            while (!condition())
            {
                // If they accidentally minimized the window while trying to do something else
                if (associatedWindow != null && associatedWindow.IsMinimized)
                {
                    TaskbarWindowButton btn = FindTaskbarButton(associatedWindow);
                    if (btn != null)
                    {
                        VirtualMascot.Show(dialogs.error_minimized, btn.GetComponent<RectTransform>(), new Vector2(100, 200));
                        yield return new WaitUntil(() => !associatedWindow.IsMinimized);
                        // Once restored, go back to pointing at the original target
                        VirtualMascot.Show(reminderMessage, target, offset);
                        timer = 0f;
                    }
                }

                timer += Time.deltaTime;
                if (timer > 6f)
                {
                    VirtualMascot.Show(reminderMessage, target, offset);
                    timer = 0f;
                }
                yield return null;
            }
        }

        private bool IsWindowClosedEarly(Window window)
        {
            if (window == null || window.IsClosed)
            {
                VirtualMascot.HideMascot();
                IsTourRunning = false;
                return true;
            }
            return false;
        }
    }

    public class InteractionTracker : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public bool Clicked { get; private set; }
        
        public void OnPointerDown(PointerEventData eventData) 
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked = true; 
        }
        
        public void OnPointerClick(PointerEventData eventData) 
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked = true;
        }
    }
}



