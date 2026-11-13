/**
 * Author: TeamCompass
 * Description: Pure JSON-serializable configuration payload defining a student's Learning Path,
 *              independent assessment toggle, starting scene, tutorial flags, and initial skill profile.
 *              Designed to work both offline (Menu selector) and over LAN (Host -> Client in Step 4).
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [Serializable]
    public class StudentConfiguration
    {
        public string studentId = "S01";
        public LearningPath learningPath = LearningPath.BeginnerFoundations;

        [Tooltip("Independent toggle: if true, the student completes AssessmentScene first before entering their flow.")]
        public bool runAssessment = true;

        [Tooltip("If true, runs the initial guided blocking tutorial in Desktop1 (Levels 1-5) before VirtualDesktop.")]
        public bool runInitialGuidedTutorial = true;

        [Tooltip("Initial scene loaded when clicking Play (AssessmentScene, Desktop1, or VirtualDesktop).")]
        public string startingScene = "AssessmentScene";

        [Tooltip("Target scene loaded right after AssessmentScene finishes (or immediately if runAssessment is false).")]
        public string postAssessmentScene = "Desktop1";

        [Tooltip("First level unlocked for the student (all 3 flows start at level1 and unlock level2..level16 progressively).")]
        public string startingLevelId = "level1";

        [Tooltip("Marks level1..levelN as completed prior to starting (0 for all standard flows so all 16 levels are played).")]
        public int preCompleteUpToLevel = 0;

        [Tooltip("If true, unlocks all 16 levels immediately (false for all 3 standard flows).")]
        public bool unlockAllLevels = false;

        [Tooltip("If true, skips DesktopTour (window management tutorial) and VirtualDesktopIntro upon entering VirtualDesktop.")]
        public bool skipBasicDesktopTours = false;

        [Tooltip("If true, skips AdvancedDesktopTour after Level 9 so the desktop stays completely unlocked.")]
        public bool skipAdvancedDesktopTour = false;

        [Tooltip("Predefined skill scores seeded into SkillManager when runAssessment is false.")]
        public List<SkillScoreEntry> initialSkillScores = new List<SkillScoreEntry>();

        /// <summary>
        /// Builds the canonical StudentConfiguration for one of the 3 Learning Paths plus the Assessment toggle.
        /// All 3 flows start at Level 1 and require completing Levels 1-16 progressively.
        /// </summary>
        public static StudentConfiguration FromLearningPath(
            LearningPath path,
            string studentId = null,
            bool runAssessment = true)
        {
            string resolvedId = !string.IsNullOrWhiteSpace(studentId)
                ? studentId.Trim()
                : PlaytestDataManager.CurrentStudentId;

            StudentConfiguration cfg = new StudentConfiguration
            {
                studentId = resolvedId,
                learningPath = path,
                startingLevelId = "level1",
                preCompleteUpToLevel = 0,
                unlockAllLevels = false,
                initialSkillScores = SkillProfilePreset.GetDefaultEntriesForPath(path)
            };

            switch (path)
            {
                case LearningPath.Intermediate:
                    // Skips initial blocking tutorial (Desktop1); runs window management tutorial (DesktopTour)
                    // in VirtualDesktop, then unlocks everything while completing Levels 1-16 progressively.
                    cfg.runInitialGuidedTutorial = false;
                    cfg.postAssessmentScene = "VirtualDesktop";
                    cfg.skipBasicDesktopTours = false;
                    cfg.skipAdvancedDesktopTour = true;
                    break;

                case LearningPath.Advanced:
                    // Drops directly into VirtualDesktop with zero tutorials and all desktop apps unlocked,
                    // while still requiring progressive completion of Levels 1-16.
                    cfg.runInitialGuidedTutorial = false;
                    cfg.postAssessmentScene = "VirtualDesktop";
                    cfg.skipBasicDesktopTours = true;
                    cfg.skipAdvancedDesktopTour = true;
                    break;

                case LearningPath.BeginnerFoundations:
                default:
                    // Full guided flow as originally planned: Desktop1 (Levels 1-5) -> VirtualDesktop
                    // (DesktopTour -> Levels 6-9 -> AdvancedDesktopTour -> Levels 10-16).
                    cfg.learningPath = path == LearningPath.Custom ? LearningPath.Custom : LearningPath.BeginnerFoundations;
                    cfg.runInitialGuidedTutorial = true;
                    cfg.postAssessmentScene = "Desktop1";
                    cfg.skipBasicDesktopTours = false;
                    cfg.skipAdvancedDesktopTour = false;
                    break;
            }

            cfg.SetRunAssessment(runAssessment);
            return cfg;
        }

        /// <summary>
        /// Updates runAssessment and keeps startingScene synchronized with postAssessmentScene.
        /// </summary>
        public void SetRunAssessment(bool enabled)
        {
            runAssessment = enabled;
            if (string.IsNullOrWhiteSpace(postAssessmentScene))
            {
                postAssessmentScene = runInitialGuidedTutorial ? "Desktop1" : "VirtualDesktop";
            }

            startingScene = enabled ? "AssessmentScene" : postAssessmentScene;
        }

        public StudentConfiguration Clone()
        {
            string json = JsonUtility.ToJson(this);
            return JsonUtility.FromJson<StudentConfiguration>(json);
        }

        public static string GetPathDisplayName(LearningPath path)
        {
            switch (path)
            {
                case LearningPath.BeginnerFoundations:
                    return "Beginner Foundations";
                case LearningPath.Intermediate:
                    return "Intermediate";
                case LearningPath.Advanced:
                    return "Advanced";
                case LearningPath.Custom:
                    return "Custom";
                default:
                    return path.ToString();
            }
        }

        public static string GetPathShortSummary(StudentConfiguration cfg)
        {
            if (cfg == null) return string.Empty;

            string lang = LocalizationManager.CurrentLanguage;
            bool isEs = string.Equals(lang, LocalizationManager.LANG_ES, StringComparison.OrdinalIgnoreCase);
            bool isNo = string.Equals(lang, LocalizationManager.LANG_NO, StringComparison.OrdinalIgnoreCase);

            UIDialogs ui = UIDialogs.Current;
            string assessmentPart = cfg.runAssessment ? ui.path_assess_on : ui.path_assess_off;

            string flowPart;
            switch (cfg.learningPath)
            {
                case LearningPath.Intermediate:
                    flowPart = isEs
                        ? "Tutorial ventanas -> Escritorio libre (Niveles 1-16)"
                        : (isNo
                            ? "Vindusveiledning -> Fritt skrivebord (Nivå 1-16)"
                            : "Window Tutorial -> Full Desktop (Levels 1-16)");
                    break;

                case LearningPath.Advanced:
                    flowPart = isEs
                        ? "Escritorio directo sin tutoriales (Niveles 1-16)"
                        : (isNo
                            ? "Direkte skrivebord uten veiledning (Nivå 1-16)"
                            : "Direct Virtual Desktop, No Tutorials (Levels 1-16)");
                    break;

                case LearningPath.BeginnerFoundations:
                default:
                    flowPart = isEs
                        ? "Guiado completo desde inicio (Niveles 1-16)"
                        : (isNo
                            ? "Full veiledet oppstart -> OS (Nivå 1-16)"
                            : "Full Guided Tutorial -> OS (Levels 1-16)");
                    break;
            }

            return $"{assessmentPart}  |  {flowPart}";
        }
    }
}
