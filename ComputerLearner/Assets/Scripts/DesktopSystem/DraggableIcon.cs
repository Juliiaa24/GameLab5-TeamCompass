/**
 * Author: Julia Vera
 * Date: 14/09/2026
 * Description:
*/

using UnityEngine;
using UnityEngine.EventSystems; 

namespace ComputerLearning
{
    /// <summary>
    /// Class to drag the desktop icons
    /// </summary>
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler
    {

        #region Public Variables
        // Public Variables [Public Constant Variables, Public Component References, Public Variables]

        // Public Constant Variables


        // Public Component References

        // Public Variables


        #endregion

        #region Private Variables
        // Private Variables [Private Constant Variables, Private Component References, Private Variables]

        // Private Component References
        private IconGrid grid;

        // Private Variables
        private Vector3 initialPosition;


        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            grid = GetComponentInParent<IconGrid>();

            grid.Register(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            initialPosition = transform.position;
            grid.Unregister(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            grid.TryPlaceIcon(this, eventData.position, initialPosition, eventData.enterEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                // Aquí instancia de la ventana
            }
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes


        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
