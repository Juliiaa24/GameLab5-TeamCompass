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
            ConfigureClockAppearance();
        }

        private void OnEnable()
        {
            lastFormattedTime = null;
            ConfigureClockAppearance();
            UpdateClock();
        }

        private void Update()
        {
            UpdateClock();
        }

        private void ConfigureClockAppearance()
        {
            RectTransform trayRt = transform as RectTransform;
            if (trayRt != null && trayRt.sizeDelta.x < 114f)
            {
                trayRt.sizeDelta = new Vector2(116f, trayRt.sizeDelta.y);
            }

            if (timeText != null)
            {
                timeText.fontSize = 14.5f;
                timeText.lineSpacing = -10f;
                timeText.textWrappingMode = TextWrappingModes.NoWrap;
                timeText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void UpdateClock()
        {
            if (timeText == null) return;

            DateTime now = DateTime.Now;
            string nowFormatted = $"<b>{now:HH:mm}</b>\n<size=72%>{now:dd/MM/yyyy}</size>";
            if (nowFormatted != lastFormattedTime)
            {
                lastFormattedTime = nowFormatted;
                timeText.text = nowFormatted;
            }
        }
    }
}
