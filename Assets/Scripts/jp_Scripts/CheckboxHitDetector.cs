using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckboxHitDetector : MonoBehaviour
{
    private Communicator_Checker parent;
    // Start is called before the first frame update
    public void Init(Communicator_Checker p)
    {
        parent = p;
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (parent.target_flask != null)
        {
            parent.flask_in_checkbox = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (parent.target_flask != null)
        {
            parent.flask_in_checkbox = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (parent.target_flask != null)
        {
            parent.flask_in_checkbox = true;
        }
    }
}
