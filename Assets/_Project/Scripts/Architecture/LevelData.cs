using UnityEngine;
using UnityEngine.Splines;

public class LevelData : MonoBehaviour
{
    
    [Header("Track Configuration")]
    [SerializeField] private SplineContainer pipelineSplineContainer;
    [SerializeField] private SplineContainer shipSplineContainer;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Triggers")]
    [SerializeField] private Collider finishLineTrigger;

    public SplineContainer PipelineSpline => pipelineSplineContainer;
    public SplineContainer ShipSpline => shipSplineContainer;
    public Transform SpawnPoint => playerSpawnPoint;
    public Collider FinishLineTrigger => finishLineTrigger;
}
