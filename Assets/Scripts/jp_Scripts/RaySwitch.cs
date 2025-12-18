using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RaySwitch : MonoBehaviour
{
    private Transform Ray;
    [HideInInspector]
    public bool isGamePlaying = false;

    public void RayOff()
    {
        Ray.gameObject.SetActive(false);
    }
    public void RayOn()
    {
        Ray.gameObject.SetActive(true);
    }

    public void Init() // This code prevents null exception error when GameManager calls RayOn()
                       // before code below has found Ray Interactor and set Ray to it.
    {
        Ray = transform.Find("Ray Interactor");
    }

    void Update() // Added this code. The Ray Interactor turns on when grab button is pressed.
                  // I think this is a bug with XR, not our development.
    {
        if (isGamePlaying)
        {
            RayOff();
        }
    }
}
