using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Pure logic helper for player movement calculations.
    /// Completely decoupled from Unity Input and Time singletons for EditMode unit testing.
    /// </summary>
    public static class PlayerMovementLogic
    {
        /// <summary>
        /// Calculates the next X position after applying horizontal input, clamped between limits.
        /// </summary>
        public static float CalculateNewXPosition(
            float currentX,
            float horizontalInput,
            float horizontalSpeed,
            float deltaTime,
            float leftLimit,
            float rightLimit)
        {
            float targetX = currentX + (horizontalInput * horizontalSpeed * deltaTime);
            return Mathf.Clamp(targetX, leftLimit, rightLimit);
        }

        /// <summary>
        /// Calculates forward displacement along the Z axis.
        /// </summary>
        public static float CalculateForwardDisplacement(float speed, float deltaTime)
        {
            return speed * deltaTime;
        }
    }
}
