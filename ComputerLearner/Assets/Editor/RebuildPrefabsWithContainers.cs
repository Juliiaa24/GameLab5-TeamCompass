using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace ComputerLearning.EditorTools
{
    public class RebuildPrefabsWithContainers : Editor
    {
        [MenuItem("ComputerLearning/Rebuild Prefabs With Containers")]
        public static void Rebuild()
        {
            RebuildSingle("Task_DoubleClick", "DOUBLE\nCLICK", new Color(0.2f, 0.6f, 1f), typeof(DoubleClickTask));
            RebuildSingle("Task_RightClick", "RIGHT\nCLICK", new Color(1f, 0.4f, 0.2f), typeof(RightClickTask));
            RebuildSingle("Task_ClickHold", "HOLD", new Color(0.8f, 0.2f, 0.8f), typeof(ClickHoldTask));
            RebuildDragDrop("Task_DragDrop");
            
            AssetDatabase.SaveAssets();
            Debug.Log("[RebuildPrefabsWithContainers] Done! All tasks now have a proper container.");
        }

        private static void RebuildSingle(string prefabName, string text, Color color, System.Type taskType)
        {
            string path = $"Assets/Prefabs/Tasks/{prefabName}.prefab";
            
            // Create root container
            GameObject container = new GameObject(prefabName, typeof(RectTransform));
            RectTransform rootRect = container.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            
            // Create target
            GameObject target = new GameObject("Target", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(container.transform, false);
            RectTransform targetRect = target.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.5f, 0.5f); targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.sizeDelta = new Vector2(150, 150);
            targetRect.anchoredPosition = Vector2.zero;
            
            Image img = target.GetComponent<Image>();
            img.color = color;
            
            // Add task script to target
            target.AddComponent(taskType);
            
            // Add Text
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(target.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            Text txt = textGo.GetComponent<Text>();
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = text;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.raycastTarget = false;
            
            PrefabUtility.SaveAsPrefabAsset(container, path);
            DestroyImmediate(container);
        }

        private static void RebuildDragDrop(string prefabName)
        {
            string path = $"Assets/Prefabs/Tasks/{prefabName}.prefab";
            
            // Create root container
            GameObject container = new GameObject(prefabName, typeof(RectTransform));
            RectTransform rootRect = container.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            
            // Create Drop Target
            GameObject dropTarget = new GameObject("DropTarget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dropTarget.transform.SetParent(container.transform, false);
            RectTransform dropRect = dropTarget.GetComponent<RectTransform>();
            dropRect.anchorMin = new Vector2(0.5f, 0.5f); dropRect.anchorMax = new Vector2(0.5f, 0.5f);
            dropRect.sizeDelta = new Vector2(250, 250);
            dropRect.anchoredPosition = new Vector2(300, 0); // Right side
            Image dropImg = dropTarget.GetComponent<Image>();
            dropImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            GameObject dropTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            dropTextGo.transform.SetParent(dropTarget.transform, false);
            RectTransform dropTextRect = dropTextGo.GetComponent<RectTransform>();
            dropTextRect.anchorMin = Vector2.zero; dropTextRect.anchorMax = Vector2.one;
            dropTextRect.sizeDelta = Vector2.zero;
            Text dropTxt = dropTextGo.GetComponent<Text>();
            dropTxt.alignment = TextAnchor.MiddleCenter;
            dropTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (dropTxt.font == null) dropTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dropTxt.text = "DROP HERE";
            dropTxt.fontSize = 24;
            dropTxt.color = Color.white;
            dropTxt.raycastTarget = false;

            // Create Draggable Target
            GameObject dragTarget = new GameObject("DragTarget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            dragTarget.transform.SetParent(container.transform, false);
            RectTransform dragRect = dragTarget.GetComponent<RectTransform>();
            dragRect.anchorMin = new Vector2(0.5f, 0.5f); dragRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragRect.sizeDelta = new Vector2(150, 150);
            dragRect.anchoredPosition = new Vector2(-300, 0); // Left side
            Image dragImg = dragTarget.GetComponent<Image>();
            dragImg.color = new Color(0.2f, 0.8f, 0.4f); // Greenish
            
            DragDropTask task = dragTarget.AddComponent<DragDropTask>();
            task.dropTarget = dropRect;

            GameObject dragTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            dragTextGo.transform.SetParent(dragTarget.transform, false);
            RectTransform dragTextRect = dragTextGo.GetComponent<RectTransform>();
            dragTextRect.anchorMin = Vector2.zero; dragTextRect.anchorMax = Vector2.one;
            dragTextRect.sizeDelta = Vector2.zero;
            Text dragTxt = dragTextGo.GetComponent<Text>();
            dragTxt.alignment = TextAnchor.MiddleCenter;
            dragTxt.font = dropTxt.font;
            dragTxt.text = "DRAG ME";
            dragTxt.fontSize = 24;
            dragTxt.color = Color.white;
            dragTxt.raycastTarget = false;
            
            PrefabUtility.SaveAsPrefabAsset(container, path);
            DestroyImmediate(container);
        }
    }
}
