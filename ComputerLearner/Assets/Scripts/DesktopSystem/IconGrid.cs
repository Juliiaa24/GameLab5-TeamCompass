/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Place desktop icons on a grid that follows the available Canvas area.
 * Updated: 16/10/2026 - Fixed 6 icons per column, uniform pattern, no overlapping long names.
*/
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public class IconGrid : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Grid Geometry (6 Icons Per Column)")]
        [SerializeField] private int targetRows = 6;
        [SerializeField] private float cellWidth = 155f;
        [SerializeField] private float cellHeight = 164f;
        [SerializeField] private float initialPosX = 30f;
        [SerializeField] private float initialPosY = 16f;
        [SerializeField] private bool autoFitRows = true;

        [Header("Legacy Compatibility Fields")]
        [SerializeField] private float INITIAL_POS_X = 30f;
        [SerializeField] private float INITIAL_POS_Y = 16f;
        [SerializeField] private float ICON_SIZE = 105f;
        [SerializeField] private float SPACING = 55f;

        private RectTransform rect;
        private DraggableIcon[,] grid;
        private readonly List<DraggableIcon> registered = new List<DraggableIcon>();
        #endregion

        #region Unity Methods
        private void Awake() 
        { 
            EnsureGrid(); 
        }

        private void Start()
        {
        }

        private void OnRectTransformDimensionsChange() 
        { 
            EnsureGrid(); 
        }
        #endregion

        #region Public Methods
        public float GetCellWidth()
        {
            return cellWidth > 0f ? cellWidth : 155f;
        }

        public float GetCellHeight()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (autoFitRows && targetRows > 0 && rect != null && rect.rect.height > 0f)
            {
                float startY = GetInitialPosY();
                return Mathf.Max(1f, (rect.rect.height - startY * 2f) / targetRows);
            }
            return cellHeight > 0f ? cellHeight : 164f;
        }

        public float GetInitialPosX()
        {
            if (initialPosX > 0f) return initialPosX;
            if (INITIAL_POS_X > 0f) return INITIAL_POS_X;
            return 30f;
        }

        public float GetInitialPosY()
        {
            if (initialPosY > 0f) return initialPosY;
            if (INITIAL_POS_Y > 0f) return INITIAL_POS_Y;
            return 16f;
        }

        public void Register(DraggableIcon icon)
        {
            EnsureGrid();
            if (icon == null || registered.Contains(icon)) return;
            registered.Add(icon);
            Place(icon, GetGridPosition(rect.InverseTransformPoint(icon.transform.position)));
        }

        public bool PlaceInNextAvailableSlot(DraggableIcon icon)
        {
            EnsureGrid();
            if (icon == null) return false;
            if (registered.Contains(icon))
            {
                Unregister(icon);
            }
            registered.Add(icon);
            return Place(icon, new Vector2Int(0, 0));
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

            float stepX = GetCellWidth();
            float stepY = GetCellHeight();
            float startX = GetInitialPosX();
            float startY = GetInitialPosY();

            int width = Mathf.Max(1, Mathf.FloorToInt((rect.rect.width - startX + 1f) / stepX));
            int height = targetRows > 0 ? targetRows : Mathf.Max(1, Mathf.FloorToInt((rect.rect.height - startY + 1f) / stepY));

            if (grid != null && grid.GetLength(0) == width && grid.GetLength(1) == height) return;
            grid = new DraggableIcon[width, height];
            foreach (DraggableIcon icon in registered)
            {
                if (icon != null) 
                    Place(icon, GetGridPosition(rect.InverseTransformPoint(icon.transform.position)));
            }
        }

        private Vector2Int GetGridPosition(Vector2 local)
        {
            float stepX = GetCellWidth();
            float stepY = GetCellHeight();
            float startX = GetInitialPosX();
            float startY = GetInitialPosY();

            int col = Mathf.FloorToInt((local.x - rect.rect.xMin - startX) / stepX);
            int row = Mathf.FloorToInt((rect.rect.yMax - local.y - startY) / stepY);

            int maxCol = grid != null ? grid.GetLength(0) - 1 : 11;
            int maxRow = grid != null ? grid.GetLength(1) - 1 : (targetRows - 1);

            return new Vector2Int(
                Mathf.Clamp(col, 0, Mathf.Max(0, maxCol)), 
                Mathf.Clamp(row, 0, Mathf.Max(0, maxRow)));
        }

        private bool Place(DraggableIcon icon, Vector2Int target)
        {
            Vector2Int closest = new Vector2Int(-1, -1);
            float distance = float.MaxValue;
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null) continue;
                    float dx = x - target.x;
                    float dy = y - target.y;
                    float candidate = (dx * dx * 1000f) + (dy * dy);
                    if (candidate >= distance) continue;
                    distance = candidate;
                    closest = new Vector2Int(x, y);
                }
            }

            if (closest.x < 0) return false;
            grid[closest.x, closest.y] = icon;

            float stepX = GetCellWidth();
            float stepY = GetCellHeight();
            float startX = GetInitialPosX();
            float startY = GetInitialPosY();

            icon.transform.position = rect.TransformPoint(new Vector2(
                rect.rect.xMin + startX + closest.x * stepX + stepX * 0.5f,
                rect.rect.yMax - startY - closest.y * stepY - stepY * 0.5f));

            if (icon == DraggableIcon.SelectedIcon)
            {
                icon.UpdateSelectionCardTransform();
            }

            return true;
        }
        #endregion
    }
}
