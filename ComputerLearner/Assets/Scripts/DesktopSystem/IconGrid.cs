/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class IconGrid : MonoBehaviour
    {

        #region Public Variables
        // Public Variables [Public Constant Variables, Public Component References, Public Variables]

        // Public Constant Variables


        // Public Component References

        // Public Variables


        #endregion

        #region Private Variables
        // Private Variables [Private Constant Variables, Private Component References, Private Variables]

        // Private Constant Variables
        [SerializeField] private float INITIAL_POS_X = 0;
        [SerializeField] private float INITIAL_POS_Y = 0;
        [SerializeField] private float ICON_SIZE = 60;
        [SerializeField] private float SPACING = 10;


        // Private Component References
        private RectTransform rect;

        // Private Variables
        private DraggableIcon[,] grid;


        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            rect = GetComponent<RectTransform>();

            int sizeX = Mathf.FloorToInt(
                rect.rect.width / (ICON_SIZE + SPACING)
            );

            int sizeY = Mathf.FloorToInt(
                rect.rect.height / (ICON_SIZE + SPACING)
            );

            grid = new DraggableIcon[sizeX, sizeY];
        }

        #endregion

        #region Public Methods
        public void Register(DraggableIcon icon)
        {
            Vector2 localPosition = rect.InverseTransformPoint(
         icon.transform.position
     );

            Vector2Int gridPosition = GetGridPosition(localPosition);
            Debug.Log($"Icon position: {gridPosition}");

            if (!IsInsideGrid(gridPosition))
            {
                Debug.LogWarning(
                    $"Icon {icon.name} is outside the grid."
                );
                return;
            }

            if (grid[gridPosition.x, gridPosition.y] != null)
            {
                Debug.LogWarning(
                    $"Grid position {gridPosition.x}, {gridPosition.y} is already occupied."
                );
                return;
            }

            grid[gridPosition.x, gridPosition.y] = icon;

            icon.transform.position = GetWorldPosition(gridPosition);

            Debug.Log(
                $"Icon {icon.name} registered at: {gridPosition.x}, {gridPosition.y}"
            );
        }

        public void Unregister(DraggableIcon icon)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] == icon)
                    {
                        grid[x, y] = null;
                        return;
                    }
                }
            }
        }

        public void TryPlaceIcon(
        DraggableIcon icon,
        Vector2 screenPosition,
        Vector3 previousPosition,
        Camera eventCamera)
        {
            Vector2 localPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                screenPosition,
                eventCamera,
                out localPosition
            );

            Vector2Int targetPosition = GetGridPosition(localPosition);

            Vector2Int freePosition = FindClosestFreePosition(targetPosition);

            if (freePosition.x == -1)
            {
                // No hay ningún espacio libre
                icon.transform.position = previousPosition;
                Register(icon);
                return;
            }

            grid[freePosition.x, freePosition.y] = icon;

            icon.transform.position = GetWorldPosition(freePosition);

            Debug.Log(
                $"Icon placed at: {freePosition.x}, {freePosition.y}"
            );
        }


        #endregion

        #region Private Methods
        // Private Methods accessible only from this class
        private Vector2Int GetGridPosition(Vector2 localPosition)
        {
            float x = localPosition.x - INITIAL_POS_X;
            float y = -localPosition.y - Mathf.Abs(INITIAL_POS_Y);

            int gridX = Mathf.FloorToInt(
                x / (ICON_SIZE + SPACING)
            );

            int gridY = Mathf.FloorToInt(
                y / (ICON_SIZE + SPACING)
            );

            return new Vector2Int(gridX, gridY);
        }

        private bool IsInsideGrid(Vector2Int position)
        {
            return position.x >= 0 &&
                   position.x < grid.GetLength(0) &&
                   position.y >= 0 &&
                   position.y < grid.GetLength(1);
        }

        private Vector3 GetWorldPosition(Vector2Int position)
        {
            float x = INITIAL_POS_X +
                      position.x * (ICON_SIZE + SPACING);

            float y = INITIAL_POS_Y -
                      position.y * (ICON_SIZE + SPACING);

            Vector2 localPosition = new Vector2(x, y);

            return rect.TransformPoint(localPosition);
        }

        private Vector2Int FindClosestFreePosition(Vector2Int targetPosition)
        {
            Vector2Int closestPosition = new Vector2Int(-1, -1);
            float closestDistance = float.MaxValue;

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                        continue;

                    Vector2Int position = new Vector2Int(x, y);

                    float distance = Vector2Int.Distance(
                        targetPosition,
                        position
                    );

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPosition = position;
                    }
                }
            }

            return closestPosition;
        }

        #endregion
    }
}
