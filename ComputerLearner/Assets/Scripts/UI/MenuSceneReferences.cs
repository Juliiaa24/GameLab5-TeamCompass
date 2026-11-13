using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// Scene-resident holder for serialized Main Menu UI references in Menu.unity.
    /// Allows DontDestroyOnLoad SceneSystem to bind cleanly whenever Menu reloads.
    /// </summary>
    public class MenuSceneReferences : MonoBehaviour
    {
        [Header("Student Menu Core Buttons")]
        public Button btnPlay;
        public TMP_Text playSpaceHintText;
        public Button btnStats;
        public Button btnJoinClass;
        public Button btnExit;

        [Header("Student Menu Header & Labels")]
        public TMP_Text desktopTitleText;
        public TMP_Text languageLabelText;

        [Header("Menu Controllers")]
        public TeacherModeGate teacherModeGate;
        public PlaytestStudentWidget playtestStudentWidget;
        public LearningPathSelectorWidget learningPathSelectorWidget;
        public ClassroomNetworkWidget classroomNetworkWidget;
        public TeacherDashboardController teacherDashboardController;

        public bool IsAnyInputOrModalActive()
        {
            if (TeacherDashboardController.IsDashboardOpen) return true;
            if (ClassroomNetworkWidget.IsAnyInputFocused) return true;
            if (PlaytestStudentWidget.IsEditingStudentId) return true;
            if (teacherModeGate != null && teacherModeGate.IsAnyBlockingModalOpen) return true;
            return false;
        }
    }
}
