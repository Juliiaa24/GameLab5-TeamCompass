using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

namespace ComputerLearning.EditorTools
{
    public class FixInputModule : Editor
    {
        [MenuItem("ComputerLearning/Fix Test Scene Input")]
        public static void FixInput()
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    DestroyImmediate(oldModule);
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(es.gameObject.scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(es.gameObject.scene);
                    Debug.Log("[ComputerLearning] Fixed Input Module successfully!");
                }
                else
                {
                    Debug.Log("[ComputerLearning] StandaloneInputModule not found. Is it already fixed?");
                }
            }
            else
            {
                Debug.Log("[ComputerLearning] No EventSystem found in the active scene.");
            }
        }
    }
}
