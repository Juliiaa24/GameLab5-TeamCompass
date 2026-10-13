/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Component that creates a gentle breathing/pulsing scale animation to guide the player to the next level or task.
 */
using UnityEngine;

namespace ComputerLearning
{
    public class PulsingIcon : MonoBehaviour
    {
        [SerializeField] private bool isPulsing = false;
        private Vector3 originalScale = Vector3.one;
        private bool hasInitialized = false;

        public bool IsPulsing
        {
            get => isPulsing;
            set => SetPulsing(value);
        }

        private void Awake()
        {
            EnsureScale();
        }

        private void OnEnable()
        {
            EnsureScale();
        }

        private void EnsureScale()
        {
            if (!hasInitialized)
            {
                originalScale = transform.localScale;
                if (originalScale == Vector3.zero) originalScale = Vector3.one;
                hasInitialized = true;
            }
        }

        private void Update()
        {
            if (isPulsing)
            {
                float factor = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.08f;
                transform.localScale = originalScale * factor;
            }
            else
            {
                if (transform.localScale != originalScale)
                {
                    transform.localScale = originalScale;
                }
            }
        }

        public void SetPulsing(bool pulse)
        {
            EnsureScale();
            isPulsing = pulse;
            if (!pulse)
            {
                transform.localScale = originalScale;
            }
        }
    }
}
