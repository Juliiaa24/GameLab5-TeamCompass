/**
 * Author: Diego
 * Date: 25/09/26
 * Description: A taskbar entry referencing its actual window.
*/
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    [RequireComponent(typeof(Button))]
    public class TaskbarWindowButton : MonoBehaviour
    {
        #region Public Variables
        public Window TargetWindow => window;
        #endregion

        #region Private Variables
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private Color normalColor = new Color(0.15f, 0.20f, 0.29f);
        [SerializeField] private Color activeColor = new Color(0.15f, 0.39f, 0.57f);
        [SerializeField] private Color minimizedColor = new Color(0.10f, 0.13f, 0.19f);
        private WindowManager manager;
        private Window window;
        #endregion

        #region Unity Methods
        private void Awake() { GetComponent<Button>().onClick.AddListener(Activate); }
        private void OnDestroy() { GetComponent<Button>().onClick.RemoveListener(Activate); }
        #endregion

        #region Public Methods
        public void Initialize(WindowManager owner, Window target)
        {
            manager = owner;
            window = target;
            Refresh();
        }

        public void Refresh()
        {
            if (window == null) return;
            if (title != null) title.text = window.Title;
            if (icon != null)
            {
                icon.sprite = window.Icon;
                icon.enabled = window.Icon != null;
            }
            if (background != null)
                background.color = window.IsFocused ? activeColor : window.IsMinimized ? minimizedColor : normalColor;
            if (activeIndicator != null) activeIndicator.SetActive(window.IsFocused);
        }
        #endregion

        #region Private Methods
        private void Activate()
        {
            if (manager != null) manager.ToggleFromTaskbar(window);
        }
        #endregion
    }
}
