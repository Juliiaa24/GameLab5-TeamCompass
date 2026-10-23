using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public static class FolderColorUtility
    {
        public struct FolderColorOption
        {
            public string name;
            public Color color;
            public string spriteName;

            public FolderColorOption(string name, Color color, string spriteName)
            {
                this.name = name;
                this.color = color;
                this.spriteName = spriteName;
            }
        }

        public static readonly FolderColorOption[] Options = new[]
        {
            new FolderColorOption("Yellow (Default)", new Color32(245, 190, 40, 255), "Folder_Icon01"),
            new FolderColorOption("Blue", new Color32(59, 130, 246, 255), "Folder_Icon01_Blue"),
            new FolderColorOption("Green", new Color32(34, 197, 94, 255), "Folder_Icon01_Green"),
            new FolderColorOption("Red", new Color32(239, 68, 68, 255), "Folder_Icon01_Red"),
            new FolderColorOption("Purple", new Color32(168, 85, 247, 255), "Folder_Icon01_Purple"),
            new FolderColorOption("Cyan", new Color32(6, 182, 212, 255), "Folder_Icon01_Cyan"),
            new FolderColorOption("Pink", new Color32(236, 72, 153, 255), "Folder_Icon01_Pink"),
            new FolderColorOption("Light Pink", new Color32(244, 114, 182, 255), "Folder_Icon01_LightPink"),
            new FolderColorOption("Grey", new Color32(107, 114, 128, 255), "Folder_Icon01_Grey"),
            new FolderColorOption("Black", new Color32(31, 41, 55, 255), "Folder_Icon01_Black")
        };

        private static readonly Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static Sprite GetSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;

            if (cachedSprites.TryGetValue(spriteName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite s = null;
#if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/App icons/FolderIcons/{spriteName}.png");
#endif
            if (s == null)
            {
                s = Resources.Load<Sprite>($"FolderIcons/{spriteName}");
            }
            if (s == null)
            {
                s = Resources.Load<Sprite>(spriteName);
            }

            if (s != null)
            {
                cachedSprites[spriteName] = s;
            }
            return s;
        }
    }
}
