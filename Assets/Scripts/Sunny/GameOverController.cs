using SoftKitty.LiquidContainer;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{

    [Header("Game Flow")]
    [SerializeField] private GameManager gameManager; 

    public GameObject gameOverRig;          // parent under camera
    public Transform leftArm, rightArm;
    public Vector3 leftStartLocalPos, leftEndLocalPos;
    public Vector3 rightStartLocalPos, rightEndLocalPos;

    public GameObject gameOverUI;
    public GameObject jp_GamePrefab;
    public float armMoveDuration = 1.2f;

    bool running = false; // flag to prevent multiple triggers

    public void TriggerGameOver()
    {
        if (!running)
        {
            jp_GamePrefab.SetActive(false);
            DestroyAllRemnants();
            StartCoroutine(GameOverSequence());
            // Play sounds
            FindObjectOfType<GameOverAudio>().PlayGameOverSounds();
        }
    }

    void DestroyAllRemnants() // Any remaining flask or spray get destroyed, this code was taken
                              // from GameController
    {
        foreach (var flask in FindObjectsOfType<LiquidControl>())
        {
            Destroy(flask.gameObject);
        }

        foreach (var spray in FindObjectsOfType<ParticleSystem>())
        {
            Destroy(spray.gameObject);
        }
    }

    // turn off gameover rig
    public void HideGameOverRig()
    {
        running = false;

        if (gameOverRig)
            gameOverRig.SetActive(false);

        if (gameOverUI)
            gameOverUI.SetActive(false);
    }

    IEnumerator GameOverSequence()
    {
        running = true;

        // turn on rig
        gameOverRig.SetActive(true);

        if (gameOverRig)
            gameOverRig.SetActive(true);

        if (gameOverUI)
            gameOverUI.SetActive(false);

        // Reset arms to starting points
        leftArm.localPosition  = leftStartLocalPos;
        rightArm.localPosition = rightStartLocalPos;

        // Slide arms inward
        float t = 0f;
        while (t < armMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / armMoveDuration);

            leftArm.localPosition  = Vector3.Lerp(leftStartLocalPos,  leftEndLocalPos,  k);
            rightArm.localPosition = Vector3.Lerp(rightStartLocalPos, rightEndLocalPos, k);
            
            yield return null;
        }

        // After arms are closed → show UI
        if (gameOverUI)
            gameOverUI.SetActive(true);

        running = false;
    }

    // Called by Play Again button
    public void OnPlayAgain()
    {
        Debug.Log("GameOverController: OnPlayAgain called");
        if (gameManager != null)
        {
            Debug.Log("GameOverController: Calling BackToOpening on GameManager");
            gameManager.BackToOpening();
        }
        else
        {
            Debug.LogWarning("GameOverController: GameManager reference not set in Inspector");
        }
         // stop sounds
        var audio = FindObjectOfType<GameOverAudio>();
        if (audio != null)
            audio.StopGameOverSounds();
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.P))   // DEBUG: press G to test
    //    {
    //        TriggerGameOver();
    //    }
    //}
}   