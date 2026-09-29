using UnityEngine;
using TMPro;

namespace ComputerLearning
{
    public class LevelBreakdownUI : MonoBehaviour
    {
        public TextMeshProUGUI levelNameText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI gradeText;

        public void Setup(LevelID levelID, SkillEvaluationData data)
        {
            levelNameText.text = levelID.ToString();
            gradeText.text = data.Grade.ToString("F1");
            
            // Textual breakdown
            statsText.text = $"Errors/Fails: {data.ErrorCount} | Time: {data.TimeTaken:F1}s";
        }
    }
}
