/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Right-click Context Menu controller for simulated OS desktop and file explorer.
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        #endregion

        #region Private Variables
        [Header("UI References")]
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private Button btnNewFolder;
        [SerializeField] private Canvas targetCanvas;

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
                        Camera cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
                        bool inside = RectTransformUtility.RectangleContainsScreenPoint(menuPanel, mousePos, cam);
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
        /// Opens a clean modal dialog on the main Canvas to prompt the user for a name (Rename / Save As).
        /// </summary>
        public void ShowInputModal(string title, string initialValue, string confirmText, Action<string> onConfirm)
        {
            Hide();

            if (activeModalObject != null)
            {
                Destroy(activeModalObject);
            }

            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null) targetCanvas = FindAnyObjectByType<Canvas>();
            if (targetCanvas == null) return;

            TMP_FontAsset fontAsset = null;
            if (btnNewFolder != null)
            {
                var tmp = btnNewFolder.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) fontAsset = tmp.font;
            }

            // Fullscreen blocker overlay
            activeModalObject = new GameObject("ModalDialogOverlay", typeof(RectTransform), typeof(Image));
            activeModalObject.transform.SetParent(targetCanvas.transform, false);
            activeModalObject.transform.SetAsLastSibling();

            RectTransform overlayRt = activeModalObject.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image overlayImg = activeModalObject.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.45f);
            overlayImg.raycastTarget = true;

            // Dialog box
            GameObject dialogGo = new GameObject("DialogPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            dialogGo.transform.SetParent(activeModalObject.transform, false);
            RectTransform dialogRt = dialogGo.GetComponent<RectTransform>();
            dialogRt.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.pivot = new Vector2(0.5f, 0.5f);
            dialogRt.sizeDelta = new Vector2(360f, 185f);
            dialogRt.anchoredPosition = Vector2.zero;

            Image dialogImg = dialogGo.GetComponent<Image>();
            dialogImg.color = new Color(0.14f, 0.18f, 0.27f, 0.98f);

            Outline dialogOutline = dialogGo.GetComponent<Outline>();
            dialogOutline.effectColor = new Color(0.38f, 0.52f, 0.75f, 0.8f);
            dialogOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title Label
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(dialogGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(-32f, 30f);

            TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleTmp.font = fontAsset;
            titleTmp.text = title;
            titleTmp.fontSize = 16f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Left;

            // InputField Background
            GameObject inputGo = new GameObject("NameInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(dialogGo.transform, false);
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0f, 0.5f);
            inputRt.anchorMax = new Vector2(1f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 0.5f);
            inputRt.anchoredPosition = new Vector2(0f, 6f);
            inputRt.sizeDelta = new Vector2(-32f, 40f);

            Image inputBg = inputGo.GetComponent<Image>();
            inputBg.color = new Color(0.95f, 0.97f, 1f, 1f);

            // Text Area inside InputField
            GameObject textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaGo.transform.SetParent(inputGo.transform, false);
            RectTransform textAreaRt = textAreaGo.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(10f, 4f);
            textAreaRt.offsetMax = new Vector2(-10f, -4f);

            GameObject inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextGo.transform.SetParent(textAreaGo.transform, false);
            RectTransform inputTextRt = inputTextGo.GetComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = Vector2.zero;
            inputTextRt.offsetMax = Vector2.zero;

            TextMeshProUGUI inputTmp = inputTextGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) inputTmp.font = fontAsset;
            inputTmp.fontSize = 15f;
            inputTmp.color = new Color(0.1f, 0.14f, 0.22f, 1f);
            inputTmp.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_InputField inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.textViewport = textAreaRt;
            inputField.textComponent = inputTmp;
            inputField.targetGraphic = inputBg;
            inputField.text = initialValue ?? string.Empty;

            // Buttons row
            Button btnCancel = CreateModalButton(dialogGo.transform, "Cancel", new Vector2(-75f, 20f), new Color(0.32f, 0.36f, 0.44f, 1f), fontAsset);
            Button btnConfirm = CreateModalButton(dialogGo.transform, string.IsNullOrEmpty(confirmText) ? "Save" : confirmText, new Vector2(75f, 20f), new Color(0.18f, 0.55f, 0.88f, 1f), fontAsset);

            Action submitAction = () =>
            {
                string val = inputField != null ? inputField.text.Trim() : string.Empty;
                if (!string.IsNullOrEmpty(val))
                {
                    if (activeModalObject != null) Destroy(activeModalObject);
                    activeModalObject = null;
                    onConfirm?.Invoke(val);
                }
            };

            btnCancel.onClick.AddListener(() =>
            {
                if (activeModalObject != null) Destroy(activeModalObject);
                activeModalObject = null;
            });

            btnConfirm.onClick.AddListener(() => submitAction());
            inputField.onSubmit.AddListener(_ => submitAction());

            // Focus input field
            inputField.ActivateInputField();
            inputField.Select();
        }
        #endregion

        #region Private Methods
        private Button CreateModalButton(Transform parent, string labelText, Vector2 anchoredPos, Color bgColor, TMP_FontAsset fontAsset)
        {
            GameObject btnGo = new GameObject($"Btn_{labelText}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(125f, 34f);

            Image img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = labelText;
            tmp.fontSize = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btnGo.GetComponent<Button>();
        }

        private void RebuildMenuButtons(List<ContextMenuItem> items)
        {
            if (btnNewFolder != null)
            {
                btnNewFolder.gameObject.SetActive(false);
            }

            // Ensure pool has enough buttons
            while (spawnedButtons.Count < items.Count)
            {
                Button newBtn = CreateMenuButton();
                spawnedButtons.Add(newBtn);
            }

            const float itemHeight = 34f;
            const float spacing = 4f;
            const float padding = 6f;
            const float menuWidth = 220f;

            float totalHeight = padding * 2f + items.Count * itemHeight + Mathf.Max(0, items.Count - 1) * spacing;
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
                    float yOffset = -padding - i * (itemHeight + spacing);
                    rt.anchoredPosition = new Vector2(0f, yOffset);
                    rt.sizeDelta = new Vector2(-padding * 2f, itemHeight);

                    TextMeshProUGUI labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (labelTmp != null)
                    {
                        labelTmp.text = item.label;
                        labelTmp.color = item.isEnabled ? Color.white : new Color(0.6f, 0.65f, 0.72f, 0.55f);
                        labelTmp.fontSize = 13f;
                        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
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

        private Button CreateMenuButton()
        {
            if (btnNewFolder != null)
            {
                Button clone = Instantiate(btnNewFolder, menuPanel);
                clone.name = "ContextMenuButton";
                clone.onClick.RemoveAllListeners();
                return clone;
            }

            GameObject btnGo = new GameObject("ContextMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(menuPanel, false);
            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.35f, 0.85f);

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 13f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return btnGo.GetComponent<Button>();
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
