/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Skills manager
*/

using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public static class Managers
    {
        /// <summary>
        /// Task Manager
        /// </summary>
        /// <returns></returns>
        public static TaskManager Tm()
        {
            return TaskManager.Instance;
        }
        /// <summary>
        /// LevelManager
        /// </summary>
        /// <returns></returns>
        public static LevelManager Lm()
        {
            return LevelManager.Instance;
        }
        /// <summary>
        /// SkillManager
        /// </summary>
        /// <returns></returns>
        public static SkillManager Sm()
        {
            return SkillManager.Instance;
        }
        /// <summary>
        /// InputManager
        /// </summary>
        /// <returns></returns>
        public static InputManager Im()
        {
            return InputManager.Instance;
        }
        /// <summary>
        /// SceneSystem
        /// </summary>
        /// <returns></returns>
        public static SceneSystem Sy()
        {
            return SceneSystem.Instance;
        }
       
    }
    
}
