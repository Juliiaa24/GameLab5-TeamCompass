using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace ComputerLearning
{
    public class RecycleBinIcon : MonoBehaviour
    {
        public static event Action<DraggableIcon> OnIconRecycled;
        private RectTransform myRect;

        private void Start()
        {
            myRect = GetComponent<RectTransform>();
            
            DraggableIcon di = GetComponent<DraggableIcon>();
            if (di != null)
            {
                // We keep DraggableIcon so it occupies a grid slot and prevents overlapping!
                // But we remove its window/level references so it doesn't open anything.
                di.SetupDynamicIcon(null, null, null, false);
            }
            
            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = new Color(0.2f, 0.2f, 0.2f);
            
            UnityEngine.UI.Text txt = GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null) txt.text = "Recycle Bin";
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DraggableIcon[] icons = FindObjectsByType<DraggableIcon>(FindObjectsSortMode.None);
                foreach (var icon in icons)
                {
                    if (icon.gameObject == this.gameObject) continue;

                    RectTransform iconRect = icon.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        float dist = Vector2.Distance(myRect.position, iconRect.position);
                        if (dist < 100f) // Snap threshold
                        {
                            Debug.Log($"[RecycleBin] Recycled {icon.gameObject.name}");
                            OnIconRecycled?.Invoke(icon);
                            Destroy(icon.gameObject);
                        }
                    }
                }
            }
        }
    }
}
