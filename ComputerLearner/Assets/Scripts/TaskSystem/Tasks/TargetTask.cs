/**
 * Author: Julia Vera
 * Date: 25/09
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
        Animator animator;

        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            animator = GetComponent<Animator>();
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
        public override void Feedback()
        {
            Debug.Log("FeedBack");
            completed = false;
            if(animator != null) animator.Play("Explosion");
        }

        public void OnAnimationEnd()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class
        public void OnPointerClick(PointerEventData eventData)
        {
            completed = true;
            Debug.Log("TargetClicked");
        }

        #endregion
    }
}
