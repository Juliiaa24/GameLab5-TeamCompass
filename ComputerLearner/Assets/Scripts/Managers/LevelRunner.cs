using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

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
        
        public static LevelRunner ActiveRunner { get; private set; }

        private BaseTask activeTask;
        private Window appWindow;
        private TutorialDialogs dialogs;

        // Session performance tracking & additive scoring (starts at 0 points)
        private float levelStartTime = 0f;
        private bool hasRecordedLevelStartTime = false;
        private int tasksAttempted = 0;
        private int tasksSucceeded = 0;
        private int tasksFailed = 0;
        private int extraTaskMistakes = 0;
        private int currentLevelScore = 0;
        private int maxPossibleScore = 0;

        public int CurrentLevelScore => currentLevelScore;
        public int MaxPossibleScore => maxPossibleScore;

        // Discreet progress bar UI for target levels (no numbers, slightly larger bar)
        [Header("Pre-Built Window UI (Serialized in Prefabs)")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private TMP_Text introTitleText;
        [SerializeField] private TMP_Text introBodyText;
        [SerializeField] private Button introStartButton;
        [SerializeField] private TMP_Text introStartButtonText;
        [SerializeField] private GameObject videoDemoFrame;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private GameObject progressBarContainer;
        [SerializeField] private RectTransform progressFillRect;

        private float displayedProgress = 0f;
        private float targetProgress = 0f;

        private void Awake()
        {
            ActiveRunner = this;
            ReloadDialogs();
            appWindow = GetComponentInParent<Window>();
        }

        public int AwardHitPoints(int exactPoints = 100, int maxTargetPoints = -1)
        {
            int pts = Mathf.Max(1, exactPoints);
            int maxPts = maxTargetPoints > 0 ? Mathf.Max(pts, maxTargetPoints) : pts;
            currentLevelScore += pts;
            maxPossibleScore += maxPts;
            return pts;
        }

        public void RegisterMissedMaxPoints(int maxTargetPoints = 100)
        {
            maxPossibleScore += Mathf.Max(1, maxTargetPoints);
        }

        public int ApplyMistakePenalty(int penalty = 50)
        {
            int absPenalty = Mathf.Abs(penalty);
            currentLevelScore = Mathf.Max(0, currentLevelScore - absPenalty);
            return -absPenalty;
        }

        private void ReloadDialogs()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            if (dialogs == null) dialogs = new TutorialDialogs();
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

            ReloadDialogs();
            if (appWindow == null) appWindow = GetComponentInParent<Window>();

            _currentLevel = level;
            currentTaskIndex = 0;
            IsCompleted = false;

            // Reset run metrics (additive scoring starts at 0 points)
            ActiveRunner = this;
            tasksAttempted = 0;
            tasksSucceeded = 0;
            tasksFailed = 0;
            extraTaskMistakes = 0;
            currentLevelScore = 0;
            maxPossibleScore = 0;
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

            string localizedTitle = level.GetLocalizedDisplayName();
            if (appWindow != null && !string.IsNullOrEmpty(localizedTitle))
            {
                appWindow.gameObject.name = "Window_" + localizedTitle;
                appWindow.setTitle(localizedTitle);
            }

            ShowInstructionScreen();
        }

        private void ResolvePrebuiltUI()
        {
            Transform searchRoot = taskContentArea != null
                ? taskContentArea
                : (appWindow != null ? appWindow.ContentArea : transform);
            if (searchRoot == null) return;

            if (introPanel == null)
                introPanel = searchRoot.Find("LevelIntroPanel")?.gameObject;

            if (introPanel != null)
            {
                Transform t = introPanel.transform;
                Transform card = t.Find("IntroCard") ?? t;
                if (introTitleText == null)
                    introTitleText = card.Find("IntroTitle")?.GetComponent<TMP_Text>();
                if (introBodyText == null)
                    introBodyText = card.Find("IntroBody")?.GetComponent<TMP_Text>();
                if (videoDemoFrame == null)
                    videoDemoFrame = card.Find("VideoDemoFrame")?.gameObject;
                if (videoDemoFrame != null)
                {
                    if (videoRawImage == null)
                        videoRawImage = videoDemoFrame.transform.Find("VideoRawImage")?.GetComponent<RawImage>()
                                        ?? videoDemoFrame.GetComponentInChildren<RawImage>(true);
                    if (videoPlayer == null)
                        videoPlayer = videoDemoFrame.GetComponentInChildren<VideoPlayer>(true);
                }
                if (introStartButton == null)
                    introStartButton = card.Find("Btn-IntroStart")?.GetComponent<Button>();
                if (introStartButton != null && introStartButtonText == null)
                    introStartButtonText = introStartButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (progressBarContainer == null)
                progressBarContainer = searchRoot.Find("LevelProgressBarContainer")?.gameObject;
            if (progressBarContainer != null && progressFillRect == null)
                progressFillRect = progressBarContainer.transform.Find("TrackBorder/TrackBg/ProgressFill")?.GetComponent<RectTransform>();
        }

        private void StopDemoVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
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

            ResolvePrebuiltUI();

            if (introPanel == null)
            {
                Debug.LogWarning("[LevelRunner] Pre-serialized LevelIntroPanel not found in window prefab; starting task directly.");
                SpawnNextTask();
                return;
            }

            introPanel.SetActive(true);
            introPanel.transform.SetAsLastSibling();

            if (introTitleText != null)
            {
                string title = currentLevel.GetLocalizedDisplayName();
                introTitleText.text = !string.IsNullOrEmpty(title) ? title : currentLevel.displayName;
            }

            if (introBodyText != null)
            {
                introBodyText.text = localizedInstruction;
            }

            // Configure pre-serialized Video Demonstration if the Level/Task defines one
            VideoClip demoClip = currentLevel.GetVideoDemonstration();
            if (videoDemoFrame != null)
            {
                if (demoClip != null && videoPlayer != null && videoRawImage != null)
                {
                    videoDemoFrame.SetActive(true);
                    RenderTexture targetRt = videoPlayer.targetTexture != null
                        ? videoPlayer.targetTexture
                        : (videoRawImage.texture as RenderTexture ?? RetroThemeAssets.Instance?.videoDemoRenderTexture);
                    if (targetRt != null)
                    {
                        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                        videoPlayer.targetTexture = targetRt;
                        videoRawImage.texture = targetRt;
                    }
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                    videoPlayer.source = VideoSource.VideoClip;
                    videoPlayer.clip = demoClip;
                    videoPlayer.isLooping = true;
                    videoPlayer.Stop();
                    videoPlayer.Play();
                }
                else
                {
                    StopDemoVideo();
                    videoDemoFrame.SetActive(false);
                }
            }

            bool isAutoSequence = PlayerPrefs.GetInt("AutoSequenceCompleted", 0) == 0 ||
                                  UnityEngine.Object.FindAnyObjectByType<FirstDesktopTutorial>() != null;

            if (isAutoSequence)
            {
                if (introStartButton != null)
                    introStartButton.gameObject.SetActive(false);

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

                VirtualMascot.Show(livelyMessage, appWindow != null ? appWindow.ContentArea : taskContentArea, new Vector2(0, -80));

                // Automatically transition after 5 seconds instead of waiting for a button click
                StartCoroutine(AutoStartLevelAfterDelay(5f));
            }
            else if (introStartButton != null)
            {
                introStartButton.gameObject.SetActive(true);
                if (introStartButtonText != null)
                    introStartButtonText.text = !string.IsNullOrEmpty(dialogs.level_start_button) ? dialogs.level_start_button : "START!";

                introStartButton.onClick.RemoveAllListeners();
                introStartButton.onClick.AddListener(() =>
                {
                    VirtualMascot.HideMascot();
                    StopDemoVideo();
                    if (introPanel != null) introPanel.SetActive(false);
                    SpawnNextTask();
                });

                RectTransform btnRect = introStartButton.GetComponent<RectTransform>();
                VirtualMascot.Show(dialogs.level_ready_prompt, btnRect, new Vector2(250, 50));
            }
        }

        private System.Collections.IEnumerator AutoStartLevelAfterDelay(float delay)
        {
            yield return VirtualMascot.WaitForDialogue(delay);
            VirtualMascot.HideMascot();
            StopDemoVideo();
            if (introPanel != null) introPanel.SetActive(false);
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

                if (maxPossibleScore <= 0)
                {
                    int fallbackTasks = Mathf.Max(1, tasksSucceeded);
                    maxPossibleScore = fallbackTasks * 100;
                    currentLevelScore = Mathf.Max(0, fallbackTasks * 100 - (totalMistakes * 50));
                }

                LevelMedal medal = ProgressData.CalculateMedalFromPoints(currentLevelScore, maxPossibleScore);

                bool improvedMedal = false;
                LevelMedal previousBestMedal = LevelMedal.None;
                if (ProgressData.Instance != null && currentLevel != null && !string.IsNullOrEmpty(currentLevel.levelId))
                {
                    LevelScoreRecord prevRecord = ProgressData.Instance.GetLevelRecord(currentLevel.levelId);
                    bool hadPreviousAttempt = prevRecord != null && prevRecord.attemptsCount >= 1;
                    previousBestMedal = prevRecord != null ? prevRecord.bestMedal : LevelMedal.None;
                    improvedMedal = hadPreviousAttempt && (int)medal > (int)previousBestMedal;

                    ProgressData.Instance.RecordLevelAttempt(currentLevel.levelId, medal, accuracy, totalDuration, totalMistakes, currentLevelScore, maxPossibleScore);
                    ProgressData.Instance.MarkLevelCompleted(currentLevel.levelId);
                }

                IsCompleted = true;

                Transform modalParent = appWindow != null ? appWindow.ContentArea : (taskContentArea != null ? taskContentArea : transform);
                LevelCelebrationModal.Show(modalParent, currentLevel, medal, accuracy, totalDuration, totalMistakes, () =>
                {
                    VirtualMascot.HideMascot();
                    if (currentLevel != null && string.Equals(currentLevel.levelId, "level9", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (PlayerPrefs.GetInt("AdvancedDesktopTourCompleted", 0) == 0 && !VirtualDesktopGuidedLevels.IsLevelRunning)
                        {
                            DesktopManager.Instance?.TriggerAdvancedDesktopTourAfterLevel9();
                        }
                    }
                    if (appWindow != null) appWindow.CloseWindow();
                }, null, currentLevelScore, maxPossibleScore);

                VirtualMascot.ShowMedalImprovementFeedback(
                    improvedMedal ? previousBestMedal : LevelMedal.None,
                    medal,
                    accuracy,
                    totalDuration,
                    null,
                    6.5f);

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
            return true;
        }

        private void EnsureProgressBarCreated()
        {
            if (progressBarContainer == null)
                ResolvePrebuiltUI();
        }

        private void RefreshProgressBar()
        {
            if (!ShouldShowProgressBar())
            {
                if (progressBarContainer != null) progressBarContainer.SetActive(false);
                return;
            }

            EnsureProgressBarCreated();
            if (progressBarContainer == null) return;
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

            if (VirtualMascot.Instance != null && !VirtualMascot.Instance.IsPinnedAtWorldPos)
            {
                VirtualMascot.HideMascot();
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
            int totalTasks = GetTotalTasksCount();
            targetProgress = Mathf.Clamp01((float)currentTaskIndex / totalTasks);
            SpawnNextTask();
        }
        
        private void OnDestroy()
        {
            if (ActiveRunner == this) ActiveRunner = null;
            if (activeTask != null)
            {
                activeTask.OnTaskCompleted -= HandleTaskCompleted;
                activeTask.OnSubTaskProgressChanged -= HandleSubTaskProgressChanged;
            }
            // Level 9 tour trigger handled in celebration modal onClosed
        }
    }
}
