using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverCaller : MonoBehaviour
{
    public GameOverController gameOverController;

    public void CallGameOver()
    {
        gameOverController.OnPlayAgain();
    }
    // Start is called before the first frame update
    //void Start()
    //{
        
    //}

    // Update is called once per frame
    //void Update()
    //{
        
    //}
}
