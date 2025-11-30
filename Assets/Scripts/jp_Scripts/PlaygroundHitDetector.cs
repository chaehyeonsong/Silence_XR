using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlaygroundHitDetector : MonoBehaviour
{
    private Communicator parent;

    
    public void Init(Communicator p)
    {
        parent = p;
    }
       
    private void OnTriggerEnter(Collider other)
    {
        
        if (parent.Playground == null) return;

        if (other.gameObject == parent.Playground)
        {
            parent.out_of_playground = true;
            Debug.Log("Playground leave triggered");
        }
        
    }
}
