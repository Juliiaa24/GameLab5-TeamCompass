/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class HoverTask : Task, IPointerEnterHandler, IPointerExitHandler
    {

        #region Public Variables

        #endregion

        #region Private Variables
        private float mouseOverTime;
        [SerializeField] private float mouseOverTotalTime = 5.0f ;
        [SerializeField] private Image progressBar;


        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {
            progressBar = GetComponentInChildren<Image>();
            progressBar.fillAmount = 0;
        }

        private void Update()
        {
            if (mouseOverTime != 0 && mouseOverTime + mouseOverTotalTime <= Time.time)
            {
                //Progress Bar
                Debug.Log(mouseOverTime + mouseOverTotalTime + "Time: " + Time.time);
                mouseOverTotalTime = 0;
                completed = true;
            }
            else if (mouseOverTime != 0)
            {
                float elapsedTime = Time.time - mouseOverTime;
                progressBar.fillAmount = elapsedTime / mouseOverTotalTime;
                Debug.Log(elapsedTime / mouseOverTotalTime);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mouseOverTime == 0)
            {
                progressBar.fillAmount = 0;
                mouseOverTime = Time.time;
                Debug.Log("MouseOver");
            }
            //else if (mouseOverTime + mouseOverTotalTime > Time.time)
            //{
            //    Debug.Log(mouseOverTime + mouseOverTotalTime + "Time: " + Time.time);
            //    mouseOverTotalTime = 0;
            //    completed = true;
            //}
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            mouseOverTime = 0;
            progressBar.fillAmount = 0;
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
            Destroy(gameObject);

        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class

        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class


        #endregion
    }
}
