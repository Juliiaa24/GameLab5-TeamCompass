/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Play-mode integration checks through the real Input System and UI.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ComputerLearning.EditorTools
{
    [InitializeOnLoad]
    public static class DesktopIntegrationValidation
    {
        private static string OutputDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Temp", "DesktopIntegrationValidation");
        private const string RunningKey = "ComputerLearning.IntegrationValidation";
        private static IEnumerator scenario;
        private static double nextStep;
        private static Mouse mouse;
        private static readonly List<string> passed = new List<string>();
        private static readonly List<string> errors = new List<string>();
        private static InputSettings previousInputSettings;
        private static readonly List<string> editorDiagnostics = new List<string>();

        static DesktopIntegrationValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run validation in an isolated Unity batch-mode project.");
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before validation.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene changes before validation.");
            // Enter Play mode against the integrated build scene.
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            Directory.CreateDirectory(OutputDirectory);
            SessionState.SetBool(RunningKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            passed.Clear(); errors.Clear(); editorDiagnostics.Clear();
            previousInputSettings = InputSystem.settings;
            InputSystem.settings = Object.Instantiate(previousInputSettings);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            mouse = InputSystem.AddDevice<Mouse>("IntegrationTestMouse");
            scenario = CheckScenario();
            nextStep = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextStep) return;
            try
            {
                if (scenario.MoveNext())
                {
                    nextStep = EditorApplication.timeSinceStartup + Convert.ToDouble(scenario.Current);
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                else Finish(null);
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        private static IEnumerator CheckScenario()
        {
            SetGameViewSize(1280, 720);
            yield return 0.6f;
            WindowManager manager = WindowManager.Instance;
            var taskbar = Object.FindAnyObjectByType<Taskbar>();
            Require(manager != null && taskbar != null, "Scene contains WindowManager and Taskbar.");
            Require(TaskManager.Instance != null && SkillManager.Instance != null && LevelManager.Instance != null,
                "Existing task, skill and level managers are configured.");
            Require(Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null,
                "UI uses InputSystemUIInputModule.");
            var icons = Object.FindObjectsByType<DraggableIcon>();
            var training = icons.Single(i => i.name == "ClickTrainingIcon");
            var other = icons.First(i => i != training);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory, "integration-start.png"));
            yield return 0.6f;
            foreach (float delay in DoubleClick(training.GetComponent<RectTransform>())) yield return delay;
            Window first = training.AppWindow;
            Require(first != null && manager.Windows.Count == 1 && taskbar.EntryCount == 1,
                "Double-clicking the desktop icon creates a real window and one taskbar entry.");
            Require(first.IsFocused, "Opened window receives focus.");
            var task = first.GetComponentInChildren<TargetTask>();
            var level = first.GetComponentInChildren<TrainingLevel>();
            var target = first.GetComponentInChildren<ClickTarget>();
            Require(task != null && task.IsRunning && task.CurrentProgress == 0 && level.IsRunning,
                "Opening training starts its Level and assigned Task.");

            yield return 0.4f;
            foreach (float delay in DoubleClick(training.GetComponent<RectTransform>())) yield return delay;
            Require(training.AppWindow == first && manager.Windows.Count == 1, "Reopening an icon reuses its original window.");
            foreach (float delay in DoubleClick(other.GetComponent<RectTransform>())) yield return delay;
            Window second = other.AppWindow;
            Require(second != null && manager.Windows.Count == 2 && taskbar.EntryCount == 2 && second.IsFocused,
                "Another icon opens another instance with independent focus.");
            var firstButton = Object.FindObjectsByType<TaskbarWindowButton>().Single(b => b.TargetWindow == first);
            foreach (float delay in Click(firstButton.GetComponent<RectTransform>())) yield return delay;
            Require(first.IsFocused && !first.IsMinimized, "Taskbar brings a background window forward.");
            foreach (float delay in Click(firstButton.GetComponent<RectTransform>())) yield return delay;
            Require(first.IsMinimized && second.IsFocused && taskbar.EntryCount == 2, "Active taskbar entry minimizes and selects the next visible window.");
            foreach (float delay in Click(firstButton.GetComponent<RectTransform>())) yield return delay;
            Require(!first.IsMinimized && first.IsFocused && task.CurrentProgress == 0, "Taskbar restores focus and preserves the exercise.");

            RectTransform firstRect = first.GetComponent<RectTransform>();
            Vector2 oldPosition = firstRect.anchoredPosition, oldSize = firstRect.sizeDelta;
            foreach (float delay in Click(first.transform.Find("WindowTop/Buttons/Maxmize") as RectTransform)) yield return delay;
            Canvas.ForceUpdateCanvases();
            Require(first.IsMaximized && Near(firstRect.rect.size, manager.WindowsContainer.rect.size),
                "Maximize uses the desktop working area.");
            var barRect = taskbar.GetComponent<RectTransform>();
            var corners = new Vector3[4]; var areaCorners = new Vector3[4];
            barRect.GetWorldCorners(corners); firstRect.GetWorldCorners(areaCorners);
            Require(areaCorners[0].y >= corners[1].y - 1f, "Maximized window stays above the taskbar.");
            foreach (float delay in Click(first.transform.Find("WindowTop/Buttons/Maxmize") as RectTransform)) yield return delay;
            Require(!first.IsMaximized && Near(firstRect.anchoredPosition, oldPosition) && Near(firstRect.sizeDelta, oldSize),
                "Restore recovers the exact previous window geometry.");

            foreach (Vector2Int resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 1024), new Vector2Int(2560, 1080) })
            {
                SetGameViewSize(resolution.x, resolution.y);
                yield return 0.6f;
                Canvas.ForceUpdateCanvases();
                first.toggleMaximize();
                Canvas.ForceUpdateCanvases();
                Require(Screen.width == resolution.x && Screen.height == resolution.y && Near(firstRect.rect.size, manager.WindowsContainer.rect.size) && manager.WindowsContainer.offsetMin.y == barRect.rect.height,
                    "Working area and maximize adapt at requested resolution " + resolution + " (actual " + Screen.width + "x" + Screen.height + ").");
                first.toggleMaximize();
            }
            SetGameViewSize(1280, 720);
            yield return 0.6f;
            Canvas.ForceUpdateCanvases();

            var titleBar = first.transform.Find("WindowTop") as RectTransform;
            Vector2 titlePoint = ScreenPoint(titleBar, new Vector2(0.25f, 0.5f));
            Vector2 initial = firstRect.anchoredPosition;
            foreach (float delay in Drag(titlePoint, titlePoint + new Vector2(70, -30))) yield return delay;
            float scale = first.GetComponentInParent<Canvas>().scaleFactor;
            Require(Near(firstRect.anchoredPosition - initial, new Vector2(70, -30) / scale, 3f),
                "Title-bar drag follows pointer coordinates through CanvasScaler.");
            var handle = first.GetComponentsInChildren<ResizeHandle>().First(h => new SerializedObject(h).FindProperty("direction").intValue == 10);
            Vector2 resizePoint = ScreenPoint(handle.GetComponent<RectTransform>());
            Vector2 initialSize = firstRect.rect.size;
            foreach (float delay in Drag(resizePoint, resizePoint + new Vector2(35, -25))) yield return delay;
            Require(Near(firstRect.rect.size - initialSize, new Vector2(35, 25) / scale, 3f),
                "Corner resize still follows Canvas-scaled pointer movement.");

            int skillBefore = SkillManager.Instance.GetSkill(SkillID.CLICK).CompletedTasks;
            int completedTasks = 0, completedLevels = 0;
            TaskManager.Instance.TaskCompleted += _ => completedTasks++;
            LevelManager.Instance.LevelCompleted += _ => completedLevels++;
            foreach (float delay in Click(target.GetComponent<RectTransform>(), PointerEventData.InputButton.Right)) yield return delay;
            Require(task.CurrentProgress == 0, "Right-clicking a target does not progress the task.");
            Vector2 backgroundPoint = ScreenPoint(first.transform.Find("WindowContents/ClickTargetExercise(Clone)/Heading") as RectTransform);
            foreach (float delay in ClickAt(backgroundPoint)) yield return delay;
            Require(task.CurrentProgress == 0, "Clicking exercise background does not count as a target.");
            foreach (float delay in Click(target.GetComponent<RectTransform>())) yield return delay;
            Require(task.CurrentProgress == 1, "A real UI left click increments task progress.");
            target.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Require(task.CurrentProgress == 1, "The same target cannot count twice during its transition.");
            first.Minimize(); yield return 0.1f; first.Restore(); yield return 0.25f;
            Require(task.CurrentProgress == 1 && task.IsRunning, "Minimize and restore preserve task progress.");
            while (!task.IsCompleted())
            {
                int previous = task.CurrentProgress;
                foreach (float delay in Click(target.GetComponent<RectTransform>())) yield return delay;
                Require(task.CurrentProgress == previous + 1, "Target hit " + task.CurrentProgress + " is counted once.");
                yield return 0.22f;
            }
            Require(completedTasks == 1 && completedLevels == 1 && level.IsCompleted() &&
                LevelManager.Instance.HasCompletedLevel(level.ID) && SkillManager.Instance.GetSkill(SkillID.CLICK).CompletedTasks == skillBefore + 1,
                "Task completes once, credits CLICK skill once, then completes its Level.");
            Require(!target.gameObject.activeSelf && !task.RegisterHit(), "Completed exercise rejects additional hits.");

            var restart = first.GetComponentsInChildren<Button>().Single(b => b.name == "Restart");
            foreach (float delay in Click(restart.GetComponent<RectTransform>())) yield return delay;
            Require(task.IsRunning && task.CurrentProgress == 0 && !level.IsCompleted(), "Replay resets the level and task without recreating the window.");
            var taskSettings = new SerializedObject(task);
            taskSettings.FindProperty("requiredTargets").intValue = 2; taskSettings.ApplyModifiedPropertiesWithoutUndo();
            level.StartExercise();
            for (int i = 0; i < 2; i++)
            {
                foreach (float delay in Click(target.GetComponent<RectTransform>())) yield return delay;
                yield return 0.22f;
            }
            Require(task.IsCompleted() && completedTasks == 2 && completedLevels == 2 &&
                SkillManager.Instance.GetSkill(SkillID.CLICK).CompletedTasks == skillBefore + 2,
                "A second attempt supports a different target count and credits one new result.");
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory, "integration-complete.png"));
            yield return 0.3f;

            foreach (float delay in Click(first.transform.Find("WindowTop/Buttons/Close") as RectTransform)) yield return delay;
            Require(first == null && taskbar.EntryCount == 1 && manager.Windows.Count == 1 && second.IsFocused,
                "Close control destroys the window, removes its taskbar entry and returns focus.");
            yield return 0.4f;
            foreach (float delay in DoubleClick(training.GetComponent<RectTransform>())) yield return delay;
            first = training.AppWindow;
            yield return 0.2f;
            Require(first != null && first.GetComponentInChildren<TargetTask>().CurrentProgress == 0 &&
                taskbar.EntryCount == 2, "Closed training can reopen with a fresh task and existing skill history.");

            second.BringToFront(); yield return 0.1f;
            foreach (float delay in Click(second.transform.Find("WindowTop/Buttons/Minimize") as RectTransform)) yield return delay;
            Require(second.IsMinimized && first.IsFocused, "Existing window prefabs have functioning minimize controls.");
            second.Restore();
            Object.Destroy(second.gameObject); yield return 0.15f;
            Require(manager.Windows.Count == 1 && taskbar.EntryCount == 1, "External window destruction also unregisters its entry.");
            first.CloseWindow(); yield return 0.15f;
            Require(manager.Windows.Count == 0 && taskbar.EntryCount == 0 && manager.ActiveWindow == null,
                "Closing all windows leaves no stale entries or focus.");

            foreach (string sceneName in new[] { "DiegoScene", "JuliaScene", "MainScene" })
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/" + sceneName + ".unity");
                EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GetAssetPath(sceneAsset),
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return 0.7f;
                Require(Object.FindObjectsByType<TaskManager>().Length == 1 &&
                    Object.FindObjectsByType<SkillManager>().Length == 1 && Object.FindObjectsByType<LevelManager>().Length == 1,
                    sceneName + " has one persistent set of progression managers.");
                manager = WindowManager.Instance; taskbar = Object.FindAnyObjectByType<Taskbar>();
                training = Object.FindObjectsByType<DraggableIcon>().Single(i => i.name == "ClickTrainingIcon");
                training.OpenApplication(); yield return 0.2f;
                Require(manager.Windows.Count == 1 && taskbar.EntryCount == 1 &&
                    training.AppWindow.GetComponentInChildren<TargetTask>().IsRunning, sceneName + " opens training with connected taskbar and progression.");
            }
            Require(errors.Count == 0, "No runtime errors or exceptions during integration checks.");
        }


        private static object sizeGroup;
        private static EditorWindow gameView;
        private static int originalSizeIndex = -1;
        private static int addedSizeCount;
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static void SetGameViewSize(int width, int height)
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            sizeGroup = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type kindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            object size = Activator.CreateInstance(sizeType, Flags, null,
                new[] { Enum.Parse(kindType, "FixedResolution"), (object)width, height, "Integration validation" }, null);
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
            addedSizeCount++;
            int index = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null) - 1;
            Type viewType = editorAssembly.GetType("UnityEditor.GameView");
            gameView = EditorWindow.GetWindow(viewType);
            PropertyInfo selectedSize = viewType.GetProperty("selectedSizeIndex", Flags);
            if (originalSizeIndex < 0) originalSizeIndex = (int)selectedSize.GetValue(gameView);
            selectedSize.SetValue(gameView, index);
            gameView.Repaint();
        }

        private static void RestoreGameView()
        {
            if (gameView != null && originalSizeIndex >= 0)
                gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, originalSizeIndex);
            while (sizeGroup != null && addedSizeCount-- > 0)
            {
                int count = (int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null);
                sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { count - 1 });
            }
        }

        private static IEnumerable<float> DoubleClick(RectTransform rect)
        {
            foreach (float delay in Click(rect)) yield return delay;
            foreach (float delay in Click(rect)) yield return delay;
        }

        private static IEnumerable<float> Click(RectTransform rect, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            if (rect == null) throw new Exception("Missing clickable RectTransform.");
            foreach (float delay in ClickAt(ScreenPoint(rect), button)) yield return delay;
        }

        private static IEnumerable<float> ClickAt(Vector2 point, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return 0.03f;
            var state = new MouseState { position = point };
            state = state.WithButton(button == PointerEventData.InputButton.Right ? MouseButton.Right : MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state);
            yield return 0.03f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return 0.03f;
        }

        private static IEnumerable<float> Drag(Vector2 from, Vector2 to)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from });
            yield return 0.05f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left));
            yield return 0.05f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = Vector2.Lerp(from, to, 0.5f) }.WithButton(MouseButton.Left));
            yield return 0.05f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = to }.WithButton(MouseButton.Left));
            yield return 0.05f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = to });
            yield return 0.05f;
        }

        private static Vector2 ScreenPoint(RectTransform rect, Vector2? normalized = null)
        {
            Vector2 t = normalized ?? new Vector2(0.5f, 0.5f);
            Vector2 local = rect.rect.min + Vector2.Scale(rect.rect.size, t);
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
        }

        private static bool Near(Vector2 a, Vector2 b, float tolerance = 1f) { return Vector2.Distance(a, b) <= tolerance; }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("FAILED: " + message);
            passed.Add(message);
            Debug.Log("PASS: " + message);
        }

        private static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (trace.Contains("UnityEditor.Search.")) editorDiagnostics.Add(message + "\n" + trace);
            else errors.Add(message + "\n" + trace);
        }

        [Serializable] private class Report { public bool success; public string failure; public string[] checks; public string[] errors; public string[] editorDiagnostics; }

        private static void Finish(string failure)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(RunningKey, false);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (previousInputSettings != null) InputSystem.settings = previousInputSettings;
            RestoreGameView();
            bool success = failure == null && errors.Count == 0;
            File.WriteAllText(Path.Combine(OutputDirectory, "IntegrationValidation.json"), JsonUtility.ToJson(new Report
                { success = success, failure = failure, checks = passed.ToArray(), errors = errors.ToArray(), editorDiagnostics = editorDiagnostics.ToArray() }, true));
            if (success) Debug.Log("DESKTOP_INTEGRATION_VALIDATION_OK: " + passed.Count + " checks.");
            else Debug.LogError("DESKTOP_INTEGRATION_VALIDATION_FAILED: " + failure);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
