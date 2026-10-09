/**
 * Author: Julia & AI (TeamCompass)
 * Date: 09/10/26
 * Description: Clean, geometric Initial Assessment Test for ComputerLearner in Light Mode.
 *              Evaluates basic mouse skills independently with minimal feedback,
 *              timeouts, skip options, English localization via JSON, and auto-export
 *              of evaluation results to Assets/Assessment/{HH-mm-ss}.json.
 */
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ComputerLearning
{
    [Serializable]
    public class AssessmentUIConfig
    {
        public string stepFormat = "Step {0} of {1}";
        public string skipButton = "Skip";
        public string exitButton = "Exit";
        public string timeExpired = "Time expired";
        public string completedTitle = "Assessment Completed";
        public string completedSubtitle = "Your initial computer skills have been evaluated.";
        public string closeButton = "Finish";
    }

    [Serializable]
    public class AssessmentTaskConfig
    {
        public string skillId;
        public string title;
        public string instruction;
        public float timeoutSeconds = 15f;
        public string shape; // "circle", "square", "rectangle", "drag_drop"
        public float requiredDuration = 0f;
    }

    [Serializable]
    public class AssessmentConfig
    {
        public AssessmentUIConfig ui = new AssessmentUIConfig();
        public List<AssessmentTaskConfig> tasks = new List<AssessmentTaskConfig>();
    }

    [Serializable]
    public class AssessmentExportReport
    {
        public string timestamp;
        public string formattedTime;
        public int totalTasksTested;
        public int completedTasks;
        public float averageScore;
        public List<AssessmentSkillExportItem> skills = new List<AssessmentSkillExportItem>();
    }

    [Serializable]
    public class AssessmentSkillExportItem
    {
        public string skillId;
        public string skillName;
        public bool completed;
        public float score;
        public int extraAttempts;
        public float timeTakenSeconds;
    }

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

        private AssessmentConfig config;
        private int currentTaskIndex = 0;

        // UI references
        private GameObject rootContainer;
        private TextMeshProUGUI stepText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI instructionText;
        private TextMeshProUGUI statusText;
        private RectTransform timerBarFill;
        private RectTransform stageArea;

        // Active task tracking
        private float currentTaskTimer = 0f;
        private float currentTaskTimeout = 15f;
        private int currentAttempts = 0;
        private bool isTaskResolving = false;
        private Coroutine activeTaskRoutine;

        // Procedural assets
        private static Sprite cachedCircleSprite;

        // Results tracking
        private class SkillResultEntry
        {
            public string skillId;
            public string title;
            public bool completed;
            public int extraAttempts;
            public float timeTaken;
            public float finalScore;
        }

        private Dictionary<string, SkillResultEntry> recordedResults = new Dictionary<string, SkillResultEntry>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadConfiguration();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsTestActive || isTaskResolving || currentTaskIndex >= config.tasks.Count) return;

            // Handle timeout countdown
            currentTaskTimer += Time.deltaTime;
            if (timerBarFill != null && currentTaskTimeout > 0f)
            {
                float progress = Mathf.Clamp01(1f - (currentTaskTimer / currentTaskTimeout));
                timerBarFill.localScale = new Vector3(progress, 1f, 1f);

                // Tint red when below 3 seconds
                Image fillImg = timerBarFill.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = (currentTaskTimeout - currentTaskTimer <= 3f)
                        ? new Color(0.88f, 0.20f, 0.20f) // Soft Red
                        : new Color(0.15f, 0.55f, 0.95f); // Modern Sky Blue
                }
            }

            if (currentTaskTimer >= currentTaskTimeout)
            {
                HandleTaskTimeout();
            }
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

            IsTestActive = true;
            appWindow = window;
            taskContentArea = appWindow != null ? appWindow.ContentArea : null;
            currentLevel = levelDef;
            currentTaskIndex = 0;
            recordedResults.Clear();

            // Completely hide the virtual mascot during the clean assessment
            VirtualMascot.HideMascot();

            // Reload configuration to ensure active language is applied
            LoadConfiguration();

            BuildCleanUI();
            SpawnCurrentTask();
        }

        public void SetContentArea(RectTransform area)
        {
            this.taskContentArea = area;
        }

        public void SkipTest()
        {
            Debug.Log("[InitialTestManager] Assessment skipped by user.");
            CalculateAndStoreResults();
            FinishTest();
        }
        #endregion

        #region Setup & Configuration
        private void LoadConfiguration()
        {
            TextAsset jsonAsset = LocalizationManager.LoadLocalizedResource("AssessmentInstructions");
            if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
            {
                config = JsonUtility.FromJson<AssessmentConfig>(jsonAsset.text);
            }

            if (config == null || config.tasks == null || config.tasks.Count == 0)
            {
                Debug.LogWarning("[InitialTestManager] Could not load AssessmentInstructions.json, using fallback configuration.");
                config = GetFallbackConfig();
            }
        }

        private AssessmentConfig GetFallbackConfig()
        {
            AssessmentConfig fallback = new AssessmentConfig();
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "mouse_move", title = "Move Cursor", instruction = "Move the cursor into the blue circle.", timeoutSeconds = 15f, shape = "circle" });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "hover", title = "Hover", instruction = "Keep the cursor inside the purple square for 2 seconds without clicking.", timeoutSeconds = 15f, shape = "square", requiredDuration = 2f });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "click", title = "Single Click", instruction = "Click the orange square once.", timeoutSeconds = 15f, shape = "square" });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "double_click", title = "Double Click", instruction = "Double-click quickly on the teal circle.", timeoutSeconds = 15f, shape = "circle" });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "right_click", title = "Right Click", instruction = "Click the amber rectangle using the right mouse button.", timeoutSeconds = 15f, shape = "rectangle" });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "click_hold", title = "Click and Hold", instruction = "Press and hold the mouse button on the red circle for 2 seconds.", timeoutSeconds = 20f, shape = "circle", requiredDuration = 2f });
            fallback.tasks.Add(new AssessmentTaskConfig { skillId = "drag_drop", title = "Drag and Drop", instruction = "Drag the blue square into the target frame.", timeoutSeconds = 20f, shape = "drag_drop" });
            return fallback;
        }
        #endregion

        #region UI Construction (Light Mode)
        private void BuildCleanUI()
        {
            if (taskContentArea == null)
            {
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas != null) taskContentArea = canvas.GetComponent<RectTransform>();
            }

            if (taskContentArea == null)
            {
                Debug.LogError("[InitialTestManager] Cannot build assessment UI: No taskContentArea or Canvas found.");
                return;
            }

            // Remove previous assessment instances if any
            if (rootContainer != null) Destroy(rootContainer);

            // Root Container (Light Mode: soft light gray #F1F5F9)
            rootContainer = new GameObject("AssessmentCleanRoot", typeof(RectTransform), typeof(Image));
            rootContainer.transform.SetParent(taskContentArea, false);
            RectTransform rootRect = rootContainer.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootContainer.GetComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f, 1f);

            // Top Header Panel (Pure white #FFFFFF)
            GameObject headerObj = new GameObject("HeaderPanel", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(rootContainer.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 110f);
            headerObj.GetComponent<Image>().color = Color.white;

            // Subtle divider under header
            GameObject dividerObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerObj.transform.SetParent(headerObj.transform, false);
            RectTransform divRect = dividerObj.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0f, 0f);
            divRect.anchorMax = new Vector2(1f, 0f);
            divRect.pivot = new Vector2(0.5f, 0f);
            divRect.sizeDelta = new Vector2(0f, 1f);
            dividerObj.GetComponent<Image>().color = new Color(0.88f, 0.90f, 0.93f, 1f);

            // Timeout Bar Background
            GameObject timerBg = new GameObject("TimerBg", typeof(RectTransform), typeof(Image));
            timerBg.transform.SetParent(headerObj.transform, false);
            RectTransform timerBgRect = timerBg.GetComponent<RectTransform>();
            timerBgRect.anchorMin = new Vector2(0f, 0f);
            timerBgRect.anchorMax = new Vector2(1f, 0f);
            timerBgRect.pivot = new Vector2(0.5f, 0f);
            timerBgRect.sizeDelta = new Vector2(0f, 4f);
            timerBg.GetComponent<Image>().color = new Color(0.89f, 0.91f, 0.94f, 1f);

            // Timeout Bar Fill
            GameObject timerFill = new GameObject("TimerFill", typeof(RectTransform), typeof(Image));
            timerFill.transform.SetParent(timerBg.transform, false);
            timerBarFill = timerFill.GetComponent<RectTransform>();
            timerBarFill.anchorMin = Vector2.zero;
            timerBarFill.anchorMax = Vector2.one;
            timerBarFill.pivot = new Vector2(0f, 0.5f);
            timerBarFill.offsetMin = Vector2.zero;
            timerBarFill.offsetMax = Vector2.zero;
            timerFill.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.95f, 1f);

            // Step Indicator Text (Slate gray #64748B)
            stepText = CreateText(headerObj.transform, "Step 1 of 7", 13, new Color(0.39f, 0.45f, 0.55f), TextAlignmentOptions.Left);
            RectTransform stepRect = stepText.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0f, 1f);
            stepRect.anchorMax = new Vector2(0.5f, 1f);
            stepRect.pivot = new Vector2(0f, 1f);
            stepRect.anchoredPosition = new Vector2(25f, -12f);
            stepRect.sizeDelta = new Vector2(300f, 20f);

            // Exit Button (Light button #F1F5F9 with dark slate text)
            CreateSimpleButton(headerObj.transform, config.ui.exitButton, new Vector2(20f, -10f), new Vector2(70f, 26f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Color(0.93f, 0.94f, 0.96f), new Color(0.28f, 0.34f, 0.44f), () => {
                    CalculateAndStoreResults();
                    FinishTest();
                });

            // Skip Button (Light button #E2E8F0 with slate text)
            CreateSimpleButton(headerObj.transform, config.ui.skipButton, new Vector2(100f, -10f), new Vector2(70f, 26f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Color(0.89f, 0.91f, 0.94f), new Color(0.20f, 0.25f, 0.33f), () => {
                    if (!isTaskResolving) HandleTaskSkip();
                });

            // Title Text (Deep dark slate #0F172A)
            titleText = CreateText(headerObj.transform, "Task Title", 22, new Color(0.06f, 0.09f, 0.16f), TextAlignmentOptions.Left);
            titleText.fontStyle = FontStyles.Bold;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(25f, -36f);
            titleRect.sizeDelta = new Vector2(600f, 30f);

            // Instruction Text (Dark slate #334155)
            instructionText = CreateText(headerObj.transform, "Task Instruction description goes here.", 15, new Color(0.20f, 0.25f, 0.33f), TextAlignmentOptions.Left);
            RectTransform instRect = instructionText.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(0f, 1f);
            instRect.anchorMax = new Vector2(1f, 1f);
            instRect.pivot = new Vector2(0f, 1f);
            instRect.anchoredPosition = new Vector2(25f, -68f);
            instRect.sizeDelta = new Vector2(700f, 30f);

            // Stage Area (Center Canvas for Shapes)
            GameObject stageObj = new GameObject("TaskStage", typeof(RectTransform));
            stageObj.transform.SetParent(rootContainer.transform, false);
            stageArea = stageObj.GetComponent<RectTransform>();
            stageArea.anchorMin = new Vector2(0f, 0f);
            stageArea.anchorMax = new Vector2(1f, 1f);
            stageArea.offsetMin = new Vector2(20f, 40f);
            stageArea.offsetMax = new Vector2(-20f, -120f);

            // Footer Status Text (Subtle Orange #EA580C)
            statusText = CreateText(rootContainer.transform, "", 14, new Color(0.92f, 0.35f, 0.05f), TextAlignmentOptions.Center);
            statusText.fontStyle = FontStyles.Bold;
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 10f);
            statusRect.sizeDelta = new Vector2(600f, 25f);
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int size, Color color, TextAlignmentOptions align)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }

        private GameObject CreateSimpleButton(Transform parent, string label, Vector2 offsetFromAnchor, Vector2 size, Vector2 anchor, Vector2 pivot, Color bgCol, Color txtCol, Action onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(-offsetFromAnchor.x, offsetFromAnchor.y);
            rt.sizeDelta = size;
            btnObj.GetComponent<Image>().color = bgCol;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            TextMeshProUGUI txt = CreateText(btnObj.transform, label, 12, txtCol, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btnObj;
        }
        #endregion

        #region Task Execution
        private void SpawnCurrentTask()
        {
            if (currentTaskIndex >= config.tasks.Count)
            {
                CalculateAndStoreResults();
                ShowCompletionScreen();
                return;
            }

            // Clean previous stage elements
            foreach (Transform child in stageArea)
            {
                Destroy(child.gameObject);
            }

            AssessmentTaskConfig task = config.tasks[currentTaskIndex];
            currentTaskTimer = 0f;
            currentTaskTimeout = task.timeoutSeconds > 0 ? task.timeoutSeconds : 15f;
            currentAttempts = 0;
            isTaskResolving = false;
            statusText.text = "";

            // Update UI headers
            stepText.text = string.Format(config.ui.stepFormat, currentTaskIndex + 1, config.tasks.Count);
            titleText.text = task.title;
            instructionText.text = task.instruction;

            // Spawn geometric interactive element
            SpawnGeometricShape(task);
        }

        private void SpawnGeometricShape(AssessmentTaskConfig task)
        {
            switch (task.skillId)
            {
                case "mouse_move":
                    CreateMouseMoveTarget();
                    break;
                case "hover":
                    CreateHoverTarget(task.requiredDuration > 0 ? task.requiredDuration : 2f);
                    break;
                case "click":
                    CreateClickTarget();
                    break;
                case "double_click":
                    CreateDoubleClickTarget();
                    break;
                case "right_click":
                    CreateRightClickTarget();
                    break;
                case "click_hold":
                    CreateClickHoldTarget(task.requiredDuration > 0 ? task.requiredDuration : 2f);
                    break;
                case "drag_drop":
                    CreateDragDropTarget();
                    break;
                default:
                    CreateClickTarget();
                    break;
            }
        }

        private void OnTaskSuccess()
        {
            if (isTaskResolving) return;
            isTaskResolving = true;

            AssessmentTaskConfig task = config.tasks[currentTaskIndex];
            RecordTaskResult(task.skillId, task.title, true, currentAttempts, currentTaskTimer);

            if (activeTaskRoutine != null) StopCoroutine(activeTaskRoutine);
            activeTaskRoutine = StartCoroutine(AdvanceAfterDelay(0.35f));
        }

        private void HandleTaskTimeout()
        {
            if (isTaskResolving) return;
            isTaskResolving = true;

            statusText.text = config.ui.timeExpired;
            AssessmentTaskConfig task = config.tasks[currentTaskIndex];
            RecordTaskResult(task.skillId, task.title, false, currentAttempts, currentTaskTimeout);

            if (activeTaskRoutine != null) StopCoroutine(activeTaskRoutine);
            activeTaskRoutine = StartCoroutine(AdvanceAfterDelay(0.7f));
        }

        private void HandleTaskSkip()
        {
            if (isTaskResolving) return;
            isTaskResolving = true;

            statusText.text = "Skipped";
            AssessmentTaskConfig task = config.tasks[currentTaskIndex];
            RecordTaskResult(task.skillId, task.title, false, currentAttempts, currentTaskTimer);

            if (activeTaskRoutine != null) StopCoroutine(activeTaskRoutine);
            activeTaskRoutine = StartCoroutine(AdvanceAfterDelay(0.2f));
        }

        private IEnumerator AdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentTaskIndex++;
            SpawnCurrentTask();
        }

        private void RecordTaskResult(string skillId, string title, bool success, int extraAttempts, float time)
        {
            float score = 0f;
            if (success)
            {
                // Accuracy factor: 10 points deducted per extra attempt / misclick
                float accuracyScore = Mathf.Clamp(100f - (extraAttempts * 10f), 20f, 100f);

                // Time factor:
                // If completed rapidly (<= 30% of allowed timeout): 0 penalty.
                // If taking longer, gently scale a penalty up to 35 points as time approaches timeout limit.
                float timeRatio = Mathf.Clamp01(time / currentTaskTimeout);
                float timePenalty = 0f;
                if (timeRatio > 0.30f)
                {
                    timePenalty = ((timeRatio - 0.30f) / 0.70f) * 35f;
                }

                score = Mathf.Clamp(accuracyScore - timePenalty, 20f, 100f);
            }

            recordedResults[skillId] = new SkillResultEntry
            {
                skillId = skillId,
                title = title,
                completed = success,
                extraAttempts = extraAttempts,
                timeTaken = time,
                finalScore = score
            };

            // Log diagnostic result into ProgressData
            if (ProgressData.Instance != null)
            {
                TaskResult tr = new TaskResult(
                    taskId: skillId,
                    skillId: skillId,
                    difficulty: 1,
                    success: success,
                    attempts: extraAttempts + 1,
                    completionTime: time,
                    scoreChange: score
                );
                tr.levelId = "assessment";
                tr.levelName = "Initial Assessment";
                ProgressData.Instance.RecordResult(tr);
            }
        }
        #endregion

        #region Geometric Shape Builders (Light Mode Palette)
        private void CreateMouseMoveTarget()
        {
            // Positioned on the right side of the screen
            GameObject circle = CreateShapeObject("Circle_Move", new Vector2(130f, 130f), new Color(0.15f, 0.39f, 0.92f), true, new Vector2(180f, 40f));
            EventTrigger trigger = circle.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((data) => {
                circle.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f); // Success Green
                OnTaskSuccess();
            });
            trigger.triggers.Add(entry);
        }

        private void CreateHoverTarget(float duration)
        {
            // Positioned on the opposite (left) side so the mouse never starts inside it!
            GameObject square = CreateShapeObject("Square_Hover", new Vector2(130f, 130f), new Color(0.49f, 0.23f, 0.93f), false, new Vector2(-180f, -40f));
            
            // Inner gauge fill
            GameObject fill = CreateShapeObject("Hover_Fill", new Vector2(130f, 130f), new Color(0.75f, 0.52f, 0.99f, 0.55f), false);
            fill.transform.SetParent(square.transform, false);
            fill.GetComponent<Image>().raycastTarget = false;
            fill.transform.localScale = Vector3.zero;

            HoverController hc = square.AddComponent<HoverController>();
            hc.duration = duration;
            hc.fillTransform = fill.transform;
            hc.onComplete = () => {
                square.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            };
            hc.onMisclick = () => currentAttempts++;
        }

        private void CreateClickTarget()
        {
            // Positioned on the bottom-right
            GameObject square = CreateShapeObject("Square_Click", new Vector2(130f, 130f), new Color(0.92f, 0.35f, 0.05f), false, new Vector2(140f, -60f));
            Button btn = square.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                square.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            });
        }

        private void CreateDoubleClickTarget()
        {
            // Positioned on the top-left
            GameObject circle = CreateShapeObject("Circle_DoubleClick", new Vector2(130f, 130f), new Color(0.05f, 0.58f, 0.53f), true, new Vector2(-120f, 70f));
            DoubleClickController dc = circle.AddComponent<DoubleClickController>();
            dc.onSuccess = () => {
                circle.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            };
            dc.onAttempt = () => currentAttempts++;
        }

        private void CreateRightClickTarget()
        {
            // Positioned on the top-right
            GameObject rect = CreateShapeObject("Rect_RightClick", new Vector2(180f, 110f), new Color(0.85f, 0.47f, 0.02f), false, new Vector2(160f, 60f));
            RightClickController rc = rect.AddComponent<RightClickController>();
            rc.onSuccess = () => {
                rect.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            };
            rc.onAttempt = () => currentAttempts++;
        }

        private void CreateClickHoldTarget(float duration)
        {
            // Positioned at the bottom-center
            GameObject circle = CreateShapeObject("Circle_ClickHold", new Vector2(130f, 130f), new Color(0.86f, 0.15f, 0.15f), true, new Vector2(0f, -40f));
            
            // Inner expanding fill
            GameObject fill = CreateShapeObject("Hold_Fill", new Vector2(130f, 130f), new Color(1f, 0.55f, 0.55f, 0.6f), true);
            fill.transform.SetParent(circle.transform, false);
            fill.GetComponent<Image>().raycastTarget = false;
            fill.transform.localScale = Vector3.zero;

            ClickHoldController chc = circle.AddComponent<ClickHoldController>();
            chc.duration = duration;
            chc.fillTransform = fill.transform;
            chc.onSuccess = () => {
                circle.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            };
            chc.onAttempt = () => currentAttempts++;
        }

        private void CreateDragDropTarget()
        {
            // Target slot on the right
            GameObject targetSlot = CreateShapeObject("Target_Slot", new Vector2(120f, 120f), new Color(0.92f, 0.94f, 0.96f), false, new Vector2(160f, 0f));
            RectTransform targetRect = targetSlot.GetComponent<RectTransform>();

            // Subtle border outline
            GameObject outline = new GameObject("Outline", typeof(RectTransform), typeof(Image));
            outline.transform.SetParent(targetSlot.transform, false);
            RectTransform outRect = outline.GetComponent<RectTransform>();
            outRect.anchorMin = Vector2.zero; outRect.anchorMax = Vector2.one;
            outRect.offsetMin = new Vector2(-4f, -4f); outRect.offsetMax = new Vector2(4f, 4f);
            outline.GetComponent<Image>().color = new Color(0.75f, 0.80f, 0.87f, 0.8f);
            outline.transform.SetAsFirstSibling();

            // Draggable square on the left (at -160, so user has to move all the way to grab it)
            GameObject dragSquare = CreateShapeObject("Square_Drag", new Vector2(100f, 100f), new Color(0.15f, 0.39f, 0.92f), false, new Vector2(-160f, 0f));
            RectTransform dragRect = dragSquare.GetComponent<RectTransform>();

            DragDropController ddc = dragSquare.AddComponent<DragDropController>();
            ddc.targetSlot = targetRect;
            ddc.startPosition = new Vector2(-160f, 0f);
            ddc.onSuccess = () => {
                dragSquare.GetComponent<Image>().color = new Color(0.09f, 0.64f, 0.29f);
                OnTaskSuccess();
            };
            ddc.onAttempt = () => currentAttempts++;
        }

        private GameObject CreateShapeObject(string name, Vector2 size, Color color, bool isCircle, Vector2? position = null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(stageArea, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = position ?? Vector2.zero;
            rt.sizeDelta = size;

            Image img = obj.GetComponent<Image>();
            img.color = color;

            if (isCircle)
            {
                img.sprite = GetCircleSprite();
                obj.AddComponent<CircleHitbox>();
            }

            return obj;
        }

        private static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = size / 2f;
            float radius = (size / 2f) - 2f;

            Color32[] cols = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius + 1.5f - dist);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(cols);
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCircleSprite;
        }
        #endregion

        #region Results, JSON Export & Completion
        private void CalculateAndStoreResults()
        {
            Debug.Log("[InitialTestManager] Calculating and persisting assessment results...");

            foreach (var kvp in recordedResults)
            {
                string skillId = kvp.Key;
                SkillResultEntry entry = kvp.Value;

                Debug.Log($"[InitialTestManager] Assessment Result: {entry.title} ({skillId}) -> Score: {entry.finalScore}% (Success: {entry.completed}, Extra Attempts: {entry.extraAttempts}, Time: {entry.timeTaken:F1}s)");

                if (SkillManager.Instance != null)
                {
                    SkillManager.Instance.SetScore(skillId, entry.finalScore);
                }
            }

            PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
            PlayerPrefs.Save();

            // Export results to Assets/Assessment/{HH-mm-ss}.json
            ExportEvaluationJson();
        }

        private void ExportEvaluationJson()
        {
            try
            {
                AssessmentExportReport report = new AssessmentExportReport
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    formattedTime = DateTime.Now.ToString("HH:mm:ss"),
                    totalTasksTested = recordedResults.Count,
                    completedTasks = 0,
                    averageScore = 0f
                };

                float totalScore = 0f;
                foreach (var kvp in recordedResults)
                {
                    SkillResultEntry entry = kvp.Value;
                    if (entry.completed) report.completedTasks++;
                    totalScore += entry.finalScore;

                    report.skills.Add(new AssessmentSkillExportItem
                    {
                        skillId = entry.skillId,
                        skillName = entry.title,
                        completed = entry.completed,
                        score = entry.finalScore,
                        extraAttempts = entry.extraAttempts,
                        timeTakenSeconds = (float)Math.Round(entry.timeTaken, 2)
                    });
                }

                if (report.totalTasksTested > 0)
                {
                    report.averageScore = (float)Math.Round(totalScore / report.totalTasksTested, 1);
                }

                string json = JsonUtility.ToJson(report, true);

                // Target folder: Assets/Assessment
                string folderPath = Path.Combine(Application.dataPath, "Assessment");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // File name is the current time
                string fileName = DateTime.Now.ToString("HH-mm-ss") + ".json";
                string fullPath = Path.Combine(folderPath, fileName);

                File.WriteAllText(fullPath, json);
                Debug.Log($"[InitialTestManager] Successfully exported assessment JSON to: {fullPath}");

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InitialTestManager] Failed to export assessment JSON: {ex.Message}");
            }
        }

        private void ShowCompletionScreen()
        {
            foreach (Transform child in rootContainer.transform)
            {
                Destroy(child.gameObject);
            }

            // Results Card (White card with subtle outline on light background)
            GameObject cardObj = new GameObject("ResultsCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(rootContainer.transform, false);
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.15f, 0.08f);
            cardRt.anchorMax = new Vector2(0.85f, 0.92f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            cardObj.GetComponent<Image>().color = Color.white;

            // Border outline around card
            GameObject cardBorder = new GameObject("CardBorder", typeof(RectTransform), typeof(Image));
            cardBorder.transform.SetParent(cardObj.transform, false);
            RectTransform borderRt = cardBorder.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1f, -1f); borderRt.offsetMax = new Vector2(1f, 1f);
            cardBorder.GetComponent<Image>().color = new Color(0.88f, 0.90f, 0.93f, 1f);
            cardBorder.transform.SetAsFirstSibling();

            // Title (Dark slate #0F172A)
            TextMeshProUGUI compTitle = CreateText(cardObj.transform, config.ui.completedTitle, 24, new Color(0.06f, 0.09f, 0.16f), TextAlignmentOptions.Center);
            compTitle.fontStyle = FontStyles.Bold;
            RectTransform titleRt = compTitle.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -25f);
            titleRt.sizeDelta = new Vector2(500f, 32f);

            // Subtitle (Slate gray #64748B)
            TextMeshProUGUI compSub = CreateText(cardObj.transform, config.ui.completedSubtitle, 14, new Color(0.39f, 0.45f, 0.55f), TextAlignmentOptions.Center);
            RectTransform subRt = compSub.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -60f);
            subRt.sizeDelta = new Vector2(500f, 24f);

            // Results Table Container
            GameObject tableObj = new GameObject("TableContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            tableObj.transform.SetParent(cardObj.transform, false);
            RectTransform tableRt = tableObj.GetComponent<RectTransform>();
            tableRt.anchorMin = new Vector2(0.08f, 0.2f);
            tableRt.anchorMax = new Vector2(0.92f, 0.82f);
            tableRt.offsetMin = Vector2.zero;
            tableRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = tableObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            int rowIndex = 0;
            foreach (var task in config.tasks)
            {
                GameObject rowObj = new GameObject("Row_" + task.skillId, typeof(RectTransform), typeof(Image));
                rowObj.transform.SetParent(tableObj.transform, false);
                rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 28f);
                // Alternating row background for clean readability
                rowObj.GetComponent<Image>().color = (rowIndex % 2 == 0)
                    ? new Color(0.96f, 0.97f, 0.98f, 1f)
                    : new Color(0.92f, 0.94f, 0.96f, 1f);
                rowIndex++;

                string statusStr;
                Color statusCol;

                if (recordedResults.TryGetValue(task.skillId, out var res))
                {
                    if (res.completed)
                    {
                        statusStr = $"{res.finalScore:0}% ({res.timeTaken:F1}s)";
                        statusCol = new Color(0.09f, 0.64f, 0.29f); // Green
                    }
                    else
                    {
                        statusStr = "0% (Timed out)";
                        statusCol = new Color(0.86f, 0.15f, 0.15f); // Red
                    }
                }
                else
                {
                    statusStr = "Not tested";
                    statusCol = new Color(0.55f, 0.60f, 0.68f);
                }

                // Row Label (Dark text #1E293B)
                TextMeshProUGUI labelTxt = CreateText(rowObj.transform, task.title, 14, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.Left);
                RectTransform lblRt = labelTxt.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0f, 0f); lblRt.anchorMax = new Vector2(0.6f, 1f);
                lblRt.offsetMin = new Vector2(15f, 0f); lblRt.offsetMax = Vector2.zero;

                // Row Score
                TextMeshProUGUI valTxt = CreateText(rowObj.transform, statusStr, 14, statusCol, TextAlignmentOptions.Right);
                valTxt.fontStyle = FontStyles.Bold;
                RectTransform valRt = valTxt.GetComponent<RectTransform>();
                valRt.anchorMin = new Vector2(0.6f, 0f); valRt.anchorMax = new Vector2(1f, 1f);
                valRt.offsetMin = Vector2.zero; valRt.offsetMax = new Vector2(-15f, 0f);
            }

            // Finish Button (Primary Blue #2563EB with white text)
            CreateSimpleButton(cardObj.transform, config.ui.closeButton, new Vector2(0f, -20f), new Vector2(140f, 38f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.15f, 0.39f, 0.92f), Color.white, () => {
                    if (appWindow != null) appWindow.CloseWindow();
                    FinishTest();
                });
        }

        private void FinishTest()
        {
            IsTestActive = false;
            Debug.Log("[InitialTestManager] Assessment completed.");
            OnTestCompleted?.Invoke();
        }
        #endregion
    }

    #region Interactive Helper Controllers
    internal class HoverController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public float duration = 2f;
        public Transform fillTransform;
        public Action onComplete;
        public Action onMisclick;

        private bool isHovering = false;
        private float hoverTime = 0f;
        private bool isDone = false;
        private float armingDelay = 0.35f; // Wait 0.35s after creation to avoid instant hover if mouse was nearby

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDone) return;
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDone) return;
            isHovering = false;
            hoverTime = 0f;
            if (fillTransform != null) fillTransform.localScale = Vector3.zero;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDone) return;
            onMisclick?.Invoke();
        }

        private void Update()
        {
            if (isDone) return;

            // Arming delay: do not charge until the shape has been visible for at least 0.35s
            if (armingDelay > 0f)
            {
                armingDelay -= Time.deltaTime;
                return;
            }

            if (!isHovering) return;

            hoverTime += Time.deltaTime;
            if (fillTransform != null)
            {
                float t = Mathf.Clamp01(hoverTime / duration);
                fillTransform.localScale = Vector3.one * t;
            }

            if (hoverTime >= duration)
            {
                isDone = true;
                onComplete?.Invoke();
            }
        }
    }

    internal class DoubleClickController : MonoBehaviour, IPointerClickHandler
    {
        public Action onSuccess;
        public Action onAttempt;

        private float lastClickTime = -1f;
        private bool isDone = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;

            float currentTime = Time.unscaledTime;

            // If a previous click occurred within the double-click window (0.5s), it's a success!
            if (lastClickTime > 0f && (currentTime - lastClickTime) <= 0.50f)
            {
                isDone = true;
                onSuccess?.Invoke();
            }
            else
            {
                // If there was already a click and they waited too long before clicking again,
                // that counts as a missed/slow attempt. The very first click is NOT penalized!
                if (lastClickTime > 0f)
                {
                    onAttempt?.Invoke();
                }

                lastClickTime = currentTime;
            }
        }
    }

    internal class RightClickController : MonoBehaviour, IPointerClickHandler
    {
        public Action onSuccess;
        public Action onAttempt;

        private bool isDone = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDone) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                isDone = true;
                onSuccess?.Invoke();
            }
            else
            {
                onAttempt?.Invoke();
            }
        }
    }

    internal class ClickHoldController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float duration = 2f;
        public Transform fillTransform;
        public Action onSuccess;
        public Action onAttempt;

        private bool isHolding = false;
        private float holdTime = 0f;
        private bool isDone = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isDone || eventData.button != PointerEventData.InputButton.Left) return;
            isHolding = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDone) return;
            if (isHolding && holdTime < duration)
            {
                onAttempt?.Invoke();
            }
            isHolding = false;
            holdTime = 0f;
            if (fillTransform != null) fillTransform.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (isDone || !isHolding) return;

            holdTime += Time.deltaTime;
            if (fillTransform != null)
            {
                float t = Mathf.Clamp01(holdTime / duration);
                fillTransform.localScale = Vector3.one * t;
            }

            if (holdTime >= duration)
            {
                isDone = true;
                onSuccess?.Invoke();
            }
        }
    }

    internal class DragDropController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform targetSlot;
        public Vector2 startPosition;
        public Action onSuccess;
        public Action onAttempt;

        private RectTransform rectTransform;
        private RectTransform parentRect;
        private bool isDone = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDone) return;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDone) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                rectTransform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDone) return;

            bool isInside = targetSlot != null && RectTransformUtility.RectangleContainsScreenPoint(targetSlot, eventData.position, eventData.pressEventCamera);

            if (isInside)
            {
                isDone = true;
                rectTransform.anchoredPosition = targetSlot.anchoredPosition;
                onSuccess?.Invoke();
            }
            else
            {
                rectTransform.anchoredPosition = startPosition;
                onAttempt?.Invoke();
            }
        }
    }
    #endregion
}
