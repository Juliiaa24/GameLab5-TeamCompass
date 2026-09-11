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
    /// <summary>
    /// 
    /// </summary>
    public class ResizeHandle : MonoBehaviour
    {

        #region Public Variables
        // Public Variables [Public Constant Variables, Public Component References, Public Variables]

        // Public Constant Variables


        // Public Component References


        // Public Variables
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
        // Private Variables [Private Constant Variables, Private Component References, Private Variables]

        // Private Constant Variables


        // Private Component References
        [SerializeField] private RectTransform window;
        private RectTransform parentRect;


        // Private Variables
        private float startLeft;
        private float startRight;
        private float startTop;
        private float startBottom;
        private Vector2 startPointerPosition;
        [SerializeField] private ResizeDirection direction;
        [SerializeField] private Vector2 minSize = new Vector2(300, 200);
        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            parentRect = window.parent as RectTransform;
        }
        private void Start()
        {

        }

        private void Update()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Guardamos dónde empezó el ratón,
            // pero en coordenadas del padre de la ventana.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out startPointerPosition
            );

            // Guardamos los 4 bordes actuales de la ventana.
            Rect rect = window.rect;
            Vector3 position = window.localPosition;

            startLeft = position.x + rect.xMin;
            startRight = position.x + rect.xMax;
            startBottom = position.y + rect.yMin;
            startTop = position.y + rect.yMax;
        }

        public void OnDrag(PointerEventData eventData)
        {
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

            // Movemos únicamente los lados que controla este handle.

            if (HasDirection(ResizeDirection.Left))
                left += delta.x;

            if (HasDirection(ResizeDirection.Right))
                right += delta.x;

            if (HasDirection(ResizeDirection.Top))
                top += delta.y;

            if (HasDirection(ResizeDirection.Bottom))
                bottom += delta.y;


            // Tamaño mínimo horizontal
            if (right - left < minSize.x)
            {
                if (HasDirection(ResizeDirection.Left))
                    left = right - minSize.x;
                else
                    right = left + minSize.x;
            }

            // Tamaño mínimo vertical
            if (top - bottom < minSize.y)
            {
                if (HasDirection(ResizeDirection.Bottom))
                    bottom = top - minSize.y;
                else
                    top = bottom + minSize.y;
            }


            float width = right - left;
            float height = top - bottom;

            // Cambiamos tamaño.
            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                width
            );

            window.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height
            );

            // Y recolocamos la ventana para mantener
            // el borde contrario en su sitio.
            Vector3 newPosition = window.localPosition;

            newPosition.x = left + width * window.pivot.x;
            newPosition.y = bottom + height * window.pivot.y;

            window.localPosition = newPosition;
        }

        #endregion

        #region Private Methods
        // Private Methods accessible only from this class

        private bool HasDirection(ResizeDirection value)
        {
            return (direction & value) != 0;
        }
        #endregion
    }
}
