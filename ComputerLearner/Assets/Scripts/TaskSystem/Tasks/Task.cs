/**
 * Author: Julia Vera
 * Date: 25/09/26
 * Description: A task with associated skills and one result per attempt.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public abstract class Task : MonoBehaviour
    {
        #region Public Variables
        public bool IsRunning { get; private set; }
        public int Attempt { get; private set; }
        public virtual int CurrentProgress => completed ? 1 : 0;
        public virtual int RequiredProgress => 1;
        public IReadOnlyList<SkillID> Skills => skills;
        public event Action<Task> ProgressChanged;
        public event Action<Task> Completed;
        #endregion

        #region Private Variables
        [SerializeField] private SkillID[] skills = new SkillID[0];
        #endregion

        #region Protected Variables
        protected Tutorial tutorial;
        protected bool completed;
        #endregion

        #region Unity Methods
        protected virtual void OnDestroy()
        {
            if (TaskManager.Instance != null) TaskManager.Instance.UnregisterTask(this);
        }
        #endregion

        #region Public Methods
        public bool IsCompleted() { return completed; }
        public abstract bool Check();
        public abstract bool Feedback();

        public virtual void BeginTask()
        {
            completed = false;
            IsRunning = true;
            Attempt++;
            NotifyProgress();
        }

        public virtual void CancelTask()
        {
            IsRunning = false;
            NotifyProgress();
        }
        #endregion

        #region Protected Methods
        protected void NotifyProgress() { ProgressChanged?.Invoke(this); }

        protected void CompleteTask()
        {
            if (!IsRunning || completed) return;
            completed = true;
            IsRunning = false;
            NotifyProgress();
            Completed?.Invoke(this);
        }
        #endregion
    }
}
