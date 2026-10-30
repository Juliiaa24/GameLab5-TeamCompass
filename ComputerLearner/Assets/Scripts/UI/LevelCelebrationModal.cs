/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Animated level results modal in the style of Angry Birds star reveal.
 *              Sequentially animates earned medals (Bronze, Silver, Gold) with scale pops and audio chimes.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class LevelCelebrationModal : MonoBehaviour
    {
        public static LevelCelebrationModal Show(
            Transform parent, 
            LevelDefinition level, 
            LevelMedal medal, 
            float accuracy, 
            float duration, 
            int mistakes, 
            Action onClosed)
        {
            GameObject modalObj = new GameObject("LevelCelebrationModal", typeof(RectTransform), typeof(CanvasGroup), typeof(LevelCelebrationModal));
            modalObj.transform.SetParent(parent, false);

            RectTransform rt = modalObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            LevelCelebrationModal modal = modalObj.GetComponent<LevelCelebrationModal>();
            modal.Initialize(level, medal, accuracy, duration, mistakes, onClosed);
            return modal;
        }

        private LevelDefinition levelDef;
        private LevelMedal earnedMedal;
        private float accuracyVal;
        private float durationVal;
        private int mistakesVal;
        private Action onClosedCallback;

        private Image[] medalImages = new Image[3];
        private RectTransform[] medalRects = new RectTransform[3];

        public void Initialize(
            LevelDefinition level, 
            LevelMedal medal, 
            float accuracy, 
            float duration, 
            int mistakes, 
            Action onClosed)
        {
            levelDef = level;
            earnedMedal = medal;
            accuracyVal = accuracy;
            durationVal = duration;
            mistakesVal = mistakes;
            onClosedCallback = onClosed;

            BuildVisuals();
            StartCoroutine(AnimateSequence());
        }

        private void BuildVisuals()
        {
            UIDialogs ui = UIDialogs.Current;

            // Dimmed backdrop
            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            RectTransform bdRect = backdrop.GetComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.88f);

            // Card Panel
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(460f, 370f);
            cardRt.anchoredPosition = Vector2.zero;

            Image cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.92f, 0.94f, 0.97f, 1f);

            VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 18, 18);
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
            title.text = ui.celebration_title;
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.12f, 0.22f, 0.38f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 32);

            // Subtitle (Level Name)
            GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI subtitle = subObj.GetComponent<TextMeshProUGUI>();
            string localizedLevelName = levelDef != null ? levelDef.GetLocalizedDisplayName() : null;
            subtitle.text = !string.IsNullOrEmpty(localizedLevelName) ? localizedLevelName : ui.celebration_subtitle_default;
            subtitle.fontSize = 17;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = new Color(0.35f, 0.40f, 0.48f);
            subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);

            // ── Medals Showcase Row (Angry Birds style 3 pedestal slots) ──
            GameObject medalsRow = new GameObject("MedalsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            medalsRow.transform.SetParent(card.transform, false);
            RectTransform mrRt = medalsRow.GetComponent<RectTransform>();
            mrRt.sizeDelta = new Vector2(0, 110);

            HorizontalLayoutGroup hlg = medalsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 28;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Slot 0: Bronze, Slot 1: Silver, Slot 2: Gold
            string[] slotNames = { "Slot_Bronze", "Slot_Silver", "Slot_Gold" };
            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = new GameObject(slotNames[i], typeof(RectTransform), typeof(Image));
                slotObj.transform.SetParent(medalsRow.transform, false);
                RectTransform sRt = slotObj.GetComponent<RectTransform>();
                // Middle medal (Gold) is slightly larger
                float size = (i == 2) ? 88f : (i == 1 ? 78f : 72f);
                sRt.sizeDelta = new Vector2(size, size);

                Image img = slotObj.GetComponent<Image>();
                img.sprite = MedalVisualUtility.GetEmptyMedalSlotSprite();
                img.preserveAspect = true;

                medalImages[i] = img;
                medalRects[i] = sRt;
            }

            // Stats row (Accuracy, Time, Mistakes)
            GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI stats = statsObj.GetComponent<TextMeshProUGUI>();
            string mistakesColor = mistakesVal == 0 ? "#28A745" : "#D9534F";
            stats.text = string.Format(ui.celebration_stats_format, accuracyVal, durationVal, mistakesColor, mistakesVal);
            stats.fontSize = 15;
            stats.alignment = TextAlignmentOptions.Center;
            stats.color = new Color(0.18f, 0.22f, 0.28f);
            stats.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 26);

            // Continue Button
            GameObject btnObj = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(card.transform, false);
            LayoutElement btnLe = btnObj.GetComponent<LayoutElement>();
            btnLe.preferredHeight = 44;
            btnLe.preferredWidth = 180;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.18f, 0.55f, 0.88f);

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(CloseModal);

            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.text = ui.celebration_continue_button;
            btnTxt.fontSize = 18;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.color = Color.white;
        }

        private IEnumerator AnimateSequence()
        {
            yield return new WaitForSeconds(0.35f);

            int targetMedalCount = 0;
            if (earnedMedal == LevelMedal.Bronze) targetMedalCount = 1;
            else if (earnedMedal == LevelMedal.Silver) targetMedalCount = 2;
            else if (earnedMedal == LevelMedal.Gold) targetMedalCount = 3;

            LevelMedal[] medalOrder = { LevelMedal.Bronze, LevelMedal.Silver, LevelMedal.Gold };

            for (int i = 0; i < targetMedalCount; i++)
            {
                yield return StartCoroutine(PopMedalRoutine(i, medalOrder[i]));
                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator PopMedalRoutine(int slotIndex, LevelMedal medalType)
        {
            if (slotIndex < 0 || slotIndex >= medalImages.Length) yield break;

            Image img = medalImages[slotIndex];
            RectTransform rt = medalRects[slotIndex];
            if (img == null || rt == null) yield break;

            // Set the real medal sprite
            img.sprite = MedalVisualUtility.GetMedalSprite(medalType);

            // Audio chime for this medal
            FeedbackManager.PlayMedalChime(slotIndex + 1);

            // Punch/pop scale animation: 0 -> 1.35 -> 1.0 with overshoot
            Vector3 targetScale = Vector3.one;
            rt.localScale = Vector3.zero;

            float elapsed = 0f;
            float popDuration = 0.22f;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                // Overshoot curve
                float scaleFactor = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.35f;
                rt.localScale = targetScale * scaleFactor;
                yield return null;
            }

            elapsed = 0f;
            float settleDuration = 0.14f;
            Vector3 startOvershoot = rt.localScale;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                rt.localScale = Vector3.Lerp(startOvershoot, targetScale, t);
                yield return null;
            }

            rt.localScale = targetScale;
        }

        public void CloseModal()
        {
            FeedbackManager.PlayClick();
            onClosedCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}
