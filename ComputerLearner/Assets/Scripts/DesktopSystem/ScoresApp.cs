/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Virtual Desktop application for viewing player level scores, medals (Bronze, Silver, Gold),
 *              completion times, and historical records in a retro OS style window.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class ScoresApp : MonoBehaviour
    {
        private Window parentWindow;
        private RectTransform contentContainer;
        private TextMeshProUGUI headerSummaryText;
        private Transform listContent;
        private readonly List<GameObject> rowInstances = new List<GameObject>();

        private void Awake()
        {
            parentWindow = GetComponentInParent<Window>();
        }

        private void Start()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null)
            {
                parentWindow.setTitle("Scores & Achievements");
            }

            BuildUI();
            RefreshData();

            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged += RefreshData;
            }
        }

        private void OnDestroy()
        {
            if (ProgressData.Instance != null)
            {
                ProgressData.Instance.OnProgressChanged -= RefreshData;
            }
        }

        private void BuildUI()
        {
            // If already built, return
            if (contentContainer != null) return;

            // Ensure this GameObject has a RectTransform filling parent
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 1. App Background (Classic Retro Windows 95/98 Light Gray)
            Image bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.88f, 0.90f, 0.92f, 1f);

            // Main vertical layout
            VerticalLayoutGroup mainLayout = GetComponent<VerticalLayoutGroup>();
            if (mainLayout == null) mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(16, 16, 14, 14);
            mainLayout.spacing = 10;
            mainLayout.childControlHeight = true;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childForceExpandWidth = true;

            // 2. Header Panel (Trophy banner & counters)
            GameObject headerPanel = new GameObject("HeaderPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            headerPanel.transform.SetParent(transform, false);
            headerPanel.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f);
            LayoutElement headerLE = headerPanel.GetComponent<LayoutElement>();
            headerLE.preferredHeight = 90;
            headerLE.flexibleWidth = 1f;

            VerticalLayoutGroup headerLayout = headerPanel.GetComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(14, 14, 10, 10);
            headerLayout.spacing = 4;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = true;

            // App Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "<b>🏆 Hall of Fame & Level Records</b>";
            titleText.fontSize = 20;
            titleText.color = new Color(0.12f, 0.22f, 0.35f);

            // Summary text (Medal counts and accuracy)
            GameObject summaryObj = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            summaryObj.transform.SetParent(headerPanel.transform, false);
            headerSummaryText = summaryObj.GetComponent<TextMeshProUGUI>();
            headerSummaryText.text = "Loading statistics...";
            headerSummaryText.fontSize = 15;
            headerSummaryText.color = new Color(0.25f, 0.30f, 0.35f);

            // 3. Toolbar (Refresh & Clear actions)
            GameObject toolbar = new GameObject("Toolbar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            toolbar.transform.SetParent(transform, false);
            LayoutElement tbLE = toolbar.GetComponent<LayoutElement>();
            tbLE.preferredHeight = 32;

            HorizontalLayoutGroup tbLayout = toolbar.GetComponent<HorizontalLayoutGroup>();
            tbLayout.spacing = 10;
            tbLayout.childControlWidth = false;
            tbLayout.childForceExpandWidth = false;

            CreateStyledButton(toolbar.transform, "🔄 Refresh", new Color(0.25f, 0.55f, 0.85f), RefreshData);
            CreateStyledButton(toolbar.transform, "🗑️ Reset Progress", new Color(0.85f, 0.30f, 0.30f), PromptResetHistory);

            // 4. Scrollable Table Container
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
            scrollObj.transform.SetParent(transform, false);
            scrollObj.GetComponent<Image>().color = new Color(0.98f, 0.98f, 0.99f);
            LayoutElement scrollLE = scrollObj.GetComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1f;
            scrollLE.flexibleWidth = 1f;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(4, 4); vpRect.offsetMax = new Vector2(-4, -4);
            viewport.GetComponent<Image>().color = Color.white;

            // Content
            GameObject listContentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContentObj.transform.SetParent(viewport.transform, false);
            listContent = listContentObj.transform;
            RectTransform contentRect = listContentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = listContentObj.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 6;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter csf = listContentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = vpRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;

            contentContainer = rootRect;
        }

        public void RefreshData()
        {
            if (listContent == null) return;

            // Clear previous items
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            rowInstances.Clear();

            int goldCount = 0;
            int silverCount = 0;
            int bronzeCount = 0;
            int completedCount = 0;
            float totalAccuracySum = 0f;
            int recordedScoresCount = 0;

            // Gather all canonical levels and any discovered definitions
            List<LevelInfoData> allLevels = GetAllLevels();

            // Table Header Row
            CreateTableRow(listContent, null, isHeader: true);

            foreach (var lvl in allLevels)
            {
                LevelScoreRecord record = ProgressData.Instance != null ? ProgressData.Instance.GetLevelRecord(lvl.levelId) : null;
                bool isCompleted = ProgressData.Instance != null && ProgressData.Instance.IsLevelCompleted(lvl.levelId);
                bool isUnlocked = ProgressData.Instance != null && ProgressData.Instance.IsLevelUnlocked(lvl.levelId);

                LevelMedal medal = record != null ? record.bestMedal : LevelMedal.None;
                if (medal == LevelMedal.Gold) goldCount++;
                else if (medal == LevelMedal.Silver) silverCount++;
                else if (medal == LevelMedal.Bronze) bronzeCount++;

                if (isCompleted) completedCount++;

                if (record != null && record.bestScore > 0f)
                {
                    totalAccuracySum += record.bestScore;
                    recordedScoresCount++;
                }

                CreateTableRow(listContent, lvl, record: record, isCompleted: isCompleted, isUnlocked: isUnlocked);
            }

            // Update Header Stats
            float avgAcc = recordedScoresCount > 0 ? (totalAccuracySum / recordedScoresCount) : 0f;
            if (headerSummaryText != null)
            {
                headerSummaryText.text =
                    $"🥇 <color=#FFD700><b>Gold: {goldCount}</b></color>   " +
                    $"🥈 <color=#9E9E9E><b>Silver: {silverCount}</b></color>   " +
                    $"🥉 <color=#CD7F32><b>Bronze: {bronzeCount}</b></color>    |    " +
                    $"Completed: <b>{completedCount}/{allLevels.Count}</b>    |    " +
                    $"Average Accuracy: <b>{avgAcc:F0}%</b>";
            }
        }

        private void CreateTableRow(Transform parent, LevelInfoData lvl, LevelScoreRecord record = null, bool isCompleted = false, bool isUnlocked = false, bool isHeader = false)
        {
            GameObject row = new GameObject(isHeader ? "HeaderRow" : $"Row_{lvl?.levelId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            LayoutElement le = row.GetComponent<LayoutElement>();
            le.minHeight = isHeader ? 30 : 42;
            le.preferredHeight = isHeader ? 30 : 42;

            Image img = row.GetComponent<Image>();
            if (isHeader)
            {
                img.color = new Color(0.78f, 0.83f, 0.88f);
            }
            else
            {
                bool isAlt = rowInstances.Count % 2 == 1;
                img.color = isAlt ? new Color(0.93f, 0.95f, 0.97f) : Color.white;
                rowInstances.Add(row);
            }

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 4, 4);
            hlg.spacing = 10;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            if (isHeader)
            {
                CreateTextColumn(row.transform, "LEVEL NAME", 220, FontStyles.Bold, TextAlignmentOptions.Left, 14);
                CreateTextColumn(row.transform, "STATUS", 110, FontStyles.Bold, TextAlignmentOptions.Center, 14);
                CreateTextColumn(row.transform, "MEDAL", 120, FontStyles.Bold, TextAlignmentOptions.Center, 14);
                CreateTextColumn(row.transform, "ACCURACY", 100, FontStyles.Bold, TextAlignmentOptions.Center, 14);
                CreateTextColumn(row.transform, "BEST TIME", 100, FontStyles.Bold, TextAlignmentOptions.Center, 14);
                CreateTextColumn(row.transform, "ATTEMPTS", 80, FontStyles.Bold, TextAlignmentOptions.Center, 14);
            }
            else
            {
                // Level Name with Icon
                string displayName = !string.IsNullOrEmpty(lvl.displayName) ? lvl.displayName : lvl.levelId;
                CreateTextColumn(row.transform, displayName, 220, FontStyles.Normal, TextAlignmentOptions.Left, 14, isUnlocked ? Color.black : new Color(0.55f, 0.55f, 0.55f));

                // Status
                string statusText = isCompleted ? "<color=#28A745>Completed</color>" : (isUnlocked ? "<color=#17A2B8>Unlocked</color>" : "<color=#888888>Locked</color>");
                CreateTextColumn(row.transform, statusText, 110, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Medal Badge
                LevelMedal medal = record != null ? record.bestMedal : LevelMedal.None;
                string medalLabel = LevelScoreRecord.GetMedalEmoji(medal);
                string hex = LevelScoreRecord.GetMedalColorHex(medal);
                if (medal != LevelMedal.None)
                {
                    medalLabel = $"<color={hex}><b>{medalLabel} {LevelScoreRecord.GetMedalName(medal)}</b></color>";
                }
                else
                {
                    medalLabel = "<color=#999999>—</color>";
                }
                CreateTextColumn(row.transform, medalLabel, 120, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Best Accuracy
                string accText = (record != null && record.bestScore > 0f) ? $"{record.bestScore:F0}%" : "—";
                CreateTextColumn(row.transform, accText, 100, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Best Time
                string timeText = "—";
                if (record != null && record.bestTimeSeconds > 0f)
                {
                    int m = (int)(record.bestTimeSeconds / 60f);
                    int s = (int)(record.bestTimeSeconds % 60f);
                    timeText = $"{m:00}:{s:00}";
                }
                CreateTextColumn(row.transform, timeText, 100, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Attempts Count
                string attemptsText = record != null && record.attemptsCount > 0 ? $"{record.attemptsCount}" : "0";
                CreateTextColumn(row.transform, attemptsText, 80, FontStyles.Normal, TextAlignmentOptions.Center, 13);
            }
        }

        private TextMeshProUGUI CreateTextColumn(Transform parent, string text, float width, FontStyles style, TextAlignmentOptions alignment, int fontSize, Color? color = null)
        {
            GameObject go = new GameObject("Col", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.fontSize = fontSize;
            tmp.color = color ?? new Color(0.2f, 0.2f, 0.2f);
            tmp.raycastTarget = false;

            return tmp;
        }

        private Button CreateStyledButton(Transform parent, string label, Color color, Action onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.preferredWidth = 140;
            le.preferredHeight = 30;

            Image img = btnObj.GetComponent<Image>();
            img.color = color;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            return btn;
        }

        private void PromptResetHistory()
        {
            if (ContextMenuController.Instance != null)
            {
                ContextMenuController.Instance.ShowInputModal(
                    "Reset Scores & Records",
                    "Type RESET to confirm clearing all records:",
                    "Confirm Reset",
                    (confirmation) =>
                    {
                        if (string.Equals(confirmation?.Trim(), "RESET", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ProgressData.Instance != null) ProgressData.Instance.ClearHistory();
                            RefreshData();
                        }
                    }
                );
            }
            else
            {
                if (ProgressData.Instance != null) ProgressData.Instance.ClearHistory();
                RefreshData();
            }
        }

        private class LevelInfoData
        {
            public string levelId;
            public string displayName;
        }

        private List<LevelInfoData> GetAllLevels()
        {
            List<LevelInfoData> levels = new List<LevelInfoData>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Canonical sequential levels
            string[] canonicalNames = {
                "Level 1: Mouse Movement",
                "Level 2: Precision Clicks",
                "Level 3: Hover & Hold",
                "Level 4: Drag & Drop",
                "Level 5: Double Click",
                "Level 6: Right Click",
                "Level 7: Hitbox Challenge",
                "Level 8: Moving Targets",
                "Level 9: Mixed Skills",
                "Level 10: Copy & Paste"
            };

            for (int i = 0; i < ProgressData.LevelOrder.Length; i++)
            {
                string id = ProgressData.LevelOrder[i];
                string name = (i < canonicalNames.Length) ? canonicalNames[i] : $"Level {i + 1}";
                levels.Add(new LevelInfoData { levelId = id, displayName = name });
                seen.Add(id);
            }

            // Find any ScriptableObject definitions loaded
            LevelDefinition[] defs = Resources.FindObjectsOfTypeAll<LevelDefinition>();
            if (defs != null)
            {
                foreach (var d in defs)
                {
                    if (d == null || string.IsNullOrEmpty(d.levelId)) continue;
                    if (!seen.Contains(d.levelId))
                    {
                        levels.Add(new LevelInfoData { levelId = d.levelId, displayName = !string.IsNullOrEmpty(d.displayName) ? d.displayName : d.name });
                        seen.Add(d.levelId);
                    }
                }
            }

            return levels;
        }
    }
}
