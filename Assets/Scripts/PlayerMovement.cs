using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float PlayerSpeed = 5f;
    public float HorizontalSpeed = 3f;
    public float LeftLimit = -9f;
    public float RightLimit = 10f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * PlayerSpeed * Time.deltaTime, Space.World);
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {   if (this.gameObject.transform.position.x > LeftLimit)
            {
                transform.Translate(Vector3.left * PlayerSpeed * Time.deltaTime, Space.World);
            }
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {   if (this.gameObject.transform.position.x < RightLimit)
            {
                transform.Translate(Vector3.right * PlayerSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
