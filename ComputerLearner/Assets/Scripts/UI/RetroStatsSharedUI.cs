using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// Shared Colorful Retro Desktop UI builder and data presenter used identically by both:
    /// - StatsScene.unity (via RetroSceneMaterializer + StatsSceneController)
    /// - In-game Scores App (via ScoresApp / SkillsReportWindow inside Desktop/VirtualDesktop)
    /// Ensures 1:1 visual parity, localization via UIDialogs.Current, and live ProgressData + SkillManager data.
    /// </summary>
    public static class RetroStatsSharedUI
    {
        public enum LevelFilter
        {
            All = 0,
            MouseSkills = 1,
            DesktopApps = 2,
            Completed = 3
        }

        [Serializable]
        public class StatsUIBinding
        {
            [Header("Summary Cards (5)")]
            public RectTransform summaryStripRoot;
            public TMP_Text goldValueText;
            public TMP_Text goldLabelText;
            public TMP_Text silverValueText;
            public TMP_Text silverLabelText;
            public TMP_Text bronzeValueText;
            public TMP_Text bronzeLabelText;
            public TMP_Text completedValueText;
            public TMP_Text completedLabelText;
            public TMP_Text accuracyValueText;
            public TMP_Text accuracyLabelText;

            [Header("Left Sub-Window: Level Records")]
            public TMP_Text levelsWindowTitleText;
            public Button[] tabButtons = new Button[4];
            public TMP_Text[] tabLabels = new TMP_Text[4];
            public Button pageUpButton;
            public TMP_Text pageUpLabel;
            public Button pageDownButton;
            public TMP_Text pageDownLabel;
            public TMP_Text colLevelHeader;
            public TMP_Text colStatusHeader;
            public TMP_Text colMedalHeader;
            public TMP_Text colAccuracyHeader;
            public TMP_Text colTimeHeader;
            public TMP_Text colTriesHeader;
            public ScrollRect levelsScrollRect;
            public RectTransform levelsViewportRect;
            public Transform levelsContentContainer;
            public TMP_Text emptyLevelsText;

            [Header("Right Sub-Window: Skill Mastery & Initial Assessment")]
            public TMP_Text skillsWindowTitleText;
            public Transform skillsTableContainer;
            public TMP_Text assessmentSectionTitleText;
            public GameObject assessmentSummaryBox;
            public TMP_Text assessmentSummaryText;
            public TMP_Text assessmentAvgText;
            public TMP_Text assessmentEmptyText;
            public Transform assessmentTableContainer;
        }

        private static readonly string[] CanonicalSkillOrder =
        {
            "mouse_move",
            "hover",
            "click",
            "double_click",
            "right_click",
            "click_hold",
            "drag_drop"
        };

        // ---------------------------------------------------------------------
        // UI CONSTRUCTION (Used at Edit-Time for StatsScene & Runtime for ScoresApp)
        // ---------------------------------------------------------------------

        public static StatsUIBinding BuildStatsBody(Transform bodyRoot, RetroThemeAssets assets, TMP_FontAsset font)
        {
            StatsUIBinding ui = new StatsUIBinding();
            if (bodyRoot == null) return ui;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            // 1. 5 Pastel Summary Cards Strip (Top 96px)
            GameObject strip = new GameObject("SummaryCardsStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            strip.transform.SetParent(bodyRoot, false);
            RectTransform stripRt = strip.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 1f);
            stripRt.anchorMax = new Vector2(1f, 1f);
            stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.offsetMin = new Vector2(0f, -96f);
            stripRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup stripHlg = strip.GetComponent<HorizontalLayoutGroup>();
            stripHlg.spacing = 12f;
            stripHlg.childControlWidth = true;
            stripHlg.childControlHeight = true;
            stripHlg.childForceExpandWidth = true;
            stripHlg.childForceExpandHeight = true;
            ui.summaryStripRoot = stripRt;

            CreateSummaryCard("Card-Gold", strip.transform, assets, font,
                assets != null ? assets.goldMedal : null,
                RetroUITheme.CardPastelYellow, RetroUITheme.BronzeAccent, dlg.scores_stat_gold,
                out ui.goldValueText, out ui.goldLabelText);

            CreateSummaryCard("Card-Silver", strip.transform, assets, font,
                assets != null ? assets.silverMedal : null,
                RetroUITheme.CardWhite, RetroUITheme.SilverAccent, dlg.scores_stat_silver,
                out ui.silverValueText, out ui.silverLabelText);

            CreateSummaryCard("Card-Bronze", strip.transform, assets, font,
                assets != null ? assets.bronzeMedal : null,
                RetroUITheme.CardPastelPeach, RetroUITheme.BronzeAccent, dlg.scores_stat_bronze,
                out ui.bronzeValueText, out ui.bronzeLabelText);

            CreateSummaryCard("Card-Completed", strip.transform, assets, font,
                assets != null ? (assets.folderYellow ?? assets.targetIcon) : null,
                RetroUITheme.CardPastelMint, RetroUITheme.SuccessMint, dlg.scores_stat_completed,
                out ui.completedValueText, out ui.completedLabelText);

            CreateSummaryCard("Card-Accuracy", strip.transform, assets, font,
                assets != null ? (assets.targetIcon ?? assets.mouseIcon) : null,
                RetroUITheme.CardPastelBlue, RetroUITheme.TitleBlue, dlg.scores_col_accuracy,
                out ui.accuracyValueText, out ui.accuracyLabelText);

            // 2. Two-Column Split Body
            GameObject split = new GameObject("SplitColumns", typeof(RectTransform));
            split.transform.SetParent(bodyRoot, false);
            RectTransform splitRt = split.GetComponent<RectTransform>();
            splitRt.anchorMin = Vector2.zero;
            splitRt.anchorMax = Vector2.one;
            splitRt.offsetMin = Vector2.zero;
            splitRt.offsetMax = new Vector2(0f, -108f);

            // LEFT COLUMN: Teal Level Records & Medals (~61%)
            BuildLevelsSubWindow(split.transform, assets, font, dlg, ui);

            // RIGHT COLUMN: Amber Skill Mastery & First Assessment (~39%)
            BuildSkillsAndAssessmentSubWindow(split.transform, assets, font, dlg, ui);

            return ui;
        }

        private static void BuildLevelsSubWindow(Transform parent, RetroThemeAssets assets, TMP_FontAsset font, UIDialogs dlg, StatsUIBinding ui)
        {
            GameObject win = CreateSlicedImage("LevelsWindow", parent, assets != null ? assets.windowBody : null, RetroUITheme.WindowCream, false);
            RectTransform winRt = win.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0f, 0f);
            winRt.anchorMax = new Vector2(0.605f, 1f);
            winRt.offsetMin = Vector2.zero;
            winRt.offsetMax = new Vector2(-6f, 0f);

            // Title bar
            GameObject bar = CreateSlicedImage("TitleBar", win.transform, assets != null ? assets.windowTitleBar : null, RetroUITheme.TitleTeal, false);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.offsetMin = new Vector2(4f, -48f);
            barRt.offsetMax = new Vector2(-4f, -4f);

            GameObject barIcon = CreateImage("Icon", bar.transform, assets != null ? assets.goldMedal : null, Color.white, false);
            RectTransform biRt = barIcon.GetComponent<RectTransform>();
            biRt.anchorMin = new Vector2(0f, 0.5f);
            biRt.anchorMax = new Vector2(0f, 0.5f);
            biRt.pivot = new Vector2(0f, 0.5f);
            biRt.anchoredPosition = new Vector2(12f, 0f);
            biRt.sizeDelta = new Vector2(28f, 28f);
            barIcon.GetComponent<Image>().preserveAspect = true;

            ui.levelsWindowTitleText = CreateTmp("LevelsWindowTitle", bar.transform, font,
                StripRichTags(dlg.scores_header_title), 19, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform lwtRt = ui.levelsWindowTitleText.GetComponent<RectTransform>();
            lwtRt.anchorMin = Vector2.zero;
            lwtRt.anchorMax = Vector2.one;
            lwtRt.offsetMin = new Vector2(48f, 2f);
            lwtRt.offsetMax = new Vector2(-12f, -2f);

            // Tabs + Pagination Row
            GameObject controlsBar = new GameObject("TabsAndPageRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            controlsBar.transform.SetParent(win.transform, false);
            RectTransform cbRt = controlsBar.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(0.5f, 1f);
            cbRt.offsetMin = new Vector2(14f, -94f);
            cbRt.offsetMax = new Vector2(-14f, -56f);

            HorizontalLayoutGroup cbHlg = controlsBar.GetComponent<HorizontalLayoutGroup>();
            cbHlg.spacing = 6f;
            cbHlg.childControlWidth = true;
            cbHlg.childControlHeight = true;
            cbHlg.childForceExpandWidth = false;
            cbHlg.childForceExpandHeight = true;

            string[] tabTitles = { dlg.scores_tab_all, dlg.scores_tab_mouse, dlg.scores_tab_desktop, dlg.scores_tab_completed };
            ui.tabButtons = new Button[4];
            ui.tabLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                Button tab = CreateRetroCompactButton($"Btn-Tab-{i}", controlsBar.transform, assets, font, tabTitles[i],
                    i == 0 ? RetroUITheme.BtnSkyBlue : RetroUITheme.BtnNeutralCream,
                    i == 0 ? RetroUITheme.InkWhite : RetroUITheme.InkDark, 13, 1.35f, out TMP_Text tabLbl);
                ui.tabButtons[i] = tab;
                ui.tabLabels[i] = tabLbl;
            }

            ui.pageUpButton = CreateRetroCompactButton("Btn-PageUp", controlsBar.transform, assets, font, dlg.scores_btn_page_up,
                RetroUITheme.BtnSoftSlate, RetroUITheme.InkDark, 13, 0.85f, out ui.pageUpLabel);
            ui.pageDownButton = CreateRetroCompactButton("Btn-PageDown", controlsBar.transform, assets, font, dlg.scores_btn_page_down,
                RetroUITheme.BtnSoftSlate, RetroUITheme.InkDark, 13, 0.85f, out ui.pageDownLabel);

            // Column Headers Pill
            GameObject colBar = CreateSlicedImage("ColumnHeaders", win.transform, assets != null ? assets.cardRounded : null, RetroUITheme.CardPastelBlue, false);
            RectTransform chRt = colBar.GetComponent<RectTransform>();
            chRt.anchorMin = new Vector2(0f, 1f);
            chRt.anchorMax = new Vector2(1f, 1f);
            chRt.pivot = new Vector2(0.5f, 1f);
            chRt.offsetMin = new Vector2(14f, -124f);
            chRt.offsetMax = new Vector2(-14f, -98f);

            ui.colLevelHeader = CreateHeaderColText("ColLevel", colBar.transform, font, dlg.scores_col_level, 0.00f, 0.40f, TextAlignmentOptions.MidlineLeft, 12f);
            ui.colStatusHeader = CreateHeaderColText("ColStatus", colBar.transform, font, dlg.scores_col_status, 0.40f, 0.53f, TextAlignmentOptions.Center, 0f);
            ui.colMedalHeader = CreateHeaderColText("ColMedal", colBar.transform, font, dlg.scores_col_medal, 0.53f, 0.70f, TextAlignmentOptions.Center, 0f);
            ui.colAccuracyHeader = CreateHeaderColText("ColAccuracy", colBar.transform, font, dlg.scores_col_accuracy, 0.70f, 0.82f, TextAlignmentOptions.Center, 0f);
            ui.colTimeHeader = CreateHeaderColText("ColTime", colBar.transform, font, dlg.scores_col_time, 0.82f, 0.92f, TextAlignmentOptions.Center, 0f);
            ui.colTriesHeader = CreateHeaderColText("ColTries", colBar.transform, font, dlg.scores_col_tries, 0.92f, 1.00f, TextAlignmentOptions.Center, 0f);

            // ScrollRect + Viewport + Content
            GameObject scrollGo = CreateSlicedImage("LevelsScroll", win.transform, assets != null ? assets.cardRounded : null, RetroUITheme.WindowSkySoft, true);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(14f, 14f);
            scrollRt.offsetMax = new Vector2(-14f, -130f);

            ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 36f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(8f, 8f);
            vpRt.offsetMax = new Vector2(-8f, -8f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform ctRt = content.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup ctVlg = content.GetComponent<VerticalLayoutGroup>();
            ctVlg.spacing = 7f;
            ctVlg.childControlWidth = true;
            ctVlg.childControlHeight = true;
            ctVlg.childForceExpandWidth = true;
            ctVlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRt;
            sr.content = ctRt;

            ui.levelsScrollRect = sr;
            ui.levelsViewportRect = vpRt;
            ui.levelsContentContainer = content.transform;

            ui.emptyLevelsText = CreateTmp("EmptyLevelsText", viewport.transform, font, dlg.scores_empty_filter, 17, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform emptyRt = ui.emptyLevelsText.GetComponent<RectTransform>();
            emptyRt.anchorMin = Vector2.zero;
            emptyRt.anchorMax = Vector2.one;
            emptyRt.offsetMin = new Vector2(24f, 24f);
            emptyRt.offsetMax = new Vector2(-24f, -24f);
            ui.emptyLevelsText.gameObject.SetActive(false);
        }

        private static void BuildSkillsAndAssessmentSubWindow(Transform parent, RetroThemeAssets assets, TMP_FontAsset font, UIDialogs dlg, StatsUIBinding ui)
        {
            GameObject win = CreateSlicedImage("SkillsAndAssessmentWindow", parent, assets != null ? assets.windowBody : null, RetroUITheme.WindowCream, false);
            RectTransform winRt = win.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.605f, 0f);
            winRt.anchorMax = new Vector2(1f, 1f);
            winRt.offsetMin = new Vector2(6f, 0f);
            winRt.offsetMax = Vector2.zero;

            // Title bar
            GameObject bar = CreateSlicedImage("TitleBar", win.transform, assets != null ? assets.windowTitleBar : null, RetroUITheme.TitleAmber, false);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.offsetMin = new Vector2(4f, -48f);
            barRt.offsetMax = new Vector2(-4f, -4f);

            GameObject barIcon = CreateImage("Icon", bar.transform, assets != null ? assets.mouseIcon : null, Color.white, false);
            RectTransform biRt = barIcon.GetComponent<RectTransform>();
            biRt.anchorMin = new Vector2(0f, 0.5f);
            biRt.anchorMax = new Vector2(0f, 0.5f);
            biRt.pivot = new Vector2(0f, 0.5f);
            biRt.anchoredPosition = new Vector2(12f, 0f);
            biRt.sizeDelta = new Vector2(28f, 28f);
            barIcon.GetComponent<Image>().preserveAspect = true;

            ui.skillsWindowTitleText = CreateTmp("SkillsWindowTitle", bar.transform, font,
                dlg.stats_card_skills_title, 19, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform swtRt = ui.skillsWindowTitleText.GetComponent<RectTransform>();
            swtRt.anchorMin = Vector2.zero;
            swtRt.anchorMax = Vector2.one;
            swtRt.offsetMin = new Vector2(48f, 2f);
            swtRt.offsetMax = new Vector2(-12f, -2f);

            // Top ~58%: Current Skill Mastery Bars (fits all 7 canonical skills cleanly)
            GameObject skillsBox = new GameObject("SkillsTableContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            skillsBox.transform.SetParent(win.transform, false);
            RectTransform sbRt = skillsBox.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0f, 0.42f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.offsetMin = new Vector2(14f, 6f);
            sbRt.offsetMax = new Vector2(-14f, -54f);

            VerticalLayoutGroup sbVlg = skillsBox.GetComponent<VerticalLayoutGroup>();
            sbVlg.spacing = 6f;
            sbVlg.childControlWidth = true;
            sbVlg.childControlHeight = true;
            sbVlg.childForceExpandWidth = true;
            sbVlg.childForceExpandHeight = true;
            ui.skillsTableContainer = skillsBox.transform;

            // Bottom ~42%: First / Initial Assessment Card
            GameObject assessCard = CreateSlicedImage("InitialAssessmentCard", win.transform, assets != null ? assets.cardRounded : null, RetroUITheme.WindowSkySoft, false);
            RectTransform acRt = assessCard.GetComponent<RectTransform>();
            acRt.anchorMin = new Vector2(0f, 0f);
            acRt.anchorMax = new Vector2(1f, 0.42f);
            acRt.offsetMin = new Vector2(14f, 10f);
            acRt.offsetMax = new Vector2(-14f, -4f);

            // Mini Header strip for Assessment
            GameObject aHdr = CreateSlicedImage("AssessmentMiniHeader", assessCard.transform, assets != null ? assets.windowTitleBar : null, RetroUITheme.TitlePlum, false);
            RectTransform ahRt = aHdr.GetComponent<RectTransform>();
            ahRt.anchorMin = new Vector2(0f, 1f);
            ahRt.anchorMax = new Vector2(1f, 1f);
            ahRt.pivot = new Vector2(0.5f, 1f);
            ahRt.offsetMin = new Vector2(0f, -34f);
            ahRt.offsetMax = Vector2.zero;

            ui.assessmentSectionTitleText = CreateTmp("AssessmentSectionTitle", aHdr.transform, font,
                dlg.stats_card_assessment_title, 14, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform astRt = ui.assessmentSectionTitleText.GetComponent<RectTransform>();
            astRt.anchorMin = Vector2.zero;
            astRt.anchorMax = Vector2.one;
            astRt.offsetMin = new Vector2(12f, 2f);
            astRt.offsetMax = new Vector2(-12f, -2f);

            // Summary Strip inside Assessment Card
            GameObject sumBox = CreateSlicedImage("AssessmentSummaryBox", assessCard.transform, assets != null ? assets.cardRounded : null, RetroUITheme.CardPastelBlue, false);
            RectTransform smRt = sumBox.GetComponent<RectTransform>();
            smRt.anchorMin = new Vector2(0f, 1f);
            smRt.anchorMax = new Vector2(1f, 1f);
            smRt.pivot = new Vector2(0.5f, 1f);
            smRt.offsetMin = new Vector2(10f, -68f);
            smRt.offsetMax = new Vector2(-10f, -38f);
            ui.assessmentSummaryBox = sumBox;

            ui.assessmentSummaryText = CreateTmp("AssessmentSummaryText", sumBox.transform, font, "", 12, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform stRt = ui.assessmentSummaryText.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(0.70f, 1f);
            stRt.offsetMin = new Vector2(10f, 0f);
            stRt.offsetMax = Vector2.zero;

            ui.assessmentAvgText = CreateTmp("AssessmentAvgText", sumBox.transform, font, "", 13, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.MidlineRight);
            RectTransform avRt = ui.assessmentAvgText.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.70f, 0f);
            avRt.anchorMax = new Vector2(1f, 1f);
            avRt.offsetMin = Vector2.zero;
            avRt.offsetMax = new Vector2(-10f, 0f);

            // Assessment Skill Rows Container
            GameObject assessRows = new GameObject("AssessmentTableContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            assessRows.transform.SetParent(assessCard.transform, false);
            RectTransform arRt = assessRows.GetComponent<RectTransform>();
            arRt.anchorMin = Vector2.zero;
            arRt.anchorMax = Vector2.one;
            arRt.offsetMin = new Vector2(10f, 6f);
            arRt.offsetMax = new Vector2(-10f, -70f);

            VerticalLayoutGroup arVlg = assessRows.GetComponent<VerticalLayoutGroup>();
            arVlg.spacing = 4f;
            arVlg.childControlWidth = true;
            arVlg.childControlHeight = true;
            arVlg.childForceExpandWidth = true;
            arVlg.childForceExpandHeight = true;
            ui.assessmentTableContainer = assessRows.transform;

            ui.assessmentEmptyText = CreateTmp("AssessmentEmptyText", assessCard.transform, font, dlg.stats_empty_state, 13, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform aeRt = ui.assessmentEmptyText.GetComponent<RectTransform>();
            aeRt.anchorMin = Vector2.zero;
            aeRt.anchorMax = Vector2.one;
            aeRt.offsetMin = new Vector2(16f, 12f);
            aeRt.offsetMax = new Vector2(-16f, -40f);
            ui.assessmentEmptyText.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // DATA REFRESH (Populates Summary Cards, Levels, Skills, and Assessment)
        // ---------------------------------------------------------------------

        public static void RenderAll(StatsUIBinding ui, LevelFilter filter, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (ui == null) return;
            if (assets == null) assets = RetroThemeAssets.Instance;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            UIDialogs dlg = UIDialogs.Current ?? new UIDialogs();

            // Localize chrome labels
            if (ui.levelsWindowTitleText != null) ui.levelsWindowTitleText.text = StripRichTags(dlg.scores_header_title);
            if (ui.skillsWindowTitleText != null) ui.skillsWindowTitleText.text = dlg.stats_card_skills_title;
            if (ui.assessmentSectionTitleText != null) ui.assessmentSectionTitleText.text = dlg.stats_card_assessment_title;

            string[] tabTitles = { dlg.scores_tab_all, dlg.scores_tab_mouse, dlg.scores_tab_desktop, dlg.scores_tab_completed };
            for (int i = 0; i < 4 && ui.tabLabels != null && i < ui.tabLabels.Length; i++)
            {
                if (ui.tabLabels[i] != null) ui.tabLabels[i].text = tabTitles[i];
                if (ui.tabButtons != null && i < ui.tabButtons.Length && ui.tabButtons[i] != null)
                {
                    bool active = ((int)filter == i);
                    RetroUITheme.ConfigureButtonColors(ui.tabButtons[i], active ? RetroUITheme.BtnSkyBlue : RetroUITheme.BtnNeutralCream);
                    if (ui.tabLabels[i] != null)
                        ui.tabLabels[i].color = active ? RetroUITheme.InkWhite : RetroUITheme.InkDark;
                }
            }

            if (ui.pageUpLabel != null) ui.pageUpLabel.text = dlg.scores_btn_page_up;
            if (ui.pageDownLabel != null) ui.pageDownLabel.text = dlg.scores_btn_page_down;
            if (ui.colLevelHeader != null) ui.colLevelHeader.text = dlg.scores_col_level;
            if (ui.colStatusHeader != null) ui.colStatusHeader.text = dlg.scores_col_status;
            if (ui.colMedalHeader != null) ui.colMedalHeader.text = dlg.scores_col_medal;
            if (ui.colAccuracyHeader != null) ui.colAccuracyHeader.text = dlg.scores_col_accuracy;
            if (ui.colTimeHeader != null) ui.colTimeHeader.text = dlg.scores_col_time;
            if (ui.colTriesHeader != null) ui.colTriesHeader.text = dlg.scores_col_tries;
            if (ui.emptyLevelsText != null) ui.emptyLevelsText.text = dlg.scores_empty_filter;

            // Load persistent snapshot so Menu StatsScene works even without DesktopManager alive
            ProgressData.GetSavedDataSnapshot(out _, out List<string> completedLevels, out List<LevelScoreRecord> levelRecords, out _);
            ProgressData progress = ProgressData.ExistingInstance;

            Dictionary<string, LevelScoreRecord> recordMap = new Dictionary<string, LevelScoreRecord>(StringComparer.OrdinalIgnoreCase);
            if (levelRecords != null)
            {
                for (int i = 0; i < levelRecords.Count; i++)
                {
                    LevelScoreRecord r = levelRecords[i];
                    if (r != null && !string.IsNullOrEmpty(r.levelId))
                        recordMap[r.levelId] = r;
                }
            }

            HashSet<string> completedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (completedLevels != null)
            {
                for (int i = 0; i < completedLevels.Count; i++)
                    completedSet.Add(completedLevels[i]);
            }

            int totalLevels = ProgressData.LevelOrder.Length;
            int gold = 0, silver = 0, bronze = 0, completedCount = 0, accCount = 0;
            float accSum = 0f;

            for (int i = 0; i < totalLevels; i++)
            {
                string id = ProgressData.LevelOrder[i];
                recordMap.TryGetValue(id, out LevelScoreRecord rec);
                bool isComp = completedSet.Contains(id) || (progress != null && progress.IsLevelCompleted(id)) || (rec != null && rec.bestMedal != LevelMedal.None);
                if (isComp) completedCount++;

                if (rec != null)
                {
                    switch (rec.bestMedal)
                    {
                        case LevelMedal.Gold: gold++; break;
                        case LevelMedal.Silver: silver++; break;
                        case LevelMedal.Bronze: bronze++; break;
                    }
                    if (rec.bestScore > 0f || rec.attemptsCount > 0)
                    {
                        accSum += rec.bestScore;
                        accCount++;
                    }
                }
            }

            float avgAccuracy = accCount > 0 ? accSum / accCount : 0f;

            // Update 5 Summary Cards
            if (ui.goldValueText != null) ui.goldValueText.text = gold.ToString();
            if (ui.goldLabelText != null) ui.goldLabelText.text = dlg.scores_stat_gold;
            if (ui.silverValueText != null) ui.silverValueText.text = silver.ToString();
            if (ui.silverLabelText != null) ui.silverLabelText.text = dlg.scores_stat_silver;
            if (ui.bronzeValueText != null) ui.bronzeValueText.text = bronze.ToString();
            if (ui.bronzeLabelText != null) ui.bronzeLabelText.text = dlg.scores_stat_bronze;
            if (ui.completedValueText != null) ui.completedValueText.text = $"{completedCount}/{totalLevels}";
            if (ui.completedLabelText != null) ui.completedLabelText.text = dlg.scores_stat_completed;
            if (ui.accuracyValueText != null) ui.accuracyValueText.text = $"{avgAccuracy:F0}%";
            if (ui.accuracyLabelText != null) ui.accuracyLabelText.text = dlg.scores_col_accuracy;

            // Render Level Rows
            RenderLevelRows(ui, filter, progress, recordMap, completedSet, assets, font, dlg);

            // Render Skill Mastery Bars
            RenderSkillMastery(ui.skillsTableContainer, assets, font, dlg);

            // Render Initial Assessment
            RenderInitialAssessment(ui, assets, font, dlg);
        }

        private static void RenderLevelRows(
            StatsUIBinding ui,
            LevelFilter filter,
            ProgressData progress,
            Dictionary<string, LevelScoreRecord> recordMap,
            HashSet<string> completedSet,
            RetroThemeAssets assets,
            TMP_FontAsset font,
            UIDialogs dlg)
        {
            if (ui.levelsContentContainer == null) return;
            ClearChildren(ui.levelsContentContainer);

            int shown = 0;
            for (int i = 0; i < ProgressData.LevelOrder.Length; i++)
            {
                string levelId = ProgressData.LevelOrder[i];
                bool isDesktop = i >= 9; // levels 1-9 mouse skills, 10-16 desktop & apps
                recordMap.TryGetValue(levelId, out LevelScoreRecord rec);
                bool isComp = completedSet.Contains(levelId) || (progress != null && progress.IsLevelCompleted(levelId)) || (rec != null && rec.bestMedal != LevelMedal.None);
                bool isUnlocked = i == 0 || isComp || (progress != null ? progress.IsLevelUnlocked(levelId) : PlayerPrefs.GetInt($"Unlocked_{levelId}", 0) == 1);

                bool matches = filter == LevelFilter.All
                    || (filter == LevelFilter.MouseSkills && !isDesktop)
                    || (filter == LevelFilter.DesktopApps && isDesktop)
                    || (filter == LevelFilter.Completed && isComp);

                if (!matches) continue;

                shown++;
                string levelTitle = GetLocalizedLevelName(i, levelId, dlg);
                string categoryName = isDesktop ? dlg.scores_cat_desktop : dlg.scores_cat_mouse;
                CreateLevelRowCard(ui.levelsContentContainer, i, isDesktop, levelTitle, categoryName, isComp, isUnlocked, rec, assets, font, dlg);
            }

            if (ui.emptyLevelsText != null)
                ui.emptyLevelsText.gameObject.SetActive(shown == 0);
        }

        private static void CreateLevelRowCard(
            Transform parent,
            int index,
            bool isDesktop,
            string title,
            string category,
            bool isCompleted,
            bool isUnlocked,
            LevelScoreRecord record,
            RetroThemeAssets assets,
            TMP_FontAsset font,
            UIDialogs dlg)
        {
            Color cardBg = (index % 2 == 0) ? RetroUITheme.CardWhite : RetroUITheme.CardPastelBlue;
            GameObject row = CreateSlicedImage($"LevelRow_{index + 1}", parent, assets != null ? assets.cardRounded : null, cardBg, false);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 62f;

            // Left Level Icon
            Sprite icon = GetLevelIcon(index, isDesktop, assets);
            if (icon != null)
            {
                GameObject icGo = CreateImage("Icon", row.transform, icon, Color.white, false);
                RectTransform icRt = icGo.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0f, 0.5f);
                icRt.anchorMax = new Vector2(0f, 0.5f);
                icRt.pivot = new Vector2(0f, 0.5f);
                icRt.anchoredPosition = new Vector2(10f, 0f);
                icRt.sizeDelta = new Vector2(36f, 36f);
                icGo.GetComponent<Image>().preserveAspect = true;
            }

            // Title + Category
            TMP_Text nameTmp = CreateTmp("NameText", row.transform, font, title, 14, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform ntRt = nameTmp.GetComponent<RectTransform>();
            ntRt.anchorMin = new Vector2(0f, 0.42f);
            ntRt.anchorMax = new Vector2(0.40f, 0.96f);
            ntRt.offsetMin = new Vector2(54f, 0f);
            ntRt.offsetMax = new Vector2(-6f, -2f);
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text catTmp = CreateTmp("CategoryText", row.transform, font, category, 11, FontStyles.Normal,
                isDesktop ? RetroUITheme.TitlePlum : RetroUITheme.TitleTeal, TextAlignmentOptions.MidlineLeft);
            RectTransform ctRt = catTmp.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 0.06f);
            ctRt.anchorMax = new Vector2(0.40f, 0.44f);
            ctRt.offsetMin = new Vector2(54f, 2f);
            ctRt.offsetMax = new Vector2(-6f, 0f);

            // Status Pill
            Color stBg = isCompleted ? RetroUITheme.CardPastelMint : (isUnlocked ? RetroUITheme.CardPastelYellow : RetroUITheme.BtnSoftSlate);
            Color stInk = isCompleted ? RetroUITheme.SuccessMint : (isUnlocked ? RetroUITheme.WarningAmber : RetroUITheme.InkMuted);
            string stText = isCompleted ? dlg.scores_status_completed : (isUnlocked ? dlg.scores_status_unlocked : dlg.scores_status_locked);
            CreatePillBadge("StatusPill", row.transform, assets, font, stText, stBg, stInk, 0.40f, 0.53f, 12);

            // Medal Earned
            LevelMedal medal = record != null ? record.bestMedal : LevelMedal.None;
            CreateMedalCell(row.transform, medal, assets, font, 0.53f, 0.70f);

            // Accuracy
            string accStr = (record != null && (record.attemptsCount > 0 || record.bestScore > 0f))
                ? (record.bestMaxPoints > 0 ? $"{record.bestScore:F0}% ({record.bestPoints}/{record.bestMaxPoints})" : $"{record.bestScore:F0}%")
                : "--";
            TMP_Text accTmp = CreateTmp("AccuracyText", row.transform, font, accStr, 13, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform acRt = accTmp.GetComponent<RectTransform>();
            acRt.anchorMin = new Vector2(0.70f, 0f);
            acRt.anchorMax = new Vector2(0.82f, 1f);
            acRt.offsetMin = Vector2.zero;
            acRt.offsetMax = Vector2.zero;

            // Best Time
            string timeStr = (record != null && record.bestTimeSeconds > 0f) ? $"{record.bestTimeSeconds:F1}s" : "--";
            TMP_Text timeTmp = CreateTmp("TimeText", row.transform, font, timeStr, 13, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform tmRt = timeTmp.GetComponent<RectTransform>();
            tmRt.anchorMin = new Vector2(0.82f, 0f);
            tmRt.anchorMax = new Vector2(0.92f, 1f);
            tmRt.offsetMin = Vector2.zero;
            tmRt.offsetMax = Vector2.zero;

            // Tries
            string triesStr = (record != null && record.attemptsCount > 0) ? record.attemptsCount.ToString() : "0";
            TMP_Text triesTmp = CreateTmp("TriesText", row.transform, font, triesStr, 13, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform trRt = triesTmp.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.92f, 0f);
            trRt.anchorMax = new Vector2(1.00f, 1f);
            trRt.offsetMin = Vector2.zero;
            trRt.offsetMax = Vector2.zero;
        }

        private static void RenderSkillMastery(Transform container, RetroThemeAssets assets, TMP_FontAsset font, UIDialogs dlg)
        {
            if (container == null) return;
            ClearChildren(container);

            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 6f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = true;
            }

            SkillManager sm = SkillManager.Instance;
            for (int i = 0; i < CanonicalSkillOrder.Length; i++)
            {
                string skillId = CanonicalSkillOrder[i];
                float score = sm != null && sm.AllStates.ContainsKey(skillId)
                    ? sm.GetScore(skillId)
                    : PlayerPrefs.GetFloat($"SkillScore_{skillId}", 0f);
                score = Mathf.Clamp(score, 0f, 100f);

                string label = GetLocalizedSkillName(skillId, dlg);
                Color barColor = score >= 80f ? RetroUITheme.SuccessMint
                    : (score >= 50f ? RetroUITheme.BtnSkyBlue : RetroUITheme.WarningAmber);
                string tierText = score >= 80f ? dlg.stats_level_mastered
                    : (score >= 50f ? dlg.stats_level_proficient : dlg.stats_level_practicing);

                GameObject row = CreateSlicedImage($"SkillBar_{skillId}", container, assets != null ? assets.cardRounded : null, RetroUITheme.CardWhite, false);
                LayoutElement le = row.AddComponent<LayoutElement>();
                le.preferredHeight = 33f;

                Sprite skIcon = GetSkillIconSprite(skillId, assets);
                if (skIcon != null)
                {
                    GameObject ic = CreateImage("Icon", row.transform, skIcon, Color.white, false);
                    RectTransform icRt = ic.GetComponent<RectTransform>();
                    icRt.anchorMin = new Vector2(0f, 0.5f);
                    icRt.anchorMax = new Vector2(0f, 0.5f);
                    icRt.pivot = new Vector2(0f, 0.5f);
                    icRt.anchoredPosition = new Vector2(8f, 0f);
                    icRt.sizeDelta = new Vector2(24f, 24f);
                    ic.GetComponent<Image>().preserveAspect = true;
                }

                TMP_Text nameTmp = CreateTmp("Name", row.transform, font, label, 13, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
                RectTransform nRt = nameTmp.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0f, 0f);
                nRt.anchorMax = new Vector2(0.36f, 1f);
                nRt.offsetMin = new Vector2(38f, 0f);
                nRt.offsetMax = new Vector2(-4f, 0f);

                // Progress bar track
                GameObject track = CreateSlicedImage("Track", row.transform, assets != null ? assets.badgePill : null, RetroUITheme.ProgressTrack, false);
                RectTransform trkRt = track.GetComponent<RectTransform>();
                trkRt.anchorMin = new Vector2(0.37f, 0.25f);
                trkRt.anchorMax = new Vector2(0.73f, 0.75f);
                trkRt.offsetMin = Vector2.zero;
                trkRt.offsetMax = Vector2.zero;

                GameObject fill = CreateSlicedImage("Fill", track.transform, assets != null ? assets.badgePill : null, barColor, false);
                RectTransform flRt = fill.GetComponent<RectTransform>();
                flRt.anchorMin = Vector2.zero;
                flRt.anchorMax = new Vector2(Mathf.Clamp01(score / 100f), 1f);
                flRt.offsetMin = Vector2.zero;
                flRt.offsetMax = Vector2.zero;

                TMP_Text pctTmp = CreateTmp("Pct", row.transform, font, $"{score:0}%", 13, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
                RectTransform pRt = pctTmp.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.73f, 0f);
                pRt.anchorMax = new Vector2(0.83f, 1f);
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;

                CreatePillBadge("TierPill", row.transform, assets, font, tierText,
                    score >= 80f ? RetroUITheme.CardPastelMint : (score >= 50f ? RetroUITheme.CardPastelBlue : RetroUITheme.CardPastelYellow),
                    barColor, 0.83f, 0.99f, 11);
            }
        }

        private static void RenderInitialAssessment(StatsUIBinding ui, RetroThemeAssets assets, TMP_FontAsset font, UIDialogs dlg)
        {
            if (ui.assessmentTableContainer == null) return;
            ClearChildren(ui.assessmentTableContainer);

            VerticalLayoutGroup vlg = ui.assessmentTableContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 4f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = true;
            }

            AssessmentExportReport report = PlaytestDataManager.GetCurrentAssessmentReport();
            bool hasReport = report != null && report.skills != null && report.skills.Count > 0;

            if (ui.assessmentSummaryBox != null) ui.assessmentSummaryBox.SetActive(hasReport);
            if (ui.assessmentEmptyText != null)
            {
                ui.assessmentEmptyText.text = dlg.stats_empty_state;
                ui.assessmentEmptyText.gameObject.SetActive(!hasReport);
            }

            if (!hasReport) return;

            if (ui.assessmentSummaryText != null)
            {
                string timeLabel = string.IsNullOrEmpty(report.formattedTime) ? dlg.stats_recent_label : report.formattedTime;
                ui.assessmentSummaryText.text = string.Format(dlg.stats_summary_format, timeLabel, report.completedTasks, report.totalTasksTested);
            }
            if (ui.assessmentAvgText != null)
                ui.assessmentAvgText.text = string.Format(dlg.stats_average_format, report.averageScore);

            // Render 2-item horizontal rows cleanly so all skills fit without clipping
            int pairCount = Mathf.CeilToInt(report.skills.Count / 2f);
            for (int rowIdx = 0; rowIdx < pairCount; rowIdx++)
            {
                GameObject pairRow = new GameObject($"AssessPair_{rowIdx}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                pairRow.transform.SetParent(ui.assessmentTableContainer, false);
                LayoutElement rowLe = pairRow.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 25f;

                HorizontalLayoutGroup hlg = pairRow.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6f;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;

                for (int col = 0; col < 2; col++)
                {
                    int idx = rowIdx * 2 + col;
                    if (idx < report.skills.Count)
                    {
                        AssessmentSkillExportItem item = report.skills[idx];
                        GameObject cell = CreateSlicedImage($"AssessCell_{idx}", pairRow.transform, assets != null ? assets.cardRounded : null, RetroUITheme.CardWhite, false);
                        string locName = GetLocalizedSkillName(item.skillId, dlg);
                        if (string.IsNullOrEmpty(locName)) locName = item.skillName;

                        TMP_Text lbl = CreateTmp("Skill", cell.transform, font, locName, 12, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
                        RectTransform lRt = lbl.GetComponent<RectTransform>();
                        lRt.anchorMin = new Vector2(0f, 0f);
                        lRt.anchorMax = new Vector2(0.72f, 1f);
                        lRt.offsetMin = new Vector2(8f, 0f);
                        lRt.offsetMax = Vector2.zero;
                        lbl.textWrappingMode = TextWrappingModes.NoWrap;
                        lbl.overflowMode = TextOverflowModes.Ellipsis;

                        Color scCol = item.score >= 80f ? RetroUITheme.SuccessMint : (item.score >= 50f ? RetroUITheme.TitleBlue : RetroUITheme.WarningAmber);
                        TMP_Text val = CreateTmp("Score", cell.transform, font, $"{item.score:0}%", 12, FontStyles.Bold, scCol, TextAlignmentOptions.MidlineRight);
                        RectTransform vRt = val.GetComponent<RectTransform>();
                        vRt.anchorMin = new Vector2(0.72f, 0f);
                        vRt.anchorMax = new Vector2(1f, 1f);
                        vRt.offsetMin = Vector2.zero;
                        vRt.offsetMax = new Vector2(-8f, 0f);
                    }
                    else
                    {
                        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
                        spacer.transform.SetParent(pairRow.transform, false);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------
        // LOCALIZATION & ASSET HELPERS
        // ---------------------------------------------------------------------

        public static string GetLocalizedSkillName(string skillId, UIDialogs dlg)
        {
            if (dlg == null) dlg = UIDialogs.Current ?? new UIDialogs();
            switch (skillId)
            {
                case "mouse_move":   return dlg.skill_mouse_move;
                case "hover":        return dlg.skill_hover;
                case "click":        return dlg.skill_click;
                case "double_click": return dlg.skill_double_click;
                case "right_click":  return dlg.skill_right_click;
                case "click_hold":   return dlg.skill_click_hold;
                case "drag_drop":    return dlg.skill_drag_drop;
                default:             return skillId;
            }
        }

        public static string GetLocalizedLevelName(int index, string levelId, UIDialogs dlg)
        {
            if (dlg != null && dlg.canonical_level_names != null && index >= 0 && index < dlg.canonical_level_names.Length)
                return dlg.canonical_level_names[index];
            return levelId;
        }

        public static Sprite GetSkillIconSprite(string skillId, RetroThemeAssets assets)
        {
            if (assets == null) return null;
            switch (skillId)
            {
                case "mouse_move":   return assets.mouseMoveIcon ?? assets.mouseIcon;
                case "hover":        return assets.targetIcon ?? assets.mouseIcon;
                case "click":
                case "double_click": return assets.mouseLeftClickIcon ?? assets.mouseIcon;
                case "right_click":  return assets.mouseRightClickIcon ?? assets.mouseIcon;
                case "click_hold":
                case "drag_drop":    return assets.mouseHoldClickIcon ?? assets.mouseIcon;
                default:             return assets.mouseIcon;
            }
        }

        private static Sprite GetLevelIcon(int index, bool isDesktop, RetroThemeAssets assets)
        {
            if (assets == null) return null;
            if (!isDesktop)
            {
                switch (index)
                {
                    case 0: return assets.mouseMoveIcon ?? assets.mouseIcon;
                    case 2: return assets.mouseHoldClickIcon ?? assets.mouseIcon;
                    case 3: return assets.mouseIcon;
                    case 5: return assets.mouseRightClickIcon ?? assets.mouseIcon;
                    default: return assets.targetIcon ?? assets.mouseLeftClickIcon;
                }
            }
            return assets.folderYellow ?? assets.computerIcon;
        }

        public static string StripRichTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("<b>", "").Replace("</b>", "");
        }

        // ---------------------------------------------------------------------
        // PRIMITIVE HELPERS
        // ---------------------------------------------------------------------

        private static void CreateSummaryCard(
            string name,
            Transform parent,
            RetroThemeAssets assets,
            TMP_FontAsset font,
            Sprite icon,
            Color bgTint,
            Color inkAccent,
            string label,
            out TMP_Text valueTmp,
            out TMP_Text labelTmp)
        {
            GameObject card = CreateSlicedImage(name, parent, assets != null ? assets.cardRounded : null, bgTint, false);

            GameObject icGo = CreateImage("Icon", card.transform, icon, Color.white, false);
            RectTransform icRt = icGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.pivot = new Vector2(0f, 0.5f);
            icRt.anchoredPosition = new Vector2(14f, 0f);
            icRt.sizeDelta = new Vector2(44f, 44f);
            Image icImg = icGo.GetComponent<Image>();
            icImg.preserveAspect = true;
            if (icon == null) icImg.color = Color.clear;

            valueTmp = CreateTmp("ValueText", card.transform, font, "0", 30, FontStyles.Bold, inkAccent, TextAlignmentOptions.BottomLeft);
            RectTransform vRt = valueTmp.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0.45f);
            vRt.anchorMax = new Vector2(1f, 1f);
            vRt.offsetMin = new Vector2(68f, 0f);
            vRt.offsetMax = new Vector2(-10f, -10f);

            labelTmp = CreateTmp("LabelText", card.transform, font, label, 12, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.TopLeft);
            RectTransform lRt = labelTmp.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(1f, 0.45f);
            lRt.offsetMin = new Vector2(68f, 10f);
            lRt.offsetMax = new Vector2(-10f, 0f);
        }

        private static void CreateMedalCell(Transform parent, LevelMedal medal, RetroThemeAssets assets, TMP_FontAsset font, float minX, float maxX)
        {
            GameObject holder = new GameObject("MedalCell", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            RectTransform hRt = holder.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(minX, 0f);
            hRt.anchorMax = new Vector2(maxX, 1f);
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            if (medal == LevelMedal.None || assets == null)
            {
                TMP_Text dash = CreateTmp("None", holder.transform, font, LevelScoreRecord.GetMedalName(LevelMedal.None), 12, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
                StretchFull(dash.GetComponent<RectTransform>());
                return;
            }

            Sprite spr = medal == LevelMedal.Gold ? assets.goldMedal
                : (medal == LevelMedal.Silver ? assets.silverMedal : assets.bronzeMedal);
            Color medalCol = medal == LevelMedal.Gold ? RetroUITheme.BronzeAccent
                : (medal == LevelMedal.Silver ? RetroUITheme.InkMuted : RetroUITheme.BronzeAccent);

            GameObject ic = CreateImage("MedalSprite", holder.transform, spr, Color.white, false);
            RectTransform icRt = ic.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.14f, 0.5f);
            icRt.anchorMax = new Vector2(0.14f, 0.5f);
            icRt.pivot = new Vector2(0.5f, 0.5f);
            icRt.anchoredPosition = Vector2.zero;
            icRt.sizeDelta = new Vector2(26f, 26f);
            ic.GetComponent<Image>().preserveAspect = true;

            TMP_Text lbl = CreateTmp("MedalName", holder.transform, font, LevelScoreRecord.GetMedalName(medal).ToUpperInvariant(), 12, FontStyles.Bold, medalCol, TextAlignmentOptions.MidlineLeft);
            RectTransform lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.32f, 0f);
            lRt.anchorMax = new Vector2(1f, 1f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = new Vector2(-4f, 0f);
        }

        private static void CreatePillBadge(string name, Transform parent, RetroThemeAssets assets, TMP_FontAsset font, string text, Color bg, Color ink, float minX, float maxX, float fontSize)
        {
            GameObject pill = CreateSlicedImage(name, parent, assets != null ? assets.badgePill : null, bg, false);
            RectTransform pRt = pill.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(minX, 0.22f);
            pRt.anchorMax = new Vector2(maxX, 0.78f);
            pRt.offsetMin = new Vector2(3f, 0f);
            pRt.offsetMax = new Vector2(-3f, 0f);

            TMP_Text txt = CreateTmp("Text", pill.transform, font, text, fontSize, FontStyles.Bold, ink, TextAlignmentOptions.Center);
            StretchFull(txt.GetComponent<RectTransform>(), 4f, 0f);
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static TMP_Text CreateHeaderColText(string name, Transform parent, TMP_FontAsset font, string text, float minX, float maxX, TextAlignmentOptions align, float padLeft)
        {
            TMP_Text tmp = CreateTmp(name, parent, font, text, 11, FontStyles.Bold, RetroUITheme.InkDark, align);
            RectTransform rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.offsetMin = new Vector2(padLeft, 0f);
            rt.offsetMax = Vector2.zero;
            return tmp;
        }

        private static Button CreateRetroCompactButton(
            string name,
            Transform parent,
            RetroThemeAssets assets,
            TMP_FontAsset font,
            string text,
            Color bg,
            Color ink,
            float fontSize,
            float flexWeight,
            out TMP_Text label)
        {
            GameObject go = CreateSlicedImage(name, parent, assets != null ? assets.button3D : null, bg, true);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWeight;
            le.minWidth = 60f;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            RetroUITheme.ConfigureButtonColors(btn, bg);

            label = CreateTmp("Label", go.transform, font, text, fontSize, FontStyles.Bold, ink, TextAlignmentOptions.Center);
            StretchFull(label.GetComponent<RectTransform>(), 6f, 2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return btn;
        }

        private static GameObject CreateSlicedImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            RetroUITheme.ApplySlicedSprite(img, sprite, color, raycast);
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        private static TMP_Text CreateTmp(string name, Transform parent, TMP_FontAsset font, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null, false);
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
