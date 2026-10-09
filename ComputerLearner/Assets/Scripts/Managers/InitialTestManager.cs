/**
 * Author: Diego / AI
 * Date: 30/09/26
 * Description: Runs the Initial Assessment Test to evaluate the child's starting skills.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Runs a short diagnostic assessment to estimate the child's starting skill scores.
    /// Results feed directly into SkillManager using SetScore rather than normal gameplay progression.
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    public class InitialTestManager : MonoBehaviour
    {
        #region Public Variables
        public static InitialTestManager Instance { get; private set; }

        public bool IsTestActive { get; private set; }
        public event Action OnTestCompleted;
        #endregion

        #region Private Variables
        private Window appWindow;
        private RectTransform taskContentArea;
        private LevelDefinition currentLevel;
        private int currentIndex = 0;
        private BaseTask currentTask;
        
        private TutorialDialogs dialogs;
        
        private class AssessmentSkillData
        {
            public int totalAttempts = 0;
            public int successfulAttempts = 0;
            public int totalTasks = 0;
            public int completedTasks = 0;
        }
        
        private Dictionary<string, AssessmentSkillData> assessmentData = new Dictionary<string, AssessmentSkillData>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            TextAsset json = Resources.Load<TextAsset>("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        public void StartAssessment(LevelDefinition levelDef, Window window)
        {
            if (IsTestActive)
            {
                Debug.LogWarning("[InitialTestManager] Assessment already active.");
                return;
            }

            if (levelDef == null || levelDef.tasks == null || levelDef.tasks.Count == 0)
            {
                Debug.LogWarning("[InitialTestManager] No tasks in assessment level. Skipping.");
                FinishTest();
                return;
            }

            IsTestActive = true;
            appWindow = window;
            taskContentArea = appWindow.ContentArea;
            currentLevel = levelDef;
            currentIndex = 0;
            assessmentData.Clear();

            Debug.Log($"[InitialTestManager] Starting assessment: '{levelDef.displayName}' with {levelDef.tasks.Count} tasks.");
            
            ShowInstructionScreen();
        }

        public void SetContentArea(RectTransform area)
        {
            // Maintained for compatibility with GameFlowController, though now dynamically set via Window
            this.taskContentArea = area;
        }

        public void SkipTest()
        {
            Debug.Log("[InitialTestManager] Initial test skipped.");
            FinishTest();
        }
        #endregion

        #region Private Methods
        private void ShowInstructionScreen()
        {
            if (currentLevel == null) return;
            
            GameObject panel = new GameObject("AssessmentIntroPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(taskContentArea, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.35f, 0.95f); 

            GameObject textObj = new GameObject("InstructionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.4f); textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Text text = textObj.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = string.IsNullOrEmpty(currentLevel.instructionText) ? "Initial Assessment" : currentLevel.instructionText;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            GameObject btnObj = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(panel.transform, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.35f, 0.15f); btnRect.anchorMax = new Vector2(0.65f, 0.25f);
            btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
            btnObj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.8f, 0.4f, 0.2f); // Orange

            GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero; btnTextRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Text btnText = btnTextObj.GetComponent<UnityEngine.UI.Text>();
            btnText.font = text.font;
            btnText.text = "START TEST";
            btnText.fontSize = 28;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => {
                VirtualMascot.HideMascot();
                Destroy(panel);
                SpawnNextTask();
            });
            
            VirtualMascot.Show("Let's see what you already know! Click START TEST when you are ready.", btnRect, new Vector2(250, 50));
        }

        private void SpawnNextTask()
        {
            if (currentIndex >= currentLevel.tasks.Count) 
            { 
                CalculateAndStoreResults();
                ShowCompletionScreen();
                return; 
            }

            // Window might have been closed prematurely
            if (appWindow == null || appWindow.IsClosed)
            {
                Debug.LogWarning("[InitialTestManager] Assessment window closed prematurely. Incomplete test.");
                // We represent this explicitly by calculating with whatever we got so far
                CalculateAndStoreResults();
                FinishTest();
                return;
            }

            TaskManager tm = TaskManager.Instance;
            if (tm == null) { Debug.LogError("[InitialTestManager] TaskManager not found!"); FinishTest(); return; }

            TaskDefinition def = currentLevel.tasks[currentIndex];
            currentTask = tm.SpawnTask(def, taskContentArea);

            if (currentTask == null)
            {
                Debug.LogWarning($"[InitialTestManager] Could not spawn task '{def.displayName}' — skipping.");
                currentIndex++;
                SpawnNextTask();
                return;
            }

            currentTask.OnTaskCompleted += HandleTaskCompleted;
            
            // Brief mascot instruction per task based on skill
            string mascotMsg = "Try this task!";
            if (def.primarySkill != null)
            {
                string skillId = def.primarySkill.skillId.ToLower();
                if (skillId.Contains("move")) mascotMsg = "Move the mouse to clean the spots!";
                else if (skillId.Contains("hover")) mascotMsg = "Hover your mouse over the target and wait!";
                else if (skillId.Contains("double")) mascotMsg = "Double click quickly!";
                else if (skillId.Contains("right")) mascotMsg = "Use the right mouse button!";
                else if (skillId.Contains("hold")) mascotMsg = "Click and hold the button down!";
                else if (skillId.Contains("drag")) mascotMsg = "Click, hold, and drag the item!";
                else if (skillId.Contains("click")) mascotMsg = "Click the target!";
            }
            
            // Create a temporary anchor at the bottom-left of the screen so the mascot doesn't block the task
            GameObject mascotAnchor = GameObject.Find("MascotAssessmentAnchor");
            if (mascotAnchor == null)
            {
                mascotAnchor = new GameObject("MascotAssessmentAnchor", typeof(RectTransform));
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                mascotAnchor.transform.SetParent(canvas != null ? canvas.transform : taskContentArea, false);
                RectTransform anchorRect = mascotAnchor.GetComponent<RectTransform>();
                anchorRect.anchorMin = new Vector2(0.15f, 0.15f);
                anchorRect.anchorMax = new Vector2(0.15f, 0.15f);
                anchorRect.anchoredPosition = Vector2.zero;
            }
            VirtualMascot.Show(mascotMsg, mascotAnchor.GetComponent<RectTransform>());
        }

        private void HandleTaskCompleted(TaskResult result)
        {
            if (currentTask != null)
            {
                currentTask.OnTaskCompleted -= HandleTaskCompleted;
                Transform prefabRoot = currentTask.transform;
                while (prefabRoot.parent != taskContentArea && prefabRoot.parent != null)
                {
                    prefabRoot = prefabRoot.parent;
                }
                Destroy(prefabRoot.gameObject);
                currentTask = null;
            }
            
            VirtualMascot.HideMascot();

            // Record into assessment dictionary
            if (result != null)
            {
                result.levelId = "assessment";
                result.levelName = currentLevel != null ? currentLevel.displayName : "Assessment";

                if (!assessmentData.ContainsKey(result.skillId))
                {
                    assessmentData[result.skillId] = new AssessmentSkillData();
                }
                
                var data = assessmentData[result.skillId];
                data.totalTasks++;
                data.totalAttempts += result.attempts;
                if (result.success) 
                {
                    data.successfulAttempts++;
                    data.completedTasks++;
                }

                // Still record in ProgressData for historical logs, but it won't affect regular Level progression
                if (ProgressData.Instance != null)
                {
                    ProgressData.Instance.RecordResult(result);
                }
            }

            currentIndex++;
            SpawnNextTask();
        }

        private void CalculateAndStoreResults()
        {
            if (SkillManager.Instance == null) 
            {
                Debug.LogWarning("[InitialTestManager] SkillManager is null! The scores will be printed but not saved to the system.");
            }

            foreach (var kvp in assessmentData)
            {
                string skillId = kvp.Key;
                AssessmentSkillData data = kvp.Value;

                float finalScore = 0f;
                if (data.completedTasks > 0)
                {
                    float rawScore = ((float)data.completedTasks / data.totalTasks) * 100f;
                    // Penalty for extra attempts (child did not perform it independently on first try)
                    int extraAttempts = data.totalAttempts - data.completedTasks;
                    float penalty = extraAttempts * 10f; 
                    finalScore = Mathf.Clamp(rawScore - penalty, 0f, 100f);
                }

                Debug.Log($"[InitialTestManager] Assessment Result for {skillId}: Score = {finalScore} (Tasks: {data.completedTasks}/{data.totalTasks}, Total Attempts: {data.totalAttempts})");
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.SetScore(skillId, finalScore);
                }
            }
            
            PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
            PlayerPrefs.Save();
        }

        private void ShowCompletionScreen()
        {
            VirtualMascot.Show("Assessment Complete! Great job!", taskContentArea, new Vector2(0, -80));
            StartCoroutine(AutoCloseAfterDelay(3.5f));
        }

        private IEnumerator AutoCloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (appWindow != null) appWindow.CloseWindow();
            FinishTest();
        }

        private void FinishTest()
        {
            IsTestActive = false;
            Debug.Log("[InitialTestManager] Initial test complete.");
            OnTestCompleted?.Invoke();
        }
        #endregion
    }
}
