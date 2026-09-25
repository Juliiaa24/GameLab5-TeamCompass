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
    public class TargetTask : Task, IPointerClickHandler
    {

        #region Public Variables

        #endregion

        #region Private Variables
        

        #endregion

        #region Protected Variables

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
        public override bool Check()
        {
            return completed;
        }
        public override bool Feedback()
        {
            return false;
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class
        public void OnPointerClick(PointerEventData eventData)
        {
            completed = true;
        }

        #endregion
    }
}
