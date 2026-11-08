/**
 * Author: TeamCompass
 * Description: Defines the teacher-selectable Learning Paths supported by Computer Learner.
 */
using System;

namespace ComputerLearning
{
    /// <summary>
    /// Explicit student profiles / learning flows controlled locally or by a teacher host.
    /// All three flows complete Levels 1 through 16 progressively, differing in tutorial guidance.
    /// </summary>
    [Serializable]
    public enum LearningPath
    {
        /// <summary>
        /// Flow 1: Full guided progression as originally planned.
        /// Runs the initial blocking tutorial in Desktop1 (Levels 1-5), then enters VirtualDesktop
        /// with the window management tutorial (DesktopTour), Levels 6-9, AdvancedDesktopTour, and Levels 10-16.
        /// </summary>
        BeginnerFoundations = 0,

        /// <summary>
        /// Flow 2: Skips the initial blocking Desktop1 tutorial.
        /// Enters VirtualDesktop directly, completes the window management tutorial (DesktopTour),
        /// and then unlocks the entire Virtual Desktop while completing Levels 1-16 progressively.
        /// </summary>
        Intermediate = 1,

        /// <summary>
        /// Flow 3: No tutorials or input blockers of any kind.
        /// Enters VirtualDesktop directly with all desktop features and applications immediately unlocked,
        /// while still completing Levels 1-16 progressively (Levels 2-16 locked until completed in order).
        /// </summary>
        Advanced = 2,

        /// <summary>
        /// Teacher-customized configuration for future LAN host overrides.
        /// </summary>
        Custom = 3
    }
}
