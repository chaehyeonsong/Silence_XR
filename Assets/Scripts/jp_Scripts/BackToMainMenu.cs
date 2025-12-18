using SoftKitty.LiquidContainer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject jp_GamePrefab;
    // Start is called before the first frame update

    private void OnEnable()
    {
        jp_GamePrefab.SetActive(false);
    }

    public void ReturnToMain()
    {
        gameManager.BackToOpening();
    }

}
