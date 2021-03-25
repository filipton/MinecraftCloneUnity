using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float rotationspeed = 60;
    public float FlyingSpeed = 20;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //Pitch rotates the camera around its local Right axis
        transform.Rotate(Vector3.left * Input.GetAxis("Mouse Y") * rotationspeed);

        //Yaw rotates the camera around its local Up axis
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotationspeed);
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * FlyingSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * FlyingSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * FlyingSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * FlyingSpeed;
        }
    }
}