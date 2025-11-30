using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FlaskCollisionDetector : MonoBehaviour
{
    private GameController parent;
    private Rigidbody rb;
    [Tooltip("Collision whose speed is greater triggers game over")]
    public float speedlimit = 3f;
    [Tooltip("Initial duration in seconds during which flask doesn't register collision")]
    public float Initial_immune_time = 0f;

    public void Init(GameController p)
    {
        parent = p;

    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Flask collided");
        if (parent != null)
        {
            if (rb != null && rb.velocity.magnitude >= speedlimit)
            {
                parent.speedticket = true;
                
            }
        }

    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb.isKinematic)
        {
            Debug.LogWarning("Turn off flask isKinematic");
        }
    }

    //void Update()
    //{

    //}
}
