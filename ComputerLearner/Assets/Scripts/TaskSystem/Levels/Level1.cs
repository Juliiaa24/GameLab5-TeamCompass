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
    /// Nivel 1: Entrena y evalúa la habilidad de CLICK usando TargetTasks y detectando clics fallados en el fondo.
    /// </summary>
    public class Level1 : Level, IPointerClickHandler
    {

        #region Public Variables

        #endregion

        #region Private Variables
        private int missedClicks = 0;
        private int totalHoverExits = 0;
        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            levelName = "Level 1";
        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!levelCompleted)
            {
                // Asegurarnos de que el clic dio exactamente en el fondo del nivel,
                // y no en un Target u otro elemento hijo que haya dejado pasar el evento.
                if (eventData.pointerPressRaycast.gameObject == gameObject)
                {
                    missedClicks++;
                    Debug.Log($"Missed click on Level 1 background. Total missed: {missedClicks}");
                }
            }
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class
        protected override void OnTaskCompleted(Task task)
        {
            if (task is TargetTask target)
            {
                totalHoverExits += target.HoverExits;
            }
        }

        protected override void EvaluateSkills()
        {
            float timeTaken = Mathf.Max(0f, Time.time - levelStartTime);
            
            // --- EVALUAR CLICK ---
            float expectedTime = Mathf.Max(1, initialTasksCount) * 2.5f;
            float extraTimePenalty = Mathf.Max(0f, timeTaken - expectedTime) * 0.3f;
            float missPenalty = missedClicks * 1.0f;

            float clickGrade = Mathf.Clamp(10f - missPenalty - extraTimePenalty, 1f, 10f);

            // --- EVALUAR MOVE ---
            float expectedMoveTime = Mathf.Max(1, initialTasksCount) * 1.8f;
            float moveTimePenalty = Mathf.Max(0f, timeTaken - expectedMoveTime) * 0.4f;
            float hoverExitPenalty = totalHoverExits * 0.5f; // Penalización por dudar o salirse del target

            float moveGrade = Mathf.Clamp(10f - moveTimePenalty - hoverExitPenalty, 1f, 10f);

            if (SkillManager.Instance != null)
            {
                // Guardar la nota de CLICK
                SkillManager.Instance.SetSkillGrade(SkillID.CLICK, id, clickGrade, timeTaken, missedClicks);
                Debug.Log($"Level 1 Evaluated CLICK Skill -> Grade: {clickGrade:F1} (MissedClicks: {missedClicks}, Time: {timeTaken:F1}s)");

                // Guardar la nota de MOVE con salidas prematuras
                SkillManager.Instance.SetSkillGrade(SkillID.MOVE, id, moveGrade, timeTaken, totalHoverExits);
                Debug.Log($"Level 1 Evaluated MOVE Skill -> Grade: {moveGrade:F1} (MoveErrors/Exits: {totalHoverExits}, Time: {timeTaken:F1}s)");
            }
        }

        #endregion
    }
}
