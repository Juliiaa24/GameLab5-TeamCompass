/**
 * Author: TeamCompass
 * Description: Defines the teacher-selectable Learning Paths supported by Computer Learner.
 */
using System;

namespace ComputerLearning
{
    /// <summary>
    /// Explicit student profiles / learning branches controlled locally or by a teacher host.
    /// </summary>
    [Serializable]
    public enum LearningPath
    {
        /// <summary>
        /// Runs the Initial Assessment first, then guides the student through the full curriculum (Levels 1-16).
        /// </summary>
        DiagnosticFull = 0,

        /// <summary>
        /// Skips the stressful timed assessment, seeds Tier 1 beginner skill scores,
        /// and guides the student step-by-step through Desktop1 (L1-L5) and Desktop2 (L6-L9) into VirtualDesktop.
        /// </summary>
        BeginnerFoundations = 1,

        /// <summary>
        /// Skips the basic auto-opened mouse levels (L1-L5), seeds Tier 2-3 intermediate scores,
        /// and starts in semi-guided Desktop2 (Levels 6-9) before transitioning to VirtualDesktop.
        /// </summary>
        StandardIntermediate = 2,

        /// <summary>
        /// Skips mouse tutorials (L1-L9) and basic desktop tours, seeds Tier 4 advanced mouse scores,
        /// and enters VirtualDesktop ready for OS context menus, keyboard shortcuts, Notepad, and Paint (Levels 10-16).
        /// </summary>
        AdvancedApplications = 3,

        /// <summary>
        /// Skips assessment and all blocking tours, unlocks all 16 levels and desktop applications,
        /// and opens VirtualDesktop immediately as an open sandbox.
        /// </summary>
        FreePractice = 4,

        /// <summary>
        /// Teacher-customized configuration (e.g. a preset path with manual overrides such as forcing assessment).
        /// </summary>
        Custom = 5
    }
}
