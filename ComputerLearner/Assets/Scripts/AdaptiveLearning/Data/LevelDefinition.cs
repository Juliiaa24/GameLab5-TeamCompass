using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Defines a discrete Level (a specific sequence of tasks).
    /// This acts as a bridge between the desktop icons and the Task system.
    /// </summary>
    [CreateAssetMenu(fileName = "L_NewLevel", menuName = "ComputerLearning/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public string displayName;
        
        [TextArea(2, 5)]
        [Tooltip("Text shown to the player in the didactic intro screen before starting the tasks.")]
        public string instructionText;
        
        [Header("Adaptive Mode")]
        [Tooltip("If true, the level ignores the fixed 'tasks' list and generates tasks dynamically.")]
        public bool isAdaptive = false;
        
        [Tooltip("How many tasks to generate before completing the level (if isAdaptive is true).")]
        public int adaptiveTaskCount = 5;

        [Header("Fixed Mode Tasks")]
        [Tooltip("The tasks that make up this level, executed in order (if isAdaptive is false).")]
        public List<TaskDefinition> tasks;
    }
}
