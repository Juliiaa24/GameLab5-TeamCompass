/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Right-click Context Menu controller for simulated OS desktop and file explorer using Inspector-configured UI & prefabs.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    [Serializable]
    public struct ContextMenuItem
    {
        public string label;
        public Action action;
        public bool isEnabled;

        public ContextMenuItem(string label, Action action, bool isEnabled = true)
        {
            this.label = label;
            this.action = action;
            this.isEnabled = isEnabled;
        }
    }

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
        public GameObject InputModalPrefab => inputModalPrefab;
        #endregion

        #region Private Variables
        [Header("UI References")]
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private Button btnNewFolder;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject inputModalPrefab;

        [Header("Menu Layout Settings")]
        [SerializeField] private float itemHeight = 32f;
        [SerializeField] private float itemSpacing = 2f;
        [SerializeField] private float menuPadding = 5f;
        [SerializeField] private float menuWidth = 228f;
        [SerializeField] private Color enabledTextColor = new Color(0.12f, 0.15f, 0.20f, 1f);
        [SerializeField] private Color disabledTextColor = new Color(0.55f, 0.58f, 0.63f, 0.65f);

        private Action onNewFolderCallback;
        private readonly List<Button> spawnedButtons = new List<Button>();
        private GameObject activeModalObject;
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

            if (btnNewFolder != null)
            {
                btnNewFolder.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Hide();
        }

        private void Update()
        {
            if (!IsMenuOpen) return;

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                bool leftPressed = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
                bool rightPressed = UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;

                if (leftPressed || rightPressed)
                {
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    if (menuPanel != null)
                    {
                        Camera cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
                        bool inside = RectTransformUtility.RectangleContainsScreenPoint(menuPanel, mousePos, cam);
                        if (!inside)
                        {
                            Hide();
                        }
                    }
                }
            }

            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Backward-compatible single-action context menu display.
        /// </summary>
        public void Show(Vector2 screenPosition, Action onNewFolderAction)
        {
            onNewFolderCallback = onNewFolderAction;
            var items = new List<ContextMenuItem>
            {
                new ContextMenuItem("+ New Folder", onNewFolderAction, true)
            };
            Show(screenPosition, items);
        }

        /// <summary>
        /// Displays the context menu at the given screen coordinates with a dynamic list of options.
        /// </summary>
        public void Show(Vector2 screenPosition, List<ContextMenuItem> items)
        {
            if (items == null || items.Count == 0) return;

            gameObject.SetActive(true);
            if (menuPanel == null) menuPanel = transform as RectTransform;
            if (menuPanel == null) return;

            RebuildMenuButtons(items);

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

        /// <summary>
        /// Instantiates the InputModalDialog prefab on the main Canvas to prompt the user for a name (Rename / Save As).
        /// </summary>
        public void ShowInputModal(string title, string initialValue, string confirmText, Action<string> onConfirm)
        {
            Hide();

            if (activeModalObject != null)
            {
                Destroy(activeModalObject);
                activeModalObject = null;
            }

            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null) targetCanvas = FindAnyObjectByType<Canvas>();
            if (targetCanvas == null) return;

            if (inputModalPrefab == null)
            {
                inputModalPrefab = Resources.Load<GameObject>("InputModalDialog");
#if UNITY_EDITOR
                if (inputModalPrefab == null)
                {
                    inputModalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/InputModalDialog.prefab");
                }
#endif
            }

            if (inputModalPrefab == null)
            {
                Debug.LogWarning("[ContextMenuController] inputModalPrefab is not assigned in Inspector.");
                onConfirm?.Invoke(initialValue);
                return;
            }

            activeModalObject = Instantiate(inputModalPrefab, targetCanvas.transform);
            activeModalObject.transform.SetAsLastSibling();

            InputModalDialog modal = activeModalObject.GetComponent<InputModalDialog>();
            if (modal != null)
            {
                modal.Initialize(
                    title,
                    initialValue,
                    confirmText,
                    (enteredValue) =>
                    {
                        activeModalObject = null;
                        onConfirm?.Invoke(enteredValue);
                    },
                    () =>
                    {
                        activeModalObject = null;
                    }
                );
            }
        }
        #endregion

        #region Private Methods
        private void RebuildMenuButtons(List<ContextMenuItem> items)
        {
            if (btnNewFolder != null)
            {
                btnNewFolder.gameObject.SetActive(false);
            }

            while (spawnedButtons.Count < items.Count)
            {
                if (btnNewFolder == null) break;
                Button newBtn = Instantiate(btnNewFolder, menuPanel);
                newBtn.name = $"ContextMenuButton_{spawnedButtons.Count}";
                spawnedButtons.Add(newBtn);
            }

            float totalHeight = menuPadding * 2f + items.Count * itemHeight + Mathf.Max(0, items.Count - 1) * itemSpacing;
            menuPanel.sizeDelta = new Vector2(menuWidth, totalHeight);

            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                Button btn = spawnedButtons[i];
                if (btn == null) continue;

                if (i < items.Count)
                {
                    ContextMenuItem item = items[i];
                    btn.gameObject.SetActive(true);

                    RectTransform rt = btn.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    float yOffset = -menuPadding - i * (itemHeight + itemSpacing);
                    rt.anchoredPosition = new Vector2(0f, yOffset);
                    rt.sizeDelta = new Vector2(-menuPadding * 2f, itemHeight);

                    TextMeshProUGUI labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (labelTmp != null)
                    {
                        labelTmp.text = item.label;
                        labelTmp.color = item.isEnabled ? enabledTextColor : disabledTextColor;
                    }

                    btn.interactable = item.isEnabled;
                    btn.onClick.RemoveAllListeners();
                    if (item.isEnabled && item.action != null)
                    {
                        Action callback = item.action;
                        btn.onClick.AddListener(() =>
                        {
                            Hide();
                            callback.Invoke();
                        });
                    }
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        private void PositionMenuAt(Vector2 screenPosition)
        {
            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : transform.parent as RectTransform;
            if (canvasRect == null) return;

            Camera cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
            {
                Vector2 menuSize = menuPanel.sizeDelta;
                Vector2 halfCanvas = canvasRect.sizeDelta * 0.5f;

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
        #endregion
    }
}
