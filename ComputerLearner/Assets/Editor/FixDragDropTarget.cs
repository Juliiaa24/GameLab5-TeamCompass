using UnityEngine;
using UnityEditor;

namespace ComputerLearning.EditorTools
{
    public class FixDragDropTarget : Editor
    {
        [MenuItem("ComputerLearning/Fix Drag Drop Target")]
        public static void Fix()
        {
            string path = "Assets/Prefabs/Tasks/Task_DragDrop.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            
            DragDropTask task = instance.GetComponent<DragDropTask>();
            Transform dropTarget = instance.transform.Find("DropTarget");
            
            if (task != null && dropTarget != null)
            {
                task.dropTarget = dropTarget.GetComponent<RectTransform>();
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Debug.Log("[FixDragDrop] Assigned drop target!");
            }

            DestroyImmediate(instance);
        }
    }
}
