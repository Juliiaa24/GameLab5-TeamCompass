/**
 * Author: Diego
 * Date: 30/09/26
 * Description: ScriptableObject that defines a learnable computer skill.
 */
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Defines a learnable computer skill (e.g. Click, Hover, DragDrop).
    /// Create assets under: ScriptableObjects/Skills/
    /// </summary>
    [CreateAssetMenu(fileName = "SK_NewSkill", menuName = "ComputerLearning/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        #region Public Variables
        [Tooltip("Unique identifier used by all managers to reference this skill. Must not change after creation.")]
        public string skillId;

        [Tooltip("Human-readable name shown in UI and debug panels.")]
        public string displayName;

        [Tooltip("Optional icon for the UI.")]
        public Sprite icon;
        #endregion
    }
}
