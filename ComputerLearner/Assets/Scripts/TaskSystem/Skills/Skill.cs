/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Skill identity, tutorial and successful task results.
*/
using System;
using UnityEngine;

namespace ComputerLearning
{
    public enum SkillID
    {
        MOVE = 0, CLICK, HOLD, DROP, SCROLL, NUM_SKILLS
    }

    public abstract class Skill : MonoBehaviour
    {
        #region Public Variables
        public SkillID ID => skillID;
        public int CompletedTasks { get; private set; }
        public event Action<Skill> ProgressChanged;
        #endregion

        #region Private Variables
        [SerializeField] private SkillID skillID;
        #endregion

        #region Protected Variables
        [SerializeField] protected Tutorial tutorial;
        #endregion

        #region Public Methods
        public abstract void ShowTutorial();

        public virtual void ReceiveResult(Task task)
        {
            if (task == null || !task.IsCompleted()) return;
            CompletedTasks++;
            ProgressChanged?.Invoke(this);
        }
        #endregion
    }
}
