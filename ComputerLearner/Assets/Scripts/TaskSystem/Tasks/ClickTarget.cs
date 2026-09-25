/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Consume one left click for each presented target.
*/
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ClickTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        #region Public Variables
        public event Action Hit;
        #endregion

        #region Private Variables
        private bool consumed;
        private float readyAt;
        #endregion

        #region Public Methods
        public void Prepare(float inputDelay)
        {
            consumed = false;
            readyAt = Time.unscaledTime + Mathf.Max(0f, inputDelay);
        }

        // Handle pointer-down on the same object so EventSystem delivers its click here.
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                !isActiveAndEnabled || consumed || Time.unscaledTime < readyAt) return;
            consumed = true;
            Hit?.Invoke();
        }
        #endregion
    }
}
