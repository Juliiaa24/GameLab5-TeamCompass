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
        [SerializeField] private Color normalColor = new Color(0.89f, 0.95f, 1.00f, 1.00f);
        [SerializeField] private Color activeColor = new Color(0.14f, 0.50f, 0.90f, 1.00f);
        [SerializeField] private Color minimizedColor = new Color(0.84f, 0.89f, 0.95f, 1.00f);
        [SerializeField] private Color normalTextColor = new Color(0.015f, 0.035f, 0.085f, 1.00f);
        [SerializeField] private Color activeTextColor = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        [SerializeField] private Color minimizedTextColor = new Color(0.10f, 0.16f, 0.28f, 1.00f);
        private WindowManager manager;
        private Window window;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Activate);
            if (icon != null) icon.preserveAspect = true;
        }
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
            bool isFocused = window.IsFocused && !window.IsMinimized;
            bool isMinimized = window.IsMinimized;
            bool hasIcon = icon != null && window.Icon != null;

            if (title != null)
            {
                title.text = window.Title;
                title.color = isFocused ? activeTextColor : (isMinimized ? minimizedTextColor : normalTextColor);
                RectTransform titleRt = title.rectTransform;
                if (titleRt != null)
                {
                    titleRt.offsetMin = new Vector2(hasIcon ? 48f : 14f, 6f);
                }
            }
            if (icon != null)
            {
                icon.sprite = window.Icon;
                icon.enabled = hasIcon;
                icon.preserveAspect = true;
                icon.color = isMinimized ? new Color(1f, 1f, 1f, 0.72f) : Color.white;
            }
            if (background != null)
                background.color = isFocused ? activeColor : (isMinimized ? minimizedColor : normalColor);
            if (activeIndicator != null) activeIndicator.SetActive(isFocused);
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
