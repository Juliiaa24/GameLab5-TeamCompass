/**
 * Author: Diego & TeamCompass
 * Date: 23/11/26
 * Description: First-time and on-demand guided tutorial using Pointy (VirtualMascot) and TutorialBlocker
 *              for the Teacher Control Window (7 steps) and Classroom Dashboard (4 steps),
 *              fully localized across English (EN), Spanish (ES), and Norwegian (NO).
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class TeacherModeTutorial : MonoBehaviour
    {
        #region Public Variables
        public const string ControlPanelTourPrefKey = "TeacherMode_PointyTutorialCompleted_v1";
        public const string DashboardTourPrefKey = "TeacherDashboard_PointyTutorialCompleted_v1";

        public static TeacherModeTutorial Instance { get; private set; }
        public static bool IsTourRunning { get; private set; }
        #endregion

        #region Private Variables
        private enum ActiveTourType
        {
            None = 0,
            ControlPanel = 1,
            Dashboard = 2
        }

        private ActiveTourType activeTourType = ActiveTourType.None;
        private Coroutine activeTourCoroutine;
        private TutorialBlocker tutorialBlocker;
        private Transform monitoredModalRoot;
        private Func<UIDialogs, string> currentStepMessageResolver;
        private RectTransform currentStepTarget;
        private Vector2 currentStepOffset;
        private int escapeConsumedFrame = -1;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (IsTourRunning)
            {
                StopActiveTour(markCompleted: false);
            }
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            CleanupBlocker();
            if (Instance == this)
            {
                Instance = null;
                IsTourRunning = false;
            }
        }

        private void Update()
        {
            if (!IsTourRunning) return;

            if (monitoredModalRoot == null || !monitoredModalRoot.gameObject.activeInHierarchy)
            {
                StopActiveTour(markCompleted: false);
                return;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    escapeConsumedFrame = Time.frameCount;
                    StopActiveTour(markCompleted: true);
                    return;
                }

                if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) &&
                    VirtualMascot.AllowSpaceToSkipDialogue &&
                    VirtualMascot.Instance != null)
                {
                    VirtualMascot.Instance.DismissActiveFeedback();
                }
            }
        }
        #endregion

        #region Public Methods
        public static bool WasEscapeConsumedThisFrame =>
            Instance != null && Instance.escapeConsumedFrame == Time.frameCount;

        public static TeacherModeTutorial EnsureAttached(Component hostComponent)
        {
            if (Instance != null) return Instance;

            Instance = FindAnyObjectByType<TeacherModeTutorial>(FindObjectsInactive.Include);
            if (Instance != null) return Instance;

            Canvas canvas = hostComponent != null ? hostComponent.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            GameObject attachTarget = canvas != null ? canvas.gameObject : (hostComponent != null ? hostComponent.gameObject : new GameObject("TeacherModeTutorial"));
            Instance = attachTarget.AddComponent<TeacherModeTutorial>();
            return Instance;
        }

        public static void TryStartControlPanelTourOnFirstOpen(TeacherModeGate gate)
        {
            if (gate == null) return;
            if (PlayerPrefs.GetInt(ControlPanelTourPrefKey, 0) == 1) return;
            StartControlPanelTour(gate, forceReplay: false);
        }

        public static void StartControlPanelTour(TeacherModeGate gate, bool forceReplay = true)
        {
            if (gate == null || !gate.IsTeacherPanelOpen) return;
            if (!forceReplay && PlayerPrefs.GetInt(ControlPanelTourPrefKey, 0) == 1) return;

            TeacherModeTutorial tutorial = EnsureAttached(gate);
            tutorial.BeginControlPanelTour(gate);
        }

        public static void TryStartDashboardTourOnFirstOpen(TeacherDashboardController dashboard)
        {
            if (dashboard == null) return;
            if (PlayerPrefs.GetInt(DashboardTourPrefKey, 0) == 1) return;
            StartDashboardTour(dashboard, forceReplay: false);
        }

        public static void StartDashboardTour(TeacherDashboardController dashboard, bool forceReplay = true)
        {
            if (dashboard == null || !dashboard.IsOpen) return;
            if (!forceReplay && PlayerPrefs.GetInt(DashboardTourPrefKey, 0) == 1) return;

            TeacherModeTutorial tutorial = EnsureAttached(dashboard);
            tutorial.BeginDashboardTour(dashboard);
        }

        public static void StopIfRunning(bool markCompleted = false)
        {
            if (Instance != null && IsTourRunning)
            {
                Instance.StopActiveTour(markCompleted);
            }
        }
        #endregion

        #region Private Methods
        private void BeginControlPanelTour(TeacherModeGate gate)
        {
            if (IsTourRunning)
            {
                StopActiveTour(markCompleted: false);
            }

            Transform panelRoot = gate != null ? gate.TeacherPanelRootTransform : null;
            if (panelRoot == null || !panelRoot.gameObject.activeInHierarchy) return;

            activeTourType = ActiveTourType.ControlPanel;
            monitoredModalRoot = panelRoot;
            IsTourRunning = true;
            SetupBlocker(panelRoot);
            activeTourCoroutine = StartCoroutine(RunControlPanelTourCoroutine(panelRoot));
        }

        private void BeginDashboardTour(TeacherDashboardController dashboard)
        {
            if (IsTourRunning)
            {
                bool wasControlPanelTour = activeTourType == ActiveTourType.ControlPanel;
                StopActiveTour(markCompleted: wasControlPanelTour);
            }

            Transform dashRoot = dashboard != null ? dashboard.transform : null;
            if (dashRoot == null || !dashRoot.gameObject.activeInHierarchy) return;

            activeTourType = ActiveTourType.Dashboard;
            monitoredModalRoot = dashRoot;
            IsTourRunning = true;
            SetupBlocker(dashRoot);
            activeTourCoroutine = StartCoroutine(RunDashboardTourCoroutine(dashRoot));
        }

        private void SetupBlocker(Transform referenceTransform)
        {
            if (TutorialBlocker.Instance != null)
            {
                tutorialBlocker = TutorialBlocker.Instance;
            }
            else
            {
                GameObject blockerObj = new GameObject("TutorialBlocker");
                Canvas rootCanvas = referenceTransform != null ? referenceTransform.GetComponentInParent<Canvas>() : null;
                if (rootCanvas != null)
                {
                    Canvas topCanvas = rootCanvas.isRootCanvas ? rootCanvas : rootCanvas.rootCanvas;
                    blockerObj.transform.SetParent(topCanvas.transform, false);
                }
                tutorialBlocker = blockerObj.AddComponent<TutorialBlocker>();
            }

            tutorialBlocker.gameObject.SetActive(true);
            tutorialBlocker.SetExcludedTarget(null);
            tutorialBlocker.SetAllowedTarget(null);
        }

        private void CleanupBlocker()
        {
            if (tutorialBlocker != null)
            {
                tutorialBlocker.ClearTarget();
                Destroy(tutorialBlocker.gameObject);
                tutorialBlocker = null;
            }
        }

        private IEnumerator RunControlPanelTourCoroutine(Transform panelRoot)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform winTr = panelRoot.Find("TeacherControlWindow") ?? panelRoot;
            RectTransform winRt = winTr as RectTransform;

            RectTransform titleTarget = FindRect(winTr, "WindowTitleBar/WindowTitleText")
                                        ?? FindRect(winTr, "WindowTitleBar")
                                        ?? winRt;
            RectTransform pathsTarget = FindRect(winTr, "LeftColumn_LearningPath/Btn-Path1")
                                        ?? FindRect(winTr, "LeftColumn_LearningPath/PathSectionTitle")
                                        ?? FindRect(winTr, "LeftColumn_LearningPath")
                                        ?? winRt;
            RectTransform assessTarget = FindRect(winTr, "LeftColumn_LearningPath/Btn-ToggleAssessment")
                                         ?? pathsTarget;
            RectTransform resetTarget = FindRect(winTr, "LeftColumn_LearningPath/Btn-ResetProgress")
                                        ?? pathsTarget;
            RectTransform exportTarget = FindRect(winTr, "RightColumn_ClassroomAndTools/Btn-ExportPlaytest")
                                         ?? FindRect(winTr, "RightColumn_ClassroomAndTools/SectionStudentToolsLabel")
                                         ?? FindRect(winTr, "RightColumn_ClassroomAndTools")
                                         ?? winRt;
            RectTransform classroomTarget = FindRect(winTr, "RightColumn_ClassroomAndTools/Btn-OpenTeacherDashboard")
                                            ?? FindRect(winTr, "RightColumn_ClassroomAndTools/SectionClassLabel")
                                            ?? exportTarget;
            RectTransform pinAndLockTarget = FindRect(winTr, "Btn-LockTeacherMode")
                                             ?? FindRect(winTr, "RightColumn_ClassroomAndTools/SectionPinLabel")
                                             ?? winRt;

            // Step 1: Welcome to Teacher Control Window
            yield return ShowTourStep(ui => ui.teacher_tut_welcome, titleTarget, new Vector2(0f, -20f), 6.5f);
            if (!IsTourRunning) yield break;

            // Step 2: 3 Learning Paths (Beginner, Intermediate, Advanced)
            yield return ShowTourStep(ui => ui.teacher_tut_paths, pathsTarget, new Vector2(185f, 0f), 7.5f);
            if (!IsTourRunning) yield break;

            // Step 3: Independent Initial Assessment ON/OFF Toggle
            yield return ShowTourStep(ui => ui.teacher_tut_assessment, assessTarget, new Vector2(185f, 0f), 6.8f);
            if (!IsTourRunning) yield break;

            // Step 4: Reset Progress & Automatic Next Student ID Progression
            yield return ShowTourStep(ui => ui.teacher_tut_reset, resetTarget, new Vector2(185f, 15f), 7.2f);
            if (!IsTourRunning) yield break;

            // Step 5: Student ID, Data Export & Auto Download
            yield return ShowTourStep(ui => ui.teacher_tut_export, exportTarget, new Vector2(-45f, -10f), 6.8f);
            if (!IsTourRunning) yield break;

            // Step 6: Custom Class Name & Open Teacher Dashboard (LAN Host)
            yield return ShowTourStep(ui => ui.teacher_tut_classroom, classroomTarget, new Vector2(-140f, -10f), 6.8f);
            if (!IsTourRunning) yield break;

            // Step 7: Custom 4-Digit Teacher PIN & Lock Back to Student Mode
            yield return ShowTourStep(ui => ui.teacher_tut_pin_and_lock, pinAndLockTarget, new Vector2(150f, 18f), 6.8f);
            if (!IsTourRunning) yield break;

            CompleteActiveTour();
        }

        private IEnumerator RunDashboardTourCoroutine(Transform dashRoot)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform winTr = dashRoot.Find("DashboardWindow") ?? dashRoot;
            RectTransform winRt = winTr as RectTransform;

            RectTransform serverTarget = FindRect(winTr, "WindowTitleBar/Btn-ToggleServer")
                                         ?? FindRect(winTr, "WindowTitleBar/ServerStatusText")
                                         ?? FindRect(winTr, "WindowTitleBar")
                                         ?? winRt;
            RectTransform batchTarget = FindRect(winTr, "BatchControlsBar/Btn-ApplyAll")
                                        ?? FindRect(winTr, "BatchControlsBar")
                                        ?? winRt;
            RectTransform permissionsTarget = FindRect(winTr, "PermissionsAndNameBar/Btn-LockStudentNames")
                                              ?? FindRect(winTr, "PermissionsAndNameBar")
                                              ?? winRt;
            RectTransform inspectorTarget = FindRect(winTr, "InspectorCard")
                                            ?? FindRect(winTr, "StudentsListCard")
                                            ?? winRt;

            // Step 1: LAN Class Server Status & Controls
            yield return ShowTourStep(ui => ui.teacher_tut_dash_server, serverTarget, new Vector2(-90f, -20f), 7.0f);
            if (!IsTourRunning) yield break;

            // Step 2: Classroom Batch Controls (Class Default, Apply to All, Launch All)
            yield return ShowTourStep(ui => ui.teacher_tut_dash_batch, batchTarget, new Vector2(-110f, -18f), 7.2f);
            if (!IsTourRunning) yield break;

            // Step 3: Live Class Name & Student Permissions (Lock Names, Lock Leave Class)
            yield return ShowTourStep(ui => ui.teacher_tut_dash_permissions, permissionsTarget, new Vector2(-110f, -18f), 7.0f);
            if (!IsTourRunning) yield break;

            // Step 4: Connected Students List & Live Telemetry Inspector (9 Skill Bars)
            yield return ShowTourStep(ui => ui.teacher_tut_dash_students_inspector, inspectorTarget, new Vector2(-240f, 90f), 7.5f);
            if (!IsTourRunning) yield break;

            CompleteActiveTour();
        }

        private IEnumerator ShowTourStep(Func<UIDialogs, string> messageResolver, RectTransform target, Vector2 offset, float duration)
        {
            if (tutorialBlocker != null)
            {
                tutorialBlocker.gameObject.SetActive(true);
                tutorialBlocker.SetAllowedTarget(null);
            }

            currentStepMessageResolver = messageResolver;
            currentStepTarget = target;
            currentStepOffset = offset;

            string msg = messageResolver != null ? messageResolver(UIDialogs.Current) : string.Empty;
            VirtualMascot.Show(msg, target, offset);
            if (VirtualMascot.Instance != null)
            {
                VirtualMascot.Instance.transform.SetAsLastSibling();
            }

            yield return VirtualMascot.WaitForDialogue(duration);
        }

        private void HandleLanguageChanged()
        {
            if (!IsTourRunning || currentStepMessageResolver == null) return;
            string msg = currentStepMessageResolver(UIDialogs.Current);
            VirtualMascot.Show(msg, currentStepTarget, currentStepOffset);
            if (VirtualMascot.Instance != null)
            {
                VirtualMascot.Instance.transform.SetAsLastSibling();
            }
        }

        private void CompleteActiveTour()
        {
            StopActiveTour(markCompleted: true);
        }

        private void StopActiveTour(bool markCompleted)
        {
            if (markCompleted)
            {
                if (activeTourType == ActiveTourType.ControlPanel)
                {
                    PlayerPrefs.SetInt(ControlPanelTourPrefKey, 1);
                    PlayerPrefs.Save();
                }
                else if (activeTourType == ActiveTourType.Dashboard)
                {
                    PlayerPrefs.SetInt(DashboardTourPrefKey, 1);
                    PlayerPrefs.Save();
                }
            }

            IsTourRunning = false;
            activeTourType = ActiveTourType.None;
            monitoredModalRoot = null;
            currentStepMessageResolver = null;
            currentStepTarget = null;

            if (activeTourCoroutine != null)
            {
                StopCoroutine(activeTourCoroutine);
                activeTourCoroutine = null;
            }

            CleanupBlocker();
            VirtualMascot.HideMascot();
        }

        private static RectTransform FindRect(Transform root, string relativePath)
        {
            if (root == null || string.IsNullOrEmpty(relativePath)) return null;
            Transform found = root.Find(relativePath);
            return found as RectTransform;
        }
        #endregion
    }
}
