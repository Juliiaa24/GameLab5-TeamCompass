/**
 * Author: Julia Vera
 * Date: 14/09/2026
 * Description: Draggable desktop icon with one window instance per icon.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        #region Public Variables
        public Window AppWindow => appWindow;
        public LevelDefinition LevelDef => levelDefinition;
        public bool AllowDoubleClick { get; set; } = true;
        #endregion

        #region Private Variables
        private IconGrid grid;
        [SerializeField] private GameObject windowPrefab;
        [SerializeField] private GameObject contentPrefab;
        [SerializeField] private LevelDefinition levelDefinition; // The level to launch (optional)
        [SerializeField] private bool isLocked = false;
        private Window appWindow;
        private WindowManager manager;
        private Vector3 initialPosition;
        private Vector3 originalScale;
        private Color originalColor = Color.white;
        private bool dragging;
        private CanvasGroup canvasGroup;
        #endregion

        #region Unity Methods
        private void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            grid = GetComponentInParent<IconGrid>();
            if (grid != null) grid.Register(this);
            manager = GetComponentInParent<WindowManager>();
            initialPosition = transform.position;
            originalScale = transform.localScale;
            
            UnityEngine.UI.Image iconImg = GetComponent<UnityEngine.UI.Image>();
            if (iconImg != null) originalColor = iconImg.color;
            
            // Check ProgressData to see if this level is unlocked
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
            {
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            }

            if (actuallyLocked)
            {
                if (iconImg != null) iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            }
        }

        private void Update()
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);

            // Update color dynamically
            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = actuallyLocked ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : originalColor;
            }

            if (!actuallyLocked && levelDefinition != null && (appWindow == null || appWindow.IsClosed))
            {
                float scale = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
                transform.localScale = originalScale * scale;
            }
            else
            {
                transform.localScale = originalScale;
            }
        }

        private void OnDestroy()
        {
            if (grid != null) grid.Unregister(this);
        }
        #endregion

        #region Public Methods
        public void SetupDynamicIcon(GameObject winPrefab, GameObject contentPref, LevelDefinition levelDef, bool lockedState)
        {
            this.windowPrefab = winPrefab;
            this.contentPrefab = contentPref;
            this.levelDefinition = levelDef;
            this.isLocked = lockedState;
            
            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                if (!lockedState) img.color = Color.white;
                originalColor = img.color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            if (actuallyLocked) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;
            initialPosition = transform.position;
            dragging = false;
            if (grid != null) grid.Unregister(this);
            transform.SetAsLastSibling();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (grid != null)
                grid.TryPlaceIcon(this, eventData.position, initialPosition, eventData.pressEventCamera);
        }

        public void OnBeginDrag(PointerEventData eventData) 
        { 
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            dragging = true;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 point))
                transform.position = point;
        }

        public void OnEndDrag(PointerEventData eventData) 
        { 
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!AllowDoubleClick) return;
            if (!dragging && eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
                OpenApplication();
        }

        public void OpenApplication()
        {
            bool actuallyLocked = isLocked;
            if (levelDefinition != null && ProgressData.Instance != null)
                actuallyLocked = !ProgressData.Instance.IsLevelUnlocked(levelDefinition.levelId);
            if (actuallyLocked) return;

            if (!DesktopTour.IsTourRunning)
            {
                VirtualMascot.HideMascot();
            }
            
            if (manager == null) manager = GetComponentInParent<WindowManager>();
            
            bool isNewWindow = false;
            
            if (manager != null)
            {
                if (appWindow == null || appWindow.IsClosed) isNewWindow = true;
                appWindow = manager.OpenWindow(windowPrefab, appWindow);
            }
            else if (appWindow != null && !appWindow.IsClosed)
            {
                appWindow.Restore();
            }
            else if (windowPrefab != null)
            {
                // Compatibility with older scenes
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) 
                {
                    appWindow = Instantiate(windowPrefab, canvas.transform).GetComponent<Window>();
                    isNewWindow = true;
                }
            }

            if (isNewWindow && appWindow != null)
            {
                // Inject the dynamic content if this icon was configured with one
                if (contentPrefab != null)
                {
                    appWindow.SetContent(Instantiate(contentPrefab));
                }
                
                if (windowPrefab != null && windowPrefab.name.Contains("SkillsReport"))
                {
                    if (appWindow.GetComponent<StatsUIBuilder>() == null)
                        appWindow.gameObject.AddComponent<StatsUIBuilder>();
                }
            }

            // If a new window was just created and we have a level definition, start it!
            if (isNewWindow && appWindow != null && levelDefinition != null)
            {
                appWindow.setTitle(levelDefinition.displayName);
                
                // Maximize the window automatically
                if (!DesktopTour.IsTourRunning)
                {
                    appWindow.toggleMaximize();
                }

                LevelRunner runner = appWindow.GetComponentInChildren<LevelRunner>();
                if (runner == null)
                {
                    runner = appWindow.gameObject.AddComponent<LevelRunner>();
                    runner.taskContentArea = appWindow.ContentArea;
                    
                    if (runner.taskContentArea == null)
                        runner.taskContentArea = appWindow.GetComponent<RectTransform>();
                }
                
                if (!DesktopTour.IsTourRunning)
                {
                    runner.StartLevel(levelDefinition);
                }
            }
        }
        #endregion
    }
}
