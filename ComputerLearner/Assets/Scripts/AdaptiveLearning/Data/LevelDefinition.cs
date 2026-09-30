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
        
        [Tooltip("The tasks that make up this level, executed in order.")]
        public List<TaskDefinition> tasks;
    }
}
