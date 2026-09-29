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
        public int PrematureExits => prematureExits;
        public float RequiredHoverTime => initialHoverTotalTime;
        #endregion

        #region Private Variables
        private float mouseOverTime;
        [SerializeField] private float mouseOverTotalTime = 5.0f ;
        [SerializeField] private Image progressBar;
        private int prematureExits = 0;
        private float initialHoverTotalTime;

        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            initialHoverTotalTime = mouseOverTotalTime;
        }

        private void Start()
        {
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
            else if (mouseOverTime != 0 && mouseOverTotalTime > 0)
            {
                float elapsedTime = Time.time - mouseOverTime;
                progressBar.fillAmount = elapsedTime / mouseOverTotalTime;
                Debug.Log(elapsedTime / mouseOverTotalTime);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mouseOverTime == 0 && !completed)
            {
                progressBar.fillAmount = 0;
                mouseOverTime = Time.time;
                Debug.Log("MouseOver");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (mouseOverTime != 0 && !completed)
            {
                prematureExits++;
            }
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
