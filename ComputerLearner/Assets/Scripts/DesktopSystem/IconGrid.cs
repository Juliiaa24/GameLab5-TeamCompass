/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class IconGrid : MonoBehaviour, IDropHandler
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
        [SerializeField] private float INITIAL_POS_X = 60;
        [SerializeField] private float INITIAL_POS_Y = -60;
        [SerializeField] private float ICON_SIZE = 60;
        [SerializeField] private float SPACING = 10;


        // Private Component References
        private RectTransform rect;

        // Private Variables
        [SerializeField] private bool[,] grid;


        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            rect = GetComponent<RectTransform>();
            float sizeX = rect.rect.width / ICON_SIZE+SPACING;
            float sizeY = rect.rect.height / ICON_SIZE+SPACING;
            grid = new bool[(int)sizeX, (int)sizeY];
        }

        private void Update()
        {

        }
        public void OnDrop(PointerEventData eventData)
        {
            this.transform.position = eventData.position;
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public void Register(Vector2 pos)
        {
            int x = (int)(pos.x / (ICON_SIZE + SPACING));
            int y = (int)(pos.y / (ICON_SIZE + SPACING));

            grid[x, y] = true;
            Debug.Log($"Position {pos} Register {x}, {y}");
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
