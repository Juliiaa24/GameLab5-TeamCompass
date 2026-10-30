/**
 * Author: Julia & AI (TeamCompass)
 * Date: 12/10/26
 * Description: UI Controller for the Stats & Assessment scene.
 *              Populates existing scene UI elements with historical assessment results
 *              and live skill proficiency metrics.
 *              Enhanced with self-healing navigation and sibling ordering.
 */
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

        private void Awake()
        {
            // Ensure HeaderBar is rendered on top of cards so raycasts are never blocked
            Transform headerBar = transform.Find("HeaderBar");
            if (headerBar == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) headerBar = canvas.transform.Find("HeaderBar");
            }
            if (headerBar != null)
            {
                headerBar.SetAsLastSibling();
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshLocalizedUI;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLocalizedUI;
        }

        private void Start()
        {
            SetupNavigation();
            RefreshLocalizedUI();
        }

        public void RefreshLocalizedUI()
        {
            LocalizeStaticSceneTexts();
            PopulateAssessmentData();
            PopulateSkillsData();
        }

        private void LocalizeStaticSceneTexts()
        {
            UIDialogs ui = UIDialogs.Current;
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            if (backToMenuButton != null)
            {
                TMP_Text t = backToMenuButton.GetComponentInChildren<TMP_Text>(true);
                if (t != null) t.text = ui.stats_btn_back;
            }

            if (retakeAssessmentButton != null)
            {
                TMP_Text t = retakeAssessmentButton.GetComponentInChildren<TMP_Text>(true);
                if (t != null) t.text = ui.stats_btn_retake;
            }

            SetTextAtRelativePath(root, "HeaderBar/Title", ui.stats_page_title);
            SetTextAtRelativePath(root, "ContentArea/AssessmentCard/Header/CardTitle", ui.stats_card_assessment_title);
            SetTextAtRelativePath(root, "ContentArea/AssessmentCard/Header/CardSubtitle", ui.stats_card_assessment_subtitle);
            SetTextAtRelativePath(root, "ContentArea/SkillsCard/Header/CardTitle", ui.stats_card_skills_title);
            SetTextAtRelativePath(root, "ContentArea/SkillsCard/Header/CardSubtitle", ui.stats_card_skills_subtitle);
        }

        private void SetTextAtRelativePath(Transform root, string path, string textValue)
        {
            if (root == null || string.IsNullOrEmpty(textValue)) return;
            Transform target = root.Find(path);
            if (target != null)
            {
                TMP_Text tmp = target.GetComponent<TMP_Text>();
                if (tmp != null) tmp.text = textValue;
            }
        }

        private void SetupNavigation()
        {
            // Self-healing fallback if buttons are unassigned in Inspector
            if (backToMenuButton == null)
            {
                Transform btnT = transform.Find("HeaderBar/Btn-Back");
                if (btnT == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null) btnT = canvas.transform.Find("HeaderBar/Btn-Back");
                }
                if (btnT != null) backToMenuButton = btnT.GetComponent<Button>();
            }

            if (retakeAssessmentButton == null)
            {
                Transform btnT = transform.Find("HeaderBar/Btn-Retake");
                if (btnT == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null) btnT = canvas.transform.Find("HeaderBar/Btn-Retake");
                }
                if (btnT != null) retakeAssessmentButton = btnT.GetComponent<Button>();
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (retakeAssessmentButton != null)
            {
                retakeAssessmentButton.onClick.RemoveAllListeners();
                retakeAssessmentButton.onClick.AddListener(OnRetakeAssessmentClicked);
            }
        }

        public void OnBackToMenuClicked()
        {
            FeedbackManager.PlayClick();
            if (SceneSystem.Instance != null)
            {
                SceneSystem.Instance.ChangeToMenu();
            }
            else
            {
                SceneManager.LoadScene("Menu");
            }
        }

        public void OnRetakeAssessmentClicked()
        {
            FeedbackManager.PlayClick();
            if (SceneSystem.Instance != null)
            {
                SceneSystem.Instance.ChangeToAssessment();
            }
            else
            {
                SceneManager.LoadScene("AssessmentScene");
            }
        }

        public void PopulateAssessmentData()
        {
            UIDialogs ui = UIDialogs.Current;
            AssessmentExportReport latestReport = LoadLatestAssessmentReport();

            if (latestReport == null || latestReport.skills == null || latestReport.skills.Count == 0)
            {
                if (summaryBox != null) summaryBox.SetActive(false);
                if (emptyStateText != null)
                {
                    emptyStateText.gameObject.SetActive(true);
                    emptyStateText.text = ui.stats_empty_state;
                }
                return;
            }

            if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);
            if (summaryBox != null) summaryBox.SetActive(true);

            if (summaryText != null)
            {
                string timeInfo = !string.IsNullOrEmpty(latestReport.formattedTime) ? latestReport.formattedTime : ui.stats_recent_label;
                summaryText.text = string.Format(ui.stats_summary_format, timeInfo, latestReport.completedTasks, latestReport.totalTasksTested);
            }

            if (avgScoreText != null)
            {
                avgScoreText.text = string.Format(ui.stats_average_format, latestReport.averageScore);
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
            UIDialogs ui = UIDialogs.Current;

            // Clear existing rows
            foreach (Transform child in skillsTableContainer)
            {
                Destroy(child.gameObject);
            }

            var skills = new (string id, string name)[]
            {
                ("mouse_move", ui.skill_mouse_move),
                ("hover", ui.skill_hover),
                ("click", ui.skill_click),
                ("double_click", ui.skill_double_click),
                ("right_click", ui.skill_right_click),
                ("click_hold", ui.skill_click_hold),
                ("drag_drop", ui.skill_drag_drop)
            };

            foreach (var sk in skills)
            {
                float score = SkillManager.GetPersistentScore(sk.id);
                CreateSkillRow(skillsTableContainer, sk.name, score);
            }
        }

        private void CreateSkillRow(Transform parent, string skillName, float score)
        {
            UIDialogs ui = UIDialogs.Current;
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
            string levelLabel = (score >= 80f) ? ui.stats_level_mastered : (score >= 50f) ? ui.stats_level_proficient : ui.stats_level_practicing;

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
