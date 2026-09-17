using UnityEngine;
using UnityEngine.InputSystem;
using Project.Runtime.Bike;

namespace Project.Runtime.Combat
{
    [RequireComponent(typeof(SplineBikeRunner))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("LAYER CONFIGURATION")]
        [SerializeField] private LayerMask enemyLayer;

        [Header("CAMERA REFERENCE")]
        [SerializeField] private Camera mainCamera;

        private SplineBikeRunner _bikeRunner;
        private PlayerCharacter _playerCharacter;

        private void Awake()
        {
            // CACHE LOCAL REFERENCES
            _bikeRunner = GetComponent<SplineBikeRunner>();
            _playerCharacter = GetComponentInChildren<PlayerCharacter>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (enemyLayer == 0)
            {
                enemyLayer = LayerMask.GetMask("Default");
            }
        }

        private void Update()
        {
            // EXECUTE COMBAT TAPS ONLY DURING SHIP DECK COMBAT STATE
            if (_bikeRunner == null || _bikeRunner.CurrentState != BikeRunState.OnShipDeckCombat) return;
            if (_playerCharacter != null && _playerCharacter.IsDead) return;

            // 1. CHECK MOBILE TOUCHSCREEN PRESS VIA NEW INPUT SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                ProcessScreenTap(touchPos);
                return;
            }

            // 2. CHECK DESKTOP MOUSE CLICK VIA NEW INPUT SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                ProcessScreenTap(mousePos);
            }
        }

        private void ProcessScreenTap(Vector2 screenPos)
        {
            // CAST RAY FROM ACTIVE CAMERA VIEWPORT
            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, enemyLayer))
            {
                // DETECT ENEMY DUMMY AND APPLY HIT
                DummyEnemy enemy = hit.collider.GetComponent<DummyEnemy>();
                if (enemy != null)
                {
                    enemy.TakeHit();
                }
            }
        }
    }
}