/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Right-click Context Menu controller for simulated OS desktop and file explorer.
*/
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ContextMenuController : MonoBehaviour
    {
        #region Public Variables
        private static ContextMenuController _instance;
        public static ContextMenuController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ContextMenuController>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public bool IsMenuOpen => menuPanel != null && menuPanel.gameObject.activeSelf;
        #endregion

        #region Private Variables
        [Header("UI References")]
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private Button btnNewFolder;
        [SerializeField] private Canvas targetCanvas;

        private Action onNewFolderCallback;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (menuPanel == null)
            {
                menuPanel = transform as RectTransform;
            }
        }

        private void Start()
        {
            if (btnNewFolder != null)
            {
                btnNewFolder.onClick.AddListener(OnNewFolderClicked);
            }

            Hide();
        }

        private void Update()
        {
            if (!IsMenuOpen) return;

            // Close on left click outside or right click elsewhere
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                bool leftPressed = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
                bool rightPressed = UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;

                if (leftPressed || rightPressed)
                {
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    if (menuPanel != null)
                    {
                        bool inside = RectTransformUtility.RectangleContainsScreenPoint(menuPanel, mousePos);
                        if (!inside)
                        {
                            Hide();
                        }
                    }
                }
            }

            // Close on Escape key
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Displays the context menu at the given screen coordinates.
        /// </summary>
        public void Show(Vector2 screenPosition, Action onNewFolderAction)
        {
            gameObject.SetActive(true);
            if (menuPanel == null) menuPanel = transform as RectTransform;
            if (menuPanel == null) return;

            onNewFolderCallback = onNewFolderAction;

            menuPanel.gameObject.SetActive(true);
            menuPanel.transform.SetAsLastSibling();

            PositionMenuAt(screenPosition);
        }

        /// <summary>
        /// Hides the context menu.
        /// </summary>
        public void Hide()
        {
            if (menuPanel != null)
            {
                menuPanel.gameObject.SetActive(false);
            }
            onNewFolderCallback = null;
        }
        #endregion

        #region Private Methods
        private void PositionMenuAt(Vector2 screenPosition)
        {
            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : transform.parent as RectTransform;
            if (canvasRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, targetCanvas != null ? targetCanvas.worldCamera : null, out Vector2 localPoint))
            {
                Vector2 menuSize = menuPanel.sizeDelta;
                Vector2 halfCanvas = canvasRect.sizeDelta * 0.5f;

                // Adjust positioning so the menu doesn't overflow screen borders
                float targetX = localPoint.x;
                float targetY = localPoint.y;

                if (targetX + menuSize.x > halfCanvas.x)
                {
                    targetX -= menuSize.x;
                }

                if (targetY - menuSize.y < -halfCanvas.y)
                {
                    targetY += menuSize.y;
                }

                menuPanel.anchoredPosition = new Vector2(targetX, targetY);
            }
        }

        private void OnNewFolderClicked()
        {
            Action callback = onNewFolderCallback;
            Hide();
            callback?.Invoke();
        }
        #endregion
    }
}
