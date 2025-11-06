using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private CurveVisualizer parent;
    private int index;

    public void Init(CurveVisualizer p, int i)
    {
        parent = p;
        index = i;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parent.targetObject == null) return;

        if (other.gameObject == parent.targetObject)
        {
            parent.collided_spheres[index] = 1;
            Debug.Log("Triggered, num: " + index.ToString());
        }
    }
}
