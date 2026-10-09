using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace ComputerLearning
{
    public class RecycleBinIcon : MonoBehaviour, UnityEngine.EventSystems.IDropHandler
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
            
            // Set the color directly to white to fix the transparent issue
            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = Color.white;
            
            UnityEngine.UI.Text txt = GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null) txt.text = "Recycle Bin";
        }

        public void OnDrop(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                DraggableIcon icon = eventData.pointerDrag.GetComponent<DraggableIcon>();
                if (icon != null && icon.gameObject != this.gameObject)
                {
                    Debug.Log($"[RecycleBin] Recycled {icon.gameObject.name}");
                    OnIconRecycled?.Invoke(icon);
                    Destroy(icon.gameObject);
                }
            }
        }
    }
}
