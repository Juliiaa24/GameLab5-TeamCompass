using UnityEngine;
using UnityEditor;

namespace ComputerLearning.EditorTools
{
    public class SkillCreator : EditorWindow
    {
        [MenuItem("ComputerLearning/Generate Mouse Skills")]
        public static void GenerateSkills()
        {
            CreateOrUpdateSkill("SK_DoubleClick", "double_click", "Double Click", "The ability to perform two consecutive left clicks within a short time interval.");
            CreateOrUpdateSkill("SK_RightClick", "right_click", "Right Click", "The ability to perform a right mouse click to interact with an element or access additional options.");
            CreateOrUpdateSkill("SK_ClickHold", "click_hold", "Click & Hold", "The ability to press and hold the left mouse button for a continuous period of time.");
            CreateOrUpdateSkill("SK_DragDrop", "drag_drop", "Drag & Drop", "The ability to click and hold an object, move it to another location, and release it.");

            AssetDatabase.SaveAssets();
            Debug.Log("[ComputerLearning] 4 Mouse Skills created/updated successfully in Assets/ScriptableObjects/Skills!");
        }

        private static void CreateOrUpdateSkill(string assetName, string id, string displayName, string desc)
        {
            string path = $"Assets/ScriptableObjects/Skills/{assetName}.asset";
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            skill.skillId = id;
            skill.displayName = displayName;
            skill.description = desc;
            EditorUtility.SetDirty(skill);
        }
    }
}
