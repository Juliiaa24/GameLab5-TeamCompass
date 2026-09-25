/**
 * Author: Diego
 * Date: 25/09/26
 * Description: A level contains the tasks required for its completion.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public enum LevelID
    {
        LEVEL1 = 0, LEVEL2, LEVEL3, LEVEL4, LEVEL5, LEVEL6, LEVEL7, LEVEL8,
        LEVEL9, LEVEL10, LEVEL11, LEVEL12, LEVEL13, LEVEL14, LEVEL15,
        LEVEL16, LEVEL17, LEVEL18, LEVEL19, NUM_LEVELS
    }

    public abstract class Level : MonoBehaviour
    {
        #region Public Variables
        public LevelID ID => levelID;
        public IReadOnlyList<Task> Tasks => tasks;
        public bool IsRunning { get; private set; }
        public int CompletedTaskCount
        {
            get
            {
                int count = 0;
                foreach (Task task in tasks)
                    if (task != null && task.IsCompleted()) count++;
                return count;
            }
        }
        public event Action<Level> StateChanged;
        #endregion

        #region Private Variables
        [SerializeField] private LevelID levelID;
        #endregion

        #region Protected Variables
        [SerializeField] protected List<Task> tasks = new List<Task>();
        protected bool levelCompleted;
        #endregion

        #region Unity Methods
        protected virtual void OnDestroy()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.UnregisterLevel(this);
        }
        #endregion

        #region Public Methods
        public bool IsCompleted() { return levelCompleted; }

        public void BeginLevel()
        {
            if (TaskManager.Instance == null) return;
            levelCompleted = false;
            IsRunning = false;
            foreach (Task task in tasks) TaskManager.Instance.BeginTask(task);
            IsRunning = true;
            StateChanged?.Invoke(this);
        }

        public void Evaluate()
        {
            if (!IsRunning) return;
            if (tasks.Count > 0 && CompletedTaskCount == tasks.Count)
            {
                levelCompleted = true;
                IsRunning = false;
            }
            StateChanged?.Invoke(this);
        }

        public void CancelLevel()
        {
            IsRunning = false;
            foreach (Task task in tasks)
                if (task != null && task.IsRunning) task.CancelTask();
            StateChanged?.Invoke(this);
        }
        #endregion
    }
}
