/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Observe WindowManager and display one button per real window.
*/
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public class Taskbar : MonoBehaviour
    {
        #region Public Variables
        public int EntryCount => buttons.Count;
        #endregion

        #region Private Variables
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private RectTransform buttonsContainer;
        [SerializeField] private TaskbarWindowButton buttonPrefab;
        private readonly Dictionary<Window, TaskbarWindowButton> buttons = new Dictionary<Window, TaskbarWindowButton>();
        private readonly List<Window> removed = new List<Window>();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (windowManager == null) windowManager = GetComponentInParent<WindowManager>();
            if (windowManager == null) return;
            windowManager.WindowsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (windowManager != null) windowManager.WindowsChanged -= Refresh;
        }
        #endregion

        #region Private Methods
        private void Refresh()
        {
            if (windowManager == null || buttonsContainer == null || buttonPrefab == null) return;
            removed.Clear();
            foreach (var pair in buttons)
                if (pair.Key == null || pair.Key.IsClosed || !ContainsWindow(pair.Key)) removed.Add(pair.Key);
            foreach (Window window in removed)
            {
                buttons[window].gameObject.SetActive(false);
                Destroy(buttons[window].gameObject);
                buttons.Remove(window);
            }

            foreach (Window window in windowManager.Windows)
            {
                if (window == null || window.IsClosed) continue;
                if (!buttons.TryGetValue(window, out TaskbarWindowButton button))
                {
                    button = Instantiate(buttonPrefab, buttonsContainer);
                    button.Initialize(windowManager, window);
                    buttons.Add(window, button);
                }
                button.Refresh();
            }
        }

        private bool ContainsWindow(Window window)
        {
            foreach (Window candidate in windowManager.Windows)
                if (candidate == window) return true;
            return false;
        }
        #endregion
    }
}
