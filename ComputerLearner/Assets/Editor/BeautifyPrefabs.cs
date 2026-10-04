using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace ComputerLearning.EditorTools
{
    public class BeautifyPrefabs : Editor
    {
        [MenuItem("ComputerLearning/Beautify Task Prefabs")]
        public static void Beautify()
        {
            BeautifyPrefab("Task_DoubleClick", "DOUBLE\nCLICK", new Color(0.2f, 0.6f, 1f));
            BeautifyPrefab("Task_RightClick", "RIGHT\nCLICK", new Color(1f, 0.4f, 0.2f));
            BeautifyPrefab("Task_ClickHold", "HOLD", new Color(0.8f, 0.2f, 0.8f));
            BeautifyPrefab("Task_DragDrop", "DRAG ME", new Color(0.2f, 0.8f, 0.4f));
            
            AssetDatabase.SaveAssets();
            Debug.Log("[BeautifyPrefabs] Done!");
        }

        private static void BeautifyPrefab(string prefabName, string text, Color color)
        {
            string path = $"Assets/Prefabs/Tasks/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            
            Image img = instance.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                // Add border or make it nicer if we had sprites, but solid color is fine
            }

            // Check if it already has text
            Text txt = instance.GetComponentInChildren<Text>();
            if (txt == null)
            {
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(instance.transform, false);
                txt = textGo.AddComponent<Text>();
                RectTransform rect = textGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            txt.text = text;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.raycastTarget = false; // Don't block clicks

            if (prefabName == "Task_DragDrop")
            {
                // Find the DropTarget child
                Transform dropTarget = instance.transform.Find("DropTarget");
                if (dropTarget != null)
                {
                    Image targetImg = dropTarget.GetComponent<Image>();
                    if (targetImg != null) targetImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    
                    Text targetTxt = dropTarget.GetComponentInChildren<Text>();
                    if (targetTxt == null)
                    {
                        GameObject tt = new GameObject("Text");
                        tt.transform.SetParent(dropTarget, false);
                        targetTxt = tt.AddComponent<Text>();
                        RectTransform r = tt.GetComponent<RectTransform>();
                        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                        targetTxt.alignment = TextAnchor.MiddleCenter;
                        targetTxt.font = txt.font;
                    }
                    targetTxt.text = "DROP HERE";
                    targetTxt.fontSize = 20;
                    targetTxt.color = Color.white;
                    targetTxt.raycastTarget = false;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
        }
    }
}
