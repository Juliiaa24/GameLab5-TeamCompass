using UnityEngine;
using UnityEditor;

namespace ComputerLearning.EditorTools
{
    public class FixPrefabs : Editor
    {
        [MenuItem("ComputerLearning/Fix Task Prefabs References")]
        public static void Fix()
        {
            AssignPrefabs("Task_DoubleClick", "T_DoubleClick");
            AssignPrefabs("Task_RightClick", "T_RightClick");
            AssignPrefabs("Task_ClickHold", "T_ClickHold");
            AssignPrefabs("Task_DragDrop", "T_DragDrop");
            
            AssetDatabase.SaveAssets();
            Debug.Log("[FixPrefabs] All prefabs assigned correctly!");
        }

        private static void AssignPrefabs(string prefabName, string prefix)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Tasks/{prefabName}.prefab");
            if (prefab == null) return;

            string[] guids = AssetDatabase.FindAssets("t:TaskDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(prefix))
                {
                    TaskDefinition task = AssetDatabase.LoadAssetAtPath<TaskDefinition>(path);
                    if (task != null && task.taskPrefab == null)
                    {
                        task.taskPrefab = prefab;
                        EditorUtility.SetDirty(task);
                    }
                }
            }
        }
    }
}
