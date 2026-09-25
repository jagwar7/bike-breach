using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig_Default", menuName = "Character/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Surveillance / Detection")]
    [SerializeField] private float detectionRange = 25f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 10f;

    [SerializeField] private float fireInterval = 1f;


    public float DetectionRange => detectionRange;
    public float TurnSpeed => turnSpeed;
    public float FireInterval => fireInterval;
}