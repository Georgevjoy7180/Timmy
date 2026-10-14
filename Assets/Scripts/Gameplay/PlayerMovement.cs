using UnityEngine;
using Gameplay;

public class PlayerMovement : MonoBehaviour
{
    public float PlayerSpeed = 5f;
    public float HorizontalSpeed = 3f;
    public float LeftLimit = -9f;
    public float RightLimit = 10f;

    void Update()
    {
        // 1. Calculate forward displacement via pure logic
        float forwardDelta = PlayerMovementLogic.CalculateForwardDisplacement(PlayerSpeed, Time.deltaTime);
        transform.Translate(Vector3.forward * forwardDelta, Space.World);

        // 2. Poll Unity input
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }

        // 3. Compute clamped position via pure logic
        if (horizontalInput != 0f)
        {
            float newX = PlayerMovementLogic.CalculateNewXPosition(
                transform.position.x,
                horizontalInput,
                PlayerSpeed,
                Time.deltaTime,
                LeftLimit,
                RightLimit
            );

            Vector3 pos = transform.position;
            pos.x = newX;
            transform.position = pos;
        }
    }
}
