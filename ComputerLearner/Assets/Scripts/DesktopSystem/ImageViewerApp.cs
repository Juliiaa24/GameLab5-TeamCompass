/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Prefab-backed Image Viewer application component with serialized UI references.
 */
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class ImageViewerApp : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image displayedImage;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button btnEditInPaint;
        [SerializeField] private Button btnZoomOut;
        [SerializeField] private Button btnZoomReset;
        [SerializeField] private Button btnZoomIn;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomStep = 0.25f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 3.0f;

        private Action onEditCallback;

        private void Awake()
        {
            if (btnEditInPaint != null)
            {
                btnEditInPaint.onClick.RemoveListener(OnEditClicked);
                btnEditInPaint.onClick.AddListener(OnEditClicked);
            }
            if (btnZoomOut != null)
            {
                btnZoomOut.onClick.RemoveListener(ZoomOut);
                btnZoomOut.onClick.AddListener(ZoomOut);
            }
            if (btnZoomReset != null)
            {
                btnZoomReset.onClick.RemoveListener(ResetZoom);
                btnZoomReset.onClick.AddListener(ResetZoom);
            }
            if (btnZoomIn != null)
            {
                btnZoomIn.onClick.RemoveListener(ZoomIn);
                btnZoomIn.onClick.AddListener(ZoomIn);
            }
        }

        public void Initialize(ExplorerItemData item, Sprite sprite, Action onEditInPaintAction)
        {
            onEditCallback = onEditInPaintAction;

            if (displayedImage != null)
            {
                displayedImage.sprite = sprite;
                displayedImage.preserveAspect = true;
                displayedImage.rectTransform.localScale = Vector3.one;
            }

            if (infoText != null && item != null)
            {
                string dimensions = sprite != null && sprite.texture != null
                    ? $" ({sprite.texture.width}x{sprite.texture.height}px)"
                    : string.Empty;
                infoText.text = $"{item.name}{dimensions}";
            }
        }

        public void ZoomIn()
        {
            if (displayedImage == null) return;
            RectTransform rt = displayedImage.rectTransform;
            float nextScale = Mathf.Clamp(rt.localScale.x + zoomStep, minZoom, maxZoom);
            rt.localScale = new Vector3(nextScale, nextScale, 1f);
        }

        public void ZoomOut()
        {
            if (displayedImage == null) return;
            RectTransform rt = displayedImage.rectTransform;
            float nextScale = Mathf.Clamp(rt.localScale.x - zoomStep, minZoom, maxZoom);
            rt.localScale = new Vector3(nextScale, nextScale, 1f);
        }

        public void ResetZoom()
        {
            if (displayedImage == null) return;
            displayedImage.rectTransform.localScale = Vector3.one;
        }

        private void OnEditClicked()
        {
            onEditCallback?.Invoke();
        }
    }
}
