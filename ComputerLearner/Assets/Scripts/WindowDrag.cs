/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window dragging script
*/


using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class WindowDrag : MonoBehaviour, IDragHandler
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


        // Private Variables
        [SerializeField] private RectTransform window;

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
        public void OnDrag(PointerEventData eventData)
        {
            window.anchoredPosition += eventData.delta;
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
