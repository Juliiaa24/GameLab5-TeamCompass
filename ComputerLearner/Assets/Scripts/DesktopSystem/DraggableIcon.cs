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

        #endregion

        #region Private Variables

        private IconGrid grid;
        [SerializeField] private GameObject windowPrefab;
        private GameObject appWindow;
        private Transform canvas;

        private Vector3 initialPosition;
        private bool windowOpen = false;
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            grid = GetComponentInParent<IconGrid>();

            grid.Register(this);

            canvas = transform.parent.parent.transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            initialPosition = transform.position;
            grid.Unregister(this);
            transform.SetAsLastSibling();
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
                if (appWindow == null)
                    windowOpen = false;
                if (windowOpen) {
                    appWindow.SetActive(true);
                }
                else
                {
                    appWindow = Instantiate(windowPrefab, canvas);
                    windowOpen = true;
                }
                
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

