using UnityEngine;

namespace Project.Core
{
    [SelectionBase]
    public class CargoShip : MonoBehaviour
    {
        [Header("Floating / Sway Animation")]
        [SerializeField] private bool enableSway = true;
        [SerializeField] private float rollAmount = 1.2f;
        [SerializeField] private float pitchAmount = 0.5f;
        [SerializeField] private float swaySpeed = 1.0f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 0.8f;

        private Vector3 _basePos;
        private Quaternion _baseRot;

        private void Start()
        {
            _basePos = transform.position;
            _baseRot = transform.rotation;
        }

        private void Update()
        {
            if (!enableSway) return;

            float t = Time.time;
            float roll = Mathf.Sin(t * swaySpeed) * rollAmount;
            float pitch = Mathf.Cos(t * swaySpeed * 0.65f) * pitchAmount;
            float bob = Mathf.Sin(t * bobSpeed) * bobHeight;

            transform.position = _basePos + new Vector3(0f, bob, 0f);
            transform.rotation = _baseRot * Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
