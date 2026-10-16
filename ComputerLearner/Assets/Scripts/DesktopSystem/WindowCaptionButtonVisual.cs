/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Handles pointer hover states for Windows caption buttons using Inspector-serialized graphics and colors.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public enum WindowCaptionButtonType
    {
        Minimize,
        Maximize,
        Close
    }

    /// <summary>
    /// Handles pointer hover states for Windows caption buttons using Inspector-serialized graphics and colors.
    /// </summary>
    public class WindowCaptionButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image background;
        [SerializeField] private WindowCaptionButtonType buttonType;
        [SerializeField] private Graphic[] glyphGraphics;

        [Header("Colors")]
        [SerializeField] private Color normalBgColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color hoverBgColor = new Color(0.84f, 0.86f, 0.90f, 1f);
        [SerializeField] private Color normalGlyphColor = new Color(0.15f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color hoverGlyphColor = new Color(0.15f, 0.18f, 0.22f, 1f);

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null)
            {
                background.color = hoverBgColor;
            }
            if (glyphGraphics != null)
            {
                foreach (var g in glyphGraphics)
                {
                    if (g != null) g.color = hoverGlyphColor;
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyNormalState();
        }

        private void OnDisable()
        {
            ApplyNormalState();
        }

        private void ApplyNormalState()
        {
            if (background != null)
            {
                background.color = normalBgColor;
            }
            if (glyphGraphics != null)
            {
                foreach (var g in glyphGraphics)
                {
                    if (g != null) g.color = normalGlyphColor;
                }
            }
        }
    }
}
