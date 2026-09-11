/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window prefab script
*/

using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class Window : MonoBehaviour
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

        // Private Variables


        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {

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

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
