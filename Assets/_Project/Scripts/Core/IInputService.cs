using UnityEngine;


namespace Project.Runtime.Core.Input
{
    public interface IInputService
    {
        /// <summary>
        /// 
        /// </summary>
        Vector2 MoveInput {get;}

        /// <summary>
        /// 
        /// </summary>
        bool IsTouching {get;}

        /// <summary>
        /// 
        /// </summary>
        bool HasTapOccurred {get;}

        /// <summary>
        /// 
        /// </summary>
        Vector2 TapScreenPosition {get;}


        /// <summary>
        /// 
        /// </summary>
        void UpdateInput();
    }
}