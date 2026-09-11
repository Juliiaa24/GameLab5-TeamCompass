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
    /// Class to drag the desktop icons
    /// </summary>
    public class DraggableIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
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
        private IconGrid grid;
        private Vector3 initialPosition;
        

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            grid = this.GetComponentInParent<IconGrid>();
            grid.Register(this.transform.position);
        }

        private void Update()
        {

        }

        public void OnPointerDown(PointerEventData eventData)
        {
            initialPosition = this.transform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {

        }

        public void OnDrag(PointerEventData eventData)
        {
            this.transform.position = eventData.position;
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
