/**
 * Author: Julia & AI (TeamCompass)
 * Date: 12/10/26
 * Description: UI Controller for the Stats & Assessment scene.
 *              Populates existing scene UI elements with historical assessment results
 *              and live skill proficiency metrics.
 */
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class StatsSceneController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button retakeAssessmentButton;

        [Header("Assessment Card")]
        [SerializeField] private GameObject summaryBox;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI avgScoreText;
        [SerializeField] private TextMeshProUGUI emptyStateText;
        [SerializeField] private Transform assessmentTableContainer;

        [Header("Skills Card")]
        [SerializeField] private Transform skillsTableContainer;

        private void Start()
        {
            SetupNavigation();
            PopulateAssessmentData();
            PopulateSkillsData();
        }

        private void SetupNavigation()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(() =>
                {
                    if (SceneSystem.Instance != null) SceneSystem.Instance.ChangeToMenu();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                });
            }

            if (retakeAssessmentButton != null)
            {
                retakeAssessmentButton.onClick.RemoveAllListeners();
                retakeAssessmentButton.onClick.AddListener(() =>
                {
                    if (SceneSystem.Instance != null) SceneSystem.Instance.ChangeToAssessment();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("AssessmentScene");
                });
            }
        }

        public void PopulateAssessmentData()
        {
            AssessmentExportReport latestReport = LoadLatestAssessmentReport();

            if (latestReport == null || latestReport.skills == null || latestReport.skills.Count == 0)
            {
                if (summaryBox != null) summaryBox.SetActive(false);
                if (emptyStateText != null)
                {
                    emptyStateText.gameObject.SetActive(true);
                    emptyStateText.text = "No assessment records found yet.\n\nPlay from the Main Menu or click 'Retake Assessment' to record your first diagnostic evaluation!";
                }
                return;
            }

            if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);
            if (summaryBox != null) summaryBox.SetActive(true);

            if (summaryText != null)
            {
                string timeInfo = !string.IsNullOrEmpty(latestReport.formattedTime) ? latestReport.formattedTime : "Recent";
                summaryText.text = $"Completed at: {timeInfo} | Tasks: {latestReport.completedTasks}/{latestReport.totalTasksTested}";
            }

            if (avgScoreText != null)
            {
                avgScoreText.text = $"Average: {latestReport.averageScore:0}%";
            }

            if (assessmentTableContainer != null)
            {
                // Clear existing row items
                foreach (Transform child in assessmentTableContainer)
                {
                    Destroy(child.gameObject);
                }

                int row = 0;
                foreach (var sk in latestReport.skills)
                {
                    CreateAssessmentRow(assessmentTableContainer, sk, row % 2 == 0);
                    row++;
                }
            }
        }

        private void CreateAssessmentRow(Transform parent, AssessmentSkillExportItem sk, bool isEven)
        {
            GameObject rowObj = new GameObject("Row_" + sk.skillId, typeof(RectTransform), typeof(Image));
            rowObj.transform.SetParent(parent, false);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);
            rowObj.GetComponent<Image>().color = isEven ? new Color(0.97f, 0.98f, 0.99f) : new Color(0.92f, 0.94f, 0.96f);

            // Skill Name
            TextMeshProUGUI nameTxt = CreateText(rowObj.transform, sk.skillName, 13, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.Left);
            RectTransform nRt = nameTxt.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0f); nRt.anchorMax = new Vector2(0.5f, 1f);
            nRt.offsetMin = new Vector2(12f, 0f); nRt.offsetMax = Vector2.zero;

            // Attempts & Time
            string meta = $"{sk.timeTakenSeconds:F1}s | {sk.extraAttempts} err";
            TextMeshProUGUI metaTxt = CreateText(rowObj.transform, meta, 12, new Color(0.45f, 0.50f, 0.58f), TextAlignmentOptions.Center);
            RectTransform mRt = metaTxt.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0f); mRt.anchorMax = new Vector2(0.78f, 1f);
            mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;

            // Score Badge
            Color col = sk.completed ? new Color(0.09f, 0.64f, 0.29f) : new Color(0.86f, 0.15f, 0.15f);
            string scoreText = sk.completed ? $"{sk.score:0}%" : "0%";
            TextMeshProUGUI sVal = CreateText(rowObj.transform, scoreText, 14, col, TextAlignmentOptions.Right);
            sVal.fontStyle = FontStyles.Bold;
            RectTransform svRt = sVal.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.78f, 0f); svRt.anchorMax = new Vector2(1f, 1f);
            svRt.offsetMin = Vector2.zero; svRt.offsetMax = new Vector2(-12f, 0f);
        }

        public void PopulateSkillsData()
        {
            if (skillsTableContainer == null) return;

            // Clear existing rows
            foreach (Transform child in skillsTableContainer)
            {
                Destroy(child.gameObject);
            }

            var skills = new (string id, string name)[]
            {
                ("mouse_move", "Move Cursor"),
                ("hover", "Hover"),
                ("click", "Single Click"),
                ("double_click", "Double Click"),
                ("right_click", "Right Click"),
                ("click_hold", "Click and Hold"),
                ("drag_drop", "Drag and Drop")
            };

            foreach (var sk in skills)
            {
                float score = SkillManager.GetPersistentScore(sk.id);
                CreateSkillRow(skillsTableContainer, sk.name, score);
            }
        }

        private void CreateSkillRow(Transform parent, string skillName, float score)
        {
            GameObject row = new GameObject("Row_" + skillName, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
            row.GetComponent<Image>().color = new Color(0.97f, 0.98f, 0.99f);

            // Skill Name (Top Left)
            TextMeshProUGUI nameTxt = CreateText(row.transform, skillName, 13, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.Left);
            nameTxt.fontStyle = FontStyles.Bold;
            RectTransform nRt = nameTxt.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.5f); nRt.anchorMax = new Vector2(0.5f, 1f);
            nRt.offsetMin = new Vector2(12f, 0f); nRt.offsetMax = Vector2.zero;

            // Score Percentage (Top Right)
            Color scoreCol = (score >= 80f) ? new Color(0.09f, 0.64f, 0.29f) : (score >= 50f) ? new Color(0.85f, 0.47f, 0.02f) : new Color(0.45f, 0.50f, 0.58f);
            string levelLabel = (score >= 80f) ? "Mastered" : (score >= 50f) ? "Proficient" : "Practicing";

            TextMeshProUGUI scoreTxt = CreateText(row.transform, $"{score:0}% ({levelLabel})", 12, scoreCol, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            RectTransform sRt = scoreTxt.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.5f); sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = new Vector2(-12f, 0f);

            // Progress Bar Background (Bottom)
            GameObject barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(row.transform, false);
            RectTransform bRt = barBg.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.offsetMin = new Vector2(12f, 6f); bRt.offsetMax = new Vector2(-12f, -4f);
            barBg.GetComponent<Image>().color = new Color(0.88f, 0.90f, 0.93f);

            // Progress Bar Fill
            GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform fRt = barFill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.pivot = new Vector2(0f, 0.5f);
            fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
            fRt.localScale = new Vector3(Mathf.Clamp01(score / 100f), 1f, 1f);
            barFill.GetComponent<Image>().color = scoreCol;
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int size, Color color, TextAlignmentOptions align)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }

        private AssessmentExportReport LoadLatestAssessmentReport()
        {
            try
            {
                string folder = Path.Combine(Application.dataPath, "Assessment");
                if (!Directory.Exists(folder)) return null;

                string[] files = Directory.GetFiles(folder, "*.json");
                if (files.Length == 0) return null;

                Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

                string newest = files[0];
                string content = File.ReadAllText(newest);
                if (string.IsNullOrEmpty(content)) return null;

                return JsonUtility.FromJson<AssessmentExportReport>(content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StatsSceneController] Error reading assessment report: {ex.Message}");
                return null;
            }
        }
    }
}
