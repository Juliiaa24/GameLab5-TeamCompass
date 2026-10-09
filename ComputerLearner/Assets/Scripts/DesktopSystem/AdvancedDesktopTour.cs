/**
 * Author: Julia Vera
 * Date: 05/10/26
 * Description: 
*/
using UnityEngine;
using System.Collections;
using System;

namespace ComputerLearning
{
    public class AdvancedDesktopTour : MonoBehaviour
    {
        private TutorialBlocker tutorialBlocker;

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
            SetupTutorialBlocker();
            yield return new WaitForSeconds(0.5f);

            DraggableIcon appIcon = null;
            DraggableIcon[] allIcons = UnityEngine.Object.FindObjectsByType<DraggableIcon>(FindObjectsSortMode.None);
            foreach (var i in allIcons)
            {
                if (i.GetComponent<RecycleBinIcon>() == null)
                {
                    appIcon = i;
                    break;
                }
            }

            RecycleBinIcon recycleBin = UnityEngine.Object.FindFirstObjectByType<RecycleBinIcon>();

            if (appIcon == null || recycleBin == null)
            {
                Debug.LogWarning("[AdvancedDesktopTour] Missing icons for tutorial!");
                yield break;
            }

            // Disable the recycle bin initially so the user doesn't accidentally use it early
            recycleBin.enabled = false;

            RectTransform iconRect = appIcon.GetComponent<RectTransform>();
            RectTransform binRect = recycleBin.GetComponent<RectTransform>();

            // 1. Welcome
            VirtualMascot.Show("Welcome back! Now that you're a task expert, let's learn how to organize your desktop.", iconRect, new Vector2(250, 0));
            yield return new WaitForSeconds(4.0f);

            // 2. Move Icon
            appIcon.AllowDoubleClick = false; // Block opening it by accident
            VirtualMascot.Show("You can drag icons to organize them. Try moving this application to another spot!", iconRect, new Vector2(250, 0));
            tutorialBlocker?.SetAllowedTarget(iconRect);
            
            Vector3 startIconPos = iconRect.position;
            yield return StartCoroutine(WaitWithReminder(() => Vector3.Distance(startIconPos, iconRect.position) > 50f, 
                "Click and hold the left mouse button on the icon to drag it.", iconRect, new Vector2(250, 0)));
            
            VirtualMascot.Show("Perfect!", iconRect, new Vector2(250, 0));
            yield return new WaitForSeconds(2.0f);

            // 3. Open Window
            appIcon.AllowDoubleClick = true;
            VirtualMascot.Show("Now, open the application by double-clicking on it.", iconRect, new Vector2(250, 0));
            tutorialBlocker?.SetAllowedTarget(iconRect);
            yield return StartCoroutine(WaitWithReminder(() => appIcon.AppWindow != null && !appIcon.AppWindow.IsClosed, 
                "Remember: double-click quickly to open it.", iconRect, new Vector2(250, 0)));

            Window targetWindow = appIcon.AppWindow;
            RectTransform windowRect = targetWindow.GetComponent<RectTransform>();
            RectTransform dragBarRect = targetWindow.transform.Find("WindowTop") as RectTransform ?? targetWindow.transform.Find("TopBar") as RectTransform;
            if (dragBarRect == null) dragBarRect = windowRect; // Fallback

            // Prepare button exclusions
            Transform btnClose = windowRect.Find("WindowTop/Buttons/Close") ?? windowRect.Find("TopBar/Buttons/CloseButton") ?? windowRect.Find("Close");
            Transform btnMin = windowRect.Find("WindowTop/Buttons/Minimize") ?? windowRect.Find("TopBar/Buttons/MinimizeButton");
            Transform btnMax = windowRect.Find("WindowTop/Buttons/Maximize") ?? windowRect.Find("TopBar/Buttons/MaximizeButton");

            // 4. Move Window
            VirtualMascot.Show("Great! Windows can also be moved. Drag the top bar to move it.", windowRect, new Vector2(0, 200));
            tutorialBlocker?.SetAllowedTarget(dragBarRect);
            
            tutorialBlocker?.SetExcludedTarget(null); // Clear previous
            if (btnClose != null) tutorialBlocker?.AddExcludedTarget(btnClose as RectTransform);
            if (btnMin != null) tutorialBlocker?.AddExcludedTarget(btnMin as RectTransform);
            if (btnMax != null) tutorialBlocker?.AddExcludedTarget(btnMax as RectTransform);

            Vector3 startWinPos = windowRect.position;
            yield return StartCoroutine(WaitWithReminder(() => {
                return Vector3.Distance(startWinPos, windowRect.position) > 50f;
            }, "Click on the top bar and drag the window.", dragBarRect, new Vector2(0, 200)));

            VirtualMascot.Show("Very good!", windowRect, new Vector2(0, 200));
            yield return new WaitForSeconds(2.0f);

            // 5. Resize Window
            VirtualMascot.Show("If you move the mouse to the edges, you can make it larger or smaller. Try it!", windowRect, new Vector2(250, 0));
            
            tutorialBlocker?.SetAllowedTarget(windowRect);
            // The exclusions for Close, Min, Max are still active from step 4!

            Vector2 startSize = windowRect.sizeDelta;
            yield return StartCoroutine(WaitWithReminder(() => {
                return Vector2.Distance(startSize, windowRect.sizeDelta) > 20f;
            }, "Drag from the corners or edges to resize the window.", windowRect, new Vector2(250, 0)));

            // Remove exclusions
            tutorialBlocker?.SetExcludedTarget(null);

            VirtualMascot.Show("Awesome! You know how to manage windows now.", windowRect, new Vector2(250, 0));
            yield return new WaitForSeconds(3.0f);

            // 6. Close Window
            VirtualMascot.Show("Close the window to continue.", windowRect, new Vector2(0, 200));
            if (btnClose != null) tutorialBlocker?.SetAllowedTarget(btnClose as RectTransform);
            else tutorialBlocker?.SetAllowedTarget(windowRect);

            yield return StartCoroutine(WaitWithReminder(() => {
                return targetWindow.IsClosed;
            }, "Click the red X to close the window.", windowRect, new Vector2(0, 200)));

            // 7. Recycle Bin
            VirtualMascot.Show("Finally, if you don't need something anymore, you can throw it in the Recycle Bin. Drag the icon there!", binRect, new Vector2(250, 0));
            
            recycleBin.enabled = true; // Enable the recycle bin only now!
            
            bool recycled = false;
            Action<DraggableIcon> onRecycled = (icon) => { if (icon == appIcon) recycled = true; };
            RecycleBinIcon.OnIconRecycled += onRecycled;

            // Completely disable the blocker so both the icon and bin can be interacted with
            tutorialBlocker?.ClearTarget();

            yield return StartCoroutine(WaitWithReminder(() => recycled, 
                "Drag the application icon over the recycle bin and drop it.", binRect, new Vector2(250, 0)));

            RecycleBinIcon.OnIconRecycled -= onRecycled;

            VirtualMascot.Show("Congratulations! You have completed your advanced desktop training.", binRect, new Vector2(250, 0));
            yield return new WaitForSeconds(4.0f);
            
            VirtualMascot.HideMascot();

            // Load the transition scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("PostTutorialScene");
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
