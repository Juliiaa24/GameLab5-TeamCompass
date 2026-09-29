using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>A skill accordion. The clipping panel's layout height animates between closed and open.</summary>
    public class SkillGradeRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI averageText;
        [SerializeField] private TextMeshProUGUI expandIndicator;
        [SerializeField] private RectTransform breakdownList;
        [SerializeField] private LayoutElement breakdownHeight;
        [SerializeField] private LevelBreakdownRowUI breakdownRowPrefab;
        [SerializeField] private GameObject emptyState;
        [SerializeField, Min(0f)] private float animationDuration = 0.25f;

        private bool expanded;
        private Coroutine animation;

        public void Setup(SkillID skill, float average, IReadOnlyDictionary<LevelID, SkillEvaluationData> details)
        {
            if (skillNameText != null)
                skillNameText.text = GetSkillName(skill);
            if (averageText != null)
                averageText.text = details != null && details.Count > 0
                    ? $"{average.ToString("F1", CultureInfo.InvariantCulture)} / 10"
                    : "Not graded";

            if (breakdownList == null || breakdownHeight == null || breakdownRowPrefab == null)
                return;

            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }

            for (int i = breakdownList.childCount - 1; i >= 0; i--)
            {
                Transform child = breakdownList.GetChild(i);
                if (emptyState != null && child.gameObject == emptyState)
                    continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (emptyState != null)
                emptyState.SetActive(details == null || details.Count == 0);

            if (details != null)
            {
                List<LevelID> levels = new List<LevelID>(details.Keys);
                levels.Sort();
                foreach (LevelID level in levels)
                    Instantiate(breakdownRowPrefab, breakdownList).Setup(level, details[level]);
            }

            Canvas.ForceUpdateCanvases();
            breakdownHeight.preferredHeight = expanded ? GetOpenHeight() : 0f;
            UpdateIndicator();
            Rebuild();
        }

        /// <summary>Assign this method to HeaderButton's On Click event.</summary>
        public void Toggle()
        {
            if (breakdownHeight == null || breakdownList == null)
                return;

            expanded = !expanded;
            UpdateIndicator();
            if (animation != null)
                StopCoroutine(animation);

            Canvas.ForceUpdateCanvases();
            float destination = expanded ? GetOpenHeight() : 0f;
            animation = StartCoroutine(Animate(destination));
        }

        private IEnumerator Animate(float destination)
        {
            float start = breakdownHeight.preferredHeight;
            float elapsed = 0f;
            if (animationDuration > 0f)
            {
                while (elapsed < animationDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    t = t * t * (3f - 2f * t);
                    breakdownHeight.preferredHeight = Mathf.Lerp(start, destination, t);
                    Rebuild();
                    yield return null;
                }
            }
            breakdownHeight.preferredHeight = destination;
            Rebuild();
            animation = null;
        }

        private float GetOpenHeight()
        {
            return LayoutUtility.GetPreferredHeight(breakdownList);
        }

        private void Rebuild()
        {
            RectTransform row = transform as RectTransform;
            if (row != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            RectTransform content = transform.parent as RectTransform;
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void UpdateIndicator()
        {
            if (expandIndicator != null)
                expandIndicator.text = expanded ? "−" : "+";
        }

        private static string GetSkillName(SkillID skill)
        {
            switch (skill)
            {
                case SkillID.MOVE: return "Move";
                case SkillID.CLICK: return "Click";
                case SkillID.HOLD: return "Click and hold";
                case SkillID.DROP: return "Drag and drop";
                case SkillID.SCROLL: return "Scroll";
                default: return skill.ToString();
            }
        }
    }
}
