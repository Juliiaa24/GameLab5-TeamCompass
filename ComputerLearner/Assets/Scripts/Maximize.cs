/**
 * Author: Diego 
 * Date: 11/09/26
 * Description:
*/

using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    /// <summary>
    /// Extend the windows
    /// </summary>
    public class Maximize : MonoBehaviour, IPointerClickHandler
    {

        #region Public Variables

        #endregion

        #region Private Variables

        [SerializeField] private Window window;


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

        public void OnPointerClick(PointerEventData eventData)
        {
            window.toggleMaximize();
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
