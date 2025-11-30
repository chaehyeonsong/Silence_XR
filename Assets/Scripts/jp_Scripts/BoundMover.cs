using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoftKitty.LiquidContainer;

public class BoundMover : MonoBehaviour
{
    [HideInInspector]
    public LiquidControl flask_to_track; //followes position and direction of liquid puzzle
    public GameObject Linetracer;
    public float speed = 2f;
    [HideInInspector]
    public Vector3 normal; //this is a normalized vector in xz plane, no y component
    [HideInInspector]
    public Vector3 prev_pos;
    [HideInInspector]
    public Vector3 rotated_normal;

    void Start()
    {
        prev_pos = flask_to_track.transform.position;
        rotated_normal = new Vector3(normal.z, 0f, -normal.x);
    }

    void Update()
    {
        Vector3 current_pos = flask_to_track.transform.position;
        Vector3 displacement = current_pos - prev_pos;

        Vector3 move = new Vector3(Vector3.Dot(displacement, rotated_normal), 
                                   Vector3.Dot(displacement, Vector3.up), 0f) * speed;

        transform.localPosition += move;

        prev_pos = current_pos;
    }
}
