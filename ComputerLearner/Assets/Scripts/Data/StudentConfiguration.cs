/**
 * Author: TeamCompass
 * Description: Pure JSON-serializable configuration payload defining a student's Learning Path,
 *              assessment requirement, starting scene/level, tour flags, and initial skill profile.
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
        public LearningPath learningPath = LearningPath.DiagnosticFull;

        [Tooltip("If true, the student runs AssessmentScene first before entering postAssessmentScene.")]
        public bool runAssessment = true;

        [Tooltip("Initial scene loaded when clicking Play (e.g. AssessmentScene, Desktop1, Desktop2, VirtualDesktop).")]
        public string startingScene = "AssessmentScene";

        [Tooltip("Target scene loaded right after AssessmentScene finishes when runAssessment is true.")]
        public string postAssessmentScene = "Desktop1";

        [Tooltip("If true, completing Desktop1 (Levels 1-5) routes to semi-guided Desktop2 (Levels 6-9) before VirtualDesktop.")]
        public bool routeDesktop1ToDesktop2 = false;

        [Tooltip("First level unlocked for the student (empty in FreePractice where all 16 levels are unlocked).")]
        public string startingLevelId = "level1";

        [Tooltip("Marks level1..levelN as completed prior to starting (0 = none, 5 = L1-L5, 9 = L1-L9).")]
        public int preCompleteUpToLevel = 0;

        [Tooltip("If true, unlocks all 16 levels (level1..level16) immediately for open sandbox access.")]
        public bool unlockAllLevels = false;

        [Tooltip("If true, skips DesktopTour and VirtualDesktopIntro upon entering VirtualDesktop.")]
        public bool skipBasicDesktopTours = false;

        [Tooltip("If true, skips AdvancedDesktopTour so the student is never blocked by window-resize/recycle tutorials.")]
        public bool skipAdvancedDesktopTour = false;

        [Tooltip("Predefined skill scores seeded into SkillManager when runAssessment is false.")]
        public List<SkillScoreEntry> initialSkillScores = new List<SkillScoreEntry>();

        /// <summary>
        /// Builds the canonical StudentConfiguration for one of the 5 Learning Paths.
        /// </summary>
        public static StudentConfiguration FromLearningPath(LearningPath path, string studentId = null)
        {
            string resolvedId = !string.IsNullOrWhiteSpace(studentId)
                ? studentId.Trim()
                : PlaytestDataManager.CurrentStudentId;

            StudentConfiguration cfg = new StudentConfiguration
            {
                studentId = resolvedId,
                learningPath = path,
                initialSkillScores = SkillProfilePreset.GetDefaultEntriesForPath(path)
            };

            switch (path)
            {
                case LearningPath.BeginnerFoundations:
                    cfg.runAssessment = false;
                    cfg.startingScene = "Desktop1";
                    cfg.postAssessmentScene = "Desktop1";
                    cfg.routeDesktop1ToDesktop2 = true;
                    cfg.startingLevelId = "level1";
                    cfg.preCompleteUpToLevel = 0;
                    cfg.unlockAllLevels = false;
                    cfg.skipBasicDesktopTours = false;
                    cfg.skipAdvancedDesktopTour = false;
                    break;

                case LearningPath.StandardIntermediate:
                    cfg.runAssessment = false;
                    cfg.startingScene = "Desktop2";
                    cfg.postAssessmentScene = "Desktop2";
                    cfg.routeDesktop1ToDesktop2 = true;
                    cfg.startingLevelId = "level6";
                    cfg.preCompleteUpToLevel = 5;
                    cfg.unlockAllLevels = false;
                    cfg.skipBasicDesktopTours = false;
                    cfg.skipAdvancedDesktopTour = false;
                    break;

                case LearningPath.AdvancedApplications:
                    cfg.runAssessment = false;
                    cfg.startingScene = "VirtualDesktop";
                    cfg.postAssessmentScene = "VirtualDesktop";
                    cfg.routeDesktop1ToDesktop2 = false;
                    cfg.startingLevelId = "level10";
                    cfg.preCompleteUpToLevel = 9;
                    cfg.unlockAllLevels = false;
                    cfg.skipBasicDesktopTours = true;
                    cfg.skipAdvancedDesktopTour = true;
                    break;

                case LearningPath.FreePractice:
                    cfg.runAssessment = false;
                    cfg.startingScene = "VirtualDesktop";
                    cfg.postAssessmentScene = "VirtualDesktop";
                    cfg.routeDesktop1ToDesktop2 = false;
                    cfg.startingLevelId = string.Empty;
                    cfg.preCompleteUpToLevel = 0;
                    cfg.unlockAllLevels = true;
                    cfg.skipBasicDesktopTours = true;
                    cfg.skipAdvancedDesktopTour = true;
                    break;

                case LearningPath.DiagnosticFull:
                default:
                    cfg.learningPath = path == LearningPath.Custom ? LearningPath.Custom : LearningPath.DiagnosticFull;
                    cfg.runAssessment = true;
                    cfg.startingScene = "AssessmentScene";
                    cfg.postAssessmentScene = "Desktop1";
                    cfg.routeDesktop1ToDesktop2 = false;
                    cfg.startingLevelId = "level1";
                    cfg.preCompleteUpToLevel = 0;
                    cfg.unlockAllLevels = false;
                    cfg.skipBasicDesktopTours = false;
                    cfg.skipAdvancedDesktopTour = false;
                    break;
            }

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
                postAssessmentScene = "Desktop1";
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
                case LearningPath.DiagnosticFull:
                    return "Diagnostic (Full)";
                case LearningPath.BeginnerFoundations:
                    return "Beginner (L1-L9)";
                case LearningPath.StandardIntermediate:
                    return "Standard (L6-L9)";
                case LearningPath.AdvancedApplications:
                    return "Advanced (L10-L16)";
                case LearningPath.FreePractice:
                    return "Free Practice";
                case LearningPath.Custom:
                    return "Custom Path";
                default:
                    return path.ToString();
            }
        }

        public static string GetPathShortSummary(StudentConfiguration cfg)
        {
            if (cfg == null) return string.Empty;

            string assessmentPart = cfg.runAssessment ? "Assessment: ON" : "Assessment: Skipped";
            string targetSceneLabel;
            string destination = cfg.runAssessment ? cfg.postAssessmentScene : cfg.startingScene;

            switch (destination)
            {
                case "Desktop1":
                    targetSceneLabel = cfg.routeDesktop1ToDesktop2
                        ? "Starts: Desktop 1 -> Desktop 2 (L1-L9)"
                        : "Starts: Desktop 1 (L1-L5) -> OS";
                    break;
                case "Desktop2":
                    targetSceneLabel = "Starts: Desktop 2 (Levels 6-9)";
                    break;
                case "VirtualDesktop":
                    targetSceneLabel = cfg.unlockAllLevels
                        ? "Starts: Virtual Desktop (All 16 Unlocked)"
                        : "Starts: Virtual Desktop (Levels 10-16)";
                    break;
                default:
                    targetSceneLabel = $"Starts: {destination}";
                    break;
            }

            return $"{assessmentPart}  |  {targetSceneLabel}";
        }
    }
}
