/**
 * Author: Diego
 * Date: 30/09/26
 * Description: ScriptableObject that defines a single task in the adaptive system.
 */
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Defines a single task: which skill it trains, its difficulty, the prefab to spawn,
    /// and the configuration passed to that prefab at runtime.
    ///
    /// Create assets under: ScriptableObjects/Tasks/
    /// One asset = one task variant. Different difficulties of the same mechanic
    /// are separate assets with different TaskConfig values.
    /// </summary>
    [CreateAssetMenu(fileName = "T_NewTask", menuName = "ComputerLearning/Task Definition")]
    public class TaskDefinition : ScriptableObject
    {
        #region Public Variables
        [Tooltip("Unique identifier used for history tracking and anti-repetition. Must not change after creation.")]
        public string taskId;

        [Tooltip("Human-readable description shown in the debug panel and inspector.")]
        public string displayName;

        [Tooltip("The primary skill this task trains. The task result updates this skill's score.")]
        public SkillDefinition primarySkill;

        [Tooltip("Difficulty level 1–5. Set to match the score band this task is appropriate for.")]
        [Range(1, 5)]
        public int difficulty = 1;

        [Tooltip("The BaseTask prefab to instantiate when this task runs.")]
        public GameObject taskPrefab;

        [Tooltip("Runtime configuration passed to the spawned task prefab.")]
        public TaskConfig config = new TaskConfig();
        #endregion
    }
}
