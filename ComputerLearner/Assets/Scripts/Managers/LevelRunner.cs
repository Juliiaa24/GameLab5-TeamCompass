using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Executes a specific LevelDefinition (sequence of tasks) inside a Window.
    /// Replaces the old Level1/Level2 hardcoded classes.
    /// Tracks attempts, completion time, accuracy, and evaluates medals.
    /// Enhanced with animated Angry Birds style celebration sequence.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The content area inside the window where tasks will be spawned.")]
        public RectTransform taskContentArea;
        
        [Header("State (Debug)")]
        [SerializeField] private LevelDefinition _currentLevel;
        public LevelDefinition currentLevel => _currentLevel;
        [SerializeField] private int currentTaskIndex = 0;
        public bool IsCompleted { get; private set; } = false;
        
        private BaseTask activeTask;
        private Window appWindow;
        private TutorialDialogs dialogs;

        // Session performance tracking for medal evaluation
        private float levelStartTime = 0f;
        private bool hasRecordedLevelStartTime = false;
        private int tasksAttempted = 0;
        private int tasksSucceeded = 0;
        private int tasksFailed = 0;
        private int extraTaskMistakes = 0;

        private void Awake()
        {
            ReloadDialogs();
            appWindow = GetComponentInParent<Window>();
        }

        private void ReloadDialogs()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            if (dialogs == null) dialogs = new TutorialDialogs();
        }

        public void StartLevel(LevelDefinition level)
        {
            if (level == null || (!level.isAdaptive && (level.tasks == null || level.tasks.Count == 0)))
            {
                Debug.LogError("[LevelRunner] Invalid LevelDefinition provided.");
                return;
            }

            ReloadDialogs();
            if (appWindow == null) appWindow = GetComponentInParent<Window>();

            _currentLevel = level;
            currentTaskIndex = 0;
            IsCompleted = false;

            // Reset run metrics
            tasksAttempted = 0;
            tasksSucceeded = 0;
            tasksFailed = 0;
            extraTaskMistakes = 0;
            hasRecordedLevelStartTime = false;
            
            string localizedTitle = level.GetLocalizedDisplayName();
            if (appWindow != null && !string.IsNullOrEmpty(localizedTitle))
            {
                appWindow.gameObject.name = "Window_" + localizedTitle;
                appWindow.setTitle(localizedTitle);
            }

            ShowInstructionScreen();
        }

        private void ShowInstructionScreen()
        {
            if (currentLevel == null) return;
            
            string localizedInstruction = currentLevel.GetLocalizedInstructionText();

            // If there's no instruction text, skip directly
            if (string.IsNullOrEmpty(localizedInstruction))
            {
                SpawnNextTask();
                return;
            }

            // Create dark background
            GameObject panel = new GameObject("IntroPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(taskContentArea, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.25f, 0.35f, 0.95f); // Dark blueish background

            // Instruction Text
            GameObject textObj = new GameObject("InstructionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.4f); textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Text text = textObj.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = localizedInstruction;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            bool isAutoSequence = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 0 ||
                                  UnityEngine.Object.FindAnyObjectByType<FirstDesktopTutorial>() != null;

            if (isAutoSequence)
            {
                // Generate a lively, localized description based on the level ID
                string livelyMessage = dialogs.intro_default + localizedInstruction;
                string lvlId = currentLevel.levelId != null ? currentLevel.levelId.Trim().ToLowerInvariant() : string.Empty;

                if (lvlId == "level1")
                    livelyMessage = dialogs.intro_move;
                else if (lvlId == "level2")
                    livelyMessage = dialogs.intro_hover;
                else if (lvlId == "level3")
                    livelyMessage = dialogs.intro_click;
                else if (lvlId == "level4")
                    livelyMessage = dialogs.intro_adaptive;
                else if (lvlId == "level5")
                    livelyMessage = dialogs.intro_double_click;
                else if (lvlId == "level6")
                    livelyMessage = dialogs.intro_right_click;
                else if (lvlId == "level7")
                    livelyMessage = dialogs.intro_click_hold;
                else if (lvlId == "level8")
                    livelyMessage = dialogs.intro_drag_drop;
                else if (lvlId == "level9")
                    livelyMessage = dialogs.intro_mixed;

                VirtualMascot.Show(livelyMessage, appWindow.ContentArea, new Vector2(0, -80));
                
                // Automatically transition after 5 seconds instead of waiting for a button click
                StartCoroutine(AutoStartLevelAfterDelay(5f, panel));
            }
            else
            {
                // Normal flow for replayability/after onboarding: Show the manual Play button
                GameObject btnObj = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                btnObj.transform.SetParent(panel.transform, false);
                RectTransform btnRect = btnObj.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.35f, 0.15f); btnRect.anchorMax = new Vector2(0.65f, 0.25f);
                btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
                btnObj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.6f, 0.8f); // Blue

                GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                btnTextObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero; btnTextRect.offsetMax = Vector2.zero;
                UnityEngine.UI.Text btnText = btnTextObj.GetComponent<UnityEngine.UI.Text>();
                btnText.font = text.font;
                btnText.text = !string.IsNullOrEmpty(dialogs.level_start_button) ? dialogs.level_start_button : "START!";
                btnText.fontSize = 28;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;

                UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
                btn.onClick.AddListener(() => {
                    VirtualMascot.HideMascot();
                    Destroy(panel);
                    SpawnNextTask();
                });
                
                // Call the mascot to point to the play button
                VirtualMascot.Show(dialogs.level_ready_prompt, btnRect, new Vector2(250, 50));
            }
        }

        private System.Collections.IEnumerator AutoStartLevelAfterDelay(float delay, GameObject panel)
        {
            yield return new WaitForSeconds(delay);
            VirtualMascot.HideMascot();
            if (panel != null) Destroy(panel);
            SpawnNextTask();
        }

        private System.Collections.IEnumerator AutoCloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (appWindow != null) appWindow.CloseWindow();
        }
        
        private void SpawnNextTask()
        {
            if (currentLevel == null) return;

            bool isComplete = currentLevel.isAdaptive 
                ? currentTaskIndex >= currentLevel.adaptiveTaskCount 
                : currentTaskIndex >= currentLevel.tasks.Count;

            if (isComplete)
            {
                Debug.Log($"[LevelRunner] Level '{currentLevel.displayName}' completed!");
                
                float totalDuration = hasRecordedLevelStartTime ? Mathf.Max(0.5f, Time.time - levelStartTime) : 1f;
                int totalMistakes = tasksFailed + extraTaskMistakes;
                int totalActions = Mathf.Max(1, tasksSucceeded + totalMistakes);
                float accuracy = ((float)tasksSucceeded / totalActions) * 100f;

                LevelMedal medal = ProgressData.CalculateMedal(accuracy, totalMistakes);

                if (ProgressData.Instance != null && currentLevel != null && !string.IsNullOrEmpty(currentLevel.levelId))
                {
                    ProgressData.Instance.RecordLevelAttempt(currentLevel.levelId, medal, accuracy, totalDuration, totalMistakes);
                    ProgressData.Instance.MarkLevelCompleted(currentLevel.levelId);
                }

                string medalName = LevelScoreRecord.GetMedalName(medal);
                string celebrationMsg = !string.IsNullOrEmpty(dialogs.level_celebration_format)
                    ? string.Format(dialogs.level_celebration_format, medalName.ToUpper(), accuracy, totalDuration)
                    : $"Awesome job! You earned the {medalName.ToUpper()} Medal!\nAccuracy: {accuracy:F0}%  Time: {totalDuration:F1}s";

                VirtualMascot.ShowContextualFeedback(celebrationMsg, appWindow != null ? appWindow.ContentArea : null, 5f);
                IsCompleted = true;

                Transform modalParent = appWindow != null ? appWindow.ContentArea : (taskContentArea != null ? taskContentArea : transform);
                LevelCelebrationModal.Show(modalParent, currentLevel, medal, accuracy, totalDuration, totalMistakes, () =>
                {
                    if (currentLevel != null && string.Equals(currentLevel.levelId, "level9", System.StringComparison.OrdinalIgnoreCase))
                    {
                        DesktopManager.Instance?.TriggerAdvancedDesktopTourAfterLevel9();
                    }
                    if (appWindow != null) appWindow.CloseWindow();
                });

                return;
            }

            if (!hasRecordedLevelStartTime)
            {
                levelStartTime = Time.time;
                hasRecordedLevelStartTime = true;
            }

            TaskDefinition def = null;
            if (currentLevel.isAdaptive)
            {
                def = GetNextAdaptiveTask();
                if (def == null)
                {
                    Debug.LogWarning("[LevelRunner] No adaptive tasks found. Ending level early.");
                    currentTaskIndex = currentLevel.adaptiveTaskCount;
                    SpawnNextTask();
                    return;
                }
            }
            else
            {
                def = currentLevel.tasks[currentTaskIndex];
            }
            
            if (TaskManager.Instance == null)
            {
                Debug.LogError("[LevelRunner] TaskManager.Instance is null. Is it in the scene?");
                return;
            }

            activeTask = TaskManager.Instance.SpawnTask(def, taskContentArea);
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted += HandleTaskCompleted;
            }
            else
            {
                Debug.LogError($"[LevelRunner] Failed to spawn task '{def?.displayName}'.");
                currentTaskIndex++;
                SpawnNextTask();
            }
        }

        private TaskDefinition GetNextAdaptiveTask()
        {
            if (SkillManager.Instance == null || TaskManager.Instance == null) return null;

            var weights = SkillManager.Instance.GetSkillWeights();
            TaskDefinition def = null;
            string skillId = null;

            while (weights.Count > 0 && def == null)
            {
                float total = 0f;
                foreach (float w in weights.Values) total += w;

                float r = UnityEngine.Random.value * total;
                foreach (var pair in weights)
                {
                    r -= pair.Value;
                    if (r <= 0f) { skillId = pair.Key; break; }
                }
                if (skillId == null) { foreach (var k in weights.Keys) skillId = k; }

                if (skillId == null) break;

                int difficulty = SkillManager.Instance.GetDifficulty(skillId);
                def = TaskManager.Instance.SelectTask(skillId, difficulty);

                if (def == null) weights.Remove(skillId);
            }

            return def;
        }

        private void HandleTaskCompleted(TaskResult result)
        {
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
                
                // Find the actual prefab root that was spawned inside taskContentArea
                Transform prefabRoot = activeTask.transform;
                while (prefabRoot.parent != taskContentArea && prefabRoot.parent != null)
                {
                    prefabRoot = prefabRoot.parent;
                }
                Destroy(prefabRoot.gameObject);
                
                activeTask = null;
            }

            tasksAttempted++;

            if (result != null)
            {
                if (result.success) tasksSucceeded++;
                else tasksFailed++;

                if (result.attempts > 0)
                {
                    extraTaskMistakes += result.attempts;
                }

                // Inject Level info for Stats
                if (currentLevel != null)
                {
                    result.levelId = currentLevel.levelId;
                    result.levelName = currentLevel.GetLocalizedDisplayName();
                }

                // Apply scores
                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.ApplyResult(result);
                }
                
                if (ProgressData.Instance != null)
                {
                    ProgressData.Instance.RecordResult(result);
                }
            }
            else
            {
                tasksSucceeded++;
            }

            // Move to next
            currentTaskIndex++;
            SpawnNextTask();
        }
        
        private void OnDestroy()
        {
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
            }
            if (IsCompleted && currentLevel != null && string.Equals(currentLevel.levelId, "level9", System.StringComparison.OrdinalIgnoreCase))
            {
                DesktopManager.Instance?.TriggerAdvancedDesktopTourAfterLevel9();
            }
        }
    }
}
