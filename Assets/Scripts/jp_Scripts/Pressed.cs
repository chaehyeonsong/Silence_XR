using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pressed : MonoBehaviour
{
    public MainMenuController mainMenuController;

    public void buttonPressed()
    {
        mainMenuController.SkipOpening();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
