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
        /// <summary>
        /// WindowManager
        /// </summary>
        /// <returns></returns>
        public static WindowManager Wm()
        {
            return WindowManager.Instance;
        }
       
    }
    
}
