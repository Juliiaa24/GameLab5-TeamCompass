/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Central shortcut accessors for all manager singletons.
*/

using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Static convenience class — provides short accessors to every manager singleton.
    /// Usage example:  Managers.Skill().GetScore("click")
    /// </summary>
    public static class Managers
    {
        // ── Existing managers ─────────────────────────────────────────────

        /// <summary>InputManager singleton.</summary>
        public static InputManager Im() => InputManager.Instance;

        /// <summary>SceneSystem singleton.</summary>
        public static SceneSystem Sy() => SceneSystem.Instance;

        /// <summary>WindowManager singleton.</summary>
        public static WindowManager Wm() => WindowManager.Instance;

        // ── Adaptive learning managers ────────────────────────────────────

        /// <summary>SkillManager — scores, difficulty, selection weights.</summary>
        public static SkillManager Skill() => SkillManager.Instance;

        /// <summary>TaskManager — task selection and prefab spawning.</summary>
        public static TaskManager Tasks() => TaskManager.Instance;

        /// <summary>LearningSessionManager — adaptive session orchestration.</summary>
        public static LearningSessionManager Session() => LearningSessionManager.Instance;

        /// <summary>ProgressData — task history and JSON persistence.</summary>
        public static ProgressData Progress() => ProgressData.Instance;
    }
}
