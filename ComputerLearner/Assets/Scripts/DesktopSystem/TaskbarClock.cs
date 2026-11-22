using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// Updates the system tray digital clock on the desktop taskbar and provides
    /// an interactive Date & Time popup when clicked so the player can adjust the clock.
    /// </summary>
    public class TaskbarClock : MonoBehaviour, IPointerClickHandler
    {
        private const string PREF_HOUR_OFFSET = "TaskbarClock_HourOffset";
        private const string PREF_MINUTE_OFFSET = "TaskbarClock_MinuteOffset";
        private const string PREF_DAY_OFFSET = "TaskbarClock_DayOffset";
        private const string PREF_USE_12H = "TaskbarClock_Use12Hour";

        private static int hourOffset;
        private static int minuteOffset;
        private static int dayOffset;
        private static bool use12HourFormat;
        private static bool settingsLoaded;

        [SerializeField] private TMP_Text timeText;
        private string lastFormattedTime;

        private GameObject clockPopupPanel;
        private TMP_Text popupPreviewTimeText;
        private TMP_Text popupPreviewDateText;
        private TMP_Text formatToggleBtnText;

        public static void ResetClockSettings()
        {
            hourOffset = 0;
            minuteOffset = 0;
            dayOffset = 0;
            use12HourFormat = false;
            settingsLoaded = true;

            PlayerPrefs.DeleteKey(PREF_HOUR_OFFSET);
            PlayerPrefs.DeleteKey(PREF_MINUTE_OFFSET);
            PlayerPrefs.DeleteKey(PREF_DAY_OFFSET);
            PlayerPrefs.DeleteKey(PREF_USE_12H);
        }

        private static void EnsureSettingsLoaded()
        {
            if (settingsLoaded) return;
            hourOffset = PlayerPrefs.GetInt(PREF_HOUR_OFFSET, 0);
            minuteOffset = PlayerPrefs.GetInt(PREF_MINUTE_OFFSET, 0);
            dayOffset = PlayerPrefs.GetInt(PREF_DAY_OFFSET, 0);
            use12HourFormat = PlayerPrefs.GetInt(PREF_USE_12H, 0) == 1;
            settingsLoaded = true;
        }

        private static void SaveClockSettings()
        {
            PlayerPrefs.SetInt(PREF_HOUR_OFFSET, hourOffset);
            PlayerPrefs.SetInt(PREF_MINUTE_OFFSET, minuteOffset);
            PlayerPrefs.SetInt(PREF_DAY_OFFSET, dayOffset);
            PlayerPrefs.SetInt(PREF_USE_12H, use12HourFormat ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static DateTime GetCurrentDisplayTime()
        {
            EnsureSettingsLoaded();
            try
            {
                return DateTime.Now.AddDays(dayOffset).AddHours(hourOffset).AddMinutes(minuteOffset);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private void Awake()
        {
            EnsureSettingsLoaded();
            if (timeText == null)
            {
                timeText = GetComponentInChildren<TMP_Text>(true);
            }
            ConfigureClockAppearance();
        }

        private void OnEnable()
        {
            EnsureSettingsLoaded();
            lastFormattedTime = null;
            ConfigureClockAppearance();
            UpdateClock(forceRefresh: true);
        }

        private void OnDestroy()
        {
            if (clockPopupPanel != null)
            {
                Destroy(clockPopupPanel);
            }
        }

        private void Update()
        {
            UpdateClock(forceRefresh: false);
            HandlePopupOutsideClickAndEscape();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            ToggleClockPopup();
        }

        private void ConfigureClockAppearance()
        {
            RectTransform trayRt = transform as RectTransform;
            if (trayRt != null && trayRt.sizeDelta.x < 122f)
            {
                trayRt.sizeDelta = new Vector2(124f, trayRt.sizeDelta.y);
            }

            Image trayBg = GetComponent<Image>();
            if (trayBg == null)
            {
                trayBg = gameObject.AddComponent<Image>();
                trayBg.color = new Color(1f, 1f, 1f, 0.01f);
            }
            trayBg.raycastTarget = true;

            if (timeText != null)
            {
                timeText.fontSize = 14f;
                timeText.lineSpacing = -10f;
                timeText.textWrappingMode = TextWrappingModes.NoWrap;
                timeText.alignment = TextAlignmentOptions.Center;
                timeText.raycastTarget = true;
            }
        }

        private void UpdateClock(bool forceRefresh)
        {
            if (timeText == null) return;

            DateTime displayTime = GetCurrentDisplayTime();
            string timePart = use12HourFormat
                ? displayTime.ToString("hh:mm tt", CultureInfo.InvariantCulture)
                : displayTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            string datePart = displayTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            string nowFormatted = $"<b>{timePart}</b>\n<size=72%>{datePart}</size>";
            if (forceRefresh || nowFormatted != lastFormattedTime)
            {
                lastFormattedTime = nowFormatted;
                timeText.text = nowFormatted;
            }

            if (clockPopupPanel != null && clockPopupPanel.activeSelf)
            {
                UpdatePopupPreview(displayTime);
            }
        }

        private void ToggleClockPopup()
        {
            FeedbackManager.PlayClick();
            if (clockPopupPanel == null)
            {
                BuildClockPopup();
            }

            if (clockPopupPanel == null) return;

            bool show = !clockPopupPanel.activeSelf;
            clockPopupPanel.SetActive(show);
            if (show)
            {
                clockPopupPanel.transform.SetAsLastSibling();
                UpdateClock(forceRefresh: true);
            }
        }

        public void CloseClockPopup()
        {
            if (clockPopupPanel != null && clockPopupPanel.activeSelf)
            {
                clockPopupPanel.SetActive(false);
            }
        }

        private void AdjustHours(int delta)
        {
            hourOffset = Mathf.Clamp(hourOffset + delta, -720, 720);
            SaveClockSettings();
            FeedbackManager.PlayClick();
            UpdateClock(forceRefresh: true);
        }

        private void AdjustMinutes(int delta)
        {
            minuteOffset = Mathf.Clamp(minuteOffset + delta, -43200, 43200);
            SaveClockSettings();
            FeedbackManager.PlayClick();
            UpdateClock(forceRefresh: true);
        }

        private void AdjustDays(int delta)
        {
            dayOffset = Mathf.Clamp(dayOffset + delta, -3650, 3650);
            SaveClockSettings();
            FeedbackManager.PlayClick();
            UpdateClock(forceRefresh: true);
        }

        private void ToggleTimeFormat()
        {
            use12HourFormat = !use12HourFormat;
            SaveClockSettings();
            FeedbackManager.PlayClick();
            UpdateClock(forceRefresh: true);
        }

        private void ResetToSystemTime()
        {
            hourOffset = 0;
            minuteOffset = 0;
            dayOffset = 0;
            SaveClockSettings();
            FeedbackManager.PlaySuccess();
            UpdateClock(forceRefresh: true);
        }

        private void UpdatePopupPreview(DateTime displayTime)
        {
            if (popupPreviewTimeText != null)
            {
                popupPreviewTimeText.text = use12HourFormat
                    ? displayTime.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture)
                    : displayTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            }
            if (popupPreviewDateText != null)
            {
                popupPreviewDateText.text = displayTime.ToString("ddd, dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            if (formatToggleBtnText != null)
            {
                formatToggleBtnText.text = use12HourFormat ? "Format: 12h" : "Format: 24h";
            }
        }

        private void HandlePopupOutsideClickAndEscape()
        {
            if (clockPopupPanel == null || !clockPopupPanel.activeSelf) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool escPressed = kb != null && kb.escapeKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!escPressed) escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escPressed)
            {
                CloseClockPopup();
                return;
            }

            bool mouseClicked = false;
            Vector2 mousePos = Vector2.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                {
                    mouseClicked = true;
                    mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!mouseClicked && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                mouseClicked = true;
                mousePos = Input.mousePosition;
            }
#endif
            if (!mouseClicked) return;

            Canvas rootCanvas = clockPopupPanel.GetComponentInParent<Canvas>();
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? rootCanvas.worldCamera
                : null;

            RectTransform popupRt = clockPopupPanel.GetComponent<RectTransform>();
            RectTransform trayRt = transform as RectTransform;

            bool insidePopup = popupRt != null && RectTransformUtility.RectangleContainsScreenPoint(popupRt, mousePos, uiCam);
            bool insideTray = trayRt != null && RectTransformUtility.RectangleContainsScreenPoint(trayRt, mousePos, uiCam);

            if (!insidePopup && !insideTray)
            {
                CloseClockPopup();
            }
        }

        private void BuildClockPopup()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.rootCanvas != null)
            {
                parentCanvas = parentCanvas.rootCanvas;
            }
            Transform parentTransform = parentCanvas != null ? parentCanvas.transform : transform.root;
            TMP_FontAsset font = timeText != null ? timeText.font : TMP_Settings.defaultFontAsset;

            clockPopupPanel = new GameObject("TaskbarClockPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            clockPopupPanel.transform.SetParent(parentTransform, false);

            RectTransform panelRt = clockPopupPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(1f, 0f);
            panelRt.anchoredPosition = new Vector2(-10f, 54f);
            panelRt.sizeDelta = new Vector2(272f, 236f);

            Image panelBg = clockPopupPanel.GetComponent<Image>();
            panelBg.color = new Color(0.14f, 0.16f, 0.21f, 0.98f);
            panelBg.raycastTarget = true;

            Outline outerOutline = clockPopupPanel.AddComponent<Outline>();
            outerOutline.effectColor = new Color(0.55f, 0.65f, 0.82f, 0.9f);
            outerOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Header bar
            GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(clockPopupPanel.transform, false);
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.offsetMin = new Vector2(4f, -32f);
            headerRt.offsetMax = new Vector2(-4f, -4f);
            Image headerImg = headerGo.GetComponent<Image>();
            headerImg.color = new Color(0.18f, 0.34f, 0.58f, 1f);

            TMP_Text headerTitle = CreateLabel(headerGo.transform, "Title", "Date & Time", font, 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform titleRt = headerTitle.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(10f, 0f);
            titleRt.offsetMax = new Vector2(-34f, 0f);

            Button closeBtn = CreatePopupButton(headerGo.transform, "BtnClose", "X", font, new Color(0.75f, 0.24f, 0.24f, 1f), CloseClockPopup);
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-4f, 0f);
            closeRt.sizeDelta = new Vector2(22f, 22f);

            // Digital clock preview box
            GameObject previewBox = new GameObject("PreviewBox", typeof(RectTransform), typeof(Image));
            previewBox.transform.SetParent(clockPopupPanel.transform, false);
            RectTransform previewRt = previewBox.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f, 1f);
            previewRt.anchorMax = new Vector2(1f, 1f);
            previewRt.pivot = new Vector2(0.5f, 1f);
            previewRt.offsetMin = new Vector2(10f, -86f);
            previewRt.offsetMax = new Vector2(-10f, -38f);
            Image previewBg = previewBox.GetComponent<Image>();
            previewBg.color = new Color(0.08f, 0.10f, 0.13f, 1f);

            popupPreviewTimeText = CreateLabel(previewBox.transform, "PreviewTime", "12:00:00", font, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            popupPreviewTimeText.color = new Color(0.48f, 0.92f, 0.65f, 1f);
            RectTransform pTimeRt = popupPreviewTimeText.GetComponent<RectTransform>();
            pTimeRt.anchorMin = new Vector2(0f, 0.42f);
            pTimeRt.anchorMax = new Vector2(1f, 1f);
            pTimeRt.offsetMin = new Vector2(6f, 0f);
            pTimeRt.offsetMax = new Vector2(-6f, -2f);

            popupPreviewDateText = CreateLabel(previewBox.transform, "PreviewDate", "01/01/2026", font, 12f, FontStyles.Normal, TextAlignmentOptions.Center);
            popupPreviewDateText.color = new Color(0.78f, 0.84f, 0.92f, 1f);
            RectTransform pDateRt = popupPreviewDateText.GetComponent<RectTransform>();
            pDateRt.anchorMin = new Vector2(0f, 0f);
            pDateRt.anchorMax = new Vector2(1f, 0.44f);
            pDateRt.offsetMin = new Vector2(6f, 2f);
            pDateRt.offsetMax = new Vector2(-6f, 0f);

            // Row 1: Hour controls
            BuildControlRow(clockPopupPanel.transform, "HourRow", "Hour:", -94f, font,
                ("-1h", () => AdjustHours(-1)),
                ("+1h", () => AdjustHours(1)));

            // Row 2: Minute controls
            BuildControlRow(clockPopupPanel.transform, "MinuteRow", "Min:", -128f, font,
                ("-10m", () => AdjustMinutes(-10)),
                ("-1m", () => AdjustMinutes(-1)),
                ("+1m", () => AdjustMinutes(1)),
                ("+10m", () => AdjustMinutes(10)));

            // Row 3: Date controls
            BuildControlRow(clockPopupPanel.transform, "DateRow", "Date:", -162f, font,
                ("-1d", () => AdjustDays(-1)),
                ("+1d", () => AdjustDays(1)));

            // Bottom Row: 12h/24h toggle & Reset button
            Button formatBtn = CreatePopupButton(clockPopupPanel.transform, "BtnFormat", "Format: 24h", font, new Color(0.24f, 0.30f, 0.42f, 1f), ToggleTimeFormat);
            formatToggleBtnText = formatBtn.GetComponentInChildren<TMP_Text>();
            RectTransform fmtRt = formatBtn.GetComponent<RectTransform>();
            fmtRt.anchorMin = new Vector2(0f, 0f);
            fmtRt.anchorMax = new Vector2(0.55f, 0f);
            fmtRt.pivot = new Vector2(0f, 0f);
            fmtRt.offsetMin = new Vector2(10f, 10f);
            fmtRt.offsetMax = new Vector2(-4f, 38f);

            Button resetBtn = CreatePopupButton(clockPopupPanel.transform, "BtnReset", "Reset Time", font, new Color(0.26f, 0.44f, 0.30f, 1f), ResetToSystemTime);
            RectTransform rstRt = resetBtn.GetComponent<RectTransform>();
            rstRt.anchorMin = new Vector2(0.55f, 0f);
            rstRt.anchorMax = new Vector2(1f, 0f);
            rstRt.pivot = new Vector2(1f, 0f);
            rstRt.offsetMin = new Vector2(4f, 10f);
            rstRt.offsetMax = new Vector2(-10f, 38f);

            clockPopupPanel.SetActive(false);
        }

        private void BuildControlRow(Transform parent, string rowName, string labelText, float topOffset, TMP_FontAsset font, params (string label, UnityEngine.Events.UnityAction onClick)[] buttons)
        {
            GameObject rowGo = new GameObject(rowName, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.offsetMin = new Vector2(10f, topOffset - 28f);
            rowRt.offsetMax = new Vector2(-10f, topOffset);

            TMP_Text lbl = CreateLabel(rowGo.transform, "Label", labelText, font, 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0f, 1f);
            lblRt.pivot = new Vector2(0f, 0.5f);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = new Vector2(54f, 0f);

            if (buttons == null || buttons.Length == 0) return;

            float startX = 56f;
            float totalWidth = 252f - startX;
            float spacing = 4f;
            float btnWidth = (totalWidth - spacing * (buttons.Length - 1)) / buttons.Length;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = CreatePopupButton(rowGo.transform, $"Btn_{i}", buttons[i].label, font, new Color(0.24f, 0.28f, 0.36f, 1f), buttons[i].onClick);
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 0f);
                btnRt.anchorMax = new Vector2(0f, 1f);
                btnRt.pivot = new Vector2(0f, 0.5f);
                float xMin = startX + i * (btnWidth + spacing);
                btnRt.offsetMin = new Vector2(xMin, 1f);
                btnRt.offsetMax = new Vector2(xMin + btnWidth, -1f);
            }
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static Button CreatePopupButton(Transform parent, string name, string label, TMP_FontAsset font, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            Image img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            TMP_Text txt = CreateLabel(btnGo.transform, "Text", label, font, 12.5f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
