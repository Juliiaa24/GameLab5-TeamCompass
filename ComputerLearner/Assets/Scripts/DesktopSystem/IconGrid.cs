/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Place desktop icons on a grid that follows the available Canvas area.
*/
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public class IconGrid : MonoBehaviour
    {
        #region Private Variables
        [SerializeField] private float INITIAL_POS_X = 0;
        [SerializeField] private float INITIAL_POS_Y = 0;
        [SerializeField] private float ICON_SIZE = 120;
        [SerializeField] private float SPACING = 30;
        private RectTransform rect;
        private DraggableIcon[,] grid;
        private readonly List<DraggableIcon> registered = new List<DraggableIcon>();
        #endregion

        #region Unity Methods
        private void Awake() { EnsureGrid(); }

        private void OnEnable()
        {
            if (ProgressData.Instance != null)
                ProgressData.Instance.OnLevelUnlocked += HandleLevelUnlocked;
        }

        private void OnDisable()
        {
            if (ProgressData.Instance != null)
                ProgressData.Instance.OnLevelUnlocked -= HandleLevelUnlocked;
        }

        private void HandleLevelUnlocked(string levelId)
        {
            if (levelId == "level2")
            {
                GameObject icon = GameObject.Find("Application Icon (1)");
                if (icon != null) VirtualMascot.Show("Level 2 is now unlocked!\nDouble-click here to continue.", icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }
            else if (levelId == "level3")
            {
                GameObject icon = GameObject.Find("Application Icon (2)");
                if (icon != null) VirtualMascot.Show("Level 3 is ready!\nHarvest the garden.", icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }
            else if (levelId == "level4")
            {
                GameObject icon = GameObject.Find("Application Icon (3)");
                if (icon != null) VirtualMascot.Show("Level 4 unlocked!\nTry Endless Practice.", icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }
        }
        
        private System.Collections.IEnumerator Start()
        {
            // Bloqueador de pantalla invisible para evitar clicks durante el tour
            GameObject blocker = new GameObject("TourBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                blocker.transform.SetParent(canvas.transform, false);
                blocker.transform.SetAsLastSibling(); // Poner por encima de todo
                RectTransform rect = blocker.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                UnityEngine.UI.Image img = blocker.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0, 0, 0, 0); // Transparente
                img.raycastTarget = true; // Bloquea clicks
            }

            yield return new WaitForSeconds(1.0f);

            GameObject level1Icon = GameObject.Find("Application Icon");
            GameObject level2Icon = GameObject.Find("Application Icon (1)");
            GameObject statsIcon = GameObject.Find("Stats Icon");

            if (ProgressData.Instance != null && !ProgressData.Instance.HasSeenDesktopTour)
            {
                if (level1Icon != null) VirtualMascot.Show("Welcome to your Virtual Desktop!\nDouble-click here to play Level 1.", level1Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
                yield return new WaitForSeconds(4.0f);
                if (statsIcon != null) VirtualMascot.Show("This is the Stats menu.\nHere you can see your learning progress!", statsIcon.GetComponent<RectTransform>(), new Vector2(160, -80));
                yield return new WaitForSeconds(5.0f);
                if (level1Icon != null) VirtualMascot.Show("Let's tend the garden!\nOpen Level 1 to start.", level1Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
                ProgressData.Instance.SetHasSeenDesktopTour(true);
            }
            else if (ProgressData.Instance != null && ProgressData.Instance.IsLevelUnlocked("level2") && !ProgressData.Instance.IsLevelUnlocked("level3"))
            {
                if (level2Icon != null) VirtualMascot.Show("Level 2 is now unlocked!\nDouble-click here to continue.", level2Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }
            else if (ProgressData.Instance != null && ProgressData.Instance.IsLevelUnlocked("level3") && !ProgressData.Instance.IsLevelUnlocked("level4"))
            {
                GameObject level3Icon = GameObject.Find("Application Icon (2)");
                if (level3Icon != null) VirtualMascot.Show("Level 3 is ready!\nHarvest the garden.", level3Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }
            else if (ProgressData.Instance != null && ProgressData.Instance.IsLevelUnlocked("level4"))
            {
                GameObject level4Icon = GameObject.Find("Application Icon (3)");
                if (level4Icon != null) VirtualMascot.Show("Level 4 unlocked!\nTry Endless Practice.", level4Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
            }

            // Destruir bloqueador cuando termine el tour
            if (blocker != null) Destroy(blocker);
        }

        private void OnRectTransformDimensionsChange() { EnsureGrid(); }
        #endregion

        #region Public Methods
        public void Register(DraggableIcon icon)
        {
            EnsureGrid();
            if (icon == null || registered.Contains(icon)) return;
            registered.Add(icon);
            Place(icon, GetGridPosition(rect.InverseTransformPoint(icon.transform.position)));
        }

        public void Unregister(DraggableIcon icon)
        {
            registered.Remove(icon);
            if (grid == null) return;
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    if (grid[x, y] == icon) grid[x, y] = null;
        }

        public void TryPlaceIcon(DraggableIcon icon, Vector2 screenPosition, Vector3 previousPosition, Camera eventCamera)
        {
            EnsureGrid();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, eventCamera, out Vector2 local))
            {
                icon.transform.position = previousPosition;
                Register(icon);
                return;
            }
            Unregister(icon);
            registered.Add(icon);
            if (!Place(icon, GetGridPosition(local)))
            {
                icon.transform.position = previousPosition;
                Place(icon, GetGridPosition(rect.InverseTransformPoint(previousPosition)));
            }
        }
        #endregion

        #region Private Methods
        private void EnsureGrid()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (rect == null) return;
            float step = Mathf.Max(1f, ICON_SIZE + SPACING);
            int width = Mathf.Max(1, Mathf.FloorToInt((rect.rect.width - INITIAL_POS_X + SPACING) / step));
            int height = Mathf.Max(1, Mathf.FloorToInt((rect.rect.height - INITIAL_POS_Y + SPACING) / step));
            if (grid != null && grid.GetLength(0) == width && grid.GetLength(1) == height) return;
            grid = new DraggableIcon[width, height];
            foreach (DraggableIcon icon in registered)
                if (icon != null) Place(icon, GetGridPosition(rect.InverseTransformPoint(icon.transform.position)));
        }

        private Vector2Int GetGridPosition(Vector2 local)
        {
            float step = Mathf.Max(1f, ICON_SIZE + SPACING);
            return new Vector2Int(Mathf.FloorToInt((local.x - rect.rect.xMin - INITIAL_POS_X) / step),
                Mathf.FloorToInt((rect.rect.yMax - local.y - INITIAL_POS_Y) / step));
        }

        private bool Place(DraggableIcon icon, Vector2Int target)
        {
            Vector2Int closest = new Vector2Int(-1, -1);
            float distance = float.MaxValue;
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null) continue;
                    float candidate = (new Vector2Int(x, y) - target).sqrMagnitude;
                    if (candidate >= distance) continue;
                    distance = candidate;
                    closest = new Vector2Int(x, y);
                }
            if (closest.x < 0) return false;
            grid[closest.x, closest.y] = icon;
            float step = Mathf.Max(1f, ICON_SIZE + SPACING);
            icon.transform.position = rect.TransformPoint(new Vector2(
                rect.rect.xMin + INITIAL_POS_X + closest.x * step + ICON_SIZE * 0.5f,
                rect.rect.yMax - INITIAL_POS_Y - closest.y * step - ICON_SIZE * 0.5f));
            return true;
        }
        #endregion
    }
}
