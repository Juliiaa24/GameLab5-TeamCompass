/**
 * Author: TeamCompass (Diego & AI)
 * Date: 30/10/2026
 * Description: Redesigned Virtual Desktop application for viewing player level scores, medals (Bronze, Silver, Gold),
 *              completion times, and historical records. Features summary stat cards, category filter tabs,
 *              scroll navigation controls, and full-width responsive level cards with full localization support.
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
        public enum LevelFilter
        {
            All = 0,
            MouseSkills = 1,
            DesktopApps = 2,
            CompletedOnly = 3
        }

        private Window parentWindow;
        private RectTransform contentContainer;
        private Transform listContent;
        private ScrollRect scrollRect;
        private readonly List<GameObject> rowInstances = new List<GameObject>();

        // Summary badge labels
        private TextMeshProUGUI titleHeaderText;
        private TextMeshProUGUI goldCountText;
        private TextMeshProUGUI silverCountText;
        private TextMeshProUGUI bronzeCountText;
        private TextMeshProUGUI completedCountText;
        private TextMeshProUGUI avgAccuracyText;
        private TextMeshProUGUI pageUpBtnText;
        private TextMeshProUGUI pageDownBtnText;
        private Transform columnHeaderTransform;

        // Filter state & tab visuals
        private LevelFilter activeFilter = LevelFilter.All;
        private readonly Dictionary<LevelFilter, Image> filterTabImages = new Dictionary<LevelFilter, Image>();
        private readonly Dictionary<LevelFilter, TextMeshProUGUI> filterTabTexts = new Dictionary<LevelFilter, TextMeshProUGUI>();

        private void Awake()
        {
            parentWindow = GetComponentInParent<Window>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void Start()
        {
            UIDialogs ui = UIDialogs.Current;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null)
            {
                parentWindow.setTitle(ui.scores_window_title);
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
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (ProgressData.HasInstance)
            {
                ProgressData.Instance.OnProgressChanged -= RefreshData;
            }
        }

        private void HandleLanguageChanged()
        {
            UIDialogs ui = UIDialogs.Current;
            if (parentWindow != null)
            {
                parentWindow.setTitle(ui.scores_window_title);
            }
            UpdateStaticLocalizedTexts();
            RefreshData();
        }

        private void UpdateStaticLocalizedTexts()
        {
            UIDialogs ui = UIDialogs.Current;
            if (titleHeaderText != null)
            {
                titleHeaderText.text = $"{ui.scores_header_title}\n<size=13><color=#B8C7DC>{ui.scores_subtitle}</color></size>";
            }

            if (filterTabTexts.TryGetValue(LevelFilter.All, out var allTxt) && allTxt != null)
                allTxt.text = $"<b>{ui.scores_tab_all}</b>";
            if (filterTabTexts.TryGetValue(LevelFilter.MouseSkills, out var mouseTxt) && mouseTxt != null)
                mouseTxt.text = $"<b>{ui.scores_tab_mouse}</b>";
            if (filterTabTexts.TryGetValue(LevelFilter.DesktopApps, out var deskTxt) && deskTxt != null)
                deskTxt.text = $"<b>{ui.scores_tab_desktop}</b>";
            if (filterTabTexts.TryGetValue(LevelFilter.CompletedOnly, out var compTxt) && compTxt != null)
                compTxt.text = $"<b>{ui.scores_tab_completed}</b>";

            if (pageUpBtnText != null) pageUpBtnText.text = $"<b>{ui.scores_btn_page_up}</b>";
            if (pageDownBtnText != null) pageDownBtnText.text = $"<b>{ui.scores_btn_page_down}</b>";

            if (columnHeaderTransform != null)
            {
                for (int i = columnHeaderTransform.childCount - 1; i >= 0; i--)
                {
                    Destroy(columnHeaderTransform.GetChild(i).gameObject);
                }
                BuildProportionalColumns(
                    columnHeaderTransform,
                    "#",
                    ui.scores_col_level,
                    "",
                    ui.scores_col_status,
                    ui.scores_col_medal,
                    ui.scores_col_accuracy,
                    ui.scores_col_time,
                    ui.scores_col_tries,
                    isHeader: true,
                    medal: LevelMedal.None,
                    isUnlocked: true,
                    isCompleted: false);
            }
        }

        private void BuildUI()
        {
            if (contentContainer != null) return;
            UIDialogs ui = UIDialogs.Current;

            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Remove any legacy layout group on root that could stretch elements
            VerticalLayoutGroup oldVlg = GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null) Destroy(oldVlg);

            // 1. App Background (Soft Cool Slate)
            Image bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.91f, 0.93f, 0.96f, 1f);

            // 2. Top Header Banner Card (Fixed 92px height at top)
            GameObject headerCard = new GameObject("HeaderCard", typeof(RectTransform), typeof(Image));
            headerCard.transform.SetParent(transform, false);
            RectTransform headerRt = headerCard.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.offsetMin = new Vector2(16f, -104f);
            headerRt.offsetMax = new Vector2(-16f, -12f);
            headerCard.GetComponent<Image>().color = new Color(0.14f, 0.22f, 0.34f, 1f);

            // Left Title Area inside HeaderCard
            GameObject titleArea = new GameObject("TitleArea", typeof(RectTransform));
            titleArea.transform.SetParent(headerCard.transform, false);
            RectTransform titleAreaRt = titleArea.GetComponent<RectTransform>();
            titleAreaRt.anchorMin = new Vector2(0f, 0f);
            titleAreaRt.anchorMax = new Vector2(0.42f, 1f);
            titleAreaRt.offsetMin = new Vector2(18f, 10f);
            titleAreaRt.offsetMax = new Vector2(-8f, -10f);

            GameObject trophyIconObj = new GameObject("TrophyIcon", typeof(RectTransform), typeof(Image));
            trophyIconObj.transform.SetParent(titleArea.transform, false);
            RectTransform trophyRt = trophyIconObj.GetComponent<RectTransform>();
            trophyRt.anchorMin = new Vector2(0f, 0.5f);
            trophyRt.anchorMax = new Vector2(0f, 0.5f);
            trophyRt.pivot = new Vector2(0f, 0.5f);
            trophyRt.anchoredPosition = new Vector2(0f, 0f);
            trophyRt.sizeDelta = new Vector2(44f, 44f);
            Image trophyImg = trophyIconObj.GetComponent<Image>();
            trophyImg.sprite = MedalVisualUtility.GetTrophyAppSprite();
            trophyImg.preserveAspect = true;

            GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleTextObj.transform.SetParent(titleArea.transform, false);
            RectTransform titleTxtRt = titleTextObj.GetComponent<RectTransform>();
            titleTxtRt.anchorMin = new Vector2(0f, 0f);
            titleTxtRt.anchorMax = new Vector2(1f, 1f);
            titleTxtRt.offsetMin = new Vector2(56f, 0f);
            titleTxtRt.offsetMax = Vector2.zero;
            titleHeaderText = titleTextObj.GetComponent<TextMeshProUGUI>();
            titleHeaderText.text = $"{ui.scores_header_title}\n<size=13><color=#B8C7DC>{ui.scores_subtitle}</color></size>";
            titleHeaderText.fontSize = 20;
            titleHeaderText.color = Color.white;
            titleHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
            titleHeaderText.raycastTarget = false;

            // Right Summary Stat Cards Area inside HeaderCard
            GameObject statsArea = new GameObject("StatsArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            statsArea.transform.SetParent(headerCard.transform, false);
            RectTransform statsAreaRt = statsArea.GetComponent<RectTransform>();
            statsAreaRt.anchorMin = new Vector2(0.43f, 0f);
            statsAreaRt.anchorMax = new Vector2(1f, 1f);
            statsAreaRt.offsetMin = new Vector2(4f, 12f);
            statsAreaRt.offsetMax = new Vector2(-16f, -12f);

            HorizontalLayoutGroup statsHlg = statsArea.GetComponent<HorizontalLayoutGroup>();
            statsHlg.spacing = 10f;
            statsHlg.childControlWidth = true;
            statsHlg.childControlHeight = true;
            statsHlg.childForceExpandWidth = true;
            statsHlg.childForceExpandHeight = true;

            goldCountText = CreateSummaryMedalStatCard(statsArea.transform, ui.scores_stat_gold, LevelMedal.Gold, new Color(0.22f, 0.30f, 0.44f, 1f), "#FFD700");
            silverCountText = CreateSummaryMedalStatCard(statsArea.transform, ui.scores_stat_silver, LevelMedal.Silver, new Color(0.22f, 0.30f, 0.44f, 1f), "#E0E6ED");
            bronzeCountText = CreateSummaryMedalStatCard(statsArea.transform, ui.scores_stat_bronze, LevelMedal.Bronze, new Color(0.22f, 0.30f, 0.44f, 1f), "#ECA060");
            completedCountText = CreateSummaryTextStatCard(statsArea.transform, ui.scores_stat_completed, "0 / 16", new Color(0.18f, 0.36f, 0.34f, 1f), "#7CE8B2");
            avgAccuracyText = CreateSummaryTextStatCard(statsArea.transform, ui.scores_col_accuracy, "0%", new Color(0.20f, 0.32f, 0.48f, 1f), "#7CC6FE");

            // 3. Navigation & Category Filter Bar (Fixed 38px height below HeaderCard)
            GameObject filterBar = new GameObject("FilterBar", typeof(RectTransform));
            filterBar.transform.SetParent(transform, false);
            RectTransform filterBarRt = filterBar.GetComponent<RectTransform>();
            filterBarRt.anchorMin = new Vector2(0f, 1f);
            filterBarRt.anchorMax = new Vector2(1f, 1f);
            filterBarRt.pivot = new Vector2(0.5f, 1f);
            filterBarRt.offsetMin = new Vector2(16f, -150f);
            filterBarRt.offsetMax = new Vector2(-16f, -112f);

            GameObject tabsGroup = new GameObject("FilterTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsGroup.transform.SetParent(filterBar.transform, false);
            RectTransform tabsRt = tabsGroup.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0f, 0f);
            tabsRt.anchorMax = new Vector2(0.75f, 1f);
            tabsRt.offsetMin = Vector2.zero;
            tabsRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabsHlg = tabsGroup.GetComponent<HorizontalLayoutGroup>();
            tabsHlg.spacing = 8f;
            tabsHlg.childControlWidth = false;
            tabsHlg.childControlHeight = true;
            tabsHlg.childForceExpandWidth = false;
            tabsHlg.childForceExpandHeight = true;
            tabsHlg.childAlignment = TextAnchor.MiddleLeft;

            CreateFilterTabButton(tabsGroup.transform, LevelFilter.All, ui.scores_tab_all, 145f);
            CreateFilterTabButton(tabsGroup.transform, LevelFilter.MouseSkills, ui.scores_tab_mouse, 165f);
            CreateFilterTabButton(tabsGroup.transform, LevelFilter.DesktopApps, ui.scores_tab_desktop, 190f);
            CreateFilterTabButton(tabsGroup.transform, LevelFilter.CompletedOnly, ui.scores_tab_completed, 150f);

            // Quick Scroll Page Buttons on the right side of FilterBar
            GameObject navGroup = new GameObject("ScrollNavButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            navGroup.transform.SetParent(filterBar.transform, false);
            RectTransform navRt = navGroup.GetComponent<RectTransform>();
            navRt.anchorMin = new Vector2(0.76f, 0f);
            navRt.anchorMax = new Vector2(1f, 1f);
            navRt.offsetMin = Vector2.zero;
            navRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup navHlg = navGroup.GetComponent<HorizontalLayoutGroup>();
            navHlg.spacing = 8f;
            navHlg.childControlWidth = false;
            navHlg.childControlHeight = true;
            navHlg.childForceExpandWidth = false;
            navHlg.childForceExpandHeight = true;
            navHlg.childAlignment = TextAnchor.MiddleRight;

            pageUpBtnText = CreateScrollNavButton(navGroup.transform, ui.scores_btn_page_up, () => ScrollByPage(1f));
            pageDownBtnText = CreateScrollNavButton(navGroup.transform, ui.scores_btn_page_down, () => ScrollByPage(-1f));

            // 4. Column Header Bar (Fixed 34px height above ScrollView)
            GameObject colHeader = new GameObject("ColumnHeaderBar", typeof(RectTransform), typeof(Image));
            colHeader.transform.SetParent(transform, false);
            columnHeaderTransform = colHeader.transform;
            RectTransform colHeaderRt = colHeader.GetComponent<RectTransform>();
            colHeaderRt.anchorMin = new Vector2(0f, 1f);
            colHeaderRt.anchorMax = new Vector2(1f, 1f);
            colHeaderRt.pivot = new Vector2(0.5f, 1f);
            colHeaderRt.offsetMin = new Vector2(16f, -190f);
            colHeaderRt.offsetMax = new Vector2(-16f, -156f);
            colHeader.GetComponent<Image>().color = new Color(0.24f, 0.32f, 0.44f, 1f);

            BuildProportionalColumns(
                colHeader.transform,
                "#",
                ui.scores_col_level,
                "",
                ui.scores_col_status,
                ui.scores_col_medal,
                ui.scores_col_accuracy,
                ui.scores_col_time,
                ui.scores_col_tries,
                isHeader: true,
                medal: LevelMedal.None,
                isUnlocked: true,
                isCompleted: false);

            // 5. Scrollable Level Cards Container (Fills remaining height)
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(transform, false);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(16f, 14f);
            scrollRt.offsetMax = new Vector2(-16f, -192f);
            scrollObj.GetComponent<Image>().color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // Viewport (leaves 18px on the right for the vertical scrollbar)
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(6f, 6f);
            vpRect.offsetMax = new Vector2(-22f, -6f);
            viewport.GetComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f, 1f);
            Mask vpMask = viewport.GetComponent<Mask>();
            vpMask.showMaskGraphic = true;

            // Content
            GameObject listContentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContentObj.transform.SetParent(viewport.transform, false);
            listContent = listContentObj.transform;
            RectTransform contentRect = listContentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = listContentObj.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 6f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter csf = listContentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Vertical Scrollbar
            Scrollbar vScrollbar = CreateVerticalScrollbar(scrollObj.transform);

            scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 38f;
            scrollRect.verticalScrollbar = vScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            UpdateFilterTabVisuals();
            contentContainer = rootRect;
        }

        private TextMeshProUGUI CreateSummaryMedalStatCard(Transform parent, string label, LevelMedal medal, Color bgColor, string valueHex)
        {
            GameObject card = new GameObject($"Stat_{label}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = bgColor;

            GameObject iconObj = new GameObject("MedalIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(card.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            iconRt.sizeDelta = new Vector2(30f, 30f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = MedalVisualUtility.GetMedalSprite(medal);
            iconImg.preserveAspect = true;

            GameObject txtObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(card.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(44f, 4f);
            txtRt.offsetMax = new Vector2(-6f, -4f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<size=11><color=#B8C7DC>{label}</color></size>\n<size=19><color={valueHex}><b>0</b></color></size>";
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        private TextMeshProUGUI CreateSummaryTextStatCard(Transform parent, string label, string initialVal, Color bgColor, string valueHex)
        {
            GameObject card = new GameObject($"Stat_{label}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = bgColor;

            GameObject txtObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(card.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 4f);
            txtRt.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<size=11><color=#CFE0F5>{label}</color></size>\n<size=18><color={valueHex}><b>{initialVal}</b></color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void CreateFilterTabButton(Transform parent, LevelFilter filter, string title, float width)
        {
            GameObject btnObj = new GameObject($"Tab_{filter}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 34f);

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.preferredHeight = 34f;

            Image img = btnObj.GetComponent<Image>();
            filterTabImages[filter] = img;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{title}</b>";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            filterTabTexts[filter] = tmp;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                activeFilter = filter;
                UpdateFilterTabVisuals();
                RefreshData();
                if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
            });
        }

        private void UpdateFilterTabVisuals()
        {
            foreach (var kvp in filterTabImages)
            {
                bool active = kvp.Key == activeFilter;
                if (kvp.Value != null)
                {
                    kvp.Value.color = active
                        ? new Color(0.18f, 0.52f, 0.86f, 1f)
                        : new Color(0.80f, 0.84f, 0.90f, 1f);
                }
                if (filterTabTexts.TryGetValue(kvp.Key, out TextMeshProUGUI txt) && txt != null)
                {
                    txt.color = active ? Color.white : new Color(0.20f, 0.26f, 0.35f, 1f);
                }
            }
        }

        private TextMeshProUGUI CreateScrollNavButton(Transform parent, string label, Action onClick)
        {
            GameObject btnObj = new GameObject($"Nav_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(105f, 32f);

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.preferredWidth = 105f;
            le.minWidth = 105f;
            le.preferredHeight = 32f;

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.26f, 0.36f, 0.50f, 1f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                FeedbackManager.PlayClick();
                onClick?.Invoke();
            });
            return tmp;
        }

        private void ScrollByPage(float direction)
        {
            if (scrollRect == null) return;
            float step = 0.35f * direction;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + step);
        }

        private Scrollbar CreateVerticalScrollbar(Transform scrollViewParent)
        {
            GameObject sbObj = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbObj.transform.SetParent(scrollViewParent, false);
            RectTransform sbRt = sbObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.offsetMin = new Vector2(-18f, 6f);
            sbRt.offsetMax = new Vector2(-4f, -6f);
            sbObj.GetComponent<Image>().color = new Color(0.78f, 0.82f, 0.88f, 1f);

            GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(sbObj.transform, false);
            RectTransform saRt = slidingArea.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(2f, 2f);
            saRt.offsetMax = new Vector2(-2f, -2f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            Image handleImg = handle.GetComponent<Image>();
            handleImg.color = new Color(0.32f, 0.45f, 0.62f, 1f);

            Scrollbar sb = sbObj.GetComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.handleRect = handleRt;
            sb.targetGraphic = handleImg;
            return sb;
        }

        public void RefreshData()
        {
            if (listContent == null) return;
            UIDialogs ui = UIDialogs.Current;

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
            int visibleIndex = 0;

            for (int i = 0; i < allLevels.Count; i++)
            {
                LevelInfoData lvl = allLevels[i];
                int levelNumber = i + 1;

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

                // Apply active navigation filter
                if (activeFilter == LevelFilter.MouseSkills && levelNumber > 9) continue;
                if (activeFilter == LevelFilter.DesktopApps && levelNumber <= 9) continue;
                if (activeFilter == LevelFilter.CompletedOnly && !isCompleted) continue;

                CreateLevelCardRow(listContent, lvl, levelNumber, visibleIndex, record, isCompleted, isUnlocked);
                visibleIndex++;
            }

            if (visibleIndex == 0)
            {
                CreateEmptyStateRow(listContent);
            }

            // Update Header Stat Cards (pure ASCII text)
            float avgAcc = recordedScoresCount > 0 ? (totalAccuracySum / recordedScoresCount) : 0f;

            if (goldCountText != null)
                goldCountText.text = $"<size=11><color=#B8C7DC>{ui.scores_stat_gold}</color></size>\n<size=19><color=#FFD700><b>{goldCount}</b></color></size>";
            if (silverCountText != null)
                silverCountText.text = $"<size=11><color=#B8C7DC>{ui.scores_stat_silver}</color></size>\n<size=19><color=#E0E6ED><b>{silverCount}</b></color></size>";
            if (bronzeCountText != null)
                bronzeCountText.text = $"<size=11><color=#B8C7DC>{ui.scores_stat_bronze}</color></size>\n<size=19><color=#ECA060><b>{bronzeCount}</b></color></size>";
            if (completedCountText != null)
                completedCountText.text = $"<size=11><color=#CFE0F5>{ui.scores_stat_completed}</color></size>\n<size=18><color=#7CE8B2><b>{completedCount} / {allLevels.Count}</b></color></size>";
            if (avgAccuracyText != null)
                avgAccuracyText.text = $"<size=11><color=#CFE0F5>{ui.scores_col_accuracy}</color></size>\n<size=18><color=#7CC6FE><b>{avgAcc:F0}%</b></color></size>";
        }

        private void CreateEmptyStateRow(Transform parent)
        {
            UIDialogs ui = UIDialogs.Current;
            GameObject row = new GameObject("EmptyStateRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 80f);

            LayoutElement le = row.GetComponent<LayoutElement>();
            le.minHeight = 80f;
            le.preferredHeight = 80f;

            row.GetComponent<Image>().color = Color.white;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(row.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = ui.scores_empty_filter;
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.42f, 0.48f, 0.56f, 1f);
            tmp.raycastTarget = false;
        }

        private void CreateLevelCardRow(
            Transform parent,
            LevelInfoData lvl,
            int levelNumber,
            int rowIndex,
            LevelScoreRecord record,
            bool isCompleted,
            bool isUnlocked)
        {
            UIDialogs ui = UIDialogs.Current;
            GameObject row = new GameObject($"Row_{lvl.levelId}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0f, 52f);

            LayoutElement le = row.GetComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;
            le.flexibleHeight = 0f;

            Image bgImg = row.GetComponent<Image>();
            bool isAlt = (rowIndex % 2) == 1;
            if (isCompleted)
            {
                bgImg.color = isAlt ? new Color(0.93f, 0.97f, 0.95f, 1f) : new Color(0.97f, 0.99f, 0.98f, 1f);
            }
            else
            {
                bgImg.color = isAlt ? new Color(0.92f, 0.94f, 0.97f, 1f) : Color.white;
            }

            rowInstances.Add(row);

            // Left vertical accent stripe
            GameObject stripe = new GameObject("AccentStripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            RectTransform stripeRt = stripe.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0f);
            stripeRt.anchorMax = new Vector2(0f, 1f);
            stripeRt.pivot = new Vector2(0f, 0.5f);
            stripeRt.offsetMin = Vector2.zero;
            stripeRt.offsetMax = new Vector2(5f, 0f);

            LevelMedal medal = record != null ? record.bestMedal : LevelMedal.None;
            Image stripeImg = stripe.GetComponent<Image>();
            if (medal == LevelMedal.Gold) stripeImg.color = new Color(0.95f, 0.72f, 0.05f, 1f);
            else if (medal == LevelMedal.Silver) stripeImg.color = new Color(0.60f, 0.68f, 0.78f, 1f);
            else if (medal == LevelMedal.Bronze) stripeImg.color = new Color(0.80f, 0.48f, 0.20f, 1f);
            else if (isCompleted) stripeImg.color = new Color(0.22f, 0.68f, 0.40f, 1f);
            else stripeImg.color = new Color(0.75f, 0.80f, 0.86f, 1f);

            string numStr = $"{levelNumber:00}";
            string displayName = !string.IsNullOrEmpty(lvl.displayName) ? lvl.displayName : lvl.levelId;
            string categoryText = levelNumber <= 9 ? ui.scores_cat_mouse : ui.scores_cat_desktop;
            string statusText = isCompleted ? ui.scores_status_completed : (isUnlocked ? ui.scores_status_unlocked : ui.scores_status_locked);

            string accText = (record != null && record.bestScore > 0f) ? $"{record.bestScore:F0}%" : "-";
            string timeText = "-";
            if (record != null && record.bestTimeSeconds > 0f)
            {
                int m = (int)(record.bestTimeSeconds / 60f);
                int s = (int)(record.bestTimeSeconds % 60f);
                timeText = $"{m:00}:{s:00}s";
            }
            string triesText = (record != null && record.attemptsCount > 0) ? $"{record.attemptsCount}" : "-";

            BuildProportionalColumns(
                row.transform,
                numStr,
                displayName,
                categoryText,
                statusText,
                "",
                accText,
                timeText,
                triesText,
                isHeader: false,
                medal: medal,
                isUnlocked: isUnlocked,
                isCompleted: isCompleted);
        }

        private void BuildProportionalColumns(
            Transform rowTransform,
            string colNum,
            string colName,
            string colCategory,
            string colStatus,
            string colMedalHeader,
            string colAccuracy,
            string colTime,
            string colTries,
            bool isHeader,
            LevelMedal medal,
            bool isUnlocked,
            bool isCompleted)
        {
            Color headerColor = Color.white;
            Color primaryColor = isUnlocked ? new Color(0.12f, 0.18f, 0.26f, 1f) : new Color(0.50f, 0.54f, 0.60f, 1f);
            int fontSize = isHeader ? 12 : 14;
            FontStyles fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;

            // 1. Number Column (0.01 - 0.06)
            CreateProportionalTextCell(
                rowTransform,
                isHeader ? colNum : $"<b>{colNum}</b>",
                0.01f, 0.06f,
                TextAlignmentOptions.Center,
                fontSize,
                isHeader ? headerColor : new Color(0.38f, 0.45f, 0.55f, 1f));

            // 2. Level Name Column (0.06 - 0.37)
            CreateProportionalTextCell(
                rowTransform,
                isHeader ? colName : $"<b>{colName}</b>",
                0.06f, 0.37f,
                TextAlignmentOptions.MidlineLeft,
                isHeader ? 12 : 15,
                isHeader ? headerColor : primaryColor);

            // 3. Category Column (0.37 - 0.51)
            CreateProportionalTextCell(
                rowTransform,
                colCategory,
                0.37f, 0.51f,
                TextAlignmentOptions.Center,
                isHeader ? 12 : 13,
                isHeader ? headerColor : new Color(0.35f, 0.42f, 0.52f, 1f),
                fontStyle);

            // 4. Status Column (0.51 - 0.64)
            if (isHeader)
            {
                CreateProportionalTextCell(rowTransform, colStatus, 0.51f, 0.64f, TextAlignmentOptions.Center, 12, headerColor, FontStyles.Bold);
            }
            else
            {
                CreateStatusBadgeCell(rowTransform, colStatus, 0.51f, 0.64f, isCompleted, isUnlocked);
            }

            // 5. Medal Column (0.64 - 0.80)
            if (isHeader)
            {
                CreateProportionalTextCell(rowTransform, colMedalHeader, 0.64f, 0.80f, TextAlignmentOptions.Center, 12, headerColor, FontStyles.Bold);
            }
            else
            {
                CreateProportionalMedalCell(rowTransform, medal, 0.64f, 0.80f);
            }

            // 6. Accuracy Column (0.80 - 0.88)
            CreateProportionalTextCell(
                rowTransform,
                isHeader ? colAccuracy : $"<b>{colAccuracy}</b>",
                0.80f, 0.88f,
                TextAlignmentOptions.Center,
                fontSize,
                isHeader ? headerColor : primaryColor);

            // 7. Best Time Column (0.88 - 0.95)
            CreateProportionalTextCell(
                rowTransform,
                colTime,
                0.88f, 0.95f,
                TextAlignmentOptions.Center,
                fontSize,
                isHeader ? headerColor : primaryColor,
                fontStyle);

            // 8. Attempts Column (0.95 - 0.995)
            CreateProportionalTextCell(
                rowTransform,
                colTries,
                0.95f, 0.995f,
                TextAlignmentOptions.Center,
                fontSize,
                isHeader ? headerColor : new Color(0.35f, 0.40f, 0.48f, 1f),
                fontStyle);
        }

        private void CreateProportionalTextCell(
            Transform parent,
            string text,
            float anchorMinX,
            float anchorMaxX,
            TextAlignmentOptions alignment,
            int fontSize,
            Color color,
            FontStyles style = FontStyles.Normal)
        {
            GameObject cell = new GameObject("Cell", typeof(RectTransform), typeof(TextMeshProUGUI));
            cell.transform.SetParent(parent, false);
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0f);
            rt.anchorMax = new Vector2(anchorMaxX, 1f);
            rt.offsetMin = new Vector2(4f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = cell.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
        }

        private void CreateStatusBadgeCell(Transform parent, string statusText, float anchorMinX, float anchorMaxX, bool isCompleted, bool isUnlocked)
        {
            GameObject cell = new GameObject("StatusCell", typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0f);
            rt.anchorMax = new Vector2(anchorMaxX, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject pill = new GameObject("BadgePill", typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(cell.transform, false);
            RectTransform pillRt = pill.GetComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.5f, 0.5f);
            pillRt.anchorMax = new Vector2(0.5f, 0.5f);
            pillRt.sizeDelta = new Vector2(104f, 26f);

            Image pillImg = pill.GetComponent<Image>();
            Color textColor;
            if (isCompleted)
            {
                pillImg.color = new Color(0.82f, 0.95f, 0.86f, 1f);
                textColor = new Color(0.10f, 0.45f, 0.22f, 1f);
            }
            else if (isUnlocked)
            {
                pillImg.color = new Color(0.84f, 0.92f, 0.98f, 1f);
                textColor = new Color(0.12f, 0.40f, 0.68f, 1f);
            }
            else
            {
                pillImg.color = new Color(0.88f, 0.90f, 0.92f, 1f);
                textColor = new Color(0.45f, 0.48f, 0.52f, 1f);
            }

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(pill.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{statusText}</b>";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false;
        }

        private void CreateProportionalMedalCell(Transform parent, LevelMedal medal, float anchorMinX, float anchorMaxX)
        {
            GameObject cell = new GameObject("MedalCell", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cell.transform.SetParent(parent, false);
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0f);
            rt.anchorMax = new Vector2(anchorMaxX, 1f);
            rt.offsetMin = new Vector2(4f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);

            HorizontalLayoutGroup hlg = cell.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            if (medal != LevelMedal.None)
            {
                GameObject imgObj = new GameObject("MedalIcon", typeof(RectTransform), typeof(Image));
                imgObj.transform.SetParent(cell.transform, false);
                RectTransform iconRt = imgObj.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(28f, 28f);

                Image img = imgObj.GetComponent<Image>();
                img.sprite = MedalVisualUtility.GetMedalSprite(medal);
                img.preserveAspect = true;
                img.raycastTarget = false;

                string name = LevelScoreRecord.GetMedalName(medal).ToUpperInvariant();
                Color medalTextColor = medal == LevelMedal.Gold
                    ? new Color(0.78f, 0.55f, 0.00f, 1f)
                    : (medal == LevelMedal.Silver ? new Color(0.42f, 0.48f, 0.56f, 1f) : new Color(0.68f, 0.38f, 0.12f, 1f));

                GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(cell.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.sizeDelta = new Vector2(72f, 28f);

                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{name}</b>";
                tmp.fontSize = 13;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = medalTextColor;
                tmp.raycastTarget = false;
            }
            else
            {
                GameObject txtObj = new GameObject("NoMedalLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(cell.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.sizeDelta = new Vector2(110f, 28f);

                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = LevelScoreRecord.GetMedalName(LevelMedal.None);
                tmp.fontSize = 12;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.58f, 0.62f, 0.68f, 1f);
                tmp.raycastTarget = false;
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

            UIDialogs ui = UIDialogs.Current;
            string[] canonicalNames = ui.canonical_level_names ?? Array.Empty<string>();

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
                        string locName = d.GetLocalizedDisplayName();
                        levels.Add(new LevelInfoData { levelId = d.levelId, displayName = !string.IsNullOrEmpty(locName) ? locName : d.name });
                        seen.Add(d.levelId);
                    }
                }
            }

            return levels;
        }
    }
}
