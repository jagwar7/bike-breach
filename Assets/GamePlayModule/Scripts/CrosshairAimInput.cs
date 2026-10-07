using UnityEngine;
using Project.Runtime.Core.Input;

public class CrosshairAimInput : MonoBehaviour
{
    [Header("Player References (Dynamic)")]
    [SerializeField] private RiderAimController riderAim;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("UI Crosshair")]
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Camera mainCamera;

    [Header("Targeting Settings")]
    [Tooltip("Layers to hit when not aiming at an enemy (ground, containers, walls).")]
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Screen Space Aim Assist")]
    [Tooltip("Leave at 0 to automatically use the crosshair UI radius. Set > 0 to manually define pixel radius.")]
    [SerializeField] private float customScreenRadius = 0f;

    [Header("Bottom Deadzone")]
    [Tooltip("Screen height fraction (0.3 = bottom 30%) where aiming/shooting is disabled to avoid firing backwards.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float bottomDeadzoneRatio = 0.4f;

    [Header("Sensitivity")]
    [Tooltip("Drag multiplier for moving the reticle.")]
    [SerializeField] private float dragSensitivity = 1f;

    [Header("Raycast Debug")]
    [SerializeField] private Transform weaponMuzzle;

    private IInputService _inputService;
    private SplineBikeRunner _bikeRunner;
    private Vector3 _currentAimWorldPoint;
    private Vector2 _lastTouchPosition;
    private bool _wasTouching;
    private bool _isInAimZone;

    private Transform _aimPointTransform;

    public Vector3 CurrentAimWorldPoint => _currentAimWorldPoint;

    private void Awake()
    {
        _inputService = FindAnyObjectByType<FloatingJoystickInputService>();
        _bikeRunner = FindAnyObjectByType<SplineBikeRunner>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        GameObject aimObj = new GameObject("Crosshair_WorldAimPoint");
        _aimPointTransform = aimObj.transform;
    }

    private void OnEnable()
    {
        _wasTouching = false;

        if (crosshairRect != null)
        {
            crosshairRect.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.75f, 0f);
        }

        EnsurePlayerReferences();
    }

    private void EnsurePlayerReferences()
    {
        // 1. Try reading the active character from SplineBikeRunner
        if (_bikeRunner == null)
        {
            _bikeRunner = FindAnyObjectByType<SplineBikeRunner>();
        }

        if (_bikeRunner != null && _bikeRunner.ActivePlayerCharacter != null)
        {
            playerCharacter = _bikeRunner.ActivePlayerCharacter;
            riderAim = playerCharacter.GetComponent<RiderAimController>();
        }
        else if (playerCharacter == null)
        {
            // Fallback search across scene
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
            if (playerCharacter != null)
            {
                riderAim = playerCharacter.GetComponent<RiderAimController>();
            }
        }

        if (riderAim != null && _aimPointTransform != null)
        {
            riderAim.target = _aimPointTransform;
        }
    }

private void Update()
{
    if (playerCharacter == null)
    {
        EnsurePlayerReferences();
    }

    // Only allow aiming and shooting if we have reached the ship combat deck
    if (_bikeRunner != null && _bikeRunner.CurrentState != BikeRunState.OnShipDeckCombat)
    {
        return;
    }

    bool isScreenTouching = IsInputActive();

    HandleScreenDrag(isScreenTouching);
    UpdateAimPointAndTarget();

    if (_isInAimZone && isScreenTouching && playerCharacter != null)
    {
        playerCharacter.TryShoot(_currentAimWorldPoint);
    }
}

    private bool IsInputActive()
    {
        if (_inputService != null && _inputService.IsTouching)
        {
            return true;
        }

        return Input.GetMouseButton(0) || Input.touchCount > 0;
    }

    private void HandleScreenDrag(bool isTouching)
    {
        if (!isTouching)
        {
            _wasTouching = false;
            return;
        }

        Vector2 currentTouchPosition = GetCurrentScreenPosition();

        if (!_wasTouching)
        {
            _lastTouchPosition = currentTouchPosition;
            _wasTouching = true;
            return;
        }

        Vector2 delta = (currentTouchPosition - _lastTouchPosition) * dragSensitivity;
        _lastTouchPosition = currentTouchPosition;

        if (delta.sqrMagnitude > 0.001f && crosshairRect != null)
        {
            crosshairRect.position += (Vector3)delta;

            Vector3 pos = crosshairRect.position;
            pos.x = Mathf.Clamp(pos.x, 0f, Screen.width);
            pos.y = Mathf.Clamp(pos.y, 0f, Screen.height);
            crosshairRect.position = pos;
        }
    }

    private Vector2 GetCurrentScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    private void UpdateAimPointAndTarget()
    {
        if (crosshairRect == null || mainCamera == null) return;

        Canvas canvas = crosshairRect.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) 
            ? canvas.worldCamera 
            : null;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, crosshairRect.position);

        float bottomThreshold = Screen.height * bottomDeadzoneRatio;
        _isInAimZone = screenPos.y > bottomThreshold;

        if (!_isInAimZone)
        {
            if (riderAim != null) riderAim.target = null;
            return;
        }

        float reticlePixelRadius = customScreenRadius > 0f 
            ? customScreenRadius 
            : (crosshairRect.rect.width * 0.5f * (canvas != null ? canvas.scaleFactor : 1f));

        Transform lockedEnemy = FindEnemyInsideReticle(screenPos, reticlePixelRadius);

        if (lockedEnemy != null)
        {
            Collider enemyCol = lockedEnemy.GetComponentInChildren<Collider>();
            _currentAimWorldPoint = (enemyCol != null) ? enemyCol.bounds.center : lockedEnemy.position + Vector3.up * 1.0f;
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, aimLayers))
            {
                _currentAimWorldPoint = hit.point;
            }
            else
            {
                _currentAimWorldPoint = ray.GetPoint(rayDistance);
            }
        }

        Vector3 origin = weaponMuzzle != null 
            ? weaponMuzzle.position 
            : (playerCharacter != null ? playerCharacter.transform.position + Vector3.up * 1.2f : transform.position);

        Debug.DrawLine(origin, _currentAimWorldPoint, Color.red);

        if (_aimPointTransform != null)
        {
            _aimPointTransform.position = _currentAimWorldPoint;

            if (riderAim != null && riderAim.target != _aimPointTransform)
            {
                riderAim.target = _aimPointTransform;
            }
        }
    }

    private Transform FindEnemyInsideReticle(Vector2 reticleCenter, float radiusInPixels)
    {
        Transform closestEnemy = null;
        float closestDist = float.MaxValue;

        Health[] allHealths = FindObjectsByType<Health>(FindObjectsSortMode.None);

        for (int i = 0; i < allHealths.Length; i++)
        {
            Health h = allHealths[i];

            if (!h.enabled || h.GetComponent<PlayerCharacter>() != null || h.GetComponentInParent<PlayerCharacter>() != null)
            {
                continue;
            }

            Vector3 worldPos = h.transform.position + Vector3.up * 1.0f;
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPoint.z <= 0) continue;

            float dist = Vector2.Distance(reticleCenter, (Vector2)screenPoint);

            if (dist <= radiusInPixels && dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = h.transform;
            }
        }

        return closestEnemy;
    }

    private void OnDestroy()
    {
        if (_aimPointTransform != null)
        {
            Destroy(_aimPointTransform.gameObject);
        }
    }
}