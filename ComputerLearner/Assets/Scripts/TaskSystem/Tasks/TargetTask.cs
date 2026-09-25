/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Configurable target-click task, using the existing Task contract.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class TargetTask : Task, IPointerClickHandler
    {
        #region Public Variables
        public override int CurrentProgress => hits;
        public override int RequiredProgress => Mathf.Max(1, requiredTargets);
        #endregion

        #region Private Variables
        [SerializeField, Min(1)] private int requiredTargets = 5;
        private int hits;
        #endregion

        #region Public Methods
        public override bool Check() { return completed; }
        public override bool Feedback() { return completed; }

        public override void BeginTask()
        {
            hits = 0;
            base.BeginTask();
        }

        public bool RegisterHit()
        {
            if (!IsRunning || completed) return false;
            hits++;
            if (hits >= RequiredProgress) CompleteTask();
            else NotifyProgress();
            return true;
        }

        // Preserve the original direct-click task behaviour for existing standalone targets.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                GetComponent<ClickTargetExercise>() == null) RegisterHit();
        }
        #endregion
    }
}
