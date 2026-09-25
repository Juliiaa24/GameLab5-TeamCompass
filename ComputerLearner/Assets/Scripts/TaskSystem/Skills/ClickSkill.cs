/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Click skill implementation using the shared skill result tracking.
*/
namespace ComputerLearning
{
    public class ClickSkill : Skill
    {
        #region Public Methods
        public override void ShowTutorial()
        {
            if (tutorial != null) tutorial.gameObject.SetActive(true);
        }
        #endregion
    }
}
