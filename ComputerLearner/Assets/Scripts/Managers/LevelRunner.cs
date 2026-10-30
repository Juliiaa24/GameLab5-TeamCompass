using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    /// <summary>
    /// Executes a specific LevelDefinition (sequence of tasks) inside a Window.
    /// Replaces the old Level1/Level2 hardcoded classes.
    /// Tracks attempts, completion time, accuracy, and evaluates medals.
    /// Enhanced with animated Angry Birds style celebration sequence and discreet progress bar.
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

        // Discreet progress bar UI for target levels (no numbers, slightly larger bar)
        private GameObject progressBarContainer;
        private RectTransform progressFillRect;
        private float displayedProgress = 0f;
        private float targetProgress = 0f;

        private void Awake()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();
            appWindow = GetComponentInParent<Window>();
        }

        private void Update()
        {
            if (progressBarContainer != null && progressBarContainer.activeSelf && progressFillRect != null)
            {
                displayedProgress = Mathf.Lerp(displayedProgress, targetProgress, Time.deltaTime * 10f);
                progressFillRect.anchorMax = new Vector2(Mathf.Clamp01(displayedProgress), 1f);
            }
        }

        public void StartLevel(LevelDefinition level)
        {
            if (level == null || (!level.isAdaptive && (level.tasks == null || level.tasks.Count == 0)))
            {
                Debug.LogError("[LevelRunner] Invalid LevelDefinition provided.");
                return;
            }

            _currentLevel = level;
            currentTaskIndex = 0;
            IsCompleted = false;

            // Reset run metrics
            tasksAttempted = 0;
            tasksSucceeded = 0;
            tasksFailed = 0;
            extraTaskMistakes = 0;
            hasRecordedLevelStartTime = false;
            displayedProgress = 0f;
            targetProgress = 0f;

            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(false);
            }
            
            if (appWindow == null) appWindow = GetComponentInParent<Window>();
            if (appWindow != null && !appWindow.IsMaximized && !DesktopTour.IsTourRunning)
            {
                appWindow.toggleMaximize();
            }
            if (appWindow != null && !string.IsNullOrEmpty(level.displayName))
            {
                appWindow.gameObject.name = "Window_" + level.displayName;
            }

            ShowInstructionScreen();
        }

        private void ShowInstructionScreen()
        {
            if (currentLevel == null) return;
            
            // If there's no instruction text, skip directly
            if (string.IsNullOrEmpty(currentLevel.instructionText))
            {
                SpawnNextTask();
                return;
            }

            // Create dark background
            GameObject panel = new GameObject("IntroPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(taskContentArea, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 0.95f); // Dark blueish background

            // Instruction Text
            GameObject textObj = new GameObject("InstructionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.4f); textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            Text text = textObj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = currentLevel.instructionText;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            bool isAutoSequence = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 0 ||
                                  UnityEngine.Object.FindAnyObjectByType<FirstDesktopTutorial>() != null;

            if (isAutoSequence)
            {
                // Generate a lively, less robotic description based on the level ID
                string livelyMessage = "Get ready! " + currentLevel.instructionText;
                
                if (currentLevel.levelId.Contains("1")) 
                    livelyMessage = "Welcome to your very first app!\nLet's practice moving the mouse. Just hover over the dirt spots to clean them!";
                else if (currentLevel.levelId.Contains("2"))
                    livelyMessage = "Awesome job so far!\nNow things will move a bit faster. Keep those clicks sharp!";
                else if (currentLevel.levelId.Contains("3"))
                    livelyMessage = "Time for something new!\nLet's keep practicing your computer skills. Make sure to read the instructions!";
                else if (currentLevel.levelId.Contains("4"))
                    livelyMessage = "You're a pro now!\nLet's test what you've learned in the Adaptive Challenge!";
                else if (currentLevel.levelId.Contains("5"))
                    livelyMessage = "One more trick!\nDouble click quickly to clear the targets!";

                VirtualMascot.Show(livelyMessage, appWindow.ContentArea, new Vector2(0, -80));
                
                // Automatically transition after 5 seconds instead of waiting for a button click
                StartCoroutine(AutoStartLevelAfterDelay(5f, panel));
            }
            else
            {
                // Normal flow for replayability/after onboarding: Show the manual Play button
                GameObject btnObj = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(panel.transform, false);
                RectTransform btnRect = btnObj.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.35f, 0.15f); btnRect.anchorMax = new Vector2(0.65f, 0.25f);
                btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
                btnObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.8f); // Blue

                GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                btnTextObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero; btnTextRect.offsetMax = Vector2.zero;
                Text btnText = btnTextObj.GetComponent<Text>();
                btnText.font = text.font;
                btnText.text = "START!";
                btnText.fontSize = 28;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;

                Button btn = btnObj.GetComponent<Button>();
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

                if (progressBarContainer != null)
                {
                    progressBarContainer.SetActive(false);
                }
                
                float totalDuration = hasRecordedLevelStartTime ? Mathf.Max(0.5f, Time.time - levelStartTime) : 1f;
                int totalMistakes = tasksFailed + extraTaskMistakes;
                int totalActions = Mathf.Max(1, tasksSucceeded + totalMistakes);
                float accuracy = ((float)tasksSucceeded / totalActions) * 100f;

                LevelMedal medal = ProgressData.CalculateMedal(accuracy, totalMistakes);

                bool improvedMedal = false;
                LevelMedal previousBestMedal = LevelMedal.None;
                if (ProgressData.Instance != null && currentLevel != null && !string.IsNullOrEmpty(currentLevel.levelId))
                {
                    LevelScoreRecord prevRecord = ProgressData.Instance.GetLevelRecord(currentLevel.levelId);
                    bool hadPreviousAttempt = prevRecord != null && prevRecord.attemptsCount >= 1;
                    previousBestMedal = prevRecord != null ? prevRecord.bestMedal : LevelMedal.None;
                    improvedMedal = hadPreviousAttempt && (int)medal > (int)previousBestMedal;

                    ProgressData.Instance.RecordLevelAttempt(currentLevel.levelId, medal, accuracy, totalDuration, totalMistakes);
                    ProgressData.Instance.MarkLevelCompleted(currentLevel.levelId);
                }

                IsCompleted = true;

                Transform modalParent = appWindow != null ? appWindow.ContentArea : (taskContentArea != null ? taskContentArea : transform);
                LevelCelebrationModal.Show(modalParent, currentLevel, medal, accuracy, totalDuration, totalMistakes, () =>
                {
                    VirtualMascot.HideMascot();
                    if (currentLevel != null && string.Equals(currentLevel.levelId, "level9", System.StringComparison.OrdinalIgnoreCase))
                    {
                        DesktopManager.Instance?.TriggerAdvancedDesktopTourAfterLevel9();
                    }
                    if (appWindow != null) appWindow.CloseWindow();
                });

                if (improvedMedal)
                {
                    VirtualMascot.ShowMedalImprovementFeedback(
                        previousBestMedal,
                        medal,
                        accuracy,
                        totalDuration,
                        null,
                        5.5f);
                }
                else
                {
                    VirtualMascot.HideMascot();
                }

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
                activeTask.OnSubTaskProgressChanged += HandleSubTaskProgressChanged;
                RefreshProgressBar();
            }
            else
            {
                Debug.LogError($"[LevelRunner] Failed to spawn task '{def?.displayName}'.");
                currentTaskIndex++;
                SpawnNextTask();
            }
        }

        private int GetTotalTasksCount()
        {
            if (currentLevel == null) return 1;
            return currentLevel.isAdaptive
                ? Mathf.Max(1, currentLevel.adaptiveTaskCount)
                : Mathf.Max(1, currentLevel.tasks != null ? currentLevel.tasks.Count : 1);
        }

        private bool ShouldShowProgressBar()
        {
            if (currentLevel == null || IsCompleted) return false;

            int totalTasks = GetTotalTasksCount();
            // Avoid duplicating MoveMouseTask's built-in "Cleaning Progress..." bar in single-task Level 1
            if (totalTasks == 1 && activeTask is MoveMouseTask)
            {
                return false;
            }

            return true;
        }

        private void EnsureProgressBarCreated()
        {
            if (progressBarContainer != null) return;

            Transform parent = taskContentArea != null ? taskContentArea : transform;
            progressBarContainer = new GameObject("LevelProgressBarContainer", typeof(RectTransform), typeof(CanvasGroup));
            progressBarContainer.transform.SetParent(parent, false);

            CanvasGroup cg = progressBarContainer.GetComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            RectTransform containerRect = progressBarContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.28f, 1f);
            containerRect.anchorMax = new Vector2(0.72f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0f, -14f);
            containerRect.sizeDelta = new Vector2(0f, 14f);

            // Outer frame / track border
            GameObject borderObj = new GameObject("TrackBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderObj.transform.SetParent(progressBarContainer.transform, false);
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0.08f, 0.12f, 0.18f, 0.48f);
            borderImg.raycastTarget = false;

            // Inner track background
            GameObject trackObj = new GameObject("TrackBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObj.transform.SetParent(borderObj.transform, false);
            RectTransform trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = new Vector2(2f, 2f);
            trackRect.offsetMax = new Vector2(-2f, -2f);
            Image trackImg = trackObj.GetComponent<Image>();
            trackImg.color = new Color(0.18f, 0.24f, 0.32f, 0.38f);
            trackImg.raycastTarget = false;

            // Fill bar
            GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(trackObj.transform, false);
            progressFillRect = fillObj.GetComponent<RectTransform>();
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(0f, 1f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.color = new Color(0.26f, 0.82f, 0.50f, 0.92f);
            fillImg.raycastTarget = false;
        }

        private void RefreshProgressBar()
        {
            if (!ShouldShowProgressBar())
            {
                if (progressBarContainer != null) progressBarContainer.SetActive(false);
                return;
            }

            EnsureProgressBarCreated();
            progressBarContainer.SetActive(true);
            progressBarContainer.transform.SetAsLastSibling();

            int totalTasks = GetTotalTasksCount();
            float subProgress = activeTask != null ? Mathf.Clamp01(activeTask.SubTaskProgress) : 0f;
            targetProgress = Mathf.Clamp01((currentTaskIndex + subProgress) / totalTasks);
        }

        private void HandleSubTaskProgressChanged(float subProgress)
        {
            RefreshProgressBar();
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
                activeTask.OnSubTaskProgressChanged -= HandleSubTaskProgressChanged;
                
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
                    result.levelName = currentLevel.displayName;
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
            int totalTasks = GetTotalTasksCount();
            targetProgress = Mathf.Clamp01((float)currentTaskIndex / totalTasks);
            SpawnNextTask();
        }
        
        private void OnDestroy()
        {
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
                activeTask.OnSubTaskProgressChanged -= HandleSubTaskProgressChanged;
            }
            if (gameObject.scene.isLoaded && IsCompleted && currentLevel != null && string.Equals(currentLevel.levelId, "level9", System.StringComparison.OrdinalIgnoreCase))
            {
                if (DesktopManager.HasInstance)
                {
                    DesktopManager.Instance.TriggerAdvancedDesktopTourAfterLevel9();
                }
            }
        }
    }
}
