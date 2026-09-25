/**
 * Author: Julia Vera
 * Date: 25/09/26
 * Description: Observe tasks and deliver their results to skills and levels.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [DefaultExecutionOrder(-90)]
    public class TaskManager : MonoBehaviour
    {
        #region Public Variables
        public static TaskManager Instance { get; private set; }
        public event Action<Task> TaskProgressChanged;
        public event Action<Task> TaskCompleted;
        #endregion

        #region Private Variables
        private readonly HashSet<Task> tasks = new HashSet<Task>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            foreach (Task task in tasks)
            {
                if (task == null) continue;
                task.ProgressChanged -= OnProgress;
                task.Completed -= OnCompleted;
            }
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        public void BeginTask(Task task)
        {
            if (task == null) return;
            if (tasks.Add(task))
            {
                task.ProgressChanged += OnProgress;
                task.Completed += OnCompleted;
            }
            task.BeginTask();
        }

        public void UnregisterTask(Task task)
        {
            if (!tasks.Remove(task)) return;
            task.ProgressChanged -= OnProgress;
            task.Completed -= OnCompleted;
        }
        #endregion

        #region Private Methods
        private void OnProgress(Task task) { TaskProgressChanged?.Invoke(task); }

        private void OnCompleted(Task task)
        {
            if (!task.Check()) return;
            if (SkillManager.Instance != null) SkillManager.Instance.ProcessResult(task);
            TaskCompleted?.Invoke(task);
        }
        #endregion
    }
}
