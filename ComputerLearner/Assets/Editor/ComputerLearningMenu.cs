using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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

    /// <summary>
    /// Clears ReadOnly attributes and removes stale Temp/BurstOutput folders prior to Player builds
    /// to prevent Windows IOException (Access Denied on Temp\BurstOutput\Data\Plugins).
    /// </summary>
    public class BurstBuildOutputPreCleaner : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            CleanDirectoryIfPresent("Temp/BurstOutput");

            if (report != null && report.summary.platform == BuildTarget.WebGL)
            {
                EnsureWebGLIdbfsIncluded();
            }
        }

        private static void EnsureWebGLIdbfsIncluded()
        {
            string args = PlayerSettings.WebGL.emscriptenArgs ?? string.Empty;
            if (!args.Contains("DEFAULT_LIBRARY_FUNCS_TO_INCLUDE"))
            {
                args = (args + " -sDEFAULT_LIBRARY_FUNCS_TO_INCLUDE=$IDBFS -lidbfs.js").Trim();
                PlayerSettings.WebGL.emscriptenArgs = args;
            }
        }

        private static void CleanDirectoryIfPresent(string relativePath)
        {
            try
            {
                string fullPath = Path.GetFullPath(relativePath);
                if (!Directory.Exists(fullPath)) return;

                DirectoryInfo dirInfo = new DirectoryInfo(fullPath);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                foreach (FileSystemInfo info in dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes &= ~FileAttributes.ReadOnly;
                }

                Directory.Delete(fullPath, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BurstBuildOutputPreCleaner] Could not pre-clean '{relativePath}': {e.Message}");
            }
        }
    }
}
