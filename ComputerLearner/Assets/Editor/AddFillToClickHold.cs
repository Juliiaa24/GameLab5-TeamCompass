using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace ComputerLearning.EditorTools
{
    public class AddFillToClickHold : Editor
    {
        [MenuItem("ComputerLearning/Add Fill To ClickHold")]
        public static void AddFill()
        {
            string path = "Assets/Prefabs/Tasks/Task_ClickHold.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            
            Transform target = instance.transform.Find("Target");
            if (target != null)
            {
                // Check if FillImage already exists
                Transform existingFill = target.Find("FillImage");
                if (existingFill != null) DestroyImmediate(existingFill.gameObject);

                GameObject fillGo = new GameObject("FillImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillGo.transform.SetParent(target, false);
                fillGo.transform.SetSiblingIndex(0); // Put it behind the text if there is text

                RectTransform fillRect = fillGo.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                Image fillImg = fillGo.GetComponent<Image>();
                
                // Assign Unity's default UI sprite so Filled mode works
                fillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                
                // Make the fill image slightly transparent white
                fillImg.color = new Color(1f, 1f, 1f, 0.5f);
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Vertical;
                fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
                fillImg.fillAmount = 0.5f; // Just for testing in editor, code sets it to 0
                fillImg.raycastTarget = false;

                ClickHoldTask task = target.GetComponent<ClickHoldTask>();
                if (task != null)
                {
                    task.fillImage = fillImg;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            Debug.Log("[AddFillToClickHold] Done!");
        }
    }
}
