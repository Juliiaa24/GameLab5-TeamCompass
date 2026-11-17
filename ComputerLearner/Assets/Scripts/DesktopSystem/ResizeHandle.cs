/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Window resizing script
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ResizeHandle : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        #region Public Variables
        [Flags]
        public enum ResizeDirection
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }
        #endregion

        #region Private Variables
        [SerializeField] private RectTransform window;
        private RectTransform parentRect;
        private Window controller;
        private bool resizing;
        private bool isPointerOver;

        private static ResizeHandle activeCursorOwner;
        private static Texture2D cursorHorizontal;
        private static Texture2D cursorVertical;
        private static Texture2D cursorDiagNWSE;
        private static Texture2D cursorDiagNESW;
        private static readonly Vector2 CursorHotspot = new Vector2(16f, 16f);

        private float startLeft;
        private float startRight;
        private float startTop;
        private float startBottom;
        private Vector2 startPointerPosition;
        [SerializeField] private ResizeDirection direction;
        [SerializeField] private Vector2 minSize = new Vector2(490, 300);
        public Vector2 MinSize { get => minSize; set => minSize = value; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (window == null) window = transform.parent as RectTransform;
            if (window == null) return;
            parentRect = window.parent as RectTransform;
            controller = window.GetComponent<Window>();
        }

        private void OnDisable()
        {
            resizing = false;
            isPointerOver = false;
            if (activeCursorOwner == this)
            {
                RestoreDefaultCursor();
            }
        }

        private void OnDestroy()
        {
            if (activeCursorOwner == this)
            {
                RestoreDefaultCursor();
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (window == null) window = transform.parent as RectTransform;
            if (controller == null && window != null) controller = window.GetComponent<Window>();
            if (controller != null && controller.IsMaximized) return;

            ApplyResizeCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            if (!resizing && activeCursorOwner == this)
            {
                RestoreDefaultCursor();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            resizing = false;
            if (!isPointerOver && activeCursorOwner == this)
            {
                RestoreDefaultCursor();
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            resizing = false;
            if (window == null) window = transform.parent as RectTransform;
            if (window == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (controller == null) controller = window.GetComponent<Window>();
            controller?.BringToFront();
            if (controller != null && controller.IsMaximized) return;

            parentRect = window.parent as RectTransform;
            resizing = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.pressPosition,
                eventData.pressEventCamera,
                out startPointerPosition
            );

            Rect rect = window.rect;
            Vector3 position = window.localPosition;

            startLeft = position.x + rect.xMin;
            startRight = position.x + rect.xMax;
            startBottom = position.y + rect.yMin;
            startTop = position.y + rect.yMax;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!resizing || window == null || (controller != null && controller.IsMaximized)) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 currentPointerPosition
            );

            Vector2 delta = currentPointerPosition - startPointerPosition;

            float left = startLeft;
            float right = startRight;
            float top = startTop;
            float bottom = startBottom;

            if (HasDirection(ResizeDirection.Left))
                left += delta.x;

            if (HasDirection(ResizeDirection.Right))
                right += delta.x;

            if (HasDirection(ResizeDirection.Top))
                top += delta.y;

            if (HasDirection(ResizeDirection.Bottom))
                bottom += delta.y;

            // Clamp dragged edges to parent desktop container bounds
            if (parentRect != null && parentRect.rect.width > 0f && parentRect.rect.height > 0f)
            {
                Rect pRect = parentRect.rect;
                if (HasDirection(ResizeDirection.Left))
                    left = Mathf.Max(left, pRect.xMin);
                if (HasDirection(ResizeDirection.Right))
                    right = Mathf.Min(right, pRect.xMax);
                if (HasDirection(ResizeDirection.Top))
                    top = Mathf.Min(top, pRect.yMax);
                if (HasDirection(ResizeDirection.Bottom))
                    bottom = Mathf.Max(bottom, pRect.yMin);
            }

            // Enforce minimum horizontal size
            if (right - left < minSize.x)
            {
                if (HasDirection(ResizeDirection.Left))
                    left = right - minSize.x;
                else
                    right = left + minSize.x;
            }

            // Enforce minimum vertical size
            if (top - bottom < minSize.y)
            {
                if (HasDirection(ResizeDirection.Bottom))
                    bottom = top - minSize.y;
                else
                    top = bottom + minSize.y;
            }

            float width = right - left;
            float height = top - bottom;

            if (controller != null) controller.IsResizing = true;

            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                width
            );

            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height
            );

            Vector3 newPosition = window.localPosition;
            newPosition.x = left + width * window.pivot.x;
            newPosition.y = bottom + height * window.pivot.y;
            window.localPosition = newPosition;

            if (controller != null)
            {
                controller.IsResizing = false;
                controller.KeepTitleVisible();
            }
        }
        #endregion

        #region Private Methods
        private bool HasDirection(ResizeDirection value)
        {
            return (direction & value) != 0;
        }

        private void ApplyResizeCursor()
        {
            EnsureCursorTextures();
            bool left = HasDirection(ResizeDirection.Left);
            bool right = HasDirection(ResizeDirection.Right);
            bool top = HasDirection(ResizeDirection.Top);
            bool bottom = HasDirection(ResizeDirection.Bottom);

            Texture2D tex = null;
            if ((left && top) || (right && bottom))
            {
                tex = cursorDiagNWSE;
            }
            else if ((right && top) || (left && bottom))
            {
                tex = cursorDiagNESW;
            }
            else if (left || right)
            {
                tex = cursorHorizontal;
            }
            else if (top || bottom)
            {
                tex = cursorVertical;
            }

            if (tex != null)
            {
                activeCursorOwner = this;
                Cursor.SetCursor(tex, CursorHotspot, CursorMode.Auto);
            }
        }

        private static void RestoreDefaultCursor()
        {
            activeCursorOwner = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private static void EnsureCursorTextures()
        {
            if (cursorHorizontal != null) return;

            cursorHorizontal = BuildOrthoResizeCursor(horizontal: true);
            cursorVertical = BuildOrthoResizeCursor(horizontal: false);
            cursorDiagNWSE = BuildDiagonalResizeCursor(nwse: true);
            cursorDiagNESW = BuildDiagonalResizeCursor(nwse: false);
        }

        private static Texture2D BuildOrthoResizeCursor(bool horizontal)
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            int[,] mask = new int[size, size];

            for (int u = 4; u <= 27; u++)
            {
                for (int v = 0; v < size; v++)
                {
                    int dv = Mathf.Abs(v - 16);
                    bool inShaft = (u >= 9 && u <= 22 && dv <= 1);
                    bool inLeftHead = (u >= 4 && u <= 9 && dv <= (u - 4));
                    bool inRightHead = (u >= 22 && u <= 27 && dv <= (27 - u));

                    if (inShaft || inLeftHead || inRightHead)
                    {
                        int x = horizontal ? u : v;
                        int y = horizontal ? v : u;
                        mask[x, y] = 2;
                    }
                }
            }

            return FinalizeCursorFromMask(tex, mask, size);
        }

        private static Texture2D BuildDiagonalResizeCursor(bool nwse)
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            int[,] mask = new int[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int cx = nwse ? x : (size - 1 - x);
                    int cy = y;

                    int diagDiff = Mathf.Abs((cx + cy) - 31);
                    bool inShaft = (cx >= 9 && cx <= 22 && cy >= 9 && cy <= 22 && diagDiff <= 1);
                    bool inTopLeftHead = (cx >= 6 && cx <= 12 && cy >= 19 && cy <= 25 && (cy - cx) >= 13);
                    bool inBottomRightHead = (cx >= 19 && cx <= 25 && cy >= 6 && cy <= 12 && (cx - cy) >= 13);

                    if (inShaft || inTopLeftHead || inBottomRightHead)
                    {
                        mask[x, y] = 2;
                    }
                }
            }

            return FinalizeCursorFromMask(tex, mask, size);
        }

        private static Texture2D FinalizeCursorFromMask(Texture2D tex, int[,] mask, int size)
        {
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 outline = new Color32(18, 22, 28, 255);
            Color32 fill = new Color32(255, 255, 255, 255);

            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    if (mask[x, y] != 0) continue;
                    bool neighborFill = false;
                    for (int dx = -1; dx <= 1 && !neighborFill; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (mask[x + dx, y + dy] == 2)
                            {
                                neighborFill = true;
                                break;
                            }
                        }
                    }
                    if (neighborFill) mask[x, y] = 1;
                }
            }

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int m = mask[x, y];
                    pixels[y * size + x] = m == 2 ? fill : (m == 1 ? outline : clear);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
        #endregion
    }
}
