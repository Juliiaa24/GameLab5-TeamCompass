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
        public static event Action OnContextMenuOpened;
        public static event Action<string> OnMenuItemClicked;
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
        private GameObject activeColorPaletteObject;
        #endregion

        #region Unity Methods
        private void Awake()
        {
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

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            Hide();
        }

        private void Update()
        {
            if (activeColorPaletteObject != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Destroy(activeColorPaletteObject);
                    activeColorPaletteObject = null;
                }
                else if (UnityEngine.InputSystem.Mouse.current != null &&
                         (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame || UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame))
                {
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    Camera cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
                    RectTransform palRect = activeColorPaletteObject.GetComponent<RectTransform>();
                    if (palRect != null && !RectTransformUtility.RectangleContainsScreenPoint(palRect, mousePos, cam))
                    {
                        Destroy(activeColorPaletteObject);
                        activeColorPaletteObject = null;
                    }
                }
            }

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
            OnContextMenuOpened?.Invoke();
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
                private Canvas GetTargetCanvas()
        {
            if (targetCanvas != null && targetCanvas.GetComponent<CRTOverlayController>() == null)
                return targetCanvas;

            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.GetComponent<CRTOverlayController>() == null)
                return targetCanvas;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c != null && c.GetComponent<CRTOverlayController>() == null && c.GetComponent<GraphicRaycaster>() != null)
                {
                    targetCanvas = c;
                    return targetCanvas;
                }
            }
            return targetCanvas;
        }
        public void ShowInputModal(string title, string initialValue, string confirmText, Action<string> onConfirm)
        {
            Hide();

            if (activeModalObject != null)
            {
                Destroy(activeModalObject);
                activeModalObject = null;
            }

            targetCanvas = GetTargetCanvas();
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
                /// <summary>
        /// Displays an interactive retro folder color palette to change folder colors.
        /// </summary>
        public void ShowFolderColorPicker(Vector2 screenPosition, Action<Sprite> onColorSelected)
        {
            Hide();

            if (activeColorPaletteObject != null)
            {
                Destroy(activeColorPaletteObject);
                activeColorPaletteObject = null;
            }

            targetCanvas = GetTargetCanvas();
            if (targetCanvas == null) return;

            // Build the Retro Folder Color Palette UI
            GameObject paletteObj = new GameObject("FolderColorPaletteModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            activeColorPaletteObject = paletteObj;
            paletteObj.transform.SetParent(targetCanvas.transform, false);
            paletteObj.transform.SetAsLastSibling();

            RectTransform panelRect = paletteObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(250f, 155f);

            // Retro 90s OS Window background styling
            Image bgImg = paletteObj.GetComponent<Image>();
            bgImg.color = new Color(0.76f, 0.77f, 0.80f, 1f);

            Outline outline = paletteObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Title Bar
            GameObject titleBarObj = new GameObject("TitleBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            titleBarObj.transform.SetParent(panelRect, false);
            RectTransform titleBarRect = titleBarObj.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0f, 1f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            titleBarRect.pivot = new Vector2(0.5f, 1f);
            titleBarRect.anchoredPosition = new Vector2(0f, -3f);
            titleBarRect.sizeDelta = new Vector2(-6f, 26f);
            Image titleBarImg = titleBarObj.GetComponent<Image>();
            titleBarImg.color = new Color(0.08f, 0.21f, 0.52f, 1f);

            // Title Text
            GameObject titleTxtObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleTxtObj.transform.SetParent(titleBarRect, false);
            RectTransform titleTxtRect = titleTxtObj.GetComponent<RectTransform>();
            titleTxtRect.anchorMin = Vector2.zero;
            titleTxtRect.anchorMax = Vector2.one;
            titleTxtRect.offsetMin = new Vector2(8f, 0f);
            titleTxtRect.offsetMax = new Vector2(-30f, 0f);
            TextMeshProUGUI titleTmp = titleTxtObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Folder Color";
            titleTmp.fontSize = 13;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(titleBarRect, false);
            RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRect.pivot = new Vector2(1f, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-3f, 0f);
            closeBtnRect.sizeDelta = new Vector2(20f, 20f);
            Image closeBtnImg = closeBtnObj.GetComponent<Image>();
            closeBtnImg.color = new Color(0.76f, 0.77f, 0.80f, 1f);
            Button closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                if (activeColorPaletteObject != null)
                {
                    Destroy(activeColorPaletteObject);
                    activeColorPaletteObject = null;
                }
            });

            GameObject closeTxtObj = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtObj.transform.SetParent(closeBtnRect, false);
            RectTransform closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            TextMeshProUGUI closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
            closeTmp.text = "x";
            closeTmp.fontSize = 13;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.color = Color.black;
            closeTmp.alignment = TextAlignmentOptions.Center;

            // Swatch Grid
            GameObject gridObj = new GameObject("ColorGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(panelRect, false);
            RectTransform gridRect = gridObj.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(10f, 10f);
            gridRect.offsetMax = new Vector2(-10f, -34f);

            GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(40f, 44f);
            grid.spacing = new Vector2(6f, 6f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var opt in FolderColorUtility.Options)
            {
                GameObject swatchObj = new GameObject($"Color_{opt.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                swatchObj.transform.SetParent(gridRect, false);

                Image swatchBg = swatchObj.GetComponent<Image>();
                swatchBg.color = new Color(0.92f, 0.93f, 0.95f, 1f);

                Outline swatchOutline = swatchObj.AddComponent<Outline>();
                swatchOutline.effectColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
                swatchOutline.effectDistance = new Vector2(1f, -1f);

                // Preview icon child
                GameObject iconChild = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconChild.transform.SetParent(swatchObj.transform, false);
                RectTransform iconChildRect = iconChild.GetComponent<RectTransform>();
                iconChildRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconChildRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconChildRect.sizeDelta = new Vector2(28f, 28f);
                iconChildRect.anchoredPosition = new Vector2(0f, 3f);
                Image iconImg = iconChild.GetComponent<Image>();
                Sprite spr = FolderColorUtility.GetSprite(opt.spriteName);
                if (spr != null)
                {
                    iconImg.sprite = spr;
                }
                else
                {
                    iconImg.color = opt.color;
                }
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Color accent strip
                GameObject dotChild = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dotChild.transform.SetParent(swatchObj.transform, false);
                RectTransform dotRect = dotChild.GetComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0.5f, 0f);
                dotRect.anchorMax = new Vector2(0.5f, 0f);
                dotRect.pivot = new Vector2(0.5f, 0f);
                dotRect.sizeDelta = new Vector2(26f, 4f);
                dotRect.anchoredPosition = new Vector2(0f, 2f);
                Image dotImg = dotChild.GetComponent<Image>();
                dotImg.color = opt.color;
                dotImg.raycastTarget = false;

                Button swatchBtn = swatchObj.GetComponent<Button>();
                string capturedSpriteName = opt.spriteName;
                swatchBtn.onClick.AddListener(() =>
                {
                    FeedbackManager.PlayClick();
                    Sprite chosen = FolderColorUtility.GetSprite(capturedSpriteName);
                    onColorSelected?.Invoke(chosen);
                    if (activeColorPaletteObject != null)
                    {
                        Destroy(activeColorPaletteObject);
                        activeColorPaletteObject = null;
                    }
                });
            }

            // Smart positioning near screenPosition
            Camera cam = targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
            RectTransform canvasRect = targetCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
            {
                float x = Mathf.Clamp(localPoint.x + 130f, -canvasRect.rect.width / 2f + 135f, canvasRect.rect.width / 2f - 135f);
                float y = Mathf.Clamp(localPoint.y - 80f, -canvasRect.rect.height / 2f + 90f, canvasRect.rect.height / 2f - 90f);
                panelRect.anchoredPosition = new Vector2(x, y);
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
                        string itemLabel = item.label;
                        btn.onClick.AddListener(() =>
                        {
                            Hide();
                            OnMenuItemClicked?.Invoke(itemLabel);
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
