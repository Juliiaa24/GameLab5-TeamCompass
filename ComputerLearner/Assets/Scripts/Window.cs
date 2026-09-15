/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window prefab script
*/

using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class Window : MonoBehaviour, IPointerDownHandler
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


        // Private Component References
        [SerializeField] private RectTransform contentArea;
        private RectTransform windowRectTransform;

        // Private Variables
        private bool maximized = false;
        private Vector2 lastAnchorMin = new Vector2 (0f, 0f);
        private Vector2 lastAnchorMax = new Vector2(0f, 0f);
        private Vector2 lastOffsetMin = new Vector2 (0f, 0f);
        private Vector2 lastOffsetMax = new Vector2 (0f, 0f);

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            windowRectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes

        /// <summary>
        /// Añade el prefab del contenido a la ventana, no tiene por que adaptarse correctamente siempre,
        /// hay que adaptar cada prefab segun las necesidades
        /// </summary>
        /// <param name="contentPrefab"></param>
        public void SetContent(GameObject contentPrefab)
        {
            GameObject content = Instantiate(contentPrefab, contentArea);
            RectTransform rect = content.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        public void toggleMaximize()
        {
            transform.SetAsLastSibling();
            if (!maximized)
            {
                maximized = true;
                lastAnchorMin = windowRectTransform.anchorMin;
                windowRectTransform.anchorMin = Vector2.zero;

                lastAnchorMax = windowRectTransform.anchorMax;
                windowRectTransform.anchorMax = Vector2.one;

                lastOffsetMin = windowRectTransform.offsetMin;
                windowRectTransform.offsetMin = Vector2.zero;

                lastOffsetMax = windowRectTransform.offsetMax;
                windowRectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                maximized = false;
                windowRectTransform.anchorMin = lastAnchorMin;
                windowRectTransform.anchorMax= lastAnchorMax;
                windowRectTransform.offsetMin = lastOffsetMin;
                windowRectTransform .offsetMax = lastOffsetMax;
            }
        }

        public void Minimize()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
