/**
 * Author: AI
 * Date: 28/09/2026
 * Description: UI component for a single skill grade row. (Updated with Accordion)
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TextMeshPro is used for UI text
using System.Collections.Generic;

namespace ComputerLearning
{
    /// <summary>
    /// Updates the UI text and visual components for a single skill's grade.
    /// </summary>
    public class SkillGradeUI : MonoBehaviour
    {
        #region Public Variables
        #endregion

        #region Private Variables
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private Slider gradeProgressSlider; // Optional visual bar

        [Header("Accordion Components")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject breakdownContainer;
        [SerializeField] private LevelBreakdownUI breakdownPrefab;

        private bool isExpanded = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleBreakdown);
            }
            if (breakdownContainer != null)
            {
                breakdownContainer.SetActive(false);
            }
        }
        #endregion

        #region Public Methods
        public void Setup(SkillID skill, float grade, Dictionary<LevelID, SkillEvaluationData> details)
        {
            if (skillNameText != null)
            {
                skillNameText.text = skill.ToString();
            }

            if (gradeText != null)
            {
                gradeText.text = grade.ToString("F1") + " / 10.0"; // Example formatting
            }

            if (gradeProgressSlider != null)
            {
                gradeProgressSlider.maxValue = 10f; // Assuming 0-10 scale
                gradeProgressSlider.value = grade;
            }

            // Clear previous breakdowns if any
            if (breakdownContainer != null)
            {
                foreach (Transform child in breakdownContainer.transform)
                {
                    Destroy(child.gameObject);
                }

                // Instantiate breakdowns
                if (breakdownPrefab != null && details != null)
                {
                    foreach (var kvp in details)
                    {
                        LevelBreakdownUI ui = Instantiate(breakdownPrefab, breakdownContainer.transform);
                        ui.Setup(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        public void ToggleBreakdown()
        {
            isExpanded = !isExpanded;
            if (breakdownContainer != null)
            {
                breakdownContainer.SetActive(isExpanded);
            }
            
            // Force Canvas layout rebuild so the ScrollRect pushes elements down correctly
            if (transform.parent != null && transform.parent.GetComponent<RectTransform>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
            }
        }
        #endregion
    }
}
