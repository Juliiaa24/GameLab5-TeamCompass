/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window prefab state, geometry and content.
*/
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>The source of truth for a window, including while it is minimized.</summary>
    public class Window : MonoBehaviour, IPointerDownHandler
    {
        #region Public Variables
        public bool IsMaximized => maximized;
        public bool IsMinimized => !gameObject.activeSelf;
        public bool IsClosed { get; private set; }
        public bool IsFocused => manager != null && manager.ActiveWindow == this;
        public string Title => string.IsNullOrWhiteSpace(windowTitle) ? name.Replace("(Clone)", "") : windowTitle;
        public Sprite Icon => windowIcon;
        public GameObject Content { get; private set; }
        public event Action<Window> StateChanged;
        #endregion

        #region Private Variables
        [Header("Window References")]
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private RectTransform closeButton;
        [SerializeField] private RectTransform maximizeButton;
        [SerializeField] private RectTransform minimizeButton;
        [SerializeField] private RectTransform contentArea;
        
        [Header("Configuration")]
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private string windowTitle;
        [SerializeField] private Sprite windowIcon;
        private RectTransform windowRectTransform;
        private WindowManager manager;
        private bool maximized;
        private Vector2 lastAnchorMin;
        private Vector2 lastAnchorMax;
        private Vector2 lastPosition;
        private Vector2 lastSize;

        public RectTransform TitleBar => titleBar;
        public RectTransform CloseButton => closeButton;
        public RectTransform MaximizeButton => maximizeButton;
        public RectTransform MinimizeButton => minimizeButton;
        public RectTransform ContentArea => contentArea;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.2f;
        private Coroutine animationCoroutine;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            windowRectTransform = GetComponent<RectTransform>();

            // Fallback for unassigned references (maintains backward compatibility with old prefabs)
            if (titleBar == null) titleBar = transform.Find("WindowTop") as RectTransform;
            if (closeButton == null) closeButton = transform.Find("WindowTop/Buttons/Close") as RectTransform;
            if (maximizeButton == null) maximizeButton = transform.Find("WindowTop/Buttons/Maximize") as RectTransform;
            if (minimizeButton == null) minimizeButton = transform.Find("WindowTop/Buttons/Minimize") as RectTransform;
            if (contentArea == null) contentArea = transform.Find("WindowContents") as RectTransform;

            windowRectTransform.localScale = Vector3.zero; // Start small for open animation
            if (contentPrefab != null) SetContent(Instantiate(contentPrefab));
            InstallFocusRelays();
        }

        private void OnEnable()
        {
            if (manager == null) manager = GetComponentInParent<WindowManager>();
            if (manager != null) manager.RegisterWindow(this);
            BringToFront();
            StateChanged?.Invoke(this);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(windowRectTransform.localScale, Vector3.one, null));
        }

        private void OnDisable() { StateChanged?.Invoke(this); }

        private void OnDestroy()
        {
            IsClosed = true;
            if (manager != null) manager.UnregisterWindow(this);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (windowRectTransform != null && !maximized) KeepTitleVisible();
        }
        #endregion

        #region Public Methods
        public void SetContent(GameObject content)
        {
            if (content == null || contentArea == null) return;
            if (Content != null) { Content.SetActive(false); Destroy(Content); }
            Content = content;
            // IMPORTANTE: El false evita que Unity intente compensar la escala cuando la ventana está a 0
            Content.transform.SetParent(contentArea, false); 
            RectTransform rect = Content.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one; // Reseteamos la escala por seguridad
            }
            InstallFocusRelays();
        }

        public void OnPointerDown(PointerEventData eventData) { BringToFront(); }

        public void BringToFront()
        {
            if (IsClosed || IsMinimized) return;
            if (manager != null) manager.FocusWindow(this);
            else transform.SetAsLastSibling();
        }

        // Preserve the original method name for existing Inspector references.
        public void toggleMaximize()
        {
            if (IsClosed) return;
            Restore();
            if (!maximized)
            {
                // Save all geometry before modifying anchors.
                lastAnchorMin = windowRectTransform.anchorMin;
                lastAnchorMax = windowRectTransform.anchorMax;
                lastPosition = windowRectTransform.anchoredPosition;
                lastSize = windowRectTransform.sizeDelta;
                maximized = true;
                windowRectTransform.anchorMin = Vector2.zero;
                windowRectTransform.anchorMax = Vector2.one;
                windowRectTransform.offsetMin = Vector2.zero;
                windowRectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                windowRectTransform.anchorMin = lastAnchorMin;
                windowRectTransform.anchorMax = lastAnchorMax;
                windowRectTransform.sizeDelta = lastSize;
                windowRectTransform.anchoredPosition = lastPosition;
                maximized = false;
                KeepTitleVisible();
            }
            StateChanged?.Invoke(this);
        }

        public void Minimize()
        {
            if (IsClosed || IsMinimized) return;
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(windowRectTransform.localScale, Vector3.zero, () => gameObject.SetActive(false)));
        }

        public void Restore()
        {
            if (IsClosed) return;
            
            bool wasInactive = !gameObject.activeSelf;
            gameObject.SetActive(true);
            
            // If it was already active (e.g., halfway minimizing or just clicking icon again),
            // OnEnable won't fire, so we trigger the animation and bring to front manually.
            if (!wasInactive)
            {
                BringToFront();
                if (animationCoroutine != null) StopCoroutine(animationCoroutine);
                animationCoroutine = StartCoroutine(AnimateScale(windowRectTransform.localScale, Vector3.one, null));
            }
        }

        public void CloseWindow()
        {
            if (IsClosed) return;
            IsClosed = true;
            
            // Ocultar mascota si estaba señalando a esta ventana
            VirtualMascot.HideMascotIfTargeting(transform);

            StateChanged?.Invoke(this);
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(windowRectTransform.localScale, Vector3.zero, () =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }));
        }

        public void KeepTitleVisible()
        {
            if (maximized || windowRectTransform == null || !(transform.parent is RectTransform parent)) return;
            if (parent.rect.width <= 0 || parent.rect.height <= 0) return;
            Rect rect = windowRectTransform.rect;
            Vector3 position = windowRectTransform.localPosition;
            float visibleWidth = Mathf.Min(100f, rect.width, parent.rect.width);
            float visibleHeight = Mathf.Min(40f, rect.height, parent.rect.height);
            position.x = Mathf.Clamp(position.x, parent.rect.xMin - rect.xMax + visibleWidth,
                parent.rect.xMax - rect.xMin - visibleWidth);
            position.y = Mathf.Clamp(position.y, parent.rect.yMin - rect.yMax + visibleHeight,
                parent.rect.yMax - rect.yMax);
            windowRectTransform.localPosition = position;
        }

        public void setTitle(string title)
        {
            windowTitle = title;
            StateChanged?.Invoke(this);
        }

        public void setIcon(Sprite icon)
        {
            windowIcon = icon;
        }

        internal void SetManager(WindowManager value) { manager = value; }
        #endregion

        #region Private Methods
        private void InstallFocusRelays()
        {
            // uGUI does not bubble pointer-down through controls that already handle it.
            foreach (MonoBehaviour control in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (control == null || control == this || control is WindowFocusRelay) continue;
                if (!(control is IPointerDownHandler) && !(control is IPointerClickHandler)) continue;
                if (control.GetComponent<WindowFocusRelay>() == null)
                    control.gameObject.AddComponent<WindowFocusRelay>();
            }
        }

        private IEnumerator AnimateScale(Vector3 fromScale, Vector3 toScale, Action onComplete)
        {
            float elapsed = 0f;
            windowRectTransform.localScale = fromScale;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                // Ease out cubic
                t = 1f - Mathf.Pow(1f - t, 3f);
                windowRectTransform.localScale = Vector3.Lerp(fromScale, toScale, t);
                yield return null;
            }

            windowRectTransform.localScale = toScale;
            onComplete?.Invoke();
            animationCoroutine = null;
        }
        #endregion
    }
}

