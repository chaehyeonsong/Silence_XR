using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpButton : MonoBehaviour
{

    private bool isOn = false;
    public GameObject TutorialPrefab;
    private GameObject Tutorial;
    public Vector3 spawnOffset;
    public Vector3 scaler;

    void Start()
    {
        
    }

    public void whenPressed()
    {

        if (isOn == false)
        {

            Tutorial = Instantiate(TutorialPrefab);
            Tutorial.transform.position = transform.position + spawnOffset;
            Tutorial.transform.localScale = scaler;
            isOn = !isOn;

        }
        else if (isOn == true)
        {

            Destroy(Tutorial);
            isOn = !isOn;

        }
    }

}
