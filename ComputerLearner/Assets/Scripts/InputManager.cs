/**
 * Author: DEVELOPERNAME
 * Date: CREATIONDATE
 * Description:
*/

using UnityEngine;
using UnityEngine.InputSystem;

namespace ComputerLearning
{
    /// <summary>
    /// 
    /// </summary>
    public class InputManager : MonoBehaviour
    {

        #region Public Variables
        // Public Variables [Public Constant Variables, Public Component References, Public Variables]

        // Public Constant Variables
        public static InputManager Instance { get; private set; }

        // Public Component References


        // Public Variables


        #endregion

        #region Private Variables
        // Private Variables [Private Constant Variables, Private Component References, Private Variables]


        // Private Constant Variables


        // Private Component References


        // Private Variables

        [SerializeField] private InputActionReference mousePosition;
        [SerializeField] private InputActionReference mouseDelta;
        [SerializeField] private InputActionReference leftClick;
        [SerializeField] private InputActionReference rightClick;
        [SerializeField] private InputActionReference scroll;

        #endregion

        #region Unity Methods
        // Unity Methods including (Awake, Start, Update, LateUpdate...)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {

        }

        private void Update()
        {
            Vector2 position = mousePosition.action.ReadValue<Vector2>();
            Vector2 delta = mouseDelta.action.ReadValue<Vector2>();
            Vector2 scrollValue = scroll.action.ReadValue<Vector2>();

            if (leftClick.action.WasPressedThisFrame())
            {
                Debug.Log($"Left click at {position}");
            }

            if (rightClick.action.WasPressedThisFrame())
            {
                Debug.Log($"Right click at {position}");
            }

            if (delta != Vector2.zero)
            {
                Debug.Log($"Mouse delta: {delta}");
            }

            if (scrollValue != Vector2.zero)
            {
                Debug.Log($"Scroll: {scrollValue.y}");
            }
        }

        #endregion

        #region Public Methods
        // Public Methods accessible from other classes


        #endregion

        #region Private Methods
        // Private Methods accessible only from this class

        private void OnEnable()
        {
            mousePosition.action.Enable();
            mouseDelta.action.Enable();
            leftClick.action.Enable();
            rightClick.action.Enable();
            scroll.action.Enable();
        }

        private void OnDisable()
        {
            mousePosition.action.Disable();
            mouseDelta.action.Disable();
            leftClick.action.Disable();
            rightClick.action.Disable();
            scroll.action.Disable();
        }

        #endregion
    }
}
