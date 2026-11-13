using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Configuration asset controlling Teacher Mode availability, unlock gesture, and default PIN.
    /// Store an asset named TeacherModeConfig inside Assets/Resources/ for automatic loading.
    /// Disable allowTeacherMode for school student-only builds.
    /// </summary>
    [CreateAssetMenu(fileName = "TeacherModeConfig", menuName = "ComputerLearner/Teacher Mode Config")]
    public class TeacherModeConfig : ScriptableObject
    {
        private const string ResourcePath = "TeacherModeConfig";
        private static TeacherModeConfig _cached;

        [Header("School Build Security")]
        [Tooltip("If false, completely hides the Teacher Mode button and blocks access for student-only builds.")]
        public bool allowTeacherMode = true;

        [Header("PIN Access")]
        [Tooltip("Default 4-digit teacher PIN used when no runtime override has been saved.")]
        public string defaultPin = "1234";

        [Header("Corner Button Activation")]
        [Tooltip("If true, the user must hold the corner lock button for longPressDuration seconds to open the PIN modal.")]
        public bool requireLongPress = false;

        [Tooltip("Seconds required when hold/long-press mode is enabled.")]
        [Range(0.4f, 4.0f)]
        public float longPressDuration = 1.5f;

        public static TeacherModeConfig Instance
        {
            get
            {
                if (_cached == null)
                {
                    _cached = Resources.Load<TeacherModeConfig>(ResourcePath);
                    if (_cached == null)
                    {
                        _cached = CreateInstance<TeacherModeConfig>();
                    }
                }
                return _cached;
            }
        }

        public string GetNormalizedDefaultPin()
        {
            string pin = string.IsNullOrEmpty(defaultPin) ? "1234" : defaultPin.Trim();
            if (pin.Length != 4) return "1234";
            for (int i = 0; i < pin.Length; i++)
            {
                if (!char.IsDigit(pin[i])) return "1234";
            }
            return pin;
        }
    }
}
