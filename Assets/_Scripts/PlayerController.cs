using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController singleton;

    public LayerMask layerMask;

    public float rotationspeed = 60;
    public float FlyingSpeed = 20;
    public float HandRange = 5;

    void Start()
    {
        singleton = this;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
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

	private void Update()
	{
        transform.Rotate(Vector3.left * Input.GetAxis("Mouse Y") * rotationspeed);

        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotationspeed);

        RaycastHit Hit;
        Ray dir = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(dir, out Hit, HandRange, layerMask))
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Vector3 hitCoord = new Vector3(Hit.point.x, Hit.point.y, Hit.point.z);
                hitCoord += (new Vector3(Hit.normal.x, Hit.normal.y, Hit.normal.z)) * -0.5f;

                int x = Mathf.RoundToInt(hitCoord.x);
                int y = Mathf.RoundToInt(hitCoord.y);
                int z = Mathf.RoundToInt(hitCoord.z);

                GeneratorCore.SetBlock(x, y, z, BlockType.Air, true, true);
            }
            else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Vector3 placeCoord = new Vector3(Hit.point.x, Hit.point.y, Hit.point.z);
                placeCoord += (new Vector3(Hit.normal.x, Hit.normal.y, Hit.normal.z)) * 0.5f;

                int px = Mathf.RoundToInt(placeCoord.x);
                int py = Mathf.RoundToInt(placeCoord.y);
                int pz = Mathf.RoundToInt(placeCoord.z);

                //GeneratorCore.SetBlock(px, py, pz, BlockType.IronOre);
                GeneratorCore.SetTree(px, py, pz);
            }
        }
    }
}