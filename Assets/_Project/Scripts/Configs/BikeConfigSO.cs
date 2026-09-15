using UnityEngine;

namespace Project.Runtime.Configs
{
    [CreateAssetMenu(fileName = "BikeConfig", menuName = "Project/Configs/BikeConfig")]
    public class BikeConfigSO : ScriptableObject
    {
        [Header("Forward Drive")]
        [Tooltip("Maximum forward speed along the pipe.")]
        public float maxForwardSpeed = 14f;
        
        [Tooltip("Acceleration rate when pushing forward.")]
        public float accelerationRate = 8f;
        
        [Tooltip("Natural drag deceleration when releasing throttle.")]
        public float decelerationRate = 6f;

        [Header("Lateral Steering & Balance")]
        [Tooltip("How quickly horizontal drag rotates the bike along the pipe's arc.")]
        public float lateralSteerSpeed = 65f;

        [Tooltip("Deadzone angle from the top crown where centering assistance activates.")]
        public float apexAssistanceAngle = 18f;

        [Tooltip("Strength of the centering torque pulling the bike toward the top apex.")]
        public float apexSnapForce = 4.5f;

        [Tooltip("Angle where surface grip breaks completely and the bike falls.")]
        public float fallAngle = 65f;

        [Header("Radial Clamping & Surface Grip")]
        [Tooltip("Inward force multiplier keeping the bike adhered to the curved cylinder.")]
        public float radialSnapMultiplier = 1.35f;

        [Tooltip("LayerMask containing only the pipeline surface colliders.")]
        public LayerMask pipelineLayer;
    }
}