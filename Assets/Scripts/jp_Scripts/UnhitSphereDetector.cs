using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnhitSphereDetector : MonoBehaviour
{
    private PassFailDisplay parent;
    
    public void Init(PassFailDisplay p)
    {
        parent = p;
    }

    
    private void OnTriggerEnter(Collider other)
    {
        MeshRenderer m = other.GetComponent<MeshRenderer>();

        if (m.enabled == false)
        {
            parent.missed_line = true;
            Debug.Log("Unhit sphere triggered");
        }
    }
}
