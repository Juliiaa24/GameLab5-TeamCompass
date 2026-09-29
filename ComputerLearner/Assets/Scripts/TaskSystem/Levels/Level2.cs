/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class Level2 : Level
    {

        #region Public Variables

        #endregion

        #region Private Variables
        private int totalPrematureExits = 0;
        private float totalRequiredHoverTime = 0f;
        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            levelName = "Level 2";
        }

        private void Start()
        {
            
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class
        protected override void OnTaskCompleted(Task task)
        {
            if (task is HoverTask hoverTask)
            {
                totalPrematureExits += hoverTask.PrematureExits;
                totalRequiredHoverTime += hoverTask.RequiredHoverTime;
            }
        }

        protected override void EvaluateSkills()
        {
            float timeTaken = Mathf.Max(0f, Time.time - levelStartTime);
            float expectedTime = totalRequiredHoverTime + (Mathf.Max(1, initialTasksCount) * 2.5f);
            float extraTimePenalty = Mathf.Max(0f, timeTaken - expectedTime) * 0.25f;
            float exitsPenalty = totalPrematureExits * 1.5f;

            float grade = Mathf.Clamp(10f - exitsPenalty - extraTimePenalty, 1f, 10f);

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.SetSkillGrade(SkillID.MOVE, id, grade, timeTaken, totalPrematureExits);
                Debug.Log($"Level 2 Evaluated MOVE Skill -> Grade: {grade:F1} (PrematureExits: {totalPrematureExits}, Time: {timeTaken:F1}s)");
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            Managers.Sy().ChangeToMenu();
        }

        #endregion
    }
}
