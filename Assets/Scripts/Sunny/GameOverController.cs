using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    public GameObject gameOverRig;          // parent under camera
    public Transform leftArm, rightArm;
    public Vector3 leftStartLocalPos, leftEndLocalPos;
    public Vector3 rightStartLocalPos, rightEndLocalPos;

    public GameObject gameOverUI;
    public float armMoveDuration = 1.2f;

    bool running = false; // flag to prevent multiple triggers

    public void TriggerGameOver()
    {
        if (!running)
            StartCoroutine(GameOverSequence());
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
        GameManager.Instance.BackToOpening();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))   // DEBUG: press G to test
        {
            TriggerGameOver();
        }
    }
}   