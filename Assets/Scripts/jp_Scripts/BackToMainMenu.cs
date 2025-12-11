using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{
    public GameManager gameManager;
    // Start is called before the first frame update
    public void ReturnToMain()
    {
        gameManager.BackToOpening();
    }

}
