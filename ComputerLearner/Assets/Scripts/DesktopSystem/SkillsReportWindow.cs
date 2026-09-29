using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>Renders the SkillManager results in the report window.</summary>
    public class SkillsReportWindow : MonoBehaviour
    {
        [SerializeField] private Transform gradesContainer;
        [SerializeField] private GameObject gradeRowPrefab;

        private readonly Dictionary<SkillID, SkillGradeRowUI> rows = new Dictionary<SkillID, SkillGradeRowUI>();
        private SkillManager subscribedManager;

        private void OnEnable()
        {
            subscribedManager = SkillManager.Instance;
            if (subscribedManager != null)
                subscribedManager.GradesChanged += RefreshGrades;
            RefreshGrades();
        }

        private void OnDisable()
        {
            if (subscribedManager != null)
                subscribedManager.GradesChanged -= RefreshGrades;
            subscribedManager = null;
        }

        public void RefreshGrades()
        {
            if (gradesContainer == null || gradeRowPrefab == null)
                return;

            SkillManager manager = SkillManager.Instance;
            if (manager == null)
                return;

            Dictionary<SkillID, float> averages = manager.GetAllAverageGrades();
            for (int i = 0; i < (int)SkillID.NUM_SKILLS; i++)
            {
                SkillID skill = (SkillID)i;
                SkillGradeRowUI row;
                if (!rows.TryGetValue(skill, out row) || row == null)
                {
                    GameObject instance = Instantiate(gradeRowPrefab, gradesContainer);
                    row = instance.GetComponent<SkillGradeRowUI>();
                    if (row == null)
                    {
                        Debug.LogError("SkillGradeRow prefab needs SkillGradeRowUI.", instance);
                        Destroy(instance);
                        continue;
                    }
                    rows[skill] = row;
                }

                row.Setup(skill, averages[skill], manager.GetSkillDetails(skill));
            }
        }
    }
}
