/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Forward focus from controls which consume uGUI pointer events.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class WindowFocusRelay : MonoBehaviour, IPointerDownHandler
    {
        #region Public Methods
        public void OnPointerDown(PointerEventData eventData)
        {
            GetComponentInParent<Window>()?.BringToFront();
        }
        #endregion
    }
}
