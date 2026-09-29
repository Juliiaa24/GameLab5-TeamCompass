/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Level abstarct class
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public enum LevelID
    {
        LEVEL1 = 0,
        LEVEL2,
        LEVEL3,
        LEVEL4,
        LEVEL5,
        LEVEL6,
        LEVEL7,
        LEVEL8,
        LEVEL9,
        LEVEL10,
        LEVEL11,
        LEVEL12,
        LEVEL13,
        LEVEL14,
        LEVEL15,
        LEVEL16,
        LEVEL17,
        LEVEL18,
        LEVEL19,
        NUM_LEVELS
    }
    /// <summary>
    /// Level with the task that the player needs to complete
    /// </summary>
    [Serializable]
    public abstract class Level : MonoBehaviour
    {
        #region Public Variables

        #endregion

        #region Private Variables
        #endregion

        #region Protected Variables
        [SerializeField]protected Task[] tasks;
        protected int tasksCount;
        protected bool levelCompleted = false;
        [SerializeField] protected List<Skill> skills;
        [SerializeField] protected GameObject tutorialWindow;
        [SerializeField] protected LevelID id;
        [SerializeField] protected string levelName;
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {

        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            ResetLevel();
            Managers.Lm().CloseLevel();   
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public string LevelName => levelName;

        public virtual void ShowTutorial()
        {
            Debug.Log("ShowTutorial");
            Window window = Managers.Wm().OpenWindow(tutorialWindow, null, "Tutorial");
            Managers.Wm().FocusWindow(window);
            foreach (var skill in skills) 
            {
                window.SetContent(Instantiate(skill.getTutorial()));
                window.Content.GetComponent<Tutorial>().Play();
            }
        }
        public bool IsCompleted() { return levelCompleted; }
        private bool hasEnded = false;
        protected float levelStartTime;
        protected int initialTasksCount;

        public void CheckTasks()
        {
            if (hasEnded) return;

            if (!levelCompleted)
            {
                foreach (var task in tasks)
                {
                    if (task != null && task.Check())
                    {
                        OnTaskCompleted(task);
                        task.Feedback();
                        tasksCount--;
                    }
                }
                
                if (tasksCount <= 0) 
                {
                    levelCompleted = true;
                }
            }
            
            if (levelCompleted && !hasEnded)
            {
                hasEnded = true;
                OnEnd();
            }
        }

        public void RegisterTasks()
        {
            tasks = GetComponentsInChildren<Task>();
            tasksCount = tasks.Length;
            initialTasksCount = tasks.Length;
            levelStartTime = Time.time;
        }

        public void ResetLevel()
        {
            if (tasks != null)
            {
                Array.Clear(tasks, 0, Mathf.Min(tasksCount, tasks.Length));
            }
            tasksCount = 0;
            levelCompleted = false;
            hasEnded = false;
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class

        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class
        protected virtual void OnTaskCompleted(Task task)
        {
        }

        protected virtual void EvaluateSkills()
        {
        }

        protected virtual void OnEnd()
        {
            EvaluateSkills();
            Managers.Lm().CompleteLevel(id);
        }

        #endregion
    }
}
