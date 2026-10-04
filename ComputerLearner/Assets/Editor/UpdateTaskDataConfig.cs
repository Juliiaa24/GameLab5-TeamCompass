using UnityEngine;
using UnityEditor;

namespace ComputerLearning.EditorTools
{
    public class UpdateTaskDataConfig : Editor
    {
        [MenuItem("ComputerLearning/Update Task Data Config")]
        public static void UpdateData()
        {
            string[] guids = AssetDatabase.FindAssets("t:TaskDefinition", new[] { "Assets/Data/Tasks" });
            int count = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TaskDefinition def = AssetDatabase.LoadAssetAtPath<TaskDefinition>(path);
                
                if (def == null) continue;

                // Only update the newly created tasks
                if (def.taskId.StartsWith("doubleclick") || 
                    def.taskId.StartsWith("rightclick") || 
                    def.taskId.StartsWith("clickhold") || 
                    def.taskId.StartsWith("dragdrop"))
                {
                    def.config.targetSize = 150f - (def.difficulty * 20f);
                    
                    if (def.taskId.StartsWith("dragdrop"))
                    {
                        def.config.targetCount = def.difficulty >= 4 ? 3 : (def.difficulty == 3 ? 2 : 1);
                        def.config.targetSize = 250f - (def.difficulty * 20f); // Base drop size
                    }
                    else if (def.taskId.StartsWith("clickhold"))
                    {
                        def.config.isMoving = false;
                        def.config.targetCount = 1;
                    }
                    else // double or right click
                    {
                        def.config.isMoving = def.difficulty >= 3;
                        def.config.moveSpeed = 100f + (def.difficulty * 30f);
                    }

                    EditorUtility.SetDirty(def);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UpdateTaskDataConfig] Updated {count} TaskDefinition assets with proper config scaling!");
        }
    }
}
