using UnityEngine;


namespace Project.Runtime.Bike
{
    public interface IBikeVisuals
    {
        Transform SeatSocket{get;}
        Transform LeftHandSocket{get;}
        Transform RightHandSocket{get;}

        void UpdateWheelSpin(float forwardSpeed);
        void SetChassisRoll(float normalizedInput);
    }
}