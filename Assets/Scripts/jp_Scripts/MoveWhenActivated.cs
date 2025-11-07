using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWhenActivated : MonoBehaviour
{
    public Vector3 direction = Vector3.forward; // movement direction
    public float speed = 2f;
    public CurveVisualizer curve_script;

    [HideInInspector]
    public bool isActive = true;

    void Update()
    {
        if (isActive == true & curve_script.finish_broadcast == false)
            transform.position += direction.normalized * speed * Time.deltaTime;
    }
}