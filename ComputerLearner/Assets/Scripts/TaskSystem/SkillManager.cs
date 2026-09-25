/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        #region Public Variables
        /**
         * Unique Instance of the SkillManager 
         */
        public static SkillManager Instance { get; private set; }
        #endregion

        #region Private Variables
        
        private Dictionary<SkillID, Skill> allSkills;

        #endregion

        #region Protected Variables

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {

        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class


        #endregion
    }
}
