/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Virtual Desktop application for viewing player level scores, medals (Bronze, Silver, Gold),
 *              completion times, and historical records in a retro OS style window.
 *              Enhanced with real medal sprite icons, clean styling and retro aesthetics.
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
            if (contentContainer != null) return;

            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 1. App Background (Classic Retro Windows 95/98 Light Gray)
            Image bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.86f, 0.88f, 0.90f, 1f);

            // Main vertical layout
            VerticalLayoutGroup mainLayout = GetComponent<VerticalLayoutGroup>();
            if (mainLayout == null) mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(14, 14, 12, 12);
            mainLayout.spacing = 8;
            mainLayout.childControlHeight = true;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childForceExpandWidth = true;

            // 2. Header Panel (Trophy banner & counters)
            GameObject headerPanel = new GameObject("HeaderPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            headerPanel.transform.SetParent(transform, false);
            headerPanel.GetComponent<Image>().color = new Color(0.95f, 0.97f, 0.99f);
            LayoutElement headerLE = headerPanel.GetComponent<LayoutElement>();
            headerLE.preferredHeight = 84;
            headerLE.flexibleWidth = 1f;

            VerticalLayoutGroup headerLayout = headerPanel.GetComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(16, 16, 8, 8);
            headerLayout.spacing = 4;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = true;

            // App Title Row
            GameObject titleRow = new GameObject("TitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(headerPanel.transform, false);
            HorizontalLayoutGroup trHlg = titleRow.GetComponent<HorizontalLayoutGroup>();
            trHlg.spacing = 10;
            trHlg.childControlWidth = false;
            trHlg.childForceExpandWidth = false;
            trHlg.childAlignment = TextAnchor.MiddleLeft;

            GameObject titleIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            titleIconObj.transform.SetParent(titleRow.transform, false);
            RectTransform tiRt = titleIconObj.GetComponent<RectTransform>();
            tiRt.sizeDelta = new Vector2(24, 24);
            Image tiImg = titleIconObj.GetComponent<Image>();
            tiImg.sprite = MedalVisualUtility.GetTrophyAppSprite();
            tiImg.preserveAspect = true;

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(titleRow.transform, false);
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "<b>Hall of Fame & Level Records</b>";
            titleText.fontSize = 18;
            titleText.color = new Color(0.12f, 0.20f, 0.32f);

            // Summary text (Medal counts and accuracy)
            GameObject summaryObj = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            summaryObj.transform.SetParent(headerPanel.transform, false);
            headerSummaryText = summaryObj.GetComponent<TextMeshProUGUI>();
            headerSummaryText.text = "Loading statistics...";
            headerSummaryText.fontSize = 14;
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

            CreateStyledButton(toolbar.transform, "Refresh Records", new Color(0.20f, 0.50f, 0.80f), RefreshData);
            CreateStyledButton(toolbar.transform, "Reset Records", new Color(0.80f, 0.25f, 0.25f), PromptResetHistory);

            // 4. Scrollable Table Container
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
            scrollObj.transform.SetParent(transform, false);
            scrollObj.GetComponent<Image>().color = new Color(0.97f, 0.98f, 0.99f);
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
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.spacing = 4;
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
                    $"<color=#FFB800>● <b>Gold: {goldCount}</b></color>    " +
                    $"<color=#8E9CA8>● <b>Silver: {silverCount}</b></color>    " +
                    $"<color=#C87D3B>● <b>Bronze: {bronzeCount}</b></color>   |   " +
                    $"Completed: <b>{completedCount}/{allLevels.Count}</b>   |   " +
                    $"Avg Accuracy: <b>{avgAcc:F0}%</b>";
            }
        }

        private void CreateTableRow(Transform parent, LevelInfoData lvl, LevelScoreRecord record = null, bool isCompleted = false, bool isUnlocked = false, bool isHeader = false)
        {
            GameObject row = new GameObject(isHeader ? "HeaderRow" : $"Row_{lvl?.levelId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            LayoutElement le = row.GetComponent<LayoutElement>();
            le.minHeight = isHeader ? 28 : 38;
            le.preferredHeight = isHeader ? 28 : 38;

            Image img = row.GetComponent<Image>();
            if (isHeader)
            {
                img.color = new Color(0.75f, 0.80f, 0.86f);
            }
            else
            {
                bool isAlt = rowInstances.Count % 2 == 1;
                img.color = isAlt ? new Color(0.92f, 0.94f, 0.97f) : Color.white;
                rowInstances.Add(row);
            }

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 4, 4);
            hlg.spacing = 8;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            if (isHeader)
            {
                CreateTextColumn(row.transform, "LEVEL NAME", 220, FontStyles.Bold, TextAlignmentOptions.Left, 13);
                CreateTextColumn(row.transform, "STATUS", 100, FontStyles.Bold, TextAlignmentOptions.Center, 13);
                CreateTextColumn(row.transform, "MEDAL EARNED", 140, FontStyles.Bold, TextAlignmentOptions.Center, 13);
                CreateTextColumn(row.transform, "ACCURACY", 90, FontStyles.Bold, TextAlignmentOptions.Center, 13);
                CreateTextColumn(row.transform, "BEST TIME", 90, FontStyles.Bold, TextAlignmentOptions.Center, 13);
                CreateTextColumn(row.transform, "TRIES", 70, FontStyles.Bold, TextAlignmentOptions.Center, 13);
            }
            else
            {
                // Level Name
                string displayName = !string.IsNullOrEmpty(lvl.displayName) ? lvl.displayName : lvl.levelId;
                CreateTextColumn(row.transform, displayName, 220, FontStyles.Normal, TextAlignmentOptions.Left, 13, isUnlocked ? Color.black : new Color(0.55f, 0.55f, 0.55f));

                // Status Badge
                string statusText = isCompleted ? "<color=#28A745>Completed</color>" : (isUnlocked ? "<color=#0078D7>Unlocked</color>" : "<color=#888888>Locked</color>");
                CreateTextColumn(row.transform, statusText, 100, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Medal Column with Sprite Icon + Text
                LevelMedal medal = record != null ? record.bestMedal : LevelMedal.None;
                CreateMedalColumn(row.transform, medal, 140);

                // Best Accuracy
                string accText = (record != null && record.bestScore > 0f) ? $"{record.bestScore:F0}%" : "—";
                CreateTextColumn(row.transform, accText, 90, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Best Time
                string timeText = "—";
                if (record != null && record.bestTimeSeconds > 0f)
                {
                    int m = (int)(record.bestTimeSeconds / 60f);
                    int s = (int)(record.bestTimeSeconds % 60f);
                    timeText = $"{m:00}:{s:00}";
                }
                CreateTextColumn(row.transform, timeText, 90, FontStyles.Normal, TextAlignmentOptions.Center, 13);

                // Attempts Count
                string attemptsText = record != null && record.attemptsCount > 0 ? $"{record.attemptsCount}" : "0";
                CreateTextColumn(row.transform, attemptsText, 70, FontStyles.Normal, TextAlignmentOptions.Center, 13);
            }
        }

        private void CreateMedalColumn(Transform parent, LevelMedal medal, float width)
        {
            GameObject container = new GameObject("MedalCol", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            container.transform.SetParent(parent, false);

            LayoutElement le = container.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            if (medal != LevelMedal.None)
            {
                // Medal Image
                GameObject imgObj = new GameObject("MedalIcon", typeof(RectTransform), typeof(Image));
                imgObj.transform.SetParent(container.transform, false);
                RectTransform rt = imgObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(22, 22);

                Image img = imgObj.GetComponent<Image>();
                img.sprite = MedalVisualUtility.GetMedalSprite(medal);
                img.preserveAspect = true;

                // Medal Text
                string name = LevelScoreRecord.GetMedalName(medal);
                string hex = LevelScoreRecord.GetMedalColorHex(medal);

                GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(container.transform, false);
                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<color={hex}><b>{name}</b></color>";
                tmp.fontSize = 13;
                tmp.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                // Slot or dash
                GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(container.transform, false);
                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "<color=#999999>—</color>";
                tmp.fontSize = 13;
                tmp.alignment = TextAlignmentOptions.Center;
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
            tmp.color = color ?? new Color(0.18f, 0.20f, 0.24f);
            tmp.raycastTarget = false;

            return tmp;
        }

        private Button CreateStyledButton(Transform parent, string label, Color color, Action onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.preferredWidth = 130;
            le.preferredHeight = 28;

            Image img = btnObj.GetComponent<Image>();
            img.color = color;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                FeedbackManager.PlayClick();
                onClick?.Invoke();
            });

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
                "Level 10: Context Menu",
                "Level 11: Open Notepad & Typing",
                "Level 12: Copy, Paste & Cut",
                "Level 13: Undo Shortcut",
                "Level 14: Save Shortcut",
                "Level 15: Notepad Evaluation",
                "Level 16: Paint Usage"
            };

            for (int i = 0; i < ProgressData.LevelOrder.Length; i++)
            {
                string id = ProgressData.LevelOrder[i];
                string name = (i < canonicalNames.Length) ? canonicalNames[i] : $"Level {i + 1}";
                levels.Add(new LevelInfoData { levelId = id, displayName = name });
                seen.Add(id);
            }

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
