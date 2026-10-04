using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using ComputerLearning;

namespace ComputerLearning.EditorTools
{
    [InitializeOnLoad]
    public class TaskPrefabGenerator : EditorWindow
    {
        static TaskPrefabGenerator()
        {
            EditorApplication.delayCall += () => {
                if (PlayerPrefs.GetInt("PrefabsGenerated", 0) == 0)
                {
                    PlayerPrefs.SetInt("PrefabsGenerated", 1);
                    GeneratePrefabs();
                }
            };
        }

        [MenuItem("ComputerLearning/Generate Task Prefabs")]
        public static void GeneratePrefabs()
        {
            CreatePrefab("Task_DoubleClick", typeof(DoubleClickTask), "T_DoubleClick");
            CreatePrefab("Task_RightClick", typeof(RightClickTask), "T_RightClick");
            CreatePrefab("Task_ClickHold", typeof(ClickHoldTask), "T_ClickHold");
            CreateDragDropPrefab("Task_DragDrop", "T_DragDrop");

            AssetDatabase.SaveAssets();
            Debug.Log("[TaskPrefabGenerator] Prefabs generated and assigned to tasks successfully!");
        }

        private static void CreatePrefab(string prefabName, System.Type taskType, string taskPrefix)
        {
            string prefabPath = $"Assets/Prefabs/Tasks/{prefabName}.prefab";
            GameObject go = new GameObject(prefabName);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            Image img = go.AddComponent<Image>();
            img.color = Color.green; // Just a placeholder color

            go.AddComponent(taskType);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);

            AssignPrefabToTasks(prefab, taskPrefix);
        }

        private static void CreateDragDropPrefab(string prefabName, string taskPrefix)
        {
            string prefabPath = $"Assets/Prefabs/Tasks/{prefabName}.prefab";
            
            // Parent container
            GameObject container = new GameObject(prefabName);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;

            // Target
            GameObject target = new GameObject("DropTarget");
            target.transform.SetParent(container.transform);
            RectTransform targetRect = target.AddComponent<RectTransform>();
            targetRect.anchoredPosition = new Vector2(200, 0); // Offset to the right
            targetRect.sizeDelta = new Vector2(150, 150);
            Image targetImg = target.AddComponent<Image>();
            targetImg.color = new Color(0, 0, 1, 0.3f); // Semi-transparent blue

            // Draggable object
            GameObject draggable = new GameObject("DraggableObject");
            draggable.transform.SetParent(container.transform);
            RectTransform dragRect = draggable.AddComponent<RectTransform>();
            dragRect.anchoredPosition = new Vector2(-200, 0); // Offset to the left
            dragRect.sizeDelta = new Vector2(100, 100);
            Image dragImg = draggable.AddComponent<Image>();
            dragImg.color = Color.red; // Red object

            DragDropTask taskScript = draggable.AddComponent<DragDropTask>();
            taskScript.dropTarget = targetRect;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(container, prefabPath);
            DestroyImmediate(container);

            AssignPrefabToTasks(prefab, taskPrefix);
        }

        private static void AssignPrefabToTasks(GameObject prefab, string taskPrefix)
        {
            string[] folders = { "DoubleClick", "RightClick", "ClickHold", "DragDrop" };
            
            foreach (string folder in folders)
            {
                for (int i = 1; i <= 5; i++)
                {
                    string path = $"Assets/ScriptableObjects/Tasks/{folder}/{taskPrefix}_{i}.asset";
                    TaskDefinition task = AssetDatabase.LoadAssetAtPath<TaskDefinition>(path);
                    if (task != null)
                    {
                        task.taskPrefab = prefab;
                        EditorUtility.SetDirty(task);
                    }
                }
            }
        }
    }
}
