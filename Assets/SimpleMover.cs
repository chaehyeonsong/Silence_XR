using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMover : MonoBehaviour
{
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 1f;

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 180f;

    void Update()
    {
        // --- Movement on the XY plane ---
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;   // up
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;   // down
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;   // left
        if (Input.GetKey(KeyCode.D)) moveX += 1f;   // right

        Vector3 moveDir = new Vector3(moveX, moveY, 0f).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        // --- Rotation around Z axis ---
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.RightArrow))
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
