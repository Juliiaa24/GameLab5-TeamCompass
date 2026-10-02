/**
 * Author: Julia Vera
 * Date: 14/09/2026
 * Description: Draggable desktop icon with one window instance per icon.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler
    {
        #region Public Variables
        public Window AppWindow => appWindow;
        #endregion

        #region Private Variables
        private IconGrid grid;
        [SerializeField] private GameObject windowPrefab;
        [SerializeField] private LevelDefinition levelDefinition; // The level to launch (optional)
        [SerializeField] private bool isLocked = false;
        private Window appWindow;
        private WindowManager manager;
        private Vector3 initialPosition;
        private Vector3 originalScale;
        private Color originalColor = Color.white;
        private bool dragging;
        #endregion

        #region Unity Methods
        private void Start()
        {
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

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            dragging = true;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 point))
                transform.position = point;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
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

            if (isNewWindow && appWindow != null && windowPrefab != null && windowPrefab.name.Contains("SkillsReport"))
            {
                if (appWindow.GetComponent<StatsUIBuilder>() == null)
                    appWindow.gameObject.AddComponent<StatsUIBuilder>();
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
                    Transform contentArea = appWindow.transform.Find("WindowContents");
                    if (contentArea != null)
                        runner.taskContentArea = contentArea.GetComponent<RectTransform>();
                    else
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
