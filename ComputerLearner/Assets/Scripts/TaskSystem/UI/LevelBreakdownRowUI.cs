using System.Globalization;
using TMPro;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>One completed level's result in a skill accordion.</summary>
    public class LevelBreakdownRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI errorsText;
        [SerializeField] private TextMeshProUGUI timeText;

        public void Setup(LevelID level, SkillEvaluationData data)
        {
            if (levelText != null) levelText.text = $"Level {(int)level + 1}";
            if (gradeText != null) gradeText.text = $"Grade: {data.Grade.ToString("F1", CultureInfo.InvariantCulture)}";
            if (errorsText != null) errorsText.text = $"Errors: {data.ErrorCount}";
            if (timeText != null) timeText.text = $"Time: {data.TimeTaken.ToString("F1", CultureInfo.InvariantCulture)} s";
        }
    }
}
