/**
 * Author: Julia & AI (TeamCompass)
 * Date: 12/10/26
 * Description: UI Controller for the Stats & Assessment scene.
 *              Populates existing scene UI elements with the current student's assessment results
 *              and live skill proficiency metrics, and embeds the PlaytestStudentWidget for ID & Export.
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

        [Header("Medals Showcase (Optional)")]
        [SerializeField] private TextMeshProUGUI goldMedalsText;
        [SerializeField] private TextMeshProUGUI silverMedalsText;
        [SerializeField] private TextMeshProUGUI bronzeMedalsText;
        [SerializeField] private TextMeshProUGUI completedLevelsText;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            // Ensure Header is rendered on top of cards so raycasts are never blocked
            Transform headerBar = root.Find("Header") ?? root.Find("HeaderBar");
            if (headerBar != null)
            {
                headerBar.SetAsLastSibling();

                // Ensure the full-width Title text inside Header never blocks clicks on Btn-Back
                Transform titleT = headerBar.Find("Title");
                if (titleT != null)
                {
                    TMP_Text titleTmp = titleT.GetComponent<TMP_Text>();
                    if (titleTmp != null) titleTmp.raycastTarget = false;
                }

                Transform backBtnT = headerBar.Find("Btn-Back");
                if (backBtnT != null) backBtnT.SetAsLastSibling();

                Transform retakeBtnT = headerBar.Find("Btn-Retake");
                if (retakeBtnT != null) retakeBtnT.SetAsLastSibling();
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
            PopulateMedalsSummary();
            PopulateAssessmentData();
            PopulateSkillsData();
        }

        private void PopulateMedalsSummary()
        {
            if (ProgressData.Instance == null) return;
            int gold = 0, silver = 0, bronze = 0, completed = 0;
            for (int i = 1; i <= 16; i++)
            {
                LevelScoreRecord lvl = ProgressData.Instance.GetLevelRecord("level" + i);
                bool isCompleted = ProgressData.Instance.IsLevelCompleted("level" + i);
                if (isCompleted) completed++;
                if (lvl == null) continue;
                switch (lvl.bestMedal)
                {
                    case LevelMedal.Gold: gold++; break;
                    case LevelMedal.Silver: silver++; break;
                    case LevelMedal.Bronze: bronze++; break;
                }
            }

            if (goldMedalsText != null) goldMedalsText.text = gold.ToString();
            if (silverMedalsText != null) silverMedalsText.text = silver.ToString();
            if (bronzeMedalsText != null) bronzeMedalsText.text = bronze.ToString();
            if (completedLevelsText != null) completedLevelsText.text = $"{completed}/16";
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

            SetTextAtRelativePath(root, "Header/Title", ui.stats_page_title);
            SetTextAtRelativePath(root, "HeaderBar/Title", ui.stats_page_title);

            SetTextAtRelativePath(root, "AssessmentCard/CardTitle", ui.stats_card_assessment_title);
            SetTextAtRelativePath(root, "AssessmentCard/WindowTitleBar/CardTitle", ui.stats_card_assessment_title);
            SetTextAtRelativePath(root, "ContentArea/AssessmentCard/Header/CardTitle", ui.stats_card_assessment_title);

            SetTextAtRelativePath(root, "AssessmentCard/CardSubtitle", ui.stats_card_assessment_subtitle);
            SetTextAtRelativePath(root, "ContentArea/AssessmentCard/Header/CardSubtitle", ui.stats_card_assessment_subtitle);

            SetTextAtRelativePath(root, "SkillsCard/CardTitle", ui.stats_card_skills_title);
            SetTextAtRelativePath(root, "SkillsCard/WindowTitleBar/CardTitle", ui.stats_card_skills_title);
            SetTextAtRelativePath(root, "ContentArea/SkillsCard/Header/CardTitle", ui.stats_card_skills_title);

            SetTextAtRelativePath(root, "SkillsCard/CardSubtitle", ui.stats_card_skills_subtitle);
            SetTextAtRelativePath(root, "ContentArea/SkillsCard/Header/CardSubtitle", ui.stats_card_skills_subtitle);

            SetTextAtRelativePath(root, "MedalShowcaseStrip/MiniStat_Gold/Caption", ui.stats_gold_label);
            SetTextAtRelativePath(root, "MedalShowcaseStrip/MiniStat_Silver/Caption", ui.stats_silver_label);
            SetTextAtRelativePath(root, "MedalShowcaseStrip/MiniStat_Bronze/Caption", ui.stats_bronze_label);
            SetTextAtRelativePath(root, "MedalShowcaseStrip/MiniStat_Levels/Caption", ui.stats_levels_label);
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
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            // Self-healing fallback if buttons are unassigned in Inspector
            if (backToMenuButton == null)
            {
                Transform btnT = root.Find("Header/Btn-Back") ?? root.Find("HeaderBar/Btn-Back");
                if (btnT != null) backToMenuButton = btnT.GetComponent<Button>();
            }

            if (retakeAssessmentButton == null)
            {
                Transform btnT = root.Find("Header/Btn-Retake") ?? root.Find("HeaderBar/Btn-Retake");
                if (btnT != null) retakeAssessmentButton = btnT.GetComponent<Button>();
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick = new Button.ButtonClickedEvent();
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (retakeAssessmentButton != null)
            {
                retakeAssessmentButton.onClick = new Button.ButtonClickedEvent();
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
                if (assessmentTableContainer != null)
                {
                    foreach (Transform child in assessmentTableContainer)
                    {
                        Destroy(child.gameObject);
                    }
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
            RetroThemeAssets assets = RetroThemeAssets.Instance;
            GameObject rowObj = new GameObject("Row_" + sk.skillId, typeof(RectTransform), typeof(Image));
            rowObj.transform.SetParent(parent, false);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
            Image rowBg = rowObj.GetComponent<Image>();
            RetroUITheme.ApplySlicedSprite(rowBg, assets != null ? assets.cardRounded : null,
                isEven ? RetroUITheme.CardWhite : RetroUITheme.CardPastelBlue, false);

            // Cute icon per skill
            Sprite iconSprite = GetSkillIconSprite(sk.skillId, assets);
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(rowObj.transform, false);
                RectTransform iRt = iconObj.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0.5f); iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.anchoredPosition = new Vector2(12f, 0f);
                iRt.sizeDelta = new Vector2(26f, 26f);
                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Skill Name
            TextMeshProUGUI nameTxt = CreateText(rowObj.transform, sk.skillName, 15, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            nameTxt.fontStyle = FontStyles.Bold;
            RectTransform nRt = nameTxt.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0f); nRt.anchorMax = new Vector2(0.52f, 1f);
            nRt.offsetMin = new Vector2(iconSprite != null ? 46f : 14f, 0f); nRt.offsetMax = Vector2.zero;

            // Attempts & Time
            string meta = $"{sk.timeTakenSeconds:F1}s | {sk.extraAttempts}";
            TextMeshProUGUI metaTxt = CreateText(rowObj.transform, meta, 13, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform mRt = metaTxt.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.52f, 0f); mRt.anchorMax = new Vector2(0.76f, 1f);
            mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;

            // Retro Score Pill
            GameObject pillObj = new GameObject("ScorePill", typeof(RectTransform), typeof(Image));
            pillObj.transform.SetParent(rowObj.transform, false);
            RectTransform pRt = pillObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.78f, 0.16f); pRt.anchorMax = new Vector2(0.98f, 0.84f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            Color pillCol = sk.completed ? RetroUITheme.SuccessMint : RetroUITheme.DangerCoral;
            RetroUITheme.ApplySlicedSprite(pillObj.GetComponent<Image>(), assets != null ? assets.badgePill : null, pillCol, false);

            string scoreText = sk.completed ? $"{sk.score:0}%" : "0%";
            TextMeshProUGUI sVal = CreateText(pillObj.transform, scoreText, 14, RetroUITheme.InkWhite, TextAlignmentOptions.Center);
            sVal.fontStyle = FontStyles.Bold;
            RectTransform svRt = sVal.GetComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero; svRt.anchorMax = Vector2.one;
            svRt.offsetMin = Vector2.zero; svRt.offsetMax = Vector2.zero;
        }

        public void PopulateSkillsData()
        {
            if (skillsTableContainer == null) return;
            UIDialogs ui = UIDialogs.Current;

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
                CreateSkillRow(skillsTableContainer, sk.id, sk.name, score);
            }
        }

        private void CreateSkillRow(Transform parent, string skillId, string skillName, float score)
        {
            UIDialogs ui = UIDialogs.Current;
            RetroThemeAssets assets = RetroThemeAssets.Instance;

            GameObject row = new GameObject("Row_" + skillName, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
            RetroUITheme.ApplySlicedSprite(row.GetComponent<Image>(), assets != null ? assets.cardRounded : null,
                RetroUITheme.CardWhite, false);

            Sprite iconSprite = GetSkillIconSprite(skillId, assets);
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iRt = iconObj.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0.5f); iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.anchoredPosition = new Vector2(12f, 0f);
                iRt.sizeDelta = new Vector2(30f, 30f);
                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Skill Name (Top Left)
            TextMeshProUGUI nameTxt = CreateText(row.transform, skillName, 15, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            nameTxt.fontStyle = FontStyles.Bold;
            RectTransform nRt = nameTxt.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.5f); nRt.anchorMax = new Vector2(0.55f, 1f);
            nRt.offsetMin = new Vector2(iconSprite != null ? 50f : 14f, 0f); nRt.offsetMax = Vector2.zero;

            // Score Percentage (Top Right)
            Color scoreCol = (score >= 80f) ? RetroUITheme.SuccessMint : (score >= 50f) ? RetroUITheme.WarningAmber : RetroUITheme.BtnSkyBlue;
            string levelLabel = (score >= 80f) ? ui.stats_level_mastered : (score >= 50f) ? ui.stats_level_proficient : ui.stats_level_practicing;

            TextMeshProUGUI scoreTxt = CreateText(row.transform, $"{score:0}% ({levelLabel})", 13, scoreCol, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            RectTransform sRt = scoreTxt.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.55f, 0.5f); sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = new Vector2(-14f, 0f);

            // Progress Bar Background (Bottom)
            GameObject barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(row.transform, false);
            RectTransform bRt = barBg.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(1f, 0.45f);
            bRt.offsetMin = new Vector2(iconSprite != null ? 50f : 14f, 7f); bRt.offsetMax = new Vector2(-14f, -3f);
            RetroUITheme.ApplySlicedSprite(barBg.GetComponent<Image>(), assets != null ? assets.inputBox : null,
                RetroUITheme.ProgressTrack, false);

            // Progress Bar Fill
            GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform fRt = barFill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = new Vector2(Mathf.Clamp01(score / 100f), 1f);
            fRt.offsetMin = new Vector2(2f, 2f); fRt.offsetMax = new Vector2(-2f, -2f);
            RetroUITheme.ApplySlicedSprite(barFill.GetComponent<Image>(), assets != null ? assets.badgePill : null,
                scoreCol, false);
        }

        private static Sprite GetSkillIconSprite(string skillId, RetroThemeAssets assets)
        {
            if (assets == null) return null;
            switch (skillId)
            {
                case "mouse_move": return assets.mouseMoveIcon ?? assets.mouseIcon;
                case "hover": return assets.targetIcon ?? assets.mouseIcon;
                case "click":
                case "double_click": return assets.mouseLeftClickIcon ?? assets.mouseIcon;
                case "right_click": return assets.mouseRightClickIcon ?? assets.mouseIcon;
                case "click_hold": return assets.mouseHoldClickIcon ?? assets.mouseIcon;
                case "drag_drop": return assets.folderYellow ?? assets.mouseIcon;
                default: return assets.mouseIcon;
            }
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
            // If the current student has not completed the initial assessment (e.g. after Reset Progress),
            // do NOT load previous students' assessment files from disk!
            if (PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0)
            {
                return null;
            }

            AssessmentExportReport currentReport = PlaytestDataManager.GetCurrentAssessmentReport();
            if (currentReport != null)
            {
                return currentReport;
            }

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
