/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Shared window lifecycle and focus coordination.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>Coordinates real windows; icons retain their single-instance reference.</summary>
    [DefaultExecutionOrder(-100)]
    public class WindowManager : MonoBehaviour
    {
        #region Public Variables
        public static WindowManager Instance { get; private set; }
        public IReadOnlyList<Window> Windows => windows;
        public Window ActiveWindow { get; private set; }
        public RectTransform WindowsContainer => windowsContainer;

        public event Action WindowsChanged;
        #endregion

        #region Private Variables
        [SerializeField] private RectTransform windowsContainer;

        private readonly List<Window> windows = new List<Window>();
        #endregion

        #region Unity Methods
        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (Window window in windows)
                if (window != null) window.StateChanged -= OnWindowChanged;
        }
        #endregion

        #region Public Methods
        public Window OpenWindow(GameObject prefab, Window existing = null)
        {
            if (existing != null && !existing.IsClosed)
            {
                existing.Restore();
                return existing;
            }

            if (prefab == null || prefab.GetComponent<Window>() == null) return null;

            Window window = Instantiate(prefab, windowsContainer != null ? windowsContainer : transform)
                .GetComponent<Window>();

            RegisterWindow(window);

            window.Restore();
            return window;
        }

        public void RegisterWindow(Window window)
        {
            if (window == null || window.IsClosed || windows.Contains(window)) return;

            windows.Add(window);
            window.StateChanged += OnWindowChanged;
            window.SetManager(this);
            WindowsChanged?.Invoke();
        }

        public void UnregisterWindow(Window window)
        {
            if (!windows.Remove(window)) return;
            window.StateChanged -= OnWindowChanged;
            if (ActiveWindow == window) FocusTopWindow();
            WindowsChanged?.Invoke();
        }

        public void FocusWindow(Window window)
        {
            if (window == null || window.IsClosed || window.IsMinimized) return;
            RegisterWindow(window);
            window.transform.SetAsLastSibling();
            ActiveWindow = window;
            WindowsChanged?.Invoke();
        }

        public void ToggleFromTaskbar(Window window)
        {
            if (window == null || window.IsClosed) return;
            if (window.IsMinimized) window.Restore();
            else if (ActiveWindow == window) window.Minimize();
            else FocusWindow(window);
        }
        #endregion

        #region Private Methods
        private void OnWindowChanged(Window window)
        {
            if (window.IsClosed) { UnregisterWindow(window); return; }
            if (window == ActiveWindow && window.IsMinimized) FocusTopWindow();
            WindowsChanged?.Invoke();
        }

        private void FocusTopWindow()
        {
            ActiveWindow = null;
            foreach (Window candidate in windows)
            {
                if (candidate == null || candidate.IsClosed || candidate.IsMinimized) continue;
                if (ActiveWindow == null || candidate.transform.GetSiblingIndex() > ActiveWindow.transform.GetSiblingIndex())
                    ActiveWindow = candidate;
            }
        }
        #endregion
    }
}
