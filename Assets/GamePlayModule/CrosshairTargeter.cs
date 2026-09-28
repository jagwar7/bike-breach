using UnityEngine;

public class CrosshairTargeter : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private RectTransform crosshairUI; 

    public Transform CurrentTarget { get; private set; }
    public Vector3 HitPoint { get; private set; }

    void Update()
    {

        Vector2 screenPos = crosshairUI != null 
            ? (Vector2)crosshairUI.position 
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Ray ray = mainCamera.ScreenPointToRay(screenPos);


        if (Physics.Raycast(ray, out RaycastHit hit, 200f, targetLayer))
        {
            CurrentTarget = hit.transform;
            HitPoint = hit.point;
        }
        else
        {
            CurrentTarget = null;
            HitPoint = ray.origin + ray.direction * 200f;
        }
    }
}