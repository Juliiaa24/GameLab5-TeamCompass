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
    public class TargetTask : Task, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {

        #region Public Variables
        public int HoverExits => hoverExits;
        #endregion

        #region Private Variables
        private Animator animator;
        private bool alreadyClicked = false;
        private bool isHovering = false;
        private int hoverExits = 0;
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
            //Debug.Log(completed);
            return completed;
        }
        public override void Feedback()
        {
            Debug.Log("FeedBack");
            completed = false;
            if (animator != null) animator.Play("Explosion");
            else Destroy(gameObject);
        }

        public void OnAnimationEnd()
        {
            Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!alreadyClicked)
            {
                isHovering = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHovering && !alreadyClicked)
            {
                isHovering = false;
                hoverExits++;
                Debug.Log($"Target missed by hover! Total exits: {hoverExits}");
            }
        }
        #endregion

        #region Private Methods
        // Private Methods accessible only from this class
        public void OnPointerClick(PointerEventData eventData)
        {
            if (alreadyClicked) return;
            alreadyClicked = true;
            isHovering = false;
            completed = true;
            Debug.Log("TargetClicked");
        }

        #endregion
    }
}
