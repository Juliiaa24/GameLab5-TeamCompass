using UnityEngine;

namespace ComputerLearning
{
    public class LevelTester : MonoBehaviour
    {
        public Transform canvasTransform;
        public LevelDefinition[] availableLevels;
        
        [Tooltip("The prefab that has the LevelRunner and Window components (usually ApplicationWindow)")]
        public GameObject windowPrefab;

        private GameObject currentWindow;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
            GUILayout.Box("Level Tester Menu", GUILayout.ExpandWidth(true));

            if (windowPrefab == null)
            {
                GUILayout.Label("ERROR: Assign Window Prefab in the Inspector!");
                GUILayout.EndArea();
                return;
            }

            if (availableLevels == null || availableLevels.Length == 0)
            {
                GUILayout.Label("No levels found.");
                GUILayout.EndArea();
                return;
            }

            foreach (var level in availableLevels)
            {
                if (level == null) continue;

                if (GUILayout.Button($"Test {level.displayName} ({level.levelId})"))
                {
                    SpawnLevel(level);
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close Current Window"))
            {
                if (currentWindow != null)
                {
                    Destroy(currentWindow);
                }
            }

            GUILayout.EndArea();
        }

        private void SpawnLevel(LevelDefinition levelDef)
        {
            if (currentWindow != null)
            {
                Destroy(currentWindow);
            }

            // Spawn the window
            currentWindow = Instantiate(windowPrefab, canvasTransform);
            
            // Get or add LevelRunner
            LevelRunner runner = currentWindow.GetComponentInChildren<LevelRunner>();
            if (runner == null)
            {
                runner = currentWindow.gameObject.AddComponent<LevelRunner>();
                Transform contentArea = currentWindow.transform.Find("WindowContents");
                if (contentArea != null)
                    runner.taskContentArea = contentArea.GetComponent<RectTransform>();
                else
                    runner.taskContentArea = currentWindow.GetComponent<RectTransform>();
            }

            // Maximize if possible
            Window windowScript = currentWindow.GetComponent<Window>();
            if (windowScript != null)
            {
                windowScript.setTitle(levelDef.displayName);
                windowScript.toggleMaximize();
            }

            // Start Level
            runner.StartLevel(levelDef);
        }
    }
}
