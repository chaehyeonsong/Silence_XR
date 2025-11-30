using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private NewCurveVisualizer parent;
    private int index;

    public void Init(NewCurveVisualizer p, int i)
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
