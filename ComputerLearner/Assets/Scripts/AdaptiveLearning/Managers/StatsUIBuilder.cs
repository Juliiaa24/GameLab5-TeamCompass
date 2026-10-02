using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace ComputerLearning
{
    public class StatsUIBuilder : MonoBehaviour
    {
        private void Start()
        {
            // Give window a moment to initialize
            StartCoroutine(BuildUI());
        }

        private System.Collections.IEnumerator BuildUI()
        {
            yield return new WaitForEndOfFrame();
            
            Window window = GetComponentInParent<Window>();
            if (window != null)
            {
                window.setTitle("Learning Progress & Stats");
            }

            // Create root container for SetContent
            GameObject rootContent = new GameObject("StatsRoot", typeof(RectTransform));
            
            // Create Scroll View
            GameObject svObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            svObj.transform.SetParent(rootContent.transform, false);
            RectTransform svRect = svObj.GetComponent<RectTransform>();
            svRect.anchorMin = Vector2.zero; svRect.anchorMax = Vector2.one;
            svRect.offsetMin = new Vector2(10, 10); svRect.offsetMax = new Vector2(-10, -10);
            svObj.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 0.8f);

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
            viewportObj.GetComponent<Image>().color = Color.white;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 15;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = svObj.GetComponent<ScrollRect>();
            scrollRect.content = cRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            BuildStats(contentObj.transform);

            // Use the built-in Window.SetContent so it doesn't break the top bar
            if (window != null)
            {
                window.SetContent(rootContent);
            }
            else
            {
                // Fallback
                rootContent.transform.SetParent(transform, false);
            }
        }

        private void BuildStats(Transform container)
        {
            // Title & Info
            CreateText(container, "Learning Progress", 36, new Color(0.1f, 0.4f, 0.1f), 50);

            // Clear Button
            GameObject clearBtnObj = new GameObject("ClearHistoryBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            clearBtnObj.transform.SetParent(container, false);
            clearBtnObj.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f);
            clearBtnObj.GetComponent<LayoutElement>().minHeight = 40;
            clearBtnObj.GetComponent<LayoutElement>().preferredWidth = 150;
            CreateText(clearBtnObj.transform, "  Delete History  ", 20, Color.white, 40).alignment = TextAlignmentOptions.Center;
            
            clearBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                if (ProgressData.Instance != null)
                {
                    ProgressData.Instance.ClearHistory();
                    Window window = GetComponentInParent<Window>();
                    if (window != null) { window.CloseWindow(); }
                }
            });

            if (ProgressData.Instance == null || ProgressData.Instance.TotalResultCount == 0)
            {
                CreateText(container, "No data available yet.\nGo play some levels!", 24, Color.black, 100);
                return;
            }

            // Group by Skill
            HashSet<string> skills = new HashSet<string>();
            foreach (var res in ProgressData.Instance.TaskHistory) skills.Add(res.skillId);

            foreach (string skill in skills)
            {
                CreateDropdownMenu(container, skill);
            }
        }

        private void CreateDropdownMenu(Transform container, string skillId)
        {
            float successRate = ProgressData.Instance.GetHistoricalSuccessRate(skillId);
            List<TaskResult> history = ProgressData.Instance.GetResultsForSkill(skillId);

            // Main Category Button
            GameObject btnObj = new GameObject("SkillHeader_" + skillId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            btnObj.transform.SetParent(container, false);
            btnObj.GetComponent<Image>().color = new Color(0.8f, 0.9f, 0.8f);
            
            VerticalLayoutGroup vlg = btnObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.spacing = 10;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            
            ContentSizeFitter csf = btnObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(btnObj.transform, $"SKILL: {skillId.ToUpper()} - Accuracy {(successRate * 100f):F0}%", 24, Color.black, 35);

            // Details Panel (Hidden by default)
            GameObject detailsObj = new GameObject("Details", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            detailsObj.transform.SetParent(btnObj.transform, false);
            VerticalLayoutGroup dvlg = detailsObj.GetComponent<VerticalLayoutGroup>();
            dvlg.spacing = 5;
            dvlg.childControlHeight = false; dvlg.childControlWidth = true;
            dvlg.childForceExpandHeight = false; dvlg.childForceExpandWidth = true;
            ContentSizeFitter dcsf = detailsObj.GetComponent<ContentSizeFitter>();
            dcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            detailsObj.SetActive(false);

            // Group history by level
            Dictionary<string, List<TaskResult>> levelGroups = new Dictionary<string, List<TaskResult>>();
            foreach (var task in history)
            {
                string key = string.IsNullOrEmpty(task.levelId) ? "Unknown Level" : task.levelId;
                if (!levelGroups.ContainsKey(key)) levelGroups[key] = new List<TaskResult>();
                levelGroups[key].Add(task);
            }

            foreach (var kvp in levelGroups)
            {
                string lId = kvp.Key;
                List<TaskResult> levelTasks = kvp.Value;
                
                int successes = 0;
                string lName = "";
                foreach (var t in levelTasks) 
                {
                    if (t.success) successes++;
                    if (!string.IsNullOrEmpty(t.levelName)) lName = t.levelName;
                }
                
                int total = levelTasks.Count;
                int errors = total - successes;
                float lAcc = total > 0 ? ((float)successes / total) * 100f : 0f;
                
                string displayName = string.IsNullOrEmpty(lName) ? lId : lName;
                
                CreateText(detailsObj.transform, $"  - {lId.ToUpper()} - \"{displayName}\" - Accuracy {lAcc:F0}%", 20, new Color(0.2f, 0.2f, 0.2f), 30);
                CreateText(detailsObj.transform, $"          - Successes: {successes}   Errors: {errors}   Total: {total}", 18, new Color(0.4f, 0.4f, 0.4f), 25);
            }

            // Toggle logic
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                detailsObj.SetActive(!detailsObj.activeSelf);
                LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
                VirtualMascot.HideMascot();
            });
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int size, Color color, float height)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            txtObj.transform.SetParent(parent, false);
            LayoutElement le = txtObj.GetComponent<LayoutElement>();
            le.minHeight = height;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = TextAlignmentOptions.Left;
            return txt;
        }
    }
}
