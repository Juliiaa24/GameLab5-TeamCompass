/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;
namespace ComputerLearning
{
    /// <summary>
    /// Enum that stores the names of every existing skill to be used as index on the skill map
    /// </summary>
    enum Skills
    {
        MOVE = 0,
        CLICK,
        HOLD,
        DROP,
        SCROLL,
        NUM_SKILLS
    }

    /// <summary>
    /// Abstract class skill, it contains a tutorial, a method to show said tutorial and
    /// </summary>
    public abstract class Skill : MonoBehaviour
    {

        #region Public Variables

        #endregion

        #region Private Variables

        #endregion

        #region Protected Variables
        [SerializeField] protected Tutorial tutorial;
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Start()
        {

        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes
        public void ShowTutorial()
        {

        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion
    }
}
