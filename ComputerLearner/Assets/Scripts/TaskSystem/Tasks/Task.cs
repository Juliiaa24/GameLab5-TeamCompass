/**
 * Author: Julia Vera
 * Date: 25/09
 * Description:
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// A individual task the player has to perform with skills associated to it
    /// </summary>
    [Serializable] 
    public abstract class Task : MonoBehaviour
    {

        #region Public Variables

        #endregion

        #region Private Variables

        #endregion

        #region Protected Variables
        
        //protected Dictionary<SkillID, Skill> skillMap;

        protected bool completed;

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes 
        public bool IsCompleted() { return completed; }

        public abstract bool Check();
        public abstract void Feedback();

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
