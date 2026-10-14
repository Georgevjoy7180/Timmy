using UnityEngine;

public class CoinRotate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] float RotationSpeed = 1;
    void Update()
    {
        transform.Rotate(0, RotationSpeed, 0,Space.World);
    }
}
