using UnityEngine;
using UnityEditor;

namespace ComputerLearning.EditorTools
{
    public class ComputerLearningMenu : Editor
    {
        [MenuItem("ComputerLearning/Reset All Progress (PlayerPrefs)")]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[ComputerLearning] PlayerPrefs cleared! You can now test the auto-sequence from the beginning.");
        }
    }
}
