/**
 * Author: Diego
 * Date: 11/09/26
 * Description: Input manager script
*/

using UnityEngine;
using UnityEngine.InputSystem;

namespace ComputerLearning
{
    /// <summary>
    /// Manage the input of the game
    /// </summary>
    public class InputManager : MonoBehaviour
    {

        #region Public Variables
        /**
         * Instancia unica del InputManager
         */
        public static InputManager Instance { get; private set; }


        #endregion

        #region Private Variables

        [SerializeField] private Transform windowSpawner;
        [SerializeField] private GameObject windowPrefab;
        
        //mouse
        [SerializeField] private InputActionReference mousePosition;
        [SerializeField] private InputActionReference mouseDelta;
        [SerializeField] private InputActionReference leftClick;
        [SerializeField] private InputActionReference rightClick;
        [SerializeField] private InputActionReference scroll;

        //keyboard
        [SerializeField] private InputActionReference spawnWindow;
        private bool ownsSpawnAction;

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
            // Input references belong to this scene; do not retain destroyed Canvas references.
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {

        }

        private void Update()
        {
            if (spawnWindow != null && spawnWindow.action != null && spawnWindow.action.WasPressedThisFrame())
            {
                WindowManager manager = WindowManager.Instance;
                if (manager != null) manager.OpenWindow(windowPrefab);
                else if (windowPrefab != null && windowSpawner != null) Instantiate(windowPrefab, windowSpawner);
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
            if (spawnWindow != null && spawnWindow.action != null)
            {
                ownsSpawnAction = !spawnWindow.action.enabled;
                spawnWindow.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (ownsSpawnAction && spawnWindow != null && spawnWindow.action != null)
                spawnWindow.action.Disable();
        }

        #endregion
    }
}
