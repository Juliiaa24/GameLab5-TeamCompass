using System;
using TMPro;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Updates the system tray digital clock on the desktop taskbar using HH:mm format.
    /// </summary>
    public class TaskbarClock : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeText;
        private string lastFormattedTime;

        private void Awake()
        {
            if (timeText == null)
            {
                timeText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnEnable()
        {
            lastFormattedTime = null;
            UpdateClock();
        }

        private void Update()
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            if (timeText == null) return;

            string nowFormatted = DateTime.Now.ToString("HH:mm");
            if (nowFormatted != lastFormattedTime)
            {
                lastFormattedTime = nowFormatted;
                timeText.text = nowFormatted;
            }
        }
    }
}
